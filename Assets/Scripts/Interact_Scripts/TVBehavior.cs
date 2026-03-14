using UnityEngine;
using System.Collections.Generic;

public class TVBehavior : MonoBehaviour
{
    [SerializeField] private Transform rightHand;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Material greyGlossMaterial;
    [SerializeField] private Material silverMaterial;
    
    private Renderer renderer;
    private Collider objectCollider;
    // private List<Material> rendMaterials;
    private Material[] rendMaterials;
    
    void Start()
    {
        renderer = GetComponent<Renderer>();
        // rendMaterials = renderer.materials;
        // rendMaterials = new List<Material>(renderer.materials);
    }
    
    public void Interact()
    {
        // TODO: When you move away from TV to point where you're not within radius,
        // materials go back to default materials, so it turns off automatically
        
        rendMaterials = renderer.materials;
        
        // rightHand.gameObject.SetActive(true);
        // leftHand.gameObject.SetActive(true);
        
        Ray rayRight = new Ray(rightHand.transform.position, rightHand.transform.forward);
        Ray rayLeft = new Ray(leftHand.transform.position, leftHand.transform.forward);
        
        RaycastHit hit;
        if (Physics.Raycast(rayRight, out hit, 20))
        {
            if (hit.collider.CompareTag("TV"))
            {
                Debug.Log("hit TV");
                TurnOnTV();
                return;
            }
            // Debug.Log("TV dealioooo");
            // TurnOnTV();
        }
        if (Physics.Raycast(rayLeft, out hit, 20))
        {
            // if (hit.collider.gameObject == gameObject)
            if (hit.collider.CompareTag("TV"))
            {
                Debug.Log("hit TV");
                TurnOnTV();
            }
        }
    }
    
    void TurnOnTV()
    {
        string screenMaterial = renderer.materials[0].name;
        Debug.Log(screenMaterial);

        if (screenMaterial.Contains("greyGloss"))
        {
            // Material[] mats = renderer.materials;
            rendMaterials[0] = silverMaterial;
            renderer.materials = rendMaterials;
            Debug.Log($"on tv -> {renderer.materials}");
        }
        else
        {
            TurnOffTV();
        }

        // foreach (Material mat in renderer.materials)
        // {
        //     if (mat.name ==)
        //     {
        //         
        //     }
        // }
    }
    
    void TurnOffTV()
    {
        rendMaterials[0] = greyGlossMaterial;
        renderer.materials = rendMaterials;
        Debug.Log($"off tv -> {renderer.materials}");
    }
}
