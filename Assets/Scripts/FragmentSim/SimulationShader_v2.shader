Shader "Custom/SimulationV2"
{
    Properties
    {
        _Width("SimulationWidth", Integer) = 100
        _Height("SimulationHeight", Integer) = 100
        _Tex("InputTex", 2D) = "white" {}
    }

    SubShader
    {
       Lighting Off
       ZTest Always Cull Off ZWrite Off
       Blend One Zero

       Pass
       {
           CGPROGRAM
           #include "UnityCustomRenderTexture.cginc"
           #pragma vertex CustomRenderTextureVertexShader
           #pragma fragment frag
            #pragma target 3.0

           sampler2D _Tex;
           int _Width;
           int _Height;

           float4 frag(v2f_customrendertexture IN) : COLOR
           {
                float2 ps = float2(1.0 / float(_Width), 1.0 / float(_Height));

                float4 p11 = tex2D(_Tex, IN.localTexcoord.xy + float2( 0.0,  0.0));

                float4 p12 = tex2D(_Tex, IN.localTexcoord.xy + float2( 0.0,  ps.y));
                float4 p10 = tex2D(_Tex, IN.localTexcoord.xy + float2( 0.0, -ps.y));

                float4 p21 = tex2D(_Tex, IN.localTexcoord.xy + float2( ps.x,  0.0));
                float4 p01 = tex2D(_Tex, IN.localTexcoord.xy + float2(-ps.x,  0.0));

                float4 p22 = tex2D(_Tex, IN.localTexcoord.xy + float2( ps.x,  ps.y));
                float4 p20 = tex2D(_Tex, IN.localTexcoord.xy + float2( ps.x, -ps.y));
                float4 p02 = tex2D(_Tex, IN.localTexcoord.xy + float2(-ps.x,  ps.y));
                float4 p00 = tex2D(_Tex, IN.localTexcoord.xy + float2(-ps.x, -ps.y));

                int neighbor_count = int((p12+p10+p21+p01+p22+p20+p02+p00).g + 0.1);
                float4 this_cell = p11 > 0.9 ? float4(1.0, 1.0, 1.0, 1.0) : float4(0.0, 0.0, 0.0, 1.0);

                if (neighbor_count < 2) {
                    return float4(0.0, 0.0, 0.0, 1.0);
                } else if (neighbor_count == 2) {
                    return this_cell;
                } else if (neighbor_count == 3) {
                    return float4(1.0, 1.0, 1.0, 1.0);
                } else if (neighbor_count > 3) {
                    return float4(0.3, 0.0, 0.0, 1.0);
                } else {
                    return float4(1.0, 0.0, 1.0, 1.0);
                }
           }
           ENDCG
        }
    }
}