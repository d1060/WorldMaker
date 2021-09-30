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
            }
            else
            {
                image.sprite = disabledSprite;
            }
            OnToggle?.Invoke(isEnabled);
        }
    }

    public void Enable()
    {
        isEnabled = true;
        Image image = GetComponent<Image>();
        image.sprite = enabledSprite;
    }

    public void Disable()
    {
        isEnabled = false;
        Image image = GetComponent<Image>();
        image.sprite = disabledSprite;
    }
}

[Serializable]
public class ToggleButtonEvent : UnityEvent<bool> { }
