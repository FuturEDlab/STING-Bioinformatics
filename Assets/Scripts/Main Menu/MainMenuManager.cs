using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
  public void LoadScene()
  {
    SceneManager.LoadSceneAsync("Hospital Room");
  }

  public void ExitApplication()
  {
    Debug.Log("Application is closing...");

    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
  }
  
}
