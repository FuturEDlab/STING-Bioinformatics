using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{

    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject controlsPanel;
    private Vector3 panelPosition = new Vector3(5.96999979f,1.8f,-10.75f);
    private Quaternion panelRotation = Quaternion.Euler(new Vector3(0,-40.66f,0));
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        settingsPanel.transform.position = panelPosition;
        settingsPanel.transform.rotation = panelRotation;
        settingsPanel.SetActive(false);
        controlsPanel.transform.position = panelPosition;
        controlsPanel.transform.rotation = panelRotation;
        controlsPanel.SetActive(false);
        mainMenuPanel.transform.position = panelPosition;
        mainMenuPanel.transform.rotation = panelRotation;
        mainMenuPanel.SetActive(true);
        //mainMenuPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BeginTraining()
    {
        mainMenuPanel.SetActive(false);
        // To do: Officially start training
    }

    public void MainMenuToSettings()
    {
        settingsPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    public void SettingsToMainMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    public void MainMenuToControls()
    {
        controlsPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    public void ControlsToMainMenu()
    {
        mainMenuPanel.SetActive(true);
        controlsPanel.SetActive(false);
    }

    public void ResetExperience()
    {
        Debug.Log("Resetting Progress...");
        //To do: reset progress
    } 
}
