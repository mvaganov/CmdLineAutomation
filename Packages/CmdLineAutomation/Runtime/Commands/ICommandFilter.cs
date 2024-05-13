namespace RunCmd
{
	public interface ICommandFilter : ICommandProcessor {
		/// <returns>if null, then this filter is considered to have consumed the command. If not null,
		/// the result will continue being processed by subsequent commands</returns>
		public string FunctionResult(object context);
	}
}
