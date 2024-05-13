using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "sleep", menuName = "ScriptableObjects/Commands/CommandSleep")]
	public class CommandSleep : ScriptableObject, INamedCommand {
		public string CommandToken => this.name;
		private Dictionary<object, int> _delays = new Dictionary<object, int>();

		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			_delays[context] = Environment.TickCount;
			string[] args = Parse.Split(command);
			if (args.Length > 1) {
				if (float.TryParse(args[1], out float seconds)) {
					_delays[context] = Environment.TickCount + (int)(seconds * 1000);
				} else {
					Debug.LogWarning($"unable to wait '{args[1]}' seconds");
				}
			} else {
				Debug.LogWarning($"missing time parameter");
			}
		}

		public string FunctionResult() => null;

		public bool IsExecutionFinished(object context) => Environment.TickCount >= _delays[context];
	}
}
