using System;

public interface ICmd {
	/// <summary>
	/// Unique command identifier that will prompt the CommandFilter. If null, then CommandFilters will be run in the sequence they are listed.
	/// </summary>
	public string Token { get; }
	/// <param name="context">What is executing this command</param>
	/// <param name="command">The command being executed</param>
	/// <param name="stdOutput">Where the results of this command will go, one line at a time</param>
	/// <returns>null if the command was consumed, or a version of the command if it can be passed on</returns>
	public string CommandFilter(object context, string command, Action<string> stdOutput);
}
