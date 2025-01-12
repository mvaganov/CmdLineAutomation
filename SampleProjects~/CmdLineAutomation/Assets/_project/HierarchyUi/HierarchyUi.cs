using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[ExecuteInEditMode]
public class HierarchyUi : MonoBehaviour {
	[System.Serializable] public class UnityEvent_Transform : UnityEvent<Transform> { }

	public ContentSizeFitter contentPanel;
	private Transform _contentPanelTransform;
	public RectTransform innerView;
	public Vector2 contentSize;
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

	private void OnValidate() {
		RectTransform rt = contentPanel.GetComponent<RectTransform>();
		rt.sizeDelta = contentSize;
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
		innerView.sizeDelta = cullBox.size;
		innerView.anchoredPosition = new Vector2(cullBox.position.x, -cullBox.position.y);
		return cullBox;
	}

	// Update is called once per frame
	void Update() {
		if (root == null) {
			RefreshHierarchyState(true);
		}
		CalcCullBox();
		if (usedCullBox != cullBox) {
			RefreshUiElements();
		}
	}

	[ContextMenu(nameof(CalculateHierarchySize))]
	private void CalculateHierarchySize() {
		elementHeight = prefabElement.GetComponent<RectTransform>().sizeDelta.y;
		elementWidth = prefabElement.GetComponent<RectTransform>().sizeDelta.x;
		indentWidth = prefabExpand.GetComponent<RectTransform>().sizeDelta.x;
		int depth = 0;
		root.CalculateHeight(0, ref depth);
		contentSize.y = root.height * elementHeight;
		contentSize.x = depth * indentWidth + elementWidth;
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
		if (!cullOffScreen || cullBox.Overlaps(expandRect))
		{
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
		if (!cullOffScreen || cullBox.Overlaps(elementRect))
		{
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

	void Start() {

	}

	public static List<Transform> GetAllRootElements() {
		Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		List<Transform> list = new List<Transform> ();
		for (int i = 0; i < all.Length; ++i) {
			Transform t = all[i];
			if (t != null && t.parent == null && t.GetComponent<HierarchyIgnore>() == null) {
				list.Add(t);
			}
		}
		list.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));
		return list;
	}

	[ContextMenu(nameof(RebuildHierarchy))]
	public void RebuildHierarchy() {
		RefreshHierarchyState(true);
		CalcCullBox();
		RefreshUiElements();
	}

	private void RefreshHierarchyState(bool expanded) {
		ElementState oldRoot = root;
		List<Transform> list = GetAllRootElements();
		root = new ElementState(null, null, 0, 0, expanded);
		for (int i = 0; i < list.Count; ++i) {
			ElementState es = new ElementState(root, list[i], 0, i, expanded);
			root.children.Add(es);
			es.AddChildren(expanded);
		}
	}

	[ContextMenu(nameof(RefreshUiElements))]
	private void RefreshUiElements() {
		CalculateHierarchySize();
		FreeCurrentUiElements();
		_contentPanelTransform = contentPanel.transform; // cache content transform
		CreateAllChildren(root);
		usedCullBox = cullBox;
	}
}
