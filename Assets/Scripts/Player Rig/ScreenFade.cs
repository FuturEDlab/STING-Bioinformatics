using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fades the player's view to black and back. Replaces BNG's <c>ScreenFader</c>, which came
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

    private Image image;
    private Canvas canvas;
    private float alpha;
    private float target;

    /// <summary>Alpha per second the fade to black runs at. Teleport steps time their wait off this.</summary>
    public float FadeInSpeed => Mathf.Max(0.01f, fadeInSpeed);

    /// <summary>Alpha per second the fade back runs at.</summary>
    public float FadeOutSpeed => Mathf.Max(0.01f, fadeOutSpeed);

    /// <summary>How black the view is right now, 0-1.</summary>
    public float CurrentAlpha => alpha;

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
}
