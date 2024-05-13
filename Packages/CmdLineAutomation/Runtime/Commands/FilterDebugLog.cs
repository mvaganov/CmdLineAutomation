using System;
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
		[SerializeField] protected LogType logType = LogType.DebugLog_Log;
		[SerializeField] protected string linePrefix = "", lineSuffix = "";
		private string result;
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			if (!enabled) {
				return;
			}
			if (!string.IsNullOrEmpty(linePrefix) || string.IsNullOrEmpty(lineSuffix)) {
				command = linePrefix + command + lineSuffix;
			}
			switch (logType) {
				case LogType.StdOutput:
					stdOutput.Invoke(command);
					break;
				case LogType.DebugLog_Error:
					Debug.LogError(command);
					break;
				case LogType.DebugLog_Assert:
					Debug.LogAssertion(command);
					break;
				case LogType.DebugLog_Warning:
					Debug.LogWarning(command);
					break;
				case LogType.DebugLog_Log:
					Debug.Log(command);
					break;
				case LogType.DebugLog_Exception:
					Debug.LogException(new System.Exception(command), context as UnityEngine.Object);
					break;
			}
			result = command;
		}

		public string FunctionResult() => result;

		public bool IsExecutionFinished() => true;
	}
}
