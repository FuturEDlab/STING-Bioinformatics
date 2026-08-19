using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Converts the scene's Caption Canvas from the desktop screen-space overlay it was authored
/// as into a world-space panel that works in a headset, in one step.
///
/// A screen-space overlay canvas is not drawn by an XR camera at all — in the headset the
/// captions are simply absent — so this is not a polish pass, it is the difference between
/// captions existing and not. What it does beyond the render mode is the part that makes
/// them comfortable: a fixed physical size at a fixed reading distance, placement below the
/// eye line, a lazy follow instead of a face-lock, and materials that keep the panel legible
/// when the player walks into the bed it happens to be floating inside.
///
/// Safe to run more than once — every step checks for its own result first.
/// </summary>
public static class CaptionVRSetup
{
    // 1.5 m wide at 2 m away puts the text baked into the Figma caption blobs at roughly
    // 1.4 degrees of cap height, which is the comfortable floor for reading in a headset.
    private const float PanelWidthMetres = 1.5f;
    private const int CanvasWidthPx = 1500;

    // The blobs are 863x143, so the panel keeps that aspect with a little air.
    private const int CanvasHeightPx = 280;

    private const string OverlayShaderName = "STING/UI Always On Top";
    private const string TmpOverlayShaderName = "TextMeshPro/Mobile/Distance Field Overlay";
    private const string MaterialFolder = "Assets/Materials";
    private const string BackdropName = "Text backdrop";

    [MenuItem("Tools/STING/Set Up Caption Canvas for VR")]
    private static void Run()
    {
        CaptionDisplay display = Object.FindFirstObjectByType<CaptionDisplay>(FindObjectsInactive.Include);

        if (display == null)
        {
            EditorUtility.DisplayDialog("Caption VR setup",
                "No CaptionDisplay in the open scene. Open Hospital Room and try again.", "OK");
            return;
        }

        Canvas canvas = display.GetComponent<Canvas>();

        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Caption VR setup",
                $"'{display.name}' has a CaptionDisplay but no Canvas on the same object, so there is nothing to convert.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(display.gameObject, "Set Up Caption Canvas for VR");

        var log = new System.Text.StringBuilder();
        log.AppendLine($"=== CAPTION CANVAS -> VR ({display.name}) ===");

        ConvertCanvas(canvas, log);
        RectTransform panel = SizePanel(canvas, log);

        var so = new SerializedObject(display);
        ClearStrayRoot(display, so, log);

        var blob = so.FindProperty("blobImage").objectReferenceValue as Image;
        var text = so.FindProperty("captionText").objectReferenceValue as TMP_Text;

        Material uiOverlay = GetOrCreateOverlayMaterial(log);

        LayOutBlob(blob, uiOverlay, log);
        TMP_Text placedText = LayOutText(text, panel, log);
        BuildBackdrop(placedText, panel, so, uiOverlay, log);
        AddFollow(display.gameObject, log);

        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(display.gameObject.scene);
        Selection.activeGameObject = display.gameObject;

        log.AppendLine("Done. Play with the headset on, then tune Distance / Drop Angle on the Caption Panel Follow component to taste.");
        Debug.Log(log.ToString(), display);
    }

    private static void ConvertCanvas(Canvas canvas, System.Text.StringBuilder log)
    {
        Undo.RecordObject(canvas, "Caption canvas");
        canvas.renderMode = RenderMode.WorldSpace;

        // Left null, the canvas uses the current camera — which is what we want here, since
        // the XR rig's camera is not something this scene object can reference up front.
        canvas.worldCamera = null;
        canvas.sortingOrder = 100;
        EditorUtility.SetDirty(canvas);
        log.AppendLine("  ok     render mode -> World Space (an Overlay canvas is invisible to an XR camera)");

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();

        if (scaler != null)
        {
            Undo.RecordObject(scaler, "Caption canvas scaler");
            // Constant Pixel Size means nothing in world space. What does matter is how many
            // texels TMP is given per world unit, and the default of 1 leaves text mushy.
            scaler.dynamicPixelsPerUnit = 3f;
            scaler.referencePixelsPerUnit = 100f;
            EditorUtility.SetDirty(scaler);
            log.AppendLine("  ok     dynamic pixels per unit -> 3 (sharper text at reading distance)");
        }

        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();

        if (raycaster != null)
        {
            // Captions are never pointed at, and a raycaster parked in front of the player's
            // face would eat every ray aimed at anything behind it.
            Undo.DestroyObjectImmediate(raycaster);
            log.AppendLine("  ok     removed the Graphic Raycaster — it would swallow pointer rays aimed past the caption");
        }
    }

    private static RectTransform SizePanel(Canvas canvas, System.Text.StringBuilder log)
    {
        var rt = (RectTransform)canvas.transform;
        Undo.RecordObject(rt, "Caption panel size");

        rt.sizeDelta = new Vector2(CanvasWidthPx, CanvasHeightPx);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one * (PanelWidthMetres / CanvasWidthPx);

        EditorUtility.SetDirty(rt);
        log.AppendLine($"  ok     panel -> {PanelWidthMetres:0.00} m wide ({CanvasWidthPx}x{CanvasHeightPx} px, so 1 px = 1 mm)");

        return rt;
    }

    private static void ClearStrayRoot(CaptionDisplay display, SerializedObject so, System.Text.StringBuilder log)
    {
        SerializedProperty rootProp = so.FindProperty("root");
        var root = rootProp.objectReferenceValue as GameObject;

        if (root == null)
            return;

        if (root.transform.IsChildOf(display.transform) || display.transform.IsChildOf(root.transform))
            return;

        log.AppendLine($"  FIXED  Root pointed at '{root.name}', which has nothing to do with the captions — every line of dialogue was switching that object on and off. Cleared, so the caption canvas itself is used.");
        rootProp.objectReferenceValue = null;
    }

