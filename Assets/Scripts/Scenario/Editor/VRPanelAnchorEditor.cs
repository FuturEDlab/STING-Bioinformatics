using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VRPanelAnchor))]
public class VRPanelAnchorEditor : Editor
{
    private SerializedProperty placementMode;
    private SerializedProperty head;
    private SerializedProperty distance;
    private SerializedProperty heightOffset;
    private SerializedProperty customPlacementPosition;
    private SerializedProperty customPlacementEulerAngles;
    private SerializedProperty panelWidthMetres;
    private SerializedProperty replaceIfFurtherThan;
    private SerializedProperty placeOnEnable;
    private SerializedProperty registerWithVrUi;
    private SerializedProperty logPlacement;

    private void OnEnable()
    {
        placementMode = serializedObject.FindProperty("placementMode");
        head = serializedObject.FindProperty("head");
        distance = serializedObject.FindProperty("distance");
        heightOffset = serializedObject.FindProperty("heightOffset");
        customPlacementPosition = serializedObject.FindProperty("customPlacementPosition");
        customPlacementEulerAngles = serializedObject.FindProperty("customPlacementEulerAngles");
        panelWidthMetres = serializedObject.FindProperty("panelWidthMetres");
        replaceIfFurtherThan = serializedObject.FindProperty("replaceIfFurtherThan");
        placeOnEnable = serializedObject.FindProperty("placeOnEnable");
        registerWithVrUi = serializedObject.FindProperty("registerWithVrUi");
        logPlacement = serializedObject.FindProperty("logPlacement");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(placementMode, new GUIContent("Placement Mode"));

        var mode = (VRPanelAnchor.PlacementMode)placementMode.enumValueIndex;
        if (mode == VRPanelAnchor.PlacementMode.VRHeadPlacement)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("VR Head Placement", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(head, new GUIContent("Head"));
            EditorGUILayout.PropertyField(distance, new GUIContent("Distance"));
            EditorGUILayout.PropertyField(heightOffset, new GUIContent("Height Offset"));
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(customPlacementPosition, new GUIContent("Position"));
            EditorGUILayout.PropertyField(customPlacementEulerAngles, new GUIContent("Rotation"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shared Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(panelWidthMetres, new GUIContent("Panel Width Metres"));
        EditorGUILayout.PropertyField(registerWithVrUi, new GUIContent("Register With VR UI"));
        EditorGUILayout.PropertyField(logPlacement, new GUIContent("Log Placement"));

        if (mode == VRPanelAnchor.PlacementMode.VRHeadPlacement)
        {
            EditorGUILayout.PropertyField(replaceIfFurtherThan, new GUIContent("Replace If Further Than"));
            EditorGUILayout.PropertyField(placeOnEnable, new GUIContent("Place On Enable"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
