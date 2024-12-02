using RunCmdRedux;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "sleep", menuName = "ScriptableObjects/Commands/sleep")]
	public class CommandSleep : ScriptableObject, CommandRunner<CommandSleep.Data>, INamedCommand {
		public string CommandToken => this.name;

		private Dictionary<object, Data> _executionData = new Dictionary<object, Data>();
		public Dictionary<object, Data> ExecutionDataAccess { get => _executionData; set => _executionData = value; }
		//private Dictionary<int, object> _executionDataByThread = new Dictionary<int, object>();
		//public Dictionary<int, object> ExecutionDataByThreadId { get => _executionDataByThread; set => _executionDataByThread = value; }
		public IEnumerable<object> GetContexts() => ExecutionDataAccess.Keys;

		public class Data
		{
			public int started, finished;
			public Data(int start, int end) { started = start; finished = end; }
		}

		public void StartCooperativeFunction(object context, string command, PrintCallback print) {
			int now = Environment.TickCount;
			this.SetExecutionData(context, new Data(now,now));
			string[] args = command.Split();
			Debug.Log("STARTING SLEEP");
			if (args.Length > 1) {
				if (float.TryParse(args[1], out float seconds)) {
					this.SetExecutionData(context, new Data(now, now + (int)(seconds * 1000)));
					print.Invoke($"{CommandToken} {seconds}\n~~~waiting {seconds} seconds~~~\n");
					//Debug.LogWarning($"waiting '{args[1]}' seconds!!!!!!!! [{print.Method}]");
				} else {
					Debug.LogWarning($"unable to wait '{args[1]}' seconds");
				}
			} else {
				Debug.LogWarning($"'{name}' missing time parameter");
			}
		}

		public bool IsExecutionFinished(object context) {
			int finished = this.GetExecutionData(context).finished;
			int now = Environment.TickCount;
			//Debug.Log($"now {now} >= {finished} finish");
			return now >= finished;
		}
		public Data CreateEmptyContextEntry(object context) => null;

		public float Progress(object context)
		{
			Data data = this.GetExecutionData(context);
			int finished = data.finished;
			int now = Environment.TickCount;
			int duration = finished - data.started;
			int waited = now - data.started;
			float normalizedProgress = (float)waited / duration;
			//Debug.Log($"~~~ {normalizedProgress}");
			return normalizedProgress;
		}

		public void RemoveExecutionData(object context) => CommandRunnerExtension.RemoveExecutionData(this, context);
	}
}
