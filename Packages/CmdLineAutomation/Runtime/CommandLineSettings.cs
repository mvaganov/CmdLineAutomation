using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
	/// <summary>
	/// TODO refactor:

	/// CommandLineSettings: has all of the settings of a command line, like what commands it accepts and what variables it looks for
	/// * Metadata/description about why this object exists
	/// * A list of command filters (including the specific named command listing in a sub-asset)
	/// * 

	/// CommandLineExecutor: has the runtime variables of a command line, like the parsed commands to execute, the command execution stack, specific found variable data. Executes commands for a command line defined by CommandLineSettings.
	/// * Logic to process commands as a cooperative process
	///   * does not block the Unity thread
	///   * tracks state of which command is executing now
	///   * can be cancelled
	/// * A list of commands to execute
	/// * keeps track of command output, which can be filtered by line with regular expressions
	/// * can be fed commands in the Unity Editor, or from runtime methods

	/// CommandLineAutomation: has a specific command line instruction to execute, which it uses to populate a CommandLineExecutor
	/// * Metadata/description about why this instruction set exists
	/// * the instruction set

	/// </summary>
	[CreateAssetMenu(fileName = "CommandLineSettings", menuName = "ScriptableObjects/CommandLineSettings", order = 1)]
	public partial class CommandLineSettings : ScriptableObject {
		private enum RegexGroupId { None = -1, HideNextLine = 0, DisableOnRead, EnableOnRead }
		/// <summary>
		/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
		/// </summary>
		[SerializeField] protected UnityEngine.Object[] _commandFilters;

		/// <summary>
		/// List if filtering functions for input, which may or may not consume a command. This is the type disambiguated version of <see cref="_commandFilters"/>
		/// </summary>
		private List<ICommandFilter> _filters;

		[SerializeField]
		public class MutableValues {
			/// <summary>
			/// Variables to read from command line input
			/// </summary>
			[SerializeField]
			public NamedRegexSearch[] _variablesFromCommandLineRegexSearch = new NamedRegexSearch[] {
				new NamedRegexSearch("WindowsTerminalVersion", @"Microsoft Windows \[Version ([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\]", new int[] { 1 }, true),
				new NamedRegexSearch("dir", NamedRegexSearch.CommandPromptRegexWindows, null, true)
			};

			public RegexMatrix _censorshipRules = new RegexMatrix();

			public MutableValues Clone() {
				MutableValues clone = new MutableValues();
				clone._variablesFromCommandLineRegexSearch = new NamedRegexSearch[_variablesFromCommandLineRegexSearch.Length];
				for(int i = 0; i < clone._variablesFromCommandLineRegexSearch.Length; ++i) {
					clone._variablesFromCommandLineRegexSearch[i] = _variablesFromCommandLineRegexSearch[i].Clone();
				}
				return clone;
			}
		}

		public MutableValues _runtimeSettings = new MutableValues();

		public IList<ICommandFilter> Filters => _filters;

		public IList<NamedRegexSearch> VariablesFromCommandLineRegexSearch => _runtimeSettings._variablesFromCommandLineRegexSearch;

		private bool NeedsInitialization() => _filters == null;

		public RegexMatrix CensorshipRules => _runtimeSettings._censorshipRules;

		/// <summary>
		/// If the given regex is triggered, all output will be hidden (until <see cref="AddUncensorshipTrigger(string)"/>)
		/// </summary>
		/// <param name="regexTrigger"></param>
		public void AddCensorshipTrigger(string regexTrigger) => CensorshipRules.Add((int)RegexGroupId.DisableOnRead, regexTrigger);

		/// <summary>
		/// If the given regex is triggered, all output will be shown again
		/// </summary>
		/// <param name="regexTrigger"></param>
		public void AddUncensorshipTrigger(string regexTrigger) => CensorshipRules.Add((int)RegexGroupId.EnableOnRead, regexTrigger);

		/// <summary>
		/// Hide lines that contain the given regex trigger
		/// </summary>
		/// <param name="regexTrigger"></param>
		public void AddCensorLineTrigger(string regexTrigger) => CensorshipRules.Add((int)RegexGroupId.HideNextLine, regexTrigger);

		/// <summary>
		/// Remove a regex trigger added by <see cref="AddCensorshipTrigger(string)"/>
		/// </summary>
		/// <param name="regexTrigger"></param>
		/// <returns></returns>
		public bool RemoveCensorshipTrigger(string regexTrigger) => CensorshipRules.Remove((int)RegexGroupId.DisableOnRead, regexTrigger);

		/// <summary>
		/// Remove a regex trigger added by <see cref="AddUncensorshipTrigger(string)"/>
		/// </summary>
		/// <param name="regexTrigger"></param>
		/// <returns></returns>
		public bool RemoveUncensorshipTrigger(string regexTrigger) => CensorshipRules.Remove((int)RegexGroupId.EnableOnRead, regexTrigger);

		/// <summary>
		/// Remove all regex triggers added by <see cref="AddCensorLineTrigger(string)"/>,
		/// <see cref="AddCensorshipTrigger(string)"/>, <see cref="AddUncensorshipTrigger(string)"/>
		/// </summary>
		public void ClearCensorshipRules() => CensorshipRules.ClearRows();

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
			//_censorshipRules = new RegexMatrix(new RegexMatrix.Row[] {
			//	new RegexMatrix.Row(HideNextLineFunc, null),
			//	new RegexMatrix.Row(HideAllFunc, null),
			//	new RegexMatrix.Row(ShowAllFunc, null),
			//});
			//_censorshipRules.IsWaitingForTriggerRecalculate();

			//if (CommandsToDo == null) {
			//	ParseCommands();
			//}
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
