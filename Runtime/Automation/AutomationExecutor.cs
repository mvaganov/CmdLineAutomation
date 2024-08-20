using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
  [System.Serializable]
  public class AutomationExecutor {
    public CommandLineSettings _settings;
    public int _currentCommandIndex = 0;
    public List<string> _currentCommands = new List<string>();
    public string _currentCommand;


    public void ExecuteCurrentCommand() {
      Debug.Log($"executing {_currentCommand}");
      // TODO
      // get the list of filters
      // pass current command through each filter until it is executed on a filter that consumes

    }
  }
}
