using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The visual half of "wait for it to glow": pulses an object on demand. Purely cosmetic
/// and driven by <see cref="ScenarioTarget"/>, which decides *when* to glow. Unlike the
/// proximity glow in <see cref="Interact"/>, nothing here reacts to the player's distance.
///
/// URP note: emission only renders when the material has the <c>_EMISSION</c> keyword on,
/// and a MaterialPropertyBlock cannot switch a keyword. So while glowing we swap in a
/// per-renderer material instance with emission force-enabled, and put the original
/// materials back afterwards. The base colour is tinted at the same time, so the highlight
/// is still obvious on shaders that ignore emission entirely.
/// </summary>
public class ScenarioHighlight : MonoBehaviour
{
    [Tooltip("Yellow for a normal prompt, red for the contraindication override — matches the script's colour cues.")]
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.2f);

    [Tooltip("Peak emission strength. Push to 3-4 if the room lighting washes it out.")]
    [SerializeField] private float intensity = 2f;

    [Tooltip("Pulses per second.")]
    [SerializeField] private float pulsesPerSecond = 1.2f;

    [Tooltip("How strongly the base colour is tinted toward the glow colour (0 = emission only). Keeps the highlight visible on unlit/mobile shaders.")]
    [Range(0f, 1f)]
    [SerializeField] private float baseColorTint = 0.35f;

    [Tooltip("Renderers to affect. Left empty, every renderer on this object and its children is used.")]
    [SerializeField] private Renderer[] targets;

    [Tooltip("Objects switched on only while glowing — an outline mesh, a halo, a floating arrow. Works even if the shader supports neither emission nor tinting.")]
    [SerializeField] private GameObject[] showWhileGlowing;

    [Tooltip("Log what this highlight does when it is switched on. Use it when a glow is not showing up.")]
    [SerializeField] private bool debugLogging;

    // URP Lit uses _BaseColor; the built-in/legacy shaders use _Color. Set whichever exists.
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColor = Shader.PropertyToID("_Color");

    private readonly List<Material[]> originalMaterials = new List<Material[]>();
    private readonly List<Material[]> glowMaterials = new List<Material[]>();
    private readonly List<Color[]> originalBaseColors = new List<Color[]>();

    private bool prepared;
    private bool glowing;
    private float phase;

    public bool IsGlowing => glowing;

    private void Awake()
    {
        if (targets == null || targets.Length == 0)
            targets = GetComponentsInChildren<Renderer>(true);

        if (targets.Length == 0 && (showWhileGlowing == null || showWhileGlowing.Length == 0))
            Debug.LogWarning($"[ScenarioHighlight] '{name}' has no renderers and no 'Show While Glowing' objects — it can never show a highlight.", this);

        SetExtras(false);
    }

    private void OnDisable()
    {
        if (glowing)
            SetGlowing(false);
    }

    private void OnDestroy()
    {
        // The instances are ours, so clean them up rather than leaking one per prop.
        for (int i = 0; i < glowMaterials.Count; i++)
        {
            Material[] mats = glowMaterials[i];
            if (mats == null) continue;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] != null)
                    Destroy(mats[m]);
            }
        }
    }

    public void SetGlowing(bool on)
    {
        if (on == glowing)
            return;

        glowing = on;
        phase = 0f;
        SetExtras(on);

        if (on)
        {
            Prepare();
            ApplyMaterials(glowMaterials);
            if (debugLogging)
                Debug.Log($"[ScenarioHighlight] '{name}' ON across {targets.Length} renderer(s).", this);
        }
        else
        {
            ApplyMaterials(originalMaterials);
        }
    }

    /// <summary>
    /// Build the glow copies once, on first use, so the cost is not paid by every prop in
    /// the room at load time.
    /// </summary>
    private void Prepare()
    {
        if (prepared)
            return;
        prepared = true;

        for (int i = 0; i < targets.Length; i++)
        {
            Renderer r = targets[i];
            if (r == null)
            {
                originalMaterials.Add(null);
                glowMaterials.Add(null);
                originalBaseColors.Add(null);
                continue;
            }

            Material[] shared = r.sharedMaterials;
            Material[] copies = new Material[shared.Length];
            Color[] baseColors = new Color[shared.Length];

            for (int m = 0; m < shared.Length; m++)
            {
                if (shared[m] == null)
                    continue;

                Material copy = new Material(shared[m]);
                // The keyword is the part a property block cannot do.
                copy.EnableKeyword("_EMISSION");
                copy.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                copies[m] = copy;

                baseColors[m] = copy.HasProperty(BaseColor) ? copy.GetColor(BaseColor)
                              : copy.HasProperty(LegacyColor) ? copy.GetColor(LegacyColor)
                              : Color.white;
            }

            originalMaterials.Add(shared);
            glowMaterials.Add(copies);
            originalBaseColors.Add(baseColors);
        }
    }

    private void ApplyMaterials(List<Material[]> set)
    {
        for (int i = 0; i < targets.Length && i < set.Count; i++)
        {
            Renderer r = targets[i];
            if (r == null || set[i] == null)
                continue;

            // sharedMaterials, not materials — assigning to .materials would instance again.
            r.sharedMaterials = set[i];
        }
    }

    private void Update()
    {
        if (!glowing)
            return;

        phase += Time.deltaTime * pulsesPerSecond * Mathf.PI * 2f;
        // Sine mapped to 0..1 so the object breathes rather than strobing.
        float t = (Mathf.Sin(phase) + 1f) * 0.5f;

        Color emission = glowColor * (t * intensity);

        for (int i = 0; i < glowMaterials.Count; i++)
        {
            Material[] mats = glowMaterials[i];
            Color[] baseColors = originalBaseColors[i];
            if (mats == null)
                continue;

            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat == null)
                    continue;

                if (mat.HasProperty(EmissionColor))
                    mat.SetColor(EmissionColor, emission);

                if (baseColorTint > 0f && baseColors != null)
                {
                    Color tinted = Color.Lerp(baseColors[m], glowColor, t * baseColorTint);
                    if (mat.HasProperty(BaseColor))
                        mat.SetColor(BaseColor, tinted);
                    else if (mat.HasProperty(LegacyColor))
                        mat.SetColor(LegacyColor, tinted);
                }
            }
        }
    }

    private void SetExtras(bool on)
    {
        if (showWhileGlowing == null)
            return;

        for (int i = 0; i < showWhileGlowing.Length; i++)
        {
            if (showWhileGlowing[i] != null)
                showWhileGlowing[i].SetActive(on);
        }
    }

#if UNITY_EDITOR
    /// <summary>Turn the glow on in play mode to check it is visible against the room lighting.</summary>
    [ContextMenu("Test Glow (play mode)")]
    private void TestGlow()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ScenarioHighlight] Enter play mode first — the glow swaps material instances.", this);
            return;
        }

        if (targets == null || targets.Length == 0)
            targets = GetComponentsInChildren<Renderer>(true);

        SetGlowing(!glowing);
    }
#endif
}
