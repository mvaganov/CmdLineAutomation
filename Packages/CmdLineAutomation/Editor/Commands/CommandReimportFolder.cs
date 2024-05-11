using UnityEditor;
using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "reimportfolder", menuName = "ScriptableObjects/Commands/ReimportFolder")]
	public class CommandReimportFolder : ScriptableObject, INamedCommand {
		public string CommandToken => this.name;
		private string _path;
		private bool _reimported;
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			string[] args = Parse.Split(command);
			if (args.Length > 1) {
				_path = args[1];
				_reimported = false;
				EditorApplication.delayCall += ReimportCurrentPathFolder;
			} else {
				_path = null;
				Debug.LogWarning($"missing time parameter");
			}
		}

		private void ReimportCurrentPathFolder() {
			AssetDatabase.ImportAsset(_path, ImportAssetOptions.ImportRecursive | ImportAssetOptions.DontDownloadFromCacheServer);
			_path = null;
			_reimported = true;
		}

		public bool IsFunctionFinished() => _path == null || _reimported;

		public string FunctionResult() => null;
	}
}
