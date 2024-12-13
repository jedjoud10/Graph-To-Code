// Cubic weight function based on Catmull-Rom spline
float CubicWeight(float x)
{
	x = abs(x);
	if (x < 1.0)
	{
		return 1.5 * x * x * x - 2.5 * x * x + 1.0;
	}
	else if (x < 2.0)
	{
		return -0.5 * x * x * x + 2.5 * x * x - 4.0 * x + 2.0;
	}
	return 0.0;
}

// Bicubic sampling function
float4 SampleBicubic(Texture3D tex, SamplerState test, float3 uv, float lod, float3 texSize)
{
    // Map uv to texture space and calculate base and fractional coordinates
	float3 texel = uv * texSize - 0.5;
	float3 base = floor(texel);
	float3 frac = texel - base;

	float4 result = float4(0.0, 0.0, 0.0, 0.0);

    // Loop through a 4x4 grid of texels
	for (int j = -1; j <= 2; j++)
	{
		for (int i = -1; i <= 2; i++)
		{
			for (int u = -1; u <= 2; u++)
			{
				float3 offset = float3(i, j, u);
				float3 sampleUV = (base + offset + 1.0) / texSize;
				float4 texelColor = tex.SampleLevel(test, sampleUV, lod);

				// Compute cubic weights for x and y
				float weightX = CubicWeight(frac.x - i);
				float weightY = CubicWeight(frac.y - j);
				float weightZ = CubicWeight(frac.z - u);

				// Accumulate weighted color
				result += texelColor * weightX * weightY * weightZ;
			}
		}
	}

	return result;
}

float4 SampleBounded(Texture3D tex, SamplerState test, float3 uv, float lod, float3 texSize)
{
	if (any(uv < 0.0) || any(uv >= 1.0))
	{
		const float aaa = -10000;
		return float4(aaa, aaa, aaa, aaa);
	}
	
	//return tex[uint3(uv * texSize + 1.0/texSize)];
	return tex.SampleLevel(test, uv + (0.5 / texSize), lod);
}