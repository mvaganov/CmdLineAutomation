using UnityEngine;

public class CommandDelay {
	public static void DelayCall(System.Action call) {
#if UNITY_EDITOR
		if (!Application.isPlaying) {
			UnityEditor.EditorApplication.delayCall += () => call();
		} else
#endif
		{
			DelayCallUsingCoroutine(call);
		}
	}

	public static void DelayCallUsingCoroutine(System.Action call) {
		CoroutineRunner.Instance.StartCoroutine(DelayCall());
		System.Collections.IEnumerator DelayCall() {
			yield return null;
			call.Invoke();
		}
	}
	private class CoroutineRunner : MonoBehaviour {
		private static CoroutineRunner _instance;
		public static CoroutineRunner Instance {
			get {
				if (_instance != null) { return _instance; }
				GameObject go = new GameObject("<CoroutineRunner>");
				DontDestroyOnLoad(go);
				return _instance = go.AddComponent<CoroutineRunner>();
			}
		}
	}
}
