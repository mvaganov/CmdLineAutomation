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
	[SerializeField] protected TextCommand[] CommandsToDo;
	[SerializeField] protected UnityEngine.Object[] _commandFilterListing;
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
	private ICommandProcessor currentCommand;
	/// <summary>
	/// Result of the last finished cooperative function 
	/// </summary>
	private string currentCommandResult;
	/// <summary>
	/// Which filter is being cooperatively processed right now
	/// </summary>
	private int filterIndex = 0;

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
		_commandDictionary = new Dictionary<string, INamedCommand>();
		_filters = new List<ICommandProcessor>();
		foreach (UnityEngine.Object obj in _commandFilterListing) {
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
		_thread = new System.Threading.Thread(ThreadStart);
		_thread.Start();
	}
	System.Threading.Thread _thread;
	private object _context;
	Action<string> _stdOutput;
	private bool _running;

	private void ThreadStart() {
		_running = true;
		for (int i = 0; _running && i < CommandsToDo.Length; i++) {
			if (CommandsToDo[i].Comment) { continue; }
			filterIndex = 0;
			StartCooperativeFunction(_context, CommandsToDo[i].Text, _stdOutput);
			while (!IsFunctionFinished() && _running) {
				System.Threading.Thread.Sleep(1);
			}
		}
		_running = false;
	}
	public void ThreadStop() {
		_running = false;
		if (_thread != null) {
			_thread.Join(200);
			_thread.Abort();
			_thread = null;
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
		if (currentCommand != null && !currentCommand.IsFunctionFinished()) {
			Debug.Log($"still processing {currentCommand}");
			return;
		}
		if (IsExecutionStoppedByFilterFunction(command)) {
			return;
		}
		if (IsExecutionStoppedByNamedFunction(command)) {
			return;
		}
		if(NativeCmdLineFallback) {
			_shell.StartCooperativeFunction(context, command, stdOutput);
		}
		currentCommand = null;
		filterIndex = 0;
	}

	private bool IsExecutionStoppedByFilterFunction(string command) {
		while (filterIndex < _filters.Count) {
			if (currentCommand == null) {
				Debug.Log(filterIndex);
				currentCommand = _filters[filterIndex];
				currentCommand.StartCooperativeFunction(_context, command, _stdOutput);
			}
			if (!currentCommand.IsFunctionFinished()) {
				return true;
			}
			currentCommand = null;
			++filterIndex;
		}
		return false;
	}

	private bool IsExecutionStoppedByNamedFunction(string command) {
		string token = GetFirstToken(command);
		currentCommand = GetNamedCommand(token);
		if (currentCommand == null) {
			return false;
		}
		currentCommand.StartCooperativeFunction(_context, command, _stdOutput);
		if (!currentCommand.IsFunctionFinished()) {
			return true;
		}
		currentCommandResult = currentCommand.FunctionResult();
		currentCommand = null;
		if (currentCommandResult == null) {
			return true;
		}
		return false;
	}

	public bool IsFunctionFinished() => currentCommand == null || currentCommand.IsFunctionFinished();

	public string FunctionResult() => currentCommand != null ? currentCommand.FunctionResult() : currentCommandResult;
}
