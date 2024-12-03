using RunCmdRedux;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	// TODO FINISH ME
	/// <summary>
	/// Removes a command from the command set if it begins with a given prefix
	/// </summary>
	[CreateAssetMenu(fileName = "Comment", menuName = "ScriptableObjects/Filters/CommentAsset")]
	public class FilterAssetComment : ScriptableObject {
		[SerializeField] protected bool _enabled = true;
		[SerializeField] protected string _prefix = "#";
		[SerializeField] protected string _suffix = "";
		//private bool _eatMessages;

		//private Dictionary<object, string> _executionData = new Dictionary<object, string>();
		//public Dictionary<object, string> ExecutionDataAccess { get => _executionData; set => _executionData = value; }
		//public ICommandProcessor GetReferencedCommand(object context) => this;
		//public IEnumerable<object> GetContexts() => ExecutionDataAccess.Keys;

		//public void StartCooperativeFunction(object context, string command, PrintCallback print) {
		//	//if (!CouldPossiblyTrigger(command)) {
		//	//	Debug.Log("NOPE");
		//	//	return;
		//	//}
		//	this.SetExecutionData(context, command);
		//	if (!_enabled) {
		//		return;
		//	}
		//	string result = command;
		//	if (!_eatMessages && string.IsNullOrEmpty(_suffix)) {
		//		if (command.StartsWith(_prefix)) {
		//			result = null;
		//		}
		//	} else {
		//		if (!_eatMessages) {
		//			int blockCommentStart = command.IndexOf(_prefix);
		//			if (blockCommentStart >= 0) {
		//				int blockCommentEnd = command.IndexOf(_suffix, blockCommentStart + _prefix.Length);
		//				if (blockCommentEnd >= 0) {
		//					string firstPart = command.Substring(0, blockCommentStart);
		//					string secondPart = command.Substring(blockCommentEnd + _suffix.Length);
		//					result = firstPart + secondPart;
		//					_eatMessages = false;
		//				} else {
		//					result = command.Substring(0, blockCommentStart);
		//					_eatMessages = true;
		//				}
		//			}
		//		} else {
		//			int blockCommentEnd = command.IndexOf(_suffix, 0);
		//			if (blockCommentEnd >= 0) {
		//				result = command.Substring(blockCommentEnd + _suffix.Length);
		//				_eatMessages = false;
		//			} else {
		//				result = null;
		//			}
		//		}
		//	}
		//	this.SetExecutionData(context, result);
		//}

		//public bool CouldPossiblyTrigger(string text) {
		//	if (_eatMessages) { return true; }
		//	if (text == null || !_enabled) { return false; }
		//	return text.IndexOf(_prefix) >= 0;
		//}

		//public string FilterResult(object context) => this.GetExecutionData(context);

		//public bool IsExecutionFinished(object context) => true;

		//public string CreateEmptyContextEntry(object context) => null;

		//public float Progress(object context) => 0;

		//public void RemoveExecutionData(object context) => CommandRunnerExtension.RemoveExecutionData(this, context);

		public class Proc : BaseNamedProcess {
			public FilterAssetComment _source;
			private bool _eatMessages;

			public Proc(FilterAssetComment source) {
				_source = source;
				_result = "";
			}

			protected string _result;
			public override bool IsExecutionFinished => true;
			public override object Result => _result;

			public override string name => _source.name;

			public override float GetProgress() => 0;
			public bool CouldPossiblyTrigger(string text) {
				if (_eatMessages) { return true; }
				if (text == null || !_source._enabled) { return false; }
				return text.IndexOf(_source._prefix) >= 0;
			}
			public override void StartCooperativeFunction(string command, PrintCallback print) {
				if (!_source._enabled) {
					return;
				}
				string result = command;
				if (!_eatMessages && string.IsNullOrEmpty(_source._suffix)) {
					if (command.StartsWith(_source._prefix)) {
						result = null;
					}
				} else {
					if (!_eatMessages) {
						int blockCommentStart = command.IndexOf(_source._prefix);
						if (blockCommentStart >= 0) {
							int blockCommentEnd = command.IndexOf(_source._suffix, blockCommentStart + _source._prefix.Length);
							if (blockCommentEnd >= 0) {
								string firstPart = command.Substring(0, blockCommentStart);
								string secondPart = command.Substring(blockCommentEnd + _source._suffix.Length);
								result = firstPart + secondPart;
								_eatMessages = false;
							} else {
								result = command.Substring(0, blockCommentStart);
								_eatMessages = true;
							}
						}
					} else {
						int blockCommentEnd = command.IndexOf(_source._suffix, 0);
						if (blockCommentEnd >= 0) {
							result = command.Substring(blockCommentEnd + _source._suffix.Length);
							_eatMessages = false;
						} else {
							result = null;
						}
					}
				}
			}
		}

		public ICommandProcess CreateCommand(object context) {
			return new Proc(this);
		}

		public string FilterResult(object context) {
			throw new System.NotImplementedException();
		}

		public bool IsExecutionFinished(object context) {
			throw new System.NotImplementedException();
		}

		public float Progress(object context) {
			throw new System.NotImplementedException();
		}
	}
}
