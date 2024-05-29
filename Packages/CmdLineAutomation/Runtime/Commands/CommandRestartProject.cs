using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "restartproject", menuName = "ScriptableObjects/Commands/RestartProject")]
	public class CommandRestartProject : ScriptableObject, INamedCommand {
		public string CommandToken => this.name;
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			if (!OperatingSystemCommandShell.RunningShells.TryGetValue(context, out OperatingSystemCommandShell shell)) {
				return;
			}
#if UNITY_EDITOR
			bool restartProject = true;
			// TODO if there is an argument, set `blockingDialog` to true and `restartProject` to false
			bool blockingDialog = false;
			if (blockingDialog && UnityEditor.EditorUtility.DisplayDialog("Restart Unity Project?",
						"Restart to be sure binary libraries load correctly",
						"Restart", "Cancel")) {
				restartProject = true;
			}
			if (restartProject) {
				UnityEditor.EditorApplication.OpenProject(System.IO.Directory.GetCurrentDirectory());
			}
#else
			Debug.Log("Cannot restart project");
#endif
			shell.ClearLines();
		}

		public bool IsExecutionFinished(object context) => true;
		public float Progress(object context) => 0;
	}
}
