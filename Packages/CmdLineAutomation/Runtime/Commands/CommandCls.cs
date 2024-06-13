using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "cls", menuName = "ScriptableObjects/Commands/cls")]
	public class CommandCls : ScriptableObject, INamedCommand {
		public string CommandToken => this.name;
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			if (context is CommandAutomation automation) {
				automation.ClearOutput(context);
				OperatingSystemCommandShell shell = automation.GetShell(context);
				shell?.ClearLines();
			} else if (OperatingSystemCommandShell.RunningShells.TryGetValue(context, out OperatingSystemCommandShell shell)) {
				shell.ClearLines();
			} else {
				Debug.LogError($"no shell for '{context}'");
			}
		}

		public bool IsExecutionFinished(object context) => true;
		public float Progress(object context) => 0;
	}
}
