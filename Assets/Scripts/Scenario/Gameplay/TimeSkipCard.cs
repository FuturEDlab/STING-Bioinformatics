using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// The "30 minutes later" title card: fades the view to black, holds a line of text on it,
/// then fades back and tells the scenario it is done.
///
/// Wired like every other world beat — a channel in, a channel out. The scenario's
/// <c>S3A_08_TimeSkip30Minutes</c> step raises <see cref="triggerChannel"/> and waits on
/// <see cref="completedChannel"/>, so the story cannot run on underneath the blackout.
///
/// Put it on an ALWAYS-ACTIVE object (the SceneEvents object is the obvious home) — like a
/// SceneEventRelay it only listens while enabled.
/// </summary>
public class TimeSkipCard : MonoBehaviour
{
    [Header("Channels")]
    [Tooltip("The world beat that starts the blackout, e.g. EV_TimeSkip30Min.")]
    [SerializeField] private GameEvent triggerChannel;

    [Tooltip("Raised once the view is back. The scenario step waits on this, so leaving it empty makes the story continue over the top of the fade.")]
    [SerializeField] private GameEvent completedChannel;

    [Header("Card")]
    [Tooltip("The line held on the black.")]
    [SerializeField] private string message = "30 minutes later";

    [Tooltip("Seconds the text is held at full black, on top of the two fades.")]
    [Min(0f)]
    [SerializeField] private float holdSeconds = 2.5f;

    [Tooltip("Extra seconds of black before the text appears, so the card does not land the instant the screen goes dark.")]
    [Min(0f)]
    [SerializeField] private float leadInSeconds = 0.35f;

    [Header("While the screen is black")]
    [Tooltip("Runs at full black, before the view comes back. This is where anything that should have 'already happened' when the player can see again goes — the patient's rashes, a prop moved, an animation state.")]
    [SerializeField] private UnityEvent whileBlack;

    [Tooltip("Log each stage of the blackout.")]
    [SerializeField] private bool debugLogging;

    private Coroutine routine;

    /// <summary>The channel this card listens on. Read by the controller's wiring report.</summary>
    public GameEvent Channel => triggerChannel;

    private void OnEnable()
    {
        if (triggerChannel != null)
            triggerChannel.Subscribe(Play);
        else
            Debug.LogWarning($"[TimeSkipCard] '{name}' has no trigger channel assigned; it will never fire.", this);
    }

    private void OnDisable()
    {
        if (triggerChannel != null)
            triggerChannel.Unsubscribe(Play);

        Stop();
    }

    /// <summary>Run the blackout. Also callable straight from a UnityEvent for testing.</summary>
    [ContextMenu("Play (play mode)")]
    public void Play()
    {
        // A second trigger mid-card would leave two coroutines fighting over the fade and
        // could raise the completion channel twice.
        if (routine != null)
            return;

        routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (debugLogging)
            Debug.Log($"[TimeSkipCard] '{name}' fading out.", this);

        bool fade = Rig.HasFade;

        if (fade)
        {
            Rig.FadeToBlack();
            // Neither fader has a completion callback; both ramp alpha at a fixed speed per
            // second, so the wait is derived from that.
            yield return new WaitForSeconds(Rig.FadeInSeconds);
        }

        if (leadInSeconds > 0f)
            yield return new WaitForSeconds(leadInSeconds);

        if (fade && !string.IsNullOrEmpty(message))
            Rig.ShowFadeMessage(message);

        // Everything that should have happened "during" the skip happens here, unseen.
        whileBlack?.Invoke();

        if (holdSeconds > 0f)
            yield return new WaitForSeconds(holdSeconds);

        if (fade)
        {
            // The card is left up across the fade rather than switched off in front of the
            // player: its opacity rides the fade's, so it dissolves with the black instead
            // of blinking out a frame before it. Dropped once the view is back.
            Rig.FadeFromBlack();
            yield return new WaitForSeconds(Rig.FadeOutSeconds);
            Rig.HideFadeMessage();
        }

        if (debugLogging)
            Debug.Log($"[TimeSkipCard] '{name}' done.", this);

        routine = null;

        if (completedChannel != null)
            completedChannel.Raise();
    }

    private void Stop()
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = null;

        // Never leave the player staring at black text on a black screen because the object
        // was switched off mid-card.
        Rig.HideFadeMessage();
    }
}
