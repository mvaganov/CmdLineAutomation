using System;

namespace RunCmd {
	/// <summary>
	/// Processing logic to respond to a string command input. The main method is a non-blocking
	/// cooperative-multithreaded function. This is a base command processing interface.
	/// </summary>
	public interface ICommandProcessor {
		/// <summary>
		/// Event handling function, which starts a command. The command line system uses a cooperative
		/// threading model, with status retrieved by <see cref="IsExecutionFinished"/> and
		/// <see cref="FunctionResult"/>
		/// </summary>
		/// <param name="context">What is executing this command</param>
		/// <param name="command">The command being executed</param>
		/// <param name="stdOutput">Where the results of this command will go, one line at a time</param>
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput);
		/// <returns>true when the command is finished</returns>
		public bool IsExecutionFinished();
	}

	public interface ICommandFilter : ICommandProcessor {
		/// <returns>if null, then this filter is considered to have consumed the command. If not null,
		/// the result will continue being processed by subsequent commands</returns>
		public string FunctionResult();
	}

	/// <summary>
	/// Callback for receiving command output
	/// </summary>
	/// <param name="text">standard output in text form</param>
	public delegate void TextResultCallback(string text);
}
