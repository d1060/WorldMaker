using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI.Extensions.ColorPicker;

public class GradientSliderHandleColorBox : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public class ColorSliderChangedEvent : UnityEvent { }

    public GameObject colorPickerPrefab;
    public GameObject sliderHandle;
    public Color color;
    public Transform handleTransform;
    public float value;
    public ColorSliderChangedEvent onColorChanged = new ColorSliderChangedEvent();
    public ColorSliderChangedEvent onColorPickerClosed = new ColorSliderChangedEvent();
    public bool inverseHeights;
    public GradientSlider sliderParent;
    bool markedForDestroy = false;
    GameObject gameObjectColorPicker = null;


    public bool MarkedForDestroy { get { return markedForDestroy; } set { markedForDestroy = value; } }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerDown(PointerEventData eventData)
    {

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Destroys this handle.
            markedForDestroy = true;
            Destroy(gameObject);
            Destroy(sliderHandle);
            return;
        }

        bool isColorPickerUp = gameObjectColorPicker != null;

        DestroyAllOtherColorPickers();

        if (isColorPickerUp)
            return;

        // Opens up the color picker.
        gameObjectColorPicker = GameObject.Instantiate(colorPickerPrefab, transform);
        gameObjectColorPicker.name = "Color Picker";
        //gameObjectColorPicker.transform.rotation = Quaternion.identity;
        gameObjectColorPicker.transform.localPosition = new Vector3(80, 0, 0);
        ColorPickerControl colorPickerControl = gameObjectColorPicker.GetComponent<ColorPickerControl>();
        colorPickerControl.CurrentColor = color;
        colorPickerControl.onValueChanged.AddListener(ColorPickerColorChanged);
        colorPickerControl.onClosed.AddListener(ColorPickerClosed);
    }

    void ColorPickerColorChanged(Color color)
    {
        this.color = color;

        UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
        image.color = color;

        UnityEngine.UI.Image handleImage = handleTransform.GetComponent<UnityEngine.UI.Image>();
        handleImage.color = color;

        onColorChanged?.Invoke();
    }

    void ColorPickerClosed()
    {
        onColorPickerClosed?.Invoke();
    }

    void DestroyAllOtherColorPickers()
    {
        Transform parentTransform = transform.parent;
        ColorPickerControl[] colorPickerControls = parentTransform.GetComponentsInChildren<ColorPickerControl>();
        foreach (ColorPickerControl colorPickerControl in colorPickerControls)
        {
            Destroy(colorPickerControl.gameObject);
        }
    }

    public void DestroyColorPicker()
    {
        Destroy(gameObjectColorPicker);
    }
}
