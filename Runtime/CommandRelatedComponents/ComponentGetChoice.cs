using RunCmdRedux;
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
		private static ComponentGetChoice _instance;

		public string Message {
			get => message.text;
			set => message.text = value;
		}

		public RectTransform ChoiceWindow => choiceWindow;

		public RectTransform Underlay => underlay;

		protected List<GameObject> optionUi = new List<GameObject>();

		public static ComponentGetChoice Instance {
			get {
				if (_instance != null) {
					return _instance;
				}
				return _instance = FindAnyObjectByType<ComponentGetChoice>(FindObjectsInactive.Include);
			}
			private set { _instance = value; }
		}

		private void Awake() {
			if (Instance != null && Instance != this) {
				throw new System.Exception($"multiple {nameof(ComponentGetChoice)}, {Instance.GetHashCode()} and {GetHashCode()}");
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

		public void SetChoice(int index, string text, object context, CommandAssetGetChoice commandGetChoice) {
			SetChoice(index, text);
			Button button = optionUi[index].GetComponentInChildren<Button>();
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(() => commandGetChoice.ChoiceMade(context, index));
		}

		public void SetChoices(IList<string> choices, System.Action<int> onChoiceMade, object context, CommandGetChoice commandGetChoice) {
			ClearOptionsUi();
			for (int i = 0; i < choices.Count; ++i) {
				GameObject optionUiElement = Instantiate(choicePrefab);
				optionUiElement.transform.SetParent(choiceContent, false);
				optionUi.Add(optionUiElement);
				optionUiElement.SetActive(true);
				// TODO is onChoiceMade the same as commandGetChoice.ChoiceMade?
				SetChoice(i, choices[i], context, commandGetChoice);
			}
		}

		public void SetChoices(IList<string> choices, System.Action<int> onChoiceMade, object context, CommandAssetGetChoice commandGetChoice) {
			ClearOptionsUi();
			for (int i = 0; i < choices.Count; ++i) {
				GameObject optionUiElement = Instantiate(choicePrefab);
				optionUiElement.transform.SetParent(choiceContent, false);
				optionUi.Add(optionUiElement);
				optionUiElement.SetActive(true);
				// TODO is onChoiceMade the same as commandGetChoice.ChoiceMade?
				SetChoice(i, choices[i], context, commandGetChoice);
			}
		}

		public void ClearOptionsUi() {
			optionUi.ForEach(go => {
				if (go != null) { Destroy(go); }
			});
			optionUi.Clear();
		}
	}
}
