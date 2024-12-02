using System.Diagnostics;

namespace RunCmdRedux {
	public interface ICommandAsset {
		public ICommandProcess CreateCommand(object context);
	}

	public static class ICommandAssetExtension {
		public static ICommandProcess GetCommand(this ICommandAsset self, object context) =>
			CommandManager.Instance.TryGet(context, self, out var proc) ? proc.process : null;
		public static ICommandProcess GetCommandCreateIfMissing(this ICommandAsset self, object context) {
			ICommandProcess proc = GetCommand(self, context);
			if (proc == null) {
				UnityEngine.Debug.LogWarning($"missing process for {self.ToString()}({context}), creating.\n" +
					$"only a problem if you see it more than once for {self.ToString()}");
				proc = self.CreateCommand(context);
			}
			return proc;
		}
	}
}
