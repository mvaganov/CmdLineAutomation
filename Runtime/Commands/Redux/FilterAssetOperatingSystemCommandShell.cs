using System;
using UnityEngine;

namespace RunCmdRedux {
	// TODO test me!
	/// <summary>
	/// Passes commands to a seperate thread running the local command terminal shell
	/// </summary>
	[CreateAssetMenu(fileName = "OperatingSystemCommandShell", menuName = "ScriptableObjects/FilterAssets/OperatingSystemCommandShell")]
	public class FilterAssetOperatingSystemCommandShell : ScriptableObject, ICommandAsset {
		private enum RegexVariable { TerminalVersion, WorkingDirecory }

		[Serializable]
		public class Proc : BaseNamedProcess {
			private FilterAssetOperatingSystemCommandShell _source;
			private object _context;
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
			private int _commandStartedMs;
			private int _commandExpectedFinishMs;
			private int _defaultCommandDurationMs = 500;
			public OperatingSystemCommandShell Shell {
				get => _shell;
				set {
					_shell = value;
					if (_shell != null) {
						Shell.Print = _print;
					}
				}
			}

			public override string name => _shell != null ? _shell.Name : null;

			public override ICommandProcess.State ExecutionState {
				get {
					if (!ShellIsExecutingSomething()) {
						return ICommandProcess.State.Finished;
					}
					return ICommandProcess.State.Executing;
				}
			}

			public Proc(FilterAssetOperatingSystemCommandShell source, object context) {
				_source = source;
				_context = context;
			}

			public bool ShellIsExecutingSomething() {
				return Environment.TickCount < _commandExpectedFinishMs;
			}

			public override float GetProgress() => (float)(Environment.TickCount - _commandStartedMs) / (_commandExpectedFinishMs - _commandStartedMs);

			public override void StartCooperativeFunction(string command, PrintCallback print) {
				_print = print;
				CreateShellAsNeeded();
				_commandStartedMs = Environment.TickCount;
				_commandExpectedFinishMs = _commandStartedMs + _defaultCommandDurationMs;
				_shell.Run(command, _print);
			}

			public void CreateShellAsNeeded() {
				bool missingShell = Shell == null;
				bool deadShell = !missingShell && !Shell.IsRunning;
				if (missingShell || deadShell) {
					if (this == null) {
						Debug.Log("THIS IS NULL?");
					}
					string name = this.name;
					if (_context is UnityEngine.Object obj) {
						name = obj.name;
					}
					Shell = CreateShell(name, _context);
				}
			}

			private OperatingSystemCommandShell CreateShell(string name, object context) {
				OperatingSystemCommandShell thisShell = OperatingSystemCommandShell.CreateUnityEditorShell(context);
				long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
				thisShell.Name = $"{name} {milliseconds}";
				thisShell.KeepAlive = () => {
					if (Shell != thisShell) {
						Debug.LogWarning($"lost {nameof(OperatingSystemCommandShell)}");
						thisShell.Stop();
						return false;
					}
					return true;
				};
				return thisShell;
			}

		}

		private Proc _currentProcess;

		public ICommandProcess CreateCommand(object context) {
			if (_currentProcess != null) {
				return _currentProcess;
			}
			return _currentProcess = new Proc(this, context);
		}
	}
}
