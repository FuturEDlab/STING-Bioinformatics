using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Small serialized bag of scene references the steps need, plus the single shared
/// voice-over audio path. Wired on the ScenarioController in the Inspector.
/// </summary>
[Serializable]
public class ScenarioContext
{
    [Header("Voice-over (route this AudioSource's output to the Narration mixer group)")]
    [SerializeField] private AudioSource voSource;

    [Header("Question / PC UI")]
    [SerializeField] private Quiz quiz;
    [SerializeField] private GameObject pcUiRoot;   // quiz canvas/panel root, toggled with SetActive
    [SerializeField] private ResultsUI resultsUI;   // optional end screen

    [Header("Player / XR Rig (optional)")]
    [SerializeField] private BNG.BNGPlayerController player;
    [SerializeField] private Transform playerRig;

    public AudioSource VoSource => voSource;
    public Quiz Quiz => quiz;
    public GameObject PcUiRoot => pcUiRoot;
    public ResultsUI ResultsUI => resultsUI;
    public BNG.BNGPlayerController Player => player;
    public Transform PlayerRig => playerRig;

    /// <summary>Injected at runtime by ScenarioController so the context can run coroutines.</summary>
    public MonoBehaviour Runner { get; set; }

    private Coroutine voRoutine;

    /// <summary>
    /// The ONE audio path used by every step (NarratorStep + UIStep feedback). Plays a
    /// clip and invokes <paramref name="onFinished"/> when it ends. A null clip (or a
    /// missing source/runner) completes immediately.
    /// </summary>
    public void PlayVoice(AudioClip clip, Action onFinished)
    {
        // Cancel any pending wait so an interrupted VO never fires a stale callback.
        if (voRoutine != null && Runner != null)
        {
            Runner.StopCoroutine(voRoutine);
            voRoutine = null;
        }

        if (clip == null || voSource == null || Runner == null)
        {
            onFinished?.Invoke();
            return;
        }

        voSource.Stop();
        voSource.clip = clip;
        voSource.Play();
        voRoutine = Runner.StartCoroutine(WaitVoice(clip.length, onFinished));
    }

    public void StopVoice()
    {
        if (voRoutine != null && Runner != null)
        {
            Runner.StopCoroutine(voRoutine);
            voRoutine = null;
        }
        if (voSource != null)
            voSource.Stop();
    }

    private IEnumerator WaitVoice(float seconds, Action done)
    {
        yield return new WaitForSeconds(seconds);
        voRoutine = null;
        done?.Invoke();
    }
}
