using UnityEngine;

public class KeyBoardBehavior : MonoBehaviour
{
    [SerializeField] private Transform rightHand;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Renderer ehrTerminal;
    [SerializeField] private Material screenOffMaterial;
    [SerializeField] private Material screenOnMaterial;
    
    private Collider objectCollider;
    private Material[] rendMaterials;
    private const string interactStr = "Interact";
    private int screenElement = 3;
    
    // void Start()
    // {
    //     Debug.Log(screenOffMaterial);
    // }
    
    public void Interact()
    {
        
        rendMaterials = ehrTerminal.materials;
        Ray rayRight = new Ray(rightHand.transform.position, rightHand.transform.forward);
        Ray rayLeft = new Ray(leftHand.transform.position, leftHand.transform.forward);
        
        RaycastHit hit;
        if (Physics.Raycast(rayRight, out hit, 20))
        {
            if (hit.collider.CompareTag(interactStr))
            {
                TurnOn_EHRTerminal();
                return;
            }
        }
        if (Physics.Raycast(rayLeft, out hit, 20))
        {
            if (hit.collider.CompareTag(interactStr))
            {
                TurnOn_EHRTerminal();
            }
        }
    }
    
    void TurnOn_EHRTerminal()
    {
        string ehrScreenMat = ehrTerminal.materials[screenElement].name;
        
        if (ehrScreenMat.Contains(screenOffMaterial.name))
        {
            rendMaterials[screenElement] = screenOnMaterial;
            ehrTerminal.materials = rendMaterials;
        }
        else
        {
            TurnOff_EHRTerminal();
        }
    }
    
    void TurnOff_EHRTerminal()
    {
        rendMaterials[screenElement] = screenOffMaterial;
        ehrTerminal.materials = rendMaterials;
    }
}
