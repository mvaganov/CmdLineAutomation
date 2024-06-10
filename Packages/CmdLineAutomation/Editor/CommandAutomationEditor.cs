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
		/// </summary>
		private List<string> _osLines = new List<string>();
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

		private object _context;

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
			if (Shell != null) {
				EditorApplication.delayCall += RefreshInspector;
			}
			_context = Target;
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
			ClearOutputButtonGUI();
			GUILayout.EndHorizontal();
			EditorGUILayout.TextArea(Target.CommandOutput, _consoleTextStyle);
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
			Target.StartCooperativeFunction(_context, command, PopulateOutputText);
			waitingForCommand = !Target.IsExecutionFinished(_context);
			PopulateOutputText();
		}

		private void PopulateOutputText(string latestLine) {
			PopulateOutputText();
		}

		// TODO this should be in CommandAutomation...
		private void PopulateOutputText() {
			if (Shell != null) {
				_osLines.Clear();
				Shell.GetRecentLines(_osLines, true);
				// TODO decide which lines should be removed, because they are considered spam, and remove those lines
				if (_osLines.Count > 0) {
					Target.CommandOutput += (string.IsNullOrEmpty(Target.CommandOutput) ? "" : "\n") +
						string.Join("\n", _osLines);
				}
			}
		}

		private void RunCommandsButtonGUI() {
			float commandProgress = Target.Progress(_context);
			if (commandProgress <= 0) {
				EditorUtility.ClearProgressBar();
				if (GUILayout.Button("Run Commands To Do"))
				{
					RunCommands();
				}
			} else {
				if (Target.CurrentCommand(_context) != null) {
					HandleProgressBar(commandProgress);
				} else {
					AbortButton(false);
				}
			}
		}

		private void HandleProgressBar(float commandProgress) {
			string title = Target.name;
			string info = Target.CurrentCommandText(_context);
			bool stop = EditorUtility.DisplayCancelableProgressBar(title, info, commandProgress);
			AbortButton(stop);
			RefreshInspectorInternal();
		}

		private void AbortButton(bool abort) {
			if (GUILayout.Button("Abort Commands") || abort) {
				Target.CancelProcess(_context);
				EditorUtility.ClearProgressBar();
			}
		}

		private void ClearOutputButtonGUI() {
			if (!GUILayout.Button("Clear Output")) {
				return;
			}
			Target.CommandOutput = "";
			if (Shell != null) {
				Shell.ClearLines();
			} else {
				_osLines.Clear();
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
					CommandAutomation.DelayCall(() => {
						ShellButton(sh, effect);
					});
				}
			}
		}

		private void ShellButton(OperatingSystemCommandShell sh, EffectOfShellButton effect) {
			if (sh == null) {
				return;
			}
			switch (effect) {
				case EffectOfShellButton.EndProcess: StopShell(sh); break;
				case EffectOfShellButton.ShowProcess: PopulateShell(sh); break;
			}
		}

		private void StopShell(OperatingSystemCommandShell sh)
		{
			sh.Stop();
			Shell = null;
			RefreshInspectorInternal();
		}
		
		private void PopulateShell(OperatingSystemCommandShell sh) {
			Shell = sh;
			RefreshInspector();
		}

		private void RunCommands() {
			Target.RunCommands(_context, StdOutput);
			RefreshInspector();
		}

		private void StdOutput(string line) {
			//Debug.Log(line);
			if (Shell == null) {
				OperatingSystemCommandShell shell = Target.GetShell(_context);
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

		public string PromptGUI(GUIStyle style) {
			if (!IsStarted) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("<no shell>");
				GUILayout.EndHorizontal();
				return null;
			}
			GUILayout.BeginHorizontal();
			GUILayout.Label(Shell.GetCurrentLine(), style, GUILayout.ExpandWidth(false));
			try {
				_inspectorCommandInput = GUILayout.TextField(_inspectorCommandInput, style, GUILayout.ExpandWidth(true));
			} catch { }
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
