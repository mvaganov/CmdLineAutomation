using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// Prints each command into the Unity console. This is mostly useful as a sample implementation of
	/// <see cref="ICommandProcessor"/>
	/// </summary>
	[CreateAssetMenu(fileName = "DebugLog", menuName = "ScriptableObjects/Filters/FilterDebugLog")]
	public class FilterDebugLog : ScriptableObject, ICommandFilter {
		public enum LogType { None, StdOutput, DebugLog_Error, DebugLog_Assert, DebugLog_Warning, DebugLog_Log, DebugLog_Exception }
		[SerializeField] protected bool enabled = true;
		[SerializeField] protected bool consumeCommand = false;
		[SerializeField] protected LogType logType = LogType.DebugLog_Log;
		[SerializeField] protected string linePrefix = "", lineSuffix = "";
		private Dictionary<object, string> result = new Dictionary<object, string>();
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			result[context] = consumeCommand ? null : command;
			if (!enabled) {
				return;
			}
			if (!string.IsNullOrEmpty(linePrefix) || string.IsNullOrEmpty(lineSuffix)) {
				command = linePrefix + command + lineSuffix;
			}
			switch (logType) {
				case LogType.StdOutput: stdOutput.Invoke(command); break;
				case LogType.DebugLog_Error: Debug.LogError(command); break;
				case LogType.DebugLog_Assert: Debug.LogAssertion(command); break;
				case LogType.DebugLog_Warning: Debug.LogWarning(command); break;
				case LogType.DebugLog_Log:      Debug.Log(command); break;
				case LogType.DebugLog_Exception: Debug.LogException(new System.Exception(command), context as UnityEngine.Object); break;
			}
		}

		public string FunctionResult(object context) => result[context];

		public bool IsExecutionFinished(object context) => true;
	}
}
