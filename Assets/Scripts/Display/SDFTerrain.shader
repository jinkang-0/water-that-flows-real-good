Shader "Custom/SDFTerrain"
{
    Properties
    {
        _DynamicDist ("Texture", 2D) = "white" {}
        _StaticDist ("Texture", 2D) = "white" {}
        _DynamicColor ("Texture", 2D) = "white" {}
        _StaticColor ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        //Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float scale;
            float2 boundsSize;

            sampler2D _DynamicDist;
            float4 _DynamicDist_ST;
            sampler2D _StaticDist;
            float4 _StaticDist_ST;
            sampler2D _DynamicColor;
            float4 _DynamicColor_ST;
            sampler2D _StaticColor;
            float4 _StaticColor_ST;

            v2f vert (appdata v)
            {
                v2f o;

                float4 pos = float4(boundsSize.x * v.vertex.x, boundsSize.y * v.vertex.y, -1.0, 1.0);

                o.vertex = UnityObjectToClipPos(pos);
                o.uv = TRANSFORM_TEX(v.uv, _DynamicDist);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (tex2D(_StaticDist, i.uv).r < 0.0) return tex2D(_StaticColor, i.uv);
                if (tex2D(_DynamicDist, i.uv).r < 0.0) return tex2D(_DynamicColor, i.uv);

                discard;
                return fixed4(0.0, 0.0, 0.0, 0.0);
            }
            ENDCG
        }
    }
}
