using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Manages a command line shell for any Unity UI that supports OnGUI
/// </summary>
public class ImguiInteractiveShell : ICmd {
	private InteractiveCmdShell _shell;
	private string cmd = "";
	List<string> lineBuffer = new List<string>();
	public Action OnLineRead = delegate { };

	public bool IsStarted => _shell != null;

	public void Start() {
		_shell = new InteractiveCmdShell("cmd.exe", Path.Combine(Application.dataPath, ".."));
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
				Start();
			}
			return _shell;
		}
	}

	public string Token => throw new NotImplementedException();

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
		shell.GetRecentLines(lineBuffer);
		GUILayout.Label(shell.GetCurrentLine(), style, GUILayout.ExpandWidth(false));
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

	public void Execute(string command) {
		shell.RunCommand(command);
	}

	/// <inheritdoc/>
	public string CommandFilter(string command, Action<string> stdOutput) {
		if (!IsStarted) {
			Start();
		}
		return _shell.CommandFilter(command, stdOutput);
	}
}
