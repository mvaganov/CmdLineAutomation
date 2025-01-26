using RunCmdRedux;
using System.Collections.Generic;

namespace RunCmd {
	public interface ICommandAutomation {
		public ICommandExecutor CommandExecutor { get; }
	}

	public interface ICommandReference {
		public ICommandProcessor ReferencedCommand { get; }
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
namespace RunCmdRedux {
	// redux
	public interface ICommandAssetAutomation {
		public ICommandAssetExecutor CommandExecutor { get; }
	}

	// redux
	public interface ICommandProcessReference {
		public ICommandProcess Process { get; }
	}

	// redux
	public interface ICommandAssetExecutor {
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
		/// The command assets to apply each command through
		/// </summary>
		IList<ICommandAsset> CommandAssets { get; }

		public void CancelProcess(object context);
	}

}
