using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Canal420.Player;

namespace Canal420.Effects
{
    public enum PsychologicalState
    {
        Normal_Clear = 0,
        Day1_Burnout = 1,
        Day2_Depressant = 2,
        Day3_Hallucinogen = 3,
        Day4_MelancholicPrelude = 4
    }

    public class SanityEffects : MonoBehaviour
    {
        public static SanityEffects Instance { get; private set; }

        [Header("Referência do URP Global Volume")]
        [Tooltip("Arraste o Global Volume da cena. Se vazio, localiza automaticamente no Awake.")]
        [SerializeField] private Volume globalVolume;

        [Header("Estado Psicológico Atual (Teste em Tempo Real no Inspector)")]
        [SerializeField] private PsychologicalState currentState = PsychologicalState.Day1_Burnout;
        [Tooltip("Velocidade de transição suave ao trocar de estado entre os dias.")]
        [SerializeField] private float stateTransitionSpeed = 2.0f;

        [Header("Dia 1: Burnout (Natural, Sombrio e Nítido)")]
        [SerializeField] private float burnoutSaturation = -14f;
        [SerializeField] private Color burnoutColorFilter = new Color(0.992f, 0.988f, 0.982f, 1f);
        [SerializeField] private float burnoutExposure = 0.02f;
        [SerializeField] private float burnoutVignette = 0.26f;
        [SerializeField] private float burnoutContrast = 9f;
        [SerializeField] private float burnoutFilmGrain = 0.17f;
        [SerializeField] private float burnoutShadowLift = 0.0012f;
        [SerializeField] private float burnoutBloom = 0.14f;

        [Header("Dia 2: Substâncias Depressoras (Penumbra Opressiva Natural)")]
        [SerializeField] private float depressantSaturation = -22f;
        [SerializeField] private Color depressantColorFilter = new Color(0.972f, 0.974f, 0.978f, 1f);
        [SerializeField] private float depressantExposure = -0.02f;
        [SerializeField] private float depressantVignette = 0.35f;
        [SerializeField] private float depressantBaseChromatic = 0.07f;
        [SerializeField] private float depressantContrast = 11f;
        [SerializeField] private float depressantFilmGrain = 0.24f;
        [SerializeField] private float depressantShadowLift = 0.0016f;
        [SerializeField] private float depressantBloom = 0.16f;

        [Header("Dia 3: Apagão e Substâncias Perturbadoras (Oscilação com Mathf.Sin)")]
        [SerializeField] private float hallucinogenSaturation = -31f;
        [SerializeField] private Color hallucinogenColorFilter = new Color(0.946f, 0.948f, 0.958f, 1f);
        [SerializeField] private float hallucinogenExposure = -0.05f;
        [SerializeField] private float hallucinogenVignette = 0.42f;
        [SerializeField] private float hallucinogenContrast = 13.5f;
        [SerializeField] private float hallucinogenFilmGrain = 0.30f;
        [SerializeField] private float hallucinogenShadowLift = 0.0018f;
        [SerializeField] private float hallucinogenBloom = 0.16f;
        [Tooltip("Frequência da onda senoidal de Chromatic Aberration.")]
        [SerializeField] private float chromaticWaveFrequency = 1.55f;
        [Tooltip("Intensidade mínima e máxima da Chromatic Aberration no Dia 3 (0 a 1).")]
        [SerializeField] private Vector2 chromaticMinMax = new Vector2(0.18f, 0.72f);
        [Tooltip("Frequência da onda senoidal de Lens Distortion.")]
        [SerializeField] private float lensWaveFrequency = 1.15f;
        [Tooltip("Intensidade mínima e máxima da Lens Distortion no Dia 3 (-1 a 1).")]
        [SerializeField] private Vector2 lensDistortionMinMax = new Vector2(-0.30f, 0.12f);

