using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerArrowController : MonoBehaviour
{
    [Header("References (assign in Inspector)")]
    [SerializeField] private CharacterController controller;  
    [SerializeField] private PlayerInteractor interactor;     

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = 9.81f;

    [Header("Auto-Collect")]
    [SerializeField] private bool autoCollect = true;
    [SerializeField] private float interactionRange = 1.5f;
    [SerializeField] private float collectCooldown = 0.05f; 

    private Vector3 velocity;
    private float lastCollectTime = -999f;
    
    private Interactable currentTarget;
    public Interactable CurrentTarget => currentTarget;

    void Update()
    {
        HandleMovement();

        if (autoCollect && Time.time - lastCollectTime >= collectCooldown)
        {
            AutoCollectAllInRange();
            lastCollectTime = Time.time;
        }
        
        currentTarget = FindNearestInRange();
    }

    private void HandleMovement()
    {
        float h = 0f, v = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))  h = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) h =  1f;
        if (Input.GetKey(KeyCode.UpArrow))    v =  1f;
        if (Input.GetKey(KeyCode.DownArrow))  v = -1f;

        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 move = input * moveSpeed;

        if (move.sqrMagnitude > 0.0001f)
            transform.forward = move.normalized;

        if (controller.isGrounded) velocity.y = -0.5f;
        else velocity.y -= gravity * Time.deltaTime;

        controller.Move((move + velocity) * Time.deltaTime);
    }

    private Interactable FindNearestInRange()
    {
        Interactable best = null;
        float bestD2 = interactionRange * interactionRange;

        foreach (var it in Interactable.Registry)
        {
            if (it == null || !it.isActiveAndEnabled) continue;
            float d2 = (it.transform.position - transform.position).sqrMagnitude;
            if (d2 <= bestD2)
            {
                bestD2 = d2;
                best = it;
            }
        }
        return best;
    }

    private void AutoCollectAllInRange()
    {
        List<Interactable> inRange = new List<Interactable>();
        float r2 = interactionRange * interactionRange;

        foreach (var it in Interactable.Registry)
        {
            if (it == null || !it.isActiveAndEnabled) continue;
            float d2 = (it.transform.position - transform.position).sqrMagnitude;
            if (d2 <= r2) inRange.Add(it);
        }

        if (inRange.Count == 0) return;
        
        inRange.Sort((a, b) =>
        {
            float da = (a.transform.position - transform.position).sqrMagnitude;
            float db = (b.transform.position - transform.position).sqrMagnitude;
            return da.CompareTo(db);
        });

        bool attachedOne = false;

        for (int i = 0; i < inRange.Count; i++)
        {
            var it = inRange[i];
            
            if (it is PickupItem pi)
            {
                if (!attachedOne && interactor.CurrentlyHeld == null)
                {
                    pi.Interact(interactor);
                    attachedOne = true;
                }
                else
                {
                    pi.CollectWithoutHolding();
                }
            }
            else
            {
                it.Interact(interactor);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
