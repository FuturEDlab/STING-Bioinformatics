using UnityEngine;

/// <summary>
/// Marks a scene object as the UI panel for one scenario question. Step assets are
/// ScriptableObjects and cannot hold scene references, so the panel is bound to its
/// <see cref="QuestionSO"/> in the ScenarioController's Inspector (Context ▸ Question
/// Panels) and resolved at runtime through <see cref="ScenarioContext.GetQuestionPanel"/>.
///
/// Put this on the panel prefab's ROOT — the object that should be switched on and off.
/// </summary>
public class QuestionPanel : MonoBehaviour
{
    [Tooltip("Object toggled on/off when this question is shown. Leave empty to use this GameObject.")]
    [SerializeField] private GameObject root;

    [Tooltip("The Quiz that draws this panel's question text and answer buttons. Auto-found in children when empty.")]
    [SerializeField] private Quiz quiz;

    [Tooltip("Hide the panel on scene start so only the question currently being asked is visible.")]
    [SerializeField] private bool hideOnAwake = true;

    public GameObject Root => root != null ? root : gameObject;
    public Quiz Quiz => quiz;

    private void Reset()
    {
        root = gameObject;
        quiz = GetComponentInChildren<Quiz>(true);
    }

    private void Awake()
    {
        // Include inactive children: the Quiz canvas is usually off until the panel shows.
        if (quiz == null)
            quiz = GetComponentInChildren<Quiz>(true);

        // Awake runs before any Start, so the controller never sees a stale visible panel.
        if (hideOnAwake)
            Hide();
    }

    public void Show()
    {
        GameObject go = Root;
        if (go != null && !go.activeSelf)
            go.SetActive(true);
    }

    public void Hide()
    {
        GameObject go = Root;
        if (go != null && go.activeSelf)
            go.SetActive(false);
    }
}
