#ifndef INTERPOLATE_FUNC
#define INTERPOLATE_FUNC

float interpolate(RWStructuredBuffer<float> map, float2 coordinates)
{
    int leftX = floor(coordinates.x);
    int rightX = ceil(coordinates.x);
    int bottomY = floor(coordinates.y);
    int topY = ceil(coordinates.y);

    float deltaX = coordinates.x - leftX;
    float deltaY = coordinates.y - bottomY;

    if (rightX >= mapWidth) rightX -= mapWidth;
    if (rightX < 0) rightX += mapWidth;
    if (leftX >= mapWidth) leftX -= mapWidth;
    if (leftX < 0) leftX += mapWidth;
    if (topY >= mapHeight) topY = mapHeight - 1;
    if (topY < 0) topY = 0;
    if (bottomY >= mapHeight) bottomY = mapHeight - 1;
    if (bottomY < 0) bottomY = 0;

    int indexBL = leftX + mapWidth * bottomY;
    int indexBR = rightX + mapWidth * bottomY;
    int indexTR = rightX + mapWidth * topY;
    int indexTL = leftX + mapWidth * topY;

    float valueBL = map[indexBL];
    float valueBR = map[indexBR];
    float valueTL = map[indexTL];
    float valueTR = map[indexTR];

    float valueXdelta0 = (valueBR - valueBL) * deltaX + valueBL;
    float valueXdelta1 = (valueTR - valueTL) * deltaX + valueTL;

    float value = (valueXdelta1 - valueXdelta0) * deltaY + valueXdelta0;
    return value;
}

#endif