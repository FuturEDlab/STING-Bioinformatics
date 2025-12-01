using UnityEngine;
using TMPro;

public class ResultsUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI resultText;

    public void ShowResult(string message)
    {
        panel.SetActive(true);
        resultText.text = message;
    }
}