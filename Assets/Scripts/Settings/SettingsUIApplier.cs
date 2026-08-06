using UnityEngine;
using UnityEngine.UI;

// Applies current SettingsData values to selection button groups and basic UI controls.
// Configure the option arrays to match the order of buttons in each ButtonSelectionGroup.
public class SettingsUIApplier : MonoBehaviour
{
    [Header("Button Groups")]
    public ButtonSelectionGroup movementGroup;
    public ButtonSelectionGroup turningGroup;

    [Header("Option Labels (must match button order)")]
    public string[] movementOptions = new string[] { "Teleport", "Continuous" };
    public string[] turningOptions = new string[] { "Snap", "Smooth" };

    [Header("Toggles (Slider Game Objects)")]
    public Slider subtitlesSlider; // optional slider for subtitles
    public ToggleSwitch subtitlesToggleSwitch; // optional ToggleSwitch component for subtitles
    public Image subtitlesBackgroundImageInspector; // optional inspector-assigned background Image for subtitles
    public Slider vignettingSlider; // optional slider for vignette
    public ToggleSwitch vignettingToggleSwitch; // optional ToggleSwitch component for vignette
    public Image vignettingBackgroundImageInspector; // optional inspector-assigned background Image for vignette

    [Header("General Toggle Sprites")]
    public Sprite toggleOnSprite;
    public Sprite toggleOffSprite;

    void Start()
    {
        // Apply saved settings on start if available
        ApplySavedSettings();
    }

    private void OnEnable()
    {
        ValidateReferences();
        ApplySavedSettings();
    }

    // Read saved settings and apply to UI
    public void ApplySavedSettings()
    {
        var sm = SettingsManager.Instance;
        if (sm == null || sm.settingsData == null) return;

        ApplySettings(sm.settingsData);
    }

    // Apply a SettingsData object to the UI controls
    public void ApplySettings(SettingsData data)
    {
        if (data == null) return;

        // Movement
        int mi = GetOptionIndex(data.movementMode, movementOptions);
        Debug.Log($"SettingsUIApplier: movementMode='{data.movementMode}', movementIndex={mi}");
        if (movementGroup == null)
        {
            Debug.LogWarning("SettingsUIApplier: movementGroup reference is missing.");
        }
        else if (mi >= 0)
        {
            movementGroup.SelectButton(mi);
            Debug.Log($"SettingsUIApplier: selected movement button index {mi}");
        }
        else
        {
            Debug.LogWarning($"SettingsUIApplier: movement mode '{data.movementMode}' did not match any movementOptions.");
        }

        // Turning
        int ti = GetOptionIndex(data.turningMode, turningOptions);
        Debug.Log($"SettingsUIApplier: turningMode='{data.turningMode}', turningIndex={ti}");
        if (turningGroup == null)
        {
            Debug.LogWarning("SettingsUIApplier: turningGroup reference is missing.");
        }
        else if (ti >= 0)
        {
            turningGroup.SelectButton(ti);
            Debug.Log($"SettingsUIApplier: selected turning button index {ti}");
        }
        else
        {
            Debug.LogWarning($"SettingsUIApplier: turning mode '{data.turningMode}' did not match any turningOptions.");
        }

        // Subtitles
        ApplySliderToggle(subtitlesSlider, subtitlesToggleSwitch, data.subtitles, subtitlesBackgroundImageInspector, "subtitles");

        // Vignetting
        ApplySliderToggle(vignettingSlider, vignettingToggleSwitch, data.comfortVignette, vignettingBackgroundImageInspector, "vignetting");
    }

    private void ApplySliderToggle(Slider slider, ToggleSwitch toggleSwitch, bool isOn, Image backgroundImage, string settingName)
    {
        if (toggleSwitch == null && slider != null)
        {
            toggleSwitch = slider.GetComponent<ToggleSwitch>()
                ?? slider.GetComponentInParent<ToggleSwitch>()
                ?? slider.GetComponentInChildren<ToggleSwitch>();
        }

        if (toggleSwitch != null)
        {
            toggleSwitch.ToggleByGroupManager(isOn);
            return;
        }

        if (slider != null)
        {
            slider.value = isOn ? 1 : 0;
        }
        else
        {
            Debug.LogWarning($"SettingsUIApplier: {settingName} slider reference is missing.");
        }

        if (backgroundImage != null)
        {
            backgroundImage.sprite = isOn ? toggleOnSprite : toggleOffSprite;
            backgroundImage.SetAllDirty();
        }
        else
        {
            Debug.LogWarning($"SettingsUIApplier: {settingName} background image reference is missing.");
        }
    }

    // Helper: find option index by case-insensitive match
    private int GetOptionIndex(string value, string[] options)
    {
        if (options == null || options.Length == 0) return -1;
        if (string.IsNullOrEmpty(value)) return -1;

        for (int i = 0; i < options.Length; i++)
        {
            if (string.Equals(options[i], value.Trim(), System.StringComparison.InvariantCultureIgnoreCase))
                return i;
        }

        return -1;
    }

    private void ValidateReferences()
    {
        if (movementGroup == null)
            Debug.LogWarning("SettingsUIApplier: movementGroup reference is missing.");
        if (turningGroup == null)
            Debug.LogWarning("SettingsUIApplier: turningGroup reference is missing.");
        if (subtitlesSlider == null)
            Debug.LogWarning("SettingsUIApplier: subtitlesSlider reference is missing.");
        if (subtitlesToggleSwitch == null)
            Debug.LogWarning("SettingsUIApplier: subtitlesToggleSwitch reference is missing.");
        if (subtitlesBackgroundImageInspector == null)
            Debug.LogWarning("SettingsUIApplier: subtitlesBackgroundImageInspector reference is missing.");
        if (vignettingSlider == null)
            Debug.LogWarning("SettingsUIApplier: vignettingSlider reference is missing.");
        if (vignettingToggleSwitch == null)
            Debug.LogWarning("SettingsUIApplier: vignettingToggleSwitch reference is missing.");
        if (vignettingBackgroundImageInspector == null)
            Debug.LogWarning("SettingsUIApplier: vignettingBackgroundImageInspector reference is missing.");
        if (toggleOnSprite == null || toggleOffSprite == null)
            Debug.LogWarning("SettingsUIApplier: toggleOnSprite and/or toggleOffSprite reference is missing.");
    }
}

