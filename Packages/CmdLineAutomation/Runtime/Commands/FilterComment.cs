using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// Removes a command from the command set if it begins with a given prefix
	/// </summary>
	[CreateAssetMenu(fileName = "Comment", menuName = "ScriptableObjects/Filters/Comment")]
	public class FilterComment : CommandRunner<string>, ICommandFilter {
		[SerializeField] protected bool _enabled = true;
		[SerializeField] protected string _prefix = "#";

		public override void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			if (!_enabled) {
				SetExecutionData(context, command);
				return;
			}
			string result = command;
			if (command.StartsWith(_prefix)) {
				result = null;
			}
			SetExecutionData(context, result);
		}

		public string FunctionResult(object context) => GetExecutionData(context);

		public override bool IsExecutionFinished(object context) => true;

		protected override string CreateEmptyContextEntry(object context) => null;

		public override float Progress(object context) => 0;
	}
}
