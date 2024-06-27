using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TestEventCopy : MonoBehaviour
{
  public UnityEvent e1;
  public UnityEvent e2;
  [ContextMenuItem(nameof(CopyTest), nameof(CopyTest))]
  public bool test;
  public void CopyTest() {
    UnityEventUtil.CopyUnityEvents(this, nameof(e1), e2, true);
	}
}
