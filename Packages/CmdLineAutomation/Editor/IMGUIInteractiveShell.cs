using System;
using System.Collections.Generic;
using UnityEngine;

public class ImguiInteractiveShell {
	private InteractiveCmdShell _shell;
	private string cmd = "";
	List<string> lineBuffer = new List<string>();
	public Action OnLineRead = delegate { };

	public bool IsStarted => _shell != null;

	public void Start() {
		_shell = new InteractiveCmdShell();
		_shell.OnLineRead = OnLineRead;
	}

	public void Stop() {
		if (IsStarted) {
			shell.Stop();
		}
		_shell = null;
	}

	public InteractiveCmdShell shell {
		get {
			if (_shell == null) {
				_shell = new InteractiveCmdShell();
			}
			return _shell;
		}
	}

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
		shell.GetRecentLines(lineBuffer);
		GUILayout.Label(shell.GetCurrentLine(), style, GUILayout.ExpandWidth(false));
		cmd = GUILayout.TextField(cmd, style, GUILayout.ExpandWidth(true));
		GUILayout.EndHorizontal();
		Event e = Event.current;
		if (cmd != "" && e.type == EventType.KeyUp && e.keyCode == KeyCode.Return) {
			shell.RunCommand(cmd);
			string executedComand = cmd;
			cmd = "";
			return executedComand;
		}
		return null;
	}
}
