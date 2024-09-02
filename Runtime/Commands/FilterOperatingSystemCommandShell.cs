using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// Passes commands to a seperate thread running the local command terminal shell
	/// </summary>
	[CreateAssetMenu(fileName = "OperatingSystemCommandShell", menuName = "ScriptableObjects/Filters/OperatingSystemCommandShell")]
	public class FilterOperatingSystemCommandShell : ScriptableObject, CommandRunner<FilterOperatingSystemCommandShell.Execution>, ICommandFilter {
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
		private PrintCallback _print;

		public class Execution {
			public string CurrentCommand;
			public string CurrentResult;
		}

		public OperatingSystemCommandShell Shell {
			get => _shell;
			set {
				_shell = value;
				if (_shell != null) {
					Shell.Print = _print;
				}
			}
		}

		private Dictionary<object, Execution> _executionData = new Dictionary<object, Execution>();
		public Dictionary<object, Execution> ExecutionDataAccess { get => _executionData; set => _executionData = value; }
		//private Dictionary<int, object> _executionDataByThread = new Dictionary<int, object>();
		//public Dictionary<int, object> ExecutionDataByThreadId { get => _executionDataByThread; set => _executionDataByThread = value; }
		public ICommandProcessor GetReferencedCommand(object context) => this;

		public IEnumerable<object> GetContexts() => ExecutionDataAccess.Keys;

		public string FilterResult(object context) => _consumeCommand ? null : this.GetExecutionData(context).CurrentResult;

		public bool IsExecutionFinished(object context) => true;

		public void StartCooperativeFunction(object context, string command, PrintCallback print) {
			Execution e = this.GetExecutionData(context);
			if (e == null) {
				this.SetExecutionData(context, e = new Execution());
			}
			e.CurrentResult = e.CurrentCommand = command;
			_print = print;
			bool missingShell = Shell == null;
			bool deadShell = !missingShell && !Shell.IsRunning;
			if (missingShell || deadShell) {
				string name = this.name;
				if (context is UnityEngine.Object obj) {
					name = obj.name;
				}
				Shell = CreateShell(name, context);
			}
			_shell.Run(command, _print);
		}

		public Execution CreateEmptyContextEntry(object context) => null;

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

		public float Progress(object context) => 0;

		public void RemoveExecutionData(object context) => CommandRunnerExtension.RemoveExecutionData(this, context);
	}
}
