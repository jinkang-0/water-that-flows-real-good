Shader "Custom/DrainCell"
{
    Properties
    {
        _Color ("Drain Color", Color) = (0,1,0,1) // Default to green
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off // Optional: Can help if quads sometimes face away

        Pass {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5 // Ensure compute buffer access

            #include "UnityCG.cginc"

            // Buffers and uniforms needed
            StructuredBuffer<int> cellTypes; // We need this to know WHICH cell we are
            float scale;                     // From Display2D
            int numCols;                     // From Display2D
            float cellSize;                  // From Display2D
            float2 boundsSize;               // From Display2D
            float4 _Color;                   // The drain color property

            // Constants
            static const int DRAIN_CELL = 4; // Must match the value in C#

            struct appdata_full // Use appdata_full to get vertex info
            {
                float4 vertex : POSITION;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
                float4 texcoord2 : TEXCOORD2;
                float4 texcoord3 : TEXCOORD3;
                fixed4 color : COLOR;
           };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0; // Use original UV for square shape
            };

            v2f vert(const appdata_full v, const uint instanceID : SV_InstanceID)
            {
                v2f o;
                o.uv = v.texcoord.xy; // Pass UV along
                o.pos = float4(0,0,0,0); // Initialize position

                // Only proceed if this instance IS a drain cell
                if (cellTypes[instanceID] != DRAIN_CELL)
                {
                    // Move vertex off-screen if it's not a drain cell
                    // This effectively culls non-drain cells for this draw call
                    o.pos = float4(10000.0, 10000.0, 10000.0, 1.0);
                    return o;
                }

                // --- Calculate position for drain cells (same logic as Grid2D shader) ---
                const int row = instanceID / numCols;
                const int col = instanceID % numCols;
                // Offset by -boundsSize/2 to center the grid at (0,0)
                const float3 boundCorner = float3(-boundsSize.x * 0.5, -boundsSize.y * 0.5, 0);
                // Calculate the center of the cell in world space
                const float3 centerWorld = boundCorner + float3((col + 0.5) * cellSize, (row + 0.5) * cellSize, 0);
                // Scale the incoming quad mesh vertex and add to the cell center
                // Assuming the mesh is a simple quad centered at origin
                const float3 scaledVertex = float3(v.vertex.x * cellSize * scale, v.vertex.y * cellSize * scale, 0);
                const float3 worldVertPos = centerWorld + scaledVertex; // Use cellSize * scale here maybe? Or just cellSize? Let's try cellSize * scale
                //const float3 worldVertPos = centerWorld + v.vertex.xyz * cellSize * scale; // Correct way to scale the mesh vertex

                o.pos = UnityWorldToClipPos(worldVertPos);
                //-------------------------------------------------------------------------

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                

               
                 float4 finalColor = _Color;
                // finalColor.a *= mask; // Apply square mask if needed (often the mesh itself is the square)

                // Discard fragments for pixels outside the quad defined by UVs
                 clip(1.0 - abs(i.uv.x - 0.5) * 2.0);
                 clip(1.0 - abs(i.uv.y - 0.5) * 2.0);


                return finalColor;
            }

            ENDCG
        }
    }
}