using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
	private bool _expanded;
	public List<ElementState> children = new List<ElementState>();
	public Button Label {
		get => _label;
		set {
			_label = value;
		}
	}
	public Button Expand {
		get => _expand;
		set {
			_expand = value;
		}
	}
	public bool Expanded {
		get => _expanded;
		set {
			if (value != _expanded) {
				_expanded = value;
				RefreshHeight();
			}
			TMP_Text txt = _expand.GetComponentInChildren<TMP_Text>();
			txt.text = _expand ? "v" : ">";
		}
	}

	public ElementState(ElementState parent, Transform target, int column, int row, bool expanded) {
		this.target = target;
		this.parent = parent;
		this.column = column;
		this.row = row;
		_expanded = expanded;
		name = (target != null) ? target.name : "";
	}

	public void DoExpand() {
		_expanded = true;
		RefreshHeight();
	}

	public void DoCollapse() {
		_expanded = false;
		RefreshHeight();
	}

	public void RefreshHeight() {
		int depth = 0;
		GetRoot().CalculateHeight(0, ref depth);
	}

	private ElementState GetRoot() {
		ElementState root = this;
		int loopguard = 0;
		while (root.parent != null) {
			root = root.parent;
			if (++loopguard > 100000) {
				throw new System.Exception("max depth reached. find recursion?");
			}
		}
		return root;
	}

	public int CalculateHeight(int depth, ref int maxDepth) {
		if (depth > maxDepth) {
			maxDepth = depth;
		}
		height = target != null ? 1 : 0;
		if (children.Count == 0) {
			return height;
		}
		if (_expanded) {
			int rowCursor = row + height;
			for (int i = 0; i < children.Count; i++) {
				children[i].row = rowCursor;
				int elementHeight = children[i].CalculateHeight(depth + 1, ref maxDepth);
				height += elementHeight;
				rowCursor += elementHeight;
			}
		}
		return height;
	}

	public void AddChildren(bool expanded) {
		int c = column + 1;
		int r = row + 1;
		for (int i = 0; i < target.childCount; ++i) {
			Transform t = target.GetChild(i);
			if (t == null || t.GetComponent<HierarchyIgnore>() != null) {
				continue;
			}
			ElementState es = new ElementState(this, t, c, r, expanded);
			children.Add(es);
			es.AddChildren(expanded);
		}
	}
}
