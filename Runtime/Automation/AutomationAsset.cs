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
		[SerializeField] protected string _commandInput;
		[SerializeField] protected string _commandOutput;

		public string CommandOutput { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

		public IList<ICommandFilter> Filters=> _settings.Filters;


		public void AddToCommandOutput(string value) {
			_commandOutput += value;
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

		private void ExecuteCurrentCommand() {
			_executor._settings = _settings;
			_executor.currentCommandText = _commandInput;
			_executor.source = this;
			_executor.ExecuteCurrentCommand();
		}
	}
}
