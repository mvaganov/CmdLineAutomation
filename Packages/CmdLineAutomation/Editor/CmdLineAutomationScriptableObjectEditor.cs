using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// TODO create custom commands for
// * `folderrefresh` reimports the current folder (so changes show up in the project view)
[CustomEditor(typeof(CmdLineAutomationScriptableObject))]
[CanEditMultipleObjects]
public class CmdLineAutomationScriptableObjectEditor : Editor, IReferencesCmdShell {
	private CmdLineAutomationScriptableObject _target;
	private ImguiInteractiveShell _shellGui;
	private List<string> _lines = new List<string>();
	private string _lastRuntime;
	private GUIStyle _consoleTextStyle = null;

	public CmdLineAutomationScriptableObject Target => _target != null ? _target
		: _target = target as CmdLineAutomationScriptableObject;

	public InteractiveCmdShell Shell => Target.Shell;

	private void OnEnable() {
		if (ShellGui.Shell != Shell) {
			ShellGui.Shell = Shell;
			PopulateOutputText();
		}
	}

	public ImguiInteractiveShell ShellGui {
		get {
			if (_shellGui != null) {
				return _shellGui;
			}
			CmdLineAutomationScriptableObject target = Target;
			_shellGui = new ImguiInteractiveShell(() => {
				InteractiveCmdShell thisShell = InteractiveCmdShell.CreateUnityEditorShell();
				thisShell.Name = $"{Target.name} {System.Environment.TickCount}";
				target.Shell = thisShell;
				thisShell.KeepAlive = () => {
					bool lostScriptableObject = Target != target;
					if (lostScriptableObject) {
						Debug.LogWarning($"lost {nameof(CmdLineAutomationScriptableObject)}");
						return false;
					}
					if (Target.Shell != thisShell) {
						Debug.LogWarning($"lost {nameof(InteractiveCmdShell)}");
						thisShell.Stop();
						return false;
					}
					return true;
				};
				return thisShell;
			});
			return _shellGui;
		}
	}

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
		if (command != null) {
			if (waitingForCommand) {
				Debug.Log("waiting for command to finish...");
			} else {
				RunInternalCommand(command);
				RefreshInspector();
			}
		}
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Run Commands To Do")) {
			if (ShellGui.IsStarted) {
				RunCommands();
			} else {
				ShellGui.Start();
				EditorApplication.delayCall += RunCommands;
			}
		}
		ShellGui.ButtonGUI(_consoleTextStyle);
		if (GUILayout.Button("Clear Output")) {
			if (ShellGui != null && ShellGui.Shell != null) {
				ShellGui.Shell.ClearLines();
			}
			PopulateOutputText();
		}
		GUILayout.EndHorizontal();
		EditorGUILayout.TextArea(_lastRuntime, _consoleTextStyle);
		serializedObject.Update();
		serializedObject.ApplyModifiedProperties();

		for (int i = 0; i < InteractiveCmdShell.RunningShells.Count; i++) {
			InteractiveCmdShell sh = InteractiveCmdShell.RunningShells[i];
			string label = "";
			bool endIfSelected = Target.Shell == sh;
			bool commandeer = Target.Shell == null;
			if (sh == null) {
				label = "null";
			} else {
				string action = endIfSelected ? "[end]" : commandeer ? "[commandeer]" : "";
				string runningMark = sh.IsRunning ? "" : "<dead> ";
				label = $"{action} {runningMark} \"{sh.Name}\" (lines {sh.Lines.Count})";
			}
			if (GUILayout.Button(label)) {
				if (sh != null) {
					if (endIfSelected) {
						sh.Stop();
						Target.Shell = null;
						ShellGui.Shell = null;
					} else if (commandeer) {
						Target.Shell = sh;
						ShellGui.Shell = sh;
						RefreshInspectorInternal();
					}
				}
			}
		}
	}

	private bool waitingForCommand = false;
	private void RunInternalCommand(string command) {
		Target.StartCooperativeFunction(this, command, PopulateOutputText);
		waitingForCommand = !Target.IsFunctionFinished();
		PopulateOutputText();
	}

	private void PopulateOutputText(string latestLine) {
		PopulateOutputText();
	}

	private void PopulateOutputText() {
		_lines.Clear();
		if (ShellGui != null && ShellGui.Shell != null) {
			ShellGui.Shell.GetRecentLines(_lines);
		}
		_lastRuntime = string.Join("\n", _lines);
		RefreshInspector();
	}

	private void RunCommands() {
		Target.RunCommands(_shellGui, StdOutput);
		RefreshInspector();
	}

	private void StdOutput(string line) {
		PopulateOutputText();
	}
}
