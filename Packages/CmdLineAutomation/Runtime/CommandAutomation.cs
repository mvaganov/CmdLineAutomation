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
	public class CommandAutomation : ScriptableObject, ICommandProcessor {
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

		/// <summary>
		/// Information about what these commands are for
		/// </summary>
		[SerializeField] protected MetaData _details;
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
		/// </summary>
		[SerializeField] protected UnityEngine.Object[] _commandFilters;
		/// <summary>
		/// The specific commands to do TODO replace with new-line-delimited text area?
		/// </summary>
		[SerializeField] protected TextCommand[] CommandsToDo;

		/// <summary>
		/// Which command from <see cref="CommandsToDo"/> is being executed right now
		/// </summary>
		private int _commandExecutingIndex;
		/// <summary>
		/// List if filtering functions for input, which may or may not consume a command
		/// </summary>
		private List<ICommandFilter> _filters;
		/// <summary>
		/// Text of the current command
		/// </summary>
		private string _currentCommandText;
		/// <summary>
		/// Which cooperative function is being executed right now
		/// </summary>
		private ICommandFilter _currentCommand;
		/// <summary>
		/// Result of the last finished cooperative function
		/// </summary>
		private string _currentCommandResult;
		/// <summary>
		/// Which <see cref="_commandFilters"/> is being cooperatively processed right now
		/// </summary>
		private int _filterIndex = 0;
		/// <summary>
		/// What object counts as the owner of this command
		/// </summary>
		private object _context;
		/// <summary>
		/// Function to pass all lines from standard output to
		/// </summary>
		private TextResultCallback _stdOutput;

		private bool NeedsInitialization() => _filters == null;

		private bool HaveCommandToDo() => _currentCommand != null;

		public void Initialize() {
			_filters = new List<ICommandFilter>();
			foreach (UnityEngine.Object obj in _commandFilters) {
				switch (obj) {
					case ICommandFilter iFilter: _filters.Add(iFilter); break;
					default:
						Debug.LogError($"unexpected filter type {obj.GetType().Name}, " +
							$"{name} expects only {nameof(ICommandFilter)} entries");
						break;
				}
			}
		}

		public void RunCommands(object context, TextResultCallback stdOutput) {
			_stdOutput = stdOutput;
			_context = context;
			Initialize();
			CooperativeFunctionStart();
		}

		private void CooperativeFunctionStart() {
			_commandExecutingIndex = 0;
			RunCommand();
		}

		private void RunCommand() {
			if (HaveCommandToDo()) {
				if (_currentCommand.IsExecutionFinished()) {
					++_commandExecutingIndex;
					_currentCommand = null;
				} else {
					DelayCall(RunCommand);
					return;
				}
			}
			if (!HaveCommandToDo()) {
				string textToDo = CommandsToDo[_commandExecutingIndex].Text;
				if (!CommandsToDo[_commandExecutingIndex].Comment) {
					_filterIndex = 0;
					//Debug.Log("execute " + _commandExecutingIndex+" "+ textToDo);
					StartCooperativeFunction(_context, textToDo, _stdOutput);
					if (HaveCommandToDo() && !_currentCommand.IsExecutionFinished()) { Debug.Log("       still doing it!"); }
				}
				if (!HaveCommandToDo() || _currentCommand.IsExecutionFinished()) {
					++_commandExecutingIndex;
				}
			} else {
				DoCurrentCommand();
				if (_currentCommand == null) {
					++_commandExecutingIndex;
				}
			}
			if (_commandExecutingIndex < CommandsToDo.Length) {
				DelayCall(RunCommand);
			} else {
				_commandExecutingIndex = 0;
			}
		}

		//private void SetShellContext(object context) {
		//	_context = context;
		//	if (_context is IReferencesOperatingSystemCommandShell shellReference) {
		//		_shell = shellReference.Shell;
		//	}
		//}

		/// <inheritdoc/>
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			_context = context;
			if (_context == null) {
				Debug.LogError("NULL!!!!!");
			}
			_stdOutput = stdOutput;
			_currentCommandText = command;
			_currentCommandResult = command;
			_filterIndex = 0;
			DoCurrentCommand();
		}

		private void DoCurrentCommand() {
			if (_currentCommand != null && !_currentCommand.IsExecutionFinished()) {
				Debug.Log($"still processing {_currentCommand}");
				return;
			}
			//Debug.Log("processing " + _currentCommandText);
			if (IsExecutionStoppedByFilterFunction(_currentCommandText)) {
				return;
			}
			//if (IsExecutionStoppedByNamedFunction(_currentCommandResult)) {
			//	return;
			//}
			//switch (unknownCommands) {
			//	case WhatToDoWithUnknownCommands.OperatingSystemCommandLine:
			//		_shell.Run(_currentCommandResult, _stdOutput);
			//		_currentCommandResult = null; // consumes command
			//		break;
			//	case WhatToDoWithUnknownCommands.Warning:
			//		Debug.LogWarning($"unprocessed \"{_currentCommandText}\"");
			//		break;
			//}
			_currentCommand = null;
			_filterIndex = 0;
		}

		private bool IsExecutionStoppedByFilterFunction(string command) {
			while (_filterIndex < _filters.Count) {
				if (_currentCommand == null) {
					_currentCommand = _filters[_filterIndex];
					if (_context == null) {
						Debug.LogError("context must not be null!");
					}
					//Debug.Log($"~~~~~~~~{name} start {command} co-op f[{_filterIndex}] {_currentCommand}\n\n{_currentCommandText}");
					_currentCommand.StartCooperativeFunction(_context, _currentCommandText, _stdOutput);
				}
				if (!_currentCommand.IsExecutionFinished()) {
					return true;
				}
				command = _currentCommand.FunctionResult();
				_currentCommand = null;
				if (command == null) {
					Debug.Log($"{_currentCommandText} consumed by {_filters[_filterIndex]}");
					return false;
				}
				++_filterIndex;
			}
			Debug.Log($"{_currentCommandText} NOT consumed");
			return false;
		}

		//private bool IsExecutionStoppedByNamedFunction(string command) {
		//	string token = GetFirstToken(command);
		//	_currentCommand = GetNamedCommand(token);
		//	if (_currentCommand == null) {
		//		return false;
		//	}
		//	_currentCommand.StartCooperativeFunction(_context, command, _stdOutput);
		//	if (!_currentCommand.IsFunctionFinished()) {
		//		Debug.Log(_currentCommand + " still running");
		//		return true;
		//	}
		//	_currentCommandResult = _currentCommand.FunctionResult();
		//	_currentCommand = null;
		//	if (_currentCommandResult == null) {
		//		return true;
		//	}
		//	return false;
		//}

		public bool IsExecutionFinished() => _currentCommand == null || _currentCommand.IsExecutionFinished();

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
