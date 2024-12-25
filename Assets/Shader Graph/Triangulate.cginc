void MyFunctionB_float(float i, out float2 uv)
{
	float2 uvs[3] = {
		float2(0, 0),
		float2(1, 0),
		float2(0, 1),
    };

    float2 quadPositions[6] = {
        float2(0.0, 0.0), // First triangle
        float2(1.0, 0.0),
        float2(0.0, 1.0),
        float2(1.0, 0.0), // Second triangle
        float2(1.0, 1.0),
        float2(0.0, 1.0)
    };

	float2 quadPositions2[6] = {
        float2(0.0, 0.0), // First triangle
        float2(1.0, 0.0),
        float2(1.0, 1.0),
        float2(0.0, 0.0), // Second triangle
        float2(1.0, 1.0),
        float2(0.0, 1.0)
    };

	/*
	// WORKS FOR HEIGHTMAP
	if (i < 3) {
		uv = quadPositions[int(i)];
	} else {
		uv = quadPositions[int(fmod(6-i+1, 6))];
	}
	*/

	if (i < 3) {
		uv = quadPositions2[i];
	} else {
		uv = quadPositions2[i];
	}
}