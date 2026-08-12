Shader "Train/Freeze Overlay"
{
    Properties
    {
        _BaseColor ("冻结蓝", Color) = (0.08, 0.55, 1, 0.38)
        _RimColor ("冰晶边缘", Color) = (0.45, 0.9, 1, 0.9)
        _Pulse ("冻结脉冲", Range(0, 1)) = 0
        _Opacity ("透明度", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back
        Offset -1, -1
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionHCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _Pulse;
                float _Opacity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float fresnel = pow(1.0 - saturate(dot(normalize(input.normalWS), viewDir)), 2.8);
                float scan = 0.5 + 0.5 * sin((input.positionWS.y * 8.0 + _Pulse * 10.0) * 6.28318);
                float3 color = lerp(_BaseColor.rgb, _RimColor.rgb, saturate(fresnel + scan * 0.28));
                float alpha = _Opacity * (0.35 + fresnel * 0.65);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
