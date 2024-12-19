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
            StructuredBuffer<float3> _Vertices;
            StructuredBuffer<float3> _Normals;
            StructuredBuffer<float3> _Colors;
        #endif

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 5.0
        #pragma surface surf Standard vertex:vert fullforwardshadows addshadow
            




        struct appdata {
           float4 vertex : SV_POSITION;
           float3 normal : NORMAL;
           float4 tangent : TANGENT;
           float4 color : COLOR;
           uint id : SV_VertexID;
        };

        struct Input {
            float3 normal;
            float3 color;
            float3 cameraRelativeWorldPos;
            INTERNAL_DATA
        };

        void vert (inout appdata v, out Input o) { //, uint inst : SV_InstanceID
           UNITY_INITIALIZE_OUTPUT(Input,o);

           #ifdef SHADER_API_D3D11
           v.vertex = float4(_Vertices[v.id],1.0);
           v.normal = float4(_Normals[v.id], 1.0);
           v.color = float4(_Colors[v.id], 0.0);
           v.tangent = float4(0, 0, 0, 0);
           o.normal = _Normals[v.id];
           o.color = _Colors[v.id];
           #endif

           o.cameraRelativeWorldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)) - _WorldSpaceCameraPos.xyz;
        }


        void surf (Input IN, inout SurfaceOutputStandard o) {
           //o.Emission = normalize(IN.normal) * 2 - 1;
           //o.Albedo *= IN.vertexId / 20000.0;
           //o.Normal = normalize(IN.normal); 

           
           // flat world normal from position derivatives
           half3 flatWorldNormal = normalize(cross(ddy(IN.cameraRelativeWorldPos.xyz), ddx(IN.cameraRelativeWorldPos.xyz)));

           // construct world to tangent matrix
           half3 worldT =  WorldNormalVector(IN, half3(1,0,0));
           half3 worldB =  WorldNormalVector(IN, half3(0,1,0));
           half3 worldN =  WorldNormalVector(IN, half3(0,0,1));
           half3x3 tbn = half3x3(worldT, worldB, worldN);
           
           //o.Albedo = flatWorldNormal;
           o.Albedo = IN.color; 
           //o.Smoothness = 1.0;
           //o.Metallic = 1.0;
           //o.Normal = float3(0,0,0);
        }

        ENDCG

    }
}
