Shader "Custom/URP_TronGridSkybox"
{
    Properties
    {
        _ZenithColor ("Zenith Sky Color", Color) = (0.006, 0.012, 0.032, 1)
        _AbyssColor ("Nadir Abyss Color", Color) = (0.004, 0.008, 0.022, 1)
        [HDR] _HorizonColor ("Horizon Glow Color", Color) = (0.0, 0.65, 1.35, 1)
        [HDR] _GridColor ("Cyber Grid Color", Color) = (0.0, 0.75, 1.4, 1)
        [HDR] _AccentColor ("City Accent Color (Orange)", Color) = (1.6, 0.45, 0.05, 1)
        _GridScale ("Perspective Grid Scale", Float) = 3.5
        _CityHeight ("Horizon City Monolith Height", Range(0.02, 0.35)) = 0.16
        _BeamIntensity ("Portal Light Beam Intensity", Range(0.0, 3.0)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "TronGridSkybox"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 texcoord   : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _AbyssColor;
                half4 _HorizonColor;
                half4 _GridColor;
                half4 _AccentColor;
                float _GridScale;
                float _CityHeight;
                float _BeamIntensity;
            CBUFFER_END

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.texcoord = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.texcoord);
                float absY = abs(dir.y);

                // 1. Base Sky Gradient & Horizon Laser
                float upMask = saturate(dir.y);
                float downMask = saturate(-dir.y);
                half3 sky = lerp(_HorizonColor.rgb * 0.35h, _ZenithColor.rgb, (half)pow(upMask, 0.42));
                sky = lerp(sky, _AbyssColor.rgb, (half)pow(downMask, 0.38));

                float horizonBand = exp(-absY * 14.0);
                float horizonLaser = exp(-absY * 120.0);
                sky += _HorizonColor.rgb * (half)(horizonBand * 0.45 + horizonLaser * 1.1);

                // Azimuth around the horizon (0..1, where +Z forward is 0.75)
                float az = atan2(dir.z, dir.x) / (2.0 * 3.14159265) + 0.5;

                // 2. Distant I/O Portal Vertical Light Beams (dir.y > 0)
                if (dir.y > 0.0)
                {
                    float beamAngles[4] = { 0.75, 0.62, 0.88, 0.25 };
                    float beamWidths[4] = { 0.0045, 0.0028, 0.0028, 0.0038 };
                    float beamHeights[4] = { 1.0, 0.65, 0.65, 0.85 };

                    [unroll]
                    for (int i = 0; i < 4; i++)
                    {
                        float dAz = abs(az - beamAngles[i]);
                        dAz = min(dAz, 1.0 - dAz);
                        float core = exp(-dAz / beamWidths[i]);
                        float halo = exp(-dAz / (beamWidths[i] * 5.0)) * 0.32;
                        float vertFade = saturate(1.0 - dir.y / beamHeights[i]);
                        float scan = 0.78 + 0.22 * sin(dir.y * 85.0 - _Time.y * 12.0 + i * 1.7);
                        half3 beamTint = (i == 1) ? lerp(_GridColor.rgb, _AccentColor.rgb, 0.4h) : _GridColor.rgb;
                        sky += beamTint * (half)((core * 0.9 + halo) * vertFade * scan * _BeamIntensity);
                    }
                }

                // 3. Anti-Aliased Infinite Perspective Grid (Overhead Cyber Dome & Lower Grid)
                float2 projUV = dir.xz / max(0.05, absY) * _GridScale;
                float2 deriv = max(fwidth(projUV), float2(0.0001, 0.0001));
                float2 gridDist = abs(frac(projUV - 0.5) - 0.5) / deriv;
                float gridLine = 1.0 - min(min(gridDist.x, gridDist.y), 1.0);
                float gridFade = smoothstep(0.06, 0.24, absY) * exp(-length(projUV) * 0.022);
                float skyGridWeight = dir.y > 0.0 ? 0.42 : 0.72;
                sky += _GridColor.rgb * (half)(gridLine * gridFade * skyGridWeight);

                // Overhead Zenith Dome Concentric Circuit Rings
                if (dir.y > 0.52)
                {
                    float r1 = exp(-abs(dir.y - 0.66) * 240.0);
                    float r2 = exp(-abs(dir.y - 0.80) * 280.0);
                    float r3 = exp(-abs(dir.y - 0.92) * 320.0);
                    float seg = step(0.1, sin(az * 2.0 * 3.14159265 * 18.0));
                    sky += _GridColor.rgb * (half)((r1 * 0.55 + r2 * seg * 0.65 + r3 * 0.75) * 0.85);
                }

                return half4(sky, 1.0h);
            }
            ENDHLSL
        }
    }
}
