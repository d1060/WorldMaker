using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class WaterLevelSlider : MonoBehaviour, IPointerUpHandler
{
    public Map map;
    Slider slider;
    public TMP_InputField valueText;
    public ToggleSliderSelectEvent OnMouseUp;

    // Start is called before the first frame update
    void Start()
    {
        slider = GetComponent<Slider>();
        slider.value = map.textureSettings.waterLevel;
        valueText.text = (slider.value * 100).ToString("##0.#") + "%";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnWaterLevelChanged()
    {
        map.textureSettings.waterLevel = slider.value;
        valueText.text = (slider.value * 100).ToString("##0.#") + "%";
        OnMouseUp?.Invoke();
    }

    public void OnWaterLevelTextChanged()
    {
        string waterLevelText = valueText.text;
        if (waterLevelText.EndsWith("%"))
            waterLevelText = waterLevelText.Remove(waterLevelText.Length - 1, 1);

        float sliderValue = float.Parse(waterLevelText);
        waterLevelText += "%";
        valueText.text = waterLevelText;

        sliderValue /= 100;
        slider.value = sliderValue;
        map.textureSettings.waterLevel = sliderValue;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnMouseUp?.Invoke();
    }
}
