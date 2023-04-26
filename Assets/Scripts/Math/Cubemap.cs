using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Cubemap
{
	public static Vector3 getNewCoordinates(Vector3 cubemap_input, float deltaX, float deltaY, int mapWidth)
	{
		Vector3 cubemap = new Vector3(cubemap_input.x / mapWidth, cubemap_input.y / mapWidth, cubemap_input.z);
		cubemap.x += deltaX / mapWidth;
		Vector3 coordinates = new Vector3(cubemap.x, cubemap.y, cubemap.z);

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
				coordinates.y = 2 - cubemap.x - 0.000001f;
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
				coordinates.x = 2 - cubemap.y - 0.000001f;
				coordinates.y = cubemap.x;
				coordinates.z = 4;
			}
			else if (cubemap.z == 3)
			{
				coordinates.x = 1 - cubemap.x;
				coordinates.y = 2 - cubemap.y - 0.000001f;
				coordinates.z = 4;
			}
			else if (cubemap.z == 4)
			{
				coordinates.x = 1 - cubemap.x;
				coordinates.y = 2 - cubemap.y - 0.000001f;
				coordinates.z = 3;
			}
			else
			{
				coordinates.x = cubemap.x;
				coordinates.y = cubemap.y - 1;
				coordinates.z = 1;
			}
		}

		return new Vector3(coordinates.x * mapWidth, coordinates.y * mapWidth, coordinates.z);
	}

	static public Int3 getLeftIntCoordinates(Int3 cubemap, int mapWidth)
	{
		Int3 newCoordinates = getNewIntCoordinates(cubemap, -1, 0, mapWidth);
		return newCoordinates;
	}

	static public Int3 getRightIntCoordinates(Int3 cubemap, int mapWidth)
	{
		Int3 newCoordinates = getNewIntCoordinates(cubemap, 1, 0, mapWidth);
		return newCoordinates;
	}

	static public Int3 getTopIntCoordinates(Int3 cubemap, int mapWidth)
	{
		Int3 newCoordinates = getNewIntCoordinates(cubemap, 0, 1, mapWidth);
		return newCoordinates;
	}

	static public Int3 getBottomIntCoordinates(Int3 cubemap, int mapWidth)
	{
		Int3 newCoordinates = getNewIntCoordinates(cubemap, 0, -1, mapWidth);
		return newCoordinates;
	}

	static public Int3 getTopLeftIntCoordinates(Int3 cubemap, int mapWidth)
	{
		Int3 newCoordinates = getNewIntCoordinates(cubemap, -1, 1, mapWidth);
		return newCoordinates;
	}

	static public Int3 getBottomRightIntCoordinates(Int3 cubemap, int mapWidth)
	{
		Int3 newCoordinates = getNewIntCoordinates(cubemap, 1, -1, mapWidth);
		return newCoordinates;
	}

	static public Int3 getTopRightIntCoordinates(Int3 cubemap, int mapWidth)
	{
		Int3 newCoordinates = getNewIntCoordinates(cubemap, 1, 1, mapWidth);
		return newCoordinates;
	}

	static public Int3 getBottomLeftIntCoordinates(Int3 cubemap, int mapWidth)
	{
		Int3 newCoordinates = getNewIntCoordinates(cubemap, -1, -1, mapWidth);
		return newCoordinates;
	}

	static public Int3 getNewIntCoordinates(Int3 cubemap_input, int deltaX, int deltaY, int mapWidth)
	{
		Int3 cubemap = new Int3(cubemap_input);
		cubemap.x += deltaX;
		Int3 coordinates = new Int3(cubemap);

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

		cubemap.x = coordinates.x;
		cubemap.y = coordinates.y;
		cubemap.z = coordinates.z;

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
}
