using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Canal420.Audio;
using Canal420.Core;
using Canal420.Effects;
using Canal420.Interaction;
using Canal420.Player;
using Canal420.Puzzles;

namespace Canal420.UI
{
    public class GameSettingsUI : MonoBehaviour
    {
        public static GameSettingsUI Instance { get; private set; }

        public const string PrefKeyVolume = "Canal420_MasterVolume";
        public const string PrefKeyBrightness = "Canal420_Brightness";
        public const string PrefKeyQuality = "Canal420_GraphicsQuality";
        public const string PrefKeyBrightShadows = "Canal420_BrightShadows";
        public const string PrefKeyVSync = "Canal420_VSync";
        public const string PrefKeyResolution = "Canal420_Resolution";
        public const string PrefKeyDisplayMode = "Canal420_DisplayMode";

        public const float MaxBrightness = 1.00f;

        private const float DefaultVolume = 0.85f;
        private const float DefaultBrightness = 0.50f;
        private const int DefaultQualityTier = 2;
        private const bool DefaultBrightShadows = false;
        private const bool DefaultVSyncEnabled = true;
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        private const int DefaultResolutionPreset = 0; // 2560x1664 (Tela interna Retina)
#else
        private const int DefaultResolutionPreset = 3; // Nativa
#endif
        private const int DefaultDisplayMode = 0; // 0 = Tela Cheia (FullScreenWindow), 1 = Janela

        private static readonly string[] QualityLabels = { "Baixo", "Médio", "Alto", "Ultra" };
        private static readonly string[] ResolutionLabels = { "2560×1664", "2560×1440", "1920×1080", "Nativa" };

        private GameObject canvasRoot;
        private CanvasGroup hudButtonGroup;

        private GameObject modalOverlayObj;
        private CanvasGroup modalCanvasGroup;
        private RectTransform modalCardRect;

        private Slider audioSlider;
        private Text audioValueText;

        private Slider brightnessSlider;
        private Text brightnessValueText;

        private readonly Image[] qualityButtonBgs = new Image[4];
        private readonly Text[] qualityButtonTexts = new Text[4];

        private readonly Image[] resolutionButtonBgs = new Image[4];
        private readonly Text[] resolutionButtonTexts = new Text[4];

        private Image displayModeButtonBg;
        private Text displayModeButtonText;
        private Image brightShadowsButtonBg;
        private Text brightShadowsButtonText;
        private Image vSyncButtonBg;
        private Text vSyncButtonText;

        private Font uiFont;

        private bool isSettingsOpen = false;
        private bool suppressSliderCallbacks = false;
        private int openedAtFrame = -10;

        private PlayerController cachedPlayer;
        private TVController cachedTvController;
        private bool tvLookupAttempted = false;
        private bool savedCanMove = true;
        private bool savedCanLook = true;
        private bool savedCursorLocked = true;

        private RenderPipelineAsset originalQualityPipelineAsset;
        private bool hasCapturedOriginalPipeline = false;
        private UniversalRenderPipelineAsset runtimeUrpClone;

        private float currentVolume = DefaultVolume;
        private float currentBrightness = DefaultBrightness;
        private int currentQualityTier = DefaultQualityTier;
        private int currentResolutionPreset = DefaultResolutionPreset;
        private int currentDisplayMode = DefaultDisplayMode;
        private bool currentBrightShadows = DefaultBrightShadows;
        private bool currentVSyncEnabled = DefaultVSyncEnabled;
        private bool pendingProbeRefreshOnBrightnessDrag = false;
        private Coroutine deferredProbeRefreshCoroutine;
#if !UNITY_EDITOR
        private bool hasRequestedResolution = false;
        private int lastRequestedWidth = 0;
        private int lastRequestedHeight = 0;
        private FullScreenMode lastRequestedFullScreenMode = FullScreenMode.FullScreenWindow;
#endif

