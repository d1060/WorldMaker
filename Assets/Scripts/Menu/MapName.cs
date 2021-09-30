using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapName : MonoBehaviour
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
    public bool isInGlobe = false;

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
            SetThickness();
            SetAlpha();
            SetSize();
        }
        prevDistance = CurrentDistance;
    }

    void SetStartingDistance()
    {
        if (!isInGlobe)
        {
            initialDistance = 190;
        }
        else
        {
            initialDistance = 200;
        }
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

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            return;

        Material material = meshRenderer.materials[0];

        Color faceColor = material.GetColor("_FaceColor");
        Color outlineColor = material.GetColor("_OutlineColor");

        faceColor.a = alpha;
        outlineColor.a = alpha;

        material.SetColor("_FaceColor", faceColor);
        material.SetColor("_OutlineColor", outlineColor);
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

    float CurrentDistance
    {
        get
        {
            if (!isInGlobe)
                return Mathf.Abs(cam.transform.position.z - mainMap.transform.position.z) / initialDistance;

            Vector3 cameraVector = cam.transform.position - mainMap.geoSphere.transform.position;
            float globeDistance = cameraVector.magnitude - mainMap.geoSphere.Radius;
            return globeDistance / initialDistance;
        }
    }
}
