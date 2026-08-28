using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(EHRSequencePlayer))]
public class EHRSequencePlayerEditor : Editor
{
    // =========================================================
    // Layout Settings
    // =========================================================

    private const float SidePadding = 8f;
    private const float TopPadding = 6f;
    private const float BottomPadding = 8f;

    // Extra spacing between logical sections.
    private const float SectionSpacing = 6f;


    // =========================================================
    // Serialized Properties
    // =========================================================

    private SerializedProperty targetRendererProp;
    private SerializedProperty stepsProp;
    private SerializedProperty autoStartOnEnableProp;
    private SerializedProperty loopSequenceProp;
    private SerializedProperty scenarioDrivenProp;
    private SerializedProperty defaultAnimatorTriggerProp;

    private ReorderableList stepsList;


    // =========================================================
    // Initialization
    // =========================================================

    private void OnEnable()
    {
        targetRendererProp =
            serializedObject.FindProperty("targetRenderer");

        stepsProp =
            serializedObject.FindProperty("steps");

        autoStartOnEnableProp =
            serializedObject.FindProperty("autoStartOnEnable");

        loopSequenceProp =
            serializedObject.FindProperty("loopSequence");

        scenarioDrivenProp =
            serializedObject.FindProperty("scenarioDriven");

        defaultAnimatorTriggerProp =
            serializedObject.FindProperty("defaultAnimatorTrigger");


        // Create the reorderable list.
        stepsList = new ReorderableList(
            serializedObject,
            stepsProp,
            true,   // Draggable
            true,   // Display Header
            true,   // Display Add Button
            true    // Display Remove Button
        );


        // Header
        stepsList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(
                rect,
                "Sequence Steps",
                EditorStyles.boldLabel
            );
        };


        // Draw each element.
        stepsList.drawElementCallback = DrawStepElement;


        // Dynamically calculate element height.
        stepsList.elementHeightCallback = GetStepHeight;


