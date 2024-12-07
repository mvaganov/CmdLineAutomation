using System.Collections.Generic;
using UnityEngine;

namespace RunCmdRedux {
	/// <summary>
	/// Command filter used to call named commands from a list
	/// </summary>
	[CreateAssetMenu(fileName = "NamedCommand", menuName = "ScriptableObjects/FilterAssets/NamedCommand")]
	public class FilterAssetNamedCommand: ScriptableObject, ICommandAsset, ICommandAssetBranch {
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
		/// </summary>
		[SerializeField] protected Object[] _commandListing;
		Dictionary<string, ICommandAsset> _namedCommands = new Dictionary<string, ICommandAsset>();
		bool FoundRecursion() => FoundRecursion(this, null);

		public void RefreshDictionary() {
			_namedCommands.Clear();
			for (int i = 0; i < GetProcessCount(); ++i) {
				ICommandAsset asset = _commandListing[i] as ICommandAsset;
				if (asset == null) {
					Debug.LogWarning($"unable to add {_commandListing[i]}, not a {nameof(ICommandAsset)}");
					continue;
				}
				Object namedObject = asset as Object;
				if (namedObject == null) {
					Debug.LogWarning($"unable to add {_commandListing[i]}, not a {nameof(UnityEngine.Object)}");
					continue;
				}
				_namedCommands[namedObject.name] = asset;
			}
		}

		bool FoundRecursion(ICommandAssetBranch self, List<int> list) {
			for(int i = 0; i < GetProcessCount(); ++i) {
				ICommandProcess proc = GetProcessByIndex(i);
				if (proc == null) {
					continue;
				}
				ICommandAssetBranch branch = proc as ICommandAssetBranch;
				if (list == null) {
					list = new List<int> { i };
				} else {
					list.Add(i);
				}
				if (FoundRecursion(branch, list)) {
					return true;
				}
				list.Remove(list.Count - 1);
			}
			return list != null && list.Count > 0;
		}

		public ICommandProcess GetProcessByIndex(int index) =>
			_commandListing[index] as ICommandProcess;

		public int GetProcessCount() => _commandListing.Length;

		public static string GetFirstToken(string command) {
			int index = command.IndexOf(' ');
			return index < 0 ? command : command.Substring(0, index);
		}

		public class Proc : BaseNamedProcess {
			private FilterAssetNamedCommand _source;
			private ICommandProcess _currentProcess;
			private object _context;
			public override string name {
				get {
					if (_currentProcess is Object obj) { return $"{_source.name}:{obj.name}"; }
					return $"{_source.name}";
				}
			}

			public override bool IsExecutionFinished => _currentProcess != null ? _currentProcess.IsExecutionFinished : true;

			public Proc(FilterAssetNamedCommand source, object context) { _source = source; _context = context; }

			public override float GetProgress() => _currentProcess != null ? _currentProcess.GetProgress() : 1;

			public override void StartCooperativeFunction(string command, PrintCallback print) {
				string firstToken = GetFirstToken(command);
				if (!_source._namedCommands.TryGetValue(firstToken, out ICommandAsset _currentAsset)) {
					return;
				}
				_currentProcess = _currentAsset.CreateCommand(_context);
				if (_currentProcess != null) {
					_currentProcess.StartCooperativeFunction(command, print);
				} else {
					Debug.LogError($"unable to create process for {_currentAsset}");
				}
			}

			public override object Result => _currentProcess != null ? _currentProcess.Result : null;
			public override object Error => _currentProcess != null ? _currentProcess.Error : null;
			public override void ContinueCooperativeFunction() {
				if (_currentProcess == null) {
					return;
				}
				_currentProcess.ContinueCooperativeFunction();
			}
		}

		public ICommandProcess CreateCommand(object context) {
			if (FoundRecursion()) {
				throw new System.Exception("recursive process");
			}
			if (_namedCommands.Count == 0 && _commandListing.Length > 0) {
				RefreshDictionary();
			}
			return new Proc(this, context);
		}

		private void OnValidate() {
			if (FoundRecursion()) {
				throw new System.Exception("recursive process");
			}
			RefreshDictionary();
		}
	}
}
