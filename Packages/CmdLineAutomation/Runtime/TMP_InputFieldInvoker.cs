using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class TMP_InputFieldInvoker : MonoBehaviour
{
	public TMPro.TMP_InputField inputField;
	public UnityEvent OnEnter = new UnityEvent();

	private void Update() {
		if (inputField != null && EventSystem.current.currentSelectedGameObject == inputField.gameObject && Input.GetKeyDown(KeyCode.Return)) {
			OnEnter.Invoke();
		}
	}
}
