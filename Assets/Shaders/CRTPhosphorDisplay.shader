Shader "Canal420/CRTPhosphorDisplay"
{
    Properties
    {
        [MainTexture] _BaseMap("CRT Feed Texture", 2D) = "black" {}
        [MainColor] _BaseColor("Glass Tint", Color) = (0.035, 0.045, 0.055, 1)
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _Smoothness("Glass Smoothness", Range(0.0, 1.0)) = 0.88
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.12
        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2, 1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Cull("Cull", Float) = 2.0

        [Header(CRT Optical Pipeline)]
        _CRTActive("CRT Active", Float) = 0.0
        _CRTResolution("CRT Feed Resolution", Vector) = (240, 180, 0, 0)
        _CRTRgbShift("RGB Fringing Pixels", Float) = 1.0
        _CRTIsUhfGhost("UHF Multipath Ghost", Float) = 0.0
        _CRTIsVhsTape("VHS Tape Mode", Float) = 0.0
        _CRTHumBarCenter("Hum Bar Center", Float) = 0.5
        _CRTVhsTrackingBand("VHS Tracking Band", Float) = 0.5
        _CRTGrainAmount("Analog Grain Amount", Float) = 0.048
        _CRTSwitchStrength("Channel Switch Strength", Float) = 0.0
        _CRTTime("CRT Clock", Float) = 0.0
        _CRTBezelCurvature("Bezel Curvature", Range(0.0, 0.15)) = 0.032
        _CRTScanlineStrength("Scanline Strength", Range(0.0, 0.6)) = 0.22
        _CRTApertureStrength("Aperture Grille Strength", Range(0.0, 0.3)) = 0.06
        _CRTGlareStrength("Glass Glare Strength", Range(0.0, 0.2)) = 0.06
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        HLSLINCLUDE
            #define DECLARE_CRT_DISPLAY_CBUFFER \
            CBUFFER_START(UnityPerMaterial) \
                float4 _BaseMap_ST; \
                float4 _CRTResolution; \
                half4 _BaseColor; \
                half4 _SpecColor; \
                half4 _EmissionColor; \
                half _Smoothness; \
                half _Metallic; \
                half _Cutoff; \
                half _CRTActive; \
                half _CRTRgbShift; \
                half _CRTIsUhfGhost; \
                half _CRTIsVhsTape; \
                half _CRTHumBarCenter; \
                half _CRTVhsTrackingBand; \
                half _CRTGrainAmount; \
                half _CRTSwitchStrength; \
                float _CRTTime; \
                half _CRTBezelCurvature; \
                half _CRTScanlineStrength; \
                half _CRTApertureStrength; \
                half _CRTGlareStrength; \
            CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend One Zero
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex CRTPassVertex
            #pragma fragment CRTPassFragment

            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            DECLARE_CRT_DISPLAY_CBUFFER

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS         : POSITION;
                float3 normalOS           : NORMAL;
                float2 texcoord           : TEXCOORD0;
                float2 staticLightmapUV   : TEXCOORD1;
                float2 dynamicLightmapUV  : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv                       : TEXCOORD0;
                float3 positionWS               : TEXCOORD1;
                half3  normalWS                 : TEXCOORD2;
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half4  fogFactorAndVertexLight  : TEXCOORD3;
            #else
                half   fogFactor                : TEXCOORD3;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord              : TEXCOORD4;
            #endif
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
            #ifdef DYNAMICLIGHTMAP_ON
                float2 dynamicLightmapUV        : TEXCOORD6;
            #endif
            #ifdef USE_APV_PROBE_OCCLUSION
                float4 probeOcclusion           : TEXCOORD7;
            #endif
                float4 positionCS               : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float CRTHash(float2 coord, float seed)
            {
                float3 p3 = frac(float3(coord.xyx) * 0.1031 + seed * 0.1373);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            void InitializeCRTInputData(Varyings input, out InputData inputData)
            {
                inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                #if defined(DEBUG_DISPLAY)
                inputData.positionCS = input.positionCS;
                #endif
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    inputData.shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
                    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                #else
                    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
                #endif

                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                #if defined(DEBUG_DISPLAY)
                    #if defined(DYNAMICLIGHTMAP_ON)
                    inputData.dynamicLightmapUV = input.dynamicLightmapUV;
                    #endif
                    #if defined(LIGHTMAP_ON)
                    inputData.staticLightmapUV = input.staticLightmapUV;
                    #else
                    inputData.vertexSH = input.vertexSH;
                    #endif
                    #if defined(USE_APV_PROBE_OCCLUSION)
                    inputData.probeOcclusion = input.probeOcclusion;
                    #endif
                #endif
            }

            void InitializeCRTBakedGI(Varyings input, inout InputData inputData)
            {
                #if defined(_SCREEN_SPACE_IRRADIANCE)
                inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy);
                #elif defined(DYNAMICLIGHTMAP_ON)
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
                #elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
                inputData.bakedGI = SAMPLE_GI(input.vertexSH,
                    GetAbsolutePositionWS(inputData.positionWS),
                    inputData.normalWS,
                    inputData.viewDirectionWS,
                    input.positionCS.xy,
                    input.probeOcclusion,
                    inputData.shadowMask);
                #else
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
                #endif
            }

            Varyings CRTPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
                half fogFactor = 0;
                #if !defined(_FOG_FRAGMENT)
                    fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                #endif

                output.uv = input.texcoord * _BaseMap_ST.xy + _BaseMap_ST.zw;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                #ifdef DYNAMICLIGHTMAP_ON
                    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
                #else
                    output.fogFactor = fogFactor;
                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                output.positionCS = vertexInput.positionCS;
                return output;
            }

            void CRTPassFragment(
                Varyings input
                , out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #ifdef LOD_FADE_CROSSFADE
                    LODFadeCrossFade(input.positionCS);
                #endif

                float2 rawUV = input.uv;
                float2 centered = rawUV * 2.0 - 1.0;
                float r2 = dot(centered, centered);

                float resX = max(32.0, _CRTResolution.x);
                float resY = max(32.0, _CRTResolution.y);
                float2 uvDeriv = fwidth(rawUV * float2(resX, resY));

                // Barrel bezel glass warp (kept gentle so corner broadcast OSD stays crisp & inside frame)
                float2 warpedUV = centered * (1.0 + (float)_CRTBezelCurvature * r2 * 0.45) * 0.5 + 0.5;
                half insideScreen = step(0.0, warpedUV.x) * step(warpedUV.x, 1.0) * step(0.0, warpedUV.y) * step(warpedUV.y, 1.0);
                float2 clampedUV = saturate(warpedUV);

                half dx = (half)centered.x;
                half dy = (half)centered.y;
                half cornerPower = dx * dx * dx * dx * 0.10h + dy * dy * dy * dy * 0.12h;
                half tubeVig = saturate(1.04h - (dx * dx * 0.18h + dy * dy * 0.22h) - cornerPower * 1.8h) * insideScreen;

                half gx = (half)clampedUV.x - 0.16h;
                half gy = (half)clampedUV.y - 0.84h;
                half glassHighlight = exp(-(gx * gx * 14.0h + gy * gy * 22.0h)) * _CRTGlareStrength * insideScreen;

                half emissionLum = dot(_EmissionColor.rgb, half3(0.333h, 0.333h, 0.333h));
                bool isTvOn = (_CRTActive >= 0.5h) && (emissionLum > 0.0001h);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = _SpecColor.rgb;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
                surfaceData.occlusion = 1.0h;
                surfaceData.alpha = 1.0h;

                if (!isTvOn)
                {
                    surfaceData.albedo = _BaseColor.rgb * (0.45h + 0.55h * tubeVig);
                    surfaceData.emission = _EmissionColor.rgb * tubeVig;
                }
                else
                {
                    float texelX = 1.0 / resX;
                    float shiftU = (float)_CRTRgbShift * texelX;
                    float smearU = 2.0 * texelX;

                    // Pass 3 Optical Pipeline on GPU: RGB convergence fringing + NTSC YIQ chroma smear
                    half3 centerCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, clampedUV).rgb;
                    half rLeft = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, saturate(clampedUV + float2(-shiftU, 0.0))).r;
                    half bRight = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, saturate(clampedUV + float2(shiftU, 0.0))).b;
                    half3 smearCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, saturate(clampedUV + float2(-smearU, 0.0))).rgb;

                    half3 crtCol;
                    crtCol.r = rLeft * 0.78h + smearCol.r * 0.22h;
                    crtCol.g = centerCol.g;
                    crtCol.b = bRight * 0.78h + smearCol.b * 0.22h;

                    // UHF multipath ghost echo on channels 09 & 17
                    if (_CRTIsUhfGhost > 0.5h)
                    {
                        half3 ghostCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, saturate(clampedUV + float2(-6.0 * texelX, 0.0))).rgb;
                        crtCol = crtCol * 0.86h + ghostCol * 0.18h;
                    }

                    // Distance-aware scanlines & RGB aperture-grille triad (float frac avoids fp16 phase quantization)
                    half scanAntiAlias = saturate(1.5h - (half)uvDeriv.y * 0.85h);
                    float scanPhase = frac(clampedUV.y * resY) * 6.2831853;
                    half scanWave = sin((half)scanPhase);
                    half scanAverage = 1.01h - _CRTScanlineStrength * 0.5h;
                    half scanMul = lerp(scanAverage, lerp(1.0h - _CRTScanlineStrength, 1.02h, scanWave * 0.5h + 0.5h), scanAntiAlias);

                    half triadAntiAlias = saturate(1.5h - (half)uvDeriv.x * 0.85h);
                    half triadPhase = (half)(frac(clampedUV.x * resX * 0.3333333) * 6.2831853);
                    half3 triadMask = half3(
                        1.0h + _CRTApertureStrength * triadAntiAlias * cos(triadPhase),
                        1.0h + _CRTApertureStrength * triadAntiAlias * cos(triadPhase - 2.0943951h),
                        1.0h + _CRTApertureStrength * triadAntiAlias * cos(triadPhase - 4.1887902h)
                    );
                    crtCol *= triadMask;

                    // Procedural GPU grain + VHS tracking snow band
                    float2 cell = floor(clampedUV * float2(resX, resY));
                    float noiseA = CRTHash(cell, floor(_CRTTime * 24.0) * 1.37);
                    if (_CRTIsVhsTape > 0.5h && abs((half)clampedUV.y - _CRTVhsTrackingBand) < 0.022h)
                    {
                        if (noiseA < 0.38)
                        {
                            half streak = 0.35h + (half)CRTHash(cell + float2(7.0, 3.0), floor(_CRTTime * 30.0)) * 0.55h;
                            crtCol = lerp(crtCol, streak.xxx, 0.65h);
                        }
                    }

                    // 60Hz hum bar + analog phosphor flicker (periodic frac keeps phase exact past large _CRTTime)
                    half humDist = abs((half)clampedUV.y - _CRTHumBarCenter);
                    half humBand = 1.0h + 0.09h * exp(-humDist * humDist * 65.0h);
                    float flickerPhase = frac(_CRTTime * (47.0 / 6.2831853)) * 6.2831853;
                    half flicker = 1.0h + 0.018h * sin((half)flickerPhase) + _CRTSwitchStrength * 0.08h * (half)(noiseA - 0.5);
                    half grain = ((half)noiseA - 0.5h) * _CRTGrainAmount * insideScreen;

                    half3 phosphorRGB = saturate(crtCol * (scanMul * humBand * tubeVig * flicker) + glassHighlight + grain);
                    surfaceData.albedo = phosphorRGB * 0.06h;
                    surfaceData.emission = phosphorRGB * _EmissionColor.rgb;
                }

                InputData inputData;
                InitializeCRTInputData(input, inputData);
                InitializeCRTBakedGI(input, inputData);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = OutputAlpha(color.a, false);

                #if REAL_IS_HALF
                outColor = min(color, HALF_MAX);
                #else
                outColor = color;
                #endif

                #ifdef _WRITE_RENDERING_LAYERS
                    outRenderingLayers = EncodeMeshRenderingLayer();
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            DECLARE_CRT_DISPLAY_CBUFFER

            struct Attributes
            {
                float4 position : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }

            half DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            DECLARE_CRT_DISPLAY_CBUFFER

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3  normalWS   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = half3(normalInput.normalWS);
                return output;
            }

            void DepthNormalsFragment(
                Varyings input
                , out half4 outNormalWS : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif

                #if defined(_GBUFFER_NORMALS_OCT)
                    float3 normalWS = normalize(input.normalWS);
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
                    outNormalWS = half4(packedNormalWS, 0.0);
                #else
                    outNormalWS = half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
                #endif

                #ifdef _WRITE_RENDERING_LAYERS
                    outRenderingLayers = EncodeMeshRenderingLayer();
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
