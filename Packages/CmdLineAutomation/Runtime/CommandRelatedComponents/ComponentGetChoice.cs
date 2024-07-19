using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RunCmd {
	public class ComponentGetChoice : MonoBehaviour {
		[SerializeField] protected GameObject choicePrefab;
		[SerializeField] protected TMP_Text message;
		[SerializeField] protected RectTransform choiceContent;

		public string Message {
			get => message.text;
			set => message.text = value;
		}

		public List<KeyValuePairStrings> options = new List<KeyValuePairStrings>();
		public List<GameObject> optionUi;

		public void SetChoiceText(int index, string text) {
			options[index].Key = text;
		}

		public void SetChoiceCommand(int index, string command) {
			options[index].Value = command;
		}

		public void SetChoice(int index, string text, string command, object context, CommandGetChoice commandGetChoice) {
			SetChoiceText(index, text);
			SetChoiceCommand(index, command);
			GameObject uiElement = optionUi[index];
			TMP_Text textComponent = uiElement.GetComponentInChildren<TMP_Text>();
			textComponent.text = text;
			Button button = uiElement.GetComponentInChildren<Button>();
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(() => commandGetChoice.ChoiceMade(context, index));
		}

		public void SetChoicesUi(IEnumerable<KeyValuePairStrings> choices, object context, CommandGetChoice commandGetChoice) {
			options.Clear();
			foreach(KeyValuePairStrings option in choices) {
				options.Add(option);
			}
			RefreshOptions(context, commandGetChoice);
		}

		public void ClearOptionsUi() {
			for(int i = 0; i < optionUi.Count; ++i) {
				GameObject go = optionUi[i];
				Destroy(go);
			}
		}
		
		public void RefreshOptions(object context, CommandGetChoice commandGetChoice) {
			ClearOptionsUi();
			for (int i = 0; i < options.Count; ++i) {
				GameObject optionUiElement = Instantiate(choicePrefab);
				optionUiElement.transform.SetParent(choiceContent, false);
				optionUi.Add(optionUiElement);
				SetChoice(i, options[i].Key, options[i].Value, context, commandGetChoice);
			}
		}

		void Start() {

		}

		void Update() {

		}
	}
}
