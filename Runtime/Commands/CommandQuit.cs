using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "quit", menuName = "ScriptableObjects/Commands/quit")]
	public class CommandQuit : ScriptableObject, INamedCommand {
		public string CommandToken => this.name;
		public void StartCooperativeFunction(object context, string command, PrintCallback print) {
			print(command);
			Quit();
		}

		public bool IsExecutionFinished(object context) => true;
		public float Progress(object context) => 0;

		public static void Quit() {
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
			Application.OpenURL(webplayerQuitURL);
#else
			Application.Quit();
#endif
		}
	}
}
