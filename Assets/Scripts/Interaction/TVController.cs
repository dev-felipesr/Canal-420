using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Canal420.Audio;
using Canal420.Core;
using Canal420.Effects;
using Canal420.Player;
using Canal420.UI;

namespace Canal420.Interaction
{
    public class TVController : MonoBehaviour, IInteractable
    {
        public static event Action OnMissingRemoteBatteriesAttempted;

        private enum TVState
        {
            Day1_OffWaitingToSit,
            Day1_SittingTransition,
            Day1_SeatedSurfing,
            Day1_LockedOn420,
            Day2_OffIdle,
            Day2_AutonomousNewsWaiting,
            Day2_PlayingNewsSequence,
            Day3_SilentGlass,
            Disabled
        }

        [Header("Referências Visuais")]
        [SerializeField] private Renderer screenRenderer;
        [SerializeField] private Light tvGlowLight;
        [SerializeField] private AudioSource tvAudioSource;

        [Header("Cadência de Troca de Canais")]
        [SerializeField] private float channelSwitchCooldown = 1.15f;
        private TVState currentState = TVState.Day1_OffWaitingToSit;
        private int day1ChannelIndex = 0;
        private float lastChannelSwitchTime = -10f;
        private string pendingDay1PromptText = "[E] Trocar de canal";
        private bool day1PromptCurrentlyShown = false;
        private Material runtimeScreenMat;
        private ProceduralCRTFeed crtFeed;
        private Coroutine activeSequenceCoroutine;
        private int frameSeated = -1;
        private float day1HypnosisProgress = 0f;
        private float day3ResidualTimer = 18f;
        private float day3ShockFlashTimer = 0f;
        private int day3ShockFlashRemainingFrames = 0;
        private const int MinDay3ShockVisibleFrames = 4;
        private Light cachedSalaFloorLamp;
        private Color smoothedTvLightColor = new Color(0.55f, 0.65f, 0.78f);
        private float smoothedTvLightIntensity = 1.4f;

        public bool IsWaitingToSitDay1 => currentState == TVState.Day1_OffWaitingToSit;
        public bool IsCutsceneActive =>
            currentState == TVState.Day1_SittingTransition ||
            currentState == TVState.Day1_SeatedSurfing ||
            currentState == TVState.Day1_LockedOn420 ||
            currentState == TVState.Day2_PlayingNewsSequence;

        public bool CanInteract
        {
            get
            {
                return currentState == TVState.Day1_OffWaitingToSit ||
                       currentState == TVState.Day2_OffIdle ||
                       currentState == TVState.Day2_AutonomousNewsWaiting ||
                       currentState == TVState.Day3_SilentGlass;
            }
        }

        private void Awake()
        {
            InitializeScreenMaterial();
            SetScreenVisual(new Color(0.02f, 0.02f, 0.03f), 0f, false);
        }

        private void Start()
        {
            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.AttachTVAudioSource(transform);
            }
        }

