using UnityEngine;

public class TMP_InputFieldInvoker : MonoBehaviour
{
	public TMPro.TMP_InputField inputField;

	public void InvokeOnEndEdit() {
		inputField.onEndEdit.Invoke(inputField.text);
	}
}
