using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Fades the player's view to black and back, and can hold a line of text on the black —
/// the "30 minutes later" title card. Replaces BNG's <c>ScreenFader</c>, which came
/// attached to the old rig's CenterEyeAnchor.
///
/// The black is a world-space canvas parented to the head rather than a fullscreen shader
/// pass: an overlay canvas is not drawn to a headset at all, and a custom shader would have
/// to be kept out of URP's stripping list. A UI Image uses the always-included UI/Default
/// shader, so this works in the Editor, on desktop and on device with no render-pipeline
/// setup.
///
/// Put it on the camera — or leave it off entirely and <see cref="PlayerRig.Fade"/> will add
/// one the first time a teleport asks for a fade.
/// </summary>
[DisallowMultipleComponent]
public class ScreenFade : MonoBehaviour
{
    [Tooltip("How fast the view goes black, in alpha per second. 2 means half a second to full black.")]
    [SerializeField] private float fadeInSpeed = 2f;

    [Tooltip("How fast the view comes back, in alpha per second.")]
    [SerializeField] private float fadeOutSpeed = 2f;

    [Tooltip("The colour faded to. Black unless you want a white-out.")]
    [SerializeField] private Color fadeColor = Color.black;

    [Tooltip("Start the scene already black, so the first fade out reveals it. Off means the scene starts visible.")]
    [SerializeField] private bool startFaded;

    [Header("Title card")]
    [Tooltip("Colour of the text shown on the black by ShowMessage.")]
    [SerializeField] private Color messageColor = Color.white;

    [Tooltip("How far in front of the player the title card sits, in metres. The black itself has to hug the camera so nothing in the room can poke through it, but TEXT that close cannot be fused by two eyes — each eye sees it somewhere completely different and it reads as a doubled, smeared image. A metre out is comfortable to read.")]
    [Min(0.3f)]
    [SerializeField] private float messageDistance = 1f;

    [Tooltip("Cap height of the title card in metres, measured at Message Distance. 0.12 is about 7 degrees of view — a comfortable title.")]
    [Min(0.01f)]
    [SerializeField] private float messageHeight = 0.12f;

    private Image image;
    private Canvas canvas;
    private TMP_Text message;
    private bool messageShowing;
    private float alpha;
    private float target;

    /// <summary>Alpha per second the fade to black runs at. Teleport steps time their wait off this.</summary>
    public float FadeInSpeed => Mathf.Max(0.01f, fadeInSpeed);

    /// <summary>Alpha per second the fade back runs at.</summary>
    public float FadeOutSpeed => Mathf.Max(0.01f, fadeOutSpeed);

    /// <summary>How black the view is right now, 0-1.</summary>
    public float CurrentAlpha => alpha;

    /// <summary>True while a title card is being held on the black.</summary>
    public bool MessageShowing => messageShowing;

    private void Awake()
    {
        Build();
        alpha = startFaded ? 1f : 0f;
        target = alpha;
        Apply();
    }

    /// <summary>Fade the view to black.</summary>
    public void DoFadeIn() => target = 1f;

    /// <summary>Fade the view back to the scene.</summary>
    public void DoFadeOut() => target = 0f;

    /// <summary>Go straight to black or straight to clear with no ramp.</summary>
    public void SetFaded(bool faded)
    {
        target = faded ? 1f : 0f;
        alpha = target;
        Apply();
    }

    /// <summary>
    /// Hold a line of text on the black. Its opacity follows the fade's, so calling this
    /// before or during a fade to black lets the text come up with it rather than appearing
    /// over a scene the player can still see.
    /// </summary>
    public void ShowMessage(string text)
    {
        BuildMessage();
        if (message == null)
            return;

        message.text = text;
        messageShowing = !string.IsNullOrEmpty(text);
        message.gameObject.SetActive(messageShowing);
        Apply();
    }

    /// <summary>Drop the title card. The black itself is unaffected.</summary>
    public void HideMessage()
    {
        messageShowing = false;
        if (message != null)
            message.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Mathf.Approximately(alpha, target)) return;

