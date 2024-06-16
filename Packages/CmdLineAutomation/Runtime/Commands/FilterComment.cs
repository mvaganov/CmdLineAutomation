using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// Removes a command from the command set if it begins with a given prefix
	/// </summary>
	[CreateAssetMenu(fileName = "Comment", menuName = "ScriptableObjects/Filters/Comment")]
	public class FilterComment : CommandRunner<string>, ICommandFilter {
		[SerializeField] protected bool _enabled = true;
		[SerializeField] protected string _prefix = "#";
		[SerializeField] protected string _suffix = "";
		private bool _eatMessages;

		public override void StartCooperativeFunction(object context, string command, PrintCallback print) {
			if (!_enabled) {
				SetExecutionData(context, command);
				return;
			}
			string result = command;
			if (!_eatMessages && string.IsNullOrEmpty(_suffix)) {
				if (command.StartsWith(_prefix)) {
					result = null;
				}
			} else {
				if (!_eatMessages) {
					int blockCommentStart = command.IndexOf(_prefix);
					if (blockCommentStart >= 0) {
						int blockCommentEnd = command.IndexOf(_suffix, blockCommentStart + _prefix.Length);
						if (blockCommentEnd >= 0) {
							string firstPart = command.Substring(0, blockCommentStart);
							string secondPart = command.Substring(blockCommentEnd + _suffix.Length);
							result = firstPart + secondPart;
							_eatMessages = false;
						} else {
							result = command.Substring(0, blockCommentStart);
							_eatMessages = true;
						}
					}
				} else {
					int blockCommentEnd = command.IndexOf(_suffix, 0);
					if (blockCommentEnd >= 0) {
						result = command.Substring(blockCommentEnd + _suffix.Length);
						_eatMessages = false;
					} else {
						result = null;
					}
				}
			}
			SetExecutionData(context, result);
		}

		public string FunctionResult(object context) => GetExecutionData(context);

		public override bool IsExecutionFinished(object context) => true;

		protected override string CreateEmptyContextEntry(object context) => null;

		public override float Progress(object context) => 0;
	}
}
