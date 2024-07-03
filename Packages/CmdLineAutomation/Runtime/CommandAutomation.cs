using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// TODO refactor:

	/// CommandLineSettings: has all of the settings of a command line, like what commands it accepts and what variables it looks for
	/// * Metadata/description about why this object exists
	/// * A list of command filters (including the specific named command listing in a sub-asset)
	/// * 

	/// CommandLineExecutor: has the runtime variables of a command line, like the parsed commands to execute, the command execution stack, specific found variable data. Executes commands for a command line defined by CommandLineSettings.
	/// * Logic to process commands as a cooperative process
	///   * does not block the Unity thread
	///   * tracks state of which command is executing now
	///   * can be cancelled
	/// * A list of commands to execute
	/// * keeps track of command output, which can be filtered by line with regular expressions
	/// * can be fed commands in the Unity Editor, or from runtime methods

	/// CommandLineAutomation: has a specific command line instruction to execute, which it uses to populate a CommandLineExecutor
	/// * Metadata/description about why this instruction set exists
	/// * the instruction set

	/// </summary>
	[CreateAssetMenu(fileName = "NewCmdLineAutomation", menuName = "ScriptableObjects/CmdLineAutomation", order = 1)]
	public partial class CommandAutomation : ScriptableObject, CommandRunner<CommandExecution>, ICommandProcessor, ICommandAutomation, ICommandExecutor {
		private enum RegexGroupId { None = -1, HideNextLine = 0, DisableOnRead, EnableOnRead }
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
		/// </summary>
		[SerializeField] protected UnityEngine.Object[] _commandFilters;

		/// <summary>
		/// List if filtering functions for input, which may or may not consume a command. This is the type disambiguated version of <see cref="_commandFilters"/>
		/// </summary>
		private List<ICommandFilter> _filters;

		/// <summary>
		/// Variables to read from command line input
		/// </summary>
		[SerializeField] protected NamedRegexSearch[] _variablesFromCommandLineRegexSearch = new NamedRegexSearch[] {
			new NamedRegexSearch("WindowsTerminalVersion", @"Microsoft Windows \[Version ([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\]", new int[] { 1 }, true, NamedRegexSearch.SpecialReadLogic.IgnoreAfterFirstValue),
			new NamedRegexSearch("dir", NamedRegexSearch.CommandPromptRegexWindows, null, true, NamedRegexSearch.SpecialReadLogic.None)
		};

		/// <summary>
		/// Information about what these commands are for
		/// </summary>
		[ContextMenuItem(nameof(ParseCommands),nameof(ParseCommands))]
		[SerializeField] protected TextCommand _command;

		/// <summary>
		/// Command being typed into the command prompt by the Unity Editor user
		/// </summary>
		private string _inspectorCommandOutput;

		private RegexMatrix _censorshipRules = new RegexMatrix();
		private bool _showOutput = true;
		private bool _hideNextLine = false;
		private List<(int row, int col)> _triggeredGroup = new List<(int row, int col)>();
		private System.Action<string> _onOutputChange;

		public IList<ParsedTextCommand> CommandsToDo {
			get => _command.ParsedCommands;
			set => _command.ParsedCommands = new List<ParsedTextCommand>(value);
		}

		public ICommandExecutor CommandExecutor => this;

		public IList<ICommandFilter> Filters => _filters;

		public TextCommand TextCommandData => _command;

		public IList<NamedRegexSearch> VariablesFromCommandLineRegexSearch => _variablesFromCommandLineRegexSearch;

		public string Commands
		{
			get => _command.Text;
			set
			{
				_command.Text = value;
				ParseCommands();
			}
		}

		public string CommandOutput {
			get => _inspectorCommandOutput;
			set {
					_inspectorCommandOutput = value;
					OnOutputChange?.Invoke(_inspectorCommandOutput);
			}
		}

		public bool ShowOutput {
			get => _showOutput;
			set => _showOutput = value;
		}

		public bool HideNextLine {
			get => _hideNextLine;
			set => _hideNextLine = value;
		}

		public System.Action<string> OnOutputChange {
			get { return _onOutputChange; }
			set { _onOutputChange = value; }
		}

		private Dictionary<object, CommandExecution> _executionData = new Dictionary<object, CommandExecution>();
		public Dictionary<object, CommandExecution> ExecutionDataAccess { get => _executionData; set => _executionData = value; }

		public void AddToCommandOutput(string value) {
			if (_censorshipRules.HasRegexTriggers) {
				_censorshipRules.ProcessAndCheckTextForTriggeringLines(value, AddProcessedLineToCommandOutput, _triggeredGroup);
			} else {
				AddLineToCommandOutputInternal(value);
			}
		}

		private void AddProcessedLineToCommandOutput(string processedLine) {
			if (ShowOutput && !_hideNextLine) {
				AddLineToCommandOutputInternal(processedLine);
			}
			_hideNextLine = false;
		}

		private void AddLineToCommandOutputInternal(string line) {
			_inspectorCommandOutput += line;
			OnOutputChange?.Invoke(_inspectorCommandOutput);
		}

		public float Progress(object context) => this.GetExecutionData(context).Progress;

		public void CancelProcess(object context) => this.GetExecutionData(context).CancelExecution();
		
		public string CurrentCommandText(object context) => this.GetExecutionData(context).CurrentCommandText();
		
		public ICommandProcessor CurrentCommand(object context) => this.GetExecutionData(context).CurrentCommand();
		
		public CommandExecution CreateEmptyContextEntry(object context)
			=> new CommandExecution(context, this);

		public OperatingSystemCommandShell GetShell(object context) => this.GetExecutionData(context).Shell;

		internal bool NeedsInitialization() => _filters == null;

		/// <summary>
		/// If the given regex is triggered, all output will be hidden (until <see cref="AddUncensorshipTrigger(string)"/>)
		/// </summary>
		/// <param name="regexTrigger"></param>
		public void AddCensorshipTrigger(string regexTrigger) => _censorshipRules.Add((int)RegexGroupId.DisableOnRead, regexTrigger);

		/// <summary>
		/// If the given regex is triggered, all output will be shown again
		/// </summary>
		/// <param name="regexTrigger"></param>
		public void AddUncensorshipTrigger(string regexTrigger) => _censorshipRules.Add((int)RegexGroupId.EnableOnRead, regexTrigger);

		/// <summary>
		/// Hide lines that contain the given regex trigger
		/// </summary>
		/// <param name="regexTrigger"></param>
		public void AddCensorLineTrigger(string regexTrigger) => _censorshipRules.Add((int)RegexGroupId.HideNextLine, regexTrigger);

		/// <summary>
		/// Remove a regex trigger added by <see cref="AddCensorshipTrigger(string)"/>
		/// </summary>
		/// <param name="regexTrigger"></param>
		/// <returns></returns>
		public bool RemoveCensorshipTrigger(string regexTrigger) => _censorshipRules.Remove((int)RegexGroupId.DisableOnRead, regexTrigger);

		/// <summary>
		/// Remove a regex trigger added by <see cref="AddUncensorshipTrigger(string)"/>
		/// </summary>
		/// <param name="regexTrigger"></param>
		/// <returns></returns>
		public bool RemoveUncensorshipTrigger(string regexTrigger) => _censorshipRules.Remove((int)RegexGroupId.EnableOnRead, regexTrigger);

		/// <summary>
		/// Remove all regex triggers added by <see cref="AddCensorLineTrigger(string)"/>,
		/// <see cref="AddCensorshipTrigger(string)"/>, <see cref="AddUncensorshipTrigger(string)"/>
		/// </summary>
		public void ClearCensorshipRules() => _censorshipRules.ClearRows();

		public void Initialize() {
			_filters = new List<ICommandFilter>();
			foreach (UnityEngine.Object obj in _commandFilters) {
				switch (obj) {
					case ICommandFilter iFilter:
						_filters.Add(iFilter);
						break;
					default:
						Debug.LogError($"unexpected filter type {obj.GetType().Name}, " +
							$"{name} expects only {nameof(ICommandFilter)} entries");
						break;
				}
			}
			_censorshipRules = new RegexMatrix(new RegexMatrix.Row[] {
				new RegexMatrix.Row(HideNextLineFunc, null),
				new RegexMatrix.Row(HideAllFunc, null),
				new RegexMatrix.Row(ShowAllFunc, null),
			});
			_censorshipRules.IsWaitingForTriggerRecalculate();
			if (CommandsToDo == null) {
				ParseCommands();
			}
		}

		private void HideNextLineFunc(string trigger) { _hideNextLine = true; }
		private void HideAllFunc(string trigger) { ShowOutput = false; }
		private void ShowAllFunc(string trigger) { ShowOutput = true; }

		public void ParseCommands() {
			_command.Parse();
		}

		public void RunCommand(string command, PrintCallback print, object context) {
			RunCommands(new string[] { command }, print, context);
		}

		public void RunCommands(string[] commands, PrintCallback print, object context) {
			ParsedTextCommand[] parsedTextCommands = new ParsedTextCommand[commands.Length];
			for(int i = 0; i < parsedTextCommands.Length; i++) {
				parsedTextCommands[i] = commands[i];
			}
			CommandsToDo = parsedTextCommands;
			RunCommands(context, print);
		}

		public void RunCommands(ParsedTextCommand[] commands, PrintCallback print, object context) {
			CommandsToDo = commands;
			RunCommands(context, print);
		}

		public void RunCommands(object context, PrintCallback print) {
			CommandExecution e = this.GetExecutionData(context);
			e.print = print;
			Initialize();
			e.StartRunningEachCommandInSequence();
		}

		public void InsertNextCommandToExecute(object context, string command) {
			CommandExecution e = this.GetExecutionData(context);
			e.InsertNextCommandToExecute(command);
		}

		public void StartCooperativeFunction(object context, string command, PrintCallback print) {
			this.GetExecutionData(context).StartCooperativeFunction(command, print);
		}

		public bool IsExecutionFinished(object context) => this.GetExecutionData(context).IsExecutionFinished();

		//public void ExecuteCommand(string command, object context, PrintCallback printCallback) {
		//	CommandExecution e = GetExecutionData(context);
		//	e.CommandsToDo
		//}

		public static void DelayCall(System.Action call) {
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				UnityEditor.EditorApplication.delayCall += () => call();
			} else
#endif
			{
				DelayCallUsingCoroutine(call);
			}
		}

		public static void DelayCallUsingCoroutine(System.Action call) {
			CoroutineRunner.Instance.StartCoroutine(DelayCall());
			System.Collections.IEnumerator DelayCall() {
				yield return null;
				call.Invoke();
			}
		}

		public void RemoveExecutionData(object context) => CommandRunnerExtension.RemoveExecutionData(this, context);

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
	}
}
