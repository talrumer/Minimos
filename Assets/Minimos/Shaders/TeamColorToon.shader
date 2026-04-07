Shader "Minimos/TeamColorToon"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _TeamColor ("Team Color", Color) = (1, 0.42, 0.42, 1)
        _AccentColor ("Accent Color (Outline/Shadow)", Color) = (0.8, 0.2, 0.2, 1)
        _PatternTex ("Pattern Overlay", 2D) = "black" {}
        _PatternStrength ("Pattern Strength", Range(0, 1)) = 0
        _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionStrength ("Emission Strength", Range(0, 2)) = 0
        _ShadowSteps ("Shadow Steps", Range(1, 5)) = 2
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.3
        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.02
        _OutlineColor ("Outline Color", Color) = (0.1, 0.1, 0.1, 1)
        _HitFlash ("Hit Flash", Range(0, 1)) = 0
        _InvulnerableBlink ("Invulnerable Blink", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // ===== OUTLINE PASS =====
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _OutlineWidth;
                float4 _OutlineColor;
                float4 _AccentColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 posOS = input.positionOS.xyz + input.normalOS * _OutlineWidth;
                output.positionCS = TransformObjectToHClip(posOS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ===== MAIN TOON PASS =====
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_PatternTex);
            SAMPLER(sampler_PatternTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TeamColor;
                float4 _AccentColor;
                float4 _PatternTex_ST;
                float _PatternStrength;
                float4 _EmissionColor;
                float _EmissionStrength;
                float _ShadowSteps;
                float _ShadowStrength;
                float _HitFlash;
                float _InvulnerableBlink;
                float _OutlineWidth;
                float4 _OutlineColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Sample base texture
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Apply team color tint
                float3 color = baseTex.rgb * _TeamColor.rgb;

                // Pattern overlay
                float4 pattern = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, input.uv * _PatternTex_ST.xy + _PatternTex_ST.zw);
                color = lerp(color, pattern.rgb * _TeamColor.rgb, pattern.a * _PatternStrength);

                // Toon shading - stepped lighting
                Light mainLight = GetMainLight();
                float NdotL = dot(normalize(input.normalWS), mainLight.direction);
                float lightStep = floor(NdotL * _ShadowSteps) / _ShadowSteps;
                lightStep = max(lightStep, 1.0 - _ShadowStrength);

                color *= lightStep * mainLight.color;

                // Add ambient
                float3 ambient = SampleSH(normalize(input.normalWS));
                color += ambient * _TeamColor.rgb * 0.3;

                // Emission (for power-ups, charging, etc.)
                color += _EmissionColor.rgb * _EmissionStrength;

                // Hit flash (white overlay)
                color = lerp(color, float3(1, 1, 1), _HitFlash);

                // Invulnerable blink (alpha pulse via time)
                float blink = sin(_Time.y * 15.0) * 0.5 + 0.5;
                float alpha = lerp(1.0, blink * 0.5 + 0.5, _InvulnerableBlink);

                return float4(color, alpha);
            }
            ENDHLSL
        }

        // Shadow caster pass for receiving shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _TeamColor;
                float4 _AccentColor;
                float4 _MainTex_ST;
                float4 _PatternTex_ST;
                float _PatternStrength;
                float4 _EmissionColor;
                float _EmissionStrength;
                float _ShadowSteps;
                float _ShadowStrength;
                float _HitFlash;
                float _InvulnerableBlink;
                float _OutlineWidth;
                float4 _OutlineColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _MainLightPosition.xyz));
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
