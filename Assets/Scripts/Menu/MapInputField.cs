using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapInputField : MonoBehaviour
{
    public Camera cam;
    public Map mainMap;
    public float maxOutlineThickness = 0.3333f;
    public float minOutlineThickness = 0.3333f;
    public float maxOutlineThicknessDistance = 1;
    public float minOutlineThicknessDistance = 0.5f;
    public float maxDistanceAlphaZero = 0;
    public float minDistanceAlphaZero = 0;
    public float maxDistanceAlpha = 0.9f;
    public float minDistanceAlpha = 0.5f;
    public float maxSize = 1.0f;
    public float minSize = 1.0f;
    float initialDistance;
    float prevDistance;
    Vector3 originalScale;

    // Start is called before the first frame update
    void Start()
    {
        SetStartingDistance();
    }

    // Update is called once per frame
    void Update()
    {
        if (CurrentDistance != prevDistance)
        {
            if (CurrentDistance > minOutlineThicknessDistance)
            {
                SetThickness();
            }
            if (CurrentDistance <= maxDistanceAlpha || (maxDistanceAlphaZero != 0 && CurrentDistance >= minDistanceAlphaZero))
            {
                SetAlpha();
            }
            SetSize();
        }
        prevDistance = CurrentDistance;
    }

    void SetStartingDistance()
    {
        initialDistance = Mathf.Abs(cam.transform.position.z - mainMap.transform.position.z);
        RectTransform rectTransform = GetComponent<RectTransform>();
        originalScale = new Vector3(rectTransform.localScale.x, rectTransform.localScale.y, rectTransform.localScale.z);
    }

    void SetThickness()
    {
        float thickness = (maxOutlineThickness - minOutlineThickness) * ((CurrentDistance - minOutlineThicknessDistance) / (maxOutlineThicknessDistance - minOutlineThicknessDistance)) + minOutlineThickness;
        if (thickness > maxOutlineThickness) thickness = maxOutlineThickness;
        if (thickness < minOutlineThickness) thickness = minOutlineThickness;
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            return;

        Material material = meshRenderer.materials[0];

        material.SetFloat("_OutlineWidth", thickness);
    }

    void SetAlpha()
    {
        float alpha = 1;
        if (maxDistanceAlphaZero != 0 && CurrentDistance >= minDistanceAlphaZero)
        {
            if (CurrentDistance >= maxDistanceAlphaZero)
            {
                alpha = 0;
            }
            else if (CurrentDistance >= minDistanceAlphaZero)
            {
                alpha = ((maxDistanceAlphaZero - CurrentDistance) / (maxDistanceAlphaZero - minDistanceAlphaZero));
            }
        }
        else
        {
            alpha = ((CurrentDistance - minDistanceAlpha) / (maxDistanceAlpha - minDistanceAlpha));
        }
        if (alpha > 1) alpha = 1;
        if (alpha < 0) alpha = 0;

        SetAlphasInChildren(alpha, transform.gameObject);
    }

    void SetAlphasInChildren(float alpha, GameObject gameObject)
    {
        CanvasRenderer cr = gameObject.GetComponent<CanvasRenderer>();
        if (cr != null)
            cr.SetAlpha(alpha);

        foreach (Transform childTransform in gameObject.transform)
        {
            if (childTransform.gameObject != gameObject)
            {
                GameObject childGameObject = childTransform.gameObject;
                SetAlphasInChildren(alpha, childGameObject);
            }
        }
    }

    void SetSize()
    {
        float sizeMultiplier = CurrentDistance * (maxSize - minSize) + minSize;
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = new Vector3(originalScale.x * sizeMultiplier, originalScale.y * sizeMultiplier, originalScale.z);
        }
    }

    float CurrentDistance { get { return Mathf.Abs(cam.transform.position.z - mainMap.transform.position.z) / initialDistance; } }
}
