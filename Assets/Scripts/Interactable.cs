using UnityEngine;
using System.Collections.Generic;

public abstract class Interactable : MonoBehaviour
{
    public static readonly HashSet<Interactable> Registry = new HashSet<Interactable>();

    [SerializeField] protected string displayName = "Item";
    public string DisplayName => displayName;

    protected virtual void OnEnable()  { Registry.Add(this); }
    protected virtual void OnDisable() { Registry.Remove(this); }
    
    public abstract void Interact(PlayerInteractor byWhom);
}