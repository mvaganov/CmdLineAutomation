using System;
using System.Text;
using System.Text.RegularExpressions;

namespace RunCmd {
	[Serializable]
	public class NamedRegexSearch {
		public const string CommandPromptRegexWindows =
			"^[A-Z]:\\\\(?:[^\\\\/:*?\" <>|\\r\\n]+\\\\)*[^\\\\/:*? \"<>|\\r\\n]*>";

		/// <summary>
		/// Name for the variable from the regex search
		/// </summary>
		public string Name;
		/// <summary>
		/// If true, do not process this regular expression.
		/// </summary>
		public bool Ignore;
		/// <summary>
		/// How the variable is discovered, using regular expression.
		/// Need help writing a regular expression? Ask ChatGPT! (I wonder how well this comment will age)
		/// </summary>
		public string _regex;
		/// <summary>
		/// Only parse the regex string once, and cache the result here.
		/// </summary>
		private Regex _compiledRegex;
		/// <summary>
		/// leave empty to get the entire match
		/// </summary>
		public int[] GroupsToInclude;
		/// <summary>
		/// Populated at runtime
		/// </summary>
		public string RuntimeValue;
		public enum SpecialReadLogic { None, IgnoreAfterFirstValue, OnlyCheckLastOutput }
		/// <summary>
		/// Processing optimizations, to prevent regex from running if it doesn't make sense
		/// </summary>
		public SpecialReadLogic readLogic = SpecialReadLogic.None;

		public string RegexString {
			get => _regex;
			set {
				_regex = value;
				_compiledRegex = null;
			}
		}

		public NamedRegexSearch(string name, string regex) : this(name, regex, null, false) { }
		public NamedRegexSearch(string name, string regex, int[] groupsToInclude, bool ignore) {
			Name = name;
			RegexString = regex;
			GroupsToInclude = groupsToInclude;
			Ignore = ignore;
		}

		public string Process(string input, bool isLastLine) {
			switch (readLogic) {
				case SpecialReadLogic.OnlyCheckLastOutput: if (!isLastLine) {
						return null;
					}
					break;
			}
			return Process(input);
		}

		public string Process(string input) {
			if (Ignore) { return null; }
			if (_compiledRegex == null) {
				_compiledRegex = new Regex(RegexString);
			}
			Match m = _compiledRegex.Match(input);
			if (!m.Success) {
				return null;
			}
			//Debug.LogWarning($"success {Regex}\n{input}\n{m.Value}");
			if (GroupsToInclude == null || GroupsToInclude.Length == 0) {
				return RuntimeValue = m.Value;
			}
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < GroupsToInclude.Length; ++i) {
				sb.Append(m.Groups[GroupsToInclude[i]]);
			}
			switch (readLogic) {
				case SpecialReadLogic.IgnoreAfterFirstValue: Ignore = true; break;
			}
			return RuntimeValue = sb.ToString();
		}

		public NamedRegexSearch Clone() {
			NamedRegexSearch result = new NamedRegexSearch(Name, _regex) {
				Ignore = Ignore,
				GroupsToInclude = GroupsToInclude,
				readLogic = readLogic,
			};

			return result;
		}

		public static implicit operator NamedRegexSearch(string regex) => new NamedRegexSearch("", regex);
	}
}
