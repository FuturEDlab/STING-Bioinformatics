using System.Collections.Generic;
using UnityEngine;

public class CapsulePro : MonoBehaviour
{
    [Tooltip("If you want multiple objects to do an action when interacting with selected object," +
             " add those objects here. Disclaimer: Selected object will need to be added as well if" +
             " you want this object to act along with other objects")]
    [SerializeField] private List<GameObject> objectsToChange = new List<GameObject>();
    
    private List<Vector3> defaultPositions = new List<Vector3>();
    private List<Transform> affectedObjects = new List<Transform>();
    private GrabHandle grab;
    private Interact interactComponent;

    void Start()
    {
        interactComponent = GetComponent<Interact>();
        if (interactComponent == null)
        {
            Destroy(this);
            // return;
        }
        
        foreach (GameObject obj in objectsToChange)
        {
            if (obj != null)
            {
                Transform objTransform = obj.transform;
                affectedObjects.Add(objTransform);
                defaultPositions.Add(objTransform.localPosition);
            }
        }

        // If no objects specified, use this object
        if (affectedObjects.Count == 0)
        {
            affectedObjects.Add(transform);
            defaultPositions.Add(transform.localPosition);
        }

        // Held state comes from GrabHandle so this works on the BNG rig, on the new XRI
        // hands, and when the desktop mouse pointer is carrying the object.
        grab = new GrabHandle(this, searchRelatives: false);
    }
    
    public void Interact()
    {
        if (grab != null && grab.IsHeld) return;
        
        ChangeObjectPositions();
    }
    
    void ChangeObjectPositions()
    {
        
        for (int i = 0; i < affectedObjects.Count; i++)
        {
            Vector3 currentPos = affectedObjects[i].localPosition;

            // Check if object has returned to its default Y position
            if (Mathf.Approximately(currentPos.y, defaultPositions[i].y))
            {
                affectedObjects[i].localPosition = new Vector3(currentPos.x, currentPos.y + 0.20f, currentPos.z);
            }
            else
            {
                affectedObjects[i].localPosition = new Vector3(currentPos.x, defaultPositions[i].y, currentPos.z);
            }
        }
        
    }
}
