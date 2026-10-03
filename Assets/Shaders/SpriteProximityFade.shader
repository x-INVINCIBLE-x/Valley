Shader "Custom/SpriteProximityFade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        _FadeStart ("Fade Start Distance", Float) = 10
        _FadeEnd ("Fade End Distance", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                float3 positionWS   : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

                float4 _Color;
                float _FadeStart;
                float _FadeEnd;

            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs vertexInput =
                    GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionHCS = vertexInput.positionCS;
                OUT.positionWS = vertexInput.positionWS;
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    IN.uv
                );

                // Camera position
                float3 cameraPos = GetCameraPositionWS();

                // Distance from camera to sprite pixel
                float distanceToCamera =
                    distance(cameraPos, IN.positionWS);

                // Fade:
                // Farther than FadeStart -> 0
                // Closer than FadeEnd   -> 1
                float fade = 1.0 -
                    smoothstep(
                        _FadeEnd,
                        _FadeStart,
                        distanceToCamera
                    );

                sprite *= IN.color;

                sprite.a *= fade;

                // Prevent transparent pixels from being written
                if (sprite.a <= 0.001)
                    discard;

                return sprite;
            }

            ENDHLSL
        }
    }
}