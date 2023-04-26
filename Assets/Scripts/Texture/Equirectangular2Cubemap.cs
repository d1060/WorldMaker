using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Equirectangular2Cubemap
{
    #region Singleton
    static Equirectangular2Cubemap myInstance = null;

    Equirectangular2Cubemap()
    {
    }

    public static Equirectangular2Cubemap instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new Equirectangular2Cubemap();
            return myInstance;
        }
    }
    #endregion

    public int mapWidth;
    public int mapHeight;
    public int cubemapDimension;
    public int subdivision;
    public int subDivisionX;
    public int subDivisionY;
    public int faceId;
    public Texture2D baseTex;
    public Texture2D Result;

    const float PI = 3.14159274f;
    const float Rad2Deg = 57.29578f;

    Color bilinear(Vector2 coord)
    {
        int leftX = (int)Mathf.Floor(coord.x);
        int rightX = (int)Mathf.Ceil(coord.x);
        int bottomY = (int)Mathf.Floor(coord.y);
        int topY = (int)Mathf.Ceil(coord.y);

        float deltaX = coord.x - leftX;
        float deltaY = coord.y - bottomY;

        if (rightX >= mapWidth) rightX -= mapWidth;
        if (rightX < 0) rightX += mapWidth;
        if (leftX >= mapWidth) leftX -= mapWidth;
        if (leftX < 0) leftX += mapWidth;
        if (topY >= mapHeight) topY = mapHeight - 1;
        if (topY < 0) topY = 0;
        if (bottomY >= mapHeight) bottomY = mapHeight - 1;
        if (bottomY < 0) bottomY = 0;

        Vector2i indexBL = new Vector2i(leftX, bottomY);
        Vector2i indexBR = new Vector2i(rightX, bottomY);
        Vector2i indexTR = new Vector2i(rightX, topY);
        Vector2i indexTL = new Vector2i(leftX, topY);

        Color valueBL = baseTex.GetPixel(indexBL.x, indexBL.y);
        Color valueBR = baseTex.GetPixel(indexBR.x, indexBR.y);
        Color valueTL = baseTex.GetPixel(indexTL.x, indexTL.y);
        Color valueTR = baseTex.GetPixel(indexTR.x, indexTR.y);

        Color valueXdelta0 = (valueBR - valueBL) * deltaX + valueBL;
        Color valueXdelta1 = (valueTR - valueTL) * deltaX + valueTL;

        Color value = (valueXdelta1 - valueXdelta0) * deltaY + valueXdelta0;
        return value;
    }

    Vector2 cartesianToPolarRatio(Vector3 cartesian)
    {
        Vector2 polar;
        float xzAtan2 = 0;

        if (cartesian.x == 0)
        {
            if (cartesian.z > 0)
                xzAtan2 = PI / 2.0f;
            else
                xzAtan2 = -PI / 2.0f;
        }
        else
            xzAtan2 = Mathf.Atan2(cartesian.z, cartesian.x);

        polar.x = xzAtan2;

        polar.y = Mathf.Asin(cartesian.y);

        polar.x *= Rad2Deg;
        polar.y *= Rad2Deg;

        polar.x /= 360;
        polar.y /= 180;

        polar.x += 0.5f;
        polar.y += 0.5f;
        return polar;
    }

    Vector3 cubemapToCartesian(Vector2 cubeMap, int faceId)
    {
        float x = 0;
        float y = 0;
        float z = 0;

        if (faceId == 0) // neg_x - Left - Z Starts from -dimension/2 to dimension/2
        {                // cubeMap.x is Z
            x = -1;
            z = (2 * cubeMap.x - 1);
            y = (2 * cubeMap.y - 1);
        }
        else if (faceId == 1) // pos_z - Back
        {                     // cubeMap.x is X
            z = 1;
            x = (2 * cubeMap.x - 1);
            y = (2 * cubeMap.y - 1);
        }
        else if (faceId == 2) // pos_x - Right
        {                     // cubeMap.x is -Z
            x = -1;
            z = (1 - 2 * cubeMap.x);
            y = (2 * cubeMap.y - 1);
        }
        else if (faceId == 3) // neg_z - Front
        {                     // cubeMap.x is -X
            z = -1;
            x = (1 - 2 * cubeMap.x);
            y = (2 * cubeMap.y - 1);
        }
        else if (faceId == 4) // pos_y - Top - Aligns with pos_z
        {                     // cubeMap.x is X, cubeMap.y is -Z
            y = 1;
            x = (2 * cubeMap.x - 1);
            z = (1 - 2 * cubeMap.y);
        }
        else if (faceId == 5) // neg_y - Bottom - Aligns with pos_z
        {                     // cubeMap.x is X, cubeMap.y is -Z
            y = -1;
            x = (2 * cubeMap.x - 1);
            z = (1 - 2 * cubeMap.y);
        }

        Vector3 cartesian = new Vector3(x, y, z);
        cartesian.Normalize();
        return cartesian;
    }

    void CSMain(int x, int y)
    {
        if (x >= (uint)cubemapDimension || y >= (uint)cubemapDimension)
            return;

        Vector2 cubeMap = new Vector2(x / (float)cubemapDimension, y / (float)cubemapDimension);
        if (subdivision > 0)
        {
            float divisionPower = Mathf.Pow(2, subdivision);
            float pixelsPerDivision = cubemapDimension / divisionPower;
            if (subDivisionX >= 0 && subDivisionX < divisionPower)
            {
                float minX = pixelsPerDivision * subDivisionX / cubemapDimension;
                cubeMap.x = minX + cubeMap.x / divisionPower;
            }
            if (subDivisionY >= 0 && subDivisionY < divisionPower)
            {
                float minY = pixelsPerDivision * (divisionPower - subDivisionY - 1) / cubemapDimension;
                cubeMap.y = minY + cubeMap.y / divisionPower;
            }
        }
        Vector3 cartesian = cubemapToCartesian(cubeMap, faceId);
        Vector2 polar = cartesianToPolarRatio(cartesian);
        // Shifts for the center meridian
        polar.x -= 0.125f;
        if (polar.x < 0) polar.x += 1;

        polar.x *= mapWidth;
        polar.y *= mapHeight;

        Color color = bilinear(polar);

        Result.SetPixel(x, y, color);
    }

    public void Run()
    {
        for (int x = 0; x < cubemapDimension; x++)
        {
            for (int y = 0; y < cubemapDimension; y++)
            {
                CSMain(x, y);
            }
        }
    }
}
