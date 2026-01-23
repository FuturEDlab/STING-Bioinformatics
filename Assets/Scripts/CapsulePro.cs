using System.Collections.Generic;
using UnityEngine;
using BNG;

public class CapsulePro : MonoBehaviour
{
    [Tooltip("If you want multiple objects to do an action when interacting with selected object," +
             " add those objects here. Disclaimer: Selected object will need to be added as well if" +
             " you want this object to act along with other objects")]
    [SerializeField] private List<GameObject> objectsToChange;
    
    private List<Vector3> defaultPositions;
    private List<Transform> affectedObjects;
    private bool isNewPosition;
    private Grabbable grabbableObj;
    private Interact interactComponent;

    void Start()
    {
        interactComponent = GetComponent<Interact>();
        if (interactComponent == null)
        {
            Destroy(this);
            // return;
        }
        
        // Initialize lists for each object
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

        grabbableObj = GetComponent<Grabbable>();
    }
    
    public void Interact()
    {
        if (grabbableObj != null && grabbableObj.BeingHeld) return;
        
        ChangeObjectPositions();
    }
    
    void ChangeObjectPositions()
    {
        
        for (int i = 0; i < affectedObjects.Count; i++)
        {
            Vector3 currentPos = affectedObjects[i].localPosition;
            Debug.Log($"current y position: {currentPos.y}. This is obj number {i}");

            if (Mathf.Approximately(currentPos.y, defaultPositions[i].y))
            {
                affectedObjects[i].localPosition = new Vector3(currentPos.x, currentPos.y + 0.20f, currentPos.z);
                Debug.Log($"new y position: {affectedObjects[i].localPosition.y}. This is obj number {i}");
            }
            else
            {
                affectedObjects[i].localPosition = new Vector3(currentPos.x, defaultPositions[i].y, currentPos.z);
            }
        }
        
    }
}
