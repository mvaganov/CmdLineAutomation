using RunCmd;
using System;
using System.Collections;
using System.Reflection;
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
		Assign(ref parsedObject, result);

	}
	public static void Assign(ref object parsedObject, object data) {
		Type type = parsedObject.GetType();
		RunCmd.Parse.ToString(data);

		if (type.IsClass) {
			if (data is IDictionary dict) {
				foreach(DictionaryEntry kvp in dict) {
					string name = kvp.Key.ToString();
					Debug.Log(kvp.Key);
					Debug.Log($"getting '{name}' from {parsedObject}");
					object value = GetValue(parsedObject, name);
					Assign(ref value, kvp.Value);
					Debug.Log($"setting {name} = {value}");
					SetValue(parsedObject, name, value);
				}
			} else {
				Debug.Log($"can't create class {type} with {data}");
			}
		}
		else if (type.IsArray) {
			Debug.Log($"array {type}");
		} else if (type.IsValueType) {
			Debug.Log($"value {type}");
		} else {
			Debug.Log($"??? {type}");
		}

		switch (data) {
			case Token token:
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

	private static object GetValue(object source, string name) {
		if (source == null) { return null; }
		System.Type type = source.GetType();
		BindingFlags bindFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
		while (type != null) {
			FieldInfo finfo = type.GetField(name, bindFlags);
			if (finfo != null) { return finfo.GetValue(source); }
			PropertyInfo pinfo = type.GetProperty(name, bindFlags | BindingFlags.IgnoreCase);
			if (pinfo != null) { return pinfo.GetValue(source, null); }
			type = type.BaseType;
		}
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

	void Start() {

	}

	// Update is called once per frame
	void Update() {

	}
}
