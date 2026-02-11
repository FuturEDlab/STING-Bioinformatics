using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EHRContext : MonoBehaviour
{
    [Header("Assign in Inspector (NO GetComponent)")]
    [SerializeField] private TMP_Text screenTextTMP;   // ScreenTextTMP
    [SerializeField] private EHRMonitorUI monitorUI;    // StateMonitor object
    [SerializeField] private GameObject redAlert3D;     // Optional: 3D RED ALERT icon/text

    [Header("Auto cycle")]
    [SerializeField] private float secondsPerState = 5f;

    // Public read-only access for states
    public TMP_Text ScreenText => screenTextTMP;

    private readonly Dictionary<EHRStateId, EHRState> states = new();
    private EHRState currentState;

    private readonly EHRStateId[] cycleOrder =
    {
        EHRStateId.SleepIdle,
        EHRStateId.Scanner,
        EHRStateId.PatientNotes,
        EHRStateId.Override,
        EHRStateId.Narration,
        EHRStateId.RedAlert
    };

    private int cycleIndex = 0;
    private float timer = 0f;

    private void Awake()
    {
        // Register all states (each in separate file)
        states[EHRStateId.SleepIdle]    = new EHRState_SleepIdle(this);
        states[EHRStateId.Scanner]      = new EHRState_Scanner(this);
        states[EHRStateId.PatientNotes] = new EHRState_PatientNotes(this);
        states[EHRStateId.Override]     = new EHRState_Override(this);
        states[EHRStateId.Narration]    = new EHRState_Narration(this);
        states[EHRStateId.RedAlert]     = new EHRState_RedAlert(this);

        cycleIndex = 0;
        ChangeState(cycleOrder[cycleIndex]);
    }

    private void Update()
    {
        // Auto-cycle every 5 seconds
        timer += Time.deltaTime;
        if (timer >= secondsPerState)
        {
            timer = 0f;
            cycleIndex = (cycleIndex + 1) % cycleOrder.Length;
            ChangeState(cycleOrder[cycleIndex]);
        }

        currentState?.Tick();
    }

    public void ChangeState(EHRStateId next)
    {
        if (currentState != null && currentState.Id == next)
            return;

        if (!states.TryGetValue(next, out var nextState))
        {
            Debug.LogError("State missing: " + next);
            return;
        }

        currentState?.Exit();
        currentState = nextState;

        // Update monitor
        if (monitorUI != null)
            monitorUI.Show(next);

        Debug.Log("[EHR] STATE CHANGED TO: " + next);

        currentState.Enter();
    }

    public void ShowRedAlert3D(bool show)
    {
        if (redAlert3D != null)
            redAlert3D.SetActive(show);
    }

    // Placeholder interaction locks (connect XR later)
    public void SetInteractables(bool keyboard, bool scanner, bool screen)
    {
        Debug.Log($"[EHR] Interactables => Keyboard:{keyboard} Scanner:{scanner} Screen:{screen}");
        // TODO: connect to XR interactables / colliders / scripts
    }
}
