#ifndef INSTANCED_FETCH_INCLUDED
#define INSTANCED_FETCH_INCLUDED

StructuredBuffer<float3> _Vertices;
StructuredBuffer<float3> _Normals;
StructuredBuffer<float3> _Colors;
StructuredBuffer<int> _Indices;

void MyFunctionA_float(float i, out float3 position, out float3 normal, out float3 color, out float index)
{
    int temp = _Indices[int(i)];
    position = _Vertices[temp];
    normal = _Normals[temp];
    color = _Colors[temp];
    index = float(temp);
}

#endif