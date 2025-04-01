#define inf 1000000.0
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#define M_PI 3.1415926

static int m_perm[512] = { 6, 245, 484, 211, 450, 177, 416, 143, 382, 109, 348, 75, 314, 41, 280, 7, 246, 485, 212, 451, 178, 417, 144, 383, 110, 349, 76, 315, 42, 281, 8, 247, 486, 213, 452, 179, 418, 145, 384, 111, 350, 77, 316, 43, 282, 9, 248, 487, 214, 453, 180, 419, 146, 385, 112, 351, 78, 317, 44, 283, 10, 249, 488, 215, 454, 181, 420, 147, 386, 113, 352, 79, 318, 45, 284, 11, 250, 489, 216, 455, 182, 421, 148, 387, 114, 353, 80, 319, 46, 285, 12, 251, 490, 217, 456, 183, 422, 149, 388, 115, 354, 81, 320, 47, 286, 13, 252, 491, 218, 457, 184, 423, 150, 389, 116, 355, 82, 321, 48, 287, 14, 253, 492, 219, 458, 185, 424, 151, 390, 117, 356, 83, 322, 49, 288, 15, 254, 493, 220, 459, 186, 425, 152, 391, 118, 357, 84, 323, 50, 289, 16, 255, 494, 221, 460, 187, 426, 153, 392, 119, 358, 85, 324, 51, 290, 17, 256, 495, 222, 461, 188, 427, 154, 393, 120, 359, 86, 325, 52, 291, 18, 257, 496, 223, 462, 189, 428, 155, 394, 121, 360, 87, 326, 53, 292, 19, 258, 497, 224, 463, 190, 429, 156, 395, 122, 361, 88, 327, 54, 293, 20, 259, 498, 225, 464, 191, 430, 157, 396, 123, 362, 89, 328, 55, 294, 21, 260, 499, 226, 465, 192, 431, 158, 397, 124, 363, 90, 329, 56, 295, 22, 261, 500, 227, 466, 193, 432, 159, 398, 125, 364, 91, 330, 57, 296, 23, 262, 501, 228, 467, 194, 433, 160, 399, 126, 365, 92, 331, 58, 297, 24, 263, 502, 229, 468, 195, 434, 161, 400, 127, 366, 93, 332, 59, 298, 25, 264, 503, 230, 469, 196, 435, 162, 401, 128, 367, 94, 333, 60, 299, 26, 265, 504, 231, 470, 197, 436, 163, 402, 129, 368, 95, 334, 61, 300, 27, 266, 505, 232, 471, 198, 437, 164, 403, 130, 369, 96, 335, 62, 301, 28, 267, 506, 233, 472, 199, 438, 165, 404, 131, 370, 97, 336, 63, 302, 29, 268, 507, 234, 473, 200, 439, 166, 405, 132, 371, 98, 337, 64, 303, 30, 269, 508, 235, 474, 201, 440, 167, 406, 133, 372, 99, 338, 65, 304, 31, 270, 509, 236, 475, 202, 441, 168, 407, 134, 373, 100, 339, 66, 305, 32, 271, 510, 237, 476, 203, 442, 169, 408, 135, 374, 101, 340, 67, 306, 33, 272, 511, 238, 477, 204, 443, 170, 409, 136, 375, 102, 341, 68, 307, 34, 273, 0, 239, 478, 205, 444, 171, 410, 137, 376, 103, 342, 69, 308, 35, 274, 1, 240, 479, 206, 445, 172, 411, 138, 377, 104, 343, 70, 309, 36, 275, 2, 241, 480, 207, 446, 173, 412, 139, 378, 105, 344, 71, 310, 37, 276, 3, 242, 481, 208, 447, 174, 413, 140, 379, 106, 345, 72, 311, 38, 277, 4, 243, 482, 209, 448, 175, 414, 141, 380, 107, 346, 73, 312, 39, 278, 5, 244, 483, 210, 449, 176, 415, 142, 381, 108, 347, 74, 313, 40, 279 };
static float F4 = 0.30901699437494742410229341718282f;
static float G4 = 0.13819660112501051517954131656344f;

