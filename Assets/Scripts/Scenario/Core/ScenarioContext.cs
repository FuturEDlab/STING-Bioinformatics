using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Context Object: the single hand-off of shared services given to every step.
/// Steps read what they need from here and never call FindObjectOfType or touch a singleton.
///
/// Serialized inline on ScenarioController, so all wiring is visible in one Inspector block.
/// </summary>
[Serializable]
public class ScenarioContext
{
    [Header("Voice Over")]
    [Tooltip("The one AudioSource all scenario narration and feedback plays through. " +
             "Route its Output to the Voices group on MainAudioMixer (exposed as NarrationVolume).")]
    [SerializeField] private AudioSource voiceSource;

    [Header("UI")]
    [Tooltip("Root object of the PC / EHR screen UI. Steps toggle this while they own the screen.")]
    [SerializeField] private GameObject pcUiRoot;

    [Tooltip("Quiz panel that displays questions and answer buttons.")]
    [SerializeField] private Quiz quiz;

    [Header("Player")]
    [Tooltip("Optional: the XR rig root, for steps that need player position or gaze.")]
    [SerializeField] private Transform playerRig;

    public AudioSource VoiceSource => voiceSource;
    public GameObject PcUiRoot => pcUiRoot;
    public Quiz Quiz => quiz;
    public Transform PlayerRig => playerRig;

    /// <summary>
    /// The MonoBehaviour steps run coroutines on. Injected by ScenarioController in Awake.
    /// </summary>
    public MonoBehaviour Runner { get; set; }

    private Coroutine voiceRoutine;

    /// <summary>
    /// The single audio path for all scenario voice over. Invokes <paramref name="onDone"/>
    /// when the clip finishes. A missing source or clip completes immediately rather than
    /// stalling the scenario.
    /// </summary>
    public void PlayVoice(AudioClip clip, Action onDone)
    {
        // Cancel any pending wait first, so a stale callback from the previous clip
        // can never fire after we have moved on.
        CancelVoiceWait();

        if (voiceSource == null || clip == null || Runner == null)
        {
            if (clip != null && voiceSource == null)
                Debug.LogWarning("[Scenario] No voiceSource assigned on ScenarioContext - skipping VO.");

            onDone?.Invoke();
            return;
        }

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();

        voiceRoutine = Runner.StartCoroutine(WaitForClip(clip.length, onDone));
    }

    /// <summary>Stops playback and cancels any pending completion callback. Called from step Exit().</summary>
    public void StopVoice()
    {
        CancelVoiceWait();

        if (voiceSource != null)
            voiceSource.Stop();
    }

    private IEnumerator WaitForClip(float seconds, Action onDone)
    {
        yield return new WaitForSeconds(seconds);

        // Clear the handle before invoking, so the Exit() that follows this callback
        // cannot try to stop a coroutine that has already run to completion.
        voiceRoutine = null;
        onDone?.Invoke();
    }

    private void CancelVoiceWait()
    {
        if (voiceRoutine != null && Runner != null)
            Runner.StopCoroutine(voiceRoutine);

        voiceRoutine = null;
    }
}
