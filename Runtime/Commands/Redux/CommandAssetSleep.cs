using System;
using UnityEngine;

namespace RunCmdRedux {

	[CreateAssetMenu(fileName = "sleep", menuName = "ScriptableObjects/CommandAsset/sleep")]
	public class CommandAssetSleep : ScriptableObject, ICommandAsset {
		public ICommandProcess CreateCommand(object context) {
			Proc proc = new Proc(this);
			CommandManager.Instance.Add(context, this, proc);
			return proc;
		}

		public class Proc : INamedProcess {
			public CommandAssetSleep source;
			public int started, finished;
			public Proc(CommandAssetSleep source) { this.source = source; }

			public string name => source.name;

			public bool IsExecutionFinished {
				get {
					int now = Environment.TickCount;
					Debug.Log($"now {now} >= {finished} finish");
					return now >= finished;
				}
			}

			public float GetProgress() {
				int duration = finished - started;
				int waited = Environment.TickCount - started;
				float normalizedProgress = (float)waited / duration;
				//Debug.Log($"~~~ {normalizedProgress}");
				return normalizedProgress;
			}

			public void StartCooperativeFunction(string command, PrintCallback print) {
				int now = started = finished = Environment.TickCount;
				string[] args = command.Split();
				Debug.Log("STARTING SLEEP");
				if (args.Length > 1) {
					if (float.TryParse(args[1], out float seconds)) {
						finished = now + (int)(seconds * 1000);
						print.Invoke($"{name} {seconds}\n~~~waiting {seconds} seconds~~~\n");
						//Debug.LogWarning($"waiting '{args[1]}' seconds!!!!!!!! [{print.Method}]");
					} else {
						Debug.LogWarning($"unable to wait '{args[1]}' seconds");
					}
				} else {
					Debug.LogWarning($"'{name}' missing time parameter");
				}
			}
		}
	}
}
