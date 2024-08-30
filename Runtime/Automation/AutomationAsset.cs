using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {

	[CreateAssetMenu(fileName = "AutomationAsset", menuName = "ScriptableObjects/AutomationAsset", order = 1)]
	public class AutomationAsset : ScriptableObject, ICommandExecutor {
		[SerializeField]
		protected CommandLineSettings _settings;

		/// <summary>
		/// Information about what these commands are for
		/// </summary>
		[ContextMenuItem(nameof(ParseCommands), nameof(ParseCommands))]
		[SerializeField] protected TextCommand _command;
		[SerializeField] protected AutomationExecutor _executor = new AutomationExecutor();
		[ContextMenuItem(nameof(ExecuteCurrentCommand), nameof(ExecuteCurrentCommand))]
		protected string _commandInput;

		public string CommandOutput { get => _executor.CommandOutput; set => _executor.CommandOutput  = value; }
		public string CurrentCommandInput { get => _commandInput; set => _commandInput = value; }

		public IList<ICommandFilter> Filters=> _settings.Filters;

		public AutomationExecutor Executor => _executor;

		public bool IsExecuting => _executor != null && _executor.IsExecuting;

		public bool _progressing;

		public float Progress {
			get {
				float value = Executor.IsExecuting ? Executor.Progress : 1;
				_progressing = value < 1;
				return value;
			}
		}

		public void AddToCommandOutput(string value) {
			_executor.AddToCommandOutput(value);
		}

		public void CancelProcess(object context) {
			throw new System.NotImplementedException();
		}

		public void InsertNextCommandToExecute(object context, string command) {
			throw new System.NotImplementedException();
		}

		public void ParseCommands() {
			_command.Parse();
		}

		public void ExecuteCurrentCommand() {
			_executor._settings = _settings;
			_executor.currentCommandText = _commandInput;
			_executor.source = this;
			_executor.ExecuteCurrentCommand();
		}
	}
}
