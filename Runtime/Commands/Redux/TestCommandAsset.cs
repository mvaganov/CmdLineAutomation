using UnityEditor;
using UnityEngine;

namespace RunCmdRedux {
	/// <summary>
	/// TODO: test one command at a time. all of them at once in a filter and a dictionary is a bit much.
	/// </summary>
	[CreateAssetMenu(fileName = "test", menuName = "ScriptableObjects/CommandAsset/_test")]
	public class TestCommandAsset : ScriptableObject {
		[Interface(nameof(_commandAsset))]
		[SerializeField] protected UnityEngine.Object commandAsset;
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

		private void OnValidate() {
			_commandAsset = null;
		}

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
				HandleProgressBar(context, progress);
			//} else if (ComponentProgressBar.IsProgressBarVisible && progress >= 1) {
				//ClearProgressBar();
			}
			return waitingForCommandToFinish;
		}

		internal bool HandleProgressBar(object context, float progress) {
			bool stop = ComponentProgressBar.DisplayCancelableProgressBar(name, Executor.CurrentInput, progress);
			if (stop) {
				//Debug.Log("CANCEL");
				DoAbort(context);
			}
			return stop;
		}

		internal void DoAbort(object context) {
			CancelProcess(context);
			ClearProgressBar();
			//EditorApplication.delayCall += ClearProgressBarAgain;
			//void ClearProgressBarAgain() {
			//	ClearProgressBar();
			//}
		}

		public void CancelProcess(object context) {
			Debug.Log($"canceling [{_executor.Process}] ({context})");
			ICommandProcess proc = CurrentCommand(context);
			if (proc != _executor.Process) {
				throw new System.Exception($"unexpected process to cancel. expected to cancel {_executor.Process}, found {proc}");
			}
			StopProcess(context, proc);
		}
		public void StopProcess(object context, ICommandProcess proc) {
			if (proc != _executor.Process) {
				Debug.LogWarning($"{_executor} no longer processing {proc}, which is being told to stop");
			}
			if (!ReferencedAsset.RemoveCommand(context, proc)) {
				throw new System.Exception("NO SUCH PROCESS");
			}
			proc.Dispose();
			_executor.Process = null;
		}
		public void ExecuteCurrentCommand() {
			_executor.Execute(ReferencedProcess, _command);
		}

		public void ExecuteCommand(string command) {
			CurrentCommandInput = command;
			_executor.Execute(ReferencedProcess, command);
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
			EditorGUI.BeginChangeCheck();
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
			if (EditorGUI.EndChangeCheck()) {
			serializedObject.Update();
				serializedObject.ApplyModifiedProperties();
			}
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
			Target.ExecuteCommand(command);
			RefreshInspector();
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
						//HandleProgressBar(commandProcessor.GetProgress());
						Target.HandleProgressBar(Context, commandProcessor.GetProgress());
						RefreshInspectorInternal();
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
