using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace RunCmd {
	public class CommandAutomationGlue : MonoBehaviour, ICommandAutomation {
		[System.Serializable]
		public class UnityEvent_string : UnityEvent<string> {}

		[SerializeField] private TMPro.TMP_InputField inputField;
		[SerializeField] private RectTransform outputUiTransform;
		[SerializeField] private Button actionButton;
		[SerializeField] private ScrollRect scrollRect;

		public CommandAutomation cmdLineAutomation;
		public UnityEvent_string OnOutputChange = new UnityEvent_string();
		private string _outputText;
		private bool _outputTextChanged;

		public ICommandExecutor CommandExecutor => cmdLineAutomation;

		void Start() {
			cmdLineAutomation.OnOutputChange += PrintCallback;
			PrintCallback(null);
			UpdateUiAfterCommand();
		}

		public void RefreshLayoutNextFrame(RectTransform rootRectTransform) {
			StartCoroutine(RefreshNextFrame());
			IEnumerator RefreshNextFrame() {
				yield return null;
				UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rootRectTransform);
			}
		}

		public void SelectNextFrame(UnityEngine.UI.Selectable selectable) {
			StartCoroutine(RefreshNextFrame());
			IEnumerator RefreshNextFrame() {
				yield return null;
				selectable.Select();
			}
		}

		public void PrintCallback(string newText) {
			_outputText = CommandExecutor.CommandOutput.Replace("\r","");
			_outputTextChanged = true;
		}

		public void ReadInputAndActivateCommandLine() {
			if (inputField.text.Length == 0) {
				Debug.LogWarning("button triggered with empty input...");
				return;
			}
			ActivateCommandLineInputField(inputField.text);
		}

		public void ActivateCommandLineInputField(string text) {
			ExecuteCommand(text);
			inputField.SetTextWithoutNotify("");
			UpdateUiAfterCommand();
		}

		public void UpdateUiAfterCommand() {
			StartCoroutine(AdjustUiAfterCommandExecutes());
			IEnumerator AdjustUiAfterCommandExecutes() {
				yield return null;
				LayoutRebuilder.ForceRebuildLayoutImmediate(outputUiTransform);
				yield return null;
				actionButton.Select();
				yield return null;
				LayoutRebuilder.ForceRebuildLayoutImmediate(outputUiTransform);
				yield return null;
				inputField.Select();
				yield return null;
				scrollRect.normalizedPosition = Vector2.zero;
			}
		}

		public void ExecuteCommand(string command) {
			Debug.Log($"invoking {cmdLineAutomation.name} '{command}'");
			cmdLineAutomation.RunCommand(command, PrintCallback, this);
		}

		private void Update() {
			if (!_outputTextChanged) {
				return;
			}
			_outputText = _outputText.Replace("\u000C", "");
			OnOutputChange.Invoke(_outputText);
			_outputTextChanged = false;
		}
	}
}
