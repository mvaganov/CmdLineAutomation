using UnityEngine;
using UnityEditor;

namespace RunCmdRedux {
	/// <summary>
	/// Editor for lists of <see cref="AutomationAsset"/>s, commands to execute.
	/// </summary>
	[CustomEditor(typeof(AutomationAsset))]
	[CanEditMultipleObjects]
	public class AutomationAssetEditor : Editor, ICommandAssetAutomation, ICommandProcessReference {
		/// <summary>
		/// The Automation being edited
		/// </summary>
		private AutomationAsset _target;
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

		public ICommandAssetExecutor CommandExecutor => Target;

		public AutomationAsset Target => _target != null ? _target
			: _target = target as AutomationAsset;

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

		public ICommandProcess Process => Target.Process;

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
			waitingForCommandToFinish = Target.UpdateExecution();
			if (waitingForCommandToFinish) {
				RefreshInspector(); // prompt GUI to animate after a change
			}
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
			Color color = waitingForCommandToFinish ? Color.black : Color.gray;
			GuiLine(1, color);
			string command = PromptGUI(_consoleTextStyle);
			GuiLine(1, color);
			if (command == null) {
				return;
			}
			if (waitingForCommandToFinish) {
				Debug.Log($"waiting for command to finish before '{command}' can execute..." +
					$" working on\n  \"{Target.CurrentCommandInput}\"");
				return;
			}
			ExecuteCommand(command);
			RefreshInspector();
			//waitingForCommandToFinish = !Target.IsExecutionFinished(_context);
			//if (waitingForCommandToFinish) {
			//	Debug.Log($"waiting for command to finish before '{command}' can execute...\n" +
			//		$"({Target.IsExecutionFinished(_context)}) \"{Target.CurrentCommand(_context)}\"");
			//} else {
			//	Debug.Log($"running command {command}");
			//	RunInternalCommand(command);
			//	RefreshInspector();
			//}
		}

		private void GuiLine(int i_height = 1, Color color = default) {
			Rect rect = EditorGUILayout.GetControlRect(false, i_height);
			rect.height = i_height;
			EditorGUI.DrawRect(rect, color);
		}

		public void ExecuteCommand(string command) {
			Target.CurrentCommandInput = command;
			// TODO make sure this command isn't marked as being a member of the command list. this should not advance the list.
			Target.ExecuteCurrentCommand();
		}

		//private void RunInternalCommand(string command) {
		//	Target.StartCooperativeFunction(_context, command, Print);
		//	waitingForCommandToFinish = !Target.IsExecutionFinished(_context);
		//	//Target.CurrentCommand
		//	RefreshInspector();
		//}

		private void RunCommandsButtonGUI() {
			float commandProgress = 0;// Target.Progress(_context);
			if (commandProgress <= 0) {
				//ComponentProgressBar.ClearProgressBar();
				if (GUILayout.Button("Run Commands To Do")) {
					RunCommands();
				}
			} else {
				ICommandProcessor commandProcessor = null;// Target.CurrentCommand(_context);
				if (commandProcessor != null) {
					if (commandProcessor.IsExecutionFinished(_context)) {
						//ComponentProgressBar.ClearProgressBar();
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
			string info = Target.Executor.currentCommandText;// Target.CurrentCommandText(_context);
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
				case EffectOfShellButton.ShowProcess: SetShell(sh); break;
			}
		}

		private void StopShell(OperatingSystemCommandShell sh) {
			sh.Stop();
			Shell = null;
			RefreshInspectorInternal();
		}

		private void SetShell(OperatingSystemCommandShell sh) {
			Shell = sh;
			RefreshInspector();
		}

		private void RunCommands() {
			Debug.Log("TODO "+ Target.Executor);
			Target.UseParsedCommands();
			// TODO feed the list of commands to the Executor/_shell?

			//CommandLineExecutor executor = Target.GetCommandExecutor();
			//executor.CommandsToDo = Target.CommandsToDo;
			//executor.RunCommands(_context, Print);
			RefreshInspector();
		}

		public void Print(string text) {
			if (Shell == null) {
				OperatingSystemCommandShell shell = Target.Executor.Shell; //Target.GetShell(_context);
				if (shell != null) {
					SetShell(shell);
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

			if (IsStarted) {
				string prompt = Shell.WorkingDirectory;
				GUILayout.Label(prompt, style, GUILayout.ExpandWidth(false));
			} else {
			}
			GUILayout.BeginHorizontal();
			GUILayout.Label(">", GUILayout.ExpandWidth(false));
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
