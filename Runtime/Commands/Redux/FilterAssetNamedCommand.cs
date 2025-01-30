using System.Collections.Generic;
using UnityEngine;

namespace RunCmdRedux {
	/// <summary>
	/// Command filter used to call named commands from a list
	/// </summary>
	[CreateAssetMenu(fileName = "NamedCommand", menuName = "ScriptableObjects/FilterAssets/NamedCommand")]
	public class FilterAssetNamedCommand: ScriptableObject, ICommandAsset, ICommandAssetBranch {
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>
		/// </summary>
		[Interface(typeof(ICommandAsset))]
		[SerializeField] protected Object[] _commandListing;
		Dictionary<string, ICommandAsset> _namedCommands = new Dictionary<string, ICommandAsset>();
		bool FoundRecursion() => this.FoundRecursion(null);

		public void RefreshDictionary() {
			_namedCommands.Clear();
			for (int i = 0; i < GetAssetCount(); ++i) {
				ICommandAsset asset = _commandListing[i] as ICommandAsset;
				if (asset == null) {
					//Debug.LogWarning($"{this}[{i}]: unable to add {_commandListing[i]}, not a {nameof(ICommandAsset)}");
					continue;
				}
				Object namedObject = asset as Object;
				if (namedObject == null) {
					//Debug.LogWarning($"{this}[{i}]: unable to add {_commandListing[i]}, not a {nameof(UnityEngine.Object)}");
					continue;
				}
				_namedCommands[namedObject.name] = asset;
			}
		}


		public ICommandAsset GetAssetByIndex(int index) =>
			_commandListing[index] as ICommandAsset;

		public int GetAssetCount() => _commandListing.Length;

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

			public override ICommandProcess.State ExecutionState => _currentProcess != null
				? _currentProcess.ExecutionState : ICommandProcess.State.None;

			public Proc(FilterAssetNamedCommand source, object context) { _source = source; _context = context; }

			public override float GetProgress() => _currentProcess != null ? _currentProcess.GetProgress() : 1;

			// TODO mark if a command was found or not to the caller...
			public override void StartCooperativeFunction(string command, PrintCallback print) {
				_state = ICommandProcess.State.Executing;
				if (string.IsNullOrEmpty(command)) {
					_state = ICommandProcess.State.Finished;
					return;
				}
				string firstToken = GetFirstToken(command);
				if (!_source._namedCommands.TryGetValue(firstToken, out ICommandAsset _currentAsset)) {
					_currentProcess = null;
					print($"unknown command {firstToken}\n\"{command}\"\n" +
						$"Valid options:{string.Join(", ", _source._namedCommands.Keys)}\n");
					_state = ICommandProcess.State.Error;
					return;
				}
				_currentProcess = _currentAsset.CreateCommand(_context);
				if (_currentProcess != null) {
					_currentProcess.StartCooperativeFunction(command, print);
					ValidateCurrentProcess();
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
				ValidateCurrentProcess();
			}
			private void ValidateCurrentProcess() {
				if (_currentProcess == null) {
					return;
				}
				if (_currentProcess.ExecutionState == ICommandProcess.State.Finished) {
					_currentProcess = null;
				}
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
