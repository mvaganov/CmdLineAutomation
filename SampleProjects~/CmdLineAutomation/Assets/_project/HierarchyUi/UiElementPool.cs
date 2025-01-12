using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable] public class ButtonPool : UiElementPool<Button> { }

public class UiElementPool<TYPE> where TYPE : Component
{
	public List<TYPE> used = new List<TYPE>();
	public List<TYPE> free = new List<TYPE>();

	public TYPE GetFreeFromPools(TYPE prefab) {
		TYPE element = null;
		while (free.Count > 0 && element == null) {
			int lastIndex = free.Count - 1;
			element = free[lastIndex];
			free.RemoveAt(lastIndex);
		}
		if (element == null) {
			element = GameObject.Instantiate(prefab.gameObject).GetComponent<TYPE>();
		}
		used.Add(element);
		element.gameObject.SetActive(true);
		return element;
	}

	public void FreeWithPools(TYPE element) {
		if (!used.Remove(element)) {
			throw new System.Exception("freeing unused element");
		}
		element.gameObject.SetActive(false);
		free.Add(element);
	}

	public void FreeAllElementFromPools() {
		used.ForEach(b => {
			if (b != null) {
				b.gameObject.SetActive(false);
			}
		});
		free.AddRange(used);
		used.Clear();
	}
}
