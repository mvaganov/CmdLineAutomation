namespace RunCmdRedux {
	public interface ICommandAsset {
		public ICommandProcess CreateCommand(object context);
	}

	public interface ICommandAssetBranch {
		public ICommandProcess GetProcessByIndex(int index);
		public int GetProcessCount();
	}

	public static class ICommandAssetExtension {
		public static ICommandProcess GetCommandIfCreated(this ICommandAsset self, object context) =>
			CommandManager.Instance.TryGet(context, self, out var proc) ? proc.process : null;
		public static ICommandProcess GetCommandCreateIfMissing(this ICommandAsset self, object context) {
			ICommandProcess proc = GetCommandIfCreated(self, context);
			if (proc == null) {
				UnityEngine.Debug.LogWarning($"missing process for {self.ToString()}({context}), creating.\n" +
					$"only a problem if you see it more than once for {self.ToString()}");
				proc = self.CreateCommand(context);
			}
			return proc;
		}
	}
}
