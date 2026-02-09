using UnityEngine;
using System.Collections.Generic;

public class TableGroup : MonoBehaviour
{
    [Tooltip("Tables that can snap or drop intersecting objects when released during play mode")]
    [SerializeField] private List<GameObject> tables = new List<GameObject>();
    
    private const string tableString = "Table";
    private HashSet<GameObject> tableSet;
    
    void Start()
    {
        if (tables == null || tables.Count == 0) return;
        tableSet = new HashSet<GameObject>(tables);
        
        foreach (GameObject table in tableSet)
        {
            if (table == null) continue;
            
            if (!table.CompareTag(tableString))
            {
                table.tag = tableString;
            }
        }
    }
}
