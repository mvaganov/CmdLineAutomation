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

			private void SetCurrentData(object data) {
				if (position.Count == 0) {
					rootData = data;
				} else {
					// TODO use TryTraverse, this one is for setting values just before the end.
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
					case Token.Kind.Delim: return ParseDelimKnownStructureCoop(tokens, ref tokenIndex, out error);
					case Token.Kind.Text: return token;
				}
				return SetErrorAndReturnNull(ParseResult.Kind.UnexpectedToken, token, out error);
			}

			public static IList ParseArrayCoop(IList<Token> tokens, ref int tokenIndex, out ParseResult error) {
				Token token = tokens[tokenIndex];
				error = ParseResult.None;
				if (!IsExpectedDelimiter(token, "[", ref error)) { return null; }
				++tokenIndex;
				List<object> arrayValue = new List<object>();
				//int loopguard = 0;
				while (tokenIndex < tokens.Count) {
					token = tokens[tokenIndex];
					//if (++loopguard > 10000) { throw new Exception($"Parsing loop exceeded at token {token.TextIndex}!"); }
					ParseArrayElementCoop(arrayValue, tokens, ref tokenIndex, ref error, out bool finished);
					if (finished) { return arrayValue; }
				}
				error = new ParseResult(ParseResult.Kind.MissingEndToken, token.TextEndIndex);
				return arrayValue;
			}

			public static IDictionary ParseDictionaryCoop(IList<Token> tokens, ref int tokenIndex, out ParseResult error) {
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
					ParseDictionaryKeyValuePairCoop(ref key, ref value, tokens, ref tokenIndex, ref error, out bool finished);
					if (finished) { return dictionaryValue; }
					if (key != null && value != null) {
						dictionaryValue[key] = value;
						key = value = null;
					}
				}
				return dictionaryValue;
			}

			private static void ParseArrayElementCoop(List<object> arrayValue, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
				Token token = tokens[tokenIndex];
				finished = false;
				switch (token.TokenKind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd: ++tokenIndex; break;
					case Token.Kind.Text: arrayValue.Add(token); ++tokenIndex; break;
					case Token.Kind.Delim:
						object elementValue = ParseDelimArrayCoop(ref token, tokens, ref tokenIndex, ref error, out finished);
						if (elementValue != null) { arrayValue.Add(elementValue); }
						break;
					default: error = new ParseResult(ParseResult.Kind.UnexpectedToken, token.TextEndIndex); finished = true; break;
				}
				if (error.ResultKind != ParseResult.Kind.None) { finished = true; }
			}

			private static void ParseDictionaryKeyValuePairCoop(ref object key, ref object value, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
				Token token = tokens[tokenIndex];
				finished = false;
				switch (token.TokenKind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd: ++tokenIndex; break;
					case Token.Kind.Text: if (key == null) { key = token; } else { value = token; } ++tokenIndex; break;
					case Token.Kind.Delim: ParseKeyValuePairDelimCoop(ref key, ref value, tokens, ref tokenIndex, ref error, out finished); break;
					default: error = new ParseResult(ParseResult.Kind.UnexpectedDelimiter, token.TextIndex); break;
				}
			}

			private static object ParseDelimArrayCoop(ref Token token, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
				finished = false;
				switch (token.Text) {
					case ",": ++tokenIndex; return null;
					case "]": ++tokenIndex; finished = true; return null;
					default: return ParseDelimKnownStructureCoop(tokens, ref tokenIndex, out error);
				}
			}

			private static object ParseDelimDictionaryKeyCoop(ref Token token, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
				finished = false;
				switch (token.Text) {
					case ":": error = new ParseResult(ParseResult.Kind.MissingDictionaryKey, token.TextIndex); return null;
					case ",": ++tokenIndex; return null;
					case "}": ++tokenIndex; finished = true; return null;
					default: return ParseDelimKnownStructureCoop(tokens, ref tokenIndex, out error);
				}
			}

			private static object ParseDelimDictionaryValueCoop(ref Token token, IList<Token> tokens, ref int tokenIndex, ref ParseResult error) {
				switch (token.Text) {
					case ":": ++tokenIndex; return null;
					case ",": error = new ParseResult(ParseResult.Kind.MissingDictionaryAssignment, token.TextIndex); return null;
					default: return ParseDelimKnownStructureCoop(tokens, ref tokenIndex, out error);
				}
			}

			private static object ParseDelimKnownStructureCoop(IList<Token> tokens, ref int tokenIndex, out ParseResult error) {
				return tokens[tokenIndex].Text switch {
					"[" => ParseArray(tokens, ref tokenIndex, out error),
					"{" => ParseDictionary(tokens, ref tokenIndex, out error),
					_ => SetErrorAndReturnNull(ParseResult.Kind.UnexpectedDelimiter, tokens[tokenIndex], out error)
				};
			}

			public static void ParseKeyValuePairDelimCoop(ref object key, ref object value, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
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
