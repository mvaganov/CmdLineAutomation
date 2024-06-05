using System.Collections.Generic;
using System.Text;
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
	public partial class CommandAutomation : CommandRunner<CommandAutomation.CommandExecution>, ICommandProcessor {
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
		/// List if filtering functions for input, which may or may not consume a command
		/// </summary>
		private List<ICommandFilter> _filters;

		public IList<ParsedTextCommand> CommandsToDo => _command.ParsedCommands;

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
					case ICommandFilter iFilter:
						_filters.Add(iFilter);
						break;
					default:
						Debug.LogError($"unexpected filter type {obj.GetType().Name}, " +
							$"{name} expects only {nameof(ICommandFilter)} entries");
						break;
				}
			}
			ParseCommands();
		}

		public void ParseCommands() {
			_command.Parse();
		}

		public void RunCommands(object context, TextResultCallback stdOutput) {
			CommandExecution e = GetExecutionData(context);
			e.stdOutput = stdOutput;
			Initialize();
			e.StartRunningEachCommandInSequence();
		}

		public void InsertNextCommandToExecute(object context, string command) {
			CommandExecution e = GetExecutionData(context);
			e.InsertNextCommandToExecute(command);
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
