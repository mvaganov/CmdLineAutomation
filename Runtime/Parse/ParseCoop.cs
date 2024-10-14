using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

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
			// TODO move to ParseObject?
			private static string PathToString(IList<object> path) => $"[{string.Join(",", path)}] {(path.Count == 0 ? "empty" : "")}";

			// TODO move to ParseObject?
			private bool TryGetCurrentData(out object found) {
				bool recovered = false;
				if (CurrentPath.Count == 0) {
					found = Result;
					recovered = true;
				} else {
					recovered = TryGet(Result, CurrentPath, out found);
				}
				return recovered;
			}

			// TODO move to ParseObject?
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

				if (!TryGetCurrentData(out object found)) {
					UnityEngine.Debug.LogError($"FAILED set {value}, missing\n{Parse.ToString(Result)}");
				}
				else if (found != value) {
					UnityEngine.Debug.LogError($"FAILED set {value}, incorrect value {found}\n{Parse.ToString(Result)}");
				}
				return setHappened;
			}

			// TODO move to ParseObject?
			/// <summary>
			/// Set a value in a branching data structure
			/// </summary>
			/// <param name="rootObj">Root data structure</param>
			/// <param name="ids">Path of member variables to traverse, including member that needs to be set</param>
			/// <param name="value">value to apply to the member at the end of the given member path</param>
			/// <returns></returns>
			public static bool TrySet(object rootObj, IList<object> ids, object value) {
				if(ids == null || ids.Count == 0) {
					UnityEngine.Debug.LogError("Is this trying to reset the root object? Shouldn't that be handled in the previous function?");
					return false;
				}
				if (!TryTraverse(rootObj, ids, out object objectWithMember, out Type branchType, 0, ids.Count - 1)) {
					UnityEngine.Debug.LogWarning("FAIL!");
					return false;
				}
				object memberId = ids[ids.Count - 1];
				switch (memberId) {
					case Token token:
					case string text:
					case int index:
						Object.TryGetValueStructured(objectWithMember, memberId, out object currentValue, out _);
						if (currentValue == value) {
							UnityEngine.Debug.Log($"{PathToString(ids)} set correctly");
						} else {
							UnityEngine.Debug.LogWarning($"Setting {PathToString(ids)}\n{objectWithMember}[{memberId}] = {value}");
						}
						return Object.TrySetValueStructured(objectWithMember, memberId, value);
					default:
						UnityEngine.Debug.LogWarning($"what is this?! {memberId} ({memberId.GetType()})");
						break;
				}
				return false;
			}

			// TODO move to ParseObject?
			public static bool TryGet(object obj, IList<object> ids, out object memberValue) {
					return TryTraverse(obj, ids, out memberValue, out _);
			}

			// TODO move to ParseObject?
			public static bool TryTraverse(object rootObject, IList<object> ids, out object memberValue, out Type memberType, int idIndexStart = 0, int idIndexEnd = -1) {
				if (rootObject == null) {
					memberValue = memberType = null;
					UnityEngine.Debug.LogError($"cannot traverse from null object\n{PathToString(ids)}");
					return false;
				}
				if (idIndexEnd < 0) {
					idIndexEnd = ids.Count;
				}
				object cursor = memberValue = rootObject;
				memberType = memberValue != null ? memberValue.GetType() : null;
				for (int i = idIndexStart; i < idIndexEnd; ++i) {
					switch (ids[i]) {
						case string text:
						case Token token:
						case int index:
							object memberName = ids[i];
							if (!Object.TryGetValueStructured(cursor, memberName, out memberValue, out memberType)) {
								UnityEngine.Debug.LogError($"{Parse.ToString(cursor)} does not have member '{Parse.ToString(memberName)}'\n{PathToString(ids)}");
								return false;
							}
							cursor = memberValue;
							break;
						default:
							UnityEngine.Debug.LogError($"{ids[i]} is not a traversable type ({ids[i].GetType()})\n{PathToString(ids)}");
							return false;
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
