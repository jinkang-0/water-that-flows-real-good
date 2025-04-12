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
            StructuredBuffer<float> vrVelocities;
            StructuredBuffer<float> hrVelocities;

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
            };

            v2f vert(appdata_full v, uint instanceID : SV_InstanceID)
            {
                const int row = instanceID / numCols;
                const int col = instanceID % numCols;

                const float3 boundCorner = float3(-boundsSize / 2, 0);
                const float3 centerWorld = boundCorner + float3(col + 0.5, row + 0.5, 0) * float3(cellSize, 0);
                const float3 worldVertPos = centerWorld + mul(unity_ObjectToWorld, v.vertex * scale);
                const float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));
                
                v2f o;
                o.uv = v.texcoord;
                o.pos = UnityObjectToClipPos(objectVertPos);
                o.color = float3(0,0,0);

                if (cellTypes[instanceID] == 1)
                    o.color = terrainColor;

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // this turns it into a circle
                // const float2 centerOffset = (i.uv.xy - 0.5) * 2;
                // const float sqrDst = dot(centerOffset, centerOffset);
                // const float delta = fwidth(sqrt(sqrDst));
                // const float alpha = 1 - smoothstep(1 - delta, 1 + delta, sqrDst);

                const float3 color = i.color;
                
                return float4(color, 1);
            }
            
            ENDCG
        }
    }
}
