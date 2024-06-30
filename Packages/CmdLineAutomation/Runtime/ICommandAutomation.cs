namespace RunCmd {
	public interface ICommandAutomation {
		public ICommandExecutor CommandExecutor { get; }
	}

	public interface ICommandExecutor {
		public string CommandOutput { get; set; }
		public void InsertNextCommandToExecute(object context, string command);
	}
}
