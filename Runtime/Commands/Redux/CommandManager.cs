using System;
using System.Collections.Generic;

namespace RunCmd {
	public class CommandManager {
		private static CommandManager _instance;
		private HashSet<Procedure> _procIds = new HashSet<Procedure>();
		public static CommandManager Instance => (_instance != null) ? _instance : _instance = new CommandManager();
		public struct Procedure {
			public object context;
			public ICommandAsset procSource;
			public ICommandProcess process;
			public Type ProcType => process != null ? process.GetType() : null;
			public Procedure(object context, ICommandAsset procSource, ICommandProcess process) {
				this.context = context; this.procSource = procSource; this.process = process;
			}
			public override bool Equals(object obj) => obj is Procedure other && context == other.context && process == other.process;
			public override int GetHashCode() => context.GetHashCode() ^ procSource.GetHashCode();
			public override string ToString() => $"{context}.{process}";
			public static bool operator ==(Procedure a, Procedure b) => a.Equals(b);
			public static bool operator !=(Procedure a, Procedure b) => !a.Equals(b);
		}
		public bool TryGet(object context, ICommandAsset procSource, out Procedure actualValue) {
			Procedure procId = new Procedure(context, procSource, null);
			return _procIds.TryGetValue(procId, out actualValue);
		}
		public void Add(object context, ICommandAsset procSource, ICommandProcess process) {
			Procedure procId = new Procedure(context, procSource, process);
			if (_procIds.Contains(procId)) {
				throw new Exception($"duplicate {procId}! should remove old one...");
			}
			_procIds.Add(procId);
		}
		public void Remove(object context, ICommandAsset procSource, ICommandProcess process) {
			Procedure procId = new Procedure(context, procSource, process);
			if (!_procIds.Contains(procId)) {
				throw new Exception($"missing {procId}!");
			}
			_procIds.Remove(procId);
		}
	}
}
