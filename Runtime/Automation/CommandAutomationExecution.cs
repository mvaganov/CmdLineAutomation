using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// This is the memory required to run a command instruction list
	/// TODO rename ProcedureExecution?
	/// </summary>
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
		/// The list of commands and filters this automation is executing
		/// </summary>
		private ICommandExecutor source;
		/// <summary>
		/// Keeps track of a shell, if one is generated
		/// </summary>
		private OperatingSystemCommandShell _shell;
		/// <summary>
		/// Explicit process cancel
		/// </summary>
		private bool cancelled = false;

		/// <summary>
		/// local copy of the commands to do. other logic can modify this list
		/// </summary>
		private List<ParsedTextCommand> _commandsToDo;

		private void OutputAnalysis(string fromProcess) {
			source.AddToCommandOutput(fromProcess); // this is where the printing happens.
			print?.Invoke(fromProcess);
		}

		public IList<ParsedTextCommand> CommandsToDo {
			get => _commandsToDo;
			set => _commandsToDo = new List<ParsedTextCommand>(value);
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

		public void StartRunningEachCommandInSequence(IList<ParsedTextCommand> commands) {
			cancelled = false;
			CommandsToDo = commands;
			//Debug.LogWarning($"~~[{source}]({source.GetHashCode()}) providing {nameof(CommandsToDo)}({_commandsToDo.Count})");
			commandExecutingIndex = 0;
			RunEachCommandInSequence();
			//Debug.LogWarning($"--[{source}]({source.GetHashCode()}) providing {nameof(CommandsToDo)}({_commandsToDo.Count})");
		}

		public void InsertNextCommandToExecute(string command) {
			if (_commandsToDo == null) {
				CommandLineExecutor cle = source as CommandLineExecutor;

				throw new System.Exception($"[{source}]({source.GetHashCode()}) did not provide {nameof(CommandsToDo)}... should be [{cle.CommandsToDo}]");
			}
			_commandsToDo.Insert(commandExecutingIndex + 1, new ParsedTextCommand(command));
		}

		private void RunEachCommandInSequence() {
			//Debug.Log($"ComandsToDo {CommandsToDo.Count} Sequence");
			if (cancelled) {
				return;
			}
			if (HaveCommandToDo()) {
				if (currentCommand.IsExecutionFinished(context)) {
					EndCurrentCommand();
					++commandExecutingIndex;
				} else {
					CommandDelay.DelayCall(RunEachCommandInSequence);
					return;
				}
			}
			if (!HaveCommandToDo()) {
				if (IsCancelled()) {
					//Debug.Log("----------CANCELLED");
					EndCurrentCommand();
					return;
				}
				ParsedTextCommand nextCommand = commandExecutingIndex < CommandsToDo.Count ? CommandsToDo[commandExecutingIndex] : null;
				if (nextCommand != null && !nextCommand.Ignore) {
					string textToDo = nextCommand.Text;
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
				CommandDelay.DelayCall(RunEachCommandInSequence);
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
			if (source == null) {
				throw new System.Exception("Missing execution source");
			}
			if (source.Filters == null) {
				throw new System.Exception($"missing filters {source}");
			}
			while (filterIndex < Filters.Count) {
				if (currentCommand == null) {
					currentCommand = Filters[filterIndex];
					if (context == null) {
						Debug.LogError("context must not be null!");
					}
					Debug.Log($"~~~~~~~~ start {currentCommand} co-op f[{filterIndex}] {currentCommand}\n\n{currentCommandText}");
					currentCommand.StartCooperativeFunction(context, currentCommandText, OutputAnalysis);
					if (filterIndex < 0) {
						return true;
					}
					if ((_shell == null || !_shell.IsRunning) && currentCommand is FilterOperatingSystemCommandShell osShell) {
						_shell = osShell.Shell;
					}
				}
				if (currentCommand != null && !currentCommand.IsExecutionFinished(context)) {
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

		public CommandExecution(object context, ICommandExecutor commandExecutor) {
			//UnityEngine.Debug.LogWarning($"######## new Execution {context} by {commandExecutor}");
			source = commandExecutor;
			if (source == null) {
				throw new System.Exception($"unable to execute {commandExecutor}, need a {nameof(ICommandExecutor)}");
			}
			this.context = context;
			commandExecutingIndex = 0;
		}
	}
}
