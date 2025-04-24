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
            StructuredBuffer<float2> cellVelocities;

            float scale;
            int numRows;
            int numCols;
            float2 cellSize;
            float2 boundsSize;
            float4 terrainColor;
            float4 stoneColor;
            float4 waterColor;
            
            float4 color;
            SamplerState linear_clamp_sampler;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
                float2 size : TEXCOORD2;
            };

            v2f vert(const appdata_full v, const uint instanceID : SV_InstanceID)
            {
                // find object coords
                const int row = instanceID / numCols;
                const int col = instanceID % numCols;
                const float3 boundCorner = float3(-boundsSize / 2, 0);
                const float3 centerWorld = boundCorner + float3(col + 0.5, row + 0.5, 0) * float3(cellSize, 0);
                const float3 worldVertPos = centerWorld + mul(unity_ObjectToWorld, v.vertex * scale);
                const float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));

                // determine cell size
                const float3 objectSize = mul(unity_WorldToObject, float4(cellSize.xy, 1, 1));
                
                v2f o;
                o.uv = v.texcoord;
                o.pos = UnityObjectToClipPos(objectVertPos);
                o.color = float4(0,0,0,0);
                o.size = objectSize.xy;

                // determine cell color
                if (cellTypes[instanceID] == 1)
                    o.color = terrainColor;
                else if (cellTypes[instanceID] == 2)
                    o.color = stoneColor;
                else
                {
                    const float2 vel = cellVelocities[instanceID];
                    const float vy = min(max(abs(vel.y), 0), 255);
                    const float vx = min(max(abs(vel.x), 0), 255);
                    o.color = float4(vy, 0, vx, 1);
                }
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // square
                const float2 edgeDist = abs((i.uv.xy - 0.5) * 2);
                const float insideX = step(edgeDist.x, i.size.x);
                const float insideY = step(edgeDist.y, i.size.y);
                const float mask = insideX * insideY;
                
                float4 color = i.color;
                color.a *= mask;
                
                return color;
            }
            
            ENDCG
        }
    }
}
