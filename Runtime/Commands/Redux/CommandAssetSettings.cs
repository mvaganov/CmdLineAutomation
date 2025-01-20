using RunCmd;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor.VersionControl;
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
		/// if true, execution of commands will be done with an async non-blocking coroutine
		/// </summary>
		[SerializeField] protected bool _iterateAsync = true;
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

		public class CommandAssetSettingsExecution {
			public int index = -1;
			public bool stillExecutingCurrentIndex = false;
			public IList<ICommandAsset> _assets;
			ICommandProcess proc;
			public object context;
			public string command;
			public string firstToken;
			public PrintCallback print;
			public Action<object> onFinish;
			public object result;
			public CommandAssetSettingsExecution(string command, PrintCallback print, object context,
				IList<ICommandAsset> assets, Action<object> onFinish) {
				this.command = command; this.print = print; this.context = context; _assets = assets; this.onFinish = onFinish;
			}

			public bool Iterate() {
				bool isNewCommand = false;
				if (index < 0) {
					index = 0;
					isNewCommand = true;
				} else if (proc == null || proc.IsExecutionFinished) {
					++index;
					isNewCommand = true;
				}
				if (isNewCommand) {
					if (index >= _assets.Count) {
						index = -1;
						return false;
					}
					ICommandAsset asset = _assets[index];
					Debug.Log($"ASSET[{index}] {asset}");
					proc = (asset != null) ? asset.GetCommandCreateIfMissing(context) : null;
					try {
						proc.StartCooperativeFunction(command, print);
					} catch (Exception e) {
						Debug.LogError(e);
						index = -1;
						result = proc.Result;
						return false;
					}
				} else if (proc != null) {
					Debug.Log($"continue[{index}] {proc}");
					try {
						proc.ContinueCooperativeFunction();
					} catch (Exception e) {
						Debug.LogError(e);
						index = -1;
						result = proc.Result;
						return false;
					}
				}
				result = proc.Result;
				return true;
			}
		}

		public void ClearCurrentExecution() {
			currentExecution = null;
		}

		private CommandAssetSettingsExecution currentExecution;
		public void Execute(string command, PrintCallback print, object context, Action<object> onFinish) {
			if (currentExecution != null) {
				Debug.LogError($"cannot execute {command}, still executing {currentExecution.command}");
				return;// true;
			}
			currentExecution = CommandAssetExecutionStack.GetDataIfMissing(this, context,
				() => new CommandAssetSettingsExecution(command, print, context, CommandAssets, onFinish));
			IterateLoopPossiblyAsync();
			//return keepIterating;
		}

		public void IterateLoopPossiblyAsync() {
			bool executionActive;
			int loopGuard = 0;
			do {
				executionActive = currentExecution.Iterate();
				if (_iterateAsync) {
					if (executionActive) {
						Debug.Log("again?");
						CommandDelay.DelayCall(IterateLoopPossiblyAsync);
					} else {
						Debug.Log("FINISHED");
						currentExecution.onFinish?.Invoke(currentExecution.result);
						currentExecution = null;
					}
					return;
				}
				if (loopGuard++ > 100000) {
					throw new Exception($"'{currentExecution.command}' took too long");
				}
			} while (executionActive);
		}
	}
}
