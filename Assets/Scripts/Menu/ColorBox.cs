using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI.Extensions.ColorPicker;

public class ColorBox : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [System.Serializable]
    public class ColorBoxChangedEvent : UnityEvent<Color> { }
    [System.Serializable]
    public class ColorBoxCloseEvent : UnityEvent { }

    public Color color;
    public GameObject colorPickerPrefab;
    [SerializeField]
    public ColorBoxChangedEvent onColorChanged;
    [SerializeField]
    public ColorBoxCloseEvent onColorPickerClosed;
    GameObject gameObjectColorPicker;

    public Color Color
    {
        get
        {
            return color;
        }

        set
        {
            color = value;

            UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
            image.color = color;

            if (gameObjectColorPicker != null)
            {
                ColorPickerControl colorPickerControl = gameObjectColorPicker.GetComponent<ColorPickerControl>();
                colorPickerControl.CurrentColor = color;
            }
        }
    }

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
        bool isColorPickerUp = gameObjectColorPicker != null;

        DestroyAllOtherColorPickers();

        if (isColorPickerUp)
            return;

        // Opens up the color picker.
        gameObjectColorPicker = GameObject.Instantiate(colorPickerPrefab, transform);
        gameObjectColorPicker.name = "Color Picker";
        //gameObjectColorPicker.transform.rotation = Quaternion.identity;

        RectTransform rectTransform = transform as RectTransform;

        RectTransform parentRectTransform = transform.parent as RectTransform;
        if (parentRectTransform != null)
        {
            Vector3 newPosition = new Vector3(gameObjectColorPicker.transform.localPosition.x, gameObjectColorPicker.transform.localPosition.y, gameObjectColorPicker.transform.localPosition.z);
            newPosition.x = (parentRectTransform.sizeDelta.x / rectTransform.localScale.x) - (rectTransform.anchoredPosition.x / rectTransform.localScale.x) + (rectTransform.localScale.x / rectTransform.localScale.x);
            gameObjectColorPicker.transform.localPosition = newPosition;

            gameObjectColorPicker.transform.localScale = new Vector3(
                1.2f * transform.localScale.x * parentRectTransform.localScale.x,
                1.2f * transform.localScale.y * parentRectTransform.localScale.y,
                1.2f * transform.localScale.x * parentRectTransform.localScale.z
                );
        }
        else
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

        onColorChanged?.Invoke(color);
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
