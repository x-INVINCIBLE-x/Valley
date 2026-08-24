Shader "Custom/EmissiveScroll"
{
    Properties
    {   
        [Header(Base Maps)]
        [Space]
        [Space]

        [HDR][MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        [Header(Emission Properties)]
        [Space]
        [Space]

        [HDR] _EmisionColor("Emission Color",Color) = (1, 1, 1, 1)
        _EmissionMask("Emission Mask",2D) = "white" {}
        _ScrollDir("Scroll Direction",Vector) = (1,0,0,0)

        [Header(Fresnel Properties)]
        [Space]
        [Space]

        [HDR] _FresnelColor("Fresnel Color",Color) = (1,1,1,1)
        _FresnelIntensity("Fresnel Intensity",Float) = 1
        _FresnelThreshold("Fresnel Threshold", Float) = 0.1
        _FresnelSmoothness("Fresnel Smoothnes",Float) = 0.1

        [Header(Transparency Properties)]
        [Space]
        [Space]

        _Transparency("Transparency", Range(0,1)) = 0

    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            
        
            struct MeshData
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct Interpolators
            {
                float4 positionHCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float3 wPos : TEXCOORD3;
                float3 normalWS: TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_EmissionMask);
            SAMPLER(sampler_EmissionMask);

            sampler2D _CameraOpaqueTexture;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;

                float4 _EmissionMask_ST;
                float4 _EmisionColor;
                float4 _ScrollDir;

                float4 _FresnelColor;
                float _FresnelIntensity;
                float _FresnelThreshold;
                float _FresnelSmoothness;

                float _Transparency;
            CBUFFER_END

            Interpolators vert(MeshData IN)
            {
                Interpolators OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);

                OUT.wPos = TransformObjectToWorld(IN.positionOS.xyz);

                OUT.normalWS = TransformObjectToWorldNormal(IN.normal);

                OUT.uv0 = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.uv1 = TRANSFORM_TEX(IN.uv, _EmissionMask);

                return OUT;
            }

            half4 frag(Interpolators IN) : SV_Target
            {   
                half3 N = normalize(IN.normalWS);
                half3 V = normalize(_WorldSpaceCameraPos - IN.wPos);

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 scrollUV = IN.uv1 +_ScrollDir.xy * _Time.y;

                float4 sceneColor = tex2D(_CameraOpaqueTexture, screenUV);
                

                half fresnelLight =  1.0 - saturate(dot(N, V));
                fresnelLight = pow(fresnelLight , _FresnelIntensity);
                half fresnelSmooth = smoothstep(_FresnelThreshold , (_FresnelThreshold + _FresnelSmoothness) , fresnelLight);
                half4 finalFresnel = fresnelSmooth * _FresnelColor.rgba;

                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,IN.uv0) * _BaseColor;
                half4 emissionColor = SAMPLE_TEXTURE2D(_EmissionMask,sampler_EmissionMask,scrollUV) * _EmisionColor;

                half3 finalrgb =
                    baseColor.rgb +
                    emissionColor.rgb +
                    finalFresnel.rgb;

                half4 finalColor;
                finalColor.rgb = finalrgb;
                finalColor.a = (1.0 - _Transparency) * baseColor.a;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}
