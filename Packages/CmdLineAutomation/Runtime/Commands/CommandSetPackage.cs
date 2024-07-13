using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "setpackage", menuName = "ScriptableObjects/Commands/setpackage")]
	public class CommandSetPackage : ScriptableObject, INamedCommand {
		private const string Assets = "Assets";
		private const string Packages = "Packages";
		private const string Manifest_json = "manifest.json";
		private const string Dependencies = "dependencies";
		public string CommandToken => this.name;
		public void StartCooperativeFunction(object context, string command, PrintCallback print) {
#if UNITY_EDITOR
			IList<string> args = Parse.ParseArgs(command);
			print(command + "\n");
			if (args.Count >= 2) {
				SetPackage(args[1], args[2]);
			}
#else
			Debug.LogError("Can only change package in Editor");
#endif
		}

		public bool IsExecutionFinished(object context) => true;
		public float Progress(object context) => 0;

		/// <summary>
		/// Changes (or adds or removes) a package entry in this project's manifest.json
		/// </summary>
		/// <param name="packageName"></param>
		/// <param name="packageValue">if empty, delete the given packageName entry</param>
		/// <returns></returns>
		public static bool SetPackage(string packageName, string packageValue) {
			string dataPath = Application.dataPath;
			if (string.IsNullOrEmpty(dataPath) || !dataPath.EndsWith(Assets)) {
				Debug.LogError($"Expected data path \"{dataPath}\" to end in \"{Assets}\"");
				return false;
			}
			string projectPath = dataPath.Substring(0, dataPath.Length - Assets.Length);
			string manifestFilePath = Path.Combine(projectPath, Packages, Manifest_json);
			if (!File.Exists(manifestFilePath)) {
				Debug.LogError($"Missing file: {manifestFilePath}");
				return false;
			}
			string manifestText = File.ReadAllText(manifestFilePath).Replace("\r", "");
			if (string.IsNullOrEmpty(manifestText)) {
				Debug.LogError($"Missing file data: {manifestFilePath}");
				return false;
			}
			List<string> lines = new List<string>(manifestText.Split("\n"));
			int depLine = lines.FindIndex(line => line.Contains(Dependencies));
			if (depLine < 0) {
				Debug.LogError($"Missing entry {Dependencies} at {manifestFilePath}");
				return false;
			}
			int endDepLine = lines.FindIndex(depLine, line => line.Contains("}"));
			int depCount = endDepLine - depLine;
			int firstActualDependency = depLine + 1;
			int packageEntryLine = lines.FindIndex(firstActualDependency, depCount,
				line => line.Contains($"\"{packageName}\""));
			if (string.IsNullOrEmpty(packageValue)) {
				string newLine = $"    \"{ packageName}\": \"{ packageValue}\"";
				if (packageEntryLine < 0) {
					lines.Insert(depLine + 1, newLine + (depCount > 0 ? "," : ""));
				} else {
					lines[packageEntryLine] = newLine + (depCount > 1 ? "," : "");
				}
			} else {
				if (packageEntryLine >= 0) {
					bool isLastAndTrailingCommanMustBeRemoved = packageEntryLine == depLine + depCount - 1;
					lines.RemoveAt(packageEntryLine);
					if (isLastAndTrailingCommanMustBeRemoved) {
						string lastLine = lines[packageEntryLine - 1];
						lastLine = lastLine.TrimEnd();
						lastLine = lastLine.Substring(0, lastLine.Length - 1);
						lines[packageEntryLine - 1] = lastLine;
					}
				} else {
					Debug.LogWarning($"{packageName} already removed");
				}
			}
			if (lines[lines.Count - 1].Length != 0) {
				lines.Add("");
			}
			string finalFile = string.Join('\n', lines);
			File.WriteAllText(manifestFilePath, finalFile);
			return true;
		}
	}
}
