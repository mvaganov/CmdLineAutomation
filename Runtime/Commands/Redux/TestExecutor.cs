using UnityEngine;

namespace RunCmdRedux {
	/// <summary>
	/// Wrapper around a <see cref="ICommandProcess"/> that helps keep track of it's state
	/// </summary>
	public class TestExecutor : ICommandProcessReference {
		[SerializeField] private string _executedCommandText;
		[SerializeField] private string _executedCommandOutput;
		private ICommandProcess _commandProcess;
		private bool _startedCommand;
		public string CurrentInput { get => _executedCommandText; set => _executedCommandText = value; }

		public string CurrentOutput { get => _executedCommandOutput; set => _executedCommandOutput = value; }

		public ICommandProcess Process {
			get => _commandProcess;
			set {
				_startedCommand = false;
				_commandProcess = value;
			}
		}

		public bool IsExecuting => _commandProcess != null && _startedCommand;

		public void AddToCommandOutput(string value) {
			CurrentOutput += value;
		}

		public void Execute(ICommandProcess process, string command) {
			Process = process;
			ExecuteCommand(command);
		}

		public void ExecuteCommand(string command) {
			CurrentInput = command;
			ExecuteCurrentCommand();
		}

		public void ExecuteCurrentCommand() {
			//OutputAnalysis($"executing \"{CurrentInput}\" -> ReferencedCommand\n");
			_startedCommand = true;
			Process.StartCooperativeFunction(CurrentInput, OutputAnalysis);
		}

		private void OutputAnalysis(string fromProcess) {
			AddToCommandOutput(fromProcess); // this is where the printing happens.
		}
	}
}
