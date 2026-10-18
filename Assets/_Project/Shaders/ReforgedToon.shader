// "Living Comic" toon shader for URP.
//  - Stepped (cel) main-light shading with a tinted shade color.
//  - Screen-space halftone dots that grow in shadow, like comic print.
//  - Rim light, and an inverted-hull ink outline that thickens on big hits (the global
//    _ReforgedImpact, driven by FeedbackDirector) and can be tinted per player (_OutlineColor).
Shader "Reforged/Toon"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _ShadeColor ("Shade Tint", Color) = (0.55, 0.5, 0.75, 1)
        _Steps ("Light Steps", Range(1, 4)) = 2
        _ShadeThreshold ("Shade Threshold", Range(-1, 1)) = 0.1
        _HalftoneScale ("Halftone Dot Size (px)", Range(2, 24)) = 7
        _HalftoneStrength ("Halftone Strength", Range(0, 1)) = 0.45
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.5
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.35
        _OutlineColor ("Outline Color", Color) = (0.05, 0.04, 0.08, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.012
        _ImpactOutlineBoost ("Impact Outline Boost", Range(0, 4)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadeColor;
            half _Steps;
            half _ShadeThreshold;
            half _HalftoneScale;
            half _HalftoneStrength;
            half4 _RimColor;
            half _RimPower;
            half _RimStrength;
            half4 _OutlineColor;
            float _OutlineWidth;
            half _ImpactOutlineBoost;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        // Global, set by FeedbackDirector: 0 normally, spikes toward 1 on heavy impacts.
        float _ReforgedImpact;
        ENDHLSL

        Pass
        {
            Name "ForwardToon"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.screenPos = ComputeScreenPos(pos.positionCS);
                o.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half Halftone(float4 screenPos, half darkness)
            {
                float2 pixel = (screenPos.xy / screenPos.w) * _ScreenParams.xy / _HalftoneScale;
                // 45-degree screen like real print.
                float2 rotated = float2(pixel.x - pixel.y, pixel.x + pixel.y) * 0.70710678;
                float2 cell = frac(rotated) - 0.5;
                float radius = darkness * 0.75;
                float d = length(cell);
                float aa = fwidth(d);
                return 1.0 - smoothstep(radius - aa, radius + aa, d);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float3 n = normalize(i.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light light = GetMainLight(shadowCoord);

                half ndl = dot(n, light.direction) * light.shadowAttenuation;
                half lit = saturate((ndl - _ShadeThreshold) * 4.0 + 0.5);
                half steps = max(1.0, _Steps);
                lit = floor(lit * steps + 0.5) / steps;

                half3 shade = albedo.rgb * _ShadeColor.rgb;
                half3 color = lerp(shade, albedo.rgb, lit) * light.color;
                color += SampleSH(n) * albedo.rgb * 0.35;

                half dots = Halftone(i.screenPos, (1.0 - lit) * _HalftoneStrength);
                color = lerp(color, shade * 0.55, dots * (1.0 - lit));

                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                half rim = pow(1.0 - saturate(dot(n, viewDir)), _RimPower) * _RimStrength * (0.4 + 0.6 * lit);
                color += _RimColor.rgb * rim;

                color = MixFog(color, i.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "InkOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                float4 clip = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP, TransformObjectToWorldNormal(input.normalOS));
                float width = _OutlineWidth * (1.0 + _ReforgedImpact * _ImpactOutlineBoost);
                // Constant screen-space width: offset in clip space scaled by w, corrected for aspect.
                float2 offset = normalize(normalCS.xy + 1e-5) * width * clip.w;
                offset.x *= _ScreenParams.y / _ScreenParams.x;
                clip.xy += offset;
                o.positionCS = clip;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - positionWS);
            #else
                float3 lightDir = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            half4 Frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half Frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
