using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RunCmd {
	public class ComponentGetChoice : MonoBehaviour {
		[SerializeField] protected RectTransform underlay;
		[SerializeField] protected RectTransform choiceWindow;
		[SerializeField] protected GameObject choicePrefab;
		[SerializeField] protected TMP_Text message;
		[SerializeField] protected RectTransform choiceContent;

		public string Message {
			get => message.text;
			set => message.text = value;
		}

		public RectTransform ChoiceWindow => choiceWindow;

		public RectTransform Underlay => underlay;

		protected List<GameObject> optionUi;

		public static ComponentGetChoice Instance { get; private set; }

		private void Awake() {
			if (Instance != null) {
				throw new System.Exception($"multiple {nameof(ComponentGetChoice)}, {Instance} and {this}");
			}
			Instance = this;
		}

		public void SetChoice(int index, string text) {
			GameObject uiElement = optionUi[index];
			TMP_Text textComponent = uiElement.GetComponentInChildren<TMP_Text>();
			textComponent.text = text;
		}

		public void SetChoice(int index, string text, object context, CommandGetChoice commandGetChoice) {
			SetChoice(index, text);
			Button button = optionUi[index].GetComponentInChildren<Button>();
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(() => commandGetChoice.ChoiceMade(context, index));
		}

		public void SetChoices(IList<string> choices, object context, CommandGetChoice commandGetChoice) {
			ClearOptionsUi();
			for (int i = 0; i < choices.Count; ++i) {
				GameObject optionUiElement = Instantiate(choicePrefab);
				optionUiElement.transform.SetParent(choiceContent, false);
				optionUi.Add(optionUiElement);
				SetChoice(i, choices[i], context, commandGetChoice);
			}
		}

		public void ClearOptionsUi() {
			optionUi.ForEach(Destroy);
			optionUi.Clear();
		}
	}
}
