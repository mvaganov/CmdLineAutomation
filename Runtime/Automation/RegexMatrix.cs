using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RunCmdRedux {
	[System.Serializable]
	public class RegexMatrix {

		[System.Serializable]
		public class Row {
			public string Purpose;
			public List<NamedRegexSearch> list = new List<NamedRegexSearch>();
			public Action<string> OnTrigger = delegate { };
			public Row(Action<string> regexCallback, string[] expressions) {
				if (expressions != null) {
					for (int i = 0; i < expressions.Length; ++i) {
						list.Add(expressions[i]);
					}
				}
				if (regexCallback != null) {
					OnTrigger += regexCallback;
				}
			}
			public Row() { }
			public Row(string purpose) : this(purpose, null) { }
			public Row(string purpose, IList<NamedRegexSearch> regexTriggers) {
				Purpose = purpose;
				if (regexTriggers != null) {
					list = new List<NamedRegexSearch>(regexTriggers);
				}
			}

			public Row Clone() {
				Row clone = new Row(Purpose);
				for (int i = 0; i < list.Count; ++i) {
					clone.list.Add(list[i].Clone());
				}
				clone.OnTrigger = OnTrigger.Clone() as Action<string>;
				return clone;
			}
		}

		[SerializeField] private Row[] _regexTriggers = new Row[0];
		private bool _isWaitingForTrigger = false;
		private string _currentLine = "";

		public Row[] Rows => _regexTriggers;

		public bool HasRegexTriggers => _isWaitingForTrigger;

		public RegexMatrix() : this(null) { }

		public RegexMatrix(Row[] matrix) {
			if (matrix == null) {
				return;
			}
			_regexTriggers = matrix;
			IsWaitingForTriggerRecalculate();
		}

		public bool IsWaitingForTriggerRecalculate() {
			for (int groupId = 0; groupId < _regexTriggers.Length; ++groupId) {
				List<NamedRegexSearch> regexList = _regexTriggers[groupId].list;
				for (int regexIndex = 0; regexIndex < regexList.Count; ++regexIndex) {
					if (!regexList[regexIndex].Ignore) {
						return _isWaitingForTrigger = true;
					}
				}
			}
			return _isWaitingForTrigger = false;
		}

		public void ClearRows() {
			for (int i = 0; i < _regexTriggers.Length; ++i) {
				_regexTriggers[i].list.Clear();
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
						_regexTriggers[coord.row].OnTrigger.Invoke(_regexTriggers[coord.row].list[coord.col].RuntimeValue);
					});
				}
				printLine(checkedLine);
				_currentLine = _currentLine.Substring(firstNewlineIndex);
			} while (firstNewlineIndex >= 0);
		}

		public int CountTriggeringExpressions(string line, List<(int row, int col)> triggeringExpressions) {
			int count = 0;
			triggeringExpressions?.Clear();
			for (int row = 0; row < _regexTriggers.Length; row++) {
				List<NamedRegexSearch> group = _regexTriggers[row].list;
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

		public RegexMatrix Clone() {
			RegexMatrix clone = new RegexMatrix();
			clone._regexTriggers = new Row[_regexTriggers.Length];
			for(int i = 0; i < _regexTriggers.Length; ++i) {
				clone._regexTriggers[i] = _regexTriggers[i].Clone();
			}
			return clone;
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
