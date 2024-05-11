using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace RunCmd {
	[CustomEditor(typeof(CmdLineAutomationScriptableObject))]
	[CanEditMultipleObjects]
	public class CmdLineAutomationScriptableObjectEditor : Editor, IReferencesOperatingSystemCommandShell {
		private CmdLineAutomationScriptableObject _target;
		private List<string> _lines = new List<string>();
		private string _lastRuntime;
		/// <summary>
		/// how to draw the console, including font, bg and fg text colors, border, etc.
		/// </summary>
		private GUIStyle _consoleTextStyle = null;
		/// <summary>
		/// value that the user is typing into the inspector
		/// </summary>
		private string _inspectorCcommandInput = "";
		/// <summary>
		/// Semaphore prevents new command from running
		/// </summary>
		private bool waitingForCommand = false;

		public CmdLineAutomationScriptableObject Target => _target != null ? _target
			: _target = target as CmdLineAutomationScriptableObject;

		public OperatingSystemCommandShell Shell {
			get => Target.Shell;
			set => Target.Shell = value;
		}

		public bool IsStarted => Shell != null;

		private void OnEnable() {
			StartShell();
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
			string command = PromptGUI(_consoleTextStyle);
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
				if (IsStarted) {
					RunCommands();
				} else {
					StartShell();
					EditorApplication.delayCall += RunCommands;
				}
			}
			ButtonGUI(_consoleTextStyle);
			if (GUILayout.Button("Clear Output")) {
				if (Shell != null) {
					Shell.ClearLines();
				}
				PopulateOutputText();
			}
			GUILayout.EndHorizontal();
			EditorGUILayout.TextArea(_lastRuntime, _consoleTextStyle);
			serializedObject.Update();
			serializedObject.ApplyModifiedProperties();

			for (int i = 0; i < OperatingSystemCommandShell.RunningShells.Count; i++) {
				OperatingSystemCommandShell sh = OperatingSystemCommandShell.RunningShells[i];
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
						} else if (commandeer) {
							Target.Shell = sh;
							RefreshInspectorInternal();
						}
					}
				}
			}
		}

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
			if (Shell != null) {
				Shell.GetRecentLines(_lines);
			}
			_lastRuntime = string.Join("\n", _lines);
			RefreshInspector();
		}

		private void RunCommands() {
			StartShell();
			Target.RunCommands(Target, StdOutput);
			RefreshInspector();
		}

		private void StdOutput(string line) {
			PopulateOutputText();
		}

		public void StartShell() {
			if (Target.Shell == null) {
				Target.Initialize();
				Debug.Log("Creating shell " + Shell);
			}
			PopulateOutputText();
		}

		public void Stop() {
			if (IsStarted) {
				Shell.Stop();
			}
			Shell = null;
		}

		/// <summary>
		/// <see cref="OnGUI"/> or <see cref="UnityEngine.Editor.OnInspectorGUI"/>
		/// </summary>
		public void ButtonGUI(GUIStyle style) {
			if (Shell == null) {
				if (GUILayout.Button("Start Process")) {
					StartShell();
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
			GUILayout.Label(Shell.GetCurrentLine(), style, GUILayout.ExpandWidth(false));
			_inspectorCcommandInput = GUILayout.TextField(_inspectorCcommandInput, style, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal();
			Event e = Event.current;
			if (_inspectorCcommandInput != "" && e.type == EventType.KeyUp && e.keyCode == KeyCode.Return) {
				string result = _inspectorCcommandInput;
				_inspectorCcommandInput = "";
				return result;
			}
			return null;
		}
	}
}
