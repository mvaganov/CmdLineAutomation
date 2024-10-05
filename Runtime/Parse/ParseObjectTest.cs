using RunCmd;
using UnityEngine;

public class ParseObjectTest : MonoBehaviour
{
	[System.Serializable]
	public class OptionData {
		public string name;
		public int id;
		public float time;
		public string[] effects;
	}
	public string title;
	public OptionData[] options;

	[TextArea(1,20)]
	public string testData =
@"{
	title : 'the data',
	options : [
		{name:'option0',id:10, effects : ['dooption 0', 'advance']},
		{name:'option1',time:3.1415,effects : ['dooption 1', 'advance']},
	]
}";

	[ContextMenu(nameof(TestParse))]
	public void TestParse() {
		object self = this;
		Parse.Object.TryParse(ref self, testData, out Parse.ParseResult resultData);
	}
}