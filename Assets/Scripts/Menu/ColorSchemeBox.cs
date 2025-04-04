using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColorSchemeBox : MonoBehaviour
{
    public enum ColorSchemeType
    {
        LAND,
        WATER
    };

    public ColorSchemeType colorSchemeType;
    public Transform colorSchemePanelTransform;
    public Map map;

    string schemeLabel;
    int schemeIndex;

    void Awake()
    {

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        switch (colorSchemeType)
        {
            case ColorSchemeType.LAND:
                schemeLabel = TextureManager.instance.Settings.landColorLabel;
                schemeIndex = TextureManager.instance.Settings.landColorIndex;
                break;
            case ColorSchemeType.WATER:
                schemeLabel = TextureManager.instance.Settings.waterColorLabel;
                schemeIndex = TextureManager.instance.Settings.waterColorIndex;
                break;
        }

        ColorScheme cs = ColorSchemes.instance.GetColorScheme(schemeLabel, schemeIndex);
        if (cs == null) return;

        Image image = transform.GetComponent<Image>();
        if (image == null) return;

        image.sprite = cs.GetSprite();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnMouseDown()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        RectTransform parentRectTransform = transform.parent.GetComponent<RectTransform>();
        RectTransform colorSchemePanelRectTransform = colorSchemePanelTransform.GetComponent<RectTransform>();
        colorSchemePanelRectTransform.anchoredPosition = new Vector2(parentRectTransform.anchoredPosition.x + parentRectTransform.sizeDelta.x + 6, rectTransform.anchoredPosition.y - rectTransform.sizeDelta.y);
        ColorSchemePanel colorSchemePanel = colorSchemePanelRectTransform.GetComponent<ColorSchemePanel>();
        colorSchemePanel.source = transform;
        colorSchemePanel.ActivateLabel(schemeLabel);

        MainMenu[] otherSubMenus = transform.parent.GetComponentsInChildren<MainMenu>();
        foreach (MainMenu otherMenu in otherSubMenus)
        {
            if (otherMenu != this)
            {
                otherMenu.ShiftMenu();
                otherMenu.ShiftMenuOut();
            }
        }
    }

    public void SetColorScheme(int index, string label)
    {
        schemeLabel = label;
        schemeIndex = index;

        ColorScheme cs = ColorSchemes.instance.GetColorScheme(label, index);
        if (cs == null) return;

        Image image = transform.GetComponent<Image>();
        if (image == null) return;

        image.sprite = cs.GetSprite();

        switch (colorSchemeType)
        {
            case ColorSchemeType.LAND:
                TextureManager.instance.Settings.landColorLabel = label;
                TextureManager.instance.Settings.landColorIndex = index;
                break;
            case ColorSchemeType.WATER:
                TextureManager.instance.Settings.waterColorLabel = label;
                TextureManager.instance.Settings.waterColorIndex = index;
                break;
        }

        MapData.instance.Save();
        map.UpdateSurfaceMaterialProperties();
    }
}