        private void OnDisable()
        {
            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ClearOverridePrompt();
            }

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.SetTVLoopMode(TVAudioLoopMode.Off, transform);
            }
        }

        private void InitializeScreenMaterial()
        {
            if (screenRenderer == null)
                return;

            if (runtimeScreenMat == null)
            {
                runtimeScreenMat = screenRenderer.material;
                runtimeScreenMat.EnableKeyword("_EMISSION");
            }

            if (crtFeed == null)
            {
                crtFeed = screenRenderer.GetComponent<ProceduralCRTFeed>();
                if (crtFeed == null)
                {
                    crtFeed = screenRenderer.gameObject.AddComponent<ProceduralCRTFeed>();
                }
            }

            crtFeed.BindMaterial(runtimeScreenMat);
        }

        private void Update()
        {
            AnimateScreenVisuals();

            if (currentState == TVState.Day3_SilentGlass)
            {
                if ((DayManager.Instance != null && (DayManager.Instance.IsTransitioning || DayManager.Instance.IsDemoEnded)) ||
                    Canal420.UI.ObjectiveHUD.IsBlockingModalOpen)
                {
                    if (day3ShockFlashTimer > 0f)
                    {
                        day3ShockFlashTimer = 0f;
                        SetScreenVisual(new Color(0.02f, 0.02f, 0.03f), 0f, false);
                    }
                }
                else
                {
                    UpdateDay3DeadGlassResidualPulse();
                }
            }

            if (currentState == TVState.Day1_SeatedSurfing &&
                Time.timeScale > 0.01f &&
                !ObjectiveHUD.IsBlockingModalOpen &&
                !ObjectiveHUD.WasModalClosedRecently(2) &&
                (DayManager.Instance == null || (!DayManager.Instance.IsTransitioning && !DayManager.Instance.IsDemoEnded)) &&
                Time.frameCount > frameSeated + 2)
            {
                bool readyToSwitch = (Time.time - lastChannelSwitchTime) >= channelSwitchCooldown;
                if (readyToSwitch && !day1PromptCurrentlyShown)
                {
                    day1PromptCurrentlyShown = true;
                    InteractionSystem.Instance?.SetOverridePrompt(pendingDay1PromptText);
                }
                else if (!readyToSwitch && day1PromptCurrentlyShown)
                {
                    day1PromptCurrentlyShown = false;
                    InteractionSystem.Instance?.ClearOverridePrompt();
                }

                bool interactionSuppressed = InteractionSystem.Instance != null && InteractionSystem.Instance.IsInteractionSuppressed;
                if (readyToSwitch && !interactionSuppressed && Cursor.lockState == CursorLockMode.Locked && WasRemoteButtonPressed())
                {
                    lastChannelSwitchTime = Time.time;
                    day1PromptCurrentlyShown = false;
                    InteractionSystem.Instance?.ClearOverridePrompt();
                    AdvanceDay1Channel();
                }
            }
        }

        private void UpdateDay3DeadGlassResidualPulse()
        {
            if (day3ShockFlashTimer > 0f || day3ShockFlashRemainingFrames > 0)
            {
                if (day3ShockFlashRemainingFrames > 0)
                    day3ShockFlashRemainingFrames--;
                if (day3ShockFlashTimer > 0f)
                    day3ShockFlashTimer -= Time.deltaTime;
                if (day3ShockFlashTimer <= 0f && day3ShockFlashRemainingFrames <= 0)
                {
                    SetScreenVisual(new Color(0.02f, 0.02f, 0.03f), 0f, false);
                }
                return;
            }

            day3ResidualTimer -= Time.deltaTime;
            if (day3ResidualTimer > 0f)
                return;

            Camera cam = Camera.main;
            if (cam == null)
            {
                day3ResidualTimer = 10f;
                return;
            }

            Vector3 tvPos = screenRenderer != null ? screenRenderer.bounds.center : transform.position;
            Vector3 toTv = tvPos - cam.transform.position;
            float dist = toTv.magnitude;
            if (dist < 6.4f && dist > 1.2f && Vector3.Dot(cam.transform.forward, toTv.normalized) < -0.22f)
            {
                day3ResidualTimer = UnityEngine.Random.Range(24f, 36f);
                day3ShockFlashTimer = 0.095f;
                day3ShockFlashRemainingFrames = MinDay3ShockVisibleFrames;
                InitializeScreenMaterial();
                if (crtFeed != null)
                {
                    crtFeed.TriggerShockFrame(0.10f, residualOnly: true);
                }
                SetScreenVisual(new Color(0.42f, 0.48f, 0.54f), 0.85f, true);
                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley3DPanned(FoleySoundId.TVSubliminalBurst, tvPos, 0.34f, minDistance: 1.2f, maxDistance: 10f, pitchMultiplier: 1.15f);
                }
            }
            else
            {
                day3ResidualTimer = 2.0f;
            }
        }

        private bool WasRemoteButtonPressed()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
                return true;
            return false;
        }

        public string GetInteractionPrompt()
        {
            switch (currentState)
            {
                case TVState.Day1_OffWaitingToSit:
                    return "Sentar no sofá e ligar a TV";
                case TVState.Day2_OffIdle:
                    return "Tocar a tela da TV";
                case TVState.Day2_AutonomousNewsWaiting:
                    return "Ver o que está passando";
                case TVState.Day3_SilentGlass:
                    return "Olhar a TV";
                default:
                    return string.Empty;
            }
        }

        public void Interact(GameObject interactor)
        {
            if (DayManager.Instance != null && (DayManager.Instance.IsTransitioning || DayManager.Instance.IsDemoEnded))
                return;

            switch (currentState)
            {
                case TVState.Day1_OffWaitingToSit:
                    TryStartDay1TVSession(interactor);
                    break;

                case TVState.Day2_OffIdle:
                    if (Canal420AudioManager.Instance != null)
                    {
                        Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.GlassInspect, 0.65f);
                    }
                    if (InteractionSystem.Instance != null)
                    {
                        InteractionSystem.Instance.ShowSubtitle(
                            "O vidro ainda tá morno. Não lembro a que horas desliguei isso.",
                            5.6f
                        );
                    }
                    break;

                case TVState.Day2_AutonomousNewsWaiting:
                    if (activeSequenceCoroutine != null) StopCoroutine(activeSequenceCoroutine);
                    activeSequenceCoroutine = StartCoroutine(RunDay2NewsAndInvasionSequence(interactor));
                    break;

                case TVState.Day3_SilentGlass:
                    if (Canal420AudioManager.Instance != null)
                    {
                        Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.GlassInspect, 0.55f);
                    }
                    if (InteractionSystem.Instance != null)
                    {
                        InteractionSystem.Instance.ShowSubtitle(
                            "Sem luz na sala, mas o tubo ainda dá uns estalos por dentro.",
                            5.4f
                        );
                    }
                    break;
            }
        }

        private void TryStartDay1TVSession(GameObject interactor)
        {
            if (DayManager.Instance != null && !DayManager.Instance.CanSitAtTVDay1(out string blockedReason, out bool missingOnlyBatteries))
            {
                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley2D(
                        missingOnlyBatteries ? FoleySoundId.BatterySnap : FoleySoundId.TVChannelClick,
                        0.72f
                    );
                }

                if (missingOnlyBatteries)
                {
                    OnMissingRemoteBatteriesAttempted?.Invoke();
                }

                if (InteractionSystem.Instance != null)
                {
                    InteractionSystem.Instance.ShowSubtitle(blockedReason, 5.8f);
                }
                return;
            }

            PlayerController player = interactor != null
                ? interactor.GetComponent<PlayerController>()
                : FindAnyObjectByType<PlayerController>();

            if (activeSequenceCoroutine != null)
            {
                StopCoroutine(activeSequenceCoroutine);
            }
            activeSequenceCoroutine = StartCoroutine(RunSitDownAndTurnOnTVRoutine(player));
        }

        private IEnumerator RunSitDownAndTurnOnTVRoutine(PlayerController player)
        {
            currentState = TVState.Day1_SittingTransition;

            if (ObjectiveHUD.Instance != null)
            {
                ObjectiveHUD.Instance.MarkCompleted("ligar_tv_dia1");
                ObjectiveHUD.Instance.SetHUDVisible(false);
            }

            Vector3 tvLookTarget = screenRenderer != null ? screenRenderer.bounds.center : transform.position;

            if (player != null)
            {
                if (DayManager.Instance != null && DayManager.Instance.SofaSitPoint != null)
                {
                    DayManager.Instance.SetSofaCollisionEnabled(false);
                    yield return player.SmoothSitAtPointRoutine(DayManager.Instance.SofaSitPoint, tvLookTarget, 1.12f, 0.68f);
                }
                else
                {
                    player.FaceWorldTarget(tvLookTarget);
                }

                player.EnableRestrictedLook(38f, -18f, 20f, customEyeHeight: 1.12f);
            }

            yield return new WaitForSeconds(0.12f);

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.TVChannelClick, 0.90f);
                Canal420AudioManager.Instance.SetTVLoopMode(TVAudioLoopMode.NormalBroadcast, transform);
            }

            SetScreenVisual(new Color(0.75f, 0.84f, 0.96f), 2.4f, true);
            crtFeed?.SetMode(CRTFeedMode.BroadcastChannel04, renderImmediately: false);
            crtFeed?.TriggerChannelSwitchBurst(0.26f);
            yield return new WaitForSeconds(0.14f);

            frameSeated = Time.frameCount;
            lastChannelSwitchTime = Time.time;
            day1ChannelIndex = 0;
            pendingDay1PromptText = "[E] Trocar de canal";
            day1PromptCurrentlyShown = false;
            currentState = TVState.Day1_SeatedSurfing;

            if (tvAudioSource != null && tvAudioSource.clip != null) tvAudioSource.Play();

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ClearOverridePrompt();
                InteractionSystem.Instance.ShowSubtitle(
                    "[CANAL 04]: \"...o enxugamento no setor administrativo já fechou mais de quatro mil vagas de escritório só neste semestre no estado...\"",
                    6.6f
                );
            }

            activeSequenceCoroutine = null;
        }

        private void AdvanceDay1Channel()
        {
            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.TVChannelClick, 0.85f);
            }

            if (day1ChannelIndex == 0)
            {
                day1ChannelIndex = 1;
                crtFeed?.SetMode(CRTFeedMode.BroadcastChannel09, renderImmediately: false);
                crtFeed?.TriggerChannelSwitchBurst(0.22f);
                if (InteractionSystem.Instance != null)
                {
                    InteractionSystem.Instance.ShowSubtitle(
                        "[CANAL 09]: \"...frente fria estacionada na serra mantém neblina fechada na Baixada até quarta-feira...\" A antena tá puxando dois canais juntos.",
                        6.0f
                    );
                }
            }
            else if (day1ChannelIndex == 1)
            {
                day1ChannelIndex = 2;
                crtFeed?.SetMode(CRTFeedMode.BroadcastChannel17, renderImmediately: false);
                crtFeed?.TriggerChannelSwitchBurst(0.22f);
                if (InteractionSystem.Instance != null)
                {
                    InteractionSystem.Instance.ShowSubtitle(
                        "[CANAL 17]: \"...café passado em quatro minutos, direto na jarra térmica! Ligue 0800...\" Nem de madrugada param de tentar vender coisa.",
                        6.2f
                    );
                }
            }
            else if (day1ChannelIndex == 2)
            {
                day1ChannelIndex = 3;
                crtFeed?.SetMode(CRTFeedMode.Canal420Ghost, renderImmediately: false);
                crtFeed?.TriggerChannelSwitchBurst(0.26f);
                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.SetTVLoopMode(TVAudioLoopMode.Canal420Hypnotic, transform);
                    Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.DreadSubPulse, 0.65f);
                }
                if (SanityEffects.Instance != null)
                {
                    SanityEffects.Instance.TriggerHallucinationSpike(0.38f, 1.2f);
                }

                if (InteractionSystem.Instance != null)
                {
                    pendingDay1PromptText = "[E] Mudar de canal";
                    InteractionSystem.Instance.ShowSubtitle(
                        "Nunca entrou sinal nessa faixa. Essas cores estão dando um sono estranho.",
                        6.8f
                    );
                }
            }
            else if (day1ChannelIndex == 3)
            {
                day1ChannelIndex = 4;
                crtFeed?.TriggerChannelSwitchBurst(0.20f);
                if (SanityEffects.Instance != null)
                {
                    SanityEffects.Instance.TriggerHallucinationSpike(0.54f, 1.5f);
                }

                if (InteractionSystem.Instance != null)
                {
                    pendingDay1PromptText = "[E] Desligar a TV";
                    InteractionSystem.Instance.ShowSubtitle(
                        "Não mudou. A imagem tá derretendo... parece um corredor lá dentro.",
                        6.5f
                    );
                }
            }
            else
            {
                if (activeSequenceCoroutine != null) StopCoroutine(activeSequenceCoroutine);
                activeSequenceCoroutine = StartCoroutine(RunDay1ChannelLockSequence());
            }
        }

        private IEnumerator RunDay1ChannelLockSequence()
        {
            currentState = TVState.Day1_LockedOn420;

            PlayerController player = FindAnyObjectByType<PlayerController>();

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ClearOverridePrompt();
                InteractionSystem.Instance.ShowSubtitle(
                    "Apertei pra desligar e não vai. Meu braço tá pesado... não tô conseguindo levantar.",
                    6.8f
                );
            }

            if (SanityEffects.Instance != null)
            {
                SanityEffects.Instance.TriggerHallucinationSpike(0.55f, 6.2f, allowSubliminalFlash: false);
            }

            day1HypnosisProgress = 0f;
            float lockDur = 6.4f;
            float elapsed = 0f;
            while (elapsed < lockDur)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / lockDur);
                day1HypnosisProgress = u;
                float heavyCurve = Mathf.SmoothStep(0f, 1f, u);
                if (player != null)
                {
                    float droopPitch = Mathf.Lerp(0f, 14.0f, heavyCurve);
                    float droopRoll = Mathf.Lerp(0f, -3.2f, heavyCurve);
                    float narrowFov = Mathf.Lerp(0f, -10.5f, heavyCurve);
                    player.SetPostureAndFovOffset(droopPitch, droopRoll, narrowFov);
                }
                yield return null;
            }

            activeSequenceCoroutine = null;
            if (DayManager.Instance != null)
            {
                // Fade out suave como se Cian estivesse adormecendo no sofá (sem glitch final nem corte seco)
                DayManager.Instance.TransitionToDay(GameDay.Day2, forceImmediateOverride: false, instantCut: false);
            }
        }

        public void ActivateDay2AutonomousNews()
        {
            if (currentState != TVState.Day2_OffIdle)
                return;
            if (DayManager.Instance != null && (DayManager.Instance.IsTransitioning || DayManager.Instance.IsDemoEnded))
                return;

            currentState = TVState.Day2_AutonomousNewsWaiting;
            crtFeed?.SetMode(CRTFeedMode.News1994Broadcast, renderImmediately: false);
            crtFeed?.TriggerChannelSwitchBurst(0.28f);

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.TVChannelClick, transform.position, 1.0f);
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.DreadSubPulse, 0.72f);
                Canal420AudioManager.Instance.SetTVLoopMode(TVAudioLoopMode.NormalBroadcast, transform);
            }

            if (SanityEffects.Instance != null)
            {
                SanityEffects.Instance.TriggerHallucinationSpike(0.42f, 0.85f);
            }

            if (ObjectiveHUD.Instance != null)
            {
                ObjectiveHUD.Instance.AddOrUpdateObjective(
                    "assistir_plantao_dia2",
                    "Ver o que ligou na sala",
                    completed: false,
                    visible: true
                );
            }

            if (InteractionSystem.Instance != null)
            {
                bool alreadyInLivingRoom = false;
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 p = cam.transform.position;
                    alreadyInLivingRoom = p.z < -0.55f && p.x > -2.70f && p.x < 1.35f;
                }

                InteractionSystem.Instance.ShowSubtitle(
                    alreadyInLivingRoom
                        ? "A televisão ligou sozinha..."
                        : "Tem alguém na sala? A televisão ligou sozinha.",
                    5.8f
                );
            }
        }

        private IEnumerator RunDay2NewsAndInvasionSequence(GameObject interactor)
        {
            currentState = TVState.Day2_PlayingNewsSequence;

            if (InteractionSystem.Instance != null && InteractionSystem.Instance.IsDocumentOpen)
            {
                InteractionSystem.Instance.CloseDocument(restorePlayerControls: false);
            }

            if (ObjectiveHUD.Instance != null)
            {
                ObjectiveHUD.Instance.MarkCompleted("assistir_plantao_dia2");
                ObjectiveHUD.Instance.SetHUDVisible(false);
            }

            PlayerController player = interactor != null
                ? interactor.GetComponent<PlayerController>()
                : FindAnyObjectByType<PlayerController>();

            Vector3 tvLookTarget = screenRenderer != null ? screenRenderer.bounds.center : transform.position;
            if (player != null)
            {
                player.SetControlsEnabled(moveEnabled: false, lookEnabled: false, lockCursor: true);
                yield return player.SmoothLookAtRoutine(tvLookTarget, 0.46f, targetFovDelta: -3.0f);
                player.SetControlsEnabled(moveEnabled: false, lookEnabled: true, lockCursor: true);
            }

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ShowSubtitle(
                    "[PLANTÃO]: \"...a Polícia Civil isolou agora à noite uma casa no fim da Rua das Acácias, após vizinhos ligarem pro 190 ouvindo gritos e pancadas...\"",
                    6.8f
                );
            }

            yield return new WaitForSeconds(7.4f);

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ShowSubtitle(
                    "[PLANTÃO]: \"...a mulher e as duas filhas do casal foram encontradas mortas dentro do quarto dos fundos, trancado por fora. O marido continuava sentado na sala.\"",
                    6.8f
                );
            }

            yield return new WaitForSeconds(7.4f);

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ShowSubtitle(
                    "Mortas...? Dez de setembro de noventa e quatro. Se a polícia entrou aqui naquela noite... como eu ainda tô nessa sala?",
                    7.2f
                );
            }

            yield return new WaitForSeconds(7.6f);

            day1ChannelIndex = 99;
            if (crtFeed != null)
            {
                crtFeed.TriggerChannelSwitchBurst(0.28f);
            }
            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.SetTVLoopMode(TVAudioLoopMode.NewsStaticBleed, transform);
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.DreadSubPulse, 1.0f);
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.TVSubliminalBurst, 0.92f, 0.90f);
            }
            if (SanityEffects.Instance != null)
            {
                SanityEffects.Instance.TriggerHallucinationSpike(0.95f, 5.5f, allowSubliminalFlash: true, SubliminalType.StaticTear);
            }

            if (player != null)
            {
                player.SetControlsEnabled(moveEnabled: false, lookEnabled: false, lockCursor: true);
                StartCoroutine(player.SmoothLookAtRoutine(tvLookTarget, 0.55f, targetFovDelta: -11.5f));
                player.TriggerCameraShake(1.65f, 5.2f);
            }

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ShowSubtitle(
                    "Tá entrando aquele mesmo canal por cima!",
                    5.4f
                );
            }

            float bleedElapsed = 0f;
            float bleedTotal = 5.4f;
            bool pulsedMidShock = false;
            float nextLampFlicker = 0.25f;
            Light floorLamp = FindSalaFloorLampLight();
            while (bleedElapsed < bleedTotal)
            {
                bleedElapsed += Time.deltaTime;

                if (bleedElapsed >= nextLampFlicker && floorLamp != null)
                {
                    bool lit = UnityEngine.Random.value > 0.45f;
                    floorLamp.enabled = lit;
                    Canal420.Core.DayManager.SyncSecondaryFixtureVisual(floorLamp, lit, floorLamp.color, lit ? 0.72f : 0f);
                    nextLampFlicker = bleedElapsed + UnityEngine.Random.Range(0.08f, 0.26f);
                }

                if (!pulsedMidShock && bleedElapsed >= 3.1f)
                {
                    pulsedMidShock = true;
                    if (crtFeed != null) crtFeed.TriggerShockFrame(0.11f, residualOnly: false);
                    if (HorrorScreenOverlay.Instance != null)
                    {
                        HorrorScreenOverlay.Instance.TriggerSubliminalFlash(SubliminalType.StaticTear, 0.095f, 0.34f);
                    }
                    if (Canal420AudioManager.Instance != null)
                    {
                        Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.TVSubliminalBurst, 0.96f, 1.06f);
                    }
                }

                yield return null;
            }

            if (floorLamp != null)
            {
                floorLamp.enabled = false;
                Canal420.Core.DayManager.SyncSecondaryFixtureVisual(floorLamp, false, Color.black, 0f);
            }

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.FuseClackSpark, transform.position, 1.0f);
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.PowerCutDown, 0.92f, 0.96f);
                Canal420AudioManager.Instance.SetTVLoopMode(TVAudioLoopMode.Off, transform);
            }
            SetScreenVisual(Color.black, 0f, false);
            if (crtFeed != null)
            {
                crtFeed.SetMode(CRTFeedMode.Off);
            }
            activeSequenceCoroutine = null;

            if (DayManager.Instance != null)
            {
                if (player != null)
                {
                    DayManager.Instance.RecordDay2EndPose(player.transform.position, player.transform.eulerAngles.y);
                }
                DayManager.Instance.TransitionToDay(GameDay.Day3, forceImmediateOverride: false, instantCut: true);
            }
        }

        public void ConfigureForDay(GameDay day)
        {
            InitializeScreenMaterial();

            StopAllCoroutines();
            activeSequenceCoroutine = null;

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ClearOverridePrompt();
            }

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.SetTVLoopMode(TVAudioLoopMode.Off, transform);
            }

            day1ChannelIndex = 0;
            day1HypnosisProgress = 0f;
            day1PromptCurrentlyShown = false;
            pendingDay1PromptText = "[E] Trocar de canal";
            day3ResidualTimer = UnityEngine.Random.Range(16f, 24f);
            day3ShockFlashTimer = 0f;
            day3ShockFlashRemainingFrames = 0;
            crtFeed?.SetMode(CRTFeedMode.Off);

            switch (day)
            {
                case GameDay.Day1:
                    currentState = TVState.Day1_OffWaitingToSit;
                    SetScreenVisual(new Color(0.02f, 0.02f, 0.03f), 0f, false);
                    break;

                case GameDay.Day2:
                    currentState = TVState.Day2_OffIdle;
                    SetScreenVisual(new Color(0.02f, 0.02f, 0.03f), 0f, false);
                    break;

                case GameDay.Day3:
                    currentState = TVState.Day3_SilentGlass;
                    SetScreenVisual(new Color(0.02f, 0.02f, 0.03f), 0f, false);
                    break;

                case GameDay.Day4_Prelude:
                    currentState = TVState.Disabled;
                    SetScreenVisual(Color.black, 0f, false);
                    break;
            }
        }

        public void SetVisualReferences(Renderer screen, Light glowLight)
        {
            screenRenderer = screen;
            tvGlowLight = glowLight;
            runtimeScreenMat = null;
            if (Application.isPlaying)
            {
                InitializeScreenMaterial();
            }
        }

        private void AnimateScreenVisuals()
        {
            float t = Time.time;

            if (currentState == TVState.Day1_SeatedSurfing)
            {
                if (day1ChannelIndex == 0)
                {
                    crtFeed?.SetMode(CRTFeedMode.BroadcastChannel04);
                    float shimmer = 0.94f + Mathf.Sin(t * 7f) * 0.06f;
                    ApplyLiveCRTLighting(shimmer, 1.35f, new Color(0.42f, 0.56f, 0.80f), 1.45f, 2.55f);
                }
                else if (day1ChannelIndex == 1)
                {
                    crtFeed?.SetMode(CRTFeedMode.BroadcastChannel09);
                    float pulse = 0.92f + Mathf.PerlinNoise(t * 10f, 0f) * 0.16f;
                    ApplyLiveCRTLighting(pulse, 1.35f, new Color(0.32f, 0.72f, 0.56f), 1.50f, 2.65f);
                }
                else if (day1ChannelIndex == 2)
                {
                    crtFeed?.SetMode(CRTFeedMode.BroadcastChannel17);
                    float warm = 0.92f + Mathf.Sin(t * 5.2f) * 0.10f;
                    ApplyLiveCRTLighting(warm, 1.38f, new Color(0.86f, 0.62f, 0.24f), 1.55f, 2.70f);
                }
                else
                {
                    crtFeed?.SetMode(CRTFeedMode.Canal420Ghost);
                    crtFeed?.SetHauntProgress(0.28f);
                    float pulse = 0.92f + 0.28f * Mathf.Sin(t * 6.5f);
                    ApplyLiveCRTLighting(pulse, 1.50f, new Color(0.68f, 0.30f, 0.88f), 1.75f, 3.35f);
                }
            }
            else if (currentState == TVState.Day1_LockedOn420)
            {
                crtFeed?.SetMode(CRTFeedMode.Canal420Ghost);
                float haunt = day1HypnosisProgress < 0.70f
                    ? Mathf.Lerp(0.28f, 0.54f, day1HypnosisProgress / 0.70f)
                    : Mathf.Lerp(0.54f, 0.82f, Mathf.SmoothStep(0f, 1f, (day1HypnosisProgress - 0.70f) / 0.30f));
                crtFeed?.SetHauntProgress(haunt);
                float dimTail = 1f - Mathf.SmoothStep(0.70f, 1f, day1HypnosisProgress) * 0.45f;
                float basePulse = (0.88f + 0.24f * Mathf.Sin(t * 3.8f)) * dimTail;
                ApplyLiveCRTLighting(basePulse, 1.42f * dimTail, new Color(0.66f, 0.28f, 0.82f), 1.35f * dimTail, 2.85f * dimTail);
            }
            else if (currentState == TVState.Day2_AutonomousNewsWaiting ||
                     (currentState == TVState.Day2_PlayingNewsSequence && day1ChannelIndex != 99))
            {
                crtFeed?.SetMode(CRTFeedMode.News1994Broadcast);
                float flicker = 0.90f + Mathf.PerlinNoise(t * 11f, 1f) * 0.22f;
                ApplyLiveCRTLighting(flicker, 1.42f, new Color(0.45f, 0.56f, 0.78f), 1.35f, 3.85f);
            }
            else if (currentState == TVState.Day2_PlayingNewsSequence && day1ChannelIndex == 99)
            {
                crtFeed?.SetMode(CRTFeedMode.Canal420Bleed);
                float strobe = 0.80f + 0.58f * Mathf.PerlinNoise(t * 20f, 3.7f);
                ApplyLiveCRTLighting(strobe, 1.58f, new Color(0.84f, 0.22f, 0.34f), 0.95f, 4.20f);
            }
            else if (crtFeed != null && currentState != TVState.Day3_SilentGlass)
            {
                crtFeed.SetMode(CRTFeedMode.Off);
            }
        }

        private void ApplyLiveCRTLighting(
            float screenGainPulse,
            float screenEmissionBase,
            Color fallbackLightColor,
            float minLightIntensity,
            float maxLightIntensity)
        {
            Color targetColor = fallbackLightColor;
            float brightness01 = 0.45f;

            if (crtFeed != null && crtFeed.ActiveMode != CRTFeedMode.Off)
            {
                targetColor = crtFeed.DominantFrameColor;
                brightness01 = Mathf.Clamp01(crtFeed.CurrentBrightness01);
            }

            // Smooth fast response so camera cuts, siren flashes, and acid hues shift the room visibly
            smoothedTvLightColor = Color.Lerp(smoothedTvLightColor, targetColor, Time.deltaTime * 15f);
            float targetIntensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, brightness01) * Mathf.Clamp(screenGainPulse, 0.65f, 1.45f);
            smoothedTvLightIntensity = Mathf.Lerp(smoothedTvLightIntensity, Mathf.Clamp(targetIntensity, 0.65f, 4.35f), Time.deltaTime * 16f);

            // Keep CRT quad true-to-texture while preserving phosphor glow
            SetScreenVisual(
                Color.white,
                screenEmissionBase * Mathf.Clamp(screenGainPulse, 0.75f, 1.45f),
                true,
                smoothedTvLightColor,
                smoothedTvLightIntensity
            );
        }

        private Light FindSalaFloorLampLight()
        {
            if (cachedSalaFloorLamp != null) return cachedSalaFloorLamp;

            Light[] allLights = FindObjectsByType<Light>(FindObjectsInactive.Include);
            for (int i = 0; i < allLights.Length; i++)
            {
                Light l = allLights[i];
                if (l == null) continue;
                string full = (l.name + " " + (l.transform.parent != null ? l.transform.parent.name : string.Empty)).ToLowerInvariant();
                if (full.Contains("luminaria_coluna_sala"))
                {
                    cachedSalaFloorLamp = l;
                    break;
                }
            }
            return cachedSalaFloorLamp;
        }

        public void SetTvLightShadowMode(LightShadows shadowMode)
        {
            if (tvGlowLight != null)
            {
                tvGlowLight.shadows = shadowMode;
            }
        }

        private void SetScreenVisual(
            Color screenTint,
            float emissionMultiplier,
            bool enableLight,
            Color? overrideGlowColor = null,
            float? overrideGlowIntensity = null)
        {
            if (runtimeScreenMat != null)
            {
                Color safeBase = enableLight ? screenTint : new Color(0.030f, 0.036f, 0.044f, 1f);
                if (runtimeScreenMat.HasProperty("_BaseColor"))
                    runtimeScreenMat.SetColor("_BaseColor", safeBase);
                else
                    runtimeScreenMat.color = safeBase;

                if (runtimeScreenMat.HasProperty("_EmissionColor"))
                    runtimeScreenMat.SetColor("_EmissionColor", enableLight ? screenTint * emissionMultiplier : Color.black);
            }

            if (tvGlowLight != null)
            {
                tvGlowLight.enabled = enableLight;
                if (enableLight)
                {
                    tvGlowLight.color = overrideGlowColor ?? screenTint;
                    tvGlowLight.intensity = overrideGlowIntensity ?? Mathf.Clamp(emissionMultiplier * 1.15f, 0.45f, 3.2f);
                }
            }
        }
    }
}
