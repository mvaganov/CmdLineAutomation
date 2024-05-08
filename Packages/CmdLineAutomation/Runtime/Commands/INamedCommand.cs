public interface INamedCommand : ICommandProcessor {
	/// <summary>
	/// Unique command identifier that will prompt the command, not always run as a filter.
	/// </summary>
	public string CommandToken { get; }
}
