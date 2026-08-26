using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

/// <summary>
/// The main menu, and — when <see cref="menuDrivesTraining"/> is on — the training session
/// around it.
///
/// Two modes, so the one prefab keeps working in every scene that already has it:
///
///  * <b>Menu only</b> (the default, and what Hospital Room and the older test scenes get).
///    Exactly what this script always did: park the three panels in front of the player and
///    switch them between one another. Nothing starts, nothing locks, and the face buttons
///    are ignored.
///
///  * <b>Menu drives training</b> (Mohamed Test Scene). The player starts in the corner with
///    the menu in front of them and cannot walk off until they press Begin Training, which is
///    also what starts the scenario. From then on Y or B fades to black, stands them back in
///    that same corner and brings the menu up again, so the menu is always read from the spot
///    it was authored for rather than from wherever the player happened to be standing.
/// </summary>
public class GameManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject controlsPanel;

    [Header("Training session")]
    [Tooltip("On: the menu is up at the start, the player cannot move until Begin Training, and Y/B brings the menu back mid-session. Off: this behaves exactly as it always has — the panels are placed and hidden, and nothing else happens. Leave OFF in scenes that only borrow the menu.")]
    [SerializeField] private bool menuDrivesTraining;

    [Tooltip("Started by Begin Training. Set its Play On Start to OFF, or the scenario runs while the menu is still up.")]
    [SerializeField] private ScenarioController scenario;

    [Tooltip("Where the player is stood for the menu. Left empty, the spot the player starts the scene at is used — which is the corner the menu was placed in front of.")]
    [SerializeField] private Transform menuStandPoint;

    [Tooltip("Fade the view to black across the move to and from the menu. Off makes the teleport instant and jarring.")]
    [SerializeField] private bool fadeOnMenuMove = true;

    [Tooltip("Extra seconds held at full black, after the fade, before the player is moved.")]
    [Min(0f)]
    [SerializeField] private float blackHoldSeconds = 0.1f;

    private Vector3 panelPosition = new Vector3(5.96999979f, 1.8f, -10.75f);
    private Quaternion panelRotation = Quaternion.Euler(new Vector3(0, -40.66f, 0));

    private Vector3 menuFootPosition;
    private Vector3 menuFacing;
    private Vector3 returnFootPosition;
    private Vector3 returnFacing;
    private bool hasReturnPoint;
    private bool trainingStarted;
    private LocomotionProvider[] locomotion;
    private Coroutine move;

    /// <summary>True while any of the three panels is on screen.</summary>
    public bool MenuOpen =>
        (mainMenuPanel != null && mainMenuPanel.activeSelf) ||
        (settingsPanel != null && settingsPanel.activeSelf) ||
        (controlsPanel != null && controlsPanel.activeSelf);

    /// <summary>True once Begin Training has been pressed.</summary>
    public bool TrainingStarted => trainingStarted;

    void Start()
    {
        PlacePanel(settingsPanel);
        PlacePanel(controlsPanel);
        PlacePanel(mainMenuPanel);

        SetActive(settingsPanel, false);
        SetActive(controlsPanel, false);

        if (!menuDrivesTraining)
        {
            SetActive(mainMenuPanel, false);
            return;
        }

        // Read before anything can move the player: this is the corner the scene was authored
        // with the menu in front of, and the spot Y/B brings them back to.
        CaptureMenuStandPoint();

        if (scenario == null)
            Debug.LogWarning($"[GameManager] '{name}' drives the training but has no Scenario Controller assigned, so Begin Training will only close the menu.", this);

        SetActive(mainMenuPanel, true);
        SetMovementEnabled(false);
    }

    void Update()
    {
        if (!menuDrivesTraining || MenuOpen || move != null)
            return;

        // Y (left hand) and B (right hand) on a Touch/Quest controller. Whichever hand the
        // player reaches for, the menu comes back.
        if (Rig.YButtonDown || Rig.BButtonDown)
            OpenMainMenu();
    }

    /// <summary>
    /// Begin Training. Starts the scenario the first time and lets the player move; pressed
    /// again later it behaves like Continue, so a menu without a Continue button on it yet
    /// still gets the player back into the room rather than restarting the story.
    /// </summary>
    public void BeginTraining()
    {
        if (trainingStarted)
        {
            ContinueTraining();
            return;
        }

        trainingStarted = true;

        HideAllPanels();
        SetMovementEnabled(true);

        if (scenario != null)
            scenario.Begin();
    }

    /// <summary>
    /// Continue Training. Closes the menu, stands the player back where they were when it
    /// opened, and gives them movement back. Wire the Continue Training button to this.
    /// </summary>
    public void ContinueTraining()
    {
        if (!hasReturnPoint)
        {
            HideAllPanels();
            SetMovementEnabled(true);
            return;
        }

        StartMove(returnFootPosition, returnFacing, showMenuOnArrival: false);
    }

    /// <summary>
    /// Fade out, stand the player back in the menu corner, and bring the main menu up. This
    /// is what Y/B does; it is public so a UI button or a test can call it too.
    /// </summary>
    [ContextMenu("Open main menu")]
    public void OpenMainMenu()
    {
        if (!menuDrivesTraining)
        {
            SetActive(settingsPanel, false);
            SetActive(controlsPanel, false);
            SetActive(mainMenuPanel, true);
            return;
        }

        RememberWhereThePlayerIs();
        SetMovementEnabled(false);
        StartMove(menuFootPosition, menuFacing, showMenuOnArrival: true);
    }

    public void MainMenuToSettings()
    {
        SetActive(settingsPanel, true);
        SetActive(mainMenuPanel, false);
    }

    public void SettingsToMainMenu()
    {
        SetActive(mainMenuPanel, true);
        SetActive(settingsPanel, false);
    }

    public void MainMenuToControls()
    {
        SetActive(controlsPanel, true);
        SetActive(mainMenuPanel, false);
    }

    public void ControlsToMainMenu()
    {
        SetActive(mainMenuPanel, true);
        SetActive(controlsPanel, false);
    }

    public void ResetExperience()
    {
        Debug.Log("Resetting Progress...");
        //To do: reset progress
    }

    // ------------------------------------------------------------------ moving the player

    /// <summary>
    /// Fade to black, put the player down at <paramref name="footPosition"/>, then fade back.
    /// The menu is only switched on once the screen is already black, so the panel never pops
    /// in over a room the player can still see.
    /// </summary>
    private void StartMove(Vector3 footPosition, Vector3 facing, bool showMenuOnArrival)
    {
        if (move != null)
            StopCoroutine(move);

        move = StartCoroutine(MoveRoutine(footPosition, facing, showMenuOnArrival));
    }

    private IEnumerator MoveRoutine(Vector3 footPosition, Vector3 facing, bool showMenuOnArrival)
    {
        bool fade = fadeOnMenuMove && Rig.HasFade;

        if (fade)
        {
            Rig.FadeToBlack();
            // Neither fader has a completion callback; both ramp alpha at a fixed speed per
            // second, so the wait is derived from that.
            yield return new WaitForSeconds(Rig.FadeInSeconds + blackHoldSeconds);
        }

        if (!Rig.TeleportTo(footPosition, facing))
            Debug.LogWarning($"[GameManager] '{name}' has no player rig to move.", this);

        // Panels are switched here, behind full black, so neither the menu appearing nor the
        // menu vanishing is ever seen happening.
        if (showMenuOnArrival)
        {
            SetActive(settingsPanel, false);
            SetActive(controlsPanel, false);
            SetActive(mainMenuPanel, true);
        }
        else
        {
            HideAllPanels();
        }

        // Let the frame finish before anything else touches the rig, so the character
        // controller switched back on inside the teleport does not resolve collisions against
        // the position it was moved out of.
        yield return new WaitForEndOfFrame();

        if (fade)
            Rig.FadeFromBlack();

        if (!showMenuOnArrival)
            SetMovementEnabled(true);

        move = null;
    }

    /// <summary>
    /// The spot the player is stood for the menu. A marker wins if one is assigned; otherwise
    /// it is wherever the rig was placed in the scene, which is the corner the menu panels
    /// were positioned in front of.
    /// </summary>
    private void CaptureMenuStandPoint()
    {
        if (menuStandPoint != null)
        {
            menuFootPosition = menuStandPoint.position;
            menuFacing = menuStandPoint.forward;
            return;
        }

        Transform body = Rig.Body;
        if (body != null)
        {
            menuFootPosition = body.position;
            menuFacing = body.forward;
            return;
        }

        // No rig at all — face the panels from where they are, so the fields are at least not
        // zero.
        menuFootPosition = panelPosition;
        menuFacing = panelRotation * Vector3.forward;
    }

    /// <summary>
    /// Where Continue Training puts the player back. Measured from the head rather than the
    /// rig root: with room scale those are metres apart once the player has walked around
    /// their play space, and the head is where they actually are.
    /// </summary>
    private void RememberWhereThePlayerIs()
    {
        Transform head = Rig.Head;
        if (head == null)
        {
            hasReturnPoint = false;
            return;
        }

        // The room floor is flat, so the height the player started at is the height they
        // should come back to — no raycast needed.
        returnFootPosition = new Vector3(head.position.x, menuFootPosition.y, head.position.z);

        Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        returnFacing = flat.sqrMagnitude > 0.0001f ? flat.normalized : menuFacing;
        hasReturnPoint = true;
    }

    // --------------------------------------------------------------------------- movement

    /// <summary>
    /// Locks and unlocks walking, turning and teleporting by switching the rig's locomotion
    /// providers off. The interactors are left alone, so the player can still point at the
    /// menu and pick things up — they simply cannot leave the corner.
    /// </summary>
    private void SetMovementEnabled(bool on)
    {
        if (locomotion == null)
        {
            PlayerRig rig = Rig.XR;
            locomotion = rig != null
                ? rig.GetComponentsInChildren<LocomotionProvider>(true)
                : System.Array.Empty<LocomotionProvider>();

            if (locomotion.Length == 0)
                Debug.LogWarning($"[GameManager] '{name}' found no locomotion providers on the rig, so movement cannot be locked.", this);
        }

        for (int i = 0; i < locomotion.Length; i++)
        {
            if (locomotion[i] != null)
                locomotion[i].enabled = on;
        }
    }

    // ------------------------------------------------------------------------------ panels

    private void HideAllPanels()
    {
        SetActive(mainMenuPanel, false);
        SetActive(settingsPanel, false);
        SetActive(controlsPanel, false);
    }

    private void PlacePanel(GameObject panel)
    {
        if (panel != null)
            panel.transform.SetPositionAndRotation(panelPosition, panelRotation);
    }

    private static void SetActive(GameObject panel, bool on)
    {
        if (panel != null)
            panel.SetActive(on);
    }
}
