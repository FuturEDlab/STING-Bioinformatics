using UnityEngine;
using TMPro;

public class ResultsUI : MonoBehaviour
{
    [SerializeField] GameObject resultPanel;
    [SerializeField] GameObject quizCanvas;
    [SerializeField] TextMeshProUGUI resultText;

    public void ShowResult(string message)
    {
        resultPanel.SetActive(true);
        quizCanvas.SetActive(false);
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
