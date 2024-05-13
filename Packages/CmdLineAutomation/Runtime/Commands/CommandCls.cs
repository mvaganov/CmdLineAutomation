using System;
using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "cls", menuName = "ScriptableObjects/Commands/CommandCls")]
	public class CommandCls : ScriptableObject, INamedCommand {
		public string CommandToken => this.name;
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			if (!OperatingSystemCommandShell.RunningShells.TryGetValue(context, out OperatingSystemCommandShell shell)) {
				Debug.LogError($"no shell for '{context}'");
				return;
			}
			shell.ClearLines();
		}

		public bool IsExecutionFinished() => true;

		public string FunctionResult() => null;
	}
}
