using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

public class MapSliderField : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerUpHandler
{
    Slider slider;
    public TMP_InputField valueText;
    public bool isPercent;
    public ToggleSliderEvent OnSliderChanged;
    public ToggleSliderSelectEvent OnEnter;
    public ToggleSliderSelectEvent OnLeave;
    public ToggleSliderSelectEvent OnMouseUp;
    public int decimalDigits = 2;
    //public float maxValue = 1;
    //public float minValue = 0;
    string textFormat;

    // Start is called before the first frame update
    void Start()
    {
        //slider = GetComponent<Slider>();
        //slider.minValue = minValue;
        //slider.maxValue = maxValue;
        textFormat = "##0";
        if (decimalDigits > 0)
        {
            textFormat += ".";
            for (int i = 0; i < decimalDigits; i++)
            {
                textFormat += "#";
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Init(float initialValue)
    {
        slider = GetComponent<Slider>();
        slider.value = initialValue;
        if (valueText == null)
        {
            TMP_InputField inputField = GetComponentInChildren<TMP_InputField>();
            if (inputField != null)
            {
                valueText = inputField;
            }
        }
        if (valueText != null)
        {
            valueText.text = (slider.value * (isPercent ? 100 : 1)).ToString(textFormat);
            if (isPercent)
                valueText.text += "%";
        }
    }

    public void OnChanged()
    {
        float sValue = slider.value;
        if (valueText == null)
        {
            TMP_InputField inputField = GetComponentInChildren<TMP_InputField>();
            if (inputField != null)
            {
                valueText = inputField;
            }
        }
        if (valueText != null)
        {
            valueText.text = (slider.value * (isPercent ? 100 : 1)).ToString(textFormat);
            if (isPercent)
                valueText.text += "%";
        }
        OnSliderChanged?.Invoke(slider.value);
    }

    public void OnTextChanged()
    {
        string levelText = valueText.text;
        if (levelText.EndsWith("%"))
            levelText = levelText.Remove(levelText.Length - 1, 1);

        float sliderValue = float.Parse(levelText);
        if (isPercent)
            levelText += "%";
        valueText.text = levelText;

        sliderValue /= (isPercent ? 100 : 1);
        slider.value = sliderValue;
        OnSliderChanged?.Invoke(slider.value);
    }

    public void OnSelect(BaseEventData data)
    {
        OnEnter?.Invoke();
    }

    public void OnDeselect(BaseEventData data)
    {
        OnLeave?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnMouseUp?.Invoke();
    }
}

[Serializable]
public class ToggleSliderEvent : UnityEvent<float> { }

[Serializable]
public class ToggleSliderSelectEvent : UnityEvent { }
