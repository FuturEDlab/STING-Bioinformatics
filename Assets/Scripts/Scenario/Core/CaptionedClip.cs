using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One spoken phrase of a voice-over line: the audio clip plus the caption shown while it
/// plays. The caption is authored as an image blob (exported from Figma); the text field
/// is a fallback so captions work before the blobs are imported, and doubles as the
/// accessible/searchable form of the phrase.
/// </summary>
[Serializable]
public class CaptionedClip
{
    [Tooltip("The recorded phrase. May be empty for lines not recorded yet — the caption is then shown alone for a duration based on the text length.")]
    [SerializeField] private AudioClip clip;

    [Tooltip("Caption blob image (from Figma) shown while this phrase plays.")]
    [SerializeField] private Sprite caption;

    [Tooltip("Fallback caption text, used when no blob sprite is assigned.")]
    [TextArea(1, 3)]
    [SerializeField] private string captionText;

    public AudioClip Clip => clip;
    public Sprite Caption => caption;
    public string CaptionText => captionText;

    /// <summary>
    /// True when this phrase carries a caption of its own. False means "keep showing the
    /// previous one" — captions are split by text length and audio by phrase, so one blob
    /// often spans two clips (and the last clip of a line sometimes has no blob at all).
    /// </summary>
    public bool HasCaption => caption != null || !string.IsNullOrWhiteSpace(captionText);

    /// <summary>True when there is anything to present — audio, blob, or text.</summary>
    public bool HasContent => clip != null || HasCaption;

    /// <summary>
    /// How long to hold the caption when there is no clip to time it against
    /// (unrecorded line): reading-speed estimate from the text, clamped to sane bounds.
    /// </summary>
    public float FallbackSeconds
    {
        get
        {
            int words = string.IsNullOrWhiteSpace(captionText) ? 0 : captionText.Split(' ').Length;
            return Mathf.Clamp(words * 0.35f, 2.5f, 8f);
        }
    }
}

/// <summary>
/// A whole spoken line as a list of phrases. Exists so steps can serialize a LIST of
/// lines (e.g. one feedback line per answer) — Unity cannot serialize nested lists
/// directly.
/// </summary>
[Serializable]
public class VoiceLine
{
    [SerializeField] private List<CaptionedClip> phrases = new List<CaptionedClip>();

    public IReadOnlyList<CaptionedClip> Phrases => phrases;

    public bool HasContent
    {
        get
        {
            for (int i = 0; i < phrases.Count; i++)
            {
                if (phrases[i] != null && phrases[i].HasContent)
                    return true;
            }
            return false;
        }
    }
}