    private static void LayOutBlob(Image blob, Material overlay, System.Text.StringBuilder log)
    {
        if (blob == null)
        {
            log.AppendLine("  note   no Blob Image assigned on the CaptionDisplay — skipped.");
            return;
        }

        Undo.RecordObject(blob, "Caption blob");
        blob.preserveAspect = true;
        blob.raycastTarget = false;

        if (overlay != null)
            blob.material = overlay;

        Stretch((RectTransform)blob.transform);
        EditorUtility.SetDirty(blob);

        log.AppendLine("  ok     blob image fills the panel, keeps its aspect, draws over geometry");
    }

    private static TMP_Text LayOutText(TMP_Text text, RectTransform panel, System.Text.StringBuilder log)
    {
        if (text == null)
        {
            log.AppendLine("  note   no Caption Text assigned on the CaptionDisplay — skipped.");
            return null;
        }

        // The fallback text was parented under the blob image, and the blob is switched OFF
        // for exactly the phrases the fallback exists to cover — so it could never appear.
        if (text.transform.parent != panel)
        {
            Undo.SetTransformParent(text.transform, panel, "Reparent caption text");
            log.AppendLine("  FIXED  the fallback text was a child of the blob image, which is hidden whenever the fallback is needed — it could never show. Moved up to the panel.");
        }

        Undo.RecordObject(text, "Caption text");
        text.enableAutoSizing = true;
        text.fontSizeMin = 40f;
        text.fontSizeMax = 72f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        text.color = Color.white;
        text.margin = new Vector4(60f, 30f, 60f, 30f);

        Shader tmpOverlay = Shader.Find(TmpOverlayShaderName);

        if (tmpOverlay != null && text.fontSharedMaterial != null && text.fontSharedMaterial.shader != tmpOverlay)
        {
            Material overlayMaterial = GetOrCreateTmpOverlayMaterial(text.fontSharedMaterial, tmpOverlay, log);

            if (overlayMaterial != null)
                text.fontSharedMaterial = overlayMaterial;
        }

        Stretch((RectTransform)text.transform);
        EditorUtility.SetDirty(text);

        log.AppendLine("  ok     fallback text -> auto-sized 40-72, centred, drawn over geometry");
        return text;
    }

    private static void BuildBackdrop(TMP_Text text, RectTransform panel, SerializedObject so, Material overlay, System.Text.StringBuilder log)
    {
        SerializedProperty backdropProp = so.FindProperty("textBackdrop");

        if (backdropProp.objectReferenceValue != null)
        {
            log.AppendLine("  ok     text backdrop already assigned");
            return;
        }

        Transform existing = panel.Find(BackdropName);
        GameObject backdrop;

        if (existing != null)
        {
            backdrop = existing.gameObject;
        }
        else
        {
            backdrop = new GameObject(BackdropName, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(backdrop, "Create caption backdrop");
            backdrop.layer = panel.gameObject.layer;
            backdrop.transform.SetParent(panel, false);
        }

        // UI draws in sibling order, so index 0 puts the plate behind everything else.
        backdrop.transform.SetSiblingIndex(0);

        Image image = backdrop.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        image.type = Image.Type.Sliced;
        image.color = new Color(0.13f, 0.13f, 0.13f, 0.86f);
        image.raycastTarget = false;

        if (overlay != null)
            image.material = overlay;

        Stretch((RectTransform)backdrop.transform);

        backdropProp.objectReferenceValue = backdrop;

        // Off until a phrase without a blob actually needs it.
        backdrop.SetActive(false);

        if (text == null)
            log.AppendLine("  note   backdrop created, but there is no fallback text for it to sit behind.");
        else
            log.AppendLine("  ok     added a dark plate behind the fallback text (hidden for blob captions, which carry their own)");
    }

    private static void AddFollow(GameObject canvasObject, System.Text.StringBuilder log)
    {
        if (canvasObject.GetComponent<CaptionPanelFollow>() != null)
        {
            log.AppendLine("  ok     Caption Panel Follow already present");
            return;
        }

        Undo.AddComponent<CaptionPanelFollow>(canvasObject);
        log.AppendLine("  ok     added Caption Panel Follow — 2 m ahead, 14 degrees below the eye line, with a dead zone so it is not welded to the face");
    }

    private static Material GetOrCreateOverlayMaterial(System.Text.StringBuilder log)
    {
        const string path = MaterialFolder + "/UI Always On Top.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (existing != null)
            return existing;

        Shader shader = Shader.Find(OverlayShaderName);

        if (shader == null)
        {
            log.AppendLine($"  note   shader '{OverlayShaderName}' not found, so the panel will be clipped by anything it floats inside. Check that Assets/Shaders/UIAlwaysOnTop.shader imported.");
            return null;
        }

        EnsureMaterialFolder();
        var material = new Material(shader) { name = "UI Always On Top" };
        AssetDatabase.CreateAsset(material, path);
        log.AppendLine($"  ok     created {path}");

        return material;
    }

    private static Material GetOrCreateTmpOverlayMaterial(Material source, Shader overlayShader, System.Text.StringBuilder log)
    {
        string path = $"{MaterialFolder}/{source.name} Overlay.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (existing != null)
            return existing;

        EnsureMaterialFolder();

        // Copied from the font's own material so the atlas and face settings come with it;
        // only the shader changes, to TMP's stock always-on-top variant.
        var material = new Material(source) { name = $"{source.name} Overlay", shader = overlayShader };
        AssetDatabase.CreateAsset(material, path);
        log.AppendLine($"  ok     created {path}");

        return material;
    }

    private static void EnsureMaterialFolder()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets", "Materials");
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }
}
