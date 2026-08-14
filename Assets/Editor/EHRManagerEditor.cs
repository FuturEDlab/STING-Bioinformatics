using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EHRManager))]
public class EHRManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EHRManager mgr = (EHRManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Scene icon objects cannot be stored in the sequence asset. Use the 'Scene Icon Instances' list on this component to reference Hierarchy objects (drag from Hierarchy).\nUse the buttons below to auto-fill from a parent.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill From Icon Parent"))
        {
            FillFromParent(mgr, mgr.iconParent);
            EditorUtility.SetDirty(mgr);
        }
        if (GUILayout.Button("Fill From TargetRenderer Children"))
        {
            Transform parent = mgr.targetRenderer != null ? mgr.targetRenderer.transform : null;
            FillFromParent(mgr, parent);
            EditorUtility.SetDirty(mgr);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Align List To Sequence Length"))
        {
            AlignListToSequence(mgr);
            EditorUtility.SetDirty(mgr);
        }

        if (GUILayout.Button("Clear Scene Icon Instances"))
        {
            Undo.RecordObject(mgr, "Clear Scene Icon Instances");
            mgr.sceneIconInstances.Clear();
            EditorUtility.SetDirty(mgr);
        }
    }

    void FillFromParent(EHRManager mgr, Transform parent)
    {
        if (mgr == null) return;
        if (parent == null)
        {
            Debug.LogWarning("Parent is null. Assign iconParent or ensure targetRenderer is set.");
            return;
        }

        Undo.RecordObject(mgr, "Fill Scene Icon Instances");
        mgr.sceneIconInstances = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i).gameObject;
            mgr.sceneIconInstances.Add(child);
        }
    }

    void AlignListToSequence(EHRManager mgr)
    {
        if (mgr == null) return;
        Undo.RecordObject(mgr, "Align Scene Icon List");
        int target = mgr.sequence != null && mgr.sequence.entries != null ? mgr.sequence.entries.Count : 0;
        if (mgr.sceneIconInstances == null)
            mgr.sceneIconInstances = new System.Collections.Generic.List<GameObject>();
        while (mgr.sceneIconInstances.Count < target) mgr.sceneIconInstances.Add(null);
        while (mgr.sceneIconInstances.Count > target) mgr.sceneIconInstances.RemoveAt(mgr.sceneIconInstances.Count - 1);
    }
}
