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
				this.Tokens = tokens;
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
				if (currentElementIndex != null && (CurrentPath.Count == 0 || currentElementIndex != CurrentPath[CurrentPath.Count - 1])) {
					throw new Exception($"unexpected back traversal!!!! {currentElementIndex} vs {CurrentPath[CurrentPath.Count - 1]}");
				}
				CurrentPath.RemoveAt(CurrentPath.Count - 1);
				if (CurrentPath.Count != 0) {
					currentElementIndex = CurrentPath[CurrentPath.Count - 1];
				} else {
					currentElementIndex = null;
				}
			}

			public void ParseTokensCoop() {
				Token token = Tokens[CurrentTokenIndex];
				Error = ParseResult.None;
				switch (token.TokenKind) {
					case Token.Kind.Delim:
						ParseDelimKnownStructureCoop();
						return;
					case Token.Kind.Text:
						++CurrentTokenIndex;
						SetCurrentData(token);
						return;
				}
				SetErrorAndReturnNull(ParseResult.Kind.UnexpectedToken, token, out Error);
			}

			private object ParseDelimKnownStructureCoop() {
				return Tokens[CurrentTokenIndex].Text switch {
					"[" => ParseArrayCoop(),
					"{" => ParseDictionaryCoop(),
					_ => SetErrorAndReturnNull(ParseResult.Kind.UnexpectedDelimiter, Tokens[CurrentTokenIndex], out Error)
				};
			}

			enum ArrayChange { None, FinishedArray, ParsedElement, Error }
			public IList ParseArrayCoop() {
				Token token = Tokens[CurrentTokenIndex];
				Error = ParseResult.None;
				if (!IsExpectedDelimiter(token, "[", ref Error)) { return null; }
				++CurrentTokenIndex;
				List<object> arrayValue = new List<object>();
				SetCurrentData(arrayValue);
				while (CurrentTokenIndex < Tokens.Count) {
					if (ParseArrayElementCoop(arrayValue) == ArrayChange.FinishedArray) {
						return arrayValue;
					}
				}
				Error = new ParseResult(ParseResult.Kind.MissingEndToken, token.TextEndIndex);
				return arrayValue;
			}

			enum DictionaryChange { None, FinishedDictionary, ParsedKey, ParsedaValue, Error }
			public IDictionary ParseDictionaryCoop() {
				Token token = Tokens[CurrentTokenIndex];
				Error = ParseResult.None;
				if (!IsExpectedDelimiter(token, "{", ref Error)) { return null; }
				++CurrentTokenIndex;
				Error = ParseResult.None;
				OrderedDictionary dictionaryValue = new OrderedDictionary();
				SetCurrentData(dictionaryValue);
				object key = null, value = null;
				while (CurrentTokenIndex < Tokens.Count) {
					if (ParseDictionaryElement(ref key, ref value) == DictionaryChange.FinishedDictionary) {
						return dictionaryValue;
					}
				}
				Error = new ParseResult(ParseResult.Kind.MissingEndToken, token.TextEndIndex);
				return dictionaryValue;
			}

			private DictionaryChange ParseDictionaryElement(ref object key, ref object value) {
				DictionaryChange change = ParseDictionaryKeyValuePairCoop(ref key, ref value);
				switch (change) {
					case DictionaryChange.ParsedKey:
						BranchPath(key);
						break;
					case DictionaryChange.ParsedaValue:
						SetCurrentData(value);
						MergePath(key);
						key = value = null;
						break;
				}
				return change;
			}

			private ArrayChange ParseArrayElementCoop(List<object> arrayValue) {
				Token token = Tokens[CurrentTokenIndex];
				switch (token.TokenKind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd:
						++CurrentTokenIndex;
						return ArrayChange.None;
					case Token.Kind.Text:
						BranchPath(arrayValue.Count);
						SetCurrentData(token);
						MergePath(arrayValue.Count-1);
						++CurrentTokenIndex;
						return ArrayChange.ParsedElement;
					case Token.Kind.Delim:
						int index = arrayValue.Count;
						BranchPath(index);
						ArrayChange change = ParseDelimArrayCoop(token, out object elementValue);
						MergePath(index);
						return change;
					default:
						Error = new ParseResult(ParseResult.Kind.UnexpectedToken, token.TextEndIndex);
						return ArrayChange.Error;
				}
			}

			private DictionaryChange ParseDictionaryKeyValuePairCoop(ref object key, ref object value) {
				Token token = Tokens[CurrentTokenIndex];
				switch (token.TokenKind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd:
						++CurrentTokenIndex;
						return DictionaryChange.None;
					case Token.Kind.Text:
						++CurrentTokenIndex;
						if (key == null) {
							key = token;
							return DictionaryChange.ParsedKey;
						}
						value = token;
						return DictionaryChange.ParsedaValue;
					case Token.Kind.Delim:
						return ParseKeyValuePairDelimCoop(ref key, ref value);
					default:
						Error = new ParseResult(ParseResult.Kind.UnexpectedDelimiter, token.TextIndex);
						return DictionaryChange.Error;
				}
			}

			private DictionaryChange ParseKeyValuePairDelimCoop(ref object key, ref object value) {
				Token token = Tokens[CurrentTokenIndex];
				return (key == null)
					? ParseDelimDictionaryKeyCoop(ref token, out key)
					: ParseDelimDictionaryValueCoop(ref token, out value);
			}

			private ArrayChange ParseDelimArrayCoop(Token token, out object result) {
				switch (token.Text) {
					case ",": ++CurrentTokenIndex; result = null; return ArrayChange.None;
					case "]": ++CurrentTokenIndex; result = null; return ArrayChange.FinishedArray;
					default: result = ParseDelimKnownStructureCoop(); return ArrayChange.ParsedElement;
				}
			}

			private DictionaryChange ParseDelimDictionaryKeyCoop(ref Token token, out object result) {
				switch (token.Text) {
					case ":": Error = new ParseResult(ParseResult.Kind.MissingDictionaryKey, token.TextIndex);
						result = null; return DictionaryChange.Error;
					case ",": ++CurrentTokenIndex; result = null; return DictionaryChange.None;
					case "}": ++CurrentTokenIndex; result = null;  return DictionaryChange.FinishedDictionary;
					default:result = ParseDelimKnownStructureCoop(); return DictionaryChange.ParsedKey;
				}
			}

			private DictionaryChange ParseDelimDictionaryValueCoop(ref Token token, out object result) {
				switch (token.Text) {
					case ":": ++CurrentTokenIndex; result = null; return DictionaryChange.None;
					case ",":
						Error = new ParseResult(ParseResult.Kind.MissingDictionaryAssignment, token.TextIndex);
						result = null; return DictionaryChange.None;
					default: result = ParseDelimKnownStructureCoop(); return DictionaryChange.ParsedaValue;
				}
			}
		}
	}
}
