using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// "Is this prop being held right now?", asked without caring which rig is holding it.
///
/// A prop in this project is grabbable in one of two ways: BNG's <c>Grabbable</c> (the army-guy
/// rig's hands) or XRI's <see cref="XRGrabInteractable"/> (the new hands). Scripts that only
/// want to know whether the player has hold of something — the scanner, a scenario task that
/// completes on pickup, the settle-down physics — used to name BNG's component directly and
/// so only worked on one rig.
///
/// Make one of these in <c>Awake</c>/<c>Start</c> and read <see cref="IsHeld"/> per frame.
/// Resolution is cached, so this costs a null check once it has found something.
/// </summary>
public sealed class GrabHandle
{
    private readonly Component owner;
    private readonly bool searchRelatives;

    private BNG.Grabbable bngGrabbable;
    private XRGrabInteractable xrGrabbable;
    private int lastSearchFrame = -1;

    /// <param name="owner">The prop. Usually <c>this</c>.</param>
    /// <param name="searchRelatives">
    /// Also look on the parent and children. Imported props often carry the grab component
    /// one level away from the script, so this defaults to on; pass false when only the exact
    /// object should count.
    /// </param>
    public GrabHandle(Component owner, bool searchRelatives = true)
    {
        this.owner = owner;
        this.searchRelatives = searchRelatives;
        Resolve();
    }

    /// <summary>True when this prop can be picked up at all, by either rig.</summary>
    public bool Exists
    {
        get
        {
            EnsureResolved();
            return bngGrabbable != null || xrGrabbable != null;
        }
    }

    /// <summary>True while a hand — or the desktop mouse — has hold of it.</summary>
    public bool IsHeld
    {
        get
        {
            EnsureResolved();

            // A prop carried by DesktopMousePointer counts as held, so the scanner's
            // "must be held" gate and tasks that complete on pickup behave the same at a
            // desk as they do on device. The check costs nothing when nothing is carried.
            if (owner != null && DesktopMousePointer.IsCarrying(owner.gameObject))
                return true;

            // XRI wins when both are present: that is what a scene converted by
            // XRGrabBridge looks like, and the disabled BNG component would always
            // report false.
            if (xrGrabbable != null)
                return xrGrabbable.isSelected;

            return bngGrabbable != null && bngGrabbable.BeingHeld;
        }
    }

    /// <summary>Which kind of grab component was found. For diagnostics and log messages.</summary>
    public string Kind
    {
        get
        {
            EnsureResolved();
            if (xrGrabbable != null) return "XRGrabInteractable";
            if (bngGrabbable != null) return "BNG Grabbable";
            return "none";
        }
    }

    /// <summary>Look again now. Call after adding or removing a grab component at runtime.</summary>
    public void Refresh()
    {
        bngGrabbable = null;
        xrGrabbable = null;
        lastSearchFrame = -1;
        Resolve();
    }

    /// <summary>
    /// Keep looking while nothing has been found, but at most once a frame. Props are
    /// converted to XRI at scene start by <see cref="XRGrabBridge"/>, and a handle built
    /// during the same Awake pass can be a step ahead of that.
    /// </summary>
    private void EnsureResolved()
    {
        if (bngGrabbable != null || xrGrabbable != null)
            return;

        if (lastSearchFrame == Time.frameCount)
            return;

        Resolve();
    }

    private void Resolve()
    {
        lastSearchFrame = Time.frameCount;

        if (owner == null)
            return;

        xrGrabbable = owner.GetComponent<XRGrabInteractable>();
        bngGrabbable = owner.GetComponent<BNG.Grabbable>();

        if (!searchRelatives || xrGrabbable != null || bngGrabbable != null)
            return;

        xrGrabbable = owner.GetComponentInParent<XRGrabInteractable>()
                   ?? owner.GetComponentInChildren<XRGrabInteractable>();

        if (xrGrabbable != null)
            return;

        bngGrabbable = owner.GetComponentInParent<BNG.Grabbable>()
                    ?? owner.GetComponentInChildren<BNG.Grabbable>();
    }
}
