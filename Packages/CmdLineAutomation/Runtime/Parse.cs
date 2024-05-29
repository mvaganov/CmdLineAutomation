using System.Collections.Generic;

namespace RunCmd {
	public static class Parse {
		public static string[] Split(string command) {
			return command.Split();
		}

		public struct Token {
			public enum Kind { None, Text, Delim, TokBeg, TokEnd }
			public string text;
			public Kind kind;
			public Token(string text, Kind kind) { this.text = text; this.kind = kind; }
			public Token(char letter, Kind kind) : this(letter.ToString(), kind) { }
			public Token(string text) : this(text, Kind.Text) { }
			public static implicit operator Token(string text) => new Token(text);
			public static implicit operator string(Token token) => token.text;
			public override string ToString() => $"({kind}){text}";
		}

		public static string GetFirstToken(string command) {
			int index = command.IndexOf(' ');
			return index < 0 ? command : command.Substring(0, index);
		}

		public static Token[] SplitTokens(string command, string delimiters, string whitespace, string literalTokens, string escapeSequence) {
			return new ParseState(command, delimiters, whitespace, literalTokens, escapeSequence).SplitTokens();
		}

		private class ParseState {
			char readingLiteralToken = '\0';
			int i, start = 0, end = -1;
			List<Token> tokens = new List<Token>();
			string command;
			char c;
			Dictionary<char, System.Action<ParseState>> perCharacterAction;
			static Dictionary<char, System.Action<ParseState>> DefaultPerCharacterAction;

			static ParseState() {
				DefaultPerCharacterAction = new Dictionary<char, System.Action<ParseState>>();
				System.Array.ForEach(",:{}[]()".ToCharArray(), c => DefaultPerCharacterAction[c] = ReadDelimiter);
				System.Array.ForEach(" \n\t".ToCharArray(), c => DefaultPerCharacterAction[c] = ReadWhitespace);
				System.Array.ForEach("\"\'".ToCharArray(), c => DefaultPerCharacterAction[c] = ReadLiteralToken);
				System.Array.ForEach("\\".ToCharArray(), c => DefaultPerCharacterAction[c] = ReadEscapeSequence);
			}

			public ParseState(string command, string delimiters, string whitespace, string literalTokens, string escapeSequence) {
				this.command = command;
				perCharacterAction = new Dictionary<char, System.Action<ParseState>>();
				System.Array.ForEach(delimiters.ToCharArray(), c => perCharacterAction[c] = ReadDelimiter);
				System.Array.ForEach(whitespace.ToCharArray(), c => perCharacterAction[c] = ReadWhitespace);
				System.Array.ForEach(literalTokens.ToCharArray(), c => perCharacterAction[c] = ReadLiteralToken);
				System.Array.ForEach(escapeSequence.ToCharArray(), c => perCharacterAction[c] = ReadEscapeSequence);
			}

			public ParseState(string command) {
				this.command = command;
				perCharacterAction = DefaultPerCharacterAction;
			}

			private static void ReadEscapeSequence(ParseState self) => self.ReadEscapeSequence();
			private static void ReadDelimiter(ParseState self) => self.ReadDelimiter();
			private static void ReadLiteralToken(ParseState self) => self.ReadLiteralToken();
			private static void ReadWhitespace(ParseState self) => self.ReadWhitespace();
			private static void ReadTokenCharacter(ParseState self) => self.ReadTokenCharacter();

			private bool IsReadingLiteral => readingLiteralToken != '\0';

			public Token[] SplitTokens() {
				for (i = 0; i < command.Length; i++) {
					c = command[i];
					if (perCharacterAction.TryGetValue(c, out System.Action<ParseState> action)) {
						action.Invoke(this);
					} else {
						ReadTokenCharacter();
					}
				}
				if (start >= 0 && end < 0) {
					readingLiteralToken = '\0';
					ReadWhitespace();
				}
				return tokens.ToArray();
			}

			private void ReadWhitespace() {
				if (IsReadingLiteral) { return; }
				if (end < 0 && start >= 0) {
					end = i;
					int len = end - start;
					if (len > 0) {
						tokens.Add(new Token(command.Substring(start, len)));
					}
					start = -1;
					end = -1;
				}
			}

			private void ReadLiteralToken() {
				if (!IsReadingLiteral) {
					readingLiteralToken = c;
					tokens.Add(new Token(c, Token.Kind.TokBeg));
					start = i + 1;
				} else if (readingLiteralToken == c) {
					end = i;
					int len = end - start;
					tokens.Add(new Token(command.Substring(start, len), Token.Kind.Text));
					tokens.Add(new Token(c, Token.Kind.TokEnd));
					start = -1;
					end = -1;
					readingLiteralToken = '\0';
				}
			}

			private void ReadDelimiter() {
				if (IsReadingLiteral) { return; }
				end = i;
				if (start < 0) {
					start = end;
				}
				int len = end - start;
				if (len > 0) {
					tokens.Add(new Token(command.Substring(start, len), Token.Kind.Text));
				}
				tokens.Add(new Token(c, Token.Kind.Delim));
				start = -1;
				end = -1;
			}

			private void ReadEscapeSequence() {
				if (!IsReadingLiteral) { return; }
				++i;
			}

			private void ReadTokenCharacter() {
				if (start >= 0) { return; }
				start = i;
			}
		}
	}
}
