using UnityEngine;

public class CaptionTester : MonoBehaviour
{
    private void Start()
    {
        CaptionManager.Instance.ShowCaption(
            "This is a very long caption designed to test paging. The font should remain large and readable. " +
            "When the caption fills the bar completely, the remaining text should appear as the next caption page. " +
            "If you can read this across multiple pages, the system is working correctly."
        );
    }
}