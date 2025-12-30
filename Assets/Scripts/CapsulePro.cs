using UnityEngine;

public class CapsulePro : MonoBehaviour
{
    private Vector3 defaultPosition;
    private bool isNewPosition;

    void Start()
    {
        defaultPosition = transform.localPosition;
    }
    
    public void Interact()
    {
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
