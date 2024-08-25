namespace RunCmd
{
	public interface ICommandFilter : ICommandProcessor {
		/// <summary>
		/// A command filter receives commands in an explicit sequence, being processed as a
		/// <see cref="ICommandProcessor"/>. The command modifies the input command into an output
		/// command, like a filter. <see cref="FilterResult(object)"/> resolves to the filtered value
		/// when the command is finished executing (<see cref="ICommandProcessor.IsExecutionFinished"/>)
		/// </summary>
		/// <returns>If null, then this filter is considered to have consumed the command. If not null,
		/// the result will continue being processed by subsequent command filters.</returns>
		public string FilterResult(object context);

		/// <summary>
		/// If this command is a branching command, it will have a non-self command result
		/// </summary>
		public ICommandProcessor GetReferencedCommand(object context);
	}
}
