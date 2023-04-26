#ifndef INDEXES_CODE
#define INDEXES_CODE

int index = id.x + mapWidth * id.y;

uint3 cubemap = uint3(id.x, id.y, id.z);

int indexL = toIndex(getLeftCoordinates(cubemap, mapWidth), mapWidth);
int indexR = toIndex(getRightCoordinates(cubemap, mapWidth), mapWidth);
int indexT = toIndex(getTopCoordinates(cubemap, mapWidth), mapWidth);
int indexB = toIndex(getBottomCoordinates(cubemap, mapWidth), mapWidth);

int indexTL = toIndex(getTopLeftCoordinates(cubemap, mapWidth), mapWidth);
int indexBR = toIndex(getBottomRightCoordinates(cubemap, mapWidth), mapWidth);
int indexTR = toIndex(getTopRightCoordinates(cubemap, mapWidth), mapWidth);
int indexBL = toIndex(getBottomLeftCoordinates(cubemap, mapWidth), mapWidth);
#endif