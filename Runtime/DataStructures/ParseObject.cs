using RunCmd;
using System;
using System.Collections;
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

	public static void Parse<TYPE>(string text, out Parse.ParseResult resultData, ref TYPE parsedObject) {
		object result = ParseText(text, out resultData);
		Debug.Log(resultData.ToString());
		Assign<TYPE>(ref parsedObject, result);

	}
	public static void Assign<TYPE>(ref TYPE parsedObject, object data) {
		Type type = typeof(TYPE);
		RunCmd.Parse.ToString(data);


		if (type.IsClass) {
			Debug.Log($"class {type}");
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

	void Start() {

	}

	// Update is called once per frame
	void Update() {

	}
}