        [Header("Dia 4: Prelúdio Melancólico (Quarto das Filhas)")]
        [SerializeField] private float preludeSaturation = -28f;
        [SerializeField] private Color preludeColorFilter = new Color(0.958f, 0.962f, 0.968f, 1f);
        [SerializeField] private float preludeExposure = -0.01f;
        [SerializeField] private float preludeVignette = 0.34f;
        [SerializeField] private float preludeContrast = 11f;
        [SerializeField] private float preludeFilmGrain = 0.20f;
        [SerializeField] private float preludeShadowLift = 0.0014f;
        [SerializeField] private float preludeBloom = 0.14f;
        private ChromaticAberration chromaticAberration;
        private LensDistortion lensDistortion;
        private Vignette vignette;
        private ColorAdjustments colorAdjustments;
        private FilmGrain filmGrain;
        private LiftGammaGain liftGammaGain;
        private ShadowsMidtonesHighlights shadowsMidtonesHighlights;
        private Bloom bloom;
        private Tonemapping tonemapping;

        private float currentSaturation;
        private Color currentColorFilter = Color.white;
        private float currentExposure;
        private float currentVignette;
        private float currentContrast;
        private float currentFilmGrain;
        private float currentShadowLift = 0.0012f;
        private float currentBloom = 0.14f;
        private float hallucinogenBlend;
        private float baselineChromatic;

        private float userBrightnessNormalized = 0.5f;
        private float userExposureOffsetEV = 0f;
        private float qualityFilmGrainScale = 1f;
        private bool brightShadowsEnabled = false;

        private float spikeWeight;
        private float spikeDecayRate = 1.5f;
        private float targetProximityDread;
        private float currentProximityDread;
        private float day3TensionEscalation;
        private float stimulantStabilization01;
        private float chemicalRebound01;
        private float chemicalReboundPulse01;
        private float lastSubliminalFlashTime = -10f;
        private PlayerController cachedPlayer;

        private float waveTime;
        private bool volumeDirty = true;

        public PsychologicalState CurrentState => currentState;
        public float CurrentProximityDread => currentProximityDread;
        public float CurrentChemicalRebound01 => chemicalRebound01;

        private void Awake()
        {
            Instance = this;
            HorrorScreenOverlay.EnsureExists();
            cachedPlayer = FindAnyObjectByType<PlayerController>();

            if (globalVolume == null)
            {
                globalVolume = FindAnyObjectByType<Volume>();
            }

            if (globalVolume == null)
            {
                Debug.LogError("[SanityEffects] Nenhum componente 'Volume' (Global Volume) encontrado na cena!");
                enabled = false;
                return;
            }

            EnsureCameraPostProcessingEnabled();
            InitializeVolumeComponents();
            SetUserBrightnessNormalized(PlayerPrefs.GetFloat("Canal420_Brightness", 0.50f), applyImmediately: false);
            SetBrightShadowsEnabled(PlayerPrefs.GetInt("Canal420_BrightShadows", 0) == 1, applyImmediately: false);
            ApplyTargetParametersImmediate(currentState);
        }

        public static void EnsureCameraPostProcessingEnabled()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = FindAnyObjectByType<Camera>();
            }
            if (cam == null) return;

            UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
            {
                camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            camData.renderPostProcessing = true;
            camData.volumeLayerMask = ~0;
        }

