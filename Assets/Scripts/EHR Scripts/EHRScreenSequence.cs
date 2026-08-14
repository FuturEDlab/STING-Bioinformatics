using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "EHR/EHR Screen Sequence")]
public class EHRScreenSequence : ScriptableObject
{
    public List<EHRScreenEntry> entries = new List<EHRScreenEntry>();
}

[System.Serializable]
public class EHRScreenEntry
{
    public Sprite sprite;
    public TriggerType trigger = TriggerType.Timer;
    public float duration = 15f;
    public string actionName;
    public bool showIcon = false;
    public GameObject iconPrefab;
    public string iconAnimatorTrigger;
    [Tooltip("Key used to match a scene-placed icon (EHRIcon.key). If set, the scene object with that key will be used instead of instantiating the prefab.")]
    public string iconKey;
}

public enum TriggerType
{
    Timer = 0,
    Action = 1,
    Manual = 2
}
