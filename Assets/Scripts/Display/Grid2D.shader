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

            StructuredBuffer<int> cellTypes;
            StructuredBuffer<float2> velocities;
            StructuredBuffer<float> pressures;
            StructuredBuffer<float3> colors;

            float scale;
            int numRows;
            int numCols;
            float2 cellSize;
            float2 boundsSize;
            float4 terrainColor;
            
            float4 color;
            SamplerState linear_clamp_sampler;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 color : TEXCOORD1;
                float2 size : TEXCOORD2;
            };

            v2f vert(const appdata_full v, const uint instanceID : SV_InstanceID)
            {
                const int row = instanceID / numCols;
                const int col = instanceID % numCols;

                const float3 boundCorner = float3(-boundsSize / 2, 0);
                const float3 centerWorld = boundCorner + float3(col + 0.5, row + 0.5, 0) * float3(cellSize, 0);
                const float3 worldVertPos = centerWorld + mul(unity_ObjectToWorld, v.vertex * scale);
                const float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));
                const float3 objectSize = mul(unity_WorldToObject, float4(cellSize.xy, 1, 1));
                
                v2f o;
                o.uv = v.texcoord;
                o.pos = UnityObjectToClipPos(objectVertPos);
                o.color = float4(0,0,0,0);
                o.size = objectSize.xy;

                // display the velocities and pressure by default

                o.color = float4(velocities[instanceID], pressures[instanceID], 1.0);
                o.color = float4(colors[instanceID], 1.0);
                //o.color = float4(pressures[instanceID], pressures[instanceID], pressures[instanceID], 1.0);

                //o.color = float4(vrVelocities[instanceID], hrVelocities[instanceID], Pressures[instanceID], 1);
                //o.color = float4(Pressures[instanceID], Pressures[instanceID], Pressures[instanceID], 1);
                // override with terrain color
                if (cellTypes[instanceID] == 1)
                    o.color = terrainColor;

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // square
                const float2 edgeDist = abs((i.uv.xy - 0.5) * 2);
                const float insideX = step(edgeDist.x, i.size.x);
                const float insideY = step(edgeDist.y, i.size.y);
                const float alpha = insideX * insideY;
                
                const float3 color = i.color;
                
                return float4(color, alpha);
            }
            
            ENDCG
        }
    }
}
