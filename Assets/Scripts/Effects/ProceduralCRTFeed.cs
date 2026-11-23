using UnityEngine;

namespace Canal420.Effects
{
    public enum CRTFeedMode
    {
        Off = 0,
        BroadcastChannel04 = 1,
        BroadcastChannel09 = 2,
        BroadcastChannel17 = 3,
        Canal420Ghost = 4,
        News1994Broadcast = 5,
        Canal420Bleed = 6,
        SubliminalShockFrame = 7,
        DeadGlassResidualPulse = 8
    }

    /// <summary>
    /// High-detail 240x180 procedural 1990s CRT broadcast synthesizer with framebuffer NTSC/VHS
    /// chroma smear, aperture-grille phosphor mask, 5x7 teletext OSD/tickers, and real-time
    /// dominant light color/brightness extraction for room bounce illumination.
    /// </summary>
    public class ProceduralCRTFeed : MonoBehaviour
    {
        private const int Width = 240;
        private const int Height = 180;
        private const float TargetFrameRate = 20f;
        private const int MinShockVisibleFrames = 4;

        private Texture2D texture;
        private Color32[] pixels;
        private float[] rBuf;
        private float[] gBuf;
        private float[] bBuf;
        private Material targetMaterial;
        private bool usesGpuPhosphorShader;

        private static readonly int PropCRTActive = Shader.PropertyToID("_CRTActive");
        private static readonly int PropCRTResolution = Shader.PropertyToID("_CRTResolution");
        private static readonly int PropCRTRgbShift = Shader.PropertyToID("_CRTRgbShift");
        private static readonly int PropCRTIsUhfGhost = Shader.PropertyToID("_CRTIsUhfGhost");
        private static readonly int PropCRTIsVhsTape = Shader.PropertyToID("_CRTIsVhsTape");
        private static readonly int PropCRTHumBarCenter = Shader.PropertyToID("_CRTHumBarCenter");
        private static readonly int PropCRTVhsTrackingBand = Shader.PropertyToID("_CRTVhsTrackingBand");
        private static readonly int PropCRTGrainAmount = Shader.PropertyToID("_CRTGrainAmount");
        private static readonly int PropCRTSwitchStrength = Shader.PropertyToID("_CRTSwitchStrength");
        private static readonly int PropCRTTime = Shader.PropertyToID("_CRTTime");

        private CRTFeedMode currentMode = CRTFeedMode.Off;
        private CRTFeedMode shockOverrideMode = CRTFeedMode.SubliminalShockFrame;
        private float hauntProgress01;
        private float shockOverrideTimer;
        private int shockMinVisibleFrames;
        private float channelSwitchTimer;
        private float osdBannerTimer;
        private float accumTime;
        private float animClock;
        private uint rngState = 0x5A17C420u;

        // Cached per-frame & per-scanline invariants (0 visual change)
        private float cachedPsyT = float.NaN;
        private float cachedPsyNy = float.NaN;
        private float psyBlobAx, psyBlobAy, psyBlobBx, psyBlobBy;
        private float psyStrobeBeat;
        private float psyFigureSway, psyFigureWidth;
        private float cachedBleedT = float.NaN;
        private float cachedBleedNy = float.NaN;
        private float bleedSliceThresh, bleedBandWave;

        public Color DominantFrameColor { get; private set; } = new Color(0.08f, 0.12f, 0.16f);
        public float CurrentBrightness01 { get; private set; }
        public CRTFeedMode ActiveMode => (shockOverrideTimer > 0f || shockMinVisibleFrames > 0) ? shockOverrideMode : currentMode;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (texture != null)
            {
                if (Application.isPlaying) Destroy(texture);
                else DestroyImmediate(texture);
                texture = null;
            }
        }

        private Material lastBoundMaterial;
        private bool isClearedToBlack = false;

        public void BindMaterial(Material mat)
        {
            EnsureInitialized();
            if (targetMaterial != mat || lastBoundMaterial != mat)
            {
                targetMaterial = mat;
                ApplyTextureToMaterial();
            }
        }

        public void SetMode(CRTFeedMode mode, bool renderImmediately = true)
        {
            EnsureInitialized();
            bool changed = currentMode != mode;
            currentMode = mode;

            if (changed)
            {
                osdBannerTimer = (mode == CRTFeedMode.Canal420Ghost) ? 0.85f : 2.8f;
            }

            if (currentMode == CRTFeedMode.Off && shockOverrideTimer <= 0f && shockMinVisibleFrames <= 0)
            {
                if (changed || !isClearedToBlack)
                {
                    ClearToBlack();
                }
                return;
            }

            if (changed && renderImmediately)
            {
                RenderFrameNow();
            }
        }

        public void SetHauntProgress(float progress01)
        {
            hauntProgress01 = Mathf.Clamp01(progress01);
        }

        public void TriggerShockFrame(float duration = 0.14f, bool residualOnly = false)
        {
            EnsureInitialized();
            shockOverrideMode = residualOnly ? CRTFeedMode.DeadGlassResidualPulse : CRTFeedMode.SubliminalShockFrame;
            shockOverrideTimer = Mathf.Max(shockOverrideTimer, duration);
            shockMinVisibleFrames = Mathf.Max(shockMinVisibleFrames, MinShockVisibleFrames);
            RenderFrameNow();
        }

        public void TriggerChannelSwitchBurst(float duration = 0.24f)
        {
            EnsureInitialized();
            channelSwitchTimer = Mathf.Max(channelSwitchTimer, duration);
            osdBannerTimer = (currentMode == CRTFeedMode.Canal420Ghost) ? 1.15f : 3.0f;
            RenderFrameNow();
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f)
                return;

            if (osdBannerTimer > 0f)
            {
                osdBannerTimer = Mathf.Max(0f, osdBannerTimer - Time.deltaTime);
            }

            if (channelSwitchTimer > 0f)
            {
                channelSwitchTimer = Mathf.Max(0f, channelSwitchTimer - Time.deltaTime);
            }

            if (shockOverrideTimer > 0f || shockMinVisibleFrames > 0)
            {
                if (shockOverrideTimer > 0f)
                    shockOverrideTimer = Mathf.Max(0f, shockOverrideTimer - Time.deltaTime);
                if (shockMinVisibleFrames > 0)
                    shockMinVisibleFrames--;
                if (shockOverrideTimer <= 0f && shockMinVisibleFrames <= 0 && currentMode == CRTFeedMode.Off)
                {
                    ClearToBlack();
                    return;
                }
            }

            CRTFeedMode effectiveMode = ActiveMode;
            if (effectiveMode == CRTFeedMode.Off)
                return;

