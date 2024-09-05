using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "quit", menuName = "ScriptableObjects/CommandAsset/quit")]
	public class CommandAssetQuit : ScriptableObject, ICommandAsset {
		public string webplayerQuitURL;
		public static void Quit() {
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
			Application.OpenURL(webplayerQuitURL);
#else
			Application.Quit();
#endif
		}

		public ICommandProcess CreateCommand(object context) {
			Proc proc = new Proc(this);
			CommandManager.Instance.Add(context, this, proc);
			return proc;
		}

		public class Proc : INamedProcess {
			public CommandAssetQuit source;
			public Proc(CommandAssetQuit source) { this.source = source; }

			public string name => source.name;

			public bool IsExecutionFinished => true;

			public float GetProgress() => 1;

			public void StartCooperativeFunction(string command, PrintCallback print) {
				print(command);
				Quit();
			}
		}
	}
}
