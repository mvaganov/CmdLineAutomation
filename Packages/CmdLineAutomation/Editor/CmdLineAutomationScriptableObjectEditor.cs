using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// TODO create custom commands for
// * `folderrefresh` reimports the current folder (so changes show up in the project view)
[CustomEditor(typeof(CmdLineAutomationScriptableObject))]
[CanEditMultipleObjects]
public class CmdLineAutomationScriptableObjectEditor : Editor {
	private CmdLineAutomationScriptableObject _target;
	private ImguiInteractiveShell _shellGui;
	private List<string> _lines = new List<string>();
	private string _lastRuntime;
	private GUIStyle _consoleTextStyle = null;

	public CmdLineAutomationScriptableObject Target => _target != null ? _target
		: _target = target as CmdLineAutomationScriptableObject;

	public ImguiInteractiveShell ShellGui => _shellGui != null ? _shellGui : _shellGui = new ImguiInteractiveShell();

	public void RefreshInspector() {
		EditorApplication.delayCall += RefreshInspectorInternal;
	}

	private void RefreshInspectorInternal() {
		EditorUtility.SetDirty(Target);
	}

	public override void OnInspectorGUI() {
		if (_consoleTextStyle == null) {
			_consoleTextStyle = new GUIStyle("label");
			_consoleTextStyle.wordWrap = false;
			_consoleTextStyle.font = Font.CreateDynamicFontFromOSFont("Consolas", 12);
		}
		DrawDefaultInspector();
		string command = ShellGui.PromptGUI(_consoleTextStyle);
		if (command != null) { // && !RunInternalCommand(command)) {
			//ShellGui.Execute(command);
			RunInternalCommand(command);
			RefreshInspector();
		}
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Run Commands To Do")) {
			if (ShellGui.IsStarted) {
				RunCommands();
			} else {
				//ShellGui.OnLineRead = PopulateOutputText;
				ShellGui.Start();
				EditorApplication.delayCall += RunCommands;
			}
		}
		ShellGui.ButtonGUI(_consoleTextStyle);
		if (GUILayout.Button("Clear Output")) {
			CmdCls.ClearLines(ShellGui.Shell);
			PopulateOutputText();
		}
		GUILayout.EndHorizontal();
		EditorGUILayout.TextArea(_lastRuntime, _consoleTextStyle);
		serializedObject.Update();
		serializedObject.ApplyModifiedProperties();
	}

	private bool RunInternalCommand(string command) {
		//string firstToken = FirstToken(command);
		//switch (firstToken.ToLower()) {
		//	case "cls":
		//		ClearLines();
		//		RefreshInspector();
		//		return true;
		//}
		command = Target.CommandFilter(ShellGui.Shell, command, PopulateOutputText);
		PopulateOutputText();
		return false;
	}

	private static string FirstToken(string command) {
		int endOfFirstToken = command.IndexOf(' ');
		return endOfFirstToken > 0 ? command.Substring(endOfFirstToken) : command;
	}

	//private void ClearLines() {
	//	_lines.Clear();
	//	_lastRuntime = "";
	//}

	private void PopulateOutputText(string latestLine) {
		PopulateOutputText();
	}

	private void PopulateOutputText() {
		_lines.Clear();
		ShellGui.Shell.GetRecentLines(_lines);
		_lastRuntime = string.Join("\n", _lines);
		//Debug.Log("LINES "+_lastRuntime);
		RefreshInspector();
	}

	private void RunCommands() {
		Target.RunCommands(_shellGui.Shell, StdOutput);
		RefreshInspector();
	}

	private void StdOutput(string line) {
		PopulateOutputText();
	}
}
