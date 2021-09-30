using System;
using SFB;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;

[Serializable]
public class StringEvent : UnityEvent<string> { }

[Serializable]
public class MapTextBoxEvent : UnityEvent { }

public class MapTextBox : MonoBehaviour
{

    public StringEvent SetField;
    TMP_InputField inputField;
    public Map map;
    public string PropertyName;
    public bool truncatePath = false;
    public MapTextBoxEvent Deselect;

    // Start is called before the first frame update
    void Start()
    {
        inputField = GetComponent<TMP_InputField>();

        if (map != null && PropertyName != "")
        {
            System.Type type = map.GetType();
            PropertyInfo propertyInfo = type.GetProperty(PropertyName);
            string propertyValue = propertyInfo.GetValue(map).ToString();
            if (truncatePath)
            {
                string file = System.IO.Path.GetFileName(propertyValue);
                propertyValue = file;
            }
            inputField.text = propertyValue;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
            inputField.DeactivateInputField();
            Deselect?.Invoke();
        }
    }

    public string Field
    {
        set
        {
            SetField?.Invoke(value);
        }
    }

    public void OnSelect(string select)
    {

    }

    public void OnDeselect(string deselect)
    {
        Deselect?.Invoke();
    }

    public void OpenImageFile()
    {
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Choose image file", "",
            new[] {
                new ExtensionFilter("All image files", "png", "jpg", "jpeg", "bmp" )
            }
            , false);
        if (paths.Length == 0)
            return;
        string path = paths[0];
        Field = path;
        if (truncatePath)
        {
            string file = System.IO.Path.GetFileName(path);
            path = file;
        }
        inputField.text = path;
    }
}
