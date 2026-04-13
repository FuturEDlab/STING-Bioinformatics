using UnityEngine;
using System.Collections.Generic;

public class TableGroup : MonoBehaviour
{
    [Tooltip("Tables that can snap or drop intersecting objects when released during play mode")]
    [SerializeField] private List<Collider> tables = new List<Collider>();
    
    private HashSet<Collider> tableSet;
    public HashSet<Collider> Tables => tableSet;

    void Awake()
    {
        if (tables == null || tables.Count == 0) return;
        tableSet = new HashSet<Collider>(tables);
    }
}
