using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum InteractInput
{
    [InspectorName("X or A")]
    X_AButton,

    // [InspectorName("Y Button / B Button")]
    [InspectorName("Y or B")]
    B_YButton,

    [InspectorName("LeftTrigger or RightTrigger")]
    Left_RightTrigger,
    
    [InspectorName("LeftGrip or RightGrip")]
    Left_RightGrip,
}

public class Interact : MonoBehaviour
{
    
    [Tooltip("Event triggered when this object is interacted with")]
    [SerializeField] private UnityEvent onInteract;
    [SerializeField] private float MaxGlowDistance = 2f;
    [SerializeField] private int submeshGlowNumber = -1;
    
    private float distance;
    private Renderer renderer;
    private Collider objectCollider;
    private List<Material> rendMaterials;
    private Material[] rendMaterialsArr;
    
    private InteractableGroup parentComponent;
    private int glowMaterialIndex = -1;
    private bool glowAdded;
    private Vector3 closestPoint;
    private Material preGlowMaterial;
    
    private bool isLeftHandNear;
    private bool isRightHandNear;
    public bool IsHandNear => isLeftHandNear || isRightHandNear;
    private const string interactStr = "Interact";
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        parentComponent = GetComponentInParent<InteractableGroup>();
        objectCollider = GetComponent<Collider>();
        renderer = GetComponent<Renderer>();

        if (submeshGlowNumber >= 0)
        {
            preGlowMaterial = renderer.materials[submeshGlowNumber];
        }

        if (!transform.CompareTag(interactStr))
        {
            transform.tag = interactStr;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Whichever rig the scene is running — the BNG army-guy rig or the new XRI hands —
        // Rig.Body is the capsule the player is standing in. Nothing here needs to know which.
        Transform player = Rig.Body;

        if (!player) return;

        // Calculates distance to nearest point on collider surface. -->
        // This helps prevent checking only the object's center
        closestPoint = objectCollider.ClosestPoint(player.position);
        distance = Vector3.Distance(closestPoint, player.position);

        if (distance <= MaxGlowDistance && !glowAdded)
        {
            glowAdded = AddGlow(submeshGlowNumber);
        }
        else if (distance > MaxGlowDistance && glowAdded)
        {
            glowAdded = RemoveGlow(submeshGlowNumber);
        }

        // At this point, we can assume that if player is outside glow radius,
        // the glow isn't present, leading to returning early since interaction
        // won't work regardless.
        if (!glowAdded) return;
        
        // if (Input.GetKeyDown(KeyCode.L)) // delete/uncomment when done testing in Unity Editor!
        if (IsInteractButtonPressed()) // Uncomment when done testing in Unity Editor!
        {
            onInteract?.Invoke();
        }

    }

    /// <summary>
    /// Fire this object's interaction without a controller button. The desktop mouse pointer
    /// calls it so a click does exactly what pressing the bound button would, glow gate and
    /// all — it does not reach past <c>onInteract</c> to do its own thing.
    /// </summary>
    public void TriggerInteract()
    {
        onInteract?.Invoke();
    }

    private bool IsInteractButtonPressed()
    {
        // Buttons come from Rig, which reads BNG's InputBridge on the army-guy rig and the
        // XR Input System on the new hands. Same call, same meaning, either scene.
        switch (parentComponent.InteractButton)
        {
            case InteractInput.X_AButton:
                return (Rig.XButtonDown && !Rig.AButton) ||
                       (Rig.AButtonDown && !Rig.XButton);

            case InteractInput.B_YButton:
                return (Rig.YButtonDown && !Rig.BButton) ||
                       (Rig.BButtonDown && !Rig.YButton);

            case InteractInput.Left_RightTrigger:
                return (Rig.LeftTriggerDown && Rig.RightTrigger < 0.1f) ||
                       (Rig.RightTriggerDown && Rig.LeftTrigger < 0.1f);

            case InteractInput.Left_RightGrip:
                return (Rig.LeftGripDown && Rig.RightGrip < 0.1f) ||
                       (Rig.RightGripDown && Rig.LeftGrip < 0.1f);

            default:
                return false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Tags LeftHand and RightHand are associated with the Grabber child objects
        // from LeftController object and RightController object (XR Rig Full Body,
        // the parent object, contains these 2 Controller objects)
        
        if (other.CompareTag("LeftHand"))
        {
            isLeftHandNear = true;
        }
        else if (other.CompareTag("RightHand"))
        {
            isRightHandNear = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Tags LeftHand and RightHand are associated with the Grabber child objects
        // from LeftController object and RightController object (XR Rig Full Body,
        // the parent object, contains these 2 controller objects)
        
        if (other.CompareTag("LeftHand"))
        {
            isLeftHandNear = false;
        }
        else if (other.CompareTag("RightHand"))
        {
            isRightHandNear = false;
        }
    }

    bool AddGlow(int glowMaterialNumber)
    {
        if (glowMaterialNumber < 0)
        {
            rendMaterials = new List<Material>(renderer.materials);
            rendMaterials.Add(parentComponent.GlowMaterial);
            glowMaterialIndex = rendMaterials.Count - 1;
            renderer.materials = rendMaterials.ToArray();
        }
        else
        {
            rendMaterialsArr = renderer.materials;
            rendMaterialsArr[glowMaterialNumber] = parentComponent.GlowMaterial;
            renderer.materials = rendMaterialsArr;
        }

        return true;
    }
    
    bool RemoveGlow(int glowMaterialNumber)
    {
        if (glowMaterialNumber < 0)
        {
            rendMaterials = new List<Material>(renderer.materials);
            rendMaterials.RemoveAt(glowMaterialIndex);
            renderer.materials = rendMaterials.ToArray();
        }
        else
        {
            rendMaterialsArr = renderer.materials;
            // We can safely assume preGlowMaterial won't be null because this will always get assigned at start time
            // anytime the glowMaterialNumber is >= 0.
            rendMaterialsArr[glowMaterialNumber] = preGlowMaterial;
            renderer.materials = rendMaterialsArr;
        }

        return false;
    }
    
}
