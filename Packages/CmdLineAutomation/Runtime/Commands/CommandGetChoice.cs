using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Specialized;

namespace RunCmd {
	[CreateAssetMenu(fileName = "getchoice", menuName = "ScriptableObjects/Commands/getchoice")]
	public class CommandGetChoice : ScriptableObject, INamedCommand {
		public string CommandToken => this.name;
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			object parsed = Parse.ParseText($"[{command}]", out Parse.Error err);
			if (err.kind != Parse.ErrorKind.None) {
				stdOutput.Invoke($"line {err.index}: {err.kind}");
				return;
			}
			IList arguments = parsed as IList;
			string  message = (arguments.Count > 1) ? (Parse.Token)arguments[1] : (Parse.Token)"<missing argument 1>";
			IDictionary args = (arguments.Count > 2) ? arguments[2] as IDictionary : 
				new OrderedDictionary() { [(Parse.Token)"<missing argument 2>"] = (Parse.Token)"<missing argument 2>" };
			Vector2 size = new Vector2(250, 30 + args.Count * 20);
			List<string> argsOptions = new List<string>();
			List<Action> argsActions = new List<Action>();
			foreach (DictionaryEntry entry in args) {
				Parse.Token token = (Parse.Token)entry.Key;
				Parse.Token value = (Parse.Token)entry.Value;
				argsOptions.Add(token.text);
				argsActions.Add(() => Debug.Log(context+" should do "+value.text));
			}
			GetChoiceWindow.Dialog(message, argsOptions, argsActions, size, size / -2, true);
		}

		public string UsageString() {
			return $"{name} \"message\" {{ \"optionText0\" : \"command0\", ... \"optionTextN\" : \"commandN\" }}";
		}

		public bool IsExecutionFinished(object context) => true;
		public float Progress(object context) => 0;

		/// <summary>
		/// Block UI, preventing other clicks. Clicking non-choice is a choice (probably cancel).
		/// </summary>
		public class ChoiceBlocker : EditorWindow {
			public Action blockedClick;

			public void Resize() {
				Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
				position = new Rect(mousePos.x - 3000, mousePos.y - 3000, 6000, 6000);
				//rootVisualElement.style.backgroundColor = new Color(0, 0, 0, .5f);
				rootVisualElement.style.backgroundImage = MakeBackgroundTexture(1, 1, new Color(0,0,0,.5f));
			}

			private static Texture2D MakeBackgroundTexture(int width, int height, Color color) {
				Color[] pixels = new Color[width * height];
				for (int i = 0; i < pixels.Length; i++) { pixels[i] = color; }
				Texture2D backgroundTexture = new Texture2D(width, height);
				backgroundTexture.SetPixels(pixels);
				backgroundTexture.Apply();
				return backgroundTexture;
			}

			private void OnGUI() {
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
			private ChoiceBlocker _choiceBlocker;

			public static void ReopenProjectDialog() {
				GetChoiceWindow.Dialog("Restart Project?",
					new string[] { "Restart", "Cancel" },
					new Action[] { () => UnityEditor.EditorApplication.OpenProject(System.IO.Directory.GetCurrentDirectory()), null },
					new Vector2(300, 100), new Vector2(-150, -50), true);
			}

			public static void Dialog(string message, IList<string> options, IList<Action> actions, Vector2 size,
				Vector2 mouseOffset, bool useUiBlocker) {
				int foundIndex = _dialogs.FindIndex(d => d._message == message);
				if (foundIndex >= 0) {
					GetChoiceWindow oldDialog = _dialogs[foundIndex];
					_dialogs.RemoveAt(foundIndex);
					oldDialog.Close();
				}

				GetChoiceWindow newDialog = CreateInstance<GetChoiceWindow>();
				if (useUiBlocker) {
					newDialog._choiceBlocker = CreateInstance<ChoiceBlocker>();
					newDialog._choiceBlocker.blockedClick = newDialog.Close;
					newDialog._choiceBlocker.ShowPopup();
					newDialog._choiceBlocker.Resize();
				}
				newDialog._message = message;
				newDialog._options = options;
				newDialog._actions = actions;
				Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition) + mouseOffset;
				newDialog.position = new Rect(mousePos.x, mousePos.y, size.x, size.y);
				newDialog.ShowPopup();
				_dialogs.Add(newDialog);
			}

			void CreateGUI() {
				Label label = new Label(_message);
				rootVisualElement.Add(label);
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
