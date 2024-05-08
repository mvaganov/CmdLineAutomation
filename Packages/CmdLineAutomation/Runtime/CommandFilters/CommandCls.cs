using System;
using UnityEngine;

[CreateAssetMenu(fileName = "cls", menuName = "ScriptableObjects/Commands/CommandCls")]
public class CommandCls : ScriptableObject, INamedCommand {
	public string CommandToken => this.name;
	public void StartCooperativeFunction(object context, string command, Action<string> stdOutput) {
		if (context is IReferencesCmdShell cmdShell) {
			cmdShell.Shell.ClearLines();
		}
	}

	public bool IsFunctionFinished() => true;

	public string FunctionResult() => null;
}
