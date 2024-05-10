using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCmdLineAutomation", menuName = "ScriptableObjects/CmdLineAutomation", order = 1)]
public class CmdLineAutomationScriptableObject : ScriptableObject, ICommandProcessor, IReferencesCmdShell {
	[Serializable] public class MetaData {
		[TextArea(1, 1000)] public string Description;
	}

	[Serializable] public class TextCommand {
		public string Text;
		public bool Comment;
	}

	private InteractiveCmdShell _shell;
	[SerializeField] protected MetaData _details;
	[SerializeField] protected UnityEngine.Object[] _commandListing;
	[SerializeField] protected TextCommand[] CommandsToDo;
	[SerializeField] protected bool NativeCmdLineFallback = true;

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
	private Action<string> _stdOutput;
	/// <summary>
	/// Which command from <see cref="CommandsToDo"/> is being executed right now
	/// </summary>
	private int _commandExecuting;

	public InteractiveCmdShell Shell {
		get => _shell;
		set => _shell = value;
	}

	public static string GetFirstToken(string command) {
		int index = command.IndexOf(' ');
		return index < 0 ? command : command.Substring(0, index);
	}

	public INamedCommand GetNamedCommand(string token) {
		if (_commandDictionary == null) {
			InitializeCommandListing();
		}
		return _commandDictionary.TryGetValue(token, out INamedCommand found) ? found : null;
	}

	private void InitializeCommandListing() {
		Debug.Log("INITIALIZING");
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

	public void RunCommands(object context, Action<string> stdOutput) {
		SetShellContext(context);
		InitializeCommandListing();
		_context = context;
		_stdOutput = stdOutput;
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
				// TODO only do this in the editor. if in a Unity game context, create an object that will run this in a coroutine
				UnityEditor.EditorApplication.delayCall += RunCommand;
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
			UnityEditor.EditorApplication.delayCall += RunCommand;
		} else {
			_commandExecuting = 0;
		}
	}

	private void SetShellContext(object context) {
		if (context is IReferencesCmdShell shellReference) {
			_shell = shellReference.Shell;
		}
	}

	/// <inheritdoc/>
	public void StartCooperativeFunction(object context, string command, Action<string> stdOutput) {
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
		if (NativeCmdLineFallback) {
			_shell.StartCooperativeFunction(_context, _currentCommandResult, _stdOutput);
			_currentCommandResult = null;
		}
		_currentCommand = null;
		filterIndex = 0;
	}

	private bool IsExecutionStoppedByFilterFunction(string command) {
		while (filterIndex < _filters.Count) {
			if (_currentCommand == null) {
				//Debug.Log(filterIndex);
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
}
