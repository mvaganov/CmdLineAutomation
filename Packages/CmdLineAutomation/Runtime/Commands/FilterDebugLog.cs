using System;
using UnityEngine;

/// <summary>
/// Prints each command into the Unity console. This is mostly useful as a sample implementation of
/// <see cref="ICommandProcessor"/>
/// </summary>
[CreateAssetMenu(fileName = "DebugLog", menuName = "ScriptableObjects/Filters/FilterDebugLog")]
public class FilterDebugLog : ScriptableObject, ICommandProcessor {
	public LogType logType = LogType.Log;
	private string result;
	public void StartCooperativeFunction(object context, string command, Action<string> stdOutput) {
		switch (logType) {
			case LogType.Error:
				Debug.LogError(command);
				break;
			case LogType.Assert:
				Debug.LogAssertion(command);
				break;
			case LogType.Warning:
				Debug.LogWarning(command);
				break;
			case LogType.Log:
				Debug.Log(command);
				break;
			case LogType.Exception:
				Debug.LogException(new System.Exception(command), context as UnityEngine.Object);
				break;
		}
		result = command;
	}

	public string FunctionResult() => result;

	public bool IsFunctionFinished() => true;
}
