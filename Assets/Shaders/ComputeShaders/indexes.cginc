#ifndef INDEXES_CODE
#define INDEXES_CODE

int index = id.x + mapWidth * id.y;

int leftX = id.x - 1;
int rightX = id.x + 1;
int topY = id.y + 1;
int bottomY = id.y - 1;

if (leftX < 0) leftX += mapWidth;
if (rightX >= mapWidth) rightX -= mapWidth;
if (topY >= mapHeight) topY = mapHeight - 1;
if (bottomY < 0) bottomY = 0;

int indexLeft = leftX + mapWidth * id.y;
int indexRight = rightX + mapWidth * id.y;
int indexTop = id.x + mapWidth * topY;
int indexBottom = id.x + mapWidth * bottomY;

#endif