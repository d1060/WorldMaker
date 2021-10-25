using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Rivers
{
    #region Singleton
    static Rivers myInstance = null;

    Rivers()
    {
    }

    public static Rivers instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new Rivers();
            return myInstance;
        }
    }
    #endregion

    public int numIterations;
    public int textureWidth;
    public int textureHeight;
    public float waterLevel;
    public float flowHeightDelta;
    public float startingAlpha;
    public Color riverColor;
    public float heightWeight;
    public float brushSize;
    public float brushExponent;
    public int numParallelThreads = 8;
    public float[] heightMap;

    float alphaStep = (2 / 255f);
    uint[] dropPoints;
    Color[] flowMap;
    List<Thread> threads = null;
    Thread controller;
    int iterationCount = 0;
    Texture2D flowTex;

    public void Init(ref Texture2D flowTex)
    {
        try
        {
            this.flowTex = flowTex;
            System.Random random = new System.Random();
            dropPoints = new uint[numIterations];
            for (int i = 0; i < numIterations; i++)
            {
                Vector3 pointInSpace = new Vector3((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5));
                Vector2 pointInMap = pointInSpace.CartesianToPolarRatio(1);
                uint mapX = (uint)(pointInMap.x * textureWidth);
                uint mapY = (uint)(pointInMap.y * textureHeight);

                uint dropPointIndex = mapY * (uint)textureWidth + mapX;
                if (heightMap[dropPointIndex] <= waterLevel)
                {
                    i--;
                    continue;
                }
                dropPoints[i] = dropPointIndex;
            }

            flowMap = new Color[textureWidth * textureHeight];
            flowMap = flowTex.GetPixels();
        }
        catch (Exception e)
        {
            Debug.Log("Error initializing Incise Flow: " + e.Message + "\n" + e.StackTrace);
        }
    }

    public void StartThreads()
    {
        if (numParallelThreads > 1)
        {
            controller = new Thread(ThreadController);
            controller.Name = "River Builder Controller Thread ";
            controller.Start();
        }
        else
        {
            iterationCount = 0;
        }
    }

    public void WaitForThreads()
    {
        while (controller != null && controller.IsAlive)
        {
            Thread.Sleep(500);
        }
    }

    public void ThreadController()
    {
        threads = new List<Thread>();

        iterationCount = 0;
        while (iterationCount < numIterations)
        {
            for (int i = threads.Count; i < numParallelThreads && i < (numIterations - iterationCount); i++)
            {
                Thread worker = new Thread(new ParameterizedThreadStart(riverThread));
                worker.Name = "River Builder Thread " + iterationCount;
                worker.Start(iterationCount);
                threads.Add(worker);
                iterationCount++;
            }

            Thread.Sleep(500);

            for (int t = 0; t < threads.Count; t++)
            {
                Thread thread = threads[t];
                if (thread == null || !thread.IsAlive)
                {
                    threads.RemoveAt(t);
                    t--;
                }
            }
        }

        bool areAllThreadsDone = false;
        while (!areAllThreadsDone)
        {
            areAllThreadsDone = true;
            foreach (Thread thread in threads)
            {
                if (thread == null || thread.IsAlive)
                {
                    areAllThreadsDone = false;
                    break;
                }
            }
            Thread.Sleep(500);
        }
    }

    public void riverThread(object oIterationIndex)
    {
        int iterationIndex = (int)oIterationIndex;

        uint index = dropPoints[iterationIndex];
        Vector2i position = new Vector2i(((int)index % textureWidth), ((int)index / textureWidth));

        // Find closest point below water on a grid of gridSearch x gridSearch
        Vector2i closestUnderwater = new Vector2i(0, 0);
        FindClosestUnderwaterPoint(position, out closestUnderwater);

        if (closestUnderwater.x == -1 && closestUnderwater.y == -1)
            return;

        Dictionary<Vector2i, Vector2i> flowVectors = new Dictionary<Vector2i, Vector2i>();
        Color[] thisFlowMap = new Color[textureWidth * textureHeight];
        Array.Copy(flowMap, thisFlowMap, thisFlowMap.Length);

        float[] thisHeightMap = new float[heightMap.Length];
        Array.Copy(heightMap, thisHeightMap, thisFlowMap.Length);

        Dictionary<int, float> erosionInstructions = new Dictionary<int, float>();
        Dictionary<int, Color> riverColorInstructions = new Dictionary<int, Color>();

        // Finds the A* path bewteen origin point and destination.
        AStar(position, closestUnderwater, thisHeightMap, thisFlowMap, flowVectors, erosionInstructions, riverColorInstructions);
        PerformInstructions(erosionInstructions, riverColorInstructions);
    }

    public int RunStep()
    {
        if (numParallelThreads <= 1)
        {
            riverThread(iterationCount++);
        }
        return iterationCount;
    }

    public void Finalize(ref Texture2D flowTex)
    {
        flowTex.SetPixels(flowMap);
        flowTex.Apply();
    }

    void FindClosestUnderwaterPoint(Vector2i xy, out Vector2i closest)
    {
        closest = new Vector2i(-1, -1);
        int index = xy.x + xy.y * textureWidth;
        float originalHeight = heightMap[index];
        List<int> lowerXsToBlock = new List<int>();
        List<int> higherXsToBlock = new List<int>();
        List<int> lowerYsToBlock = new List<int>();
        List<int> higherYsToBlock = new List<int>();

        for (int i = 1; i < textureHeight / 2; i++)
        {
            for (int gridX = xy.x - i; gridX <= xy.x + i; gridX += 1)
            {
                int actualGridX = gridX;
                if (actualGridX < 0) actualGridX += textureWidth;
                if (actualGridX >= textureWidth) actualGridX -= textureWidth;

                for (int gridY = xy.y - i; gridY <= xy.y + i; gridY += 1)
                {
                    if (gridY < 0 || gridY >= textureHeight)
                        continue;

                    if (gridX == xy.x - i || gridX == xy.x + i || gridY == xy.y - i || gridY == xy.y + i)
                    {
                        if ((gridX == xy.x - i && lowerYsToBlock.Contains(gridY)) ||
                            (gridX == xy.x + i && higherYsToBlock.Contains(gridY)) ||
                            (gridY == xy.y - i && lowerXsToBlock.Contains(gridX)) ||
                            (gridY == xy.y + i && higherXsToBlock.Contains(gridX)))
                            continue;

                        //This is a grid Corner.
                        int indexOfHeightToTest = gridY * textureWidth + actualGridX;
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
                        else if (height <= waterLevel)
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

    void AStar(Vector2i origin, Vector2i target, float[] thisHeightMap, Color[] thisFlowMap, Dictionary<Vector2i, Vector2i> flowVectors, Dictionary<int, float> erosionInstructions, Dictionary<int, Color> riverColorInstructions)
    {
        List<Vector2i> openSet = new List<Vector2i>();
        openSet.Add(origin);
        List<Vector2i> closedSet = new List<Vector2i>();

        Dictionary<Vector2i, Vector2i> cameFrom = new Dictionary<Vector2i, Vector2i>();

        Dictionary<Vector2i, float> gScores = new Dictionary<Vector2i, float>();
        gScores.Add(origin, 0);

        Dictionary<Vector2i, float> fScores = new Dictionary<Vector2i, float>();
        fScores.Add(origin, GetHCost(origin, target, ref thisHeightMap));

        while (openSet.Count > 0)
        {
            Vector2i current = GetLowestScorePoint(openSet, fScores);
            float currentHeight = thisHeightMap[current.ToIndex(textureWidth)];

            if (current == target || currentHeight <= waterLevel)
            {
                // Found a path.
                TracePath(current, cameFrom, thisFlowMap, ref thisHeightMap, flowVectors, erosionInstructions, riverColorInstructions);
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

                float neighborHeight = thisHeightMap[neighbor.ToIndex(textureWidth)];
                Color neighborColor = thisFlowMap[neighbor.ToIndex(textureWidth)];

                if (neighborHeight <= waterLevel || neighborColor.a > 0)
                {
                    if (cameFrom.ContainsKey(neighbor))
                        cameFrom[neighbor] = current;
                    else
                        cameFrom.Add(neighbor, current);

                    // Underwater Neighbor. Ends the pathfinding.
                    TracePath(neighbor, cameFrom, thisFlowMap, ref thisHeightMap, flowVectors, erosionInstructions, riverColorInstructions);
                    return;
                }

                float newGScore = GetNeighborWeight(current, neighbor, neighborDistance, ref thisHeightMap);
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
                        fScores[neighbor] = gScores[neighbor] + GetHCost(neighbor, target, ref thisHeightMap);
                    else
                        fScores.Add(neighbor, gScores[neighbor] + GetHCost(neighbor, target, ref thisHeightMap));

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

        if (lastX < 0) lastX += textureWidth;
        if (nextX >= textureWidth) nextX -= textureWidth;
        if (lastY < 0) lastY = 0;
        if (nextY >= textureHeight) nextY = textureHeight - 1;

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

    float GetNeighborWeight(Vector2i from, Vector2i to, float neighborDistance, ref float[] thisHeightMap)
    {
        float pointHeight = thisHeightMap[from.ToIndex(textureWidth)];
        float targetHeight = thisHeightMap[to.ToIndex(textureWidth)];
        float heightDelta = (targetHeight - pointHeight) * heightWeight;
        if (heightDelta > 0)
            heightDelta *= 10;
        heightDelta += neighborDistance;
        return heightDelta;
    }

    float GetHCost(Vector2i point, Vector2i target, ref float[] thisHeightMap)
    {
        float pointHeight = thisHeightMap[point.ToIndex(textureWidth)];
        float targetHeight = thisHeightMap[point.ToIndex(textureWidth)];
        Vector2i vector = target - point;
        float heightDelta = (targetHeight - pointHeight) * heightWeight;
        if (heightDelta > 0)
            heightDelta *= 10;
        heightDelta += vector.magnitude;
        return heightDelta;
    }

    void TracePath(Vector2i point, Dictionary<Vector2i, Vector2i> cameFrom, Color[] thisFlowMap, ref float[] thisHeightMap, Dictionary<Vector2i, Vector2i> flowVectors, Dictionary<int, float> erosionInstructions, Dictionary<int, Color> riverColorInstructions)
    {
        List<Vector2i> pointsToErode = new List<Vector2i>();
        Vector2i current = point;
        float highestHeightInPath = float.MinValue;
        Vector2i highestHeightPoint = new Vector2i(-1, -1);
        while (cameFrom.ContainsKey(current))
        {
            int index = current.ToIndex(textureWidth);
            float height = thisHeightMap[index];
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
        float alpha = startingAlpha;
        for (int i = pointsToErode.Count - 1; i >= 0; i--)
        {
            Vector2i erodePoint = pointsToErode[i];
            int erodePointIndex = erodePoint.ToIndex(textureWidth);

            if (i == pointsToErode.Count - 1)
            {
                lastHeight = thisHeightMap[erodePointIndex];
            }

            if (i > 0)
            {
                if (!flowVectors.ContainsKey(pointsToErode[i]))
                    flowVectors.Add(pointsToErode[i], pointsToErode[i - 1]);
            }
            ErodeHeightsAround(erodePoint, ref thisHeightMap, ref lastHeight, ref alpha, erosionInstructions, riverColorInstructions, false);

            if (i == 0 && flowVectors.ContainsKey(erodePoint))
            {
                while (flowVectors.ContainsKey(erodePoint))
                {
                    erodePoint = flowVectors[erodePoint];
                    erodePointIndex = erodePoint.ToIndex(textureWidth);
                    float pointAlpha = thisFlowMap[erodePointIndex].a;

                    if (pointAlpha > 0 && pointAlpha <= 1)
                    {
                        if (!riverColorInstructions.ContainsKey(erodePointIndex))
                        {
                            Color c = riverColor;
                            c.a = alpha;
                            riverColorInstructions.Add(erodePointIndex, c);
                        }
                        else
                        {
                            Color c = riverColorInstructions[erodePointIndex];
                            c.a += alpha;
                            riverColorInstructions[erodePointIndex] = c;
                        }
                    }
                    else if (pointAlpha > 1)
                    {
                        float newAlpha = thisFlowMap[erodePointIndex].a - 1;
                        ErodeHeightsAround(erodePoint, ref thisHeightMap, ref lastHeight, ref newAlpha, erosionInstructions, riverColorInstructions, true);
                    }
                }
            }
        }
    }

    void ErodeHeightsAround(Vector2i point, ref float[] thisHeightMap, ref float lastHeight, ref float alpha, Dictionary<int, float> erosionInstructions, Dictionary<int, Color> riverColorInstructions, bool setAlphaToOne)
    {
        float pointHeight = thisHeightMap[point.ToIndex(textureWidth)];
        float thisflowHeightDelta = flowHeightDelta;
        if (lastHeight < pointHeight)
        {
            thisflowHeightDelta += pointHeight - lastHeight;
        }
        else
            alpha += alphaStep;

        if (alpha > 1)
            thisflowHeightDelta *= alpha;

        int pointIndex = point.ToIndex(textureWidth);
        if (thisHeightMap[pointIndex] - thisflowHeightDelta > waterLevel)
        {
            if (!riverColorInstructions.ContainsKey(pointIndex))
            {
                Color c = riverColor;
                c.a = setAlphaToOne ? 1 : alpha;
                riverColorInstructions.Add(pointIndex, c);
            }
            else
            {
                Color c = riverColorInstructions[pointIndex];
                c = riverColor;
                c.a = setAlphaToOne ? 1 : alpha;
                riverColorInstructions[pointIndex] = c;
            }

            //flowMap[pointIndex] = riverColor;
            //flowMap[pointIndex].a = setAlphaToOne ? 1 : alpha;
        }

        if (brushSize <= 0)
            return;

        for (int i = 0; i < 2 * brushSize + 1; i++)
        {
            int yDelta = (int)(i - brushSize);
            int brushY = point.y + yDelta;
            if (brushY < 0 || brushY >= textureHeight)
                continue;

            for (int j = 0; j < 2 * brushSize + 1; j++)
            {
                int xDelta = (int)(j - brushSize);
                int brushX = point.x + xDelta;
                if (brushX < 0) brushX += textureWidth;
                if (brushX >= textureWidth) brushX -= textureWidth;

                float distanceToCenter = Mathf.Sqrt(yDelta * yDelta + xDelta * xDelta);
                if (distanceToCenter > brushSize)
                    continue;

                float heightRatio = (brushSize - distanceToCenter) / brushSize;
                heightRatio = Mathf.Pow(Mathf.Abs(heightRatio), brushExponent);

                float heightToDecrease = thisflowHeightDelta * heightRatio;
                Vector2i brushPoint = new Vector2i(brushX, brushY);
                float brushPointHeight = thisHeightMap[brushPoint.ToIndex(textureWidth)];
                brushPointHeight -= heightToDecrease;

                int brushPointIndex = brushPoint.ToIndex(textureWidth);
                if (!erosionInstructions.ContainsKey(brushPointIndex))
                {
                    erosionInstructions.Add(brushPointIndex, brushPointHeight);
                }
                else
                {
                    erosionInstructions[brushPointIndex] = brushPointHeight;
                }

                //lock (heightMap)
                //{
                    //heightMap[brushPoint.ToIndex(textureWidth)] = brushPointHeight;
                //}
            }
        }
    }

    void PerformInstructions(Dictionary<int, float> erosionInstructions, Dictionary<int, Color> riverColorInstructions)
    {
        if (erosionInstructions.Count > 0)
        {
            lock (heightMap)
            {
                foreach (KeyValuePair<int, float> kvp in erosionInstructions)
                {
                    heightMap[kvp.Key] = kvp.Value;
                }
            }
        }

        if (riverColorInstructions.Count > 0)
        {
            lock (flowMap)
            {
                foreach (KeyValuePair<int, Color> kvp in riverColorInstructions)
                {
                    flowMap[kvp.Key] = kvp.Value;
                }
            }
        }
    }
}

