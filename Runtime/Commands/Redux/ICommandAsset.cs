using System.Text;

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
				//DebugInfoAboutExistingProcesses(self, context);
				proc = self.CreateCommand(context);
			}
			return proc;
		}

		private static void DebugInfoAboutExistingProcesses(ICommandAsset self, object context) {
			StringBuilder sb = new StringBuilder();
			CommandManager.Procedure selfProc = new CommandManager.Procedure(context, self, null);
			foreach (var item in CommandManager._GetProcedures) {
				sb.Append(item).Append(item.Equals(selfProc));
				sb.Append(":").Append(item.context.Equals(selfProc.context));
				sb.Append(":").Append(item.procSource.Equals(selfProc.procSource));
				sb.Append(":").Append(item.process.Equals(selfProc.process));
				sb.Append("\n");
			}
			UnityEngine.Debug.LogWarning($"missing process for {self.ToString()}({context}), creating.\n" +
				$"only a problem if you see it more than once for {self.ToString()}\n-----{selfProc}-----\n{sb}");
		}

		public static bool RemoveCommand(this ICommandAsset self, object context, ICommandProcess proc) {
			CommandManager.Procedure selfProc = new CommandManager.Procedure(context, self, null);
			return CommandManager.Instance.Remove(context, self, proc);
		}
	}
}
