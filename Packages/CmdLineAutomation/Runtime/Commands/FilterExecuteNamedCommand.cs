using System.Collections;
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

		// TODO make this in a variables class, with a Get method, like in CommandAutomation
		private object _context;
		private TextResultCallback _stdOutput;
		private string _currentCommandText;
		private string _currentCommandFilterResult;
		private ICommandProcessor _currentCommand;

		private bool NeedsInitialization() => _commandDictionary == null || _commandDictionary.Count != _commandListing.Length;

		public bool IsExecutionFinished(object context) => _currentCommand == null || _currentCommand.IsExecutionFinished(context);

		public string FunctionResult(object context) => _currentCommandFilterResult;

		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			_context = context;
			_stdOutput = stdOutput;
			_currentCommandText = command;
			_currentCommandFilterResult = command;
			//Debug.Log("....... " + command);
			if (_currentCommand != null && !_currentCommand.IsExecutionFinished(context)) {
				Debug.Log($"still processing {_currentCommand}");
				return;
			}
			if (IsExecutionStoppedByNamedFunction(_currentCommandText)) {
				return;
			}
			_currentCommand = null;
		}

		private bool IsExecutionStoppedByNamedFunction(string command) {
			string token = Parse.GetFirstToken(command);
			//Debug.Log("=============" + command);
			_currentCommand = GetNamedCommand(token);
			if (_currentCommand == null) {
				return false;
			}
			_currentCommand.StartCooperativeFunction(_context, command, _stdOutput);
			if (!_currentCommand.IsExecutionFinished(_context)) {
				//Debug.Log("~~~~~~~~~~~~~~~~~~~~~"+_currentCommand + " still running");
				return true;
			}
			//_currentCommandFilterResult = _currentCommand.FunctionResult();
			//Debug.Log($"{_currentCommandText} result : [{_currentCommandFilterResult}]");
			_currentCommand = null;
			return false;
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
