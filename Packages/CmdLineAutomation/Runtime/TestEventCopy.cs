using System.Collections;
using System.Collections.Generic;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

public class TestEventCopy : MonoBehaviour
{
	public UnityEvent e1;
	public UnityEvent e2;
	[ContextMenuItem(nameof(CopyTest), nameof(CopyTest))]
	[ContextMenuItem(nameof(SetTest), nameof(SetTest))]
	public bool test;
	public void CopyTest() {
		UnityEventUtil.CopyUnityEvents(this, nameof(e1), e2, true);
	}

	public void Hey(Transform t) {
		Debug.Log(t);
	}

	public void SetTest() {
		//var obj_execute = System.Delegate.CreateDelegate(typeof(UnityAction<Transform>), this, "Hey") as UnityAction<Transform>;
		var obj_execute = System.Delegate.CreateDelegate(typeof(UnityAction<Transform>), transform, "set_parent") as UnityAction<Transform>;
		Debug.Log(obj_execute);
		UnityEventTools.AddObjectPersistentListener(
			e2 as UnityEventBase,
			obj_execute,
			transform
		);
	}
}
