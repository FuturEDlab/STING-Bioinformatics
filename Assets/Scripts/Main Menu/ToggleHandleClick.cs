using System.Drawing;
using UnityEngine;
using UnityEngine.EventSystems;

public class NewMonoBehaviourScript : MonoBehaviour, IPointerClickHandler
{

    [SerializeField] private ToggleSwitch toggleSwitch;

    public void OnPointerClick(PointerEventData eventData)
    {
        toggleSwitch.ToggleFromHandle();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
