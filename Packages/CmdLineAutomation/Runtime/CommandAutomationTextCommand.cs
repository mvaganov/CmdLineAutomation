using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RunCmd {
	public partial class CommandAutomation {
		/// <summary>
		/// Exposed information about what commands to automate
		/// </summary>
		[Serializable]
		public class TextCommand : ICloneable {
			[TextArea(1, 1000)] public string Description;

			[TextArea(1, 100)] public string Text;

			public List<ParsedTextCommand> ParsedCommands;

			public void Parse() {
				string[] lines = Text.Split("\n");
				ParsedCommands = new List<ParsedTextCommand>(lines.Length);
				for (int i = 0; i < lines.Length; ++i) {
					string text = lines[i].Replace("\r", "");
					ParsedCommands.Add(new ParsedTextCommand(text));
				}
			}

			public TextCommand CloneSelf() {
				TextCommand textCommand = new TextCommand();
				textCommand.Description = Description;
				textCommand.Text = Text;
				textCommand.ParsedCommands = new List<ParsedTextCommand>(ParsedCommands);
				return textCommand;
			}

			public object Clone() => CloneSelf();
		}

		[Serializable]
		public class ParsedTextCommand {
			public string Text;
			public bool Ignore;

			public ParsedTextCommand(string text) {
				Text = text;
			}
		}

		[Serializable]
		public class RegexSearch {
			/// <summary>
			/// Name for the variable from the regex search
			/// </summary>
			public string Name;
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

			public RegexSearch(string name, string regex) : this(name, regex, null) { }
			public RegexSearch(string name, string regex, int[] groupsToInclude) {
				Name = name;
				Regex = regex;
				GroupsToInclude = groupsToInclude;
			}
			public string Process(string input) {
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
				return RuntimeValue = sb.ToString();
			}
			public static implicit operator RegexSearch(string regex) => new RegexSearch("", regex);
		}
	}
}
