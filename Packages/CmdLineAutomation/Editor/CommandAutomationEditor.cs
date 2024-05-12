using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RunCmd {
	/// <summary>
	/// Editor for lists of <see cref="CommandAutomation"/>s, commands to execute.
	/// </summary>
	[CustomEditor(typeof(CommandAutomation))]
	[CanEditMultipleObjects]
	public class CommandAutomationEditor : Editor, IReferencesOperatingSystemCommandShell {
		/// <summary>
		/// The Automation being edited
		/// </summary>
		private CommandAutomation _target;
		/// <summary>
		/// Results of the commandline running in the operating system
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

		public CommandAutomation Target => _target != null ? _target
			: _target = target as CommandAutomation;

		public OperatingSystemCommandShell Shell {
			get => Target.Shell;
			set => Target.Shell = value;
		}

		public bool IsStarted => Shell != null;

		private void OnEnable() {
			//StartShell();
			if (Shell != null) {
				EditorApplication.delayCall += RefreshInspector;
			}
		}

		public void RefreshInspector() {
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
			StartStopButtonGUI();
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
			Target.StartCooperativeFunction(this, command, PopulateOutputText);
			waitingForCommand = !Target.IsFunctionFinished();
			PopulateOutputText();
		}

		private void PopulateOutputText(string latestLine) {
			PopulateOutputText();
		}

		private void RunCommandsButtonGUI() {
			if (!GUILayout.Button("Run Commands To Do")) {
				return;
			}
			if (IsStarted) {
				RunCommands();
			} else {
				StartShell();
				EditorApplication.delayCall += RunCommands;
			}
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

		private enum EffectOfShellButton { None, End, Comandeer }
		private void ShowAllCommandAutomationsListingGUI() {
			for (int i = 0; i < OperatingSystemCommandShell.RunningShells.Count; i++) {
				OperatingSystemCommandShell sh = OperatingSystemCommandShell.RunningShells[i];
				string label;
				EffectOfShellButton effect = Target.Shell == sh
					? EffectOfShellButton.End : Target.Shell == null
					? EffectOfShellButton.Comandeer
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
				case EffectOfShellButton.End:
					sh.Stop();
					Target.Shell = null;
					break;
				case EffectOfShellButton.Comandeer:
					Target.Shell = sh;
					RefreshInspector();
					break;
			}
		}

		private void PopulateOutputText() {
			_osLines.Clear();
			_inspectorCommandOutput = "";
			if (Shell != null) {
				Shell.GetRecentLines(_osLines);
			}
			_inspectorCommandOutput = string.Join("\n", _osLines);
			RefreshInspector();
		}

		private void RunCommands() {
			StartShell();
			Target.RunCommands(Target, StdOutput);
			RefreshInspector();
		}

		private void StdOutput(string line) {
			//Debug.Log(line);
			PopulateOutputText();
			RefreshInspector();
		}

		public void StartShell() {
			if (Target.Shell == null) {
				Target.StdOutput = StdOutput;
				Target.Initialize();
			}
			PopulateOutputText();
			RefreshInspector();
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
		public void StartStopButtonGUI() {
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
