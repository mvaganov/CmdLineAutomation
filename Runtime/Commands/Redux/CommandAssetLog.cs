using UnityEngine;

namespace RunCmdRedux {

	[CreateAssetMenu(fileName = "log", menuName = "ScriptableObjects/CommandAsset/log")]
	public class CommandAssetLog : ScriptableObject, ICommandAsset {
		public ICommandProcess CreateCommand(object context) {
			Proc proc = new Proc(this);
			CommandManager.Instance.Add(context, this, proc);
			return proc;
		}

		public class Proc : BaseNamedProcess {
			public CommandAssetLog source;
			public string err;
			public Proc(CommandAssetLog source) { this.source = source; }

			public override string name => source.name;

			public override object Error => err;

			public override bool IsExecutionFinished => true;

			public override float GetProgress() => 1;

			public override void StartCooperativeFunction(string command, PrintCallback print) {
				string[] args = command.Split();
				err = null;
				if (args.Length > 0) {
					for (int i = 1; i < args.Length; ++i) {
						print(args[i] + "\n");
					}
				} else {
					err = $"'{name}' missing parameters";
					Debug.LogWarning(err);
				}
			}
		}
	}
}
