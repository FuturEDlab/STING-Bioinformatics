using UnityEngine;

public class TVBehavior : MonoBehaviour
{
    [Header("Hands — leave empty to use whichever rig is in the scene")]
    [Tooltip("The transform whose forward the 'am I pointing at the TV?' ray is cast along. Empty falls back to Rig.RightHand, so this works on the BNG rig and on the new XRI hands without being re-wired per scene.")]
    [SerializeField] private Transform rightHand;

    [Tooltip("As above, for the left hand.")]
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

        Transform right = rightHand != null ? rightHand : Rig.RightHand;
        Transform left = leftHand != null ? leftHand : Rig.LeftHand;

        RaycastHit hit;

        if (right != null && Physics.Raycast(new Ray(right.position, right.forward), out hit, 20))
        {
            if (hit.collider.CompareTag(interactStr))
            {
                TurnOnTV();
                return;
            }
        }

        if (left != null && Physics.Raycast(new Ray(left.position, left.forward), out hit, 20))
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
