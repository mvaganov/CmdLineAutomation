using RunCmd;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Command filter used to call named commands from a list
/// </summary>
[CreateAssetMenu(fileName = "NamedCommandAssets", menuName = "ScriptableObjects/CommandAsset/NamedCommandAssets")]
public class NamedCommandAssets : ScriptableObject {
	/// <summary>
	/// List of the possible custom commands written as C# <see cref="ICommandProcessor"/>s
	/// </summary>
	[SerializeField] protected UnityEngine.Object[] _commandListing;

}