        public bool IsOpen => isSettingsOpen;
        public float CurrentBrightness => currentBrightness;
        public int CurrentQualityTier => currentQualityTier;
        public int CurrentResolutionPreset => currentResolutionPreset;
        public int CurrentDisplayMode => currentDisplayMode;
        public bool IsBrightShadowsEnabled => currentBrightShadows;
        public bool IsVSyncEnabled => currentVSyncEnabled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            currentVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKeyVolume, DefaultVolume));
            currentBrightness = Mathf.Clamp(PlayerPrefs.GetFloat(PrefKeyBrightness, DefaultBrightness), 0f, MaxBrightness);
            currentQualityTier = Mathf.Clamp(PlayerPrefs.GetInt(PrefKeyQuality, DefaultQualityTier), 0, 3);
            currentResolutionPreset = Mathf.Clamp(PlayerPrefs.GetInt(PrefKeyResolution, DefaultResolutionPreset), 0, 3);
            currentDisplayMode = Mathf.Clamp(PlayerPrefs.GetInt(PrefKeyDisplayMode, DefaultDisplayMode), 0, 1);
            currentBrightShadows = PlayerPrefs.GetInt(PrefKeyBrightShadows, DefaultBrightShadows ? 1 : 0) == 1;
            currentVSyncEnabled = PlayerPrefs.GetInt(PrefKeyVSync, DefaultVSyncEnabled ? 1 : 0) == 1;

            ApplyResolutionSetting();
            ApplyVSyncOrFrameRateCap();

            EnsureEventSystemExists();
            BuildUIIfNeeded();
        }

        private void Start()
        {
            ApplyAllSettingsImmediate(playAudioTick: false, savePrefs: false);
#if !UNITY_EDITOR
            StartCoroutine(LogRuntimeVideoConfigCoroutine());
#endif
        }

        private void OnDisable()
        {
            if (isSettingsOpen)
            {
                CloseSettingsMenu(restorePlayerState: false);
            }
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (hasCapturedOriginalPipeline)
            {
                QualitySettings.renderPipeline = originalQualityPipelineAsset;
                hasCapturedOriginalPipeline = false;
            }
            if (runtimeUrpClone != null)
            {
                if (Application.isPlaying) Destroy(runtimeUrpClone);
                else DestroyImmediate(runtimeUrpClone);
                runtimeUrpClone = null;
            }

            if (Instance == this)
            {
                Instance = null;
            }

            if (canvasRoot != null)
            {
                if (Application.isPlaying) Destroy(canvasRoot);
                else DestroyImmediate(canvasRoot);
                canvasRoot = null;
            }
        }

        private void Update()
        {
            bool inDayTransitionOrEnd = DayManager.Instance != null &&
                                        (DayManager.Instance.IsTransitioning ||
                                         DayManager.Instance.IsDemoEnded ||
                                         DayManager.Instance.CurrentDay == GameDay.Day4_Prelude);

            if (cachedTvController == null && !tvLookupAttempted)
            {
                tvLookupAttempted = true;
                cachedTvController = FindAnyObjectByType<TVController>();
            }
            bool inTvCutscene = cachedTvController != null && cachedTvController.IsCutsceneActive;

            UpdateHUDButtonVisibility(inDayTransitionOrEnd || inTvCutscene);

            if (inDayTransitionOrEnd)
            {
                if (isSettingsOpen)
                {
                    CloseSettingsMenu(restorePlayerState: false);
                }
                return;
            }

            if (!inTvCutscene || isSettingsOpen)
            {
                HandleShortcuts();
            }
            AnimateModal();
        }

        private void UpdateHUDButtonVisibility(bool hideCompletely)
        {
            if (hudButtonGroup == null) return;

            bool blockedByOtherModal = !isSettingsOpen && ObjectiveHUD.IsBlockingModalOpen;
            float targetAlpha = (hideCompletely || blockedByOtherModal) ? 0f : (isSettingsOpen ? 0.96f : 0.86f);
            hudButtonGroup.alpha = Mathf.MoveTowards(hudButtonGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 6f);
            hudButtonGroup.interactable = targetAlpha > 0.25f;
            hudButtonGroup.blocksRaycasts = targetAlpha > 0.25f;
        }

        private void HandleShortcuts()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            bool toggleKeyPressed = kb.escapeKey.wasPressedThisFrame ||
                                    kb.oKey.wasPressedThisFrame ||
                                    kb.f1Key.wasPressedThisFrame;

            if (!toggleKeyPressed)
                return;

            if (isSettingsOpen)
            {
                if (Time.frameCount > openedAtFrame)
                {
                    CloseSettingsMenu(restorePlayerState: true);
                }
                return;
            }

            if (ObjectiveHUD.IsBlockingModalOpen || ObjectiveHUD.WasModalClosedRecently(2))
                return;

            OpenSettingsMenu();
        }

        private void AnimateModal()
        {
            if (modalCanvasGroup == null || !isSettingsOpen)
                return;

            modalCanvasGroup.alpha = Mathf.MoveTowards(modalCanvasGroup.alpha, 1f, Time.unscaledDeltaTime * 7.5f);
            if (modalCardRect != null)
            {
                float s = Mathf.Lerp(0.96f, 1.0f, modalCanvasGroup.alpha);
                modalCardRect.localScale = new Vector3(s, s, 1f);
            }
        }

        public void ToggleSettingsMenu()
        {
            bool blockedByDayState = DayManager.Instance != null &&
                                     (DayManager.Instance.IsTransitioning ||
                                      DayManager.Instance.IsDemoEnded ||
                                      DayManager.Instance.CurrentDay == GameDay.Day4_Prelude);

            if (isSettingsOpen)
            {
                CloseSettingsMenu(restorePlayerState: true);
            }
            else if (!blockedByDayState && !ObjectiveHUD.IsBlockingModalOpen)
            {
                OpenSettingsMenu();
            }
        }

        public void OpenSettingsMenu()
        {
            if (isSettingsOpen) return;

            EnsureEventSystemExists();
            BuildUIIfNeeded();

            cachedPlayer = FindAnyObjectByType<PlayerController>();
            if (cachedPlayer != null)
            {
                savedCanMove = cachedPlayer.CanMove;
                savedCanLook = cachedPlayer.CanLook;
                savedCursorLocked = cachedPlayer.IsCursorLocked;
                cachedPlayer.SetControlsEnabled(moveEnabled: false, lookEnabled: false, lockCursor: false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            isSettingsOpen = true;
            openedAtFrame = Time.frameCount;
            ObjectiveHUD.IsBlockingModalOpen = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;

            SyncControlsFromCurrentValues();

            if (modalOverlayObj != null) modalOverlayObj.SetActive(true);
            if (modalCanvasGroup != null) modalCanvasGroup.alpha = 0f;
            if (modalCardRect != null) modalCardRect.localScale = new Vector3(0.96f, 0.96f, 1f);

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayMenuClick(0.32f, 0.96f);
            }
        }

        public void CloseSettingsMenu(bool restorePlayerState = true)
        {
            if (!isSettingsOpen) return;

            FlushPendingBrightnessProbeRefresh();
            isSettingsOpen = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            ObjectiveHUD.IsBlockingModalOpen = false;
            ObjectiveHUD.NotifyModalClosedThisFrame();
            PlayerPrefs.Save();

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if (modalOverlayObj != null)
            {
                modalOverlayObj.SetActive(false);
            }

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.SuppressInteractionBriefly(0.20f);
            }

            if (restorePlayerState)
            {
                if (cachedPlayer == null) cachedPlayer = FindAnyObjectByType<PlayerController>();
                if (cachedPlayer != null)
                {
                    cachedPlayer.SetControlsEnabled(
                        moveEnabled: savedCanMove,
                        lookEnabled: savedCanLook,
                        lockCursor: savedCursorLocked || savedCanLook
                    );
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            if (restorePlayerState && Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.TVChannelClick, 0.28f, 0.88f);
            }
        }

        private void OnAudioSliderChanged(float val)
        {
            if (suppressSliderCallbacks) return;
            currentVolume = Mathf.Clamp01(val);
            PlayerPrefs.SetFloat(PrefKeyVolume, currentVolume);
            ApplyAudioSetting(playTick: true);
            RefreshValueLabels();
        }

        private void OnBrightnessSliderChanged(float val)
        {
            if (suppressSliderCallbacks) return;
            currentBrightness = Mathf.Clamp(val, 0f, MaxBrightness);
            PlayerPrefs.SetFloat(PrefKeyBrightness, currentBrightness);
            ApplyBrightnessSetting(refreshReflectionProbes: false);
            pendingProbeRefreshOnBrightnessDrag = true;
            ScheduleDeferredBrightnessProbeRefresh();
            RefreshValueLabels();
        }

        private void SelectQualityTier(int tier)
        {
            currentQualityTier = Mathf.Clamp(tier, 0, 3);
            PlayerPrefs.SetInt(PrefKeyQuality, currentQualityTier);
            ApplyQualitySetting();
            RefreshQualityVisuals();

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.TVChannelClick, 0.35f, 1.05f);
            }
        }

        private void SelectResolutionPreset(int preset)
        {
            currentResolutionPreset = Mathf.Clamp(preset, 0, 3);
            PlayerPrefs.SetInt(PrefKeyResolution, currentResolutionPreset);
            ApplyResolutionSetting(explicitUserChoice: true);
            RefreshResolutionVisuals();

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.TVChannelClick, 0.35f, 1.04f);
            }
        }

        private void ToggleDisplayMode()
        {
            currentDisplayMode = currentDisplayMode == 0 ? 1 : 0;
            PlayerPrefs.SetInt(PrefKeyDisplayMode, currentDisplayMode);
            ApplyResolutionSetting(explicitUserChoice: true);
            RefreshDisplayModeVisuals();

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.TVChannelClick, 0.35f, currentDisplayMode == 0 ? 1.06f : 0.94f);
            }
        }

        private void ToggleBrightShadows()
        {
            currentBrightShadows = !currentBrightShadows;
            PlayerPrefs.SetInt(PrefKeyBrightShadows, currentBrightShadows ? 1 : 0);
            ApplyBrightShadowsSetting();
            RefreshBrightShadowsVisuals();

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.TVChannelClick, 0.35f, currentBrightShadows ? 1.08f : 0.92f);
            }
        }

        private void ToggleVSync()
        {
            currentVSyncEnabled = !currentVSyncEnabled;
            PlayerPrefs.SetInt(PrefKeyVSync, currentVSyncEnabled ? 1 : 0);
            ApplyVSyncOrFrameRateCap();
            RefreshVSyncVisuals();

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.TVChannelClick, 0.35f, currentVSyncEnabled ? 1.08f : 0.92f);
            }
        }

        private void ResetToDefaults()
        {
            currentVolume = DefaultVolume;
            currentBrightness = Mathf.Clamp(DefaultBrightness, 0f, MaxBrightness);
            currentQualityTier = DefaultQualityTier;
            currentResolutionPreset = DefaultResolutionPreset;
            currentDisplayMode = DefaultDisplayMode;
            currentBrightShadows = DefaultBrightShadows;
            currentVSyncEnabled = DefaultVSyncEnabled;

            PlayerPrefs.SetFloat(PrefKeyVolume, currentVolume);
            PlayerPrefs.SetFloat(PrefKeyBrightness, currentBrightness);
            PlayerPrefs.SetInt(PrefKeyQuality, currentQualityTier);
            PlayerPrefs.SetInt(PrefKeyResolution, currentResolutionPreset);
            PlayerPrefs.SetInt(PrefKeyDisplayMode, currentDisplayMode);
            PlayerPrefs.SetInt(PrefKeyBrightShadows, currentBrightShadows ? 1 : 0);
            PlayerPrefs.SetInt(PrefKeyVSync, currentVSyncEnabled ? 1 : 0);
            PlayerPrefs.Save();

            SyncControlsFromCurrentValues();
            ApplyResolutionSetting(explicitUserChoice: true);
            ApplyAllSettingsImmediate(playAudioTick: true, savePrefs: false);
        }

        public void ApplyAllSettingsImmediate(bool playAudioTick, bool savePrefs)
        {
            ApplyAudioSetting(playAudioTick);
            ApplyBrightnessSetting(refreshReflectionProbes: false);
            ApplyBrightShadowsSetting();
            ApplyResolutionSetting(explicitUserChoice: false);
            ApplyQualitySetting();
            RefreshValueLabels();
            RefreshQualityVisuals();
            RefreshResolutionVisuals();
            RefreshDisplayModeVisuals();
            RefreshBrightShadowsVisuals();
            RefreshVSyncVisuals();

            if (savePrefs)
            {
                PlayerPrefs.SetFloat(PrefKeyVolume, currentVolume);
                PlayerPrefs.SetFloat(PrefKeyBrightness, currentBrightness);
                PlayerPrefs.SetInt(PrefKeyQuality, currentQualityTier);
                PlayerPrefs.SetInt(PrefKeyResolution, currentResolutionPreset);
                PlayerPrefs.SetInt(PrefKeyDisplayMode, currentDisplayMode);
                PlayerPrefs.SetInt(PrefKeyBrightShadows, currentBrightShadows ? 1 : 0);
                PlayerPrefs.SetInt(PrefKeyVSync, currentVSyncEnabled ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private void ApplyAudioSetting(bool playTick)
        {
            AudioListener.volume = currentVolume;
            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.SetUserMasterVolume(currentVolume, playFeedbackTick: playTick);
            }
        }

        private void ApplyBrightnessSetting(bool refreshReflectionProbes = true)
        {
            if (SanityEffects.Instance != null)
            {
                SanityEffects.Instance.SetUserBrightnessNormalized(currentBrightness, applyImmediately: true);
            }

            if (DayManager.Instance != null)
            {
                if (refreshReflectionProbes)
                {
                    DayManager.Instance.SetUserBrightnessNormalized(currentBrightness);
                }
                else
                {
                    DayManager.Instance.SetUserBrightnessNormalizedDeferredProbes(currentBrightness);
                }
            }
        }

        private void ScheduleDeferredBrightnessProbeRefresh()
        {
            if (!isActiveAndEnabled) return;
            if (deferredProbeRefreshCoroutine != null)
            {
                StopCoroutine(deferredProbeRefreshCoroutine);
            }
            deferredProbeRefreshCoroutine = StartCoroutine(DeferredBrightnessProbeRefreshCoroutine());
        }

        private System.Collections.IEnumerator DeferredBrightnessProbeRefreshCoroutine()
        {
            yield return new WaitForSecondsRealtime(0.16f);
            deferredProbeRefreshCoroutine = null;
            FlushPendingBrightnessProbeRefresh();
        }

        private void FlushPendingBrightnessProbeRefresh()
        {
            if (deferredProbeRefreshCoroutine != null)
            {
                StopCoroutine(deferredProbeRefreshCoroutine);
                deferredProbeRefreshCoroutine = null;
            }
            if (!pendingProbeRefreshOnBrightnessDrag) return;
            pendingProbeRefreshOnBrightnessDrag = false;
            if (DayManager.Instance != null)
            {
                DayManager.Instance.RefreshInteriorReflectionProbes();
            }
        }

        private void ApplyBrightShadowsSetting()
        {
            if (SanityEffects.Instance != null)
            {
                SanityEffects.Instance.SetBrightShadowsEnabled(currentBrightShadows, applyImmediately: true);
            }

            if (DayManager.Instance != null)
            {
                DayManager.Instance.SetBrightShadowsEnabled(currentBrightShadows);
            }
        }

        private void ApplyQualitySetting()
        {
            UniversalRenderPipelineAsset urp = EnsureRuntimeUrpPipelineClone();
            Camera mainCam = Camera.main;
            UniversalAdditionalCameraData camData = mainCam != null ? mainCam.GetComponent<UniversalAdditionalCameraData>() : null;

            switch (currentQualityTier)
            {
                case 0:
                    if (urp != null)
                    {
                        urp.renderScale = 0.80f;
                        urp.msaaSampleCount = 1;
                        urp.shadowDistance = 16f;
                        urp.additionalLightsShadowmapResolution = 2048;
                    }
                    ApplyVSyncOrFrameRateCap();
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                    QualitySettings.realtimeReflectionProbes = true;
                    if (camData != null)
                    {
                        camData.antialiasing = AntialiasingMode.None;
                        camData.dithering = false;
                    }
                    if (SanityEffects.Instance != null) SanityEffects.Instance.SetFilmGrainQualityScale(0f);
                    if (DayManager.Instance != null) DayManager.Instance.ApplyShadowQualityToInteriorLights(enableSoftShadows: false);
                    break;

                case 1:
                    if (urp != null)
                    {
                        urp.renderScale = 0.92f;
                        urp.msaaSampleCount = 2;
                        urp.shadowDistance = 20f;
                        urp.additionalLightsShadowmapResolution = 2048;
                    }
                    ApplyVSyncOrFrameRateCap();
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                    QualitySettings.realtimeReflectionProbes = true;
                    if (camData != null)
                    {
                        camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                        camData.dithering = true;
                    }
                    if (SanityEffects.Instance != null) SanityEffects.Instance.SetFilmGrainQualityScale(0.75f);
                    if (DayManager.Instance != null) DayManager.Instance.ApplyShadowQualityToInteriorLights(enableSoftShadows: true);
                    break;

                case 2:
                    if (urp != null)
                    {
                        urp.renderScale = 1.00f;
                        urp.msaaSampleCount = 2;
                        urp.shadowDistance = 22f;
                        urp.additionalLightsShadowmapResolution = 4096;
                    }
                    ApplyVSyncOrFrameRateCap();
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    QualitySettings.realtimeReflectionProbes = true;
                    if (camData != null)
                    {
                        camData.antialiasing = AntialiasingMode.None;
                        camData.dithering = true;
                    }
                    if (SanityEffects.Instance != null) SanityEffects.Instance.SetFilmGrainQualityScale(1f);
                    if (DayManager.Instance != null) DayManager.Instance.ApplyShadowQualityToInteriorLights(enableSoftShadows: true);
                    break;

                case 3:
                    if (urp != null)
                    {
                        urp.renderScale = 1.00f;
                        urp.msaaSampleCount = 4;
                        urp.shadowDistance = 26f;
                        urp.additionalLightsShadowmapResolution = 4096;
                    }
                    ApplyVSyncOrFrameRateCap();
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    QualitySettings.realtimeReflectionProbes = true;
                    if (camData != null)
                    {
                        camData.antialiasing = AntialiasingMode.None;
                        camData.dithering = true;
                    }
                    if (SanityEffects.Instance != null) SanityEffects.Instance.SetFilmGrainQualityScale(1f);
                    if (DayManager.Instance != null) DayManager.Instance.ApplyShadowQualityToInteriorLights(enableSoftShadows: true);
                    break;
            }
        }

        private void ApplyVSyncOrFrameRateCap()
        {
            QualitySettings.vSyncCount = currentVSyncEnabled ? 1 : 0;
            Application.targetFrameRate = -1;
        }

        private void ApplyResolutionSetting(bool explicitUserChoice = false)
        {
#if !UNITY_EDITOR
            ResolveTargetResolution(currentResolutionPreset, out int targetW, out int targetH);
            FullScreenMode targetMode = currentDisplayMode == 1
                ? FullScreenMode.Windowed
                : FullScreenMode.FullScreenWindow;

            if (ShouldCallSetResolution(
                Screen.width,
                Screen.height,
                Screen.fullScreenMode,
                targetW,
                targetH,
                targetMode,
                hasRequestedResolution,
                lastRequestedWidth,
                lastRequestedHeight,
                lastRequestedFullScreenMode,
                explicitUserChoice))
            {
                Screen.SetResolution(targetW, targetH, targetMode);
            }

            if (targetW >= 640 && targetH >= 480)
            {
                hasRequestedResolution = true;
                lastRequestedWidth = targetW;
                lastRequestedHeight = targetH;
                lastRequestedFullScreenMode = targetMode;
            }
#endif
        }

#if !UNITY_EDITOR
        private System.Collections.IEnumerator LogRuntimeVideoConfigCoroutine()
        {
            for (int f = 0; f < 4; f++)
            {
                yield return null;
            }

            int rendW = Display.main != null ? Display.main.renderingWidth : Screen.width;
            int rendH = Display.main != null ? Display.main.renderingHeight : Screen.height;
            string qualityName = QualitySettings.names != null &&
                QualitySettings.GetQualityLevel() >= 0 &&
                QualitySettings.GetQualityLevel() < QualitySettings.names.Length
                ? QualitySettings.names[QualitySettings.GetQualityLevel()]
                : QualitySettings.GetQualityLevel().ToString();
            var urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset
                ?? GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            string urpSummary = urp != null
                ? $"renderScale={urp.renderScale:F2} msaa={urp.msaaSampleCount} shadowDist={urp.shadowDistance:F0} addShadow={urp.additionalLightsShadowmapResolution}"
                : "urp=null";

            Debug.Log(
                $"[Canal 420] Video: Screen={Screen.width}x{Screen.height} CurRes={Screen.currentResolution.width}x{Screen.currentResolution.height} " +
                $"Rend={rendW}x{rendH} Mode={Screen.fullScreenMode} VSync={QualitySettings.vSyncCount} Quality={qualityName} {urpSummary}");
        }
#endif

        public static bool ShouldCallSetResolution(
            int actualWidth,
            int actualHeight,
            FullScreenMode actualMode,
            int targetWidth,
            int targetHeight,
            FullScreenMode targetMode,
            bool hasRequested,
            int lastWidth,
            int lastHeight,
            FullScreenMode lastMode,
            bool explicitUserChoice)
        {
            if (targetWidth < 640 || targetHeight < 480)
                return false;

            if (actualWidth == targetWidth && actualHeight == targetHeight && actualMode == targetMode)
                return false;

            if (!explicitUserChoice &&
                hasRequested &&
                lastWidth == targetWidth &&
                lastHeight == targetHeight &&
                lastMode == targetMode)
            {
                return false;
            }

            return true;
        }

        private static void ResolveTargetResolution(int preset, out int width, out int height)
        {
            int sysW = Display.main != null ? Display.main.systemWidth : 0;
            int sysH = Display.main != null ? Display.main.systemHeight : 0;
            Resolution cur = Screen.currentResolution;
            ResolveTargetResolutionFromCandidates(
                preset,
                Screen.resolutions,
                sysW,
                sysH,
                cur.width,
                cur.height,
                out width,
                out height);
        }

        public static void ResolveTargetResolutionFromCandidates(
            int preset,
            Resolution[] supportedResolutions,
            int sysWidth,
            int sysHeight,
            int currentResWidth,
            int currentResHeight,
            out int width,
            out int height)
        {
            int bestSupportedW = 0;
            int bestSupportedH = 0;
            if (supportedResolutions != null && supportedResolutions.Length > 0)
            {
                long bestPixels = -1;
                double bestRefresh = -1d;
                for (int i = 0; i < supportedResolutions.Length; i++)
                {
                    Resolution r = supportedResolutions[i];
                    if (r.width < 640 || r.height < 480)
                        continue;

                    long pixels = (long)r.width * r.height;
                    double hz = r.refreshRateRatio.value;
                    if (pixels > bestPixels || (pixels == bestPixels && hz > bestRefresh))
                    {
                        bestPixels = pixels;
                        bestRefresh = hz;
                        bestSupportedW = r.width;
                        bestSupportedH = r.height;
                    }
                }
            }

            switch (preset)
            {
                case 0:
                    width = 2560;
                    height = 1664;
                    break;
                case 1:
                    width = 2560;
                    height = 1440;
                    break;
                case 2:
                    width = 1920;
                    height = 1080;
                    break;
                default:
                    width = bestSupportedW;
                    height = bestSupportedH;

                    if (width <= 0 || height <= 0)
                    {
                        width = sysWidth;
                        height = sysHeight;
                    }
                    if (width <= 0 || height <= 0)
                    {
                        width = currentResWidth;
                        height = currentResHeight;
                    }
                    if (width <= 0 || height <= 0)
                    {
                        width = 1920;
                        height = 1080;
                    }
                    return;
            }

            // Safety clamp for external displays / projectors (e.g., 1080p or 720p):
            // never request a preset larger than the connected display's maximum supported resolution.
            if (bestSupportedW > 0 && bestSupportedH > 0 && (width > bestSupportedW || height > bestSupportedH))
            {
                width = bestSupportedW;
                height = bestSupportedH;
            }
        }

        private UniversalRenderPipelineAsset EnsureRuntimeUrpPipelineClone()
        {
            if (runtimeUrpClone != null)
                return runtimeUrpClone;

            RenderPipelineAsset currentAsset = QualitySettings.renderPipeline != null
                ? QualitySettings.renderPipeline
                : GraphicsSettings.currentRenderPipeline;

            if (currentAsset is UniversalRenderPipelineAsset urpSource)
            {
                originalQualityPipelineAsset = QualitySettings.renderPipeline;
                hasCapturedOriginalPipeline = true;
                runtimeUrpClone = Instantiate(urpSource);
                runtimeUrpClone.name = urpSource.name + "_RuntimeClone";
                QualitySettings.renderPipeline = runtimeUrpClone;
            }

            return runtimeUrpClone;
        }

        private void SyncControlsFromCurrentValues()
        {
            suppressSliderCallbacks = true;
            if (audioSlider != null) audioSlider.value = currentVolume;
            if (brightnessSlider != null) brightnessSlider.value = currentBrightness;
            suppressSliderCallbacks = false;

            RefreshValueLabels();
            RefreshQualityVisuals();
            RefreshResolutionVisuals();
            RefreshDisplayModeVisuals();
            RefreshBrightShadowsVisuals();
            RefreshVSyncVisuals();
        }

        private void RefreshValueLabels()
        {
            if (audioValueText != null)
            {
                int pct = Mathf.RoundToInt(currentVolume * 100f);
                audioValueText.text = $"{pct}%";
            }

            if (brightnessValueText != null)
            {
                int pct = Mathf.RoundToInt(currentBrightness * 100f);
                brightnessValueText.text = $"{pct}%";
            }
        }

        private void RefreshQualityVisuals()
        {
            for (int i = 0; i < 4; i++)
            {
                bool active = (i == currentQualityTier);
                if (qualityButtonBgs[i] != null)
                {
                    qualityButtonBgs[i].color = active
                        ? new Color(0.26f, 0.26f, 0.28f, 0.98f)
                        : new Color(0.10f, 0.10f, 0.11f, 0.96f);
                }
                if (qualityButtonTexts[i] != null)
                {
                    qualityButtonTexts[i].color = active
                        ? new Color(0.98f, 0.98f, 0.98f, 1.0f)
                        : new Color(0.60f, 0.60f, 0.60f, 0.92f);
                    qualityButtonTexts[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                }
            }
        }

        private void RefreshResolutionVisuals()
        {
            for (int i = 0; i < 4; i++)
            {
                bool active = (i == currentResolutionPreset);
                if (resolutionButtonBgs[i] != null)
                {
                    resolutionButtonBgs[i].color = active
                        ? new Color(0.26f, 0.26f, 0.28f, 0.98f)
                        : new Color(0.10f, 0.10f, 0.11f, 0.96f);
                }
                if (resolutionButtonTexts[i] != null)
                {
                    resolutionButtonTexts[i].color = active
                        ? new Color(0.98f, 0.98f, 0.98f, 1.0f)
                        : new Color(0.60f, 0.60f, 0.60f, 0.92f);
                    resolutionButtonTexts[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                }
            }
        }

        private void RefreshDisplayModeVisuals()
        {
            bool isFullscreen = currentDisplayMode == 0;
            if (displayModeButtonBg != null)
            {
                displayModeButtonBg.color = isFullscreen
                    ? new Color(0.28f, 0.30f, 0.32f, 0.98f)
                    : new Color(0.10f, 0.10f, 0.11f, 0.96f);
            }

            if (displayModeButtonText != null)
            {
                displayModeButtonText.text = isFullscreen ? "TELA CHEIA" : "JANELA";
                displayModeButtonText.color = isFullscreen
                    ? new Color(0.98f, 0.98f, 0.98f, 1.0f)
                    : new Color(0.64f, 0.64f, 0.64f, 0.92f);
                displayModeButtonText.fontStyle = isFullscreen ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private void RefreshBrightShadowsVisuals()
        {
            if (brightShadowsButtonBg != null)
            {
                brightShadowsButtonBg.color = currentBrightShadows
                    ? new Color(0.28f, 0.30f, 0.32f, 0.98f)
                    : new Color(0.10f, 0.10f, 0.11f, 0.96f);
            }

            if (brightShadowsButtonText != null)
            {
                brightShadowsButtonText.text = currentBrightShadows ? "LIGADO" : "DESLIGADO";
                brightShadowsButtonText.color = currentBrightShadows
                    ? new Color(0.98f, 0.98f, 0.98f, 1.0f)
                    : new Color(0.64f, 0.64f, 0.64f, 0.92f);
                brightShadowsButtonText.fontStyle = currentBrightShadows ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private void RefreshVSyncVisuals()
        {
            if (vSyncButtonBg != null)
            {
                vSyncButtonBg.color = currentVSyncEnabled
                    ? new Color(0.28f, 0.30f, 0.32f, 0.98f)
                    : new Color(0.10f, 0.10f, 0.11f, 0.96f);
            }

            if (vSyncButtonText != null)
            {
                vSyncButtonText.text = currentVSyncEnabled ? "LIGADO" : "DESLIGADO";
                vSyncButtonText.color = currentVSyncEnabled
                    ? new Color(0.98f, 0.98f, 0.98f, 1.0f)
                    : new Color(0.64f, 0.64f, 0.64f, 0.92f);
                vSyncButtonText.fontStyle = currentVSyncEnabled ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private static void EnsureEventSystemExists()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject esObj = new GameObject("EventSystem_Auto");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildUIIfNeeded()
        {
            if (canvasRoot != null) return;

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            canvasRoot = new GameObject("HUD_Configuracoes_Auto");
            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 140;

            CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRoot.AddComponent<GraphicRaycaster>();

            BuildTopRightHUDButton();
            BuildSettingsModal();
            SyncControlsFromCurrentValues();
        }

        private void BuildTopRightHUDButton()
        {
            GameObject hudContainer = new GameObject("HUD_Settings_Container");
            hudContainer.transform.SetParent(canvasRoot.transform, false);
            RectTransform contRect = hudContainer.AddComponent<RectTransform>();
            contRect.anchorMin = new Vector2(1f, 1f);
            contRect.anchorMax = new Vector2(1f, 1f);
            contRect.pivot = new Vector2(1f, 1f);
            contRect.anchoredPosition = new Vector2(-24f, -24f);
            contRect.sizeDelta = new Vector2(128f, 24f);

            hudButtonGroup = hudContainer.AddComponent<CanvasGroup>();
            hudButtonGroup.alpha = 0f;
            hudButtonGroup.interactable = false;
            hudButtonGroup.blocksRaycasts = false;

            GameObject btnObj = new GameObject("Btn_OpenSettings_HUD");
            btnObj.transform.SetParent(hudContainer.transform, false);
            Image hitArea = btnObj.AddComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0f);

            RectTransform btnRect = hitArea.rectTransform;
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = Vector2.zero;
            btnRect.sizeDelta = Vector2.zero;

            Text btnTxt = CreateLabel(
                btnObj.transform,
                "Label",
                "[ESC] Opções",
                13,
                FontStyle.Normal,
                new Color(0.84f, 0.84f, 0.84f, 0.90f),
                TextAnchor.UpperRight
            );
            btnTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            btnTxt.verticalOverflow = VerticalWrapMode.Overflow;
            StretchFull(btnTxt.rectTransform, 0f, 0f, 0f, 0f);

            Shadow txtShadow = btnTxt.gameObject.AddComponent<Shadow>();
            txtShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            txtShadow.effectDistance = new Vector2(1f, -1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.targetGraphic = btnTxt;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(ToggleSettingsMenu);
        }

        private void BuildSettingsModal()
        {
            modalOverlayObj = new GameObject("Modal_Configuracoes_Overlay");
            modalOverlayObj.transform.SetParent(canvasRoot.transform, false);
            RectTransform overlayRect = modalOverlayObj.AddComponent<RectTransform>();
            StretchFull(overlayRect, 0f, 0f, 0f, 0f);

            Image dimBg = modalOverlayObj.AddComponent<Image>();
            dimBg.color = new Color(0f, 0f, 0f, 0.72f);
            Button backdropCloseBtn = modalOverlayObj.AddComponent<Button>();
            backdropCloseBtn.transition = Selectable.Transition.None;
            backdropCloseBtn.navigation = new Navigation { mode = Navigation.Mode.None };
            backdropCloseBtn.onClick.AddListener(() => CloseSettingsMenu(restorePlayerState: true));

            modalCanvasGroup = modalOverlayObj.AddComponent<CanvasGroup>();

            GameObject cardObj = new GameObject("Settings_Card_Body");
            cardObj.transform.SetParent(modalOverlayObj.transform, false);
            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.055f, 0.055f, 0.06f, 0.96f);
            modalCardRect = cardBg.rectTransform;
            modalCardRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalCardRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalCardRect.pivot = new Vector2(0.5f, 0.5f);
            modalCardRect.anchoredPosition = Vector2.zero;
            modalCardRect.sizeDelta = new Vector2(416f, 612f);

            Button swallowClick = cardObj.AddComponent<Button>();
            swallowClick.transition = Selectable.Transition.None;
            swallowClick.navigation = new Navigation { mode = Navigation.Mode.None };

            Text titleTxt = CreateLabel(
                cardObj.transform,
                "Header_Title",
                "OPÇÕES",
                14,
                FontStyle.Bold,
                new Color(0.95f, 0.95f, 0.95f, 1f),
                TextAnchor.MiddleLeft
            );
            PositionTopBox(titleTxt.rectTransform, 22f, -15f, 260f, 20f);

            CreateActionButton(
                cardObj.transform,
                "Btn_Close_X",
                "X",
                new Vector2(370f, -13f),
                new Vector2(24f, 24f),
                new Color(0.11f, 0.11f, 0.12f, 0.96f),
                12,
                () => CloseSettingsMenu(restorePlayerState: true)
            );

            CreateDivider(cardObj.transform, 22f, -43f, 372f);

            Text audioTitle = CreateLabel(
                cardObj.transform,
                "Audio_Label",
                "ÁUDIO",
                11,
                FontStyle.Bold,
                new Color(0.74f, 0.74f, 0.74f, 0.92f),
                TextAnchor.MiddleLeft
            );
            PositionTopBox(audioTitle.rectTransform, 22f, -55f, 180f, 18f);

            audioValueText = CreateLabel(
                cardObj.transform,
                "Audio_Value",
                "85%",
                11,
                FontStyle.Bold,
                new Color(0.90f, 0.90f, 0.90f, 1f),
                TextAnchor.MiddleRight
            );
            PositionTopBox(audioValueText.rectTransform, 234f, -55f, 160f, 18f);

            audioSlider = CreateStyledSlider(
                cardObj.transform,
                "Audio_Slider",
                leftX: 22f,
                topY: -77f,
                width: 372f,
                minVal: 0f,
                maxVal: 1f,
                fillColor: new Color(0.68f, 0.68f, 0.68f, 0.96f),
                onChanged: OnAudioSliderChanged
            );

            CreateDivider(cardObj.transform, 22f, -107f, 372f);

            Text brightTitle = CreateLabel(
                cardObj.transform,
                "Brightness_Label",
                "BRILHO",
                11,
                FontStyle.Bold,
                new Color(0.74f, 0.74f, 0.74f, 0.92f),
                TextAnchor.MiddleLeft
            );
            PositionTopBox(brightTitle.rectTransform, 22f, -119f, 180f, 18f);

            brightnessValueText = CreateLabel(
                cardObj.transform,
                "Brightness_Value",
                "50%",
                11,
                FontStyle.Bold,
                new Color(0.90f, 0.90f, 0.90f, 1f),
                TextAnchor.MiddleRight
            );
            PositionTopBox(brightnessValueText.rectTransform, 234f, -119f, 160f, 18f);

            brightnessSlider = CreateStyledSlider(
                cardObj.transform,
                "Brightness_Slider",
                leftX: 22f,
                topY: -141f,
                width: 372f,
                minVal: 0f,
                maxVal: MaxBrightness,
                fillColor: new Color(0.68f, 0.68f, 0.68f, 0.96f),
                onChanged: OnBrightnessSliderChanged
            );

            CreateDivider(cardObj.transform, 22f, -171f, 372f);

            Text qualTitle = CreateLabel(
                cardObj.transform,
                "Quality_Label",
                "GRÁFICOS",
                11,
                FontStyle.Bold,
                new Color(0.74f, 0.74f, 0.74f, 0.92f),
                TextAnchor.MiddleLeft
            );
            PositionTopBox(qualTitle.rectTransform, 22f, -183f, 200f, 18f);

            float btnWidth = 87f;
            float spacing = 8f;
            for (int i = 0; i < 4; i++)
            {
                int tierIndex = i;
                float x = 22f + i * (btnWidth + spacing);
                Button qBtn = CreateActionButton(
                    cardObj.transform,
                    $"Btn_Quality_{QualityLabels[i]}",
                    QualityLabels[i].ToUpperInvariant(),
                    new Vector2(x, -205f),
                    new Vector2(btnWidth, 26f),
                    new Color(0.10f, 0.10f, 0.11f, 0.96f),
                    11,
                    () => SelectQualityTier(tierIndex)
                );
                qualityButtonBgs[i] = qBtn.GetComponent<Image>();
                qualityButtonTexts[i] = qBtn.GetComponentInChildren<Text>();
                if (qualityButtonTexts[i] != null)
                {
                    qualityButtonTexts[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                }
            }

            CreateDivider(cardObj.transform, 22f, -242f, 372f);

            Text resTitle = CreateLabel(
                cardObj.transform,
                "Resolution_Label",
                "RESOLUÇÃO",
                11,
                FontStyle.Bold,
                new Color(0.74f, 0.74f, 0.74f, 0.92f),
                TextAnchor.MiddleLeft
            );
            PositionTopBox(resTitle.rectTransform, 22f, -253f, 200f, 18f);

            for (int i = 0; i < 4; i++)
            {
                int presetIndex = i;
                float x = 22f + i * (btnWidth + spacing);
                Button rBtn = CreateActionButton(
                    cardObj.transform,
                    $"Btn_Resolution_{i}",
                    ResolutionLabels[i].ToUpperInvariant(),
                    new Vector2(x, -275f),
                    new Vector2(btnWidth, 26f),
                    new Color(0.10f, 0.10f, 0.11f, 0.96f),
                    10,
                    () => SelectResolutionPreset(presetIndex)
                );
                resolutionButtonBgs[i] = rBtn.GetComponent<Image>();
                resolutionButtonTexts[i] = rBtn.GetComponentInChildren<Text>();
                if (resolutionButtonTexts[i] != null)
                {
                    resolutionButtonTexts[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                }
            }

            CreateDivider(cardObj.transform, 22f, -312f, 372f);

            Text displayModeTitle = CreateLabel(
                cardObj.transform,
                "DisplayMode_Label",
                "MODO DE TELA",
                11,
                FontStyle.Bold,
                new Color(0.74f, 0.74f, 0.74f, 0.92f),
                TextAnchor.MiddleLeft
            );
            PositionTopBox(displayModeTitle.rectTransform, 22f, -323f, 256f, 18f);

            Text displayModeHint = CreateLabel(
                cardObj.transform,
                "DisplayMode_Hint",
                "Tela cheia sem bordas ou modo janela",
                10,
                FontStyle.Normal,
                new Color(0.58f, 0.58f, 0.58f, 0.88f),
                TextAnchor.MiddleLeft
            );
            PositionTopBox(displayModeHint.rectTransform, 22f, -341f, 256f, 18f);

            Button displayModeBtn = CreateActionButton(
                cardObj.transform,
                "Btn_DisplayMode_Toggle",
                "TELA CHEIA",
                new Vector2(290f, -327f),
                new Vector2(104f, 28f),
                new Color(0.10f, 0.10f, 0.11f, 0.96f),
                11,
                ToggleDisplayMode
            );
            displayModeButtonBg = displayModeBtn.GetComponent<Image>();
            displayModeButtonText = displayModeBtn.GetComponentInChildren<Text>();

            CreateDivider(cardObj.transform, 22f, -368f, 372f);

            Text brightShadowsTitle = CreateLabel(
                cardObj.transform,
                "BrightShadows_Label",
                "SOMBRAS CLARAS",
                11,
                FontStyle.Bold,
                new Color(0.74f, 0.74f, 0.74f, 0.92f),
                TextAnchor.MiddleLeft
            );
            PositionTopBox(brightShadowsTitle.rectTransform, 22f, -379f, 256f, 18f);

            Text brightShadowsHint = CreateLabel(
                cardObj.transform,
                "BrightShadows_Hint",
                "Clareia cantos escuros sem luz direta",
                10,
                FontStyle.Normal,
                new Color(0.58f, 0.58f, 0.58f, 0.88f),
                TextAnchor.MiddleLeft
            );
            PositionTopBox(brightShadowsHint.rectTransform, 22f, -397f, 256f, 18f);

            Button brightShadowsBtn = CreateActionButton(
                cardObj.transform,
                "Btn_BrightShadows_Toggle",
                "DESLIGADO",
                new Vector2(290f, -383f),
                new Vector2(104f, 28f),
                new Color(0.10f, 0.10f, 0.11f, 0.96f),
                11,
                ToggleBrightShadows
            );
            brightShadowsButtonBg = brightShadowsBtn.GetComponent<Image>();
            brightShadowsButtonText = brightShadowsBtn.GetComponentInChildren<Text>();

            CreateDivider(cardObj.transform, 22f, -424f, 372f);

            Text vSyncTitle = CreateLabel(
                cardObj.transform,
                "VSync_Label",
                "VSYNC",
                11,
                FontStyle.Bold,
                new Color(0.74f, 0.74f, 0.74f, 0.92f),
                TextAnchor.MiddleLeft
            );
            PositionTopBox(vSyncTitle.rectTransform, 22f, -435f, 256f, 18f);

            Text vSyncHint = CreateLabel(
                cardObj.transform,
                "VSync_Hint",
                "Sincroniza quadros ao monitor (evita cortes)",
                10,
                FontStyle.Normal,
                new Color(0.58f, 0.58f, 0.58f, 0.88f),
                TextAnchor.MiddleLeft
            );
            PositionTopBox(vSyncHint.rectTransform, 22f, -453f, 256f, 18f);

            Button vSyncBtn = CreateActionButton(
                cardObj.transform,
                "Btn_VSync_Toggle",
                "LIGADO",
                new Vector2(290f, -439f),
                new Vector2(104f, 28f),
                new Color(0.10f, 0.10f, 0.11f, 0.96f),
                11,
                ToggleVSync
            );
            vSyncButtonBg = vSyncBtn.GetComponent<Image>();
            vSyncButtonText = vSyncBtn.GetComponentInChildren<Text>();

            CreateDivider(cardObj.transform, 22f, -480f, 372f);

            CreateActionButton(
                cardObj.transform,
                "Btn_ResetDefaults",
                "PADRÃO",
                new Vector2(22f, -492f),
                new Vector2(180f, 28f),
                new Color(0.11f, 0.11f, 0.12f, 0.96f),
                11,
                ResetToDefaults
            );

            CreateActionButton(
                cardObj.transform,
                "Btn_CloseResume",
                "RETOMAR",
                new Vector2(214f, -492f),
                new Vector2(180f, 28f),
                new Color(0.22f, 0.22f, 0.23f, 0.98f),
                11,
                () => CloseSettingsMenu(restorePlayerState: true)
            );

            CreateActionButton(
                cardObj.transform,
                "Btn_RestartGame",
                "REINICIAR",
                new Vector2(22f, -528f),
                new Vector2(180f, 28f),
                new Color(0.11f, 0.11f, 0.12f, 0.96f),
                11,
                RestartGameFromMenu
            );

            CreateActionButton(
                cardObj.transform,
                "Btn_QuitGame",
                "SAIR DO JOGO",
                new Vector2(214f, -528f),
                new Vector2(180f, 28f),
                new Color(0.24f, 0.09f, 0.09f, 0.96f),
                11,
                QuitGameFromMenu
            );

            CreateDivider(cardObj.transform, 22f, -567f, 372f);

            Text controlsFooter = CreateLabel(
                cardObj.transform,
                "Controls_Footer",
                "WASD Mover  |  E Interagir  |  F Lanterna  |  TAB Tarefas",
                11,
                FontStyle.Normal,
                new Color(0.58f, 0.58f, 0.58f, 0.88f),
                TextAnchor.MiddleCenter
            );
            PositionTopBox(controlsFooter.rectTransform, 22f, -576f, 372f, 22f);

            modalOverlayObj.SetActive(false);
        }

        private void RestartGameFromMenu()
        {
            FlushPendingBrightnessProbeRefresh();
            PlayerPrefs.Save();

            isSettingsOpen = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            InventoryState.ClearAll();
            DoorInteractable.PlayerHasSafeKey = false;
            ObjectiveHUD.IsBlockingModalOpen = false;
            SafeBoxPuzzle.CloseAnyOpenKeypad();
            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.CloseDocument(restorePlayerControls: false);
            }

            if (modalOverlayObj != null)
            {
                modalOverlayObj.SetActive(false);
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void QuitGameFromMenu()
        {
            FlushPendingBrightnessProbeRefresh();
            PlayerPrefs.Save();

            isSettingsOpen = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            ObjectiveHUD.IsBlockingModalOpen = false;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private Slider CreateStyledSlider(
            Transform parent,
            string name,
            float leftX,
            float topY,
            float width,
            float minVal,
            float maxVal,
            Color fillColor,
            UnityEngine.Events.UnityAction<float> onChanged)
        {
            GameObject sliderRoot = new GameObject(name);
            sliderRoot.transform.SetParent(parent, false);
            Image hitArea = sliderRoot.AddComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0.01f);
            PositionTopBox(hitArea.rectTransform, leftX, topY, width, 22f);

            GameObject bgObj = new GameObject("Track_Bg");
            bgObj.transform.SetParent(sliderRoot.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.14f, 0.14f, 0.15f, 0.98f);
            bgImg.raycastTarget = false;
            RectTransform bgRect = bgImg.rectTransform;
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(0f, 5f);

            GameObject fillArea = new GameObject("Fill_Area");
            fillArea.transform.SetParent(sliderRoot.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
            fillAreaRect.anchoredPosition = Vector2.zero;
            fillAreaRect.sizeDelta = new Vector2(-12f, 5f);

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.raycastTarget = false;
            RectTransform fillRect = fillImg.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;

            GameObject handleSlideArea = new GameObject("Handle_Slide_Area");
            handleSlideArea.transform.SetParent(sliderRoot.transform, false);
            RectTransform slideAreaRect = handleSlideArea.AddComponent<RectTransform>();
            slideAreaRect.anchorMin = new Vector2(0f, 0f);
            slideAreaRect.anchorMax = new Vector2(1f, 1f);
            slideAreaRect.offsetMin = new Vector2(6f, 0f);
            slideAreaRect.offsetMax = new Vector2(-6f, 0f);

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleSlideArea.transform, false);
            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.color = new Color(0.94f, 0.94f, 0.94f, 1f);
            RectTransform handleRect = handleImg.rectTransform;
            handleRect.sizeDelta = new Vector2(12f, 18f);

            Slider slider = sliderRoot.AddComponent<Slider>();
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = minVal;
            slider.maxValue = maxVal;
            slider.wholeNumbers = false;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.onValueChanged.AddListener(onChanged);

            return slider;
        }

        private Button CreateActionButton(
            Transform parent,
            string name,
            string label,
            Vector2 topLeftPos,
            Vector2 size,
            Color bgColor,
            int fontSize,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            Image bg = btnObj.AddComponent<Image>();
            bg.color = bgColor;
            PositionTopBox(bg.rectTransform, topLeftPos.x, topLeftPos.y, size.x, size.y);

            Button btn = btnObj.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            cb.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(onClick);

            Text txt = CreateLabel(
                btnObj.transform,
                "Label",
                label,
                fontSize,
                FontStyle.Bold,
                new Color(0.92f, 0.92f, 0.92f, 1f),
                TextAnchor.MiddleCenter
            );
            StretchFull(txt.rectTransform, 4f, 4f, 0f, 0f);
            return btn;
        }

        private void CreateDivider(Transform parent, float leftX, float topY, float width)
        {
            GameObject div = new GameObject("Divider");
            div.transform.SetParent(parent, false);
            Image img = div.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.08f);
            img.raycastTarget = false;
            PositionTopBox(img.rectTransform, leftX, topY, width, 1f);
        }

        private Text CreateLabel(
            Transform parent,
            string name,
            string content,
            int fontSize,
            FontStyle style,
            Color color,
            TextAnchor anchor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text txt = go.AddComponent<Text>();
            txt.font = uiFont;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = anchor;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        private static void PositionTopBox(RectTransform rt, float leftX, float topY, float width, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(leftX, topY);
            rt.sizeDelta = new Vector2(width, height);
        }

        private static void StretchFull(RectTransform rt, float left, float right, float bottom, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
