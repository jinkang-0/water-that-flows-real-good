Shader "Custom/Particle2D"
{
    Properties
    {
        _BGTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"
            
            StructuredBuffer<float2> particleVelocities;
            StructuredBuffer<float2> particlePositions;
            StructuredBuffer<int> lookupStartIndices;
            StructuredBuffer<int> particleLookup;

            float scale;
            float particleRadius;
            float partitionSpacing;
            int partitionNumX;
            int partitionNumY;
            float2 boundsSize;
            float4 waterColor;
            int numCols;
            int numRows;
            int numParticles;

            float softness;
            float threshold;
            
            float4 color;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float radius: TEXCOORD1;
                float2 bg_uv : TEXCOORD2;
            };

            sampler2D _BGTex;
            float4 _BGTex_ST;

            v2f vert(const appdata_full v, const uint instanceID : SV_InstanceID)
            {
                // find object coords
                const float3 boundCorner = float3(-boundsSize / 2, 0);
                const float3 centerWorld = boundCorner + float3(particlePositions[instanceID], 0);
                const float3 worldVertPos = centerWorld + mul(unity_ObjectToWorld, v.vertex * scale);
                const float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));

                const float3 objectSize = mul(unity_WorldToObject, float4(particleRadius, particleRadius, 1, 1));
                
                v2f o;
                o.uv = v.texcoord;
                o.pos = UnityObjectToClipPos(objectVertPos);
                o.radius = length(float2(objectSize.x, objectSize.y));
                o.bg_uv = particlePositions[instanceID] / boundsSize;

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                const float2 centerOffset = i.uv.xy - 0.5;
                const float d2 = dot(centerOffset, centerOffset);
                const float normDist = d2 / (i.radius * i.radius);
                const float alpha = 1 - smoothstep(threshold - softness, threshold + softness, normDist);

                float4 color = waterColor;

                float3 bg_color = tex2D(_BGTex, i.bg_uv);
                //float4 bg_color = float4(i.bg_uv.x, i.bg_uv.y, 0.0, 1.0);//tex2D(_BGTex, i.bg_uv);
                color = 0.3 * color + 1.0 * float4(bg_color.x, bg_color.y, bg_color.z, 1.0);

                color.a *= alpha;
                
                return color;
            }
            
            ENDCG
        }
    }
}
