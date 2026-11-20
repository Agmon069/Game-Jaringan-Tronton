Shader "Custom/URP_TronCarAutoNeon"
{
    Properties
    {
        [MainTexture] _BaseMap("Car Texture (Black = Body, Colored = Neon)", 2D) = "black" {}
        
        [Header(Car Body Settings)]
        _BodyColor("Dark Body Tint", Color) = (0.04, 0.04, 0.06, 1)
        _Metallic("Body Metallic", Range(0.0, 1.0)) = 0.85
        _Smoothness("Body Smoothness", Range(0.0, 1.0)) = 0.92

        [Header(Auto Neon Detection)]
        _BlackThreshold("Black Cutoff Threshold", Range(0.001, 0.5)) = 0.05
        _NeonIntensity("Neon Glow Intensity (HDR)", Range(0.0, 15.0)) = 4.0
        _PulseSpeed("Neon Pulse Speed", Range(0.0, 10.0)) = 2.0
        _PulseMin("Pulse Min Brightness", Range(0.0, 1.0)) = 0.6

        [Header(Fresnel Edge Glow)]
        [HDR] _FresnelColor("Fresnel Rim Color", Color) = (0.0, 1.5, 2.5, 1)
        _FresnelPower("Fresnel Sharpness", Range(0.5, 8.0)) = 3.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Keyword pencahayaan standar URP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BodyColor;
                half   _Metallic;
                half   _Smoothness;
                half   _BlackThreshold;
                half   _NeonIntensity;
                half   _PulseSpeed;
                half   _PulseMin;
                half4  _FresnelColor;
                half   _FresnelPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Ambil warna dari tekstur mobil
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                // 2. Deteksi apakah piksel berwarna selain hitam
                // Mengambil nilai kanal warna tertinggi (R, G, atau B)
                half maxChannel = max(texColor.r, max(texColor.g, texColor.b));
                
                // Mask: 0 jika hitam, 1 jika berwarna (menggunakan smoothstep agar pinggiran garis halus)
                half neonMask = smoothstep(_BlackThreshold, _BlackThreshold + 0.05, maxChannel);

                // 3. Siapkan Data PBR untuk Bodi Mobil
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(inputData.normalWS);

                SurfaceData surfaceData = (SurfaceData)0;
                // Jika neonMask = 1, albedo dibuat gelap agar warna murni datang dari Emission
                surfaceData.albedo = lerp(_BodyColor.rgb, half3(0, 0, 0), neonMask);
                surfaceData.metallic = lerp(_Metallic, 0.0h, neonMask);
                surfaceData.smoothness = lerp(_Smoothness, 0.2h, neonMask);
                surfaceData.occlusion = 1.0h;
                surfaceData.alpha = 1.0h;

                // 4. Hitung Efek Denyut (Pulse) & Cahaya Garis Tekstur
                half pulse = lerp(_PulseMin, 1.0h, (sin(_Time.y * _PulseSpeed) + 1.0h) * 0.5h);
                half3 textureNeon = texColor.rgb * neonMask * _NeonIntensity * pulse;

                // 5. Hitung Fresnel Glow (Cahaya tipis di lekukan sudut bodi)
                half fresnel = 1.0h - saturate(dot(inputData.viewDirectionWS, inputData.normalWS));
                fresnel = pow(fresnel, _FresnelPower);
                half3 fresnelGlow = _FresnelColor.rgb * fresnel * (1.0h - neonMask);

                // Gabungkan Emission
                surfaceData.emission = textureNeon + fresnelGlow;

                // 6. Kalkulasi Lighting PBR URP + Fog
                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
                finalColor.rgb = MixFog(finalColor.rgb, inputData.fogCoord);

                return finalColor;
            }
            ENDHLSL
        }

        // Pass tambahan agar mobil tetap menghasilkan bayangan (Shadow Caster)
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}