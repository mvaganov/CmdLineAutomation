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

			private string PathToString() => PathToString(CurrentPath);
			private static string PathToString(IList<object> path) => $"[{string.Join(",", path)}]({path.Count})";

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
					setHappened = TrySet(Result, CurrentPath, value);
				}
				UnityEngine.Debug.Log($"CURRENT STATE {PathToString()}\n{Parse.ToString(Result)}");
				return setHappened;
			}

			/// <summary>
			/// Set a value in a branching data structure
			/// </summary>
			/// <param name="obj">Root data structure</param>
			/// <param name="ids">Path of member variables to traverse, including member that needs to be set</param>
			/// <param name="value">value to apply to the member at the end of the given member path</param>
			/// <returns></returns>
			public static bool TrySet(object obj, IList<object> ids, object value) {
				if(ids == null || ids.Count == 0) {
					UnityEngine.Debug.LogWarning("H!");
					return false;
				}
				if (!TryTraverse(obj, ids, out object lastBranch, out Type branchType, 0, ids.Count - 1)) {
					UnityEngine.Debug.LogWarning("FAIL!");
					return false;
				}
				object memberValue = ids[ids.Count - 1];
				switch (memberValue) {
					case Token token: return SetMember(token);
					case string text: return SetMember(text);
					case int index:
						IList ilist = obj as IList;
						if (ilist == null) {
							return false;
						}
						if (ilist[index] == value) {
							UnityEngine.Debug.Log($"{PathToString(ids)} set correctly");
						} else {
							UnityEngine.Debug.LogWarning($"Setting {PathToString(ids)}\n{value}");
						}
						ilist[index] = value;
						return true;
				}
				bool SetMember(object memberName) {
					Object.TryGetValuePossiblyDictionary(obj, memberName, out object currentValue, out _);
					if (currentValue == value) {
						UnityEngine.Debug.Log($"{PathToString(ids)} set correctly");
					} else {
						UnityEngine.Debug.LogWarning($"Setting {PathToString(ids)}\n{obj}[{memberName}] = {value}");
					}
					return Object.TrySetValuePossiblyIDictionary(obj, memberName, value);
				}
				UnityEngine.Debug.LogWarning($"what is this?! {memberValue} ({memberValue.GetType()})");
				return false;
			}

			public static bool TryTraverse(object obj, IList<object> ids, out object memberValue, out Type memberType, int idIndexStart = 0, int idIndexEnd = -1) {
				if (obj == null) {
					memberValue = memberType = null;
					UnityEngine.Debug.LogError($"cannot traverse from null object\n{PathToString(ids)}");
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
							if (!Traverse(text, out memberValue, out memberType)) { return false; }
							break;
						case Token token:
							if (!Traverse(token, out memberValue, out memberType)) { return false; }
							break;
						case int index:
							IList ilist = cursor as IList;
							if (ilist == null) {
								UnityEngine.Debug.LogError($"{cursor} is not an IList, cannot traverse {index}\n{PathToString(ids)}");
								memberValue = memberType = null;
								return false;
							}
							memberValue = ilist[index];
							memberType = ilist.GetType().GetElementType();
							break;
						default:
							UnityEngine.Debug.LogError($"{ids[i]} is not a traversable type ({ids[i].GetType()})\n{PathToString(ids)}");
							return false;
					}
				}
				bool Traverse(object memberName, out object memberValue, out Type memberType) {
					if (cursor is IDictionary dict && dict.Contains(memberName)) {
						memberValue = dict[memberName];
						memberType = (memberValue != null) ? memberValue.GetType() : null;
						return true;
					}
					if (!Object.TryGetValuePossiblyDictionary(cursor, memberName, out memberValue, out memberType)) {
						UnityEngine.Debug.LogError($"{cursor} does not have member '{memberName}'\n{PathToString(ids)}");
						return false;
					}
					cursor = memberValue;
					return true;
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
				UnityEngine.Debug.Log($"CREATING ARRAY @ {PathToString()}");
				SetCurrentData(arrayValue);
				//int loopguard = 0;
				while (CurrentTokenIndex < Tokens.Count) {
					token = Tokens[CurrentTokenIndex];
					//if (++loopguard > 10000) { throw new Exception($"Parsing loop exceeded at token {token.TextIndex}!"); }
					ParseArrayElementCoop(arrayValue, out bool finished);
					if (finished) {
						return arrayValue;
					}
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
				UnityEngine.Debug.Log($"CREATING DICTIONARY @ {PathToString()}");
				SetCurrentData(dictionaryValue);
				object key = null, value = null;
				//int loopguard = 0;
				while (CurrentTokenIndex < Tokens.Count) {
					token = Tokens[CurrentTokenIndex];
					//if (loopguard++ > 10000) { throw new System.Exception($"Parsing loop exceeded at token {token.TextIndex}!"); }
					ParseDictionaryKeyValuePairCoop(ref key, ref value, out bool finishedDictionary, out bool parsedValue);
					if (finishedDictionary) { return dictionaryValue; }
					if (parsedValue) {
						dictionaryValue[key] = value;
						SetCurrentData(value);
						MergePath(key);
						key = value = null;
					}
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
					case Token.Kind.Text:
						BranchPath(arrayValue.Count);
						SetCurrentData(token);
						arrayValue.Add(token);
						MergePath(arrayValue.Count-1);
						++CurrentTokenIndex; break;
					case Token.Kind.Delim:
						BranchPath(arrayValue.Count);
						object elementValue = ParseDelimArrayCoop(token, out finished, out bool parsedElement);
						SetCurrentData(elementValue);
						MergePath(arrayValue.Count);
						if (parsedElement) { arrayValue.Add(elementValue); }
						break;
					default: Error = new ParseResult(ParseResult.Kind.UnexpectedToken, token.TextEndIndex); finished = true; break;
				}
				if (Error.ResultKind != ParseResult.Kind.None) { finished = true; }
			}

			private void ParseDictionaryKeyValuePairCoop(ref object key, ref object value, out bool finishedDictionary, out bool parsedValue) {
				Token token = Tokens[CurrentTokenIndex];
				finishedDictionary = false;
				switch (token.TokenKind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd:
						++CurrentTokenIndex;
						parsedValue = false;
						break;
					case Token.Kind.Text:
						if (key == null) {
							parsedValue = false;
							key = token;
							BranchPath(key);
						} else {
							parsedValue = true;
							value = token;
						}
						++CurrentTokenIndex;
						break;
					case Token.Kind.Delim:
						ParseKeyValuePairDelimCoop(ref key, ref value, out finishedDictionary, out parsedValue);
						break;
					default:
						parsedValue = false;
						Error = new ParseResult(ParseResult.Kind.UnexpectedDelimiter, token.TextIndex);
						break;
				}
			}

			public void ParseKeyValuePairDelimCoop(ref object key, ref object value, out bool finishedDictionary, out bool parsedValue) {
				Token token = Tokens[CurrentTokenIndex];
				if (key == null) {
					key = ParseDelimDictionaryKeyCoop(ref token, out finishedDictionary, out parsedValue);
					if (parsedValue) {
						BranchPath(key);
					}
				} else {
					finishedDictionary = false;
					value = ParseDelimDictionaryValueCoop(ref token, out parsedValue);
					//if (parsedValue) {
					//	MergePath(key);
					//}
				}
				if (Error.IsError) {
					finishedDictionary = true;
				}
			}

			private object ParseDelimArrayCoop(Token token, out bool finished, out bool parsedElement) {
				finished = false;
				switch (token.Text) {
					case ",": ++CurrentTokenIndex; parsedElement = false; return null;
					case "]": ++CurrentTokenIndex; finished = true; parsedElement = false; return null;
					default: parsedElement = true; return ParseDelimKnownStructureCoop();
				}
			}

			private object ParseDelimDictionaryKeyCoop(ref Token token, out bool finishedDictionary, out bool parsedValue) {
				finishedDictionary = false;
				switch (token.Text) {
					case ":": Error = new ParseResult(ParseResult.Kind.MissingDictionaryKey, token.TextIndex); parsedValue = true; return null;
					case ",": ++CurrentTokenIndex; parsedValue = false; return null;
					case "}": ++CurrentTokenIndex; finishedDictionary = true; parsedValue = false; return null;
					default: parsedValue = true; return ParseDelimKnownStructureCoop();
				}
			}

			private object ParseDelimDictionaryValueCoop(ref Token token, out bool parsedValue) {
				switch (token.Text) {
					case ":": ++CurrentTokenIndex; parsedValue = false; return null;
					case ",": Error = new ParseResult(ParseResult.Kind.MissingDictionaryAssignment, token.TextIndex); parsedValue = false; return null;
					default: parsedValue = true; return ParseDelimKnownStructureCoop();
				}
			}
		}
	}
}
