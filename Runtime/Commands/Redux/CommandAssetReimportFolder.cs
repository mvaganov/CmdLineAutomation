using UnityEngine;

namespace RunCmdRedux {
	[CreateAssetMenu(fileName = "reimportfolder", menuName = "ScriptableObjects/CommandAsset/reimportfolder")]
	public class CommandAssetReimportFolder : ScriptableObject, ICommandAsset {
		public ICommandProcess CreateCommand(object context) {
			Proc proc = new Proc(this);
			return proc;
		}

		public class Proc : BaseNamedProcess {
			public CommandAssetReimportFolder source;
			public string folder;
			public Proc(CommandAssetReimportFolder source) { this.source = source; }

			public override string name => source.name;

			public override float GetProgress() => _state == ICommandProcess.State.Finished ? 1 : 0;

			public override void StartCooperativeFunction(string command, PrintCallback print) {
				_state = ICommandProcess.State.Executing;
				string[] args = command.Split();
				if (args.Length > 1) {
					folder = args[1];
					CommandDelay.DelayCall(() => ReimportCurrentPathFolder());
				} else {
					Debug.LogWarning($"missing folder parameter");
					_state = ICommandProcess.State.Error;
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
				_state = ICommandProcess.State.Finished;
			}
		}
	}
}
