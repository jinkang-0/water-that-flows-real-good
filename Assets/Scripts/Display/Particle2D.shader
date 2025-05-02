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

            float blendThreshold;
            float blendSoftness;
            float orthographicSize;
            
            float4 color;
            SamplerState linear_clamp_sampler;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
                float radius : TEXCOORD2;
                int id : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
            };

            v2f vert(const appdata_full v, const uint instanceID : SV_InstanceID)
            {
                // find object coords
                const float3 boundCorner = float3(-boundsSize / 2, 0);
                const float3 centerWorld = boundCorner + float3(particlePositions[instanceID], 0);
                const float3 worldVertPos = centerWorld + mul(unity_ObjectToWorld, v.vertex * scale);
                const float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));

                // find object size
                const float3 objectSize = mul(unity_WorldToObject, float4(particleRadius, 1, 1, 1));
                
                v2f o;
                o.uv = v.texcoord;
                o.pos = UnityObjectToClipPos(objectVertPos);
                o.color = waterColor;
                o.radius = objectSize.x;
                o.id = instanceID;
                o.screenPos = ComputeScreenPos(o.pos);

                // hide out of bounds particles
                // const float2 pos = particlePositions[instanceID];
                // const float minX = 1 - particleRadius;
                // const float maxX = numCols - 1 + particleRadius;
                // const float minY = 1 - particleRadius;
                // const float maxY = numRows - 1 + particleRadius;
                // if (pos.x < minX || pos.x > maxX || pos.y < minY || pos.y > maxY)
                // {
                //     o.color = float4(0, 0, 0, 0);
                // }
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // get fragment position in screen space
                const float2 screenUV = i.screenPos.xy / i.screenPos.w;
                const float2 fragPos = screenUV * _ScreenParams.xy;
                const float rInScreenSpace = particleRadius * _ScreenParams.y / orthographicSize;
                const float r2SS = rInScreenSpace * rInScreenSpace;
                
                // find total density
                const float2 p = particlePositions[i.id];
                const int px = clamp(floor(p.x / partitionSpacing), 0, partitionNumX - 1);
                const int py = clamp(floor(p.y / partitionSpacing), 0, partitionNumY - 1);
                const int x0 = clamp(px - 1, 0, partitionNumX - 1);
                const int x1 = clamp(px + 1, 0, partitionNumX - 1);
                const int y0 = clamp(py - 1, 0, partitionNumY - 1);
                const int y1 = clamp(py + 1, 0, partitionNumY - 1);
                float totalDensity = 0;
                
                for (int x = x0; x <= x1; x++)
                {
                    for (int y = y0; y <= y1; y++)
                    {
                        const int cellIdx = y * partitionNumY + x;
                        const int startIndex = lookupStartIndices[cellIdx];
                        const int endIndex = lookupStartIndices[cellIdx + 1];

                        for (int pid = startIndex; pid < endIndex; pid++)
                        {
                            const int particleId = particleLookup[pid];

                            // get particle position in screenspace
                            const float p2 = particlePositions[particleId] * _ScreenParams.xy;
                            
                            // compute particle density
                            const float2 dPos = fragPos - p2;
                            const float dSqrDist = dot(dPos, dPos);
                            totalDensity += r2SS / (dSqrDist + 0.001);
                        }
                    }
                }

                const float alpha = smoothstep(blendThreshold - blendSoftness, blendThreshold + blendSoftness, totalDensity);
                
                float4 color = i.color;
                color.a = alpha;
                
                return color;
            }
            
            ENDCG
        }
    }
}
