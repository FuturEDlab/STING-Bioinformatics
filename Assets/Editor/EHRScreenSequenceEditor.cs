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
            float lines = 1; // first row: sprite + trigger
            var triggerProp = prop.FindPropertyRelative("trigger");
            if (triggerProp != null)
            {
                // second row for duration or action
                lines += 1;
            }
            // showIcon checkbox occupies one additional row
            var showIconProp = prop.FindPropertyRelative("showIcon");
                if (showIconProp != null)
                {
                    lines += 1;
                    if (showIconProp.boolValue)
                    {
                        // icon prefab + icon key + animator trigger (three additional rows)
                        lines += 3;
                    }
                }

            return EditorGUIUtility.singleLineHeight * lines + 16;
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
            var showIconProp = element.FindPropertyRelative("showIcon");
            var iconPrefabProp = element.FindPropertyRelative("iconPrefab");
            var iconAnimProp = element.FindPropertyRelative("iconAnimatorTrigger");
            var iconKeyProp = element.FindPropertyRelative("iconKey");

            // First row: sprite (small) on left, trigger on right
            float spriteWidth = 120f;
            Rect spriteRect = new Rect(rect.x, rect.y, spriteWidth, lineHeight);
            Rect triggerRect = new Rect(rect.x + spriteWidth + 8, rect.y, rect.width - spriteWidth - 8, lineHeight);
            // Draw object field for sprite so it's easy to pick
            spriteProp.objectReferenceValue = (Sprite)EditorGUI.ObjectField(spriteRect, spriteProp.objectReferenceValue, typeof(Sprite), false);
            EditorGUI.PropertyField(triggerRect, triggerProp, new GUIContent("Trigger"));

            // Next row: duration or action
            float y = rect.y + lineHeight + 4;
            Rect r = new Rect(rect.x, y, rect.width, lineHeight);

            TriggerType trigger = (TriggerType)triggerProp.enumValueIndex;
            if (trigger == TriggerType.Timer)
            {
                EditorGUI.PropertyField(r, durationProp, new GUIContent("Duration"));
                y += lineHeight + 4;
            }
            else if (trigger == TriggerType.Action)
            {
                EditorGUI.PropertyField(r, actionProp, new GUIContent("Action Name"));
                y += lineHeight + 4;
            }

            r = new Rect(rect.x, y, rect.width, lineHeight);
            EditorGUI.PropertyField(r, showIconProp, new GUIContent("Show Icon"));
            y += lineHeight + 4;
            if (showIconProp != null && showIconProp.boolValue)
            {
                r = new Rect(rect.x, y, rect.width, lineHeight);
                EditorGUI.PropertyField(r, iconPrefabProp, new GUIContent("Icon Prefab"));
                y += lineHeight + 4;

                r = new Rect(rect.x, y, rect.width, lineHeight);
                EditorGUI.PropertyField(r, iconKeyProp, new GUIContent("Icon Key (scene)")); 
                y += lineHeight + 4;

                r = new Rect(rect.x, y, rect.width, lineHeight);
                EditorGUI.PropertyField(r, iconAnimProp, new GUIContent("Icon Animator Trigger"));
                y += lineHeight + 4;
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
