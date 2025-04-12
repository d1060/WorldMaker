using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HorizontalDragArea : MonoBehaviour
{
    public Texture2D pointerCursor;
    public Texture2D verticalResizeCursor;
    public Transform nodesPanel;

    NodeTreePanel nodeTreePanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nodeTreePanel = nodesPanel.GetComponent<NodeTreePanel>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void DragStart(BaseEventData evt)
    {
        nodeTreePanel.DragStart(evt);
    }

    public void Drag(BaseEventData evt)
    {
        nodeTreePanel.Resize(evt);
    }

    public void MouseEnter(BaseEventData evt)
    {
        Cursor.SetCursor(verticalResizeCursor, new Vector2(5, 12), CursorMode.ForceSoftware);
    }

    public void MouseLeave(BaseEventData evt)
    {
        Cursor.SetCursor(pointerCursor, Vector2.zero, CursorMode.ForceSoftware);
    }
}
