using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

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

			private bool SetCurrentData(object data) {
				UnityEngine.Debug.Log($"setting [{string.Join(",", CurrentPath)}]({CurrentPath.Count})\n{Parse.ToString(data)}");
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
				CurrentPath.Add(newRoute);
				currentElementIndex = newRoute;
				UnityEngine.Debug.Log($"Path to: {newRoute}       [{string.Join(",",CurrentPath)}]({CurrentPath.Count})");
			}

			private void BackPath(object oldRoute) {
				if (!currentElementIndex.Equals(oldRoute)) {
					Type a = currentElementIndex != null ? currentElementIndex.GetType() : null;
					Type b = oldRoute != null ? oldRoute.GetType() : null;
					throw new Exception($"unexpected path back traversal?!??!   '{currentElementIndex}'({a}) vs '{oldRoute}'({b})    [{string.Join(",", CurrentPath)}]({CurrentPath.Count})");
				}
				if (currentElementIndex != null && (CurrentPath.Count == 0 || currentElementIndex != CurrentPath[CurrentPath.Count - 1])) {
					throw new Exception($"unexpected path traversal!!!! {currentElementIndex} vs {CurrentPath[CurrentPath.Count - 1]}");
				}
				CurrentPath.RemoveAt(CurrentPath.Count - 1);
				UnityEngine.Debug.Log($"Finished path: {oldRoute}       [{string.Join(",", CurrentPath)}]({CurrentPath.Count})");
				if (CurrentPath.Count != 0) {
					currentElementIndex = CurrentPath[CurrentPath.Count - 1];
					UnityEngine.Debug.Log($"back to: {currentElementIndex}");
				} else {
					UnityEngine.Debug.Log($"back to root!");
					currentElementIndex = null;
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
				UnityEngine.Debug.Log("PARSEARRAY...");
				bool ignoredLastToken = false;
				while (CurrentTokenIndex < Tokens.Count) {
					if (!ignoredLastToken) {
						ContinuePath(index);
					}
					token = Tokens[CurrentTokenIndex];
					//if (++loopguard > 10000) { throw new Exception($"Parsing loop exceeded at token {token.TextIndex}!"); }
					ParseArrayElementCoop(arrayValue, out bool finished, out ignoredLastToken);
					if (!ignoredLastToken) {
						BackPath(index);
						++index;
					}
					if (finished) { return arrayValue; }
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
				}
				return dictionaryValue;
			}

			private void ParseArrayElementCoop(List<object> arrayValue, out bool finished, out bool ignored) {
				Token token = Tokens[CurrentTokenIndex];
				finished = false;
				switch (token.TokenKind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd: ignored = true; ++CurrentTokenIndex; break;
					case Token.Kind.Text: arrayValue.Add(token); ignored = false; ++CurrentTokenIndex; break;
					case Token.Kind.Delim:
						object elementValue = ParseDelimArrayCoop(ref token, out finished, out ignored);
						if (!ignored) { arrayValue.Add(elementValue); }
						break;
					default: Error = new ParseResult(ParseResult.Kind.UnexpectedToken, token.TextEndIndex); finished = true; ignored = false; break;
				}
				if (Error.ResultKind != ParseResult.Kind.None) { finished = true; }
			}

			private void ParseDictionaryKeyValuePairCoop(ref object key, ref object value, out bool finished) {
				Token token = Tokens[CurrentTokenIndex];
				finished = false;
				switch (token.TokenKind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd:
						++CurrentTokenIndex;
						break;
					case Token.Kind.Text:
						if (key == null) {
							key = token;
							ContinuePath(key);
						} else {
							value = token;
							BackPath(key);
						}
						++CurrentTokenIndex;
						break;
					case Token.Kind.Delim:
						ParseKeyValuePairDelimCoop(ref key, ref value, out finished);
						break;
					default: Error = new ParseResult(ParseResult.Kind.UnexpectedDelimiter, token.TextIndex); break;
				}
			}

			private object ParseDelimArrayCoop(ref Token token, out bool finished, out bool ignored) {
				finished = false;
				switch (token.Text) {
					case ",": ++CurrentTokenIndex; ignored = true; return null;
					case "]": ++CurrentTokenIndex; finished = true; ignored = true; return null;
					default: ignored = false; return ParseDelimKnownStructureCoop();
				}
			}

			private object ParseDelimDictionaryKeyCoop(ref Token token, out bool finished, out bool ignored) {
				finished = false;
				switch (token.Text) {
					case ":": Error = new ParseResult(ParseResult.Kind.MissingDictionaryKey, token.TextIndex); ignored = true; return null;
					case ",": ++CurrentTokenIndex; ignored = true; return null;
					case "}": ++CurrentTokenIndex; finished = true; ignored = true; return null;
					default: ignored = false; return ParseDelimKnownStructureCoop();
				}
			}

			private object ParseDelimDictionaryValueCoop(ref Token token, out bool ignored) {
				switch (token.Text) {
					case ":": ++CurrentTokenIndex; ignored = true; return null;
					case ",": Error = new ParseResult(ParseResult.Kind.MissingDictionaryAssignment, token.TextIndex); ignored = true; return null;
					default: ignored = false; return ParseDelimKnownStructureCoop();
				}
			}

			public void ParseKeyValuePairDelimCoop(ref object key, ref object value, out bool finished) {
				Token token = Tokens[CurrentTokenIndex];
				if (key == null) {
					key = ParseDelimDictionaryKeyCoop(ref token, out finished, out bool ignored);
					if (!ignored) {
						ContinuePath(key);
					}
				} else {
					finished = false;
					value = ParseDelimDictionaryValueCoop(ref token, out bool ignored);
					if (!ignored) {
						BackPath(key);
					}
				}
				if (Error.IsError) {
					finished = true;
				}
			}
		}
	}
}
