using System;

namespace RunCmdRedux {
	[Serializable]
	public class KeyValuePairStrings {
		public string Key, Value;
		public KeyValuePairStrings(string key, string value) { Key = key; Value = value; }
	}
}
