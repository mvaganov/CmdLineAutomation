using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/CmdLineAutomation", order = 1)]
public class CmdLineAutomationScriptableObject : ScriptableObject, ICmd {
	[Serializable] public class Data {
		[TextArea(1, 1000)] public string Description;
	}

	[Serializable] public class Command {
		public string Text;
		public bool Comment;
	}

	public InteractiveCmdShell Shell => shell;

	private InteractiveCmdShell shell;
	public Data _details;
	public Command[] CommandsToDo;

	[SerializeField]
	protected UnityEngine.Object[] _commandListing;
	public bool UseNativeCommandLine = true;
	private Dictionary<string, ICmd> _commandDictionary;
	private List<ICmd> _unnamedCommands;

	public string Token => null;

	public string GetFirstToken(string command) {
		int index = command.IndexOf(' ');
		return index < 0 ? command : command.Substring(0, index);
	}

	public ICmd GetNamedCommand(string token) {
		if (_commandDictionary == null) {
			InitializeCommandListing();
		}
		return _commandDictionary.TryGetValue(token, out ICmd found) ? found : null;
	}

	private void InitializeCommandListing() {
		_commandDictionary = new Dictionary<string, ICmd>();
		_unnamedCommands = new List<ICmd>();
		foreach (UnityEngine.Object obj in _commandListing) {
			string foundToken;
			if (!(obj is ICmd iCmd)) {
				continue;
			} else if ((foundToken = iCmd.Token) != null) {
				bool alreadyHaveIt = _commandDictionary.TryGetValue(foundToken, out ICmd existingCmd);
				if (alreadyHaveIt) {
					Debug.LogWarning($"Replacing {foundToken} {existingCmd.GetType()} with {iCmd.GetType()}");
				}
				_commandDictionary[foundToken] = iCmd;
			} else {
				_unnamedCommands.Add(iCmd);
			}
		}
	}

	public void RunCommands(object context, Action<string> stdOutput) {
		switch (context) {
			case InteractiveCmdShell shell:
				this.shell = shell;
				break;
			case CmdLineAutomationScriptableObject cmdObj:
				this.shell = cmdObj.Shell;
				break;
		}
		for (int i = 0; i < CommandsToDo.Length; i++) {
			if (CommandsToDo[i].Comment) { continue; }
			CommandFilter(context, CommandsToDo[i].Text, stdOutput);
		}
	}

	public string CommandFilter(object context, string command, Action<string> stdOutput) {
		string token = GetFirstToken(command);
		ICmd iCmd = GetNamedCommand(token);
		if (iCmd != null) {
			command = iCmd.CommandFilter(context, command, stdOutput);
			if (command == null) {
				return null;
			}
		}
		for(int i = 0; i < _unnamedCommands.Count; ++i) {
			command = _unnamedCommands[i].CommandFilter(context, command, stdOutput);
			if (command == null) {
				return null;
			}
		}
		return UseNativeCommandLine ? shell.CommandFilter(context, command, stdOutput) : command;
	}
}
