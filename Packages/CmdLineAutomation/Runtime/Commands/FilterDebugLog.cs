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
		[SerializeField] protected bool _enabled = true;
		[SerializeField] protected bool _consumeCommand = false;
		[SerializeField] protected LogType _logType = LogType.DebugLog_Log;
		[SerializeField] protected string _linePrefix = "", _lineSuffix = "";
		private Dictionary<object, string> _lastCommand = new Dictionary<object, string>();
		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			_lastCommand[context] = _consumeCommand ? null : command;
			if (!_enabled) {
				return;
			}
			if (!string.IsNullOrEmpty(_linePrefix) || string.IsNullOrEmpty(_lineSuffix)) {
				command = _linePrefix + command + _lineSuffix;
			}
			switch (_logType) {
				case LogType.StdOutput: stdOutput.Invoke(command); break;
				case LogType.DebugLog_Error: Debug.LogError(command); break;
				case LogType.DebugLog_Assert: Debug.LogAssertion(command); break;
				case LogType.DebugLog_Warning: Debug.LogWarning(command); break;
				case LogType.DebugLog_Log:      Debug.Log(command); break;
				case LogType.DebugLog_Exception: Debug.LogException(new System.Exception(command), context as UnityEngine.Object); break;
			}
		}

		public string FunctionResult(object context) => _lastCommand[context];

		public bool IsExecutionFinished(object context) => true;
	}
}
