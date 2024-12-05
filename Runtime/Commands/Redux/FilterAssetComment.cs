using RunCmdRedux;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// Removes a command from the command set if it begins with a given prefix
	/// </summary>
	[CreateAssetMenu(fileName = "Comment", menuName = "ScriptableObjects/FilterAssets/Comment")]
	public class FilterAssetComment : ScriptableObject, ICommandAsset {
		[SerializeField] protected bool _enabled = true;
		[SerializeField] protected string _prefix = "#";
		[SerializeField] protected string _suffix = "";

		public class Proc : BaseNamedProcess {
			public FilterAssetComment _source;
			private bool _inMultiLineComment;

			public Proc(FilterAssetComment source) {
				_source = source;
				_result = "";
			}

			protected string _result;
			public override bool IsExecutionFinished => true;
			public override object Result => _result;

			public override string name => _source.name;

			public override float GetProgress() => 0;
			public override void StartCooperativeFunction(string command, PrintCallback print) {
				if (!_source._enabled) {
					return;
				}
				_result = command;
				if (!_inMultiLineComment && string.IsNullOrEmpty(_source._suffix)) {
					if (command.StartsWith(_source._prefix)) {
						_result = null;
					}
				} else {
					if (!_inMultiLineComment) {
						FindAndIgnoreBlockCommentOrMultilineComment();
					} else {
						IgnoreTextInMultilineCommentUntillCommentEnd();
					}
				}

				void FindAndIgnoreBlockCommentOrMultilineComment() {
					int blockCommentStart = command.IndexOf(_source._prefix);
					if (blockCommentStart >= 0) {
						int blockCommentEnd = command.IndexOf(_source._suffix, blockCommentStart + _source._prefix.Length);
						if (blockCommentEnd >= 0) {
							string firstPart = command.Substring(0, blockCommentStart);
							string secondPart = command.Substring(blockCommentEnd + _source._suffix.Length);
							_result = firstPart + secondPart;
							_inMultiLineComment = false;
						} else {
							_result = command.Substring(0, blockCommentStart);
							_inMultiLineComment = true;
						}
					}
				}

				void IgnoreTextInMultilineCommentUntillCommentEnd() {
					int blockCommentEnd = command.IndexOf(_source._suffix, 0);
					if (blockCommentEnd >= 0) {
						_result = command.Substring(blockCommentEnd + _source._suffix.Length);
						_inMultiLineComment = false;
					} else {
						_result = null;
					}
				}
			}
		}

		public ICommandProcess CreateCommand(object context) {
			return new Proc(this);
		}
	}
}
