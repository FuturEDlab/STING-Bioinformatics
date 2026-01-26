using UnityEngine;

public class CapsuleVillian : MonoBehaviour
{
    private Vector3 defaultScale;
    private bool isNewScale;

    void Start()
    {
        defaultScale = transform.localScale;
    }
    
    public void Interact()
    {
        ChangeObjectScale();
    }
    
    void ChangeObjectScale()
    {
        if (isNewScale)
        {
            transform.localScale = defaultScale;
            isNewScale = false;
        }
        else
        {
            transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            isNewScale = true;
        }
    }
}
