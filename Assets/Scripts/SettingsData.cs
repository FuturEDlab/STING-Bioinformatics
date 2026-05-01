using System;

[Serializable]
public class SettingsData
{
    public bool locomotionEnabled = true;
    public bool snapTurningEnabled = true;
    public bool teleportationEnabled = true;
    public bool vignettingEnabled = true;
    public bool subtitlesEnabled = true;

    public float masterVolume = 1f;
    public float backgroundVolume = 1f;
    public float narrationVolume = 1f;
    public float sfxVolume = 1f;
}