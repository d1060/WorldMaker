using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class NodeContextMenu : MonoBehaviour
{
    public List<Transform> prefabNodes = new List<Transform>();
    public EventSystem eventSystem;
    public Canvas canvas;
    public GameObject optionButtonPrefab;
    public Transform nodeTreePanelTransform;
    public Transform zoomableNodesPanel;

    List<GameObject> nodeGameObjects = new List<GameObject>();

    GraphicRaycaster graphicRaycaster;
    Vector3 initialPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
        initialPosition = rectTransform.anchoredPosition3D;

        PopulateNodeOptions();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Close()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition3D = initialPosition;
    }

    void PopulateNodeOptions()
    {
        Transform viewportContent = transform.GetChildNamed_Recursive("Content");
        if (viewportContent == null) return;

        Transform transformSearchBox = transform.GetChildNamed_Recursive("SearchBox (TMP)");
        if (transformSearchBox == null) return;

        TMP_InputField tmp_InputField = transformSearchBox.GetComponent<TMP_InputField>();
        if (tmp_InputField == null) return;

        string searchBox = tmp_InputField.text;

        float positionY = 0;
        int i = 0;
        foreach (Transform prefab in prefabNodes)
        {
            string name = prefab.name.Replace(" Node", "");
            if (searchBox != "" && !name.Contains(searchBox, System.StringComparison.OrdinalIgnoreCase))
                continue;

            GameObject gameObject = null;
            if (i < nodeGameObjects.Count)
                gameObject = nodeGameObjects[i];
            else
            {
                gameObject = Object.Instantiate(optionButtonPrefab, viewportContent);
                nodeGameObjects.Add(gameObject);
            }

            RectTransform goRectTransform = gameObject.GetComponent<RectTransform>();

            float positionX = 5;
            positionY = 4 + i * (goRectTransform.sizeDelta.y + 4);

            gameObject.transform.position = new Vector3(positionX, positionY, 0);
            gameObject.transform.localPosition = new Vector3(positionX, -positionY, 0);

            TextMeshProUGUI textMeshProUGUI = gameObject.transform.GetComponentInChildren<TextMeshProUGUI>();
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.text = name;
            }

            NodeContextButton nodeContextButton = gameObject.GetComponent<NodeContextButton>();
            if (nodeContextButton != null)
            {
                nodeContextButton.prefab = prefab.gameObject;
                nodeContextButton.nodesPanel = nodeTreePanelTransform;
                nodeContextButton.zoomableNodesPanel = zoomableNodesPanel;
            }

            i++;
            positionY = 4 + i * (goRectTransform.sizeDelta.y + 4);
        }

        for (; i < nodeGameObjects.Count; i++)
        {
            GameObject gameObject = nodeGameObjects[i];
            nodeGameObjects.Remove(gameObject);
            GameObject.Destroy(gameObject);
            i--;
        }

        RectTransform rectTransform = viewportContent.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, positionY);
    }

    public void SearchBoxTextChanged()
    {
        PopulateNodeOptions();
    }
}
