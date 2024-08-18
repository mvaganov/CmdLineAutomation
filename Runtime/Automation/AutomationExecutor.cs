using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunCmd {
  [System.Serializable]
  public class AutomationExecutor {
    public int _currentCommandIndex = 0;
    public List<string> _currentCommands = new List<string>();
  }
}
