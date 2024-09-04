using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "cls", menuName = "ScriptableObjects/CommandAsset/cls")]
	public class CommandAssetCls : ScriptableObject, ICommandAsset {
		public ICommandProcess CreateCommand(object context) {
			Proc proc = new Proc(this, context);
			CommandManager.Instance.Add(context, this, proc);
			return proc;
		}
		public class Proc : INamedProcess {
			public CommandAssetCls source;
			public object context;
			public string name => source.name;
			public Proc(CommandAssetCls source, object context) {
				this.source = source;
				this.context = context;
			}
			public bool IsExecutionFinished => true;
			public float GetProgress() => 1;
			public void StartCooperativeFunction(string command, PrintCallback print) {
				if (context is ICommandAutomation automation) {
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
