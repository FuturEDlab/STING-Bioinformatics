using UnityEngine;

public class ButtonSelectionGroup : MonoBehaviour
{
    [SerializeField] private StatefulButtonSprites[] buttons;

    //Selects one button and deselects every other button.
    public void SelectButton(int selectedIndex)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].SetSelected(i == selectedIndex);
        }
    }
}