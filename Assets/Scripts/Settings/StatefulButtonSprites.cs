using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Changes a UI element's sprite based on its hover and selected states.
///
/// Available visual states:
/// - Default
/// - Hover
/// - Selected
/// - Selected Hover
/// </summary>
[DisallowMultipleComponent]
public class StatefulButtonSprites : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Required References")]

    // Assign the Button or Toggle that this script controls.
    //
    // This reference is used to disable Unity's built-in transition system
    // so it does not overwrite the sprites assigned by this script.
    [SerializeField] private Selectable selectable;

    // Assign the Image whose sprite should change.
    //
    // This is usually the background Image used as the Button's
    // Target Graphic.
    [SerializeField] private Image targetImage;

    [Header("Optional Toggle Reference")]

    // Assign this only when the UI element is a Toggle.
    //
    // When assigned, the Toggle's isOn value automatically controls
    // whether this element is selected.
    //
    // Leave this empty when using a regular Button.
    [SerializeField] private Toggle toggle;

    [Header("State Sprites")]

    // Displayed when the UI element is not selected or hovered.
    [SerializeField] private Sprite defaultSprite;

    // Displayed when the pointer or XR ray is hovering over the UI element.
    [SerializeField] private Sprite hoverSprite;

    // Displayed when the UI element is selected but not hovered.
    [SerializeField] private Sprite selectedSprite;

    // Displayed when the UI element is both selected and hovered.
    [SerializeField] private Sprite selectedHoverSprite;

    [Header("Optional Disabled State")]
    [SerializeField] private Sprite disabledSprite;

    // Tracks whether the pointer or XR ray is currently over this element.
    private bool isHovered;

    // Tracks whether this element is persistently selected.
    private bool isSelected;
    private bool isDisabled;

    /// <summary>
    /// Allows other scripts to check whether this element is selected.
    /// </summary>
    public bool IsSelected => isSelected;

    /// <summary>
    /// Allows other scripts to check whether this element is disabled.
    /// </summary>
    public bool IsDisabled => isDisabled;

    private void Awake()
    {
        // All required references must be manually assigned.
        if (selectable == null)
        {
            Debug.LogError(
                $"{nameof(StatefulButtonSprites)} on '{name}' is missing " +
                "its Selectable reference.",
                this
            );

            enabled = false;
            return;
        }

        if (targetImage == null)
        {
            Debug.LogError(
                $"{nameof(StatefulButtonSprites)} on '{name}' is missing " +
                "its Target Image reference.",
                this
            );

            enabled = false;
            return;
        }

        // Disable Unity's built-in Color Tint or Sprite Swap transition.
        //
        // Otherwise, Unity's transition system could overwrite the sprite
        // being displayed by this script.
        selectable.transition = Selectable.Transition.None;

        // A Toggle reference is optional.
        //
        // When one is assigned, its current isOn value becomes the
        // starting selected state.
        if (toggle != null)
        {
            isSelected = toggle.isOn;
        }

        selectable.interactable = !isDisabled;
        RefreshVisual();
    }

    private void OnEnable()
    {
        // Awake may not have run yet when the component is first enabled,
        // so make sure the required references exist before continuing.
        if (selectable == null || targetImage == null)
        {
            return;
        }

        // Subscribe to the Toggle only while this component is enabled.
        if (toggle != null)
        {
            // Synchronize this script with the Toggle's current value.
            isSelected = toggle.isOn;

            // Remove the listener first to prevent duplicate listeners.
            toggle.onValueChanged.RemoveListener(SetSelected);
            toggle.onValueChanged.AddListener(SetSelected);
        }

        RefreshVisual();
    }

    private void OnDisable()
    {
        // Remove the listener when this component is disabled.
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(SetSelected);
        }

        // Hover is temporary, so clear it when the object is disabled.
        isHovered = false;
    }

    /// <summary>
    /// Called when a mouse pointer, controller pointer, or XR ray
    /// begins hovering over this UI element.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDisabled)
            return;

        isHovered = true;
        RefreshVisual();
    }

    /// <summary>
    /// Called when a mouse pointer, controller pointer, or XR ray
    /// stops hovering over this UI element.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDisabled)
            return;

        isHovered = false;
        RefreshVisual();
    }

    /// <summary>
    /// Changes the persistent selected state.
    ///
    /// Toggles call this automatically through onValueChanged.
    /// Regular Buttons can call this through another script or
    /// through their OnClick event.
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshVisual();
    }

    /// <summary>
    /// Sets the button's disabled state.
    /// </summary>
    public void SetDisabled(bool disabled)
    {
        isDisabled = disabled;
        if (selectable != null)
        {
            selectable.interactable = !disabled;
        }

        RefreshVisual();
    }

    /// <summary>
    /// Chooses the correct sprite based on the current combination
    /// of selected and hovered states.
    /// </summary>
    public void RefreshVisual()
    {
        if (targetImage == null)
        {
            return;
        }

        if (isDisabled)
        {
            if (disabledSprite != null)
            {
                SetSprite(disabledSprite);
                return;
            }

            SetSprite(defaultSprite);
            return;
        }

        // Selected Hover:
        // The element is selected and currently being hovered.
        if (isSelected && isHovered)
        {
            SetSprite(selectedHoverSprite);
        }
        // Selected:
        // The element is selected but is not currently being hovered.
        else if (isSelected)
        {
            SetSprite(selectedSprite);
        }
        // Hover:
        // The element is being hovered but is not selected.
        else if (isHovered)
        {
            SetSprite(hoverSprite);
        }
        // Default:
        // The element is neither selected nor hovered.
        else
        {
            SetSprite(defaultSprite);
        }
    }

    /// <summary>
    /// Applies a sprite to the target Image.
    ///
    /// The null check prevents an unassigned sprite from removing
    /// the Image's current sprite.
    /// </summary>
    private void SetSprite(Sprite newSprite)
    {
        if (newSprite != null)
        {
            targetImage.sprite = newSprite;
        }
    }

    /// <summary>
    /// Updates the state sprites used by this component.
    /// </summary>
    public void SetStateSprites(
        Sprite defaultSprite,
        Sprite hoverSprite,
        Sprite selectedSprite,
        Sprite selectedHoverSprite,
        Sprite disabledSprite)
    {
        this.defaultSprite = defaultSprite;
        this.hoverSprite = hoverSprite;
        this.selectedSprite = selectedSprite;
        this.selectedHoverSprite = selectedHoverSprite;
        this.disabledSprite = disabledSprite;
        RefreshVisual();
    }
}