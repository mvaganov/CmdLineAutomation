using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Specialized;

namespace RunCmd {
	[CreateAssetMenu(fileName = "getchoice", menuName = "ScriptableObjects/Commands/getchoice")]
	public class CommandGetChoice : CommandRunner<CommandGetChoice.Execution>, INamedCommand {
		public static Vector2 MousePosition;
		public bool _blockUntilChoiceIsMade = true;
		public bool _defaultChoiceOnTimeout = true;
		public bool _blockingUnderlay = true;
		public int _defaultChoice = -1;
		public float _choiceTimeout = 30;
		public Color _choiceWindowColor = new Color(0.25f,0.25f,0.25f);
		public Color _underlayWindowColor = new Color(0.125f, 0.125f, 0.125f);
		public string _defaultMessage = "Continue execution?";
		public Choice[] _defaultChoices = new Choice[] {
			new Choice("yes", ""),
			new Choice("no", "exit"),
		};

		[Serializable]
		public class Choice {
			public string OptionText;
			public string OptionCommand;
			public Choice(string optionText, string optionCommand) { this.OptionText = optionText; this.OptionCommand = optionCommand; }
		}

		public class Execution {
			public bool finished;
			public int timeoutStart;
			public int timeoutDuration;
			public GetChoiceWindow choiceWindow;
		}

		public static void UpdateMousePosition() {
			if (Event.current == null) {
				return;
			}
			switch (Event.current.type) {
				case EventType.MouseDown:
				case EventType.MouseUp:
				case EventType.MouseMove:
				case EventType.MouseDrag:
#if UNITY_EDITOR
					MousePosition = Event.current.mousePosition;
					Debug.Log(MousePosition);
#endif
					break;
			}
		}
		public string CommandToken => this.name;
		public override void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			UpdateMousePosition();
			object parsed = Parse.ParseText($"[{command}]", out Parse.Error err);
			if (err.kind != Parse.ErrorKind.None) {
				stdOutput.Invoke($"line {err.index}: {err.kind}");
				return;
			}
			IList arguments = parsed as IList;
			string  message = (arguments.Count > 1) ? (Parse.Token)arguments[1] : (Parse.Token)_defaultMessage;
			IDictionary args = (arguments.Count > 2) ? arguments[2] as IDictionary : DefaultChoiceDitionary();
			Vector2 size = new Vector2(250, 30 + args.Count * 20);
			List<string> argsOptions = new List<string>();
			List<Action> argsActions = new List<Action>();
			Action<int> onChoiceMade = null;
			Execution exec = GetExecutionData(context);
			if (_blockUntilChoiceIsMade) {
				exec.timeoutStart = Environment.TickCount;
				exec.timeoutDuration = (int)(_choiceTimeout * 1000);
				exec.finished = false;
				onChoiceMade = (choice) => ChoiceMade(context, choice);
			}
			int entryIndex = 0;
			foreach (DictionaryEntry entry in args) {
				Parse.Token token = (Parse.Token)entry.Key;
				Parse.Token value = (Parse.Token)entry.Value;
				argsOptions.Add(token.text);
				int index = entryIndex;
				argsActions.Add(() => {
					onChoiceMade?.Invoke(index);
					CommandAutomation commandAutomation = context as CommandAutomation;
					commandAutomation.InsertNextCommandToExecute(context, value.text);
				});
				++entryIndex;
			}
			exec.choiceWindow = GetChoiceWindow.Dialog(message, argsOptions,
				argsActions, onChoiceMade, size, size / -2, _blockingUnderlay);
			exec.choiceWindow.rootVisualElement.style.backgroundColor = _choiceWindowColor;
			if (_blockingUnderlay) {
				exec.choiceWindow._choiceBlocker.rootVisualElement.style.backgroundColor = _underlayWindowColor;
			}
		}

		public IDictionary DefaultChoiceDitionary() {
			OrderedDictionary dict = new OrderedDictionary();
			for (int i = 0; i < _defaultChoices.Length; ++i) {
				dict[(Parse.Token)_defaultChoices[i].OptionText] = (Parse.Token)_defaultChoices[i].OptionCommand;
			}
			return dict;
		}

		public void ChoiceMade(object context, int choiceIndex) {
			Execution exec = GetExecutionData(context);
			exec.finished = true;
			exec.choiceWindow?.CloseChoiceWindow();
		}

		public string UsageString() {
			return $"{name} \"message\" {{ \"optionText0\" : \"command0\", ... \"optionTextN\" : \"commandN\" }}";
		}

