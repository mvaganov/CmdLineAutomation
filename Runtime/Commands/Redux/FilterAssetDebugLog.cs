using UnityEngine;

namespace RunCmdRedux {
	/// <summary>
	/// Prints each command into the Unity console. This is mostly useful as a sample implementation of
	/// <see cref="ICommandProcessor"/>
	/// </summary>
	[CreateAssetMenu(fileName = "DebugLog", menuName = "ScriptableObjects/FilterAssets/DebugLog")]
	public class FilterAssetDebugLog : ScriptableObject, ICommandAsset {
		public enum LogType { None, StdOutput, UnityDebugLogError, UnityDebugLogAssert, UnityDebugLogWarning, UnityDebugLog, UnityDebugLogException }
		[SerializeField] protected bool _enabled = true;
		[SerializeField] protected bool _consumeCommand = false;
		[SerializeField] protected LogType _logType = LogType.UnityDebugLog;
		[SerializeField] protected string _linePrefix = "", _lineSuffix = "";

		public class Proc : BaseNamedProcess {
			private FilterAssetDebugLog _source;
			private object _context;
			public Proc(FilterAssetDebugLog source, object context) {
				_source = source;
				_context = context;
			}

			public override string name => _source.name;

			public override float GetProgress() => 1;

			public override void StartCooperativeFunction(string command, PrintCallback print) {
				if (!_source._enabled) {
					_state = ICommandProcess.State.Disabled;
					return;
				}
				_state = ICommandProcess.State.Executing;
				if (!string.IsNullOrEmpty(_source._linePrefix) || string.IsNullOrEmpty(_source._lineSuffix)) {
					command = _source._linePrefix + command + _source._lineSuffix;
				}
				switch (_source._logType) {
					case LogType.StdOutput: print.Invoke(command); break;
					case LogType.UnityDebugLogError: Debug.LogError(command); break;
					case LogType.UnityDebugLogAssert: Debug.LogAssertion(command); break;
					case LogType.UnityDebugLogWarning: Debug.LogWarning(command); break;
					case LogType.UnityDebugLog: Debug.Log(command); break;
					case LogType.UnityDebugLogException: Debug.LogException(new System.Exception(command), _context as UnityEngine.Object); break;
				}
				_state = ICommandProcess.State.Finished;
			}
		}

		public ICommandProcess CreateCommand(object context) => new Proc(this, context);
	}
}
