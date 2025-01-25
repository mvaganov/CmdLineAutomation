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

		public class Proc : BaseNamedProcess {
			public CommandAssetSleep source;
			public int started, finished;
			public string err;
			public Proc(CommandAssetSleep source) { this.source = source; }

			public override string name => source.name;

			public override object Error => err;

			public override bool IsExecutionFinished {
				get {
					if (err != null) {
						return true;
					}
					int now = Environment.TickCount;
					//Debug.Log($"now {now} >= {finished} finish : {now >= finished}");
					return now >= finished;
				}
			}

			public override float GetProgress() {
				int duration = finished - started;
				int waited = Environment.TickCount - started;
				float normalizedProgress = (float)waited / duration;
				//Debug.Log($"~~~ {normalizedProgress}");
				return normalizedProgress;
			}

			public override void StartCooperativeFunction(string command, PrintCallback print) {
				int now = started = finished = Environment.TickCount;
				string[] args = command.Split();
				err = null;
				//Debug.Log("STARTING SLEEP");
				if (args.Length > 1) {
					if (float.TryParse(args[1], out float seconds)) {
						finished = now + (int)(seconds * 1000);
						//print.Invoke($"{name} {seconds}\n~~~waiting {seconds} seconds~~~\n");
						//Debug.LogWarning($"waiting '{args[1]}' seconds!!!!!!!! [{print.Method}]");
					} else {
						err = $"unable to wait '{args[1]}' seconds";
						Debug.LogWarning(err);
					}
				} else {
					err = $"'{name}' missing time parameter";
					Debug.LogWarning(err);
				}
			}
		}
	}
}
