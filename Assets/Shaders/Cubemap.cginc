#ifndef CUBEMAP_FUNC
#define CUBEMAP_FUNC

//
//       CUBEMAP Outline
//
//         +--------+
//         |  Top   |
//         |   +y   |
// +-------+--------+-------+------+
// |  Left |  Front | Right | Back |
// |  -x   |   +z   |  +x   |  -z  |
// +-------+--------+-------+------+
//         | Bottom |
//         |   -y   |
//         +--------+
//
//                  ARRAY Outline
//
// +-------+-------+-------+------+------+--------+
// |  Left | Front | Right | Back | Top  | Bottom |
// |  -x   |  +z   |  +x   |  -z  |  +y  |   -y   |
// +-------+-------+-------+------+------+--------+
//

float3 cartesianToCubemap(float3 cartesian)
{
	float3 cubeMap;

	float tanZx = cartesian.z / cartesian.x;
	float tanYx = cartesian.y / cartesian.x;

	float tanZy = cartesian.z / cartesian.y;
	float tanXy = cartesian.x / cartesian.y;

	float tanXz = cartesian.x / cartesian.z;
	float tanYz = cartesian.y / cartesian.z;

	if (cartesian.x < 0 && (tanYx >= -1 && tanYx <= 1) && (tanZx >= -1 && tanZx <= 1)) // neg_x - Left
	{
		cubeMap.x = (1 - tanZx) / 2;
		cubeMap.y = (1 - tanYx) / 2;
		cubeMap.z = 0;
	}
	else if (cartesian.y < 0 && (tanXy >= -1 && tanXy <= 1) && (tanZy >= -1 && tanZy <= 1)) // neg_y - Bottom
	{
		cubeMap.x = (1 - tanXy) / 2;
		cubeMap.y = (1 - tanZy) / 2;
		cubeMap.z = 5;
	}
	else if (cartesian.z < 0 && (tanYz >= -1 && tanYz <= 1) && (tanXz >= -1 && tanXz <= 1)) // neg_z - Back
	{
		cubeMap.x = (tanXz + 1) / 2;
		cubeMap.y = (1 - tanYz) / 2;
		cubeMap.z = 3;
	}
	else if (cartesian.z > 0 && (tanYz >= -1 && tanYz <= 1) && (tanXz >= -1 && tanXz <= 1)) // pos_z - Front
	{
		cubeMap.x = (tanXz + 1) / 2;
		cubeMap.y = (tanYz + 1) / 2;
		cubeMap.z = 1;
	}
	else if (cartesian.x > 0 && (tanYx >= -1 && tanYx <= 1) && (tanZx >= -1 && tanZx <= 1)) // pos_x - Right
	{
		cubeMap.x = (1 - tanZx) / 2;
		cubeMap.y = (tanYx + 1) / 2;
		cubeMap.z = 2;
	}
	else if (cartesian.y > 0 && (tanXy >= -1 && tanXy <= 1) && (tanZy >= -1 && tanZy <= 1)) // pos_y - Top
	{
		cubeMap.x = (tanXy + 1) / 2;
		cubeMap.y = (1 - tanZy) / 2;
		cubeMap.z = 4;
	}
	return cubeMap;
}

