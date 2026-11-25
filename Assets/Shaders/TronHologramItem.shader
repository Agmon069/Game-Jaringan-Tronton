Shader "Custom/URP_TronHologramItem"
{
    Properties
    {
        [MainTexture] _BaseMap ("Optional Icon / Detail Map", 2D) = "white" {}
        [MainColor] _Color ("Hologram Neon Color", Color) = (0.0, 0.95, 1.0, 1.0)
        [HDR] _RimColor ("Fresnel Rim Accent", Color) = (0.45, 1.8, 2.5, 1.0)

        [Header(Glow and Transparency)]
        _EmissionIntensity ("HDR Glow Intensity", Range(0.5, 10.0)) = 2.6
        _BaseAlpha ("Core Fill Opacity", Range(0.0, 0.6)) = 0.14
        _BackfaceOpacity ("Backface Projection Opacity", Range(0.0, 0.6)) = 0.25

        [Header(Fresnel Silhouette)]
        _FresnelPower ("Fresnel Sharpness", Range(0.5, 8.0)) = 2.6
        _FresnelIntensity ("Fresnel Strength", Range(0.0, 4.0)) = 1.9

        [Header(Tron Wireframe and Grid)]
        _WireframeStrength ("Edge Wireframe Strength", Range(0.0, 3.0)) = 1.65
        _WireframeWidth ("Edge Wireframe Width", Range(0.01, 0.20)) = 0.045
        _GridScale ("Micro Grid Density", Range(2.0, 32.0)) = 10.0
        _GridStrength ("Micro Grid Opacity", Range(0.0, 1.5)) = 0.35

        [Header(Scanlines and Energy Sweep)]
        _ScanlineDensity ("Scanline Density", Range(5.0, 120.0)) = 42.0
        _ScanlineSpeed ("Scanline Scroll Speed", Range(-20.0, 20.0)) = 4.5
        _ScanlineContrast ("Scanline Contrast", Range(0.0, 1.0)) = 0.55
        _SweepSpeed ("Energy Sweep Speed", Range(0.0, 5.0)) = 0.65
        _SweepIntensity ("Energy Sweep Brightness", Range(0.0, 3.0)) = 1.25

        [Header(Hologram Glitch and Flicker)]
        _GlitchIntensity ("Glitch Offset Strength", Range(0.0, 0.15)) = 0.025
        _GlitchFrequency ("Glitch Frequency", Range(0.5, 10.0)) = 2.5
        _FlickerStrength ("Projector Flicker", Range(0.0, 0.4)) = 0.08

        [Header(Power Up Animation)]
        _RotateSpeed ("Spin Speed (Rad/s)", Range(-10.0, 10.0)) = 1.6
        _TiltAngle ("Power-Up Tilt Angle (Deg)", Range(0.0, 45.0)) = 24.0
        _BobSpeed ("Hover Bob Speed", Range(0.0, 10.0)) = 2.8
        _BobAmplitude ("Hover Bob Height", Range(0.0, 0.6)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _Color;
            half4  _RimColor;
            half   _EmissionIntensity;
            half   _BaseAlpha;
            half   _BackfaceOpacity;
            half   _FresnelPower;
            half   _FresnelIntensity;
            half   _WireframeStrength;
            half   _WireframeWidth;
            float  _GridScale;
            half   _GridStrength;
            float  _ScanlineDensity;
            float  _ScanlineSpeed;
            half   _ScanlineContrast;
            float  _SweepSpeed;
            half   _SweepIntensity;
            float  _GlitchIntensity;
            float  _GlitchFrequency;
            half   _FlickerStrength;
            float  _RotateSpeed;
            float  _TiltAngle;
            float  _BobSpeed;
            float  _BobAmplitude;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
            float3 positionOS : TEXCOORD2;
            float3 normalWS   : TEXCOORD3;
            float  glitchMask : TEXCOORD4;
        };

        float Hash11(float p)
        {
            p = frac(p * 0.1031);
            p *= p + 33.33;
            p *= p + p;
            return frac(p);
        }

        float3 RotateX(float3 v, float a)
        {
            float s, c;
            sincos(a, s, c);
            return float3(v.x, v.y * c - v.z * s, v.y * s + v.z * c);
        }

        float3 RotateY(float3 v, float a)
        {
            float s, c;
            sincos(a, s, c);
            return float3(v.x * c + v.z * s, v.y, -v.x * s + v.z * c);
        }

        float3 RotateZ(float3 v, float a)
        {
            float s, c;
            sincos(a, s, c);
            return float3(v.x * c - v.y * s, v.x * s + v.y * c, v.z);
        }

        Varyings VertHologram(Attributes input)
        {
            Varyings output;

            float3 posOS = input.positionOS.xyz;
            float3 normOS = input.normalOS;

            // Power-Up diagonal tilt + continuous Y-axis spin (rotate both position & normal)
            float tiltRad = radians(_TiltAngle);
            float spinRad = _Time.y * _RotateSpeed;

            posOS = RotateX(posOS, tiltRad);
            posOS = RotateZ(posOS, tiltRad * 0.65);
            posOS = RotateY(posOS, spinRad);

            normOS = RotateX(normOS, tiltRad);
            normOS = RotateZ(normOS, tiltRad * 0.65);
            normOS = RotateY(normOS, spinRad);

            // Save vertical position before hover bob for clean energy sweep
            float sweepLocalY = posOS.y;

            // Smooth vertical hover bobbing
            posOS.y += sin(_Time.y * _BobSpeed) * _BobAmplitude;

            // Occasional brief horizontal digital glitch slice
            float timeSlot = floor(_Time.y * _GlitchFrequency);
            float glitchTrigger = step(0.84, Hash11(timeSlot * 17.31));
            float sliceBand = floor(sweepLocalY * 14.0 + _Time.y * 18.0);
            float sliceRand = Hash11(sliceBand + timeSlot * 3.7);
            float activeSlice = step(0.62, sliceRand) * glitchTrigger;
            float offsetSign = (Hash11(sliceBand * 5.1 + timeSlot) * 2.0 - 1.0);
            posOS.x += activeSlice * offsetSign * _GlitchIntensity;

            VertexPositionInputs vertexInput = GetVertexPositionInputs(posOS);
            VertexNormalInputs normalInput = GetVertexNormalInputs(normOS);

            output.positionCS = vertexInput.positionCS;
            output.positionWS = vertexInput.positionWS;
            output.positionOS = float3(posOS.x, sweepLocalY, posOS.z);
            output.normalWS = normalInput.normalWS;
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            output.glitchMask = activeSlice;
            return output;
        }

        half4 EvalHologram(Varyings input, bool isBackface)
        {
            half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

            float3 normalWS = normalize(input.normalWS);
            if (isBackface)
            {
                normalWS = -normalWS;
            }
            float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

            // 1. Fresnel rim glow
            half ndotv = saturate(dot(normalWS, viewDirWS));
            half fresnel = pow(1.0h - ndotv, _FresnelPower) * _FresnelIntensity;

            // 2. Crisp UV face edge wireframe (iconic Tron Data Cube border)
            float2 edgeDist = min(input.uv, 1.0 - input.uv);
            float minEdge = min(edgeDist.x, edgeDist.y);
            half wireframe = (1.0h - (half)smoothstep(0.0, _WireframeWidth, minEdge)) * _WireframeStrength;

            // 3. Subtle anti-aliased interior micro circuit grid
            float2 gridUV = input.uv * _GridScale;
            float2 gridDeriv = max(fwidth(gridUV), float2(0.0001, 0.0001));
            float2 gridCell = abs(frac(gridUV - 0.5) - 0.5) / gridDeriv;
            half microGrid = (1.0h - (half)min(min(gridCell.x, gridCell.y), 1.0)) * _GridStrength;

            // 4. Scrolling horizontal hologram scanlines (world-space Y for uniform projection)
            float scanWave = sin(input.positionWS.y * _ScanlineDensity - _Time.y * _ScanlineSpeed) * 0.5 + 0.5;
            half scanlines = lerp(1.0h, (half)pow(scanWave, 1.4), _ScanlineContrast);

            // 5. Upward sweeping bright energy band
            float sweepPhase = frac(input.positionOS.y * 0.85 - _Time.y * _SweepSpeed);
            half sweepBand = (half)exp(-pow(sweepPhase - 0.5, 2.0) * 55.0) * _SweepIntensity;

            // 6. Subtle projector flicker
            float flickNoise = sin(_Time.y * 47.0) * sin(_Time.y * 19.3) * 0.5 + 0.5;
            half flicker = 1.0h - _FlickerStrength * (half)flickNoise;

            // Combine hologram energy
            half coreEnergy = (_BaseAlpha + microGrid * 0.65h) * scanlines;
            half edgeEnergy = fresnel + wireframe + sweepBand * (0.45h + fresnel * 0.5h + wireframe * 0.4h);
            half glitchBoost = (half)input.glitchMask * 0.9h;

            // Tint cleanly with _Color (driven by material.color in PowerUpItem / AmmoOrb)
            half3 baseTint = _Color.rgb * tex.rgb;
            half3 hotRim = lerp(baseTint * 1.45h, baseTint * _RimColor.rgb + half3(0.35h, 0.35h, 0.35h), 0.45h);

            half3 col = baseTint * (coreEnergy + sweepBand * 0.75h + glitchBoost)
                      + hotRim * (fresnel * 0.85h + wireframe * 0.95h + sweepBand * 0.45h);

            col *= _EmissionIntensity * flicker;

            half alpha = saturate((coreEnergy + edgeEnergy * 0.72h + glitchBoost * 0.5h) * tex.a * _Color.a * flicker);

            if (isBackface)
            {
                col *= _BackfaceOpacity;
                alpha *= _BackfaceOpacity;
            }

            return half4(col, alpha);
        }
        ENDHLSL

        // Pass 1: Backface internal volumetric hologram projection
        Pass
        {
            Name "HologramBack"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex VertHologram
            #pragma fragment FragBack

            half4 FragBack(Varyings input) : SV_Target
            {
                return EvalHologram(input, true);
            }
            ENDHLSL
        }

        // Pass 2: Frontface main Tron hologram pass
        Pass
        {
            Name "HologramFront"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite Off
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex VertHologram
            #pragma fragment FragFront

            half4 FragFront(Varyings input) : SV_Target
            {
                return EvalHologram(input, false);
            }
            ENDHLSL
        }
    }
}
