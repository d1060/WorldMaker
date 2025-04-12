using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using UnityEngine.UI;

public class NodeTreePanel : MonoBehaviour
{
    RectTransform rectTransform;
    RectTransform rectTransformDragArea;
    RectTransform canvasRectTransform;
    RectTransform zoomableNodesRectTransform;
    public GameObject eventSystemObject;
    public Transform zoomableNodesPanel;
    public float minHeight = 50;
    public float maxHeight = 500;
    public Transform contextMenu;
    public Transform dragArea;
    Transform canvas;

    Vector2 initialMousePos;
    Vector2 initialDeltaSize;
    Vector2 initialDragAreaAnchoredPosition;
    bool prevMouseLeftButtonDown = false;
    bool prevMouseRightButtonDown = false;
    float currentZoom = 1;
    float zoomStep = 1;
    float originalWidth = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransformDragArea = dragArea.GetComponent<RectTransform>();
        zoomableNodesRectTransform = zoomableNodesPanel.GetComponent<RectTransform>();
        canvas = transform.parent;
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        originalWidth = canvasRectTransform.rect.width;
        maxHeight = canvasRectTransform.rect.height / 2;

        //VisualElement rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        //rootVisualElement.RegisterCallback<WheelEvent>(OnMouseWheel, TrickleDown.TrickleDown);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnRectTransformDimensionsChange()
    {
        //if (canvasRectTransform != null && rectTransform != null)
        //{
        //    float currentSizeDeltaX = rectTransform.sizeDelta.x;
        //    float currentWidth = originalWidth;

        //    originalWidth = canvasRectTransform.rect.width;
        //    float newSizeDeltaX = currentSizeDeltaX * originalWidth / currentWidth;
        //    rectTransform.sizeDelta = new Vector2(newSizeDeltaX, rectTransform.sizeDelta.y);
        //}
    }

    public void DragStart(BaseEventData evt)
    {
        initialMousePos = Input.mousePosition;
        initialDeltaSize = rectTransform.sizeDelta;
        initialDragAreaAnchoredPosition = rectTransformDragArea.anchoredPosition;
    }

    public void Resize(BaseEventData evt)
    {
        Vector2 delta = (Vector2)Input.mousePosition - initialMousePos;

        if (delta.y + initialDeltaSize.y < minHeight)
            delta.y = minHeight - initialDeltaSize.y;

        if (delta.y + initialDeltaSize.y > maxHeight)
            delta.y = maxHeight - initialDeltaSize.y;

        float deltaY = delta.y;
        delta = new Vector2(rectTransform.sizeDelta.x, delta.y);
        delta.y += initialDeltaSize.y;

        rectTransform.sizeDelta = delta;
        rectTransformDragArea.anchoredPosition = new Vector2(initialDragAreaAnchoredPosition.x, initialDragAreaAnchoredPosition.y + deltaY);
    }

    public void OpenContextMenu()
    {
        if (contextMenu != null)
        {
            RectTransform parentRect = transform.parent as RectTransform;
            RectTransform contextRect = contextMenu as RectTransform;
            Vector2 newPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            if (Input.mousePosition.x > parentRect.sizeDelta.x - contextRect.sizeDelta.x)
                newPosition.x = parentRect.sizeDelta.x - contextRect.sizeDelta.x;

            if (Input.mousePosition.y < contextRect.sizeDelta.y)
                newPosition.y = contextRect.sizeDelta.y;

            contextRect.anchoredPosition = newPosition;
        }
    }

    public void CloseContextMenu()
    {
        if (contextMenu != null)
        {
            NodeContextMenu nodeContextMenu = contextMenu.GetComponent<NodeContextMenu>();
            nodeContextMenu.Close();
        }
    }

    public void MouseDown(BaseEventData evt)
    {
        PointerEventData ped = evt as PointerEventData;
        if (ped == null)
            return;

        if (ped.button == PointerEventData.InputButton.Left)
        {
            CloseContextMenu();
        }
        else if (ped.button == PointerEventData.InputButton.Right)
        {
            OpenContextMenu();
        }
    }

    public void MouseWheel(float delta)
    {
        float prevZoom = currentZoom;
        Vector2 mousePosition = Input.mousePosition;
        Vector2 zoomPanelCenter = new Vector2(zoomableNodesRectTransform.position.x, zoomableNodesRectTransform.position.y);

        Vector2 mousePositionToZoomPanelCenter = mousePosition - zoomPanelCenter;

        if (delta >= 0)
            currentZoom *= 1 + (zoomStep * delta);
        else
            currentZoom /= 1 - (zoomStep * delta);

        if (zoomableNodesRectTransform.sizeDelta.x * currentZoom < canvasRectTransform.rect.width) // Panel going too short.
            currentZoom = canvasRectTransform.rect.width / zoomableNodesRectTransform.sizeDelta.x;
        if (zoomableNodesRectTransform.sizeDelta.y * currentZoom < canvasRectTransform.rect.height) // Panel going too short.
            currentZoom = canvasRectTransform.rect.height / zoomableNodesRectTransform.sizeDelta.y;

        Vector2 newScale = new Vector2(currentZoom, currentZoom);
        zoomableNodesRectTransform.localScale = newScale;

        Vector2 mousePositionInZoomPanelScaled = mousePositionToZoomPanelCenter * currentZoom / prevZoom;

        Vector2 panelDrag = (mousePositionToZoomPanelCenter - mousePositionInZoomPanelScaled);

        Vector3 newPosition = new Vector3(
            zoomableNodesRectTransform.position.x + panelDrag.x,
            zoomableNodesRectTransform.position.y + panelDrag.y,
            zoomableNodesRectTransform.position.z );

        if (newPosition.x - (zoomableNodesRectTransform.sizeDelta.x * zoomableNodesRectTransform.localScale.x) / 2 > 0) // Panel going too far Right.
            newPosition.x = (zoomableNodesRectTransform.sizeDelta.x * zoomableNodesRectTransform.localScale.x) / 2;
        if (newPosition.x + (zoomableNodesRectTransform.sizeDelta.x * zoomableNodesRectTransform.localScale.x) / 2 < canvasRectTransform.rect.width) // Panel going too far Left.
            newPosition.x = canvasRectTransform.rect.width - (zoomableNodesRectTransform.sizeDelta.x * zoomableNodesRectTransform.localScale.x) / 2;

        if (newPosition.y - (zoomableNodesRectTransform.sizeDelta.y * zoomableNodesRectTransform.localScale.y) / 2 > 0) // Panel going too far Up.
            newPosition.y = (zoomableNodesRectTransform.sizeDelta.y * zoomableNodesRectTransform.localScale.y) / 2;
        if (newPosition.y + (zoomableNodesRectTransform.sizeDelta.y * zoomableNodesRectTransform.localScale.y) / 2 < canvasRectTransform.rect.height) // Panel going too far Down.
            newPosition.y = canvasRectTransform.rect.height - (zoomableNodesRectTransform.sizeDelta.y * zoomableNodesRectTransform.localScale.y) / 2;

        zoomableNodesRectTransform.position = newPosition;
    }
}
