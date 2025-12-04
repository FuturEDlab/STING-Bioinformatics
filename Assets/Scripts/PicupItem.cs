using UnityEngine;
using System.Collections;

public class PickupItem : Interactable
{
    [Header("Optional (drag if you use physics/collision)")]
    [SerializeField] private Collider itemCollider;    
    [SerializeField] private Rigidbody itemRigidbody;  

    [Header("Colors / Visuals")]
    [Tooltip("All renderers whose material color we will tint (drag MeshRenderer(s) here).")]
    [SerializeField] private Renderer[] colorRenderers;   
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private Color heldColor = new Color(0.95f, 0.95f, 0.95f); 
    [SerializeField] private Color collectFlashColor = new Color(1f, 1f, 1f);  
    [SerializeField] private float collectFlashTime = 0.12f;

    [Header("Behavior")]
    [SerializeField] private bool disablePhysicsWhenHeld = true;
    [SerializeField] private bool disableColliderWhenHeld = true;

    [Header("Targeting")]
    [Tooltip("If true, this item will NOT be directly targetable; group/auto-collect still see it.")]
    [SerializeField] private bool disableDirectInteraction = false;

    private bool isHeld = false;
    private bool registered = false;

    protected override void OnEnable()
    {
        SetColor(baseColor);

        if (!disableDirectInteraction)
        {
            base.OnEnable(); 
            registered = true;
        }
        else
        {
            Registry.Add(this);
        }
    }

    protected override void OnDisable()
    {
        if (registered)
        {
            base.OnDisable();
            registered = false;
        }
        else
        {
            Registry.Remove(this);
        }
    }

    public override void Interact(PlayerInteractor byWhom)
    {
        if (byWhom == null || isHeld) return;
        
        if (byWhom.CurrentlyHeld != null) { StartCoroutine(FlashThenDisable()); return; }

        Transform hp = byWhom.HoldPoint;
        if (hp == null) { StartCoroutine(FlashThenDisable()); return; }
        
        transform.SetParent(hp, worldPositionStays: false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (disablePhysicsWhenHeld && itemRigidbody != null)
        {
            itemRigidbody.isKinematic = true;
            itemRigidbody.useGravity  = false;
            itemRigidbody.linearVelocity = Vector3.zero;
            itemRigidbody.angularVelocity = Vector3.zero;
        }
        if (disableColliderWhenHeld && itemCollider != null)
        {
            itemCollider.enabled = false;
        }

        byWhom.CurrentlyHeld = transform;
        isHeld = true;
        
        SetColor(heldColor);

        Debug.Log($"Picked up (attached): {DisplayName}");
    }

    public void CollectWithoutHolding()
    {
        StartCoroutine(FlashThenDisable());
    }

    private IEnumerator FlashThenDisable()
    {
        SetColor(collectFlashColor);
        yield return new WaitForSeconds(collectFlashTime);
        gameObject.SetActive(false);
        Debug.Log($"Collected (no hold): {DisplayName}");
    }

    private void SetColor(Color c)
    {
        if (colorRenderers == null) return;
        for (int i = 0; i < colorRenderers.Length; i++)
        {
            if (colorRenderers[i] == null) continue;
            colorRenderers[i].material.color = c;
        }
    }
}
