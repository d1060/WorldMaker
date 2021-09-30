using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathSmoother
{
    int numPeriods = 2;

    #region Singleton
    static PathSmoother myInstance = null;

    PathSmoother()
    {
    }

    public static PathSmoother instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new PathSmoother();
            return myInstance;
        }
    }
    #endregion

    public Vector3[] SmoothPath(Vector3[] path, float minDistanceFromCenter)
    {
        Vector3[] smoothedPath = new Vector3[path.Length];
        for (int i = 0; i < path.Length; i++)
        {
            int actualPeriodsToAverage = i < numPeriods ? i : ( (path.Length - 1 - i) > numPeriods ? numPeriods : (path.Length - 1 - i) );
            if (actualPeriodsToAverage == 0)
            {
                smoothedPath[i] = path[i];
                continue;
            }
            Vector3[] vectorsToSmooth = new Vector3[actualPeriodsToAverage * 2 + 1];
            for (int a = 0; a < actualPeriodsToAverage * 2 + 1; a++)
            {
                vectorsToSmooth[a] = path[i + (a - actualPeriodsToAverage)];
            }
            Vector3 avgVec = vectorsToSmooth.Average();
            avgVec.Normalize();
            avgVec *= minDistanceFromCenter;
            smoothedPath[i] = avgVec;
        }
        return smoothedPath;
    }

    public Vector3[] SmoothPath2D(Vector3[] path)
    {
        Vector3[] smoothedPath = new Vector3[path.Length];
        for (int i = 0; i < path.Length; i++)
        {
            int actualPeriodsToAverage = i < numPeriods ? i : ((path.Length - 1 - i) > numPeriods ? numPeriods : (path.Length - 1 - i));
            if (actualPeriodsToAverage == 0)
            {
                smoothedPath[i] = path[i];
                continue;
            }
            double sumX = 0;
            double sumY = 0;
            for (int a = 0; a < actualPeriodsToAverage * 2 + 1; a++)
            {
                sumX += path[i + (a - actualPeriodsToAverage)].x;
                sumY += path[i + (a - actualPeriodsToAverage)].y;
            }
            double avgX = sumX /= (actualPeriodsToAverage * 2 + 1);
            double avgY = sumY /= (actualPeriodsToAverage * 2 + 1);
            Vector3 avgVec = new Vector3((float)avgX, (float)avgY, path[i].z);
            smoothedPath[i] = avgVec;
        }
        return smoothedPath;
    }
}
