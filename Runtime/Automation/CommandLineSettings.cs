using RunCmdRedux;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// CommandLineSettings: has all of the settings of a command line, like what commands it accepts and what variables it looks for
	/// * Metadata/description about why this object exists
	/// * A list of command filters (including the specific named command listing in a sub-asset)
	/// </summary>
	[CreateAssetMenu(fileName = "CommandLineSettings", menuName = "ScriptableObjects/CommandLineSettings", order = 1)]
	public partial class CommandLineSettings : ScriptableObject {
		internal enum RegexGroupId { None = -1, Variable = 0, HideNextOutputLine = 1, DisableOutputOnRead = 2, EnableOutputOnRead = 3 }
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
		/// </summary>
		[SerializeField] protected UnityEngine.Object[] _commandFilters;

		/// <summary>
		/// List if filtering functions for input, which may or may not consume a command. This is the type disambiguated version of <see cref="_commandFilters"/>
		/// </summary>
		private List<ICommandFilter> _filters;

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

		public IList<ICommandFilter> Filters {
			get {
				if (NeedsInitialization()) {
					Initialize();
				}
				return _filters;
			}
		} 

		private bool NeedsInitialization() => _filters == null;

		public void Initialize() {
			_filters = new List<ICommandFilter>();
			foreach (UnityEngine.Object obj in _commandFilters) {
				switch (obj) {
					case ICommandFilter iFilter:
						_filters.Add(iFilter);
						break;
					default:
						Debug.LogError($"unexpected filter type {obj.GetType().Name}, " +
							$"{name} expects only {nameof(ICommandFilter)} entries");
						break;
				}
			}
		}
	}
}
