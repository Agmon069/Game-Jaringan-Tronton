Shader "Custom/URP_TronExplosionVFX"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (0, 0.95, 1, 1)
        _BodyColor ("Voxel Body Color", Color) = (0.04, 0.05, 0.09, 1)
        _EmissionStrength ("Emission Strength", Range(0.5, 15.0)) = 3.5
        _EdgeWidth ("Voxel Wireframe Width", Range(0.02, 0.35)) = 0.12
        _Mode ("Procedural Mode (0=Glow, 1=ShockwaveRing, 2=VoxelCube, 3=Beam)", Float) = 0
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

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "TronExplosionVFXForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _BodyColor;
                half  _EmissionStrength;
                half  _EdgeWidth;
                half  _Mode;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 neon = _BaseColor * input.color;
                half3 finalRgb = half3(0, 0, 0);
                half finalAlpha = 0.0h;

                if (_Mode < 0.5h)
                {
                    // Mode 0: Soft Plasma / Spark / Trail
                    float2 c = input.uv * 2.0 - 1.0;
                    float r = length(c);
                    float mask = pow(saturate(1.0 - r), 1.8);
                    float core = pow(saturate(1.0 - r), 6.0);

                    finalRgb = (neon.rgb * mask + half3(1.0h, 1.0h, 1.0h) * (half)core * 0.9h) * _EmissionStrength;
                    finalAlpha = (half)mask * neon.a;
                }
                else if (_Mode < 1.5h)
                {
                    // Mode 1: Procedural Tron Shockwave Ring & Grid Decal
                    float2 c = input.uv * 2.0 - 1.0;
                    float r = length(c);
                    float angle = atan2(c.y, c.x);

                    float outerRing = smoothstep(0.82, 0.92, r) * (1.0 - smoothstep(0.94, 1.0, r));
                    float midRing = smoothstep(0.56, 0.62, r) * (1.0 - smoothstep(0.64, 0.70, r));
                    float segMask = step(0.15, sin(angle * 24.0));
                    float segmentedRing = midRing * segMask * 0.85;
                    float innerFalloff = pow(saturate(1.0 - r), 2.5) * (1.0 - smoothstep(0.85, 1.0, r)) * 0.25;

                    float combined = saturate(outerRing + segmentedRing + innerFalloff);
                    finalRgb = (neon.rgb * (half)combined + half3(1.0h, 1.0h, 1.0h) * (half)pow(outerRing, 2.0) * 0.8h) * _EmissionStrength;
                    finalAlpha = (half)combined * neon.a;
                }
                else if (_Mode < 2.5h)
                {
                    // Mode 2: 3D Derez Voxel Cube (Wireframe Edges + Dark Core)
                    float2 edge = min(input.uv, 1.0 - input.uv);
                    float minEdge = min(edge.x, edge.y);
                    float wire = 1.0 - smoothstep(0.0, _EdgeWidth, minEdge);

                    half3 interior = _BodyColor.rgb + neon.rgb * 0.25h;
                    half3 edgeGlow = neon.rgb * _EmissionStrength * (1.2h + (half)wire * 2.8h);
                    finalRgb = lerp(interior, edgeGlow, (half)wire);
                    finalAlpha = saturate(0.35h + (half)wire * 0.65h) * neon.a;
                }
                else
                {
                    // Mode 3: Vertical Digital Derez Beam
                    float horiz = pow(saturate(1.0 - abs(input.uv.x * 2.0 - 1.0)), 2.2);
                    float vertFade = saturate(1.0 - input.uv.y);
                    float scanline = 0.75 + 0.25 * sin(input.uv.y * 60.0 - _Time.y * 25.0);
                    float beam = horiz * vertFade * scanline;

                    finalRgb = (neon.rgb + half3(1.0h, 1.0h, 1.0h) * (half)pow(horiz, 4.0) * 0.65h) * _EmissionStrength * (half)beam;
                    finalAlpha = (half)beam * neon.a;
                }

                return half4(finalRgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
