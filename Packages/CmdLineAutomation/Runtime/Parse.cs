namespace RunCmd {
	public static class Parse {
		public static string[] Split(string command) {
			return command.Split();
		}

		public static string GetFirstToken(string command) {
			int index = command.IndexOf(' ');
			return index < 0 ? command : command.Substring(0, index);
		}
	}
}
