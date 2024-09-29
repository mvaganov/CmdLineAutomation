using RunCmd;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using static RunCmd.Parse;

public class ParseObject : MonoBehaviour {
	[System.Serializable]
	public class OptionData {
		public string name;
		public string[] effects;
	}
	public string title;
	public OptionData[] options;

	public string testData =
@"{
	title : 'the data',
	options : [
		{name:'option0',effects : ['dooption 0', 'advance']},
		{name:'option1',effects : ['dooption 1', 'advance']},
	]
";

	[ContextMenu(nameof(TestParse))]
	public void TestParse() {
		object self = this;
		Parse(testData, out Parse.ParseResult resultData, ref self);
		Debug.Log(resultData.ToString());
	}

	public static void Parse(string text, out Parse.ParseResult resultData, ref object parsedObject) {
		object result = ParseText(text, out resultData);
		Debug.Log(resultData.ToString());
		Assign(ref parsedObject, parsedObject.GetType(), result);

	}
	public static void Assign(ref object parsedObject, Type objectType, object data) {
		//Type type = parsedObject.GetType();
		RunCmd.Parse.ToString(data);

		bool isString = objectType == typeof(string);

		if (objectType.IsClass && !isString) {
			if (data is IDictionary dict) {
				foreach(DictionaryEntry kvp in dict) {
					string name = kvp.Key.ToString();
					Debug.Log(kvp.Key);
					Debug.Log($"getting '{name}' from {parsedObject}");
					object value = GetValue(parsedObject, name, out Type valueType);
					Assign(ref value, valueType, kvp.Value);
					Debug.Log($"setting {name} = {value}");
					SetValue(parsedObject, name, value);
				}
			} else {
					Debug.LogError($"can't create class {objectType} with {data}");
				if (objectType.IsArray) {
					IList iList = data as IList;
					Debug.LogWarning("ARRAY! " + IsList(data));
					if (iList != null) {
						Debug.LogWarning(iList.Count);
						Type arrayElementType = objectType.GetElementType();
						Debug.LogWarning(arrayElementType);
						Array arr = Array.CreateInstance(arrayElementType, iList.Count);
						for(int i = 0; i < iList.Count; ++i) {
							object elementData = iList[i];
							object element = Activator.CreateInstance(arrayElementType);
							arr.SetValue(element, i);
							Assign(ref element, arrayElementType, elementData);
							arr.SetValue(element, i);
						}
						parsedObject = arr;
					}
				}
			}
		}
		else if (objectType.IsArray) {
			Debug.Log($"array {objectType}");
		} else if (objectType.IsValueType || isString) {
			if (data is RunCmd.Parse.Token token) {
				parsedObject = Convert.ChangeType(token.Text, objectType);
			} else {
				Debug.LogError($"unable to set {objectType} to {data}");
			}
		} else {
			Debug.Log($"??? {objectType}");
		}

		switch (data) {
			case RunCmd.Parse.Token token:
				Debug.Log($"set {parsedObject} to {token}");
				break;
			case IList list:
				Debug.Log($"set {parsedObject} to {list.Count} elements");
				break;
			case IDictionary dict:
				Debug.Log($"set {parsedObject} to {dict.Count} values");
				break;
		}
	}

	private static object GetValue(object source, string name, out Type resultType) {
		if (source == null) { resultType = typeof(int); return null; }
		System.Type type = source.GetType();
		BindingFlags bindFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
		while (type != null) {
			FieldInfo field = type.GetField(name, bindFlags);
			if (field != null) { resultType = field.FieldType; return field.GetValue(source); }
			PropertyInfo prop = type.GetProperty(name, bindFlags | BindingFlags.IgnoreCase);
			if (prop != null) { resultType = prop.PropertyType; return prop.GetValue(source, null); }
			type = type.BaseType;
		}
		resultType = typeof(byte);
		return null;
	}

	private static void SetValue(object source, string name, object value) {
		if (source == null) { return; }
		System.Type type = source.GetType();
		BindingFlags bindFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
		while (type != null) {
			FieldInfo field = type.GetField(name, bindFlags);
			if (field != null) { field.SetValue(source, value); }
			PropertyInfo prop = type.GetProperty(name, bindFlags | BindingFlags.IgnoreCase);
			if (prop != null) { prop.SetValue(source, null); }
			type = type.BaseType;
		}
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
