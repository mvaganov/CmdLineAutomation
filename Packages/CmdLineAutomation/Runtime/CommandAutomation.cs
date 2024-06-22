using System.Collections;
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
	/// * keeps track of command output, which can be filtered by line with regular expressions
	/// </summary>
	[CreateAssetMenu(fileName = "NewCmdLineAutomation", menuName = "ScriptableObjects/CmdLineAutomation", order = 1)]
	public partial class CommandAutomation : CommandRunner<CommandAutomation.CommandExecution>, ICommandProcessor, ICommandAutomation {
		private enum RegexGroupId { None = -1, HideNextLine = 0, DisableOnRead, EnableOnRead }
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
		/// </summary>
		[SerializeField] protected UnityEngine.Object[] _commandFilters;

		/// <summary>
		/// Variables to read from command line input
		/// </summary>
		[SerializeField] protected NamedRegexSearch[] _variablesFromCommandLineRegexSearch = new NamedRegexSearch[] {
			new NamedRegexSearch("WindowsTerminalVersion", @"Microsoft Windows \[Version ([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\]", new int[] { 1 }, true),
			new NamedRegexSearch("dir", NamedRegexSearch.CommandPromptRegexWindows, null, true)
		};

		/// <summary>
		/// Information about what these commands are for
		/// </summary>
		[ContextMenuItem(nameof(ParseCommands),nameof(ParseCommands))]
		[SerializeField] protected TextCommand _command;

		public bool _recapOutputAtEnd;

		/// <summary>
		/// Command being typed into the command prompt by the Unity Editor user
		/// </summary>
		private string _inspectorCommandOutput;

		/// <summary>
		/// List if filtering functions for input, which may or may not consume a command
		/// </summary>
		private List<ICommandFilter> _filters;

		private RegexMatrix regexMatrix = new RegexMatrix();
		private bool _showOutput = true;
		private bool _hideNextLine = false;
		private List<(int row, int col)> _triggeredGroup = new List<(int row, int col)>();
		private System.Action<string> _onOutputChange;

		public IList<ParsedTextCommand> CommandsToDo {
			get => _command.ParsedCommands;
			set => _command.ParsedCommands = new List<ParsedTextCommand>(value);
		}

		public CommandAutomation CommandExecutor => this;

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

		public string CommandOutput => _inspectorCommandOutput;

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

		public void AddToCommandOutput(string value) {
			if (regexMatrix.HasRegexTriggers) {
				regexMatrix.ProcessAndCheckTextForTriggeringLines(value, AddProcessedLineToCommandOutput, _triggeredGroup);
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

		public override float Progress(object context) => GetExecutionData(context).Progress;

		public void CancelProcess(object context) => GetExecutionData(context).CancelExecution();
		
		public string CurrentCommandText(object context) => GetExecutionData(context).CurrentCommandText();
		
		public ICommandProcessor CurrentCommand(object context) => GetExecutionData(context).CurrentCommand();
		
		protected override CommandExecution CreateEmptyContextEntry(object context)
			=> new CommandExecution(context, this);

		public OperatingSystemCommandShell GetShell(object context) => GetExecutionData(context).Shell;

		private bool NeedsInitialization() => _filters == null;

		/// <summary>
		/// If the given regex is triggered, all output will be hidden (until <see cref="AddShowAllTrigger(string)"/>)
		/// </summary>
		/// <param name="regexTrigger"></param>
		public void AddHideAllTrigger(string regexTrigger) => regexMatrix.Add((int)RegexGroupId.DisableOnRead, regexTrigger);

		/// <summary>
		/// If the given regex is triggered, all output will be shown again
		/// </summary>
		/// <param name="regexTrigger"></param>
		public void AddShowAllTrigger(string regexTrigger) => regexMatrix.Add((int)RegexGroupId.EnableOnRead, regexTrigger);
		/// <summary>
		/// Hide lines that contain the given regex trigger
		/// </summary>
		/// <param name="regexTrigger"></param>
		public void AddHideLineTrigger(string regexTrigger) => regexMatrix.Add((int)RegexGroupId.HideNextLine, regexTrigger);
		/// <summary>
		/// Remove a regex trigger added by <see cref="AddHideAllTrigger(string)"/>
		/// </summary>
		/// <param name="regexTrigger"></param>
		/// <returns></returns>
		public bool RemoveHideAllTrigger(string regexTrigger) => regexMatrix.Remove((int)RegexGroupId.DisableOnRead, regexTrigger);
		/// <summary>
		/// Remove a regex trigger added by <see cref="AddShowAllTrigger(string)"/>
		/// </summary>
		/// <param name="regexTrigger"></param>
		/// <returns></returns>
		public bool RemoveShowAllTrigger(string regexTrigger) => regexMatrix.Remove((int)RegexGroupId.EnableOnRead, regexTrigger);
		/// <summary>
		/// Remove all regex triggers added by <see cref="AddHideLineTrigger(string)"/>,
		/// <see cref="AddHideAllTrigger(string)"/>, <see cref="AddShowAllTrigger(string)"/>
		/// </summary>
		public void ClearRegexFilterRules() => regexMatrix.ClearRows();

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
			if (CommandsToDo == null) {
				ParseCommands();
			}
			regexMatrix = new RegexMatrix(new RegexMatrix.Row[] {
				new RegexMatrix.Row(HideNextLineFunc, null),
				new RegexMatrix.Row(HideAllFunc, null),
				new RegexMatrix.Row(ShowAllFunc, null),
			});
			regexMatrix.IsWaitingForTriggerRecalculate();
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
			CommandExecution e = GetExecutionData(context);
			e.print = print;
			Initialize();
			e.StartRunningEachCommandInSequence();
		}

		public void InsertNextCommandToExecute(object context, string command) {
			CommandExecution e = GetExecutionData(context);
			e.InsertNextCommandToExecute(command);
		}

		public override void StartCooperativeFunction(object context, string command, PrintCallback print) {
			GetExecutionData(context).StartCooperativeFunction(command, print);
		}

		public override bool IsExecutionFinished(object context) => GetExecutionData(context).IsExecutionFinished();

		public void ClearOutput(object context) {
			_inspectorCommandOutput = "";
			OnOutputChange?.Invoke(_inspectorCommandOutput);
		}

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
