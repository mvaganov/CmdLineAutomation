using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

public class InteractiveCMDShell {
	System.Diagnostics.ProcessStartInfo startInfo;
	System.Diagnostics.Process process;
	System.Threading.Thread thread;
	System.IO.StreamReader output;
	StringBuilder lineBuffer = new StringBuilder();
	List<string> lines = new List<string>();
	bool m_Running = false;
	public Action OnLineRead = delegate { };

	public InteractiveCMDShell() {
		startInfo = new System.Diagnostics.ProcessStartInfo("Cmd.exe");
		startInfo.WorkingDirectory = "C:\\Windows\\System32\\";
		startInfo.UseShellExecute = false;
		startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
		startInfo.CreateNoWindow = true;
		startInfo.ErrorDialog = false;
		startInfo.RedirectStandardInput = true;
		startInfo.RedirectStandardOutput = true;
		process = new System.Diagnostics.Process();
		process.StartInfo = startInfo;
		process.Start();
		output = process.StandardOutput;
		thread = new System.Threading.Thread(Thread);
		thread.Start();
	}
	~InteractiveCMDShell() {
		try {
			Stop();
		} catch { }
	}

	public void RunCommand(string aInput) {
		if (m_Running) {
			process.StandardInput.WriteLine(aInput);
			process.StandardInput.Flush();
		}
	}
	public void Stop() {
		if (process != null) {
			process.Kill();
			thread.Join(200);
			thread.Abort();
			process = null;
			thread = null;
			m_Running = false;
		}
	}
	public string GetCurrentLine() {
		if (!m_Running)
			return "";
		return lineBuffer.ToString();
	}
	public void GetRecentLines(List<string> aLines) {
		if (!m_Running || aLines == null)
			return;
		if (lines.Count == 0)
			return;
		PeekRecentLines(aLines);
		lock (lines) {
			lines.Clear();
		}
	}

	public void PeekRecentLines(List<string> aLines) {
		lock (lines) {
			if (lines.Count > 0) {
				aLines.AddRange(lines);
			}
		}
	}

	void Thread() {
		m_Running = true;
		try {
			while (m_Running) {
				int c = output.Read();
				if (c <= 0)
					break;
				else if (c == '\n') {
					lock (lines) {
						lines.Add(GetCurrentLine());
						lineBuffer.Clear();
						OnLineRead.Invoke();
					}
				} else if (c != '\r')
					lineBuffer.Append((char)c);
			}
		} catch (Exception e) {
			Debug.LogException(e);
		}
		m_Running = false;
	}
}
