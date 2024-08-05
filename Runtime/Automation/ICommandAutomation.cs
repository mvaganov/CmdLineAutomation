using System.Collections.Generic;

namespace RunCmd {
	public interface ICommandAutomation {
		public ICommandExecutor CommandExecutor { get; }
	}


	// TODO merge into CommandProcessor?
	public interface ICommandExecutor {
		/// <summary>
		/// Command output
		/// </summary>
		public string CommandOutput { get; set; }

		/// <summary>
		/// Allows insertion of another command immediately after this one, modifying the expected queue of instructions.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="command"></param>
		public void InsertNextCommandToExecute(object context, string command);

		/// <summary>
		/// Adds to CommandOutput using any relevant filters
		/// </summary>
		/// <param name="value"></param>
		public void AddToCommandOutput(string value);

		/// <summary>
		/// The execution filters to apply each command through
		/// </summary>
		IList<ICommandFilter> Filters { get; }

		public void CancelProcess(object context);
	}
}