static float GRAD_4D[] =
{
	0,1,1,1,0,1,1,-1,0,1,-1,1,0,1,-1,-1,
	0,-1,1,1,0,-1,1,-1,0,-1,-1,1,0,-1,-1,-1,
	1,0,1,1,1,0,1,-1,1,0,-1,1,1,0,-1,-1,
	-1,0,1,1,-1,0,1,-1,-1,0,-1,1,-1,0,-1,-1,
	1,1,0,1,1,1,0,-1,1,-1,0,1,1,-1,0,-1,
	-1,1,0,1,-1,1,0,-1,-1,-1,0,1,-1,-1,0,-1,
	1,1,1,0,1,1,-1,0,1,-1,1,0,1,-1,-1,0,
	-1,1,1,0,-1,1,-1,0,-1,-1,1,0,-1,-1,-1,0
};

int Index4D_32(int offset, int x, int y, int z, int w)
{
	return m_perm[(x & 0xff) + m_perm[(y & 0xff) + m_perm[(z & 0xff) + m_perm[(((w & 0xff) + offset) * 7919) % 512]]]] & 31;
}

float GradCoord4D(int offset, int x, int y, int z, int w, float xd, float yd, float zd, float wd)
{
	int lutPos = Index4D_32(offset, x, y, z, w) << 2;
	return xd * GRAD_4D[lutPos] + yd * GRAD_4D[lutPos + 1] + zd * GRAD_4D[lutPos + 2] + wd * GRAD_4D[lutPos + 3];
}

int FastFloor(float f)
{
	return (f >= 0 ? (int)f : (int)f - 1);
}

float snoise(float4 coords, int offset = 0)
{
	float x = coords.x;
	float y = coords.y;
	float z = coords.z;
	float w = coords.w;

	float n0, n1, n2, n3, n4;
	float t = (x + y + z + w) * F4;
	int i = FastFloor(x + t);
	int j = FastFloor(y + t);
	int k = FastFloor(z + t);
	int l = FastFloor(w + t);
	t = (i + j + k + l) * G4;
	float X0 = i - t;
	float Y0 = j - t;
	float Z0 = k - t;
	float W0 = l - t;
	float x0 = x - X0;
	float y0 = y - Y0;
	float z0 = z - Z0;
	float w0 = w - W0;

	int rankx = 0;
	int ranky = 0;
	int rankz = 0;
	int rankw = 0;

	if (x0 > y0) rankx++; else ranky++;
	if (x0 > z0) rankx++; else rankz++;
	if (x0 > w0) rankx++; else rankw++;
	if (y0 > z0) ranky++; else rankz++;
	if (y0 > w0) ranky++; else rankw++;
	if (z0 > w0) rankz++; else rankw++;

	int i1 = rankx >= 3 ? 1 : 0;
	int j1 = ranky >= 3 ? 1 : 0;
	int k1 = rankz >= 3 ? 1 : 0;
	int l1 = rankw >= 3 ? 1 : 0;

	int i2 = rankx >= 2 ? 1 : 0;
	int j2 = ranky >= 2 ? 1 : 0;
	int k2 = rankz >= 2 ? 1 : 0;
	int l2 = rankw >= 2 ? 1 : 0;

	int i3 = rankx >= 1 ? 1 : 0;
	int j3 = ranky >= 1 ? 1 : 0;
	int k3 = rankz >= 1 ? 1 : 0;
	int l3 = rankw >= 1 ? 1 : 0;

	float x1 = x0 - i1 + G4;
	float y1 = y0 - j1 + G4;
	float z1 = z0 - k1 + G4;
	float w1 = w0 - l1 + G4;
	float x2 = x0 - i2 + 2 * G4;
	float y2 = y0 - j2 + 2 * G4;
	float z2 = z0 - k2 + 2 * G4;
	float w2 = w0 - l2 + 2 * G4;
	float x3 = x0 - i3 + 3 * G4;
	float y3 = y0 - j3 + 3 * G4;
	float z3 = z0 - k3 + 3 * G4;
	float w3 = w0 - l3 + 3 * G4;
	float x4 = x0 - 1 + 4 * G4;
	float y4 = y0 - 1 + 4 * G4;
	float z4 = z0 - 1 + 4 * G4;
	float w4 = w0 - 1 + 4 * G4;

	t = 0.6f - x0 * x0 - y0 * y0 - z0 * z0 - w0 * w0;
	if (t < 0) n0 = 0;
	else
	{
		t *= t;
		n0 = t * t * GradCoord4D(offset, i, j, k, l, x0, y0, z0, w0);
	}
	t = 0.6f - x1 * x1 - y1 * y1 - z1 * z1 - w1 * w1;
	if (t < 0) n1 = 0;
	else
	{
		t *= t;
		n1 = t * t * GradCoord4D(offset, i + i1, j + j1, k + k1, l + l1, x1, y1, z1, w1);
	}
	t = 0.6f - x2 * x2 - y2 * y2 - z2 * z2 - w2 * w2;
	if (t < 0) n2 = 0;
	else
	{
		t *= t;
		n2 = t * t * GradCoord4D(offset, i + i2, j + j2, k + k2, l + l2, x2, y2, z2, w2);
	}
	t = 0.6f - x3 * x3 - y3 * y3 - z3 * z3 - w3 * w3;
	if (t < 0) n3 = 0;
	else
	{
		t *= t;
		n3 = t * t * GradCoord4D(offset, i + i3, j + j3, k + k3, l + l3, x3, y3, z3, w3);
	}
	t = 0.6f - x4 * x4 - y4 * y4 - z4 * z4 - w4 * w4;
	if (t < 0) n4 = 0;
	else
	{
		t *= t;
		n4 = t * t * GradCoord4D(offset, i + 1, j + 1, k + 1, l + 1, x4, y4, z4, w4);
	}

	return 27 * (n0 + n1 + n2 + n3 + n4);
}

