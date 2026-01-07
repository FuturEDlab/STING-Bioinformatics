    using UnityEngine;
    using UnityEngine.UI;

    public class SettingsUIController : MonoBehaviour
    {
        [Header("Audio Sliders")]
        public Slider masterVolume;
        public Slider backgroundVolume;
        public Slider narrationVolume;
        public Slider sfxVolume;

        [Header("Toggles")]
        public Toggle locomotion;
        public Toggle snapTurning;
        public Toggle teleportation;
        public Toggle vignetting;
        public Toggle subtitles;

        void Start()
        {
            var s = SettingsManager.Instance.CurrentSettings;

            // Initialize UI from saved settings
            masterVolume.value = s.masterVolume;
            backgroundVolume.value = s.backgroundVolume;
            narrationVolume.value = s.narrationVolume;
            sfxVolume.value = s.sfxVolume;

            locomotion.isOn = s.locomotionEnabled;
            snapTurning.isOn = s.snapTurningEnabled;
            teleportation.isOn = s.teleportationEnabled;
            vignetting.isOn = s.vignettingEnabled;
            subtitles.isOn = s.subtitlesEnabled;
        }

        // --- AUDIO ---
        public void OnMasterVolumeChanged(float value)
        {
            SettingsManager.Instance.CurrentSettings.masterVolume = value;
            SettingsManager.Instance.Save();

        }

        public void OnBackgroundVolumeChanged(float value)
        {
            SettingsManager.Instance.CurrentSettings.backgroundVolume = value;
            SettingsManager.Instance.Save();

        }

        public void OnNarrationVolumeChanged(float value)
        {
            SettingsManager.Instance.CurrentSettings.narrationVolume = value;
            SettingsManager.Instance.Save();

            // TODO: Apply to narration audio
        }

        public void OnSFXVolumeChanged(float value)
        {
            SettingsManager.Instance.CurrentSettings.sfxVolume = value;
            SettingsManager.Instance.Save();

        }

        // --- TOGGLES ---
        public void OnLocomotionChanged(bool value)
        {
            SettingsManager.Instance.CurrentSettings.locomotionEnabled = value;
            SettingsManager.Instance.Save();
        }

        public void OnSnapTurningChanged(bool value)
        {
            SettingsManager.Instance.CurrentSettings.snapTurningEnabled = value;
            SettingsManager.Instance.Save();

            // TODO: Apply to XR snap turn provider
        }

        public void OnTeleportationChanged(bool value)
        {
            SettingsManager.Instance.CurrentSettings.teleportationEnabled = value;
            SettingsManager.Instance.Save();

            // TODO: Enable/disable teleport provider
        }

        public void OnVignettingChanged(bool value)
        {
            SettingsManager.Instance.CurrentSettings.vignettingEnabled = value;
            SettingsManager.Instance.Save();

            // TODO: Toggle vignette post-processing
        }

        public void OnSubtitlesChanged(bool value)
        {
            SettingsManager.Instance.CurrentSettings.subtitlesEnabled = value;
            SettingsManager.Instance.Save();

            // TODO: Toggle subtitle UI
        }
    }
