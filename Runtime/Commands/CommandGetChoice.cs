using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Specialized;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RunCmd {
	// TODO make choice UI that can execute during runtime
	[CreateAssetMenu(fileName = "getchoice", menuName = "ScriptableObjects/Commands/getchoice")]
	public class CommandGetChoice : ScriptableObject, CommandRunner<CommandGetChoice.Execution>, INamedCommand {
		public static Vector2 MousePosition;
		public bool _blockUntilChoiceIsMade = true;
		public bool _defaultChoiceOnTimeout = true;
		public bool _blockingUnderlay = true;
		public int _defaultChoice = -1;
		public float _choiceTimeout = 30;
		public Color _choiceWindowColor = new Color(0.25f,0.25f,0.25f);
		public Color _underlayWindowColor = new Color(0.125f, 0.125f, 0.125f);
		public string _defaultMessage = "Continue execution?";
		public KeyValuePairStrings[] _defaultChoices = new KeyValuePairStrings[] {
			new KeyValuePairStrings("yes", ""),
			new KeyValuePairStrings("no", "exit"),
		};

		public class Execution {
			public bool finished;
			public int timeoutStart;
			public int timeoutDuration;
			public ComponentGetChoice runtimeComponent;
#if UNITY_EDITOR
			public GetChoiceWindow choiceWindow;
#endif
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
				case EventType.Repaint:
#if UNITY_EDITOR
					MousePosition = Event.current.mousePosition;
					//Debug.Log("MousePosition : " + MousePosition);
#else
					MousePosition = Input.mousePosition;
#endif
					break;
			}
		}

		public string CommandToken => this.name;

		private Dictionary<object, Execution> _executionData = new Dictionary<object, Execution>();
		public Dictionary<object, Execution> ExecutionDataAccess { get => _executionData; set => _executionData = value; }
		public IEnumerable<object> GetContexts() => ExecutionDataAccess.Keys;

		public void StartCooperativeFunction(object context, string command, PrintCallback print) {
			UpdateMousePosition();
			object parsed = Parse.ParseText($"[{command}]", out Parse.ParseResult err);
			if (err.IsError) {
				string errorMessage = $"error @{err.TextIndex}: {err.ResultKind}";
				Debug.LogError(errorMessage);
				print.Invoke(errorMessage);
				return;
			}
			IList arguments = parsed as IList;
			string  message = (arguments.Count > 1) ? (Parse.Token)arguments[1] : (Parse.Token)_defaultMessage;
			IDictionary args = (arguments.Count > 2) ? arguments[2] as IDictionary : DefaultChoiceDictionary();
			Vector2 size = new Vector2(250, 30 + args.Count * 20);
			List<string> argsOptions = new List<string>();
			List<Action> argsActions = new List<Action>();
			Action<int> onChoiceMade = null;
			Execution exec = this.GetExecutionData(context);
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
				argsOptions.Add(token.Text);
				int index = entryIndex;
				argsActions.Add(() => {
					onChoiceMade?.Invoke(index);
					ICommandAutomation commandAutomation = context as ICommandAutomation;
					commandAutomation.CommandExecutor.InsertNextCommandToExecute(context, value.Text);
				});
				++entryIndex;
			}
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				exec.choiceWindow = GetChoiceWindow.Dialog(message, argsOptions,
					argsActions, onChoiceMade, size, size / -2, _blockingUnderlay);
				exec.choiceWindow.rootVisualElement.style.backgroundColor = _choiceWindowColor;
				if (_blockingUnderlay) {
					exec.choiceWindow._choiceBlocker.rootVisualElement.style.backgroundColor = _underlayWindowColor;
				}
				return;
			}
#endif
			Debug.Log($"choice? {ComponentGetChoice.Instance}");
			if (ComponentGetChoice.Instance != null) {
				exec.runtimeComponent = ComponentGetChoice.Instance;
				exec.runtimeComponent.Message = message;
				exec.runtimeComponent.SetChoices(argsOptions, onChoiceMade, context, this);
				GameObject choiceGameObject = exec.runtimeComponent.ChoiceWindow.gameObject;
				Debug.Log(choiceGameObject);
				UnityEngine.UI.Image img = choiceGameObject.GetComponent<UnityEngine.UI.Image>();
				img.color = _choiceWindowColor;
				choiceGameObject.SetActive(true);
				if (_blockingUnderlay) {
					exec.runtimeComponent.Underlay.GetComponent<UnityEngine.UI.Image>().color = _underlayWindowColor;
				}
				exec.runtimeComponent.Underlay.gameObject.SetActive(_blockingUnderlay);
				exec.runtimeComponent.gameObject.SetActive(true);
			}
		}

		public IDictionary DefaultChoiceDictionary() {
			OrderedDictionary dict = new OrderedDictionary();
			for (int i = 0; i < _defaultChoices.Length; ++i) {
				dict[(Parse.Token)_defaultChoices[i].Key] = (Parse.Token)_defaultChoices[i].Value;
			}
			return dict;
		}

		public void ChoiceMade(object context, int choiceIndex) {
			Execution exec = this.GetExecutionData(context);
			exec.finished = true;
#if UNITY_EDITOR
			exec.choiceWindow?.CloseChoiceWindow();
#endif
			if (ComponentGetChoice.Instance != null && exec.runtimeComponent != null) {
				exec.runtimeComponent.gameObject.SetActive(false);
			}
		}

		public string UsageString() {
			return $"{name} \"message\" {{ \"optionText0\" : \"command0\", ... \"optionTextN\" : \"commandN\" }}";
		}

		public bool IsExecutionFinished(object context) =>
			_blockUntilChoiceIsMade ? this.GetExecutionData(context).finished : true;

		public float Progress(object context) {
			if (_defaultChoiceOnTimeout) {
				Execution exec = this.GetExecutionData(context);
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

		public Execution CreateEmptyContextEntry(object context) => new Execution();

		public void RemoveExecutionData(object context) {
			Execution exec = this.GetExecutionData(context);
			if (exec != null && exec.choiceWindow != null) {
#if UNITY_EDITOR
				exec.choiceWindow?.CloseChoiceWindow();
			}
#endif
			if (ComponentGetChoice.Instance != null && exec.runtimeComponent != null) {
				exec.runtimeComponent.gameObject.SetActive(false);
			}
			CommandRunnerExtension.RemoveExecutionData(this, context);
		}

#if UNITY_EDITOR
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
			private bool _initailized = false;
			private Vector2 _mouseOffset;

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
				newDialog._mouseOffset = mouseOffset;
				newDialog.ShowPopup();
				_dialogs.Add(newDialog);
				return newDialog;
			}

			private void OnInspectorUpdate() {
				if (!_initailized) {
					UpdateMousePosition();
					_initailized = true;
					Vector2 size = position.size;
					size.y = 30 + _options.Count * 20;
					Vector2 mousePos = GUIUtility.GUIToScreenPoint(MousePosition) + _mouseOffset;
					position = new Rect(mousePos.x, mousePos.y, size.x, size.y);
				}
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
					button.text = _options[i].Replace("\r","");
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
#endif
	}
}
