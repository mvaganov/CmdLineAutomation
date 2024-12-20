using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;

namespace RunCmdRedux {
	public class OperatingSystemCommandShell {
		/// <summary>
		/// What object owns this shell, which might be used to get scope-specific data
		/// </summary>
		private object _context;
		/// <summary>
		/// What to name this object
		/// </summary>
		private string _name;
		private System.Diagnostics.ProcessStartInfo _startInfo;
		private System.Diagnostics.Process _process;
		private System.Threading.Thread _thread;
		private StreamReader _output;
		private StringBuilder _lineBuffer = new StringBuilder();
		private bool _running = false;
		char[] _readBuffer = new char[1024];
		public PrintCallback Print = delegate { };
		/// <summary>
		/// Condition to end this command shell thread
		/// </summary>
		public Func<bool> KeepAlive;
		/// <summary>
		/// Variables to read from command line input
		/// </summary>
		private NamedRegexSearch[] _variablesFromCommandLineRegexSearch = new NamedRegexSearch[] {
			new NamedRegexSearch("WindowsTerminalVersion",
				@"Microsoft Windows \[Version ([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\]",
				new int[] { 1 }, false, NamedRegexSearch.SpecialReadLogic.IgnoreAfterFirstValue),
			new NamedRegexSearch("dir", NamedRegexSearch.CommandPromptRegexWindows, null, false,
				NamedRegexSearch.SpecialReadLogic.None)
		};

		public static Dictionary<object, OperatingSystemCommandShell> RunningShells
			= new Dictionary<object, OperatingSystemCommandShell>();

		public static OperatingSystemCommandShell CreateUnityEditorShell(object context) =>
			new OperatingSystemCommandShell(context, "cmd.exe", Path.Combine(Application.dataPath, ".."));

		public string WorkingDirectory => GetVariableValue(1);

		public string Name { get => _name; set => _name = value; }

		public OperatingSystemCommandShell Shell => this;

		public bool IsRunning {
			get => _running && _thread.IsAlive && (KeepAlive == null || KeepAlive.Invoke());
			set {
				if (_running && !value) {
					Stop();
				}
				_running = value;
			}
		}

		public OperatingSystemCommandShell(object context,
		string command = "cmd.exe", string workingDirectory = "C:\\Windows\\System32\\") {
			_context = context;
			if (_context is UnityEngine.Object obj) {
				_name = obj.name;
			}
			_startInfo = new System.Diagnostics.ProcessStartInfo(command);
			_startInfo.WorkingDirectory = workingDirectory;
			_startInfo.UseShellExecute = false;
			_startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			_startInfo.CreateNoWindow = true;
			_startInfo.ErrorDialog = false;
			_startInfo.RedirectStandardInput = true;
			_startInfo.RedirectStandardOutput = true;
			_process = new System.Diagnostics.Process();
			_process.StartInfo = _startInfo;
			_process.Start();
			_output = _process.StandardOutput;
			_thread = new System.Threading.Thread(Thread);
			_thread.Start();
			if (RunningShells.TryGetValue(context, out var shell)) {
				Debug.LogWarning($"replacing shell for {context}");
			}
			RunningShells[_context] = this;
		}

		~OperatingSystemCommandShell() {
			RunningShells.Remove(_context);
			try {
				Stop();
			} catch { }
		}

		void Thread() {
			_running = true;
			try {
				while (IsRunning && Reading()) ;
			} catch (System.Threading.ThreadAbortException) {
#if UNITY_EDITOR
				Debug.LogWarning($"Aborted {nameof(OperatingSystemCommandShell)}: {Name}");
#endif
			} catch (Exception e) {
#if UNITY_EDITOR
				Debug.LogException(e);
#endif
			}
			if (_running) {
				Stop();
			}
			_running = false;
		}

		private bool Reading() {
			int read = _output.Read(_readBuffer, 0, _readBuffer.Length);
			if (read > 0) {
				string line = new string(_readBuffer, 0, read);
				RegexProcessLineFromTerminal(line);
				Print?.Invoke(line);
			} else {
				return false;
			}
			return true;
		}

		public void RunCommand(string command) {
			//Debug.Log($"running \"{command}\"");
			if (IsRunning) {
				_process.StandardInput.WriteLine(command);
				_process.StandardInput.Flush();
			}
		}

		public void Stop() {
			_running = false;
			if (_process != null) {
				try {
					_process.Kill();
				} catch (Exception e) {
					Debug.LogWarning(e);
				}
				_thread.Join(200);
				_thread.Abort();
				_process = null;
				_thread = null;
			}
			RunningShells.Remove(_context);
		}

		public string GetCurrentLine() {
			if (!IsRunning)
				return null;
			return _lineBuffer.ToString();
		}

		public void Run(string command, PrintCallback print) {
			Print = print;
			RunCommand(command);
		}

		public void Exit() {
			RunCommand("exit");
		}

		public string GetVariableName(int index) {
			return Shell._variablesFromCommandLineRegexSearch[index].Name;
		}

		public string GetVariableValue(int index) {
			return Shell._variablesFromCommandLineRegexSearch[index].RuntimeValue;
		}

		public void RegexProcessLineFromTerminal(string line) {
			int countProcessed = 0;
			for (int i = 0; i < Shell._variablesFromCommandLineRegexSearch.Length; ++i) {
				NamedRegexSearch regexSearch = Shell._variablesFromCommandLineRegexSearch[i];
				if (regexSearch.Ignore) { continue; }
				string result =
				regexSearch.Process(line);
				if (result != null) {
					//Debug.LogWarning(result+"\n"+ regexSearch.Name+":"+regexSearch.RuntimeValue);
					++countProcessed;
				}
			}
		}
	}
}
