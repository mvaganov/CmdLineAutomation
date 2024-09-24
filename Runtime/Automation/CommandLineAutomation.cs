using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// CommandLineAutomation: has a specific command line instruction to execute, which it uses to populate a CommandLineExecutor
	/// * Metadata/description about why this instruction set exists
	/// * the instruction set
	/// </summary>
	[CreateAssetMenu(fileName = "CommandLineAutomation", menuName = "ScriptableObjects/CommandLineAutomation", order = 1)]
	/// TODO determine if the interfaces can be combined.
	/// TODO rewrite this while class... there is too much abstraction, and too many pieces of data that can't be audited in the inspector.
	public partial class CommandLineAutomation : ScriptableObject, CommandRunner<ProcedureExecution>, ICommandProcessor, ICommandAutomation, ICommandExecutor, ICommandReference {
		[SerializeField]
		protected CommandLineSettings _settings;

		//[SerializeField]
		protected CommandLineExecutor[] _executor;

		/// <summary>
		/// Information about what these commands are for
		/// </summary>
		[ContextMenuItem(nameof(ParseCommands), nameof(ParseCommands))]
		[SerializeField] protected TextCommand _command;
		[SerializeField] protected bool finishedDebug;
		[SerializeField] protected float progressDebug;

		public Dictionary<object, ProcedureExecution> _executions = new Dictionary<object, ProcedureExecution>();
		public Dictionary<object, ProcedureExecution> ExecutionDataAccess { get => _executions; set => _executions = value; }
		//private Dictionary<int, object> _executionDataByThread = new Dictionary<int, object>();
		//public Dictionary<int, object> ExecutionDataByThreadId { get => _executionDataByThread; set => _executionDataByThread = value; }
		public IEnumerable<object> GetContexts() => ExecutionDataAccess.Keys;

		public IList<ParsedTextCommand> CommandsToDo {
			get => _command.ParsedCommands;
			set => _command.ParsedCommands = new List<ParsedTextCommand>(value);
		}

		public ICommandExecutor CommandExecutor => GetCommandExecutor();

		public CommandLineExecutor GetCommandExecutor() => _executor != null && _executor.Length == 1 && _executor[0].Settings == _settings
			? _executor[0] : (_executor = new CommandLineExecutor[] { new CommandLineExecutor(_settings) })[0];

		private CommandLineSettings.MutableValues MutableSettings => GetCommandExecutor().MutableSettings;

		public IList<ICommandFilter> Filters => _settings.Filters;

		public TextCommand TextCommandData => _command;

		public string Commands {
			get => _command.Text;
			set {
				_command.Text = value;
				ParseCommands();
			}
		}

		public string CommandOutput {
			get => GetCommandExecutor().CommandOutput;
			set {
				GetCommandExecutor().CommandOutput = value;
			}
		}

		public bool ShowOutput {
			get => GetCommandExecutor().ShowOutput;
			set => GetCommandExecutor().ShowOutput = value;
		}

		public bool HideNextLine {
			get => GetCommandExecutor().HideNextLine;
			set => GetCommandExecutor().HideNextLine = value;
		}

		public System.Action<string> OnOutputChange {
			get { return GetCommandExecutor().OnOutputChange; }
			set { GetCommandExecutor().OnOutputChange = value; }
		}

		public RegexMatrix RegexTriggers => MutableSettings.RegexTriggers;

		public ICommandProcessor ReferencedCommand => GetCurrentCommand(this);

		public void AddToCommandOutput(string value) {
			GetCommandExecutor().AddToCommandOutput(value);
		}

		public float Progress(object context) {
			ProcedureExecution data = GetExecutionData(context);
			if (data == null) {
				return -1;
			}
			finishedDebug = data.IsExecutionFinished();
			return progressDebug = GetExecutionData(context).Progress;
		}

		public void CancelProcess(object context) => GetExecutionData(context).CancelExecution();

		public string CurrentCommandText(object context) => GetExecutionData(context).CurrentCommandText();

		public ICommandProcessor GetCurrentCommand(object context) => GetExecutionData(context).CurrentCommand();

		public ProcedureExecution CreateEmptyContextEntry(object context) {
			ProcedureExecution execution = GetCommandExecutor().CreateEmptyContextEntry(context);//new CommandExecution(context, GetCommandExecutor());
			return execution;
		}

		/// <summary>
		/// All <see cref="CommandRunner{ExecutionData}"/> functionality delegates to <see cref="GetCommandExecutor()"/>
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public ProcedureExecution GetExecutionData(object context) => GetCommandExecutor().GetExecutionData(context);

		public OperatingSystemCommandShell GetShell(object context) => GetExecutionData(context).Shell;

		public void ParseCommands() {
			_command.Parse();
		}

		public void RunCommand(string command, PrintCallback print, object context) {
			GetCommandExecutor().RunCommand(command, print, context);
		}

		public void RunCommands(string[] commands, PrintCallback print, object context) {
			GetCommandExecutor().RunCommands(commands, print, context);
		}

		public void RunCommands(ParsedTextCommand[] commands, PrintCallback print, object context) {
			GetCommandExecutor().RunCommands(commands, print, context);
		}

		public void RunCommands(object context, PrintCallback print) {
			GetCommandExecutor().RunCommands(context, print);
		}

		public void InsertNextCommandToExecute(object context, string command) {
			GetCommandExecutor().InsertNextCommandToExecute(context, command);
		}

		public void StartCooperativeFunction(object context, string command, PrintCallback print) {
			GetExecutionData(context).StartCooperativeFunction(command, print);
		}

		public bool IsExecutionFinished(object context) => GetExecutionData(context).IsExecutionFinished();

		public void RemoveExecutionData(object context) => CommandRunnerExtension.RemoveExecutionData(this, context);
	}
}
