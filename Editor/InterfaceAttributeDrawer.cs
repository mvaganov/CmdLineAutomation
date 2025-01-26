using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace RunCmdRedux {
	[CustomPropertyDrawer(typeof(InterfaceAttribute))]
	public class InterfaceAttributeDrawer : PropertyDrawer {
		private int _controlID;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			_controlID = GUIUtility.GetControlID(FocusType.Passive);
			if (property.propertyType != SerializedPropertyType.ObjectReference) {
				EditorGUI.LabelField(position, label.text, "InterfaceAttribute is for Object fields only");
				return;
			}
			EditorGUI.BeginChangeCheck();
			EditorGUI.BeginProperty(position, label, property);
			Type interfaceType = GetInterface(property, (InterfaceAttribute)attribute);
			Object candidateObject = DrawObjectInputField(interfaceType, property, position, label);
			Object resolved = ResolveCandidate(candidateObject, interfaceType, property);
			if ((candidateObject == null || resolved != null)
			&& property.objectReferenceValue != resolved) {
				property.objectReferenceValue = resolved;
			}
			if (EditorGUI.EndChangeCheck()) {
				property.serializedObject.ApplyModifiedProperties();
			}
			EditorGUI.EndProperty();
		}

		private static Object UseInterfacePicker(GameObject gameObject, Type interfaceType,
		SerializedProperty property) {
			List<Component> options = new List<Component>();
			MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();
			for (int i = 0; i < behaviours.Length; ++i) {
				MonoBehaviour mb = behaviours[i];
				if (!IsAssignableTo(mb.GetType(), interfaceType)) {
					continue;
				}
				options.Add(behaviours[i]);
			}
			if (options.Count > 1) {
				EditorApplication.delayCall += () => InterfaceAttributePicker.Show(property, options);
			} else {
				return options.Count == 1 ? options[0] : null;
			}
			return null;
		}

		private static GameObject EnsureReferenceIsntNull(ref Object refrence, Type type) {
			GameObject tempGameObject = null;
			if (Event.current.type == EventType.Repaint && refrence == null) {
				string newObjectName = $"temp ({type.Name})";
				refrence = tempGameObject = GenerateTemporaryObject(newObjectName);
			}
			return tempGameObject;
		}

		private static GameObject GenerateTemporaryObject(string objectName) {
			GameObject tempGameObject = new GameObject(objectName);
			tempGameObject.hideFlags = HideFlags.HideAndDontSave;
			return tempGameObject;
		}

		private Object DrawObjectInputField(Type interfaceType,
		SerializedProperty property, Rect position, GUIContent label) {
			Object oldObject = property.objectReferenceValue;
			GameObject tempGameObject = EnsureReferenceIsntNull(ref oldObject, interfaceType);
			Object candidateObject =
				EditorGUI.ObjectField(position, label, oldObject, typeof(Object), true);
			ReplaceObjectPickerForControl(interfaceType);
			if (Event.current.commandName == "ObjectSelectorUpdated"
			&& EditorGUIUtility.GetObjectPickerControlID() == _controlID) {
				candidateObject = EditorGUIUtility.GetObjectPickerObject();
			}
			if (tempGameObject != null) {
				GameObject.DestroyImmediate(tempGameObject);
			}
			return candidateObject;
		}

		private static Object ResolveCandidate(Object candidateObject, Type interfaceType,
		SerializedProperty property) {
			if (candidateObject == null) {
				return null;
			}
			if (IsAssignableTo(candidateObject.GetType(), interfaceType)) {
				return candidateObject;
			} else if (candidateObject is GameObject gameObject) {
				return UseInterfacePicker(gameObject, interfaceType, property);
			}
			throw new NotImplementedException($"Unable to resolve {candidateObject}");
		}

		private static Type GetInterface(SerializedProperty prop, InterfaceAttribute attribute) {
			Type result = attribute.InterfaceType;
			if (!String.IsNullOrEmpty(attribute.InferTypeFromFieldName)) {
				Type type = prop.serializedObject.targetObject.GetType();
				while (type != null) {
					FieldInfo referredFieldInfo = type.GetField(attribute.InferTypeFromFieldName,
							BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					if (referredFieldInfo != null) {
						result = referredFieldInfo.FieldType;
						break;
					}
					PropertyInfo referredPropertyInfo = type.GetProperty(attribute.InferTypeFromFieldName,
							BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					if (referredPropertyInfo != null) {
						result = referredPropertyInfo.PropertyType;
						break;
					}
					type = type.BaseType;
				}
			}
			return result != null ? result : typeof(MonoBehaviour);
		}

		void ReplaceObjectPickerForControl(Type interfaceType) {
			var currentObjectPickerID = EditorGUIUtility.GetObjectPickerControlID();
			if (currentObjectPickerID != _controlID) {
				return;
			}
			// start filter with long empty area to allow for easy clicking and typing
			var filterBuilder = new StringBuilder("                       ");
			if (interfaceType.IsGenericType) {
				return;
			}
			filterBuilder.Append($"t:{interfaceType.FullName} ");
			string filter = filterBuilder.ToString();
			EditorGUIUtility.ShowObjectPicker<Component>(null, true, filter, _controlID);
		}

		private static bool IsAssignableTo(Type fromType, Type toType) {
			if (toType.IsGenericType && toType.IsGenericTypeDefinition) {
				return IsAssignableToGenericType(fromType, toType);
			}
			return toType.IsAssignableFrom(fromType);
		}

		private static bool IsAssignableToGenericType(Type fromType, Type toType) {
			Type[] interfaceTypes = fromType.GetInterfaces();
			foreach (var it in interfaceTypes) {
				if (it.IsGenericType && it.GetGenericTypeDefinition() == toType) {
					return true;
				}
			}
			if (fromType.IsGenericType && fromType.GetGenericTypeDefinition() == toType) {
				return true;
			}
			Type baseType = fromType.BaseType;
			if (baseType == null) {
				return false;
			}
			return IsAssignableToGenericType(baseType, toType);
		}
	}
}
