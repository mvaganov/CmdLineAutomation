using UnityEngine;

namespace RunCmdRedux {
	[CreateAssetMenu(fileName = "cls", menuName = "ScriptableObjects/CommandAsset/cls")]
	public class CommandAssetCls : ScriptableObject, ICommandAsset {
		public ICommandProcess CreateCommand(object context)  => new Proc(this, context);
		public class Proc : BaseNamedProcess {
			public CommandAssetCls source;
			public object context;
			public override string name => source.name;
			public Proc(CommandAssetCls source, object context) {
				this.source = source;
				this.context = context;
			}
			public override float GetProgress() => 1;
			public override void StartCooperativeFunction(string command, PrintCallback print) {
				_state = ICommandProcess.State.Executing;
				if (context is RunCmd.ICommandAutomation automation) {
					// clear the screen just after this command is processed
					CommandDelay.DelayCall(ClearOnNextUpdate);
					void ClearOnNextUpdate() {
						automation.CommandExecutor.CommandOutput = "";
						_state = ICommandProcess.State.Finished;
					}
				} else if (context is ICommandExecutor executor) {
					// clear the screen just after this command is processed
					CommandDelay.DelayCall(ClearExecutorOnNextUpdate);
					void ClearExecutorOnNextUpdate() {
						executor.CommandOutput = "";
						_state = ICommandProcess.State.Finished;
					}
				} else {
					Debug.LogWarning($"{name} unable to clear {context}");
					_state = ICommandProcess.State.Error;
				}
			}
		}
	}
}
