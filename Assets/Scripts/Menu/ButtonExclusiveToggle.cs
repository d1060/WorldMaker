using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonExclusiveToggle : MonoBehaviour
{
    public ButtonExclusiveToggle[] buttonsToExclude;
    public Color colorSelected;
    public Color colorDeselected;
    public bool isEnabled = false;
    public ToggleButtonEvent OnToggle;

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
        if (!isEnabled)
        {
            isEnabled = true;
            Button button = GetComponent<Button>();
            if (button == null)
                return;

            if (button.colors == null)
                return;

            ColorBlock colorBlock = button.colors;
            colorBlock.normalColor = colorSelected;
            button.colors = colorBlock;

            if (buttonsToExclude == null || buttonsToExclude.Length == 0)
                return;

            foreach (ButtonExclusiveToggle buttonExclusiveToggle in buttonsToExclude)
            {
                buttonExclusiveToggle.Deselect();
            }
            OnToggle?.Invoke(isEnabled);
        }
    }

    public void Deselect()
    {
        isEnabled = false;
        Button button = GetComponent<Button>();
        if (button == null)
            return;

        if (button.colors == null)
            return;

        ColorBlock colorBlock = button.colors;
        colorBlock.normalColor = colorDeselected;
        button.colors = colorBlock;
    }
}