float3 cubemapToCartesian(float2 cubeMap, int faceId)
{
	float x = 0;
	float y = 0;
	float z = 0;

	if (faceId == 0) // neg_x - Left - Z Starts from -dimension/2 to dimension/2
	{                // cubeMap.x is Z
		x = -1;
		z = 1 - 2 * cubeMap.x;
		y = 2 * cubeMap.y - 1;
	}
	else if (faceId == 1) // pos_z - Back
	{                     // cubeMap.x is X
		z = -1;
		x = 2 * cubeMap.x - 1;
		y = 2 * cubeMap.y - 1;
	}
	else if (faceId == 2) // pos_x - Right
	{                     // cubeMap.x is -Z
		x = 1;
		z = 2 * cubeMap.x - 1;
		y = 2 * cubeMap.y - 1;
	}
	else if (faceId == 3) // neg_z - Front
	{                     // cubeMap.x is -X
		z = 1;
		x = 1 - 2 * cubeMap.x;
		y = 2 * cubeMap.y - 1;
	}
	else if (faceId == 4) // pos_y - Top - Aligns with pos_z
	{                     // cubeMap.x is X, cubeMap.y is Z
		y = 1;
		x = 2 * cubeMap.x - 1;
		z = 2 * cubeMap.y - 1;
	}
	else if (faceId == 5) // neg_y - Bottom - Aligns with pos_z
	{                     // cubeMap.x is X, cubeMap.y is -Z
		y = -1;
		x = 2 * cubeMap.x - 1;
		z = 1 - 2 * cubeMap.y;
	}
	float3 cartesian = float3(x, y, z);
	cartesian = normalize(cartesian);
	return cartesian;
}

float2 cartesianToPolarRatio(float3 cartesian)
{
	float2 polar;
	float xzAtan2 = 0;

	if (cartesian.x == 0)
	{
		if (cartesian.z > 0)
			xzAtan2 = 1.570796326795; // PI / 2
		else
			xzAtan2 = -1.570796326795; // -PI / 2
	}
	else
		xzAtan2 = atan2(cartesian.z, cartesian.x);

	polar.x = xzAtan2;

	polar.y = asin(cartesian.y);

	polar.x *= 57.29578; // Rad2Deg;
	polar.y *= 57.29578; // Rad2Deg;

	polar.x /= 360;
	polar.y /= 180;

	polar.x += 0.5;
	polar.y += 0.5;
	return polar;
}

#define UV_PI 3.14159265359
float3 uvToCartesian(float2 coords)
{
	float lon = (0.5 - coords.x) * 2 * UV_PI;
	float lat = (coords.y - 0.5) * UV_PI;
	float sinLon = sin(lon); // co / h
	float cosLon = cos(lon); // ca / h

	if (cosLon == 0 && lon > (UV_PI / 2) - 0.01 && lon < UV_PI / 2 + 0.01)
	{
		cosLon = UV_PI / 2 - lon;
	}
	else if (cosLon == 0 && lon > (3 * UV_PI / 2) - 0.01 && lon < 3 * UV_PI / 2 + 0.01)
	{
		cosLon = lon - 3 * UV_PI / 2;
	}

	if (sinLon == 0 && lon < 0.01 && lon > 0)
	{
		sinLon = lon;
	}
	else if (sinLon == 0 && lon > UV_PI - 0.01 && lon < UV_PI + 0.01)
	{
		sinLon = UV_PI - lon;
	}
	else if (sinLon == 0 && lon > (2 * UV_PI) - 0.01)
	{
		sinLon = 2 * UV_PI - lon;
	}

	float a = cos(lat);
	float y = sin(lat);
	float z = a * sinLon;
	float x = a * cosLon;

	return float3(x, y, z);
}

float2 cubemapXYZtoUV(uint x, uint y, uint z, uint radius)
{
	float2 uv;
	float2 cubeMap;

	cubeMap.y = y / ((float)radius);
	cubeMap.x = x / ((float)radius);

	float3 cartesian = cubemapToCartesian(cubeMap, z);
	return cartesianToPolarRatio(cartesian);
}

float2 cubemapToUV(uint3 cubemap, uint radius)
{
	return cubemapXYZtoUV(cubemap.x, cubemap.y, cubemap.z, radius);
}

//uint3 toCubemapCoordinates(uint x, uint y, uint mapWidth)
//{
//	int face = x / mapWidth;
//	int actualX = x - (face * mapWidth);
//	return uint3(actualX, y, face);
//}

uint toIndex(int3 cubemap, uint mapWidth)
{
	return cubemap.x + cubemap.z * mapWidth + cubemap.y * 6 * mapWidth;
}

