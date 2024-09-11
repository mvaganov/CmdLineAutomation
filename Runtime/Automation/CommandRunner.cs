using System.Collections.Generic;
using System.Text;

namespace RunCmd {
	/// <summary>
	/// Commands can be configured in a single asset, and used by multiple different contexts.
	/// This abstract class is intended as a base class for commands, and keeps track of runtime
	/// data, organized by context.
	/// 
	/// </summary>
	/// <typeparam name="ExecutionData"></typeparam>
	public interface CommandRunner<ExecutionData> : CommandRunnerBase {

		Dictionary<object, ExecutionData> ExecutionDataAccess { get; set; }
		//Dictionary<int, object> ExecutionDataByThreadId { get; set; }
		abstract ExecutionData CreateEmptyContextEntry(object context);
	}

	public static class CommandRunnerExtension {
		public static ExecutionData GetExecutionData<ExecutionData>(this CommandRunner<ExecutionData> self, object context) {
			if (self.ExecutionDataAccess == null) {
				//UnityEngine.Debug.LogError("@@@@@@@@@@@@@@@@@@@@@@ no data?");
				self.ExecutionDataAccess = new Dictionary<object, ExecutionData>();
			}
			if (!self.ExecutionDataAccess.TryGetValue(context, out ExecutionData commandExecution)) {
				//UnityEngine.Debug.LogWarning($"~~~~~~~~~{self.GetType().Name} making execution data for {context} {context.GetHashCode()}");
				commandExecution = self.CreateEmptyContextEntry(context);
				self.SetExecutionData(context, commandExecution);
				//self.ExecutionDataAccess[context] = commandExecution = self.CreateEmptyContextEntry(context);
			}
			return commandExecution;
		}

		public static void SetExecutionData<ExecutionData>(this CommandRunner<ExecutionData> self, object context, ExecutionData data) {
			self.ExecutionDataAccess[context] = data;
			//int threadId = System.Environment.CurrentManagedThreadId;
			//self.ExecutionDataByThreadId[threadId] = data;
		}

		//public static bool TryGetThreadId<ExecutionData>(this CommandRunner<ExecutionData> self, object context, out int id) {
		//	foreach (var kvp in self.ExecutionDataByThreadId) {
		//		if (kvp.Value == context) {
		//			id = kvp.Key;
		//			return true;
		//		}
		//	}
		//	id = 0;
		//	return false;
		//}

		//public static bool TryGetDefaultContextForThisThread<ExecutionData>(this CommandRunner<ExecutionData> self, out object context) {
		//	int threadId = System.Environment.CurrentManagedThreadId;
		//	return self.ExecutionDataByThreadId.TryGetValue(threadId, out context);
		//}

		public static void RemoveExecutionData<ExecutionData>(this CommandRunner<ExecutionData> self, object context) {
			self.ExecutionDataAccess.Remove(context);
			//TryGetThreadId(self, context, out int threadId);
			//self.ExecutionDataByThreadId.Remove(threadId);
		}

		public static IList<object> GetOwningContexts<ExecutionData>(this CommandRunnerBase self) {
			List<object> contexts = new List<object>();
			foreach (object key in self.GetContexts()) {
				contexts.Add(key);
			}
			return contexts;
		}

		public static string GetDescriptionOfContexts(this CommandRunnerBase self) {
			StringBuilder sb = new StringBuilder();
			IEnumerable<object> contexts = self.GetContexts();
			if (contexts == null) {
				return "<none>";
			}
			foreach (object key in contexts) {
				string description;
				if (key is UnityEngine.Object uObj) {
					description = uObj.name;
				} else {
					description = key.ToString() + key.GetHashCode();
				}
				string percentDone = (self.Progress(key) * 100).ToString("###.#");
				string done = self.IsExecutionFinished(key) ? "done" : "work";
				string line = $"{done} {percentDone}% {description}\n";
				UnityEngine.Debug.Log(line);
				sb.Append(line);
			}
			return sb.ToString();
		}
	}

	public interface CommandRunnerBase : ICommandProcessor {
		public void RemoveExecutionData(object context);
		public IEnumerable<object> GetContexts();
	}
}
