using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmdRedux {
	//public partial class CommandAutomation {
	/// <summary>
	/// Exposed information about what commands to automate
	/// </summary>
	[Serializable]
	public class TextCommand : ICloneable {
		[TextArea(1, 1000)] public string Description;

		[TextArea(1, 100)] public string Text;

		public List<ParsedTextCommand> ParsedCommands;

		public IList<string> GetCommands() {
			List<string> commands = new List<string>();
			for(int i = 0; i < ParsedCommands.Count; i++) {
				if (ParsedCommands[i].Ignore) {
					continue;
				}
				commands.Add(ParsedCommands[i].Text);
			}
			return commands;
		}

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
	//}

	[Serializable]
	public class ParsedTextCommand {
		public string Text;
		public bool Ignore;

		public ParsedTextCommand(string text) {
			Text = text;
		}

		public static implicit operator ParsedTextCommand(string text) => new ParsedTextCommand(text);
	}
}
