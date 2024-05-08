using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a command line shell for any Unity UI that supports OnGUI
/// </summary>
public class ImguiInteractiveShell : IReferencesCmdShell {
	private InteractiveCmdShell _shell;
	private string cmd = "";
	private List<string> lineBuffer = new List<string>();
	private Func<InteractiveCmdShell> CreateShell;

	public bool IsStarted => _shell != null;

	public ImguiInteractiveShell(Func<InteractiveCmdShell> createShell) {
		CreateShell = createShell;
	}

	public void Start() {
		if (_shell == null) {
			_shell = CreateShell();
		}
	}

	public void Stop() {
		if (IsStarted) {
			Shell.Stop();
		}
		_shell = null;
	}

	public InteractiveCmdShell Shell => _shell;

	public string CommandToken => throw new NotImplementedException();

	/// <summary>
	/// <see cref="OnGUI"/> or <see cref="UnityEngine.Editor.OnInspectorGUI"/>
	/// </summary>
	public void ButtonGUI(GUIStyle style) {
		if (_shell == null) {
			if (GUILayout.Button("Start Process")) {
				if (_shell == null)
					Start();
			}
			return;
		}
		if (GUILayout.Button("Stop Process")) {
			Stop();
		}
	}

	public string PromptGUI(GUIStyle style) {
		if (!IsStarted) {
			return null;
		}
		GUILayout.BeginHorizontal();
		Shell.GetRecentLines(lineBuffer);
		GUILayout.Label(Shell.GetCurrentLine(), style, GUILayout.ExpandWidth(false));
		cmd = GUILayout.TextField(cmd, style, GUILayout.ExpandWidth(true));
		GUILayout.EndHorizontal();
		Event e = Event.current;
		if (cmd != "" && e.type == EventType.KeyUp && e.keyCode == KeyCode.Return) {
			string result = cmd;
			cmd = "";
			return result;
		}
		return null;
	}
}