float fbm(float4 coords, float4 offset, int seed, float multiplier, int octaves, float lacunarity, float persistence, int ridged)
{
	coords /= multiplier;
	coords += offset;

	float maxValue = 0;
	float amplitude = 1;
	float val = 0;
	for (int n = 0; n < octaves; n++)
	{
		float noiseValue = snoise(coords, seed) / amplitude;

		if (ridged > 0)
			noiseValue = 1 - abs(noiseValue);
		else
		{
			noiseValue += 1;
			noiseValue /= 2;
		}

		noiseValue *= amplitude;
		val += noiseValue;
		maxValue += amplitude;
		coords *= lacunarity;
		amplitude *= persistence;
	}

	val /= maxValue;
	if (val < 0) val = 0;

	return val;
}

float3 UvToSphere(float2 coords)
{
	float lon = coords.x * 2 * M_PI;
	float lat = (coords.y - 0.5) * M_PI;

	float a = cos(lat);
	float y = sin(lat);
	float z = a * sin(lon);
	float x = a * cos(lon);

	return float3(x, y, z);
}

float2 SphereToUv(float3 cartesian)
{
	float2 polar;
	float xzAtan2 = 0;

	if (cartesian.x == 0)
	{
		if (cartesian.z > 0)
			xzAtan2 = (M_PI / 2);
		else
			xzAtan2 = -(M_PI / 2);
	}
	else
		xzAtan2 = atan2(cartesian.z, cartesian.x);

	polar.x = xzAtan2;

	polar.y = asin(cartesian.y);

	polar.x /= (2 * M_PI);
	polar.y /= M_PI;

	if (polar.x < 0) polar.x += 1;
	polar.y += 0.5;
	return polar;
}

float sphere4DNoise(float2 uv, float timeframe, float4 offset, int seed, float multiplier, int octaves, float lacunarity, float persistence, int ridged, float domainWarping)
{
	float3 sphereCoords = UvToSphere(uv) + 1;
	float4 coords = float4(sphereCoords.x, sphereCoords.y, sphereCoords.z, timeframe);
	float noiseValue = 0;

	if (domainWarping > 0)
	{
		float4 coordsOffset1 = float4(offset.y, offset.z, offset.w, offset.x);
		float4 coordsOffset2 = float4(offset.z, offset.w, offset.x, offset.y);
		float4 coordsOffset3 = float4(offset.w, offset.x, offset.y, offset.z);
		float4 coordsOffset4 = float4(offset.x, offset.y, offset.z, offset.w);

		float qCoordsX = fbm(coords + coordsOffset1, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
		float qCoordsY = fbm(coords + coordsOffset2, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
		float qCoordsZ = fbm(coords + coordsOffset3, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
		float qCoordsW = fbm(coords + coordsOffset4, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);

		float4 qCoords = float4(qCoordsX, qCoordsY, qCoordsZ, qCoordsW);

		noiseValue = fbm(coords + domainWarping * qCoords, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
	}
	else
	{
		noiseValue = fbm(coords, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
	}

	return noiseValue;
}

float sphereHeight(float2 coords, float timeframe, float4 offset, int seed, float multiplier, int octaves, float lacunarity, float persistence, int ridged, float heightExponent, float domainWarping,
	float minHeight, float maxHeight)
{
	float height = sphere4DNoise(coords, timeframe, offset, seed, multiplier, octaves, lacunarity, persistence, ridged, domainWarping);

	height = pow(abs(height), heightExponent);
	return height;
}
