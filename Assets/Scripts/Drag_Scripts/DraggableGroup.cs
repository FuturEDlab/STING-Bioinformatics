using BNG;
using UnityEngine;

public class DraggableGroup : MonoBehaviour
{
    [SerializeField] private GameObject ground;

    [Header("Player rig — leave empty to take these from whichever rig is in the scene")]
    [Tooltip("The player's capsule, so dragged objects can be told to ignore it. Empty falls back to Rig.BodyCollider.")]
    [SerializeField] private Collider playerCollider;

    [SerializeField] private Rigidbody playerRb;

    [Tooltip("BNG-only. On the new XRI hands this stays empty and LeftHandTransform reads the rig instead.")]
    [SerializeField] private HandController leftHand;

    [Tooltip("BNG-only. On the new XRI hands this stays empty and RightHandTransform reads the rig instead.")]
    [SerializeField] private HandController rightHand;

    [SerializeField] private Collider[] objectsToCollideWith;

    public Collider PlayerCollider => playerCollider != null ? playerCollider : Rig.BodyCollider;
    public Rigidbody PlayerRb => playerRb;
    public GameObject Ground => ground;

    public HandController LeftHand => leftHand;
    public HandController RightHand => rightHand;

    /// <summary>
    /// Where the left hand is, whichever rig is running. The serialized BNG HandController
    /// wins when it is set, so the Hospital Room scene keeps pointing at exactly the object
    /// it always did; the new hands leave it empty and get the rig's hand instead.
    /// </summary>
    public Transform LeftHandTransform => leftHand != null ? leftHand.transform : Rig.LeftHand;

    /// <summary>Where the right hand is. See <see cref="LeftHandTransform"/>.</summary>
    public Transform RightHandTransform => rightHand != null ? rightHand.transform : Rig.RightHand;
}
