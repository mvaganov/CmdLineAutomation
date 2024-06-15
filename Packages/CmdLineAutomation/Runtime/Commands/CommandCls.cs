using System.Collections;
using UnityEngine;

namespace RunCmd {
	[CreateAssetMenu(fileName = "cls", menuName = "ScriptableObjects/Commands/cls")]
	public class CommandCls : ScriptableObject, INamedCommand {
		public string CommandToken => this.name;
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			if (context is CommandAutomation automation) {
				// clear the screen just after this command is processed
				CommandAutomation.DelayCall(ClearOnNextUpdate);
				void ClearOnNextUpdate() {
					automation.ClearOutput(automation);
				}
			} else {
				Debug.LogWarning($"{name} unable to clear {context}");
			}
		}

		public bool IsExecutionFinished(object context) => true;
		public float Progress(object context) => 0;
	}
}
