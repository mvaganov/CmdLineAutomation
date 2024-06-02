using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RunCmd {
	public partial class CommandAutomation {
		public class CommandExecution {
			/// <summary>
			/// What object counts as the owner of this command
			/// </summary>
			public object context;
			/// <summary>
			/// Which command from <see cref="CommandsToDo"/> is being executed right now
			/// </summary>
			public int commandExecutingIndex;
			/// <summary>
			/// Text of the current command
			/// </summary>
			public string currentCommandText;
			/// <summary>
			/// Which cooperative function is being executed right now
			/// </summary>
			public ICommandFilter currentCommand;
			/// <summary>
			/// Result of the last finished cooperative function
			/// </summary>
			public string currentCommandResult;
			/// <summary>
			/// Which <see cref="_commandFilters"/> is being cooperatively processed right now
			/// </summary>
			public int filterIndex = 0;
			/// <summary>
			/// Function to pass all lines from standard output to
			/// </summary>
			public TextResultCallback stdOutput;
			/// <summary>
			/// used when getchoice or some other command needs to adjust the commands as they are being executed
			/// </summary>
			private TextCommand modifiedTextCommand;
			/// <summary>
			/// The list of commands and filters this automation is executing
			/// </summary>
			private CommandAutomation source;
			/// <summary>
			/// Keeps track of a shell, if one is generated
			/// </summary>
			private OperatingSystemCommandShell _shell;
			/// <summary>
			/// Explicit process cancel
			/// </summary>
			private bool cancelled = false;
			/// <summary>
			/// records output
			/// </summary>
			private StringBuilder outputToScan = new StringBuilder();

			// TODO move this command line regex analysis to OperatingSystemCommandShell
			// TODO make a data structure that can set variables by looking for specific regex patterns
			private const string CommandPromptRegex =
				"^[A-Z]:\\\\(?:[^\\\\/:*?\" <>|\\r\\n]+\\\\)*[^\\\\/:*? \"<>|\\r\\n]*>";

			[System.Serializable] public class RegexSearch {
				/// <summary>
				/// Need help writing a regular expression? Ask ChatGPT! (I wonder how well this comment will age)
				/// </summary>
				public string Regex;
				/// <summary>
				/// leave empty to get the entire match
				/// </summary>
				public int[] GroupsToInclude;
				public RegexSearch(string regex) : this(regex, null) { }
				public RegexSearch(string regex, int[] groupsToInclude) {
					Regex = regex;
					GroupsToInclude = groupsToInclude;
				}
				public string Process(string input) {
					Match m = System.Text.RegularExpressions.Regex.Match(input, Regex);
					if (!m.Success) {
						return null;
					}
					//Debug.LogWarning($"success {Regex}\n{input}\n{m.Value}");
					if (GroupsToInclude == null || GroupsToInclude.Length == 0) {
						return m.Value;
					}
					StringBuilder sb = new StringBuilder();
					for(int i = 0; i < GroupsToInclude.Length; ++i) {
						sb.Append(m.Groups[GroupsToInclude[i]]);
					}
					return sb.ToString();
				}
				public static implicit operator RegexSearch(string regex) => new RegexSearch(regex);
			}

			private void OutputAnalysis(string fromProcess) {
				Dictionary<string, RegexSearch> regexSearches = new Dictionary<string, RegexSearch>() {
					["WindowsTerminalVersion"] = new RegexSearch(@"Microsoft Windows \[Version ([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\]", new int[]{ 1 }),
					["dir"] = CommandPromptRegex,
				};
				outputToScan.AppendLine(fromProcess);
				stdOutput?.Invoke(fromProcess);
				foreach (var kvp in regexSearches) {
					string value = kvp.Value.Process(fromProcess);
					if (value != null) {
						Debug.LogWarning($"success {kvp.Key}\n{fromProcess}\n{value}");
					}
				}
			}

			public IList<ParsedTextCommand> CommandsToDo {
				get {
					if (modifiedTextCommand == null) {
						return source.CommandsToDo;
					}
					return modifiedTextCommand.ParsedCommands;
				}
			}

			public IList<ICommandFilter> Filters => source.Filters;

			public float Progress {
				get {
					ICommandFilter cmd = CurrentCommand();
					float cmdProgress = cmd != null ? cmd.Progress(context) : 0;
					if (cmdProgress > 0) { return cmdProgress; }
					if (commandExecutingIndex < 0 || source == null || CommandsToDo == null) return 0;
					float majorProgress = (float)commandExecutingIndex / CommandsToDo.Count;
					float minorTotal = 1f / CommandsToDo.Count;
					float minorProgress = Filters != null ?
						minorTotal * filterIndex / Filters.Count : 0;
					return majorProgress + minorProgress;
				}
			}

			public OperatingSystemCommandShell Shell { get => _shell; }

			public bool HaveCommandToDo() => currentCommand != null;

			public bool IsCancelled() => cancelled || commandExecutingIndex < 0;

			public string CurrentCommandText() => currentCommandText;

			public ICommandFilter CurrentCommand() => currentCommand;

			public void CancelExecution() {
				EndCurrentCommand();
				commandExecutingIndex = -1;
				cancelled = true;
				//Debug.Log("CANCELLED");
			}

			private void EndCurrentCommand() {
				if (currentCommand is CommandRunnerBase runner) {
					runner.RemoveExecutionData(context);
				}
				filterIndex = 0;
				currentCommand = null;
			}

			public void StartRunningEachCommandInSequence() {
				cancelled = false;
				modifiedTextCommand = null;
				commandExecutingIndex = 0;
				RunEachCommandInSequence();
			}

			public void InsertNextCommandToExecute(string command) {
				if (modifiedTextCommand == null) {
					modifiedTextCommand = source.TextCommandData.CloneSelf();
				}
				modifiedTextCommand.ParsedCommands.Insert(commandExecutingIndex + 1, new ParsedTextCommand(command));
			}

			private void RunEachCommandInSequence() {
				if (cancelled) {
					return;
				}
				if (HaveCommandToDo()) {
					if (currentCommand.IsExecutionFinished(context)) {
						EndCurrentCommand();
						++commandExecutingIndex;
					} else {
						DelayCall(RunEachCommandInSequence);
						return;
					}
				}
				if (!HaveCommandToDo()) {
					if (IsCancelled()) {
						//Debug.Log("----------CANCELLED");
						EndCurrentCommand();
						return;
					}
					string textToDo = CommandsToDo[commandExecutingIndex].Text;
					if (!CommandsToDo[commandExecutingIndex].Ignore) {
						filterIndex = 0;
						//Debug.Log("execute " + _commandExecutingIndex+" "+ textToDo);
						StartCooperativeFunction(textToDo, stdOutput);
						if (IsCancelled()) {
							//Debug.Log("----------CANCELLED");
							EndCurrentCommand();
							return;
						}
						//if (HaveCommandToDo() && !_currentCommand.IsExecutionFinished()) { Debug.Log("       still doing it!"); }
					}
					if (!HaveCommandToDo() || currentCommand.IsExecutionFinished(context)) {
						++commandExecutingIndex;
					}
				} else {
					DoCurrentCommand();
					if (currentCommand == null) {
						++commandExecutingIndex;
					}
				}
				if (commandExecutingIndex >= 0 && commandExecutingIndex < CommandsToDo.Count) {
					DelayCall(RunEachCommandInSequence);
				} else {
					commandExecutingIndex = 0;
					if (source._recapOutputAtEnd) {
						Debug.Log(outputToScan);
					}
				}
			}

			/// <inheritdoc/>
			public void StartCooperativeFunction(string command, TextResultCallback stdOutput) {
				this.stdOutput = stdOutput;
				currentCommandText = command;
				currentCommandResult = command;
				filterIndex = 0;
				DoCurrentCommand();
			}

			private void DoCurrentCommand() {
				if (currentCommand != null && !currentCommand.IsExecutionFinished(context)) {
					Debug.Log($"still processing {currentCommand}");
					return;
				}
				//Debug.Log("processing " + _currentCommandText);
				if (IsExecutionStoppedByFilterFunction()) {
					return;
				}
				currentCommand = null;
				filterIndex = 0;
			}

			private bool IsExecutionStoppedByFilterFunction() {
				while (filterIndex < Filters.Count) {
					if (currentCommand == null) {
						currentCommand = Filters[filterIndex];
						if (context == null) {
							Debug.LogError("context must not be null!");
						}
						//Debug.Log($"~~~~~~~~{name} start {command} co-op f[{_filterIndex}] {_currentCommand}\n\n{_currentCommandText}");
						currentCommand.StartCooperativeFunction(context, currentCommandText, OutputAnalysis);
						if (filterIndex < 0) {
							return true;
						}
						if ((_shell == null || !_shell.IsRunning) && currentCommand is FilterOperatingSystemCommandShell osShell) {
							_shell = osShell.Shell;
						}
					}
					if (currentCommand == null || !currentCommand.IsExecutionFinished(context)) {
						return true;
					}
					currentCommandResult = currentCommand.FunctionResult(context);
					currentCommand = null;
					if (currentCommandResult == null) {
						//Debug.Log($"@@@@@ {currentCommandText} consumed by {Filters[filterIndex]}");
						return false;
					} else if (currentCommandResult != currentCommandText) {
						//Debug.Log($"@@@@@ {currentCommandText} changed into {currentCommandResult} by {Filters[filterIndex]}");
						currentCommandText = currentCommandResult;
					}
					++filterIndex;
				}
				//Debug.Log($"{currentCommandText} NOT consumed");
				return false;
			}

			public bool IsExecutionFinished() => currentCommand == null || currentCommand.IsExecutionFinished(context);

			public string FunctionResult() => currentCommand != null ? currentCommand.FunctionResult(context) : currentCommandResult;

			public CommandExecution(object context, CommandAutomation commandAutomation) {
				source = commandAutomation;
				this.context = context;
				commandExecutingIndex = 0;
			}
		}
	}
}
