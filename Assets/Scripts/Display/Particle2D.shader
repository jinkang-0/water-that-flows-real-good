Shader "Custom/Particle2D"
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

            StructuredBuffer<float2> positions;
            StructuredBuffer<float2> velocities;

            float scale;
            float4 color;
            Texture2D<float4> colorMap;
            SamplerState linear_clamp_sampler;
            float velocityMax;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 color : TEXCOORD1;
            };

            v2f vert(appdata_full v, uint instanceID : SV_InstanceID)
            {
                const float speed = length(velocities[instanceID]);
                const float speedT = saturate(speed / velocityMax);
                const float colT = speedT;

                const float3 centerWorld = float3(positions[instanceID], 0);
                const float3 worldVertPos = centerWorld + mul(unity_ObjectToWorld, v.vertex * scale);
                const float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));

                v2f o;
                o.uv = v.texcoord;
                o.pos = UnityObjectToClipPos(objectVertPos);
                o.color = colorMap.SampleLevel(linear_clamp_sampler, float2(colT, 0.5), 0);

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                const float2 centerOffset = (i.uv.xy - 0.5) * 2;
                const float sqrDst = dot(centerOffset, centerOffset);
                const float delta = fwidth(sqrt(sqrDst));
                const float alpha = 1 - smoothstep(1 - delta, 1 + delta, sqrDst);

                const float3 color = i.color;
                
                return float4(color, alpha);
            }
            
            ENDCG
        }
    }
}
