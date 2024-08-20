using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {

	[CreateAssetMenu(fileName = "AutomationAsset", menuName = "ScriptableObjects/AutomationAsset", order = 1)]
	public class AutomationAsset : ScriptableObject {
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

		public void ParseCommands() {
			_command.Parse();
		}

		private void ExecuteCurrentCommand() {
			_executor._currentCommand = _commandInput;
			_executor.ExecuteCurrentCommand();
		}
	}
}
