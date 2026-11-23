Shader "Canal420/PlanarMirror"
{
    Properties
    {
        _ReflectionTex ("Reflection RenderTexture", 2D) = "black" {}
        _GlassTint ("Aged Mirror Tint", Color) = (0.86, 0.89, 0.88, 1)
        _EdgeGrime ("Edge Aging Grime", Range(0, 1)) = 0.24
        _ReflectionStrength ("Reflection Strength", Range(0, 1.2)) = 0.92
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "PlanarMirrorUnlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
            };

            TEXTURE2D(_ReflectionTex);
            SAMPLER(sampler_ReflectionTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _GlassTint;
                half _EdgeGrime;
                half _ReflectionStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.uv = IN.uv;
                OUT.screenPos = posInputs.positionNDC;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 0.0001);
                // Virtual reflection camera uses a positive-determinant mirrored basis (mirrored F/U, flipped R),
                // so flipping screenUV.x reproduces the exact planar mirror image without GL.invertCulling.
                float2 reflUV = float2(1.0 - screenUV.x, screenUV.y);

                half3 refl = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, reflUV).rgb;
                refl = clamp(refl, half3(0.0, 0.0, 0.0), half3(16.0, 16.0, 16.0));

                // Subtle 1990s aged mirror darkening near frame edges + faint silvered dust
                float2 centered = (IN.uv - 0.5) * 2.0;
                float edgeFactor = saturate(max(abs(centered.x), abs(centered.y)));
                float borderShade = 1.0 - smoothstep(0.68, 1.0, edgeFactor) * _EdgeGrime;

                // Subtle low-contrast grime speckle along bottom rim
                float grimeNoise = frac(sin(dot(floor(IN.uv * 96.0), float2(12.9898, 78.233))) * 43758.5453);
                float bottomFog = smoothstep(0.32, 0.0, IN.uv.y) * (0.04 + 0.03 * grimeNoise);

                half3 col = refl * _GlassTint.rgb * _ReflectionStrength * borderShade;
                col = lerp(col, half3(0.08, 0.09, 0.095), bottomFog);

                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            DepthVaryings DepthVert(DepthAttributes IN)
            {
                DepthVaryings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 DepthFrag(DepthVaryings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthNormVert
            #pragma fragment DepthNormFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DepthNormAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct DepthNormVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
            };

            DepthNormVaryings DepthNormVert(DepthNormAttributes IN)
            {
                DepthNormVaryings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(IN.normalOS));
                return OUT;
            }

            half4 DepthNormFrag(DepthNormVaryings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                return half4(PackNormalOctRectEncode(TransformWorldToViewDir(n, true)), 0.0, 0.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
