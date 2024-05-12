using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// * Metadata about why this object exists
	/// * A list of possible commands
	/// * A list of commands to execute (TODO derived from a single string)
	/// * Logic to process commands as a cooperative process (track state of which command is executing now, TODO progress bar)
	/// * TODO an object to manage output from commands
	/// * TODO create variable listing? auto-populate variables based on input?
	/// </summary>
	[CreateAssetMenu(fileName = "NewCmdLineAutomation", menuName = "ScriptableObjects/CmdLineAutomation", order = 1)]
	public class CommandAutomation : ScriptableObject, ICommandProcessor, IReferencesOperatingSystemCommandShell {
		[Serializable]
		public class MetaData {
			[TextArea(1, 1000)] public string Description;
		}

		/// <summary>
		/// TODO remove the need for making this public by using a parsed TextArea, with one command per line
		/// </summary>
		[Serializable]
		public class TextCommand {
			public string Text;
			public bool Comment;
		}

		public enum WhatToDoWithUnknownCommands {
			Nothing,
			Warning,
			CommandLine
		}

		/// <summary>
		/// The command line shell
		/// </summary>
		private OperatingSystemCommandShell _shell;
		/// <summary>
		/// Information about what these commands are for
		/// </summary>
		[SerializeField] protected MetaData _details;
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
		/// </summary>
		[SerializeField] protected UnityEngine.Object[] _commandListing;
		/// <summary>
		/// The specific commands to do TODO replace with new-line-delimited text area?
		/// </summary>
		[SerializeField] protected TextCommand[] CommandsToDo;
		/// <summary>
		/// What to do with commands that are not listed in <see cref="_commandListing"/>
		/// </summary>
		[SerializeField] protected WhatToDoWithUnknownCommands whatToDoWithUnknownCommands
			= WhatToDoWithUnknownCommands.CommandLine;

		/// <summary>
		/// List if filtering functions for input, which may or may not consume a command
		/// </summary>
		private List<ICommandProcessor> _filters;
		/// <summary>
		/// Named functions which may or may not consume a command
		/// </summary>
		private Dictionary<string, INamedCommand> _commandDictionary;
		/// <summary>
		/// Which cooperative function is being executed right now
		/// </summary>
		private ICommandProcessor _currentCommand;
		/// <summary>
		/// Result of the last finished cooperative function
		/// </summary>
		private string _currentCommandResult;
		/// <summary>
		/// Which filter is being cooperatively processed right now
		/// </summary>
		private int filterIndex = 0;
		/// <summary>
		/// What object counts as the owner of this command line terminal
		/// </summary>
		private object _context;
		/// <summary>
		/// Function to pass all lines from standard input to
		/// </summary>
		private TextResultCallback _stdOutput;
		/// <summary>
		/// Which command from <see cref="CommandsToDo"/> is being executed right now
		/// </summary>
		private int _commandExecuting;

		public TextResultCallback StdOutput {
			get => _stdOutput;
			set {
				_stdOutput = value;
				if (Shell != null) {
					Shell.LineOutput = value;
				}
			}
		}

		public OperatingSystemCommandShell Shell {
			get => _shell;
			set {
				_shell = value;
				if (_shell != null) {
					Shell.LineOutput = _stdOutput;
				}
			}
		}

		public static string GetFirstToken(string command) {
			int index = command.IndexOf(' ');
			return index < 0 ? command : command.Substring(0, index);
		}

		public INamedCommand GetNamedCommand(string token) {
			if (NeedsInitialization()) {
				Initialize();
			}
			return _commandDictionary.TryGetValue(token, out INamedCommand found) ? found : null;
		}

		private bool NeedsInitialization() => _commandDictionary == null ||
			(whatToDoWithUnknownCommands == WhatToDoWithUnknownCommands.CommandLine && Shell == null);

		public void Initialize() {
			InitializeShell();
			InitializeCommands();
		}

		private void InitializeShell() {
			if (whatToDoWithUnknownCommands != WhatToDoWithUnknownCommands.CommandLine || Shell != null) {
				return;
			}
			Shell = CreateShell(name);
		}

		private OperatingSystemCommandShell CreateShell(string name) {
			OperatingSystemCommandShell thisShell = OperatingSystemCommandShell.CreateUnityEditorShell();
			thisShell.Name = $"{name} {Environment.TickCount}";
			CommandAutomation cmdLine = this;
			thisShell.KeepAlive = () => {
				bool lostScriptableObject = cmdLine == null;
				if (lostScriptableObject) {
					Debug.LogWarning($"lost {nameof(CommandAutomation)}");
					return false;
				}
				if (Shell != thisShell) {
					Debug.LogWarning($"lost {nameof(OperatingSystemCommandShell)}");
					thisShell.Stop();
					return false;
				}
				return true;
			};
			return thisShell;
		}

		private void InitializeCommands() {
			_commandDictionary = new Dictionary<string, INamedCommand>();
			_filters = new List<ICommandProcessor>();
			foreach (UnityEngine.Object obj in _commandListing) {
				switch (obj) {
					case INamedCommand iCmd: Add(iCmd); break;
					case ICommandProcessor iFilter: _filters.Add(iFilter); break;
				}
			}
		}

		private void Add(INamedCommand iCmd) {
			string token = iCmd.CommandToken;
			bool alreadyHaveIt = _commandDictionary.TryGetValue(token, out INamedCommand existingCmd);
			if (alreadyHaveIt) {
				Debug.LogWarning($"Replacing {token} {existingCmd.GetType()} with {iCmd.GetType()}");
			}
			if ((token = iCmd.CommandToken) != null) {
			}
			_commandDictionary[token] = iCmd;
		}

		public void RunCommands(object context, TextResultCallback stdOutput) {
			_stdOutput = stdOutput;
			SetShellContext(context);
			Initialize();
			CooperativeFunctionStart();
		}

		private void CooperativeFunctionStart() {
			_commandExecuting = 0;
			RunCommand();
		}

		private void RunCommand() {
			if (_currentCommand != null) {
				if (_currentCommand.IsFunctionFinished()) {
					++_commandExecuting;
					_currentCommand = null;
				} else {
					DelayCall(RunCommand);
					return;
				}
			}
			if (_currentCommand == null) {
				string textToDo = CommandsToDo[_commandExecuting].Text;
				if (!CommandsToDo[_commandExecuting].Comment) {
					filterIndex = 0;
					StartCooperativeFunction(_context, textToDo, _stdOutput);
				}
				if (_currentCommand == null) {
					++_commandExecuting;
				}
			} else {
				ServiceFunctions();
				if (_currentCommand == null) {
					++_commandExecuting;
				}
			}
			if (_commandExecuting < CommandsToDo.Length) {
				DelayCall(RunCommand);
			} else {
				_commandExecuting = 0;
			}
		}

		private void SetShellContext(object context) {
			_context = context;
			if (_context is IReferencesOperatingSystemCommandShell shellReference) {
				_shell = shellReference.Shell;
			}
		}

		/// <inheritdoc/>
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			_context = context;
			_stdOutput = stdOutput;
			_currentCommandResult = command;
			filterIndex = 0;
			ServiceFunctions();
		}

		private void ServiceFunctions() {
			if (_currentCommand != null && !_currentCommand.IsFunctionFinished()) {
				Debug.Log($"still processing {_currentCommand}");
				return;
			}
			if (IsExecutionStoppedByFilterFunction(_currentCommandResult)) {
				return;
			}
			if (IsExecutionStoppedByNamedFunction(_currentCommandResult)) {
				return;
			}
			switch (whatToDoWithUnknownCommands) {
				case WhatToDoWithUnknownCommands.CommandLine:
					_shell.Run(_currentCommandResult, _stdOutput);
					_currentCommandResult = null; // consumes command
					break;
				case WhatToDoWithUnknownCommands.Warning:
					Debug.LogWarning($"unprocessed {_currentCommand}");
					break;
			}
			_currentCommand = null;
			filterIndex = 0;
		}

		private bool IsExecutionStoppedByFilterFunction(string command) {
			while (filterIndex < _filters.Count) {
				if (_currentCommand == null) {
					_currentCommand = _filters[filterIndex];
					_currentCommand.StartCooperativeFunction(_context, command, _stdOutput);
				}
				if (!_currentCommand.IsFunctionFinished()) {
					return true;
				}
				_currentCommand = null;
				++filterIndex;
			}
			return false;
		}

		private bool IsExecutionStoppedByNamedFunction(string command) {
			string token = GetFirstToken(command);
			_currentCommand = GetNamedCommand(token);
			if (_currentCommand == null) {
				return false;
			}
			_currentCommand.StartCooperativeFunction(_context, command, _stdOutput);
			if (!_currentCommand.IsFunctionFinished()) {
				Debug.Log(_currentCommand + " still running");
				return true;
			}
			_currentCommandResult = _currentCommand.FunctionResult();
			_currentCommand = null;
			if (_currentCommandResult == null) {
				return true;
			}
			return false;
		}

		public bool IsFunctionFinished() => _currentCommand == null || _currentCommand.IsFunctionFinished();

		public string FunctionResult() => _currentCommand != null ? _currentCommand.FunctionResult() : _currentCommandResult;

#if UNITY_EDITOR
		public static void DelayCall(UnityEditor.EditorApplication.CallbackFunction call) {
			UnityEditor.EditorApplication.delayCall += call;
		}
#else
		public static void DelayCall(Action call) {
			CoroutineRunner.Instance.StartCoroutine(DelayCall());
			System.Collections.IEnumerator DelayCall() {
				yield return null;
				call.Invoke();
			}
		}
		private class CoroutineRunner : MonoBehaviour {
			private static CoroutineRunner _instance;
			public static CoroutineRunner Instance {
				get {
					if (_instance != null) { return _instance; }
					GameObject go = new GameObject("<CoroutineRunner>");
					DontDestroyOnLoad(go);
					return _instance = go.AddComponent<CoroutineRunner>();
				}
			}
		}
#endif
	}
}
