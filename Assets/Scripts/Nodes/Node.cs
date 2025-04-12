using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Node : MonoBehaviour
{
    public List<string> inputs;
    public List<string> outputs;

    RectTransform rectTransform = null;
    Vector2 initialMousePos;
    Vector2 initialAnchoredPosition;
    float clicktime = 0;
    float clickdelay = 0.5f;
    bool expanded = true;
    bool expanding = false;
    bool shrinking = false;
    Vector2 shrunkSize = new Vector2(110, 20);
    Vector2 initialSize = new Vector2(0, 0);
    Vector2 morphDelta = new Vector2(0, 0);
    float morphStep = 0.05f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        initialSize = rectTransform.sizeDelta;
        morphDelta = initialSize * morphStep;
    }

    // Update is called once per frame
    void Update()
    {
        if (shrinking)
        {
            Vector2 size = rectTransform.sizeDelta;
            size -= morphDelta;
            if (size.x <= shrunkSize.x || size.y <= shrunkSize.y)
            {
                rectTransform.sizeDelta = shrunkSize;
                shrinking = false;
            }
            else
            {
                rectTransform.sizeDelta = size;
            }
        }
        else if (expanding)
        {
            Vector2 size = rectTransform.sizeDelta;
            size += morphDelta;
            if (size.x >= initialSize.x || size.y >= initialSize.y)
            {
                rectTransform.sizeDelta = initialSize;
                expanding = false;
            }
            else
            {
                rectTransform.sizeDelta = size;
            }
        }
    }

    public void DragStart(BaseEventData ev)
    {
        initialMousePos = Input.mousePosition;
        initialAnchoredPosition = rectTransform.anchoredPosition;
    }

    public void Drag(BaseEventData ev)
    {
        Vector2 deltaPosition = (Vector2)Input.mousePosition - initialMousePos;
        rectTransform.anchoredPosition = initialAnchoredPosition + deltaPosition;
    }

    public void DragEnd(BaseEventData ev)
    {
        MapData.instance.Save();
    }

    public void PointerClick(BaseEventData ev)
    {
        PointerEventData ped = ev as PointerEventData;
        if (Time.time - clicktime < clickdelay)
        {
            //Double Click.
            if (expanded)
            {
                Shrink();
            }
            else
            {
                Expand();
            }
        }
        clicktime = Time.time;
    }

    void Shrink()
    {
        shrinking = true;
        expanded = false;
        expanding = false;
    }

    void Expand()
    {
        expanding = true;
        expanded = true;
        shrinking = false;
    }
}
