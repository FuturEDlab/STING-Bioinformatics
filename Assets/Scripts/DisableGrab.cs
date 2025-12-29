using UnityEngine;
using BNG;

public class DisableGrab : MonoBehaviour
{
    private Grabbable grabbable;
    
    void Start()
    {
        grabbable = GetComponent<Grabbable>();
    }
    
    void Update()
    {
        // If someone tries to grab it, immediately release it
        if (grabbable.BeingHeld)
        {
            grabbable.DropItem(false, false);
        }
    }
}
