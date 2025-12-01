using UnityEngine;

public class FramingCamera : MonoBehaviour
{
    [Header("References (assign in Inspector)")]
    [SerializeField] private Transform player;                 
    [SerializeField] private PlayerArrowController controller;  
    [SerializeField] private PlayerInteractor interactor;      

    [Header("Positioning")]
    [SerializeField] private float baseDistance = 8f;       
    [SerializeField] private float height = 4.5f;           
    [SerializeField] private float sideOffset = 2.0f;       
    [SerializeField] private float pitchDegrees = 20f;      
    [SerializeField] private Vector2 distanceLimits = new Vector2(6f, 14f); 
    [SerializeField] private float smooth = 8f;             

    private void LateUpdate()
    {
        if (player == null || controller == null) return;
        
        Vector3 focus = player.position;
        var target = controller.CurrentTarget;

        if (target != null)
        {
            focus = (player.position + target.transform.position) * 0.5f;
        }
        else if (interactor != null && interactor.HoldPoint != null && interactor.CurrentlyHeld != null)
        {
            focus = Vector3.Lerp(player.position, interactor.HoldPoint.position, 0.4f);
        }
        
        float focusSpread = Vector3.Distance(player.position, focus);
        float desiredDistance = Mathf.Clamp(baseDistance + focusSpread * 0.6f, distanceLimits.x, distanceLimits.y);
        
        Vector3 back = -player.forward;
        Vector3 right = Vector3.Cross(Vector3.up, back);
        Vector3 offset = (back * desiredDistance) + (Vector3.up * height) + (right.normalized * sideOffset);
        
        Quaternion pitch = Quaternion.Euler(pitchDegrees, 0f, 0f);
        Vector3 desiredPos = focus + pitch * offset;
        
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * smooth);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(focus - transform.position, Vector3.up),
            Time.deltaTime * smooth
        );
    }
}
