Shader "Custom/CelshadeTwist"
{
    Properties
    {   
        [Header(Main Textures)]
        [Space]
        [Space]
        [HDR] [MainColor] _MainColor("Main Color", Color) = (1, 1, 1, 1)
        [MainTexture] _MainTex("Main Texture" , 2D) = "white" {}
        _ShadeTex1("Shaded Tex" ,2D) = "white" {}
        _ShadeStrength("Shade Strength" ,Range(0,1)) = 1
        [Space]
        [Space]
        
        [Header(World Space Tiling)]
        [Space]
        [Space]
        [ToggleUI] _UseWorldTiling ("Use World Tiling", Float) = 0
        [Space]
        [Space]
        [ToggleUI] _XZProjection ("XZ Projection", Float) = 0 // Top down
        [ToggleUI] _XYProjection ("XY Projection", Float) = 0 // Front 
        [ToggleUI] _ZYProjection ("ZY Projection", Float) = 0 // Side
        [Space]
        [Space]
        _WorldTilingXMain("World Tiling X Main" , Float) = 1
        _WorldTilingYMain("World Tiling Y Main" , Float) = 1
        [Space]
        _WorldTilingXShade("World Tiling X Main" , Float) = 1
        _WorldTilingYShade("World Tiling Y Main" , Float) = 1

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
        [ToggleUI] _UseNormal ("Use Normal", Float) = 0
        [ToggleUI] _UseBlingPhong("BlingPhong" , Float) = 0
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
        [Header(Ambient properties)]
        [Space]
        [Space]
        [ToggleUI] _UseAmbient ("Use Ambient", Float) = 0
        _AmbientStrength("Ambient Strength" , Float) = 1

        [Space]
        [Space]
        [Header(Emission properties)]
        [Space]
        [Space]
        [ToggleUI] _UseEmission ("Use Emission", Float) = 0
        _EmissionTex("Emission Texture" , 2D) = "" {}
        _EmissionColor("Emission Color" , Color) = (1,1,1,1)
        _EmissionIntensity("Emission Intensity" , Float) = 1
        
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
        [Header(Additional Lightings properties)]
        [Space]
        [Space]
        [ToggleUI] _UseAdditionalLights("Use Additional Lights" , Float) = 0
    }

    SubShader
    {   
        Name "CelShadingPass"

        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Blend One Zero
        BlendOp Add
        ZWrite On
           
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "TwistedSpiral.hlsl"
        #include "VertexManipulation.hlsl"

        

        CBUFFER_START(UnityPerMaterial)
            float4 _MainColor;
            float4 _MainTex_ST;
            float4 _ShadeTex1_ST;
           
            float _UseWorldTiling;
            float _XZProjection;
            float _XYProjection;
            float _ZYProjection;
            float _WorldTilingXMain;
            float _WorldTilingYMain;
            float _WorldTilingXShade;
            float _WorldTilingYShade;

            float4 _DiffuseColor;
            float4 _SpecularColor;
            float4 _EmissionColor;

            float _DiffuseStrength;
            float _DiffuseThreshold;
            float _DiffuseSmoothness;
            float _ShadeStrength;

            float _UseNormal;
            float _UseBlingPhong;
            float _NormalStrength;
            float _Gloss;
            float _SpecularThreshold;
            float _SpecularSmoothness;

            float _UseAmbient;
            float _AmbientStrength;

            float _UseEmission;
            float _EmissionIntensity;

            float _FresnelIntensity;
            float _FresnelThreshold;
            float _FresnelSmoothness;

            float _UseBend;
            float3 _BendAmount;
            float _PivotY;

            float _UseTwist;
            float3 _TwistAmount;

            float _UseAdditionalLights;

        CBUFFER_END
        ENDHLSL

        Pass
        {   
            Name "Forward Pass"
            Tags { 
                "RenderType" = "Opaque" 
                "Queue" = "Geometry"
                "LightMode" = "UniversalForward" 
                }


            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
        

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
         
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _LIGHTS_PER_OBJECT
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            
            // forward +
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog


            // Baking compilations 
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_instancing
    
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            #pragma shader_feature_local _BEND_ON
            #pragma shader_feature_local _TWIST_ON

            struct MeshData
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 lightmapUV: TEXCOORD1;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Interpolators
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float3 normalWS: TEXCOORD2;
                float3 wPos : TEXCOORD3;
                float3 normalOS : TEXCOORD4;
                float3 positionOS : TEXCOORD5;
                float2 lightmapUV: TEXCOORD6;
                float  fogCoord	: TEXCOORD7;
                float3 tangentWS : TEXCOORD8;
                float3 bitangentWS : TEXCOORD9;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
       
            sampler2D _MainTex;
            sampler2D _ShadeTex1;
            sampler2D _EmissionTex;
            sampler2D _NormalTex;
            //float3 _BendPivotWS;
           
            Interpolators vert(MeshData IN)
            {   
                Interpolators OUT = (Interpolators)0;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 positionOS = IN.positionOS.xyz;


                
                #ifdef _TWIST_ON
                positionOS = TwistArountAxis(positionOS,float3(1,0,0),_TwistAmount.x);
                positionOS = TwistArountAxis(positionOS,float3(0,1,0),_TwistAmount.y);
                positionOS = TwistArountAxis(positionOS,float3(0,0,1),_TwistAmount.z);
                #endif

                #ifdef _BEND_ON
                positionOS = BendAroundAxis(positionOS,float3(0,1,0),float3(1,0,0),_BendAmount.x);
                positionOS = BendAroundAxis(positionOS,float3(1,0,0),float3(0,1,0),_BendAmount.y);
                positionOS = BendAroundAxis(positionOS,float3(0,1,0),float3(0,0,1),_BendAmount.z);
                #endif

                // Vertex Bending
                // #ifdef _BEND_ON
                // {
                //     if(_BendAmount.x != 0)
                //     {
                //         BendX(positionOS,_BendAmount.x,_PivotY);
                //     }
                //     if(_BendAmount.y != 0)
                //     {
                //         BendY(positionOS,_BendAmount.y,_PivotY);
                //     }
                //     if(_BendAmount.z != 0)
                //     {
                //         BendZ(positionOS,_BendAmount.z,_PivotY);
                //     }
                // }
                // #endif

                // #ifdef _TWIST_ON
                // {
                //     if(_TwistAmount.x != 0)
                //     {
                //         TwistX(positionOS, _TwistAmount.x);
                //     }
                //     if(_TwistAmount.y != 0)
                //     {
                //         TwistY(positionOS, _TwistAmount.y);
                //     }
                //     if(_TwistAmount.z != 0)
                //     {
                //         TwistZ(positionOS, _TwistAmount.z);
                //     } 
                // }
                // #endif

                OUT.positionHCS = TransformObjectToHClip(positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uv1 = TRANSFORM_TEX(IN.uv, _ShadeTex1);
               
                OUT.normalOS = IN.normal;
                OUT.positionOS = positionOS;
                OUT.wPos = TransformObjectToWorld(positionOS);

                float3 normalWS = TransformObjectToWorldNormal(IN.normal);
                float3 tangentWS = TransformObjectToWorldDir(IN.tangent.xyz);
                float3 bitangentWS = cross(normalWS, tangentWS) * IN.tangent.w;

                OUT.lightmapUV = IN.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;

                OUT.normalWS = normalWS;
                OUT.tangentWS = tangentWS;
                OUT.bitangentWS = bitangentWS;
                OUT.fogCoord = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
                
            }

            half4 frag(Interpolators IN) : SV_Target
            {     
                UNITY_SETUP_INSTANCE_ID(IN);

   
                // Lighting Data
                float4 shadowCoord = TransformWorldToShadowCoord(IN.wPos);
                Light mainLight = GetMainLight(shadowCoord);
                float3 mlDirection = mainLight.direction;
                float3 mlColor = mainLight.color;
                float mldistanceAtt = mainLight.distanceAttenuation;
                float mlshadowAtt = mainLight.shadowAttenuation;
                float shadow = mlshadowAtt * mldistanceAtt;
                shadow = 1; // shadow acne quick fix


                half3 N = normalize(IN.normalWS); //NORMAL
                half3 L = mlDirection; // Light direction
                half3 V = normalize(_WorldSpaceCameraPos - IN.wPos); // View Angle
                
                float2 TilingMain = IN.uv;
                float2 TilingShade = IN.uv1;

                // Convert toggles to 0 or 1
                float useWorld = step(0.5, _UseWorldTiling);
                float projXZ = step(0.5, _XZProjection);
                float projXY = step(0.5, _XYProjection);
                float projZY = step(0.5, _ZYProjection);

                // All the  projections
                float2 uvXZ_Main = float2(IN.wPos.x * _WorldTilingXMain,IN.wPos.z * _WorldTilingYMain);
                float2 uvXY_Main = float2(IN.wPos.x * _WorldTilingXMain,IN.wPos.y * _WorldTilingYMain);
                float2 uvZY_Main = float2(IN.wPos.z * _WorldTilingXMain,IN.wPos.y * _WorldTilingYMain);
                                       
                float2 uvXZ_Shade = float2(IN.wPos.x * _WorldTilingXShade,IN.wPos.z * _WorldTilingYShade);
                float2 uvXY_Shade = float2(IN.wPos.x * _WorldTilingXShade,IN.wPos.y * _WorldTilingYShade);
                float2 uvZY_Shade = float2(IN.wPos.z * _WorldTilingXShade,IN.wPos.y * _WorldTilingYShade);
                                           

                // Blend projections (1 ideally)
                float2 worldMain = uvXZ_Main * projXZ + uvXY_Main * projXY + uvZY_Main * projZY;                 
                float2 worldShade = uvXZ_Shade * projXZ + uvXY_Shade * projXY + uvZY_Shade * projZY;
                                      
                // Final UV
                TilingMain  = lerp(TilingMain,  worldMain,  useWorld);
                TilingShade = lerp(TilingShade, worldShade, useWorld);
                                
                // Diffuse Lighting (CelShading)
                float4 texColor = tex2D(_MainTex,TilingMain);
                half diffuseLight = saturate(dot(N,L)) * 0.5 + 0.5;
                half diffuseSmooth = smoothstep(_DiffuseThreshold , (_DiffuseThreshold + _DiffuseSmoothness) , diffuseLight);
                half maxDiffuse = max(diffuseSmooth, _DiffuseStrength);
                
                // Shaded Texture Blend
                half3 shadedAlbedo = texColor.rgb;

                if(_ShadeStrength > 0.001)
                {
                    float3 shadeTex = tex2D(_ShadeTex1,TilingShade).rgb;
                    shadedAlbedo = lerp(texColor.rgb,texColor.rgb * shadeTex.rgb,_ShadeStrength);
                }
                shadedAlbedo *= _MainColor.rgb;

                half3 finalDiffuse = maxDiffuse * shadedAlbedo * mlColor * shadow;

                // Specular Lighting
                half specularLight = 0;
                half specularExponent = 0; 
                half specularSmooth = 0;
                half3 finalSpecular = 0;

                if(_UseNormal > 0.5)
                {
                    if (_UseBlingPhong > 0.5)
                    {
                        // Specualar Lighting (Realistic) (Bling Phong)

                        // Texture to normal map convertion 
                        float3 normalTS = UnpackNormal(tex2D(_NormalTex,IN.uv)); 

                        // Normal and Tangent
                        normalTS.xy *= _NormalStrength;  // normal intensity
                        normalTS = normalize(normalTS);
               
                        float3x3 TBN = float3x3(
                        normalize(IN.tangentWS),
                        normalize(IN.bitangentWS),
                        normalize(IN.normalWS)
                        );

                        half3 H = normalize(L + V); // Half Vector
                        float3 Nt = normalize(mul(normalTS,TBN));
                        specularLight = saturate(dot(H,Nt)) * (finalDiffuse.x > 0);
                        specularExponent = exp2(_Gloss * 64);
                        specularLight = pow(specularLight , specularExponent);
                        specularSmooth = smoothstep(_SpecularThreshold , (_SpecularThreshold + _SpecularSmoothness) , specularLight);
                        finalSpecular = specularSmooth * mlColor * shadow * _SpecularColor.rgb;    
                    }
                    else 
                    {
                        // Specular Lighting (CelShading) (Phong)
                        half3 R = reflect(-L,N); // Relect Angle
                        specularLight = saturate(dot(V,R)) * (finalDiffuse.x > 0);
                        specularExponent = exp2(_Gloss * 64);
                        specularLight = pow(specularLight , specularExponent);
                        specularSmooth = smoothstep(_SpecularThreshold , (_SpecularThreshold + _SpecularSmoothness) , specularLight);
                        finalSpecular = specularSmooth * mlColor * shadow * _SpecularColor.rgb;    
                    }
                }
                
                // Ambient Lighting
                half3 finalAmbient = 0;
                if(_UseAmbient)
                {
                    half ambientLight = saturate(dot(N,L));
                    half3 maxAmbient = max(ambientLight , _AmbientStrength);
                    finalAmbient = maxAmbient * SampleSH(N);
                }
                
                //Emission 
                half3 finalEmission = 0;
                if (_UseEmission > 0.5)
                {
                    float4 emissionTex = tex2D(_EmissionTex, IN.uv);
                    half mask = dot(emissionTex.rgb, half3(0.299, 0.587, 0.114)); // Use texture brightness as mask
                    finalEmission = emissionTex.rgb * _EmissionColor.rgb * _EmissionIntensity * mask;
                }

                half3 additionalDiffuse = 0;
                half3 additionalSpecular = 0;

                if(_UseAdditionalLights > 0.5)
                {
                    // Additional Light Calculations
                    InputData inputData = (InputData)0;
                    inputData.positionWS = IN.wPos;
                    inputData.normalWS = N;
                    inputData.viewDirectionWS = V;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionHCS);

                    #if defined(_ADDITIONAL_LIGHTS)

                        uint pixelLightCount = GetAdditionalLightsCount();

                        LIGHT_LOOP_BEGIN(pixelLightCount)
                            Light light = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));

                            half3 L = normalize(light.direction);
                            half3 R = reflect(-L, N);
                            // Texture to normal map convertion 
                            float3 normalTS = UnpackNormal(tex2D(_NormalTex,IN.uv)); 

                            // Normal and Tangent
                            normalTS.xy *= _NormalStrength;  // normal intensity
                            normalTS = normalize(normalTS);
               
                            float3x3 TBN = float3x3(
                            normalize(IN.tangentWS),
                            normalize(IN.bitangentWS),
                            normalize(IN.normalWS)
                            );

                            half3 H = normalize(L + V); // Half Vector
                            float3 Nt = normalize(mul(normalTS,TBN));
                            

                            float att = light.distanceAttenuation * light.shadowAttenuation;

                            // Toon diffuse (Additional)
                            half diff = saturate(dot(N, L)) * 0.5 + 0.5;
                            diff = smoothstep(_DiffuseThreshold,_DiffuseThreshold + _DiffuseSmoothness,diff);

                            additionalDiffuse += diff * att * light.color * shadedAlbedo;

                            // Toon specular (Additional)
                            half spec = (saturate(dot(H,Nt)));
                            half specExp = exp2(_Gloss * 64);
                            spec = pow(spec,specExp);
                            half specStep = smoothstep(_SpecularThreshold,_SpecularThreshold + _SpecularSmoothness,spec);
                            additionalSpecular += specStep * att * light.color;

                        LIGHT_LOOP_END
                    #endif
                }
                // Baked lighting
                half3 bakedGI = SAMPLE_GI(IN.lightmapUV, SampleSH(N), N);

                // Final Output (CelShading)         
                half3 mixedColor = (bakedGI * shadedAlbedo) + (finalDiffuse + additionalDiffuse) + (finalSpecular + additionalSpecular) + finalAmbient + finalEmission;
                half4 finalOutput = half4(mixedColor,1);
                finalOutput.rgb = MixFog(finalOutput.rgb, IN.fogCoord);
                return finalOutput;   
            }
            ENDHLSL
        }


        Pass 
        {
            Name "ShadowCaster"
            
            Tags{ "LightMode" = "ShadowCaster"}

            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            struct MeshData
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Interpolators
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            float4 GetShadowPositionHClip(MeshData IN)
            {   
                float3 lightDirectionWS = _MainLightPosition.xyz;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(posWS,normalWS,lightDirectionWS));
                positionCS = ApplyShadowClamping(positionCS);

                return positionCS;
            };

            Interpolators vert (MeshData IN)
            {
                Interpolators OUT;
                UNITY_SETUP_INSTANCE_ID(IN);

                OUT.positionHCS = GetShadowPositionHClip(IN);

                return OUT;
            }

            half4 frag (Interpolators IN) : SV_Target
            {  
                UNITY_SETUP_INSTANCE_ID(IN);
         
                return 0;
            }

            ENDHLSL
        }
      
        //UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
