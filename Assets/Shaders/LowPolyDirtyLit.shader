Shader "Canal420/LowPolyDirtyLit"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.25
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2, 1)
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0
        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        _Cull("Cull", Float) = 2.0

        [Header(Canal 420 Procedural Grime and Low Poly)]
        _DampFloorHeight("Floor Damp Height (m)", Range(0.05, 1.0)) = 0.38
        _DampCeilingStart("Ceiling Grime Start (m)", Range(1.5, 2.74)) = 2.46
        _DampCeilingEnd("Ceiling Grime End (m)", Range(1.8, 3.0)) = 2.74
        _DampStrength("Damp and Grime Strength", Range(0.0, 1.0)) = 0.32
        _DampTint("Damp and Grime Tint", Color) = (0.34, 0.36, 0.30, 1)
        _MicroVarStrength("Spatial Grime Variation", Range(0.0, 0.5)) = 0.14
        _VertexAOStrength("Vertex Color AO Strength", Range(0.0, 1.0)) = 0.85
        _PenumbraSteps("Penumbra Quantization Steps", Range(4.0, 32.0)) = 12.0
        _PenumbraQuantBlend("Penumbra Quantization Blend", Range(0.0, 1.0)) = 0.50
        _DitherStrength("Screen Bayer Dither Strength", Range(0.0, 0.05)) = 0.012

        [Header(Canal 420 Retro PS1 Surface Texture)]
        [NoScaleOffset] _SurfaceTex("Retro Diffuse Surface Texture", 2D) = "gray" {}
        _SurfaceTexScale("Surface World Scale", Range(0.1, 4.0)) = 0.65
        _SurfaceTexStrength("Surface Texture Strength", Range(0.0, 0.35)) = 0.0
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
            #define DECLARE_DIRTY_LIT_CBUFFER \
            CBUFFER_START(UnityPerMaterial) \
                half4 _BaseColor; \
                half4 _SpecColor; \
                half4 _EmissionColor; \
                half4 _DampTint; \
                half _Smoothness; \
                half _Metallic; \
                half _Cutoff; \
                half _DampFloorHeight; \
                half _DampCeilingStart; \
                half _DampCeilingEnd; \
                half _DampStrength; \
                half _MicroVarStrength; \
                half _VertexAOStrength; \
                half _PenumbraSteps; \
                half _PenumbraQuantBlend; \
                half _DitherStrength; \
                half _SurfaceTexScale; \
                half _SurfaceTexStrength; \
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

            #pragma vertex DirtyLitPassVertex
            #pragma fragment DirtyLitPassFragment

            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF

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

            DECLARE_DIRTY_LIT_CBUFFER

            TEXTURE2D(_SurfaceTex);
            SAMPLER(sampler_SurfaceTex);

            struct Attributes
            {
                float4 positionOS         : POSITION;
                float3 normalOS           : NORMAL;
                half4  color              : COLOR;
                float2 staticLightmapUV   : TEXCOORD1;
                float2 dynamicLightmapUV  : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float3 positionWS               : TEXCOORD0;
                half3  normalWS                 : TEXCOORD1;
                half4  color                    : COLOR;
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half4  fogFactorAndVertexLight  : TEXCOORD2;
            #else
                half   fogFactor                : TEXCOORD2;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord              : TEXCOORD3;
            #endif
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 4);
            #ifdef DYNAMICLIGHTMAP_ON
                float2 dynamicLightmapUV        : TEXCOORD5;
            #endif
            #ifdef USE_APV_PROBE_OCCLUSION
                float4 probeOcclusion           : TEXCOORD6;
            #endif
                float4 positionCS               : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return (half)frac((p.x + p.y) * p.z);
            }

            half ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                half n000 = Hash31(i + float3(0.0, 0.0, 0.0));
                half n100 = Hash31(i + float3(1.0, 0.0, 0.0));
                half n010 = Hash31(i + float3(0.0, 1.0, 0.0));
                half n110 = Hash31(i + float3(1.0, 1.0, 0.0));
                half n001 = Hash31(i + float3(0.0, 0.0, 1.0));
                half n101 = Hash31(i + float3(1.0, 0.0, 1.0));
                half n011 = Hash31(i + float3(0.0, 1.0, 1.0));
                half n111 = Hash31(i + float3(1.0, 1.0, 1.0));

                half nx00 = lerp(n000, n100, (half)f.x);
                half nx10 = lerp(n010, n110, (half)f.x);
                half nx01 = lerp(n001, n101, (half)f.x);
                half nx11 = lerp(n011, n111, (half)f.x);
                half nxy0 = lerp(nx00, nx10, (half)f.y);
                half nxy1 = lerp(nx01, nx11, (half)f.y);
                return lerp(nxy0, nxy1, (half)f.z);
            }

            half Bayer4x4(float2 screenPixelPos)
            {
                float2 pos = abs(screenPixelPos);
                float2 p = floor(pos - 4.0 * floor(pos * 0.25));
                float x1 = floor(p.x * 0.5);
                float y1 = floor(p.y * 0.5);
                float x0 = p.x - 2.0 * x1;
                float y0 = p.y - 2.0 * y1;
                float idx = (abs(x0 - y0) * 2.0 + y0) * 4.0 + (abs(x1 - y1) * 2.0 + y1);
                return (half)(idx * (1.0 / 16.0));
            }

            half3 ShadeDirtyLitLight(
                BRDFData brdfData,
                Light light,
                half3 normalWS,
                half3 viewDirectionWS,
                half bayer01,
                bool specularHighlightsOff)
            {
                half NdotL = saturate(dot(normalWS, light.direction));
                float rawAtten = light.distanceAttenuation * light.shadowAttenuation;
                float rawTerm = rawAtten * (float)NdotL;

                float effectiveTerm = 0.0;
                if (rawTerm > 1e-5)
                {
                    float steps = max(2.0, (float)_PenumbraSteps);
                    float curved = sqrt(saturate(rawTerm));
                    float dithered = saturate(curved + ((float)bayer01 - 0.5) * (0.65 / steps));
                    float stepped = floor(dithered * steps + 0.5) / steps;
                    float quant01 = stepped * stepped;
                    float edgeFade = smoothstep(0.5 / steps, 1.5 / steps, curved);
                    float blended01 = lerp(saturate(rawTerm), quant01, (float)_PenumbraQuantBlend * edgeFade);
                    effectiveTerm = (rawTerm > 1.0) ? rawTerm : blended01;
                }

                half3 radiance = light.color * (half)effectiveTerm;
                half3 brdf = brdfData.diffuse;
                #ifndef _SPECULARHIGHLIGHTS_OFF
                [branch]
                if (!specularHighlightsOff)
                {
                    brdf += brdfData.specular * DirectBRDFSpecular(brdfData, normalWS, light.direction, viewDirectionWS);
                }
                #endif
                return brdf * radiance;
            }

            void InitializeDirtyInputData(Varyings input, out InputData inputData)
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

            void InitializeDirtyBakedGI(Varyings input, inout InputData inputData)
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

            Varyings DirtyLitPassVertex(Attributes input)
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

                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.color = input.color;

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

            void DirtyLitPassFragment(
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

                InputData inputData;
                InitializeDirtyInputData(input, inputData);

                // World-space damp & ceiling grime with organic low-frequency variation
                half nLow = ValueNoise3D(inputData.positionWS * 1.15);
                half nMed = ValueNoise3D(inputData.positionWS * 3.70 + 17.3);
                half grimeNoise = nLow * 0.65h + nMed * 0.35h;

                half wallMask = 1.0h - saturate(abs(inputData.normalWS.y) * 1.35h);
                half effFloorH = max(0.05h, _DampFloorHeight) * (0.68h + 0.64h * grimeNoise);
                half floorGrad = saturate(1.0h - (half)max(inputData.positionWS.y, 0.0) / effFloorH);
                floorGrad *= floorGrad;

                half ceilRange = max(0.05h, _DampCeilingEnd - _DampCeilingStart);
                half effCeilStart = _DampCeilingStart - (grimeNoise - 0.5h) * 0.14h;
                half ceilGrad = saturate(((half)inputData.positionWS.y - effCeilStart) / ceilRange);
                ceilGrad *= ceilGrad;

                half heightGrime = max(floorGrad, ceilGrad) * lerp(0.24h, 1.0h, wallMask);
                half dampMask = saturate(heightGrime * _DampStrength * (0.70h + 0.60h * grimeNoise));

                // Vertex Color AO: guard unbound COLOR attribute (Metal/D3D11/Vulkan may supply (0,0,0,1))
                half maxVCol = max(input.color.r, max(input.color.g, input.color.b));
                half vertexAO = (maxVCol < 0.01h) ? 1.0h : saturate(lerp(1.0h, input.color.r, _VertexAOStrength));

                half3 albedo = _BaseColor.rgb * (1.0h + (grimeNoise - 0.5h) * _MicroVarStrength);
                [branch]
                if (_SurfaceTexStrength > 0.001h)
                {
                    half3 triW = abs(inputData.normalWS);
                    triW = triW * triW;
                    triW = triW * triW;
                    triW /= max(dot(triW, half3(1.0h, 1.0h, 1.0h)), 0.0001h);

                    float3 pScaled = inputData.positionWS * (float)_SurfaceTexScale;
                    half3 sx = SAMPLE_TEXTURE2D(_SurfaceTex, sampler_SurfaceTex, pScaled.zy).rgb;
                    half3 sy = SAMPLE_TEXTURE2D(_SurfaceTex, sampler_SurfaceTex, pScaled.xz).rgb;
                    half3 sz = SAMPLE_TEXTURE2D(_SurfaceTex, sampler_SurfaceTex, pScaled.xy).rgb;
                    half3 surfSample = sx * triW.x + sy * triW.y + sz * triW.z;

                    half3 surfDelta = (surfSample - 0.5h) * 2.0h;
                    albedo *= max(0.0h, 1.0h + surfDelta * _SurfaceTexStrength);
                }
                albedo = lerp(albedo, albedo * _DampTint.rgb, dampMask);
                albedo *= lerp(1.0h, vertexAO, 0.40h);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = saturate(albedo);
                surfaceData.metallic = _Metallic;
                surfaceData.specular = _SpecColor.rgb;
                surfaceData.smoothness = saturate(_Smoothness * (1.0h - dampMask * 0.45h));
                surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
                #ifdef _EMISSION
                surfaceData.emission = _EmissionColor.rgb;
                #endif
                surfaceData.occlusion = vertexAO;
                surfaceData.alpha = _BaseColor.a;

                InitializeDirtyBakedGI(input, inputData);

                #if defined(_SPECULARHIGHLIGHTS_OFF)
                bool specularHighlightsOff = true;
                #else
                bool specularHighlightsOff = false;
                #endif

                BRDFData brdfData;
                InitializeBRDFData(surfaceData, brdfData);

                #if defined(DEBUG_DISPLAY)
                half4 debugColor;
                if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
                {
                    outColor = debugColor;
                    #ifdef _WRITE_RENDERING_LAYERS
                    outRenderingLayers = EncodeMeshRenderingLayer();
                    #endif
                    return;
                }
                #endif

                BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
                half4 shadowMask = CalculateShadowMask(inputData);
                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
                uint meshRenderingLayers = GetMeshRenderingLayer();

                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
                MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

                LightingData lightingData = CreateLightingData(inputData, surfaceData);
                lightingData.giColor = GlobalIllumination(
                    brdfData, brdfDataClearCoat, 0.0h,
                    inputData.bakedGI, aoFactor.indirectAmbientOcclusion,
                    inputData.positionWS, inputData.normalWS,
                    inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);

                half bayer01 = Bayer4x4(input.positionCS.xy);

                #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
                #endif
                {
                    lightingData.mainLightColor = ShadeDirtyLitLight(
                        brdfData, mainLight, inputData.normalWS, inputData.viewDirectionWS, bayer01, specularHighlightsOff);
                }

                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();

                #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                    #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                    #endif
                    {
                        lightingData.additionalLightsColor += ShadeDirtyLitLight(
                            brdfData, light, inputData.normalWS, inputData.viewDirectionWS, bayer01, specularHighlightsOff);
                    }
                }
                #endif

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                    #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                    #endif
                    {
                        lightingData.additionalLightsColor += ShadeDirtyLitLight(
                            brdfData, light, inputData.normalWS, inputData.viewDirectionWS, bayer01, specularHighlightsOff);
                    }
                LIGHT_LOOP_END
                #endif

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
                #endif

                half4 color = CalculateFinalColor(lightingData, surfaceData.alpha);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);

                // Luminance-aware screen-space 4x4 Bayer dither breaks up dark corridor banding without lifting pure blacks
                half lum = dot(color.rgb, half3(0.299h, 0.587h, 0.114h));
                half ditherAmp = _DitherStrength * saturate(lum * 10.0h) * (0.30h + 0.70h * sqrt(saturate(lum)));
                color.rgb = max(0.0h, color.rgb + (bayer01 - 0.5h) * ditherAmp);
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
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            DECLARE_DIRTY_LIT_CBUFFER

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.positionCS = ApplyShadowClamping(positionCS);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif
                return 0;
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

            DECLARE_DIRTY_LIT_CBUFFER

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

            DECLARE_DIRTY_LIT_CBUFFER

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
