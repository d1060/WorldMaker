#ifndef NOISE_SIMPLEX_FUNC
#define NOISE_SIMPLEX_FUNC

// Original Code from https://pastebin.com/BycaHKEP

// Simplex noise range is from -SQRT(N/4) to +SQRT(N/4). 0.3 is multiplied as probability tolerance.
//#define SIMPLEX_NOISE_RANGE 0.32f

//int simplexSeed;

int hash(int a, int simplexSeed)
{
    a = (a ^ 61) ^ (a >> 16);
    a = a + (a << 3);
    a = a ^ (a >> 4);
    a = a * simplexSeed;
    a = a ^ (a >> 15);
    return a;
}

/*
** 3D
*/

static float3 grad3lut[16] =
{
  { 1.0f, 0.0f, 1.0f }, { 0.0f, 1.0f, 1.0f },
  { -1.0f, 0.0f, 1.0f }, { 0.0f, -1.0f, 1.0f },
  { 1.0f, 0.0f, -1.0f }, { 0.0f, 1.0f, -1.0f },
  { -1.0f, 0.0f, -1.0f }, { 0.0f, -1.0f, -1.0f },
  { 1.0f, -1.0f, 0.0f }, { 1.0f, 1.0f, 0.0f },
  { -1.0f, 1.0f, 0.0f }, { -1.0f, -1.0f, 0.0f },
  { 1.0f, 0.0f, 1.0f }, { -1.0f, 0.0f, 1.0f },
  { 0.0f, 1.0f, -1.0f }, { 0.0f, -1.0f, -1.0f }
};

float3 grad3(int hash)
{
    return grad3lut[hash & 15];
}

#define SPHERE_PI 3.14159265359f
#define SQRT_3_4 0.8660254f

// Returns a Simplex Noise from 0 to 1.
float simplexNoise(float3 input, int simplexSeed)
{
    float n0, n1, n2, n3;
    float noise;
    float3 g0, g1, g2, g3;

    float s = (input.x + input.y + input.z) * 0.333333333;
    float3 a = input + s;
    int3 ijk = floor(a);

    float t = (float)(ijk.x + ijk.y + ijk.z) * 0.166666667;
    float3 b = ijk - t;
    float3 c = input - b;

    int3 ijk1;
    int3 ijk2;

    if (c.x >= c.y) {
        if (c.y >= c.z)
        {
            ijk1 = int3(1, 0, 0); ijk2 = int3(1, 1, 0);
        }
        else if (c.x >= c.z) { ijk1 = int3(1, 0, 0); ijk2 = int3(1, 0, 1); }
        else { ijk1 = int3(0, 0, 1); ijk2 = int3(1, 0, 1); }
    }
    else {
        if (c.y < c.z) { ijk1 = int3(0, 0, 1); ijk2 = int3(0, 1, 1); }
        else if (c.x < c.z) { ijk1 = int3(0, 1, 0); ijk2 = int3(0, 1, 1); }
        else { ijk1 = int3(0, 1, 0); ijk2 = int3(1, 1, 0); }
    }

    float3 c1 = c - ijk1 + 0.166666667;
    float3 c2 = c - ijk2 + 2.0f * 0.166666667;
    float3 c3 = c - 1.0f + 3.0f * 0.166666667;

    int ii = ijk.x & 0xff;
    int jj = ijk.y & 0xff;
    int kk = ijk.z & 0xff;

    float t0 = 0.6f - c.x * c.x - c.y * c.y - c.z * c.z;
    float t20, t40;
    if (t0 < 0.0f) n0 = t0 = t20 = t40 = g0.x = g0.y = g0.z = 0.0f;
    else {
        g0 = grad3(hash(ii + hash(jj + hash(kk, simplexSeed), simplexSeed), simplexSeed));
        t20 = t0 * t0;
        t40 = t20 * t20;
        n0 = t40 * (g0.x * c.x + g0.y * c.y + g0.z * c.z);
    }

    float t1 = 0.6f - c1.x * c1.x - c1.y * c1.y - c1.z * c1.z;
    float t21, t41;
    if (t1 < 0.0f) n1 = t1 = t21 = t41 = g1.x = g1.y = g1.z = 0.0f;
    else {
        g1 = grad3(hash(ii + ijk1.x + hash(jj + ijk1.y + hash(kk + ijk1.z, simplexSeed), simplexSeed), simplexSeed));
        t21 = t1 * t1;
        t41 = t21 * t21;
        n1 = t41 * (g1.x * c1.x + g1.y * c1.y + g1.z * c1.z);
    }

    float t2 = 0.6f - c2.x * c2.x - c2.y * c2.y - c2.z * c2.z;
    float t22, t42;
    if (t2 < 0.0f) n2 = t2 = t22 = t42 = g2.x = g2.y = g2.z = 0.0f;
    else {
        g2 = grad3(hash(ii + ijk2.x + hash(jj + ijk2.y + hash(kk + ijk2.z, simplexSeed), simplexSeed), simplexSeed));
        t22 = t2 * t2;
        t42 = t22 * t22;
        n2 = t42 * (g2.x * c2.x + g2.y * c2.y + g2.z * c2.z);
    }

    float t3 = 0.6f - c3.x * c3.x - c3.y * c3.y - c3.z * c3.z;
    float t23, t43;
    if (t3 < 0.0f) n3 = t3 = t23 = t43 = g3.x = g3.y = g3.z = 0.0f;
    else {
        g3 = grad3(hash(ii + 1 + hash(jj + 1 + hash(kk + 1, simplexSeed), simplexSeed), simplexSeed));
        t23 = t3 * t3;
        t43 = t23 * t23;
        n3 = t43 * (g3.x * c3.x + g3.y * c3.y + g3.z * c3.z);
    }

    noise = 20.0f * (n0 + n1 + n2 + n3);
	
	noise /= SQRT_3_4; // 3D Simplex Noise ranges from -Sqrt(3/4) to +Sqrt(3/4)
	//noise += 1;
    //noise /= 2;
	//noise = saturate(noise); // Never below 0, never above 1.
	
    return noise;
}

