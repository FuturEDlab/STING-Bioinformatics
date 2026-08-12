using System.Collections;
using UnityEngine;

public class EHRManager : MonoBehaviour
{
    [Tooltip("Sequence asset that defines the ordered screens and triggers")]
    public EHRScreenSequence sequence;

    [Tooltip("SpriteRenderer that will display the screen images")]
    public SpriteRenderer targetRenderer;

    [Tooltip("Automatically start the sequence on Start")]
    public bool playOnStart = true;

    [Tooltip("Loop the sequence when it reaches the end")]
    public bool loop = false;

    int currentIndex = -1;
    Coroutine runningCoroutine;

    void Start()
    {
        if (playOnStart)
            StartSequence();
    }

    public void StartSequence()
    {
        if (sequence == null || sequence.entries == null || sequence.entries.Count == 0)
            return;
        StopSequence();
        currentIndex = 0;
        ShowEntry(currentIndex);
    }

    public void StopSequence()
    {
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }
    }

    void ShowEntry(int index)
    {
        if (sequence == null || sequence.entries == null || index < 0 || index >= sequence.entries.Count)
            return;

        var entry = sequence.entries[index];
        if (targetRenderer != null && entry.sprite != null)
            targetRenderer.sprite = entry.sprite;

        // Cancel any existing timers
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }

        if (entry.trigger == TriggerType.Timer)
        {
            runningCoroutine = StartCoroutine(TimerAdvance(entry.duration));
        }
        // Action and Manual triggers wait for external calls
    }

    IEnumerator TimerAdvance(float duration)
    {
        yield return new WaitForSeconds(duration);
        runningCoroutine = null;
        Advance();
    }

    public void Advance()
    {
        if (sequence == null || sequence.entries == null || sequence.entries.Count == 0)
            return;

        int next = currentIndex + 1;
        if (next >= sequence.entries.Count)
        {
            if (loop) next = 0;
            else return;
        }

        currentIndex = next;
        ShowEntry(currentIndex);
    }

    public void AdvanceByAction(string actionName)
    {
        if (sequence == null || sequence.entries == null || sequence.entries.Count == 0)
            return;

        if (currentIndex < 0 || currentIndex >= sequence.entries.Count)
            return;

        var entry = sequence.entries[currentIndex];
        if (entry.trigger != TriggerType.Action)
            return;

        if (string.IsNullOrEmpty(entry.actionName) || entry.actionName == actionName)
        {
            Advance();
        }
    }

    // Force show a specific index (editor or other controller usage)
    public void ShowIndex(int index)
    {
        if (sequence == null || sequence.entries == null || index < 0 || index >= sequence.entries.Count)
            return;
        currentIndex = index;
        ShowEntry(currentIndex);
    }
}
