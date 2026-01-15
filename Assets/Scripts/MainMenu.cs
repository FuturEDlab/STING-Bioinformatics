using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
  public void LoadScene()
  {
    SceneManager.LoadSceneAsync("Hospital Room");
  }
  
}
