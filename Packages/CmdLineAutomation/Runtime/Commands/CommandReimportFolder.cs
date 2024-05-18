using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "reimportfolder", menuName = "ScriptableObjects/Commands/ReimportFolder")]
	public class CommandReimportFolder : CommandRunner<string>, INamedCommand {
		public string CommandToken => this.name;
		protected override string CreateEmptyContextEntry(object context) => null;

		public override void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			string[] args = Parse.Split(command);
			if (args.Length > 1) {
				SetExecutionData(context, args[1]);
				CommandAutomation.DelayCall(() => ReimportCurrentPathFolder(context));
			} else {
				SetExecutionData(context, null);
				Debug.LogWarning($"missing time parameter");
			}
		}

		private void ReimportCurrentPathFolder(object context) {
#if UNITY_EDITOR
			UnityEditor.AssetDatabase.ImportAsset(GetExecutionData(context), 
				UnityEditor.ImportAssetOptions.ImportRecursive |
				UnityEditor.ImportAssetOptions.DontDownloadFromCacheServer);
#else
			Debug.LogWarning("Unable to import Asset folder at runtime");
#endif
			SetExecutionData(context, null);
		}

		public override bool IsExecutionFinished(object context) => GetExecutionData(context) == null;

		public override float Progress(object context) => 0;
	}
}
