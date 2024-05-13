using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// Passes commands to a seperate thread running the local command terminal shell
	/// </summary>
	[CreateAssetMenu(fileName = "OperatingSystemCommandShell", menuName = "ScriptableObjects/Filters/OperatingSystemCommandShell")]
	public class FilterOperatingSystemCommandShell : ScriptableObject, ICommandFilter {
		/// <summary>
		/// The command line shell
		/// </summary>
		private OperatingSystemCommandShell _shell;
		/// <summary>
		/// Function to pass all lines from standard input to
		/// </summary>
		private TextResultCallback _stdOutput;
		private string _lastCalledCommand;

		public OperatingSystemCommandShell Shell {
			get => _shell;
			set {
				_shell = value;
				if (_shell != null) {
					Shell.LineOutput = _stdOutput;
				}
			}
		}

		public TextResultCallback StdOutput {
			get => _stdOutput;
			set {
				_stdOutput = value;
				if (Shell != null) {
					Shell.LineOutput = value;
				}
			}
		}

		public string FunctionResult() => null;

		public bool IsExecutionFinished() => true;

		public void StartCooperativeFunction(object context, string command, TextResultCallback stdOutput) {
			_stdOutput = stdOutput;
			_lastCalledCommand = command;
			if (Shell == null) {
				string name = this.name;
				if (context is UnityEngine.Object obj) {
					name = obj.name;
				}
				Shell = CreateShell(name, context);
			}
			_shell.Run(_lastCalledCommand, _stdOutput);
		}

		private OperatingSystemCommandShell CreateShell(string name, object context) {
			OperatingSystemCommandShell thisShell = OperatingSystemCommandShell.CreateUnityEditorShell(context);
			thisShell.Name = $"{name} {System.Environment.TickCount}";
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
}
