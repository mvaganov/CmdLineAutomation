using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// Removes a command from the command set if it begins with a given prefix
	/// </summary>
	[CreateAssetMenu(fileName = "Comment", menuName = "ScriptableObjects/Filters/Comment")]
	public class FilterComment : ScriptableObject, CommandRunner<string>, ICommandFilter {
		[SerializeField] protected bool _enabled = true;
		[SerializeField] protected string _prefix = "#";
		[SerializeField] protected string _suffix = "";
		private bool _eatMessages;

		private Dictionary<object, string> _executionData = new Dictionary<object, string>();
		public Dictionary<object, string> ExecutionDataAccess { get => _executionData; set => _executionData = value; }
		public ICommandProcessor GetReferencedCommand(object context) => this;
		public IEnumerable<object> GetContexts() => ExecutionDataAccess.Keys;

		public void StartCooperativeFunction(object context, string command, PrintCallback print) {
			//if (!CouldPossiblyTrigger(command)) {
			//	Debug.Log("NOPE");
			//	return;
			//}
			if (!_enabled) {
				this.SetExecutionData(context, command);
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
			this.SetExecutionData(context, result);
		}

		public bool CouldPossiblyTrigger(string text) {
			if (_eatMessages) { return true; }
			if (text == null || !_enabled) { return false; }
			return text.IndexOf(_prefix) >= 0;
		}

		public string FunctionResult(object context) => this.GetExecutionData(context);

		public bool IsExecutionFinished(object context) => true;

		public string CreateEmptyContextEntry(object context) => null;

		public float Progress(object context) => 0;

		public void RemoveExecutionData(object context) => CommandRunnerExtension.RemoveExecutionData(this, context);
	}
}
