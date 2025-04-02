using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColorSchemePanel : MonoBehaviour
{
    public Transform source;
    Vector3 originalPosition;
    public Transform colorSchemePanelTransform;
    public GameObject tabLabelPrefab;
    int schemesPerRow = 5;
    int scrollBarWidth = 20;
    int schemesPadding = 6;
    int schemesHeight = 32;
    bool colorSchemePanelBuilt = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalPosition = transform.position;
        SetupColorSchemePanel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ReturnToOrigin()
    {
        transform.position = originalPosition;
    }

    public void ActivateLabel(string label)
    {
        float minHeight = ColorSchemes.instance.ActivateLabel(label);

        Transform viewportContent = colorSchemePanelTransform.GetChildNamed_Recursive("Content");
        if (viewportContent == null) return;

        RectTransform rectTransform = viewportContent.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, -minHeight);
    }

    void SetupColorSchemePanel()
    {
        if (colorSchemePanelTransform == null || colorSchemePanelBuilt) return;

        int i = 0;
        foreach (KeyValuePair<string, List<ColorScheme>> kvp in ColorSchemes.instance.Schemes)
        {
            string schemeName = kvp.Key;
            List<ColorScheme> schemes = kvp.Value;
            CreateSchemeLabel(i, schemeName);
            int s = 0;
            foreach (ColorScheme cs in schemes)
            {
                CreateSchemePanel(cs, s, i, schemeName);
                s++;
            }
            i++;
        }
        SetupLabelExclusivities();
        colorSchemePanelBuilt = true;
    }

    void CreateSchemeLabel(int index, string label)
    {
        GameObject gameObject = Object.Instantiate(tabLabelPrefab, colorSchemePanelTransform);
        gameObject.name = "ColorScheme " + index + " Label";
        gameObject.transform.position = new Vector3(52.5f + 105 * index, -31, 0);
        gameObject.transform.localPosition = new Vector3(52.5f + 105 * index, -31, 0);

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 16);
        rectTransform.localScale = new Vector3(1, 1, 1);

        TMP_Text textMeshPro = gameObject.GetComponentInChildren<TMP_Text>();
        textMeshPro.text = label;
        //textMeshPro.fontSize = 140;
        textMeshPro.fontStyle = FontStyles.Normal;
        textMeshPro.alignment = TextAlignmentOptions.Center;
        textMeshPro.overflowMode = TextOverflowModes.Overflow;

        ButtonExclusiveToggle buttonExclusiveToggle = gameObject.GetComponent<ButtonExclusiveToggle>();
        buttonExclusiveToggle.OnToggleWithString.AddListener(ActivateLabel);
        buttonExclusiveToggle.data = label;
    }

    void SetupLabelExclusivities()
    {
        List<Transform> buttons = new List<Transform>();
        foreach (Transform transform in colorSchemePanelTransform.transform)
        {
            ButtonExclusiveToggle buttonExclusiveToggle = transform.GetComponent<ButtonExclusiveToggle>();
            if (buttonExclusiveToggle != null)
            {
                buttons.Add(transform);
            }
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            ButtonExclusiveToggle buttonExclusiveToggle = buttons[i].GetComponent<ButtonExclusiveToggle>();
            buttonExclusiveToggle.buttonsToExclude = new ButtonExclusiveToggle[buttons.Count - 1];

            for (int a = 0; a < buttons.Count; a++)
            {
                if (i == a) continue;
                int index = a;
                if (a > i) index--;

                ButtonExclusiveToggle otherButton = buttons[a].GetComponent<ButtonExclusiveToggle>();
                buttonExclusiveToggle.buttonsToExclude[index] = otherButton;
            }
        }
    }

    void CreateSchemePanel(ColorScheme cs, int index, int parentIndex, string parentLabel)
    {
        Transform viewportContent = colorSchemePanelTransform.GetChildNamed_Recursive("Content");
        if (viewportContent == null) return;

        RectTransform parentRectTransform = colorSchemePanelTransform.GetComponent<RectTransform>();

        int rowIndex = index / schemesPerRow;
        int columnIndex = index % schemesPerRow;
        float width = ((parentRectTransform.sizeDelta.x - schemesPadding - scrollBarWidth) / schemesPerRow) - schemesPadding;
        float positionX = (columnIndex + 1) * (width + schemesPadding) - width / 2;
        float positionY = schemesHeight / 2 - (rowIndex + 1) * (schemesHeight + schemesPadding);

        GameObject gameObject = new GameObject("ColorScheme " + parentIndex + "." + index + " Panel");
        gameObject.transform.parent = viewportContent;

        RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, schemesHeight);
        rectTransform.localScale = new Vector3(1, 1, 1);
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);

        gameObject.transform.position = new Vector3(positionX, positionY, 0);
        gameObject.transform.localPosition = new Vector3(positionX, positionY, 0);

        Image image = gameObject.AddComponent<Image>();
        image.sprite = cs.GetSprite();

        ColorSchemeOption colorSchemeOption = gameObject.AddComponent<ColorSchemeOption>();
        colorSchemeOption.index = index;
        colorSchemeOption.groupIndex = parentIndex;
        colorSchemeOption.groupLabel = parentLabel;
        colorSchemeOption.panel = colorSchemePanelTransform;

        gameObject.SetActive(false);
        cs.GameObject = gameObject;
    }
}
