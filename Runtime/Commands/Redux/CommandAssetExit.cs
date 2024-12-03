using RunCmd;
using UnityEngine;

namespace RunCmdRedux {
	[CreateAssetMenu(fileName = "exit", menuName = "ScriptableObjects/CommandAsset/exit")]
	public class CommandAssetExit : ScriptableObject, ICommandAsset {
		public ICommandProcess CreateCommand(object context) {
			Proc proc = new Proc(context, this);
			CommandManager.Instance.Add(context, this, proc);
			return proc;
		}

		public class Proc : BaseNamedProcess {
			public CommandAssetExit source;
			public object context;
			public Proc(object context, CommandAssetExit source) { this.source = source; this.context = context; }

			public override string name => source.name;

			public override bool IsExecutionFinished => true;

			public override float GetProgress() => 1;

			public override void StartCooperativeFunction(string command, PrintCallback print) {
				// TODO replace with redux version of ICommandExecutor
				if (context is ICommandExecutor automation) {
					automation.CancelProcess(context);
				}
				if (!OperatingSystemCommandShell.RunningShells.TryGetValue(context, out OperatingSystemCommandShell shell)) {
					Debug.LogError($"no shell for '{context}'");
					return;
				}
				shell.Exit();
			}
		}
	}
}
