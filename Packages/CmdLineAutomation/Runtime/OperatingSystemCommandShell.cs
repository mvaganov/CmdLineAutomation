using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

namespace RunCmd {
	public interface IReferencesOperatingSystemCommandShell {
		public OperatingSystemCommandShell Shell { get; }
	}

	// TODO keep track of the current working directory
	// TODO read formatted output from command line programs and do something with that logic?
	public class OperatingSystemCommandShell : IReferencesOperatingSystemCommandShell {
		private string _name;
		private System.Diagnostics.ProcessStartInfo _startInfo;
		private System.Diagnostics.Process _process;
		private System.Threading.Thread _thread;
		private StreamReader _output;
		private string _lineBuffer = ""; // TODO semaphores to make StringBuilder threadsafe
		private List<string> _lines = new List<string>();
		private bool _running = false;
		public Action<string> LineOutput = delegate { };
		public Func<bool> KeepAlive;

		public static List<OperatingSystemCommandShell> RunningShells = new List<OperatingSystemCommandShell>();
		public static OperatingSystemCommandShell CreateUnityEditorShell() =>
			new OperatingSystemCommandShell("cmd.exe", Path.Combine(Application.dataPath, ".."));

		public List<string> Lines => _lines;

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

		public OperatingSystemCommandShell(string command = "cmd.exe", string workingDirectory = "C:\\Windows\\System32\\") {
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
			RunningShells.Add(this);
		}

		~OperatingSystemCommandShell() {
			try {
				Stop();
			} catch { }
		}

		public void RunCommand(string command) {
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

		void Thread() {
			_running = true;
			try {
				while (IsRunning && Reading()) ;
			} catch (System.Threading.ThreadAbortException) {
#if UNITY_EDITOR
				Debug.LogWarning($"Aborted {nameof(OperatingSystemCommandShell)} Thread");
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
			int c = _output.Read();
			if (c <= 0) {
				return false;
			} else if (c == '\n') {
				lock (_lines) {
					string line = GetCurrentLine();
					_lines.Add(line);
					_lineBuffer = "";//.Clear();
					LineOutput.Invoke(line);
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

		public void StartCooperativeFunction(object context, string command, Action<string> stdOutput) {
			LineOutput = stdOutput;
			RunCommand(command);
		}
	}
}
