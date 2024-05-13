using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

namespace RunCmd {
	// TODO keep track of the current working directory
	// TODO read formatted output from command line programs and do something with that logic?
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
		private string _lineBuffer = ""; // TODO semaphores to make StringBuilder threadsafe
		private List<string> _lines = new List<string>();
		private bool _running = false;
		public TextResultCallback LineOutput = delegate { };
		public Func<bool> KeepAlive;

		public static Dictionary<object, OperatingSystemCommandShell> RunningShells
			= new Dictionary<object, OperatingSystemCommandShell>();

		public static OperatingSystemCommandShell CreateUnityEditorShell(object context) =>
			new OperatingSystemCommandShell(context, "cmd.exe", Path.Combine(Application.dataPath, ".."));
		
		public int LineCount => _lines.Count;

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
			//Debug.Log($"!!!!!!!!!!!!!!!!!!!!!!! Creating shell \"{Name}\" for '{_context}'");
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

		public void RunCommand(string command) {
			Debug.Log($"running \"{command}\"");
			if (IsRunning) {
				_process.StandardInput.WriteLine(command);
				_process.StandardInput.Flush();
			}
		}

		public void Stop() {
			_running = false;
			if (_process != null) {
				_process.Kill();
				_thread.Join(200);
				_thread.Abort();
				_process = null;
				_thread = null;
			}
			RunningShells.Remove(this);
		}

		public string GetCurrentLine() {
			if (!IsRunning)
				return null;
			return _lineBuffer.ToString();
		}

		public void GetRecentLines(List<string> aLines) {
			if (!IsRunning || aLines == null) {
				return;
			}
			PeekRecentLines(aLines);
		}

		public void PeekRecentLines(List<string> aLines) {
			lock (_lines) {
				if (_lines.Count > 0) {
					aLines.AddRange(_lines);
				}
			}
		}

		private bool Reading() {
			int c = _output.Read();
			if (c <= 0) {
				return false;
			} else if (c == '\n') {
				lock (_lines) {
					string line = GetCurrentLine();
					_lines.Add(line);
					_lineBuffer = "";//.Clear();
					LineOutput?.Invoke(line);
				}
			} else if (c != '\r') {
				_lineBuffer += ((char)c);
			}
			return true;
		}

		public void ClearLines() {
			lock (_lines) {
				_lines.Clear();
			}
		}

		public void Run(string command, TextResultCallback stdOutput) {
			LineOutput = stdOutput;
			RunCommand(command);
		}
	}
}
