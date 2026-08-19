using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EHRSequencePlayer))]
public class EHRSequencePlayerEditor : Editor
{
    private SerializedProperty targetRendererProp;
    private SerializedProperty stepsProp;
    private SerializedProperty autoStartOnEnableProp;
    private SerializedProperty loopSequenceProp;
    private SerializedProperty scenarioDrivenProp;
    private SerializedProperty defaultAnimatorTriggerProp;

    private void OnEnable()
    {
        targetRendererProp = serializedObject.FindProperty("targetRenderer");
        stepsProp = serializedObject.FindProperty("steps");
        autoStartOnEnableProp = serializedObject.FindProperty("autoStartOnEnable");
        loopSequenceProp = serializedObject.FindProperty("loopSequence");
        scenarioDrivenProp = serializedObject.FindProperty("scenarioDriven");
        defaultAnimatorTriggerProp = serializedObject.FindProperty("defaultAnimatorTrigger");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(8f);
        EditorGUILayout.PropertyField(targetRendererProp, new GUIContent("Target Sprite Renderer"));
        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(autoStartOnEnableProp, new GUIContent("Auto Start On Enable"));
        EditorGUILayout.PropertyField(loopSequenceProp, new GUIContent("Loop Sequence"));
        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(scenarioDrivenProp, new GUIContent("Scenario Driven"));

        if (scenarioDrivenProp.boolValue)
        {
            EditorGUILayout.HelpBox(
                "The Scenario Controller drives this terminal. Step durations are ignored and a button " +
                "press no longer advances the screen - the EHR Scenario Bridge switches screens when the " +
                "scenario reaches each beat. Durations and action names below are still the authoring " +
                "surface: the bridge cues screens by Step Name and reports presses by Action Name.",
                MessageType.Info);
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(defaultAnimatorTriggerProp, new GUIContent("Default Animator Trigger"));
        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Sequence Steps", EditorStyles.boldLabel);

        for (int i = 0; i < stepsProp.arraySize; i++)
        {
            SerializedProperty stepProp = stepsProp.GetArrayElementAtIndex(i);
            SerializedProperty conditionTypeProp = stepProp.FindPropertyRelative("conditionType");
            SerializedProperty showSceneObjectProp = stepProp.FindPropertyRelative("showSceneObject");
            EHRSequencePlayer.StepConditionType conditionType = (EHRSequencePlayer.StepConditionType)conditionTypeProp.enumValueIndex;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            string stepName = stepProp.FindPropertyRelative("stepName").stringValue;
            string headerText = "Step " + (i + 1);
            if (!string.IsNullOrWhiteSpace(stepName))
            {
                headerText += " - " + stepName;
            }
            EditorGUILayout.LabelField(headerText, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("↑", GUILayout.Width(26f)))
            {
                MoveStep(i, -1);
                return;
            }

            if (GUILayout.Button("↓", GUILayout.Width(26f)))
            {
                MoveStep(i, 1);
                return;
            }

            if (GUILayout.Button("Remove", GUILayout.Width(70f)))
            {
                stepsProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("stepName"), new GUIContent("Step Name"));
            EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("image"), new GUIContent("Image"));
            EditorGUILayout.PropertyField(conditionTypeProp, new GUIContent("Advance Condition"));

            if (conditionType == EHRSequencePlayer.StepConditionType.Time)
            {
                EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("duration"), new GUIContent("Duration"));
            }
            else if (conditionType == EHRSequencePlayer.StepConditionType.Action)
            {
                EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("actionName"), new GUIContent("Action Name"));
            }

            EditorGUILayout.PropertyField(showSceneObjectProp, new GUIContent("Show Scene Object"));

            if (showSceneObjectProp.boolValue)
            {
                EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("sceneObject"), new GUIContent("Scene Object"));
                EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("animator"), new GUIContent("Animator"));
                EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("iconAnimatorTrigger"), new GUIContent("Animator Trigger"));
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }

        if (GUILayout.Button("Add Step"))
        {
            int index = stepsProp.arraySize;
            stepsProp.arraySize++;
            SerializedProperty newStep = stepsProp.GetArrayElementAtIndex(index);
            newStep.FindPropertyRelative("conditionType").enumValueIndex = (int)EHRSequencePlayer.StepConditionType.Time;
            newStep.FindPropertyRelative("duration").floatValue = 3f;
            newStep.FindPropertyRelative("showSceneObject").boolValue = false;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void MoveStep(int index, int direction)
    {
        int newIndex = index + direction;
        if (newIndex < 0 || newIndex >= stepsProp.arraySize)
        {
            return;
        }

        stepsProp.MoveArrayElement(index, newIndex);
        serializedObject.ApplyModifiedProperties();
    }
}
