using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine;

public class GradientSlider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameObject sliderHandleColorBoxPrefab;
    public GameObject colorPickerPrefab;
    public Map map;
    public bool inverseHeights;
    public bool isOcean;
    public int maxItems = 8;

    float[] stages = new float[0];
    Color32[] colors = new Color32[0];

    public float[] Stages { get { return stages; } }
    public Color32[] Colors { get { return colors; } }

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
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
            return;
        int numCreatedGradientPoints = GetNumCreatedGradientPoints();
        if (numCreatedGradientPoints >= maxItems)
            return;

        float height = rectTransform.rect.height - rectTransform.rect.width / 2;
        Vector2 point;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out point);
        float clickRatio = (point.y + height / 2) / height;
        UnityEngine.UI.Slider slider = GetComponent<UnityEngine.UI.Slider>();
        if (slider.direction == UnityEngine.UI.Slider.Direction.TopToBottom)
            clickRatio = 1 - clickRatio;
        CreateNewGradientPoint(clickRatio, new Color32(255, 255, 255, 255));
        GradientChanged();
    }

    public void CreateNewGradientPoint(float value, Color32 color)
    {
        // Creates a new Handle.
        GameObject newSliderHandle = GameObject.Instantiate(transform.gameObject, transform.parent);
        newSliderHandle.name = "Gradient Slider Handle";
        GradientSlider gradientSlider = newSliderHandle.GetComponent<GradientSlider>();
        Destroy(gradientSlider);
        GradientSliderHandle gradientSliderHandle = newSliderHandle.AddComponent<GradientSliderHandle>();
        gradientSliderHandle.onValueChanged.AddListener(GradientChanged);
        gradientSliderHandle.onMouseUp.AddListener(RedrawColors);
        gradientSliderHandle.onDestroy.AddListener(DestroyedSlider);
        gradientSliderHandle.inverseHeights = inverseHeights;

        UnityEngine.UI.Slider slider = newSliderHandle.GetComponent<UnityEngine.UI.Slider>();
        if (inverseHeights)
            slider.value = 1 - value;
        else
            slider.value = value;

        // Creates the color square
        GameObject colorBox = GameObject.Instantiate(sliderHandleColorBoxPrefab, transform.parent);
        colorBox.name = "Handle Slider Color Box";
        UnityEngine.UI.Image image = colorBox.GetComponent<UnityEngine.UI.Image>();
        image.color = color;

        GradientSliderHandleColorBox gradientSliderHandleColorBox = colorBox.GetComponent<GradientSliderHandleColorBox>();
        gradientSliderHandleColorBox.sliderParent = this;
        gradientSliderHandleColorBox.colorPickerPrefab = colorPickerPrefab;
        gradientSliderHandleColorBox.color = color;
        gradientSliderHandleColorBox.sliderHandle = newSliderHandle;
        gradientSliderHandleColorBox.inverseHeights = inverseHeights;
        if (inverseHeights)
            gradientSliderHandleColorBox.value = 1 - value;
        else
            gradientSliderHandleColorBox.value = value;
        gradientSliderHandleColorBox.onColorChanged.AddListener(GradientChanged);
        gradientSliderHandleColorBox.onColorPickerClosed.AddListener(RedrawColors);

        gradientSliderHandle.colorBox = colorBox;

        foreach (Transform childTransform in newSliderHandle.transform)
        {
            if (childTransform.name == "Background")
            {
                childTransform.gameObject.SetActive(false);
            }
            else if (childTransform.name == "Fill Area")
            {
                childTransform.gameObject.SetActive(false);
            }
            else if (childTransform.name == "Handle Slider Area")
            {
                childTransform.gameObject.SetActive(true);

                foreach (Transform childChildTransform in childTransform)
                {
                    if (childChildTransform.name == "Handle")
                    {
                        UnityEngine.UI.Image handleImage = childChildTransform.GetComponent<UnityEngine.UI.Image>();
                        gradientSliderHandleColorBox.handleTransform = childChildTransform;
                        gradientSliderHandle.handleTransform = childChildTransform;
                        //colorBox.transform.localPosition = new Vector3(colorBox.transform.localPosition.x, childChildTransform.localPosition.y * transform.localScale.y + 7, colorBox.transform.localPosition.z);
                        colorBox.transform.position = new Vector3(colorBox.transform.position.x - 0.1f, childChildTransform.position.y, colorBox.transform.position.z);
                        handleImage.color = color;
                    }
                }
            }
        }
    }

    public void GradientChanged()
    {
        SortedDictionary<float, Color> gradient = new SortedDictionary<float, Color>();

        GradientSliderHandleColorBox[] gradientSliderHandleColorBoxes = transform.parent.GetComponentsInChildren<GradientSliderHandleColorBox>();
        foreach (GradientSliderHandleColorBox gradientSliderHandleColorBox in gradientSliderHandleColorBoxes)
        {
            if (gradientSliderHandleColorBox.sliderParent != this || gradientSliderHandleColorBox.MarkedForDestroy)
                continue;

            float value = gradientSliderHandleColorBox.value;
            Color color = gradientSliderHandleColorBox.color;

            while (gradient.ContainsKey(value))
            {
                value += 0.000001f;
            }

            gradient.Add(value, color);
        }

        stages = new float[gradient.Count];
        colors = new Color32[gradient.Count];

        int i = 0;
        foreach (KeyValuePair<float, Color> kvp in gradient)
        {
            stages[i] = kvp.Key;
            colors[i] = kvp.Value;
            i++;
        }

        if (isOcean)
            map.OnOnceanGradientChanged(this);
        else
            map.OnLandGradientChanged(this);
    }

    public void RedrawColors()
    {
        map.ReGenerateLandColors();
    }

    public void DestroyedSlider()
    {
        GradientChanged();
    }

    public int GetNumCreatedGradientPoints()
    {
        int count = 0;
        GradientSliderHandleColorBox[] gradientSliderHandleColorBoxes = transform.parent.GetComponentsInChildren<GradientSliderHandleColorBox>();
        foreach (GradientSliderHandleColorBox gradientSliderHandleColorBox in gradientSliderHandleColorBoxes)
        {
            if (gradientSliderHandleColorBox.sliderParent == this)
                count++;
        }
        return count;
    }
}
