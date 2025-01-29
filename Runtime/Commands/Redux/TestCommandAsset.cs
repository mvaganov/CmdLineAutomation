using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RunCmdRedux {
	/// <summary>
	/// TODO: test one command at a time. all of them at once in a filter and a dictionary is a bit much.
	/// </summary>
	[CreateAssetMenu(fileName = "test", menuName = "ScriptableObjects/CommandAsset/_test")]
	public class TestCommandAsset : ScriptableObject, ICommandExecutor {
		[SerializeField] protected bool _populateZeroArgWithCommand = true;
		[Interface(nameof(_commandAsset))]
		[SerializeField] protected Object commandAsset;
		private ICommandAsset _commandAsset;
		protected TestExecutor _executor = new TestExecutor();
		[SerializeField] protected string _command;

		public TestExecutor Executor => _executor;

		public bool IsExecuting => _executor != null && _executor.Process != null
			&& _executor.Process.ExecutionState == ICommandProcess.State.Executing;

		public string CommandInput {
			get => _command;
			set => _command = value;
		}

		public string CommandOutput {
			get => _executor.CommandOutput;
			set => _executor.CommandOutput = value;
		}

		public ICommandAsset CommandAsset => _commandAsset != null
			? _commandAsset : _commandAsset = commandAsset as ICommandAsset;

		public ICommandProcess Process => _executor.Process;

		public ICommandProcess GetProcess(object context) => CommandAsset != null ?
			CommandAsset.GetCommandCreateIfMissing(context) : null;

		public ICommandProcess CurrentCommand(object context) => CommandAsset.GetCommandIfCreated(context);

		private void OnValidate() {
			_commandAsset = null;
		}

		public float Progress(object context) {
			ICommandProcess proc = CurrentCommand(context);
			if (proc == null) { return 0; }
			return proc.GetProgress();
		}

		public bool UpdateExecution(object context) {
			ICommandProcess proc = CurrentCommand(context);
			float progress = IsExecuting ? proc.GetProgress() : 1;
			bool waitingForCommandToFinish = progress < 1;
			if (waitingForCommandToFinish) {
				HandleProgressBar(context, progress);
			}
			return waitingForCommandToFinish;
		}

		internal bool HandleProgressBar(object context, float progress) {
			bool stop = ComponentProgressBar.DisplayCancelableProgressBar(name, Executor.CommandInput, progress);
			if (stop) {
				DoAbort(context);
			}
			return stop;
		}

		internal void DoAbort(object context) {
			CancelProcess(context);
			ClearProgressBar();
		}

		public void CancelProcess(object context) {
			//Debug.Log($"canceling [{_executor.Process}] ({context})");
			ICommandProcess proc = CurrentCommand(context);
			if (proc != _executor.Process) {
				throw new Exception($"unexpected process to cancel. expected to cancel {_executor.Process}, found {proc}");
			}
			StopProcess(context, proc);
		}

		public void StopProcess(object context, ICommandProcess proc) {
			if (_executor.Process != null && proc != _executor.Process) {
				Debug.LogWarning($"{_executor} no longer processing {proc}, which is being told to stop");
			}
			if (!CommandAsset.RemoveCommand(context, proc)) {
				throw new Exception("NO SUCH PROCESS");
			}
			proc.Dispose();
			_executor.Process = null;
		}

		public void ExecuteCurrentCommand(object context) {
			string command = CommandInput;
			if (_populateZeroArgWithCommand) {
				string zeroArg = commandAsset != null ? commandAsset.name : name;
				command = $"{zeroArg} {command}";
			}
			Debug.Log(command);
			_executor.Execute(GetProcess(context), command);
		}

		public void ExecuteCommand(object context, string command) {
			CommandInput = command;
			ExecuteCurrentCommand(context);
		}

		[ContextMenu(nameof(ClearProgressBar))]
		public void ClearProgressBar() {
			ComponentProgressBar.ClearProgressBar();
		}

		[ContextMenu(nameof(DebugLogTimestamp))]
		public void DebugLogTimestamp() {
			CommandOutput += $"{Environment.TickCount}\n";
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
		private object Context => Target;

		public TestCommandAsset Target => _target != null ? _target
			: _target = target as TestCommandAsset;

		public ICommandProcess ReferencedProcess => Target.GetProcess(Context);

		public override void OnInspectorGUI() {
			CreateTextStyle();
			EditorGUI.BeginChangeCheck();
			DrawDefaultInspector();
			UpdateExecution();
			GUILayout.BeginHorizontal();
			RunCommandsButtonGUI();
			ShowButtonClearOutput();
			GUILayout.EndHorizontal();
			EditorGUILayout.TextArea(Target.CommandOutput, _consoleTextStyle);
			InputPromptGUI();
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

		private void UpdateExecution() {
			Target.UpdateExecution(Context);
			if (Target.IsExecuting) {
				RefreshInspector(); // prompt GUI to animate after a change
			}
			ICommandProcess process = Target.GetProcess(Context);
			if (ProcessShouldBeStopped(process)) {
				Target.ClearProgressBar();
				Target.StopProcess(Context, process);
			}
		}

		private bool ProcessShouldBeStopped(ICommandProcess process) {
			switch (process.ExecutionState) {
				case ICommandProcess.State.Finished:
				case ICommandProcess.State.Disabled:
				case ICommandProcess.State.Error:
				case ICommandProcess.State.Cancelled:
					return true;
			}
			return false;
		}

		private void RunCommandsButtonGUI() {
			ICommandProcess process = Target.CurrentCommand(Context);
			if (process == null || process.ExecutionState != ICommandProcess.State.Executing) {
				ShowButtonRun();
			} else {
				ShowButtonAbort();
			}
		}

		private void ShowButtonRun() {
			if (GUILayout.Button("Run Command")) {
				DoRunCommand();
			}
		}

		private void DoRunCommand() {
			Target.ExecuteCurrentCommand(Context);
			RefreshInspector();
		}

		private void ShowButtonAbort() {
			if (GUILayout.Button($"Abort Command")) {
				DoAbort();
			}
		}

		private void DoAbort() {
			Target.CancelProcess(Context);
			CommandDelay.DelayCall(Target.ClearProgressBar);
		}

		private void ShowButtonClearOutput() {
			if (!GUILayout.Button("Clear Output")) {
				return;
			}
			Target.CommandOutput = "";
			RefreshInspector();
		}

		private void InputPromptGUI() {
			Color color = Target.IsExecuting ? Color.black : Color.gray;
			GuiLine(1, color);
			string command = PromptGUI(_consoleTextStyle);
			GuiLine(1, color);
			if (command == null) {
				return;
			}
			if (Target.IsExecuting) {
				Debug.Log($"waiting for command to finish before '{command}' can execute..." +
					$" working on\n  \"{Target.CommandInput}\"");
				Target.CommandInput = command;
				return;
			}
			Target.ExecuteCommand(Context, command);
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
			if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Escape) {
				Target.CommandInput = "";
				if (Target.IsExecuting) {
					DoAbort();
				}
			}
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
			CommandDelay.DelayCall(RefreshInspectorInternal);
		}

		private void RefreshInspectorInternal() {
			EditorUtility.SetDirty(Target);
		}
	}
}
