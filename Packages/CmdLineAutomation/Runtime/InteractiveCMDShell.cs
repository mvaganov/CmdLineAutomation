using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public interface IReferencesCmdShell {
	public InteractiveCmdShell Shell { get; }
}

// TODO keep track of the current working directory
// TODO why does "Run Commands To Do" work, but not "Start Process"?
// TODO read formatted output from command line programs and do something with that logic?
public class InteractiveCmdShell : ICommandProcessor, IReferencesCmdShell {
	private System.Diagnostics.ProcessStartInfo _startInfo;
	private System.Diagnostics.Process _process;
	private System.Threading.Thread _thread;
	private StreamReader _output;
	private string _lineBuffer = ""; // StringBuilder is faster but less thread safe
	private List<string> _lines = new List<string>();
	private bool _running = false;
	public Action<string> LineOutput = delegate { };

	public static InteractiveCmdShell CreateUnityEditorShell() =>
		new InteractiveCmdShell("cmd.exe", Path.Combine(Application.dataPath, ".."));

	public InteractiveCmdShell Shell => this;

	public bool IsRunning {
		get => _running;
		set {
			if (_running && !value) {
				Stop();
			}
			_running = value;
		}
	}

	public InteractiveCmdShell(string command = "cmd.exe", string workingDirectory = "C:\\Windows\\System32\\") {
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
	}

	~InteractiveCmdShell() {
		try {
			Stop();
		} catch { }
	}

	public void RunCommand(string command) {
		if (_running) {
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
	}

	public string GetCurrentLine() {
		if (!_running)
			return "";
		return _lineBuffer.ToString();
	}

	public void GetRecentLines(List<string> aLines) {
		if (!_running || aLines == null) {
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
			while (_running && Reading()) ;
		} catch (System.Threading.ThreadAbortException) {
#if UNITY_EDITOR
			Debug.LogWarning($"Aborted {nameof(InteractiveCmdShell)} Thread");
#endif
		} catch (Exception e) {
#if UNITY_EDITOR
			Debug.LogException(e);
#endif
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

	public bool IsFunctionFinished() => true;

	public string FunctionResult() => null;

	public string[] Split(string command) {
		return command.Split();
	}
}
