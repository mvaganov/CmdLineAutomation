using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteInEditMode]
public class HierarchyUi : MonoBehaviour {
	[System.Serializable] public class UnityEvent_Transform : UnityEvent<Transform> { }

	public ContentSizeFitter contentPanel;
	private Transform _contentPanelTransform;
	private Vector2 contentSize;
	public Button prefabElement;
	public Button prefabExpand;
	public UnityEvent_Transform onElementSelect;
	public ButtonPool elementPool = new ButtonPool();
	public ButtonPool expandPool = new ButtonPool();
	private ScrollRect scrollView;
	private ElementState root;
	private Rect cullBox;
	private Rect usedCullBox;
	private float elementHeight;
	private float elementWidth;
	private float indentWidth;

	void Start() {

	}

	void Update() {
		if (root == null) {
			RefreshHierarchyState(true);
		}
		CalcCullBox();
		if (usedCullBox != cullBox) {
			RefreshUiElements();
		}
	}

	private void OnValidate() {
		RectTransform rt = contentPanel.GetComponent<RectTransform>();
		rt.sizeDelta = contentSize;
	}

	[ContextMenu(nameof(RebuildHierarchy))]
	public void RebuildHierarchy() {
		RefreshHierarchyState(true);
		CalcCullBox();
		RefreshUiElements();
	}

	[ContextMenu(nameof(CalcCullBox))]
	private Rect CalcCullBox() {
		if (scrollView == null) {
			scrollView = GetComponentInChildren<ScrollRect>();
		}
		const float bevel = -10;
		Vector2 viewSize = scrollView.viewport.rect.size;
		Vector2 contentSize = scrollView.content.sizeDelta;
		Vector2 offset = scrollView.normalizedPosition;
		offset.y = 1 - offset.y;
		offset.x *= (contentSize.x - viewSize.x);
		offset.y *= (contentSize.y - viewSize.y);
		offset += new Vector2(bevel, bevel);
		cullBox = new Rect(offset, viewSize - new Vector2(bevel * 2, bevel * 2));
		return cullBox;
	}

	private void RefreshHierarchyState(bool expanded) {
		ElementState oldRoot = root;
		List<Transform>[] objectsPerScene = GetAllRootElementsByScene();
		root = new ElementState(null, null, 0, 0, expanded);
		for(int sceneIndex = 0; sceneIndex < objectsPerScene.Length; ++sceneIndex) {
			List<Transform> list = objectsPerScene[sceneIndex];
			ElementState sceneStateNode = new ElementState(root, null, 0, 0, expanded);
			sceneStateNode.name = list[0].gameObject.scene.name;
			for (int i = 0; i < list.Count; ++i) {
				ElementState es = new ElementState(sceneStateNode, list[i], 0, i, expanded);
				root.children.Add(es);
				es.AddChildren(expanded);
			}
		}
	}

