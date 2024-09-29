using RunCmd;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static RunCmd.Parse;

public class ParseObject : MonoBehaviour {
	[System.Serializable]
	public class OptionData {
		public string name;
		public int id;
		public float time;
		public string[] effects;
	}
	public string title;
	public OptionData[] options;

	public string testData =
@"{
	title : 'the data',
	options : [
		{name:'option0',id:10, effects : ['dooption 0', 'advance']},
		{name:'option1',time:3.1415,effects : ['dooption 1', 'advance']},
	]
";

	[ContextMenu(nameof(TestParse))]
	public void TestParse() {
		object self = this;
		TryParse(testData, out Parse.ParseResult resultData, ref self);
	}

	public static bool TryParse(string text, out Parse.ParseResult resultData, ref object parsedObject) {
		object result = ParseText(text, out resultData);
		if (resultData.IsError) {
			Debug.LogWarning(resultData.ToString());
			return false;
		}
		Assign(ref parsedObject, parsedObject.GetType(), result);
		return true;
	}

	public static void Assign(ref object targetObject, Type targetType, object data) {
		bool isString = targetType == typeof(string);
		if (targetType.IsClass && !isString && !targetType.IsArray) {
			bool isPrimitive = targetType.IsPrimitive || isString;
			if (targetObject == null && !isPrimitive) {
				targetObject = Activator.CreateInstance(targetType);
			}
			CompileObject(ref targetObject, data);
		} else if (targetType.IsArray) {
			targetObject = CompileArray(targetType, data);
		} else if (targetType.IsValueType || isString) {
			targetObject = CompilePrimitiveValue(data, targetType);
		} else {
			Debug.LogError($"??? {targetType}");
		}
	}

	private static void CompileObject(ref object targetObject, object data) {
		if (!(data is IDictionary dict)) {
			return;
		}
		foreach (DictionaryEntry kvp in dict) {
			string name = kvp.Key.ToString();
			if (!TryGetValue(targetObject, name, out object value, out Type valueType)) {
				Debug.LogError($"missing {name} in type {targetObject.GetType()}");
			}
			Assign(ref value, valueType, kvp.Value);
			TrySetValue(targetObject, name, value);
		}
	}

	private static object CompilePrimitiveValue(object data, Type targetType) {
		if (data is RunCmd.Parse.Token token) {
			return Convert.ChangeType(token.Text, targetType);
		}
		return Convert.ChangeType(data.ToString(), targetType);
	}

	private static object CompileArray(Type objectType, object data) {
		if (!(data is IList iList)) {
			return null;
		}
		Type elementType = objectType.GetElementType();
		Array arr = Array.CreateInstance(elementType, iList.Count);
		for (int i = 0; i < iList.Count; ++i) {
			object elementData = iList[i];
			object elementObject = null;
			if (elementType.IsValueType || elementType == typeof(string)) {
				elementObject = CompilePrimitiveValue(elementData, elementType);
			} else {
				elementObject = Activator.CreateInstance(elementType);
				Assign(ref elementObject, elementType, elementData);
			}
			arr.SetValue(elementObject, i);
		}
		return arr;
	}

	/// <summary>
	/// Use reflection to get a value by the member name
	/// </summary>
	/// <param name="self"></param>
	/// <param name="memberName"></param>
	/// <param name="memberType"></param>
	/// <returns></returns>
	private static bool TryGetValue(object self, string memberName, out object memberValue, out Type memberType) {
		if (self == null) {
			memberType = typeof(int);
			memberValue = null;
			return false;
		}
		Type type = self.GetType();
		BindingFlags bindFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
		while (type != null) {
			FieldInfo field = type.GetField(memberName, bindFlags);
			if (field != null) {
				memberType = field.FieldType;
				memberValue = field.GetValue(self);
				return true;
			}
			PropertyInfo prop = type.GetProperty(memberName, bindFlags | BindingFlags.IgnoreCase);
			if (prop != null) {
				memberType = prop.PropertyType;
				memberValue = prop.GetValue(self, null);
				return true;
			}
			type = type.BaseType;
		}
		memberType = typeof(byte);
		memberValue = null;
		return false;
	}

	private static bool TrySetValue(object source, string name, object value) {
		if (source == null) { return false; }
		System.Type type = source.GetType();
		BindingFlags bindFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
		while (type != null) {
			FieldInfo field = type.GetField(name, bindFlags);
			if (field != null) {
				field.SetValue(source, value);
			}
			PropertyInfo prop = type.GetProperty(name, bindFlags | BindingFlags.IgnoreCase);
			if (prop != null) {
				prop.SetValue(source, null);
			}
			type = type.BaseType;
		}
		return true;
	}

	public static bool IsList(object o) {
		if (o == null) return false;
		return o is IList &&
					 o.GetType().IsGenericType &&
					 o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
	}
	void Start() {

	}

	// Update is called once per frame
	void Update() {

	}
}
