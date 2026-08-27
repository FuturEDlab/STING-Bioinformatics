using UnityEngine;

public class KeyBoardBehavior : MonoBehaviour
{
    [Header("Hands — leave empty to use whichever rig is in the scene")]
    [Tooltip("The transform whose forward the 'am I pointing at the keyboard?' ray is cast along. Empty falls back to Rig.RightHand, so this works on the BNG rig and on the new XRI hands without being re-wired per scene.")]
    [SerializeField] private Transform rightHand;

    [Tooltip("As above, for the left hand.")]
    [SerializeField] private Transform leftHand;

    [SerializeField] private Renderer ehrTerminal;
    [SerializeField] private Material screenOffMaterial;
    [SerializeField] private Material screenOnMaterial;
    
    private Collider objectCollider;
    private Material[] rendMaterials;
    private const string interactStr = "Interact";
    private int screenElement = 3;
    private Interact interactComp;
    
    void Start()
    {
        interactComp = GetComponent<Interact>();
        if (interactComp != null)
        {
            interactComp.SetButtonInteractionEnabled(false);
            interactComp.SetTouchInteractionEnabled(true);
        }
    }
    
    public void Interact()
    {
        rendMaterials = ehrTerminal.materials;

        Transform right = rightHand != null ? rightHand : Rig.RightHand;
        Transform left = leftHand != null ? leftHand : Rig.LeftHand;

        RaycastHit hit;

        if (right != null && Physics.Raycast(new Ray(right.position, right.forward), out hit, 20))
        {
            if (hit.collider.CompareTag(interactStr) || interactComp.IsHandNear)
            {
                TurnOn_EHRTerminal();
                return;
            }
        }

        if (left != null && Physics.Raycast(new Ray(left.position, left.forward), out hit, 20))
        {
            if (hit.collider.CompareTag(interactStr) || interactComp.IsHandNear)
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
