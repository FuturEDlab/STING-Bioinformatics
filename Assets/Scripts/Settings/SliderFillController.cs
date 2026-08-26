using UnityEngine;
using UnityEngine.UI;

public class SliderFillController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;

    [Header("Slider Layout")]
    [SerializeField] private float leftPadding = 50f;
    [SerializeField] private float rightPadding = 50f;

    private void Start()
    {
        // Make sure the fill image matches the slider
        // when this UI first appears.
        //UpdateFill(slider.value);
    }

    public void UpdateFill(float value)
    {
        float fillWidth = fillImage.rectTransform.rect.width;

        float minFill = leftPadding / fillWidth;
        float maxFill = 1f - (rightPadding / fillWidth);

        fillImage.fillAmount = Mathf.Lerp(
            minFill,
            maxFill,
            slider.normalizedValue
        );
    }
}
