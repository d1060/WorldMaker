using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ButtonToggle : MonoBehaviour
{
    public bool isEnabled = false;
    public Sprite enabledSprite;
    public Sprite disabledSprite;
    public Map map;
    public ToggleButtonEvent OnToggle;
    public bool activeIfDisabled = true;
    public GameObject hintPanel = null;
    public GameObject[] objectsToHideWhenEnabled;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnClick()
    {
        if (isEnabled || activeIfDisabled)
        {
            isEnabled = !isEnabled;
            Image image = GetComponent<Image>();

            if (isEnabled)
            {
                image.sprite = enabledSprite;
                if (hintPanel) hintPanel.SetActive(true);
                if (objectsToHideWhenEnabled != null && objectsToHideWhenEnabled.Length > 0)
                {
                    foreach (GameObject gameObject in objectsToHideWhenEnabled)
                    {
                        gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                image.sprite = disabledSprite;
                if (hintPanel) hintPanel.SetActive(false);
                if (objectsToHideWhenEnabled != null && objectsToHideWhenEnabled.Length > 0)
                {
                    foreach (GameObject gameObject in objectsToHideWhenEnabled)
                    {
                        gameObject.SetActive(true);
                    }
                }
            }
            OnToggle?.Invoke(isEnabled);
        }
    }

    public void Enable()
    {
        isEnabled = true;
        Image image = GetComponent<Image>();
        image.sprite = enabledSprite;
        if (hintPanel) hintPanel.SetActive(true);
        if (objectsToHideWhenEnabled != null && objectsToHideWhenEnabled.Length > 0)
        {
            foreach (GameObject gameObject in objectsToHideWhenEnabled)
            {
                gameObject.SetActive(false);
            }
        }
    }

    public void Disable()
    {
        isEnabled = false;
        Image image = GetComponent<Image>();
        image.sprite = disabledSprite;
        if (hintPanel) hintPanel.SetActive(false);
        if (objectsToHideWhenEnabled != null && objectsToHideWhenEnabled.Length > 0)
        {
            foreach (GameObject gameObject in objectsToHideWhenEnabled)
            {
                gameObject.SetActive(true);
            }
        }
    }

    public void Toggle()
    {
        if (isEnabled) Disable();
        else           Enable();
    }
}

[Serializable]
public class ToggleButtonEvent : UnityEvent<bool> { }
