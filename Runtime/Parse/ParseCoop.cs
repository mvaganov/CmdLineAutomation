using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System;

namespace RunCmd {
	public static partial class Parse {

		public class ParseCoop {
			public object Result;
			public object CurrentPosition = null;
			public List<object> CurrentPath = new List<object>();
			public IList<Token> Tokens;
			public int CurrentTokenIndex;
			public ParseResult Error;
			public object currentElementIndex = null;

			private Token CurrentToken => Tokens[CurrentTokenIndex];
			private object CurrentIndex => CurrentPath[CurrentPath.Count - 1];
			private string PathToString() => Object.ShowList(CurrentPath);

			private bool TryGetCurrentData(out object found) {
				bool recovered = false;
				if (CurrentPath.Count == 0) {
					found = Result;
					recovered = true;
				} else {
					recovered = Object.TryGet(Result, CurrentPath, out found);
				}
				return recovered;
			}

			private bool SetCurrentData(object value) {
				bool setHappened = false;
				if (CurrentPath.Count == 0) {
					if (Result == value) {
						UnityEngine.Debug.Log("root already set correctly");
					} else {
						UnityEngine.Debug.LogWarning($"Setting root\n{Parse.ToString(value)}");
					}
					Result = value;
					UnityEngine.Debug.Log($"setting {PathToString()}\n{Parse.ToString(value)}");
					setHappened = true;
				} else {
					UnityEngine.Debug.Log($"setting {PathToString()}\n{Parse.ToString(value)}");
					setHappened = Object.TrySet(Result, CurrentPath, value);
				}
				UnityEngine.Debug.Log($"CURRENT STATE {PathToString()}\n{Parse.ToString(Result)}");

				if (!TryGetCurrentData(out object found)) {
					UnityEngine.Debug.LogError($"FAILED set {value}, missing\n{Parse.ToString(Result)}");
				} else if (found != value) {
					UnityEngine.Debug.LogError($"FAILED set {value}, incorrect value {found}\n{Parse.ToString(Result)}");
				}
				return setHappened;
			}

			public ParseCoop(IList<Token> tokens) {
				Tokens = tokens;
			}

			// TODO create cooperative func. with SplitTokensIncrementally, make a non-blocking compile
			public bool ParseTokensIteratively(Func<bool> shouldBreak) {
				CurrentPosition = null;
				CurrentPath.Clear();

				int loopguard = 0;
				while (CurrentTokenIndex < Tokens.Count) {
					if (shouldBreak != null && shouldBreak.Invoke()) {
						return true;
					}
					if (loopguard > 10000) {
						throw new Exception($"infinite loop? {CurrentTokenIndex}: {Tokens[CurrentTokenIndex]}");
					}
					ParseTokensCoop();
				}
				return false;
			}

			private void BranchPath(object newRoute) {
				CurrentPath.Add(newRoute);
				currentElementIndex = newRoute;
			}

			private void MergePath(object oldRoute) {
				if (!currentElementIndex.Equals(oldRoute)) {
					Type a = currentElementIndex != null ? currentElementIndex.GetType() : null;
					Type b = oldRoute != null ? oldRoute.GetType() : null;
					throw new Exception($"unexpected back traversal?!??!   current '{currentElementIndex}'({a}) vs given '{oldRoute}'({b})    [{string.Join(",", CurrentPath)}]({CurrentPath.Count})");
				}
				if (currentElementIndex != null && (CurrentPath.Count == 0 || currentElementIndex != CurrentIndex)) {
					throw new Exception($"unexpected back traversal!!!! {currentElementIndex} vs {CurrentIndex}");
				}
				CurrentPath.RemoveAt(CurrentPath.Count - 1);
				if (CurrentPath.Count != 0) {
					currentElementIndex = CurrentIndex;
				} else {
					currentElementIndex = null;
				}
			}

			public void ParseTokensCoop() {
				//Token token = Tokens[CurrentTokenIndex];
				Error = ParseResult.None;
				switch (CurrentToken.TokenKind) {
					case Token.Kind.Delim:
						ParseDelimKnownStructureCoop();
						return;
					case Token.Kind.Text:
						SetCurrentData(CurrentToken);
						++CurrentTokenIndex;
						return;
				}
				SetError(ParseResult.Kind.UnexpectedToken, CurrentToken);
			}

			private void ParseDelimKnownStructureCoop() {
				switch (CurrentToken.Text) {
					case "[": ParseArrayCoop(); break;
					case "{": ParseDictionaryCoop(); break;
					default: SetError(ParseResult.Kind.UnexpectedDelimiter, CurrentToken); break;
				}
			}

			enum ArrayChange { None, FinishedArray, ParsedElement, Error }
			public IList ParseArrayCoop() {
				Token token = CurrentToken;
				Error = ParseResult.None;
				if (!IsExpectedDelimiter(token, "[", ref Error)) { return null; }
				++CurrentTokenIndex;
				List<object> arrayValue = new List<object>();
				SetCurrentData(arrayValue);
				while (CurrentTokenIndex < Tokens.Count) {
					switch (ParseArrayElementCoop(arrayValue)) {
						case ArrayChange.FinishedArray:
						case ArrayChange.Error:
							return arrayValue;
					}
				}
				SetError(ParseResult.Kind.MissingEndToken, token);
				return arrayValue;
			}

