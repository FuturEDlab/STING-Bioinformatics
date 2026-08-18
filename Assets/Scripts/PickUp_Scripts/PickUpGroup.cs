using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;

[ExecuteAlways]
public class PickUpGroup : MonoBehaviour
{
    [SerializeField] private GameObject ground;
    [SerializeField] private TableGroup tables;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private Collider[] objectsToCollideWith;
    [SerializeField] private List<Collider> ignoreObjectsTemp;
    // [SerializeField] private Collider[] objectsToCollideWith;
    
    
    
    public Collider PlayerCollider => playerCollider;
    // public Collider LeftGrabberCollider => leftGrabberCollider;
    // public Collider RightGrabberCollider => rightGrabberCollider;
    public GameObject Ground => ground;
    // public Collider ObjectsToCollideWith => objectsToCollideWith;
    public List<Collider> IgnoreObjectsTemp => ignoreObjectsTemp;
    public TableGroup TablesGroup => tables;
    
    private void Awake()
    {
        if (!Application.isPlaying) return;

        // GrabStability tells every prop to ignore this collider, so an empty slot means
        // held objects shove the player around. Take it from the rig when it is not set.
        if (playerCollider == null && PlayerRig.Instance != null)
            playerCollider = PlayerRig.Instance.BodyCollider;
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        
        if (ignoreObjectsTemp == null || ignoreObjectsTemp.Count == 0) return;
        foreach (Collider c in ignoreObjectsTemp)
        {
            c.gameObject.layer = LayerMask.NameToLayer("Collidable");
        }
    }

    public void AddDefault_PickUpComponents(Transform Child)
    {
        Rigidbody rigidObject;
        XRGrabInteractable grabbableObject;

        // The Rigidbody goes on first: XRGrabInteractable requires one, so adding it the
        // other way round lets Unity create a default body and the settings below never
        // reach it.
        if (Child.GetComponent<Rigidbody>() == null)
        {
            rigidObject = Child.gameObject.AddComponent<Rigidbody>();
            rigidObject.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidObject.angularDamping = 0.5f;
        }

        if (Child.GetComponent<XRGrabInteractable>() == null)
        {
            grabbableObject = Child.gameObject.AddComponent<XRGrabInteractable>();

            // Velocity tracking is the closest match to the physics-joint grab these props
            // were tuned against: a held bottle still collides with the cart instead of
            // passing through it.
            grabbableObject.movementType = XRBaseInteractable.MovementType.VelocityTracking;

            // Single = the second hand takes the object off the first, which is what the
            // old SwapHands behaviour did.
            grabbableObject.selectMode = InteractableSelectMode.Single;

            // Grab the object where the hand actually touched it rather than snapping it
            // to a fixed attach point, so a bottle picked up by the neck stays that way.
            grabbableObject.useDynamicAttach = true;
        }
        
        if (Child.GetComponent<GrabStability>() == null)
        {
            Child.gameObject.AddComponent<GrabStability>();
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
