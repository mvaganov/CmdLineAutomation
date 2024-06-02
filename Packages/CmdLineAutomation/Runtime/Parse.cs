using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace RunCmd {
	public static class Parse {
		public static string[] Split(string command) {
			return command.Split();
		}

		public struct Token {
			public enum Kind { None, Text, Delim, TokBeg, TokEnd }
			public string text;
			public Kind kind;
			public int index;
			public Token(string text, Kind kind, int index) { this.text = text; this.kind = kind; this.index = index; }
			public Token(char letter, Kind kind, int index) : this(letter.ToString(), kind, index) { }
			public Token(string text, int index) : this(text, Kind.Text, index) { }
			public static implicit operator Token(string text) => new Token(text, -1);
			public static implicit operator string(Token token) => token.text;
			public override string ToString() => index >= 0 ? $"({kind}){text}@{index}" : text;
			public override int GetHashCode() => text.GetHashCode();
			public override bool Equals(object obj) => obj is Token t && t.kind == kind && t.text == text;
		}

		public static string GetFirstToken(string command) {
			int index = command.IndexOf(' ');
			return index < 0 ? command : command.Substring(0, index);
		}

		public static IList<Token> SplitTokens(string command, string delimiters, string whitespace, string literalTokens, string escapeSequence) {
			return new ParseState(command, delimiters, whitespace, literalTokens, escapeSequence).SplitTokens();
		}

		public static IList<Token> SplitTokens(string command) => new ParseState(command).SplitTokens();

		private class ParseState {
			char readingLiteralToken = '\0';
			int i, start = 0, end = -1;
			List<Token> tokens = new List<Token>();
			string command;
			char c;
			Dictionary<char, System.Action<ParseState>> perCharacterAction;
			static Dictionary<char, System.Action<ParseState>> DefaultPerCharacterAction;
			private string _escapeSequence;

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

			public IList<Token> SplitTokens() {
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
				return tokens;
			}

			private void ReadWhitespace() {
				if (IsReadingLiteral) { return; }
				if (end < 0 && start >= 0) {
					end = i;
					int len = end - start;
					if (len > 0) {
						tokens.Add(new Token(command.Substring(start, len), start));
					}
					start = -1;
					end = -1;
				}
			}

			private void ReadLiteralToken() {
				if (!IsReadingLiteral) {
					readingLiteralToken = c;
					tokens.Add(new Token(c, Token.Kind.TokBeg, i));
					start = i + 1;
				} else if (readingLiteralToken == c) {
					end = i;
					int len = end - start;
					// TODO replace all escape sequence characters...
					string substring = command.Substring(start, len).Replace("\\", "");
					tokens.Add(new Token(substring, Token.Kind.Text, start));
					tokens.Add(new Token(c, Token.Kind.TokEnd, i));
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
					tokens.Add(new Token(command.Substring(start, len), Token.Kind.Text, start));
				}
				tokens.Add(new Token(c, Token.Kind.Delim, i));
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

		public enum ErrorKind {
			None, UnexpectedInitialToken, MissingEndToken, UnexpectedDelimiter, MissingDictionaryKey, MissingDictionaryValue, UnexpectedToken
		}

		public struct Error {
			public ErrorKind kind;
			public int index;
			public Error(ErrorKind kind, int index) { this.kind = kind; this.index = index; }
			public static Error None = new Error(ErrorKind.None, -1);
			public override string ToString() => $"{kind}@{index}";
		}

		public static object ParseText(string text, out Error error) {
			IList<Token> tokens = new ParseState(text).SplitTokens();
			int index = 0;
			return ParseTokens(tokens, ref index, out error);
		}

		public static object ParseTokens(IList<Token> tokens, ref int index, out Error error) {
			Token token = tokens[index];
			error = Error.None;
			switch (token.kind) {
				case Token.Kind.Delim:
					switch (token.text) {
						case "[": return ParseArray(tokens, ref index, out error);
						case "{": return ParseDictionary(tokens, ref index, out error);
						default:
							error = new Error(ErrorKind.UnexpectedDelimiter, token.index);
							return null;
					}
				case Token.Kind.Text:
					return token;
			}
			error = new Error(ErrorKind.UnexpectedToken, token.index);
			return null;
		}

		public static IList ParseArray(IList<Token> tokens, ref int index, out Error error) {
			Token token = tokens[index];
			if (token.kind != Token.Kind.Delim || token.text != "[") {
				error = new Error(ErrorKind.UnexpectedInitialToken, token.index);
				return null;
			}
			++index;
			error = Error.None;
			List<object> arrayValue = new List<object>();
			int loopguard = 0;
			while (index < tokens.Count) {
				token = tokens[index];
				if (loopguard++ > 10000) {
					throw new System.Exception($"broke @{token.index}!");
				}
				switch (token.kind) {
					case Token.Kind.None:
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd:
						++index;
						continue;
					case Token.Kind.Delim:
						switch (token.text) {
							case "]":
								++index;
								return arrayValue;
							case "[":
								IList subArray = ParseArray(tokens, ref index, out error);
								arrayValue.Add(subArray);
								if (error.kind != ErrorKind.None) {
									return arrayValue;
								}
								continue;
							case "{":
								IDictionary subDictionary = ParseDictionary(tokens, ref index, out error);
								arrayValue.Add(subDictionary);
								if (error.kind != ErrorKind.None) {
									return arrayValue;
								}
								continue;
							case "}":
								error = new Error(ErrorKind.UnexpectedDelimiter, token.index);
								return arrayValue;
						}
						break;
				}
				arrayValue.Add(token);
				++index;
			}
			token = tokens[tokens.Count - 1];
			error = new Error(ErrorKind.MissingEndToken, token.index + token.text.Length);
			return arrayValue;
		}

		public static IDictionary ParseDictionary(IList<Token> tokens, ref int index, out Error error) {
			Token token = tokens[index];
			if (token.kind != Token.Kind.Delim || token.text != "{") {
				error = new Error(ErrorKind.UnexpectedInitialToken, token.index);
				return null;
			}
			++index;
			error = Error.None; 
			OrderedDictionary dictionaryValue = new OrderedDictionary();
			bool isReadingKey = true;
			object key = null, value = null;
			int loopguard = 0;
			while (index < tokens.Count) {
				token = tokens[index];
				if (loopguard++ > 10000) {
					throw new System.Exception($"broke @{token.index}!");
				}
				switch (token.kind) {
					case Token.Kind.Delim:
						switch (token.text) {
							case ":":
								if (isReadingKey) {
									error = new Error(ErrorKind.MissingDictionaryKey, token.index);
									return dictionaryValue;
								}
								++index;
								continue;
							case ",":
								if (!isReadingKey) {
									error = new Error(ErrorKind.MissingDictionaryValue, token.index);
									return dictionaryValue;
								}
								++index;
								continue;
							case "[":
								IList subArray = ParseArray(tokens, ref index, out error);
								if (isReadingKey) {
									key = subArray;
								} else {
									value = subArray;
								}
								break;
							case "{":
								IDictionary subDictionary = ParseDictionary(tokens, ref index, out error);
								if (isReadingKey) {
									key = subDictionary;
								} else {
									value = subDictionary;
								}
								break;
							case "}":
								++index;
								return dictionaryValue;
							case "]":
								error = new Error(ErrorKind.UnexpectedDelimiter, token.index);
								return dictionaryValue;
							default:
								++index;
								break;
						}
						break;
					case Token.Kind.TokBeg:
					case Token.Kind.TokEnd:
						++index;
						continue;
					default:
						value = token;
						++index;
						break;
				}
				if (isReadingKey) {
					key = token;
				} else {
					dictionaryValue[key] = value;
				}
				isReadingKey = !isReadingKey;
			}
			return dictionaryValue;
		}

		public static string ToString(object parsedToken, int indent = 0, bool includeWhitespace = true) {
			StringBuilder sb = new StringBuilder();
			StringBuilder Indent() {
				for (int i = 0; i < indent; ++i) { sb.Append("  "); }
				return sb;
			}
			switch (parsedToken) {
				case Token tok:
					sb.Append("\"").Append(tok.text).Append("\"");
					break;
				case IList<object> list:
					sb.Append("[");
					for (int i = 0; i < list.Count; ++i) {
						if (i > 0) {
							sb.Append(includeWhitespace ? ", " : ",");
						}
						sb.Append(ToString(list[i], indent + 1, includeWhitespace));
					}
					sb.Append("]");
					break;
				case IDictionary dict:
					sb.Append("{");
					++indent;
					bool addedOne = false;
					foreach (DictionaryEntry kvp in dict) {
						if (addedOne) { sb.Append(","); }
						if (includeWhitespace) {
							sb.Append("\n");
							Indent();
						}
						sb.Append(ToString(kvp.Key, indent + 1, includeWhitespace))
							.Append(includeWhitespace?" : ":":").Append(ToString(kvp.Value, indent + 1));
						addedOne = true;
					}
					--indent;
					if (includeWhitespace) {
						sb.Append("\n");
						Indent();
					}
					sb.Append(includeWhitespace?"}\n":"}");
					break;
			}
			return sb.ToString();
		}
	}
}