float3 UvToSphere(float2 coords)
{
    float lon = coords.x * 2 * SPHERE_PI;
    float lat = (coords.y - 0.5) * SPHERE_PI;

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
            xzAtan2 = (SPHERE_PI / 2);
        else
            xzAtan2 = -(SPHERE_PI / 2);
    }
    else
        xzAtan2 = atan2(cartesian.z, cartesian.x);

    polar.x = xzAtan2;

    polar.y = asin(cartesian.y);

    polar.x /= (2 * SPHERE_PI);
    polar.y /= SPHERE_PI;

    if (polar.x < 0) polar.x += 1;
    polar.y += 0.5;
    return polar;
}

float fbm(float3 coords, float3 offset, int seed, float multiplier, int octaves, float lacunarity, float persistence, int ridged)
{
    coords /= multiplier;
    coords += offset;

    float maxValue = 0;
    float amplitude = 1;
    float val = 0;
    for (int n = 0; n < octaves; n++)
    {
        float noiseValue = 0;
        noiseValue = simplexNoise(coords, seed); // 3D Simplex Noise ranges from -Sqrt(3/4) to +Sqrt(3/4)

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

float sphereNoise(float2 coords, float3 offset, int seed, float multiplier, int octaves, float lacunarity, float persistence, int ridged, float domainWarping, float minHeight, float maxHeight)
{
    float3 sphereCoords = UvToSphere(coords) + 1;
	float noiseValue = 0;

	if (domainWarping > 0)
	{
		float3 sphereCoordsOffset1 = float3(offset.y, offset.z, offset.x);
		float3 sphereCoordsOffset2 = float3(offset.z, offset.x, offset.y);
		float3 sphereCoordsOffset3 = float3(offset.x, offset.y, offset.z);

		float qCoordsX = fbm(sphereCoords + sphereCoordsOffset1, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
		float qCoordsY = fbm(sphereCoords + sphereCoordsOffset2, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
		float qCoordsZ = fbm(sphereCoords + sphereCoordsOffset3, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);

		qCoordsX = (qCoordsX - minHeight) / (maxHeight - minHeight);
		qCoordsY = (qCoordsY - minHeight) / (maxHeight - minHeight);
		qCoordsZ = (qCoordsZ - minHeight) / (maxHeight - minHeight);

		float3 qCoords = float3(qCoordsX, qCoordsY, qCoordsZ);

		//if (domainWarping == 1)
			noiseValue = fbm(sphereCoords + domainWarping * qCoords, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
		//else
		//{
		//	float3 sphereCoordsOffset4 = float3(offset.z, offset.x, offset.y);
		//	float3 sphereCoordsOffset5 = float3(offset.x * 0.9, offset.y * 0.9, offset.z * 0.9);
		//	float3 sphereCoordsOffset6 = float3(offset.y * 0.75, offset.x * 0.75, offset.z * 0.75);

		//	float3 rCoords = float3(
		//		fbm(qCoords.x * qCoords + sphereCoordsOffset4, offset, seed, multiplier, octaves, lacunarity, persistence, ridged),
		//		fbm(qCoords.y * qCoords + sphereCoordsOffset5, offset, seed, multiplier, octaves, lacunarity, persistence, ridged),
		//		fbm(qCoords.z * qCoords + sphereCoordsOffset6, offset, seed, multiplier, octaves, lacunarity, persistence, ridged)
		//		);

		//	noiseValue = fbm((sphereCoords + rCoords) / 2, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
		//}
	}
	else
	{
		noiseValue = fbm(sphereCoords, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
	}

	noiseValue = (noiseValue - minHeight) / (maxHeight - minHeight);

    return noiseValue;
}

float sphereHeight(float2 coords, float3 offset, int seed, float multiplier, int octaves, float lacunarity, float persistence, int ridged, float heightExponent, float layerStrength, float domainWarping,
                                  float3 offset2, int seed2, float multiplier2, int octaves2, float lacunarity2, float persistence2, int ridged2, float heightExponent2, float layerStrength2, float domainWarping2,
                                  float minHeight, float maxHeight )
{
    float height = 0;
    if (layerStrength > 0 || layerStrength2 == 0)
    {
        float height1 = sphereNoise(coords, offset, seed, multiplier, octaves, lacunarity, persistence, ridged, domainWarping, minHeight, maxHeight);
        height1 = pow(abs(height1), heightExponent);
        height += height1 * layerStrength;
    }
    if (layerStrength2 > 0)
    {
        float height2 = sphereNoise(coords, offset2, seed2, multiplier2, octaves2, lacunarity2, persistence2, ridged2, domainWarping2, minHeight, maxHeight);
        height2 = pow(abs(height2), heightExponent2);
        height += height2 * layerStrength2;
    }
    float sumLayerStrengths = layerStrength + layerStrength2;
    if (sumLayerStrengths <= 0) sumLayerStrengths = 1;
    height /= (layerStrength + layerStrength2);
    return height;
}
#endif
