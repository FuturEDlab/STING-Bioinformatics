using UnityEngine;

public class ButtonSelectionGroup : MonoBehaviour
{
    [SerializeField] private StatefulButtonSprites[] buttons;

    // Selects one button and deselects every other button.
    public void SelectButton(int selectedIndex)
    {
        if (buttons == null)
        {
            Debug.LogWarning($"{nameof(ButtonSelectionGroup)}: buttons array is null.");
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= buttons.Length)
        {
            Debug.LogWarning($"{nameof(ButtonSelectionGroup)}: selectedIndex {selectedIndex} is out of range for buttons.Length {buttons.Length}.");
            return;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
            {
                Debug.LogWarning($"{nameof(ButtonSelectionGroup)}: buttons[{i}] is null.");
                continue;
            }

            buttons[i].SetSelected(i == selectedIndex);
        }
    }
}