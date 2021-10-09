using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class FlatMap : MonoBehaviour
{
    public int meshSubdivisions = 256;
    public int width = 200;
    public int height = 100;

    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] normals;

    int prevDivisions = 5;
    int prevWidth = 200;
    int prevHeight = 100;

    // Start is called before the first frame update
    void Start()
    {
        prevDivisions = meshSubdivisions;
        prevWidth = width;
        prevHeight = height;

        BuildArrays();
        BuildGameObject();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnValidate()
    {
        if (prevDivisions != meshSubdivisions || prevWidth != width || prevHeight != height)
        {
            BuildArrays();
            BuildGameObject();

            prevDivisions = meshSubdivisions;
            prevWidth = width;
            prevHeight = height;
        }
    }

    void BuildArrays()
    {
        int xDivisions = 0;
        int yDivisions = 0;

        if (width > height)
        {
            xDivisions = 3 * meshSubdivisions;
            yDivisions = (int)(((float)height / (float)width) * meshSubdivisions);
        }
        else
        {
            yDivisions = meshSubdivisions;
            xDivisions = 3 * ((int)(((float)width / (float)height) * meshSubdivisions));
        }

        float xStep = (float)3 * width / xDivisions;
        float yStep = (float)height / yDivisions;

        vertices = new Vector3[(xDivisions +1) * (yDivisions + 1)];
        triangles = new int[xDivisions * yDivisions * 2 * 3];
        uvs = new Vector2[(xDivisions + 1) * (yDivisions + 1)];
        normals = new Vector3[(xDivisions + 1) * (yDivisions + 1)];

        int index = 0;
        int trianglesIndex = 0;
        for (float y = 0; y <= height; y += yStep)
        {
            float v = y / height;
            for (float x = -width; x <= 2 * width; x += xStep)
            {
                Vector3 vertex = new Vector3(x - (width/2), y - (height/2), 0);
                Vector3 normal = new Vector3(0, 0, -1);
                float u = (x + width) / width;
                Vector2 uv = new Vector2(u, v);

                vertices[index] = vertex;
                normals[index] = normal;
                uvs[index] = uv;

                if (x > -width && y > 0)
                {
                    int lastXindex = index - 1;
                    int lastYindex = index - (int)(3 * width / xStep) - 1;
                    int lastXYindex = index - (int)(3 * width / xStep) - 2;

                    triangles[trianglesIndex++] = index;
                    triangles[trianglesIndex++] = lastYindex;
                    triangles[trianglesIndex++] = lastXindex;

                    triangles[trianglesIndex++] = lastXindex;
                    triangles[trianglesIndex++] = lastYindex;
                    triangles[trianglesIndex++] = lastXYindex;
                }

                index++;
            }
        }
    }

    void BuildGameObject()
    {
        // Builds Game Object
#if UNITY_EDITOR
        EditorApplication.delayCall += () => {
#endif
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = vertices;
            meshFilter.sharedMesh.triangles = triangles;
            meshFilter.sharedMesh.uv = uvs;
            meshFilter.sharedMesh.normals = normals;
#if UNITY_EDITOR
        };
#endif
    }
}
