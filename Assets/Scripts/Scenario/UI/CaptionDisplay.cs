using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The single scene surface that shows VO captions. Put it on a small world-space canvas
/// (e.g. parented under the camera rig so it stays in view) with an Image for the Figma
/// caption blobs and a TMP text as fallback for phrases whose blob isn't imported yet.
/// Driven by <see cref="ScenarioContext.PlayVoice"/>; assign it on the ScenarioController
/// under Context ▸ Voice-over.
/// </summary>
public class CaptionDisplay : MonoBehaviour
{
    private const string PrefsKey = "captions_enabled";

    [Tooltip("Object toggled on/off with the caption. Leave empty to use this GameObject.")]
    [SerializeField] private GameObject root;

    [Tooltip("Shows the caption blob image exported from Figma.")]
    [SerializeField] private Image blobImage;

    [Tooltip("Fallback caption text, shown only when the current phrase has no blob sprite.")]
    [SerializeField] private TMP_Text captionText;

    [Tooltip("Optional plate shown behind the fallback text so it stays readable against a bright wall. Hidden for blob captions, which carry their own background.")]
    [SerializeField] private GameObject textBackdrop;

    /// <summary>
    /// Player-facing captions toggle, persisted in PlayerPrefs (on by default). Wire a
    /// settings toggle to <see cref="SetCaptionsEnabled"/>.
    /// </summary>
    public static bool CaptionsEnabled
    {
        get => PlayerPrefs.GetInt(PrefsKey, 1) == 1;
        set => PlayerPrefs.SetInt(PrefsKey, value ? 1 : 0);
    }

    /// <summary>UnityEvent-friendly instance wrapper (Toggle.onValueChanged can bind it).</summary>
    public void SetCaptionsEnabled(bool enabled)
    {
        CaptionsEnabled = enabled;
        if (!enabled)
            Hide();
    }

    private GameObject Root => root != null ? root : gameObject;

    private void Awake()
    {
        ValidateRoot();
        Hide();
    }

    /// <summary>
    /// Root is switched on and off every time somebody speaks, so pointing it at something
    /// outside the caption hierarchy does not fail quietly — it makes an unrelated panel
    /// flash in and out with the dialogue. Caught here rather than left to be found in a
    /// headset.
    /// </summary>
    private void ValidateRoot()
    {
        if (root == null)
            return;

        if (root.transform.IsChildOf(transform) || transform.IsChildOf(root.transform))
            return;

        Debug.LogWarning($"[CaptionDisplay] Root is set to '{root.name}', which is not part of the caption hierarchy — every caption would switch that object on instead. Ignoring it and using '{name}'. Clear the field, or point it at the caption canvas.", this);
        root = null;
    }

    /// <summary>Show the caption for one phrase: blob when available, text otherwise.</summary>
    public void Show(CaptionedClip phrase)
    {
        if (!CaptionsEnabled || phrase == null)
        {
            Hide();
            return;
        }

        bool hasBlob = phrase.Caption != null;
        bool hasText = !string.IsNullOrWhiteSpace(phrase.CaptionText);

        if (!hasBlob && !hasText)
        {
            Hide();
            return;
        }

        if (blobImage != null)
        {
            blobImage.sprite = phrase.Caption;
            blobImage.gameObject.SetActive(hasBlob);
        }

        bool showingText = !hasBlob && hasText;

        if (captionText != null)
        {
            captionText.text = hasText ? phrase.CaptionText : string.Empty;
            // The blob already contains the words — never show both at once.
            captionText.gameObject.SetActive(showingText);
        }

        // The blobs are drawn on their own dark plate, so a second one behind them would
        // only fatten the outline. It is the bare fallback text that needs the backing.
        if (textBackdrop != null)
            textBackdrop.SetActive(showingText);

        Root.SetActive(true);
    }

    public void Hide()
    {
        Root.SetActive(false);
    }
}
