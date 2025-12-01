using UnityEngine;
using System.Collections.Generic;

public class InteractableGroup : Interactable
{
    [Tooltip("Drag Interactable objects here (children or anywhere in scene).")]
    [SerializeField] private List<Interactable> members = new List<Interactable>();

    public override void Interact(PlayerInteractor byWhom)
    {
        if (byWhom == null || members == null || members.Count == 0) return;

      
        Interactable nearest = null;
        float bestDistSqr = float.PositiveInfinity;
        Vector3 who = byWhom.transform.position;

        foreach (var m in members)
        {
            if (m == null || !m.isActiveAndEnabled) continue;
            float d2 = (m.transform.position - who).sqrMagnitude;
            if (d2 < bestDistSqr)
            {
                bestDistSqr = d2;
                nearest = m;
            }
        }
        
        if (nearest != null)
            nearest.Interact(byWhom);

      
        foreach (var m in members)
        {
            if (m == null || !m.isActiveAndEnabled || m == nearest) continue;

            if (m is PickupItem pi)
            {
                pi.CollectWithoutHolding();
            }
            else
            {
                m.Interact(byWhom);
            }
        }

        Debug.Log($"Group interact: {DisplayName} (attached 1, collected {members.Count - 1})");
    }
}