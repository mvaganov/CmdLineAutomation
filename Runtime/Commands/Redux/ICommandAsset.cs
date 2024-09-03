namespace RunCmd {
	public interface ICommandAsset {
		public ICommandProcess CreateCommand(object context);
	}

	public static class ICommandAssetExtension {
		public static ICommandProcess GetCommand(this ICommandAsset self, object context) =>
			CommandManager.Instance.TryGet(context, self, out var proc) ? proc.process : null;
		public static ICommandProcess GetCommandCreateIfMissing(this ICommandAsset self, object context) {
			ICommandProcess proc = GetCommand(self, context);
			if (proc == null) {
				proc = self.CreateCommand(context);
			}
			return proc;
		}
	}
}
