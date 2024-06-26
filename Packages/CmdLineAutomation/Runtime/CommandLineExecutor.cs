//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace RunCmd {
//	/// <summary>
//	/// TODO refactor:

//	/// CommandLineExecutor: has the runtime variables of a command line, like the parsed commands to execute, the command execution stack, specific found variable data. Executes commands for a command line defined by CommandLineSettings.
//	/// * Logic to process commands as a cooperative process
//	///   * does not block the Unity thread
//	///   * tracks state of which command is executing now
//	///   * can be cancelled
//	/// * A list of commands to execute
//	/// * keeps track of command output, which can be filtered by line with regular expressions
//	/// * can be fed commands in the Unity Editor, or from runtime methods

//	/// CommandLineAutomation: has a specific command line instruction to execute, which it uses to populate a CommandLineExecutor
//	/// * Metadata/description about why this instruction set exists
//	/// * the instruction set

//	/// </summary>
//	[CreateAssetMenu(fileName = "CmdLineExecutor", menuName = "ScriptableObjects/CmdLineExecutor", order = 1)]
//	public partial class CommandLineExecutor : CommandRunner<CommandExecution>, ICommandProcessor, ICommandAutomation {
//		[SerializeField] protected CommandLineSettings _settings;

//		/// <summary>
//		/// Command being typed into the command prompt by the Unity Editor user
//		/// </summary>
//		private string _inspectorCommandOutput;

//		private bool _showOutput = true;
//		private bool _hideNextLine = false;
//		private List<(int row, int col)> _triggeredGroup = new List<(int row, int col)>();
//		private System.Action<string> _onOutputChange;

//		IList<ParsedTextCommand> _commandsToDo;

//		///// <summary>
//		///// Variables to read from command line input
//		///// </summary>
//		//[SerializeField]
//		//protected NamedRegexSearch[] _variablesFromCommandLineRegexSearch = new NamedRegexSearch[] {
//		//	// TODO copy from _settings
//		//};

//		public IList<ParsedTextCommand> CommandsToDo {
//			get => _commandsToDo;
//			set => _commandsToDo = new List<ParsedTextCommand>(value);
//		}

//		public CommandLineExecutor CommandExecutor => this;

//		public IList<NamedRegexSearch> VariablesFromCommandLineRegexSearch => _settings.VariablesFromCommandLineRegexSearch;// _variablesFromCommandLineRegexSearch;

//		public RegexMatrix CensorshipRules => _settings.CensorshipRules;
		
//		public string CommandOutput => _inspectorCommandOutput;

//		public bool ShowOutput {
//			get => _showOutput;
//			set => _showOutput = value;
//		}

//		public bool HideNextLine {
//			get => _hideNextLine;
//			set => _hideNextLine = value;
//		}

//		public System.Action<string> OnOutputChange {
//			get { return _onOutputChange; }
//			set { _onOutputChange = value; }
//		}

//		public void AddToCommandOutput(string value) {
//			if (_settings.CensorshipRules.HasRegexTriggers) {
//				_settings.CensorshipRules.ProcessAndCheckTextForTriggeringLines(value, AddProcessedLineToCommandOutput, _triggeredGroup);
//			} else {
//				AddLineToCommandOutputInternal(value);
//			}
//		}

//		private void AddProcessedLineToCommandOutput(string processedLine) {
//			if (ShowOutput && !_hideNextLine) {
//				AddLineToCommandOutputInternal(processedLine);
//			}
//			_hideNextLine = false;
//		}

//		private void AddLineToCommandOutputInternal(string line) {
//			_inspectorCommandOutput += line;
//			OnOutputChange?.Invoke(_inspectorCommandOutput);
//		}

//		public override float Progress(object context) => GetExecutionData(context).Progress;

//		public void CancelProcess(object context) => GetExecutionData(context).CancelExecution();

//		public string CurrentCommandText(object context) => GetExecutionData(context).CurrentCommandText();

//		public ICommandProcessor CurrentCommand(object context) => GetExecutionData(context).CurrentCommand();

//		protected override CommandExecution CreateEmptyContextEntry(object context)
//			=> new CommandExecution(context, this);

//		public OperatingSystemCommandShell GetShell(object context) => GetExecutionData(context).Shell;

//		/// <inheritdoc cref="CommandLineSettings.AddCensorshipTrigger(string)"/>
//		public void AddCensorshipTrigger(string regexTrigger) => _settings.AddCensorshipTrigger(regexTrigger);

//		/// <inheritdoc cref="CommandLineSettings.AddUncensorshipTrigger(string)"/>
//		public void AddUncensorshipTrigger(string regexTrigger) => _settings.AddUncensorshipTrigger(regexTrigger);

