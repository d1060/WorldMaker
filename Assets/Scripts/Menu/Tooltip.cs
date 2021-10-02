using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour
{
    public GameObject tooltipPanel;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            pointerId = -1,
        };

        pointerData.position = Input.mousePosition;
        
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        bool isActive = false;

        foreach (RaycastResult raycastResult in results)
        {
            if (raycastResult.gameObject == this.gameObject || raycastResult.gameObject.transform.parent.gameObject == this.gameObject || raycastResult.gameObject.transform.parent.parent.gameObject == this.gameObject)
            {
                tooltipPanel.SetActive(true);
                isActive = true;
            }
        }

        if (!isActive)
            tooltipPanel.SetActive(false);
    }
}
