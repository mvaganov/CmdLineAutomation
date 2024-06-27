using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

public static class UnityEventUtil
{
  const BindingFlags allBindings = BindingFlags.Instance | BindingFlags.Public;
  public static void CopyUnityEvents(object sourceObj, string source_UnityEvent, object dest, bool debug = false) {
    FieldInfo unityEvent = sourceObj.GetType().GetField(source_UnityEvent, allBindings);
    if (unityEvent.FieldType != dest.GetType()) {
      if (debug == true) {
        Debug.Log("Source Type: " + unityEvent.FieldType);
        Debug.Log("Dest Type: " + dest.GetType());
        Debug.Log("CopyUnityEvents - Source & Dest types don't match, exiting.");
      }
      return;
    } else {
      SerializedObject so = new SerializedObject((Object)sourceObj);
      SerializedProperty persistentCalls = so.FindProperty(source_UnityEvent).FindPropertyRelative("m_PersistentCalls.m_Calls");
      for (int i = 0; i < persistentCalls.arraySize; ++i) {
        Object target = persistentCalls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Target").objectReferenceValue;
        string methodName = persistentCalls.GetArrayElementAtIndex(i).FindPropertyRelative("m_MethodName").stringValue;
        MethodInfo method = null;
        try {
          method = target.GetType().GetMethod(methodName, allBindings);
        } catch {
          foreach (MethodInfo info in target.GetType().GetMethods(allBindings).Where(x => x.Name == methodName)) {
            ParameterInfo[] _params = info.GetParameters();
            if (_params.Length < 2) {
              method = info;
            }
          }
        }
        ParameterInfo[] parameters = method.GetParameters();
        switch (parameters[0].ParameterType.Name) {
          case nameof(System.Boolean):
            bool bool_value = persistentCalls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue;
            var bool_execute = System.Delegate.CreateDelegate(typeof(UnityAction<bool>), target, methodName) as UnityAction<bool>;
            UnityEventTools.AddBoolPersistentListener(
                dest as UnityEventBase,
                bool_execute,
                bool_value
            );
            break;
          case nameof(System.Int32):
            int int_value = persistentCalls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Arguments.m_IntArgument").intValue;
            var int_execute = System.Delegate.CreateDelegate(typeof(UnityAction<int>), target, methodName) as UnityAction<int>;
            UnityEventTools.AddIntPersistentListener(
                dest as UnityEventBase,
                int_execute,
                int_value
            );
            break;
          case nameof(System.Single):
            float float_value = persistentCalls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Arguments.m_FloatArgument").floatValue;
            var float_execute = System.Delegate.CreateDelegate(typeof(UnityAction<float>), target, methodName) as UnityAction<float>;
            UnityEventTools.AddFloatPersistentListener(
                dest as UnityEventBase,
                float_execute,
                float_value
            );
            break;
          case nameof(System.String):
            string str_value = persistentCalls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Arguments.m_StringArgument").stringValue;
            var str_execute = System.Delegate.CreateDelegate(typeof(UnityAction<string>), target, methodName) as UnityAction<string>;
            UnityEventTools.AddStringPersistentListener(
                dest as UnityEventBase,
                str_execute,
                str_value
            );
            break;
          case nameof(System.Object):
            Object obj_value = persistentCalls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Arguments.m_ObjectArgument").objectReferenceValue;
            var obj_execute = System.Delegate.CreateDelegate(typeof(UnityAction<Object>), target, methodName) as UnityAction<Object>;
            UnityEventTools.AddObjectPersistentListener(
                dest as UnityEventBase,
                obj_execute,
                obj_value
            );
            break;
          default:
            Debug.Log(method.Name+" : "+parameters[0].ParameterType.Name);
            foreach(var a in persistentCalls.GetArrayElementAtIndex(i)) {
              SerializedProperty sp = a as SerializedProperty;
              Debug.Log(sp.type+ " '" + sp.displayName + "' : " + sp.hasChildren + " " + sp.name);
						}
            //Object obj = persistentCalls.GetArrayElementAtIndex(i).//.FindPropertyRelative("m_Arguments").objectReferenceValue;
            var void_execute = System.Delegate.CreateDelegate(typeof(UnityAction), target, methodName) as UnityAction;
            UnityEventTools.AddPersistentListener(
                dest as UnityEvent,
                void_execute
            );
            break;
        }
      }
    }
  }
}