//		/// <inheritdoc cref="CommandLineSettings.AddCensorLineTrigger(string)"/>
//		public void AddCensorLineTrigger(string regexTrigger) => _settings.AddCensorLineTrigger(regexTrigger);

//		/// <inheritdoc cref="CommandLineSettings.RemoveCensorshipTrigger(string)"/>
//		public bool RemoveCensorshipTrigger(string regexTrigger) => _settings.RemoveCensorshipTrigger(regexTrigger);

//		/// <inheritdoc cref="CommandLineSettings.RemoveUncensorshipTrigger(string)"/>
//		public bool RemoveUncensorshipTrigger(string regexTrigger) => _settings.RemoveUncensorshipTrigger(regexTrigger);

//		/// <inheritdoc cref="CommandLineSettings.ClearCensorshipRules()"/>
//		public void ClearCensorshipRules() => _settings.ClearCensorshipRules();

//		public void Initialize() {
//			// TODO make a mutable copy of _settings
//			_filters = new List<ICommandFilter>();
//			foreach (UnityEngine.Object obj in _commandFilters) {
//				switch (obj) {
//					case ICommandFilter iFilter:
//						_filters.Add(iFilter);
//						break;
//					default:
//						Debug.LogError($"unexpected filter type {obj.GetType().Name}, " +
//							$"{name} expects only {nameof(ICommandFilter)} entries");
//						break;
//				}
//			}
//			_censorshipRules = new RegexMatrix(new RegexMatrix.Row[] {
//				new RegexMatrix.Row(HideNextLineFunc, null),
//				new RegexMatrix.Row(HideAllFunc, null),
//				new RegexMatrix.Row(ShowAllFunc, null),
//			});
//			_censorshipRules.IsWaitingForTriggerRecalculate();
//			if (CommandsToDo == null) {
//				ParseCommands();
//			}
//		}

//		private void HideNextLineFunc(string trigger) { _hideNextLine = true; }
//		private void HideAllFunc(string trigger) { ShowOutput = false; }
//		private void ShowAllFunc(string trigger) { ShowOutput = true; }

//		public void ParseCommands() {
//			_command.Parse();
//		}

//		public void RunCommand(string command, PrintCallback print, object context) {
//			RunCommands(new string[] { command }, print, context);
//		}

//		public void RunCommands(string[] commands, PrintCallback print, object context) {
//			ParsedTextCommand[] parsedTextCommands = new ParsedTextCommand[commands.Length];
//			for (int i = 0; i < parsedTextCommands.Length; i++) {
//				parsedTextCommands[i] = commands[i];
//			}
//			CommandsToDo = parsedTextCommands;
//			RunCommands(context, print);
//		}

//		public void RunCommands(ParsedTextCommand[] commands, PrintCallback print, object context) {
//			CommandsToDo = commands;
//			RunCommands(context, print);
//		}

//		public void RunCommands(object context, PrintCallback print) {
//			CommandExecution e = GetExecutionData(context);
//			e.print = print;
//			Initialize();
//			e.StartRunningEachCommandInSequence();
//		}

//		public void InsertNextCommandToExecute(object context, string command) {
//			CommandExecution e = GetExecutionData(context);
//			e.InsertNextCommandToExecute(command);
//		}

//		public override void StartCooperativeFunction(object context, string command, PrintCallback print) {
//			GetExecutionData(context).StartCooperativeFunction(command, print);
//		}

//		public override bool IsExecutionFinished(object context) => GetExecutionData(context).IsExecutionFinished();

//		public void ClearOutput(object context) {
//			_inspectorCommandOutput = "";
//			OnOutputChange?.Invoke(_inspectorCommandOutput);
//		}

//		//public void ExecuteCommand(string command, object context, PrintCallback printCallback) {
//		//	CommandExecution e = GetExecutionData(context);
//		//	e.CommandsToDo
//		//}

//		public static void DelayCall(System.Action call) {
//#if UNITY_EDITOR
//			if (!Application.isPlaying) {
//				UnityEditor.EditorApplication.delayCall += () => call();
//			} else
//#endif
//			{
//				DelayCallUsingCoroutine(call);
//			}
//		}

//		public static void DelayCallUsingCoroutine(System.Action call) {
//			CoroutineRunner.Instance.StartCoroutine(DelayCall());
//			System.Collections.IEnumerator DelayCall() {
//				yield return null;
//				call.Invoke();
//			}
//		}
//		private class CoroutineRunner : MonoBehaviour {
//			private static CoroutineRunner _instance;
//			public static CoroutineRunner Instance {
//				get {
//					if (_instance != null) { return _instance; }
//					GameObject go = new GameObject("<CoroutineRunner>");
//					DontDestroyOnLoad(go);
//					return _instance = go.AddComponent<CoroutineRunner>();
//				}
//			}
//		}
//	}
//}
