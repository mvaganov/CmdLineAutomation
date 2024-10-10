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
			public object currentElement = null;

			private bool SetCurrentData(object data) {
				UnityEngine.Debug.Log($"setting [{string.Join(",", CurrentPath)}]");
				if (CurrentPath.Count == 0) {
					Result = data;
					return true;
				} else {
					return TrySet(Result, CurrentPath, data);
				}
			}

			public static bool TrySet(object obj, IList<object> ids, object value) {
				if(ids == null || ids.Count == 0) {
					return false;
				}
				if (!TryTraverse(obj, ids, out object lastBranch, out Type branchType, 0, ids.Count - 1)) {
					return false;
				}
				switch (ids[ids.Count - 1]) {
					case string text:
						return Object.TrySetValue(obj, text, value);
					case int index:
						IList ilist = obj as IList;
						if (ilist == null) {
							return false;
						}
						ilist[index] = value;
						return true;
				}
				return true;
			}

			public static bool TryTraverse(object obj, IList<object> ids, out object memberValue, out Type memberType, int idIndexStart = 0, int idIndexEnd = -1) {
				if (obj == null) {
					memberValue = memberType = null;
					return false;
				}
				if (idIndexEnd < 0) {
					idIndexEnd = ids.Count;
				}
				object cursor = memberValue = obj;
				memberType = memberValue != null ? memberValue.GetType() : null;
				for (int i = idIndexStart; i < idIndexEnd; ++i) {
					switch (ids[i]) {
						case string text:
							if (!Object.TryGetValue(cursor, text, out memberValue, out memberType)) {
								return false;
							}
							cursor = memberValue;
							break;
						case int index:
							IList ilist = cursor as IList;
							if (ilist == null) {
								memberValue = memberType = null;
								return false;
							}
							memberValue = ilist[index];
							memberType = ilist.GetType().GetElementType();
							break;
					}
				}
				return true;
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
					object nextValue = ParseTokensCoop();
					if (nextValue != null) {
						SetCurrentData(nextValue);
					}
				}
				return false;
			}

			private void ContinuePath(object newRoute) {
				CurrentPath.Add(currentElement);
				currentElement = newRoute;
				UnityEngine.Debug.Log($"Path to: {newRoute}");
			}

			private void BackPath(object oldRoute) {
				if (currentElement != oldRoute) {
					throw new Exception("unexpected path traversal?!??!");
				}
				if (currentElement != CurrentPath[CurrentPath.Count - 1]) {
					throw new Exception("unexpected path traversal!!!!");
				}
				UnityEngine.Debug.Log($"Finished path: {oldRoute}");
				CurrentPath.RemoveAt(CurrentPath.Count - 1);
				if (CurrentPath.Count != 0) {
					currentElement = CurrentPath[CurrentPath.Count - 1];
					UnityEngine.Debug.Log($"back to: {currentElement}");
				} else {
					UnityEngine.Debug.Log($"back to root!");
				}
			}

			public object ParseTokensCoop() {
				Token token = Tokens[CurrentTokenIndex];
				Error = ParseResult.None;
				switch (token.TokenKind) {
					case Token.Kind.Delim:
						return ParseDelimKnownStructureCoop();
					case Token.Kind.Text:
						return token;
				}
				return SetErrorAndReturnNull(ParseResult.Kind.UnexpectedToken, token, out Error);
			}

			private object ParseDelimKnownStructureCoop() {
				return Tokens[CurrentTokenIndex].Text switch {
					"[" => ParseArrayCoop(),
					"{" => ParseDictionaryCoop(),
					_ => SetErrorAndReturnNull(ParseResult.Kind.UnexpectedDelimiter, Tokens[CurrentTokenIndex], out Error)
				};
			}

			public IList ParseArrayCoop() {
				Token token = Tokens[CurrentTokenIndex];
				Error = ParseResult.None;
				if (!IsExpectedDelimiter(token, "[", ref Error)) { return null; }
				++CurrentTokenIndex;
				List<object> arrayValue = new List<object>();
				//int loopguard = 0;
				int index = 0;
				while (CurrentTokenIndex < Tokens.Count) {
					ContinuePath(index);
					token = Tokens[CurrentTokenIndex];
					//if (++loopguard > 10000) { throw new Exception($"Parsing loop exceeded at token {token.TextIndex}!"); }
					ParseArrayElementCoop(arrayValue, out bool finished);
					if (finished) { return arrayValue; }
					BackPath(index);
					++index;
				}
				Error = new ParseResult(ParseResult.Kind.MissingEndToken, token.TextEndIndex);
				return arrayValue;
			}

			public IDictionary ParseDictionaryCoop() {
				Token token = Tokens[CurrentTokenIndex];
				Error = ParseResult.None;
				if (!IsExpectedDelimiter(token, "{", ref Error)) { return null; }
				++CurrentTokenIndex;
				Error = ParseResult.None;
				OrderedDictionary dictionaryValue = new OrderedDictionary();
				object key = null, value = null;
				//int loopguard = 0;
				while (CurrentTokenIndex < Tokens.Count) {
					token = Tokens[CurrentTokenIndex];
					//if (loopguard++ > 10000) { throw new System.Exception($"Parsing loop exceeded at token {token.TextIndex}!"); }
					ParseDictionaryKeyValuePairCoop(ref key, ref value, out bool finished);
					if (finished) { return dictionaryValue; }
					if (key != null && value != null) {
						dictionaryValue[key] = value;
						key = value = null;
					}
					BackPath(key);
				}
				return dictionaryValue;
			}

			private void ParseArrayElementCoop(List<object> arrayValue, out bool finished) {
				Token token = Tokens[CurrentTokenIndex];
				finished = false;
				switch (token.TokenKind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd: ++CurrentTokenIndex; break;
					case Token.Kind.Text: arrayValue.Add(token); ++CurrentTokenIndex; break;
					case Token.Kind.Delim:
						object elementValue = ParseDelimArrayCoop(ref token, out finished);
						if (elementValue != null) { arrayValue.Add(elementValue); }
						break;
					default: Error = new ParseResult(ParseResult.Kind.UnexpectedToken, token.TextEndIndex); finished = true; break;
				}
				if (Error.ResultKind != ParseResult.Kind.None) { finished = true; }
			}

			private void ParseDictionaryKeyValuePairCoop(ref object key, ref object value, out bool finished) {
				Token token = Tokens[CurrentTokenIndex];
				finished = false;
				switch (token.TokenKind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd: ++CurrentTokenIndex; break;
					case Token.Kind.Text:
						if (key == null) {
							key = token;
							ContinuePath(key);
						} else {
							value = token;
						}
						++CurrentTokenIndex;
						break;
					case Token.Kind.Delim: ParseKeyValuePairDelimCoop(ref key, ref value, out finished); break;
					default: Error = new ParseResult(ParseResult.Kind.UnexpectedDelimiter, token.TextIndex); break;
				}
			}

			private object ParseDelimArrayCoop(ref Token token, out bool finished) {
				finished = false;
				switch (token.Text) {
					case ",": ++CurrentTokenIndex; return null;
					case "]": ++CurrentTokenIndex; finished = true; return null;
					default: return ParseDelimKnownStructureCoop();
				}
			}

			private object ParseDelimDictionaryKeyCoop(ref Token token, out bool finished) {
				finished = false;
				switch (token.Text) {
					case ":": Error = new ParseResult(ParseResult.Kind.MissingDictionaryKey, token.TextIndex); return null;
					case ",": ++CurrentTokenIndex; return null;
					case "}": ++CurrentTokenIndex; finished = true; return null;
					default: return ParseDelimKnownStructureCoop();
				}
			}

			private object ParseDelimDictionaryValueCoop(ref Token token) {
				switch (token.Text) {
					case ":": ++CurrentTokenIndex; return null;
					case ",": Error = new ParseResult(ParseResult.Kind.MissingDictionaryAssignment, token.TextIndex); return null;
					default: return ParseDelimKnownStructureCoop();
				}
			}

			public void ParseKeyValuePairDelimCoop(ref object key, ref object value, out bool finished) {
				Token token = Tokens[CurrentTokenIndex];
				if (key == null) {
					key = ParseDelimDictionaryKey(ref token, Tokens, ref CurrentTokenIndex, ref Error, out finished);
				} else {
					finished = false;
					value = ParseDelimDictionaryValue(ref token, Tokens, ref CurrentTokenIndex, ref Error);
				}
				if (Error.IsError) {
					finished = true;
				}
			}
		}
	}
}
