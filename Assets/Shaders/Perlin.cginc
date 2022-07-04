#ifndef NOISE_SIMPLEX_FUNC
#define NOISE_SIMPLEX_FUNC

#define SPHERE_PI 3.14159265359

float hash( float n, float seed )
{
    return frac(((sin(n) + 0.9 * cos(2 * n) + 0.8 * sin(5 * n) + 0.7 * cos(13 * n)) / 3.4) * seed);
}

float perlinNoise( float3 x, float seed )
{
    // The noise function returns a value in the range -1.0f -> 1.0f

    float3 p = floor(x);
    float3 f = frac(x);

    f       = f * f * (3.0 - 2.0 * f);
    float n = p.x + 
	          p.y * 71.0 + 
			  p.z * 173.0;

    float noise = lerp(lerp(lerp( hash(n + 0.0, seed),  hash(n + 1.0, seed),  f.x),
                            lerp( hash(n + 71.0, seed), hash(n + 72.0, seed), f.x),
							f.y),
                       lerp(lerp( hash(n + 173.0, seed), hash(n + 174.0, seed), f.x),
                            lerp( hash(n + 244.0, seed), hash(n + 245.0, seed), f.x),
							f.y),
					   f.z);

	return noise; // Returns a value from -1 to 1.
}

float3 UvToSphere(float2 coords)
{
    float lon = coords.x * 2 * SPHERE_PI;
    float lat = (coords.y - 0.5) * SPHERE_PI;

    float a = cos(lat);
    float y = sin(lat);
    float z = a * sin(lon);
    float x = a * cos(lon);

    float3 spherePoint = float3(x, y, z);
    return spherePoint;
}

float fbm(float3 coords, float3 offset, float seed, float multiplier, int octaves, float lacunarity, float persistence, int ridged)
{
    coords /= multiplier;
    coords += offset;

    float maxValue = 0;
    float amplitude = 1;
    float val = 0;
    for (int n = 0; n < octaves; n++)
    {
        float noiseValue = 0;
        noiseValue = perlinNoise(coords, seed) * 0.5 + 0.5; // Normalizes the value to the 0 - 1 range.

        if (ridged > 0)
            noiseValue = 1 - (abs(noiseValue - 0.5) * 2);

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

float sphereNoise(float2 coords, float3 offset, float seed, float multiplier, int octaves, float lacunarity, float persistence, int ridged, float domainWarping, float minHeight, float maxHeight)
{
    float3 sphereCoords = UvToSphere(coords);
	float noiseValue = 0;

	if (domainWarping > 0)
	{
		float3 sphereCoordsOffset1 = float3(offset.y, offset.z, offset.x);
		float3 sphereCoordsOffset2 = float3(offset.z, offset.x, offset.y);
		float3 sphereCoordsOffset3 = float3(offset.x, offset.y, offset.z);

		float qCoordsX = fbm(sphereCoords + sphereCoordsOffset1, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
		float qCoordsY = fbm(sphereCoords + sphereCoordsOffset2, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
		float qCoordsZ = fbm(sphereCoords + sphereCoordsOffset3, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);

		//qCoordsX = (qCoordsX - minHeight) / (maxHeight - minHeight);
		//qCoordsY = (qCoordsY - minHeight) / (maxHeight - minHeight);
		//qCoordsZ = (qCoordsZ - minHeight) / (maxHeight - minHeight);

		float3 qCoords = float3(qCoordsX, qCoordsY, qCoordsZ);

		//if (domainWarping == 1)
			noiseValue = fbm(sphereCoords + domainWarping * qCoords, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
		//else
		//{
		//	float3 sphereCoordsOffset4 = float3(offset.z, offset.x, offset.y);
		//	float3 sphereCoordsOffset5 = float3(offset.x * 0.9, offset.y * 0.9, offset.z * 0.9);
		//	float3 sphereCoordsOffset6 = float3(offset.y * 0.75, offset.x * 0.75, offset.z * 0.75);

		//	float3 rCoords = float3(
		//		fbm(domainWarping * qCoords.x + sphereCoordsOffset4, offset, seed, multiplier, octaves, lacunarity, persistence, ridged),
		//		fbm(domainWarping * qCoords.y + sphereCoordsOffset5, offset, seed, multiplier, octaves, lacunarity, persistence, ridged),
		//		fbm(domainWarping * qCoords.z + sphereCoordsOffset6, offset, seed, multiplier, octaves, lacunarity, persistence, ridged)
		//		);

		//	noiseValue = fbm(sphereCoords + domainWarping * rCoords, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
		//}
	}
	else
	{
		noiseValue = fbm(sphereCoords, offset, seed, multiplier, octaves, lacunarity, persistence, ridged);
	}

	//if (ridged > 0)
	//{
	//	noiseValue = 1 - (abs(noiseValue - 0.5) * 2);
	//}

	if (maxHeight != minHeight)
		noiseValue = ((noiseValue - minHeight) / (maxHeight - minHeight));

    return noiseValue;
}

float sphereHeight(float2 coords, float3 offset, float seed, float multiplier, int octaves, float lacunarity, float persistence, int ridged, float heightExponent, float layerStrength, float domainWarping,
                                  float3 offset2, float seed2, float multiplier2, int octaves2, float lacunarity2, float persistence2, int ridged2, float heightExponent2, float layerStrength2, float domainWarping2, float minHeight, float maxHeight )
{
    float height = 0;
    if (layerStrength > 0)
    {
        float height1 = sphereNoise(coords, offset, seed, multiplier, octaves, lacunarity, persistence, ridged, domainWarping, minHeight, maxHeight);
		
		if (heightExponent != 1)
			height1 = pow(abs(height1 < 0 ? 0 : height1), heightExponent);

		if (layerStrength2 > 0)
			height += height1 * layerStrength;
		else
			height = height1;
    }

    if (layerStrength2 > 0)
    {
        float height2 = sphereNoise(coords, offset2, seed2, multiplier2, octaves2, lacunarity2, persistence2, ridged2, domainWarping2, minHeight, maxHeight);

		if (heightExponent2 != 1)
			height2 = pow(abs(height2 < 0 ? 0 : height2), heightExponent2);

		if (layerStrength > 0)
			height += height2 * layerStrength2;
		else
			height = height2;
    }
	
	if (layerStrength > 0 && layerStrength2 > 0)
		height /= (layerStrength + layerStrength2);

    return height;
}
#endif
