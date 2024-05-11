using System;
using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "sleep", menuName = "ScriptableObjects/Commands/CommandSleep")]
	public class CommandSleep : ScriptableObject, INamedCommand {
		public string CommandToken => this.name;
		private int _delayUntil;

		public void StartCooperativeFunction(object context, string command, Action<string> stdOutput) {
			_delayUntil = Environment.TickCount;
			string[] args = Parse.Split(command);
			if (args.Length > 1) {
				if (float.TryParse(args[1], out float seconds)) {
					_delayUntil = Environment.TickCount + (int)(seconds * 1000);
				} else {
					Debug.LogWarning($"unable to wait '{args[1]}' seconds");
				}
			} else {
				Debug.LogWarning($"missing time parameter");
			}
		}

		public string FunctionResult() => null;

		public bool IsFunctionFinished() => Environment.TickCount >= _delayUntil;
	}
}
