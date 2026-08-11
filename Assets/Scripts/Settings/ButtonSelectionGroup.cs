using System;
using UnityEngine;

public class ButtonSelectionGroup : MonoBehaviour
{
    [SerializeField] private StatefulButtonSprites[] buttons;

    public int SelectedIndex { get; private set; } = -1;

    public bool HasSelection => SelectedIndex >= 0;

    public event Action<int> OnSelectionChanged;

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

        SelectedIndex = selectedIndex;

        var selectedName = buttons[selectedIndex]?.gameObject.name ?? "Unknown";
        Debug.Log($"{nameof(ButtonSelectionGroup)}: selected major index {SelectedIndex} ({selectedName})");

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
            {
                Debug.LogWarning($"{nameof(ButtonSelectionGroup)}: buttons[{i}] is null.");
                continue;
            }

            buttons[i].SetSelected(i == selectedIndex);
        }

        OnSelectionChanged?.Invoke(SelectedIndex);
    }

    public void ClearSelection()
    {
        SelectedIndex = -1;

        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            buttons[i].SetSelected(false);
        }

        OnSelectionChanged?.Invoke(SelectedIndex);
    }
}
