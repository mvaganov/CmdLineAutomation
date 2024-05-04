using System;

public interface ICmd {
	/// <summary>
	/// Unique command identifier that will prompt the CommandFilter. If null, then CommandFilters will be run in the sequence they are listed.
	/// </summary>
	public string Token { get; }
	/// <param name="command"></param>
	/// <param name="stdOutput"></param>
	/// <returns>null if the command was consumed, or a version of the command if it can be passed on</returns>
	public string CommandFilter(string command, Action<string> stdOutput);
}
