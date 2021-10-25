using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PlotRivers
{
    public TextureSettings textureSettings;
    public PlotRiversSettings plotRiversSettings;
    float alphaStep = (2 / 255f);
    uint[] dropPoints;
    Color[] flowMap;
    Dictionary<Vector2i, Vector2i> flowVectors;

    #region Singleton
    static PlotRivers myInstance = null;

    PlotRivers()
    {
    }

    public static PlotRivers instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new PlotRivers();
            return myInstance;
        }
    }
    #endregion

    public void Init(ref float[] heightMap, ref Texture2D flowTex)
    {
        try
        {
            System.Random random = new System.Random();
            dropPoints = new uint[plotRiversSettings.numIterations];
            for (int i = 0; i < plotRiversSettings.numIterations; i++)
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

            flowMap = new Color[textureSettings.textureWidth * textureSettings.textureHeight];
            flowMap = flowTex.GetPixels();
            flowVectors = new Dictionary<Vector2i, Vector2i>();
        }
        catch (Exception e)
        {
            Debug.Log("Error initializing Incise Flow: " + e.Message + "\n" + e.StackTrace);
        }
    }

    public void RunStep(ref float[] heightMap, int idx)
    {
        uint index = dropPoints[idx];
        Vector2i position = new Vector2i(((int)index % textureSettings.textureWidth), ((int)index / textureSettings.textureWidth));

        // Find closest point below water on a grid of gridSearch x gridSearch
        Vector2i closestUnderwater = new Vector2i(0, 0);
        FindClosestUnderwaterPoint(position, out closestUnderwater, ref heightMap);

        if (closestUnderwater.x == -1 && closestUnderwater.y == -1)
            return;

        // Finds the A* path bewteen origin point and destination.
        AStar(position, closestUnderwater, ref heightMap, flowMap, flowVectors);
    }

    public void Finalize(ref Texture2D flowTex)
    {
        flowTex.SetPixels(flowMap);
        flowTex.Apply();
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
        int index = xy.x + xy.y * textureSettings.textureWidth;
        float originalHeight = heightMap[index];
        List<int> lowerXsToBlock = new List<int>();
        List<int> higherXsToBlock = new List<int>();
        List<int> lowerYsToBlock = new List<int>();
        List<int> higherYsToBlock = new List<int>();

        for (int i = 1; i < textureSettings.textureHeight / 2; i ++)
        {
            for (int gridX = xy.x - i; gridX <= xy.x + i; gridX += 1)
            {
                int actualGridX = gridX;
                if (actualGridX < 0) actualGridX += textureSettings.textureWidth;
                if (actualGridX >= textureSettings.textureWidth) actualGridX -= textureSettings.textureWidth;

                for (int gridY = xy.y - i; gridY <= xy.y + i; gridY += 1)
                {
                    if (gridY < 0 || gridY >= textureSettings.textureHeight)
                        continue;

                    if (gridX == xy.x - i || gridX == xy.x + i || gridY == xy.y - i || gridY == xy.y + i)
                    {
                        if ((gridX == xy.x - i && lowerYsToBlock.Contains(gridY)) ||
                            (gridX == xy.x + i && higherYsToBlock.Contains(gridY)) ||
                            (gridY == xy.y - i && lowerXsToBlock.Contains(gridX)) ||
                            (gridY == xy.y + i && higherXsToBlock.Contains(gridX)))
                            continue;

                        //This is a grid Corner.
                        int indexOfHeightToTest = gridY * textureSettings.textureWidth + actualGridX;
                        float height = heightMap[indexOfHeightToTest];

                        if (height > originalHeight)
                        {
                            if (gridX == xy.x - i)
                                lowerYsToBlock.Add(gridY);
                            else if (gridX == xy.x + i)
                                higherYsToBlock.Add(gridY);
                            else if (gridY == xy.y - i)
                                lowerXsToBlock.Add(gridX);
                            else if (gridY == xy.y + i)
                                higherXsToBlock.Add(gridX);
                        }
                        else if (height <= textureSettings.waterLevel)
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
        float heightDelta = (targetHeight - pointHeight) * plotRiversSettings.heightWeight;
        if (heightDelta > 0)
            heightDelta *= 10;
        heightDelta += neighborDistance;
        return heightDelta;
    }

    float GetHCost(Vector2i point, Vector2i target, ref float[] heightMap)
    {
        float pointHeight = heightMap[point.ToIndex(textureSettings.textureWidth)];
        float targetHeight = heightMap[point.ToIndex(textureSettings.textureWidth)];
        Vector2i vector = target - point;
        float heightDelta = (targetHeight - pointHeight) * plotRiversSettings.heightWeight;
        if (heightDelta > 0)
            heightDelta *= 10;
        heightDelta += vector.magnitude;
        return heightDelta;
    }

    void TracePath(Vector2i point, Dictionary<Vector2i, Vector2i> cameFrom, Color[] flowMap, ref float[] heightMap, Dictionary<Vector2i, Vector2i> flowVectors)
    {
        List<Vector2i> pointsToErode = new List<Vector2i>();
        Vector2i current = point;
        float highestHeightInPath = float.MinValue;
        Vector2i highestHeightPoint = new Vector2i(-1, -1);
        while (cameFrom.ContainsKey(current))
        {
            int index = current.ToIndex(textureSettings.textureWidth);
            float height = heightMap[index];
            if (height > highestHeightInPath)
            {
                highestHeightInPath = height;
                highestHeightPoint = current;
            }
            pointsToErode.Add(current);
            current = cameFrom[current];
        }

        // Removes points lower than the highest height at the start of the list.
        while (pointsToErode.Count > 0 && pointsToErode[pointsToErode.Count - 1] != highestHeightPoint)
        {
            pointsToErode.RemoveAt(0);
        }

        // Erodes from Start to Finish.
        float lastHeight = float.MaxValue;
        float alpha = plotRiversSettings.startingAlpha;
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
        float flowHeightDelta = plotRiversSettings.flowHeightDelta;
        if (lastHeight < pointHeight)
        {
            flowHeightDelta += pointHeight - lastHeight;
        }
        else
            alpha += alphaStep;

        if (alpha > 1)
            flowHeightDelta *= alpha;

        int pointIndex = point.ToIndex(textureSettings.textureWidth);
        if (heightMap[pointIndex] - plotRiversSettings.flowHeightDelta > textureSettings.waterLevel)
        {
            flowMap[pointIndex] = plotRiversSettings.riverColor;
            flowMap[pointIndex].a = setAlphaToOne ? 1 : alpha;
        }

        if (plotRiversSettings.brushSize <= 0)
            return;

        for (int i = 0; i < 2 * plotRiversSettings.brushSize + 1; i++)
        {
            int yDelta = (int)(i - plotRiversSettings.brushSize);
            int brushY = point.y + yDelta;
            if (brushY < 0 || brushY >= textureSettings.textureHeight)
                continue;

            for (int j = 0; j < 2 * plotRiversSettings.brushSize + 1; j++)
            {
                int xDelta = (int)(j - plotRiversSettings.brushSize);
                int brushX = point.x + xDelta;
                if (brushX < 0) brushX += textureSettings.textureWidth;
                if (brushX >= textureSettings.textureWidth) brushX -= textureSettings.textureWidth;

                float distanceToCenter = Mathf.Sqrt(yDelta * yDelta + xDelta * xDelta);
                if (distanceToCenter > plotRiversSettings.brushSize)
                    continue;

                float heightRatio = (plotRiversSettings.brushSize - distanceToCenter) / plotRiversSettings.brushSize;
                heightRatio = Mathf.Pow(Mathf.Abs(heightRatio), plotRiversSettings.brushExponent);

                float heightToDecrease = plotRiversSettings.flowHeightDelta * heightRatio;
                Vector2i brushPoint = new Vector2i(brushX, brushY);
                float brushPointHeight = heightMap[brushPoint.ToIndex(textureSettings.textureWidth)];
                brushPointHeight -= heightToDecrease;
                heightMap[brushPoint.ToIndex(textureSettings.textureWidth)] = brushPointHeight;
            }
        }
    }
}
