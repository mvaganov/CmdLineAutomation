using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HierarchyUi : MonoBehaviour {
	[System.Serializable]
	public class ElementState {
		[HideInInspector]
		public string name;
		public int column, row, height;
		public ElementState parent;
		[HideInInspector]
		public Transform target;
		//public HierarchyElement ui;
		[HideInInspector]
		public Button label;
		[HideInInspector]
		public Button expand; 
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

		public void Expand() {
			expanded = true;
			RefreshHeight();
		}

		public void Collapse() {
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

		public int CalculateHeight() {
			int depth = 0;
			return CalculateHeight(0, ref depth);
		}

		public int CalculateHeight(int depth, ref int maxDepth) {
			if (depth > maxDepth) {
				maxDepth = depth;
			}
			height = parent != null ? 1 : 0;
			if (children.Count == 0) {
				return height;
			}
			if (expanded) {
				int r = row + 1;
				for (int i = 0; i < children.Count; i++) {
					children[i].row = r;
					int elementHeight = children[i].CalculateHeight(depth+1, ref maxDepth);
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
				if (t == null) {
					continue;
				}
				ElementState es = new ElementState(this, t, c, r, expanded);
				children.Add(es);
				es.AddChildren(expanded);
			}
		}
	}
	public HierarchyElement prefab;
	public ContentSizeFitter contentPanel;
	public Vector2 contentSize;
	public List<GameObject> pool = new List<GameObject>();
	public Button prefabElement;
	public Button prefabExpand;

	public ElementState root;

	private void OnValidate() {
		RectTransform rt = contentPanel.GetComponent<RectTransform>();
		rt.sizeDelta = contentSize;
	}

	[ContextMenu(nameof(CalculateHierarchy))]
	private void CalculateHierarchy() {
		float elementHeight = prefabElement.GetComponent<RectTransform>().sizeDelta.y;
		float elementWidth = prefabElement.GetComponent<RectTransform>().sizeDelta.x;
		float indentWidth = prefabExpand.GetComponent<RectTransform>().sizeDelta.x;
		int depth = 0;
		root.CalculateHeight(0, ref depth);
		contentSize.y = root.height * elementHeight;
		contentSize.x = depth * indentWidth + elementWidth;
		RectTransform rt = contentPanel.GetComponent<RectTransform>();
		rt.sizeDelta = contentSize;
		ClearAllUi();
		CreateAllChildren(root);
	}

	private void ClearAllUi() {
		pool.ForEach(e => DestroyImmediate(e));
		pool.Clear();
	}

	private void CreateAllChildren(ElementState es) {
		for (int i = 0; i < es.children.Count; i++) {
			ElementState child = es.children[i];
			Debug.Log($"{i} creating {child.name}");
			CreateElement(child);
			CreateAllChildren(child);
		}
	}

	private void CreateElement(ElementState es) {
		float elementHeight = prefabElement.GetComponent<RectTransform>().sizeDelta.y;
		float indentWidth = prefabExpand.GetComponent<RectTransform>().sizeDelta.x;
		Vector2 cursor = new Vector2(indentWidth * es.column, -elementHeight * es.row);
		Vector2 elementPosition = cursor + Vector2.right * indentWidth;
		Transform content = contentPanel.transform;
		RectTransform rt;
		if (es.children.Count > 0) {
			GameObject expand = Instantiate(prefabExpand.gameObject);
			rt = expand.GetComponent<RectTransform>();
			rt.SetParent(content, false);
			rt.anchoredPosition = cursor;
			rt.name = $"> {es.name}";
			es.expand = expand.GetComponent<Button>();
			expand.gameObject.SetActive(true);
			pool.Add(expand);
		}
		GameObject element = Instantiate(prefabElement.gameObject);
		rt = element.GetComponent<RectTransform>();
		rt.SetParent(content, false);
		rt.anchoredPosition = elementPosition;
		rt.name = $"({es.name})";
		es.label = element.GetComponent<Button>();
		element.gameObject.SetActive(true);
		TMP_Text text = element.GetComponentInChildren<TMP_Text>();
		text.text = es.name;
		pool.Add(element);
	}

	void Start() {

	}

	// Update is called once per frame
	void Update() {

	}

	public static List<Transform> GetAllElements() {
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
		List<Transform> list = GetAllElements();
		bool expanded = true;
		root = new ElementState(null, null, 0, -1, expanded);
		for(int i = 0; i < list.Count; ++i) {
			ElementState es = new ElementState(root, list[i], 0, i, expanded);
			root.children.Add(es);
			es.AddChildren(expanded);
		}
		//root.CalculateHeight();
		CalculateHierarchy();
	}
}
