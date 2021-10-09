using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class InciseFlow
{
    public ComputeShader inciseFlow;

    public TextureSettings textureSettings;
    public InciseFlowSettings inciseFlowSettings;
    bool runInCpu = true;
    float alphaStep = (2 / 255f);

    #region Singleton
    static InciseFlow myInstance = null;

    InciseFlow()
    {
    }

    public static InciseFlow instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new InciseFlow();
            return myInstance;
        }
    }
    #endregion

    public void Run(ref float[] heightMap, ref Texture2D flowTex)
    {
        try
        {
            int numThreads = inciseFlowSettings.numIterations / 8;
            if (numThreads <= 0) numThreads = 1;

            System.Random random = new System.Random();
            uint[] dropPoints = new uint[inciseFlowSettings.numIterations];
            for (int i = 0; i < inciseFlowSettings.numIterations; i++)
            {
                Vector3 pointInSpace = new Vector3((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5));
                Vector2 pointInMap = pointInSpace.CartesianToPolarRatio(1);
                uint mapX = (uint)(pointInMap.x * textureSettings.textureWidth);
                uint mapY = (uint)(pointInMap.y * textureSettings.textureHeight);

                uint dropPointIndex = mapY * (uint)textureSettings.textureWidth + mapX;
                if (heightMap[dropPointIndex] <= textureSettings.waterLevel)
                {
                    i--;
                    continue;
                }
                dropPoints[i] = dropPointIndex;
            }

            if (runInCpu)
            {
                RunInCPU(ref dropPoints, ref heightMap, ref flowTex);
            }
            else
            {
                ComputeBuffer dropPointsBuffer = new ComputeBuffer(dropPoints.Length, sizeof(uint));
                dropPointsBuffer.SetData(dropPoints);

                ComputeBuffer heightMapBuffer = new ComputeBuffer(heightMap.Length, sizeof(float));
                heightMapBuffer.SetData(heightMap);

                RenderTexture flowTexture = new RenderTexture(textureSettings.textureWidth, textureSettings.textureHeight, 32, RenderTextureFormat.ARGB32);
                flowTexture.enableRandomWrite = true;
                flowTexture.Create();
                RenderTexture prevActive = RenderTexture.active;
                RenderTexture.active = flowTexture;
                Graphics.Blit(flowTex, flowTexture);

                // Settings
                inciseFlow.SetBuffer(0, "dropPoints", dropPointsBuffer);
                inciseFlow.SetBuffer(0, "heightMap", heightMapBuffer);
                inciseFlow.SetTexture(0, "flowMap", flowTexture);
                inciseFlow.SetInt("mapWidth", textureSettings.textureWidth);
                inciseFlow.SetInt("mapHeight", textureSettings.textureHeight);
                inciseFlow.SetFloat("waterLevel", textureSettings.waterLevel);
                inciseFlow.SetFloats("riverColor", new float[4] { 0, 0, 1, 1 });
                inciseFlow.SetInt("maxFlowLength", 100000);
                inciseFlow.SetFloat("flowHeightDelta", 0.05f);
                inciseFlow.SetFloat("brushSize", 2);
                inciseFlow.SetFloat("brushExponent", 1.5f);
                inciseFlow.SetFloat("inertia", 0.25f);

                // Run compute shader
                inciseFlow.Dispatch(0, numThreads, 1, 1);

                heightMapBuffer.GetData(heightMap);

                RenderTexture.active = flowTexture;
                flowTex.ReadPixels(new Rect(0, 0, flowTexture.width, flowTexture.height), 0, 0);
                flowTex.Apply();

                heightMapBuffer.Release();
                dropPointsBuffer.Release();
                RenderTexture.active = prevActive;
                flowTexture.Release();
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error performing Incise Flow: " + e.Message + "\n" + e.StackTrace);
        }
    }

    const int GRID_SEARCH_DIVISOR = 10;
    void RunInCPU(ref uint[] dropPoints, ref float[] heightMap, ref Texture2D flowTex)
    {
        int gridSearch = textureSettings.textureWidth / GRID_SEARCH_DIVISOR;
        Color[] flowMap = new Color[textureSettings.textureWidth * textureSettings.textureHeight];
        flowMap = flowTex.GetPixels();

        Dictionary<Vector2i, Vector2i> flowVectors = new Dictionary<Vector2i, Vector2i>();

        for (int idx = 0; idx < dropPoints.Length; idx++)
        {
            uint index = dropPoints[idx];
            Vector2i position = new Vector2i(((int)index % textureSettings.textureWidth), ((int)index / textureSettings.textureWidth));

            // Find closest point below water on a grid of gridSearch x gridSearch
            Vector2i closestUnderwater = new Vector2i(0, 0);
            FindClosestUnderwaterPoint(position, out closestUnderwater, ref heightMap);

            if (closestUnderwater.x == -1 && closestUnderwater.y == -1)
                continue;

            // Finds the A* path bewteen origin point and destination.
            AStar(position, closestUnderwater, ref heightMap, flowMap, flowVectors);
        }

        flowTex.SetPixels(flowMap);
        flowTex.Apply();
    }

    void FindClosestUnderwaterPoint(Vector2i xy, out Vector2i closest, ref float[] heightMap)
    {
        closest = new Vector2i(-1, -1);

        for (int i = 1; i <= GRID_SEARCH_DIVISOR; i++)
        {
            for (int gridX = xy.x - GRID_SEARCH_DIVISOR * i; gridX <= xy.x + GRID_SEARCH_DIVISOR * i; gridX += GRID_SEARCH_DIVISOR)
            {
                int actualGridX = gridX;
                if (actualGridX < 0) actualGridX += textureSettings.textureWidth;
                if (actualGridX >= textureSettings.textureWidth) actualGridX -= textureSettings.textureWidth;

                for (int gridY = xy.y - GRID_SEARCH_DIVISOR * i; gridY <= xy.y + GRID_SEARCH_DIVISOR * i; gridY += GRID_SEARCH_DIVISOR)
                {
                    if (gridY < 0 || gridY >= textureSettings.textureHeight)
                        continue;

                    if (gridX == xy.x - GRID_SEARCH_DIVISOR * i || gridX == xy.x + GRID_SEARCH_DIVISOR * i || gridY == xy.y - GRID_SEARCH_DIVISOR * i || gridY == xy.y + GRID_SEARCH_DIVISOR * i)
                    {
                        //This is a grid Corner.
                        int indexOfHeightToTest = gridY * textureSettings.textureWidth + actualGridX;
                        float height = heightMap[indexOfHeightToTest];
                        if (height <= textureSettings.waterLevel)
                        {
                            closest.x = gridX;
                            closest.y = gridY;
                            return;
                        }
                    }
                }
            }
        }
    }

    void AStar(Vector2i origin, Vector2i target, ref float[] heightMap, Color[] flowMap, Dictionary<Vector2i, Vector2i> flowVectors)
    {
        List<Vector2i> openSet = new List<Vector2i>();
        openSet.Add(origin);
        List<Vector2i> closedSet = new List<Vector2i>();

        Dictionary<Vector2i, Vector2i> cameFrom = new Dictionary<Vector2i, Vector2i>();

        Dictionary<Vector2i, float> gScores = new Dictionary<Vector2i, float>();
        gScores.Add(origin, 0);

        Dictionary<Vector2i, float> fScores = new Dictionary<Vector2i, float>();
        fScores.Add(origin, GetHCost(origin, target, ref heightMap));

        while (openSet.Count > 0)
        {
            Vector2i current = GetLowestScorePoint(openSet, fScores);
            float currentHeight = heightMap[current.ToIndex(textureSettings.textureWidth)];

            if (current == target || currentHeight <= textureSettings.waterLevel)
            {
                // Found a path.
                TracePath(current, cameFrom, flowMap, ref heightMap, flowVectors);
                return;
            }
            openSet.Remove(current);
            closedSet.Add(current);

            List<Vector2i> neighbors = GetNeighborsOf(current, closedSet);
            int i = 0;
            foreach (Vector2i neighbor in neighbors)
            {
                float neighborDistance = 1;
                if (neighbor.x != current.x && neighbor.y != current.y)
                    neighborDistance = 1.414213f;

                float neighborHeight = heightMap[neighbor.ToIndex(textureSettings.textureWidth)];
                Color neighborColor = flowMap[neighbor.ToIndex(textureSettings.textureWidth)];

                if (neighborHeight <= textureSettings.waterLevel || neighborColor.a > 0)
                {
                    if (cameFrom.ContainsKey(neighbor))
                        cameFrom[neighbor] = current;
                    else
                        cameFrom.Add(neighbor, current);

                    // Underwater Neighbor. Ends the pathfinding.
                    TracePath(neighbor, cameFrom, flowMap, ref heightMap, flowVectors);
                    return;
                }

                float newGScore = GetNeighborWeight(current, neighbor, neighborDistance, ref heightMap);
                if (gScores.ContainsKey(neighbor))
                    newGScore += gScores[neighbor];

                if (!gScores.ContainsKey(neighbor) || newGScore < gScores[neighbor])
                {
                    if (cameFrom.ContainsKey(neighbor))
                    {
                        cameFrom[neighbor] = current;
                    }
                    else
                        cameFrom.Add(neighbor, current);

                    if (gScores.ContainsKey(neighbor))
                        gScores[neighbor] = newGScore;
                    else
                        gScores.Add(neighbor, newGScore);

                    if (fScores.ContainsKey(neighbor))
                        fScores[neighbor] = gScores[neighbor] + GetHCost(neighbor, target, ref heightMap);
                    else
                        fScores.Add(neighbor, gScores[neighbor] + GetHCost(neighbor, target, ref heightMap));

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
                i++;
            }
        }
    }

    Vector2i GetLowestScorePoint(List<Vector2i> set, Dictionary<Vector2i, float> fScores)
    {
        float lowestScore = float.MaxValue;
        Vector2i lowestScorePoint = new Vector2i(0, 0);
        foreach (Vector2i point in set)
        {
            float fScore = fScores[point];
            if (fScore < lowestScore)
            {
                lowestScore = fScore;
                lowestScorePoint = point;
            }
        }
        return lowestScorePoint;
    }

    List<Vector2i> GetNeighborsOf(Vector2i point, List<Vector2i> closedSet)
    {
        int lastX = point.x - 1;
        int nextX = point.x + 1;
        int lastY = point.y - 1;
        int nextY = point.y + 1;

        if (lastX < 0) lastX += textureSettings.textureWidth;
        if (nextX >= textureSettings.textureWidth) nextX -= textureSettings.textureWidth;
        if (lastY < 0) lastY = 0;
        if (nextY >= textureSettings.textureHeight) nextY = textureSettings.textureHeight - 1;

        Vector2i point1 = new Vector2i(lastX, point.y);
        Vector2i point2 = new Vector2i(nextX, point.y);
        Vector2i point3 = new Vector2i(point.x, lastY);
        Vector2i point4 = new Vector2i(point.x, nextY);
        Vector2i point5 = new Vector2i(lastX, lastY);
        Vector2i point6 = new Vector2i(lastX, nextY);
        Vector2i point7 = new Vector2i(nextX, lastY);
        Vector2i point8 = new Vector2i(nextX, nextY);

        List<Vector2i> neighbors = new List<Vector2i>();
        if (!closedSet.Contains(point1)) neighbors.Add(point1);
        if (!closedSet.Contains(point2)) neighbors.Add(point2);
        if (!closedSet.Contains(point3)) neighbors.Add(point3);
        if (!closedSet.Contains(point4)) neighbors.Add(point4);
        if (!closedSet.Contains(point5)) neighbors.Add(point5);
        if (!closedSet.Contains(point6)) neighbors.Add(point6);
        if (!closedSet.Contains(point7)) neighbors.Add(point7);
        if (!closedSet.Contains(point8)) neighbors.Add(point8);

        return neighbors;
    }

    float GetNeighborWeight(Vector2i from, Vector2i to, float neighborDistance, ref float[] heightMap)
    {
        float pointHeight = heightMap[from.ToIndex(textureSettings.textureWidth)];
        float targetHeight = heightMap[to.ToIndex(textureSettings.textureWidth)];
        float heightDelta = (targetHeight - pointHeight) * inciseFlowSettings.heightWeight;
        heightDelta += neighborDistance;
        return heightDelta;
    }

    float GetHCost(Vector2i point, Vector2i target, ref float[] heightMap)
    {
        float pointHeight = heightMap[point.ToIndex(textureSettings.textureWidth)];
        float targetHeight = heightMap[point.ToIndex(textureSettings.textureWidth)];
        Vector2i vector = target - point;
        float heightDelta = (targetHeight - pointHeight) * inciseFlowSettings.heightWeight;
        heightDelta += vector.magnitude;
        return heightDelta;
    }

    void TracePath(Vector2i point, Dictionary<Vector2i, Vector2i> cameFrom, Color[] flowMap, ref float[] heightMap, Dictionary<Vector2i, Vector2i> flowVectors)
    {
        List<Vector2i> pointsToErode = new List<Vector2i>();
        Vector2i current = point;
        while (cameFrom.ContainsKey(current))
        {
            pointsToErode.Add(current);
            current = cameFrom[current];
        }

        // Erodes from Start to Finish.
        float lastHeight = float.MaxValue;
        float alpha = inciseFlowSettings.startingAlpha;
        for (int i = pointsToErode.Count-1; i >= 0; i--)
        {
            Vector2i erodePoint = pointsToErode[i];
            int erodePointIndex = erodePoint.ToIndex(textureSettings.textureWidth);

            if (i == pointsToErode.Count - 1)
            {
                lastHeight = heightMap[erodePointIndex];
            }

            if (i > 0)
            {
                if (!flowVectors.ContainsKey(pointsToErode[i]))
                    flowVectors.Add(pointsToErode[i], pointsToErode[i - 1]);
            }
            ErodeHeightsAround(erodePoint, ref heightMap, flowMap, ref lastHeight, ref alpha, false);

            if (i == 0 && flowVectors.ContainsKey(erodePoint))
            {
                while (flowVectors.ContainsKey(erodePoint))
                {
                    erodePoint = flowVectors[erodePoint];
                    erodePointIndex = erodePoint.ToIndex(textureSettings.textureWidth);
                    float pointAlpha = flowMap[erodePointIndex].a;

                    if (pointAlpha > 0 && pointAlpha <= 1)
                    {
                        flowMap[erodePointIndex].a += alpha;
                    }
                    else if (pointAlpha > 1)
                    {
                        float newAlpha = flowMap[erodePointIndex].a - 1;
                        ErodeHeightsAround(erodePoint, ref heightMap, flowMap, ref lastHeight, ref newAlpha, true);
                    }
                }
            }
        }
    }

    void ErodeHeightsAround(Vector2i point, ref float[] heightMap, Color[] flowMap, ref float lastHeight, ref float alpha, bool setAlphaToOne)
    {
        float pointHeight = heightMap[point.ToIndex(textureSettings.textureWidth)];
        float flowHeightDelta = inciseFlowSettings.flowHeightDelta;
        if (lastHeight < pointHeight)
        {
            flowHeightDelta += pointHeight - lastHeight;
        }
        else
            alpha += alphaStep;

        if (alpha > 1)
            flowHeightDelta *= alpha;

        int pointIndex = point.ToIndex(textureSettings.textureWidth);
        if (heightMap[pointIndex] - inciseFlowSettings.flowHeightDelta > textureSettings.waterLevel)
        {
            flowMap[pointIndex] = inciseFlowSettings.riverColor;
            flowMap[pointIndex].a = setAlphaToOne ? 1 : alpha;
        }

        if (inciseFlowSettings.brushSize <= 0)
            return;

        for (int i = 0; i < 2 * inciseFlowSettings.brushSize + 1; i++)
        {
            int yDelta = (int)(i - inciseFlowSettings.brushSize);
            int brushY = point.y + yDelta;
            if (brushY < 0 || brushY >= textureSettings.textureHeight)
                continue;

            for (int j = 0; j < 2 * inciseFlowSettings.brushSize + 1; j++)
            {
                int xDelta = (int)(j - inciseFlowSettings.brushSize);
                int brushX = point.x + xDelta;
                if (brushX < 0) brushX += textureSettings.textureWidth;
                if (brushX >= textureSettings.textureWidth) brushX -= textureSettings.textureWidth;

                float distanceToCenter = Mathf.Sqrt(yDelta * yDelta + xDelta * xDelta);
                if (distanceToCenter > inciseFlowSettings.brushSize)
                    continue;

                float heightRatio = (inciseFlowSettings.brushSize - distanceToCenter) / inciseFlowSettings.brushSize;
                heightRatio = Mathf.Pow(Mathf.Abs(heightRatio), inciseFlowSettings.brushExponent);

                float heightToDecrease = inciseFlowSettings.flowHeightDelta * heightRatio;
                Vector2i brushPoint = new Vector2i(brushX, brushY);
                float brushPointHeight = heightMap[brushPoint.ToIndex(textureSettings.textureWidth)];
                brushPointHeight -= heightToDecrease;
                heightMap[brushPoint.ToIndex(textureSettings.textureWidth)] = brushPointHeight;
            }
        }
    }
}
