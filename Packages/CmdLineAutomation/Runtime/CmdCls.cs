using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Commands/CmdLineAutomation")]
public class CmdCls : ScriptableObject, ICmd {
	public string Token => this.name;

	public string CommandFilter(object context, string command, Action<string> stdOutput) {
		if (context is InteractiveCmdShell cmdShell) {
			ClearLines(cmdShell);
			return null;
		}
		return command;
	}

	public static void ClearLines(InteractiveCmdShell cmdShell) {
		cmdShell.ClearLines();
	}
}
