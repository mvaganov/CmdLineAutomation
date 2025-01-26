using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RunCmdRedux {
	public class InterfaceAttributePicker : EditorWindow {
		private class ComponentInspector {
			public readonly Component component;
			public readonly Editor editor;
			public ComponentInspector(Component comp) { component = comp; editor = Editor.CreateEditor(comp); }
			public void Destroy() { DestroyImmediate(editor); }
		}

		private const float SELECT_BUTTON_HEIGHT_PX = 32f;
		private const float LABEL_COLUMN_RATIO = 0.4f;
		private const int EDGE_PADDING_PX = 8;

		private Object[] _targets;
		private string _propertyPath;
		private List<ComponentInspector> _inspectors;
		private Vector2 _scrollPos = Vector2.zero;

		public static bool AnyOpen => HasOpenInstances<InterfaceAttributePicker>();

		public static void Show(SerializedProperty prop, List<Component> components) {
			if (components == null || components.Count == 0 || prop == null) {
				return;
			}
			InterfaceAttributePicker picker = GetWindow<InterfaceAttributePicker>(true);
			picker.AssignAndShow(prop, components);
		}

		private void AssignAndShow(SerializedProperty prop, List<Component> components) {
			_propertyPath = prop.propertyPath;
			_targets = prop.serializedObject.targetObjects;
			_inspectors?.ForEach(mi => mi.Destroy());
			_inspectors = new List<ComponentInspector>();
			string title = components.Count > 0 ? components[0].gameObject.name :
				$"{nameof(InterfaceAttributePicker)}::{nameof(Show)}";
			titleContent = new GUIContent(title);
			components.ForEach(m => _inspectors.Add(new ComponentInspector(m)));
			ShowUtility();
		}

		private void OnGUI() {
			if (_targets == null || _targets.Length == 0) {
				Close();
				return;
			}
			PruneDestroyedComponents();
			DrawAllComponents();
		}

		private void OnDestroy() {
			_inspectors.ForEach(mi => mi.Destroy());
		}

		private void PruneDestroyedComponents() {
			_inspectors.FindAll(m => m.component == null).ForEach(mi => {
				_inspectors.Remove(mi);
				mi.Destroy();
			});
		}

		private void DrawAllComponents() {
			_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, PickerGuiStyle.Window);
			for(int i = 0; i < _inspectors.Count; ++i) {
				ComponentInspector inspector = _inspectors[i];
				EditorGUILayout.Separator();
				EditorGUILayout.BeginVertical(PickerGuiStyle.Inspector);
				DrawHeader(inspector);
				EditorGUILayout.Separator();
				DrawComponent(inspector);
				EditorGUILayout.EndVertical();
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndScrollView();
		}

		private void DrawHeader(ComponentInspector inspector) {
			if (GUILayout.Button($"{inspector.component.GetType().Name}",
			GUILayout.Height(SELECT_BUTTON_HEIGHT_PX))) {
				EditorApplication.delayCall += () => {
					Apply(inspector.component);
					Close();
				};
			}
		}

		private void DrawComponent(ComponentInspector inspector) {
			GUI.enabled = false;
			EditorGUIUtility.labelWidth = position.width * LABEL_COLUMN_RATIO;
			inspector.editor.OnInspectorGUI();
			GUI.enabled = true;
		}

		private void Apply(Component component) {
			for(int i = 0; i < _targets.Length; ++i) {
				Object target = _targets[i];
				SerializedProperty property = new SerializedObject(target).FindProperty(_propertyPath);
				property.objectReferenceValue = component;
				property.serializedObject.ApplyModifiedProperties();
			}
		}

		private static class PickerGuiStyle {
			public static readonly GUIStyle Default, Window, Inspector;
			public static readonly RectOffset padding =
					new RectOffset(EDGE_PADDING_PX, EDGE_PADDING_PX, EDGE_PADDING_PX, EDGE_PADDING_PX);
			static PickerGuiStyle() {
				Default = new GUIStyle();
				Window = new GUIStyle(Default);
				Window.padding = padding;
				Inspector = new GUIStyle(GUI.skin.window);
				Inspector.padding = padding;
			}
		}
	}
}
