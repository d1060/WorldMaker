using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideObjectsWhenEnabled : MonoBehaviour
{
    public GameObject objectToHide;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        if (objectToHide) objectToHide.SetActive(false);
    }

    void OnDisable()
    {
        if (objectToHide) objectToHide.SetActive(true);
    }
}
