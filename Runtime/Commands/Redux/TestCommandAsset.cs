using UnityEditor;
using UnityEngine;

namespace RunCmdRedux {
	/// <summary>
	/// TODO: test one command at a time. all of them at once in a filter and a dictionary is a bit much.
	/// </summary>
	[CreateAssetMenu(fileName = "test", menuName = "ScriptableObjects/CommandAsset/_test")]
	public class TestCommandAsset : ScriptableObject {
		public Object commandAsset;
		private ICommandAsset _commandAsset;
		public string commandInput;
		private float _progress;
		private string commandOutput;
		// TODO replace _executor with a more basic executor.
		[SerializeField] protected TestExecutor _executor = new TestExecutor();

		public TestExecutor Executor => _executor;
		public bool IsExecuting => _executor != null && _executor.IsExecuting;
		public string CurrentCommandInput {
			get => commandInput;
			set => commandInput = value;
		} 

		public string CommandOutput {
			get => commandOutput;
			set => commandOutput = value;
		}

		public float Progress {
			get => _progress;
			set => _progress = value;
		}

		public ICommandAsset ReferencedAsset => _commandAsset != null ? _commandAsset : _commandAsset = commandAsset as ICommandAsset;

		public ICommandProcess ReferencedProcess => ReferencedAsset != null ? ReferencedAsset.GetCommand(this) : null;

		public bool UpdateExecution() {
			float progress = IsExecuting ? Progress : 1;
			bool waitingForCommandToFinish = progress < 1;
			//waitingForCommandToFinish = !Target.IsExecutionFinished(_context);
			if (waitingForCommandToFinish) {
				//Debug.Log($"PROGRESSBAR {progress}");
				bool stop = ComponentProgressBar.DisplayCancelableProgressBar(name, Executor.CurrentCommandText, progress);
				if (stop) {
					Debug.Log("CANCEL");
					CancelProcess(this);
					ComponentProgressBar.ClearProgressBar();
				}
			} else if (ComponentProgressBar.IsProgressBarVisible) {
				ComponentProgressBar.ClearProgressBar();
			}
			return waitingForCommandToFinish;
		}

		public void CancelProcess(object context) {
			Debug.Log($"({context}) canceling [{_executor.ReferencedCommand}]");
			_executor.CancelProcess(context);
		}
		public void ExecuteCurrentCommand() {
			//_executor._settings = _settings;
			//_executor.currentCommandText = _commandInput;
			//_executor.source = this;
			ICommandProcess proc = ReferencedProcess;
			Debug.Log($"proc: [{proc}]");
			_executor.ReferencedCommand = proc;
			_executor.CurrentCommandText = commandInput;
			_executor.ExecuteCurrentCommand();
		}
	}

	[CustomEditor(typeof(TestCommandAsset))]
	[CanEditMultipleObjects]
	public class AutomationAssetEditor : Editor {
		/// <summary>
		/// The Automation being edited
		/// </summary>
		private TestCommandAsset _target;
		/// <summary>
		/// How to draw the console, including font, bg and fg text colors, border, etc.
		/// </summary>
		private GUIStyle _consoleTextStyle = null;
		/// <summary>
		/// Semaphore prevents new command from running
		/// </summary>
		private bool waitingForCommandToFinish = false;
		private object _context;

		public TestCommandAsset Target => _target != null ? _target
			: _target = target as TestCommandAsset;

		public ICommandProcess ReferencedProcess => Target.ReferencedProcess;

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
		}

		public void ExecuteCommand(string command) {
			Target.CurrentCommandInput = command;
			// TODO make sure this command isn't marked as being a member of the command list. this should not advance the list.
			Target.ExecuteCurrentCommand();
		}

		public string PromptGUI(GUIStyle style) {
			GUILayout.BeginHorizontal();
			GUILayout.Label(">", GUILayout.ExpandWidth(false));
			try {
				Target.commandInput = GUILayout.TextField(Target.commandInput, style, GUILayout.ExpandWidth(true));
			} catch { }
			GUILayout.EndHorizontal();
			Event e = Event.current;
			if (Target.commandInput != "" && e.type == EventType.KeyUp && e.keyCode == KeyCode.Return) {
				string result = Target.commandInput;
				Target.commandInput = "";
				return result;
			}
			return null;
		}

		private void GuiLine(int i_height = 1, Color color = default) {
			Rect rect = EditorGUILayout.GetControlRect(false, i_height);
			rect.height = i_height;
			EditorGUI.DrawRect(rect, color);
		}

		public void RefreshInspector() {
			EditorApplication.delayCall += RefreshInspectorInternal;
		}

		private void RefreshInspectorInternal() {
			EditorUtility.SetDirty(Target);
		}

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
			string info = Target.Executor.CurrentCommandText;// Target.CurrentCommandText(_context);
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

		private void RunCommands() {
			Debug.Log("TODO " + Target.Executor);
			//Target.UseParsedCommands();
			// TODO feed the list of commands to the Executor/_shell?

			//CommandLineExecutor executor = Target.GetCommandExecutor();
			//executor.CommandsToDo = Target.CommandsToDo;
			//executor.RunCommands(_context, Print);

			Target.Executor.commandText = Target.commandInput;
			Target.ExecuteCurrentCommand();

			RefreshInspector();
		}

		private void ClearOutputButtonGUI() {
			if (!GUILayout.Button("Clear Output")) {
				return;
			}
			Target.CommandOutput = "";// ClearOutput(this);
			RefreshInspector();
		}
	}
}
