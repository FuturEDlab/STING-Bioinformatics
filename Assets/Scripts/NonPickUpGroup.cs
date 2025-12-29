using UnityEngine;
using BNG;
// using NUnit.Framework;
using System.Collections.Generic;

public class NonPickUpGroup : MonoBehaviour
{
    
    private void Awake()
    {
        
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out Grabbable grabbableObject))
            {
                Destroy(grabbableObject);
            }
            
            if (child.TryGetComponent(out Rigidbody rigidObject))
            {
                Destroy(rigidObject);
            }
        }
    }
}
