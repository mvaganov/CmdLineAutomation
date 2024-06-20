using UnityEngine;
using UnityEngine.Events;

namespace RunCmd {
	public class CommandAutomationGlue : MonoBehaviour {
		[System.Serializable] public class UnityEvent_string : UnityEvent<string> { }

		public CommandAutomation cmdLineAutomation;
		public UnityEvent_string OnOutputChange = new UnityEvent_string();
		private string _outputText;
		private bool _outputTextChanged;
		
		void Start() {
			cmdLineAutomation.OnOutputChange += DoOnOutputChange;
		}

		public void DoOnOutputChange(string newText) {
			_outputText = newText;
			_outputTextChanged = true;
		}

		public void ExecuteCommand(string command) {
			Debug.Log($"invoking {cmdLineAutomation.name} '{command}'");
			cmdLineAutomation.ExecuteCommand(command, this, DoOnOutputChange);
		}

		private void Update() {
			if (!_outputTextChanged) {
				return;
			}
			OnOutputChange.Invoke(_outputText);
			_outputTextChanged = false;
		}
	}
}
