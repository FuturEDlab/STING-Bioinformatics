    using UnityEngine;
    using UnityEngine.UI;

    public class SettingsUIController : MonoBehaviour
    {
        // [SerializeField] private PlayerManager playManage;
        
        [Header("Toggles")]
        public Toggle locomotion;
        public Toggle snapTurning;
        public Toggle teleportation;
        public Toggle vignetting;
        public Toggle subtitles;

        private SettingsData s;

        private void HookUpControls()
        {
            if (locomotion != null) {
                locomotion.onValueChanged.RemoveListener(SetLocoMotion);
                locomotion.onValueChanged.AddListener(SetLocoMotion);
            }

            if (snapTurning != null) {
                snapTurning.onValueChanged.RemoveListener(SetSnapTurning);
                snapTurning.onValueChanged.AddListener(SetSnapTurning);
            }

            if (teleportation != null) {
                teleportation.onValueChanged.RemoveListener(SetTeleportation);
                teleportation.onValueChanged.AddListener(SetTeleportation);
            }

            if (vignetting != null) {
                vignetting.onValueChanged.RemoveListener(SetVignetting);
                vignetting.onValueChanged.AddListener(SetVignetting);
            }
            
            if (subtitles != null) {
                subtitles.onValueChanged.RemoveListener(SetSubtitles);
                subtitles.onValueChanged.AddListener(SetSubtitles);
            }
        }

        void Start()
        {
            s = SettingsManager.Instance.CurrentSettings;

            // Initialize UI from saved settings
            locomotion.isOn = s.locomotionEnabled;
            snapTurning.isOn = s.snapTurningEnabled;
            teleportation.isOn = s.teleportationEnabled;
            vignetting.isOn = s.vignettingEnabled;
            subtitles.isOn = s.subtitlesEnabled;
            
            HookUpControls();
        }
        
        private void SetLocoMotion(bool value)
        {
            s.locomotionEnabled = value;
            locomotion.isOn = s.locomotionEnabled;
            SettingsManager.Instance.Save();
            
            // if (playManage == null) return;
            // playManage.WireLocomotion(locomotion.isOn);
        }
        
        private void SetSnapTurning(bool value)
        {
            s.snapTurningEnabled = value;
            snapTurning.isOn = s.snapTurningEnabled;
            SettingsManager.Instance.Save();
            
            // if (playManage == null) return;
            // playManage.WireSnapTurn(snapTurning.isOn);
        }
        
        private void SetTeleportation(bool value)
        {
            s.teleportationEnabled = value;
            teleportation.isOn = s.teleportationEnabled;
            SettingsManager.Instance.Save();
            
            // if (playManage == null) return;
            // playManage.WireTeleport(teleportation.isOn);
        }
        
        private void SetVignetting(bool value)
        {
            s.vignettingEnabled = value;
            vignetting.isOn = s.vignettingEnabled;
            SettingsManager.Instance.Save();
            
            // TODO: Wire up with post-processing vignette effect to turn off/on.
            // You can either wire it up here or in the WireVignetting method in
            // 'PlayerManager.cs' script
        }
        
        private void SetSubtitles(bool value)
        {
            s.subtitlesEnabled = value;
            subtitles.isOn = s.subtitlesEnabled;
            SettingsManager.Instance.Save();

            // TODO: Wire up with subtitle display system to show/hide subtitles.
            // You can either wire it up here or in the WireSubtitles method in
            // 'PlayerManager.cs' script
        }
    }
