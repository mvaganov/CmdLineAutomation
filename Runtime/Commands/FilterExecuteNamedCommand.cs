using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// Command filter used to call named commands from a list
	/// </summary>
	[CreateAssetMenu(fileName = "ExecuteNamedCommand", menuName = "ScriptableObjects/Filters/ExecuteNamedCommand")]
	public class FilterExecuteNamedCommand : ScriptableObject, CommandRunner<FilterExecuteNamedCommand.CommandExecution>, ICommandFilter {
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
		/// </summary>
		[SerializeField] protected UnityEngine.Object[] _commandListing;
		protected ICommandFilter _nextCommand;
		/// <summary>
		/// Named functions which may or may not consume a command
		/// </summary>
		private Dictionary<string, INamedCommand> _commandDictionary;

		private Dictionary<object, CommandExecution> _executionData = new Dictionary<object, CommandExecution>();
		public Dictionary<object, CommandExecution> ExecutionDataAccess { get => _executionData; set => _executionData = value; }
		public IEnumerable<object> GetContexts() => ExecutionDataAccess.Keys;
		public ICommandProcessor GetReferencedCommand(object context) => this.GetExecutionData(context).currentCommand;

		public class CommandExecution {
			private object context;
			private string currentCommandText;
			private string currentCommandFilterResult;
			internal ICommandProcessor currentCommand;
			private FilterExecuteNamedCommand source;

			public CommandExecution(object context, FilterExecuteNamedCommand source) {
				this.context = context;
				this.source = source;
			}

			public bool IsExecutionFinished() => currentCommand == null || currentCommand.IsExecutionFinished(context);

			// TODO functionResult should pass the correct value from it's current command...
			public string FunctionResult() => currentCommandFilterResult;

			public void RemoveExecutionData() {
				if (currentCommand is CommandRunnerBase runner) {
					runner.RemoveExecutionData(context);
				}
			}

			public void StartCooperativeFunction(string command, PrintCallback print) {
				currentCommandText = command;
				currentCommandFilterResult = command;
				//Debug.Log("....... " + command);
				if (currentCommand != null && !currentCommand.IsExecutionFinished(context)) {
					Debug.Log($"still processing {currentCommand}");
					return;
				}
				if (IsExecutionStoppedByNamedFunction(currentCommandText, print)) {
					return;
				}
				currentCommand = null;
			}

			public static string GetFirstToken(string command) {
				int index = command.IndexOf(' ');
				return index < 0 ? command : command.Substring(0, index);
			}

			private bool IsExecutionStoppedByNamedFunction(string command, PrintCallback print) {
				string token = GetFirstToken(command);
				//Debug.Log("=============" + command);
				currentCommand = source.GetNamedCommand(token);
				if (currentCommand == null) {
					return false;
				}
				currentCommand.StartCooperativeFunction(context, command, print);
				if (!currentCommand.IsExecutionFinished(context)) {
					Debug.Log($"~~~~~~~~~~~~~~~~~~~~~{currentCommand} still running");
					return true;
				}
				//_currentCommandFilterResult = _currentCommand.FunctionResult();
				//Debug.Log($"{_currentCommandText} result : [{_currentCommandFilterResult}]");
				currentCommand = null;
				return false;
			}

			public float Progress => currentCommand != null ? currentCommand.Progress(context) : -1;
		}

#if UNITY_EDITOR
		private void Reset() {
			_commandListing = GetAllScriptableObjectAssets<INamedCommand>();
		}
		public static Object[] GetAllScriptableObjectAssets<TYPE>(string[] searchInFolders = null) {
			List<Object> found = new List<Object>();
			string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(ScriptableObject)}", searchInFolders);
			foreach (string guid in guids) {
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
				ScriptableObject so = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
				if (so is TYPE) {
					found.Add(so);
				}
			}
			return found.ToArray();
		}
#endif

		private bool NeedsInitialization() => _commandDictionary == null || _commandDictionary.Count != _commandListing.Length;

		public bool IsExecutionFinished(object context) => this.GetExecutionData(context).IsExecutionFinished();

		public void RemoveExecutionData(object context) {
			this.GetExecutionData(context).RemoveExecutionData();
			CommandRunnerExtension.RemoveExecutionData(this, context);
		}

		public string FilterResult(object context) => this.GetExecutionData(context).FunctionResult();

		public void StartCooperativeFunction(object context, string command, PrintCallback print) {
			RemoveExecutionData(context);
			this.GetExecutionData(context).StartCooperativeFunction(command, print);
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

		public CommandExecution CreateEmptyContextEntry(object context)
			=> new CommandExecution(context, this);

		public float Progress(object context) => this.GetExecutionData(context).Progress;
	}
}
