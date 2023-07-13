using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContextMenu : MonoBehaviour
{
    bool open = false;
    bool opening = false;
    bool closing = false;
    float alpha = 0;
    float alphaStep = 0.3f;
    Vector3 newPosition = Vector3.zero;
    DateTime lastTimeOpen;
    Vector3 restPosition;
    RectTransform rectTransform;

    // Start is called before the first frame update
    void Start()
    {
        lastTimeOpen = DateTime.Now;
        rectTransform = transform as RectTransform;
        restPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, rectTransform.localPosition.z);
    }

    // Update is called once per frame
    void Update()
    {
        if (opening && alpha < 1)
        {
            //Debug.Log("Opening Context Menu. Alpha = " + alpha);
            //Log.Write("Opening Context Menu. Alpha = " + alpha);
            alpha += alphaStep;
            if (alpha >= 1)
            {
                alpha = 1;
                open = true;
                opening = false;
                closing = false;
                lastTimeOpen = DateTime.Now;
            }

            RectTransform rectTransform = transform as RectTransform;
            if (newPosition != rectTransform.localPosition)
            {
                //Debug.Log("Setting new Context Menu Position.");
                //Log.Write("Setting new Context Menu Position.");
                rectTransform.localPosition = newPosition;
            }

            SetAlphas();
        }

        if (closing && alpha > 0)
        {
            //Debug.Log("Closing Context Menu. Alpha = " + alpha);
            //Log.Write("Closing Context Menu. Alpha = " + alpha);
            alpha -= alphaStep;
            if (alpha <= 0)
            {
                alpha = 0;
                open = false;
                opening = false;
                closing = false;
                SetAlphas();

                RectTransform rectTransform = transform as RectTransform;
                if (newPosition != rectTransform.localPosition)
                {
                    //Debug.Log("Setting new Context Menu Position.");
                    //Log.Write("Setting new Context Menu Position.");
                    rectTransform.localPosition = newPosition;
                    if (rectTransform.localPosition != restPosition)
                        opening = true;
                }
            }
            else
                SetAlphas();
        }

        TimeSpan ts = DateTime.Now - lastTimeOpen;
        if (ts.Seconds >= 30 && !closing && rectTransform.localPosition != restPosition)
        {
            Close();
        }
    }

    public bool IsOpen
    {
        get
        {
            return open;
        }
    }

    public void Open(Vector3 position)
    {
        //Debug.Log("Open Context Menu.");
        //Log.Write("Open Context Menu.");
        lastTimeOpen = DateTime.Now;
        newPosition = new Vector3(position.x, position.y, position.z);
        if (opening || open)
        {
            opening = false;
            closing = true;
        }
        else
        {
            opening = true;
            closing = false;
        }
    }

    public void Close()
    {
        //Debug.Log("Close Context Menu.");
        //Log.Write("Close Context Menu.");
        RectTransform rectTransform = transform as RectTransform;
        newPosition = restPosition;
        if (newPosition != rectTransform.localPosition)
        {
            if (opening || open)
            {
                opening = false;
                closing = true;
            }
        }
    }

    void SetAlphas()
    {
        Image image = GetComponent<Image>();
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha / 2);

        Image[] images = GetComponentsInChildren<Image>();
        foreach (Image childImage in images)
        {
            if (childImage != image)
                childImage.color = new Color(childImage.color.r, childImage.color.g, childImage.color.b, alpha);
        }

        TMPro.TextMeshProUGUI[] tmpros = GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        foreach (TMPro.TextMeshProUGUI textMeshProUGUI in tmpros)
        {
            Material fontMaterial = textMeshProUGUI.fontMaterial;
            Color faceColor = fontMaterial.GetColor("_FaceColor");
            fontMaterial.SetColor("_FaceColor", new Color(faceColor.r, faceColor.g, faceColor.b, alpha));
            Color outlineColor = fontMaterial.GetColor("_OutlineColor");
            fontMaterial.SetColor("_OutlineColor", new Color(outlineColor.r, outlineColor.g, outlineColor.b, alpha));
        }
    }
}
