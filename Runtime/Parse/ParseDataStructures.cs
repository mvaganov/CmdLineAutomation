using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RunCmd {
	public static partial class Parse {
		/// <summary>
		/// String with metadata describing where the token came from in it's source text body.
		/// </summary>
		public struct Token {
			public enum Kind { None, Text, Delim, TokBeg, TokEnd }
			public string Text;
			public Kind TokenKind;
			public int TextIndex;
			public int TextEndIndex => TextIndex + Text.Length;
			public Token(string text, Kind kind, int textIndex) {
				Text = text; TokenKind = kind; TextIndex = textIndex;
			}
			public Token(char letter, Kind kind, int textIndex)
				: this(letter.ToString(), kind, textIndex) { }
			public Token(string text, int textIndex)
				: this(text, Kind.Text, textIndex) { }
			public static implicit operator Token(string text) => new Token(text, -1);
			public static implicit operator string(Token token) => token.Text;
			public override string ToString() =>
				TextIndex >= 0 ? $"({TokenKind}){Text}@{TextIndex}" : Text;
			public override int GetHashCode() => Text.GetHashCode();
			public override bool Equals(object obj) =>
				obj is Token t && t.TokenKind == TokenKind && t.Text == Text;
		}

		/// <summary>
		/// Result of a string parse
		/// </summary>
		public struct ParseResult {
			public enum Kind {
				None, Success, UnexpectedInitialToken, MissingEndToken, UnexpectedDelimiter,
				MissingDictionaryKey, MissingDictionaryAssignment, UnexpectedToken, UnknownError,
				MissingTarget
			}
			public Kind ResultKind;
			public int TextIndex;
			public bool IsError => ResultKind switch {
				Kind.None => false,
				Kind.Success => false,
				_ => true
			};
			public ParseResult(Kind kind, int textIndex) {
				this.ResultKind = kind;
				this.TextIndex = textIndex;
			}
			public static ParseResult None = new ParseResult(Kind.None, -1);
			public override string ToString() =>
				TextIndex < 0 ? $"{ResultKind}" : $"{ResultKind}@{TextIndex}";
		}

		/// <summary>
		/// Data structure that turns a string into a combination of IList/IDictionary/Token values.
		/// Roughly modeled on JSON text parsing.
		/// </summary>
		public class TokenParsing {
			private char _readingLiteralToken = '\0';
			private int _index, _start = 0, _end = -1;
			private readonly List<Token> _tokens = new List<Token>();
			private readonly string _text;
			private char _currentChar;
			private readonly Dictionary<char, Action<TokenParsing>> _perCharacterAction;
			private static readonly Dictionary<char, Action<TokenParsing>> DefaultPerCharacterAction;

			/// <summary>
			/// If using <see cref="SplitTokensIncrementally"/>, this value will give a progress when
			/// compared to <see cref="LastIndex"/>
			/// </summary>
			public int CurrentIndex => _index;

			/// <summary>
			/// If using <see cref="SplitTokensIncrementally"/>, this value will give a progress when
			/// compared to <see cref="CurrentIndex"/>
			/// </summary>
			public int LastIndex => _text.Length;

			static TokenParsing() {
				DefaultPerCharacterAction = InitializeDefaultActions();
			}

			private static Dictionary<char, Action<TokenParsing>> InitializeDefaultActions() {
				var actions = new Dictionary<char, Action<TokenParsing>>();
				InitializeActions(",:{}[]()", actions, ReadDelimiter);
				InitializeActions(" \t\n\r", actions, ReadWhitespace);
				InitializeActions("\"\'", actions, ReadLiteralToken);
				InitializeActions("\\", actions, ReadEscapeSequence);
				return actions;
			}

			public TokenParsing(string command, string delimiters, string whitespace,
			string literalTokens, string escapeSequence) {
				_text = command;
				_perCharacterAction = new Dictionary<char, System.Action<TokenParsing>>();
				InitializeActions(delimiters, _perCharacterAction, ReadDelimiter);
				InitializeActions(whitespace, _perCharacterAction, ReadWhitespace);
				InitializeActions(literalTokens, _perCharacterAction, ReadLiteralToken);
				InitializeActions(escapeSequence, _perCharacterAction, ReadEscapeSequence);
			}

			private static void InitializeActions(string characters,
			Dictionary<char, Action<TokenParsing>> actionDictionary, Action<TokenParsing> action) {
				foreach (char c in characters) { actionDictionary[c] = action; }
			}

			public TokenParsing(string command) {
				_text = command;
				_perCharacterAction = DefaultPerCharacterAction;
			}

			private static void ReadEscapeSequence(TokenParsing self) => self.HandleEscapeSequence();
			private static void ReadDelimiter(TokenParsing self) => self.HandleDelimiter();
			private static void ReadLiteralToken(TokenParsing self) => self.HandleLiteralToken();
			private static void ReadWhitespace(TokenParsing self) => self.HandleWhitespace();

			private bool IsReadingLiteral => _readingLiteralToken != '\0';

			/// <summary>
			/// Split all tokens at once, blocking call.
			/// </summary>
			/// <returns></returns>
			public IList<Token> SplitTokens() {
				for (_index = 0; _index < _text.Length; _index++) {
					IterateCharacter();
				}
				FinishParsing();
				return _tokens;
			}

			/// <summary>
			/// Non-blocking parse method, for halting parse if text is very large
			/// </summary>
			/// <param name="tokens">should be null when this is called the first time</param>
			/// <param name="shouldPause"></param>
			public bool SplitTokensIncrementally(ref IList<Token> tokens, Func<bool> shouldPause) {
				if (tokens == null) {
					_tokens.Clear();
					_readingLiteralToken = '\0';
					_index = _start = 0;
					_end = -1;
					tokens = _tokens;
				}
				while (_index < _text.Length) {
					IterateCharacter();
					++_index;
					if (shouldPause.Invoke()) {
						return true;
					}
				}
				FinishParsing();
				return false;
			}

			private void IterateCharacter() {
				_currentChar = _text[_index];
				if (_perCharacterAction.TryGetValue(_currentChar, out var action)) {
					action.Invoke(this);
				} else {
					HandleTokenCharacter();
				}
			}

			private void FinishParsing() {
				if (_start >= 0 && _end < 0) {
					_readingLiteralToken = '\0';
					HandleWhitespace();
				}
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
				if (end <= start) { return; }
				_tokens.Add(GetTokenSubstring(start, end));
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
					string substring = Regex.Unescape(GetTokenSubstring(_start, _end));
					_tokens.Add(new Token(substring, Token.Kind.Text, _start));
					_tokens.Add(new Token(_currentChar, Token.Kind.TokEnd, _index));
					ResetIndices();
					_readingLiteralToken = '\0';
				}
			}

			private string GetTokenSubstring(int start, int end) {
				return _text.Substring(start, end - start);
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
	}
}
