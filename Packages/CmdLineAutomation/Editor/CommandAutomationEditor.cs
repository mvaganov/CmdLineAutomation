using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RunCmd {
	/// <summary>
	/// Editor for lists of <see cref="CommandAutomation"/>s, commands to execute.
	/// </summary>
	[CustomEditor(typeof(CommandAutomation))]
	[CanEditMultipleObjects]
	public class CommandAutomationEditor : Editor {
		/// <summary>
		/// The Automation being edited
		/// </summary>
		private CommandAutomation _target;
		/// <summary>
		/// Results of the commandline running in the operating system
		/// TODO keep all stdoutput from commands, not just lines from the OperatingSystemCommandShell
		/// </summary>
		private List<string> _osLines = new List<string>();
		/// <summary>
		/// Command being typed into the command prompt by the Unity Editor user
		/// </summary>
		private string _inspectorCommandOutput;
		/// <summary>
		/// How to draw the console, including font, bg and fg text colors, border, etc.
		/// </summary>
		private GUIStyle _consoleTextStyle = null;
		/// <summary>
		/// Value that the user is typing into the inspector
		/// </summary>
		private string _inspectorCommandInput = "";
		/// <summary>
		/// Semaphore prevents new command from running
		/// </summary>
		private bool waitingForCommand = false;
		/// <summary>
		/// References an optional shell that exists for this command automation
		/// </summary>
		private OperatingSystemCommandShell _shell;

		public CommandAutomation Target => _target != null ? _target
			: _target = target as CommandAutomation;

		public OperatingSystemCommandShell Shell {
			get {
				if (_shell == null || !_shell.IsRunning) {
					OperatingSystemCommandShell.RunningShells.TryGetValue(this, out _shell);
				}
				return _shell;
			}
			set => _shell = value;
		}

		public bool IsStarted => Shell != null;

		private void OnEnable() {
			//StartShell();
			if (Shell != null) {
				EditorApplication.delayCall += RefreshInspector;
			}
		}

		public void RefreshInspector() {
			PopulateOutputText();
			EditorApplication.delayCall += RefreshInspectorInternal;
		}

		private void RefreshInspectorInternal() {
			EditorUtility.SetDirty(Target);
		}

		/// <inheritdoc/>
		public override void OnInspectorGUI() {
			CreateTextStyle();
			DrawDefaultInspector();
			InputPromptGUI();
			GUILayout.BeginHorizontal();
			RunCommandsButtonGUI();
			//StartStopButtonGUI();
			ClearOutputButtonGUI();
			GUILayout.EndHorizontal();
			EditorGUILayout.TextArea(_inspectorCommandOutput, _consoleTextStyle);
			ShowAllCommandAutomationsListingGUI();
			serializedObject.Update();
			serializedObject.ApplyModifiedProperties();
		}

		private void CreateTextStyle() {
			if (_consoleTextStyle != null) {
				return;
			}
			_consoleTextStyle = new GUIStyle("label");
			_consoleTextStyle.wordWrap = false;
			_consoleTextStyle.font = Font.CreateDynamicFontFromOSFont("Consolas", 12);
		}

		private void InputPromptGUI() {
			string command = PromptGUI(_consoleTextStyle);
			if (command == null) {
				return;
			}
			if (waitingForCommand) {
				Debug.Log("waiting for command to finish...");
			} else {
				RunInternalCommand(command);
				RefreshInspector();
			}
		}

		private void RunInternalCommand(string command) {
			Target.StartCooperativeFunction(Target, command, PopulateOutputText);
			waitingForCommand = !Target.IsExecutionFinished(Target);
			PopulateOutputText();
		}

		private void PopulateOutputText(string latestLine) {
			PopulateOutputText();
		}

		private void PopulateOutputText() {
			if (Shell != null) {
				_osLines.Clear();
				_inspectorCommandOutput = "";
				Shell.GetRecentLines(_osLines);
			}
			_inspectorCommandOutput = string.Join("\n", _osLines);
		}

		private void RunCommandsButtonGUI() {
			if (!GUILayout.Button("Run Commands To Do")) {
				return;
			}
			RunCommands();
		}

		private void ClearOutputButtonGUI() {
			if (!GUILayout.Button("Clear Output")) {
				return;
			}
			if (Shell != null) {
				Shell.ClearLines();
			}
			PopulateOutputText();
		}

		private enum EffectOfShellButton { None, EndProcess, ShowProcess }
		private void ShowAllCommandAutomationsListingGUI() {
			foreach(var kvp in OperatingSystemCommandShell.RunningShells) {
				OperatingSystemCommandShell sh = kvp.Value;
				string label;
				EffectOfShellButton effect = Shell == sh
					? EffectOfShellButton.EndProcess : Shell == null
					? EffectOfShellButton.ShowProcess
					: EffectOfShellButton.None;
				if (sh == null) {
					label = "null";
				} else {
					string action = effect != EffectOfShellButton.None ? $"[{effect}]" : "";
					string runningMark = sh.IsRunning ? "" : "<dead> ";
					label = $"{action} {runningMark} \"{sh.Name}\" (lines {sh.LineCount})";
				}
				if (GUILayout.Button(label)) {
					ShellButton(sh, effect);
				}
			}
		}

		private void ShellButton(OperatingSystemCommandShell sh, EffectOfShellButton effect) {
			if (sh == null) {
				return;
			}
			switch (effect) {
				case EffectOfShellButton.EndProcess:
					sh.Stop();
					Shell = null;
					break;
				case EffectOfShellButton.ShowProcess:
					PopulateShell(sh);
					break;
			}
		}

		private void PopulateShell(OperatingSystemCommandShell sh) {
			Shell = sh;
			RefreshInspector();
		}

		private void RunCommands() {
			Target.RunCommands(Target, StdOutput);
			RefreshInspector();
		}

		private void StdOutput(string line) {
			//Debug.Log(line);
			if (Shell == null) {
				OperatingSystemCommandShell shell = Target.GetShell(Target);
				if (shell != null) {
					PopulateShell(shell);
				}
			}
			if (Shell != null) {
				RefreshInspector();
			}
		}

		public void Stop() {
			if (IsStarted) {
				Shell.Stop();
			}
			Shell = null;
		}

		//public void StartStopButtonGUI() {
		//	if (Shell == null) {
		//		if (GUILayout.Button("Start Process")) {
		//			StartShell();
		//		}
		//		return;
		//	}
		//	if (GUILayout.Button("Stop Process")) {
		//		Stop();
		//	}
		//}

		public string PromptGUI(GUIStyle style) {
			if (!IsStarted) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("<no shell>");
				GUILayout.EndHorizontal();
				return null;
			}
			GUILayout.BeginHorizontal();
			GUILayout.Label(Shell.GetCurrentLine(), style, GUILayout.ExpandWidth(false));
			_inspectorCommandInput = GUILayout.TextField(_inspectorCommandInput, style, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal();
			Event e = Event.current;
			if (_inspectorCommandInput != "" && e.type == EventType.KeyUp && e.keyCode == KeyCode.Return) {
				string result = _inspectorCommandInput;
				_inspectorCommandInput = "";
				return result;
			}
			return null;
		}
	}
}
