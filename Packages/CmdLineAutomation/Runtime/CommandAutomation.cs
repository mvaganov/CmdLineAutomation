using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// * Metadata about why this object exists
	/// * A list of command filters (including the specific named command listing in a sub-asset)
	/// * A list of commands to execute (TODO derived from a single string)
	/// * Logic to process commands as a cooperative process (track state of which command is executing now, TODO progress bar)
	/// * TODO an object to manage output from commands
	/// * TODO create variable listing? auto-populate variables based on input?
	/// </summary>
	[CreateAssetMenu(fileName = "NewCmdLineAutomation", menuName = "ScriptableObjects/CmdLineAutomation", order = 1)]
	public class CommandAutomation : CommandRunner<CommandAutomation.CommandExecution>, ICommandProcessor {
		[Serializable]
		public class MetaData {
			[TextArea(1, 1000)] public string Description;
		}

		/// <summary>
		/// TODO remove the need for making this public by using a parsed TextArea, with one command per line
		/// </summary>
		[Serializable]
		public class TextCommand {
			public string Text;
			public bool Comment;
		}

		/// <summary>
		/// Information about what these commands are for
		/// </summary>
		[SerializeField] protected MetaData _details;
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
		/// </summary>
		[SerializeField] protected UnityEngine.Object[] _commandFilters;
		/// <summary>
		/// The specific commands to do TODO replace with new-line-delimited text area?
		/// </summary>
		[SerializeField] protected TextCommand[] CommandsToDo;

		/// <summary>
		/// List if filtering functions for input, which may or may not consume a command
		/// </summary>
		private List<ICommandFilter> _filters;

		public class CommandExecution {
			/// <summary>
			/// What object counts as the owner of this command
			/// </summary>
			public object context;
			/// <summary>
			/// Which command from <see cref="CommandsToDo"/> is being executed right now
			/// </summary>
			public int commandExecutingIndex;
			/// <summary>
			/// Text of the current command
			/// </summary>
			public string currentCommandText;
			/// <summary>
			/// Which cooperative function is being executed right now
			/// </summary>
			public ICommandFilter currentCommand;
			/// <summary>
			/// Result of the last finished cooperative function
			/// </summary>
			public string currentCommandResult;
			/// <summary>
			/// Which <see cref="_commandFilters"/> is being cooperatively processed right now
			/// </summary>
			public int filterIndex = 0;
			/// <summary>
			/// Function to pass all lines from standard output to
			/// </summary>
			public TextResultCallback stdOutput;
			private CommandAutomation source;
			/// <summary>
			/// Keeps track of a shell, if one is generated
			/// </summary>
			private OperatingSystemCommandShell _shell;

			public OperatingSystemCommandShell Shell { get => _shell; }

			public bool HaveCommandToDo() => currentCommand != null;

			public void RunEachCommandInSequence() {
				if (HaveCommandToDo()) {
					if (currentCommand.IsExecutionFinished(context)) {
						++commandExecutingIndex;
						currentCommand = null;
					} else {
						DelayCall(RunEachCommandInSequence);
						return;
					}
				}
				if (!HaveCommandToDo()) {
					string textToDo = source.CommandsToDo[commandExecutingIndex].Text;
					if (!source.CommandsToDo[commandExecutingIndex].Comment) {
						filterIndex = 0;
						//Debug.Log("execute " + _commandExecutingIndex+" "+ textToDo);
						StartCooperativeFunction(textToDo, stdOutput);
						//if (HaveCommandToDo() && !_currentCommand.IsExecutionFinished()) { Debug.Log("       still doing it!"); }
					}
					if (!HaveCommandToDo() || currentCommand.IsExecutionFinished(context)) {
						++commandExecutingIndex;
					}
				} else {
					DoCurrentCommand();
					if (currentCommand == null) {
						++commandExecutingIndex;
					}
				}
				if (commandExecutingIndex < source.CommandsToDo.Length) {
					DelayCall(RunEachCommandInSequence);
				} else {
					commandExecutingIndex = 0;
				}
			}

			/// <inheritdoc/>
			public void StartCooperativeFunction(string command, TextResultCallback stdOutput) {
				if (context == null) {
					Debug.LogError("NULL!!!!!");
				}
				this.stdOutput = stdOutput;
				currentCommandText = command;
				currentCommandResult = command;
				filterIndex = 0;
				DoCurrentCommand();
			}

			private void DoCurrentCommand() {
				if (currentCommand != null && !currentCommand.IsExecutionFinished(context)) {
					Debug.Log($"still processing {currentCommand}");
					return;
				}
				//Debug.Log("processing " + _currentCommandText);
				if (IsExecutionStoppedByFilterFunction(currentCommandText)) {
					return;
				}
				currentCommand = null;
				filterIndex = 0;
			}

			private bool IsExecutionStoppedByFilterFunction(string command) {
				while (filterIndex < source._filters.Count) {
					if (currentCommand == null) {
						currentCommand = source._filters[filterIndex];
						if (context == null) {
							Debug.LogError("context must not be null!");
						}
						//Debug.Log($"~~~~~~~~{name} start {command} co-op f[{_filterIndex}] {_currentCommand}\n\n{_currentCommandText}");
						currentCommand.StartCooperativeFunction(context, currentCommandText, stdOutput);
						if (_shell == null && currentCommand is FilterOperatingSystemCommandShell osShell) {
							_shell = osShell.Shell;
						}
					}
					if (!currentCommand.IsExecutionFinished(context)) {
						return true;
					}
					command = currentCommand.FunctionResult(context);
					currentCommand = null;
					if (command == null) {
						//Debug.Log($"{_currentCommandText} consumed by {_filters[_filterIndex]}");
						return false;
					}
					++filterIndex;
				}
				Debug.Log($"{currentCommandText} NOT consumed");
				return false;
			}

			public bool IsExecutionFinished() => currentCommand == null || currentCommand.IsExecutionFinished(context);

			public string FunctionResult() => currentCommand != null ? currentCommand.FunctionResult(context) : currentCommandResult;

			public CommandExecution(object context, CommandAutomation commandAutomation) {
				source = commandAutomation;
				this.context = context;
				commandExecutingIndex = 0;
			}
		}

		protected override CommandExecution CreateEmptyContextEntry(object context)
			=> new CommandExecution(context, this);

		public OperatingSystemCommandShell GetShell(object context) => GetExecutionData(context).Shell;

		private bool NeedsInitialization() => _filters == null;

		public void Initialize() {
			_filters = new List<ICommandFilter>();
			foreach (UnityEngine.Object obj in _commandFilters) {
				switch (obj) {
					case ICommandFilter iFilter: _filters.Add(iFilter); break;
					default:
						Debug.LogError($"unexpected filter type {obj.GetType().Name}, " +
							$"{name} expects only {nameof(ICommandFilter)} entries");
						break;
				}
			}
		}

		public void RunCommands(object context, TextResultCallback stdOutput) {
			CommandExecution e = GetExecutionData(context);
			e.stdOutput = stdOutput;
			Initialize();
			e.RunEachCommandInSequence();
		}

		public override void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			GetExecutionData(context).StartCooperativeFunction(command, stdOutput);
		}

		public override bool IsExecutionFinished(object context) => GetExecutionData(context).IsExecutionFinished();

		public string FunctionResult(object context) => GetExecutionData(context).FunctionResult();

#if UNITY_EDITOR
		public static void DelayCall(UnityEditor.EditorApplication.CallbackFunction call) {
			UnityEditor.EditorApplication.delayCall += call;
		}
#else
		public static void DelayCall(Action call) {
			CoroutineRunner.Instance.StartCoroutine(DelayCall());
			System.Collections.IEnumerator DelayCall() {
				yield return null;
				call.Invoke();
			}
		}
		private class CoroutineRunner : MonoBehaviour {
			private static CoroutineRunner _instance;
			public static CoroutineRunner Instance {
				get {
					if (_instance != null) { return _instance; }
					GameObject go = new GameObject("<CoroutineRunner>");
					DontDestroyOnLoad(go);
					return _instance = go.AddComponent<CoroutineRunner>();
				}
			}
		}
#endif
	}
}
