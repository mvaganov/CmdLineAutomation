using System;
using UnityEngine;

[CreateAssetMenu(fileName = "sleep", menuName = "ScriptableObjects/Commands/CommandSleep")]
public class CommandSleep : ScriptableObject, INamedCommand {
	public string CommandToken => this.name;
	private int _delayUntil;

	public void StartCooperativeFunction(object context, string command, Action<string> stdOutput) {
		if (context is IReferencesCmdShell shellRef) {
			InteractiveCmdShell cmdShell = shellRef.Shell;
			_delayUntil = Environment.TickCount;
			string[] args = cmdShell.Split(command);
			if (args.Length > 1) {
				if (float.TryParse(args[1], out float seconds)) {
					_delayUntil = Environment.TickCount + (int)(seconds * 1000);
				} else {
					Debug.LogWarning($"unable to wait '{args[1]}' seconds");
				}
			} else {
				Debug.LogWarning($"missing time parameter");
			}
		} else {
			Debug.LogError($"unexpected context: {context}");
		}
	}

	public string FunctionResult() => null;

	public bool IsFunctionFinished() => Environment.TickCount >= _delayUntil;
}
