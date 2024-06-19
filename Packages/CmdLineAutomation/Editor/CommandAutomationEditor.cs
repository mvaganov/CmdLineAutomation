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

		private string _currentLine = "";
		private bool _showOutput = true;
		private bool _isWaitingForTrigger = false;

		private object _context;

		// TODO move this to another class, and trigger it in CommandAutomation
		/// TODO regular expressions that will turn on and turn off the output (to limit spam from something like logcat)
		/// TODO create command that ignores output until a specific regex is found.
		/// TODO create command that ignores output when a specific regex is found.
		/// TODO create command that stops ignoring output
		/// TODO create command that clears triggers that would ignore output
		private enum RegexGroupId { None, DisableOnRead, EnableOnRead }
		private List<NamedRegexSearch>[] regexGroup = new List<NamedRegexSearch>[0];
		private List<RegexGroupId> _triggeredGroup = new List<RegexGroupId>();

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

		public bool ShowOutput {
			get => _showOutput;
			set => _showOutput = value;
		}

		public bool IsStarted => Shell != null;

		private bool IsWaitingForTrigger() {
			for (int groupId = 0; groupId < regexGroup.Length; ++groupId) {
				List<NamedRegexSearch> regexList = regexGroup[groupId];
				for (int regexIndex = 0; regexIndex < regexList.Count; ++regexIndex) {
					if (!regexList[regexIndex].Ignore) {
						return _isWaitingForTrigger = true;
					}
				}
			}
			return _isWaitingForTrigger = false;
		}

		// TODO test this
		public void AddHideTrigger(string trigger) => regexGroup[(int)RegexGroupId.DisableOnRead].Add(trigger);

		public void AddShowTrigger(string trigger) => regexGroup[(int)RegexGroupId.EnableOnRead].Add(trigger);

		public bool RemoveHideTrigger(string trigger) => Remove(regexGroup[(int)RegexGroupId.DisableOnRead], trigger);

		public bool RemoveShowTrigger(string trigger) => Remove(regexGroup[(int)RegexGroupId.EnableOnRead], trigger);

		private bool Remove(List<NamedRegexSearch> regexSearchList, string trigger) {
			int index = regexSearchList.FindIndex(namedSearch => namedSearch.RegexString == trigger);
			if (index >= 0) {
				regexSearchList.RemoveAt(index);
				return true;
			}
			return false;
		}

		private void ClearTriggers() {
			for (int i = 0; i < regexGroup.Length; ++i) {
				regexGroup[i].Clear();
			}
			_isWaitingForTrigger = false;
		}

		private int CountRegexTriggerGroups(string line, List<RegexGroupId> triggeredGroup) {
			int count = 0;
			for (int groupIndex = 0; groupIndex < regexGroup.Length; groupIndex++) {
				List<NamedRegexSearch> group = regexGroup[groupIndex];
				for (int i = 0; i < group.Count; ++i) {
					NamedRegexSearch regexSearch = group[i];
					if (regexSearch.Process(line) != null) {
						triggeredGroup?.Add((RegexGroupId)groupIndex);
						++count;
						break;
					}
				}
			}
			return count;
		}

		private void OnEnable() {
			if (Shell != null) {
				EditorApplication.delayCall += RefreshInspector;
			}
			_context = Target;
			IsWaitingForTrigger();
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
			Target.StartCooperativeFunction(_context, command, Print);
			waitingForCommand = !Target.IsExecutionFinished(_context);
			RefreshInspector();
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
			Target.ClearOutput(this);
			RefreshInspector();
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
					label = $"{action} {runningMark} \"{sh.Name}\"";// (lines {sh.LineCount})";
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
			Target.RunCommands(_context, Print);
			RefreshInspector();
		}

		public void Print(string text) {
			if (Shell == null) {
				OperatingSystemCommandShell shell = Target.GetShell(_context);
				if (shell != null) {
					PopulateShell(shell);
				}
			}
			//Debug.Log("PRINT: "+line);
			if (_isWaitingForTrigger) {
				ProcessAndCheckTextForTriggeringLines(text);
			} else {
				Target.AddToCommandOutput(text);// + "\n";
			}
			RefreshInspector();
		}

		private void ProcessAndCheckTextForTriggeringLines(string newTextToCheck) {
			_currentLine += newTextToCheck;
			int firstNewlineIndex;
			do {
				firstNewlineIndex = _currentLine.IndexOf('\n');
				if (firstNewlineIndex >= 0) {
					++firstNewlineIndex;
					string checkedLine = _currentLine.Substring(0, firstNewlineIndex);
					_triggeredGroup.Clear();
					if (CountRegexTriggerGroups(checkedLine, _triggeredGroup) > 0) {
						_triggeredGroup.ForEach(ActivateRegexTriggeredEffect);
					}
					if (ShowOutput) {
						Target.CommandOutput += checkedLine;
					}
					_currentLine = _currentLine.Substring(firstNewlineIndex);
				}
			} while (firstNewlineIndex >= 0);
		}

		private void ActivateRegexTriggeredEffect(RegexGroupId groupId) {
			switch (groupId) {
				case RegexGroupId.DisableOnRead:
					ShowOutput = false;
					break;
				case RegexGroupId.EnableOnRead:
					ShowOutput = true;
					break;
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
			string prompt = Shell.WorkingDirectory;
			GUILayout.Label(prompt, style, GUILayout.ExpandWidth(false));
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
