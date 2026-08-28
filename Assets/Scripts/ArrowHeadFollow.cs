using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps a world-space arrow in the player's view without making it part of the caption panel.
/// The arrow remains an independent object, so it can be shown while captions are hidden.
/// </summary>
[DisallowMultipleComponent]
public class ArrowHeadFollow : MonoBehaviour
{
    private const string AlwaysOnTopShaderName = "STING/3D Always On Top";

    [Header("Who to follow")]
    [Tooltip("The player's head. Left empty, the main camera is used — that is the VR rig's Main Camera at runtime.")]
    [SerializeField] private Transform head;

    [Header("Placement")]
    [Tooltip("Metres in front of the eyes.")]
    [Range(0.75f, 5f)]
    [SerializeField] private float distance = 2f;

    [Tooltip("Degrees below the eye line. Positive values place the arrow lower in the player's view.")]
    [Range(0f, 40f)]
    [SerializeField] private float dropAngle = 14f;

    [Header("Follow")]
    [Tooltip("How far the head can turn left/right before the arrow starts coming with it.")]
    [Range(0f, 45f)]
    [SerializeField] private float yawDeadzone = 12f;

    [Tooltip("How far the head can tilt up/down before the arrow starts coming with it.")]
    [Range(0f, 45f)]
    [SerializeField] private float pitchDeadzone = 10f;

    [Tooltip("Roughly how long the arrow takes to catch up once it is being dragged.")]
    [Range(0.02f, 1.5f)]
    [SerializeField] private float followSmoothTime = 0.3f;

    [Tooltip("How far the arrow may end up above/below horizontal.")]
    [SerializeField] private Vector2 pitchLimits = new Vector2(-20f, 30f);

    [Tooltip("Jump straight to centre whenever the arrow is switched on.")]
    [SerializeField] private bool centreOnShow = true;

    private float yaw;
    private float pitch;
    private float yawVelocity;
    private float pitchVelocity;
    private bool headMissingReported;
    private Quaternion initialLocalRotation;
    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<Material[]> originalMaterials = new List<Material[]>();
    private readonly List<Material[]> overlayMaterials = new List<Material[]>();

    private void Awake()
    {
        initialLocalRotation = transform.localRotation;
        PrepareAlwaysOnTopMaterials();
    }

    public void Recentre()
    {
        if (!ResolveHead())
            return;

        ReadHeadAngles(out yaw, out pitch);
        yawVelocity = 0f;
        pitchVelocity = 0f;
        Place();
    }

    private void OnEnable()
    {
        if (centreOnShow)
            Recentre();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null && i < originalMaterials.Count)
                renderers[i].sharedMaterials = originalMaterials[i];
        }

        for (int i = 0; i < overlayMaterials.Count; i++)
        {
            Material[] materials = overlayMaterials[i];
            for (int m = 0; m < materials.Length; m++)
            {
                if (materials[m] != null)
                    Destroy(materials[m]);
            }
        }
    }

    private void PrepareAlwaysOnTopMaterials()
    {
        Shader overlayShader = Shader.Find(AlwaysOnTopShaderName);
        if (overlayShader == null)
        {
            Debug.LogWarning($"[ArrowHeadFollow] Could not find '{AlwaysOnTopShaderName}', so the arrow may be occluded by scene geometry.", this);
            return;
        }

        Renderer[] foundRenderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < foundRenderers.Length; i++)
        {
            Renderer target = foundRenderers[i];
            Material[] originals = target.sharedMaterials;
            Material[] overlays = new Material[originals.Length];

            for (int m = 0; m < originals.Length; m++)
            {
                if (originals[m] == null)
                    continue;

                Material overlay = new Material(overlayShader)
                {
                    name = originals[m].name + " (Arrow Always On Top)"
                };

                if (originals[m].HasProperty("_BaseMap"))
                    overlay.SetTexture("_BaseMap", originals[m].GetTexture("_BaseMap"));
                else if (originals[m].HasProperty("_MainTex"))
                    overlay.SetTexture("_BaseMap", originals[m].GetTexture("_MainTex"));

                if (originals[m].HasProperty("_BaseColor"))
                    overlay.SetColor("_BaseColor", originals[m].GetColor("_BaseColor"));
                else if (originals[m].HasProperty("_Color"))
                    overlay.SetColor("_BaseColor", originals[m].GetColor("_Color"));

                overlays[m] = overlay;
            }

            renderers.Add(target);
            originalMaterials.Add(originals);
            overlayMaterials.Add(overlays);
            target.sharedMaterials = overlays;
        }
    }

    private void LateUpdate()
    {
        if (!ResolveHead())
            return;

        ReadHeadAngles(out float headYaw, out float headPitch);

        float targetYaw = DragTowards(yaw, headYaw, yawDeadzone, true);
        float targetPitch = DragTowards(pitch, headPitch, pitchDeadzone, false);

        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, followSmoothTime);
        pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, followSmoothTime);

        Place();
    }

    private void Place()
    {
        float placedPitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y) + dropAngle;
        Quaternion facing = Quaternion.Euler(placedPitch, yaw, 0f);

        transform.position = head.position + facing * (Vector3.forward * distance);

        if (transform.parent == null)
            transform.rotation = facing * initialLocalRotation;
        else
            transform.localRotation = Quaternion.Inverse(transform.parent.rotation) * facing * initialLocalRotation;
    }

    private void ReadHeadAngles(out float headYaw, out float headPitch)
    {
        Vector3 forward = head.forward;

        headYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        headPitch = -Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
    }

    private static float DragTowards(float current, float target, float deadzone, bool wrapsAround)
    {
        float delta = wrapsAround ? Mathf.DeltaAngle(current, target) : target - current;

        if (Mathf.Abs(delta) <= deadzone)
            return current;

        return current + delta - Mathf.Sign(delta) * deadzone;
    }

    private bool ResolveHead()
    {
        if (head != null)
            return true;

        head = Rig.Head;
        if (head == null)
        {
            Camera main = Camera.main;
            if (main != null)
                head = main.transform;
        }

        if (head != null)
            return true;

        if (!headMissingReported)
        {
            headMissingReported = true;
            Debug.LogWarning($"[ArrowHeadFollow] '{name}' could not find the player's head or Main Camera.", this);
        }

        return false;
    }
}
