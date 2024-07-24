using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// Prints each command into the Unity console. This is mostly useful as a sample implementation of
	/// <see cref="ICommandProcessor"/>
	/// </summary>
	[CreateAssetMenu(fileName = "DebugLog", menuName = "ScriptableObjects/Filters/DebugLog")]
	public class FilterDebugLog : ScriptableObject, CommandRunner<string>, ICommandFilter {
		public enum LogType { None, StdOutput, DebugLog_Error, DebugLog_Assert, DebugLog_Warning, DebugLog_Log, DebugLog_Exception }
		[SerializeField] protected bool _enabled = true;
		[SerializeField] protected bool _consumeCommand = false;
		[SerializeField] protected LogType _logType = LogType.DebugLog_Log;
		[SerializeField] protected string _linePrefix = "", _lineSuffix = "";

		private Dictionary<object, string> _executionData = new Dictionary<object, string>();
		public Dictionary<object, string> ExecutionDataAccess { get => _executionData; set => _executionData = value; }

		public void StartCooperativeFunction(object context, string command, PrintCallback print) {
			this.SetExecutionData(context, _consumeCommand ? null : command);
			if (!_enabled) {
				return;
			}
			if (!string.IsNullOrEmpty(_linePrefix) || string.IsNullOrEmpty(_lineSuffix)) {
				command = _linePrefix + command + _lineSuffix;
			}
			switch (_logType) {
				case LogType.StdOutput: print.Invoke(command); break;
				case LogType.DebugLog_Error: Debug.LogError(command); break;
				case LogType.DebugLog_Assert: Debug.LogAssertion(command); break;
				case LogType.DebugLog_Warning: Debug.LogWarning(command); break;
				case LogType.DebugLog_Log:      Debug.Log(command); break;
				case LogType.DebugLog_Exception: Debug.LogException(new System.Exception(command), context as UnityEngine.Object); break;
			}
		}

		public string FunctionResult(object context) => this.GetExecutionData(context);

		public bool IsExecutionFinished(object context) => true;

		public string CreateEmptyContextEntry(object context) => null;

		public float Progress(object context) => 0;

		public void RemoveExecutionData(object context) => CommandRunnerExtension.RemoveExecutionData(this, context);
	}
}
