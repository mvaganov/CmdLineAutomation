using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	[System.Serializable]
	public class AutomationExecutor {
		public CommandLineSettings _settings;
		public int commandExecutingIndex = 0;
		int filterIndex;
		public List<string> _currentCommands = new List<string>();
		public string currentCommandText;
		public bool cancelled;
		/// <summary>
		/// Result of the last finished cooperative function
		/// </summary>
		public string currentCommandResult;
		/// <summary>
		/// Which cooperative function is being executed right now
		/// </summary>
		private ICommandFilter currentCommand;
		/// <summary>
		/// The list of commands and filters this automation is executing
		/// </summary>
		internal ICommandExecutor source;
		/// <summary>
		/// Function to pass all lines from standard output to
		/// </summary>
		public PrintCallback print;
		/// <summary>
		/// Keeps track of a shell, if one is generated
		/// </summary>
		private OperatingSystemCommandShell _shell;

		public bool HaveCommandToDo() => currentCommand != null;
		public IList<ICommandFilter> Filters => source.Filters;
		public object Context => this;
		public OperatingSystemCommandShell Shell => _shell;

		private void EndCurrentCommand() {
			if (currentCommand is CommandRunnerBase runner) {
				runner.RemoveExecutionData(Context);
			}
			filterIndex = 0;
			currentCommand = null;
		}

		public void ExecuteCurrentCommand() {
			Debug.Log($"executing {currentCommandText}");
			// TODO
			StartCooperativeFunction(currentCommandText, print);
			if (IsCancelled()) {
				//Debug.Log("----------CANCELLED");
				EndCurrentCommand();
				return;
			}
			// get the list of filters
			// pass current command through each filter until it is executed on a filter that consumes
			if (!HaveCommandToDo() || currentCommand.IsExecutionFinished(Context)) {
				++commandExecutingIndex;
			}
			//if (HaveCommandToDo() && !_currentCommand.IsExecutionFinished()) { Debug.Log("       still doing it!"); }
		}

		public void StartCooperativeFunction(string command, PrintCallback print) {
			this.print = print;
			currentCommandText = command;
			currentCommandResult = command;
			filterIndex = 0;
			DoCurrentCommand();
		}

		private void DoCurrentCommand() {
			if (currentCommand != null && !currentCommand.IsExecutionFinished(Context)) {
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
					if (Context == null) {
						Debug.LogError("context must not be null!");
					}
					//Debug.Log($"~~~ [{filterIndex}] {commandObj.name}\n\n{currentCommandText}\n\n");
					currentCommand.StartCooperativeFunction(Context, currentCommandText, OutputAnalysis);
					if (filterIndex < 0) {
						Object commandObj = currentCommand as Object;
						Debug.Log($"~~~ CANCEL [{filterIndex}] {commandObj.name}\n\n{currentCommandText}\n\n");
						return true;
					}
					if ((_shell == null || !_shell.IsRunning) && currentCommand is FilterOperatingSystemCommandShell osShell) {
						_shell = osShell.Shell;
					}
				}
				if (currentCommand != null && !currentCommand.IsExecutionFinished(Context)) {
					Object commandObj = currentCommand as Object;
					ICommandFilter filter = commandObj as ICommandFilter;
					if (filter != null) {
						ICommandProcessor subCommand = filter.GetReferencedCommand(Context);
						Object subCommandObj = subCommand as Object;
						Debug.Log($"~~~ more [{filterIndex}] {commandObj.name} -> {subCommandObj.name}\n\n{currentCommandText}\n\n");
					} else {
						Debug.Log($"~~~ more [{filterIndex}] {commandObj.name}\n\n{currentCommandText}\n\n");
					}
					return true;
				}
				currentCommandResult = currentCommand.FunctionResult(Context);
				if (currentCommandResult == null) {
					Debug.Log($"@@@@@ {currentCommandText} consumed by {Filters[filterIndex]}");
					return false;
				} else if (currentCommandResult != currentCommandText) {
					Debug.Log($"@@@@@ {currentCommandText} changed into {currentCommandResult} by {Filters[filterIndex]} {currentCommand}");
					currentCommandText = currentCommandResult;
				}
				currentCommand = null;
				++filterIndex;
			}
			//Debug.Log($"{currentCommandText} NOT consumed");
			return false;
		}

		private void OutputAnalysis(string fromProcess) {
			source.AddToCommandOutput(fromProcess); // this is where the printing happens.
			print?.Invoke(fromProcess);
		}

		//public void StartRunningCurrentCommand() {
		//	cancelled = false;
		//	RunEachCommandInSequence();
		//}

		public bool IsCancelled() => cancelled || commandExecutingIndex < 0;

		//private void RunEachCommandInSequence() {
		//	//Debug.Log($"ComandsToDo {CommandsToDo.Count} Sequence");
		//	if (cancelled) {
		//		return;
		//	}
		//	if (HaveCommandToDo()) {
		//		if (currentCommand.IsExecutionFinished(Context)) {
		//			EndCurrentCommand();
		//			++commandExecutingIndex;
		//		} else {
		//			CommandDelay.DelayCall(RunEachCommandInSequence);
		//			return;
		//		}
		//	}
		//	if (!HaveCommandToDo()) {
		//		if (IsCancelled()) {
		//			//Debug.Log("----------CANCELLED");
		//			EndCurrentCommand();
		//			return;
		//		}
		//		ParsedTextCommand nextCommand = commandExecutingIndex < CommandsToDo.Count ? CommandsToDo[commandExecutingIndex] : null;
		//		if (nextCommand != null && !nextCommand.Ignore) {
		//			string textToDo = nextCommand.Text;
		//			filterIndex = 0;
		//			//Debug.Log("execute " + _commandExecutingIndex+" "+ textToDo);
		//			StartCooperativeFunction(textToDo, print);
		//			if (IsCancelled()) {
		//				//Debug.Log("----------CANCELLED");
		//				EndCurrentCommand();
		//				return;
		//			}
		//			//if (HaveCommandToDo() && !_currentCommand.IsExecutionFinished()) { Debug.Log("       still doing it!"); }
		//		}
		//		if (!HaveCommandToDo() || currentCommand.IsExecutionFinished(context)) {
		//			++commandExecutingIndex;
		//		}
		//	} else {
		//		DoCurrentCommand();
		//		if (currentCommand == null) {
		//			++commandExecutingIndex;
		//		}
		//	}
		//	if (commandExecutingIndex >= 0 && commandExecutingIndex < CommandsToDo.Count) {
		//		CommandDelay.DelayCall(RunEachCommandInSequence);
		//	} else {
		//		commandExecutingIndex = 0;
		//	}
		//}
	}
}
