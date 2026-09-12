Shader "Custom/EmissiveScrollMobile"
{
    Properties
    {
        [Header(Base Maps)]

        [HDR][MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        [Header(Emission)]

        [HDR] _EmisionColor("Emission Color", Color) = (1,1,1,1)
        _EmissionMask("Emission Mask", 2D) = "white" {}
        _ScrollDir("Scroll Direction", Vector) = (1,0,0,0)

        [Header(Fresnel)]

        [HDR] _FresnelColor("Fresnel Color", Color) = (1,1,1,1)
        _FresnelIntensity("Fresnel Intensity", Range(0.1,8)) = 1
        _FresnelThreshold("Fresnel Threshold", Range(0,1)) = 0.1
        _FresnelSmoothness("Fresnel Smoothness", Range(0.001,1)) = 0.1

        [Header(Transparency)]

        _Transparency("Transparency", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "Forward"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;

                half2 uvBase : TEXCOORD0;
                half2 uvEmission : TEXCOORD1;

                half3 normalWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
            };


            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_EmissionMask);
            SAMPLER(sampler_EmissionMask);


            CBUFFER_START(UnityPerMaterial)

                half4 _BaseColor;
                float4 _BaseMap_ST;

                half4 _EmisionColor;
                float4 _EmissionMask_ST;

                half4 _ScrollDir;

                half4 _FresnelColor;
                half _FresnelIntensity;
                half _FresnelThreshold;
                half _FresnelSmoothness;

                half _Transparency;

            CBUFFER_END


            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;

                OUT.normalWS =
                    TransformObjectToWorldNormal(IN.normalOS);

                OUT.uvBase =
                    TRANSFORM_TEX(IN.uv, _BaseMap);

                OUT.uvEmission =
                    TRANSFORM_TEX(IN.uv, _EmissionMask);

                return OUT;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                //----------------------------------
                // Base Texture
                //----------------------------------

                half4 baseColor =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        IN.uvBase
                    ) * _BaseColor;


                //----------------------------------
                // Scrolling Emission
                //----------------------------------

                half2 scrollUV =
                    IN.uvEmission +
                    _ScrollDir.xy * _Time.y;

                half3 emission =
                    SAMPLE_TEXTURE2D(
                        _EmissionMask,
                        sampler_EmissionMask,
                        scrollUV
                    ).rgb *
                    _EmisionColor.rgb;


                //----------------------------------
                // Fresnel
                //----------------------------------

                half3 N =
                    normalize(IN.normalWS);

                half3 V =
                    normalize(
                        _WorldSpaceCameraPos -
                        IN.positionWS
                    );

                half fresnel =
                    1.0h -
                    saturate(dot(N, V));

                fresnel =
                    pow(fresnel, _FresnelIntensity);

                half fresnelMask =
                    smoothstep(
                        _FresnelThreshold,
                        _FresnelThreshold +
                        _FresnelSmoothness,
                        fresnel
                    );

                half3 fresnelColor =
                    fresnelMask *
                    _FresnelColor.rgb;


                //----------------------------------
                // Final
                //----------------------------------

                half4 finalColor;

                finalColor.rgb =
                    baseColor.rgb +
                    emission +
                    fresnelColor;

                finalColor.a =
                    baseColor.a *
                    (1.0h - _Transparency);

                return finalColor;
            }

            ENDHLSL
        }
    }
}