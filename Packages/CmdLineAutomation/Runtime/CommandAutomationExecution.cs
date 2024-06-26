using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RunCmd {
	//public partial class CommandAutomation {
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
			private ICommandFilter currentCommand;
			/// <summary>
			/// Result of the last finished cooperative function
			/// </summary>
			public string currentCommandResult;
			/// <summary>
			/// Which <see cref="_commandFilters"/> is being cooperatively processed right now
			/// </summary>
			private int filterIndex = 0;
			/// <summary>
			/// Function to pass all lines from standard output to
			/// </summary>
			public PrintCallback print;
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

			private Dictionary<string, NamedRegexSearch> _regexSearches = new Dictionary<string, NamedRegexSearch>();

			private void OutputAnalysis(string fromProcess) {
				source.AddToCommandOutput(fromProcess); // this is where the printing happens.
				print?.Invoke(fromProcess);
				foreach (var kvp in _regexSearches) {
					string value = kvp.Value.Process(fromProcess);
					if (value != null) {
						int index = FindIndex(source.VariablesFromCommandLineRegexSearch, 0, r => r.Name == kvp.Key);
						if (index >= 0) {
							source.VariablesFromCommandLineRegexSearch[index].RuntimeValue = value;
						}
					}
				}
				int FindIndex<T>(IList<T> source, int startIndex, System.Func<T, bool> predicate) {
					for (int i = startIndex; i < source.Count; i++) {
						if (predicate.Invoke(source[i])) { return i; }
					}
					return -1;
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

			public int CommandIndex {
				get => commandExecutingIndex;
				set => commandExecutingIndex = value;
			}

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
				RefreshRegexSearch();
				RunEachCommandInSequence();
			}

			private void RefreshRegexSearch() {
				_regexSearches.Clear();
				for (int i = 0; i < source.VariablesFromCommandLineRegexSearch.Count; ++i) {
					NamedRegexSearch regexSearch = source.VariablesFromCommandLineRegexSearch[i];
					_regexSearches[regexSearch.Name] = regexSearch;
				}
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
					CommandLineSettings.DelayCall(RunEachCommandInSequence);
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
						StartCooperativeFunction(textToDo, print);
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
					CommandLineSettings.DelayCall(RunEachCommandInSequence);
				} else {
					commandExecutingIndex = 0;
				}
			}

			/// <inheritdoc/>
			public void StartCooperativeFunction(string command, PrintCallback print) {
				this.print = print;
				currentCommandText = command;
				currentCommandResult = command;
				filterIndex = 0;
				if (source.NeedsInitialization()) {
					source.Initialize();
				}
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

			public CommandExecution(object context, ICommandAutomation commandAutomation) {
				source = commandAutomation.CommandExecutor;
				this.context = context;
				commandExecutingIndex = 0;
			}
		}
	//}
}