	public static List<Transform>[] GetAllRootElementsByScene() {
		Transform[] allObjects = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		Dictionary<string, List<Transform>> objectsPerScene = new Dictionary<string, List<Transform>>();
		for (int i = 0; i < allObjects.Length; ++i) {
			Transform t = allObjects[i];
			if (t != null && t.parent == null && t.GetComponent<HierarchyIgnore>() == null) {
				string sceneName = t.gameObject.scene.name;
				if (sceneName == null) {
					Debug.Log($"should not have found a pure prefab: {t.name}");
					continue;
				}
				if (!objectsPerScene.TryGetValue(sceneName, out List<Transform> list)) {
					list = new List<Transform>();
					objectsPerScene[sceneName] = list;
				}
				list.Add(t);
			}
		}
		List<Transform>[] resultListOfObjectsByScene = new List<Transform>[objectsPerScene.Count];
		Dictionary<string, int> sceneIndexByName = new Dictionary<string, int>();
		for (int i = 0; i < SceneManager.sceneCount; ++i) {
			sceneIndexByName[SceneManager.GetSceneAt(i).name] = i;
		}
		int unlistedScenes = 0;
		foreach (var kvp in objectsPerScene) {
			List<Transform> list = kvp.Value;
			list.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));
			if (sceneIndexByName.TryGetValue(kvp.Key, out int index)) {
				resultListOfObjectsByScene[index] = list;
			} else {
				resultListOfObjectsByScene[SceneManager.sceneCount + unlistedScenes] = list;
				++unlistedScenes;
			}
		}
		return resultListOfObjectsByScene;
	}

	[ContextMenu(nameof(RefreshUiElements))]
	private void RefreshUiElements() {
		CalculateHierarchySize();
		FreeCurrentUiElements();
		_contentPanelTransform = contentPanel.transform; // cache content transform
		CreateAllChildren(root);
		usedCullBox = cullBox;
	}

	[ContextMenu(nameof(CalculateHierarchySize))]
	private void CalculateHierarchySize() {
		elementHeight = prefabElement.GetComponent<RectTransform>().sizeDelta.y;
		elementWidth = prefabElement.GetComponent<RectTransform>().sizeDelta.x;
		indentWidth = prefabExpand.GetComponent<RectTransform>().sizeDelta.x;
		int depth = 0;
		root.CalculateHeight(0, ref depth, 0);
		contentSize.y = root.height * elementHeight;
		contentSize.x = (depth + 1) * indentWidth + elementWidth;
		RectTransform rt = contentPanel.GetComponent<RectTransform>();
		rt.sizeDelta = contentSize;
	}

	private void FreeCurrentUiElements() {
		elementPool.FreeAllElementFromPools();
		expandPool.FreeAllElementFromPools();
	}

	private void CreateAllChildren(ElementState es) {
		if (!es.Expanded) { return; }
		for (int i = 0; i < es.children.Count; i++) {
			ElementState child = es.children[i];
			if (child.target != null && child.target.GetComponent<HierarchyIgnore>() != null) {
				continue;
			}
			//Debug.Log($"{i} creating {child.name}");
			CreateElement(child, true);
			CreateAllChildren(child);
		}
	}

	private void CreateElement(ElementState es, bool cullOffScreen) {
		Vector2 cursor = new Vector2(indentWidth * es.column, elementHeight * es.row);
		Vector2 anchoredPosition = new Vector2(cursor.x, -cursor.y);
		Vector2 elementPosition = anchoredPosition + Vector2.right * indentWidth;
		RectTransform rt;
		Rect expandRect = new Rect(cursor, new Vector2(indentWidth, elementHeight));
		Rect elementRect = new Rect(cursor + Vector2.right * indentWidth, new Vector2(elementWidth, elementHeight));
		if (!cullOffScreen || cullBox.Overlaps(expandRect)) {
			if (es.children.Count > 0) {
				Button expand = expandPool.GetFreeFromPools(prefabExpand);
				rt = expand.GetComponent<RectTransform>();
				rt.SetParent(_contentPanelTransform, false);
				rt.anchoredPosition = anchoredPosition;
				rt.name = $"> {es.name}";
				expand.onClick.RemoveAllListeners();
				expand.onClick.AddListener(() => ToggleExpand(es));
				es.Expand = expand;
			} else {
				es.Expand = null;
			}
		}
		if (!cullOffScreen || cullBox.Overlaps(elementRect)) {
			Button element = elementPool.GetFreeFromPools(prefabElement);
			rt = element.GetComponent<RectTransform>();
			rt.SetParent(_contentPanelTransform, false);
			rt.anchoredPosition = elementPosition;
			rt.name = $"({es.name})";
			element.onClick.RemoveAllListeners();
			element.onClick.AddListener(() => SelectElement(es));
			es.Label = element;
		} else {
			es.Label = null;
		}
	}

	private void ToggleExpand(ElementState es) {
		Debug.Log($"toggle {es.name}");
		es.Expanded = !es.Expanded;
		RefreshUiElements();
	}

	private void SelectElement(ElementState es) {
		Debug.Log($"selected {es.name}");
		onElementSelect.Invoke(es.target);
	}
}
