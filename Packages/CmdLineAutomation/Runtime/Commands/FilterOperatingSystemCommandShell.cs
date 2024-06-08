using System;
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

		public class Execution {
			public string CurrentCommand;
			public string CurrentResult;
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

		public string FunctionResult(object context) => _consumeCommand ? null : GetExecutionData(context).CurrentResult;

		public override bool IsExecutionFinished(object context) => true;

		public override void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			Execution e = GetExecutionData(context);
			if (e == null) {
				SetExecutionData(context, e = new Execution());
			}
			e.CurrentResult = e.CurrentCommand = command;
			_stdOutput = stdOutput;
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
			long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			thisShell.Name = $"{name} {milliseconds}";
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
