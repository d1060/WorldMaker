using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class MainMenuShiftEvent : UnityEvent { }

public class MainMenu : MonoBehaviour
{
    public enum REST_TYPE
    {
        DOWN,
        RIGHT,
        RIGHT_AFTER_PARENT
    }

    public GameObject subMenu;
    public GameObject[] extraMenus;
    Vector3 startingPosition;
    Vector3[] extraMenusStartingPositions;
    bool shifting = false;
    bool shifted = false;
    float currentShift = 0;
    float shiftStep = 0.1f;
    public MainMenuShiftEvent ShiftIn;
    public MainMenuShiftEvent ShiftOut;
    Vector3 finalPosition;
    public REST_TYPE restType = REST_TYPE.DOWN;

    public bool IsOut { get { return currentShift == 1; } }
    public bool IsIn { get { return currentShift == 0; } }
    public bool IsShifting { get { return currentShift != 0 && currentShift != 1; } }

    // Start is called before the first frame update
    void Start()
    {
        RectTransform rectTransform = subMenu.GetComponent<RectTransform>();
        startingPosition = rectTransform.anchoredPosition3D;

        RectTransform baseRectTransform = GetComponent<RectTransform>();

        float baseMenuX = baseRectTransform.anchoredPosition3D.x;
        float startingMenuX = rectTransform.anchoredPosition3D.x;

        float baseMenuY = baseRectTransform.anchoredPosition3D.y;
        float startingMenuY = rectTransform.anchoredPosition3D.y;

        extraMenusStartingPositions = new Vector3[extraMenus != null ? extraMenus.Length : 0];
        if (extraMenus != null)
        {
            for(int i = 0; i < extraMenus.Length; i++)
            {
                GameObject extraMenu = extraMenus[i];
                RectTransform extraMenuRectTransform = extraMenu.GetComponent<RectTransform>();
                extraMenusStartingPositions[i] = extraMenuRectTransform.anchoredPosition3D;
            }
        }

        if (restType == REST_TYPE.DOWN)
            finalPosition = new Vector3(baseMenuX, baseMenuY - baseRectTransform.localScale.y * baseRectTransform.rect.height - 5, 1);
        else if (restType == REST_TYPE.RIGHT)
            finalPosition = new Vector3(baseMenuX + baseRectTransform.localScale.x * baseRectTransform.rect.width + 5, baseMenuY, 1);
        else if (restType == REST_TYPE.RIGHT_AFTER_PARENT)
        {
            RectTransform parentRectTransform = transform.parent.GetComponent<RectTransform>();
            finalPosition = new Vector3(parentRectTransform.anchoredPosition.x + parentRectTransform.sizeDelta.x + 5, baseMenuY, 1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (shifting)
        {
            if (shifted)
            {
                if (currentShift == 1)
                    ShiftOut?.Invoke();
                ShiftMenuOut();
            }
            else
            {
                if (currentShift == 0)
                    ShiftIn?.Invoke();
                ShiftMenuIn();
            }
        }
    }

    public void ShiftMenu()
    {
        shifting = true;
    }

    void ShiftMenuIn()
    {
        currentShift += shiftStep;
        float actualCurrentShift = (1 - Mathf.Cos(currentShift * Mathf.PI))/2;
        if (currentShift > 1)
        {
            currentShift = 1;
            actualCurrentShift = 1;
            shifting = false;
            shifted = true;
        }
        float positionX = (finalPosition.x * actualCurrentShift) + (startingPosition.x * (1 - actualCurrentShift));
        float positionY = (finalPosition.y * actualCurrentShift) + (startingPosition.y * (1 - actualCurrentShift));

        Vector3 newPosition = new Vector3(positionX, positionY, startingPosition.z);

        RectTransform rectTransform = subMenu.GetComponent<RectTransform>();
        rectTransform.anchoredPosition3D = newPosition;

        MainMenu[] otherMenus = transform.parent.GetComponentsInChildren<MainMenu>();
        foreach (MainMenu otherMenu in otherMenus)
        {
            if (otherMenu != this)
            {
                otherMenu.ShiftMenuOut();
            }
        }
    }

    void ShiftMenuOut()
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            MainMenu childMainMenu = child.gameObject.GetComponent<MainMenu>();
            if (childMainMenu != null)
                childMainMenu.ShiftMenuOut();

            foreach (Transform child2 in child.gameObject.transform)
            {
                MainMenu childMainMenu2 = child2.gameObject.GetComponent<MainMenu>();
                if (childMainMenu2 != null)
                    childMainMenu2.ShiftMenuOut();
            }
        }
        MainMenu[] mainMenus = subMenu.transform.GetComponentsInChildren<MainMenu>();
        foreach (MainMenu mainMenu in mainMenus)
        {
            mainMenu.ShiftMenuOut();
        }

        ColorBox[] colorBoxes = subMenu.transform.GetComponentsInChildren<ColorBox>();
        foreach (ColorBox colorBox in colorBoxes)
        {
            colorBox.DestroyColorPicker();
        }

        ColorSchemePanel[] colorSchemePanels = transform.parent.GetComponentsInChildren<ColorSchemePanel>();
        foreach (ColorSchemePanel colorSchemePanel in colorSchemePanels)
        {
            colorSchemePanel.ReturnToOrigin();
        }

        GradientSliderHandleColorBox[] gradientSliderHandleColorBoxes = subMenu.transform.GetComponentsInChildren<GradientSliderHandleColorBox>();
        foreach (GradientSliderHandleColorBox gradientSliderHandleColorBox in gradientSliderHandleColorBoxes)
        {
            gradientSliderHandleColorBox.DestroyColorPicker();
        }

        currentShift -= shiftStep;
        float actualCurrentShift = (1 - Mathf.Cos(currentShift * Mathf.PI)) / 2;
        if (currentShift < 0)
        {
            currentShift = 0;
            actualCurrentShift = 0;
            shifting = false;
            shifted = false;
        }
        float positionX = (finalPosition.x * actualCurrentShift) + (startingPosition.x * (1 - actualCurrentShift));
        float positionY = (finalPosition.y * actualCurrentShift) + (startingPosition.y * (1 - actualCurrentShift));

        Vector3 newPosition = new Vector3(positionX, positionY, startingPosition.z);

        RectTransform rectTransform = subMenu.GetComponent<RectTransform>();
        rectTransform.anchoredPosition3D = newPosition;

        if (extraMenus != null)
        {
            for (int i = 0; i < extraMenus.Length; i++)
            {
                GameObject extraMenu = extraMenus[i];
                RectTransform extraMenuRectTransform = extraMenu.GetComponent<RectTransform>();

                float extraPositionX = ((extraMenuRectTransform.anchoredPosition3D.x - extraMenusStartingPositions[i].x) * actualCurrentShift) + extraMenusStartingPositions[i].x;
                Vector3 extraMenuNewPosition = new Vector3(extraPositionX, extraMenusStartingPositions[i].y, extraMenusStartingPositions[i].z);

                extraMenuRectTransform.anchoredPosition3D = extraMenuNewPosition;
            }
        }
    }

    public Vector3 StartingPosition
    {
        get { return startingPosition; }
        set { startingPosition = value; }
    }
}
