using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/CmdLineAutomation", order = 1)]
public class CmdLineAutomationScriptableObject : ScriptableObject {
	[System.Serializable]
	public class Data {
		[TextArea(1, 1000)]
		public string Description;
	}
	public string[] Commands;
	public Data _details;

	public void RunCommands(InteractiveCmdShell shell) {
		for (int i = 0; i < Commands.Length; i++) {
			shell.RunCommand(Commands[i]);
		}
	}
}
