using System;
using UnityEngine;
using System.Collections.Generic;

public class GrabCollisionsOn : MonoBehaviour
{
    [SerializeField] private List<GameObject> collidableObjects;

    private void Awake()
    {
        int collidableLayer = LayerMask.NameToLayer("Collidable");
        if (collidableLayer == -1)
        {
            Debug.LogError("Layer 'Collidable' doesn't exist.");
            return;
        }
       
        foreach (GameObject obj in collidableObjects)
        {
            if (obj != null)
            {
                obj.layer = collidableLayer;
            }
        }
    }
}
