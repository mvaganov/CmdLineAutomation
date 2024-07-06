using System.Collections.Generic;

namespace RunCmd {
	/// <summary>
	/// Commands can be configured in a single asset, and used by multiple different contexts.
	/// This abstract class is intended as a base class for commands, and keeps track of runtime
	/// data, organized by context.
	/// </summary>
	/// <typeparam name="ExecutionData"></typeparam>
	public interface CommandRunner<ExecutionData> : CommandRunnerBase {

		Dictionary<object, ExecutionData> ExecutionDataAccess { get; set; }
		abstract ExecutionData CreateEmptyContextEntry(object context);
	}

	public static class CommandRunnerExtension {
		public static ExecutionData GetExecutionData<ExecutionData>(this CommandRunner<ExecutionData> self, object context) {
			if (!self.ExecutionDataAccess.TryGetValue(context, out ExecutionData commandExecution)) {
				UnityEngine.Debug.LogWarning($"{self.GetType().Name} making execution data for {context} {context.GetHashCode()}");
				self.ExecutionDataAccess[context] = commandExecution = self.CreateEmptyContextEntry(context);
			}
			return commandExecution;
		}

		public static void SetExecutionData<ExecutionData>(this CommandRunner<ExecutionData> self, object context, ExecutionData data) {
			self.ExecutionDataAccess[context] = data;
		}

		public static void RemoveExecutionData<ExecutionData>(this CommandRunner<ExecutionData> self, object context) {
			self.ExecutionDataAccess.Remove(context);
		}
	}

	public interface CommandRunnerBase : ICommandProcessor {
		public void RemoveExecutionData(object context);
	}
}
