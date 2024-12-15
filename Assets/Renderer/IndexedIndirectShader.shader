Shader "Custom/NewSurfaceShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM

        #ifdef SHADER_API_D3D11
            StructuredBuffer<float4> vertices;
            StructuredBuffer<float4> normals;
        #endif

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 5.0
        #pragma vertex vert
        #pragma surface surf Lambert
            




        struct appdata {
           float4 vertex : SV_POSITION;
           float3 normal : NORMAL;
           float4 color : COLOR;
           uint id : SV_VertexID;
        };

        struct Input {
            float3 normal : NORMAL;
            float vertexId;
        };

        void vert (inout appdata v, out Input o) { //, uint inst : SV_InstanceID
           #ifdef SHADER_API_D3D11
           float4 vertex_position =  float4(vertices[v.id].xyz,1.0);
           float4 vertex_normal = float4(normals[v.id].xyz, 1.0); 
           v.vertex = vertex_position;
           v.normal = vertex_normal;
           o.normal = vertex_normal;
           o.vertexId = float(v.id);
           #endif

        }


        void surf (Input IN, inout SurfaceOutput o) {
           o.Emission = normalize(IN.normal) * 2 - 1;
           //o.Albedo *= IN.vertexId / 20000.0;
        }

        ENDCG
    }
}
