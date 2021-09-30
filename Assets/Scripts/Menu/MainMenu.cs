using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public GameObject subMenu;
    public GameObject[] extraMenus;
    Vector3 startingPosition;
    Vector3[] extraMenusStartingPositions;
    public float xDelta;
    float shiftDelta = 0;
    bool shifting = false;
    bool shifted = false;
    Vector3 currentPosition;
    float currentShift = 0;
    float shiftStep = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        currentPosition = subMenu.transform.position;

        RectTransform rectTransform = subMenu.GetComponent<RectTransform>();
        startingPosition = rectTransform.anchoredPosition3D;

        RectTransform baseRectTransform = GetComponent<RectTransform>();

        float baseMenuX = baseRectTransform.anchoredPosition3D.x - baseRectTransform.localScale.x * baseRectTransform.rect.width / 2;
        float startingMenuX = rectTransform.anchoredPosition3D.x - rectTransform.localScale.x * rectTransform.rect.width / 2;

        shiftDelta = (baseMenuX - startingMenuX) + xDelta;

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
    }

    // Update is called once per frame
    void Update()
    {
        if (shifting)
        {
            if (shifted)
                ShiftMenuOut();
            else
                ShiftMenuIn();
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
        float positionX = (shiftDelta * actualCurrentShift) + startingPosition.x;

        Vector3 newPosition = new Vector3(positionX, startingPosition.y, startingPosition.z);

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

        currentShift -= shiftStep;
        float actualCurrentShift = (1 - Mathf.Cos(currentShift * Mathf.PI)) / 2;
        if (currentShift < 0)
        {
            currentShift = 0;
            actualCurrentShift = 0;
            shifting = false;
            shifted = false;
        }
        float positionX = (shiftDelta * actualCurrentShift) + startingPosition.x;

        Vector3 newPosition = new Vector3(positionX, startingPosition.y, startingPosition.z);

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
}
