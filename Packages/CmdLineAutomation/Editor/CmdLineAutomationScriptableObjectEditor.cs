using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(CmdLineAutomationScriptableObject))]
[CanEditMultipleObjects]
public class CmdLineAutomationScriptableObjectEditor : Editor {
	CmdLineAutomationScriptableObject _target;
	private ImguiInteractiveShell _shellGui;
	private List<string> lines = new List<string>();
	private string _lastRuntime;
	private GUIStyle textStyleNoWrap = null;

	public CmdLineAutomationScriptableObject Target => _target != null ? _target
		: _target = target as CmdLineAutomationScriptableObject;

	public ImguiInteractiveShell ShellGui => _shellGui != null ? _shellGui : _shellGui = new ImguiInteractiveShell();

	public override void OnInspectorGUI() {
		if (textStyleNoWrap == null) {
			textStyleNoWrap = new GUIStyle("label");
			textStyleNoWrap.wordWrap = false;
			textStyleNoWrap.font = Font.CreateDynamicFontFromOSFont("Consolas", 12);
		}
		DrawDefaultInspector();
		string command = ShellGui.PromptGUI(textStyleNoWrap);
		if (command != null) {
			EditorUtility.SetDirty(Target);
		}
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Run Commands")) {
			if (ShellGui.IsStarted) {
				RunCommands();
			} else {
				ShellGui.OnLineRead = PopulateOutputText;
				ShellGui.Start();
				EditorApplication.delayCall += RunCommands;
			}
		}
		ShellGui.ButtonGUI(textStyleNoWrap);
		if (GUILayout.Button("Clear Output")) {
			ClearLines();
		}
		GUILayout.EndHorizontal();
		EditorGUILayout.TextArea(_lastRuntime, textStyleNoWrap);
		serializedObject.Update();
		serializedObject.ApplyModifiedProperties();
	}

	private void ClearLines() {
		lines.Clear();
		_lastRuntime = "";
	}

	private void PopulateOutputText() {
		ShellGui.shell.GetRecentLines(lines);
		_lastRuntime = string.Join("\n", lines);
	}

	private void RunCommands() {
		Target.RunCommands(_shellGui.shell);
	}
}
