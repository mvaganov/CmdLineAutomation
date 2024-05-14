using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "reimportfolder", menuName = "ScriptableObjects/Commands/ReimportFolder")]
	public class CommandReimportFolder : ScriptableObject, INamedCommand {
		public string CommandToken => this.name;
		// TODO like CommandAutomation, with Get
		private Dictionary<object,string> _path = new Dictionary<object, string>();
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			string[] args = Parse.Split(command);
			if (args.Length > 1) {
				_path[context] = args[1];
				CommandAutomation.DelayCall(() => ReimportCurrentPathFolder(context));
			} else {
				_path = null;
				Debug.LogWarning($"missing time parameter");
			}
		}

		private void ReimportCurrentPathFolder(object context) {
#if UNITY_EDITOR
			UnityEditor.AssetDatabase.ImportAsset(_path[context], 
				UnityEditor.ImportAssetOptions.ImportRecursive |
				UnityEditor.ImportAssetOptions.DontDownloadFromCacheServer);
#else
			Debug.LogWarning("Unable to import Asset folder at runtime");
#endif
			_path[context] = null;
		}

		public bool IsExecutionFinished(object context) => _path[context] == null;

		public string FunctionResult(object context) => null;
	}
}
