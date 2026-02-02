using System;
using UnityEngine;
using System.Collections.Generic;

public class GrabCollisionsOn : MonoBehaviour
{
    // [SerializeField] private List<Transform> collidableObjects;
    [SerializeField] private List<GameObject> collidableObjects;
    // [SerializeField] private GameObject[] collidableObjects;
    // private int colliderCount = 0;
    // private bool allLayersAdded;
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
        // Iterate through all child transforms of this GameObject
        foreach (GameObject obj in collidableObjects)
        {
            obj.gameObject.layer = LayerMask.NameToLayer("Collidable");
        }
    }
#endif

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // void Start()
    // {
    //     int collidableLayer = LayerMask.NameToLayer("Collidable");
    //     Debug.Log($"Collidable layer index: {collidableLayer}"); // Should NOT be -1
    //    
    //     foreach (GameObject obj in collidableObjects)
    //     {
    //         if (obj != null)
    //         {
    //             // Debug.Log($"Changing {obj.name} from layer {obj.layer} to {collidableLayer}");
    //             obj.gameObject.layer = collidableLayer;
    //             // Debug.Log($"After change: {obj.name} is now on layer {obj.layer}");
    //         }
    //     }
    // }
    
    // Update is called once per frame
    // void Update()
    // {
    //     int collidableLayer = LayerMask.NameToLayer("Collidable");
    //     Debug.Log($"Collidable layer index: {collidableLayer}"); // Should NOT be -1
    //    
    //     foreach (GameObject obj in collidableObjects)
    //     {
    //         if (obj != null)
    //         {
    //             // Debug.Log($"Changing {obj.name} from layer {obj.layer} to {collidableLayer}");
    //             obj.layer = LayerMask.NameToLayer("Collidable");
    //             colliderCount += 1;
    //             // Debug.Log($"After change: {obj.name} is now on layer {obj.layer}");
    //         }
    //     }
    //     
    //     if (colliderCount = )
    // }
}
