using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using RunCmdRedux;

namespace RunCmd {
	/// <summary>
	/// Editor for lists of <see cref="CommandLineAutomation"/>s, commands to execute.
	/// </summary>
	[CustomEditor(typeof(CommandLineAutomation))]
	[CanEditMultipleObjects]
	public class CommandLineAutomationEditor : Editor, ICommandAutomation, ICommandReference {
		/// <summary>
		/// The Automation being edited
		/// </summary>
		private CommandLineAutomation _target;
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
		private bool waitingForCommandToFinish = false;
		/// <summary>
		/// References an optional shell that exists for this command automation
		/// </summary>
		private OperatingSystemCommandShell _shell;

		private object _context;

		public ICommandExecutor CommandExecutor => Target;

		public CommandLineAutomation Target => _target != null ? _target
			: _target = target as CommandLineAutomation;

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

		public ICommandProcessor ReferencedCommand => Target.ReferencedCommand;

		private void OnEnable() {
			if (Shell != null) {
				EditorApplication.delayCall += RefreshInspector;
			}
			_context = Target;
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
			//waitingForCommandToFinish = !Target.IsExecutionFinished(_context);
			//if (ComponentProgressBar.IsProgressBarVisible && Target.Progress(_context) >= 1) {
			//	ComponentProgressBar.ClearProgressBar();
			//}
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
			waitingForCommandToFinish = !Target.IsExecutionFinished(_context);
			if (waitingForCommandToFinish) {
				Debug.Log($"waiting for command to finish before '{command}' can execute...\n" +
					$"({Target.IsExecutionFinished(_context)}) \"{Target.GetCurrentCommand(_context)}\"");
			} else {
				Debug.Log($"running command {command}");
				RunInternalCommand(command);
				RefreshInspector();
			}
		}

		private void RunInternalCommand(string command) {
			Target.StartCooperativeFunction(_context, command, Print);
			waitingForCommandToFinish = !Target.IsExecutionFinished(_context);
			//Target.CurrentCommand
			RefreshInspector();
		}

		private void RunCommandsButtonGUI() {
			float commandProgress = Target.Progress(_context);
			if (commandProgress <= 0) {
				ComponentProgressBar.ClearProgressBar();
				if (GUILayout.Button("Run Commands To Do")) {
					RunCommands();
				}
			} else {
				ICommandProcessor commandProcessor = Target.GetCurrentCommand(_context);
				if (commandProcessor != null) {
					if (commandProcessor.IsExecutionFinished(_context)) {
						ComponentProgressBar.ClearProgressBar();
					} else {
						HandleProgressBar(commandProgress);
					}
				} else {
					AbortButton(false);
				}
			}
		}

		private void HandleProgressBar(float commandProgress) {
			string title = Target.name;
			string info = Target.CurrentCommandText(_context);
			bool stop = ComponentProgressBar.DisplayCancelableProgressBar(title, info, commandProgress);
			AbortButton(stop);
			RefreshInspectorInternal();
		}

		private void AbortButton(bool abort) {
			if (GUILayout.Button("Abort Commands") || abort) {
				Target.CancelProcess(_context);
				ComponentProgressBar.ClearProgressBar();
				waitingForCommandToFinish = false;
			}
		}

		private void ClearOutputButtonGUI() {
			if (!GUILayout.Button("Clear Output")) {
				return;
			}
			Target.CommandOutput = "";// ClearOutput(this);
			RefreshInspector();
		}

		private enum EffectOfShellButton { None, EndProcess, ShowProcess }
		private void ShowAllCommandAutomationsListingGUI() {
			foreach (var kvp in OperatingSystemCommandShell.RunningShells) {
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
					label = $"{action} {runningMark} \"{sh.Name}\"";// (lines {sh.LineCount})";
				}
				if (GUILayout.Button(label)) {
					CommandDelay.DelayCall(() => {
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

		private void StopShell(OperatingSystemCommandShell sh) {
			sh.Stop();
			Shell = null;
			RefreshInspectorInternal();
		}

		private void PopulateShell(OperatingSystemCommandShell sh) {
			Shell = sh;
			RefreshInspector();
		}

		private void RunCommands() {
			CommandLineExecutor executor = Target.GetCommandExecutor();
			executor.CommandsToDo = Target.CommandsToDo;
			executor.RunCommands(_context, Print);
			RefreshInspector();
		}

		public void Print(string text) {
			if (Shell == null) {
				OperatingSystemCommandShell shell = Target.GetShell(_context);
				if (shell != null) {
					PopulateShell(shell);
				}
			}
			RefreshInspector();
		}

		public void Stop() {
			if (IsStarted) {
				Shell.Stop();
			}
			Shell = null;
		}

		public string PromptGUI(GUIStyle style) {
			//if (!IsStarted) {
			//	GUILayout.BeginHorizontal();
			//	GUILayout.Label("<no shell>");
			//	GUILayout.EndHorizontal();
			//	return null;
			//}
			GUILayout.BeginHorizontal();
			if (IsStarted) {
				string prompt = Shell.WorkingDirectory;
				GUILayout.Label(prompt, style, GUILayout.ExpandWidth(false));
			} else {
				GUILayout.Label("<no shell>", GUILayout.ExpandWidth(false));
			}
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
