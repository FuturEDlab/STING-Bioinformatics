using BNG;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Makes the hand lasers able to click world-space UI.
///
/// The rig already carries BNG's <see cref="UIPointer"/> on both hands, but a UIPointer only
/// draws the beam — the pointer events themselves come from <see cref="VRUISystem"/>, an
/// input module that was never added to the scene. What the EventSystem does have is
/// InputSystemUIInputModule, the desktop mouse/gamepad module.
///
/// An EventSystem runs exactly one input module: it walks its modules and keeps the first
/// that says it can run. The desktop one is first and always says yes, so even once a
/// VRUISystem exists it would never get a turn. Hence this both adds the VR module and
/// switches the desktop one off.
///
/// Safe to run more than once.
/// </summary>
public static class VRUIInputSetup
{
    [MenuItem("Tools/STING/Set Up VR UI Input (hand laser clicks)")]
    private static void Run()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);

        if (eventSystem == null)
        {
            EditorUtility.DisplayDialog("VR UI input setup",
                "No EventSystem in the open scene. Open Hospital Room and try again.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(eventSystem.gameObject, "Set Up VR UI Input");

        var log = new System.Text.StringBuilder();
        log.AppendLine($"=== VR UI INPUT ({eventSystem.name}) ===");

        DisableDesktopModule(eventSystem, log);
        VRUISystem vr = AddVrModule(eventSystem, log);
        EnablePointers(vr, log);
        ReportRaycasters(log);

        EditorSceneManager.MarkSceneDirty(eventSystem.gameObject.scene);
        Selection.activeGameObject = eventSystem.gameObject;

        log.AppendLine("Point a hand at the panel and pull the right trigger. To get the mouse working again for desktop testing, re-enable Input System UI Input Module and disable VR UI System.");
        Debug.Log(log.ToString(), eventSystem);
    }

    private static void DisableDesktopModule(EventSystem eventSystem, System.Text.StringBuilder log)
    {
        InputSystemUIInputModule desktop = eventSystem.GetComponent<InputSystemUIInputModule>();

        if (desktop == null)
        {
            log.AppendLine("  ok     no desktop input module in the way");
            return;
        }

        if (!desktop.enabled)
        {
            log.AppendLine("  ok     desktop input module already disabled");
            return;
        }

        Undo.RecordObject(desktop, "Disable desktop input module");

        // Disabled rather than deleted: it is the only thing that makes the panels clickable
        // with a mouse in the editor, and getting it back should be one tick box.
        desktop.enabled = false;
        EditorUtility.SetDirty(desktop);

        log.AppendLine("  ok     disabled Input System UI Input Module — it was claiming the EventSystem, so the VR module never ran (mouse clicks on UI stop working in this scene)");
    }

    private static VRUISystem AddVrModule(EventSystem eventSystem, System.Text.StringBuilder log)
    {
        VRUISystem vr = eventSystem.GetComponent<VRUISystem>();

        if (vr == null)
        {
            vr = Undo.AddComponent<VRUISystem>(eventSystem.gameObject);
            log.AppendLine("  ok     added VR UI System to the EventSystem");
        }
        else
        {
            log.AppendLine("  ok     VR UI System already present");
        }

        Undo.RecordObject(vr, "Configure VR UI System");
        vr.enabled = true;
        vr.SelectedHand = ControllerHand.Right;

        if (vr.ControllerInput == null || vr.ControllerInput.Count == 0)
            vr.ControllerInput = new System.Collections.Generic.List<ControllerBinding> { ControllerBinding.RightTrigger };

        EditorUtility.SetDirty(vr);
        log.AppendLine($"  ok     click binding: {string.Join(", ", vr.ControllerInput)}");

        return vr;
    }

    /// <summary>
    /// The pointer for the selected hand has to be ACTIVE, and this is the part that is easy
    /// to miss. A UIPointer registers itself as the aiming transform from its OnEnable, so a
    /// pointer switched off in the scene never tells the UI system where the hand is. The
    /// module then raycasts from an unparented camera sitting at the world origin: no beam,
    /// no hover, no clicks, and nothing in the console to say why.
    /// </summary>
    private static void EnablePointers(VRUISystem vr, System.Text.StringBuilder log)
    {
        UIPointer[] pointers = Object.FindObjectsByType<UIPointer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (pointers.Length == 0)
        {
            log.AppendLine("PROBLEM  no UIPointer anywhere in the scene, so there is no beam to point with. They live on the rig's controllers.");
            return;
        }

        bool foundSelected = false;

        for (int i = 0; i < pointers.Length; i++)
        {
            UIPointer pointer = pointers[i];

            // Without this the pointer never tells the UI system where it is aiming.
            if (!pointer.AutoUpdateUITransforms)
            {
                Undo.RecordObject(pointer, "Enable pointer auto transforms");
                pointer.AutoUpdateUITransforms = true;
                EditorUtility.SetDirty(pointer);
                log.AppendLine($"  FIXED  '{pointer.name}' had Auto Update UI Transforms off — it would never have registered itself as the aiming transform.");
            }

            if (pointer.ControllerSide != vr.SelectedHand)
            {
                log.AppendLine($"  note   '{pointer.name}' is the {pointer.ControllerSide} pointer; the UI system is set to {vr.SelectedHand}, so it is left as it is ({(pointer.gameObject.activeInHierarchy ? "active" : "inactive")}).");
                continue;
            }

            foundSelected = true;
            ActivateChain(pointer.gameObject, log);
        }

        if (!foundSelected)
            log.AppendLine($"PROBLEM  no UIPointer is set to the {vr.SelectedHand} hand, which is the one the UI system is listening to.");
    }

    private static void ActivateChain(GameObject target, System.Text.StringBuilder log)
    {
        bool changed = false;

        // Outermost first: switching a child on inside a dead parent achieves nothing.
        var chain = new System.Collections.Generic.List<GameObject>();

        for (Transform t = target.transform; t != null; t = t.parent)
            chain.Add(t.gameObject);

        chain.Reverse();

        foreach (GameObject go in chain)
        {
            if (go.activeSelf)
                continue;

            Undo.RecordObject(go, "Enable hand pointer");
            go.SetActive(true);
            EditorUtility.SetDirty(go);

            log.AppendLine($"  FIXED  '{go.name}' was switched off in the scene — switched on.");
            changed = true;
        }

        if (!changed)
            log.AppendLine($"  ok     '{target.name}' is active");
    }

    private static void ReportRaycasters(System.Text.StringBuilder log)
    {
        var panel = Object.FindFirstObjectByType<QuestionPanelManager>(FindObjectsInactive.Include);

        if (panel == null)
        {
            log.AppendLine("  note   no question panel in this scene to check.");
            return;
        }

        if (panel.GetComponent<GraphicRaycaster>() == null)
            log.AppendLine("PROBLEM  the question panel canvas has no Graphic Raycaster, so nothing on it can be hit at all.");
        else
            log.AppendLine("  ok     question panel canvas has a Graphic Raycaster");

        // Left over from the XR Interaction Toolkit experiment. BNG only defers to XRIT when
        // XRIT_INTEGRATION is defined, and it is not, so this component does nothing here.
        if (panel.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() != null)
            log.AppendLine("  note   the panel also has a TrackedDeviceGraphicRaycaster (XR Interaction Toolkit). Harmless, but inert — this project drives UI through BNG, not XRIT.");
    }
}