        float speed = target > alpha ? FadeInSpeed : FadeOutSpeed;
        alpha = Mathf.MoveTowards(alpha, target, speed * Time.unscaledDeltaTime);
        Apply();
    }

    private void Apply()
    {
        if (image == null) return;

        Color c = fadeColor;
        c.a = alpha;
        image.color = c;

        if (message != null)
        {
            Color t = messageColor;
            // Ramped against the last third of the fade rather than tied to it one for one:
            // the card is then already gone by the time the room starts showing through, so
            // it never reads as ghost text floating over the scene on the way back.
            t.a = messageShowing
                ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, alpha))
                : 0f;
            message.color = t;
        }

        // A fully clear canvas still costs a draw call every frame, and leaving it enabled
        // means it can catch a stray raycast.
        if (canvas != null)
            canvas.enabled = alpha > 0.001f;
    }

    private void Build()
    {
        if (image != null) return;

        var go = new GameObject("Screen Fade");
        go.transform.SetParent(transform, false);

        // Sit just past the near clip plane so nothing in the scene can poke through, and so
        // a camera with an unusual near plane still shows the fade.
        Camera cam = GetComponent<Camera>() ?? GetComponentInParent<Camera>();
        float distance = cam != null ? Mathf.Max(0.05f, cam.nearClipPlane * 2f) : 0.1f;
        go.transform.localPosition = new Vector3(0f, 0f, distance);

        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = short.MaxValue;
        if (cam != null)
            canvas.worldCamera = cam;

        // Wide enough to cover any headset's field of view at that distance.
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(distance * 40f, distance * 40f);
        rect.localScale = Vector3.one;

        image = go.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
    }

    /// <summary>
    /// The title card, built on first use. It is a child of the fade's own canvas and is
    /// added after the Image, so it is drawn on top of the black by hierarchy order without
    /// any sorting guesswork.
    ///
    /// It is NOT on the black plane, though. That plane sits just past the near clip so the
    /// room cannot poke through it, and a headset cannot converge two eyes on text a few
    /// centimetres from the face — it comes out doubled and smeared. So the card is pushed
    /// forward off the plane to <see cref="messageDistance"/>, where two eyes read it as one
    /// image, while still riding the same canvas.
    ///
    /// The canvas is authored in metres (its rect is 40x the plane distance, to cover any
    /// FOV), so the text sits on a scaled-down RectTransform: one rect unit is
    /// messageHeight/100 metres, which makes a font size of 100 exactly messageHeight tall.
    /// </summary>
    private void BuildMessage()
    {
        if (message != null || image == null)
            return;

        const float fontUnits = 100f;

        float planeDistance = image.transform.localPosition.z;
        float unitsToMetres = Mathf.Max(0.01f, messageHeight) / fontUnits;

        // A camera with an unusually far near plane could push the black out past the card,
        // which would bury it. The card always sits in front of the black.
        float cardDistance = Mathf.Max(messageDistance, planeDistance + 0.05f);

        var go = new GameObject("Fade Message");
        go.transform.SetParent(image.transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);

        // A block 1.2x the viewing distance wide — about 62 degrees — so a title never
        // wraps, and three lines tall so a longer one has somewhere to go.
        rect.sizeDelta = new Vector2(
            cardDistance * 1.2f / unitsToMetres,
            messageHeight * 3f / unitsToMetres);
        rect.localScale = Vector3.one * unitsToMetres;
        rect.anchoredPosition3D = new Vector3(0f, 0f, cardDistance - planeDistance);

        message = go.AddComponent<TextMeshProUGUI>();
        message.alignment = TextAlignmentOptions.Center;
        message.raycastTarget = false;
        message.fontSize = fontUnits;
        message.color = messageColor;

        // Out at a readable distance the card is far enough into the room to be depth-tested
        // against it, and a bed or a trolley between the player and it would cut a hole in
        // the text. Its own material overrides the ZTest the canvas system hands to UI, so
        // the card stays whole whatever the player happens to be stood in front of.
        // Guarded on the shared material: asking for fontMaterial before TMP has a font
        // asset to instance from is a null reference, and a project with no TMP Settings
        // asset is exactly the case where that happens.
        if (message.fontSharedMaterial != null)
        {
            Material mat = message.fontMaterial;
            if (mat != null)
                mat.SetFloat("unity_GUIZTestMode", (float)CompareFunction.Always);
        }

        go.SetActive(false);
    }
}
