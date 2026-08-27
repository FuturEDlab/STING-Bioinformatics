using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Puts a prop back where it started once it has been left somewhere else for a while.
///
/// The room only works if the bottles and the scanner are on their shelf: a bottle rolled
/// under the bed, kicked behind the cart, or dropped through the floor is a dead end for the
/// scenario, and in a headset the player often cannot even see where it went. So each prop
/// remembers the pose it was authored at, and returns to it after
/// <see cref="respawnDelaySeconds"/> of sitting away from that spot untouched.
///
/// Holding it stops the clock and resets it, so a prop being carried around — or put down,
/// picked up and put down again — is never yanked out of the player's hands or out from
/// under a task they are in the middle of.
///
/// Not to be confused with the older <see cref="RespawnObject"/>, which only catches props
/// that fall out of the world and is still used by the Hospital Room scene.
/// </summary>
[DisallowMultipleComponent]
public class RespawnToOrigin : MonoBehaviour
{
    [Tooltip("Seconds the prop has to sit away from its starting spot, untouched, before it is put back.")]
    [Min(0f)]
    [SerializeField] private float respawnDelaySeconds = 30f;

    [Tooltip("How far it may drift and still count as 'in place', in metres. Anything inside this never starts the clock.")]
    [Min(0.001f)]
    [SerializeField] private float positionTolerance = 0.05f;

    [Tooltip("Where it goes back to. Left empty, the pose it starts the scene at is used — which is what you want for a prop placed by hand in the scene.")]
    [SerializeField] private Transform homeOverride;

    [Tooltip("Log when the clock starts, resets, and when the prop is put back.")]
    [SerializeField] private bool debugLogging;

    private GrabHandle grab;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private Rigidbody body;
    private float awaySeconds;

    /// <summary>Seconds the prop has been sitting away from home untouched.</summary>
    public float AwaySeconds => awaySeconds;

    /// <summary>Where this prop returns to.</summary>
    public Vector3 Home => homePosition;

    /// <summary>True while this prop is held by a hand or desktop pointer.</summary>
    public bool IsHeld => grab != null && grab.IsHeld;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        grab = new GrabHandle(this);

        if (homeOverride != null)
            homeOverride.GetPositionAndRotation(out homePosition, out homeRotation);
        else
            transform.GetPositionAndRotation(out homePosition, out homeRotation);
    }

    private void Update()
    {
        // Being held is the one thing that always resets the clock — including the case where
        // the player picks the prop up on second 29 and puts it straight back down.
        if (grab != null && grab.IsHeld)
        {
            ResetClock("held");
            return;
        }

        if ((transform.position - homePosition).sqrMagnitude <= positionTolerance * positionTolerance)
        {
            ResetClock("back in place");
            return;
        }

        if (respawnDelaySeconds <= 0f)
        {
            Respawn();
            return;
        }

        bool startingNow = awaySeconds <= 0f;
        awaySeconds += Time.deltaTime;

        if (startingNow && debugLogging)
            Debug.Log($"[RespawnToOrigin] '{name}' left its spot; {respawnDelaySeconds:0.#}s until it goes back.", this);

        if (awaySeconds >= respawnDelaySeconds)
            Respawn();
    }

    /// <summary>Put the prop back now, whatever the clock says. Wire to a reset button.</summary>
    [ContextMenu("Respawn now")]
    public void Respawn()
    {
        awaySeconds = 0f;
        ReleaseFromHands();

        // Clearing the velocities first stops the prop from carrying the speed it had when it
        // was moved and immediately sliding off the shelf again.
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(homePosition, homeRotation);

        if (debugLogging)
            Debug.Log($"[RespawnToOrigin] '{name}' put back.", this);
    }

    private void ReleaseFromHands()
    {
        BNG.Grabbable bngGrabbable = GetComponent<BNG.Grabbable>();
        if (bngGrabbable != null)
        {
            BNG.Grabber[] grabbers = FindObjectsByType<BNG.Grabber>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < grabbers.Length; i++)
            {
                if (grabbers[i] != null && grabbers[i].HeldGrabbable == bngGrabbable)
                    grabbers[i].TryRelease();
            }
        }

        XRGrabInteractable xrGrabbable = GetComponent<XRGrabInteractable>();
        if (xrGrabbable == null || !xrGrabbable.isSelected || xrGrabbable.interactionManager == null)
            return;

        List<IXRSelectInteractor> selectingInteractors = new List<IXRSelectInteractor>(
            xrGrabbable.interactorsSelecting);

        for (int i = selectingInteractors.Count - 1; i >= 0; i--)
            xrGrabbable.interactionManager.SelectExit(selectingInteractors[i], xrGrabbable);
    }

    /// <summary>Re-read the current pose as the spot to return to.</summary>
    [ContextMenu("Use current pose as home")]
    public void SetHomeToCurrentPose()
    {
        transform.GetPositionAndRotation(out homePosition, out homeRotation);
        awaySeconds = 0f;
    }

    private void ResetClock(string because)
    {
        if (awaySeconds > 0f && debugLogging)
            Debug.Log($"[RespawnToOrigin] '{name}' clock reset ({because}).", this);

        awaySeconds = 0f;
    }
}
