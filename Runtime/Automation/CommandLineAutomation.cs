using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// CommandLineAutomation: has a specific command line instruction to execute, which it uses to populate a CommandLineExecutor
	/// * Metadata/description about why this instruction set exists
	/// * the instruction set
	/// </summary>
	[CreateAssetMenu(fileName = "CommandLineAutomation", menuName = "ScriptableObjects/CommandLineAutomation", order = 1)]
	public partial class CommandLineAutomation : ScriptableObject, CommandRunner<CommandExecution>, ICommandProcessor, ICommandAutomation, ICommandExecutor {
		[SerializeField]
		protected CommandLineSettings _settings;
		protected CommandLineExecutor _executor;

		/// <summary>
		/// Information about what these commands are for
		/// </summary>
		[ContextMenuItem(nameof(ParseCommands), nameof(ParseCommands))]
		[SerializeField] protected TextCommand _command;

		public Dictionary<object, CommandExecution> _executions = new Dictionary<object, CommandExecution>();

		public Dictionary<object, CommandExecution> ExecutionDataAccess { get => _executions; set => _executions = value; }

		public IList<ParsedTextCommand> CommandsToDo {
			get => _command.ParsedCommands;
			set => _command.ParsedCommands = new List<ParsedTextCommand>(value);
		}

		public ICommandExecutor CommandExecutor => GetCommandExecutor();

		public CommandLineExecutor GetCommandExecutor() => _executor != null && _executor.Settings == _settings ? _executor : _executor = new CommandLineExecutor(_settings);

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

		public void AddToCommandOutput(string value) {
			GetCommandExecutor().AddToCommandOutput(value);
		}

		public float Progress(object context) => GetExecutionData(context).Progress;

		public void CancelProcess(object context) => GetExecutionData(context).CancelExecution();

		public string CurrentCommandText(object context) => GetExecutionData(context).CurrentCommandText();

		public ICommandProcessor CurrentCommand(object context) => GetExecutionData(context).CurrentCommand();

		public CommandExecution CreateEmptyContextEntry(object context) {
			CommandExecution execution = GetCommandExecutor().CreateEmptyContextEntry(context);//new CommandExecution(context, GetCommandExecutor());
			return execution;
		}

		/// <summary>
		/// All <see cref="CommandRunner{ExecutionData}"/> functionality delegates to <see cref="GetCommandExecutor()"/>
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public CommandExecution GetExecutionData(object context) => GetCommandExecutor().GetExecutionData(context);

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
