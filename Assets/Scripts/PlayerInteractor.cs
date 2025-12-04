using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Where held items attach")]
    [SerializeField] private Transform holdPoint; 

    [SerializeField] private Transform currentlyHeld; 

    public Transform HoldPoint => holdPoint;
    public Transform CurrentlyHeld
    {
        get => currentlyHeld;
        set => currentlyHeld = value;
    }
}

