Shader "Custom/CelShadingCharacter"
{
     Properties
    {   
        [Header(Main Textures)]
        [Space]
        [Space]
        [HDR] [MainColor] _MainColor("Main Color", Color) = (1, 1, 1, 1)
        [MainTexture] _MainTex("Main Texture" , 2D) = "white" {}
        _ShadeTex1("Shaded Tex" ,2D) = "white" {}
        _ILMTex("ILM Texture", 2D) = "white" {}
        _ShadeStrength("Shade Strength" ,Range(0,1)) = 1
        
        [Space]
        [Space]
        [Header(Diffuse properties)]
        [Space]
        [Space]
        _DiffuseColor("Diffuse Color" , Color) = (1,1,1,1)
        _DiffuseStrength("Diffuse Strength" , Float) = 1
        _DiffuseThreshold("Diffuse Threshold", Float) = 0.01
        _DiffuseSmoothness("Diffuse Smoothness" , Float) = 0.01
       
        [Space]
        [Space]
        [Header(Specular properties)]
        [Space]
        [Space]
        [Toggle(_USE_Normal)] _UseNormal ("Use Normal", Float) = 0
        [NoScaleOffset][Normal] _NormalTex("Normal Texture" , 2D) = "bump" {}
        _NormalStrength("Normal Strength" , Range(-2,2)) = 1

        [Space]
        [Space]
        [Header(Gloss properties)]
        [Space]
        [Space]
        _SpecularColor("Specular Color" , Color) = (1,1,1,1)
        _Gloss("Glossiness" , Range(0,1)) = 0.5
        _SpecularThreshold("Specular Threshold" , Float) = 0.1
        _SpecularSmoothness("Specular Smoothness" , Float) = 0.1

        [Space]
        [Space]
        [Header(Metallic properties)]
        [Space]
        [Space]
        [Toggle(_USE_Metallic)] _UseMetallic ("Use Metallic", Float) = 0
        _MetallicStrength("Metallic Strength" ,Float) = 0.5

        [Space]
        [Space]
        [Header(Fresnel properties)]
        [Space]
        [Space]
        _FresnelColor("Fresnel Color" , Color) = (1,1,1,1)
        _FresnelIntensity("Fresnel Intensity" , Float) = 1
        _FresnelThreshold("Fresnel Threshold" , Float) = 0.1
        _FresnelSmoothness("Fresnel Smoothness" , Float) = 0.1
        
        [Space]
        [Space]
        [Header(Ambient Occulision properties )]
        [Space]
        [Space]
        [Toggle(_USE_AO)] _UseAO("Use Ambient Occulison" , Float) = 0
        _AmbientStrength("Ambient Strength" , Float) = 1
        _OcculisionStrength("Occulision Strength" , Range(0,1)) = 0.5

        [Space]
        [Space]
        [Header(Emission properties)]
        [Space]
        [Space]
        [Toggle(_USE_Emission)] _UseEmission ("Use Emission", Float) = 0
        _EmissionTex("Emission Texture" , 2D) = "" {}
        _EmissionColor("Emission Color" , Color) = (1,1,1,1)
        _EmissionIntensity("Emission Intensity" , Float) = 1
        
       
        [Space]
        [Space]
        [Header(Hatching properties)]
        [Space]
        [Space]
        [Toggle(_USE_HATCHING)] _UseHatching ("Use Hatching", Float) = 0
        [HDR] _HatchColor("Hatch Color" , Color) = (1,1,1,1)
        _HatchTex("Hatch Texture" , 2D) = "white" {}
        _HatchMask("Hatch mask" ,2D) = "white" {}
        _HatchThreshold("Hatch Threshold", Float) = 0.01
        _HatchSmoothness("Hatch Smoothness" , Float) = 0.01
        _HatchOpacity("Hatch Opacity" , Float) = 0.5

        [Space]
        [Space]
        [Header(Hatching Animation)]
        [Space]
        [Space]
        _AnimationSpeed("Animation Speed" , Float) = 1
        _AnimationOffset("Animation Offset" , Range(0,1)) = 0.37

        [Space]
        [Space]
        [Header(Outline)]
        [Space]
        [Space]
        [Toggle(_USE_OUTLINE)] _UseOutline("Use Outline", Float) = 0
        [HDR] _OutlineColor("Outline Color" , Color) = (0,0,0,1)
        _OutlineWidth("Outline Width" , Float) = 0.01
        
        [Space]
        [Space]
        [Header(Dissolve)]
        [Space]
        [Space]
        [Toggle(_USE_Dissolve)] _UseDissolve("Use Dissolve" , Float) = 0
        [HDR] _DissolveColor("Dissolve Color" , Color) = (1,1,1,1)
        [NoScaleOffset] _DissolveTex("Dissolve Tex" , 2D) = "white" {}
        _DissolveThreshold("Dissolve Threshold" , Float) = 0.1
        _DissolveThickness("Dissolve Thickness" , Float) = 0.1

        [Space]
        [Space]
        [Header(Blending)]
        [Space]
        [Space]
        [Enum(UnityEngine.Rendering.BlendMode)]
            _SrcFactor("Src Factor", Float) = 5

        [Enum(UnityEngine.Rendering.BlendMode)]
            _DstFactor("Dst Factor" , Float) = 10

        [Enum(UnityEngine.Rendering.BlendOp)]
            _Opp("Operation" , Float) = 0     
        
    }

    SubShader
    {   
        Name "CelShadingPass"

        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent"}

        Blend [_SrcFactor] [_DstFactor]
        BlendOp [_Opp]

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _MainTex_ST;
                float4 _HatchTex_ST;
                float4 _ShadeTex1_ST;
                float4 _HatchMask_ST;

                float _ShadeStrength;

                float4 _DiffuseColor;
                float4 _SpecularColor;
                float4 _FresnelColor;
                float4 _EmissionColor;
                float4 _HatchColor;

                float _DiffuseStrength;
                float _DiffuseThreshold;
                float _DiffuseSmoothness;

                float _UseNormal;
                float _NormalStrength;
                float _Gloss;
                float _SpecularThreshold;
                float _SpecularSmoothness;


                float _UseMetallic;
                float _MetallicStrength;

                float _FresnelIntensity;
                float _FresnelThreshold;
                float _FresnelSmoothness;

                float _UseAO;
                float _OcculisionStrength;
                float _AmbientStrength;
                float _EmissionIntensity;
                
                float _UseHatching;
                float _HatchThreshold;
                float _HatchSmoothness;
                float _HatchOpacity;

                float _AnimationSpeed;
                float _AnimationOffset;

                float _UseOutline;
                float4 _OutlineColor;
                float _OutlineWidth;

                float _DissolveThreshold;
                float _DissolveThickness;
                float4 _DissolveColor;
                
            CBUFFER_END

        ENDHLSL

        Pass
        {   
            Tags { 
                "RenderType" = "Opaque" 
                "Queue" = "Geometry"
                "RenderPipeline" = "UniversalPipeline"
                "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _LIGHTS_PER_OBJECT
            
            // forward +
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            #pragma shader_feature_local _USE_HATCHING
            #pragma shader_feature_local _USE_Emission
            #pragma shader_feature_local _USE_Dissolve
            #pragma shader_feature_local _USE_Normal
            #pragma shader_feature_local _USE_Metallic
            #pragma shader_feature_local _USE_AO

            struct MeshData
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Interpolators
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS: TEXCOORD1;
                float3 wPos : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float3 normalOS : TEXCOORD4;

                float3 positionOS : TEXCOORD6;
                float2 hatchUV : TEXCOORD7; 
                float  fogCoord	: TEXCOORD8;
                float3 tangentWS : TEXCOORD9;
                float3 bitangentWS : TEXCOORD10;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
       
            sampler2D _MainTex;
            sampler2D _ShadeTex1;
            sampler2D _ILMTex;
            sampler2D _EmissionTex;
            sampler2D _NormalTex;
            sampler2D _HatchTex;
            sampler2D _DissolveTex;
            sampler2D _HatchMask;

            Interpolators vert(MeshData IN)
            {
                Interpolators OUT = (Interpolators)0;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.hatchUV = TRANSFORM_TEX(IN.uv, _HatchTex);

                OUT.normalOS = IN.normal;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.wPos = TransformObjectToWorld(OUT.positionOS);
                // OUT.wPos = mul(unity_ObjectToWorld , IN.positionOS).xyz;

                float3 normalWS = TransformObjectToWorldNormal(IN.normal);
                float3 tangentWS = TransformObjectToWorldDir(IN.tangent.xyz);
                float3 bitangentWS = cross(normalWS, tangentWS) * IN.tangent.w;

                OUT.normalWS = normalWS;
                OUT.tangentWS = tangentWS;
                OUT.bitangentWS = bitangentWS;

                //OUT.shadowCoord = TransformWorldToShadowCoord(OUT.wPos);
                OUT.fogCoord = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 frag(Interpolators IN) : SV_Target
            {   
                // Main and Tint (Shaded) Textures
                float4 texColor = tex2D(_MainTex,IN.uv);
                float4 shadeTex = tex2D(_ShadeTex1,IN.uv);
                float4 IlmTex = tex2D(_ILMTex,IN.uv);
                // texColor = texColor * _MainColor;

                // Lighting Data
                float4 shadowCoord = TransformWorldToShadowCoord(IN.wPos);
                Light mainLight = GetMainLight(shadowCoord);
                float3 mlDirection = mainLight.direction;
                float3 mlColor = mainLight.color;
                float mldistanceAtt = mainLight.distanceAttenuation;
                float mlshadowAtt = mainLight.shadowAttenuation;
                float shadow = mldistanceAtt * mlshadowAtt;
               
                half3 N = normalize(IN.normalWS); // World Space Normals (Vec3)
                half3 L = normalize(mlDirection); // Light direction (Vec3)
                half3 V = normalize(_WorldSpaceCameraPos - IN.wPos); // View Angle (Vec3)
                half3 R = reflect(-L,N); // Relect Angle (Vec3)
                half3 H = normalize(L + V); // Half Angle (Vec3)

                // Diffuse Lighting (CelShading)
                float diffuseLight = saturate(dot(N,L)) * 0.5 + 0.5; // Half Lambert 
                float diffuseSmooth = smoothstep(_DiffuseThreshold , (_DiffuseThreshold + _DiffuseSmoothness) , diffuseLight);
                float maxDiffuse = max(diffuseSmooth, _DiffuseStrength);
                float3 shadedAlbedo = lerp(texColor.rgb,texColor.rgb * shadeTex.rgb,_ShadeStrength);
                float3 finalDiffuse = maxDiffuse * shadedAlbedo *_MainColor.rgb * mlColor * shadow * _DiffuseColor.rgb;
                
                // Specular Lighting
                half specularLight = 0;
                half specularExponent = 0; 
                half specularSmooth = 0;
                half3 finalSpecular = 0;

                #if defined(_USE_Normal)
                    // Specualar Lighting (Realistic) (Bling Phong)

                    // converts to normal map 
                    float3 normalTS = UnpackNormal(tex2D(_NormalTex,IN.uv)); 
                    // Normal and Tangent
                    normalTS.xy *= _NormalStrength;  // normal intensity
                    normalTS = normalize(normalTS);
               
                    float3x3 TBN = float3x3(
                        normalize(IN.tangentWS),
                        normalize(IN.bitangentWS),
                        normalize(IN.normalWS)
                    );

                    float3 Nt = normalize(mul(normalTS,TBN));
                    specularLight = saturate(dot(H,Nt)) * (finalDiffuse.x > 0);
                    specularExponent = exp2(_Gloss * 6);
                    specularLight = pow(specularLight , specularExponent);
                    specularSmooth = smoothstep(_SpecularThreshold , (_SpecularThreshold + _SpecularSmoothness) , specularLight);
                    finalSpecular = specularSmooth * shadow * mlColor * _SpecularColor.rgb;    
                #else 
                    // Specular Lighting (CelShading) (Phong)
                    specularLight = saturate(dot(V,R)) * (finalDiffuse.x > 0);
                    specularExponent = exp2(_Gloss * 6);
                    specularLight = pow(specularLight , specularExponent);
                    specularSmooth = smoothstep(_SpecularThreshold , (_SpecularThreshold + _SpecularSmoothness) , specularLight);
                    finalSpecular = specularSmooth * mlColor * shadow * _SpecularColor.rgb;    
                #endif

                // Fresnel Lighting (CelShading)
                half fresnelLight =  1.0 - saturate(dot(N, V));
                fresnelLight = pow(fresnelLight , _FresnelIntensity);
                half fresnelSmooth = smoothstep(_FresnelThreshold , (_FresnelThreshold + _FresnelSmoothness) , fresnelLight);
                half3 finalFresnel = fresnelSmooth * _FresnelColor.rgb;

                // Ambient Occulision Lighting
                half ambientLight = 0;
                half3 maxAmbient = 0;
                half3 ambient = 0;
                half ambientOcculison = 0;

                #if defined(_USE_AO)
                    float occulisionMask = IlmTex.g;
                    ambientLight = saturate(dot(N,L));
                    maxAmbient = max(ambientLight , _AmbientStrength);
                    ambient = maxAmbient * SampleSH(N) * texColor.rgb * _DiffuseColor.rgb;
                    ambientOcculison = lerp(1 , occulisionMask, _OcculisionStrength.r);
                    ambient *= ambientOcculison;
                #endif

                //Object-space Triplanar Hatching
                float3 finalHatch = 0;

                #if defined(_USE_HATCHING)
                    float hatchMask = tex2D(_HatchMask,IN.uv).r;   

                    float3 p = IN.positionOS * _HatchTex_ST.x;
                    float phaseOffset = _AnimationOffset;
                    float animPhase = frac(_Time.y * _AnimationSpeed);
                    float3 p1 = p;
                    float3 p2 = p + phaseOffset;
                    float3 nOS = normalize(IN.normalOS);

                    float3 blend = abs(nOS);
                    blend /= (blend.x + blend.y + blend.z + 1e-5);

                    float3 hatchX = tex2D(_HatchTex, p1.yz).rgb;
                    float3 hatchY = tex2D(_HatchTex, p1.xz).rgb;
                    float3 hatchZ = tex2D(_HatchTex, p1.xy).rgb;
                    float3 hatchTex1 = hatchX * blend.x + hatchY * blend.y + hatchZ * blend.z;


                    float3 hatchX2 = tex2D(_HatchTex, p2.yz).rgb;
                    float3 hatchY2 = tex2D(_HatchTex, p2.xz).rgb;
                    float3 hatchZ2 = tex2D(_HatchTex, p2.xy).rgb;
                    float3 hatchTex2 = hatchX2 * blend.x + hatchY2 * blend.y + hatchZ2 * blend.z;


                    float hatchLight = saturate(1.0 - dot(N, L));
                    float hatchSmooth = smoothstep(_HatchThreshold - _HatchSmoothness,_HatchThreshold + _HatchSmoothness,hatchLight);
                    float hatchOpacitySmooth = smoothstep(0,_HatchOpacity,1.0 - mlshadowAtt); 
                    hatchSmooth *= shadow;

                
                    float hatchAnim = smoothstep(0.3, 0.7, animPhase);
                    float3 mixedHatch = lerp(hatchTex1, hatchTex2, hatchAnim);
                    float hatch = 1.0 - dot(mixedHatch, float3(0.333,0.333,0.333));
                    finalHatch = -hatch * hatchMask * hatchSmooth * _HatchColor.rgb * hatchOpacitySmooth;   
                #endif

                //Emission mask
                float3 finalEmission = 0;

                #if defined(_USE_Emission)
                    float4 emissionTex = tex2D(_EmissionTex, IN.uv);
                    float mask = dot(emissionTex.rgb, float3(0.299, 0.587, 0.114)); // Use texture brightness as mask
                    finalEmission = emissionTex.rgb * _EmissionColor.rgb * _EmissionIntensity * mask;
                #endif

                // Additional Light Calculations
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.wPos;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionHCS);

    
                // #if defined(_LIGHTS_PER_OBJECT)
                //     return float4(1, 0, 0, 1);
                // #endif

                half3 additionalDiffuse = 0;
                half3 additionalSpecular = 0;

                #if defined(_ADDITIONAL_LIGHTS)

                    uint pixelLightCount = GetAdditionalLightsCount();

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));

                        float3 L = normalize(light.direction);
                        float3 R = reflect(-L, N);

                        float att = light.distanceAttenuation * light.shadowAttenuation;

                        // Toon diffuse (Additional)
                        float diff = saturate(dot(N, L)) * 0.5 + 0.5;
                        diff = smoothstep(_DiffuseThreshold,_DiffuseThreshold + _DiffuseSmoothness,diff);

                        additionalDiffuse += diff * att * light.color * texColor.rgb * _DiffuseColor.rgb;

                        // Toon specular (Additional)
                        half spec = pow(saturate(dot(V, R)), _Gloss);
                        half specStep = smoothstep(_SpecularThreshold,_SpecularThreshold + _SpecularSmoothness,spec);
                        additionalSpecular += specStep * att * light.color* _SpecularColor.rgb;

                    LIGHT_LOOP_END
                #endif

                // Final Output (CelShading)         
                half3 mixedColor = finalDiffuse + additionalDiffuse + additionalSpecular + finalSpecular + finalFresnel + ambient + finalEmission + finalHatch;
                half4 finalOutput = half4(mixedColor,1);
                finalOutput.rgb = MixFog(finalOutput.rgb, IN.fogCoord);

                // Dissolve Shader
                float dissolveStepUp = 1;
                half3 finalDissolve = finalOutput.rgb;
                float visibleMask = 1;

                #if defined (_USE_Dissolve)
                    float threshold = saturate(_DissolveThreshold);
                    float thickness = saturate(_DissolveThickness);
                    float4 dissolveTex = tex2D(_DissolveTex,IN.positionOS);
                    float dissolveValue = saturate(dissolveTex.r);
                    visibleMask = step(dissolveValue,threshold);
                    dissolveStepUp = step(dissolveValue,threshold + thickness);
                    float dissolveStepDown = step(dissolveTex.r,threshold - thickness);
                    float dissolveDifference = dissolveStepUp - dissolveStepDown;
                    finalDissolve = lerp(finalOutput.rgb,_DissolveColor.rgb,dissolveDifference);
                #endif

                return half4(finalDissolve.rgb , finalOutput.a * visibleMask);
            }
            ENDHLSL
        }
        
        //Outline
        Pass
        {
            Name "Outline"
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _USE_OUTLINE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            
            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                #ifdef _USE_OUTLINE
                    float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                    float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                    positionWS += normalWS * _OutlineWidth;
                    OUT.positionHCS = TransformWorldToHClip(positionWS);
                #else
                    OUT.positionHCS = 0;
                #endif

        return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {   
                return _OutlineColor;
            }

            ENDHLSL
        }


    //     Pass 
    //     {
    //         Name "ShadowCaster"
            
    //         Tags{ "LightMode" = "ShadowCaster"}

    //         ColorMask 0

    //         HLSLPROGRAM
    //         #pragma vertex vert
    //         #pragma fragment frag
               
    //         #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    //         struct MeshData
    //         {
    //             float4 positionOS : POSITION;
    //             float3 normalOS : NORMAL;
    //         };

    //         struct Interpolators
    //         {
    //             float4 positionHCS : SV_POSITION;
    //         };
            
    //         float4 GetShadowPositionHClip(MeshData IN)
    //         {   
    //             float3 lightDirectionWS = _MainLightPosition.xyz;
    //             float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
    //             float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
    //             float4 positionCS = TransformWorldToHClip(ApplyShadowBias(posWS,normalWS,lightDirectionWS));
    //             positionCS = ApplyShadowClamping(positionCS);

    //             return positionCS;
    //         };

    //         Interpolators vert (MeshData IN)
    //         {
    //             Interpolators OUT;

    //             OUT.positionHCS = GetShadowPositionHClip(IN);

    //             return OUT;
    //         }

    //         half4 frag (Interpolators IN) : SV_Target
    //         {   
    //             return 0;
    //         }

    //         ENDHLSL
    //     }

        //UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
