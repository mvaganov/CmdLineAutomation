using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/CmdLineAutomation", order = 1)]
public class CmdLineAutomationScriptableObject : ScriptableObject {
	[System.Serializable] public class Data {
		[TextArea(1, 1000)] public string Description;
	}
	[System.Serializable] public class Command {
		public string Text;
		public bool Comment;
	}
	private InteractiveCmdShell shell;
	public Command[] Commands;
	public Data _details;

	public void RunCommand(string command) {
		shell.RunCommand(command);
	}

	public void RunCommands(InteractiveCmdShell shell) {
		this.shell = shell;
		for (int i = 0; i < Commands.Length; i++) {
			if (Commands[i].Comment) { continue; }
			RunCommand(Commands[i].Text);
		}
	}
}
