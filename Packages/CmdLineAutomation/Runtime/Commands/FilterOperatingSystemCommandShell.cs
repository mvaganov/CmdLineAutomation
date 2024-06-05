using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// Passes commands to a seperate thread running the local command terminal shell
	/// </summary>
	[CreateAssetMenu(fileName = "OperatingSystemCommandShell", menuName = "ScriptableObjects/Filters/OperatingSystemCommandShell")]
	public class FilterOperatingSystemCommandShell : CommandRunner<FilterOperatingSystemCommandShell.Execution>, ICommandFilter {
		private enum RegexVariable { TerminalVersion, WorkingDirecory }
		/// <summary>
		/// If true, does not pass command to others in the filter chain
		/// </summary>
		[SerializeField] protected bool _consumeCommand = true;

		/// <summary>
		/// The command line shell
		/// </summary>
		private OperatingSystemCommandShell _shell;

		/// <summary>
		/// Function to pass all lines from standard input to
		/// </summary>
		private TextResultCallback _stdOutput;

		/// <summary>
		/// Variables to read from command line input
		/// </summary>
		private NamedRegexSearch[] _variablesFromCommandLineRegexSearch = new NamedRegexSearch[] {
			new NamedRegexSearch("WindowsTerminalVersion", @"Microsoft Windows \[Version ([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\]", new int[] { 1 }, false),
			new NamedRegexSearch("dir", NamedRegexSearch.CommandPromptRegexWindows, null, false)
		};

		public class Execution {
			public string CurrentCommand;
			public string CurrentResult;
			public TextResultCallback StdOutput;
			private string[] _variableData = new string[0];
			public FilterOperatingSystemCommandShell Shell;
			public int VariableCount => _variableData.Length;

			public string VariableName(int index) {
				return Shell._variablesFromCommandLineRegexSearch[index].Name;
			}

			public string VariableData(int index) {
				return _variableData[index];
			}

			public void ReadLineFromTerminal(string line) {
				if (_variableData == null || _variableData.Length != Shell._variablesFromCommandLineRegexSearch.Length) {
					_variableData = new string[Shell._variablesFromCommandLineRegexSearch.Length];
				}
				for(int i = 0; i < Shell._variablesFromCommandLineRegexSearch.Length; ++i) {
					string result = Shell._variablesFromCommandLineRegexSearch[i].Process(line);
					if (result != null) {
						_variableData[i] = result;
						Shell._variablesFromCommandLineRegexSearch[i].RuntimeValue = result;
					}
				}
				StdOutput?.Invoke(line);
			}
		}

		public OperatingSystemCommandShell Shell {
			get => _shell;
			set {
				_shell = value;
				if (_shell != null) {
					Shell.LineOutput = _stdOutput;
				}
			}
		}

		public string TerminalVersion(object context) => GetExecutionData(context).VariableData((int)RegexVariable.TerminalVersion);

		public string WorkingDirecory(object context) => GetExecutionData(context).VariableData((int)RegexVariable.WorkingDirecory);

		public string FunctionResult(object context) => _consumeCommand ? null : GetExecutionData(context).CurrentResult;

		public override bool IsExecutionFinished(object context) => true;

		public override void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			Execution e = GetExecutionData(context);
			if (e == null) {
				SetExecutionData(context, e = new Execution());
				e.Shell = this;
			}
			e.CurrentResult = e.CurrentCommand = command;
			e.StdOutput = stdOutput;
			_stdOutput = e.ReadLineFromTerminal;
			bool missingShell = Shell == null;
			bool deadShell = !missingShell && !Shell.IsRunning;
			if (missingShell || deadShell) {
				string name = this.name;
				if (context is UnityEngine.Object obj) {
					name = obj.name;
				}
				Shell = CreateShell(name, context);
			}
			_shell.Run(command, _stdOutput);
		}

		protected override Execution CreateEmptyContextEntry(object context) => null;

		private OperatingSystemCommandShell CreateShell(string name, object context) {
			OperatingSystemCommandShell thisShell = OperatingSystemCommandShell.CreateUnityEditorShell(context);
			thisShell.Name = $"{name} {System.Environment.TickCount}";
			thisShell.KeepAlive = () => {
				if (Shell != thisShell) {
					Debug.LogWarning($"lost {nameof(OperatingSystemCommandShell)}");
					thisShell.Stop();
					RemoveExecutionData(context);
					return false;
				}
				return true;
			};
			return thisShell;
		}

		public override float Progress(object context) => 0;
	}
}
