using RunCmd;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmdRedux {

	[CreateAssetMenu(fileName = "log", menuName = "ScriptableObjects/CommandAsset/log")]
	public class CommandAssetLog : ScriptableObject, ICommandAsset {
		public ICommandProcess CreateCommand(object context) {
			Proc proc = new Proc(this);
			//CommandManager.Instance.Add(context, this, proc);
			return proc;
		}

		public class Proc : BaseNamedProcess {
			public CommandAssetLog source;
			public string err;
			public Proc(CommandAssetLog source) { this.source = source; }

			public override string name => source.name;

			public override object Error => err;

			public override float GetProgress() => 1;

			public override void StartCooperativeFunction(string command, PrintCallback print) {
				_state = ICommandProcess.State.Executing;
				IList<string> args = Parse.ParseArgs(command);
				err = null;
				if (args.Count > 1) {
					for (int i = 1; i < args.Count; ++i) {
						print(args[i] + "\n");
					}
					_state = ICommandProcess.State.Finished;
				} else {
					err = $"'{name}' missing parameters";
					print($"{err}\n");
					Debug.LogWarning(err);
					_state = ICommandProcess.State.Error;
				}
			}
		}
	}
}
