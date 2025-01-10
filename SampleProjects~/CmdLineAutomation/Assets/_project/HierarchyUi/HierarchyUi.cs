using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HierarchyUi : MonoBehaviour {
	[System.Serializable]
	public class ElementState {
		[HideInInspector]
		public string name;
		public Transform target;
		public ElementState parent;
		//public HierarchyElement ui;
		public Button label;
		public Button expand; 
		public bool expanded;
		public List<ElementState> children = new List<ElementState>();
		public int column, row, height;

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
				parent.CalculateHeight(ref depth);
			} else {
				CalculateHeight(ref depth);
			}
		}

		public int CalculateHeight() {
			int depth = 0;
			return CalculateHeight(ref depth);
		}

		public int CalculateHeight(ref int maxDepth) {
			height = 1;
			if (children.Count == 0) {
				return height;
			}
			if (expanded) {
				++maxDepth;
				for (int i = 0; i < children.Count; i++) {
					height += children[i].CalculateHeight(ref maxDepth);
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
	public List<HierarchyElement> pool = new List<HierarchyElement>();
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
		root.CalculateHeight(ref depth);
		contentSize.y = root.height * elementHeight;
		contentSize.x = depth * indentWidth + elementWidth;

	}

	private void CreateElement(ElementState es) {
		float elementHeight = prefabElement.GetComponent<RectTransform>().sizeDelta.y;
		float indentWidth = prefabExpand.GetComponent<RectTransform>().sizeDelta.x;
		Vector2 cursor = new Vector2(indentWidth * es.column, elementHeight * es.row);
		Vector2 elementPosition = cursor + Vector2.right * indentWidth;
		GameObject element = Instantiate(prefabElement.gameObject);
		element.transform.SetParent(contentPanel.transform, false);
		element.transform.localPosition = elementPosition;
		if (es.children.Count > 0) {
			GameObject expand = Instantiate(prefabExpand.gameObject);
			expand.transform.SetParent(expand.transform, false);
			expand.transform.localPosition = cursor;
		}
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
			if (t != null && t.parent == null) {
				list.Add(t);
			}
		}
		return list;
	}

	[ContextMenu(nameof(RefreshRoot))]
	public void RefreshRoot() {
		List<Transform> list = GetAllElements();
		bool expanded = true;
		root = new ElementState(null, null, 0, 0, expanded);
		for(int i = 0; i < list.Count; ++i) {
			ElementState es = new ElementState(root, list[i], 0, i, expanded);
			root.children.Add(es);
			es.AddChildren(expanded);
		}
		root.CalculateHeight();
	}
}
