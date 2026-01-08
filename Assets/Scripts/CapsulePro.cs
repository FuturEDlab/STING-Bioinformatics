using UnityEngine;
using BNG;

public class CapsulePro : MonoBehaviour
{
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
