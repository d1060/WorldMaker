using UnityEngine;
using TMPro;

public class NodeContextButton : MonoBehaviour
{
    public GameObject prefab;
    public Transform nodesPanel;
    public Transform zoomableNodesPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Select()
    {
        NodeContextMenu nodeContextMenu = transform.GetComponentInFirstParent<NodeContextMenu>();
        if (nodeContextMenu == null)
            return;

        RectTransform nodeContextMenuRectTransform = nodeContextMenu.transform.GetComponent<RectTransform>();
        RectTransform zoomableNodesPanelRectTransform = zoomableNodesPanel.GetComponent<RectTransform>();
        RectTransform nodesPanelRectTransform = nodesPanel.GetComponent<RectTransform>();

        GameObject instance = Object.Instantiate(prefab, nodeContextMenu.transform.position, Quaternion.identity);
        instance.transform.SetParent(zoomableNodesPanel);

        RectTransform rectTransform = instance.transform.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = new Vector3(1, 1, 1);
            Vector2 newPosition = nodeContextMenuRectTransform.anchoredPosition;

            newPosition.x += (zoomableNodesPanelRectTransform.sizeDelta.x / 2) - (nodesPanelRectTransform.rect.width / 2);
            newPosition.y += (zoomableNodesPanelRectTransform.sizeDelta.y / 2) - (nodesPanelRectTransform.rect.height / 2);

            //newPosition.x += rectTransform.sizeDelta.x / 2;
            //newPosition.y -= nodeTreePanelRectTransform.sizeDelta.y;
            //newPosition.y -= rectTransform.sizeDelta.y / 2;

            rectTransform.anchoredPosition = newPosition;
        }
        //instance.transform.position = new Vector3(nodeContextMenu.transform.position.x, nodeContextMenu.transform.position.y, 0);
        //instance.transform.localPosition = new Vector3(nodeContextMenu.transform.position.x, nodeContextMenu.transform.position.y, 0);

        nodeContextMenu.Close();

        Transform transformSearchBox = nodeContextMenu.transform.GetChildNamed_Recursive("SearchBox (TMP)");
        if (transformSearchBox == null) return;

        TMP_InputField tmp_InputField = transformSearchBox.GetComponent<TMP_InputField>();
        if (tmp_InputField == null) return;

        tmp_InputField.text = "";
    }
}
