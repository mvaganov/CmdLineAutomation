using RunCmdRedux;
using UnityEngine;

// for testing only
public class CommandAssetComponent : MonoBehaviour, ICommandAsset
{
	[Interface(typeof(ICommandAsset))]
	public Object otherAsset;
	public ICommandProcess CreateCommand(object context) {
		return null;
	}
}
