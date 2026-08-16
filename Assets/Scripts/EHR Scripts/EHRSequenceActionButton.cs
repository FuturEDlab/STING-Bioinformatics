using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class EHRSequenceActionButton : MonoBehaviour
{
    [Header("Sequence Reference")]
    public EHRSequencePlayer sequencePlayer;

    [Header("Matching Action Name")]
    public string actionName = "Next";

    private Button button;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(Trigger);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(Trigger);
        }
    }

    public void Trigger()
    {
        if (sequencePlayer == null)
        {
            Debug.LogWarning("EHRSequenceActionButton: Sequence Player is not assigned.");
            return;
        }

        sequencePlayer.TriggerAction(actionName);
    }
}
