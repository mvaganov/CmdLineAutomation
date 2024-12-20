using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace RunCmd {
	/// <summary>
	/// Parse lists and dictionaries into <see cref="IList"/> and <see cref="IDictionary"/>,
	/// or a raw token <see cref="IList{T}"/> using type  <see cref="Token"/>
	/// </summary>
	public static partial class Parse {
		/// <summary>
		/// Parse strings, without any meta data or data structures.
		/// Use this to separate string literals from string tokens.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static IList<string> ParseArgs(string text) {
			IList<Token> tokens = new TokenParsing(text).SplitTokens();
			List<string> args = new List<string>();
			for (int i = 0; i < tokens.Count; i++) {
				Token token = tokens[i];
				switch (token.TokenKind) {
					case Token.Kind.Text:
					case Token.Kind.Delim:
						args.Add(token.Text);
						break;
				}
			}
			return args;
		}

		/// <summary>
		/// Parse JSON-like text into a strucure of <see cref="IDictionary"/>, <see cref="IList"/>, and
		/// <see cref="Token"/>
		/// </summary>
		/// <param name="text"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		public static object ParseText(string text, out ParseResult error) {
			IList<Token> tokens = new TokenParsing(text).SplitTokens();
			int tokenIndex = 0;
			object result = ParseTokens(tokens, ref tokenIndex, out error);
			if (error.ResultKind == ParseResult.Kind.None) {
				error.ResultKind = ParseResult.Kind.Success;
			}
			return result;
		}

		/// <summary>
		/// Convert parsed data (from <see cref="TokenParsing"/>) into a structure of
		/// <see cref="IDictionary"/>, <see cref="IList"/>, and <see cref="Token"/>
		/// </summary>
		/// <param name="tokens"></param>
		/// <param name="tokenIndex"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		public static object ParseTokens(IList<Token> tokens, ref int tokenIndex, out ParseResult error) {
			Token token = tokens[tokenIndex];
			error = ParseResult.None;
			switch (token.TokenKind) {
				case Token.Kind.Delim: return ParseDelimKnownStructure(tokens, ref tokenIndex, out error);
				case Token.Kind.Text:  return token;
			}
			return SetErrorAndReturnNull(ParseResult.Kind.UnexpectedToken, token, out error);
		}

		private static object SetErrorAndReturnNull(ParseResult.Kind kind, Token token, out ParseResult error) {
			error = new ParseResult(kind, token.TextIndex);
			return null;
		}

		public static IList ParseArray(IList<Token> tokens, ref int tokenIndex, out ParseResult error) {
			Token token = tokens[tokenIndex];
			error = ParseResult.None;
			if (!IsExpectedDelimiter(token, "[", ref error)) { return null; }
			++tokenIndex;
			List<object> arrayValue = new List<object>();
			//int loopguard = 0;
			while (tokenIndex < tokens.Count) {
				token = tokens[tokenIndex];
				//if (++loopguard > 10000) { throw new Exception($"Parsing loop exceeded at token {token.TextIndex}!"); }
				ParseArrayElement(arrayValue, tokens, ref tokenIndex, ref error, out bool finished);
				if (finished) { return arrayValue; }
			}
			error = new ParseResult(ParseResult.Kind.MissingEndToken, token.TextEndIndex);
			return arrayValue;
		}

		public static IDictionary ParseDictionary(IList<Token> tokens, ref int tokenIndex, out ParseResult error) {
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
				ParseDictionaryKeyValuePair(ref key, ref value, tokens, ref tokenIndex, ref error, out bool finished);
				if (finished) { return dictionaryValue; }
				if (key != null && value != null) {
					dictionaryValue[key] = value;
					key = value = null;
				}
			}
			return dictionaryValue;
		}

		private static bool IsExpectedDelimiter(Token token, string expected, ref ParseResult error) {
			if (token.TokenKind == Token.Kind.Delim && token.Text == expected) { return true; }
			error = new ParseResult(ParseResult.Kind.UnexpectedDelimiter, token.TextIndex);
			return false;
		}

		private static void ParseArrayElement(List<object> arrayValue, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
			Token token = tokens[tokenIndex];
			finished = false;
			switch (token.TokenKind) {
				case Token.Kind.None:
				case Token.Kind.TokBeg:
				case Token.Kind.TokEnd: ++tokenIndex; break;
				case Token.Kind.Text: arrayValue.Add(token); ++tokenIndex; break;
				case Token.Kind.Delim: 
					object elementValue = ParseDelimArray(ref token, tokens, ref tokenIndex, ref error, out finished);
					if (elementValue != null) { arrayValue.Add(elementValue); }
					break;
				default: error = new ParseResult(ParseResult.Kind.UnexpectedToken, token.TextEndIndex); finished = true; break;
			}
			if (error.ResultKind != ParseResult.Kind.None) { finished = true; }
		}

		private static void ParseDictionaryKeyValuePair(ref object key, ref object value, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
			Token token = tokens[tokenIndex];
			finished = false;
			switch (token.TokenKind) {
				case Token.Kind.None:
				case Token.Kind.TokBeg:
				case Token.Kind.TokEnd: ++tokenIndex; break;
				case Token.Kind.Text: if (key == null) { key = token; } else { value = token; } ++tokenIndex; break;
				case Token.Kind.Delim: ParseKeyValuePairDelim(ref key, ref value, tokens, ref tokenIndex, ref error, out finished); break;
				default: error = new ParseResult(ParseResult.Kind.UnexpectedDelimiter, token.TextIndex); break;
			}
		}

		private static object ParseDelimArray(ref Token token, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
			finished = false;
			switch (token.Text) {
				case ",": ++tokenIndex; return null;
				case "]": ++tokenIndex; finished = true; return null;
				default: return ParseDelimKnownStructure(tokens, ref tokenIndex, out error);
			}
		}

		private static object ParseDelimDictionaryKey(ref Token token, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
			finished = false;
			switch (token.Text) {
				case ":": error = new ParseResult(ParseResult.Kind.MissingDictionaryKey, token.TextIndex); return null;
				case ",": ++tokenIndex; return null;
				case "}": ++tokenIndex; finished = true; return null;
				default: return ParseDelimKnownStructure(tokens, ref tokenIndex, out error);
			}
		}

		private static object ParseDelimDictionaryValue(ref Token token, IList<Token> tokens, ref int tokenIndex, ref ParseResult error) {
			switch (token.Text) {
				case ":": ++tokenIndex; return null;
				case ",": error = new ParseResult(ParseResult.Kind.MissingValue, token.TextIndex); return null;
				default: return ParseDelimKnownStructure(tokens, ref tokenIndex, out error);
			}
		}

		private static object ParseDelimKnownStructure(IList<Token> tokens, ref int tokenIndex, out ParseResult error) {
			return tokens[tokenIndex].Text switch {
				"[" => ParseArray(tokens, ref tokenIndex, out error),
				"{" => ParseDictionary(tokens, ref tokenIndex, out error),
				_ => SetErrorAndReturnNull(ParseResult.Kind.UnexpectedDelimiter, tokens[tokenIndex], out error)
			};
		}

		public static void ParseKeyValuePairDelim(ref object key, ref object value, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
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
