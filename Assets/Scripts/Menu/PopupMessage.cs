using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupMessage : MonoBehaviour
{
    public int messageTimeout = 5;
    float alpha = 1;
    float alphaStep = 0.1f;
    bool open = true;
    bool opening = false;
    bool closing = false;
    DateTime lastTimeOpen;
    Vector3 newPosition = Vector3.zero;
    Vector3 restPosition;
    RectTransform rectTransform;
    Canvas canvas;

    // Start is called before the first frame update
    void Start()
    {
        lastTimeOpen = DateTime.Now;
        rectTransform = transform as RectTransform;
        restPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, rectTransform.localPosition.z);

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Transform parentParent = transform.parent;
            while (parentParent != null)
            {
                canvas = parentParent.GetComponentInParent<Canvas>();
                if (canvas != null)
                    break;

                parentParent = parentParent.parent;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (opening && alpha < 1)
        {
            alpha += alphaStep;
            if (alpha >= 1)
            {
                alpha = 1;
                open = true;
                opening = false;
                closing = false;
                lastTimeOpen = DateTime.Now;
            }
            SetAlphas();
        }
        else if (opening)
            opening = false;

        if (closing && alpha > 0)
        {
            alpha -= alphaStep;
            if (alpha <= 0)
            {
                alpha = 0;
                open = false;
                closing = false;
                lastTimeOpen = DateTime.Now;
                SetAlphas();

                RectTransform rectTransform = transform as RectTransform;
                if (newPosition != rectTransform.localPosition)
                {
                    rectTransform.localPosition = newPosition;
                }
                opening = true;
            }
            else
                SetAlphas();
        }
        else if (closing)
            closing = false;

        TimeSpan ts = DateTime.Now - lastTimeOpen;
        if (ts.Seconds >= messageTimeout && !closing && rectTransform.localPosition != restPosition)
        {
            Hide();
        }
    }

    public void Show()
    {
        if (canvas == null)
            return;

        RectTransform canvasRect = canvas.transform as RectTransform;

        newPosition = new Vector3(0, 0, rectTransform.localPosition.z);
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

    public void Hide()
    {
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
