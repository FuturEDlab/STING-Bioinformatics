using UnityEngine;
using System.Collections.Generic;

public class TableGroup : MonoBehaviour
{
    [Tooltip("Tables that can snap or drop intersecting objects when released during play mode")]
    [SerializeField] private List<Collider> tables = new List<Collider>();
    
    // private const string tableString = "Table";
    private HashSet<Collider> tableSet;

    public List<Collider> Tables => tables;

    void Start()
    {
        if (tables == null || tables.Count == 0) return;
        tableSet = new HashSet<Collider>(tables);
        
        // foreach (Collider table in tableSet)
        // {
        //     if (table == null) continue;
        //     
        //     if (!table.CompareTag(tableString))
        //     {
        //         table.tag = tableString;
        //     }
        // }
    }
}
