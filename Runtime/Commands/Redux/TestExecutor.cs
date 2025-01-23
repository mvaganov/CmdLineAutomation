using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmdRedux {
	// redux
	[System.Serializable]
	public class TestExecutor : ICommandProcessReference {
		public string commandText;
		private object _context;
		private ICommandProcess commandProcess;
		public string CommandOutput { get; set; }

		public ICommandProcess ReferencedCommand {
			get => commandProcess;
			set => commandProcess = value;
		}

		public bool IsExecuting { get; set; }

		public void AddToCommandOutput(string value) {
			CommandOutput += value;
		}

		public void CancelProcess(object context) {
			_context = null;
		}

		public string CurrentCommandText { get => commandText; set => commandText = value; }

		public void ExecuteCurrentCommand() {
			Debug.Log($"executing \"{CurrentCommandText}\" -> {ReferencedCommand}");
			OutputAnalysis($"executing \"{CurrentCommandText}\" -> ReferencedCommand\n");
			// TODO TODO TODO do me next
			// TODO go through filters, including the filter that finds named commands
			//if (_settings.NeedsInitialization()) {
			//}

			// TODO test me out!
			ReferencedCommand.StartCooperativeFunction(CurrentCommandText, OutputAnalysis);

			//StartCooperativeFunction(currentCommandText, print);
		}

		private void OutputAnalysis(string fromProcess) {
			Debug.Log("!!!!!!!!!!!! "+fromProcess);
			AddToCommandOutput(fromProcess); // this is where the printing happens.
		}
	}
}
