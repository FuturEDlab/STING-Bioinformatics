using UnityEngine;

public class TVBehavior : MonoBehaviour
{
    [SerializeField] private Transform rightHand;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Material greyGlossMaterial;
    [SerializeField] private Material phillyMaterial;
    
    private Renderer renderer;
    private Collider objectCollider;
    private Material[] rendMaterials;
    private const string interactStr = "Interact";
    
    void Start()
    {
        renderer = GetComponent<Renderer>();
    }
    
    public void Interact()
    {
        rendMaterials = renderer.materials;
        
        Ray rayRight = new Ray(rightHand.transform.position, rightHand.transform.forward);
        Ray rayLeft = new Ray(leftHand.transform.position, leftHand.transform.forward);
        
        RaycastHit hit;
        if (Physics.Raycast(rayRight, out hit, 20))
        {
            if (hit.collider.CompareTag(interactStr))
            {
                TurnOnTV();
                return;
            }
        }
        
        if (Physics.Raycast(rayLeft, out hit, 20))
        {
            if (hit.collider.CompareTag(interactStr))
            {
                TurnOnTV();
            }
        }
    }
    
    void TurnOnTV()
    {
        string screenMaterial = renderer.materials[0].name;

        if (screenMaterial.Contains("greyGloss"))
        {
            rendMaterials[0] = phillyMaterial;
            renderer.materials = rendMaterials;
        }
        else
        {
            TurnOffTV();
        }
    }
    
    void TurnOffTV()
    {
        rendMaterials[0] = greyGlossMaterial;
        renderer.materials = rendMaterials;
        Debug.Log($"off tv -> {renderer.materials}");
    }
}
