using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(EHRScreenSequence))]
public class EHRScreenSequenceEditor : Editor
{
    ReorderableList list;

    void OnEnable()
    {
        list = new ReorderableList(serializedObject,
            serializedObject.FindProperty("entries"),
            true, true, true, true);

        list.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "EHR Screen Entries");
        };

        list.elementHeightCallback = (int index) =>
        {
            var prop = list.serializedProperty.GetArrayElementAtIndex(index);
            float lines = 2; // sprite + trigger
            var triggerProp = prop.FindPropertyRelative("trigger");
            if (triggerProp != null)
            {
                if ((TriggerType)triggerProp.enumValueIndex == TriggerType.Timer) lines += 1;
                if ((TriggerType)triggerProp.enumValueIndex == TriggerType.Action) lines += 1;
            }
            return EditorGUIUtility.singleLineHeight * lines + 12;
        };

        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 4;
            float lineHeight = EditorGUIUtility.singleLineHeight;

            var spriteProp = element.FindPropertyRelative("sprite");
            var triggerProp = element.FindPropertyRelative("trigger");
            var durationProp = element.FindPropertyRelative("duration");
            var actionProp = element.FindPropertyRelative("actionName");

            Rect r = new Rect(rect.x, rect.y, rect.width, lineHeight);
            EditorGUI.PropertyField(r, spriteProp, GUIContent.none);

            r.y += lineHeight + 4;
            EditorGUI.PropertyField(r, triggerProp);

            TriggerType trigger = (TriggerType)triggerProp.enumValueIndex;
            if (trigger == TriggerType.Timer)
            {
                r.y += lineHeight + 4;
                EditorGUI.PropertyField(r, durationProp);
            }
            else if (trigger == TriggerType.Action)
            {
                r.y += lineHeight + 4;
                EditorGUI.PropertyField(r, actionProp);
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        list.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}
