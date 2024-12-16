using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmdRedux {
	// redux
	[System.Serializable]
	public class AutomationExecutor : ICommandAssetExecutor, ICommandAssetAutomation, ICommandProcessReference {
		public CommandAssetSettings _settings;
		public int commandExecutingIndex = 0;
		private int commandAssetIndex;
		public List<string> _currentCommands = new List<string>();
		public bool cancelled;
		/// <summary>
		/// Text of the current command
		/// </summary>
		public string currentCommandText;
		/// <summary>
		/// Results of commands
		/// </summary>
		[SerializeField] protected string commandOutput;
		/// <summary>
		/// Result of the command that could filter the current command into something else
		/// </summary>
		public string currentCommandAfterFilter;
		/// <summary>
		/// Which cooperative function is being executed right now
		/// </summary>
		private ICommandProcess currentCommand;
		/// <summary>
		/// The list of commands and filters this automation is executing
		/// </summary>
		internal ICommandAssetExecutor source;
		/// <summary>
		/// Function to pass all lines from standard output to
		/// </summary>
		public PrintCallback print;
		/// <summary>
		/// Keeps track of a shell, if one is generated
		/// </summary>
		private OperatingSystemCommandShell _shell;

		public bool HaveCommandToDo() => currentCommand != null;
		public IList<ICommandAsset> CommandAssets => source.CommandAssets;
		public object Context => this;
		public OperatingSystemCommandShell Shell => _shell;

		public string CommandOutput { get => commandOutput; set => commandOutput = value; }

		public ICommandAssetExecutor CommandExecutor => this;

		public bool IsExecuting => HaveCommandToDo();
		public float Progress => currentCommand.GetProgress();
		public ICommandProcess ReferencedCommand => currentCommand;
		public ICommandProcess CurrentCommandEnd {
			get {
				ICommandProcess cursor = currentCommand;
				while (cursor is ICommandProcessReference automation) {
					cursor = automation.ReferencedCommand;
				}
				return cursor;
			}
		}

		private void EndCurrentCommand() {
			throw new System.Exception("implement me");
			//if (currentCommand is CommandRunnerBase runner) {
			//	runner.RemoveExecutionData(Context);
			//}
			//commandAssetIndex = 0;
			//currentCommand = null;
		}

		public void ExecuteCurrentCommand() {
			Debug.Log($"executing {currentCommandText}");
			// TODO TODO TODO do me next
			// TODO go through filters, including the filter that finds named commands

			//StartCooperativeFunction(currentCommandText, print);
			if (IsCancelled()) {
				Debug.Log("----------CANCELLED");
				EndCurrentCommand();
				return;
			}
			// get the list of filters
			// pass current command through each filter until it is executed on a filter that consumes
			if (!HaveCommandToDo() || currentCommand.IsExecutionFinished) {
				++commandExecutingIndex;
			}
			//if (HaveCommandToDo() && !_currentCommand.IsExecutionFinished()) { Debug.Log("       still doing it!"); }
		}

		// TODO replace
		public void StartCooperativeFunction(string command, PrintCallback print) {
			this.print = print;
			currentCommandText = command;
			currentCommandAfterFilter = command;
			commandAssetIndex = 0;
			DoCurrentCommand();
		}

		// TODO replace
		private void DoCurrentCommand() {
			if (currentCommand != null && !currentCommand.IsExecutionFinished) {
				Debug.Log($"still processing {currentCommand}");
				return;
			}
			//Debug.Log("processing " + currentCommandText);
			if (IsExecutionStoppedByFilterFunction()) {
				return;
			}
			currentCommand = null;
			commandAssetIndex = 0;
		}

		// TODO replace
		private bool IsExecutionStoppedByFilterFunction() {
			if (source == null) {
				throw new System.Exception("Missing execution source");
			}
			if (source.CommandAssets == null) {
				throw new System.Exception($"missing filters {source}");
			}
			int loopguard = 0;
			while (commandAssetIndex < CommandAssets.Count) {
				if (loopguard++ > 100) {
					throw new System.Exception("executor loop guard");
				}
				if (currentCommand != null && currentCommand.IsExecutionFinished) {
					currentCommand = null;
				}
				if (currentCommand == null) {
					ICommandAsset commandAsset = CommandAssets[commandAssetIndex];
					currentCommand = commandAsset.GetCommandCreateIfMissing(Context);
					if (currentCommand == null) {
						Debug.LogError($"failed to create process from: {commandAsset}");
					}
					//Debug.Log($"~~~ [{filterIndex}] {commandObj.name}\n\n{currentCommandText}\n\n");
					currentCommand.StartCooperativeFunction(currentCommandText, OutputAnalysis);
					//if (filterIndex < 0) {
					//	Object commandObj = currentCommand as Object;
					//	Debug.Log($"~~~ CANCEL [{filterIndex}] {commandObj.name}\n\n{currentCommandText}\n\n");
					//	return true;
					//}

					// TODO implement the redux version of FilterOperatingSystemCommandShell
					//if ((_shell == null || !_shell.IsRunning) && currentCommand is FilterOperatingSystemCommandShell osShell) {
					//	_shell = osShell.Shell;
					//}
				}
				bool currentCommandStillExecuting = currentCommand != null && !currentCommand.IsExecutionFinished;
				if (currentCommandStillExecuting) {
					Object commandObj = currentCommand as Object;
					ICommandFilter filter = commandObj as ICommandFilter;
					if (filter != null) {
						ICommandProcessor subCommand = filter.GetReferencedCommand(Context);
						Object subCommandObj = subCommand as Object;
						Debug.Log($"~~~ executing [{commandAssetIndex}] {commandObj.name} -> {subCommandObj.name}\n\n{currentCommandText}\n\n");
					} else {
						Debug.Log($"~~~ executing [{commandAssetIndex}] {commandObj.name}\n\n{currentCommandText}\n\n");
					}
					return true;
				}
				ICommandFilter commandFilter = currentCommand as ICommandFilter;
				currentCommandAfterFilter = (commandFilter != null) ? commandFilter.FilterResult(Context) : null;
				Debug.Log($"{commandAssetIndex} {CommandAssets[commandAssetIndex]}     {currentCommandText} -> {currentCommandAfterFilter}");
				if (currentCommandAfterFilter == null) {
					Debug.Log($"@@@@@ {currentCommandText} consumed by {CommandAssets[commandAssetIndex]}");
					return false;
				} else if (currentCommandAfterFilter != currentCommandText) {
					Debug.Log($"@@@@@ {currentCommandText} changed into {currentCommandAfterFilter} by {CommandAssets[commandAssetIndex]} {currentCommand}  (ctx {Context})");
					currentCommandText = currentCommandAfterFilter;
				}
				currentCommand = null;
				++commandAssetIndex;
			}
			//Debug.Log($"{currentCommandText} NOT consumed");
			return false;
		}

		// TODO replace
		private void OutputAnalysis(string fromProcess) {
			source.AddToCommandOutput(fromProcess); // this is where the printing happens.
			print?.Invoke(fromProcess);
		}

		//public void StartRunningCurrentCommand() {
		//	cancelled = false;
		//	RunEachCommandInSequence();
		//}

		public bool IsCancelled() => cancelled || commandExecutingIndex < 0;

		public void InsertNextCommandToExecute(object context, string command) {
			throw new System.NotImplementedException();
		}

		public void AddToCommandOutput(string value) {
			commandOutput += value;
		}

		public void CancelProcess(object context) {
			cancelled = true;
			currentCommand = null;
		}

		public void SetCommands(IList<string> commands) {
			_currentCommands.Clear();
			for(int i = 0; i < commands.Count; ++i) {
				_currentCommands.Add(commands[i]);
			}
		}

		public void StartCommands() {
			commandExecutingIndex = 0;
		}

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