			enum DictionaryChange { None, FinishedDictionary, ParsedKey, ParsedValue, Error }
			public IDictionary ParseDictionaryCoop() {
				Token token = CurrentToken;
				Error = ParseResult.None;
				if (!IsExpectedDelimiter(token, "{", ref Error)) { return null; }
				++CurrentTokenIndex;
				Error = ParseResult.None;
				OrderedDictionary dictionaryValue = new OrderedDictionary();
				SetCurrentData(dictionaryValue);
				bool needToParseKey = true;
				while (CurrentTokenIndex < Tokens.Count) {
					switch(ParseDictionaryKeyValuePairCoop(ref needToParseKey)){
						case DictionaryChange.FinishedDictionary:
						case DictionaryChange.Error:
							return dictionaryValue;
					}
				}
				SetError(ParseResult.Kind.MissingEndToken, token);
				return dictionaryValue;
			}

			private ArrayChange ParseArrayElementCoop(List<object> arrayValue) {
				Token token = CurrentToken;
				switch (token.TokenKind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd:
						++CurrentTokenIndex;
						return ArrayChange.None;
					case Token.Kind.Text:
						int arrayIndex = arrayValue.Count;
						BranchPath(arrayIndex);
						SetCurrentData(token);
						MergePath(arrayIndex);
						++CurrentTokenIndex;
						return ArrayChange.ParsedElement;
					case Token.Kind.Delim:
						int index = arrayValue.Count;
						BranchPath(index);
						ArrayChange change = ParseDelimArrayCoop();
						MergePath(index);
						return change;
					default:
						SetError(ParseResult.Kind.UnexpectedToken, token);
						return ArrayChange.Error;
				}
			}

			private DictionaryChange ParseDictionaryKeyValuePairCoop(ref bool needToParseKey) {
				Token token = CurrentToken;
				switch (token.TokenKind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd:
						++CurrentTokenIndex;
						return DictionaryChange.None;
					case Token.Kind.Text:
						++CurrentTokenIndex;
						if (needToParseKey) {
							BranchPath(token);
							needToParseKey = false;
							return DictionaryChange.ParsedKey;
						}
						SetCurrentData(token);
						MergePath(CurrentIndex);
						needToParseKey = true;
						return DictionaryChange.ParsedValue;
					case Token.Kind.Delim:
						return ParseKeyValuePairDelimCoop(ref needToParseKey);
					default:
						SetError(ParseResult.Kind.UnexpectedDelimiter, token);
						return DictionaryChange.Error;
				}
			}

			private DictionaryChange ParseKeyValuePairDelimCoop(ref bool needToParseKey) {
				DictionaryChange result;
				if (needToParseKey) {
					result = ParseDelimDictionaryKeyCoop();
					needToParseKey = (result != DictionaryChange.ParsedKey);
					return result;
				}
				result = ParseDelimDictionaryValueCoop();
				needToParseKey = (result == DictionaryChange.ParsedValue);
				return result;
			}

			private ArrayChange ParseDelimArrayCoop() {
				switch (CurrentToken.Text) {
					case ",": ++CurrentTokenIndex; return ArrayChange.None;
					case "]": ++CurrentTokenIndex; return ArrayChange.FinishedArray;
					default: ParseDelimKnownStructureCoop(); return ArrayChange.ParsedElement;
				}
			}

			private DictionaryChange ParseDelimDictionaryKeyCoop() {
				Token token = CurrentToken;
				switch (token.Text) {
					case ":": SetError(ParseResult.Kind.MissingDictionaryKey, token);
						return DictionaryChange.Error;
					case ",": ++CurrentTokenIndex; return DictionaryChange.None;
					case "}": ++CurrentTokenIndex; return DictionaryChange.FinishedDictionary;
					default:
						switch (token.TokenKind) {
							case Token.Kind.Text:
							case Token.Kind.Delim:
								BranchPath(token.Text);
								++CurrentTokenIndex;
								return DictionaryChange.ParsedKey;
							default:
								SetError(ParseResult.Kind.InvalidKey, token);
								return DictionaryChange.Error;
						}
				}
			}

			private DictionaryChange ParseDelimDictionaryValueCoop() {
				switch (CurrentToken.Text) {
					case ":": ++CurrentTokenIndex; return DictionaryChange.None;
					case ",": SetError(ParseResult.Kind.MissingValue, CurrentToken);
						return DictionaryChange.None;
					default:
						ParseDelimKnownStructureCoop();
						MergePath(CurrentIndex);
						return DictionaryChange.ParsedValue;
				}
			}

			private void SetError(ParseResult.Kind errorKind, Token token) {
				Error = new ParseResult(errorKind, token.TextIndex);
			}
		}
	}
}
