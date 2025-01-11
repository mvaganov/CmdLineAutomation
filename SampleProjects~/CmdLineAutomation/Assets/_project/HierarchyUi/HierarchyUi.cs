using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class HierarchyUi : MonoBehaviour {
	//[System.Serializable]
	public class ElementState {
		[HideInInspector]
		public string name;
		public int column, row, height;
		public ElementState parent;
		[HideInInspector]
		public Transform target;
		//public HierarchyElement ui;
		private Button _label, _expand;
		[HideInInspector]
		public Button Label {
			get => _label;
			set {
				_label = value;
			}
		}
		[HideInInspector]
		public Button Expand {
			get => _expand;
			set {
				_expand = value;
			}
		}
		public bool expanded;
		public List<ElementState> children = new List<ElementState>();

		public ElementState(ElementState parent, Transform target, int column, int row, bool expanded) {
			this.target = target;
			this.parent = parent;
			this.column = column;
			this.row = row;
			this.expanded = expanded;
			this.name = (target != null) ? target.name : "";
		}

		public void DoExpand() {
			expanded = true;
			RefreshHeight();
		}

		public void DoCollapse() {
			expanded = false;
			RefreshHeight();
		}

		private void RefreshHeight() {
			int depth = 0;
			if (parent != null) {
				parent.CalculateHeight(0, ref depth);
			} else {
				CalculateHeight(0, ref depth);
			}
		}

		public int CalculateHeight(int depth, ref int maxDepth) {
			if (depth > maxDepth) {
				maxDepth = depth;
			}
			height = target != null ? 1 : 0;
			if (children.Count == 0) {
				return height;
			}
			if (expanded) {
				int r = row + 1;
				for (int i = 0; i < children.Count; i++) {
					children[i].row = r;
					int elementHeight = children[i].CalculateHeight(depth + 1, ref maxDepth);
					height += elementHeight;
					r += elementHeight;
				}
			}
			return height;
		}

		public void AddChildren(bool expanded) {
			int c = column + 1;
			int r = row + 1;
			for(int i = 0; i <target.childCount; ++i) {
				Transform t =	target.GetChild(i);
				if (t == null || t.GetComponent<HierarchyIgnore>() != null) {
					continue;
				}
				ElementState es = new ElementState(this, t, c, r, expanded);
				children.Add(es);
				es.AddChildren(expanded);
			}
		}
	}
	public ContentSizeFitter contentPanel;
	private Transform _contentPanelTransform;
	public RectTransform innerView;
	public Vector2 contentSize;
	public List<Button> usedElement = new List<Button>();
	public List<Button> usedExpand = new List<Button>();
	public List<Button> freeElement = new List<Button>();
	public List<Button> freeExpand = new List<Button>();
	public Button prefabElement;
	public Button prefabExpand;
	private ScrollRect scrollView;

	private ElementState root;
	private Rect cullBox;
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
		const float bevel = 10;
		Vector2 viewSize = scrollView.viewport.rect.size;
		Vector2 contentSize = scrollView.content.sizeDelta;
		Vector2 offset = scrollView.normalizedPosition;
		offset.y = 1 - offset.y;
		offset.x *= (contentSize.x - viewSize.x);
		offset.y *= -(contentSize.y - viewSize.y);
		offset += new Vector2(bevel, -bevel);
		cullBox = new Rect(offset, viewSize - new Vector2(bevel * 2, bevel * 2));
		innerView.sizeDelta = cullBox.size;
		innerView.anchoredPosition = cullBox.position;
		return cullBox;
	}

	// Update is called once per frame
	void Update() {
		CalcCullBox();
	}

	[ContextMenu(nameof(CalculateHierarchy))]
	private void CalculateHierarchy() {
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

	private void ClearAllUi() {
		//usedElement.ForEach(e => DestroyImmediate(e));
		//usedElement.Clear();
		//usedExpand.ForEach(e => DestroyImmediate(e));
		//usedExpand.Clear();
		FreeElements();
		FreeExpands();
	}

	private void CreateAllChildren(ElementState es) {
		for (int i = 0; i < es.children.Count; i++) {
			ElementState child = es.children[i];
			if (child.target != null && child.target.GetComponent<HierarchyIgnore>() != null) {
				continue;
			}
			//Debug.Log($"{i} creating {child.name}");
			CreateElement(child);
			CreateAllChildren(child);
		}
	}

	private void CreateElement(ElementState es) {

		Vector2 cursor = new Vector2(indentWidth * es.column, -elementHeight * es.row);
		Vector2 elementPosition = cursor + Vector2.right * indentWidth;
		RectTransform rt;
		Rect fixedCullBox = cullBox;
		fixedCullBox.position = new Vector2(cullBox.position.x, -cullBox.position.y);
		Rect expandRect = new Rect(new Vector2(cursor.x, -cursor.y), new Vector2(indentWidth, elementHeight));
		Rect elementRect = new Rect(new Vector2(elementPosition.x, -elementPosition.y), new Vector2(elementWidth, elementHeight));
		//if (cullBox.Overlaps(expandRect))
		{
			if (es.children.Count > 0) {
				Button expand = GetFreeExpand();
				rt = expand.GetComponent<RectTransform>();
				rt.SetParent(_contentPanelTransform, false);
				rt.anchoredPosition = cursor;
				rt.name = $"> {es.name}";
				es.Expand = expand;
				if (fixedCullBox.Overlaps(expandRect)) { expand.GetComponent<Image>().color = Color.green; }
			}
		}
		//if (cullBox.Overlaps(elementRect))
		{
			Button element = GetFreeElement();
			rt = element.GetComponent<RectTransform>();
			rt.SetParent(_contentPanelTransform, false);
			rt.anchoredPosition = elementPosition;
			rt.name = $"({es.name})";
			es.Label = element;
			TMP_Text text = element.GetComponentInChildren<TMP_Text>();
			text.text = es.name;
			if (fixedCullBox.Overlaps(elementRect)) { element.GetComponent<Image>().color = Color.green; }
		}
	}

	private void ToggleExpand(ElementState es) {

	}

	private Button GetFreeElement() => GetFreeFromPools(usedElement, freeElement, prefabElement);
	private void FreeElement(Button button) => FreeWithPools(button, usedElement, freeElement);
	private void FreeElements() => FreeAllElementFromPools(usedElement, freeElement);
	private Button GetFreeExpand() => GetFreeFromPools(usedExpand, freeExpand, prefabExpand);
	private void FreeExpand(Button button) => FreeWithPools(button, usedExpand, freeExpand);
	private void FreeExpands() => FreeAllElementFromPools(usedExpand, freeExpand);

	private static Button GetFreeFromPools(List<Button> used, List<Button> free, Button prefab) {
		Button element = null;
		while (free.Count > 0 && element == null) {
			int lastIndex = free.Count - 1;
			element = free[lastIndex];
			free.RemoveAt(lastIndex);
		} 
		if (element == null) {
			element = Instantiate(prefab.gameObject).GetComponent<Button>();
		}
		used.Add(element);
		element.gameObject.SetActive(true);
		return element;
	}

	private void FreeWithPools(Button element, List<Button> used, List<Button> free) {
		if (!used.Remove(element)) {
			throw new System.Exception("freeing unused element");
		}
		element.gameObject.SetActive(false);
		free.Add(element);
	}

	private void FreeAllElementFromPools(List<Button> used, List<Button> free) {
		used.ForEach(b => b.gameObject.SetActive(false));
		free.AddRange(used);
		used.Clear();
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
		return list;
	}

	[ContextMenu(nameof(RefreshRoot))]
	public void RefreshRoot() {
		List<Transform> list = GetAllRootElements();
		bool expanded = true;
		root = new ElementState(null, null, 0, -1, expanded);
		for(int i = 0; i < list.Count; ++i) {
			ElementState es = new ElementState(root, list[i], 0, i, expanded);
			root.children.Add(es);
			es.AddChildren(expanded);
		}
		//root.CalculateHeight();
		CalculateHierarchy();
		CalcCullBox();
		RefreshUiElements();
	}

	private void RefreshUiElements() {
		ClearAllUi();
		Debug.Log($"cullbox {cullBox}");
		_contentPanelTransform = contentPanel.transform;
		CreateAllChildren(root);
	}
}
