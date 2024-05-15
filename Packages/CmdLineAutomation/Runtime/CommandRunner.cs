using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// Abstract class that keeps track of runtime data by context
	/// </summary>
	/// <typeparam name="ExecutionData"></typeparam>
	public abstract class CommandRunner<ExecutionData> : ScriptableObject, ICommandProcessor {

		Dictionary<object, ExecutionData> _executionData = new Dictionary<object, ExecutionData>();
		abstract protected ExecutionData CreateEmptyContextEntry(object context);

		protected ExecutionData GetExecutionData(object context) {
			if (!_executionData.TryGetValue(context, out ExecutionData commandExecution)) {
				_executionData[context] = commandExecution = CreateEmptyContextEntry(context);
			}
			return commandExecution;
		}

		protected void SetExecutionData(object context, ExecutionData data) {
			_executionData[context] = data;
		}

		protected void RemoveExecutionData(object context) {
			_executionData.Remove(context);
		}

		abstract public bool IsExecutionFinished(object context);

		abstract public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput);
	}
}
