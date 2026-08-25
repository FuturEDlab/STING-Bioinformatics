using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds a <see cref="VRPanelAnchor"/> to the scene's question panel so it opens in front of
/// the player, and reports where it currently sits relative to the rig — which is the whole
/// story of why it never appeared in the headset.
///
/// The canvas was already World Space, so the panel was being drawn the entire time. It was
/// simply drawn at the world position it happened to be authored at, which is metres away
/// from where the player actually stands.
///
/// Safe to run more than once.
/// </summary>
public static class QuestionPanelVRSetup
{
    [MenuItem("Tools/STING/Set Up Question Panel for VR")]
    private static void Run()
    {
        QuestionPanelManager panel = Object.FindFirstObjectByType<QuestionPanelManager>(FindObjectsInactive.Include);

        if (panel == null)
        {
            EditorUtility.DisplayDialog("Question panel VR setup",
                "No QuestionPanelManager in the open scene. Open Hospital Room and try again.", "OK");
            return;
        }

        var log = new System.Text.StringBuilder();
        log.AppendLine($"=== QUESTION PANEL -> VR ({panel.name}) ===");

        ReportCanvas(panel, log);
        ReportDistanceFromPlayer(panel, log);
        DisarmLegacyHideOnAwake(panel, log);
        FixContainerOverride(panel, log);

        VRPanelAnchor anchor = panel.GetComponent<VRPanelAnchor>();

        if (anchor == null)
        {
            anchor = Undo.AddComponent<VRPanelAnchor>(panel.gameObject);
            log.AppendLine("  ok     added VR Panel Anchor — the panel is now placed 1.7 m in front of the player, 1.4 m wide, each time it opens");
        }
        else
        {
            log.AppendLine("  ok     VR Panel Anchor already present");
        }

        SerializedObject so = new SerializedObject(panel);
        SerializedProperty anchorProp = so.FindProperty("vrAnchor");

        if (anchorProp != null && anchorProp.objectReferenceValue == null)
        {
            anchorProp.objectReferenceValue = anchor;
            so.ApplyModifiedProperties();
            log.AppendLine("  ok     linked it on the QuestionPanelManager");
        }

        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        Selection.activeGameObject = panel.gameObject;

        log.AppendLine("Nothing else to wire. Open the quiz in the headset and tune Distance / Panel Width Metres on the anchor.");
        Debug.Log(log.ToString(), panel);
    }

    /// <summary>
    /// The scene instance carries its own override for QP, and an instance override beats
    /// anything fixed on the prefab — so repairing the prefab alone changes nothing here.
    ///
    /// The override points QP at the canvas root, the object the manager itself is on,
    /// rather than the child the pages hang off. Showing the panel then switches on an
    /// object that was already on and never touches the pages' real parent, and hiding it
    /// would switch the manager's own GameObject off.
    /// </summary>
    private static void FixContainerOverride(QuestionPanelManager panel, System.Text.StringBuilder log)
    {
        GameObject page = panel.QPQuestion != null ? panel.QPQuestion : panel.QPTitle;

        if (page == null || page.transform.parent == null)
        {
            log.AppendLine("  note   no page assigned, so the container could not be checked.");
            return;
        }

        GameObject container = page.transform.parent.gameObject;

        if (panel.QP == container)
        {
            log.AppendLine($"  ok     QP -> '{container.name}'");
            return;
        }

        string was = panel.QP == null ? "empty" : $"'{panel.QP.name}'";

        var so = new SerializedObject(panel);
        so.FindProperty("QP").objectReferenceValue = container;
        so.ApplyModifiedProperties();

        log.AppendLine($"  FIXED  QP was {was} on the scene instance — the pages' real parent was never switched on, so the panel opened empty. Set to '{container.name}'.");
    }

    /// <summary>
    /// A legacy QuestionPanel component sits on the same canvas root, left over from the
    /// older text-based quiz. Nothing references it — the ScenarioController's Question
    /// Panels binding list is empty — but its Hide On Awake switches the WHOLE canvas root
    /// off during Awake, which is the object the manager and the VR anchor both live on.
    ///
    /// That defers their own Awake indefinitely, so the anchor may never be resolved and the
    /// panel opens at its authored world position instead of in front of the player. Now
    /// that the pages themselves start switched off, hiding the root buys nothing anyway.
    /// </summary>
    private static void DisarmLegacyHideOnAwake(QuestionPanelManager panel, System.Text.StringBuilder log)
    {
        var legacy = panel.GetComponent<QuestionPanel>();

        if (legacy == null)
            return;

        var so = new SerializedObject(legacy);
        SerializedProperty hideOnAwake = so.FindProperty("hideOnAwake");

        if (hideOnAwake == null || !hideOnAwake.boolValue)
        {
            log.AppendLine("  ok     the legacy QuestionPanel component is not hiding the canvas root");
            return;
        }

        hideOnAwake.boolValue = false;
        so.ApplyModifiedProperties();

        log.AppendLine("  FIXED  a legacy QuestionPanel component was switching the whole canvas root off during Awake — the object the manager and the VR anchor live on. Hide On Awake cleared; the pages start hidden on their own now.");
    }

    private static void ReportCanvas(QuestionPanelManager panel, System.Text.StringBuilder log)
    {
        Canvas canvas = panel.GetComponent<Canvas>();

        if (canvas == null)
        {
            log.AppendLine("PROBLEM  no Canvas on the same object as the QuestionPanelManager.");
            return;
        }

        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            log.AppendLine("  ok     canvas is already World Space, so it was being rendered all along — the problem is where, not whether.");
            return;
        }

        log.AppendLine($"PROBLEM  canvas render mode is {canvas.renderMode}. An XR camera does not draw a screen-space canvas at all. Set it to World Space.");
    }

    private static void ReportDistanceFromPlayer(QuestionPanelManager panel, System.Text.StringBuilder log)
    {
        // Either rig: the BNG army guy or the new hands. Measuring from whichever is here
        // beats reporting "no player rig" in half the project's scenes.
        Transform rig = Object.FindFirstObjectByType<BNG.BNGPlayerController>(FindObjectsInactive.Include)?.transform;

        if (rig == null)
            rig = Object.FindFirstObjectByType<PlayerRig>(FindObjectsInactive.Include)?.transform;

        if (rig == null)
        {
            log.AppendLine("  note   no player rig found in the scene, so the current distance could not be measured.");
            return;
        }

        Vector3 offset = panel.transform.position - rig.position;
        offset.y = 0f;

        log.AppendLine($"  note   the panel currently sits {offset.magnitude:0.0} m from the player rig, at {panel.transform.position}. Anything past about 3 m is why it reads as 'not showing'.");
    }
}