            animClock += Time.deltaTime;
            accumTime += Time.deltaTime;
            float interval = 1f / TargetFrameRate;
            if (accumTime >= interval)
            {
                accumTime = Mathf.Repeat(accumTime, interval);
                RenderFrameNow();
            }
        }

        private void EnsureInitialized()
        {
            bool needsBind = false;
            if (texture == null)
            {
                texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "ProceduralCRTFeed_RT"
                };
                int total = Width * Height;
                pixels = new Color32[total];
                rBuf = new float[total];
                gBuf = new float[total];
                bBuf = new float[total];
                ClearToBlack();
                needsBind = true;
            }

            if (targetMaterial == null)
            {
                Renderer r = GetComponent<Renderer>();
                if (r != null)
                {
                    targetMaterial = Application.isPlaying ? r.material : r.sharedMaterial;
                }
            }

            if (needsBind || targetMaterial != lastBoundMaterial)
            {
                ApplyTextureToMaterial();
            }
        }

        private void ApplyTextureToMaterial()
        {
            if (targetMaterial == null || texture == null)
            {
                usesGpuPhosphorShader = false;
                lastBoundMaterial = null;
                return;
            }

            lastBoundMaterial = targetMaterial;
            usesGpuPhosphorShader = targetMaterial.shader != null &&
                                    targetMaterial.shader.name == "Canal420/CRTPhosphorDisplay";

            if (targetMaterial.HasProperty("_BaseMap"))
                targetMaterial.SetTexture("_BaseMap", texture);
            if (targetMaterial.HasProperty("_MainTex"))
                targetMaterial.SetTexture("_MainTex", texture);
            if (targetMaterial.HasProperty("_EmissionMap"))
                targetMaterial.SetTexture("_EmissionMap", texture);

            if (usesGpuPhosphorShader)
            {
                targetMaterial.SetVector(PropCRTResolution, new Vector4(Width, Height, 0f, 0f));
                targetMaterial.SetFloat(PropCRTActive, ActiveMode == CRTFeedMode.Off ? 0f : 1f);
            }
        }

        private void ClearToBlack()
        {
            if (pixels == null || texture == null) return;
            Color32 black = new Color32(4, 5, 7, 255);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = black;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            isClearedToBlack = true;
            DominantFrameColor = new Color(0.02f, 0.03f, 0.04f);
            CurrentBrightness01 = 0f;
            if (usesGpuPhosphorShader && targetMaterial != null)
            {
                targetMaterial.SetFloat(PropCRTActive, 0f);
                targetMaterial.SetFloat(PropCRTSwitchStrength, 0f);
            }
        }

        private float NextRand01()
        {
            rngState ^= rngState << 13;
            rngState ^= rngState >> 17;
            rngState ^= rngState << 5;
            return (rngState & 0xFFFFu) / 65535f;
        }

        private void RenderFrameNow()
        {
            CRTFeedMode mode = ActiveMode;
            if (mode == CRTFeedMode.Off) return;
            isClearedToBlack = false;

            float t = animClock;
            float syncTearPhase = Mathf.Repeat(t * 1.95f, 1f);
            bool heavyTearActive = (mode == CRTFeedMode.Canal420Bleed || (mode == CRTFeedMode.Canal420Ghost && hauntProgress01 > 0.60f))
                && syncTearPhase > 0.86f;
            bool isUhfGhost = mode == CRTFeedMode.BroadcastChannel09 || mode == CRTFeedMode.BroadcastChannel17;
            bool isVhsTape = mode == CRTFeedMode.News1994Broadcast || mode == CRTFeedMode.Canal420Bleed;
            bool switchingBurst = channelSwitchTimer > 0f && mode != CRTFeedMode.SubliminalShockFrame;
            float switchStrength = switchingBurst ? Mathf.Clamp01(channelSwitchTimer / 0.24f) : 0f;
            int rollOffsetY = switchingBurst ? Mathf.RoundToInt(switchStrength * 44f) : 0;

            // Pass 1: Sample clean scene into floating-point framebuffer
            for (int y = 0; y < Height; y++)
            {
                int sySample = (y + rollOffsetY) % Height;
                float ny = sySample / (float)(Height - 1);
                int xJitter = 0;

                if (switchingBurst)
                {
                    xJitter = Mathf.RoundToInt(Mathf.Sin(ny * 42f + t * 50f) * 26f * switchStrength + (NextRand01() - 0.5f) * 18f * switchStrength);
                }
                else if (mode == CRTFeedMode.Canal420Ghost || mode == CRTFeedMode.Canal420Bleed || mode == CRTFeedMode.SubliminalShockFrame)
                {
                    if (NextRand01() < (mode == CRTFeedMode.Canal420Bleed ? 0.18f : 0.09f))
                        xJitter = Mathf.RoundToInt((NextRand01() - 0.5f) * 22f);
                    if (heavyTearActive && ny > 0.26f && ny < 0.78f)
                        xJitter += Mathf.RoundToInt(Mathf.Sin(ny * 48f + t * 32f) * 26f);
                }
                else if (isVhsTape)
                {
                    // Classic VHS bottom head-switching skew & rare sync jitter
                    if (ny < 0.055f)
                        xJitter = Mathf.RoundToInt(Mathf.Sin(ny * 140f + t * 48f) * 9f + (NextRand01() - 0.5f) * 7f);
                    else if (NextRand01() < 0.04f)
                        xJitter = Mathf.RoundToInt((NextRand01() - 0.5f) * 6f);
                }

                bool vBlankBar = switchingBurst && Mathf.Abs((y / (float)(Height - 1)) - Mathf.Repeat(1f - switchStrength * 1.4f, 1f)) < 0.075f;
                int rowOffset = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    int sx = Mathf.Clamp(x + xJitter, 0, Width - 1);
                    float nx = sx / (float)(Width - 1);
                    SampleModePixel(mode, sx, sySample, nx, ny, t, out float r, out float g, out float b);
                    if (vBlankBar)
                    {
                        r *= 0.14f; g *= 0.14f; b *= 0.16f;
                    }
                    else if (switchingBurst && NextRand01() < (0.48f * switchStrength))
                    {
                        float snow = 0.25f + NextRand01() * 0.72f;
                        r = Mathf.Lerp(r, snow, 0.78f);
                        g = Mathf.Lerp(g, snow, 0.78f);
                        b = Mathf.Lerp(b, snow, 0.78f);
                    }
                    int idx = rowOffset + x;
                    rBuf[idx] = r;
                    gBuf[idx] = g;
                    bBuf[idx] = b;
                }
            }

            // Pass 2: Overlay crisp broadcast OSD / VHS corner badges directly into framebuffer before CRT glass optical pass
            DrawBroadcastOSD(mode, t);

            float humBarCenter = Mathf.Repeat(t * 0.22f, 1f);
            float vhsTrackingBand = Mathf.Repeat(-t * 0.14f, 1f);
            int rgbShift = (mode == CRTFeedMode.Canal420Ghost || mode == CRTFeedMode.Canal420Bleed || mode == CRTFeedMode.SubliminalShockFrame) ? 2 : 1;

            if (usesGpuPhosphorShader && targetMaterial != null)
            {
                // Keep rngState advancing deterministically while offloading Pass 3 optics (scanlines, aperture-grille,
                // chroma smear, UHF ghost, VHS snow, barrel bezel, glass glare) to Canal420/CRTPhosphorDisplay on GPU.
                NextRand01();

                targetMaterial.SetFloat(PropCRTActive, 1f);
                targetMaterial.SetFloat(PropCRTRgbShift, rgbShift);
                targetMaterial.SetFloat(PropCRTIsUhfGhost, isUhfGhost ? 1f : 0f);
                targetMaterial.SetFloat(PropCRTIsVhsTape, isVhsTape ? 1f : 0f);
                targetMaterial.SetFloat(PropCRTHumBarCenter, humBarCenter);
                targetMaterial.SetFloat(PropCRTVhsTrackingBand, vhsTrackingBand);
                targetMaterial.SetFloat(PropCRTGrainAmount, isVhsTape ? 0.085f : 0.048f);
                targetMaterial.SetFloat(PropCRTSwitchStrength, switchStrength);
                targetMaterial.SetFloat(PropCRTTime, t);

                float gpuSumR = 0f, gpuSumG = 0f, gpuSumB = 0f, gpuSumWeight = 0f, gpuSumLum = 0f;
                int gpuSampleCount = 0;
                for (int y = 0; y < Height; y++)
                {
                    int row = y * Width;
                    bool sampleRow = (y % 3) == 0;
                    for (int x = 0; x < Width; x++)
                    {
                        int idx = row + x;
                        float r = rBuf[idx];
                        float g = gBuf[idx];
                        float b = bBuf[idx];
                        if (r < 0f) r = 0f; else if (r > 1f) r = 1f;
                        if (g < 0f) g = 0f; else if (g > 1f) g = 1f;
                        if (b < 0f) b = 0f; else if (b > 1f) b = 1f;

                        pixels[idx] = new Color32((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), 255);

                        if (sampleRow && (x % 3) == 0)
                        {
                            float lum = r * 0.299f + g * 0.587f + b * 0.114f;
                            float maxC = r > g ? (r > b ? r : b) : (g > b ? g : b);
                            float minC = r < g ? (r < b ? r : b) : (g < b ? g : b);
                            float chroma = maxC - minC;
                            float w = 0.03f + lum * 0.52f + chroma * 1.65f;
                            gpuSumR += r * w;
                            gpuSumG += g * w;
                            gpuSumB += b * w;
                            gpuSumWeight += w;
                            gpuSumLum += lum;
                            gpuSampleCount++;
                        }
                    }
                }

                if (gpuSumWeight > 0.0001f && gpuSampleCount > 0)
                {
                    Color avg = new Color(gpuSumR / gpuSumWeight, gpuSumG / gpuSumWeight, gpuSumB / gpuSumWeight);
                    float avgLum = gpuSumLum / gpuSampleCount;
                    Color.RGBToHSV(avg, out float h, out float s, out float v);
                    s = Mathf.Clamp01(s * 1.22f);
                    v = Mathf.Clamp(v, 0.26f, 1f);
                    DominantFrameColor = Color.HSVToRGB(h, s, v);
                    CurrentBrightness01 = Mathf.Clamp01(avgLum * 1.72f);
                }

                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                return;
            }

            // Pass 3: CPU Optical Pipeline fallback (used when shader is URP/Lit or in headless unit tests)
            float sumR = 0f, sumG = 0f, sumB = 0f, sumWeight = 0f, sumLum = 0f;
            int sampleCount = 0;

            for (int y = 0; y < Height; y++)
            {
                float ny = y / (float)(Height - 1);
                bool scanDark = (y & 1) == 1;
                float scanMul = scanDark ? 0.80f : 1.02f;

                float humDist = Mathf.Abs(ny - humBarCenter);
                float humBand = 1f + 0.09f * Mathf.Exp(-humDist * humDist * 65f);

                bool inTrackingNoise = isVhsTape && Mathf.Abs(ny - vhsTrackingBand) < 0.022f;
                int row = y * Width;

                for (int x = 0; x < Width; x++)
                {
                    float nx = x / (float)(Width - 1);

                    // Horizontal RGB convergence fringing + mild YIQ chroma smear
                    int rxIdx = row + Mathf.Clamp(x - rgbShift, 0, Width - 1);
                    int cxIdx = row + x;
                    int bxIdx = row + Mathf.Clamp(x + rgbShift, 0, Width - 1);
                    int smearIdx = row + Mathf.Clamp(x - 2, 0, Width - 1);

                    float r = rBuf[rxIdx] * 0.78f + rBuf[smearIdx] * 0.22f;
                    float g = gBuf[cxIdx];
                    float b = bBuf[bxIdx] * 0.78f + bBuf[smearIdx] * 0.22f;

                    // Subtle UHF multipathing ghost echo on channels 09 & 17
                    if (isUhfGhost && x >= 6)
                    {
                        int ghostIdx = row + (x - 6);
                        r = r * 0.86f + rBuf[ghostIdx] * 0.18f;
                        g = g * 0.86f + gBuf[ghostIdx] * 0.18f;
                        b = b * 0.86f + bBuf[ghostIdx] * 0.18f;
                    }

                    // Aperture-grille RGB subpixel triad micro-variation
                    int triad = x % 3;
                    if (triad == 0) { r *= 1.05f; g *= 0.96f; b *= 0.96f; }
                    else if (triad == 1) { r *= 0.96f; g *= 1.05f; b *= 0.96f; }
                    else { r *= 0.96f; g *= 0.96f; b *= 1.05f; }

                    // VHS tracking snow band
                    if (inTrackingNoise && NextRand01() < 0.38f)
                    {
                        float streak = 0.35f + NextRand01() * 0.55f;
                        r = Mathf.Lerp(r, streak, 0.65f);
                        g = Mathf.Lerp(g, streak, 0.65f);
                        b = Mathf.Lerp(b, streak, 0.65f);
                    }

                    // Analog grain + CRT bezel curvature vignette + faint tube glass reflection corner
                    float grainAmt = isVhsTape ? 0.085f : 0.048f;
                    float grain = (NextRand01() - 0.5f) * grainAmt;
                    float dx = (nx - 0.5f) * 2f;
                    float dy = (ny - 0.5f) * 2f;
                    float cornerPower = dx * dx * dx * dx * 0.10f + dy * dy * dy * dy * 0.12f;
                    float tubeVig = Mathf.Clamp01(1.04f - (dx * dx * 0.18f + dy * dy * 0.22f) - cornerPower * 1.8f);

                    float glassHighlight = Mathf.Exp(-((nx - 0.16f) * (nx - 0.16f) * 14f + (ny - 0.84f) * (ny - 0.84f) * 22f)) * 0.06f;

                    float mul = scanMul * humBand * tubeVig;
                    r = Mathf.Clamp01(r * mul + glassHighlight + grain);
                    g = Mathf.Clamp01(g * mul + glassHighlight + grain);
                    b = Mathf.Clamp01(b * mul + glassHighlight + grain);

                    pixels[cxIdx] = new Color32(
                        (byte)(r * 255f),
                        (byte)(g * 255f),
                        (byte)(b * 255f),
                        255
                    );

                    // Subsample 3x3 grid for low-overhead live room bounce light color & luminance
                    if ((x % 3) == 0 && (y % 3) == 0)
                    {
                        float lum = r * 0.299f + g * 0.587f + b * 0.114f;
                        float maxC = Mathf.Max(r, Mathf.Max(g, b));
                        float minC = Mathf.Min(r, Mathf.Min(g, b));
                        float chroma = maxC - minC;
                        float w = 0.03f + lum * 0.52f + chroma * 1.65f;
                        sumR += r * w;
                        sumG += g * w;
                        sumB += b * w;
                        sumWeight += w;
                        sumLum += lum;
                        sampleCount++;
                    }
                }
            }

            if (sumWeight > 0.0001f && sampleCount > 0)
            {
                Color avg = new Color(sumR / sumWeight, sumG / sumWeight, sumB / sumWeight);
                float avgLum = sumLum / sampleCount;
                Color.RGBToHSV(avg, out float h, out float s, out float v);
                s = Mathf.Clamp01(s * 1.22f);
                v = Mathf.Clamp(v, 0.26f, 1f);
                DominantFrameColor = Color.HSVToRGB(h, s, v);
                CurrentBrightness01 = Mathf.Clamp01(avgLum * 1.72f);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private void SampleModePixel(
            CRTFeedMode mode,
            int px, int py,
            float nx, float ny,
            float t,
            out float r, out float g, out float b)
        {
            switch (mode)
            {
                case CRTFeedMode.BroadcastChannel04:
                    SampleChannel04News(px, py, nx, ny, t, out r, out g, out b);
                    return;
                case CRTFeedMode.BroadcastChannel09:
                    SampleChannel09WeatherAndStage(px, py, nx, ny, t, out r, out g, out b);
                    return;
                case CRTFeedMode.BroadcastChannel17:
                    SampleChannel17Infomercial(px, py, nx, ny, t, out r, out g, out b);
                    return;
                case CRTFeedMode.Canal420Ghost:
                    SampleChannel420PsychedelicHorror(px, py, nx, ny, t, out r, out g, out b);
                    return;
                case CRTFeedMode.News1994Broadcast:
                    SampleNews1994(px, py, nx, ny, t, out r, out g, out b);
                    return;
                case CRTFeedMode.Canal420Bleed:
                    SampleCanal420Bleed(px, py, nx, ny, t, out r, out g, out b);
                    return;
                case CRTFeedMode.SubliminalShockFrame:
                    SampleShockHorrorFace(nx, ny, t, out r, out g, out b);
                    return;
                case CRTFeedMode.DeadGlassResidualPulse:
                    float pulse = 0.5f + 0.5f * Mathf.Sin(t * 7f);
                    float glow = Mathf.Exp(-((nx - 0.5f) * (nx - 0.5f) + (ny - 0.5f) * (ny - 0.5f)) * 5.2f) * pulse;
                    r = 0.08f * glow;
                    g = 0.24f * glow;
                    b = 0.34f * glow;
                    return;
                default:
                    r = g = b = 0.02f;
                    return;
            }
        }

        /// <summary>
        /// Channel 04: 1995 Regional Night Newsroom (shaded anchor speaking behind desk, live harbor/chart inset, scrolling ticker),
        /// rainy night avenue B-roll (wet puddles, sodium streetlamps, headlights, slanted rain), and financial board cut.
        /// </summary>
        private void SampleChannel04News(int px, int py, float nx, float ny, float t, out float r, out float g, out float b)
        {
            float cycle = Mathf.Repeat(t, 8.4f);
            if (cycle < 3.7f || cycle >= 6.3f)
            {
                bool isGraphicCut = cycle >= 6.3f;

                // Ribbed studio curtain backdrop with soft studio spotlight dome & secondary rim gradient
                float spotDome = Mathf.Exp(-((nx - 0.38f) * (nx - 0.38f) * 3.8f + (ny - 0.62f) * (ny - 0.62f) * 4.5f));
                float pleats = 0.84f + 0.16f * Mathf.Sin(nx * 55f);
                r = (0.03f + spotDome * 0.08f) * pleats;
                g = (0.10f + spotDome * 0.21f) * pleats;
                b = (0.26f + spotDome * 0.44f) * pleats;

                // Subtle studio floor perspective strip & glowing blue desk monitor backdrop
                if (ny >= 0.295f && ny < 0.37f)
                {
                    float railGlow = 0.5f + 0.5f * Mathf.Sin(nx * 18f);
                    r = 0.06f + 0.04f * railGlow;
                    g = 0.14f + 0.08f * railGlow;
                    b = 0.32f + 0.16f * railGlow;
                }
                if (ny > 0.355f && ny < 0.375f)
                {
                    r = 0.34f; g = 0.52f; b = 0.78f;
                }

                // Studio monitor inset over right shoulder (animated financial graph OR skyline silhouette)
                if (nx > 0.61f && nx < 0.93f && ny > 0.43f && ny < 0.84f)
                {
                    bool frame = nx < 0.628f || nx > 0.912f || ny < 0.448f || ny > 0.822f;
                    if (frame)
                    {
                        r = 0.56f; g = 0.72f; b = 0.90f;
                    }
                    else if (!isGraphicCut)
                    {
                        // Night harbor skyline & grid monitor
                        r = 0.03f; g = 0.08f; b = 0.17f;
                        float bx = (nx - 0.628f) / 0.284f;
                        float by = (ny - 0.448f) / 0.374f;
                        if ((px % 12) == 0 || (py % 10) == 0) { r += 0.03f; g += 0.08f; b += 0.14f; }
                        int bar = Mathf.FloorToInt(bx * 6f);
                        float bh = 0.22f + 0.52f * Mathf.Abs(Mathf.Sin(bar * 1.7f + t * 1.4f));
                        if (by < bh && (Mathf.Repeat(bx * 6f, 1f) > 0.18f && Mathf.Repeat(bx * 6f, 1f) < 0.82f))
                        {
                            r = bar >= 4 ? 0.86f : 0.22f;
                            g = bar >= 4 ? 0.22f : 0.66f;
                            b = bar >= 4 ? 0.20f : 0.90f;
                        }
                    }
                    else
                    {
                        // Economic downward trendline chart close-up inside studio screen
                        r = 0.05f; g = 0.12f; b = 0.22f;
                        float bx = (nx - 0.628f) / 0.284f;
                        float by = (ny - 0.448f) / 0.374f;
                        float curveY = 0.72f - bx * 0.45f + 0.11f * Mathf.Sin(bx * 18f - t * 2.4f);
                        if (Mathf.Abs(by - curveY) < 0.045f) { r = 0.96f; g = 0.26f; b = 0.20f; }
                        else if (by < curveY && by > 0.05f) { r += 0.16f; g += 0.03f; b += 0.03f; }
                    }
                }

                // Detailed Shaded Anchor Silhouette + Lip/Eye animation
                float ax = nx - 0.36f;
                float faceR = Mathf.Sqrt((ax / 0.068f) * (ax / 0.068f) + ((ny - 0.605f) / 0.105f) * ((ny - 0.605f) / 0.105f));
                float shoulderHalfW = 0.145f - Mathf.Clamp01((ny - 0.28f) / 0.24f) * 0.055f;
                bool inTorso = ny >= 0.27f && ny <= 0.51f && Mathf.Abs(ax) < shoulderHalfW;
                bool inNeck = ny >= 0.49f && ny <= 0.54f && Mathf.Abs(ax) < 0.028f;

                if (faceR < 1f)
                {
                    float keyShade = Mathf.Clamp01(0.72f + (ax * 3.5f) - Mathf.Max(0f, 0.56f - ny) * 2.2f);
                    bool hair = ny > 0.648f + 0.015f * Mathf.Cos(ax * 30f) || (ny > 0.61f && Mathf.Abs(ax) > 0.056f);
                    if (hair)
                    {
                        r = 0.08f * keyShade + 0.04f; g = 0.07f * keyShade + 0.03f; b = 0.09f * keyShade + 0.04f;
                    }
                    else
                    {
                        // Contoured skin tone
                        r = 0.80f * keyShade; g = 0.59f * keyShade; b = 0.48f * keyShade;

                        // Eyes (blinks cleanly every 3.8s)
                        bool blink = Mathf.Repeat(t, 3.8f) > 3.68f;
                        if (!blink && Mathf.Abs(ny - 0.612f) < 0.008f && (Mathf.Abs(ax + 0.022f) < 0.008f || Mathf.Abs(ax - 0.022f) < 0.008f))
                        {
                            r = 0.10f; g = 0.09f; b = 0.11f;
                        }
                        // Speaking mouth animation
                        float talk = 0.004f + 0.006f * Mathf.Abs(Mathf.Sin(t * 14f));
                        if (Mathf.Abs(ax) < 0.014f && Mathf.Abs(ny - 0.542f) < talk)
                        {
                            r = 0.22f; g = 0.10f; b = 0.10f;
                        }
                    }
                }
                else if (inNeck)
                {
                    r = 0.62f; g = 0.46f; b = 0.37f;
                }
                else if (inTorso)
                {
                    // Blazer with V-shirt opening, lapel rim, and crimson tie
                    float shirtSpan = Mathf.Clamp01((ny - 0.35f) / 0.16f) * 0.036f;
                    if (Mathf.Abs(ax) < 0.008f && ny < 0.49f)
                    {
                        r = 0.78f; g = 0.12f; b = 0.14f; // tie
                    }
                    else if (Mathf.Abs(ax) < shirtSpan)
                    {
                        r = 0.86f; g = 0.88f; b = 0.92f; // crisp shirt
                    }
                    else if (Mathf.Abs(Mathf.Abs(ax) - shirtSpan) < 0.012f)
                    {
                        r = 0.18f; g = 0.21f; b = 0.28f; // lapel highlight
                    }
                    else
                    {
                        float suitShade = 0.85f + ax * 1.8f;
                        r = 0.10f * suitShade; g = 0.12f * suitShade; b = 0.18f * suitShade;
                        // White pocket square accent on viewer's right chest
                        if (ax > 0.052f && ax < 0.080f && ny > 0.36f && ny < 0.395f)
                        {
                            r = 0.82f; g = 0.84f; b = 0.88f;
                        }
                    }
                }

                // Mahogany anchor desk + specular top edge + script sheets & coffee mug & gooseneck mic
                if (ny >= 0.17f && ny < 0.295f && nx > 0.06f && nx < 0.88f)
                {
                    r = 0.30f; g = 0.14f; b = 0.08f;
                    if (ny > 0.276f) { r = 0.62f; g = 0.42f; b = 0.26f; } // specular glass lip
                    if (nx > 0.23f && nx < 0.38f && ny > 0.278f && ny < 0.294f) { r = 0.90f; g = 0.90f; b = 0.86f; } // scripts
                    if (nx > 0.48f && nx < 0.515f && ny > 0.282f && ny < 0.326f) { r = 0.72f; g = 0.76f; b = 0.80f; } // newsroom mug
                }
                // Table microphone boom & foam wind-screen
                if (nx > 0.425f && nx < 0.438f && ny >= 0.28f && ny < 0.40f)
                {
                    r = 0.12f; g = 0.12f; b = 0.14f;
                }
                if (Mathf.Sqrt((nx - 0.415f) * (nx - 0.415f) + (ny - 0.41f) * (ny - 0.41f)) < 0.016f)
                {
                    r = 0.06f; g = 0.06f; b = 0.08f;
                }

                // Lower-third news graphic bar
                if (ny < 0.17f)
                {
                    if (nx < 0.26f)
                    {
                        r = 0.68f; g = 0.08f; b = 0.10f; // red station badge zone
                    }
                    else
                    {
                        r = 0.08f; g = 0.20f; b = 0.44f; // scrolling news ribbon
                    }
                    if (ny > 0.155f) { r = 0.85f; g = 0.88f; b = 0.94f; }
                }
            }
            else
            {
                // Exterior B-Roll: Rainy urban night street, vanishing-point perspective, sodium vapor glow cones, puddles & passing car lights
                r = 0.03f; g = 0.03f; b = 0.06f;

                // Distant port cranes & apartment silhouettes above horizon (ny = 0.45)
                if (ny >= 0.45f)
                {
                    int bld = Mathf.FloorToInt(nx * 14f);
                    float bh = 0.48f + 0.18f * Mathf.Abs(Mathf.Sin(bld * 2.13f));
                    if (ny < bh)
                    {
                        r = 0.04f; g = 0.04f; b = 0.07f;
                        if (((px + py * 3) % 29) == 0 && (bld % 2) == 0) { r = 0.58f; g = 0.44f; b = 0.20f; }
                    }
                    // Distant Santos port gantry crane orange warning light near horizon right
                    float craneDist = Mathf.Sqrt((nx - 0.78f) * (nx - 0.78f) + (ny - 0.67f) * (ny - 0.67f));
                    if (craneDist < 0.032f && Mathf.Sin(t * 3.6f) > 0.1f)
                    {
                        float warn = Mathf.Exp(-craneDist * 60f);
                        r += 0.92f * warn; g += 0.20f * warn; b += 0.06f * warn;
                    }
                }
                else
                {
                    // Perspective road surface narrowing toward (0.5, 0.45)
                    float depth = (0.45f - ny) / 0.45f; // 0 horizon -> 1 foreground
                    float roadHalf = 0.04f + depth * 0.42f;
                    bool onRoad = Mathf.Abs(nx - 0.5f) < roadHalf;
                    if (onRoad)
                    {
                        r = 0.06f; g = 0.05f; b = 0.06f;
                        if (Mathf.Abs(nx - 0.5f) < (0.004f + depth * 0.008f) && Mathf.Repeat(depth * 7f - t * 2.2f, 1f) < 0.48f)
                        {
                            r = 0.62f; g = 0.56f; b = 0.34f;
                        }
                        // Wet red traffic-light reflection wash on foreground asphalt
                        float redSignalGlow = Mathf.Exp(-((nx - 0.35f) * (nx - 0.35f) * 32f + (ny - 0.20f) * (ny - 0.20f) * 18f))
                            * (Mathf.Sin(t * 0.8f) > -0.2f ? 0.28f : 0.06f);
                        r += redSignalGlow; g += redSignalGlow * 0.14f;
                    }
                    else
                    {
                        r = 0.04f; g = 0.04f; b = 0.05f; // wet sidewalk
                    }
                }

                // Sodium vapor streetlights receding in perspective along left and right curbs
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int s = 0; s < 3; s++)
                    {
                        float d = 0.25f + s * 0.32f;
                        float lx = 0.5f + side * (0.06f + d * 0.36f);
                        float ly = 0.54f + d * 0.24f;
                        float distLamp = Mathf.Sqrt((nx - lx) * (nx - lx) + (ny - ly) * (ny - ly) * 1.4f);
                        float glow = Mathf.Exp(-distLamp * 13f);
                        r += 1.05f * glow; g += 0.58f * glow; b += 0.14f * glow;

                        // Wet asphalt smeared sodium puddle reflection directly below pole
                        if (ny < 0.45f)
                        {
                            float ripple = 1f + 0.25f * Mathf.Sin(ny * 120f + t * 6f);
                            float refl = Mathf.Exp(-Mathf.Abs(nx - lx) * 20f) * (1f - ny * 1.4f) * 0.42f * ripple;
                            r += 0.88f * refl; g += 0.44f * refl; b += 0.10f * refl;
                        }
                    }
                }

                // Passing oncoming car headlights gliding down road lane
                float carProg = Mathf.Repeat((t - 3.7f) * 0.48f, 1.15f);
                float carY = 0.44f - carProg * 0.36f;
                float carX = 0.5f + 0.04f + carProg * 0.14f;
                float sep = 0.012f + carProg * 0.055f;
                float h1 = Mathf.Exp(-((nx - (carX - sep)) * (nx - (carX - sep)) * 260f + (ny - carY) * (ny - carY) * 420f));
                float h2 = Mathf.Exp(-((nx - (carX + sep)) * (nx - (carX + sep)) * 260f + (ny - carY) * (ny - carY) * 420f));
                float beams = (h1 + h2) * 1.15f;
                r += beams * 0.98f; g += beams * 0.90f; b += beams * 0.65f;

                // Diagonal falling rain streaks illuminated near lamps
                float rainPhase = Mathf.Repeat(nx * 28f + ny * 11f + t * 5.2f, 1f);
                if (rainPhase > 0.93f && (((px * 7 + py) % 13) == 0))
                {
                    r += 0.16f; g += 0.15f; b += 0.18f;
                }
            }
        }

        /// <summary>
        /// Channel 09: Topographic Baixada weather radar map (coastline, Serra ridge, sweeping radar & rain front)
        /// alternating with smoky UHF ghosted late-night stage spotlight performance.
        /// </summary>
        private void SampleChannel09WeatherAndStage(int px, int py, float nx, float ny, float t, out float r, out float g, out float b)
        {
            float cycle = Mathf.Repeat(t, 7.6f);
            if (cycle < 4.0f)
            {
                // Coastline & Ocean contour (Santos bay notch + coastal ridge)
                float bayNotch = 0.07f * Mathf.Exp(-((ny - 0.56f) * (ny - 0.56f)) * 75f);
                float coastX = 0.62f - ny * 0.24f + 0.05f * Mathf.Sin(ny * 10f) - bayNotch;
                bool isLand = nx < coastX;
                bool isRidge = isLand && nx < (coastX - 0.16f + 0.04f * Mathf.Cos(ny * 13f));

                if (isRidge) { r = 0.12f; g = 0.28f; b = 0.16f; }
                else if (isLand) { r = 0.16f; g = 0.42f; b = 0.24f; }
                else
                {
                    float shelf = Mathf.Clamp01(1f - (nx - coastX) * 4.5f);
                    r = Mathf.Lerp(0.03f, 0.07f, shelf);
                    g = Mathf.Lerp(0.12f, 0.28f, shelf);
                    b = Mathf.Lerp(0.26f, 0.42f, shelf);
                }

                // Crisp yellow coastline highlight
                if (Mathf.Abs(nx - coastX) < 0.008f) { r = 0.58f; g = 0.68f; b = 0.34f; }

                // Fine map coordinate grid & concentric radar range rings centered on Santos station (0.48, 0.52)
                if ((px % 30) == 0 || (py % 24) == 0) { r += 0.04f; g += 0.10f; b += 0.12f; }
                float stationD = Mathf.Sqrt((nx - 0.48f) * (nx - 0.48f) * 1.45f + (ny - 0.52f) * (ny - 0.52f));
                if (Mathf.Abs(stationD - 0.18f) < 0.0045f || Mathf.Abs(stationD - 0.36f) < 0.0045f)
                {
                    r += 0.05f; g += 0.22f; b += 0.24f;
                }

                // Drifting storm cloud bank + multi-tier Doppler rain core
                float sx = 0.38f + 0.14f * Mathf.Sin(t * 0.6f);
                float sy = 0.56f + 0.10f * Mathf.Cos(t * 0.45f);
                float warp = 0.04f * Mathf.Sin(nx * 24f + t) * Mathf.Cos(ny * 18f - t);
                float stormD = Mathf.Sqrt((nx - sx) * (nx - sx) + (ny - sy) * (ny - sy)) + warp;
                if (stormD < 0.22f)
                {
                    float core = 1f - stormD / 0.22f;
                    if (core > 0.68f) { r += 0.85f * core; g += 0.24f * core; b += 0.08f * core; }
                    else if (core > 0.40f) { r += 0.72f * core; g += 0.78f * core; b += 0.10f * core; }
                    else { r += 0.18f * core; g += 0.62f * core; b += 0.34f * core; }
                }

                // Rotating radar sweep beam centered on Santos station (0.48, 0.52)
                float ang = Mathf.Atan2(ny - 0.52f, nx - 0.48f);
                float sweep = Mathf.Atan2(Mathf.Sin(-t * 2.1f), Mathf.Cos(-t * 2.1f));
                float diff = Mathf.Repeat(sweep - ang + Mathf.PI * 2f, Mathf.PI * 2f);
                if (diff < 0.72f)
                {
                    float beam = Mathf.Pow(1f - diff / 0.72f, 1.5f) * 0.72f;
                    r += 0.06f * beam; g += 0.96f * beam; b += 0.66f * beam;
                }

                // Weather bar bottom
                if (ny < 0.16f)
                {
                    r = 0.05f; g = 0.22f; b = 0.28f;
                    if (ny > 0.148f) { r = 0.35f; g = 0.82f; b = 0.75f; }
                }
            }
            else
            {
                // Late-night smoky music club stage (magenta & warm amber crisscrossing beams over wood stage)
                float sway1 = 0.48f + 0.28f * Mathf.Sin(t * 1.9f);
                float sway2 = 0.52f - 0.24f * Mathf.Sin(t * 1.4f + 0.8f);
                float cone1 = Mathf.Clamp01(1f - Mathf.Abs(nx - sway1) / (0.10f + (1f - ny) * 0.26f));
                float cone2 = Mathf.Clamp01(1f - Mathf.Abs(nx - sway2) / (0.09f + (1f - ny) * 0.22f));
                float haze = 0.80f + 0.20f * Mathf.Sin(nx * 14f + ny * 9f - t * 2.2f);

                r = 0.07f + cone1 * 0.78f * haze + cone2 * 0.52f;
                g = 0.03f + cone1 * 0.14f + cone2 * 0.22f;
                b = 0.12f + cone1 * 0.66f * haze + cone2 * 0.74f;

                // Wood floor stage below ny = 0.24
                if (ny < 0.24f)
                {
                    float plank = 0.82f + 0.18f * Mathf.Sin(nx * 48f);
                    r = (r * 0.55f + 0.12f) * plank;
                    g = (g * 0.45f + 0.06f) * plank;
                    b = (b * 0.50f + 0.05f) * plank;
                }

                // Singer & mic stand silhouettes + rim outline
                float pxS = nx - (0.48f + 0.015f * Mathf.Sin(t * 2.5f));
                bool head = ((pxS * pxS * 2.0f) + ((ny - 0.56f) * (ny - 0.56f) * 0.9f)) < 0.0042f;
                bool torso = ny >= 0.20f && ny < 0.48f && Mathf.Abs(pxS) < (0.068f - (ny - 0.20f) * 0.06f);
                bool micPole = Mathf.Abs(nx - 0.56f) < 0.0055f && ny >= 0.19f && ny < 0.52f;
                bool micHead = Mathf.Sqrt((nx - 0.545f) * (nx - 0.545f) + (ny - 0.525f) * (ny - 0.525f)) < 0.014f;
                if (head || torso || micPole || micHead)
                {
                    r *= 0.14f; g *= 0.14f; b *= 0.18f;
                }
            }
        }

        /// <summary>
        /// Channel 17: 1995 Warm Golden Infomercial turntable showcase + flashing promo badge & late-night wall clock.
        /// </summary>
        private void SampleChannel17Infomercial(int px, int py, float nx, float ny, float t, out float r, out float g, out float b)
        {
            float cycle = Mathf.Repeat(t, 7.2f);
            if (cycle < 4.4f)
            {
                // Pleated burgundy/gold curtain backdrop
                float drape = 0.80f + 0.20f * Mathf.Sin(nx * 52f);
                r = 0.46f * drape; g = 0.22f * drape; b = 0.08f * drape;

                // Stage pedestal spotlight
                float dist = Mathf.Sqrt((nx - 0.44f) * (nx - 0.44f) + (ny - 0.52f) * (ny - 0.52f));
                float spot = Mathf.Exp(-dist * 3.8f);
                r += spot * 0.48f; g += spot * 0.36f; b += spot * 0.16f;

                // Rotating wood display plinth
                if (ny >= 0.24f && ny < 0.32f && Mathf.Abs(nx - 0.44f) < 0.16f)
                {
                    r = 0.32f; g = 0.18f; b = 0.09f;
                    if (ny > 0.305f) { r = 0.62f; g = 0.44f; b = 0.24f; }
                }

                // Detailed rotating 90s coffee-maker/appliance silhouette on plinth
                float ax = nx - 0.44f;
                float spinScale = 0.72f + 0.28f * Mathf.Abs(Mathf.Cos(t * 1.9f));
                float bodyW = 0.095f * spinScale;
                if (ny >= 0.32f && ny < 0.66f && Mathf.Abs(ax) < bodyW)
                {
                    float cylShade = 0.55f + 0.45f * Mathf.Cos((ax / Mathf.Max(0.01f, bodyW)) * 1.3f - t * 2.8f);
                    // Glass carafe lower front vs plastic hood upper
                    if (ny < 0.50f && Mathf.Abs(ax) < bodyW * 0.72f)
                    {
                        r = 0.32f * cylShade; g = 0.36f * cylShade; b = 0.40f * cylShade;
                        if (ny < 0.42f) { r *= 0.35f; g *= 0.28f; b *= 0.22f; } // dark coffee inside carafe
                    }
                    else
                    {
                        r = 0.84f * cylShade; g = 0.82f * cylShade; b = 0.76f * cylShade;
                    }
                }

                // Flashing jagged promo starburst badge (bottom-right)
                float bx = nx - 0.77f;
                float by = ny - 0.32f;
                float bAng = Mathf.Atan2(by, bx);
                float starRad = 0.135f + 0.024f * Mathf.Sin(bAng * 12f + t * 5f);
                if (Mathf.Sqrt(bx * bx + by * by) < starRad)
                {
                    bool flash = Mathf.Sin(t * 9f) > 0f;
                    r = flash ? 0.98f : 0.90f;
                    g = flash ? 0.86f : 0.14f;
                    b = flash ? 0.12f : 0.10f;
                }

                // Scrolling 0800 order strip bottom
                if (ny < 0.15f)
                {
                    r = 0.62f; g = 0.10f; b = 0.08f;
                    if (ny > 0.138f) { r = 0.95f; g = 0.85f; b = 0.22f; }
                }
            }
            else
            {
                // Dim warm brass-rimmed station clock against dark walnut paneling
                float panelling = ((px % 30) < 2) ? 0.68f : 1f;
                r = 0.13f * panelling; g = 0.08f * panelling; b = 0.05f * panelling;

                float dx = (nx - 0.50f) * 1.333f;
                float dy = ny - 0.54f;
                float rad = Mathf.Sqrt(dx * dx + dy * dy);
                if (rad < 0.30f)
                {
                    if (rad > 0.265f)
                    {
                        // Shaded brass bezel
                        r = 0.64f; g = 0.48f; b = 0.22f;
                    }
                    else
                    {
                        float shade = 0.92f - rad * 0.7f;
                        r = 0.88f * shade; g = 0.80f * shade; b = 0.62f * shade;

                        float ang = Mathf.Atan2(dy, dx);
                        // 12 hour tick marks
                        float tickStep = Mathf.Abs(Mathf.Sin(ang * 6f));
                        if (rad > 0.21f && tickStep > 0.985f) { r = 0.16f; g = 0.12f; b = 0.10f; }

                        // Hour, Minute, and discrete ticking Second hand
                        float secAng = -Mathf.Floor(t) * (Mathf.PI * 2f / 60f) + Mathf.PI * 0.5f;
                        float minAng = -0.35f;
                        float hrAng = 2.65f;
                        if (rad < 0.23f && Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, secAng * Mathf.Rad2Deg)) < 3.5f)
                        {
                            r = 0.76f; g = 0.10f; b = 0.10f;
                        }
                        else if (rad < 0.20f && Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, minAng * Mathf.Rad2Deg)) < 5.0f)
                        {
                            r = 0.12f; g = 0.10f; b = 0.09f;
                        }
                        else if (rad < 0.13f && Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, hrAng * Mathf.Rad2Deg)) < 7.0f)
                        {
                            r = 0.12f; g = 0.10f; b = 0.09f;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Canal 420: Psychedelic liquid-light & optical drug-trip hallucination evolving into a dark
        /// bruised bad-trip vortex and an oppressive corridor/doorway silhouette in static. NEVER displays "420".
        /// (Claws, bulging eyes, and cartoon screaming faces removed.)
        /// </summary>
        private void SampleChannel420PsychedelicHorror(int px, int py, float nx, float ny, float t, out float r, out float g, out float b)
        {
            float p = hauntProgress01;
            if (t != cachedPsyT)
            {
                cachedPsyT = t;
                cachedPsyNy = float.NaN;
                psyBlobAx = 0.5f + 0.19f * Mathf.Sin(t * 1.4f);
                psyBlobAy = 0.5f + 0.16f * Mathf.Cos(t * 1.1f);
                psyBlobBx = 0.5f - 0.22f * Mathf.Cos(t * 1.7f);
                psyBlobBy = 0.5f - 0.14f * Mathf.Sin(t * 1.5f);
                psyStrobeBeat = Mathf.Repeat(t * 4.0f, 1f);
            }

            // Act III strobe cut fast-path (stark corridor doorway flash)
            if (p >= 0.76f && psyStrobeBeat > 0.88f)
            {
                r = 0.04f; g = 0.02f; b = 0.02f;
                bool openDoor = nx > 0.37f && nx < 0.63f && ny > 0.10f && ny < 0.88f;
                bool doorThreshold = nx > 0.42f && nx < 0.58f && ny > 0.12f && ny < 0.84f;
                bool doorwayShadow = Mathf.Abs(nx - 0.50f) < 0.07f && ny > 0.12f && ny < 0.68f;
                if (doorwayShadow) { r = 0.01f; g = 0.01f; b = 0.02f; }
                else if (doorThreshold) { r = 0.96f; g = 0.16f; b = 0.14f; }
                else if (openDoor) { r = 0.52f; g = 0.08f; b = 0.08f; }
                else if (ny < 0.12f) { r = 0.24f; g = 0.04f; b = 0.04f; }
                return;
            }

            if (ny != cachedPsyNy)
            {
                cachedPsyNy = ny;
                psyFigureSway = 0.50f + 0.025f * Mathf.Sin(t * 2.1f + ny * 5f);
                psyFigureWidth = 0.085f + (0.72f - ny) * 0.11f;
            }

            float dx = (nx - 0.5f) * 1.333f;
            float dy = ny - 0.5f;
            float rad = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = Mathf.Atan2(dy, dx);

            // Multi-layered domain-warped liquid plasma + psychedelic overhead-projector oil-drop interference
            float meltY = p * 0.24f * Mathf.Sin(nx * 24f - t * 3.4f) * (0.5f + 0.5f * Mathf.Cos(nx * 9f + t));
            float warpAng = ang + 0.42f * Mathf.Sin(rad * 11f - t * 2.2f);
            float warpRad = rad + meltY + 0.065f * Mathf.Sin(warpAng * 6f + t * 2.8f);

            float bax = (nx - psyBlobAx) * 3.1f;
            float bay = (ny - psyBlobAy) * 3.4f;
            float blobA = Mathf.Exp(-(bax * bax + bay * bay));
            float bbx = (nx - psyBlobBx) * 3.4f;
            float bby = (ny - psyBlobBy) * 3.0f;
            float blobB = Mathf.Exp(-(bbx * bbx + bby * bby));
            float ring = Mathf.Sin(warpRad * (22f + p * 8f) - t * (3.8f + p * 4.2f) + warpAng * 2f + (blobA - blobB) * 3.2f);
            float ribbon = Mathf.Cos(dx * 11f + Mathf.Sin(dy * 9f - t * 2.5f) * 2.2f + t * 1.9f);
            float oil = 0.5f + 0.5f * Mathf.Sin((ring + ribbon) * 2.0f + (blobA + blobB) * 2.4f + Mathf.Cos(warpAng * 5f - t * 1.7f));

            float hue = Mathf.Repeat(oil * 0.72f + t * 0.18f + rad * 0.52f + blobA * 0.14f, 1f);
            Color acid = Color.HSVToRGB(hue, 0.94f, 0.48f + 0.50f * oil);

            if (p < 0.38f)
            {
                r = acid.r; g = acid.g; b = acid.b;
                return;
            }

            if (p < 0.76f)
            {
                // Act II: Bad trip — acid colors bruise & sour; tall swaying silhouette recedes into the psychedelic haze
                float rot = Mathf.Lerp(0f, 0.82f, (p - 0.38f) / 0.38f);
                r = Mathf.Lerp(acid.r, acid.r * 0.76f + 0.24f * oil, rot);
                g = Mathf.Lerp(acid.g, acid.g * 0.28f, rot);
                b = Mathf.Lerp(acid.b, acid.b * 0.52f, rot);

                // Tall swaying doorway shadow standing deep in the psychedelic haze
                if (ny < 0.78f && Mathf.Abs(nx - psyFigureSway) < psyFigureWidth && rot > 0.25f)
                {
                    float bodyMask = 0.48f * Mathf.Clamp01((rot - 0.25f) / 0.50f);
                    r = Mathf.Lerp(r, 0.04f, bodyMask);
                    g = Mathf.Lerp(g, 0.02f, bodyMask);
                    b = Mathf.Lerp(b, 0.06f, bodyMask);
                }

                // Subtle acid-vignette darkening toward tube edges (no claws or bulging eyes)
                float edgeDark = Mathf.SmoothStep(0.38f, 0.86f, rad) * rot * 0.55f;
                r *= (1f - edgeDark);
                g *= (1f - edgeDark);
                b *= (1f - edgeDark);
                return;
            }

            // Act III: Deep psychedelic bad-trip vortex collapsing into a dark corridor doorway void (zero cartoon face/eyes)
            float vortex = Mathf.Pow(oil, 1.55f);
            float pulseWave = 0.5f + 0.5f * Mathf.Sin(t * 7.0f + rad * 11f);
            r = 0.16f + 0.62f * vortex + 0.12f * acid.r;
            g = 0.02f + 0.07f * acid.g;
            b = 0.08f + 0.36f * pulseWave + 0.14f * acid.b;

            // Perspective corridor doorway void opening in the center of the melting plasma
            float doorWidth = 0.11f + 0.04f * Mathf.Sin(t * 2.6f);
            bool inDoorVoid = Mathf.Abs(nx - 0.50f) < doorWidth && ny > 0.12f && ny < 0.86f;
            if (inDoorVoid)
            {
                float depthShade = Mathf.SmoothStep(0.0f, doorWidth, Mathf.Abs(nx - 0.50f));
                float voidMask = Mathf.Lerp(0.92f, 0.42f, depthShade);
                r = Mathf.Lerp(r, 0.02f, voidMask);
                g = Mathf.Lerp(g, 0.01f, voidMask);
                b = Mathf.Lerp(b, 0.03f, voidMask);
            }
        }

        /// <summary>
        /// Day 2 News Report (10/09/1994): "PLANTAO" emergency anchor desk, detailed night facade of the house on Rua das Acacias
        /// behind iron gate & diagonal yellow/black police tape with sweeping RED & BLUE patrol beacons, and forensic doorway flash.
        /// </summary>
        private void SampleNews1994(int px, int py, float nx, float ny, float t, out float r, out float g, out float b)
        {
            float cycle = Mathf.Repeat(t, 7.8f);
            if (cycle < 2.2f)
            {
                // Urgent red/navy emergency studio desk
                r = 0.08f; g = 0.10f; b = 0.22f;
                if (ny < 0.18f) { r = 0.74f; g = 0.08f; b = 0.08f; }
                if (ny > 0.18f && ny < 0.195f) { r = 0.92f; g = 0.88f; b = 0.82f; }

                float ax = nx - 0.58f;
                float faceR = Mathf.Sqrt((ax / 0.068f) * (ax / 0.068f) + ((ny - 0.62f) / 0.10f) * ((ny - 0.62f) / 0.10f));
                if (faceR < 1f)
                {
                    float sh = 0.76f + ax * 2.5f;
                    r = 0.74f * sh; g = 0.55f * sh; b = 0.45f * sh;
                    if (ny > 0.66f) { r = 0.10f; g = 0.09f; b = 0.11f; }
                }
                else if (Mathf.Abs(ax) < 0.135f && ny >= 0.19f && ny <= 0.51f)
                {
                    r = Mathf.Abs(ax) < 0.018f ? 0.82f : 0.11f;
                    g = Mathf.Abs(ax) < 0.018f ? 0.84f : 0.13f;
                    b = Mathf.Abs(ax) < 0.018f ? 0.88f : 0.17f;
                }

                // Shoulder photo inset showing crime-scene house silhouette
                if (nx > 0.08f && nx < 0.40f && ny > 0.42f && ny < 0.79f)
                {
                    r = 0.14f; g = 0.15f; b = 0.18f;
                    if (ny < 0.62f && Mathf.Abs(nx - 0.24f) < 0.10f) { r = 0.06f; g = 0.06f; b = 0.08f; }
                    if (Mathf.Abs((ny - 0.48f) - (nx - 0.08f) * 0.25f) < 0.02f) { r = 0.88f; g = 0.74f; b = 0.10f; }
                }
            }
            else if (cycle < 6.3f)
            {
                // Night exterior of the house behind iron gate + hedges + police car + alternating RED/BLUE sirens!
                r = 0.03f; g = 0.03f; b = 0.06f;
                if (ny < 0.27f) { r = 0.05f; g = 0.06f; b = 0.05f; } // wet street & lawn

                // Two-story house stucco body + pitched roof overhang + garage
                bool roof = ny >= 0.58f && ny <= 0.75f && Mathf.Abs(nx - 0.53f) < (0.36f - (ny - 0.58f) * 1.45f);
                bool houseBody = ny >= 0.28f && ny < 0.58f && Mathf.Abs(nx - 0.53f) < 0.29f;
                if (roof) { r = 0.08f; g = 0.07f; b = 0.08f; }
                else if (houseBody) { r = 0.14f; g = 0.14f; b = 0.16f; }

                // Open front doorway glowing dim warm hallway light & lit upstairs window
                if (nx > 0.48f && nx < 0.56f && ny >= 0.28f && ny < 0.44f)
                {
                    r = 0.58f; g = 0.49f; b = 0.30f;
                    if (Mathf.Abs(nx - 0.52f) < 0.018f && ny < 0.40f) { r *= 0.25f; g *= 0.25f; b *= 0.25f; } // officer silhouette
                }
                if (nx > 0.32f && nx < 0.41f && ny > 0.46f && ny < 0.54f)
                {
                    r = 0.45f; g = 0.38f; b = 0.22f;
                }

                // Front hedge & iron gate vertical pickets foreground
                if (ny >= 0.24f && ny < 0.32f && (nx < 0.42f || nx > 0.64f))
                {
                    r = 0.04f; g = 0.07f; b = 0.04f;
                }
                if (ny >= 0.24f && ny < 0.36f && (px % 9) < 2)
                {
                    r = 0.03f; g = 0.03f; b = 0.04f;
                }

                // Alternating RED (left patrol car) and BLUE (right patrol car) sweeping giroflex beacons!
                float redSweep = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 7.8f)), 2f);
                float blueSweep = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 7.8f + Mathf.PI)), 2f);

                float dRed = Mathf.Sqrt((nx - 0.19f) * (nx - 0.19f) + (ny - 0.29f) * (ny - 0.29f));
                float dBlue = Mathf.Sqrt((nx - 0.82f) * (nx - 0.82f) + (ny - 0.29f) * (ny - 0.29f));

                float redLight = Mathf.Exp(-dRed * 2.9f) * (0.28f + 1.05f * redSweep);
                float blueLight = Mathf.Exp(-dBlue * 2.9f) * (0.28f + 1.05f * blueSweep);
                r += redLight * 1.10f;
                g += (redLight + blueLight) * 0.08f;
                b += blueLight * 1.12f;

                // Dual diagonal yellow/black police cordon ribbons across foreground
                float tape1Y = 0.19f + nx * 0.09f;
                float tape2Y = 0.14f - nx * 0.05f;
                if (Mathf.Abs(ny - tape1Y) < 0.016f || Mathf.Abs(ny - tape2Y) < 0.014f)
                {
                    bool stripe = (((px + py) / 7) % 2) == 0;
                    if (stripe) { r = 0.94f; g = 0.78f; b = 0.08f; }
                    else { r = 0.05f; g = 0.05f; b = 0.05f; }
                }
            }
            else
            {
                // Forensic doorway camera flash illuminating open bedroom door + dark floor threshold
                float flashEnv = Mathf.Exp(-Mathf.Repeat(cycle - 6.3f, 0.75f) * 5.2f);
                bool doorway = nx > 0.35f && nx < 0.65f && ny > 0.08f && ny < 0.90f;
                bool darkRoomBeyond = nx > 0.42f && nx < 0.58f && ny > 0.14f && ny < 0.84f;
                if (darkRoomBeyond)
                {
                    r = 0.08f + 0.34f * flashEnv; g = 0.08f + 0.36f * flashEnv; b = 0.10f + 0.42f * flashEnv;
                }
                else if (doorway)
                {
                    r = 0.18f + 0.82f * flashEnv; g = 0.19f + 0.82f * flashEnv; b = 0.22f + 0.85f * flashEnv;
                }
                else
                {
                    r = 0.04f + 0.18f * flashEnv; g = 0.05f + 0.18f * flashEnv; b = 0.07f + 0.22f * flashEnv;
                }
            }
        }

        private void SampleCanal420Bleed(int px, int py, float nx, float ny, float t, out float r, out float g, out float b)
        {
            // Scanline-sliced VHS carrier hijack rather than flat alpha crossfade
            if (t != cachedBleedT)
            {
                cachedBleedT = t;
                cachedBleedNy = float.NaN;
                float bleedPulse = 0.45f + 0.55f * Mathf.Sin(t * 4.8f);
                bleedSliceThresh = Mathf.Lerp(0.75f, -0.35f, bleedPulse);
            }
            if (ny != cachedBleedNy)
            {
                cachedBleedNy = ny;
                bleedBandWave = Mathf.Sin(ny * 34f - t * 15f) + 0.55f * Mathf.Cos(ny * 78f + t * 23f);
            }

            float prevHaunt = hauntProgress01;
            hauntProgress01 = 0.89f;
            SampleChannel420PsychedelicHorror(px, py, nx, ny, t, out float hr, out float hg, out float hb);
            hauntProgress01 = prevHaunt;

            if (bleedBandWave > bleedSliceThresh)
            {
                float mix = Mathf.Clamp01((bleedBandWave - bleedSliceThresh) * 1.85f);
                if (mix >= 0.9999f)
                {
                    r = hr;
                    g = hg;
                    b = hb;
                    return;
                }

                SampleNews1994(px, py, nx, ny, t, out float nr, out float ng, out float nb);
                r = Mathf.Lerp(nr, hr, mix);
                g = Mathf.Lerp(ng, hg, mix);
                b = Mathf.Lerp(nb, hb, mix);
            }
            else
            {
                SampleNews1994(px, py, nx, ny, t, out float nr, out float ng, out float nb);
                r = nr * 0.88f + hr * 0.14f;
                g = ng * 0.72f + hg * 0.08f;
                b = nb * 0.82f + hb * 0.16f;
            }
        }

        private static void SampleShockHorrorFace(float nx, float ny, float t, out float r, out float g, out float b)
        {
            // Analog VHS tracking-tear & dark corridor doorway burst (zero cartoon eyes/face)
            float band = Mathf.Sin(ny * 48f - t * 32f);
            float tearShift = (band > 0.45f) ? 0.045f * Mathf.Sin(t * 27f + ny * 18f) : 0f;
            float sx = nx + tearShift;

            float pulse = 0.24f + 0.52f * Mathf.Abs(Mathf.Sin(t * 19f + ny * 8f));
            r = pulse;
            g = 0.03f + 0.05f * Mathf.Max(0f, band);
            b = 0.06f + 0.14f * Mathf.Abs(Mathf.Cos(t * 13f + nx * 9f));

            bool doorFrame = sx > 0.36f && sx < 0.64f && ny > 0.08f && ny < 0.90f;
            bool doorVoid = sx > 0.41f && sx < 0.59f && ny > 0.11f && ny < 0.85f;
            if (doorVoid)
            {
                r = 0.015f; g = 0.010f; b = 0.018f;
            }
            else if (doorFrame)
            {
                r = 0.78f; g = 0.16f; b = 0.18f;
            }
        }

        #region Broadcast 5x7 Pixel OSD & Tickers

        private void DrawBroadcastOSD(CRTFeedMode mode, float t)
        {
            // Channel OSD corner stamp on switches (Phosphor Green CRT text, ZERO "420" text ever)
            if (osdBannerTimer > 0f)
            {
                string osd = mode switch
                {
                    CRTFeedMode.BroadcastChannel04 => "CH 04",
                    CRTFeedMode.BroadcastChannel09 => "CH 09",
                    CRTFeedMode.BroadcastChannel17 => "CH 17",
                    CRTFeedMode.Canal420Ghost => "-- --",
                    _ => null
                };

                if (osd != null)
                {
                    FillRect(Width - 78, Height - 26, 68, 18, 0.02f, 0.08f, 0.03f, 0.72f);
                    DrawBitmapString(osd, Width - 72, Height - 22, 2, 0.32f, 0.98f, 0.42f);
                }
            }

            if (mode == CRTFeedMode.BroadcastChannel04)
            {
                float cycle = Mathf.Repeat(t, 8.4f);
                if (cycle < 3.7f || cycle >= 6.3f)
                {
                    // Pulsing red AO VIVO indicator top-left + station bug
                    if (Mathf.Sin(t * 4f) > -0.3f)
                    {
                        FillRect(12, Height - 24, 58, 14, 0.76f, 0.08f, 0.08f, 0.90f);
                        DrawBitmapString("AO VIVO", 16, Height - 21, 1, 0.98f, 0.96f, 0.96f);
                    }
                    DrawBitmapString("JORNAL", 14, 15, 1, 0.96f, 0.96f, 0.96f);
                    DrawBitmapString("NOITE", 16, 5, 1, 0.92f, 0.86f, 0.65f);

                    // Clean seamless full-width marquee across bottom-right ribbon [64..Width-8]
                    string newsTicker = "BAIXADA - CORTES NO SETOR E BALANCOS DA SEMANA   SANTOS 19C";
                    int scrollOffset = Mathf.RoundToInt(t * 36f) % 430;
                    DrawBitmapStringClipped(newsTicker, Width - 6 - scrollOffset, 9, 64, Width - 8, 1, 0.94f, 0.95f, 0.98f);
                }
            }
            else if (mode == CRTFeedMode.BroadcastChannel09 && Mathf.Repeat(t, 7.6f) < 4.0f)
            {
                DrawBitmapString("BAIXADA 19C  CHUVA E NEVOEIRO", 14, 10, 1, 0.88f, 0.98f, 0.94f);
            }
            else if (mode == CRTFeedMode.BroadcastChannel17)
            {
                float cyc = Mathf.Repeat(t, 7.2f);
                if (cyc < 4.4f)
                {
                    DrawBitmapString("SO HOJE", 164, 48, 1, 0.14f, 0.08f, 0.05f);
                    string promoTicker = "LIGUE JA 0800-1995   ULTIMAS UNIDADES";
                    int scr = Mathf.RoundToInt(t * 34f) % 390;
                    DrawBitmapStringClipped(promoTicker, Width - 8 - scr, 8, 10, Width - 10, 1, 0.98f, 0.92f, 0.62f);
                }
                else
                {
                    FillRect(74, 12, 92, 16, 0.08f, 0.06f, 0.05f, 0.80f);
                    DrawBitmapString("HORA CERTA", 86, 16, 1, 0.94f, 0.86f, 0.64f);
                }
            }
            else if (mode == CRTFeedMode.News1994Broadcast || mode == CRTFeedMode.Canal420Bleed)
            {
                FillRect(12, Height - 26, 62, 16, 0.82f, 0.06f, 0.08f, 0.92f);
                DrawBitmapString("PLANTAO", 17, Height - 22, 1, 0.98f, 0.98f, 0.98f);
                FillRect(10, 6, 120, 13, 0.04f, 0.04f, 0.05f, 0.65f);
                DrawBitmapString("10/09/94  23:42", 14, 9, 1, 0.94f, 0.94f, 0.92f);
                DrawBitmapString("R. ACACIAS", Width - 88, 9, 1, 0.96f, 0.90f, 0.68f);
            }
        }

        private void FillRect(int x0, int y0, int w, int h, float rr, float gg, float bb, float alpha)
        {
            int x1 = Mathf.Min(Width, x0 + w);
            int y1 = Mathf.Min(Height, y0 + h);
            int sx = Mathf.Max(0, x0);
            int sy = Mathf.Max(0, y0);
            for (int y = sy; y < y1; y++)
            {
                int row = y * Width;
                for (int x = sx; x < x1; x++)
                {
                    int idx = row + x;
                    rBuf[idx] = Mathf.Lerp(rBuf[idx], rr, alpha);
                    gBuf[idx] = Mathf.Lerp(gBuf[idx], gg, alpha);
                    bBuf[idx] = Mathf.Lerp(bBuf[idx], bb, alpha);
                }
            }
        }

        private void DrawBitmapString(string text, int startX, int startY, int scale, float r, float g, float b)
        {
            DrawBitmapStringClipped(text, startX, startY, 0, Width, scale, r, g, b);
        }

        private void DrawBitmapStringClipped(
            string text, int startX, int startY, int clipMinX, int clipMaxX, int scale, float r, float g, float b)
        {
            if (string.IsNullOrEmpty(text)) return;
            int advance = 4 * scale;
            for (int c = 0; c < text.Length; c++)
            {
                byte[] glyph = GetGlyph3x5(text[c]);
                int gx = startX + c * advance;
                if (glyph == null || gx + 3 * scale < clipMinX || gx >= clipMaxX)
                    continue;

                for (int gy = 0; gy < 5; gy++)
                {
                    byte bits = glyph[4 - gy];
                    for (int col = 0; col < 3; col++)
                    {
                        if (((bits >> (2 - col)) & 1) == 0) continue;
                        for (int sy = 0; sy < scale; sy++)
                        {
                            int py = startY + gy * scale + sy;
                            if (py < 0 || py >= Height) continue;
                            int row = py * Width;
                            for (int sx = 0; sx < scale; sx++)
                            {
                                int px = gx + col * scale + sx;
                                if (px < clipMinX || px >= clipMaxX || px < 0 || px >= Width) continue;
                                int idx = row + px;
                                rBuf[idx] = r;
                                gBuf[idx] = g;
                                bBuf[idx] = b;
                            }
                        }
                    }
                }
            }
        }

        private static byte[] GetGlyph3x5(char ch)
        {
            return ch switch
            {
                '0' => Glyph0,
                '1' => Glyph1,
                '2' => Glyph2,
                '3' => Glyph3,
                '4' => Glyph4,
                '5' => Glyph5,
                '6' => Glyph6,
                '7' => Glyph7,
                '8' => Glyph8,
                '9' => Glyph9,
                'A' => GlyphA,
                'B' => GlyphB,
                'C' => GlyphC,
                'D' => GlyphD,
                'E' => GlyphE,
                'F' => GlyphF,
                'G' => GlyphG,
                'H' => GlyphH,
                'I' => GlyphI,
                'J' => GlyphJ,
                'K' => GlyphK,
                'L' => GlyphL,
                'M' => GlyphM,
                'N' => GlyphN,
                'O' => GlyphO,
                'P' => GlyphP,
                'Q' => GlyphQ,
                'R' => GlyphR,
                'S' => GlyphS,
                'T' => GlyphT,
                'U' => GlyphU,
                'V' => GlyphV,
                'W' => GlyphW,
                'X' => GlyphX,
                'Z' => GlyphZ,
                '.' => GlyphDot,
                '-' => GlyphDash,
                '/' => GlyphSlash,
                ':' => GlyphColon,
                _ => null
            };
        }

        // Compact 3x5 bitmap glyphs (never containing any "420" badge/draw logic)
        private static readonly byte[] Glyph0 = { 7, 5, 5, 5, 7 };
        private static readonly byte[] Glyph1 = { 2, 6, 2, 2, 7 };
        private static readonly byte[] Glyph2 = { 7, 1, 7, 4, 7 };
        private static readonly byte[] Glyph3 = { 7, 1, 7, 1, 7 };
        private static readonly byte[] Glyph4 = { 5, 5, 7, 1, 1 };
        private static readonly byte[] Glyph5 = { 7, 4, 7, 1, 7 };
        private static readonly byte[] Glyph6 = { 7, 4, 7, 5, 7 };
        private static readonly byte[] Glyph7 = { 7, 1, 2, 2, 2 };
        private static readonly byte[] Glyph8 = { 7, 5, 7, 5, 7 };
        private static readonly byte[] Glyph9 = { 7, 5, 7, 1, 7 };
        private static readonly byte[] GlyphA = { 2, 5, 7, 5, 5 };
        private static readonly byte[] GlyphB = { 6, 5, 6, 5, 6 };
        private static readonly byte[] GlyphC = { 7, 4, 4, 4, 7 };
        private static readonly byte[] GlyphD = { 6, 5, 5, 5, 6 };
        private static readonly byte[] GlyphE = { 7, 4, 6, 4, 7 };
        private static readonly byte[] GlyphF = { 7, 4, 6, 4, 4 };
        private static readonly byte[] GlyphG = { 7, 4, 5, 5, 7 };
        private static readonly byte[] GlyphH = { 5, 5, 7, 5, 5 };
        private static readonly byte[] GlyphI = { 7, 2, 2, 2, 7 };
        private static readonly byte[] GlyphJ = { 1, 1, 1, 5, 7 };
        private static readonly byte[] GlyphK = { 5, 5, 6, 5, 5 };
        private static readonly byte[] GlyphL = { 4, 4, 4, 4, 7 };
        private static readonly byte[] GlyphM = { 5, 7, 5, 5, 5 };
        private static readonly byte[] GlyphN = { 5, 6, 5, 3, 5 };
        private static readonly byte[] GlyphO = { 7, 5, 5, 5, 7 };
        private static readonly byte[] GlyphP = { 7, 5, 7, 4, 4 };
        private static readonly byte[] GlyphQ = { 7, 5, 5, 7, 1 };
        private static readonly byte[] GlyphR = { 6, 5, 6, 5, 5 };
        private static readonly byte[] GlyphS = { 7, 4, 7, 1, 7 };
        private static readonly byte[] GlyphT = { 7, 2, 2, 2, 2 };
        private static readonly byte[] GlyphU = { 5, 5, 5, 5, 7 };
        private static readonly byte[] GlyphV = { 5, 5, 5, 5, 2 };
        private static readonly byte[] GlyphW = { 5, 5, 5, 7, 5 };
        private static readonly byte[] GlyphX = { 5, 5, 2, 5, 5 };
        private static readonly byte[] GlyphZ = { 7, 1, 2, 4, 7 };
        private static readonly byte[] GlyphDot = { 0, 0, 0, 0, 2 };
        private static readonly byte[] GlyphDash = { 0, 0, 7, 0, 0 };
        private static readonly byte[] GlyphSlash = { 1, 1, 2, 4, 4 };
        private static readonly byte[] GlyphColon = { 0, 2, 0, 2, 0 };

        #endregion
    }
}
