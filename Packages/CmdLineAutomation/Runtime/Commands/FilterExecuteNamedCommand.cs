using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// Command filter used to call named commands from a list
	/// </summary>
	[CreateAssetMenu(fileName = "ExecuteNamedCommand", menuName = "ScriptableObjects/Filters/ExecuteNamedCommand")]
	public class FilterExecuteNamedCommand : CommandRunner<FilterExecuteNamedCommand.CommandExecution>, ICommandFilter {
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
		/// </summary>
		[SerializeField] protected UnityEngine.Object[] _commandListing;
		/// <summary>
		/// Named functions which may or may not consume a command
		/// </summary>
		private Dictionary<string, INamedCommand> _commandDictionary;

		public class CommandExecution {
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

			public void RemoveExecutionData() {
				if (currentCommand is CommandRunnerBase runner) {
					runner.RemoveExecutionData(context);
				}
			}

			public void StartCooperativeFunction(string command, TextResultCallback stdOutput) {
				this.stdOutput = stdOutput;
				currentCommandText = command;
				currentCommandFilterResult = command;
				//Debug.Log("....... " + command);
				if (currentCommand != null && !currentCommand.IsExecutionFinished(context)) {
					Debug.Log($"still processing {currentCommand}");
					return;
				}
				if (IsExecutionStoppedByNamedFunction(currentCommandText, stdOutput)) {
					return;
				}
				currentCommand = null;
			}

			public static string GetFirstToken(string command) {
				int index = command.IndexOf(' ');
				return index < 0 ? command : command.Substring(0, index);
			}

			private bool IsExecutionStoppedByNamedFunction(string command, TextResultCallback stdOutput) {
				string token = GetFirstToken(command);
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

			public float Progress => currentCommand.Progress(context);
		}

#if UNITY_EDITOR
		private void Reset() {
			_commandListing = GetAllScriptableObjectAssets<INamedCommand>();
		}
		private static Object[] GetAllScriptableObjectAssets<TYPE>(string[] searchInFolders = null) {
			List<Object> found = new List<Object>();
			string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(ScriptableObject)}", searchInFolders);
			foreach (string guid in guids) {
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
				ScriptableObject so = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
				if (so is INamedCommand) {
					found.Add(so);
				}
			}
			return found.ToArray();
		}
#endif

		private bool NeedsInitialization() => _commandDictionary == null || _commandDictionary.Count != _commandListing.Length;

		public override bool IsExecutionFinished(object context) => GetExecutionData(context).IsExecutionFinished();

		public override void RemoveExecutionData(object context) {
			GetExecutionData(context).RemoveExecutionData();
			base.RemoveExecutionData(context);
		}

		public string FunctionResult(object context) => GetExecutionData(context).FunctionResult();

		public override void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			GetExecutionData(context).StartCooperativeFunction(command, stdOutput);
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

		protected override CommandExecution CreateEmptyContextEntry(object context)
			=> new CommandExecution(context, this);

		public override float Progress(object context) => GetExecutionData(context).Progress;
	}
}
