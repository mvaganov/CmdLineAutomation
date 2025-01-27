using System;
using System.Collections.Generic;

namespace RunCmdRedux {
	public class CommandManager {
		private static CommandManager _instance;
		private Dictionary<Procedure,ICommandProcess> _procs = new Dictionary<Procedure, ICommandProcess>();
		public static CommandManager Instance => (_instance != null) ? _instance : _instance = new CommandManager();
		public static Dictionary<Procedure, ICommandProcess> _GetProcedures => Instance._procs;
		public struct Procedure {
			public object context;
			public ICommandAsset procSource;
			public Procedure(object context, ICommandAsset procSource) {
				this.context = context; this.procSource = procSource;
			}
			public override bool Equals(object obj) => obj is Procedure other && context == other.context
				&& procSource == other.procSource;
			public override int GetHashCode() => context.GetHashCode() ^ procSource.GetHashCode();
			public override string ToString() => $"[{procSource}@{context}]";
			public static bool operator ==(Procedure a, Procedure b) => a.Equals(b);
			public static bool operator !=(Procedure a, Procedure b) => !a.Equals(b);
		}
		public bool TryGet(object context, ICommandAsset procSource, out ICommandProcess process) {
			Procedure procId = new Procedure(context, procSource);
			return _procs.TryGetValue(procId, out process);
		}
		public void Add(object context, ICommandAsset procSource, ICommandProcess process) {
			Procedure procId = new Procedure(context, procSource);
			if (_procs.ContainsKey(procId)) {
				throw new Exception($"duplicate {procId}! should remove old one...");
			}
			_procs[procId] = process;
		}
		public bool Remove(object context, ICommandAsset procSource, ICommandProcess process) {
			Procedure procId = new Procedure(context, procSource);
			if (!_procs.ContainsKey(procId)) {
				throw new Exception($"missing {procId}!");
			}
			return _procs.Remove(procId);
		}
	}
}