int3 fromIndex(uint index, uint mapWidth)
{
	int x = index % (mapWidth * 6);
	int z = x / mapWidth;
	x -= z * mapWidth;
	int y = index / (mapWidth * 6);

	return int3(x, y, z);
}

float3 getNewCoordinates(float3 cubemap_input, float deltaX, float deltaY, int mapWidth)
{
	float3 cubemap = float3(cubemap_input.x / mapWidth, cubemap_input.y / mapWidth, cubemap_input.z);
	cubemap.x += deltaX / mapWidth;
	float3 coordinates = float3(cubemap.x, cubemap.y, cubemap.z);

	if (cubemap.x < 0)
	{
		if (cubemap.z == 0)
		{
			coordinates.x += 1;
			coordinates.z = 3;
		}
		else if (cubemap.z == 1)
		{
			coordinates.x += 1;
			coordinates.z = 0;
		}
		else if (cubemap.z == 2)
		{
			coordinates.x += 1;
			coordinates.z = 1;
		}
		else if (cubemap.z == 3)
		{
			coordinates.x += 1;
			coordinates.z = 2;
		}
		else if (cubemap.z == 4)
		{
			coordinates.y = 1 + cubemap.x;
			coordinates.x = 1 - cubemap.y;
			coordinates.z = 0;
		}
		else
		{
			coordinates.y = -cubemap.x;
			coordinates.x = cubemap.y;
			coordinates.z = 0;
		}
	}
	else if (cubemap.x >= 1)
	{
		if (cubemap.z == 0)
		{
			coordinates.x -= 1;
			coordinates.z = 1;
		}
		else if (cubemap.z == 1)
		{
			coordinates.x -= 1;
			coordinates.z = 2;
		}
		else if (cubemap.z == 2)
		{
			coordinates.x -= 1;
			coordinates.z = 3;
		}
		else if (cubemap.z == 3)
		{
			coordinates.x -= 1;
			coordinates.z = 0;
		}
		else if (cubemap.z == 4)
		{
			coordinates.x = cubemap.y;
			coordinates.y = 2 - cubemap.x - 0.000001;
			coordinates.z = 2;
		}
		else
		{
			coordinates.x = 1 - cubemap.y;
			coordinates.y = cubemap.x - 1;
			coordinates.z = 2;
		}
	}

	cubemap = coordinates;
	cubemap.y += deltaY / mapWidth;
	coordinates.y += deltaY / mapWidth;

	if (cubemap.y < 0)
	{
		if (cubemap.z == 0)
		{
			coordinates.x = -cubemap.y;
			coordinates.y = cubemap.x;
			coordinates.z = 5;
		}
		else if (cubemap.z == 1)
		{
			coordinates.x = cubemap.x;
			coordinates.y += 1;
			coordinates.z = 5;
		}
		else if (cubemap.z == 2)
		{
			coordinates.x = 1 + cubemap.y;
			coordinates.y = 1 - cubemap.x;
			coordinates.z = 5;
		}
		else if (cubemap.z == 3)
		{
			coordinates.x = 1 - cubemap.x;
			coordinates.y = -cubemap.y;
			coordinates.z = 5;
		}
		else if (cubemap.z == 4)
		{
			coordinates.x = cubemap.x;
			coordinates.y = 1 + cubemap.y;
			coordinates.z = 1;
		}
		else
		{
			coordinates.x = 1 - cubemap.x;
			coordinates.y = -cubemap.y;
			coordinates.z = 3;
		}
	}
	else if (cubemap.y >= 1)
	{
		if (cubemap.z == 0)
		{
			coordinates.x = cubemap.y - 1;
			coordinates.y = 1 - cubemap.x;
			coordinates.z = 4;
		}
		else if (cubemap.z == 1)
		{
			coordinates.x = cubemap.x;
			coordinates.y = cubemap.y - 1;
			coordinates.z = 4;
		}
		else if (cubemap.z == 2)
		{
			coordinates.x = 2 - cubemap.y - 0.000001;
			coordinates.y = cubemap.x;
			coordinates.z = 4;
		}
		else if (cubemap.z == 3)
		{
			coordinates.x = 1 - cubemap.x;
			coordinates.y = 2 - cubemap.y - 0.000001;
			coordinates.z = 4;
		}
		else if (cubemap.z == 4)
		{
			coordinates.x = 1 - cubemap.x;
			coordinates.y = 2 - cubemap.y - 0.000001;
			coordinates.z = 3;
		}
		else
		{
			coordinates.x = cubemap.x;
			coordinates.y = cubemap.y - 1;
			coordinates.z = 1;
		}
	}

	return float3(coordinates.x * mapWidth, coordinates.y * mapWidth, coordinates.z);
}

