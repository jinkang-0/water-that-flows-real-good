Shader "Custom/Particle2D"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
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
            float particleRadius;
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
                float radius : TEXCOORD2;
            };

            v2f vert(const appdata_full v, const uint instanceID : SV_InstanceID)
            {
                // find object coords
                const float3 boundCorner = float3(-boundsSize / 2, 0);
                const float3 centerWorld = boundCorner + float3(positions[instanceID], 0) * float3(cellSize, 0);
                const float3 worldVertPos = centerWorld + mul(unity_ObjectToWorld, v.vertex * scale);
                const float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));

                // find object size
                const float3 objectSize = mul(unity_WorldToObject, float4(particleRadius * cellSize.x, 1, 1, 1));
                
                v2f o;
                o.uv = v.texcoord;
                o.pos = UnityObjectToClipPos(objectVertPos);
                o.color = waterColor;
                o.radius = objectSize.x;
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // circle
                const float2 centerOffset = i.uv.xy - 0.5;
                const float sqrDst = dot(centerOffset, centerOffset);
                const float normalizedDist = sqrDst / (i.radius * i.radius);
                const float edge = fwidth(sqrt(sqrDst));
                const float alpha = 1 - smoothstep(1 - edge, 1 + edge, normalizedDist);
                
                float4 color = i.color;
                color.a *= alpha;
                
                return color;
            }
            
            ENDCG
        }
    }
}
