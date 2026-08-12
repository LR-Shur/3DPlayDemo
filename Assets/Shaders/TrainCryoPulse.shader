Shader "Train/Cryo Pulse"
{
    Properties
    {
        _BaseColor ("颜色", Color) = (0.04, 0.65, 1, 0.9)
        _Pulse ("脉冲", Range(0, 1)) = 0
        _EdgePower ("边缘", Float) = 2
        _Opacity ("透明度", Range(0, 1)) = 0.9
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Pulse;
                float _EdgePower;
                float _Opacity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float edge = pow(saturate(1.0 - abs(input.uv.y - 0.5) * 2.0), _EdgePower);
                float sparkle = 0.5 + 0.5 * sin((input.uv.x * 17.0 + input.uv.y * 9.0 + _Pulse * 15.0) * 6.28318);
                float alpha = _BaseColor.a * _Opacity * (0.18 + edge * 0.62 + sparkle * 0.2);
                return half4(_BaseColor.rgb * (0.75 + sparkle * 0.6), alpha);
            }
            ENDHLSL
        }
    }
}
