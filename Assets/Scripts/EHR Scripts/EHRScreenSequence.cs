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
}

public enum TriggerType
{
    Timer = 0,
    Action = 1,
    Manual = 2
}
