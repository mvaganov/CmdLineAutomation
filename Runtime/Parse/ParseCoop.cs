using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System;

namespace RunCmd {
	public static partial class Parse {

		private class ParseCoop {
			public object rootData;
			public List<object> position = new List<object>();
			public IList<Token> tokens;
			public int tokenIndex;
			public ParseResult error;
			public int layerIndex = -1;

			private bool SetCurrentData(object data) {
				if (position.Count == 0) {
					rootData = data;
					return true;
				} else {
					if (!TryTraverse(rootData, position, out object obj, out Type objType, 0, position.Count - 1)) {
						return false;
					}
					switch (position[position.Count-1]) {
						case string text:
							return Object.TrySetValue(obj, text, data);
						case int index:
							IList ilist = obj as IList;
							if (ilist == null) {
								return false;
							}
							ilist[index] = data;
							return true;
					}
					return false;
				}
			}

			public static bool TryTraverse(object obj, List<object> ids, out object memberValue, out Type memberType, int idIndexStart = 0, int idIndexEnd = -1) {
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
				this.tokens = tokens;
			}

			// TODO create cooperative func. with SplitTokensIncrementally, make a non-blocking compile
			public object ParseTokensCoop() {
				Token token = tokens[tokenIndex];
				error = ParseResult.None;
				switch (token.TokenKind) {
					case Token.Kind.Delim: return ParseDelimKnownStructureCoop();
					case Token.Kind.Text: return token;
				}
				return SetErrorAndReturnNull(ParseResult.Kind.UnexpectedToken, token, out error);
			}

			public IList ParseArrayCoop() {
				Token token = tokens[tokenIndex];
				error = ParseResult.None;
				if (!IsExpectedDelimiter(token, "[", ref error)) { return null; }
				++tokenIndex;
				List<object> arrayValue = new List<object>();
				//int loopguard = 0;
				while (tokenIndex < tokens.Count) {
					token = tokens[tokenIndex];
					//if (++loopguard > 10000) { throw new Exception($"Parsing loop exceeded at token {token.TextIndex}!"); }
					ParseArrayElementCoop(arrayValue, out bool finished);
					if (finished) { return arrayValue; }
				}
				error = new ParseResult(ParseResult.Kind.MissingEndToken, token.TextEndIndex);
				return arrayValue;
			}

			public IDictionary ParseDictionaryCoop() {
				Token token = tokens[tokenIndex];
				error = ParseResult.None;
				if (!IsExpectedDelimiter(token, "{", ref error)) { return null; }
				++tokenIndex;
				error = ParseResult.None;
				OrderedDictionary dictionaryValue = new OrderedDictionary();
				object key = null, value = null;
				//int loopguard = 0;
				while (tokenIndex < tokens.Count) {
					token = tokens[tokenIndex];
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

			private void ParseArrayElementCoop(List<object> arrayValue, out bool finished) {
				Token token = tokens[tokenIndex];
				finished = false;
				switch (token.TokenKind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd: ++tokenIndex; break;
					case Token.Kind.Text: arrayValue.Add(token); ++tokenIndex; break;
					case Token.Kind.Delim:
						object elementValue = ParseDelimArrayCoop(ref token, out finished);
						if (elementValue != null) { arrayValue.Add(elementValue); }
						break;
					default: error = new ParseResult(ParseResult.Kind.UnexpectedToken, token.TextEndIndex); finished = true; break;
				}
				if (error.ResultKind != ParseResult.Kind.None) { finished = true; }
			}

			private void ParseDictionaryKeyValuePairCoop(ref object key, ref object value, out bool finished) {
				Token token = tokens[tokenIndex];
				finished = false;
				switch (token.TokenKind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd: ++tokenIndex; break;
					case Token.Kind.Text: if (key == null) { key = token; } else { value = token; } ++tokenIndex; break;
					case Token.Kind.Delim: ParseKeyValuePairDelimCoop(ref key, ref value, out finished); break;
					default: error = new ParseResult(ParseResult.Kind.UnexpectedDelimiter, token.TextIndex); break;
				}
			}

			private object ParseDelimArrayCoop(ref Token token, out bool finished) {
				finished = false;
				switch (token.Text) {
					case ",": ++tokenIndex; return null;
					case "]": ++tokenIndex; finished = true; return null;
					default: return ParseDelimKnownStructureCoop();
				}
			}

			private object ParseDelimDictionaryKeyCoop(ref Token token, out bool finished) {
				finished = false;
				switch (token.Text) {
					case ":": error = new ParseResult(ParseResult.Kind.MissingDictionaryKey, token.TextIndex); return null;
					case ",": ++tokenIndex; return null;
					case "}": ++tokenIndex; finished = true; return null;
					default: return ParseDelimKnownStructureCoop();
				}
			}

			private object ParseDelimDictionaryValueCoop(ref Token token) {
				switch (token.Text) {
					case ":": ++tokenIndex; return null;
					case ",": error = new ParseResult(ParseResult.Kind.MissingDictionaryAssignment, token.TextIndex); return null;
					default: return ParseDelimKnownStructureCoop();
				}
			}

			private object ParseDelimKnownStructureCoop() {
				return tokens[tokenIndex].Text switch {
					"[" => ParseArray(tokens, ref tokenIndex, out error),
					"{" => ParseDictionary(tokens, ref tokenIndex, out error),
					_ => SetErrorAndReturnNull(ParseResult.Kind.UnexpectedDelimiter, tokens[tokenIndex], out error)
				};
			}

			public void ParseKeyValuePairDelimCoop(ref object key, ref object value, out bool finished) {
				Token token = tokens[tokenIndex];
				if (key == null) {
					key = ParseDelimDictionaryKey(ref token, tokens, ref tokenIndex, ref error, out finished);
				} else {
					finished = false;
					value = ParseDelimDictionaryValue(ref token, tokens, ref tokenIndex, ref error);
				}
				if (error.IsError) {
					finished = true;
				}
			}
		}
	}
}
