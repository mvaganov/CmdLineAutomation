using TMPro;
using UnityEngine;

public class ComponentGetChoice : MonoBehaviour {
	[SerializeField] protected GameObject choicePrefab;
	[SerializeField] protected TMP_Text message;
	[SerializeField] protected RectTransform choiceContent;

	public string Message {
		get => message.text;
		set => message.text = value;
	}



	public void SetChoiceText(int index, string text) {

	}

	public void SetChoiceCommand(int index, string command) {

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
