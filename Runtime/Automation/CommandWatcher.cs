using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RunCmd {
	[CreateAssetMenu(fileName = "CommandWatcher", menuName = "ScriptableObjects/CommandWatcher", order = 1)]
	public partial class CommandWatcher : ScriptableObject {
		[Serializable]
		public class ActiveCommandRunner {
			[HideInInspector]
			public string name;
			public Object commandRunner;
			[TextArea(1,10)]
			public string description;
		}

		[ContextMenuItem(nameof(PopulateInfo), nameof(PopulateInfo))]
		public List<ActiveCommandRunner> commandRunners = new List<ActiveCommandRunner>();

		[ContextMenu(nameof(PopulateInfo))]
		public void PopulateInfo() {
			Object[] commandRunnerAssets = FilterExecuteNamedCommand.GetAllScriptableObjectAssets<CommandRunnerBase>();
			Debug.Log($"found {commandRunnerAssets.Length}: " + string.Join(", ", (object[])commandRunnerAssets));
			for (int i = 0; i < commandRunnerAssets.Length; i++) {
				ActiveCommandRunner runner;
				CommandRunnerBase runnerBase = commandRunnerAssets[i] as CommandRunnerBase;
				if (commandRunners.Count <= i) {
					commandRunners.Add(runner = new ActiveCommandRunner());
				} else {
					runner = commandRunners[i];
				}
				runner.commandRunner = commandRunnerAssets[i];
				runner.name = runner.commandRunner.name;
				runner.description = runnerBase.GetDescriptionOfContexts();
			}
		}
	}
}
