#ifndef INDEXES_CODE
#define INDEXES_CODE

int index = id.x + mapWidth * id.y;

int leftX = id.x - 1;
int rightX = id.x + 1;
int topY = id.y - 1;
int bottomY = id.y + 1;

if (leftX < 0) leftX += mapWidth;
if (rightX >= mapWidth) rightX -= mapWidth;
if (bottomY >= mapHeight) bottomY = mapHeight - 1;
if (topY < 0) topY = 0;

int indexL = leftX + mapWidth * id.y;
int indexR = rightX + mapWidth * id.y;
int indexU = id.x + mapWidth * topY;
int indexD = id.x + mapWidth * bottomY;
int indexDL = leftX + mapWidth * bottomY;
int indexDR = rightX + mapWidth * bottomY;
int indexUL = leftX + mapWidth * topY;
int indexUR = rightX + mapWidth * topY;
#endif