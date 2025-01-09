using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HierarchyUi : MonoBehaviour {
	[System.Serializable]
	public class ElementState {
		[HideInInspector]
		public string name;
		public Transform target;
		public HierarchyElement ui;
		public bool expanded;
		public List<ElementState> children = new List<ElementState>();

		public ElementState(Transform target) {
			this.target = target;
			this.name = (target != null) ? target.name : "";
		}

		public void AddChildren() {
			for(int i = 0; i <target.childCount; ++i) {
				Transform t =	target.GetChild(i);
				if (t == null) {
					continue;
				}
				ElementState es = new ElementState(t);
				children.Add(es);
				es.AddChildren();
			}
		}
	}
	public HierarchyElement prefab;
	public List<HierarchyElement> pool = new List<HierarchyElement>();

	public ElementState root;
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
		root = new ElementState(null);
		for(int i = 0; i < list.Count; ++i) {
			ElementState es = new ElementState(list[i]);
			root.children.Add(es);
			es.AddChildren();
		}
	}
}
