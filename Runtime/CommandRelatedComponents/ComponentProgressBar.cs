using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComponentProgressBar : MonoBehaviour
{
	public Image progress;
	public Image background;
	public TMP_Text title;
	public TMP_Text text;
	public Button cancelButton;
	[SerializeField] private bool _cancelled;
	[SerializeField] private bool _cancelAvailable;
	private static ComponentProgressBar _instance;

	public static ComponentProgressBar Instance => _instance;
	public static bool _progressBarVisible;

	public bool Cancelled {
		get => _cancelled;
		set => _cancelled = value;
	}

	public bool CancelledAvailable {
		get => _cancelAvailable;
		set {
			_cancelAvailable = value;
			if (progress.gameObject.activeSelf) {
				cancelButton.gameObject.SetActive(_cancelAvailable);
			}
		}
	}

	private void Awake() {
		_instance = this;
		Hide();
	}

	public void Cancel() {
		Cancelled = true;
	}

	public void Hide() {
		text.gameObject.SetActive(false);
		title.gameObject.SetActive(false);
		progress.gameObject.SetActive(false);
		background.gameObject.SetActive(false);
		cancelButton.gameObject.SetActive(false);
	}

	public void Show() {
		text.gameObject.SetActive(true);
		title.gameObject.SetActive(true);
		progress.gameObject.SetActive(true);
		background.gameObject.SetActive(true);
		if (CancelledAvailable) {
			cancelButton.gameObject.SetActive(true);
		}
	}

	public static bool IsProgressBarVisible => _progressBarVisible;

	public static bool DisplayCancelableProgressBar(string title, string info, float progress) {
		_progressBarVisible = true;
#if UNITY_EDITOR
		//Debug.Log($"progress {title} {progress}");
		if (!Application.isPlaying) {
			return UnityEditor.EditorUtility.DisplayCancelableProgressBar(title, info, progress);
		}
#endif
		ComponentProgressBar bar = Instance;
		if (bar == null) {
			return false;
		}
		bar.CancelledAvailable = true;
		bool wasCancelled = bar.Cancelled;
		bool sameProgressBarAsBefore = bar.title.text == title;
		bar.title.text = title;
		bar.text.text = info;
		bar.progress.fillAmount = progress;
		if (sameProgressBarAsBefore) {
			bar.Cancelled = false;
		}
		bar.Show();
		return wasCancelled;
	}

	public static void ClearProgressBar() {
		_progressBarVisible = false;
#if UNITY_EDITOR
		//Debug.Log("done");
		if (!Application.isPlaying) {
			UnityEditor.EditorUtility.ClearProgressBar();
			return;
		}
#endif
		ComponentProgressBar bar = Instance;
		if (bar == null) {
			return;
		}
		bar.Cancelled = false;
		bar.Hide();
	}
}
