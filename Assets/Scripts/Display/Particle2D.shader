Shader "Custom/Grid2D"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"

            StructuredBuffer<float2> particleVelocities;
            StructuredBuffer<float2> positions;

            float scale;
            float2 cellSize;
            float2 boundsSize;
            float4 waterColor;
            
            float4 color;
            SamplerState linear_clamp_sampler;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
            };

            v2f vert(const appdata_full v, const uint instanceID : SV_InstanceID)
            {
                const float3 boundCorner = float3(-boundsSize / 2, 0);
                const float3 centerWorld = boundCorner + float3(positions[instanceID], 0) * float3(cellSize, 0);
                const float3 worldVertPos = centerWorld + mul(unity_ObjectToWorld, v.vertex * scale);
                const float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));
                
                v2f o;
                o.uv = v.texcoord;
                o.pos = UnityObjectToClipPos(objectVertPos);
                o.color = waterColor;
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // circle
                const float2 centerOffset = (i.uv.xy - 0.5) * 4;
                const float sqrDst = dot(centerOffset, centerOffset);
                const float delta = fwidth(sqrt(sqrDst));
                const float alpha = 1 - smoothstep(1 - delta, 1 + delta, sqrDst);
                
                float4 color = i.color;
                color.a *= alpha;
                
                return color;
            }
            
            ENDCG
        }
    }
}
