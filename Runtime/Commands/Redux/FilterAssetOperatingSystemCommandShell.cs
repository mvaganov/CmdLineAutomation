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
			public OperatingSystemCommandShell Shell {
				get => _shell;
				set {
					_shell = value;
					if (_shell != null) {
						Shell.Print = _print;
					}
				}
			}

			public override string name => throw new NotImplementedException();

			public override bool IsExecutionFinished => throw new NotImplementedException();

			public Proc(FilterAssetOperatingSystemCommandShell source, object context) {
				_source = source;
				_context = context;
			}

			public override float GetProgress() => 0;

			public override void StartCooperativeFunction(string command, PrintCallback print) {
				_print = print;
				bool missingShell = Shell == null;
				bool deadShell = !missingShell && !Shell.IsRunning;
				if (missingShell || deadShell) {
					string name = this.name;
					if (_context is UnityEngine.Object obj) {
						name = obj.name;
					}
					Shell = CreateShell(name, _context);
				}
				_shell.Run(command, _print);
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

		public ICommandProcess CreateCommand(object context) {
			throw new NotImplementedException();
		}
	}
}
