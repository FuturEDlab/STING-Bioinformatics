using System.Collections.Generic;
using UnityEngine;
using BNG;
using UnityEditor;

// [Tooltip("If you remove this component, ensure no other objects depend on this instance.")]
public class CapsulePro : MonoBehaviour
{
    // [SerializeField] private
    // bool removeComponentWhenInteractComponentGetsRemoved = true;
    // [SerializeField] 
    // private List<GameObject> otherObjectsUsingThisObjectsCapsulePro;
    // [SerializeField] private Interact interactScript = ;
    // [Header("⚠️ Important")]
    // [Header("If you remove this component,\n" +
    //         "ensure no other objects depend on this instance.")]
    // [SerializeField]
    // private bool removalWarning;
    
    
    private Vector3 defaultPosition;
    private bool isNewPosition;
    private Grabbable grabbableObj;
    private bool isBeingHeld;
    

    void Start()
    {
        defaultPosition = transform.localPosition;
        grabbableObj = GetComponent<Grabbable>();
    }
    
    public void Interact()
    {
        if (grabbableObj != null && grabbableObj.BeingHeld) return;
        ChangeObjectPosition();
    }
    
    void ChangeObjectPosition()
    {
        if (isNewPosition)
        {
            transform.localPosition = defaultPosition;
            isNewPosition = false;
        }
        else
        {
            transform.localPosition = new Vector3(-5.57f, 0.05f, -0.2f);
            isNewPosition = true;
        }
    }
}
