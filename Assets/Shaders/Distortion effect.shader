Shader "Custom/Distortioneffect"
{
    Properties
    {   
        [Header(Distortion Effect)]
        [Space]
        [Space]

        [NoScaleOffset] _DistortionTex("Distortion Texture" , 2D) = "bump" {}
        [HDR] _MainColor("Main Color" , Color) = (0,0,0,1)
        _DistortionStrength("DistortionStrength" , Float) = 0.1
        _ScrollSpeed("Scroll Speed" , Vector) = (1,0,0,0)
        _Transparency("Transparency" , Range(0,1)) = 0.5

        [Header(Masking)]
        [Space]
        [Space]

        _MaskTex("Mask Texture" , 2D) = "" {}
        _MaskingStrength("Masking Strength" , Float) = 1
        _Threshold("Mask Threshold" , Float) = 0.10
        _Smoothness("Mask Smoothness" , Float) = 0.3

        [Header(Emmision)]
        [Space]
        [Space]

        [NoScaleOffset] _EmissionTex("Emission Texture" , 2D) = "" {}
        [HDR] _EmissionColor("Emission Color" , Color) = (1,1,1,1)

        [Header(Blending Modes)]
        [Space]
        [Space]

        [Enum(UnityEngine.Rendering.BlendMode)]
        _SrcFactor("Src Factor", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)]
        _DstFactor("Dst Factor" , Float) = 10
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" 
        "Queue" = "Transparent"
        "RenderPipeline" = "UniversalPipeline" }
        
        Blend [_SrcFactor] [_DstFactor] // Blend option
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct MeshData
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct Interpolator
            {
                float4 positionHCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float4 color : TEXCOORD3;
            };

            sampler2D _DistortionTex;
            sampler2D _EmissionTex;
            sampler2D _MaskTex;
            sampler2D _CameraOpaqueTexture; // Camera texture

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _ScrollSpeed;
                float4 _EmissionColor;
                float _DistortionStrength;
                float _Transparency;
                float _Smoothness;
                float _Threshold;
                float _MaskingStrength;
            CBUFFER_END

            Interpolator vert(MeshData IN)
            {
                Interpolator OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                OUT.uv0 = IN.uv;
                OUT.uv1 = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            float4 frag(Interpolator IN) : SV_Target
            {   

                // Scroll effect with UV
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 scrollUV = IN.uv0 +_ScrollSpeed.xy * _Time.y;


                // Normal map 
                float3 normalMap = UnpackNormal(tex2D(_DistortionTex,scrollUV)); // converts to normal map 
                screenUV += normalMap.xy * _DistortionStrength;

                // Scene Color
                float4 sceneColor = tex2D(_CameraOpaqueTexture, screenUV);

                // Masking effect
                float4 maskMap = tex2D(_MaskTex ,IN.uv1).r;
                maskMap = saturate(maskMap * _MaskingStrength);
                float maskSmoothStep = smoothstep(_Threshold - _Smoothness ,_Threshold + _Smoothness, maskMap); 
                //float maskStep = saturate((_MaskingStrength - 0.5, _MaskingStrength + 0.5, IN.uv0));

                // Emission effect
                float4 emissionMap = tex2D(_EmissionTex,scrollUV);
                float mask = dot(emissionMap.rgb, float3(0.299, 0.587, 0.114)); //Use texture brightness as mask
                float4 finalEmission = emissionMap * _EmissionColor * mask;

                // Transparency
                sceneColor.a = _Transparency * maskSmoothStep;

                float4 finalOutput = (sceneColor * _MainColor  + finalEmission) * maskSmoothStep * IN.color;  

                return finalOutput;
            }
            ENDHLSL
        }
    }
}


