using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "reimportfolder", menuName = "ScriptableObjects/Commands/reimportfolder")]
	public class CommandReimportFolder : ScriptableObject, CommandRunner<string>, INamedCommand {
		public string CommandToken => this.name;

		private Dictionary<object, string> _executionData = new Dictionary<object, string>();
		public Dictionary<object, string> ExecutionDataAccess { get => _executionData; set => _executionData = value; }
		public IEnumerable<object> GetContexts() => ExecutionDataAccess.Keys;

		public string CreateEmptyContextEntry(object context) => null;

		public void StartCooperativeFunction(object context, string command, PrintCallback print) {
			string[] args = command.Split();
			if (args.Length > 1) {
				this.SetExecutionData(context, args[1]);
				CommandDelay.DelayCall(() => ReimportCurrentPathFolder(context));
			} else {
				this.SetExecutionData(context, null);
				Debug.LogWarning($"missing time parameter");
			}
		}

		private void ReimportCurrentPathFolder(object context) {
#if UNITY_EDITOR
			UnityEditor.AssetDatabase.ImportAsset(this.GetExecutionData(context), 
				UnityEditor.ImportAssetOptions.ImportRecursive |
				UnityEditor.ImportAssetOptions.DontDownloadFromCacheServer);
#else
			Debug.LogWarning("Unable to import Asset folder at runtime");
#endif
			this.SetExecutionData(context, null);
		}

		public bool IsExecutionFinished(object context) => this.GetExecutionData(context) == null;

		public float Progress(object context) => 0;

		public void RemoveExecutionData(object context) => CommandRunnerExtension.RemoveExecutionData(this, context);
	}
}
