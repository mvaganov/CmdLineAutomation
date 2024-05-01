using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(CmdLineAutomationScriptableObject))]
[CanEditMultipleObjects]
public class LookAtPointEditor : Editor {

	CmdLineAutomationScriptableObject _target;
	private IMGUIInteractiveShell _shellGui;

	public CmdLineAutomationScriptableObject Target => _target != null ? _target
		: _target = target as CmdLineAutomationScriptableObject;

	public IMGUIInteractiveShell Shell => _shellGui != null ? _shellGui : _shellGui = new IMGUIInteractiveShell();

	void OnEnable() {
		//scriptableObject = serializedObject.FindProperty("lookAtPoint");
	}

	public override void OnInspectorGUI() {
		if (DrawDefaultInspector()) {
		}
		if (GUILayout.Button("Run Commands")) {
			if (_shellGui.IsStarted) {
				RunCommands();
			} else {
				_shellGui.Start();
				_shellGui.shell.OnLineRead += PopulateOutputText;
				EditorApplication.delayCall += RunCommands;
			}
		}
		serializedObject.Update();
		Shell.OnGUI();
		serializedObject.ApplyModifiedProperties();
	}

	private void PopulateOutputText() {
		List<string> lines = new List<string>();
		Shell.shell.PeekRecentLines(lines);
		Target.LastRuntimeOutput = string.Join("\n", lines);
	}

	private void RunCommands() {
		for (int i = 0; i < Target.Commands.Length; i++) {
			_shellGui.shell.RunCommand(Target.Commands[i]);
		}
	}
}
