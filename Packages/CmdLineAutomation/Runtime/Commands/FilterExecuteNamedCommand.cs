using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// Command filter used to call named commands
	/// </summary>
	[CreateAssetMenu(fileName = "ExecuteNamedCommand", menuName = "ScriptableObjects/Filters/ExecuteNamedCommand")]
	public class FilterExecuteNamedCommand : ScriptableObject, ICommandFilter {
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
		/// </summary>
		[SerializeField] protected UnityEngine.Object[] _commandListing;
		/// <summary>
		/// Named functions which may or may not consume a command
		/// </summary>
		private Dictionary<string, INamedCommand> _commandDictionary;

		private class CommandExecution {
			private object context;
			private TextResultCallback stdOutput;
			private string currentCommandText;
			private string currentCommandFilterResult;
			private ICommandProcessor currentCommand;
			private FilterExecuteNamedCommand source;

			public CommandExecution(object context, FilterExecuteNamedCommand source) {
				this.context = context;
				this.source = source;
			}

			public bool IsExecutionFinished() => currentCommand == null || currentCommand.IsExecutionFinished(context);

			public string FunctionResult() => currentCommandFilterResult;

			public void StartCooperativeFunction(string command, TextResultCallback stdOutput) {
				this.stdOutput = stdOutput;
				currentCommandText = command;
				currentCommandFilterResult = command;
				//Debug.Log("....... " + command);
				if (currentCommand != null && !currentCommand.IsExecutionFinished(context)) {
					Debug.Log($"still processing {currentCommand}");
					return;
				}
				if (IsExecutionStoppedByNamedFunction(currentCommandText)) {
					return;
				}
				currentCommand = null;
			}

			private bool IsExecutionStoppedByNamedFunction(string command) {
				string token = Parse.GetFirstToken(command);
				//Debug.Log("=============" + command);
				currentCommand = source.GetNamedCommand(token);
				if (currentCommand == null) {
					return false;
				}
				currentCommand.StartCooperativeFunction(context, command, stdOutput);
				if (!currentCommand.IsExecutionFinished(context)) {
					//Debug.Log("~~~~~~~~~~~~~~~~~~~~~"+_currentCommand + " still running");
					return true;
				}
				//_currentCommandFilterResult = _currentCommand.FunctionResult();
				//Debug.Log($"{_currentCommandText} result : [{_currentCommandFilterResult}]");
				currentCommand = null;
				return false;
			}
		}

		private Dictionary<object, CommandExecution> _executions = new Dictionary<object, CommandExecution>();

		private CommandExecution Get(object context) {
			if (!_executions.TryGetValue(context, out CommandExecution commandExecution)) {
				_executions[context] = commandExecution = new CommandExecution(context, this);
			}
			return commandExecution;
		}

		private bool NeedsInitialization() => _commandDictionary == null || _commandDictionary.Count != _commandListing.Length;

		public bool IsExecutionFinished(object context) => Get(context).IsExecutionFinished();//_currentCommand == null || _currentCommand.IsExecutionFinished(context);

		public string FunctionResult(object context) => Get(context).FunctionResult();//_currentCommandFilterResult;

		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			Get(context).StartCooperativeFunction(command, stdOutput);
		}

		public INamedCommand GetNamedCommand(string token) {
			if (NeedsInitialization()) {
				Initialize();
			}
			return _commandDictionary.TryGetValue(token, out INamedCommand found) ? found : null;
		}

		private void Initialize() {
			_commandDictionary = new Dictionary<string, INamedCommand>();
			foreach (UnityEngine.Object obj in _commandListing) {
				switch (obj) {
					case INamedCommand iCmd: Add(iCmd); break;
					default:
						Debug.LogError($"unexpected command type {obj.GetType().Name}, " +
							$"{nameof(FilterExecuteNamedCommand)} expects only {GetType().Name} entries");
						break;
				}
			}
		}

		private void Add(INamedCommand iCmd) {
			string token = iCmd.CommandToken;
			bool alreadyHaveIt = _commandDictionary.TryGetValue(token, out INamedCommand existingCmd);
			if (alreadyHaveIt) {
				Debug.LogError($"Replacing {token} <{existingCmd.GetType()}> with <{iCmd.GetType()}>");
			}
			_commandDictionary[token] = iCmd;
		}
	}
}
