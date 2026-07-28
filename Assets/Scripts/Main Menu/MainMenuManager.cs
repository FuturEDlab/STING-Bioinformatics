using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the in-world menu panels: placement, show/hide, scene load and quit.
///
/// Deliberately knows nothing about the training sequence - the Begin button calls
/// ScenarioController.Begin() directly, and HideMenu() here just gets the panel out of the way.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
  [Header("Panels (optional - leave empty in the Main Menu scene)")]
  [SerializeField] private GameObject mainMenuPanel;
  [SerializeField] private GameObject settingsPanel;

  [Header("Placement")]
  [Tooltip("Reposition the panels on Start. Turn off to keep whatever the scene author set.")]
  [SerializeField] private bool placePanelsOnStart = true;
  [SerializeField] private Vector3 panelPosition = new Vector3(5.96999979f, 1.8f, -10.75f);
  [SerializeField] private Vector3 panelEulerAngles = new Vector3(0f, -40.66f, 0f);

  private void Start()
  {
    if (!placePanelsOnStart)
      return;

    Quaternion rotation = Quaternion.Euler(panelEulerAngles);
    Place(settingsPanel, rotation);
    Place(mainMenuPanel, rotation);

    ShowMainMenu();
  }

  private void Place(GameObject panel, Quaternion rotation)
  {
    if (panel == null) return;

    panel.transform.position = panelPosition;
    panel.transform.rotation = rotation;
  }

  /// <summary>Main menu visible, settings hidden.</summary>
  public void ShowMainMenu()
  {
    if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    if (settingsPanel != null) settingsPanel.SetActive(false);
  }

  /// <summary>Settings visible, main menu hidden.</summary>
  public void ShowSettings()
  {
    if (settingsPanel != null) settingsPanel.SetActive(true);
    if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
  }

  /// <summary>Clears both panels. Wire alongside ScenarioController.Begin() on the Begin button.</summary>
  public void HideMenu()
  {
    if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    if (settingsPanel != null) settingsPanel.SetActive(false);
  }

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
