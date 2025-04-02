using UnityEngine;

public class ColorSchemeOption : MonoBehaviour
{
    public int index;
    public int groupIndex;
    public string groupLabel;
    public Transform panel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnMouseDown()
    {
        ColorSchemePanel colorSchemePanel = panel.GetComponent<ColorSchemePanel>();
        Transform source = colorSchemePanel.source;
        ColorSchemeBox colorSchemeBox = source.GetComponent<ColorSchemeBox>();
        colorSchemeBox.SetColorScheme(index, groupLabel);

        colorSchemePanel.ReturnToOrigin();
    }
}
