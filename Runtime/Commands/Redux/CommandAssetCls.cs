using UnityEngine;

namespace RunCmdRedux {
	[CreateAssetMenu(fileName = "cls", menuName = "ScriptableObjects/CommandAsset/cls")]
	public class CommandAssetCls : ScriptableObject, ICommandAsset {
		public ICommandProcess CreateCommand(object context) {
			Proc proc = new Proc(this, context);
			Debug.Log("created cls proc " + proc);
			CommandManager.Instance.Add(context, this, proc);
			return proc;
		}
		public class Proc : BaseNamedProcess {
			public CommandAssetCls source;
			public object context;
			public override string name => source.name;
			public Proc(CommandAssetCls source, object context) {
				this.source = source;
				this.context = context;
			}
			public override bool IsExecutionFinished => true;
			public override float GetProgress() => 1;
			public override void StartCooperativeFunction(string command, PrintCallback print) {
				if (context is RunCmd.ICommandAutomation automation) {
					// clear the screen just after this command is processed
					CommandDelay.DelayCall(ClearOnNextUpdate);
					void ClearOnNextUpdate() {
						automation.CommandExecutor.CommandOutput = "";
					}
				} else {
					Debug.LogWarning($"{name} unable to clear {context}");
				}
			}
		}
	}
}
