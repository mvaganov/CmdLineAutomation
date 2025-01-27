namespace RunCmdRedux {
	public interface ICommandExecutor {
		public string CommandInput { get; set; }

		public string CommandOutput { get; set; }

		public ICommandProcess Process { get; }
	}
}