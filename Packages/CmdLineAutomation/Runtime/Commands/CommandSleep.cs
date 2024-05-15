using System;
using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "sleep", menuName = "ScriptableObjects/Commands/CommandSleep")]
	public class CommandSleep : CommandRunner<int>, INamedCommand {
		public string CommandToken => this.name;

		public override void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			SetExecutionData(context, Environment.TickCount);
			string[] args = Parse.Split(command);
			if (args.Length > 1) {
				if (float.TryParse(args[1], out float seconds)) {
					SetExecutionData(context, Environment.TickCount + (int)(seconds * 1000));
				} else {
					Debug.LogWarning($"unable to wait '{args[1]}' seconds");
				}
			} else {
				Debug.LogWarning($"missing time parameter");
			}
		}

		public string FunctionResult() => null;

		public override bool IsExecutionFinished(object context) => Environment.TickCount >= GetExecutionData(context);

		protected override int CreateEmptyContextEntry(object context) => 0;
	}
}
