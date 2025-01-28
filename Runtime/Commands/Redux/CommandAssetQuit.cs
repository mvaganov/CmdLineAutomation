using UnityEngine;

namespace RunCmdRedux {
	[CreateAssetMenu(fileName = "quit", menuName = "ScriptableObjects/CommandAsset/quit")]
	public class CommandAssetQuit : ScriptableObject, ICommandAsset {
		public string webplayerQuitURL;
		public static void Quit() {
#if UNITY_EDITOR
			if (!UnityEditor.EditorApplication.isPlaying) {
				Debug.LogWarning("Application not running, cannot quit");
			}
			UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
			Application.OpenURL(webplayerQuitURL);
#else
			Application.Quit();
#endif
		}

		public ICommandProcess CreateCommand(object context) {
			Proc proc = new Proc(this);
			//CommandManager.Instance.Add(context, this, proc);
			return proc;
		}

		public class Proc : BaseNamedProcess {
			public CommandAssetQuit source;
			public Proc(CommandAssetQuit source) { this.source = source; }

			public override string name => source.name;

			public override float GetProgress() => 1;

			public override void StartCooperativeFunction(string command, PrintCallback print) {
				_state = ICommandProcess.State.Executing;
				Quit();
				_state = ICommandProcess.State.Finished;
			}
		}
	}
}
