using UnityEngine;
using BNG;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[ExecuteAlways]
public class PickUpGroup : MonoBehaviour
{
    [SerializeField] private GameObject ground;
    [SerializeField] private TableGroup tables;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private Collider[] objectsToCollideWith;
    [SerializeField] private List<Collider> ignoreObjectsTemp;
    // [SerializeField] private Collider[] objectsToCollideWith;
    
    
    
    /// <summary>
    /// The player's capsule, which every prop is told to ignore so a held bottle cannot shove
    /// the player across the room. Falls back to whichever rig is in the scene when the slot
    /// is empty, so the same prefab works in the BNG scene and in the new-hands scene.
    /// </summary>
    public Collider PlayerCollider => playerCollider != null ? playerCollider : Rig.BodyCollider;
    // public Collider LeftGrabberCollider => leftGrabberCollider;
    // public Collider RightGrabberCollider => rightGrabberCollider;
    public GameObject Ground => ground;
    // public Collider ObjectsToCollideWith => objectsToCollideWith;
    public List<Collider> IgnoreObjectsTemp => ignoreObjectsTemp;
    public TableGroup TablesGroup => tables;

    void Start()
    {
        if (!Application.isPlaying) return;
        
        if (ignoreObjectsTemp == null || ignoreObjectsTemp.Count == 0) return;
        foreach (Collider c in ignoreObjectsTemp)
        {
            c.gameObject.layer = LayerMask.NameToLayer("Collidable");
        }
    }

    /// <summary>
    /// Stamp a child with everything it needs to be picked up. Which grab component that
    /// means depends on the rig the open scene is built around: the new XRI hands answer
    /// <see cref="XRGrabInteractable"/>, the BNG army-guy rig answers <c>Grabbable</c>, and
    /// nothing understands both. Deciding it here means dragging a prop into either scene
    /// gives it the right one without anybody having to remember which scene they are in.
    /// </summary>
    public void AddDefault_PickUpComponents(Transform Child)
    {
        // This runs at edit time as well as at runtime, and Rig's cached lookup is built for
        // a running frame loop. Outside play mode, just look — it happens once per prop drag,
        // not once per frame.
        bool newHands = Application.isPlaying
            ? Rig.UsingXRHands
            : FindAnyObjectByType<PlayerRig>(FindObjectsInactive.Include) != null;

        // The Rigidbody goes on FIRST. XRGrabInteractable carries [RequireComponent(Rigidbody)],
        // so adding the grab component first makes Unity add a default Rigidbody itself — and
        // then the block below finds one already there and never applies the settings a prop in
        // this project actually needs.
        if (Child.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rigidObject = Child.gameObject.AddComponent<Rigidbody>();
            rigidObject.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidObject.angularDamping = 0.5f;
        }

        if (newHands)
            AddXRIGrab(Child);
        else
            AddBngGrab(Child);

        if (Child.GetComponent<GrabStability>() == null)
        {
            Child.gameObject.AddComponent<GrabStability>();
        }
    }

    private static void AddXRIGrab(Transform Child)
    {
        if (Child.GetComponent<XRGrabInteractable>() != null)
            return;

        XRGrabInteractable grab = Child.gameObject.AddComponent<XRGrabInteractable>();

        // These three are the XRI equivalents of what the BNG version below sets:
        // PhysicsJoint -> VelocityTracking, RemoteGrabbable off -> no ray grabbing (the
        // hands' direct interactors are the only thing that will select it), and
        // SwapHands -> Single, so the second hand takes it rather than tearing it in two.
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grab.selectMode = InteractableSelectMode.Single;
        grab.useDynamicAttach = true;

        // Nothing in this simulation is meant to be thrown, and tracked hands report a
        // speed spike on the frame a grab ends — which is exactly what sends a gently
        // released bottle across the room. GrabStability tames the rest.
        grab.throwOnDetach = false;
    }

    private static void AddBngGrab(Transform Child)
    {
        if (Child.GetComponent<Grabbable>() == null)
        {
            Grabbable grabbableObject = Child.gameObject.AddComponent<Grabbable>();
            grabbableObject.GrabPhysics = GrabPhysics.PhysicsJoint;
            grabbableObject.RemoteGrabbable = false;
            grabbableObject.SecondaryGrabBehavior = OtherGrabBehavior.SwapHands;
        }

        if (Child.GetComponent<GrabbableRingHelper>() == null)
        {
            GrabbableRingHelper ringHelper = Child.gameObject.AddComponent<GrabbableRingHelper>();
            ringHelper.RingHelperScale = 0.8f;
        }
    }
    
#if UNITY_EDITOR
    private void OnTransformChildrenChanged()
    {
        if (Application.isPlaying) return;
        
        // Iterate through all child transforms of this GameObject
        foreach (Transform child in transform)
        {
            if (child.GetComponent<InteractableGroup>() != null)
            {
                continue;
            }
            
            if (child.TryGetComponent(out Interact interactComp))
            {
                DestroyImmediate(interactComp);
            }
            
            AddDefault_PickUpComponents(child);
            
        }
    }
#endif

}
