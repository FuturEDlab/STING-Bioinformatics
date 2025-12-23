using UnityEngine;
using BNG;

public class NonPickUpGroup : MonoBehaviour
{
    private void Awake()
    {

        Grabbable grabbableObject;
        Rigidbody rigidObject;
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Grabbable>() != null)
            {
                grabbableObject = child.gameObject.GetComponent<Grabbable>();
                Destroy(grabbableObject);
            }

            if (child.GetComponent<Rigidbody>() != null)
            {
                rigidObject = child.gameObject.GetComponent<Rigidbody>();
                Destroy(rigidObject);
            }
        }
    }
}
