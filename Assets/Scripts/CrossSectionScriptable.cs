using UnityEngine;

[CreateAssetMenu(fileName = "CrossSectionScriptable", menuName = "Data/CrossSectionScriptable")]
public class CrossSectionScriptable : ScriptableObject
{
    [TextArea(20, 200)]
    public string rawText;
}