        // Add button behavior.
        stepsList.onAddCallback = list =>
        {
            int index = list.serializedProperty.arraySize;

            list.serializedProperty.arraySize++;

            SerializedProperty newStep =
                list.serializedProperty.GetArrayElementAtIndex(index);


            // Default values for new steps.
            newStep.FindPropertyRelative("conditionType").enumValueIndex =
                (int)EHRSequencePlayer.StepConditionType.Time;

            newStep.FindPropertyRelative("duration").floatValue = 3f;

            newStep.FindPropertyRelative("showSceneObject").boolValue = false;

            newStep.FindPropertyRelative("stepName").stringValue = string.Empty;
        };
    }


    // =========================================================
    // Main Inspector
    // =========================================================

    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        // -----------------------------------------------------
        // General Settings
        // -----------------------------------------------------

        EditorGUILayout.Space(8f);

        EditorGUILayout.PropertyField(
            targetRendererProp,
            new GUIContent("Target Sprite Renderer")
        );

        EditorGUILayout.Space(6f);

        EditorGUILayout.PropertyField(
            autoStartOnEnableProp,
            new GUIContent("Auto Start On Enable")
        );

        EditorGUILayout.PropertyField(
            loopSequenceProp,
            new GUIContent("Loop Sequence")
        );

        EditorGUILayout.Space(6f);

        EditorGUILayout.PropertyField(
            defaultAnimatorTriggerProp,
            new GUIContent("Default Animator Trigger")
        );


        // -----------------------------------------------------
        // Sequence Steps
        // -----------------------------------------------------

        EditorGUILayout.Space(10f);

        stepsList.DoLayoutList();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Scenario Control", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(
            scenarioDrivenProp,
            new GUIContent("Scenario Driven")
        );


        serializedObject.ApplyModifiedProperties();
    }


    // =========================================================
    // Draw Step
    // =========================================================

    private void DrawStepElement(
        Rect rect,
        int index,
        bool isActive,
        bool isFocused)
    {
        SerializedProperty stepProp =
            stepsProp.GetArrayElementAtIndex(index);


        // -----------------------------------------------------
        // Find Step Properties
        // -----------------------------------------------------

        SerializedProperty stepNameProp =
            stepProp.FindPropertyRelative("stepName");

        SerializedProperty imageProp =
            stepProp.FindPropertyRelative("image");

        SerializedProperty conditionTypeProp =
            stepProp.FindPropertyRelative("conditionType");

        SerializedProperty durationProp =
            stepProp.FindPropertyRelative("duration");

        SerializedProperty actionNameProp =
            stepProp.FindPropertyRelative("actionName");

        SerializedProperty showSceneObjectProp =
            stepProp.FindPropertyRelative("showSceneObject");

        SerializedProperty sceneObjectProp =
            stepProp.FindPropertyRelative("sceneObject");

        SerializedProperty animatorProp =
            stepProp.FindPropertyRelative("animator");

        SerializedProperty animatorTriggerProp =
            stepProp.FindPropertyRelative("iconAnimatorTrigger");


        EHRSequencePlayer.StepConditionType conditionType =
            (EHRSequencePlayer.StepConditionType)
            conditionTypeProp.enumValueIndex;


        // -----------------------------------------------------
        // Layout
        // -----------------------------------------------------

        float x = rect.x + SidePadding;

        float width =
            rect.width - (SidePadding * 2f);

        float y =
            rect.y + TopPadding;


        // -----------------------------------------------------
        // Step Header
        // -----------------------------------------------------

        string headerText =
            $"Step {index + 1}";


        if (!string.IsNullOrWhiteSpace(stepNameProp.stringValue))
        {
            headerText +=
                $" - {stepNameProp.stringValue}";
        }


        EditorGUI.LabelField(
            new Rect(
                x,
                y,
                width,
                EditorGUIUtility.singleLineHeight
            ),
            headerText,
            EditorStyles.boldLabel
        );


        y +=
            EditorGUIUtility.singleLineHeight
            + SectionSpacing;


        // -----------------------------------------------------
        // Basic Step Settings
        // -----------------------------------------------------

        DrawProperty(
            ref y,
            x,
            width,
            stepNameProp,
            "Step Name"
        );


        DrawProperty(
            ref y,
            x,
            width,
            imageProp,
            "Image"
        );


        DrawProperty(
            ref y,
            x,
            width,
            conditionTypeProp,
            "Advance Condition"
        );


        // -----------------------------------------------------
        // Condition-Specific Settings
        // -----------------------------------------------------

        if (conditionType ==
            EHRSequencePlayer.StepConditionType.Time)
        {
            DrawProperty(
                ref y,
                x,
                width,
                durationProp,
                "Duration"
            );
        }
        else if (conditionType ==
                 EHRSequencePlayer.StepConditionType.Action)
        {
            DrawProperty(
                ref y,
                x,
                width,
                actionNameProp,
                "Action Name"
            );
        }


        // -----------------------------------------------------
        // Scene Object Toggle
        // -----------------------------------------------------

        DrawProperty(
            ref y,
            x,
            width,
            showSceneObjectProp,
            "Show Scene Object"
        );


        // -----------------------------------------------------
        // Optional Scene Object Settings
        // -----------------------------------------------------

        if (showSceneObjectProp.boolValue)
        {
            y += SectionSpacing;


            // Section title.
            EditorGUI.LabelField(
                new Rect(
                    x,
                    y,
                    width,
                    EditorGUIUtility.singleLineHeight
                ),
                "Scene Object During This Step",
                EditorStyles.boldLabel
            );


            y +=
                EditorGUIUtility.singleLineHeight
                + EditorGUIUtility.standardVerticalSpacing;


            DrawProperty(
                ref y,
                x,
                width,
                sceneObjectProp,
                "Scene Object"
            );


            DrawProperty(
                ref y,
                x,
                width,
                animatorProp,
                "Animator"
            );


            DrawProperty(
                ref y,
                x,
                width,
                animatorTriggerProp,
                "Animator Trigger"
            );
        }
    }


    // =========================================================
    // Draw Individual Property
    // =========================================================

    private void DrawProperty(
        ref float y,
        float x,
        float width,
        SerializedProperty property,
        string label)
    {
        GUIContent content =
            new GUIContent(label);


        // Ask Unity how tall this property actually needs to be.
        float propertyHeight =
            EditorGUI.GetPropertyHeight(
                property,
                content,
                true
            );


        Rect propertyRect =
            new Rect(
                x,
                y,
                width,
                propertyHeight
            );


        EditorGUI.PropertyField(
            propertyRect,
            property,
            content,
            true
        );


        // Move down for the next property.
        y +=
            propertyHeight
            + EditorGUIUtility.standardVerticalSpacing;
    }


    // =========================================================
    // Calculate Step Height
    // =========================================================

    private float GetStepHeight(int index)
    {
        SerializedProperty stepProp =
            stepsProp.GetArrayElementAtIndex(index);


        SerializedProperty conditionTypeProp =
            stepProp.FindPropertyRelative("conditionType");

        SerializedProperty showSceneObjectProp =
            stepProp.FindPropertyRelative("showSceneObject");


        EHRSequencePlayer.StepConditionType conditionType =
            (EHRSequencePlayer.StepConditionType)
            conditionTypeProp.enumValueIndex;


        float height = TopPadding;


        // -----------------------------------------------------
        // Header
        // -----------------------------------------------------

        height +=
            EditorGUIUtility.singleLineHeight
            + SectionSpacing;


        // -----------------------------------------------------
        // Basic Properties
        // -----------------------------------------------------

        height += GetPropertyHeight(
            stepProp.FindPropertyRelative("stepName"),
            "Step Name"
        );


        height += GetPropertyHeight(
            stepProp.FindPropertyRelative("image"),
            "Image"
        );


        height += GetPropertyHeight(
            conditionTypeProp,
            "Advance Condition"
        );


        // -----------------------------------------------------
        // Condition-Specific Property
        // -----------------------------------------------------

        if (conditionType ==
            EHRSequencePlayer.StepConditionType.Time)
        {
            height += GetPropertyHeight(
                stepProp.FindPropertyRelative("duration"),
                "Duration"
            );
        }
        else if (conditionType ==
                 EHRSequencePlayer.StepConditionType.Action)
        {
            height += GetPropertyHeight(
                stepProp.FindPropertyRelative("actionName"),
                "Action Name"
            );
        }


        // -----------------------------------------------------
        // Show Scene Object
        // -----------------------------------------------------

        height += GetPropertyHeight(
            showSceneObjectProp,
            "Show Scene Object"
        );


        // -----------------------------------------------------
        // Optional Scene Object Properties
        // -----------------------------------------------------

        if (showSceneObjectProp.boolValue)
        {
            height += SectionSpacing;


            // Section title.
            height +=
                EditorGUIUtility.singleLineHeight
                + EditorGUIUtility.standardVerticalSpacing;


            height += GetPropertyHeight(
                stepProp.FindPropertyRelative("sceneObject"),
                "Scene Object"
            );


            height += GetPropertyHeight(
                stepProp.FindPropertyRelative("animator"),
                "Animator"
            );


            height += GetPropertyHeight(
                stepProp.FindPropertyRelative("iconAnimatorTrigger"),
                "Animator Trigger"
            );
        }


        // Bottom padding so one list element does not
        // visually run into the next one.
        height += BottomPadding;


        return height;
    }


    // =========================================================
    // Property Height Helper
    // =========================================================

    private float GetPropertyHeight(
        SerializedProperty property,
        string label)
    {
        GUIContent content =
            new GUIContent(label);


        float height =
            EditorGUI.GetPropertyHeight(
                property,
                content,
                true
            );


        return
            height
            + EditorGUIUtility.standardVerticalSpacing;
    }
}