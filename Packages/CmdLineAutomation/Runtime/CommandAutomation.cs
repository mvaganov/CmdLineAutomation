using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// * Metadata about why this object exists
	/// * A list of command filters (including the specific named command listing in a sub-asset)
	/// * A list of commands to execute
	/// * Logic to process commands as a cooperative process
	///   * does not block the Unity thread
	///   * tracks state of which command is executing now
	///   * can be cancelled
	/// * TODO an object to manage output from commands
	/// * TODO create variable listing? auto-populate variables based on std output?
	/// </summary>
	[CreateAssetMenu(fileName = "NewCmdLineAutomation", menuName = "ScriptableObjects/CmdLineAutomation", order = 1)]
	public class CommandAutomation : CommandRunner<CommandAutomation.CommandExecution>, ICommandProcessor {
		/// <summary>
		/// 
		/// </summary>
		[Serializable]
		public class TextCommand
		{
			[TextArea(1, 1000)] public string Description;

			[TextArea(1,100)] public string Text;

			public ParsedTextCommand[] ParsedCommands;

			public void Parse()
			{
				string text = Text.Replace("\r", "");
				string[] lines = text.Split("\n");
				ParsedCommands = new ParsedTextCommand[lines.Length];
				for (int i = 0; i < ParsedCommands.Length; ++i)
				{
					ParsedCommands[i] = new ParsedTextCommand(lines[i]);
				}
			}
		}

		[Serializable]
		public class ParsedTextCommand {
			public string Text;
			public bool Comment;

			public ParsedTextCommand(string text)
			{
				Text = text;
			}
		}

		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
		/// </summary>
		[SerializeField] protected UnityEngine.Object[] _commandFilters;

		/// <summary>
		/// Information about what these commands are for
		/// </summary>
		[ContextMenuItem(nameof(ParseCommands),nameof(ParseCommands))]
		[SerializeField] protected TextCommand _command;

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

			public float Progress
			{
				get
				{
					ICommandFilter cmd = CurrentCommand();
					float cmdProgress = cmd != null ? cmd.Progress(context) : 0;
					if (cmdProgress > 0) { return cmdProgress; }
					if (commandExecutingIndex < 0 || source == null || source.CommandsToDo == null) return 0;
					float majorProgress = (float)commandExecutingIndex / source.CommandsToDo.Length;
					float minorTotal = 1f / source.CommandsToDo.Length;
					float minorProgress = source._filters != null ? 
						minorTotal * filterIndex / source._filters.Count : 0;
					return majorProgress + minorProgress;
				}
			}

			public OperatingSystemCommandShell Shell { get => _shell; }

			public bool HaveCommandToDo() => currentCommand != null;

			public bool IsCancelled() => commandExecutingIndex < 0;

			public string CurrentCommandText() => currentCommandText;

			public ICommandFilter CurrentCommand() => currentCommand;
			
			public void CancelExecution()
			{
				currentCommand = null;
				commandExecutingIndex = -1;
				filterIndex = 0;
				//Debug.Log("CANCELLED");
			}

			public void StartRunningEachCommandInSequence()
			{
				commandExecutingIndex = 0;
				RunEachCommandInSequence();
			}
			
			private void RunEachCommandInSequence() {
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
					if (IsCancelled())
					{
						//Debug.Log("----------CANCELLED");
						return;
					}
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
				if (IsExecutionStoppedByFilterFunction()) {
					return;
				}
				currentCommand = null;
				filterIndex = 0;
			}

			private bool IsExecutionStoppedByFilterFunction() {
				while (filterIndex < source._filters.Count) {
					if (currentCommand == null) {
						currentCommand = source._filters[filterIndex];
						if (context == null) {
							Debug.LogError("context must not be null!");
						}
						//Debug.Log($"~~~~~~~~{name} start {command} co-op f[{_filterIndex}] {_currentCommand}\n\n{_currentCommandText}");
						currentCommand.StartCooperativeFunction(context, currentCommandText, stdOutput);
						if ((_shell == null || !_shell.IsRunning) && currentCommand is FilterOperatingSystemCommandShell osShell) {
							_shell = osShell.Shell;
						}
					}
					if (!currentCommand.IsExecutionFinished(context)) {
						return true;
					}
					currentCommandResult = currentCommand.FunctionResult(context);
					currentCommand = null;
					if (currentCommandResult == null) {
						//Debug.Log($"@@@@@ {currentCommandText} consumed by {source._filters[filterIndex]}");
						return false;
					} else if(currentCommandResult != currentCommandText) {
						//Debug.Log($"@@@@@ {currentCommandText} changed into {currentCommandResult} by {source._filters[filterIndex]}");
						currentCommandText = currentCommandResult;
					}
					++filterIndex;
				}
				//Debug.Log($"{currentCommandText} NOT consumed");
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

		public ParsedTextCommand[] CommandsToDo => _command.ParsedCommands;

		public string Commands
		{
			get => _command.Text;
			set
			{
				_command.Text = value;
				ParseCommands();
			}
		}

		public override float Progress(object context) => GetExecutionData(context).Progress;

		public void CancelProcess(object context) => GetExecutionData(context).CancelExecution();
		
		public string CurrentCommandText(object context) => GetExecutionData(context).CurrentCommandText();
		
		public ICommandProcessor CurrentCommand(object context) => GetExecutionData(context).CurrentCommand();
		
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
			ParseCommands();
		}

		public void ParseCommands()
		{
			_command.Parse();
		}

		public void RunCommands(object context, TextResultCallback stdOutput) {
			CommandExecution e = GetExecutionData(context);
			e.stdOutput = stdOutput;
			Initialize();
			e.StartRunningEachCommandInSequence();
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
