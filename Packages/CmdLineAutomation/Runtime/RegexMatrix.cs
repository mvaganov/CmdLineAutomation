using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace RunCmd {
	[System.Serializable]
	public class RegexMatrix {
		[System.Serializable]
		public class UnityEvent_string : UnityEvent<string> { }

		[System.Serializable]
		public class Row {
			public string purpose;
			public List<NamedRegexSearch> list = new List<NamedRegexSearch>();
			public UnityEvent_string OnTrigger = new UnityEvent_string();
			public Row(Action<string> regexCallback, string[] expressions) {
				if (expressions != null) {
					for (int i = 0; i < expressions.Length; ++i) {
						list.Add(expressions[i]);
					}
				}
				if (regexCallback != null) {
					UnityAction<string> unityAction = new UnityAction<string>(regexCallback);
					OnTrigger.AddListener(unityAction);
				}
			}
		}

		private Row[] _regexMatrix = new Row[0];
		private bool _isWaitingForTrigger = false;
		private string _currentLine = "";

		public Row[] Rows => _regexMatrix;

		public bool HasRegexTriggers => _isWaitingForTrigger;

		public RegexMatrix() : this(null) { }

		public RegexMatrix(Row[] matrix) {
			if (matrix == null) {
				return;
			}
			_regexMatrix = matrix;
			IsWaitingForTriggerRecalculate();
		}

		public bool IsWaitingForTriggerRecalculate() {
			for (int groupId = 0; groupId < _regexMatrix.Length; ++groupId) {
				List<NamedRegexSearch> regexList = _regexMatrix[groupId].list;
				for (int regexIndex = 0; regexIndex < regexList.Count; ++regexIndex) {
					if (!regexList[regexIndex].Ignore) {
						return _isWaitingForTrigger = true;
					}
				}
			}
			return _isWaitingForTrigger = false;
		}

		public void ClearRows() {
			for (int i = 0; i < _regexMatrix.Length; ++i) {
				_regexMatrix[i].list.Clear();
			}
			_isWaitingForTrigger = false;
		}

		public void ProcessAndCheckTextForTriggeringLines(string newTextToCheck, System.Action<string> printLine, List<(int row, int col)> _triggeredGroup) {
			_currentLine += newTextToCheck;
			int firstNewlineIndex;
			do {
				firstNewlineIndex = _currentLine.IndexOf('\n');
				if (firstNewlineIndex < 0) {
					break;
				}
				++firstNewlineIndex;
				string checkedLine = _currentLine.Substring(0, firstNewlineIndex);
				_triggeredGroup.Clear();
				if (CountTriggeringExpressions(checkedLine, _triggeredGroup) > 0) {
					_triggeredGroup.ForEach(coord => {
						_regexMatrix[coord.row].OnTrigger.Invoke(_regexMatrix[coord.row].list[coord.col].RuntimeValue);
					});
				}
				printLine(checkedLine);
				_currentLine = _currentLine.Substring(firstNewlineIndex);
			} while (firstNewlineIndex >= 0);
		}

		public int CountTriggeringExpressions(string line, List<(int row, int col)> triggeringExpressions) {
			int count = 0;
			triggeringExpressions?.Clear();
			for (int row = 0; row < _regexMatrix.Length; row++) {
				List<NamedRegexSearch> group = _regexMatrix[row].list;
				for (int col = 0; col < group.Count; ++col) {
					NamedRegexSearch regexSearch = group[col];
					if (regexSearch.Process(line) != null) {
						triggeringExpressions?.Add((row,col));
						++count;
					}
				}
			}
			return count;
		}

		public void Add(int row, string trigger) {
			Rows[row].list.Add(trigger);
		}

		public bool Remove(int row, string trigger) => Remove(Rows[row].list, trigger);

		private bool Remove(List<NamedRegexSearch> regexSearchList, string trigger) {
			int index = regexSearchList.FindIndex(namedSearch => namedSearch.RegexString == trigger);
			if (index >= 0) {
				regexSearchList.RemoveAt(index);
				return true;
			}
			return false;
		}
	}
}
