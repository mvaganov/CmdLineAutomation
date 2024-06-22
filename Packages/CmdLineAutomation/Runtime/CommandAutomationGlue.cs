using UnityEngine;
using UnityEngine.Events;

namespace RunCmd {
	public class CommandAutomationGlue : MonoBehaviour, ICommandAutomation {
		[System.Serializable]
		public class UnityEvent_string : UnityEvent<string> {}

		public CommandAutomation cmdLineAutomation;
		public UnityEvent_string OnOutputChange = new UnityEvent_string();
		private string _outputText;
		private bool _outputTextChanged;

		public CommandAutomation CommandExecutor => cmdLineAutomation;

		void Start() {
			cmdLineAutomation.OnOutputChange += PrintCallback;
		}

		public void PrintCallback(string newText) {
			_outputText = CommandExecutor.CommandOutput;
			_outputTextChanged = true;
		}

		public void ExecuteCommand(string command) {
			Debug.Log($"invoking {cmdLineAutomation.name} '{command}'");
			cmdLineAutomation.RunCommand(command, PrintCallback, this);
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
