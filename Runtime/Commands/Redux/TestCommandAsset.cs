using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace RunCmdRedux {
	/// <summary>
	/// TODO: test one command at a time. all of them at once in a filter and a dictionary is a bit much.
	/// </summary>
	[CreateAssetMenu(fileName = "test", menuName = "ScriptableObjects/CommandAsset/_test")]
	public class TestCommandAsset : ScriptableObject {
		[SerializeField] protected Object commandAsset;
		private ICommandAsset _commandAsset;
		[SerializeField] protected TestExecutor _executor = new TestExecutor();
		[SerializeField] protected string _command;

		public TestExecutor Executor => _executor;
		public bool IsExecuting => _executor != null && _executor.IsExecuting;
		public string CurrentCommandInput {
			get => _command;
			set => _command = value;
		} 

		public string CommandInput {
			get => _command;
			set => _command = value;
		}

		public string CommandOutput {
			get => _executor.CurrentOutput;
			set => _executor.CurrentOutput = value;
		}

		public ICommandAsset ReferencedAsset => _commandAsset != null ? _commandAsset : _commandAsset = commandAsset as ICommandAsset;

		public ICommandProcess ReferencedProcess => ReferencedAsset != null ?
			ReferencedAsset.GetCommandCreateIfMissing(this) : null;

		public ICommandProcess CurrentCommand(object context) => ReferencedAsset.GetCommandIfCreated(context);

		public float Progress(object context) {
			ICommandProcess proc = CurrentCommand(context);
			if (proc == null) { return 0; }
			return proc.GetProgress();
		}

		public bool UpdateExecution(object context) {
			float progress = IsExecuting ? Progress(context) : 1;
			bool waitingForCommandToFinish = progress < 1;
			//waitingForCommandToFinish = !Target.IsExecutionFinished(_context);
			if (waitingForCommandToFinish) {
				//Debug.Log($"PROGRESSBAR {progress}");
				bool stop = ComponentProgressBar.DisplayCancelableProgressBar(name, Executor.CurrentInput, progress);
				if (stop) {
					//Debug.Log("CANCEL");
					CancelProcess(context);
					ClearProgressBar();
				}
			} else if (ComponentProgressBar.IsProgressBarVisible && progress >= 1) {
				//ClearProgressBar();
			}
			return waitingForCommandToFinish;
		}

		public void CancelProcess(object context) {
			Debug.Log($"canceling [{_executor.ReferencedCommand}] ({context})");
			ICommandProcess proc = CurrentCommand(context);
			if (proc != _executor.ReferencedCommand) {
				throw new System.Exception($"unexpected process to cancel. expected to cancel {_executor.ReferencedCommand}, found {proc}");
			}
			StopProcess(context, proc);
		}
		public void StopProcess(object context, ICommandProcess proc) {
			if (!ReferencedAsset.RemoveCommand(context, proc)) {
				throw new System.Exception("NO SUCH PROCESS");
			}
			_executor.ReferencedCommand = null;
		}
		public void ExecuteCurrentCommand() {
			_executor.ReferencedCommand = ReferencedProcess;
			_executor.CurrentInput = _command;
			_executor.ExecuteCurrentCommand();
		}

		[ContextMenu(nameof(ClearProgressBar))]
		public void ClearProgressBar() {
			ComponentProgressBar.ClearProgressBar();
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
		private object Context => Target;

		public TestCommandAsset Target => _target != null ? _target
			: _target = target as TestCommandAsset;

		public ICommandProcess ReferencedProcess => Target.ReferencedProcess;

		public override void OnInspectorGUI() {
			CreateTextStyle();
			DrawDefaultInspector();
			//InputPromptGUI();
			waitingForCommandToFinish = Target.UpdateExecution(Context);
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
				Target.CommandInput = GUILayout.TextField(Target.CommandInput, style, GUILayout.ExpandWidth(true));
			} catch { }
			GUILayout.EndHorizontal();
			Event e = Event.current;
			if (Target.CommandInput != "" && e.type == EventType.KeyUp && e.keyCode == KeyCode.Return) {
				string result = Target.CommandInput;
				Target.CommandInput = "";
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
			ICommandProcess commandProcessor = Target.CurrentCommand(Context);
			if (commandProcessor == null || commandProcessor.GetProgress() <= 0) {
				//ComponentProgressBar.ClearProgressBar();
				if (GUILayout.Button("Run Command")) {
					RunCommand();
				}
			} else {
				AbortButton();
				if (commandProcessor != null) {
					if (commandProcessor.IsExecutionFinished) {
						Target.ClearProgressBar();
						Target.StopProcess(Context, commandProcessor);
						//AbortButton();
					} else {
						HandleProgressBar(commandProcessor.GetProgress());
					}
				//} else {
				//	AbortButton();
				}
			}
		}

		private void HandleProgressBar(float commandProgress) {
			string title = Target.name;
			string info = Target.Executor.CurrentInput;
			bool stop = ComponentProgressBar.DisplayCancelableProgressBar(title, info, commandProgress);
			if (stop) {
				//Debug.Log("CANCEL ON POPUP");
				DoAbort();
			//} else {
			//	AbortButton(stop);
			}
			RefreshInspectorInternal();
		}

		private void AbortButton() {
			// forceAbort is checked last to ensure the Abort button draws
			if (GUILayout.Button("Abort Commands")) {
				DoAbort();
			}
		}

		private void DoAbort() {
			Target.CancelProcess(Context);
			waitingForCommandToFinish = false;
			EditorApplication.delayCall += ClearProgressBarAgain;
			void ClearProgressBarAgain(){
				Target.ClearProgressBar();
			}
		}

		private void RunCommand() {
			Target.Executor.CurrentInput = Target.CommandInput;
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