int3 getLeftCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewCoordinates(float3(cubemap.x, cubemap.y, cubemap.z), -1, 0, mapWidth);
	return newCoordinates;
}

int3 getRightCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewCoordinates(float3(cubemap.x, cubemap.y, cubemap.z), 1, 0, mapWidth);
	return newCoordinates;
}

int3 getTopCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewCoordinates(float3(cubemap.x, cubemap.y, cubemap.z), 0, 1, mapWidth);
	return newCoordinates;
}

int3 getBottomCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewCoordinates(float3(cubemap.x, cubemap.y, cubemap.z), 0, -1, mapWidth);
	return newCoordinates;
}

int3 getTopLeftCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewCoordinates(float3(cubemap.x, cubemap.y, cubemap.z), -1, 1, mapWidth);
	return newCoordinates;
}

int3 getBottomRightCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewCoordinates(float3(cubemap.x, cubemap.y, cubemap.z), 1, -1, mapWidth);
	return newCoordinates;
}

int3 getTopRightCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewCoordinates(float3(cubemap.x, cubemap.y, cubemap.z), 1, 1, mapWidth);
	return newCoordinates;
}

int3 getBottomLeftCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewCoordinates(float3(cubemap.x, cubemap.y, cubemap.z), -1, -1, mapWidth);
	return newCoordinates;
}

