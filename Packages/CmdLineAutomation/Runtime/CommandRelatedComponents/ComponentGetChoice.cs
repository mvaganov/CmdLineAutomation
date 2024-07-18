using RunCmd;
using TMPro;
using UnityEngine;

namespace RunCmd {
	public class ComponentGetChoice : MonoBehaviour {
		[SerializeField] protected GameObject choicePrefab;
		[SerializeField] protected TMP_Text message;
		[SerializeField] protected RectTransform choiceContent;

		public string Message {
			get => message.text;
			set => message.text = value;
		}

		public KeyValuePairStrings[] options;

		public void SetChoiceText(int index, string text) {
			options[index].Key = text;
		}

		public void SetChoiceCommand(int index, string command) {
			options[index].Value = command;
		}

		public void SetChoice(int index, string text, string command) {
			SetChoiceText(index, text);
			SetChoiceCommand(index, command);
		}

		void Start() {

		}

		void Update() {

		}
	}
}
