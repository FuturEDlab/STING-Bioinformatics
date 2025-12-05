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
    
    // 🚪 Exit button
    public void ExitApplication()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // stop Play Mode
    #else
        Application.Quit();                                // quit the built app
    #endif
    }
}
