using System;
using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "sleep", menuName = "ScriptableObjects/Commands/sleep")]
	public class CommandSleep : CommandRunner<CommandSleep.Data>, INamedCommand {
		public string CommandToken => this.name;

		public class Data
		{
			public int started, finished;
			public Data(int start, int end) { started = start; finished = end; }
		}

		public override void StartCooperativeFunction(object context, string command, PrintCallback print) {
			int now = Environment.TickCount;
			SetExecutionData(context, new Data(now,now));
			string[] args = command.Split();
			if (args.Length > 1) {
				if (float.TryParse(args[1], out float seconds)) {
					SetExecutionData(context, new Data(now, now + (int)(seconds * 1000)));
					print.Invoke($"{CommandToken} {seconds}\n~~~waiting {seconds} seconds~~~\n");
					//Debug.LogWarning($"waiting '{args[1]}' seconds!!!!!!!! [{print.Method}]");
				} else {
					Debug.LogWarning($"unable to wait '{args[1]}' seconds");
				}
			} else {
				Debug.LogWarning($"'{name}' missing time parameter");
			}
		}

		public override bool IsExecutionFinished(object context) => Environment.TickCount >= GetExecutionData(context).finished;

		protected override Data CreateEmptyContextEntry(object context) => null;

		public override float Progress(object context)
		{
			Data data = GetExecutionData(context);
			int duration = data.finished - data.started;
			int waited = Environment.TickCount - data.started;
			return (float)waited / duration;
		}
	}
}
