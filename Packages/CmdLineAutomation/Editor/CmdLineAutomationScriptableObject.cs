using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/CmdLineAutomation", order = 1)]
public class CmdLineAutomationScriptableObject : ScriptableObject {
	public string Name;
	public string[] Commands;
	[TextArea(1, 1000)]
	public string LastRuntimeOutput;
}
