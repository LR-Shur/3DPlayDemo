Shader "Train/Active Field"
{
    Properties
    {
        _BaseColor ("颜色", Color) = (0.45, 0.05, 1, 0.8)
        _Pulse ("脉冲", Range(0, 1)) = 0
        _WaveScale ("波纹密度", Float) = 8
        _Opacity ("透明度", Range(0, 1)) = 0.8
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
                float _WaveScale;
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
                float band = 0.5 + 0.5 * sin((input.uv.x * _WaveScale + _Pulse * 12.0) * 6.28318);
                float grid = 0.5 + 0.5 * sin((input.uv.y * _WaveScale - _Pulse * 8.0) * 6.28318);
                float alpha = _BaseColor.a * _Opacity * (0.42 + band * 0.34 + grid * 0.24);
                return half4(_BaseColor.rgb * (0.8 + band * 0.45), alpha);
            }
            ENDHLSL
        }
    }
}
