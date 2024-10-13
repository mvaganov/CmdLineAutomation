using System.Collections;
using System.Text;

namespace RunCmd {
	public static partial class Parse {

		public static string ToString(object parsedToken, int indent = 0, bool includeWhitespace = true) {
			StringBuilder sb = new StringBuilder();
			switch (parsedToken) {
				case string s: sb.Append("\"").Append(s).Append("\""); break;
				case Token token: sb.Append("`").Append(token.Text).Append("`"); break;
				case IList list: ToStringArray(sb, list, indent, includeWhitespace); break;
				case IDictionary dict: ToStringDictionary(sb, dict, indent, includeWhitespace); break;
				default: sb.Append($"unexpected {(parsedToken == null ? "null" : parsedToken.GetType().ToString())}"); break;
			}
			return sb.ToString();
		}

		private static void ToStringToken(StringBuilder sb, Token tok) => sb.Append("\"").Append(tok.Text).Append("\"");

		private static void ToStringArray(StringBuilder sb, IList list, int indent, bool includeWhitespace) {
			sb.Append("[");
			int lines = 0;
			for (int i = 0; i < list.Count; ++i) {
				if (i > 0) { sb.Append(includeWhitespace ? ", " : ","); }
				string element = ToString(list[i], indent + 1, includeWhitespace);
				if (includeWhitespace) { PossiblyIndent(sb, element, indent); }
				sb.Append(element);
				lines += Count(element, "\n");
			}
			if (includeWhitespace && lines > 0) {
				sb.Append("\n");
				IndentInternal(sb, indent);
			}
			sb.Append("]");
		}

		private static void PossiblyIndent(StringBuilder sb, string element, int indent) {
			if ("[{".IndexOf(element[0]) >= 0 && Count(element, "\n") > 0) {
				sb.Append("\n");
				IndentInternal(sb, indent + 1);
			}
		}

		private static int Count(string haystack, string needle) {
			int i = -1, count = 0;
			while (i < haystack.Length) {
				i = haystack.IndexOf(needle, i + 1);
				if (i < 0) { break; }
				++count;
			}
			return count;
		}

		private static void ToStringDictionary(StringBuilder sb, IDictionary dict, int indent, bool includeWhitespace) {
			sb.Append("{");
			++indent;
			bool addedOne = false;
			foreach (DictionaryEntry kvp in dict) {
				if (addedOne) { sb.Append(","); }
				PossibleWhiteSpaceAfterKeyValuePair();
				sb.Append(ToString(kvp.Key, indent + 1, includeWhitespace))
					.Append(includeWhitespace ? " : " : ":");
				string element = ToString(kvp.Value, indent + 1, includeWhitespace);
				if (includeWhitespace) { PossiblyIndent(sb, element, indent); }
				sb.Append(element);
				addedOne = true;
			}
			--indent;
			PossibleWhiteSpaceAfterKeyValuePair();
			sb.Append("}");

			void PossibleWhiteSpaceAfterKeyValuePair() {
				if (!includeWhitespace) { return; }
				sb.Append("\n");
				IndentInternal(sb, indent);
			}
		}

		private static void IndentInternal(StringBuilder sb, int indent) {
			for (int i = 0; i < indent; ++i) { sb.Append("  "); }
		}
	}
}