        private VolumeProfile runtimeClonedProfile;
        private bool playerLookupAttempted = false;

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            if (runtimeClonedProfile != null)
            {
                if (Application.isPlaying) Destroy(runtimeClonedProfile);
                else DestroyImmediate(runtimeClonedProfile);
                runtimeClonedProfile = null;
            }
        }

        private void InitializeVolumeComponents()
        {
            VolumeProfile sharedBeforeClone = globalVolume.sharedProfile;
            VolumeProfile runtimeProfile = globalVolume.profile;
            if (runtimeProfile != null && runtimeProfile != sharedBeforeClone)
            {
                runtimeClonedProfile = runtimeProfile;
            }
            if (runtimeProfile != null && runtimeProfile.components != null)
            {
                runtimeProfile.components.RemoveAll(c => c == null);
            }

            if (!runtimeProfile.TryGet(out chromaticAberration))
            {
                chromaticAberration = runtimeProfile.Add<ChromaticAberration>(true);
            }
            chromaticAberration.active = true;

            if (!runtimeProfile.TryGet(out lensDistortion))
            {
                lensDistortion = runtimeProfile.Add<LensDistortion>(true);
            }
            lensDistortion.active = true;
            lensDistortion.scale.Override(1.0f);

            if (!runtimeProfile.TryGet(out vignette))
            {
                vignette = runtimeProfile.Add<Vignette>(true);
            }
            vignette.active = true;
            vignette.smoothness.Override(0.52f);
            vignette.rounded.Override(false);

            if (!runtimeProfile.TryGet(out colorAdjustments))
            {
                colorAdjustments = runtimeProfile.Add<ColorAdjustments>(true);
            }
            colorAdjustments.active = true;

            if (!runtimeProfile.TryGet(out filmGrain))
            {
                filmGrain = runtimeProfile.Add<FilmGrain>(true);
            }
            filmGrain.active = true;
            filmGrain.type.Override(FilmGrainLookup.Medium3);
            filmGrain.response.Override(0.76f);

            if (!runtimeProfile.TryGet(out liftGammaGain))
            {
                liftGammaGain = runtimeProfile.Add<LiftGammaGain>(true);
            }
            liftGammaGain.active = true;
            liftGammaGain.gamma.Override(new Vector4(1f, 1f, 1f, 0f));
            liftGammaGain.gain.Override(new Vector4(1f, 1f, 1f, 0f));

            if (!runtimeProfile.TryGet(out shadowsMidtonesHighlights))
            {
                shadowsMidtonesHighlights = runtimeProfile.Add<ShadowsMidtonesHighlights>(true);
            }
            shadowsMidtonesHighlights.active = true;
            shadowsMidtonesHighlights.shadowsStart.Override(0f);
            shadowsMidtonesHighlights.shadowsEnd.Override(0.26f);
            shadowsMidtonesHighlights.highlightsStart.Override(0.55f);
            shadowsMidtonesHighlights.highlightsEnd.Override(1f);
            shadowsMidtonesHighlights.shadows.Override(new Vector4(1f, 1f, 1f, 0f));
            shadowsMidtonesHighlights.midtones.Override(new Vector4(1f, 1f, 1f, 0f));
            shadowsMidtonesHighlights.highlights.Override(new Vector4(1f, 1f, 1f, 0f));

            if (!runtimeProfile.TryGet(out bloom))
            {
                bloom = runtimeProfile.Add<Bloom>(true);
            }
            bloom.active = true;
            bloom.threshold.Override(1.18f);
            bloom.scatter.Override(0.42f);

            if (!runtimeProfile.TryGet(out tonemapping))
            {
                tonemapping = runtimeProfile.Add<Tonemapping>(true);
            }
            tonemapping.active = true;
            tonemapping.mode.Override(TonemappingMode.Neutral);
        }

        private void Update()
        {
            bool hasActiveSpike = spikeWeight > 0f;
            bool hasDread = currentProximityDread > 0.0005f || Mathf.Abs(currentProximityDread - targetProximityDread) > 0.0005f;
            bool hasChemicalMod = stimulantStabilization01 > 0.0005f || chemicalRebound01 > 0.0005f;
            bool isHallucinogenWaveActive = currentState == PsychologicalState.Day3_Hallucinogen || hallucinogenBlend > 0.0005f;

            if (!volumeDirty && !hasActiveSpike && !hasDread && !hasChemicalMod && !isHallucinogenWaveActive)
            {
                return;
            }

            if (Time.deltaTime <= 0f && !volumeDirty)
            {
                return;
            }

            waveTime += Time.deltaTime;

            if (spikeWeight > 0f)
            {
                spikeWeight = Mathf.MoveTowards(spikeWeight, 0f, spikeDecayRate * Time.deltaTime);
                if (spikeWeight <= 0.0005f) spikeWeight = 0f;
            }

            if (hasDread)
            {
                currentProximityDread = Mathf.MoveTowards(currentProximityDread, targetProximityDread, Time.deltaTime * 1.6f);
                if (Mathf.Abs(currentProximityDread - targetProximityDread) <= 0.0005f)
                {
                    currentProximityDread = targetProximityDread;
                }
            }

            GetTargetValuesForState(
                currentState,
                out float targetSat,
                out Color targetColor,
                out float targetExp,
                out float targetVig,
                out float targetContrast,
                out float targetGrain,
                out float targetBaseChrom,
                out float targetHallucinogenWeight,
                out float targetLift,
                out float targetBloom
            );

            float dt = Time.deltaTime * stateTransitionSpeed;
            currentSaturation = Mathf.Lerp(currentSaturation, targetSat, dt);
            currentColorFilter = Color.Lerp(currentColorFilter, targetColor, dt);
            currentExposure = Mathf.Lerp(currentExposure, targetExp, dt);
            currentVignette = Mathf.Lerp(currentVignette, targetVig, dt);
            currentContrast = Mathf.Lerp(currentContrast, targetContrast, dt);
            currentFilmGrain = Mathf.Lerp(currentFilmGrain, targetGrain, dt);
            currentShadowLift = Mathf.Lerp(currentShadowLift, targetLift, dt);
            currentBloom = Mathf.Lerp(currentBloom, targetBloom, dt);
            baselineChromatic = Mathf.Lerp(baselineChromatic, targetBaseChrom, dt);
            hallucinogenBlend = Mathf.Lerp(hallucinogenBlend, targetHallucinogenWeight, dt);

            bool transitionSettled =
                Mathf.Abs(currentSaturation - targetSat) <= 0.01f &&
                Mathf.Abs(currentColorFilter.r - targetColor.r) <= 0.0005f &&
                Mathf.Abs(currentColorFilter.g - targetColor.g) <= 0.0005f &&
                Mathf.Abs(currentColorFilter.b - targetColor.b) <= 0.0005f &&
                Mathf.Abs(currentExposure - targetExp) <= 0.0005f &&
                Mathf.Abs(currentVignette - targetVig) <= 0.0005f &&
                Mathf.Abs(currentContrast - targetContrast) <= 0.01f &&
                Mathf.Abs(currentFilmGrain - targetGrain) <= 0.0005f &&
                Mathf.Abs(currentShadowLift - targetLift) <= 0.0001f &&
                Mathf.Abs(currentBloom - targetBloom) <= 0.0005f &&
                Mathf.Abs(baselineChromatic - targetBaseChrom) <= 0.0005f &&
                Mathf.Abs(hallucinogenBlend - targetHallucinogenWeight) <= 0.0005f;

            if (transitionSettled)
            {
                currentSaturation = targetSat;
                currentColorFilter = targetColor;
                currentExposure = targetExp;
                currentVignette = targetVig;
                currentContrast = targetContrast;
                currentFilmGrain = targetGrain;
                currentShadowLift = targetLift;
                currentBloom = targetBloom;
                baselineChromatic = targetBaseChrom;
                hallucinogenBlend = targetHallucinogenWeight;
            }

            ApplyEffectsToVolume();

            volumeDirty = !transitionSettled
                || spikeWeight > 0f
                || currentProximityDread > 0.0005f
                || Mathf.Abs(currentProximityDread - targetProximityDread) > 0.0005f
                || stimulantStabilization01 > 0.0005f
                || chemicalRebound01 > 0.0005f
                || currentState == PsychologicalState.Day3_Hallucinogen
                || hallucinogenBlend > 0.0005f;
        }

        private void ApplyEffectsToVolume()
        {
            float sinChromatic01 = (Mathf.Sin(waveTime * chromaticWaveFrequency) + 1f) * 0.5f;
            float waveChromatic = Mathf.Lerp(chromaticMinMax.x, chromaticMinMax.y, sinChromatic01);

            float sinLensRaw = (Mathf.Sin(waveTime * lensWaveFrequency) * 0.75f) +
                               (Mathf.Sin(waveTime * (lensWaveFrequency * 2.3f)) * 0.25f);
            float sinLens01 = (sinLensRaw + 1f) * 0.5f;
            float waveLens = Mathf.Lerp(lensDistortionMinMax.x, lensDistortionMinMax.y, sinLens01);

            float stabCalm = Mathf.Clamp01(stimulantStabilization01);
            float reb = Mathf.Clamp01(chemicalRebound01);
            float rebPulse = Mathf.Clamp01(chemicalReboundPulse01);

            float dampedWaveChrom = Mathf.Lerp(waveChromatic, chromaticMinMax.x * 0.52f, stabCalm * 0.76f);
            float dampedWaveLens = Mathf.Lerp(waveLens, waveLens * 0.22f, stabCalm);

            float finalChromatic = Mathf.Lerp(baselineChromatic, dampedWaveChrom, hallucinogenBlend);
            float finalLens = Mathf.Lerp(0f, dampedWaveLens, hallucinogenBlend);

            if (reb > 0.001f)
            {
                finalChromatic += reb * (0.08f + 0.11f * rebPulse);
                finalLens -= reb * (0.055f + 0.045f * rebPulse);
            }

            if (currentProximityDread > 0.001f)
            {
                // Keep corridor perspective completely stable across all days (no fish-eye lens warp in corridors)
                finalChromatic += currentProximityDread * 0.14f;
            }

            if (spikeWeight > 0f)
            {
                float spikePulse = Mathf.Sin(waveTime * 28f) * 0.05f * spikeWeight;
                finalChromatic = Mathf.Clamp(finalChromatic + (spikeWeight * 0.46f), 0f, 0.78f);
                finalLens = Mathf.Clamp(finalLens - (0.18f * spikeWeight) + spikePulse, -0.35f, 0.20f);
            }

            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.Override(Mathf.Clamp(finalChromatic, 0f, 0.78f));
            }

            if (lensDistortion != null)
            {
                float clampedLens = Mathf.Clamp(finalLens, -0.35f, 0.20f);
                lensDistortion.intensity.Override(clampedLens);
                float safeScale = Mathf.Abs(clampedLens) > 0.005f
                    ? (clampedLens > 0f ? Mathf.Lerp(1.0f, 1.08f, Mathf.Clamp01(clampedLens / 0.25f)) : 1.01f)
                    : 1.0f;
                lensDistortion.scale.Override(safeScale);
            }

            if (vignette != null)
            {
                float brightnessVigRelief = Mathf.Max(0f, userBrightnessNormalized - 0.5f) * 0.24f;
                float brightShadowVigRelief = brightShadowsEnabled ? 0.11f : 0f;
                float chemVigDelta = -(stabCalm * 0.035f) + (reb * (0.045f + 0.065f * rebPulse));
                float finalVig = Mathf.Clamp(currentVignette - brightnessVigRelief - brightShadowVigRelief + chemVigDelta + (currentProximityDread * 0.10f) + (spikeWeight * 0.18f), 0.10f, 0.56f);
                vignette.intensity.Override(finalVig);
            }

            if (colorAdjustments != null)
            {
                Color spikeTint = Color.Lerp(currentColorFilter, new Color(0.92f, 0.915f, 0.91f, 1f), spikeWeight * 0.38f);
                colorAdjustments.saturation.Override(currentSaturation + (stabCalm * 4f) - (reb * 5f) - (currentProximityDread * 8f) - (spikeWeight * 10f));
                colorAdjustments.colorFilter.Override(spikeTint);
                colorAdjustments.postExposure.Override(currentExposure + userExposureOffsetEV - (reb * 0.025f * rebPulse) - (currentProximityDread * 0.04f));
                float contrastScale = brightShadowsEnabled ? 0.72f : 1f;
                colorAdjustments.contrast.Override(Mathf.Clamp((currentContrast * contrastScale) + (stabCalm * 1.2f) + (spikeWeight * 5f), 0f, 18f));
            }

            if (liftGammaGain != null)
            {
                float brightnessLiftRelief = (userBrightnessNormalized - 0.5f) * 0.0020f;
                float brightShadowLift = brightShadowsEnabled ? 0.0038f : 0f;
                float maxLift = brightShadowsEnabled ? 0.0085f : 0.0050f;
                float safeLift = Mathf.Clamp(currentShadowLift + brightnessLiftRelief + brightShadowLift + (spikeWeight * 0.0008f), 0.0004f, maxLift);
                liftGammaGain.lift.Override(new Vector4(1f, 1f, 1f, safeLift));
                liftGammaGain.gamma.Override(new Vector4(1f, 1f, 1f, -0.01f * spikeWeight));
                liftGammaGain.gain.Override(new Vector4(1f, 1f, 1f, 0f));
            }

            if (shadowsMidtonesHighlights != null)
            {
                shadowsMidtonesHighlights.shadowsStart.Override(0f);
                shadowsMidtonesHighlights.shadowsEnd.Override(0.26f);
                shadowsMidtonesHighlights.highlightsStart.Override(0.55f);
                shadowsMidtonesHighlights.highlightsEnd.Override(1f);
                shadowsMidtonesHighlights.shadows.Override(brightShadowsEnabled
                    ? new Vector4(1f, 1f, 1f, 0.30f)
                    : new Vector4(1f, 1f, 1f, 0f));
                shadowsMidtonesHighlights.midtones.Override(new Vector4(1f, 1f, 1f, 0f));
                shadowsMidtonesHighlights.highlights.Override(new Vector4(1f, 1f, 1f, 0f));
            }

            if (bloom != null)
            {
                bloom.intensity.Override(Mathf.Clamp(currentBloom + (spikeWeight * 0.10f), 0.08f, 0.38f));
            }

            if (filmGrain != null)
            {
                float chemGrainDelta = -(stabCalm * 0.08f) + (reb * (0.06f + 0.13f * rebPulse));
                float finalGrain = Mathf.Clamp01((currentFilmGrain + chemGrainDelta + (currentProximityDread * 0.15f) + (spikeWeight * 0.24f)) * qualityFilmGrainScale);
                filmGrain.active = qualityFilmGrainScale > 0.01f;
                filmGrain.intensity.Override(finalGrain);
            }

            if (cachedPlayer == null && !playerLookupAttempted)
            {
                playerLookupAttempted = true;
                cachedPlayer = FindAnyObjectByType<PlayerController>();
            }
            if (cachedPlayer != null)
            {
                // Never warp FOV or tilt camera perspective just from walking in the corridor on any day.
                // Only subtle transient spike impacts may momentarily nudge camera roll/FOV.
                float spikeRoll = Mathf.Sin(waveTime * 10.5f) * (spikeWeight * 0.85f);
                float totalRoll = Mathf.Clamp(spikeRoll, -1.6f, 1.6f);
                float spikeFov = -(spikeWeight * 2.2f);
                cachedPlayer.SetHorrorCameraInfluence(totalRoll, spikeFov);
            }

            if (HorrorScreenOverlay.Instance != null)
            {
                float reboundEdge = reb * (0.30f + 0.38f * rebPulse);
                HorrorScreenOverlay.Instance.SetPanicWeight(Mathf.Clamp01(currentProximityDread * 0.90f + spikeWeight * 0.52f + reboundEdge));
            }
        }

        private void GetTargetValuesForState(
            PsychologicalState state,
            out float sat,
            out Color filter,
            out float exposure,
            out float vig,
            out float contrast,
            out float grain,
            out float baseChrom,
            out float hallWeight,
            out float shadowLift,
            out float bloomIntensity)
        {
            switch (state)
            {
                case PsychologicalState.Day1_Burnout:
                    sat = burnoutSaturation;
                    filter = burnoutColorFilter;
                    exposure = burnoutExposure;
                    vig = burnoutVignette;
                    contrast = burnoutContrast;
                    grain = burnoutFilmGrain;
                    baseChrom = 0.04f;
                    hallWeight = 0f;
                    shadowLift = burnoutShadowLift;
                    bloomIntensity = burnoutBloom;
                    break;

                case PsychologicalState.Day2_Depressant:
                    sat = depressantSaturation;
                    filter = depressantColorFilter;
                    exposure = depressantExposure;
                    vig = depressantVignette;
                    contrast = depressantContrast;
                    grain = depressantFilmGrain;
                    baseChrom = depressantBaseChromatic;
                    hallWeight = 0f;
                    shadowLift = depressantShadowLift;
                    bloomIntensity = depressantBloom;
                    break;

                case PsychologicalState.Day3_Hallucinogen:
                    float esc = Mathf.Clamp01(day3TensionEscalation);
                    sat = hallucinogenSaturation - (esc * 11f);
                    filter = Color.Lerp(hallucinogenColorFilter, new Color(0.916f, 0.922f, 0.935f, 1f), esc * 0.65f);
                    exposure = hallucinogenExposure - (esc * 0.025f);
                    vig = Mathf.Clamp(hallucinogenVignette + (esc * 0.055f), 0.36f, 0.48f);
                    contrast = hallucinogenContrast + (esc * 2.0f);
                    grain = Mathf.Clamp01(hallucinogenFilmGrain + (esc * 0.06f));
                    baseChrom = chromaticMinMax.x + (esc * 0.10f);
                    hallWeight = 1f;
                    // Preserve shadow floor so navigable silhouettes remain visible even at maximum escalation
                    shadowLift = Mathf.Max(0.0017f, hallucinogenShadowLift);
                    bloomIntensity = hallucinogenBloom;
                    break;

                case PsychologicalState.Day4_MelancholicPrelude:
                    sat = preludeSaturation;
                    filter = preludeColorFilter;
                    exposure = preludeExposure;
                    vig = preludeVignette;
                    contrast = preludeContrast;
                    grain = preludeFilmGrain;
                    baseChrom = 0.08f;
                    hallWeight = 0f;
                    shadowLift = preludeShadowLift;
                    bloomIntensity = preludeBloom;
                    break;

                default:
                    sat = 0f;
                    filter = Color.white;
                    exposure = 0f;
                    vig = 0.22f;
                    contrast = 6f;
                    grain = 0.14f;
                    baseChrom = 0f;
                    hallWeight = 0f;
                    shadowLift = 0.0045f;
                    bloomIntensity = 0.20f;
                    break;
            }
        }

        private void ApplyTargetParametersImmediate(PsychologicalState state)
        {
            GetTargetValuesForState(
                state,
                out currentSaturation,
                out currentColorFilter,
                out currentExposure,
                out currentVignette,
                out currentContrast,
                out currentFilmGrain,
                out baselineChromatic,
                out hallucinogenBlend,
                out currentShadowLift,
                out currentBloom
            );
            ApplyEffectsToVolume();
        }

        public void ClearSpikesImmediate()
        {
            spikeWeight = 0f;
            spikeDecayRate = 1.5f;
            targetProximityDread = 0f;
            currentProximityDread = 0f;
            stimulantStabilization01 = 0f;
            chemicalRebound01 = 0f;
            chemicalReboundPulse01 = 0f;
            lastSubliminalFlashTime = Time.unscaledTime;
            if (HorrorScreenOverlay.Instance != null)
            {
                HorrorScreenOverlay.Instance.ClearImmediate();
            }
            ApplyEffectsToVolume();
            volumeDirty = currentState == PsychologicalState.Day3_Hallucinogen || hallucinogenBlend > 0.0005f;
        }

        public void SetStimulantModulation(float stabilization01, float rebound01, float pulse01)
        {
            float s = Mathf.Clamp01(stabilization01);
            float r = Mathf.Clamp01(rebound01);
            float p = Mathf.Clamp01(pulse01);
            if (Mathf.Abs(s - stimulantStabilization01) > 0.0005f
                || Mathf.Abs(r - chemicalRebound01) > 0.0005f
                || Mathf.Abs(p - chemicalReboundPulse01) > 0.0005f)
            {
                stimulantStabilization01 = s;
                chemicalRebound01 = r;
                chemicalReboundPulse01 = p;
                volumeDirty = true;
            }
        }

        public void ClearStimulantModulationImmediate()
        {
            stimulantStabilization01 = 0f;
            chemicalRebound01 = 0f;
            chemicalReboundPulse01 = 0f;
            volumeDirty = true;
            ApplyEffectsToVolume();
        }

        public void SetPsychologicalState(PsychologicalState newState, bool immediate = false)
        {
            currentState = newState;
            if (newState != PsychologicalState.Day3_Hallucinogen)
            {
                day3TensionEscalation = 0f;
                stimulantStabilization01 = 0f;
                chemicalRebound01 = 0f;
                chemicalReboundPulse01 = 0f;
            }
            volumeDirty = true;
            EnsureCameraPostProcessingEnabled();
            if (immediate)
            {
                spikeWeight = 0f;
                spikeDecayRate = 1.5f;
                targetProximityDread = 0f;
                currentProximityDread = 0f;
                stimulantStabilization01 = 0f;
                chemicalRebound01 = 0f;
                chemicalReboundPulse01 = 0f;
                lastSubliminalFlashTime = Time.unscaledTime;
                if (HorrorScreenOverlay.Instance != null)
                {
                    HorrorScreenOverlay.Instance.ClearImmediate();
                }
                ApplyTargetParametersImmediate(newState);
                volumeDirty = newState == PsychologicalState.Day3_Hallucinogen;
            }
        }

        public void SetDay3TensionEscalation(float escalation01)
        {
            float clamped = Mathf.Clamp01(escalation01);
            if (Mathf.Abs(clamped - day3TensionEscalation) > 0.001f)
            {
                day3TensionEscalation = clamped;
                volumeDirty = true;
            }
        }

        public void SetProximityDread(float dread01)
        {
            float clamped = Mathf.Clamp01(dread01);
            if (Mathf.Abs(clamped - targetProximityDread) > 0.0001f || (clamped == 0f && targetProximityDread != 0f))
            {
                targetProximityDread = clamped;
                volumeDirty = true;
            }
        }

        public void SetUserBrightnessNormalized(float normalized01, bool applyImmediately = true)
        {
            userBrightnessNormalized = Mathf.Clamp(normalized01, 0f, 1.00f);
            if (userBrightnessNormalized < 0.5f)
            {
                userExposureOffsetEV = Mathf.Lerp(-0.80f, 0f, userBrightnessNormalized / 0.5f);
            }
            else
            {
                userExposureOffsetEV = Mathf.Lerp(0f, 1.85f, (userBrightnessNormalized - 0.5f) / 0.5f);
            }

            if (applyImmediately)
            {
                ApplyEffectsToVolume();
            }
            else
            {
                volumeDirty = true;
            }
        }

        public void SetBrightShadowsEnabled(bool enabled, bool applyImmediately = true)
        {
            brightShadowsEnabled = enabled;
            if (applyImmediately)
            {
                ApplyEffectsToVolume();
            }
            else
            {
                volumeDirty = true;
            }
        }

        public void SetFilmGrainQualityScale(float scale01)
        {
            qualityFilmGrainScale = Mathf.Clamp01(scale01);
            ApplyEffectsToVolume();
        }

        public void TriggerHallucinationSpike(float intensity = 1.0f, float duration = 0.9f)
        {
            TriggerHallucinationSpike(intensity, duration, allowSubliminalFlash: true, SubliminalType.StaticTear);
        }

        public void TriggerHallucinationSpike(float intensity, float duration, bool allowSubliminalFlash, SubliminalType flashType = SubliminalType.StaticTear)
        {
            spikeWeight = Mathf.Clamp01(Mathf.Max(spikeWeight, intensity));
            spikeDecayRate = duration > 0.05f ? (1f / duration) : 10f;
            volumeDirty = true;

            bool qualifiesForFlash = intensity >= 0.84f || (flashType != SubliminalType.StaticTear && intensity >= 0.40f);
            float cooldown = flashType != SubliminalType.StaticTear ? 1.5f : 4.5f;
            if (allowSubliminalFlash && qualifiesForFlash && (Time.unscaledTime - lastSubliminalFlashTime) >= cooldown)
            {
                lastSubliminalFlashTime = Time.unscaledTime;
                HorrorScreenOverlay overlay = HorrorScreenOverlay.Instance != null
                    ? HorrorScreenOverlay.Instance
                    : HorrorScreenOverlay.EnsureExists();
                if (overlay != null)
                {
                    float flashAlpha = Mathf.Lerp(0.24f, 0.36f, Mathf.Clamp01(intensity));
                    overlay.TriggerSubliminalFlash(flashType, duration: 0.090f, maxAlpha: flashAlpha);
                }
            }
        }

        [ContextMenu("Testar Pico de Alucinação (Glitch Spike)")]
        private void DebugTriggerSpike()
        {
            TriggerHallucinationSpike(1.0f, 1.2f);
        }
    }
}
