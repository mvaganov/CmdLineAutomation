using RunCmdRedux;
using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "exit", menuName = "ScriptableObjects/Commands/exit")]
	public class CommandExit : ScriptableObject, INamedCommand {
		public string CommandToken => this.name;
		public void StartCooperativeFunction(object context, string command, PrintCallback print) {
			if (context is ICommandExecutor automation) {
				automation.CancelProcess(context);
			}
			if (!OperatingSystemCommandShell.RunningShells.TryGetValue(context, out OperatingSystemCommandShell shell)) {
				Debug.LogError($"no shell for '{context}'");
				return;
			}
			shell.Exit();
		}

		public bool IsExecutionFinished(object context) => true;
		public float Progress(object context) => 0;
	}
}