		public override bool IsExecutionFinished(object context) =>
			_blockUntilChoiceIsMade ? GetExecutionData(context).finished : true;

		public override float Progress(object context) {
			if (_defaultChoiceOnTimeout) {
				Execution exec = GetExecutionData(context);
				int passed = Environment.TickCount - exec.timeoutStart;
				float progress = (float)passed / exec.timeoutDuration;
				if (progress >= 1) {
					exec.finished = true;
					progress = 1;

				}
				return progress;
			}
			return 0;
		}

		protected override Execution CreateEmptyContextEntry(object context) => new Execution();

		public override void RemoveExecutionData(object context) {
			Execution exec = GetExecutionData(context);
			exec.choiceWindow.CloseChoiceWindow();
			base.RemoveExecutionData(context);
		}

		/// <summary>
		/// Block UI, preventing other clicks. Clicking non-choice is a choice (probably cancel).
		/// </summary>
		public class ChoiceBlocker : EditorWindow {
			public Action blockedClick;
			public void Resize() {
				Vector2 mousePos = GUIUtility.GUIToScreenPoint(MousePosition);
				position = new Rect(mousePos.x - 3000, mousePos.y - 3000, 6000, 6000);
				rootVisualElement.style.backgroundColor = new Color(0, 0, 0, .5f);
			}

			private void OnGUI() {
				UpdateMousePosition();
				if (Event.current.type == EventType.MouseDown) {
					Close();
					blockedClick.Invoke();
				}
			}
		}

		public class GetChoiceWindow : EditorWindow {
			private static List<GetChoiceWindow> _dialogs = new List<GetChoiceWindow>();
			private string _message;
			private IList<string> _options;
			private IList<Action> _actions;
			public ChoiceBlocker _choiceBlocker;

			public static void ReopenProjectDialog(Action<int> onChoiceMade) {
				GetChoiceWindow.Dialog("Restart Project?",
					new string[] { "Restart", "Cancel" },
					new Action[] { () => UnityEditor.EditorApplication.OpenProject(System.IO.Directory.GetCurrentDirectory()), null },
					onChoiceMade, new Vector2(300, 100), new Vector2(-150, -50), true);
			}

			public static GetChoiceWindow Dialog(string message, IList<string> options, IList<Action> actions,
				Action<int> onFinished, Vector2 size, Vector2 mouseOffset, bool useUiBlocker) {
				int foundIndex = _dialogs.FindIndex(d => d._message == message);
				if (foundIndex >= 0) {
					GetChoiceWindow oldDialog = _dialogs[foundIndex];
					_dialogs.RemoveAt(foundIndex);
					oldDialog.Close();
				}

				GetChoiceWindow newDialog = CreateInstance<GetChoiceWindow>();
				if (useUiBlocker) {
					newDialog._choiceBlocker = CreateInstance<ChoiceBlocker>();
					newDialog._choiceBlocker.blockedClick = () => {
						onFinished?.Invoke(-1);
						newDialog.Close();
					};
					newDialog._choiceBlocker.ShowPopup();
					newDialog._choiceBlocker.Resize();
				}
				newDialog._message = message;
				newDialog._options = options;
				newDialog._actions = actions;
				Vector2 mousePos = GUIUtility.GUIToScreenPoint(MousePosition) + mouseOffset;
				newDialog.position = new Rect(mousePos.x, mousePos.y, size.x, size.y);
				newDialog.ShowPopup();
				_dialogs.Add(newDialog);
				return newDialog;
			}

			private void OnInspectorUpdate() {
				UpdateMousePosition();
			}

			public void CloseChoiceWindow() {
				Close();
				RemoveNotification();
			}

			void CreateGUI() {
				Label label = new Label(_message);
				rootVisualElement.Add(label);
				if (_options == null) {
					CloseChoiceWindow();
					return;
				}
				for (int i = 0; i < _options.Count; ++i) {
					Button button = new Button();
					button.text = _options[i];
					{
						int index = i;
						button.clicked += () => DoAction(index);
					}
					rootVisualElement.Add(button);
				}
			}

			public void DoAction(int index) {
				Close();
				if (_actions == null) { return; }
				Action action = _actions[index];
				if (action != null) { action.Invoke(); }
			}

			private void OnDestroy() {
				_dialogs.Remove(this);
				if (_choiceBlocker != null) { _choiceBlocker.Close(); }
			}
		}
	}
}
