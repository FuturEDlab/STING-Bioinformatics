
using TMPro;
using UnityEngine;

public class EHRMonitorUI : MonoBehaviour
{
    [Header("Assign in Inspector (NO GetComponent)")]
    [SerializeField] private TMP_Text labelTMP;
    [SerializeField] private Renderer panelRenderer;

    public void Show(EHRStateId stateId)
    {
        if (labelTMP != null)
            labelTMP.text = $"STATE: {stateId}";

        if (panelRenderer != null)
            panelRenderer.material.color = StateColor(stateId);
    }

    private Color StateColor(EHRStateId id)
    {
        return id switch
        {
            EHRStateId.SleepIdle    => new Color(0.20f, 0.45f, 0.85f), // blue
            EHRStateId.Scanner      => new Color(0.20f, 0.80f, 0.30f), // green
            EHRStateId.PatientNotes => new Color(0.95f, 0.70f, 0.15f), // orange
            EHRStateId.Override     => new Color(0.60f, 0.25f, 0.85f), // purple
            EHRStateId.Narration    => new Color(0.20f, 0.85f, 0.85f), // cyan
            EHRStateId.RedAlert     => new Color(0.95f, 0.15f, 0.15f), // RED ALERT
            _ => Color.white
        };
    }
}
