using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "reimportfolder", menuName = "ScriptableObjects/Commands/ReimportFolder")]
	public class CommandReimportFolder : ScriptableObject, INamedCommand {
		public string CommandToken => this.name;
		// TODO like CommandAutomation, with Get
		private string _path;
		private bool _reimported;
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			string[] args = Parse.Split(command);
			if (args.Length > 1) {
				_path = args[1];
				_reimported = false;
				CommandAutomation.DelayCall(ReimportCurrentPathFolder);
			} else {
				_path = null;
				Debug.LogWarning($"missing time parameter");
			}
		}

		private void ReimportCurrentPathFolder() {
#if UNITY_EDITOR
			UnityEditor.AssetDatabase.ImportAsset(_path, 
				UnityEditor.ImportAssetOptions.ImportRecursive |
				UnityEditor.ImportAssetOptions.DontDownloadFromCacheServer);
#else
			Debug.LogWarning("Unable to import Asset folder at runtime");
#endif
			_path = null;
			_reimported = true;
		}

		public bool IsExecutionFinished(object context) => _path == null || _reimported;

		public string FunctionResult(object context) => null;
	}
}
