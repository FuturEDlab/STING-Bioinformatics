using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Repairs three unassigned references on the Question Panels prefab. All three fail
/// silently — every call site is guarded with a null check — which is why the panel could
/// look wired up and still not work:
///
///  * QP — the container holding the five pages. Everything that shows or hides the panel
///    goes through it, so with it empty ShowSingleQuestion and ClosePanel are both no-ops.
///    The Question Page is then left in whatever state it was saved in.
///
///  * Answer Selection Group — the group that draws the highlight on the answer you picked.
///    Empty, the manager falls back to looking for a group on each answer BUTTON, but the
///    group lives on their parent, so it finds none and no answer ever looks selected.
///
///  * The page active states — every page should start switched off, and the Question Page
///    is currently saved switched on, so the quiz sits live in the room from scene load.
///
/// Edits the prefab asset, so the fix reaches every scene using it.
/// </summary>
public static class QuestionPanelWiringFix
{
    private const string PrefabPath = "Assets/Prefabs/UI/Question Prefab/Question Panels.prefab";

    [MenuItem("Tools/STING/Fix Question Panel Wiring")]
    private static void Run()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

        if (root == null)
        {
            EditorUtility.DisplayDialog("Question panel wiring", $"Could not open {PrefabPath}.", "OK");
            return;
        }

        try
        {
            var manager = root.GetComponentInChildren<QuestionPanelManager>(true);

            if (manager == null)
            {
                EditorUtility.DisplayDialog("Question panel wiring", "No QuestionPanelManager on that prefab.", "OK");
                return;
            }

            var log = new System.Text.StringBuilder();
            log.AppendLine("=== QUESTION PANEL WIRING ===");

            bool changed = false;
            changed |= AssignContainer(manager, log);
            changed |= AssignAnswerGroup(manager, log);
            changed |= ResetPageStates(manager, log);

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                log.AppendLine("Prefab saved.");
            }
            else
            {
                log.AppendLine("Nothing needed changing.");
            }

            Debug.Log(log.ToString());
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool AssignContainer(QuestionPanelManager manager, System.Text.StringBuilder log)
    {
        if (manager.QP != null)
        {
            log.AppendLine($"  ok     QP -> '{manager.QP.name}'");
            return false;
        }

        // Derived rather than hard-coded: the container is simply whatever the pages hang
        // off, so this keeps working if the prefab is reorganised.
        GameObject page = manager.QPQuestion != null ? manager.QPQuestion : manager.QPTitle;

        if (page == null || page.transform.parent == null)
        {
            log.AppendLine("PROBLEM  QP is empty and there is no page to derive the container from. Assign it by hand.");
            return false;
        }

        manager.QP = page.transform.parent.gameObject;
        log.AppendLine($"  FIXED  QP was empty — showing and hiding the panel did nothing at all. Set to '{manager.QP.name}'.");
        return true;
    }

    private static bool AssignAnswerGroup(QuestionPanelManager manager, System.Text.StringBuilder log)
    {
        if (manager.answerSelectionGroup != null)
        {
            log.AppendLine($"  ok     answer selection group -> '{manager.answerSelectionGroup.name}'");
            return false;
        }

        if (manager.answerButtons == null || manager.answerButtons.Length == 0 || manager.answerButtons[0] == null)
        {
            log.AppendLine("  note   no answer buttons assigned, so no group could be found.");
            return false;
        }

        var group = manager.answerButtons[0].GetComponentInParent<ButtonSelectionGroup>(true);

        if (group == null)
        {
            log.AppendLine("PROBLEM  no ButtonSelectionGroup above the answer buttons. Picking an answer will never highlight it.");
            return false;
        }

        manager.answerSelectionGroup = group;
        log.AppendLine($"  FIXED  answer selection group was empty — the manager was looking for a group on each answer button, but it lives on '{group.name}', so picking an answer never highlighted it. Assigned.");
        return true;
    }

    private static bool ResetPageStates(QuestionPanelManager manager, System.Text.StringBuilder log)
    {
        GameObject[] pages =
        {
            manager.QP,
            manager.QPTitle,
            manager.QPMajor,
            manager.QPQuestion,
            manager.QPSummary,
            manager.QPExit,
        };

        bool changed = false;

        for (int i = 0; i < pages.Length; i++)
        {
            GameObject page = pages[i];

            if (page == null || !page.activeSelf)
                continue;

            // Saved switched on, a page shows in the room from the moment the scene loads —
            // and the manager only ever switches pages ON, so nothing would ever hide it.
            page.SetActive(false);
            log.AppendLine($"  FIXED  '{page.name}' was saved switched on, so it was visible in the room from scene load. Switched off; the manager turns it on when it needs it.");
            changed = true;
        }

        return changed;
    }
}
