using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clouds : MonoBehaviour
{
    public float speed = 1;
    public float speed2 = 1;
    public Camera cam;
    public Map mainMap;
    float divisionFactor = 64;
    float initialDistance = 200;
    float prevDistance;
    public float maxDistanceAlphaZero = 0;
    public float minDistanceAlphaZero = 0;
    public float maxDistanceAlpha = 0.9f;
    public float minDistanceAlpha = 0.5f;
    public float maxBumpScale = 2;
    public float maxGlossiness = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float xzRotation = speed * (Random.value + 1) / divisionFactor;
        transform.Rotate(Vector3.down, xzRotation);

        float xyRotation = speed2 * Random.value / (divisionFactor * 2) - (1 / (divisionFactor * 4));
        transform.Rotate(Vector3.forward, xyRotation);

        if (CurrentDistance != prevDistance)
        {
            SetAlpha();
        }
        prevDistance = CurrentDistance;
    }

    float CurrentDistance
    {
        get
        {
            Vector3 cameraVector = cam.transform.position - mainMap.geoSphere.transform.position;
            float globeDistance = cameraVector.magnitude - mainMap.geoSphere.Radius;
            return globeDistance / initialDistance;
        }
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

        Color _BaseColor = material.GetColor("_Color");
        _BaseColor.a = alpha;
        material.SetColor("_Color", _BaseColor);

        float bumpScale = maxBumpScale * alpha;
        float glossiness = maxGlossiness * alpha;

        material.SetFloat("_Glossiness", glossiness);
        material.SetFloat("_BumpScale", bumpScale);
    }
}
