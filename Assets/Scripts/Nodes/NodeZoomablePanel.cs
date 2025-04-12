using UnityEngine;
using UnityEngine.EventSystems;

public class NodeZoomablePanel : MonoBehaviour
{
    Vector2 initialMousePos;
    Vector2 initialPosition;
    RectTransform rectTransform;
    Transform canvas;
    RectTransform canvasRectTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvas = transform.parent.parent;
        rectTransform = GetComponent<RectTransform>();
        canvasRectTransform = canvas.GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DragStart(BaseEventData evt)
    {
        initialMousePos = Input.mousePosition;
        initialPosition = transform.position;
    }

    public void Drag(BaseEventData evt)
    {
        Vector2 temp = (Vector2)Input.mousePosition - initialMousePos;
        Vector2 newPosition = initialPosition + temp;

        if (newPosition.x - (rectTransform.sizeDelta.x * rectTransform.localScale.x) / 2 > 0) // Panel going too far Right.
            newPosition.x = (rectTransform.sizeDelta.x * rectTransform.localScale.x) / 2;
        if (newPosition.x + (rectTransform.sizeDelta.x * rectTransform.localScale.x) / 2 < canvasRectTransform.rect.width) // Panel going too far Left.
            newPosition.x = canvasRectTransform.rect.width - (rectTransform.sizeDelta.x * rectTransform.localScale.x) / 2;

        if (newPosition.y - (rectTransform.sizeDelta.y * rectTransform.localScale.y) / 2 > 0) // Panel going too far Up.
            newPosition.y = (rectTransform.sizeDelta.y * rectTransform.localScale.y) / 2;
        if (newPosition.y + (rectTransform.sizeDelta.y * rectTransform.localScale.y) / 2 < canvasRectTransform.rect.height) // Panel going too far Down.
            newPosition.y = canvasRectTransform.rect.height - (rectTransform.sizeDelta.y * rectTransform.localScale.y) / 2;

        transform.position = newPosition;
    }
}
