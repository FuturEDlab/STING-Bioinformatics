using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
{
    [Header("Slider Setup")]
    [SerializeField] private Slider _slider;

    [SerializeField, Range(0f, 1f)]
    private float sliderValue;

    public bool CurrentValue { get; private set; }

    private bool _previousValue;

    [Header("Toggle Background")]
    [SerializeField] private Image backgroundImageReference;
    [SerializeField] private Sprite toggleOnSprite;
    [SerializeField] private Sprite toggleOffSprite;

    [Header("Animation")]
    [SerializeField, Range(0f, 1f)]
    private float animationDuration = 0.5f;

    [SerializeField]
    private AnimationCurve slideEase =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine _animateSliderCoroutine;

    [Header("Events")]
    [SerializeField] private UnityEvent onToggleOn;
    [SerializeField] private UnityEvent onToggleOff;

    protected Action transitionEffect;

    protected virtual void OnValidate()
    {
        CurrentValue = sliderValue >= 0.5f;

        if (_slider != null)
        {
            ConfigureSlider();
            _slider.value = sliderValue;
        }

        UpdateBackgroundSprite();
    }

    protected virtual void Awake()
    {
        if (_slider == null)
        {
            Debug.LogError(
                "No Slider has been assigned to ToggleSwitch.",
                this
            );

            enabled = false;
            return;
        }

        ConfigureSlider();

        CurrentValue = sliderValue >= 0.5f;
        sliderValue = CurrentValue ? 1f : 0f;
        _slider.value = sliderValue;

        UpdateBackgroundSprite();
    }

    private void ConfigureSlider()
    {
        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.wholeNumbers = false;
        _slider.interactable = false;
        _slider.transition = Selectable.Transition.None;

        ColorBlock sliderColors = _slider.colors;
        sliderColors.disabledColor = Color.white;
        _slider.colors = sliderColors;
    }

    private void UpdateBackgroundSprite()
    {
        if (backgroundImageReference == null)
            return;

        Sprite spriteToUse = CurrentValue
            ? toggleOnSprite
            : toggleOffSprite;

        if (spriteToUse != null)
            backgroundImageReference.sprite = spriteToUse;
    }

    public void ToggleFromHandle()
    {
        SetStateAndStartAnimation(!CurrentValue);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleFromHandle();
    }

    public void ToggleByGroupManager(bool valueToSetTo)
    {
        SetStateAndStartAnimation(valueToSetTo);
    }

    private void SetStateAndStartAnimation(bool state)
    {
        _previousValue = CurrentValue;
        CurrentValue = state;

        if (_animateSliderCoroutine != null)
            StopCoroutine(_animateSliderCoroutine);

        _animateSliderCoroutine = StartCoroutine(AnimateSlider());
    }

    private IEnumerator AnimateSlider()
    {
        float startValue = _slider.value;
        float endValue = CurrentValue ? 1f : 0f;
        float time = 0f;

        if (animationDuration <= 0f)
        {
            sliderValue = endValue;
            _slider.value = endValue;

            FinishToggleChange();

            _animateSliderCoroutine = null;
            yield break;
        }

        while (time < animationDuration)
        {
            time += Time.deltaTime;

            float progress = Mathf.Clamp01(
                time / animationDuration
            );

            float lerpFactor = slideEase.Evaluate(progress);

            sliderValue = Mathf.Lerp(
                startValue,
                endValue,
                lerpFactor
            );

            _slider.value = sliderValue;

            transitionEffect?.Invoke();

            yield return null;
        }

        sliderValue = endValue;
        _slider.value = endValue;

        FinishToggleChange();

        _animateSliderCoroutine = null;
    }

    private void FinishToggleChange()
    {
        UpdateBackgroundSprite();

        if (_previousValue == CurrentValue)
            return;

        if (CurrentValue)
            onToggleOn?.Invoke();
        else
            onToggleOff?.Invoke();
    }
}