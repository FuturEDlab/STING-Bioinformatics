using System.Collections;
using System.Collections.Generic;
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

    [Tooltip("Optional parent transform under which spawned icons will be placed")]
    public Transform iconParent;

    [Tooltip("Optional list of scene icon instances. If provided, the entry at the same index will use this scene object instead of instantiating the prefab.")]
    public List<GameObject> sceneIconInstances = new List<GameObject>();

    int currentIndex = -1;
    Coroutine runningCoroutine;
    GameObject currentIconInstance;
    bool currentIconIsScene = false;

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
        HideAllSceneIcons();
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
        if (currentIconInstance != null)
        {
            if (currentIconIsScene)
                currentIconInstance.SetActive(false);
            else
                Destroy(currentIconInstance);
            currentIconInstance = null;
            currentIconIsScene = false;
        }
    }

    void HideAllSceneIcons()
    {
        if (sceneIconInstances == null) return;
        for (int i = 0; i < sceneIconInstances.Count; i++)
        {
            var go = sceneIconInstances[i];
            if (go != null)
                go.SetActive(false);
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

        // Handle icon display
        if (currentIconInstance != null)
        {
            if (currentIconIsScene)
                currentIconInstance.SetActive(false);
            else
                Destroy(currentIconInstance);
            currentIconInstance = null;
            currentIconIsScene = false;
        }

        if (entry.showIcon)
        {
            // Prefer a scene instance if provided for this index
            if (sceneIconInstances != null && index >= 0 && index < sceneIconInstances.Count && sceneIconInstances[index] != null)
            {
                currentIconInstance = sceneIconInstances[index];
                currentIconIsScene = true;
                currentIconInstance.SetActive(true);
            }
            else if (entry.iconPrefab != null)
            {
                Transform parent = iconParent != null ? iconParent : (targetRenderer != null ? targetRenderer.transform : null);
                if (parent != null)
                    currentIconInstance = Instantiate(entry.iconPrefab, parent);
                else
                    currentIconInstance = Instantiate(entry.iconPrefab);

                currentIconIsScene = false;
            }

            if (currentIconInstance != null)
            {
                var animator = currentIconInstance.GetComponent<Animator>();
                if (animator != null)
                {
                    if (!string.IsNullOrEmpty(entry.iconAnimatorTrigger))
                        animator.SetTrigger(entry.iconAnimatorTrigger);
                    else
                        animator.Play(0);
                }
            }
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
