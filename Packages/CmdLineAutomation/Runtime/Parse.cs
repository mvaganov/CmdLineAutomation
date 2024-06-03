using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace RunCmd {
	/// <summary>
	/// parse lists and dictionaries
	/// </summary>
	public static class Parse {
		public struct Token {
			public enum Kind { None, Text, Delim, TokBeg, TokEnd }
			public string Text;
			public Kind kind;
			public int TextIndex;
			public int TextEndIndex => TextIndex + Text.Length;
			public Token(string text, Kind kind, int textIndex) { this.Text = text; this.kind = kind; this.TextIndex = textIndex; }
			public Token(char letter, Kind kind, int textIndex) : this(letter.ToString(), kind, textIndex) { }
			public Token(string text, int textIndex) : this(text, Kind.Text, textIndex) { }
			public static implicit operator Token(string text) => new Token(text, -1);
			public static implicit operator string(Token token) => token.Text;
			public override string ToString() => TextIndex >= 0 ? $"({kind}){Text}@{TextIndex}" : Text;
			public override int GetHashCode() => Text.GetHashCode();
			public override bool Equals(object obj) => obj is Token t && t.kind == kind && t.Text == Text;
		}

		private class TokenParsing {
			private char _readingLiteralToken = '\0';
			private int _index, _start = 0, _end = -1;
			private readonly List<Token> _tokens = new List<Token>();
			private readonly string _command;
			private char _currentChar;
			private readonly Dictionary<char, System.Action<TokenParsing>> _perCharacterAction;
			private static readonly Dictionary<char, System.Action<TokenParsing>> DefaultPerCharacterAction;
			private readonly string _escapeSequence;

			static TokenParsing() {
				DefaultPerCharacterAction = InitializeDefaultActions();
			}

			private static Dictionary<char, System.Action<TokenParsing>> InitializeDefaultActions() {
				var actions = new Dictionary<char, System.Action<TokenParsing>>();
				foreach (char c in ",:{}[]()") actions[c] = ReadDelimiter;
				foreach (char c in " \n\t") actions[c] = ReadWhitespace;
				foreach (char c in "\"\'") actions[c] = ReadLiteralToken;
				actions['\\'] = ReadEscapeSequence;
				return actions;
			}

			public TokenParsing(string command, string delimiters, string whitespace, string literalTokens, string escapeSequence) {
				_command = command;
				_perCharacterAction = new Dictionary<char, System.Action<TokenParsing>>();
				InitializeActions(delimiters, ReadDelimiter);
				InitializeActions(whitespace, ReadWhitespace);
				InitializeActions(literalTokens, ReadLiteralToken);
				InitializeActions(escapeSequence, ReadEscapeSequence);
			}

			private void InitializeActions(string characters, System.Action<TokenParsing> action) {
				foreach (char c in characters) {
					_perCharacterAction[c] = action;
				}
			}

			public TokenParsing(string command) {
				this._command = command;
				_perCharacterAction = DefaultPerCharacterAction;
			}

			private static void ReadEscapeSequence(TokenParsing self) => self.HandleEscapeSequence();
			private static void ReadDelimiter(TokenParsing self) => self.HandleDelimiter();
			private static void ReadLiteralToken(TokenParsing self) => self.HandleLiteralToken();
			private static void ReadWhitespace(TokenParsing self) => self.HandleWhitespace();

			private bool IsReadingLiteral => _readingLiteralToken != '\0';

			public IList<Token> SplitTokens() {
				for (_index = 0; _index < _command.Length; _index++) {
					_currentChar = _command[_index];
					if (_perCharacterAction.TryGetValue(_currentChar, out var action)) {
						action.Invoke(this);
					} else {
						HandleTokenCharacter();
					}
				}
				if (_start >= 0 && _end < 0) {
					_readingLiteralToken = '\0';
					HandleWhitespace();
				}
				return _tokens;
			}

			private void HandleWhitespace() {
				if (IsReadingLiteral) { return; }
				if (_end < 0 && _start >= 0) {
					_end = _index;
					AddTokenIfNotEmpty(_start, _end);
					ResetIndices();
				}
			}

			private void AddTokenIfNotEmpty(int start, int end) {
				int len = end - start;
				if (len > 0) {
					_tokens.Add(new Token(_command.Substring(start, len), start));
				}
			}

			private void ResetIndices() {
				_start = -1;
				_end = -1;
			}

			private void HandleLiteralToken() {
				if (!IsReadingLiteral) {
					_readingLiteralToken = _currentChar;
					_tokens.Add(new Token(_currentChar, Token.Kind.TokBeg, _index));
					_start = _index + 1;
				} else if (_readingLiteralToken == _currentChar) {
					_end = _index;
					string substring = GetTokenSubstring(_start, _end).Replace("\\", "");
					_tokens.Add(new Token(substring, Token.Kind.Text, _start));
					_tokens.Add(new Token(_currentChar, Token.Kind.TokEnd, _index));
					ResetIndices();
					_readingLiteralToken = '\0';
				}
			}

			private string GetTokenSubstring(int start, int end) {
				return _command.Substring(start, end - start);
			}

			private void HandleDelimiter() {
				if (IsReadingLiteral) { return; }
				_end = _index;
				if (_start < 0) {
					_start = _end;
				}
				AddTokenIfNotEmpty(_start, _end);
				_tokens.Add(new Token(_currentChar, Token.Kind.Delim, _index));
				ResetIndices();
			}

			private void HandleEscapeSequence() {
				if (!IsReadingLiteral) { return; }
				++_index;
			}

			private void HandleTokenCharacter() {
				if (_start >= 0) { return; }
				_start = _index;
			}
		}

		public enum ErrorKind {
			None, Success, UnexpectedInitialToken, MissingEndToken, UnexpectedDelimiter, MissingDictionaryKey, MissingDictionaryValue, UnexpectedToken
		}

		public struct ParseResult {
			public ErrorKind kind;
			public int TextIndex;
			public bool IsError => kind switch { ErrorKind.None => false, ErrorKind.Success => false, _ => true };
			public ParseResult(ErrorKind kind, int textIndex) { this.kind = kind; this.TextIndex = textIndex; }
			public static ParseResult None = new ParseResult(ErrorKind.None, -1);
			public override string ToString() => $"{kind}@{TextIndex}";
		}

		public static object ParseText(string text, out ParseResult error) {
			IList<Token> tokens = new TokenParsing(text).SplitTokens();
			int tokenIndex = 0;
			object result = ParseTokens(tokens, ref tokenIndex, out error);
			if (error.kind == ErrorKind.None) {
				error.kind = ErrorKind.Success;
			}
			return result;
		}

		public static object ParseTokens(IList<Token> tokens, ref int tokenIndex, out ParseResult error) {
			Token token = tokens[tokenIndex];
			error = ParseResult.None;
			switch (token.kind) {
				case Token.Kind.Delim:
					return token.Text switch {
						"[" => ParseArray(tokens, ref tokenIndex, out error),
						"{" => ParseDictionary(tokens, ref tokenIndex, out error),
						_ => SetErrorAndReturnNull(ErrorKind.UnexpectedDelimiter, token, ref error)
					};
				case Token.Kind.Text:
					return token;
			}
			return SetErrorAndReturnNull(ErrorKind.UnexpectedToken, token, ref error);
		}

		private static object SetErrorAndReturnNull(ErrorKind kind, Token token, ref ParseResult error) {
			error = new ParseResult(kind, token.TextIndex);
			return null;
		}

		public static IList ParseArray(IList<Token> tokens, ref int tokenIndex, out ParseResult error) {
			Token token = tokens[tokenIndex];
			error = ParseResult.None;
			if (!IsExpectedDelimiter(token, "[", ref error)) {
				return null;
			}
			++tokenIndex;
			List<object> arrayValue = new List<object>();
			int loopguard = 0;
			while (tokenIndex < tokens.Count) {
				token = tokens[tokenIndex];
				if (++loopguard > 10000) {
					throw new System.Exception($"Parsing loop exceeded at token {token.TextIndex}!");
				}
				ParseArrayElement(arrayValue, tokens, ref tokenIndex, ref error, out bool finished);
				if (finished) {
					return arrayValue;
				}
			}
			error = new ParseResult(ErrorKind.MissingEndToken, token.TextEndIndex);
			return arrayValue;
		}

		public static IDictionary ParseDictionary(IList<Token> tokens, ref int tokenIndex, out ParseResult error) {
			Token token = tokens[tokenIndex];
			error = ParseResult.None;
			if (!IsExpectedDelimiter(token, "{", ref error)) {
				return null;
			}
			++tokenIndex;
			error = ParseResult.None;
			OrderedDictionary dictionaryValue = new OrderedDictionary();
			object key = null, value = null;
			int loopguard = 0;
			while (tokenIndex < tokens.Count) {
				token = tokens[tokenIndex];
				if (loopguard++ > 10000) {
					throw new System.Exception($"Parsing loop exceeded at token {token.TextIndex}!");
				}
				ParseDictionaryElement(ref key, ref value, tokens, ref tokenIndex, ref error, out bool finished);
				if (finished) { return dictionaryValue; }
				if (key != null && value != null) {
					dictionaryValue[key] = value;
					key = value = null;
				}
			}
			return dictionaryValue;
		}

		private static bool IsExpectedDelimiter(Token token, string expected, ref ParseResult error) {
			if (token.kind == Token.Kind.Delim && token.Text == expected) {
				return true;
			}
			error = new ParseResult(ErrorKind.UnexpectedDelimiter, token.TextIndex);
			return false;
		}

		private static void ParseArrayElement(List<object> arrayValue, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
			Token token = tokens[tokenIndex];
			finished = false;
			switch (token.kind) {
				case Token.Kind.None:
				case Token.Kind.TokBeg:
				case Token.Kind.TokEnd: ++tokenIndex; break;
				case Token.Kind.Text: arrayValue.Add(token); ++tokenIndex; break;
				case Token.Kind.Delim: ParseArrayDelim(arrayValue, tokens, ref tokenIndex, ref error, out finished); break;
				default: error = new ParseResult(ErrorKind.UnexpectedToken, token.TextEndIndex); finished = true; break;
			}
		}

		private static void ParseDictionaryElement(ref object key, ref object value, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
			Token token = tokens[tokenIndex];
			finished = false;
			switch (token.kind) {
				case Token.Kind.None:
				case Token.Kind.TokBeg:
				case Token.Kind.TokEnd: ++tokenIndex; break;
				case Token.Kind.Text: if (key == null) { key = token; } else { value = token; } ++tokenIndex; break;
				case Token.Kind.Delim: ParseKeyValuePairDelim(ref key, ref value, tokens, ref tokenIndex, ref error, out finished); break;
				default: error = new ParseResult(ErrorKind.UnexpectedDelimiter, token.TextIndex); break;
			}
		}

		private static void ParseArrayDelim(List<object> arrayValue, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
			finished = false;
			switch (tokens[tokenIndex].Text) {
				case ",": ++tokenIndex; break;
				case "]": ++tokenIndex; finished = true; break;
				case "[": arrayValue.Add(ParseArray(tokens, ref tokenIndex, out error)); break;
				case "{": arrayValue.Add(ParseDictionary(tokens, ref tokenIndex, out error)); break;
				default: error = new ParseResult(ErrorKind.UnexpectedDelimiter, tokens[tokenIndex].TextIndex); break;
			}
			if (error.kind != ErrorKind.None) { finished = true; }
		}

		public static void ParseKeyValuePairDelim(ref object key, ref object value, IList<Token> tokens, ref int tokenIndex, ref ParseResult error, out bool finished) {
			Token token = tokens[tokenIndex];
			finished = false;
			if (key == null) {
				switch (token.Text) {
					case ":": error = new ParseResult(ErrorKind.MissingDictionaryKey, token.TextIndex); break;
					case ",": ++tokenIndex; break;
					case "[": key = ParseArray(tokens, ref tokenIndex, out error); break;
					case "{": key = ParseDictionary(tokens, ref tokenIndex, out error); break;
					case "}": ++tokenIndex; finished = true; break;
					default: error = new ParseResult(ErrorKind.UnexpectedDelimiter, token.TextIndex); break;
				}
			} else {
				switch (token.Text) {
					case ":": ++tokenIndex; break;
					case ",": error = new ParseResult(ErrorKind.MissingDictionaryValue, token.TextIndex); break;
					case "[": value = ParseArray(tokens, ref tokenIndex, out error); break;
					case "{": value = ParseDictionary(tokens, ref tokenIndex, out error); break;
					case "}": error = new ParseResult(ErrorKind.UnexpectedDelimiter, token.TextIndex); break;
					default: error = new ParseResult(ErrorKind.UnexpectedDelimiter, token.TextIndex); break;
				}
			}
			if (error.IsError) {
				finished = true;
			}
		}

		public static string ToString(object parsedToken, int indent = 0, bool includeWhitespace = true) {
			StringBuilder sb = new StringBuilder();
			switch (parsedToken) {
				case Token token:        ToStringToken(sb, token); break;
				case IList<object> list: ToStringArray(sb, list, indent, includeWhitespace); break;
				case IDictionary dict:   ToStringDictionary(sb, dict, indent, includeWhitespace); break;
			}
			return sb.ToString();
		}

		private static void ToStringToken(StringBuilder sb, Token tok) => sb.Append("\"").Append(tok.Text).Append("\"");

		private static void ToStringArray(StringBuilder sb, IList<object> list, int indent, bool includeWhitespace) {
			sb.Append("[");
			for (int i = 0; i < list.Count; ++i) {
				if (i > 0) {
					sb.Append(includeWhitespace ? ", " : ",");
				}
				sb.Append(ToString(list[i], indent + 1, includeWhitespace));
			}
			sb.Append("]");
		}

		private static void ToStringDictionary(StringBuilder sb, IDictionary dict, int indent, bool includeWhitespace) {
			sb.Append("{");
			++indent;
			bool addedOne = false;
			foreach (DictionaryEntry kvp in dict) {
				if (addedOne) { sb.Append(","); }
				PossibleWhiteSpaceAfterKeyValuePair();
				sb.Append(ToString(kvp.Key, indent + 1, includeWhitespace))
					.Append(includeWhitespace ? " : " : ":").Append(ToString(kvp.Value, indent + 1));
				addedOne = true;
			}
			--indent;
			PossibleWhiteSpaceAfterKeyValuePair();
			sb.Append(includeWhitespace ? "}\n" : "}");
			
			void PossibleWhiteSpaceAfterKeyValuePair() {
				if (!includeWhitespace) { return; }
				sb.Append("\n");
				for (int i = 0; i < indent; ++i) { sb.Append("  "); }
			}
		}
	}
}