int3 getNewIntCoordinates(int3 cubemap, int deltaX, int deltaY, int mapWidth)
{
	cubemap.x += deltaX;
	int3 coordinates = cubemap;

	if (cubemap.x < 0)
	{
		if (cubemap.z == 0)
		{
			coordinates.x += mapWidth;
			coordinates.z = 3;
		}
		else if (cubemap.z == 1)
		{
			coordinates.x += mapWidth;
			coordinates.z = 0;
		}
		else if (cubemap.z == 2)
		{
			coordinates.x += mapWidth;
			coordinates.z = 1;
		}
		else if (cubemap.z == 3)
		{
			coordinates.x += mapWidth;
			coordinates.z = 2;
		}
		else if (cubemap.z == 4)
		{
			coordinates.y = mapWidth + cubemap.x;
			coordinates.x = mapWidth - cubemap.y - 1;
			coordinates.z = 0;
		}
		else
		{
			coordinates.y = -cubemap.x + 1;
			coordinates.x = cubemap.y;
			coordinates.z = 0;
		}
	}
	else if (cubemap.x >= mapWidth)
	{
		if (cubemap.z == 0)
		{
			coordinates.x -= mapWidth;
			coordinates.z = 1;
		}
		else if (cubemap.z == 1)
		{
			coordinates.x -= mapWidth;
			coordinates.z = 2;
		}
		else if (cubemap.z == 2)
		{
			coordinates.x -= mapWidth;
			coordinates.z = 3;
		}
		else if (cubemap.z == 3)
		{
			coordinates.x -= mapWidth;
			coordinates.z = 0;
		}
		else if (cubemap.z == 4)
		{
			coordinates.x = cubemap.y;
			coordinates.y = mapWidth - (cubemap.x - mapWidth) - 1;
			coordinates.z = 2;
		}
		else
		{
			coordinates.x = mapWidth - cubemap.y - 1;
			coordinates.y = cubemap.x - mapWidth;
			coordinates.z = 2;
		}
	}

	cubemap = coordinates;
	cubemap.y += deltaY;
	coordinates.y += deltaY;

	if (cubemap.y < 0)
	{
		if (cubemap.z == 0)
		{
			coordinates.x = -cubemap.y - 1;
			coordinates.y = cubemap.x;
			coordinates.z = 5;
		}
		else if (cubemap.z == 1)
		{
			coordinates.x = cubemap.x;
			coordinates.y += mapWidth;
			coordinates.z = 5;
		}
		else if (cubemap.z == 2)
		{
			coordinates.x = mapWidth + cubemap.y;
			coordinates.y = mapWidth - cubemap.x - 1;
			coordinates.z = 5;
		}
		else if (cubemap.z == 3)
		{
			coordinates.x = mapWidth - cubemap.x - 1;
			coordinates.y = -cubemap.y - 1;
			coordinates.z = 5;
		}
		else if (cubemap.z == 4)
		{
			coordinates.x = cubemap.x;
			coordinates.y = mapWidth + cubemap.y;
			coordinates.z = 1;
		}
		else
		{
			coordinates.x = mapWidth - cubemap.x - 1;
			coordinates.y = -cubemap.y - 1;
			coordinates.z = 3;
		}
	}
	else if (cubemap.y >= mapWidth)
	{
		if (cubemap.z == 0)
		{
			coordinates.x = cubemap.y - mapWidth;
			coordinates.y = mapWidth - cubemap.x - 1;
			coordinates.z = 4;
		}
		else if (cubemap.z == 1)
		{
			coordinates.x = cubemap.x;
			coordinates.y = cubemap.y - mapWidth;
			coordinates.z = 4;
		}
		else if (cubemap.z == 2)
		{
			coordinates.x = mapWidth - (cubemap.y - mapWidth) - 1;
			coordinates.y = cubemap.x;
			coordinates.z = 4;
		}
		else if (cubemap.z == 3)
		{
			coordinates.x = mapWidth - cubemap.x - 1;
			coordinates.y = 2 * mapWidth - cubemap.y - 1;
			coordinates.z = 4;
		}
		else if (cubemap.z == 4)
		{
			coordinates.x = mapWidth - cubemap.x - 1;
			coordinates.y = 2 * mapWidth - cubemap.y - 1;
			coordinates.z = 3;
		}
		else
		{
			coordinates.x = cubemap.x;
			coordinates.y = cubemap.y - mapWidth;
			coordinates.z = 1;
		}
	}

	return coordinates;
}

int3 getLeftIntCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewIntCoordinates(cubemap, -1, 0, mapWidth);
	return newCoordinates;
}

int3 getRightIntCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewIntCoordinates(cubemap, 1, 0, mapWidth);
	return newCoordinates;
}

int3 getTopIntCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewIntCoordinates(cubemap, 0, 1, mapWidth);
	return newCoordinates;
}

int3 getBottomIntCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewIntCoordinates(cubemap, 0, -1, mapWidth);
	return newCoordinates;
}

int3 getTopLeftIntCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewIntCoordinates(cubemap, -1, 1, mapWidth);
	return newCoordinates;
}

int3 getBottomRightIntCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewIntCoordinates(cubemap, 1, -1, mapWidth);
	return newCoordinates;
}

int3 getTopRightIntCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewIntCoordinates(cubemap, 1, 1, mapWidth);
	return newCoordinates;
}

int3 getBottomLeftIntCoordinates(int3 cubemap, int mapWidth)
{
	int3 newCoordinates = getNewIntCoordinates(cubemap, -1, -1, mapWidth);
	return newCoordinates;
}

float Interpolate(float valueDL, float valueDR, float valueUL, float valueUR, float Xrate, float Yrate)
{
	float valueR = valueDR * (1 - Yrate) + valueUR * Yrate;
	float valueL = valueDL * (1 - Yrate) + valueUL * Yrate;

	float value = valueL * (1 - Xrate) + valueR * Xrate;
	return value;
}

#endif