using RunCmd;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmdRedux {
	/// <summary>
	/// CommandAssetListing: has all of the settings of a command line, like what commands it accepts and what variables it looks for
	/// * Metadata/description about why this object exists
	/// * A list of command assets (including the specific named command listing in a sub-asset)
	/// </summary>
	[CreateAssetMenu(fileName = "CommandAssetSettings", menuName = "ScriptableObjects/CommandAssetSettings", order = 1)]
	public partial class CommandAssetSettings: ScriptableObject {
		internal enum RegexGroupId { None = -1, Variable = 0, HideNextOutputLine = 1, DisableOutputOnRead = 2, EnableOutputOnRead = 3 }
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandAsset"/>s
		/// </summary>
		[SerializeField] protected UnityEngine.Object[] _commandAssets;

		/// <summary>
		/// List if command assets to feed input into, which may or may not consume a command. This is the type disambiguated version of <see cref="_commandAssets"/>
		/// </summary>
		private List<ICommandAsset> _iCommandAssets;

		[System.Serializable]
		public class MutableValues {
			[SerializeField]
			public RegexMatrix _regexTriggerProcessor = new RegexMatrix(new RegexMatrix.Row[] {
				new RegexMatrix.Row(RegexGroupId.Variable.ToString(), new NamedRegexSearch[] {
					new NamedRegexSearch("WindowsTerminalVersion", @"Microsoft Windows \[Version ([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\]",
						new int[] { 1 }, true, NamedRegexSearch.SpecialReadLogic.IgnoreAfterFirstValue),
					new NamedRegexSearch("dir", NamedRegexSearch.CommandPromptRegexWindows,
						null, true, NamedRegexSearch.SpecialReadLogic.None)
				}),
				new RegexMatrix.Row(RegexGroupId.HideNextOutputLine.ToString()),
				new RegexMatrix.Row(RegexGroupId.DisableOutputOnRead.ToString()),
				new RegexMatrix.Row(RegexGroupId.EnableOutputOnRead.ToString()),
			});

			public RegexMatrix RegexTriggers => _regexTriggerProcessor;

			public MutableValues Clone() {
				MutableValues clone = new MutableValues();
				clone._regexTriggerProcessor = _regexTriggerProcessor.Clone();
				return clone;
			}

			/// <summary>
			/// If the given regex is triggered, all output will be hidden (until <see cref="AddUncensorshipTrigger(string)"/>)
			/// </summary>
			/// <param name="regexTrigger"></param>
			public void AddCensorshipTrigger(string regexTrigger) => _regexTriggerProcessor.Add((int)RegexGroupId.DisableOutputOnRead, regexTrigger);

			/// <summary>
			/// If the given regex is triggered, all output will be shown again
			/// </summary>
			/// <param name="regexTrigger"></param>
			public void AddUncensorshipTrigger(string regexTrigger) => _regexTriggerProcessor.Add((int)RegexGroupId.EnableOutputOnRead, regexTrigger);

			/// <summary>
			/// Hide lines that contain the given regex trigger
			/// </summary>
			/// <param name="regexTrigger"></param>
			public void AddCensorLineTrigger(string regexTrigger) => _regexTriggerProcessor.Add((int)RegexGroupId.HideNextOutputLine, regexTrigger);

			/// <summary>
			/// Remove a regex trigger added by <see cref="AddCensorshipTrigger(string)"/>
			/// </summary>
			/// <param name="regexTrigger"></param>
			/// <returns></returns>
			public bool RemoveCensorshipTrigger(string regexTrigger) => _regexTriggerProcessor.Remove((int)RegexGroupId.DisableOutputOnRead, regexTrigger);

			/// <summary>
			/// Remove a regex trigger added by <see cref="AddUncensorshipTrigger(string)"/>
			/// </summary>
			/// <param name="regexTrigger"></param>
			/// <returns></returns>
			public bool RemoveUncensorshipTrigger(string regexTrigger) => _regexTriggerProcessor.Remove((int)RegexGroupId.EnableOutputOnRead, regexTrigger);

			/// <summary>
			/// Remove all regex triggers added by <see cref="AddCensorLineTrigger(string)"/>,
			/// <see cref="AddCensorshipTrigger(string)"/>, <see cref="AddUncensorshipTrigger(string)"/>
			/// </summary>
			public void ClearCensorshipRules() => _regexTriggerProcessor.ClearRows();
		}

		private void ResetRuntimeSettings() { _runtimeSettings = new MutableValues(); }

		[ContextMenuItem(nameof(ResetRuntimeSettings), nameof(ResetRuntimeSettings))]
		public MutableValues _runtimeSettings = new MutableValues();

		public IList<ICommandAsset> CommandAssets {
			get {
				if (NeedsInitialization()) {
					Initialize();
				}
				return _iCommandAssets;
			}
		} 

		private bool NeedsInitialization() => _iCommandAssets == null;

		public void Initialize() {
			_iCommandAssets = new List<ICommandAsset>();
			foreach (UnityEngine.Object obj in _commandAssets) {
				//if (obj is ICommandAsset commandAsset) {
				//	_iCommandAssets.Add(commandAsset);
				//} else {
				//	Debug.LogError($"unexpected asset type {obj.GetType().Name}, " +
				//		$"{name} expects only {nameof(ICommandAsset)} entries");
				//}
				switch (obj) {
					case ICommandAsset iAsset:
						_iCommandAssets.Add(iAsset);
						break;
					default:
						Debug.LogError($"unexpected asset type {obj.GetType().Name}, " +
							$"{name} expects only {nameof(ICommandAsset)} entries");
						break;
				}
			}
		}

		// TODO keep working here.
		public bool Iterate(string command, PrintCallback print, object context, int start, int end, ref int index) {
			IList<ICommandAsset> assets = CommandAssets;
			for (index = start; index < end; ++index) {
				ICommandAsset asset = assets[index];
				ICommandProcess proc = asset.GetCommandCreateIfMissing(context);
				// TODO if this command hasn't been run yet, start it.
				proc.StartCooperativeFunction(command, print);
					// return true if still working
				// if it has been run and it's iterating, continue iterating
				// if it has been run and it's finished, advance to the next one...
			}
			// return false when finished
			return false;
		}
	}
}
