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

			public RegexMatrix CensorshipRules => _regexTriggerProcessor;

			public MutableValues Clone() {
				MutableValues clone = new MutableValues();
				clone._regexTriggerProcessor = _regexTriggerProcessor.Clone();
				return clone;
			}
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

		public RegexMatrix RegexTriggers => _runtimeSettings.CensorshipRules;

		/// <summary>
		/// If the given regex is triggered, all output will be hidden (until <see cref="AddUncensorshipTrigger(string)"/>)
		/// </summary>
		/// <param name="regexTrigger"></param>
		public void AddCensorshipTrigger(string regexTrigger) => RegexTriggers.Add((int)RegexGroupId.DisableOutputOnRead, regexTrigger);

		/// <summary>
		/// If the given regex is triggered, all output will be shown again
		/// </summary>
		/// <param name="regexTrigger"></param>
		public void AddUncensorshipTrigger(string regexTrigger) => RegexTriggers.Add((int)RegexGroupId.EnableOutputOnRead, regexTrigger);

		/// <summary>
		/// Hide lines that contain the given regex trigger
		/// </summary>
		/// <param name="regexTrigger"></param>
		public void AddCensorLineTrigger(string regexTrigger) => RegexTriggers.Add((int)RegexGroupId.HideNextOutputLine, regexTrigger);

		/// <summary>
		/// Remove a regex trigger added by <see cref="AddCensorshipTrigger(string)"/>
		/// </summary>
		/// <param name="regexTrigger"></param>
		/// <returns></returns>
		public bool RemoveCensorshipTrigger(string regexTrigger) => RegexTriggers.Remove((int)RegexGroupId.DisableOutputOnRead, regexTrigger);

		/// <summary>
		/// Remove a regex trigger added by <see cref="AddUncensorshipTrigger(string)"/>
		/// </summary>
		/// <param name="regexTrigger"></param>
		/// <returns></returns>
		public bool RemoveUncensorshipTrigger(string regexTrigger) => RegexTriggers.Remove((int)RegexGroupId.EnableOutputOnRead, regexTrigger);

		/// <summary>
		/// Remove all regex triggers added by <see cref="AddCensorLineTrigger(string)"/>,
		/// <see cref="AddCensorshipTrigger(string)"/>, <see cref="AddUncensorshipTrigger(string)"/>
		/// </summary>
		public void ClearCensorshipRules() => RegexTriggers.ClearRows();

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

		public static void DelayCall(System.Action call) {
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				UnityEditor.EditorApplication.delayCall += () => call();
			} else
#endif
			{
				DelayCallUsingCoroutine(call);
			}
		}

		public static void DelayCallUsingCoroutine(System.Action call) {
			CoroutineRunner.Instance.StartCoroutine(DelayCall());
			System.Collections.IEnumerator DelayCall() {
				yield return null;
				call.Invoke();
			}
		}
		private class CoroutineRunner : MonoBehaviour {
			private static CoroutineRunner _instance;
			public static CoroutineRunner Instance {
				get {
					if (_instance != null) { return _instance; }
					GameObject go = new GameObject("<CoroutineRunner>");
					DontDestroyOnLoad(go);
					return _instance = go.AddComponent<CoroutineRunner>();
				}
			}
		}
	}
}
