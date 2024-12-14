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
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        //#include "UnityCG.cginc"
        //#define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
        //#include "UnityIndirect.cginc"
        struct Input {
            float nothing;    
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf (Input input, inout SurfaceOutputStandard o)
        {
            o.Albedo = _Color;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }

        void vert(inout float3 vertex: POSITION, inout uint svVertexID: SV_VertexID)
        {
            InitIndirectDrawArgs(0);
            uint cmdID = GetCommandID(0);
            float3 pos = _Positions[GetIndirectVertexID(svVertexID)];
            float4 wpos = mul(_ObjectToWorld, float4(pos, 1.0f));
            vertex = mul(UNITY_MATRIX_VP, wpos);
            //v.color = float4(cmdID & 1 ? 0.0f : 1.0f, cmdID & 1 ? 1.0f : 0.0f, instanceID / float(GetIndirectInstanceCount()), 0.0f);
            return o;
        }

        ENDCG
    }
    FallBack "Diffuse"
}
