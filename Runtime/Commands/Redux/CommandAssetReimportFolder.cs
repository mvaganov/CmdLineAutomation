using UnityEngine;

namespace RunCmdRedux {
	[CreateAssetMenu(fileName = "reimportfolder", menuName = "ScriptableObjects/CommandAsset/reimportfolder")]
	public class CommandAssetReimportFolder : ScriptableObject, ICommandAsset {
		public ICommandProcess CreateCommand(object context) {
			Proc proc = new Proc(this);
			CommandManager.Instance.Add(context, this, proc);
			return proc;
		}

		public class Proc : INamedProcess {
			public CommandAssetReimportFolder source;
			public string folder;
			public bool finished;
			public Proc(CommandAssetReimportFolder source) { this.source = source; }

			public string name => source.name;

			public bool IsExecutionFinished => finished;

			public float GetProgress() => finished ? 1 : 0;

			public void StartCooperativeFunction(string command, PrintCallback print) {
				string[] args = command.Split();
				if (args.Length > 1) {
					folder = args[1];
					CommandDelay.DelayCall(() => ReimportCurrentPathFolder());
				} else {
					Debug.LogWarning($"missing folder parameter");
					finished = true;
				}
			}

			private void ReimportCurrentPathFolder() {
#if UNITY_EDITOR
				UnityEditor.AssetDatabase.ImportAsset(folder,
					UnityEditor.ImportAssetOptions.ImportRecursive |
					UnityEditor.ImportAssetOptions.DontDownloadFromCacheServer);
#else
				Debug.LogWarning($"Unable to import Asset folder {folder} at runtime");
#endif
				finished = true;
			}
		}
	}
}
