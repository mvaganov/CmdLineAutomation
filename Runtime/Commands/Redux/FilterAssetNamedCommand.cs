using UnityEngine;

namespace RunCmdRedux {
	/// <summary>
	/// Command filter used to call named commands from a list
	/// </summary>
	[CreateAssetMenu(fileName = "NamedCommand", menuName = "ScriptableObjects/FilterAssets/NamedCommand")]
	public class FilterAssetNamedCommand: ScriptableObject, ICommandAsset {
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
		/// </summary>
		[SerializeField] protected Object[] _commandListing;

		public class Proc : BaseNamedProcess {
			private FilterAssetNamedCommand _source;
			private ICommandProcess _currentProcess;
			public override string name {
				get {
					if (_currentProcess is Object obj) { return obj.name; }
					return null;
				}
			}

			public override bool IsExecutionFinished => _currentProcess != null ? _currentProcess.IsExecutionFinished : true;

			public Proc(FilterAssetNamedCommand source) { _source = source; }

			public override float GetProgress() => _currentProcess != null ? _currentProcess.GetProgress() : 1;

			public override void StartCooperativeFunction(string command, PrintCallback print) {
				// TODO find command from _source that matches this command, and execute it.
				throw new System.NotImplementedException();
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
			throw new System.NotImplementedException();
		}
	}
}
