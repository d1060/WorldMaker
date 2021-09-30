using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions.ColorPicker;

public class GradientSliderHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public class ValueSliderChangedEvent : UnityEvent { }

    Slider slider;
    public GameObject colorBox;
    public Transform handleTransform;
    public ValueSliderChangedEvent onValueChanged = new GradientSliderHandle.ValueSliderChangedEvent();
    public ValueSliderChangedEvent onMouseUp = new GradientSliderHandle.ValueSliderChangedEvent();
    public ValueSliderChangedEvent onDestroy = new GradientSliderHandle.ValueSliderChangedEvent();
    public bool inverseHeights = false;

    // Start is called before the first frame update
    void Start()
    {
        slider = transform.GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnValueChanged);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnDrag(PointerEventData eventData)
    {

    }

    public void OnPointerDown(PointerEventData eventData)
    {

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            GradientSliderHandleColorBox gradientSliderHandleColorBox = colorBox.GetComponent<GradientSliderHandleColorBox>();
            gradientSliderHandleColorBox.MarkedForDestroy = true;

            // Destroys this handle.
            Destroy(gameObject);
            Destroy(colorBox);
            onDestroy?.Invoke();
            return;
        }
        else
        {
            onMouseUp?.Invoke();
        }
    }

    public void OnValueChanged(float newValue)
    {
        GradientSliderHandleColorBox gradientSliderHandleColorBox = colorBox.GetComponent<GradientSliderHandleColorBox>();
        gradientSliderHandleColorBox.value = inverseHeights ? 1 - newValue : newValue;
        //colorBox.transform.localPosition = new Vector3(colorBox.transform.localPosition.x, handleTransform.localPosition.y * transform.localScale.y + 7, colorBox.transform.localPosition.z);
        colorBox.transform.position = new Vector3(colorBox.transform.position.x, handleTransform.position.y, colorBox.transform.position.z);
        onValueChanged?.Invoke();
    }
}
