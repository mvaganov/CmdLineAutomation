using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// CommandLineExecutor: has the runtime variables of a command line, like the parsed commands to execute, the command execution stack, specific found variable data. Executes commands for a command line defined by CommandLineSettings.
	/// * Logic to process commands as a cooperative process
	///   * does not block the Unity thread
	///   * tracks state of which command is executing now
	///   * can be cancelled
	/// * A list of commands to execute
	/// * keeps track of command output, which can be filtered by line with regular expressions
	/// * can be fed commands in the Unity Editor, or from runtime methods
	/// </summary>
	public partial class CommandLineExecutor : CommandRunner<CommandExecution>, ICommandAutomation, ICommandExecutor {
		[SerializeField] protected CommandLineSettings _settings;

		/// <summary>
		/// Command being typed into the command prompt by the Unity Editor user
		/// </summary>
		private string _inspectorCommandOutput;

		private bool _showOutput = true;
		private bool _hideNextLine = false;
		private List<(int row, int col)> _triggeredGroup = new List<(int row, int col)>();
		private System.Action<string> _onOutputChange;
		private CommandLineSettings.MutableValues _mutableSettings;
		private IList<ParsedTextCommand> _commandsToDo;

		public IList<ParsedTextCommand> CommandsToDo {
			get => _commandsToDo;
			set => _commandsToDo = value;
		}

		public ICommandExecutor CommandExecutor => this;

		public IList<ICommandFilter> Filters => _settings.Filters;

		public RegexMatrix RegexTriggers => _mutableSettings.RegexTriggers;

		public CommandLineSettings Settings => _settings;

		public CommandLineSettings.MutableValues MutableSettings => _mutableSettings;

		public string CommandOutput {
			get => _inspectorCommandOutput;
			set {
				_inspectorCommandOutput = value;
				_onOutputChange?.Invoke(_inspectorCommandOutput);
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

		public CommandLineExecutor(CommandLineSettings settings) {
			_settings = settings;
			_mutableSettings = _settings._runtimeSettings.Clone();
		}

		public void AddToCommandOutput(string value) {
			if (RegexTriggers.HasRegexTriggers) {
				RegexTriggers.ProcessAndCheckTextForTriggeringLines(value, AddProcessedLineToCommandOutput, _triggeredGroup);
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

		public CommandExecution GetExecutionData(object context) => ((CommandRunner<CommandExecution>)this).GetExecutionData(context);

		public float Progress(object context) => GetExecutionData(context).Progress;

		public void CancelProcess(object context) => GetExecutionData(context).CancelExecution();

		public string CurrentCommandText(object context) => GetExecutionData(context).CurrentCommandText();

		public ICommandProcessor CurrentCommand(object context) => GetExecutionData(context).CurrentCommand();

		public CommandExecution CreateEmptyContextEntry(object context) {
			CommandExecution execution = new CommandExecution(context, this);
			//Debug.Log($"$$$$$$$$$$$$$$$$$$ HEY! I'm {this}({this.GetHashCode()}), for {context}({context.GetHashCode()})");
			return execution;
		}

		public OperatingSystemCommandShell GetShell(object context) => GetExecutionData(context).Shell;

		/// <inheritdoc cref="CommandLineSettings.AddCensorshipTrigger(string)"/>
		public void AddCensorshipTrigger(string regexTrigger) => _mutableSettings.AddCensorshipTrigger(regexTrigger);

		/// <inheritdoc cref="CommandLineSettings.AddUncensorshipTrigger(string)"/>
		public void AddUncensorshipTrigger(string regexTrigger) => _mutableSettings.AddUncensorshipTrigger(regexTrigger);

		/// <inheritdoc cref="CommandLineSettings.AddCensorLineTrigger(string)"/>
		public void AddCensorLineTrigger(string regexTrigger) => _mutableSettings.AddCensorLineTrigger(regexTrigger);

		/// <inheritdoc cref="CommandLineSettings.RemoveCensorshipTrigger(string)"/>
		public bool RemoveCensorshipTrigger(string regexTrigger) => _mutableSettings.RemoveCensorshipTrigger(regexTrigger);

		/// <inheritdoc cref="CommandLineSettings.RemoveUncensorshipTrigger(string)"/>
		public bool RemoveUncensorshipTrigger(string regexTrigger) => _mutableSettings.RemoveUncensorshipTrigger(regexTrigger);

		/// <inheritdoc cref="CommandLineSettings.ClearCensorshipRules()"/>
		public void ClearCensorshipRules() => _mutableSettings.ClearCensorshipRules();

		public void RunCommand(string command, PrintCallback print, object context) {
			RunCommands(new string[] { command }, print, context);
		}

		public void RunCommands(string[] commands, PrintCallback print, object context) {
			ParsedTextCommand[] parsedTextCommands = new ParsedTextCommand[commands.Length];
			for (int i = 0; i < parsedTextCommands.Length; i++) {
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
			e.StartRunningEachCommandInSequence(CommandsToDo);
		}

		public void InsertNextCommandToExecute(object context, string command) {
			CommandExecution e = this.GetExecutionData(context);
			e.InsertNextCommandToExecute(command);
		}

		public void StartCooperativeFunction(object context, string command, PrintCallback print) {
			this.GetExecutionData(context).StartCooperativeFunction(command, print);
		}

		public bool IsExecutionFinished(object context) => this.GetExecutionData(context).IsExecutionFinished();

		public void ClearOutput(object context) {
			_inspectorCommandOutput = "";
			OnOutputChange?.Invoke(_inspectorCommandOutput);
		}

		public void RemoveExecutionData(object context) => CommandRunnerExtension.RemoveExecutionData(this, context);
	}
}
