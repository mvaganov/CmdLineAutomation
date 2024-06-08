using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Events;

namespace RunCmd {
	[Serializable]
	public class NamedRegexSearch {
		public const string CommandPromptRegexWindows =
			"^[A-Z]:\\\\(?:[^\\\\/:*?\" <>|\\r\\n]+\\\\)*[^\\\\/:*? \"<>|\\r\\n]*>";

		[Serializable]
		public class UnityEvent_string : UnityEvent<string> { }
		[Serializable]
		public struct UnityEvents {
			public UnityEvent_string onValueProcessed;
		}

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
		public string Regex;
		/// <summary>
		/// leave empty to get the entire match
		/// </summary>
		public int[] GroupsToInclude;
		/// <summary>
		/// Populated at runtime
		/// </summary>
		public string RuntimeValue;
		/// <summary>
		/// Called when the value is set by <see cref="Process(string)"/>
		/// </summary>
		public UnityEvents Events;

		public NamedRegexSearch(string name, string regex) : this(name, regex, null, false) { }
		public NamedRegexSearch(string name, string regex, int[] groupsToInclude, bool ignore) {
			Name = name;
			Regex = regex;
			GroupsToInclude = groupsToInclude;
			Ignore = ignore;
		}
		public string Process(string input) {
			if (Ignore) { return null; }
			Match m = System.Text.RegularExpressions.Regex.Match(input, Regex);
			if (!m.Success) {
				return null;
			}
			//Debug.LogWarning($"success {Regex}\n{input}\n{m.Value}");
			if (GroupsToInclude == null || GroupsToInclude.Length == 0) {
				return m.Value;
			}
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < GroupsToInclude.Length; ++i) {
				sb.Append(m.Groups[GroupsToInclude[i]]);
			}
			Events.onValueProcessed?.Invoke(RuntimeValue);
			return RuntimeValue = sb.ToString();
		}
		public static implicit operator NamedRegexSearch(string regex) => new NamedRegexSearch("", regex);
	}
}
