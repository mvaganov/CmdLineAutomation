using System.Collections.Generic;
using System.Text;

namespace RunCmdRedux {
	public interface ICommandAsset {
		public ICommandProcess CreateCommand(object context);
	}

	public interface ICommandAssetBranch {
		public ICommandAsset GetAssetByIndex(int index);
		public int GetAssetCount();
	}

	public static class ICommandAssetExtension {
		public static ICommandProcess GetCommandIfCreated(this ICommandAsset self, object context) =>
			CommandManager.Instance.TryGet(context, self, out ICommandProcess proc) ? proc : null;
		public static ICommandProcess GetCommandCreateIfMissing(this ICommandAsset self, object context) {
			ICommandProcess proc = GetCommandIfCreated(self, context);
			if (proc == null) {
				UnityEngine.Debug.LogWarning(DebugInfoAboutExistingProcesses(self, context));
				proc = self.CreateCommand(context);
				CommandManager.Instance.Add(context, self, proc);
				ICommandProcess test = GetCommandIfCreated(self, context);
				if (test == null) {
					throw new System.Exception("### unable to find process that was just added");
				}
				if (test != proc) {
					throw new System.Exception("### found process is not what was just added!!");
				}
			}
			return proc;
		}

		private static string DebugInfoAboutExistingProcesses(ICommandAsset self, object context) {
			StringBuilder sb = new StringBuilder();
			CommandManager.Procedure selfProc = new CommandManager.Procedure(context, self);
			foreach (var kvp in CommandManager._GetProcedures) {
				sb.Append(kvp.Key).Append(kvp.Key.Equals(selfProc));
				sb.Append(":").Append(kvp.Key.context.Equals(selfProc.context));
				sb.Append(":").Append(kvp.Key.procSource.Equals(selfProc.procSource));
				sb.Append("\n");
			}
			return ($"missing process for {self.ToString()} @{context}, creating.\n" +
				$"only a problem if you see it more than once for {self.ToString()}\n-----{selfProc}-----\n{sb}\n<done>");
		}

		public static bool RemoveCommand(this ICommandAsset self, object context, ICommandProcess proc) {
			CommandManager.Procedure selfProc = new CommandManager.Procedure(context, self);
			//UnityEngine.Debug.Log($"removing {selfProc}");
			return CommandManager.Instance.Remove(context, self, proc);
		}

		public static bool FoundRecursion(this ICommandAssetBranch self, List<ICommandAssetBranch> list) {

			if (list != null) {
				int found = list.IndexOf(self);
				// self should be at the end of this list already, so ignore the end element.
        if (found >= 0 && found < list.Count-1) {
					return true;
				}
			}
			for (int i = 0; i < self.GetAssetCount(); ++i) {
				ICommandAsset proc = self.GetAssetByIndex(i);
				if (proc == null) {
					continue;
				}
				ICommandAssetBranch branch = proc as ICommandAssetBranch;
				if (branch == null) {
					continue;
				}
				if (list == null) {
					list = new List<ICommandAssetBranch> { branch };
				} else {
					list.Add(branch);
				}
				if (FoundRecursion(branch, list)) {
					return true;
				}
				list.RemoveAt(list.Count - 1);
			}
			return list != null && list.Count > 0;
		}
	}
}
