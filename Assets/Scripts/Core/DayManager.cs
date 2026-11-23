using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Canal420.Audio;
using Canal420.Effects;
using Canal420.Events;
using Canal420.Interaction;
using Canal420.Player;
using Canal420.Puzzles;
using Canal420.UI;

namespace Canal420.Core
{
    public enum GameDay
    {
        Day1 = 1,
        Day2 = 2,
        Day3 = 3,
        Day4_Prelude = 4
    }

    public class DayManager : MonoBehaviour
    {
        public static DayManager Instance { get; private set; }

        [Header("Estado Inicial")]
        [SerializeField] private GameDay startingDay = GameDay.Day1;
        [SerializeField] private bool showOpeningTitleOnStart = true;

        [Header("Logo do Estúdio (Introdução)")]
        [SerializeField] private bool showStudioLogoOnStart = true;
        [SerializeField] private Texture2D studioLogoTexture;
        [SerializeField] private float studioLogoFadeInDuration = 1.5625f;
        [SerializeField] private float studioLogoHoldDuration = 2.25f;
        [SerializeField] private float studioLogoFadeOutDuration = 1.4375f;
        [SerializeField] private float studioLogoPostBlackPause = 0.45f;

        [Header("Grupos da Hierarquia (Cena Única)")]
        [SerializeField] private GameObject casaBase;
        [SerializeField] private GameObject propsDia1;
        [SerializeField] private GameObject propsDia2;
        [SerializeField] private GameObject propsDia3;
        [SerializeField] private GameObject quartoFilhas;

        [Header("Pontos de Posicionamento (Spawn Points)")]
        [SerializeField] private Transform spawnDia1;
        [SerializeField] private Transform spawnSofaSentado;
        [SerializeField] private Transform spawnSofaLevantar;
        [SerializeField] private Transform spawnDia2Sofa;
        [SerializeField] private Transform spawnDia3;
        [SerializeField] private Transform spawnDia4QuartoFilhas;

        [Header("Referências Principais")]
        [SerializeField] private PlayerController player;
        [SerializeField] private SanityEffects sanityEffects;
        [SerializeField] private TVController tvController;
        [SerializeField] private ObjectiveHUD objectiveHUD;
        [SerializeField] private Canal420AudioManager audioManager;
        [SerializeField] private DoorInteractable officeDoor;
        [SerializeField] private DoorInteractable corridorLockedDoor;
        [SerializeField] private Transform sofaRootTransform;
        [SerializeField] private Light houseMainLight;
        [SerializeField] private Light[] houseInteriorLights;
        [SerializeField] private Light day3RestoredCircuitLight;
        [SerializeField] private Light[] secondaryMotivatedLights;
        [SerializeField] private Light[] architecturalFillLights;
        [SerializeField] private ReflectionProbe[] interiorReflectionProbes;

        [Header("Progressão do Dia 1 (Burnout + Chave do Escritório + Pilhas)")]
        [SerializeField] private bool requireDay1TasksBeforeTV = true;
        [SerializeField] private List<string> day1RequiredTaskIds = new List<string>
        {
            "lavar_rosto",
            "checar_secretaria",
            "olhar_papeis",
            "checar_calendario"
        };

        [Header("Progressão do Dia 2 (Depressoras + Fita da Secretária + Corredor -> Plantão 1994)")]
        [SerializeField] private int day2EventsNeededForTV = 5;

        [Header("Dia 3: Jumpscare de Margaret na Porta")]
        [SerializeField] private GameObject margaretJumpscareObject;
        [SerializeField] private AudioSource margaretJumpscareAudio;

        [Header("Dia 4: Prelúdio no Quarto das Filhas")]
        [SerializeField] private float day4PreludeDuration = 17.5f;
        private GameDay currentDay;
        private GameDay lastAppliedLightingDay;
        private readonly HashSet<string> completedDay1Tasks = new HashSet<string>();
        private readonly HashSet<string> completedDay2LinearEvents = new HashSet<string>();
        private readonly HashSet<string> completedDay3Clues = new HashSet<string>();
        private bool day2NewsTriggered = false;
        private bool day3SafeUnlocked = false;
        private bool day3DoorTrapWaitingForEntry = false;
        private bool isTransitioning = false;
        private bool demoEnded = false;
        private bool corridorDoorSubscribed = false;
        private bool officeDoorSubscribed = false;

        private HallucinationTrigger day2ShadowTrigger;
        private HallucinationTrigger day2FramesTrigger;
        private HallucinationTrigger day2WhispersTrigger;

        private Light eastCorridorLight;
        private Light centerCorridorLight;
        private Light westCorridorLight;
        private Light daughtersRoomLight;
        private Light margaretFlashLight;
        private Light margaretFaceGrazeLight;
        private float daughtersRoomTrapScale = 0f;
        private float eastCorridorBaseIntensity = 0.45f;
        private int corridorAutoSputterSuppressDepth = 0;
        private float userBrightnessNormalized = 0.50f;
        private bool softShadowsEnabledByQuality = true;
        private bool brightShadowsEnabled = false;
        private int lastProbeRefreshFrame = -1;
        private bool suppressNextProbeRefresh = false;

        private PsychologicalHorrorDirector horrorDirector;
        private MirrorHauntController mirrorHaunt;
        private FlashlightController flashlightController;
        private InspectableObject cachedFrontDoorInspectable;
        private GameObject cachedKitchenDrawerFlashlightVisual;
        private readonly List<WatcherHallucination> cachedWatchers = new List<WatcherHallucination>();
        private Collider[] cachedSofaColliders;

        private bool hasRecordedDay2EndPose = false;
        private Vector3 recordedDay2EndPos = new Vector3(-0.84f, 0.02f, -4.55f);
        private float recordedDay2EndYaw = 180f;

        private bool day3MandatoryPillTaken = false;
        private bool day3BaseStimulantActive = false;
        private bool pendingDay3NewspaperCrash = false;
        private bool isStimulantBoostOrReboundActive = false;

        private Coroutine activeTransitionCoroutine;
        private Coroutine day4EndingCoroutine;
        private Coroutine day2BeckonCoroutine;
        private Coroutine day3CircuitRestoreCoroutine;
        private Coroutine day3ClueReactionCoroutine;
        private Coroutine day3SiegeCoroutine;
        private Coroutine day3DoorTrapCoroutine;
        private Coroutine day2DoorScareCoroutine;
        private Coroutine day2TVActivationCoroutine;
        private Coroutine activeCutsceneLookCoroutine;
        private Coroutine stimulantEffectCoroutine;
        private Coroutine probeRefreshCoroutine;

        private GameObject transitionCanvasObject;
        private CanvasGroup blackFadeGroup;
        private CanvasGroup titleCanvasGroup;
        private CanvasGroup subCanvasGroup;
        private Text dayCardTitleText;
        private Text dayCardSubText;
        private GameObject endDemoHintObj;

        private GameObject studioLogoRoot;
        private CanvasGroup studioLogoGroup;

        public GameDay CurrentDay => currentDay;
        public Transform SofaSitPoint => spawnSofaSentado;
        public bool IsTransitioning => isTransitioning;
        public bool IsDemoEnded => demoEnded;
        public bool IsStimulantBoostOrReboundActive => isStimulantBoostOrReboundActive;

        private void Awake()
        {
            Instance = this;
            userBrightnessNormalized = Mathf.Clamp(PlayerPrefs.GetFloat(GameSettingsUI.PrefKeyBrightness, 0.50f), 0f, GameSettingsUI.MaxBrightness);
            softShadowsEnabledByQuality = PlayerPrefs.GetInt(GameSettingsUI.PrefKeyQuality, 2) > 0;
            brightShadowsEnabled = PlayerPrefs.GetInt(GameSettingsUI.PrefKeyBrightShadows, 0) == 1;

            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                flashlightController = player.GetComponent<FlashlightController>();
                if (flashlightController == null) flashlightController = player.gameObject.AddComponent<FlashlightController>();
            }
            if (sanityEffects == null) sanityEffects = FindAnyObjectByType<SanityEffects>();
            if (tvController == null) tvController = FindAnyObjectByType<TVController>();
            if (objectiveHUD == null) objectiveHUD = GetComponent<ObjectiveHUD>();
            if (objectiveHUD == null) objectiveHUD = FindAnyObjectByType<ObjectiveHUD>();
            if (objectiveHUD == null) objectiveHUD = gameObject.AddComponent<ObjectiveHUD>();

            if (audioManager == null) audioManager = GetComponent<Canal420AudioManager>();
            if (audioManager == null) audioManager = FindAnyObjectByType<Canal420AudioManager>();
            if (audioManager == null) audioManager = gameObject.AddComponent<Canal420AudioManager>();

            horrorDirector = GetComponent<PsychologicalHorrorDirector>();
            if (horrorDirector == null) horrorDirector = FindAnyObjectByType<PsychologicalHorrorDirector>();
            if (horrorDirector == null) horrorDirector = gameObject.AddComponent<PsychologicalHorrorDirector>();

            HorrorScreenOverlay.EnsureExists();

            if (GetComponent<GameSettingsUI>() == null && FindAnyObjectByType<GameSettingsUI>() == null)
            {
                gameObject.AddComponent<GameSettingsUI>();
            }

            if (margaretJumpscareObject != null)
            {
                margaretJumpscareObject.SetActive(false);
            }

            CacheSpecialCorridorLights();
            DisableLegacyHeadlightAndMainLight();
            ValidateSpawnReferences();
            BuildTransitionOverlayUI();

            if (showOpeningTitleOnStart && showStudioLogoOnStart && startingDay == GameDay.Day1)
            {
                EnsureStudioLogoUIReady();
                if (player != null)
                {
                    player.SetControlsEnabled(moveEnabled: false, lookEnabled: false, lockCursor: true);
                }
                if (audioManager != null)
                {
                    float silenceSpan = 0.25f + Mathf.Max(0.15f, studioLogoFadeInDuration) + Mathf.Max(0f, studioLogoHoldDuration) + Mathf.Max(0.15f, studioLogoFadeOutDuration);
                    audioManager.PulseSilenceDread(silenceSpan, 0.35f);
                }
            }
        }

        private void OnEnable()
        {
            InspectableObject.OnTaskCompleted += HandleTaskCompleted;
            InspectableObject.OnLockedRequirementAttempted += HandleLockedRequirementAttempted;
            TVController.OnMissingRemoteBatteriesAttempted += HandleDay1MissingRemoteBatteries;
            HallucinationTrigger.OnHallucinationCompleted += HandleHallucinationCompleted;
            FallingPictureFrame.OnPictureFrameFell += HandleDay3PictureFrameFell;
            SafeBoxPuzzle.OnSafeNoPowerExamined += HandleDay3SafeNoPowerExamined;
            SafeBoxPuzzle.OnSafeUnlockedGlobal += HandleDay3SafeUnlocked;
            SubscribeDoorEvents();
        }

        private void OnDisable()
        {
            InspectableObject.OnTaskCompleted -= HandleTaskCompleted;
            InspectableObject.OnLockedRequirementAttempted -= HandleLockedRequirementAttempted;
            TVController.OnMissingRemoteBatteriesAttempted -= HandleDay1MissingRemoteBatteries;
            HallucinationTrigger.OnHallucinationCompleted -= HandleHallucinationCompleted;
            FallingPictureFrame.OnPictureFrameFell -= HandleDay3PictureFrameFell;
            SafeBoxPuzzle.OnSafeNoPowerExamined -= HandleDay3SafeNoPowerExamined;
            SafeBoxPuzzle.OnSafeUnlockedGlobal -= HandleDay3SafeUnlocked;
            UnsubscribeDoorEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (transitionCanvasObject != null) Destroy(transitionCanvasObject);
            lampVisualCache.Clear();
        }

        private static string GetLampIdentifier(Light l)
        {
            if (l == null) return string.Empty;
            return l.transform.parent != null
                ? $"{l.transform.parent.name}_{l.name}".ToLowerInvariant()
                : l.name.ToLowerInvariant();
        }

        private void CacheSpecialCorridorLights()
        {
            eastCorridorLight = null;
            centerCorridorLight = null;
            westCorridorLight = null;
            daughtersRoomLight = null;
            if (houseInteriorLights == null) return;

            for (int i = 0; i < houseInteriorLights.Length; i++)
            {
                Light l = houseInteriorLights[i];
                if (l == null) continue;
                string n = GetLampIdentifier(l);
                if (n.Contains("corredor_leste"))
                {
                    eastCorridorLight = l;
                }
                else if (n.Contains("corredor_centro"))
                {
                    centerCorridorLight = l;
                }
                else if (n.Contains("corredor_oeste"))
                {
                    westCorridorLight = l;
                }
                else if (n.Contains("quartofilhas"))
                {
                    daughtersRoomLight = l;
                }
            }
        }

        public void SetCorridorAutoSputterSuppressed(bool suppressed)
        {
            if (suppressed)
                corridorAutoSputterSuppressDepth++;
            else
                corridorAutoSputterSuppressDepth = Mathf.Max(0, corridorAutoSputterSuppressDepth - 1);
        }

        private void DisableLegacyHeadlightAndMainLight()
        {
            if (houseMainLight != null)
            {
                houseMainLight.intensity = 0f;
                houseMainLight.enabled = false;
            }

            if (player != null && player.CameraTransform != null)
            {
                Transform legacyHeadlight = player.CameraTransform.Find("Luz_Adaptacao_Escuro");
                if (legacyHeadlight != null)
                {
                    if (legacyHeadlight.TryGetComponent<Light>(out Light l))
                    {
                        l.intensity = 0f;
                        l.enabled = false;
                    }
                    Destroy(legacyHeadlight.gameObject);
                }
            }
        }

        private void SubscribeDoorEvents()
        {
            if (!corridorDoorSubscribed && corridorLockedDoor != null)
            {
                corridorLockedDoor.OnLockedAttemptEvent += HandleCorridorDoorLockedAttempt;
                corridorLockedDoor.OnUnlockedAndOpenedEvent += HandleCorridorDoorUnlockedDay3;
                corridorDoorSubscribed = true;
            }

            if (!officeDoorSubscribed && officeDoor != null)
            {
                officeDoor.OnLockedAttemptEvent += HandleOfficeDoorLockedAttempt;
                officeDoor.OnUnlockedAndOpenedEvent += HandleOfficeDoorUnlocked;
                officeDoorSubscribed = true;
            }
        }

        private void UnsubscribeDoorEvents()
        {
            if (corridorDoorSubscribed && corridorLockedDoor != null)
            {
                corridorLockedDoor.OnLockedAttemptEvent -= HandleCorridorDoorLockedAttempt;
                corridorLockedDoor.OnUnlockedAndOpenedEvent -= HandleCorridorDoorUnlockedDay3;
            }
            corridorDoorSubscribed = false;

            if (officeDoorSubscribed && officeDoor != null)
            {
                officeDoor.OnLockedAttemptEvent -= HandleOfficeDoorLockedAttempt;
                officeDoor.OnUnlockedAndOpenedEvent -= HandleOfficeDoorUnlocked;
            }
            officeDoorSubscribed = false;
        }

        private void ValidateSpawnReferences()
        {
            if (spawnSofaSentado != null && spawnDia2Sofa != null)
            {
                string sitName = spawnSofaSentado.name.ToLowerInvariant();
                string day2Name = spawnDia2Sofa.name.ToLowerInvariant();
                if (sitName.Contains("dia2") && day2Name.Contains("sentado"))
                {
                    (spawnSofaSentado, spawnDia2Sofa) = (spawnDia2Sofa, spawnSofaSentado);
                }
            }
        }

        private void Start()
        {
            if (showOpeningTitleOnStart)
            {
                bool playStudioLogo = showStudioLogoOnStart && startingDay == GameDay.Day1;
                activeTransitionCoroutine = StartCoroutine(TransitionRoutine(startingDay, instantCutToBlack: true, playStudioLogoIntro: playStudioLogo));
            }
            else
            {
                ApplyDaySetupImmediate(startingDay, enablePlayerControlsNow: true);
                if (objectiveHUD != null) objectiveHUD.SetHUDVisible(startingDay != GameDay.Day4_Prelude, immediate: true);
            }
        }

        private void Update()
        {
            if (demoEnded && Keyboard.current != null)
            {
                if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    InventoryState.ClearAll();
                    DoorInteractable.PlayerHasSafeKey = false;
                    ObjectiveHUD.IsBlockingModalOpen = false;
                    SafeBoxPuzzle.CloseAnyOpenKeypad();
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                    return;
                }

                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    return;
                }
            }

            UpdateAmbientHorrorAtmosphere();
        }

        private void UpdateAmbientHorrorAtmosphere()
        {
            if (isTransitioning || currentDay == GameDay.Day4_Prelude || player == null)
            {
                if (sanityEffects != null) sanityEffects.SetProximityDread(0f);
                if (audioManager != null) audioManager.SetDreadProximityWeight(0f);
                return;
            }

            if (corridorAutoSputterSuppressDepth == 0
                && eastCorridorLight != null
                && eastCorridorLight.enabled
                && eastCorridorBaseIntensity > 0.02f)
            {
                float noiseFreq = (currentDay == GameDay.Day3 && (day3SafeUnlocked || completedDay3Clues.Count >= 2)) ? 7.2f : 4.8f;
                float dipThreshold = (currentDay == GameDay.Day3 && day3SafeUnlocked) ? 0.34f : 0.24f;
                float n = Mathf.PerlinNoise(Time.time * noiseFreq, 2.3f);
                float sputter = (n < dipThreshold) ? 0.26f : Mathf.Lerp(0.78f, 1.06f, n);
                eastCorridorLight.intensity = eastCorridorBaseIntensity * sputter;
                SyncFixtureEmissive(eastCorridorLight, sputter);
            }

            if (day3DoorTrapWaitingForEntry)
            {
                // False calm while the unlocked door stands open: subtle visual tension, near-total acoustic silence
                if (sanityEffects != null) sanityEffects.SetProximityDread(0.34f);
                if (audioManager != null) audioManager.SetDreadProximityWeight(0.06f);
                return;
            }

            Vector3 p = player.transform.position;
            float dread = 0f;
            bool inCorridorMainBranch = (p.z >= 0.45f && p.z <= 2.15f && p.x >= -0.20f && p.x <= 2.60f);
            bool inCorridorNorthWing = (p.x >= 0.90f && p.x <= 2.60f && p.z >= 0.45f && p.z <= 6.10f);
            if (inCorridorMainBranch || inCorridorNorthWing)
            {
                Vector3 doorPos = corridorLockedDoor != null
                    ? corridorLockedDoor.GetClosedDoorCenterWorld()
                    : new Vector3(2.60f, 0f, 4.60f);
                float distToDoor = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(doorPos.x, doorPos.z));
                float distFactor = Mathf.InverseLerp(5.40f, 1.05f, distToDoor);
                // Day 1 has zero corridor dread warping; Day 2/3 drive subtle audio tension only (no perspective warp)
                float dayScale = currentDay == GameDay.Day1 ? 0.0f : (currentDay == GameDay.Day2 ? 0.72f : 0.90f);
                dread = distFactor * dayScale;
            }
            else if (currentDay == GameDay.Day3 && completedDay3Clues.Count > 0)
            {
                dread = Mathf.Clamp01(completedDay3Clues.Count * 0.08f + (day3SafeUnlocked ? 0.16f : 0f));
            }

            if (sanityEffects != null) sanityEffects.SetProximityDread(dread);
            // When standing right in front of the locked door with the safe key on Day 3, drop the drone into heavy silence before unlocking
            float audioDread = (currentDay == GameDay.Day3 && day3SafeUnlocked && dread > 0.72f)
                ? Mathf.Lerp(dread, 0.12f, Mathf.InverseLerp(0.72f, 0.96f, dread))
                : dread;
            if (audioManager != null) audioManager.SetDreadProximityWeight(audioDread);
        }

        private static string NormalizeKey(string raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim().ToLowerInvariant();
        }

        private void HandleTaskCompleted(string taskId)
        {
            string normalized = NormalizeKey(taskId);
            if (string.IsNullOrEmpty(normalized))
                return;

            if (currentDay == GameDay.Day1)
            {
                completedDay1Tasks.Add(normalized);
                if (objectiveHUD != null)
                {
                    objectiveHUD.MarkCompleted(normalized);
                }
                if (normalized == "olhar_papeis")
                {
                    if (horrorDirector != null) horrorDirector.NotifyDay1OfficePapersRead();
                }
            }
            else if (currentDay == GameDay.Day2)
            {
                if (normalized == "frascos_calmantes_dia2")
                {
                    if (objectiveHUD != null) objectiveHUD.MarkCompleted("frascos_calmantes_dia2");
                    RegisterDay2LinearProgress("frascos_calmantes_dia2");
                }
                else if (normalized == "espelho_dia2_opc" || normalized == "espelho_banheiro_dia2")
                {
                    if (mirrorHaunt == null) mirrorHaunt = FindAnyObjectByType<MirrorHauntController>(FindObjectsInactive.Include);
                    if (mirrorHaunt != null) mirrorHaunt.TriggerImmediateMirrorFlash();
                }
                else if (normalized == "pegar_fita_banheiro_dia2")
                {
                    if (objectiveHUD != null)
                    {
                        objectiveHUD.MarkCompleted("pegar_fita_banheiro_dia2");
                        objectiveHUD.AddOrUpdateObjective(
                            "secretaria_fita_dia2",
                            "Ouvir a fita na secretária eletrônica",
                            completed: completedDay2LinearEvents.Contains("secretaria_fita_dia2"),
                            visible: true
                        );
                    }
                    // Spawn the real 3D PS1 figure standing motionless in the bathroom doorway behind Cian
                    // (reflected directly in the planar bathroom mirror until Cian turns around!)
                    if (mirrorHaunt == null) mirrorHaunt = FindAnyObjectByType<MirrorHauntController>(FindObjectsInactive.Include);
                    if (mirrorHaunt != null) mirrorHaunt.TriggerDay2DoorwayApparition();
                }
                else if (normalized == "secretaria_fita_dia2")
                {
                    if (objectiveHUD != null)
                    {
                        objectiveHUD.MarkCompleted("pegar_fita_banheiro_dia2");
                        objectiveHUD.MarkCompleted("secretaria_fita_dia2");
                        objectiveHUD.AddOrUpdateObjective(
                            "sussurros_porta_trancada",
                            "Verificar o barulho no fim do corredor",
                            completed: false,
                            visible: true
                        );
                        objectiveHUD.PromoteObjectiveToFront("sussurros_porta_trancada");
                    }

                    // Strict sequential Day 2 corridor progression: arm ONLY Trigger_1 (Vulto) first.
                    // Trigger_2 (Quadros) arms ONLY after Trigger_1 completes, and Trigger_3 ONLY after Trigger_2.
                    // (Do NOT arm redundant corridor watchers in Day 2.)
                    if (day2ShadowTrigger != null && !day2ShadowTrigger.HasTriggered)
                    {
                        day2ShadowTrigger.SetArmed(true);
                    }
                    if (horrorDirector != null) horrorDirector.NotifyDay2TapeHeard();
                    if (day2BeckonCoroutine != null) StopCoroutine(day2BeckonCoroutine);
                    day2BeckonCoroutine = StartCoroutine(PlayEastCorridorBeckonCueRoutine());

                    RegisterDay2LinearProgress("secretaria_fita_dia2");
                }
            }
            else if (currentDay == GameDay.Day3)
            {
                if (normalized == "espelho_dia3_opc")
                {
                    if (mirrorHaunt == null) mirrorHaunt = FindAnyObjectByType<MirrorHauntController>(FindObjectsInactive.Include);
                    if (mirrorHaunt != null) mirrorHaunt.TriggerImmediateMirrorFlash();
                }
                else if (normalized == "pegar_lanterna_dia3")
                {
                    SetKitchenDrawerFlashlightVisualVisible(false);
                    EnsureFlashlightController();
                    if (flashlightController != null)
                    {
                        flashlightController.SetShadowQuality(softShadowsEnabledByQuality);
                        flashlightController.SetEquipped(true, turnOnImmediately: true, playSound: true);
                    }
                    if (horrorDirector != null) horrorDirector.NotifyDay3FlashlightCollected();
                    if (objectiveHUD != null)
                    {
                        objectiveHUD.MarkCompleted("pegar_lanterna_dia3");
                        objectiveHUD.SetObjectiveVisible("engolir_capsula_dia3", true);
                        objectiveHUD.PromoteObjectiveToFront("engolir_capsula_dia3");
                    }
                }
                else if (normalized == "engolir_capsula_dia3")
                {
                    if (!day3MandatoryPillTaken)
                    {
                        day3MandatoryPillTaken = true;
                        bool alreadyCrashedFromNewspaper = completedDay3Clues.Contains("arquivo_metalico_opc");
                        if (!alreadyCrashedFromNewspaper)
                        {
                            day3BaseStimulantActive = true;
                            if (player != null) player.SetActivatingDrugEffect(true);
                        }
                        if (objectiveHUD != null)
                        {
                            objectiveHUD.MarkCompleted("engolir_capsula_dia3");
                            if (!InventoryState.HasItem("energia_corredor_ativa"))
                            {
                                if (InventoryState.HasItem("fusivel_reserva"))
                                {
                                    objectiveHUD.SetObjectiveLabel("restaurar_energia_dia3", "Trocar o fusível no quadro de força");
                                }
                                objectiveHUD.SetObjectiveVisible("restaurar_energia_dia3", true);
                                objectiveHUD.PromoteObjectiveToFront("restaurar_energia_dia3");
                            }
                        }
                    }
                    TriggerStimulantDrySwallowBoost();
                }
                else if (normalized == "pegar_fusivel_dia3")
                {
                    if (objectiveHUD != null)
                    {
                        objectiveHUD.MarkCompleted("pegar_fusivel_dia3");
                        if (!InventoryState.HasItem("energia_corredor_ativa"))
                        {
                            objectiveHUD.SetObjectiveLabel("restaurar_energia_dia3", "Trocar o fusível no quadro de força");
                            if (day3MandatoryPillTaken)
                            {
                                objectiveHUD.SetObjectiveVisible("restaurar_energia_dia3", true);
                                objectiveHUD.PromoteObjectiveToFront("restaurar_energia_dia3");
                            }
                        }
                    }
                    if (horrorDirector != null) horrorDirector.TriggerFuseGrabKnockCue();
                }
                else if (normalized == "restaurar_energia_dia3")
                {
                    InventoryState.AddItem("energia_corredor_ativa");
                    if (horrorDirector != null) horrorDirector.NotifyDay3PowerRestored();
                    if (day3CircuitRestoreCoroutine != null) StopCoroutine(day3CircuitRestoreCoroutine);
                    day3CircuitRestoreCoroutine = StartCoroutine(RestoreDay3CorridorCircuitLightsRoutine());

                    if (objectiveHUD != null)
                    {
                        objectiveHUD.MarkCompleted("pegar_fusivel_dia3");
                        objectiveHUD.MarkCompleted("restaurar_energia_dia3");
                        objectiveHUD.SetObjectiveLabel("retrato_margaret_dia3", "Examinar o retrato de Margaret no corredor");
                        objectiveHUD.SetObjectiveVisible("retrato_margaret_dia3", true);
                        objectiveHUD.PromoteObjectiveToFront("retrato_margaret_dia3");
                    }
                }
                else if (normalized == "agenda_margaret_dia3_opc"
                      || normalized == "bilhete_margaret_dia3_opc"
                      || normalized == "calendario_folha_anterior_opc"
                      || normalized == "calendario_d3_opc"
                      || normalized == "chave_arquivo_pessoal_dia3"
                      || normalized == "arquivo_metalico_opc")
                {
                    string clueCategory = (normalized == "calendario_d3_opc") ? "calendario_folha_anterior_opc" : normalized;
                    bool isNewClue = completedDay3Clues.Add(clueCategory);
                    if (isNewClue)
                    {
                        if (normalized == "arquivo_metalico_opc")
                        {
                            pendingDay3NewspaperCrash = true;
                        }
                        if (day3ClueReactionCoroutine != null)
                        {
                            StopCoroutine(day3ClueReactionCoroutine);
                            day3ClueReactionCoroutine = null;
                            if (pendingDay3NewspaperCrash && normalized != "arquivo_metalico_opc")
                            {
                                if (horrorDirector != null)
                                {
                                    horrorDirector.NotifyDay3NewspaperRead();
                                }
                                BeginDay3NewspaperChemicalCrash();
                            }
                            if (!day3SafeUnlocked && !day3DoorTrapWaitingForEntry)
                            {
                                ConfigureSecondaryAndFillLights(GameDay.Day3, GetUserAmbientMultiplier(), GetUserLampMultiplier(), InventoryState.HasItem("energia_corredor_ativa"));
                            }
                        }
                        day3ClueReactionCoroutine = StartCoroutine(RunDay3ClueReactionRoutine(normalized));
                    }
                }
            }
        }

        private IEnumerator RunDay3ClueReactionRoutine(string clueId)
        {
            // If a document viewer modal was opened by this interaction, wait until the player finishes reading and closes it
            yield return null;
            while (InteractionSystem.Instance != null && InteractionSystem.Instance.IsDocumentOpen)
            {
                if (currentDay != GameDay.Day3 || isTransitioning)
                {
                    day3ClueReactionCoroutine = null;
                    yield break;
                }
                yield return null;
            }

            yield return new WaitForSeconds(0.38f);
            if (currentDay != GameDay.Day3 || isTransitioning || day3DoorTrapWaitingForEntry)
            {
                day3ClueReactionCoroutine = null;
                yield break;
            }

            int clueCount = completedDay3Clues.Count;
            float esc01 = day3SafeUnlocked ? 1.0f : Mathf.Clamp01(clueCount / 4f);
            if (sanityEffects != null) sanityEffects.SetDay3TensionEscalation(esc01);
            if (horrorDirector != null) horrorDirector.NotifyDay3ClueDiscovered(clueCount);

            Vector3 lockedDoorSoundPos = corridorLockedDoor != null
                ? corridorLockedDoor.GetClosedDoorCenterWorld() + Vector3.up * 1.32f
                : new Vector3(2.60f, 1.32f, 4.60f);

            if (clueId == "agenda_margaret_dia3_opc")
            {
                // Torn planner on Margaret's nightstand: restrained quiet wood creak outside bedroom doorway (zero fake buzz/hiss)
                if (audioManager != null)
                {
                    audioManager.PlayFoley3DPanned(FoleySoundId.DistantFloorCreak, new Vector3(-1.75f, 0.08f, 1.60f), 0.62f, 1.0f, 14f, 0.90f);
                }
            }
            else if (clueId == "bilhete_margaret_dia3_opc")
            {
                // Margaret's folded note ("encosta a porta do corredor devagar"): office door quietly creaks slightly inward
                if (officeDoor != null && officeDoor.IsOpen)
                {
                    officeDoor.OpenToPartialAngle(24f, 22f, playSound: true);
                }
            }
            else if (clueId == "calendario_folha_anterior_opc" || clueId == "calendario_d3_opc")
            {
                // Subtle knock from the locked room (no cheap screen flash)
                if (audioManager != null)
                {
                    audioManager.PlayFoley3DPanned(FoleySoundId.DoorKnockWood, lockedDoorSoundPos, 0.72f, 1.2f, 20f, 0.84f);
                }
            }
            else if (clueId == "chave_arquivo_pessoal_dia3")
            {
                // Finding the small key in Margaret's coat pocket: clean, quiet realism — zero weird white-noise hiss or forced shake
            }
            else if (clueId == "arquivo_metalico_opc")
            {
                // Reading the 1994 Newspaper Clipping: Cian's stimulant-fueled denial crashes gradually
                if (horrorDirector != null)
                {
                    horrorDirector.NotifyDay3NewspaperRead();
                }
                BeginDay3NewspaperChemicalCrash();
                if (audioManager != null)
                {
                    audioManager.PulseSilenceDread(0.92f, 0.70f);
                }
                if (sanityEffects != null)
                {
                    sanityEffects.TriggerHallucinationSpike(0.55f, 0.80f, allowSubliminalFlash: false);
                }

                yield return new WaitForSeconds(0.68f);
                if (currentDay == GameDay.Day3 && !isTransitioning && !day3DoorTrapWaitingForEntry && audioManager != null)
                {
                    audioManager.PlayFoley3DPanned(FoleySoundId.DoorKnockWood, lockedDoorSoundPos, 0.84f, 1.2f, 22f, 0.80f);
                    yield return new WaitForSeconds(0.32f);
                    if (currentDay == GameDay.Day3 && !isTransitioning && !day3DoorTrapWaitingForEntry)
                    {
                        audioManager.PlayFoley3DPanned(FoleySoundId.DoorKnockWood, lockedDoorSoundPos, 0.90f, 1.2f, 22f, 0.76f);
                    }
                }
            }

            day3ClueReactionCoroutine = null;
        }

        public void RecordDay2EndPose(Vector3 worldPos, float yawDeg)
        {
            recordedDay2EndPos = new Vector3(worldPos.x, 0.02f, worldPos.z);
            recordedDay2EndYaw = yawDeg;
            hasRecordedDay2EndPose = true;
        }

        private void ResolveDay3WakeupPose(out Vector3 pos, out Quaternion rot)
        {
            if (hasRecordedDay2EndPose)
            {
                pos = recordedDay2EndPos;
                // Keep clear of Day 3's overturned dining chair (0.62, -1.42) if player happened to stand right there
                Vector2 chairPos = new Vector2(0.62f, -1.42f);
                Vector2 p2 = new Vector2(pos.x, pos.z);
                float distToChair = Vector2.Distance(p2, chairPos);
                if (distToChair < 0.78f)
                {
                    Vector2 pushDir = distToChair > 0.01f ? (p2 - chairPos).normalized : Vector2.left;
                    Vector2 nudged = chairPos + pushDir * 0.82f;
                    pos = new Vector3(Mathf.Clamp(nudged.x, -2.40f, 0.95f), 0.02f, Mathf.Clamp(nudged.y, -5.05f, 0.10f));
                }
                rot = Quaternion.Euler(0f, recordedDay2EndYaw, 0f);
                return;
            }

            if (spawnDia3 != null)
            {
                pos = spawnDia3.position;
                rot = spawnDia3.rotation;
                return;
            }

            pos = new Vector3(-0.84f, 0.02f, -4.55f);
            rot = Quaternion.Euler(0f, 180f, 0f);
        }

        private void EnsureFlashlightController()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (player != null && flashlightController == null)
            {
                flashlightController = player.GetComponent<FlashlightController>();
                if (flashlightController == null) flashlightController = player.gameObject.AddComponent<FlashlightController>();
            }
        }

        private void SetKitchenDrawerFlashlightVisualVisible(bool visible)
        {
            if (cachedKitchenDrawerFlashlightVisual == null && casaBase != null)
            {
                Transform[] nodes = casaBase.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (nodes[i] != null && nodes[i].name == "Lanterna_Sem_Tampa")
                    {
                        cachedKitchenDrawerFlashlightVisual = nodes[i].gameObject;
                        break;
                    }
                }
            }
            if (cachedKitchenDrawerFlashlightVisual != null)
            {
                cachedKitchenDrawerFlashlightVisual.SetActive(visible);
            }
        }

        public bool IsDay3PrePillGateBlocked(out string blockedReason)
        {
            blockedReason = string.Empty;
            if (currentDay != GameDay.Day3)
                return false;

            if (!InventoryState.HasItem("lanterna_dia3"))
            {
                blockedReason = "Tá escuro demais pra mexer nisso agora. A lanterna tá na primeira gaveta da cozinha.";
                if (objectiveHUD != null) objectiveHUD.PromoteObjectiveToFront("pegar_lanterna_dia3");
                return true;
            }

            if (!day3MandatoryPillTaken)
            {
                blockedReason = "Minha vista tá escurecendo de novo... Se eu não tomar aquelas pílulas na mesa da sala, vou apagar aqui.";
                if (objectiveHUD != null)
                {
                    objectiveHUD.SetObjectiveVisible("engolir_capsula_dia3", true);
                    objectiveHUD.PromoteObjectiveToFront("engolir_capsula_dia3");
                }
                return true;
            }

            return false;
        }

        public bool CanConsumeStimulantCapsule(out string blockedReason)
        {
            blockedReason = string.Empty;
            if (currentDay != GameDay.Day3 || isTransitioning || day3DoorTrapWaitingForEntry)
            {
                blockedReason = "Agora não.";
                return false;
            }

            if (!InventoryState.HasItem("lanterna_dia3"))
            {
                blockedReason = "Tá escuro demais pra mexer nesses remédios agora. Preciso pegar a lanterna na gaveta da cozinha primeiro.";
                if (objectiveHUD != null) objectiveHUD.PromoteObjectiveToFront("pegar_lanterna_dia3");
                return false;
            }

            if (isStimulantBoostOrReboundActive)
            {
                return false;
            }

            return true;
        }

        public void TriggerStimulantDrySwallowBoost()
        {
            if (currentDay != GameDay.Day3 || isTransitioning || day3DoorTrapWaitingForEntry)
                return;

            float startSpeedBlend = player != null
                ? player.CurrentStimulantSpeedBlend
                : (day3BaseStimulantActive ? 1f : 0f);
            StopAndResetStimulantEffectsImmediate(clearPlayerDrugState: false);
            stimulantEffectCoroutine = StartCoroutine(RunStimulantDrySwallowRoutine(startSpeedBlend));
        }

        private void BeginDay3NewspaperChemicalCrash()
        {
            pendingDay3NewspaperCrash = false;
            day3BaseStimulantActive = false;
            if (currentDay != GameDay.Day3 || isTransitioning || day3DoorTrapWaitingForEntry)
            {
                StopAndResetStimulantEffectsImmediate(clearPlayerDrugState: true);
                return;
            }

            float startSpeedBlend = player != null ? player.CurrentStimulantSpeedBlend : 1f;
            float startRebound = sanityEffects != null ? sanityEffects.CurrentChemicalRebound01 : 0f;
            if (stimulantEffectCoroutine != null)
            {
                StopCoroutine(stimulantEffectCoroutine);
                stimulantEffectCoroutine = null;
            }
            if (probeRefreshCoroutine != null)
            {
                StopCoroutine(probeRefreshCoroutine);
                probeRefreshCoroutine = null;
            }
            if (audioManager != null) audioManager.SetStimulantRoomToneClarity(0f);
            stimulantEffectCoroutine = StartCoroutine(RunNewspaperChemicalCrashRoutine(startSpeedBlend, startRebound));
        }

        private void StopAndResetStimulantEffectsImmediate(bool clearPlayerDrugState)
        {
            if (stimulantEffectCoroutine != null)
            {
                StopCoroutine(stimulantEffectCoroutine);
                stimulantEffectCoroutine = null;
            }
            if (probeRefreshCoroutine != null)
            {
                StopCoroutine(probeRefreshCoroutine);
                probeRefreshCoroutine = null;
            }

            isStimulantBoostOrReboundActive = false;
            if (audioManager != null) audioManager.SetStimulantRoomToneClarity(0f);
            if (sanityEffects != null) sanityEffects.ClearStimulantModulationImmediate();

            if (player != null)
            {
                if (clearPlayerDrugState)
                {
                    player.SetActivatingDrugEffect(false);
                }
                player.ResetChemicalEffectsImmediate();
            }
        }

        private IEnumerator RunStimulantDrySwallowRoutine(float startSpeedBlend)
        {
            const float stabilizeDuration = 14.0f;
            const float reboundDuration = 8.0f;
            const float totalDuration = stabilizeDuration + reboundDuration;
            const float beatInterval = 0.86f;

            isStimulantBoostOrReboundActive = true;
            float elapsed = 0f;
            float nextBeatTime = stabilizeDuration + 0.12f;
            float lastBeatTime = -10f;

            while (elapsed < totalDuration)
            {
                if (currentDay != GameDay.Day3 || isTransitioning || day3DoorTrapWaitingForEntry)
                {
                    StopAndResetStimulantEffectsImmediate(clearPlayerDrugState: !day3BaseStimulantActive);
                    yield break;
                }

                elapsed += Time.deltaTime;

                if (elapsed < stabilizeDuration)
                {
                    // 0..14s: temporary stabilization of tremor/buzz/focus while sustaining activatingDrugMoveSpeed
                    float rampIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 1.10f));
                    float tailFade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - (stabilizeDuration - 1.15f)) / 1.15f));
                    float stab01 = rampIn * tailFade;
                    float currentSpeedBlend = Mathf.Lerp(startSpeedBlend, 1f, rampIn);

                    if (player != null)
                    {
                        player.SetChemicalEffectModifiers(
                            speedBlend01: currentSpeedBlend,
                            speedMultiplier: 1f,
                            lookDrag01: 0f,
                            eyeDropMeters: 0f,
                            pitchOffsetDeg: 0f,
                            rollOffsetDeg: 0f,
                            fovOffsetDeg: 0.9f * stab01);
                    }

                    if (sanityEffects != null) sanityEffects.SetStimulantModulation(stab01, 0f, 0f);
                    if (audioManager != null) audioManager.SetStimulantRoomToneClarity(stab01);
                }
                else
                {
                    // 14..22s: 8s chemical rebound — subtle FOV narrowing, vignette/grain pulse + heavy heartbeat, momentary camera weight
                    float rebElapsed = elapsed - stabilizeDuration;
                    float u = Mathf.Clamp01(rebElapsed / reboundDuration);
                    float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.22f));
                    float recover = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.66f) / 0.34f));
                    float rebound01 = rise * recover;

                    if (elapsed >= nextBeatTime && rebound01 > 0.16f && !ObjectiveHUD.IsBlockingModalOpen)
                    {
                        lastBeatTime = elapsed;
                        nextBeatTime = elapsed + beatInterval;
                        float beatVol = Mathf.Lerp(0.55f, 0.94f, rebound01);
                        float beatPitch = Mathf.Lerp(1.04f, 0.90f, u);
                        if (audioManager != null) audioManager.PlayFoley2D(FoleySoundId.HeavyHeartbeatThud, beatVol, beatPitch);
                    }

                    float sinceBeat = elapsed - lastBeatTime;
                    float beatPulse01 = (sinceBeat >= 0f && sinceBeat < 0.72f)
                        ? Mathf.Clamp01(Mathf.Exp(-sinceBeat * 5.8f) + 0.56f * Mathf.Exp(-Mathf.Abs(sinceBeat - 0.175f) * 20f))
                        : 0f;

                    float speedBlend = day3BaseStimulantActive ? 1f : Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, u));
                    float speedMult = 1f - (0.12f * rebound01);
                    float fovNarrow = -rebound01 * (2.5f + 1.8f * beatPulse01);
                    float camWeightPitch = rebound01 * (1.45f + 0.55f * beatPulse01);
                    float camWeightRoll = Mathf.Sin(elapsed * 1.85f) * 1.10f * rebound01;

                    if (player != null)
                    {
                        player.SetChemicalEffectModifiers(
                            speedBlend01: speedBlend,
                            speedMultiplier: speedMult,
                            lookDrag01: rebound01 * 0.85f,
                            eyeDropMeters: rebound01 * 0.045f,
                            pitchOffsetDeg: camWeightPitch,
                            rollOffsetDeg: camWeightRoll,
                            fovOffsetDeg: fovNarrow);
                    }

                    if (sanityEffects != null) sanityEffects.SetStimulantModulation(0f, rebound01, beatPulse01);
                    if (audioManager != null) audioManager.SetStimulantRoomToneClarity(0f);
                }

                yield return null;
            }

            StopAndResetStimulantEffectsImmediate(clearPlayerDrugState: !day3BaseStimulantActive);
        }

        private IEnumerator RunNewspaperChemicalCrashRoutine(float startSpeedBlend, float startRebound)
        {
            const float crashDuration = 4.6f;
            const float beatInterval = 0.92f;

            isStimulantBoostOrReboundActive = true;
            float clampedStartSpeed = Mathf.Clamp01(startSpeedBlend);
            float clampedStartRebound = Mathf.Clamp01(startRebound);
            float elapsed = 0f;
            float nextBeatTime = 0.25f;
            float lastBeatTime = -10f;

            while (elapsed < crashDuration)
            {
                if (currentDay != GameDay.Day3 || isTransitioning || day3DoorTrapWaitingForEntry)
                {
                    StopAndResetStimulantEffectsImmediate(clearPlayerDrugState: true);
                    yield break;
                }

                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / crashDuration);

                // Smooth gradual drop from current stimulant speed down to normalMoveSpeed (0.0) without snapping
                float speedBlend = Mathf.Lerp(clampedStartSpeed, 0f, Mathf.SmoothStep(0f, 1f, u));
                float waveEnv = Mathf.Sin(u * Mathf.PI);
                float carryEnv = clampedStartRebound * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.35f)));
                float crashEnv = Mathf.Clamp01(Mathf.Max(waveEnv, carryEnv));
                float speedMult = 1f - (0.09f * crashEnv);

                if (elapsed >= nextBeatTime && crashEnv > 0.24f && !ObjectiveHUD.IsBlockingModalOpen)
                {
                    lastBeatTime = elapsed;
                    nextBeatTime = elapsed + beatInterval;
                    if (audioManager != null) audioManager.PlayFoley2D(FoleySoundId.HeavyHeartbeatThud, 0.62f + 0.28f * crashEnv, Mathf.Lerp(0.96f, 0.84f, u));
                }

                float sinceBeat = elapsed - lastBeatTime;
                float beatPulse01 = (sinceBeat >= 0f && sinceBeat < 0.72f)
                    ? Mathf.Clamp01(Mathf.Exp(-sinceBeat * 5.8f) + 0.52f * Mathf.Exp(-Mathf.Abs(sinceBeat - 0.175f) * 20f))
                    : 0f;

                if (player != null)
                {
                    player.SetChemicalEffectModifiers(
                        speedBlend01: speedBlend,
                        speedMultiplier: speedMult,
                        lookDrag01: crashEnv * 0.78f,
                        eyeDropMeters: crashEnv * 0.040f,
                        pitchOffsetDeg: crashEnv * (1.25f + 0.45f * beatPulse01),
                        rollOffsetDeg: Mathf.Sin(elapsed * 1.65f) * 0.90f * crashEnv,
                        fovOffsetDeg: -crashEnv * (2.2f + 1.3f * beatPulse01));
                }

                if (sanityEffects != null) sanityEffects.SetStimulantModulation(0f, crashEnv * 0.88f, beatPulse01);
                if (audioManager != null) audioManager.SetStimulantRoomToneClarity(0f);

                yield return null;
            }

            StopAndResetStimulantEffectsImmediate(clearPlayerDrugState: true);
        }

        private Light FindSecondaryLightContaining(string tokenLower)
        {
            if (secondaryMotivatedLights == null) return null;
            for (int i = 0; i < secondaryMotivatedLights.Length; i++)
            {
                Light sec = secondaryMotivatedLights[i];
                if (sec != null && sec.name.ToLowerInvariant().Contains(tokenLower))
                {
                    return sec;
                }
            }
            return null;
        }

        private IEnumerator PlayEastCorridorBeckonCueRoutine()
        {
            yield return new WaitForSeconds(1.35f);
            if (currentDay != GameDay.Day2 || isTransitioning) yield break;

            if (audioManager != null)
            {
                Vector3 eastPos = eastCorridorLight != null
                    ? eastCorridorLight.transform.position
                    : new Vector3(1.78f, 2.55f, 4.03f);
                audioManager.PlayFoley3D(FoleySoundId.BulbFlickerBuzz, eastPos, 0.78f);
                audioManager.PlayFoley3D(FoleySoundId.DistantFloorCreak, new Vector3(eastPos.x, 0.1f, Mathf.Max(2.2f, eastPos.z - 0.8f)), 0.85f);
            }
        }

        private IEnumerator RestoreDay3CorridorCircuitLightsRoutine()
        {
            if (day3RestoredCircuitLight != null)
            {
                day3RestoredCircuitLight.enabled = true;
            }

            Vector3 buzzPos = westCorridorLight != null
                ? westCorridorLight.transform.position
                : (eastCorridorLight != null ? eastCorridorLight.transform.position : new Vector3(-0.42f, 2.55f, 1.28f));

            for (int step = 0; step < 3; step++)
            {
                if (currentDay != GameDay.Day3 || isTransitioning)
                {
                    day3CircuitRestoreCoroutine = null;
                    yield break;
                }

                bool on = (step % 2 == 0);
                SetDay3CircuitLightsState(on, on ? 0.55f : 0f);
                if (step == 1)
                {
                    ArmWatchersForDay(GameDay.Day3, "dia3_energia");
                }
                if (on && audioManager != null)
                {
                    audioManager.PlayFoley3D(FoleySoundId.BulbFlickerBuzz, buzzPos, 0.75f);
                }
                yield return new WaitForSeconds(0.11f);
            }

            if (currentDay == GameDay.Day3 && !isTransitioning)
            {
                SetDay3CircuitLightsState(true, 1f);
                RefreshInteriorReflectionProbes();
            }
            day3CircuitRestoreCoroutine = null;
        }

        private void SetDay3CircuitLightsState(bool enabledState, float intensityScale)
        {
            float lampMult = GetUserLampMultiplier();
            float ambMult = GetUserAmbientMultiplier();
            bool on = enabledState && intensityScale > 0.01f;
            float warmIntensity = 0.62f * lampMult;
            float coolIntensity = 0.76f * lampMult;

            if (houseInteriorLights != null)
            {
                for (int i = 0; i < houseInteriorLights.Length; i++)
                {
                    Light l = houseInteriorLights[i];
                    if (l == null) continue;
                    string n = GetLampIdentifier(l);
                    if (n.Contains("corredor_oeste"))
                    {
                        l.enabled = on;
                        l.color = new Color(0.96f, 0.78f, 0.54f);
                        l.intensity = on ? (day3SafeUnlocked ? 0.18f : 0.54f) * intensityScale * lampMult : 0f;
                        SyncLampFixtureVisual(l, on, l.color, (day3SafeUnlocked ? 0.22f : 0.52f) * intensityScale);
                    }
                    else if (n.Contains("corredor_centro"))
                    {
                        float siegeScale = day3SafeUnlocked ? 0.42f : 1.0f;
                        l.enabled = on;
                        l.color = new Color(0.96f, 0.78f, 0.54f);
                        l.intensity = on ? 0.60f * intensityScale * siegeScale * lampMult : 0f;
                        SyncLampFixtureVisual(l, on, l.color, 0.56f * intensityScale * siegeScale);
                    }
                    else if (n.Contains("corredor_leste"))
                    {
                        l.enabled = on;
                        l.color = new Color(0.92f, 0.72f, 0.48f);
                        eastCorridorBaseIntensity = on ? (0.50f * intensityScale * lampMult) : 0f;
                        l.intensity = eastCorridorBaseIntensity;
                        SyncLampFixtureVisual(l, on, l.color, 0.54f * intensityScale);
                    }
                    else if (n.Contains("escritorio"))
                    {
                        l.enabled = on;
                        l.color = new Color(0.90f, 0.92f, 0.91f);
                        l.intensity = on ? (day3SafeUnlocked ? 0.14f : 0.34f) * intensityScale * lampMult : 0f;
                        SyncLampFixtureVisual(l, on, l.color, (day3SafeUnlocked ? 0.20f : 0.42f) * intensityScale);
                    }
                    else if (n.Contains("cozinha"))
                    {
                        l.enabled = on;
                        l.color = new Color(0.88f, 0.91f, 0.89f);
                        l.intensity = on ? (day3SafeUnlocked ? coolIntensity * 0.28f : coolIntensity) * intensityScale : 0f;
                        SyncLampFixtureVisual(l, on, l.color, (day3SafeUnlocked ? 0.24f : 0.64f) * intensityScale);
                    }
                    else if (n.Contains("banheiro"))
                    {
                        l.enabled = on;
                        l.color = new Color(0.91f, 0.94f, 0.96f);
                        l.intensity = on ? (day3SafeUnlocked ? coolIntensity * 0.22f : coolIntensity * 0.44f) * intensityScale : 0f;
                        SyncLampFixtureVisual(l, on, l.color, (day3SafeUnlocked ? 0.22f : 0.48f) * intensityScale);
                    }
                    else if (n.Contains("quarto_cian"))
                    {
                        l.enabled = on;
                        l.color = new Color(0.95f, 0.77f, 0.52f);
                        l.intensity = on ? (day3SafeUnlocked ? warmIntensity * 0.22f : warmIntensity * 0.42f) * intensityScale : 0f;
                        SyncLampFixtureVisual(l, on, l.color, (day3SafeUnlocked ? 0.20f : Mathf.Clamp01(l.intensity * 0.72f)) * intensityScale);
                    }
                }
            }

            if (day3RestoredCircuitLight != null)
            {
                day3RestoredCircuitLight.enabled = on;
                day3RestoredCircuitLight.color = new Color(1.00f, 0.88f, 0.66f);
                day3RestoredCircuitLight.intensity = on ? (day3SafeUnlocked ? 0.42f : 1.36f) * intensityScale * lampMult : 0f;
                SyncBankerLampVisual(day3RestoredCircuitLight, on, Mathf.Clamp01(intensityScale * (day3SafeUnlocked ? 0.38f : 0.92f)));
            }

            if (secondaryMotivatedLights != null)
            {
                for (int i = 0; i < secondaryMotivatedLights.Length; i++)
                {
                    Light sec = secondaryMotivatedLights[i];
                    if (sec == null) continue;
                    string sn = sec.name.ToLowerInvariant();
                    if (sn.Contains("abajur_margaret"))
                    {
                        sec.enabled = on;
                        sec.color = new Color(1.00f, 0.83f, 0.58f);
                        sec.intensity = on ? (day3SafeUnlocked ? 0.26f : 0.84f) * intensityScale * lampMult : 0f;
                        SyncSecondaryFixtureVisual(sec, on, sec.color, on ? (day3SafeUnlocked ? 0.30f : 0.85f) * intensityScale : 0f);
                    }
                    else if (sn.Contains("arandela_banheiro"))
                    {
                        sec.enabled = on;
                        sec.color = new Color(0.92f, 0.94f, 0.96f);
                        sec.intensity = on ? (day3SafeUnlocked ? 0.24f : 0.78f) * intensityScale * lampMult : 0f;
                        SyncSecondaryFixtureVisual(sec, on, sec.color, on ? (day3SafeUnlocked ? 0.28f : 0.80f) * intensityScale : 0f);
                    }
                }
            }

            if (architecturalFillLights != null)
            {
                for (int i = 0; i < architecturalFillLights.Length; i++)
                {
                    Light fill = architecturalFillLights[i];
                    if (fill == null) continue;
                    if (fill.name.ToLowerInvariant().Contains("fill_spill_arcocozinha"))
                    {
                        fill.enabled = on;
                        fill.color = new Color(0.93f, 0.95f, 0.93f);
                        fill.intensity = on ? (day3SafeUnlocked ? 0.09f : 0.28f) * intensityScale * ambMult : 0f;
                    }
                }
            }
        }

        private void HandleLockedRequirementAttempted(string taskId)
        {
            string normalized = NormalizeKey(taskId);
            if (objectiveHUD == null || string.IsNullOrEmpty(normalized))
                return;

            if (currentDay == GameDay.Day2 && normalized == "secretaria_fita_dia2")
            {
                if (!InventoryState.HasItem("fita_secretaria"))
                {
                    objectiveHUD.SetObjectiveVisible("pegar_fita_banheiro_dia2", true);
                }
            }
            else if (currentDay == GameDay.Day3 && normalized == "restaurar_energia_dia3")
            {
                if (!InventoryState.HasItem("energia_corredor_ativa"))
                {
                    objectiveHUD.SetObjectiveLabel("restaurar_energia_dia3", "Trocar o fusível no quadro de força");
                    objectiveHUD.SetObjectiveVisible("restaurar_energia_dia3", true);
                }
                if (!InventoryState.HasItem("fusivel_reserva"))
                {
                    objectiveHUD.SetObjectiveVisible("pegar_fusivel_dia3", true);
                    objectiveHUD.PromoteObjectiveToFront("pegar_fusivel_dia3");
                }
            }
        }

        private void HandleOfficeDoorLockedAttempt()
        {
            if (currentDay != GameDay.Day1 || objectiveHUD == null)
                return;

            if (!InventoryState.HasItem("chave_escritorio"))
            {
                objectiveHUD.SetObjectiveVisible("pegar_chave_escritorio", true);
            }
        }

        private void HandleOfficeDoorUnlocked()
        {
            if (currentDay != GameDay.Day1 || objectiveHUD == null)
                return;

            objectiveHUD.MarkCompleted("pegar_chave_escritorio");
            objectiveHUD.MarkCompleted("destrancar_escritorio_dia1");
        }

        private void HandleDay1MissingRemoteBatteries()
        {
            if (currentDay != GameDay.Day1 || objectiveHUD == null)
                return;

            if (!InventoryState.HasItem("pilhas_controle"))
            {
                objectiveHUD.SetObjectiveVisible("pilhas_controle_task", true);
            }
        }

        private void HandleHallucinationCompleted(string hallucinationId)
        {
            string normalized = NormalizeKey(hallucinationId);
            if (currentDay != GameDay.Day2 || string.IsNullOrEmpty(normalized))
                return;

            if (normalized == "vulto_corredor")
            {
                // Avoid double-silhouette overlap if player rushed past corner watcher straight into Vulto
                DisarmUntriggeredWatchersForDay(GameDay.Day2);
                if (day2FramesTrigger != null && !day2FramesTrigger.HasTriggered) day2FramesTrigger.SetArmed(true);
            }
            else if (normalized == "quadros_familia_alterados" &&
                     day2WhispersTrigger != null &&
                     !day2WhispersTrigger.HasTriggered &&
                     !completedDay2LinearEvents.Contains("sussurros_porta_trancada") &&
                     !day2NewsTriggered)
            {
                day2WhispersTrigger.SetArmed(true);
            }

            if (objectiveHUD != null)
            {
                objectiveHUD.MarkCompleted(normalized, silentCompletion: true);
            }
            RegisterDay2LinearProgress(normalized);
        }

        private void HandleCorridorDoorLockedAttempt()
        {
            if (currentDay != GameDay.Day2 || isTransitioning)
                return;

            bool tapeHeard = completedDay2LinearEvents.Contains("secretaria_fita_dia2");
            if (!tapeHeard)
            {
                if (InteractionSystem.Instance != null)
                {
                    InteractionSystem.Instance.ShowSubtitle(
                        "Trancada... A madeira inchada estala com a umidade.",
                        4.6f
                    );
                }
                return;
            }

            bool firstPostTapeAttempt = !completedDay2LinearEvents.Contains("sussurros_porta_trancada");
            if (firstPostTapeAttempt)
            {
                if (day2WhispersTrigger != null && !day2WhispersTrigger.HasTriggered)
                {
                    day2WhispersTrigger.SetArmed(false);
                }
                if (day2DoorScareCoroutine != null) StopCoroutine(day2DoorScareCoroutine);
                day2DoorScareCoroutine = StartCoroutine(RunDay2LockedDoorScareRoutine());
                if (SanityEffects.Instance != null)
                {
                    SanityEffects.Instance.TriggerHallucinationSpike(0.60f, 1.05f, allowSubliminalFlash: false);
                }
            }

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ShowSubtitle(
                    "Não tem ninguém aí dentro... Preciso voltar pra sala.",
                    5.2f
                );
            }

            if (objectiveHUD != null) objectiveHUD.MarkCompleted("sussurros_porta_trancada", silentCompletion: true);
            RegisterDay2LinearProgress("sussurros_porta_trancada");
        }

        private IEnumerator RunDay2LockedDoorScareRoutine()
        {
            Vector3 doorSoundPos = corridorLockedDoor != null
                ? corridorLockedDoor.GetClosedDoorCenterWorld() + Vector3.up * 1.30f
                : new Vector3(2.60f, 1.30f, 4.60f);

            // Let initial knob clack settle, then knock sharply from inside behind solid wood
            yield return new WaitForSeconds(0.18f);
            if (currentDay != GameDay.Day2) yield break;

            if (corridorLockedDoor != null) corridorLockedDoor.TriggerLockedRattle(0.20f);
            if (audioManager != null)
            {
                audioManager.PlayFoley3DPanned(FoleySoundId.DoorKnockWood, doorSoundPos, 0.90f, 0.8f, 12f, 0.78f);
            }

            yield return new WaitForSeconds(0.21f);
            if (currentDay != GameDay.Day2) yield break;

            if (corridorLockedDoor != null) corridorLockedDoor.TriggerLockedRattle(0.16f);
            if (audioManager != null)
            {
                audioManager.PlayFoley3DPanned(FoleySoundId.DoorKnockWood, doorSoundPos, 0.96f, 0.8f, 12f, 0.72f);
            }
        }

        private void RegisterDay2LinearProgress(string linearKey)
        {
            completedDay2LinearEvents.Add(linearKey);

            bool tapeHeard = completedDay2LinearEvents.Contains("secretaria_fita_dia2");
            bool coreCluesDone =
                completedDay2LinearEvents.Contains("frascos_calmantes_dia2") &&
                tapeHeard;

            int requiredCount = Mathf.Max(3, day2EventsNeededForTV);
            bool corridorDoorCheckedAfterTape = tapeHeard && completedDay2LinearEvents.Contains("sussurros_porta_trancada");
            if (!day2NewsTriggered && (corridorDoorCheckedAfterTape || (coreCluesDone && completedDay2LinearEvents.Count >= requiredCount)))
            {
                day2NewsTriggered = true;
                float activationDelay = (linearKey == "frascos_calmantes_dia2") ? 1.25f : 3.20f;
                if (day2TVActivationCoroutine != null) StopCoroutine(day2TVActivationCoroutine);
                day2TVActivationCoroutine = StartCoroutine(DelayedDay2TVActivationRoutine(activationDelay));
            }
        }

        private IEnumerator DelayedDay2TVActivationRoutine(float delaySeconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0.4f, delaySeconds));
            day2TVActivationCoroutine = null;
            if (currentDay == GameDay.Day2 && !isTransitioning && tvController != null)
            {
                if (objectiveHUD != null)
                {
                    objectiveHUD.MarkCompleted("sussurros_porta_trancada", silentCompletion: true);
                    if (!completedDay2LinearEvents.Contains("frascos_calmantes_dia2"))
                    {
                        objectiveHUD.SetObjectiveVisible("frascos_calmantes_dia2", false);
                    }
                }
                tvController.ActivateDay2AutonomousNews();
                if (objectiveHUD != null) objectiveHUD.PromoteObjectiveToFront("assistir_plantao_dia2");
            }
        }

        private void HandleDay3PictureFrameFell()
        {
            if (currentDay != GameDay.Day3 || objectiveHUD == null) return;
            objectiveHUD.MarkCompleted("retrato_margaret_dia3", silentCompletion: true);
            objectiveHUD.SetObjectiveVisible("cofre_1009_dia3", true);
        }

        private void HandleDay3SafeNoPowerExamined()
        {
            if (currentDay != GameDay.Day3 || objectiveHUD == null) return;
            if (!InventoryState.HasItem("energia_corredor_ativa"))
            {
                if (InventoryState.HasItem("fusivel_reserva"))
                {
                    objectiveHUD.SetObjectiveLabel("restaurar_energia_dia3", "Trocar o fusível no quadro de força");
                }
                objectiveHUD.SetObjectiveVisible("restaurar_energia_dia3", true);
                objectiveHUD.PromoteObjectiveToFront("restaurar_energia_dia3");
            }
        }

        private void HandleDay3SafeUnlocked()
        {
            if (currentDay != GameDay.Day3 || objectiveHUD == null) return;
            objectiveHUD.MarkCompleted("pegar_fusivel_dia3");
            objectiveHUD.MarkCompleted("restaurar_energia_dia3");
            objectiveHUD.MarkCompleted("cofre_1009_dia3");
            objectiveHUD.SetObjectiveVisible("destrancar_porta_dia3", true);

            day3SafeUnlocked = true;
            SyncFrontDoorForDay(GameDay.Day3);
            if (sanityEffects != null) sanityEffects.SetDay3TensionEscalation(1.0f);
            if (horrorDirector != null) horrorDirector.NotifyDay3SafeUnlocked();

            // Pre-activate Quarto_Filhas in eerie low-key penumbra using its physical ceiling fixture so there is zero frame hitch when unlocking the door
            PrepareDaughtersRoomForDay3Trap(1.0f);

            if (day3SiegeCoroutine != null) StopCoroutine(day3SiegeCoroutine);
            day3SiegeCoroutine = StartCoroutine(RunDay3SafeUnlockedSiegeRoutine());
        }

        private IEnumerator RunDay3SafeUnlockedSiegeRoutine()
        {
            yield return new WaitForSeconds(0.75f);
            if (currentDay != GameDay.Day3 || isTransitioning || day3DoorTrapWaitingForEntry)
            {
                day3SiegeCoroutine = null;
                yield break;
            }

            float lampMult = GetUserLampMultiplier();
            float ambMult = GetUserAmbientMultiplier();
            float coolIntensity = 0.76f * lampMult;
            float warmIntensity = 0.62f * lampMult;

            // Step 1: Dim distant rooms (Kitchen, Bathroom) to a dying brownout so the house closes in around the corridor while staying navigable
            if (houseInteriorLights != null)
            {
                for (int i = 0; i < houseInteriorLights.Length; i++)
                {
                    Light l = houseInteriorLights[i];
                    if (l == null) continue;
                    string n = GetLampIdentifier(l);
                    if (n.Contains("cozinha"))
                    {
                        l.enabled = true;
                        l.intensity = coolIntensity * 0.28f;
                        SyncLampFixtureVisual(l, true, l.color, 0.24f);
                    }
                    else if (n.Contains("banheiro"))
                    {
                        l.enabled = true;
                        l.intensity = coolIntensity * 0.22f;
                        SyncLampFixtureVisual(l, true, l.color, 0.22f);
                    }
                }
            }

            Light bathSconce = FindSecondaryLightContaining("arandela_banheiro");
            if (bathSconce != null)
            {
                bathSconce.enabled = true;
                bathSconce.intensity = 0.24f * lampMult;
                SyncSecondaryFixtureVisual(bathSconce, true, bathSconce.color, 0.28f);
            }

            if (architecturalFillLights != null)
            {
                for (int i = 0; i < architecturalFillLights.Length; i++)
                {
                    Light fill = architecturalFillLights[i];
                    if (fill != null && fill.name.ToLowerInvariant().Contains("fill_spill_arcocozinha"))
                    {
                        fill.enabled = true;
                        fill.intensity = 0.09f * ambMult;
                    }
                }
            }

            if (audioManager != null)
            {
                audioManager.PlayFoley3DPanned(FoleySoundId.LightSwitchClick, new Vector3(-4.20f, 1.40f, 1.28f), 0.85f, 1.0f, 18f, 0.90f);
            }

            yield return new WaitForSeconds(0.55f);
            if (currentDay != GameDay.Day3 || isTransitioning || day3DoorTrapWaitingForEntry)
            {
                day3SiegeCoroutine = null;
                yield break;
            }

            lampMult = GetUserLampMultiplier();
            warmIntensity = 0.62f * lampMult;

            // Step 2: Dim Bedroom & Office lights to low brownout, close open interior doors, and narrow corridor lighting to the East/North end
            if (houseInteriorLights != null)
            {
                for (int i = 0; i < houseInteriorLights.Length; i++)
                {
                    Light l = houseInteriorLights[i];
                    if (l == null) continue;
                    string n = GetLampIdentifier(l);
                    if (n.Contains("quarto_cian"))
                    {
                        l.enabled = true;
                        l.intensity = warmIntensity * 0.22f;
                        SyncLampFixtureVisual(l, true, l.color, 0.20f);
                    }
                }
            }

            Light abajur = FindSecondaryLightContaining("abajur_margaret");
            if (abajur != null)
            {
                abajur.enabled = true;
                abajur.intensity = 0.26f * lampMult;
                SyncSecondaryFixtureVisual(abajur, true, abajur.color, 0.30f);
            }

            if (officeDoor != null && officeDoor.IsOpen)
            {
                officeDoor.SetDoorOpen(false, playSound: true);
            }

            SetDay3CircuitLightsState(true, 1f);
            if (audioManager != null)
            {
                audioManager.PlayFoley3DPanned(FoleySoundId.PowerCutDown, new Vector3(2.00f, 1.80f, 0.46f), 0.82f, 1.2f, 18f, 0.92f);
            }

            RefreshInteriorReflectionProbes();
            day3SiegeCoroutine = null;
        }

        private void PrepareDaughtersRoomForDay3Trap(float lampIntensityScale)
        {
            daughtersRoomTrapScale = Mathf.Max(0f, lampIntensityScale);
            if (quartoFilhas == null) return;
            if (!quartoFilhas.activeSelf)
            {
                quartoFilhas.SetActive(true);
            }

            if (daughtersRoomLight == null)
            {
                CacheSpecialCorridorLights();
            }

            if (daughtersRoomLight != null)
            {
                bool active = daughtersRoomTrapScale > 0.01f;
                Color trapColor = new Color(0.78f, 0.82f, 0.88f);
                daughtersRoomLight.enabled = active;
                daughtersRoomLight.color = trapColor;
                daughtersRoomLight.intensity = active ? (0.28f * daughtersRoomTrapScale * GetUserLampMultiplier()) : 0f;
                SyncLampFixtureVisual(daughtersRoomLight, active, trapColor, Mathf.Clamp01(0.28f * daughtersRoomTrapScale));
            }
        }

        public bool CanSitAtTVDay1(out string blockedReason, out bool missingOnlyBatteries)
        {
            blockedReason = string.Empty;
            missingOnlyBatteries = false;

            if (!requireDay1TasksBeforeTV)
                return true;

            if (day1RequiredTaskIds != null)
            {
                for (int i = 0; i < day1RequiredTaskIds.Count; i++)
                {
                    string req = NormalizeKey(day1RequiredTaskIds[i]);
                    if (string.IsNullOrEmpty(req)) continue;

                    if (!completedDay1Tasks.Contains(req))
                    {
                        blockedReason = "Se eu sentar agora, eu apago. Preciso resolver o que ficou pendente antes.";
                        return false;
                    }
                }
            }

            if (!InventoryState.HasItem("pilhas_controle"))
            {
                missingOnlyBatteries = true;
                blockedReason = "O botão do painel tá afundado e o controle morreu. Acho que tem pilha naquela lanterna velha da cozinha.";
                return false;
            }

            return true;
        }

        private void ResetAllInspectablesInGroup(GameObject groupRoot)
        {
            if (groupRoot == null) return;
            InspectableObject[] items = groupRoot.GetComponentsInChildren<InspectableObject>(true);
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    items[i].SetInteractableState(true, resetUsedState: true);
                }
            }
        }

        private void ResetAllContainersInGroup(GameObject groupRoot, GameDay day)
        {
            if (groupRoot == null) return;
            AnimatedContainerInteractable[] containers = groupRoot.GetComponentsInChildren<AnimatedContainerInteractable>(true);
            for (int i = 0; i < containers.Length; i++)
            {
                if (containers[i] != null)
                {
                    containers[i].ResetForDay(day);
                }
            }
        }

        private void ResetDay2SceneObjects()
        {
            day2ShadowTrigger = null;
            day2FramesTrigger = null;
            day2WhispersTrigger = null;

            if (propsDia2 == null) return;

            Light[] corridorFlickerLights = BuildCorridorFlickerLightArray();
            HallucinationTrigger[] triggers = propsDia2.GetComponentsInChildren<HallucinationTrigger>(true);

            for (int i = 0; i < triggers.Length; i++)
            {
                HallucinationTrigger trig = triggers[i];
                if (trig == null) continue;

                trig.ResetHallucinationState();
                trig.SetLightsToFlicker(corridorFlickerLights);

                trig.SetArmed(false);

                string id = trig.NormalizedId;
                if (id == "vulto_corredor")
                {
                    day2ShadowTrigger = trig;
                }
                else if (id == "quadros_familia_alterados")
                {
                    day2FramesTrigger = trig;
                }
                else if (id == "sussurros_porta_trancada" || id == "pre_sussurros_corredor")
                {
                    day2WhispersTrigger = trig;
                }
            }

            ResetAllInspectablesInGroup(propsDia2);
        }

        private Light[] BuildCorridorFlickerLightArray()
        {
            var list = new List<Light>();
            if (westCorridorLight != null) list.Add(westCorridorLight);
            if (centerCorridorLight != null) list.Add(centerCorridorLight);
            if (eastCorridorLight != null) list.Add(eastCorridorLight);
            return list.ToArray();
        }

        private void PopulateObjectivesForDay(GameDay day)
        {
            if (objectiveHUD == null) return;

            switch (day)
            {
                case GameDay.Day1:
                    objectiveHUD.ConfigureDayHeader("DIA 1", new Color(0.86f, 0.86f, 0.86f, 0.92f));
                    objectiveHUD.AddOrUpdateObjective("lavar_rosto", "Lavar o rosto no banheiro", false, true);
                    objectiveHUD.AddOrUpdateObjective("checar_secretaria", "Ouvir a secretária eletrônica", false, true);
                    objectiveHUD.AddOrUpdateObjective("pegar_chave_escritorio", "Achar a chave do escritório na cozinha", false, false);
                    objectiveHUD.AddOrUpdateObjective("destrancar_escritorio_dia1", "Abrir o escritório", false, true);
                    objectiveHUD.AddOrUpdateObjective("olhar_papeis", "Conferir os papéis na mesa", false, true);
                    objectiveHUD.AddOrUpdateObjective("checar_calendario", "Confirmar a data no calendário", false, true);
                    objectiveHUD.AddOrUpdateObjective("pilhas_controle_task", "Pegar pilhas na cozinha", false, false);
                    objectiveHUD.AddOrUpdateObjective("ligar_tv_dia1", "Sentar na sala e ligar a TV", false, true);
                    break;

                case GameDay.Day2:
                    objectiveHUD.ConfigureDayHeader("DIA 2", new Color(0.86f, 0.86f, 0.86f, 0.92f));
                    objectiveHUD.AddOrUpdateObjective("frascos_calmantes_dia2", "Ver os frascos na mesa da sala", false, true);
                    objectiveHUD.AddOrUpdateObjective("pegar_fita_banheiro_dia2", "Achar a fita da secretária", false, false);
                    objectiveHUD.AddOrUpdateObjective("secretaria_fita_dia2", "Checar a secretária apitando no corredor", false, true);
                    objectiveHUD.AddOrUpdateObjective("vulto_corredor", "Ir até o corredor", false, false);
                    objectiveHUD.AddOrUpdateObjective("quadros_familia_alterados", "Seguir pelo corredor leste", false, false);
                    objectiveHUD.AddOrUpdateObjective("sussurros_porta_trancada", "Verificar o barulho no fim do corredor", false, false);
                    break;

                case GameDay.Day3:
                    objectiveHUD.ConfigureDayHeader("DIA 3", new Color(0.86f, 0.86f, 0.86f, 0.92f));
                    objectiveHUD.AddOrUpdateObjective("pegar_lanterna_dia3", "Pegar a lanterna na gaveta da cozinha", false, true);
                    objectiveHUD.AddOrUpdateObjective("engolir_capsula_dia3", "Tomar as pílulas na mesa da sala", false, false);
                    objectiveHUD.AddOrUpdateObjective("restaurar_energia_dia3", "Checar o quadro de força na cozinha", false, false);
                    objectiveHUD.AddOrUpdateObjective("pegar_fusivel_dia3", "Procurar um fusível no banheiro", false, false);
                    objectiveHUD.AddOrUpdateObjective("retrato_margaret_dia3", "Examinar o retrato de Margaret no corredor", false, false);
                    objectiveHUD.AddOrUpdateObjective("cofre_1009_dia3", "Abrir o cofre atrás do retrato", false, false);
                    objectiveHUD.AddOrUpdateObjective("destrancar_porta_dia3", "Abrir a porta no fim do corredor", false, false);
                    objectiveHUD.AddOrUpdateObjective("entrar_quarto_dia3", "Entrar no quarto", false, false);
                    break;

                case GameDay.Day4_Prelude:
                    objectiveHUD.ClearObjectives();
                    objectiveHUD.SetHUDVisible(false, immediate: true);
                    break;
            }
        }

        public void TransitionToDay(GameDay targetDay, bool forceImmediateOverride = false, bool instantCut = false)
        {
            if (isTransitioning && !forceImmediateOverride)
                return;

            if (activeTransitionCoroutine != null)
            {
                StopCoroutine(activeTransitionCoroutine);
                activeTransitionCoroutine = null;
            }

            if (day4EndingCoroutine != null)
            {
                StopCoroutine(day4EndingCoroutine);
                day4EndingCoroutine = null;
            }

            activeTransitionCoroutine = StartCoroutine(TransitionRoutine(targetDay, instantCut));
        }

        private void StopTransientDayCoroutines(bool keepDoorTrapRoutine = false)
        {
            if (day2BeckonCoroutine != null)
            {
                StopCoroutine(day2BeckonCoroutine);
                day2BeckonCoroutine = null;
            }
            if (day3CircuitRestoreCoroutine != null)
            {
                StopCoroutine(day3CircuitRestoreCoroutine);
                day3CircuitRestoreCoroutine = null;
            }
            if (day3ClueReactionCoroutine != null)
            {
                StopCoroutine(day3ClueReactionCoroutine);
                day3ClueReactionCoroutine = null;
            }
            if (day3SiegeCoroutine != null)
            {
                StopCoroutine(day3SiegeCoroutine);
                day3SiegeCoroutine = null;
            }
            if (!keepDoorTrapRoutine && day3DoorTrapCoroutine != null)
            {
                StopCoroutine(day3DoorTrapCoroutine);
                day3DoorTrapCoroutine = null;
            }
            if (day2DoorScareCoroutine != null)
            {
                StopCoroutine(day2DoorScareCoroutine);
                day2DoorScareCoroutine = null;
            }
            if (day2TVActivationCoroutine != null)
            {
                StopCoroutine(day2TVActivationCoroutine);
                day2TVActivationCoroutine = null;
            }
            if (activeCutsceneLookCoroutine != null)
            {
                StopCoroutine(activeCutsceneLookCoroutine);
                activeCutsceneLookCoroutine = null;
            }
            if (stimulantEffectCoroutine != null)
            {
                StopCoroutine(stimulantEffectCoroutine);
                stimulantEffectCoroutine = null;
            }
            if (probeRefreshCoroutine != null)
            {
                StopCoroutine(probeRefreshCoroutine);
                probeRefreshCoroutine = null;
            }
            isStimulantBoostOrReboundActive = false;
            if (audioManager != null) audioManager.SetStimulantRoomToneClarity(0f);
            if (sanityEffects != null) sanityEffects.ClearStimulantModulationImmediate();
            if (player != null) player.ResetChemicalEffectsImmediate();
        }

        private IEnumerator TransitionRoutine(GameDay targetDay, bool instantCutToBlack, bool playStudioLogoIntro = false)
        {
            isTransitioning = true;
            StopTransientDayCoroutines();

            if (GameSettingsUI.Instance != null)
            {
                GameSettingsUI.Instance.CloseSettingsMenu(restorePlayerState: false);
            }
            ObjectiveHUD.IsBlockingModalOpen = false;
            SafeBoxPuzzle.CloseAnyOpenKeypad();

            if (objectiveHUD != null)
            {
                objectiveHUD.SetHUDVisible(false, immediate: true);
            }

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.CloseDocument(restorePlayerControls: false);
                InteractionSystem.Instance.ClearOverridePrompt();
                InteractionSystem.Instance.ClearSubtitleImmediate();
            }

            if (player != null)
            {
                // Keep exact seated/standing eye height locked while fading to black so Cian never stands up mid-fadeout.
                player.FreezeCurrentEyeHeightForFade();
                player.SetControlsEnabled(moveEnabled: false, lookEnabled: false, lockCursor: true);
                player.SetHorrorCameraInfluence(0f, 0f);
            }

            if (sanityEffects != null)
            {
                sanityEffects.ClearSpikesImmediate();
            }
            if (HorrorScreenOverlay.Instance != null)
            {
                HorrorScreenOverlay.Instance.ClearImmediate();
            }

            titleCanvasGroup.alpha = 0f;
            subCanvasGroup.alpha = 0f;
            if (studioLogoGroup != null) studioLogoGroup.alpha = 0f;
            if (studioLogoRoot != null) studioLogoRoot.SetActive(false);

            if (instantCutToBlack)
            {
                blackFadeGroup.alpha = 1f;
            }
            else
            {
                bool drowsyDay1SleepFade = targetDay == GameDay.Day2;
                float fadeOutDuration = drowsyDay1SleepFade ? 2.05f : 1.85f;
                if (audioManager != null)
                {
                    audioManager.FadeOutAndStopTVLoop(fadeOutDuration);
                }

                float startAlpha = blackFadeGroup.alpha;
                float elapsed = 0f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeOutDuration));
                    blackFadeGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);

                    if (drowsyDay1SleepFade && player != null)
                    {
                        // Continua abaixando suavemente o olhar enquanto adormece no sofá ao fim do Dia 1
                        player.SetPostureAndFovOffset(
                            Mathf.Lerp(14.0f, 19.5f, t),
                            Mathf.Lerp(-3.2f, -5.2f, t),
                            Mathf.Lerp(-10.5f, -12.0f, t));
                    }
                    yield return null;
                }
                blackFadeGroup.alpha = 1f;
            }

            if (player != null)
            {
                player.ClearWakeupEyeHeightOverride();
                player.ResetTransientCameraEffects();
                player.SetHorrorCameraInfluence(0f, 0f);
            }

            if (!(instantCutToBlack && targetDay == GameDay.Day4_Prelude))
            {
                if (audioManager != null) audioManager.ResetTransientAudioForTransition();
            }

            if (sanityEffects != null)
            {
                sanityEffects.ClearSpikesImmediate();
            }
            if (HorrorScreenOverlay.Instance != null)
            {
                HorrorScreenOverlay.Instance.ClearImmediate();
            }

            if (playStudioLogoIntro)
            {
                yield return PlayStudioLogoIntroRoutine();
            }
            else
            {
                float blackHold = (instantCutToBlack && targetDay == GameDay.Day4_Prelude) ? 0.20f : 0.35f;
                yield return new WaitForSeconds(blackHold);
            }

            ApplyDaySetupImmediate(targetDay, enablePlayerControlsNow: false);

            GetDayCardTexts(targetDay, out string title, out string subtitle);
            if (!string.IsNullOrEmpty(title))
            {
                dayCardTitleText.text = title;
                dayCardSubText.text = subtitle;

                if (audioManager != null && targetDay == GameDay.Day3)
                {
                    audioManager.PlayFoley2D(FoleySoundId.DreadSubPulse, 0.52f, 0.86f);
                }

                float titleInElapsed = 0f;
                const float titleInDur = 0.85f;
                while (titleInElapsed < titleInDur)
                {
                    titleInElapsed += Time.deltaTime;
                    titleCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(titleInElapsed / titleInDur));
                    yield return null;
                }
                titleCanvasGroup.alpha = 1f;

                yield return new WaitForSeconds(0.20f);

                float subInElapsed = 0f;
                const float subInDur = 0.70f;
                while (subInElapsed < subInDur)
                {
                    subInElapsed += Time.deltaTime;
                    subCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(subInElapsed / subInDur));
                    yield return null;
                }
                subCanvasGroup.alpha = 1f;

                yield return new WaitForSeconds(2.15f);

                float cardOutElapsed = 0f;
                const float cardOutDur = 0.80f;
                while (cardOutElapsed < cardOutDur)
                {
                    cardOutElapsed += Time.deltaTime;
                    float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cardOutElapsed / cardOutDur));
                    titleCanvasGroup.alpha = a;
                    subCanvasGroup.alpha = a;
                    yield return null;
                }
                titleCanvasGroup.alpha = 0f;
                subCanvasGroup.alpha = 0f;
                dayCardTitleText.text = string.Empty;
                dayCardSubText.text = string.Empty;

                yield return new WaitForSeconds(0.30f);
            }
            else
            {
                yield return new WaitForSeconds(0.15f);
            }

            float startWakePitch = targetDay == GameDay.Day2 ? 11f : (targetDay == GameDay.Day3 ? -8.0f : 6.5f);
            float startWakeRoll = targetDay == GameDay.Day2 ? -4.5f : (targetDay == GameDay.Day3 ? 26.0f : -2.0f);
            float startWakeFov = targetDay == GameDay.Day3 ? -7.0f : -5.5f;
            if (player != null)
            {
                player.SetPostureAndFovOffset(startWakePitch, startWakeRoll, startWakeFov, snapFov: true);
            }

            float openEyesDuration = targetDay == GameDay.Day4_Prelude ? 2.15f : (targetDay == GameDay.Day3 ? 1.60f : 1.85f);
            float openElapsed = 0f;
            bool lookUnlockedEarly = false;

            while (openElapsed < openEyesDuration)
            {
                openElapsed += Time.deltaTime;
                float u = Mathf.Clamp01(openElapsed / openEyesDuration);
                float easedClear = Mathf.SmoothStep(0f, 1f, u);

                blackFadeGroup.alpha = 1f - easedClear;

                // On Day 3, keep Cian collapsed sideways on the floor while eyes open; stand-up animation follows immediately
                if (player != null && targetDay != GameDay.Day3)
                {
                    float postureEase = 1f - easedClear;
                    player.SetPostureAndFovOffset(
                        startWakePitch * postureEase,
                        startWakeRoll * postureEase,
                        startWakeFov * postureEase
                    );
                }

                if (targetDay != GameDay.Day3 && !lookUnlockedEarly && u >= 0.78f)
                {
                    lookUnlockedEarly = true;
                    if (player != null)
                    {
                        player.SetControlsEnabled(moveEnabled: false, lookEnabled: true, lockCursor: true);
                    }
                }

                yield return null;
            }

            blackFadeGroup.alpha = 0f;

            ShowWakeupMonologue(targetDay);

            if (targetDay == GameDay.Day2 && player != null && spawnSofaSentado != null)
            {
                player.ResetPostureAndFovOffsets(snap: false);
                Vector3 sitPos = spawnSofaSentado.position;
                Vector3 standPos = GetSofaStandUpWorldPosition();
                const float standUpDuration = 0.90f;
                float standElapsed = 0f;
                while (standElapsed < standUpDuration)
                {
                    standElapsed += Time.deltaTime;
                    float u = Mathf.Clamp01(standElapsed / standUpDuration);
                    float moveEase = Mathf.SmoothStep(0f, 1f, u);
                    player.transform.position = Vector3.Lerp(sitPos, standPos, moveEase);
                    player.UpdateWakeupStandEase(1.12f, u);
                    yield return null;
                }
                player.transform.position = standPos;
                Physics.SyncTransforms();
                player.ClearWakeupEyeHeightOverride();
                SetSofaCollisionEnabled(true);
            }
            else if (targetDay == GameDay.Day3 && player != null)
            {
                // Collapsed-on-floor -> 2-stage weighted stand-up camera animation (0.28m -> 0.92m -> 1.65m)
                yield return new WaitForSeconds(0.45f);
                if (audioManager != null)
                {
                    audioManager.PlayFoley2D(FoleySoundId.ClothInspect, 0.72f, 0.88f);
                    audioManager.PlayFoley3D(FoleySoundId.DistantFloorCreak, player.transform.position, 0.56f);
                }

                const float riseDuration = 1.95f;
                float riseElapsed = 0f;
                bool stage2FoleyPlayed = false;
                while (riseElapsed < riseDuration)
                {
                    riseElapsed += Time.deltaTime;
                    float u = Mathf.Clamp01(riseElapsed / riseDuration);
                    float eyeY;
                    float curPitch;
                    float curRoll;
                    float curFov;

                    if (u < 0.55f)
                    {
                        float s = Mathf.SmoothStep(0f, 1f, u / 0.55f);
                        eyeY = Mathf.Lerp(0.28f, 0.92f, s);
                        curRoll = Mathf.Lerp(26.0f, 7.0f, s) + Mathf.Sin(s * Mathf.PI) * 2.4f;
                        curPitch = Mathf.Lerp(-8.0f, 6.5f, s);
                        curFov = Mathf.Lerp(-7.0f, -2.5f, s);
                    }
                    else
                    {
                        if (!stage2FoleyPlayed)
                        {
                            stage2FoleyPlayed = true;
                            if (audioManager != null)
                            {
                                audioManager.PlayFoley2D(FoleySoundId.ClothInspect, 0.60f, 0.96f);
                            }
                        }
                        float s = Mathf.SmoothStep(0f, 1f, (u - 0.55f) / 0.45f);
                        eyeY = Mathf.Lerp(0.92f, 1.65f, s);
                        curRoll = Mathf.Lerp(7.0f, 0f, s) - Mathf.Sin(s * Mathf.PI) * 1.4f;
                        curPitch = Mathf.Lerp(6.5f, 0f, s);
                        curFov = Mathf.Lerp(-2.5f, 0f, s);
                    }

                    player.SetWakeupEyeHeightTarget(eyeY, snapCameraY: true);
                    player.SetPostureAndFovOffset(curPitch, curRoll, curFov, snapFov: false);
                    yield return null;
                }

                player.ClearWakeupEyeHeightOverride();
                player.ResetPostureAndFovOffsets(snap: false);
            }
            else if (player != null)
            {
                player.ResetPostureAndFovOffsets(snap: false);
            }

            RestorePlayerControlForCurrentDay();

            if (objectiveHUD != null && targetDay != GameDay.Day4_Prelude)
            {
                objectiveHUD.SetHUDVisible(true);
            }

            isTransitioning = false;
            activeTransitionCoroutine = null;

            if (targetDay == GameDay.Day4_Prelude)
            {
                day4EndingCoroutine = StartCoroutine(RunDay4PreludeEndingRoutine());
            }
        }

        private void ApplyDaySetupImmediate(GameDay day, bool enablePlayerControlsNow = true)
        {
            currentDay = day;
            demoEnded = false;
            if (endDemoHintObj != null) endDemoHintObj.SetActive(false);

            InventoryState.ClearAll();
            DoorInteractable.PlayerHasSafeKey = false;
            if (objectiveHUD != null) objectiveHUD.ClearEventHides();
            CacheSpecialCorridorLights();
            DisableLegacyHeadlightAndMainLight();

            if (audioManager != null)
            {
                audioManager.SetDayAmbienceMood((int)day);
            }

            if (casaBase != null)
            {
                casaBase.SetActive(true);
                ResetAllInspectablesInGroup(casaBase);
            }

            if (propsDia1 != null) propsDia1.SetActive(day == GameDay.Day1);
            if (propsDia2 != null) propsDia2.SetActive(day == GameDay.Day2);
            if (propsDia3 != null) propsDia3.SetActive(day == GameDay.Day3);
            if (quartoFilhas != null) quartoFilhas.SetActive(day == GameDay.Day4_Prelude);

            if (margaretJumpscareObject != null) margaretJumpscareObject.SetActive(false);
            SetMargaretChiaroscuroLightsActive(false, 0f);
            daughtersRoomTrapScale = 0f;
            if (day3RestoredCircuitLight != null) day3RestoredCircuitLight.enabled = false;
            completedDay3Clues.Clear();
            day3SafeUnlocked = false;
            day3DoorTrapWaitingForEntry = false;
            day3MandatoryPillTaken = false;
            day3BaseStimulantActive = false;
            pendingDay3NewspaperCrash = false;
            StopAndResetStimulantEffectsImmediate(clearPlayerDrugState: true);
            SetKitchenDrawerFlashlightVisualVisible(true);
            EnsureFlashlightController();
            if (flashlightController != null)
            {
                flashlightController.SetEquipped(false, turnOnImmediately: false, playSound: false);
            }

            if (mirrorHaunt == null) mirrorHaunt = FindAnyObjectByType<MirrorHauntController>(FindObjectsInactive.Include);
            if (mirrorHaunt != null) mirrorHaunt.ConfigureForDay(day);
            FaucetWaterController bathroomFaucet = FindAnyObjectByType<FaucetWaterController>(FindObjectsInactive.Include);
            if (bathroomFaucet != null) bathroomFaucet.ResetImmediate();

            RefreshWatchersCacheAndDisarmAll();

            if (tvController != null)
            {
                tvController.ConfigureForDay(day);
            }

            switch (day)
            {
                case GameDay.Day1:
                    completedDay1Tasks.Clear();
                    ResetAllInspectablesInGroup(propsDia1);
                    ResetAllContainersInGroup(casaBase, GameDay.Day1);
                    ResetAllContainersInGroup(propsDia1, GameDay.Day1);

                    if (officeDoor != null)
                    {
                        officeDoor.ConfigureLockState(
                            locked: true,
                            allowSafeKey: false,
                            forceCloseImmediately: true,
                            keyItemId: "chave_escritorio"
                        );
                    }

                    if (corridorLockedDoor != null)
                        corridorLockedDoor.ConfigureLockState(locked: true, allowSafeKey: false, forceCloseImmediately: true);

                    if (player != null)
                    {
                        SetSofaCollisionEnabled(true);
                        player.DisableRestrictedLook();
                        player.SetDepressantEffect(false);
                        player.SetActivatingDrugEffect(false);
                        if (spawnDia1 != null) player.TeleportTo(spawnDia1);
                    }

                    if (sanityEffects != null)
                        sanityEffects.SetPsychologicalState(PsychologicalState.Day1_Burnout, immediate: true);

                    ApplyAtmosphericLightingForDay(GameDay.Day1);
                    break;

                case GameDay.Day2:
                    completedDay2LinearEvents.Clear();
                    day2NewsTriggered = false;

                    ResetDay2SceneObjects();
                    ResetAllContainersInGroup(casaBase, GameDay.Day2);
                    ResetAllContainersInGroup(propsDia2, GameDay.Day2);

                    if (officeDoor != null)
                        officeDoor.ConfigureLockState(locked: false, allowSafeKey: false, forceCloseImmediately: false);

                    if (corridorLockedDoor != null)
                        corridorLockedDoor.ConfigureLockState(locked: true, allowSafeKey: false, forceCloseImmediately: true);

                    if (player != null)
                    {
                        player.DisableRestrictedLook();
                        player.SetActivatingDrugEffect(false);
                        player.SetDepressantEffect(true);
                        if (!enablePlayerControlsNow && spawnSofaSentado != null)
                        {
                            SetSofaCollisionEnabled(false);
                            player.SetCharacterControllerEnabledForCutscene(false);
                            player.TeleportTo(spawnSofaSentado);
                            player.SetWakeupEyeHeightImmediate(1.12f);
                        }
                        else
                        {
                            SetSofaCollisionEnabled(true);
                            if (spawnDia2Sofa != null)
                            {
                                player.TeleportTo(spawnDia2Sofa);
                            }
                        }
                    }

                    if (sanityEffects != null)
                        sanityEffects.SetPsychologicalState(PsychologicalState.Day2_Depressant, immediate: true);

                    ApplyAtmosphericLightingForDay(GameDay.Day2);
                    break;

                case GameDay.Day3:
                    ResetAllInspectablesInGroup(propsDia3);
                    ResetAllContainersInGroup(casaBase, GameDay.Day3);
                    ResetAllContainersInGroup(propsDia3, GameDay.Day3);

                    if (propsDia3 != null)
                    {
                        SafeBoxPuzzle[] safes = propsDia3.GetComponentsInChildren<SafeBoxPuzzle>(true);
                        for (int i = 0; i < safes.Length; i++)
                        {
                            if (safes[i] != null) safes[i].ResetSafeState();
                        }

                        FallingPictureFrame[] frames = propsDia3.GetComponentsInChildren<FallingPictureFrame>(true);
                        for (int i = 0; i < frames.Length; i++)
                        {
                            if (frames[i] != null) frames[i].ResetFrameState();
                        }
                    }

                    if (officeDoor != null)
                        officeDoor.ConfigureLockState(locked: false, allowSafeKey: false, forceCloseImmediately: false);

                    if (corridorLockedDoor != null)
                        corridorLockedDoor.ConfigureLockState(locked: true, allowSafeKey: true, forceCloseImmediately: true);

                    if (player != null)
                    {
                        SetSofaCollisionEnabled(true);
                        player.DisableRestrictedLook();
                        player.SetDepressantEffect(false);
                        player.SetActivatingDrugEffect(false);
                        ResolveDay3WakeupPose(out Vector3 d3Pos, out Quaternion d3Rot);
                        player.TeleportTo(d3Pos, d3Rot);
                        if (!enablePlayerControlsNow)
                        {
                            player.SetWakeupEyeHeightImmediate(0.28f);
                        }
                    }

                    if (sanityEffects != null)
                        sanityEffects.SetPsychologicalState(PsychologicalState.Day3_Hallucinogen, immediate: true);

                    ApplyAtmosphericLightingForDay(GameDay.Day3);
                    break;

                case GameDay.Day4_Prelude:
                    if (officeDoor != null)
                        officeDoor.ConfigureLockState(locked: true, allowSafeKey: false, forceCloseImmediately: true);

                    if (corridorLockedDoor != null)
                        corridorLockedDoor.ConfigureLockState(locked: true, allowSafeKey: false, forceCloseImmediately: true);

                    if (player != null)
                    {
                        SetSofaCollisionEnabled(true);
                        player.SetActivatingDrugEffect(false);
                        player.SetDepressantEffect(true);
                        if (spawnDia4QuartoFilhas != null) player.TeleportTo(spawnDia4QuartoFilhas);
                        player.EnableRestrictedLook(72f, -24f, 22f, customEyeHeight: 1.30f);
                    }

                    if (sanityEffects != null)
                        sanityEffects.SetPsychologicalState(PsychologicalState.Day4_MelancholicPrelude, immediate: true);

                    ApplyAtmosphericLightingForDay(GameDay.Day4_Prelude);
                    break;
            }

            PopulateObjectivesForDay(day);
            SyncClocksForDay(day);
            SyncFrontDoorForDay(day);

            if (horrorDirector == null)
            {
                horrorDirector = GetComponent<PsychologicalHorrorDirector>();
                if (horrorDirector == null) horrorDirector = gameObject.AddComponent<PsychologicalHorrorDirector>();
            }
            horrorDirector.ConfigureForDay(day, player, audioManager, houseInteriorLights, eastCorridorLight, corridorLockedDoor);

            if (enablePlayerControlsNow)
            {
                RestorePlayerControlForCurrentDay();
            }
            else if (player != null)
            {
                player.SetControlsEnabled(moveEnabled: false, lookEnabled: false, lockCursor: true);
            }
        }

        private void RefreshWatchersCacheAndDisarmAll()
        {
            corridorAutoSputterSuppressDepth = 0;
            cachedWatchers.Clear();
            WatcherHallucination[] found = FindObjectsByType<WatcherHallucination>(FindObjectsInactive.Include);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                {
                    found[i].DisarmAndHide();
                    cachedWatchers.Add(found[i]);
                }
            }
        }

        private void DisarmUntriggeredWatchersForDay(GameDay targetDay)
        {
            for (int i = 0; i < cachedWatchers.Count; i++)
            {
                WatcherHallucination w = cachedWatchers[i];
                if (w == null || w.HasTriggered) continue;
                string wn = w.gameObject.name.ToLowerInvariant();
                if ((targetDay == GameDay.Day2 && (wn.Contains("d2") || wn.Contains("dia2")))
                    || (targetDay == GameDay.Day3 && (wn.Contains("d3") || wn.Contains("dia3"))))
                {
                    w.DisarmAndHide();
                }
            }
        }

        private void ArmWatchersForDay(GameDay targetDay, string contextFilter)
        {
            if (cachedWatchers.Count == 0)
            {
                WatcherHallucination[] found = FindObjectsByType<WatcherHallucination>(FindObjectsInactive.Include);
                cachedWatchers.AddRange(found);
            }

            Light[] corridorLights = BuildCorridorFlickerLightArray();
            string filterLower = string.IsNullOrEmpty(contextFilter) ? string.Empty : contextFilter.ToLowerInvariant();
            for (int i = 0; i < cachedWatchers.Count; i++)
            {
                WatcherHallucination w = cachedWatchers[i];
                if (w == null || w.HasTriggered) continue;

                string wn = w.gameObject.name.ToLowerInvariant();
                if (targetDay == GameDay.Day2 && (wn.Contains("d2") || wn.Contains("dia2") || wn.Contains(filterLower)))
                {
                    if (corridorLights != null && corridorLights.Length > 0) w.SetLightsToPulse(corridorLights);
                    w.ArmWatcher();
                }
                else if (targetDay == GameDay.Day3 && (wn.Contains("d3") || wn.Contains("dia3") || wn.Contains(filterLower)))
                {
                    if (corridorLights != null && corridorLights.Length > 0) w.SetLightsToPulse(corridorLights);
                    w.ArmWatcher();
                }
            }
        }

        private void RestorePlayerControlForCurrentDay()
        {
            if (player == null) return;

            if (currentDay == GameDay.Day4_Prelude)
            {
                if (!player.IsRestrictedLook)
                {
                    player.EnableRestrictedLook(72f, -24f, 22f, customEyeHeight: 1.30f);
                }
                else
                {
                    player.SetControlsEnabled(moveEnabled: false, lookEnabled: true, lockCursor: true);
                }
                player.SetCursorLocked(true);
            }
            else
            {
                player.DisableRestrictedLook();
                player.SetControlsEnabled(moveEnabled: true, lookEnabled: true, lockCursor: true);
            }
        }

        public void SetUserBrightnessNormalized(float normalized01)
        {
            userBrightnessNormalized = Mathf.Clamp(normalized01, 0f, GameSettingsUI.MaxBrightness);
            ApplyAtmosphericLightingForDay(ResolveEditableLightingDay());
        }

        public void SetUserBrightnessNormalizedDeferredProbes(float normalized01)
        {
            suppressNextProbeRefresh = true;
            try
            {
                SetUserBrightnessNormalized(normalized01);
            }
            finally
            {
                suppressNextProbeRefresh = false;
            }
        }

        public void SetBrightShadowsEnabled(bool enabled)
        {
            brightShadowsEnabled = enabled;
            ApplyAtmosphericLightingForDay(ResolveEditableLightingDay());
        }

        private GameDay ResolveEditableLightingDay()
        {
            if (System.Enum.IsDefined(typeof(GameDay), currentDay))
                return currentDay;
            if (System.Enum.IsDefined(typeof(GameDay), lastAppliedLightingDay))
                return lastAppliedLightingDay;
            return startingDay;
        }

        public void ApplyShadowQualityToInteriorLights(bool enableSoftShadows)
        {
            softShadowsEnabledByQuality = enableSoftShadows;
            LightShadows casterMode = enableSoftShadows ? LightShadows.Soft : LightShadows.Hard;

            if (houseInteriorLights != null)
            {
                for (int i = 0; i < houseInteriorLights.Length; i++)
                {
                    Light l = houseInteriorLights[i];
                    if (l == null) continue;
                    l.shadows = casterMode;
                }
            }

            if (day3RestoredCircuitLight != null)
            {
                day3RestoredCircuitLight.shadows = casterMode;
            }

            if (secondaryMotivatedLights != null)
            {
                for (int i = 0; i < secondaryMotivatedLights.Length; i++)
                {
                    Light sec = secondaryMotivatedLights[i];
                    if (sec == null) continue;
                    sec.shadows = casterMode;
                }
            }

            if (architecturalFillLights != null)
            {
                for (int i = 0; i < architecturalFillLights.Length; i++)
                {
                    Light fill = architecturalFillLights[i];
                    if (fill == null) continue;
                    fill.shadows = LightShadows.None;
                }
            }

            if (tvController != null)
            {
                tvController.SetTvLightShadowMode(casterMode);
            }

            if (flashlightController != null)
            {
                flashlightController.SetShadowQuality(enableSoftShadows);
            }
        }

        private float GetUserAmbientMultiplier()
        {
            return userBrightnessNormalized < 0.5f
                ? Mathf.Lerp(0.58f, 1.0f, userBrightnessNormalized / 0.5f)
                : Mathf.Lerp(1.0f, 2.42f, (userBrightnessNormalized - 0.5f) / 0.50f);
        }

        private float GetUserLampMultiplier()
        {
            return userBrightnessNormalized < 0.5f
                ? Mathf.Lerp(0.75f, 1.0f, userBrightnessNormalized / 0.5f)
                : Mathf.Lerp(1.0f, 1.70f, (userBrightnessNormalized - 0.5f) / 0.50f);
        }

        private static Color ScaleLinear(Color srgbColor, float linearMultiplier)
        {
            return (srgbColor.linear * Mathf.Max(0f, linearMultiplier)).gamma;
        }

        private void ApplyShadowCornerAmbient(
            Color normalSky, Color normalEquator, Color normalGround,
            Color clearSky, Color clearEquator, Color clearGround,
            float ambMult)
        {
            if (!brightShadowsEnabled)
            {
                RenderSettings.ambientSkyColor = ScaleLinear(normalSky, ambMult);
                RenderSettings.ambientEquatorColor = ScaleLinear(normalEquator, ambMult);
                RenderSettings.ambientGroundColor = ScaleLinear(normalGround, ambMult);
                return;
            }

            float clearScale = userBrightnessNormalized < 0.5f
                ? Mathf.Lerp(0.72f, 1.0f, userBrightnessNormalized / 0.5f)
                : Mathf.Lerp(1.0f, 1.42f, (userBrightnessNormalized - 0.5f) / 0.50f);

            RenderSettings.ambientSkyColor = ScaleLinear(clearSky, clearScale);
            RenderSettings.ambientEquatorColor = ScaleLinear(clearEquator, clearScale);
            RenderSettings.ambientGroundColor = ScaleLinear(clearGround, clearScale);
        }

        private void ApplyAtmosphericLightingForDay(GameDay day)
        {
            if (System.Enum.IsDefined(typeof(GameDay), day))
            {
                lastAppliedLightingDay = day;
            }
            DisableLegacyHeadlightAndMainLight();

            RenderSettings.skybox = null;
            RenderSettings.sun = null;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = brightShadowsEnabled ? 0.52f : 0.28f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;

            float ambMult = GetUserAmbientMultiplier();
            float lampMult = GetUserLampMultiplier();

            switch (day)
            {
                case GameDay.Day1:
                    ApplyShadowCornerAmbient(
                        new Color(0.052f, 0.050f, 0.048f),
                        new Color(0.066f, 0.064f, 0.061f),
                        new Color(0.030f, 0.028f, 0.026f),
                        new Color(0.156f, 0.153f, 0.148f),
                        new Color(0.194f, 0.190f, 0.184f),
                        new Color(0.122f, 0.119f, 0.115f),
                        ambMult
                    );
                    RenderSettings.fogColor = new Color(0.020f, 0.019f, 0.018f);
                    RenderSettings.fogDensity = 0.007f;

                    ConfigureRoomLamps(
                        day: GameDay.Day1,
                        warmTungstenColor: new Color(1.00f, 0.85f, 0.64f),
                        warmIntensity: 1.32f * lampMult,
                        coolFluorescentColor: new Color(0.93f, 0.95f, 0.93f),
                        coolIntensity: 1.54f * lampMult,
                        eastCorridorIntensity: 0.60f * lampMult,
                        blackoutCorridorCircuit: false
                    );
                    ConfigureSecondaryAndFillLights(GameDay.Day1, ambMult, lampMult, corridorRestored: true);
                    break;

                case GameDay.Day2:
                    ApplyShadowCornerAmbient(
                        new Color(0.046f, 0.045f, 0.044f),
                        new Color(0.058f, 0.057f, 0.056f),
                        new Color(0.027f, 0.026f, 0.026f),
                        new Color(0.146f, 0.144f, 0.142f),
                        new Color(0.182f, 0.180f, 0.177f),
                        new Color(0.114f, 0.112f, 0.110f),
                        ambMult
                    );
                    RenderSettings.fogColor = new Color(0.017f, 0.017f, 0.017f);
                    RenderSettings.fogDensity = 0.010f;

                    ConfigureRoomLamps(
                        day: GameDay.Day2,
                        warmTungstenColor: new Color(0.96f, 0.80f, 0.58f),
                        warmIntensity: 0.96f * lampMult,
                        coolFluorescentColor: new Color(0.90f, 0.93f, 0.92f),
                        coolIntensity: 1.34f * lampMult,
                        eastCorridorIntensity: 0.46f * lampMult,
                        blackoutCorridorCircuit: false
                    );
                    ConfigureSecondaryAndFillLights(GameDay.Day2, ambMult, lampMult, corridorRestored: true);
                    break;

                case GameDay.Day3:
                    ApplyShadowCornerAmbient(
                        new Color(0.032f, 0.032f, 0.034f),
                        new Color(0.046f, 0.046f, 0.048f),
                        new Color(0.020f, 0.020f, 0.021f),
                        new Color(0.128f, 0.128f, 0.132f),
                        new Color(0.160f, 0.160f, 0.165f),
                        new Color(0.098f, 0.098f, 0.101f),
                        ambMult
                    );
                    RenderSettings.fogColor = new Color(0.011f, 0.011f, 0.012f);
                    RenderSettings.fogDensity = 0.0145f;

                    bool circuitActive = InventoryState.HasItem("energia_corredor_ativa");
                    ConfigureRoomLamps(
                        day: GameDay.Day3,
                        warmTungstenColor: new Color(0.95f, 0.77f, 0.52f),
                        warmIntensity: 0.62f * lampMult,
                        coolFluorescentColor: new Color(0.88f, 0.91f, 0.89f),
                        coolIntensity: 0.76f * lampMult,
                        eastCorridorIntensity: 0.0f,
                        blackoutCorridorCircuit: !circuitActive
                    );
                    ConfigureSecondaryAndFillLights(GameDay.Day3, ambMult, lampMult, corridorRestored: circuitActive);

                    if (circuitActive)
                    {
                        SetDay3CircuitLightsState(true, 1f);
                    }
                    if (daughtersRoomTrapScale > 0.01f && (day3SafeUnlocked || day3DoorTrapWaitingForEntry))
                    {
                        PrepareDaughtersRoomForDay3Trap(daughtersRoomTrapScale);
                    }
                    break;

                case GameDay.Day4_Prelude:
                    ApplyShadowCornerAmbient(
                        new Color(0.044f, 0.044f, 0.046f),
                        new Color(0.058f, 0.058f, 0.060f),
                        new Color(0.026f, 0.026f, 0.028f),
                        new Color(0.140f, 0.140f, 0.144f),
                        new Color(0.176f, 0.176f, 0.180f),
                        new Color(0.108f, 0.108f, 0.112f),
                        ambMult
                    );
                    RenderSettings.fogColor = new Color(0.015f, 0.015f, 0.016f);
                    RenderSettings.fogDensity = 0.008f;

                    ConfigureRoomLamps(
                        day: GameDay.Day4_Prelude,
                        warmTungstenColor: new Color(0.88f, 0.90f, 0.94f),
                        warmIntensity: 1.18f * lampMult,
                        coolFluorescentColor: new Color(0.88f, 0.90f, 0.94f),
                        coolIntensity: 1.18f * lampMult,
                        eastCorridorIntensity: 0.0f,
                        blackoutCorridorCircuit: true
                    );
                    ConfigureSecondaryAndFillLights(GameDay.Day4_Prelude, ambMult, lampMult, corridorRestored: false);
                    break;
            }

            SyncAppliancePowerIndicators(day, blackoutOverride: false);
            ApplyShadowQualityToInteriorLights(softShadowsEnabledByQuality);
            if (Application.isPlaying && !suppressNextProbeRefresh)
            {
                RefreshInteriorReflectionProbes();
            }
        }

        private void ConfigureRoomLamps(
            GameDay day,
            Color warmTungstenColor,
            float warmIntensity,
            Color coolFluorescentColor,
            float coolIntensity,
            float eastCorridorIntensity,
            bool blackoutCorridorCircuit)
        {
            eastCorridorBaseIntensity = eastCorridorIntensity;
            if (houseInteriorLights == null) return;

            for (int i = 0; i < houseInteriorLights.Length; i++)
            {
                Light l = houseInteriorLights[i];
                if (l == null) continue;

                string n = GetLampIdentifier(l);
                if (day == GameDay.Day4_Prelude)
                {
                    bool isDaughtersLamp = n.Contains("quartofilhas");
                    l.enabled = isDaughtersLamp;
                    l.color = new Color(0.88f, 0.90f, 0.94f);
                    l.intensity = isDaughtersLamp ? warmIntensity : 0f;
                    SyncLampFixtureVisual(l, isDaughtersLamp && l.intensity > 0.02f, l.color, isDaughtersLamp ? 0.82f : 0f);
                    continue;
                }

                // True total blackout at Day 3 start until circuit breaker is restored
                if (day == GameDay.Day3 && blackoutCorridorCircuit)
                {
                    l.enabled = false;
                    l.intensity = 0f;
                    SyncLampFixtureVisual(l, false, Color.black, 0f);
                    continue;
                }

                if ((blackoutCorridorCircuit || day == GameDay.Day3) &&
                    (n.Contains("sala") || n.Contains("quartofilhas")))
                {
                    l.enabled = false;
                    l.intensity = 0f;
                    SyncLampFixtureVisual(l, false, Color.black, 0f);
                    continue;
                }

                if (blackoutCorridorCircuit &&
                    (n.Contains("corredor") || n.Contains("escritorio")))
                {
                    l.enabled = false;
                    l.intensity = 0f;
                    SyncLampFixtureVisual(l, false, Color.black, 0f);
                    continue;
                }

                l.enabled = true;
                if (n.Contains("corredor_leste"))
                {
                    l.color = new Color(0.94f, 0.76f, 0.54f);
                    l.intensity = eastCorridorIntensity;
                    SyncLampFixtureVisual(l, eastCorridorIntensity > 0.02f, l.color, Mathf.Clamp01(eastCorridorIntensity * 0.78f));
                }
                else if (n.Contains("banheiro"))
                {
                    l.enabled = true;
                    l.color = new Color(0.91f, 0.94f, 0.96f);
                    if (day == GameDay.Day3)
                    {
                        l.intensity = day3SafeUnlocked ? coolIntensity * 0.22f : coolIntensity * 0.44f;
                        SyncLampFixtureVisual(l, l.intensity > 0.02f, l.color, day3SafeUnlocked ? 0.22f : 0.48f);
                    }
                    else
                    {
                        l.intensity = coolIntensity * 0.92f;
                        SyncLampFixtureVisual(l, l.intensity > 0.02f, l.color, 0.84f);
                    }
                }
                else if (n.Contains("cozinha"))
                {
                    l.enabled = true;
                    l.color = coolFluorescentColor;
                    if (day == GameDay.Day3 && day3SafeUnlocked)
                    {
                        l.intensity = coolIntensity * 0.28f;
                        SyncLampFixtureVisual(l, l.intensity > 0.02f, l.color, 0.24f);
                    }
                    else
                    {
                        l.intensity = coolIntensity;
                        SyncLampFixtureVisual(l, l.intensity > 0.02f, l.color, day == GameDay.Day3 ? 0.64f : 0.88f);
                    }
                }
                else if (n.Contains("quarto_cian"))
                {
                    // On Day 3, keep a very dim under-voltage filament glow so the west wardrobe stays navigable while Margaret's bedside lamp dominates the nightstand
                    l.enabled = true;
                    l.color = warmTungstenColor;
                    if (day == GameDay.Day3)
                    {
                        l.intensity = day3SafeUnlocked ? warmIntensity * 0.22f : warmIntensity * 0.42f;
                    }
                    else
                    {
                        l.intensity = warmIntensity;
                    }
                    SyncLampFixtureVisual(l, l.intensity > 0.02f, l.color, day == GameDay.Day3 && day3SafeUnlocked ? 0.20f : Mathf.Clamp01(l.intensity * 0.72f));
                }
                else if (n.Contains("escritorio"))
                {
                    l.color = new Color(0.93f, 0.94f, 0.93f);
                    l.intensity = day == GameDay.Day1 ? coolIntensity * 0.90f : coolIntensity * 0.76f;
                    SyncLampFixtureVisual(l, l.intensity > 0.02f, l.color, 0.78f);
                }
                else if (n.Contains("sala"))
                {
                    l.color = warmTungstenColor;
                    l.intensity = day == GameDay.Day2 ? warmIntensity * 0.80f : warmIntensity * 0.94f;
                    SyncLampFixtureVisual(l, l.intensity > 0.02f, l.color, day == GameDay.Day2 ? 0.62f : 0.80f);
                }
                else
                {
                    l.color = warmTungstenColor;
                    l.intensity = warmIntensity;
                    SyncLampFixtureVisual(l, l.intensity > 0.02f, l.color, Mathf.Clamp01(l.intensity * 0.72f));
                }
            }
        }

        private void ConfigureSecondaryAndFillLights(GameDay day, float ambMult, float lampMult, bool corridorRestored)
        {
            if (day3RestoredCircuitLight != null)
            {
                bool deskLampLit = (day == GameDay.Day1) || (day == GameDay.Day3 && corridorRestored);
                day3RestoredCircuitLight.enabled = deskLampLit;
                day3RestoredCircuitLight.color = new Color(1.00f, 0.89f, 0.70f);
                day3RestoredCircuitLight.intensity = deskLampLit
                    ? (day == GameDay.Day1 ? 0.95f * lampMult : (day3SafeUnlocked ? 0.42f * lampMult : 1.36f * lampMult))
                    : 0f;
                SyncBankerLampVisual(day3RestoredCircuitLight, deskLampLit, day == GameDay.Day1 ? 0.68f : (day3SafeUnlocked ? 0.38f : 0.92f));
            }

            if (secondaryMotivatedLights != null)
            {
                for (int i = 0; i < secondaryMotivatedLights.Length; i++)
                {
                    Light sec = secondaryMotivatedLights[i];
                    if (sec == null) continue;
                    string sn = sec.name.ToLowerInvariant();

                    if (sn.Contains("abajur_margaret"))
                    {
                        bool on = day == GameDay.Day1 || day == GameDay.Day2 || (day == GameDay.Day3 && corridorRestored);
                        float baseVal = day == GameDay.Day1 ? 0.95f : (day == GameDay.Day2 ? 0.78f : (day3SafeUnlocked ? 0.26f : 0.84f));
                        sec.enabled = on;
                        sec.color = new Color(1.00f, 0.83f, 0.58f);
                        sec.intensity = on ? baseVal * lampMult : 0f;
                        SyncSecondaryFixtureVisual(sec, on, sec.color, on ? (day == GameDay.Day3 && day3SafeUnlocked ? 0.30f : 0.85f) : 0f);
                    }
                    else if (sn.Contains("luminaria_coluna_sala"))
                    {
                        bool on = day == GameDay.Day1 || day == GameDay.Day2;
                        float baseVal = day == GameDay.Day1 ? 1.02f : 0.76f;
                        sec.enabled = on;
                        sec.color = new Color(1.00f, 0.84f, 0.60f);
                        sec.intensity = on ? baseVal * lampMult : 0f;
                        SyncSecondaryFixtureVisual(sec, on, sec.color, on ? 0.85f : 0f);
                    }
                    else if (sn.Contains("arandela_banheiro"))
                    {
                        bool on = day == GameDay.Day1 || day == GameDay.Day2 || (day == GameDay.Day3 && corridorRestored);
                        float baseVal = day == GameDay.Day1 ? 0.90f : (day == GameDay.Day2 ? 0.82f : (day3SafeUnlocked ? 0.24f : 0.78f));
                        sec.enabled = on;
                        sec.color = new Color(0.92f, 0.94f, 0.96f);
                        sec.intensity = on ? baseVal * lampMult : 0f;
                        SyncSecondaryFixtureVisual(sec, on, sec.color, on ? (day == GameDay.Day3 && day3SafeUnlocked ? 0.28f : 0.80f) : 0f);
                    }
                    else
                    {
                        sec.enabled = false;
                        sec.intensity = 0f;
                        SyncSecondaryFixtureVisual(sec, false, Color.black, 0f);
                    }
                }
            }

            if (architecturalFillLights != null)
            {
                for (int i = 0; i < architecturalFillLights.Length; i++)
                {
                    Light fill = architecturalFillLights[i];
                    if (fill == null) continue;
                    string fn = fill.name.ToLowerInvariant();
                    if (fn.Contains("fill_spill_arcocozinha"))
                    {
                        bool kitchenOn = day == GameDay.Day1 || day == GameDay.Day2 || (day == GameDay.Day3 && corridorRestored);
                        fill.enabled = kitchenOn;
                        fill.color = new Color(0.93f, 0.95f, 0.93f);
                        float fillBase = day == GameDay.Day3 ? (day3SafeUnlocked ? 0.09f : 0.28f) : 0.22f;
                        fill.intensity = kitchenOn ? (fillBase * ambMult) : 0f;
                    }
                    else
                    {
                        fill.enabled = false;
                        fill.intensity = 0f;
                    }
                }
            }
        }

        public static void SyncBankerLampVisual(Light deskLight, bool isLit, float emissionWeight)
        {
            if (deskLight == null || deskLight.transform.parent == null) return;
            Transform desk = deskLight.transform.parent;

            Transform onState = desk.Find("Estado_Ligado_Luminaria");
            Transform offState = desk.Find("Estado_Desligado_Luminaria");
            if (onState != null) onState.gameObject.SetActive(isLit);
            if (offState != null) offState.gameObject.SetActive(!isLit);

            if (isLit && onState != null)
            {
                ApplyEmissiveBlock(onState.Find("Luminaria_Cupula"), true, new Color(0.16f, 0.56f, 0.24f), emissionWeight * 0.95f, new Color(0.14f, 0.44f, 0.20f, 1f));
                ApplyEmissiveBlock(onState.Find("Luminaria_Vidro_Verde"), true, new Color(1.00f, 0.90f, 0.68f), emissionWeight * 1.25f, new Color(0.92f, 0.88f, 0.76f, 1f));
                ApplyEmissiveBlock(onState.Find("Bulbo_Luminaria_Escritorio"), true, new Color(1.00f, 0.92f, 0.72f), Mathf.Max(1.35f, emissionWeight * 1.85f), new Color(1.00f, 0.95f, 0.84f, 1f));
            }
        }

        public static void SyncSecondaryFixtureVisual(Light sec, bool isLit, Color glowTint, float emissionWeight)
        {
            if (sec == null || sec.transform.parent == null) return;
            Transform parent = sec.transform.parent;

            Transform onState = parent.Find("Estado_Ligado");
            Transform offState = parent.Find("Estado_Desligado");
            if (onState != null) onState.gameObject.SetActive(isLit);
            if (offState != null) offState.gameObject.SetActive(!isLit);

            if (!isLit || onState == null) return;

            for (int i = 0; i < onState.childCount; i++)
            {
                Transform child = onState.GetChild(i);
                string cn = child.name;
                if (cn.Contains("Bulbo_") || cn.Contains("Tubo_"))
                {
                    ApplyEmissiveBlock(child, true, glowTint, Mathf.Max(1.40f, emissionWeight * 1.85f), new Color(1.00f, 0.95f, 0.86f, 1f));
                }
                else
                {
                    ApplyEmissiveBlock(child, true, glowTint, Mathf.Max(0.45f, emissionWeight * 0.95f), new Color(0.92f, 0.89f, 0.82f, 1f));
                }
            }
        }

        private sealed class LampVisualCache
        {
            public Transform fixtureRoot;
            public Transform onState;
            public Transform offState;
            public Light halo;
            public Renderer plafonRenderer;
            public Renderer cupulaRenderer;
            public Renderer bulboRenderer;
            public bool hasEmissive;
            public bool lastLit;
            public float lastWeight;
            public Color lastTint;
        }

        private static readonly System.Collections.Generic.Dictionary<EntityId, LampVisualCache> lampVisualCache =
            new System.Collections.Generic.Dictionary<EntityId, LampVisualCache>();
        private static MaterialPropertyBlock sharedEmissiveBlock;
        private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropEmissionColor = Shader.PropertyToID("_EmissionColor");

        private static MaterialPropertyBlock GetSharedEmissiveBlock()
        {
            if (sharedEmissiveBlock == null) sharedEmissiveBlock = new MaterialPropertyBlock();
            return sharedEmissiveBlock;
        }

        private static void ApplyEmissiveBlock(Transform target, bool isLit, Color glowTint, float emissionWeight, Color baseAlbedo)
        {
            if (target == null || !target.TryGetComponent<Renderer>(out Renderer r)) return;
            ApplyEmissiveBlock(r, isLit, glowTint, emissionWeight, baseAlbedo);
        }

        private static void ApplyEmissiveBlock(Renderer r, bool isLit, Color glowTint, float emissionWeight, Color baseAlbedo)
        {
            if (r == null) return;
            MaterialPropertyBlock block = GetSharedEmissiveBlock();
            block.Clear();
            r.GetPropertyBlock(block);
            Color surfaceColor = isLit ? Color.Lerp(baseAlbedo, glowTint, 0.45f) : baseAlbedo;
            Color emissive = isLit ? glowTint * Mathf.Max(0.20f, emissionWeight) : Color.black;
            block.SetColor(PropBaseColor, surfaceColor);
            block.SetColor(PropColor, surfaceColor);
            block.SetColor(PropEmissionColor, emissive);
            r.SetPropertyBlock(block);
        }

        public static void SyncFixtureEmissive(Light l, float normalizedIntensity)
        {
            if (l == null) return;
            bool lit = l.enabled && l.intensity > 0.02f && normalizedIntensity > 0.01f;
            float weight = lit ? Mathf.Clamp01(l.intensity * 0.65f) * Mathf.Clamp01(normalizedIntensity) : 0f;
            SyncLampFixtureVisual(l, lit, l.color, weight);
        }

        public static void SyncLampFixtureVisual(Light l, bool isLit, Color glowTint, float emissionWeight)
        {
            if (l == null || l.transform.parent == null) return;
            Transform fixtureRoot = l.transform.parent;

            if (!Application.isPlaying)
            {
                Transform editOn = fixtureRoot.Find("Estado_Ligado");
                Transform editOff = fixtureRoot.Find("Estado_Desligado");
                if (editOn != null && editOn.gameObject.activeSelf != isLit) editOn.gameObject.SetActive(isLit);
                if (editOff != null && editOff.gameObject.activeSelf != !isLit) editOff.gameObject.SetActive(!isLit);

                if (editOn != null)
                {
                    Light editHalo = editOn.GetComponentInChildren<Light>(true);
                    if (editHalo != null && editHalo != l)
                    {
                        editHalo.enabled = isLit && l.intensity > 0.02f;
                        editHalo.color = glowTint;
                        editHalo.intensity = editHalo.enabled ? Mathf.Clamp(l.intensity * 0.28f, 0.05f, 0.44f) : 0f;
                    }

                    if (isLit)
                    {
                        ApplyEmissiveBlock(editOn.Find("Plafon_Teto"), true, glowTint, Mathf.Max(0.85f, emissionWeight * 1.45f), new Color(0.95f, 0.92f, 0.85f, 1f));
                        ApplyEmissiveBlock(editOn.Find("Cupula_Fosca"), true, glowTint, Mathf.Max(0.50f, emissionWeight * 0.95f), new Color(0.90f, 0.88f, 0.82f, 1f));
                        ApplyEmissiveBlock(editOn.Find("Bulbo_Interno"), true, glowTint, Mathf.Max(1.40f, emissionWeight * 2.00f), new Color(1.00f, 0.96f, 0.88f, 1f));
                    }
                }
                return;
            }

            EntityId key = l.GetEntityId();
            if (!lampVisualCache.TryGetValue(key, out LampVisualCache cached) || cached.fixtureRoot != fixtureRoot)
            {
                Transform onStateFound = fixtureRoot.Find("Estado_Ligado");
                Transform offStateFound = fixtureRoot.Find("Estado_Desligado");
                cached = new LampVisualCache
                {
                    fixtureRoot = fixtureRoot,
                    onState = onStateFound,
                    offState = offStateFound,
                    halo = onStateFound != null ? onStateFound.GetComponentInChildren<Light>(true) : null,
                    plafonRenderer = onStateFound != null && onStateFound.Find("Plafon_Teto") != null ? onStateFound.Find("Plafon_Teto").GetComponent<Renderer>() : null,
                    cupulaRenderer = onStateFound != null && onStateFound.Find("Cupula_Fosca") != null ? onStateFound.Find("Cupula_Fosca").GetComponent<Renderer>() : null,
                    bulboRenderer = onStateFound != null && onStateFound.Find("Bulbo_Interno") != null ? onStateFound.Find("Bulbo_Interno").GetComponent<Renderer>() : null,
                    hasEmissive = false
                };
                lampVisualCache[key] = cached;
            }

            if (cached.onState != null && cached.onState.gameObject.activeSelf != isLit)
                cached.onState.gameObject.SetActive(isLit);
            if (cached.offState != null && cached.offState.gameObject.activeSelf != !isLit)
                cached.offState.gameObject.SetActive(!isLit);

            if (cached.onState != null)
            {
                Light halo = cached.halo;
                if (halo != null && halo != l)
                {
                    bool haloShouldEnable = isLit && l.intensity > 0.02f;
                    float haloTargetIntensity = haloShouldEnable ? Mathf.Clamp(l.intensity * 0.28f, 0.05f, 0.44f) : 0f;
                    if (halo.enabled != haloShouldEnable) halo.enabled = haloShouldEnable;
                    if (halo.color != glowTint) halo.color = glowTint;
                    if (Mathf.Abs(halo.intensity - haloTargetIntensity) > 0.005f) halo.intensity = haloTargetIntensity;
                }

                if (isLit)
                {
                    float dr = glowTint.r - cached.lastTint.r;
                    float dg = glowTint.g - cached.lastTint.g;
                    float db = glowTint.b - cached.lastTint.b;
                    float colorDeltaSq = dr * dr + dg * dg + db * db;
                    if (!cached.hasEmissive
                        || !cached.lastLit
                        || Mathf.Abs(emissionWeight - cached.lastWeight) > 0.015f
                        || colorDeltaSq > 0.0001f)
                    {
                        ApplyEmissiveBlock(cached.plafonRenderer, true, glowTint, Mathf.Max(0.85f, emissionWeight * 1.45f), new Color(0.95f, 0.92f, 0.85f, 1f));
                        ApplyEmissiveBlock(cached.cupulaRenderer, true, glowTint, Mathf.Max(0.50f, emissionWeight * 0.95f), new Color(0.90f, 0.88f, 0.82f, 1f));
                        ApplyEmissiveBlock(cached.bulboRenderer, true, glowTint, Mathf.Max(1.40f, emissionWeight * 2.00f), new Color(1.00f, 0.96f, 0.88f, 1f));
                        cached.hasEmissive = true;
                        cached.lastWeight = emissionWeight;
                        cached.lastTint = glowTint;
                    }
                    cached.lastLit = true;
                }
                else
                {
                    cached.lastLit = false;
                }
            }
        }

        private void SyncAppliancePowerIndicators(GameDay day, bool blackoutOverride)
        {
            if (casaBase == null) return;

            bool livingRoomPowered = !blackoutOverride && (day == GameDay.Day1 || day == GameDay.Day2);
            bool bedroomPowered = !blackoutOverride && day != GameDay.Day4_Prelude;

            Transform tvLed = casaBase.transform.Find("TV_Sala/LED_Standby_TV");
            ApplyEmissiveBlock(tvLed, livingRoomPowered, new Color(0.95f, 0.10f, 0.08f), 1.35f, new Color(0.14f, 0.05f, 0.05f, 1f));

            Transform vcrVisor = casaBase.transform.Find("Rack_TV/Visor_VCR");
            ApplyEmissiveBlock(vcrVisor, livingRoomPowered, new Color(0.18f, 0.55f, 0.22f), 1.15f, new Color(0.05f, 0.07f, 0.06f, 1f));

            Transform alarm = casaBase.transform.Find("Despertador_CriadoMudo");
            if (alarm != null && blackoutOverride && alarm.TryGetComponent<AnalogClockController>(out AnalogClockController alarmClock))
            {
                alarmClock.SetRunning(false);
            }
        }

        private void SyncClocksForDay(GameDay day)
        {
            if (casaBase == null) return;

            Transform alarmTr = casaBase.transform.Find("Despertador_CriadoMudo");
            if (alarmTr != null && alarmTr.TryGetComponent<AnalogClockController>(out AnalogClockController alarmClock))
            {
                switch (day)
                {
                    case GameDay.Day1:
                        alarmClock.SetClockTime(17, 40, 0f, running: true);
                        break;
                    case GameDay.Day2:
                        alarmClock.SetClockTime(10, 15, 0f, running: true);
                        break;
                    case GameDay.Day3:
                        alarmClock.SetClockTime(3, 12, 0f, running: true);
                        break;
                    default:
                        alarmClock.SetClockTime(23, 40, 0f, running: false);
                        break;
                }
            }

            Transform alarmSpotTr = casaBase.transform.Find("Despertador_Interagir");
            if (alarmSpotTr != null && alarmSpotTr.TryGetComponent<InspectableObject>(out InspectableObject alarmInsp))
            {
                switch (day)
                {
                    case GameDay.Day1:
                        alarmInsp.ConfigureTask(
                            id: "despertador_quarto_opc",
                            prompt: "Olhar o despertador",
                            subtitle: "Cinco e quarenta. Olho pra esse relógio toda hora esperando dar segunda.",
                            repeatSubtitle: "Cinco e quarenta.",
                            linear: false,
                            sound: FoleySoundId.MetalInspect
                        );
                        break;
                    case GameDay.Day2:
                        alarmInsp.ConfigureTask(
                            id: "despertador_quarto_opc",
                            prompt: "Olhar o despertador",
                            subtitle: "Passa das dez da manhã... O expediente no escritório já começou faz tempo.",
                            repeatSubtitle: "Dez e quinze. Nem liguei pra avisar.",
                            linear: false,
                            sound: FoleySoundId.MetalInspect
                        );
                        break;
                    case GameDay.Day3:
                        alarmInsp.ConfigureTask(
                            id: "despertador_quarto_opc",
                            prompt: "Olhar o despertador",
                            subtitle: "Três e doze... Não sei se é madrugada de terça ou se esse relógio parou faz tempo.",
                            repeatSubtitle: "Três e doze.",
                            linear: false,
                            sound: FoleySoundId.MetalInspect
                        );
                        break;
                }
                alarmInsp.SetInteractableState(day != GameDay.Day4_Prelude, resetUsedState: true);
            }

            Transform shelfClockTr = casaBase.transform.Find("Estante_Sala/Relogio_Estante_Parado");
            if (shelfClockTr != null && shelfClockTr.TryGetComponent<AnalogClockController>(out AnalogClockController mantleClock))
            {
                mantleClock.SetClockTime(23, 40, 18f, running: false);
            }
        }

        private void SyncFrontDoorForDay(GameDay day)
        {
            if (cachedFrontDoorInspectable == null && casaBase != null)
            {
                InspectableObject[] allBase = casaBase.GetComponentsInChildren<InspectableObject>(true);
                for (int i = 0; i < allBase.Length; i++)
                {
                    if (allBase[i] != null && allBase[i].gameObject.name == "Porta_Rua_Interagir")
                    {
                        cachedFrontDoorInspectable = allBase[i];
                        break;
                    }
                }
            }

            if (cachedFrontDoorInspectable == null)
                return;

            switch (day)
            {
                case GameDay.Day1:
                    cachedFrontDoorInspectable.ConfigureTask(
                        id: "porta_rua_opc",
                        prompt: "Olhar a porta da rua",
                        subtitle: "Duas voltas na chave e corrente passada. Nem um carro passa na rua lá fora... Não abro essa porta pra ninguém.",
                        repeatSubtitle: "Cheia de folheto sem data acumulado por baixo.",
                        linear: false,
                        sound: FoleySoundId.DoorLocked
                    );
                    break;
                case GameDay.Day2:
                    cachedFrontDoorInspectable.ConfigureTask(
                        id: "porta_rua_opc",
                        prompt: "Testar a porta da rua",
                        subtitle: "A corrente tá esticada e a tranca pesada demais. Mesmo se eu quisesse ir pro escritório, as pernas não obedecem.",
                        repeatSubtitle: "Duas voltas na chave. A rua parece longe demais.",
                        linear: false,
                        sound: FoleySoundId.DoorLocked
                    );
                    break;
                case GameDay.Day3:
                    if (day3SafeUnlocked)
                    {
                        cachedFrontDoorInspectable.ConfigureTask(
                            id: "porta_rua_opc",
                            prompt: "Olhar a porta da rua",
                            subtitle: "Essa chave de latão não é daqui. Eu sei muito bem qual porta ela abre.",
                            repeatSubtitle: "Não é por aqui que isso acaba.",
                            linear: false,
                            sound: FoleySoundId.DoorLocked
                        );
                    }
                    else
                    {
                        cachedFrontDoorInspectable.ConfigureTask(
                            id: "porta_rua_opc",
                            prompt: "Forçar a porta da rua",
                            subtitle: "Não adianta puxar a corrente no escuro... Tudo o que eu escondi tá do outro lado do corredor, não lá fora.",
                            repeatSubtitle: "A corrente nem cede. Não tem saída por aqui.",
                            linear: false,
                            sound: FoleySoundId.DoorLocked
                        );
                    }
                    break;
            }

            cachedFrontDoorInspectable.SetInteractableState(day != GameDay.Day4_Prelude, resetUsedState: true);
        }

        public void RefreshInteriorReflectionProbes()
        {
            if (interiorReflectionProbes == null) return;
            if (!Application.isPlaying)
            {
                for (int i = 0; i < interiorReflectionProbes.Length; i++)
                {
                    ReflectionProbe probe = interiorReflectionProbes[i];
                    if (probe != null && probe.gameObject.activeInHierarchy)
                    {
                        probe.RenderProbe();
                    }
                }
                return;
            }

            if (lastProbeRefreshFrame == Time.frameCount) return;
            lastProbeRefreshFrame = Time.frameCount;

            if (!isActiveAndEnabled) return;
            if (probeRefreshCoroutine != null)
            {
                StopCoroutine(probeRefreshCoroutine);
            }
            probeRefreshCoroutine = StartCoroutine(StaggeredReflectionProbeRefreshRoutine());
        }

        private IEnumerator StaggeredReflectionProbeRefreshRoutine()
        {
            if (interiorReflectionProbes == null)
            {
                probeRefreshCoroutine = null;
                yield break;
            }

            // Stagger 1 probe per frame (with IndividualFaces time-slicing) to prevent Metal Graphics Ring Buffer exhaustion
            for (int i = 0; i < interiorReflectionProbes.Length; i++)
            {
                ReflectionProbe probe = interiorReflectionProbes[i];
                if (probe != null && probe.gameObject.activeInHierarchy)
                {
                    probe.RenderProbe();
                    yield return null;
                }
            }
            probeRefreshCoroutine = null;
        }

        private void SetJumpscareBlackoutLighting()
        {
            ApplyJumpscareBlackoutLightingInternal(preserveFlashlight: false);
        }

        private void ApplyJumpscareBlackoutLightingInternal(bool preserveFlashlight)
        {
            if (!preserveFlashlight && flashlightController != null)
            {
                flashlightController.ForceBlackout(true);
            }
            SetDay3CircuitLightsState(false, 0f);
            if (houseInteriorLights != null)
            {
                for (int i = 0; i < houseInteriorLights.Length; i++)
                {
                    Light l = houseInteriorLights[i];
                    if (l == null) continue;
                    l.enabled = false;
                    l.intensity = 0f;
                    SyncLampFixtureVisual(l, false, Color.black, 0f);
                }
            }

            if (secondaryMotivatedLights != null)
            {
                for (int i = 0; i < secondaryMotivatedLights.Length; i++)
                {
                    Light sec = secondaryMotivatedLights[i];
                    if (sec != null)
                    {
                        sec.enabled = false;
                        sec.intensity = 0f;
                        SyncSecondaryFixtureVisual(sec, false, Color.black, 0f);
                    }
                }
            }

            if (architecturalFillLights != null)
            {
                for (int i = 0; i < architecturalFillLights.Length; i++)
                {
                    if (architecturalFillLights[i] != null)
                    {
                        architecturalFillLights[i].enabled = false;
                        architecturalFillLights[i].intensity = 0f;
                    }
                }
            }

            SyncAppliancePowerIndicators(currentDay, blackoutOverride: true);
            RestoreJumpscareBlackoutAmbient();
        }

        private void SetPitchBlackTrapDarkness()
        {
            RenderSettings.ambientSkyColor = Color.black;
            RenderSettings.ambientEquatorColor = Color.black;
            RenderSettings.ambientGroundColor = Color.black;
            RenderSettings.reflectionIntensity = 0f;
        }

        private void RestoreJumpscareBlackoutAmbient()
        {
            RenderSettings.reflectionIntensity = brightShadowsEnabled ? 0.52f : 0.28f;
            if (brightShadowsEnabled)
            {
                RenderSettings.ambientSkyColor = new Color(0.056f, 0.056f, 0.058f);
                RenderSettings.ambientEquatorColor = new Color(0.076f, 0.076f, 0.078f);
                RenderSettings.ambientGroundColor = new Color(0.042f, 0.042f, 0.044f);
            }
            else
            {
                RenderSettings.ambientSkyColor = new Color(0.016f, 0.016f, 0.017f);
                RenderSettings.ambientEquatorColor = new Color(0.024f, 0.024f, 0.025f);
                RenderSettings.ambientGroundColor = new Color(0.010f, 0.010f, 0.010f);
            }
        }

        private void HandleCorridorDoorUnlockedDay3()
        {
            if (currentDay == GameDay.Day3 && !isTransitioning && !day3DoorTrapWaitingForEntry)
            {
                if (objectiveHUD != null)
                {
                    objectiveHUD.MarkCompleted("destrancar_porta_dia3", silentCompletion: true);
                    objectiveHUD.SetObjectiveVisible("entrar_quarto_dia3", true);
                }
                TriggerMargaretAttackSequence();
            }
        }

        public void TriggerMargaretAttackSequence()
        {
            if (isTransitioning || day3DoorTrapWaitingForEntry) return;
            if (day3DoorTrapCoroutine != null) StopCoroutine(day3DoorTrapCoroutine);
            day3DoorTrapCoroutine = StartCoroutine(RunMargaretDoorTrapSequenceRoutine());
        }

        public static Vector3 ComputeSafeMargaretClimaxPosition(Vector3 playerFloorPos, Vector3 camForwardXZ)
        {
            const float minX = 3.08f;
            const float maxX = 4.78f;
            const float minZ = 2.15f;
            // Cama_Filha_1 footboard corner reaches (X >= 4.466, Z >= 4.776); keep >= 0.29m radius clearance
            const float maxZ = 4.48f;

            Vector3 fwd = new Vector3(camForwardXZ.x, 0f, camForwardXZ.z);
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.right;
            fwd.Normalize();

            Vector3 pFloor = new Vector3(playerFloorPos.x, 0f, playerFloorPos.z);

            // Preferred: directly behind the player at ~1.28m inside the obstacle-free rug/interior zone
            Vector3 behindRaw = pFloor - fwd * 1.28f;
            Vector3 behindClamped = new Vector3(
                Mathf.Clamp(behindRaw.x, minX, maxX),
                0f,
                Mathf.Clamp(behindRaw.z, minZ, maxZ));

            float behindDist = Vector2.Distance(new Vector2(pFloor.x, pFloor.z), new Vector2(behindClamped.x, behindClamped.z));
            if (behindDist >= 1.05f)
            {
                return behindClamped;
            }

            // Fallback when player is pressed near a wall/doorway: pick the best open room candidate
            // at least 1.08m away and furthest out of the player's current forward view cone.
            Vector3[] candidates =
            {
                new Vector3(4.52f, 0f, 3.29f),
                new Vector3(4.52f, 0f, 4.35f),
                new Vector3(4.42f, 0f, 2.38f),
                new Vector3(3.26f, 0f, 2.48f),
                new Vector3(3.26f, 0f, 4.38f)
            };

            Vector3 bestPos = candidates[0];
            float bestScore = float.PositiveInfinity;
            for (int i = 0; i < candidates.Length; i++)
            {
                Vector3 c = candidates[i];
                Vector3 toC = c - pFloor;
                toC.y = 0f;
                float dist = toC.magnitude;
                if (dist < 1.05f) continue;

                float dot = Vector3.Dot(fwd, toC / dist);
                float distPenalty = Mathf.Abs(dist - 1.38f) * 0.45f;
                float score = dot + distPenalty;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestPos = c;
                }
            }

            return new Vector3(
                Mathf.Clamp(bestPos.x, minX, maxX),
                0f,
                Mathf.Clamp(bestPos.z, minZ, maxZ));
        }

        private IEnumerator RunMargaretDoorTrapSequenceRoutine()
        {
            // =================================================================================
            // PHASE 1: ENTRADA TENSA NO QUARTO DAS FILHAS
            // Door unlocks and opens quietly into the dark room so Cian's flashlight beam cuts
            // across the empty child beds and boarded window in near-total silence.
            // =================================================================================
            day3DoorTrapWaitingForEntry = true;
            if (pendingDay3NewspaperCrash)
            {
                pendingDay3NewspaperCrash = false;
                day3BaseStimulantActive = false;
            }
            StopAndResetStimulantEffectsImmediate(clearPlayerDrugState: !day3BaseStimulantActive);
            if (horrorDirector != null)
            {
                horrorDirector.NotifyDay3DoorTrapOpened();
            }
            DisarmUntriggeredWatchersForDay(GameDay.Day3);

            if (day3ClueReactionCoroutine != null)
            {
                StopCoroutine(day3ClueReactionCoroutine);
                day3ClueReactionCoroutine = null;
            }
            if (day3SiegeCoroutine != null)
            {
                StopCoroutine(day3SiegeCoroutine);
                day3SiegeCoroutine = null;
                if (officeDoor != null && officeDoor.IsOpen)
                {
                    officeDoor.SetDoorOpen(false, playSound: false);
                }
                ApplyAtmosphericLightingForDay(GameDay.Day3);
            }

            // Faint penumbra (~0.16x) so the player's flashlight dominates the empty twin beds and boarded window
            EnsureFlashlightController();
            bool hasEquippedFlashlight = flashlightController != null && flashlightController.HasFlashlight;
            PrepareDaughtersRoomForDay3Trap(lampIntensityScale: hasEquippedFlashlight ? 0.16f : 0.65f);
            if (margaretJumpscareObject != null)
            {
                margaretJumpscareObject.SetActive(false);
            }
            SetMargaretChiaroscuroLightsActive(false, 0f);

            if (corridorLockedDoor != null)
            {
                corridorLockedDoor.SetInteractable(false);
                if (!corridorLockedDoor.IsOpen)
                {
                    corridorLockedDoor.OpenToPartialAngle(88f, 26f, playSound: false);
                }
            }

            if (audioManager != null)
            {
                audioManager.SetDreadProximityWeight(0.05f);
                audioManager.PulseSilenceDread(4.0f, 0.32f);
                audioManager.PlayFoley2D(FoleySoundId.DreadSubPulse, 0.42f, 0.72f);
            }

            Vector3 doorCenter = corridorLockedDoor != null
                ? corridorLockedDoor.GetClosedDoorCenterWorld()
                : new Vector3(2.60f, 0f, 4.60f);
            doorCenter.y = 0f;

            // =================================================================================
            // PHASE 2: PLAYER AGENCY — Step ~2 steps inside Quarto_Filhas with flashlight active.
            // =================================================================================
            bool thresholdCreakPlayed = false;
            bool hesitationThoughtPlayed = false;
            bool roomBeckonPlayed = false;
            bool retreatDeterrentPlayed = false;
            float totalOpenElapsed = 0f;
            float deepInsideTimer = 0f;
            float thresholdHoverTimer = 0f;

            bool startedNearDoor = player == null || Vector2.Distance(
                new Vector2(player.transform.position.x, player.transform.position.z),
                new Vector2(doorCenter.x, doorCenter.z)) <= 4.5f;

            while (startedNearDoor && currentDay == GameDay.Day3 && !isTransitioning && player != null)
            {
                totalOpenElapsed += Time.deltaTime;
                Vector3 p = player.transform.position;

                if (!hesitationThoughtPlayed && !thresholdCreakPlayed && totalOpenElapsed >= 5.0f)
                {
                    hesitationThoughtPlayed = true;
                    if (InteractionSystem.Instance != null)
                    {
                        InteractionSystem.Instance.ShowSubtitle("Abriu... Tá tudo quieto lá dentro.", 3.6f);
                    }
                }

                if (!roomBeckonPlayed && !thresholdCreakPlayed && totalOpenElapsed >= 9.4f)
                {
                    roomBeckonPlayed = true;
                    if (audioManager != null)
                    {
                        audioManager.PlayFoley3DPanned(FoleySoundId.EerieLullabyHum, new Vector3(4.85f, 0.85f, 3.29f), 0.42f, 0.9f, 12f, 0.92f);
                        audioManager.PlayFoley3DPanned(FoleySoundId.DistantFloorCreak, new Vector3(4.24f, 0.04f, 3.29f), 0.48f, 0.8f, 10f, 0.84f);
                    }
                }

                if (!retreatDeterrentPlayed && p.z < 3.10f)
                {
                    retreatDeterrentPlayed = true;
                    float lampMult = GetUserLampMultiplier();
                    if (centerCorridorLight != null)
                    {
                        centerCorridorLight.enabled = true;
                        centerCorridorLight.intensity = 0.14f * lampMult;
                        SyncLampFixtureVisual(centerCorridorLight, true, centerCorridorLight.color, 0.18f);
                    }
                    if (westCorridorLight != null)
                    {
                        westCorridorLight.enabled = true;
                        westCorridorLight.intensity = 0.12f * lampMult;
                        SyncLampFixtureVisual(westCorridorLight, true, westCorridorLight.color, 0.16f);
                    }
                    if (audioManager != null)
                    {
                        audioManager.PlayFoley3DPanned(FoleySoundId.LightSwitchClick, new Vector3(1.78f, 1.4f, 2.0f), 0.88f, 1.0f, 14f, 0.90f);
                        audioManager.PlayBehindPlayer(FoleySoundId.PhantomStepBehind, player.transform, 0.95f, 0.22f, 0.95f);
                    }
                    if (InteractionSystem.Instance != null)
                    {
                        InteractionSystem.Instance.ShowSubtitle("Não dá mais pra voltar pro sofá.", 3.8f);
                    }
                }

                // Filter Z to Quarto_Filhas (Z: 0.54..6.04) so walking into the Office (Z <= 0.38, X >= 2.74)
                // can never trigger or teleport the player into the Day 3 climax trap.
                bool inDaughtersRoomZ = p.z >= 0.60f && p.z <= 6.10f;

                if (!thresholdCreakPlayed && inDaughtersRoomZ && p.x >= 2.74f)
                {
                    thresholdCreakPlayed = true;
                    if (objectiveHUD != null)
                    {
                        objectiveHUD.MarkCompleted("entrar_quarto_dia3", silentCompletion: true);
                        objectiveHUD.SetHUDVisible(false);
                    }
                    if (audioManager != null)
                    {
                        audioManager.PlayFoley3DPanned(FoleySoundId.DistantFloorCreak, new Vector3(2.88f, 0.04f, 4.52f), 0.50f, 0.8f, 10f, 0.86f);
                    }
                    if (eastCorridorLight != null && eastCorridorLight.enabled)
                    {
                        eastCorridorBaseIntensity = 0.22f * GetUserLampMultiplier();
                        eastCorridorLight.intensity = eastCorridorBaseIntensity;
                    }
                }

                bool deepInsideRoom = inDaughtersRoomZ && (p.x >= 3.20f);
                bool pastThreshold = inDaughtersRoomZ && (p.x >= 2.84f);

                if (deepInsideRoom)
                {
                    deepInsideTimer += Time.deltaTime;
                }
                else
                {
                    deepInsideTimer = Mathf.MoveTowards(deepInsideTimer, 0f, Time.deltaTime * 0.5f);
                }

                if (pastThreshold)
                {
                    thresholdHoverTimer += Time.deltaTime;
                }

                if (deepInsideTimer >= 0.45f || thresholdHoverTimer >= 2.35f)
                {
                    break;
                }

                yield return null;
            }

            if (currentDay != GameDay.Day3)
            {
                day3DoorTrapWaitingForEntry = false;
                day3DoorTrapCoroutine = null;
                yield break;
            }

            // =================================================================================
            // PHASE 3: ARMADILHA FÍSICA — Door slams & locks behind Cian; flashlight flickers
            // twice and dies for 0.8s while the player RETAINS free mouse-look agency!
            // =================================================================================
            day3DoorTrapWaitingForEntry = false;
            isTransitioning = true;
            StopTransientDayCoroutines(keepDoorTrapRoutine: true);
            StopAndResetStimulantEffectsImmediate(clearPlayerDrugState: true);

            if (objectiveHUD != null)
            {
                objectiveHUD.MarkCompleted("entrar_quarto_dia3", silentCompletion: true);
                objectiveHUD.SetHUDVisible(false);
            }

            if (GameSettingsUI.Instance != null)
            {
                GameSettingsUI.Instance.CloseSettingsMenu(restorePlayerState: false);
            }
            ObjectiveHUD.IsBlockingModalOpen = false;
            SafeBoxPuzzle.CloseAnyOpenKeypad();

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.CloseDocument(restorePlayerControls: false);
                InteractionSystem.Instance.ClearOverridePrompt();
                InteractionSystem.Instance.ClearSubtitleImmediate();
            }

            // Hold position inside the room so Cian cannot clip through the slamming door,
            // while KEEPING free mouse-look (lookEnabled: true) so player agency stays intact!
            // Disable CharacterController BEFORE writing transform.position so PhysX CCT never snaps/drifts trapStandPos.
            Vector3 trapStandPos = new Vector3(3.56f, 0.02f, 4.30f);
            if (player != null)
            {
                player.SetCharacterControllerEnabledForCutscene(false);
                if (!startedNearDoor)
                {
                    player.transform.position = trapStandPos;
                }
                Vector3 curPos = player.transform.position;
                trapStandPos = new Vector3(
                    Mathf.Clamp(curPos.x, 3.36f, 4.32f),
                    curPos.y,
                    Mathf.Clamp(curPos.z, 2.70f, 4.54f));
                player.transform.position = trapStandPos;
                Physics.SyncTransforms();
                player.SetControlsEnabled(moveEnabled: false, lookEnabled: true, lockCursor: true);
            }

            // Heavy physical door slam + deadbolt lock behind Cian
            if (corridorLockedDoor != null)
            {
                corridorLockedDoor.SlamShutAndLock(760f, playSound: false);
            }

            Vector3 slamSoundPos = doorCenter + Vector3.up * 1.15f;
            if (audioManager != null)
            {
                audioManager.PlayFoley3DPanned(FoleySoundId.DoorClose, slamSoundPos, 0.76f, 1.0f, 14f, 1.08f);
            }
            yield return new WaitForSeconds(0.10f);

            ApplyJumpscareBlackoutLightingInternal(preserveFlashlight: true);
            PrepareDaughtersRoomForDay3Trap(lampIntensityScale: 0f);
            if (player != null)
            {
                player.TriggerCameraShake(2.40f, 0.26f);
            }
            if (audioManager != null)
            {
                audioManager.PlayFoley3DPanned(FoleySoundId.HeavyDoorSlamTrap, slamSoundPos, 1.0f, 1.4f, 18f, 0.92f);
                audioManager.PlayFoley3DPanned(FoleySoundId.DoorLocked, slamSoundPos, 0.96f, 1.0f, 14f, 0.85f);
                audioManager.PulseSilenceDread(2.40f, 0.40f);
            }

            // Flashlight sputters TWICE then cuts out completely for 0.80s into pitch darkness
            SetPitchBlackTrapDarkness();
            if (flashlightController != null && flashlightController.HasFlashlight)
            {
                yield return flashlightController.RunClimaxDoubleFlickerAndBlackoutRoutine(blackoutDuration: 0.80f);
            }
            else
            {
                yield return new WaitForSeconds(0.80f);
            }

            // =================================================================================
            // PHASE 4: SUSPENSE + VIRADA (Agência do jogador até virar ou 1.1s de retenção tensa)
            // =================================================================================
            Vector3 playerFloor = new Vector3(trapStandPos.x, 0f, trapStandPos.z);
            Vector3 camFwd = (player != null && player.CameraTransform != null)
                ? player.CameraTransform.forward
                : Vector3.right;
            Vector3 camRight = (player != null && player.CameraTransform != null)
                ? player.CameraTransform.right
                : Vector3.back;

            Vector3 margaretStandPos = ComputeSafeMargaretClimaxPosition(playerFloor, camFwd);
            Vector3 roomInteriorDir = margaretStandPos - playerFloor;
            roomInteriorDir.y = 0f;
            roomInteriorDir = roomInteriorDir.sqrMagnitude > 0.001f ? roomInteriorDir.normalized : Vector3.right;

            EnsureMargaretChiaroscuroLights();
            HumanoidSpasmAnimator margaretAnim = null;
            if (margaretJumpscareObject != null)
            {
                margaretJumpscareObject.transform.position = margaretStandPos;
                margaretJumpscareObject.transform.LookAt(playerFloor);
                margaretAnim = margaretJumpscareObject.GetComponent<HumanoidSpasmAnimator>();
                if (margaretAnim == null) margaretAnim = margaretJumpscareObject.AddComponent<HumanoidSpasmAnimator>();
                margaretJumpscareObject.SetActive(true);
                margaretAnim.SetMode(SpasmAnimMode.DeadStillBrokenNeck, immediate: true);
            }

            // Revive flashlight in a weak, nervous trembling beam + soft chiaroscuro rim
            RestoreJumpscareBlackoutAmbient();
            bool reviveFlashlight = flashlightController != null && flashlightController.HasFlashlight;
            if (reviveFlashlight)
            {
                flashlightController.StartClimaxWeakTremble(minMult: 0.26f, maxMult: 0.56f);
            }
            else
            {
                PrepareDaughtersRoomForDay3Trap(lampIntensityScale: 0.24f);
            }
            SetMargaretChiaroscuroLightsActive(true, 0.72f);

            // Dry close breathing & floorboard creak right from Margaret's direction
            if (audioManager != null)
            {
                Vector3 camRightXZ = new Vector3(camRight.x, 0f, camRight.z).normalized;
                float pan = Vector3.Dot(camRightXZ, roomInteriorDir);
                if (Mathf.Abs(pan) < 0.25f) pan = -0.52f;
                audioManager.PlayBinauralCue(FoleySoundId.NeckBreath, Mathf.Clamp(pan, -0.82f, 0.82f), 1.0f, 0.84f);
                audioManager.PlayFoley3DPanned(FoleySoundId.DistantFloorCreak, margaretStandPos + Vector3.up * 0.06f, 0.56f, 0.7f, 10f, 0.84f);
            }

            // Player retains full mouse-look agency: reveal triggers the instant they turn toward Margaret,
            // or automatically after 1.10s of tense suspension.
            Vector3 margaretEyeFocus = margaretStandPos + Vector3.up * 1.54f;
            bool playerTurnedToSeeMargaret = false;
            float tenseHoldElapsed = 0f;
            const float maxTenseHoldDuration = 1.10f;
            while (tenseHoldElapsed < maxTenseHoldDuration && player != null)
            {
                tenseHoldElapsed += Time.deltaTime;
                Transform camTr = player.CameraTransform;
                if (camTr != null && tenseHoldElapsed >= 0.14f)
                {
                    Vector3 toFace = margaretEyeFocus - camTr.position;
                    if (toFace.sqrMagnitude > 0.0001f && Vector3.Dot(camTr.forward, toFace.normalized) >= 0.70f)
                    {
                        playerTurnedToSeeMargaret = true;
                        break;
                    }
                }
                yield return null;
            }

            // =================================================================================
            // PHASE 5: INVESTIDA ESQUELÉTICA VISCERAL DE MARGARET -> CORTE BRUSCO + TINNITUS -> DIA 4
            // =================================================================================
            if (player != null)
            {
                player.SetControlsEnabled(moveEnabled: false, lookEnabled: false, lockCursor: true);
                player.SetCharacterControllerEnabledForCutscene(false);
                float snapLookDur = playerTurnedToSeeMargaret ? 0.09f : 0.14f;
                activeCutsceneLookCoroutine = StartCoroutine(player.SmoothLookAtRoutine(margaretEyeFocus, snapLookDur, targetFovDelta: -10.5f));
                yield return activeCutsceneLookCoroutine;
                activeCutsceneLookCoroutine = null;
            }

            // Full chiaroscuro directional rim (1.35f) + face graze (0.035f) carving her sculpted face out of the dark
            PrepareDaughtersRoomForDay3Trap(lampIntensityScale: 0.22f);
            SetMargaretChiaroscuroLightsActive(true, 1.0f);

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ShowSubtitle("Margaret...", 1.25f);
            }

            if (audioManager != null)
            {
                audioManager.PlayFoley2D(FoleySoundId.VisceralSubShock, 1.0f, 0.94f);
                audioManager.PlayFoley2D(FoleySoundId.MargaretStinger, 0.92f, 0.90f);
            }

            if (margaretJumpscareAudio != null && margaretJumpscareAudio.clip != null)
            {
                margaretJumpscareAudio.Play();
            }

            // Brief petrifying stillness in chiaroscuro (0.30s) before the fluid skeletal attack lunge
            if (player != null)
            {
                player.TriggerCameraShake(1.85f, 0.24f);
            }
            yield return new WaitForSeconds(0.30f);

            // Explosive fluid skeletal attack lunge (Anim_Char_VisceralAttackLunge.anim) into the lens
            if (margaretAnim != null)
            {
                margaretAnim.SetMode(SpasmAnimMode.VisceralImpactSnap, immediate: true);
            }

            if (SanityEffects.Instance != null)
            {
                SanityEffects.Instance.TriggerHallucinationSpike(1.0f, 1.15f, allowSubliminalFlash: false);
            }

            if (player != null)
            {
                player.TriggerCameraShake(6.5f, 0.34f);
                player.SetPostureAndFovOffset(-4.8f, 6.8f, -18.5f, snapFov: true);
            }

            if (HorrorScreenOverlay.Instance != null)
            {
                HorrorScreenOverlay.Instance.TriggerHighContrastStrobeFrame(0.085f);
            }

            if (margaretJumpscareObject != null)
            {
                Vector3 snapEndPos = playerFloor + roomInteriorDir * 0.45f;
                snapEndPos.y = 0f;
                float snapElapsed = 0f;
                const float snapDuration = 0.22f;
                while (snapElapsed < snapDuration)
                {
                    snapElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(snapElapsed / snapDuration);
                    float lungeEase = t * t * (1.25f - 0.25f * t);
                    margaretJumpscareObject.transform.position = Vector3.Lerp(margaretStandPos, snapEndPos, lungeEase);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(0.22f);
            }

            // Abrupt, heavy hard cut to black at the exact peak of impact -> unilateral tinnitus ring -> Day 4
            if (blackFadeGroup != null)
            {
                blackFadeGroup.alpha = 1f;
            }
            if (flashlightController != null)
            {
                flashlightController.StopClimaxEffectsAndBlackout();
            }
            if (margaretJumpscareAudio != null)
            {
                margaretJumpscareAudio.Stop();
            }
            if (audioManager != null)
            {
                audioManager.Stop2DFoleyImmediate(stop3DPool: true);
                audioManager.ResetTransientAudioForTransition();
                audioManager.PulseSilenceDread(2.20f, 0.35f);
                audioManager.PlayFoley2D(FoleySoundId.HeavyDoorSlamTrap, 0.78f, 0.66f);
                audioManager.PlayBinauralCue(FoleySoundId.TinnitusRing, -0.78f, 0.92f, 0.96f);
            }

            StopAndResetStimulantEffectsImmediate(clearPlayerDrugState: true);
            if (player != null)
            {
                player.ResetTransientCameraEffects();
                player.SetHorrorCameraInfluence(0f, 0f);
            }
            if (HorrorScreenOverlay.Instance != null)
            {
                HorrorScreenOverlay.Instance.ClearImmediate();
            }
            if (sanityEffects != null)
            {
                sanityEffects.ClearSpikesImmediate();
            }
            SetMargaretChiaroscuroLightsActive(false, 0f);
            PrepareDaughtersRoomForDay3Trap(lampIntensityScale: 0f);
            if (margaretAnim != null)
            {
                margaretAnim.ResetPose();
                margaretAnim.SetMode(SpasmAnimMode.Idle);
            }
            if (margaretJumpscareObject != null) margaretJumpscareObject.SetActive(false);

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ClearSubtitleImmediate();
            }

            yield return new WaitForSeconds(0.95f);

            day3DoorTrapCoroutine = null;
            activeTransitionCoroutine = StartCoroutine(TransitionRoutine(GameDay.Day4_Prelude, instantCutToBlack: true));
        }

        private void EnsureMargaretChiaroscuroLights()
        {
            if (margaretJumpscareObject == null) return;

            if (margaretFlashLight == null)
            {
                Transform existingRim = margaretJumpscareObject.transform.Find("Luz_Estrobo_Margaret");
                if (existingRim == null) existingRim = margaretJumpscareObject.transform.Find("Luz_Contraluz_Margaret");
                if (existingRim != null)
                {
                    margaretFlashLight = existingRim.GetComponent<Light>();
                }
                else
                {
                    GameObject rimGo = new GameObject("Luz_Contraluz_Margaret");
                    rimGo.transform.SetParent(margaretJumpscareObject.transform, false);
                    margaretFlashLight = rimGo.AddComponent<Light>();
                }
            }

            // Position behind her head/shoulders (-Z local) so her front remains a hollowed-out silhouette in contraluz
            margaretFlashLight.transform.localPosition = new Vector3(0f, 1.58f, -0.44f);
            margaretFlashLight.type = LightType.Point;
            margaretFlashLight.color = new Color(0.50f, 0.60f, 0.76f);
            margaretFlashLight.range = 2.0f;
            margaretFlashLight.shadows = LightShadows.None;
            margaretFlashLight.enabled = false;

            if (margaretFaceGrazeLight == null)
            {
                Transform existingGraze = margaretJumpscareObject.transform.Find("Luz_Penumbra_Rosto_Margaret");
                if (existingGraze != null)
                {
                    margaretFaceGrazeLight = existingGraze.GetComponent<Light>();
                }
                else
                {
                    GameObject grazeGo = new GameObject("Luz_Penumbra_Rosto_Margaret");
                    grazeGo.transform.SetParent(margaretJumpscareObject.transform, false);
                    margaretFaceGrazeLight = grazeGo.AddComponent<Light>();
                }
            }

            // Subtle steep grazing light below the chin to catch only the hollow eye sockets and pinprick reflections
            margaretFaceGrazeLight.transform.localPosition = new Vector3(0f, 1.18f, 0.24f);
            margaretFaceGrazeLight.type = LightType.Point;
            margaretFaceGrazeLight.color = new Color(0.58f, 0.63f, 0.70f);
            margaretFaceGrazeLight.range = 0.78f;
            margaretFaceGrazeLight.shadows = LightShadows.None;
            margaretFaceGrazeLight.enabled = false;
        }

        private void SetMargaretChiaroscuroLightsActive(bool active, float intensityScale)
        {
            if (margaretFlashLight != null)
            {
                margaretFlashLight.enabled = active;
                margaretFlashLight.intensity = active ? 1.35f * intensityScale : 0f;
            }
            if (margaretFaceGrazeLight != null)
            {
                margaretFaceGrazeLight.enabled = active;
                margaretFaceGrazeLight.intensity = active ? 0.035f * intensityScale : 0f;
            }
        }

        private IEnumerator RunDay4PreludeEndingRoutine()
        {
            float firstMonologueElapsed = 0f;
            while (firstMonologueElapsed < 11.5f)
            {
                if (firstMonologueElapsed >= 7.5f &&
                    (InteractionSystem.Instance == null || !InteractionSystem.Instance.IsShowingSubtitle))
                {
                    break;
                }
                firstMonologueElapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.60f);

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ShowSubtitle(
                    "Eu girei a chave pelo lado de fora, tomei dois comprimidos pra não ouvir mais nada... e aumentei o volume da TV.",
                    8.5f
                );
            }

            float confessionMinWait = Mathf.Max(9.0f, day4PreludeDuration * 0.45f);
            float confessionElapsed = 0f;
            while (confessionElapsed < 12.5f)
            {
                if (confessionElapsed >= confessionMinWait &&
                    (InteractionSystem.Instance == null || !InteractionSystem.Instance.IsShowingSubtitle))
                {
                    break;
                }
                confessionElapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.55f);

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.ShowSubtitle(
                    "Não sei mais que ano é hoje... nem se algum dia eu realmente saí dessa sala.",
                    7.2f
                );
            }

            float ambigElapsed = 0f;
            while (ambigElapsed < 8.0f)
            {
                if (ambigElapsed >= 5.5f &&
                    (InteractionSystem.Instance == null || !InteractionSystem.Instance.IsShowingSubtitle))
                {
                    break;
                }
                ambigElapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.80f);

            float fadeDuration = 3.2f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                blackFadeGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            blackFadeGroup.alpha = 1f;

            if (player != null)
            {
                player.SetControlsEnabled(moveEnabled: false, lookEnabled: false, lockCursor: true);
            }

            yield return new WaitForSeconds(0.90f);

            dayCardTitleText.fontSize = 54;
            dayCardTitleText.text = "CANAL 420";
            dayCardSubText.text = "Fim da demonstração. Obrigado por jogar.";

            float endInElapsed = 0f;
            const float endInDuration = 1.45f;
            while (endInElapsed < endInDuration)
            {
                endInElapsed += Time.deltaTime;
                float u = Mathf.Clamp01(endInElapsed / endInDuration);
                titleCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.82f));
                subCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.18f) / 0.82f));
                yield return null;
            }
            titleCanvasGroup.alpha = 1f;
            subCanvasGroup.alpha = 1f;

            if (endDemoHintObj != null) endDemoHintObj.SetActive(true);
            demoEnded = true;
            day4EndingCoroutine = null;
        }

        private void GetDayCardTexts(GameDay day, out string title, out string sub)
        {
            dayCardTitleText.fontSize = 48;
            switch (day)
            {
                case GameDay.Day1:
                    title = "DIA 1";
                    sub = "Domingo, 29 de outubro de 1995";
                    break;
                case GameDay.Day2:
                    title = "DIA 2";
                    sub = "Segunda-feira";
                    break;
                case GameDay.Day3:
                    title = "DIA 3";
                    sub = "Terça-feira, madrugada";
                    break;
                default:
                    title = string.Empty;
                    sub = string.Empty;
                    break;
            }
        }

        private void ShowWakeupMonologue(GameDay day)
        {
            if (InteractionSystem.Instance == null) return;

            switch (day)
            {
                case GameDay.Day1:
                    InteractionSystem.Instance.ShowSubtitle(
                        "Já escureceu. Apaguei vestido de novo... Preciso jogar uma água no rosto.",
                        6.0f
                    );
                    break;
                case GameDay.Day2:
                    InteractionSystem.Instance.ShowSubtitle(
                        "Que gosto ruim na boca... Quanto daquele remédio eu tomei?",
                        6.2f
                    );
                    break;
                case GameDay.Day3:
                    InteractionSystem.Instance.ShowSubtitle(
                        "Apaguei aqui no chão... Caiu a força da casa inteira. Que apagão geral... A lanterna tá na primeira gaveta da cozinha.",
                        7.4f
                    );
                    break;
                case GameDay.Day4_Prelude:
                    InteractionSystem.Instance.ShowSubtitle(
                        "O cobertor pequeno continua dobrado no pé da cama, do jeito que ela deixou.",
                        6.4f
                    );
                    break;
            }
        }

        private void BuildTransitionOverlayUI()
        {
            if (transitionCanvasObject != null)
                return;

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            transitionCanvasObject = new GameObject("HUD_TransicaoDias_Auto");
            Canvas canvas = transitionCanvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = transitionCanvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelObj = new GameObject("BlackScreen_Panel");
            panelObj.transform.SetParent(transitionCanvasObject.transform, false);
            Image blackImg = panelObj.AddComponent<Image>();
            blackImg.color = Color.black;
            blackImg.raycastTarget = false;

            blackFadeGroup = panelObj.AddComponent<CanvasGroup>();
            blackFadeGroup.alpha = showOpeningTitleOnStart ? 1f : 0f;
            blackFadeGroup.blocksRaycasts = false;

            RectTransform panelRect = blackImg.rectTransform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            GameObject titleObj = new GameObject("DayTitle_Text");
            titleObj.transform.SetParent(panelObj.transform, false);
            titleCanvasGroup = titleObj.AddComponent<CanvasGroup>();
            titleCanvasGroup.alpha = 0f;
            dayCardTitleText = titleObj.AddComponent<Text>();
            dayCardTitleText.font = builtinFont;
            dayCardTitleText.fontSize = 48;
            dayCardTitleText.fontStyle = FontStyle.Bold;
            dayCardTitleText.alignment = TextAnchor.MiddleCenter;
            dayCardTitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            dayCardTitleText.verticalOverflow = VerticalWrapMode.Overflow;
            dayCardTitleText.color = new Color(0.94f, 0.94f, 0.93f, 1f);
            dayCardTitleText.raycastTarget = false;

            RectTransform titleRect = dayCardTitleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 24f);
            titleRect.sizeDelta = new Vector2(1200f, 76f);

            GameObject subObj = new GameObject("DaySubtitle_Text");
            subObj.transform.SetParent(panelObj.transform, false);
            subCanvasGroup = subObj.AddComponent<CanvasGroup>();
            subCanvasGroup.alpha = 0f;
            dayCardSubText = subObj.AddComponent<Text>();
            dayCardSubText.font = builtinFont;
            dayCardSubText.fontSize = 20;
            dayCardSubText.fontStyle = FontStyle.Normal;
            dayCardSubText.alignment = TextAnchor.MiddleCenter;
            dayCardSubText.horizontalOverflow = HorizontalWrapMode.Wrap;
            dayCardSubText.verticalOverflow = VerticalWrapMode.Overflow;
            dayCardSubText.color = new Color(0.72f, 0.72f, 0.71f, 0.96f);
            dayCardSubText.raycastTarget = false;

            RectTransform subRect = dayCardSubText.rectTransform;
            subRect.anchorMin = new Vector2(0.5f, 0.5f);
            subRect.anchorMax = new Vector2(0.5f, 0.5f);
            subRect.anchoredPosition = new Vector2(0f, -26f);
            subRect.sizeDelta = new Vector2(1100f, 64f);

            endDemoHintObj = new GameObject("RestartHint_Text");
            endDemoHintObj.transform.SetParent(panelObj.transform, false);
            Text restartText = endDemoHintObj.AddComponent<Text>();
            restartText.font = builtinFont;
            restartText.fontSize = 15;
            restartText.alignment = TextAnchor.MiddleCenter;
            restartText.color = new Color(0.66f, 0.66f, 0.65f, 0.90f);
            restartText.text = "[R] Reiniciar   |   [ESC] Sair";
            restartText.raycastTarget = false;

            RectTransform hintRect = restartText.rectTransform;
            hintRect.anchorMin = new Vector2(0.5f, 0.10f);
            hintRect.anchorMax = new Vector2(0.5f, 0.10f);
            hintRect.anchoredPosition = Vector2.zero;
            hintRect.sizeDelta = new Vector2(900f, 44f);
            endDemoHintObj.SetActive(false);
        }

        private IEnumerator PlayStudioLogoIntroRoutine()
        {
            EnsureStudioLogoUIReady();
            if (studioLogoRoot == null || studioLogoGroup == null)
            {
                yield return new WaitForSeconds(0.35f);
                yield break;
            }

            studioLogoRoot.SetActive(true);
            studioLogoGroup.alpha = 0f;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(studioLogoRoot.GetComponent<RectTransform>());

            float fadeInDur = Mathf.Max(0.15f, studioLogoFadeInDuration);
            float holdDur = Mathf.Max(0f, studioLogoHoldDuration);
            float fadeOutDur = Mathf.Max(0.15f, studioLogoFadeOutDuration);
            float postPause = Mathf.Max(0f, studioLogoPostBlackPause);

            if (audioManager != null)
            {
                audioManager.PulseSilenceDread(0.25f + fadeInDur + holdDur + fadeOutDur, 0.35f);
            }

            // Let frame-0 init settle before starting the fade-in timer.
            yield return null;

            float settle = 0f;
            while (settle < 0.20f)
            {
                settle += Mathf.Min(Time.deltaTime, 0.05f);
                yield return null;
            }

            float elapsed = 0f;
            while (elapsed < fadeInDur)
            {
                elapsed += Mathf.Min(Time.deltaTime, 0.05f);
                studioLogoGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeInDur));
                yield return null;
            }
            studioLogoGroup.alpha = 1f;

            float holdElapsed = 0f;
            while (holdElapsed < holdDur)
            {
                holdElapsed += Mathf.Min(Time.deltaTime, 0.05f);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < fadeOutDur)
            {
                elapsed += Mathf.Min(Time.deltaTime, 0.05f);
                studioLogoGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeOutDur));
                yield return null;
            }
            studioLogoGroup.alpha = 0f;
            studioLogoRoot.SetActive(false);

            float pauseElapsed = 0f;
            while (pauseElapsed < postPause)
            {
                pauseElapsed += Mathf.Min(Time.deltaTime, 0.05f);
                yield return null;
            }
        }

        private void EnsureStudioLogoUIReady()
        {
            if (studioLogoRoot != null)
                return;

            BuildTransitionOverlayUI();
            if (blackFadeGroup == null)
                return;

            Texture2D logoTex = studioLogoTexture != null
                ? studioLogoTexture
                : Resources.Load<Texture2D>("UI/KENOMA");

            if (logoTex == null)
            {
                Debug.LogWarning("[DayManager] Texture2D da logo do estúdio não encontrada em Resources/UI/KENOMA.");
                return;
            }

            studioLogoRoot = new GameObject("StudioLogo_Container");
            studioLogoRoot.transform.SetParent(blackFadeGroup.transform, false);
            RectTransform rootRect = studioLogoRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            studioLogoGroup = studioLogoRoot.AddComponent<CanvasGroup>();
            studioLogoGroup.alpha = 0f;
            studioLogoGroup.blocksRaycasts = false;
            studioLogoGroup.interactable = false;

            // 56% vertical band (80% reduced by 30%: 22% black top, 22% black bottom)
            const float logoHeightFraction = 0.80f * 0.70f;
            float verticalMargin = (1f - logoHeightFraction) * 0.5f;

            GameObject bandObj = new GameObject("StudioLogo_VerticalBand");
            bandObj.transform.SetParent(studioLogoRoot.transform, false);
            RectTransform bandRect = bandObj.AddComponent<RectTransform>();
            bandRect.anchorMin = new Vector2(0f, verticalMargin);
            bandRect.anchorMax = new Vector2(1f, 1f - verticalMargin);
            bandRect.pivot = new Vector2(0.5f, 0.5f);
            bandRect.offsetMin = Vector2.zero;
            bandRect.offsetMax = Vector2.zero;

            // Centered 1:1 square inside the 56% vertical band (proportional black margins on left/right)
            GameObject logoObj = new GameObject("StudioLogo_Image");
            logoObj.transform.SetParent(bandObj.transform, false);
            RawImage logoImg = logoObj.AddComponent<RawImage>();
            logoImg.texture = logoTex;
            logoImg.color = Color.white;
            logoImg.raycastTarget = false;

            RectTransform logoRect = logoImg.rectTransform;
            logoRect.anchorMin = Vector2.zero;
            logoRect.anchorMax = Vector2.one;
            logoRect.pivot = new Vector2(0.5f, 0.5f);
            logoRect.offsetMin = Vector2.zero;
            logoRect.offsetMax = Vector2.zero;

            AspectRatioFitter fitter = logoObj.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = logoTex.height > 0 ? (float)logoTex.width / logoTex.height : 1f;

            studioLogoRoot.SetActive(false);
        }

        public void ConfigureStudioLogoTexture(Texture2D logoTexture)
        {
            studioLogoTexture = logoTexture;
        }

        public void ConfigureManagerReferences(
            GameObject casa,
            GameObject p1,
            GameObject p2,
            GameObject p3,
            GameObject qFilhas,
            Transform sp1,
            Transform spSofaSit,
            Transform sp2Sofa,
            Transform sp3,
            Transform sp4Filhas,
            PlayerController pl,
            SanityEffects sanity,
            TVController tv,
            ObjectiveHUD hud,
            Canal420AudioManager audioMgr,
            DoorInteractable offDoor,
            DoorInteractable lockedDoor,
            Light mainLight,
            Light[] roomLights,
            Light day3CircuitLight,
            GameObject margaretObj)
        {
            UnsubscribeDoorEvents();

            casaBase = casa;
            propsDia1 = p1;
            propsDia2 = p2;
            propsDia3 = p3;
            quartoFilhas = qFilhas;

            spawnDia1 = sp1;
            spawnSofaSentado = spSofaSit;
            spawnDia2Sofa = sp2Sofa;
            spawnDia3 = sp3;
            spawnDia4QuartoFilhas = sp4Filhas;

            player = pl;
            sanityEffects = sanity;
            tvController = tv;
            objectiveHUD = hud;
            audioManager = audioMgr;
            officeDoor = offDoor;
            corridorLockedDoor = lockedDoor;
            houseMainLight = mainLight;
            houseInteriorLights = roomLights;
            day3RestoredCircuitLight = day3CircuitLight;
            margaretJumpscareObject = margaretObj;

            day1RequiredTaskIds = new List<string>
            {
                "lavar_rosto",
                "checar_secretaria",
                "olhar_papeis",
                "checar_calendario"
            };
            requireDay1TasksBeforeTV = true;
            day2EventsNeededForTV = 5;

            if (Application.isPlaying && isActiveAndEnabled)
            {
                SubscribeDoorEvents();
            }
        }

        public void ConfigureExtendedLightingReferences(
            Light[] secondaryLights,
            Light[] architecturalFills,
            ReflectionProbe[] reflectionProbes)
        {
            secondaryMotivatedLights = secondaryLights;
            architecturalFillLights = architecturalFills;
            interiorReflectionProbes = reflectionProbes;
        }

        public void ConfigureSofaReferences(Transform sofaRoot, Transform standUpPoint)
        {
            sofaRootTransform = sofaRoot;
            spawnSofaLevantar = standUpPoint;
            cachedSofaColliders = null;
        }

        public void SetSofaCollisionEnabled(bool enabledState)
        {
            if (sofaRootTransform == null)
            {
                SofaTVProxy proxy = FindAnyObjectByType<SofaTVProxy>(FindObjectsInactive.Include);
                if (proxy != null) sofaRootTransform = proxy.transform;
            }

            if (sofaRootTransform == null)
                return;

            if (cachedSofaColliders == null || cachedSofaColliders.Length == 0)
            {
                cachedSofaColliders = sofaRootTransform.GetComponentsInChildren<Collider>(true);
            }

            for (int i = 0; i < cachedSofaColliders.Length; i++)
            {
                if (cachedSofaColliders[i] != null)
                {
                    cachedSofaColliders[i].enabled = enabledState;
                }
            }
        }

        public Vector3 GetSofaStandUpWorldPosition()
        {
            if (spawnSofaLevantar != null)
                return spawnSofaLevantar.position;

            if (spawnSofaSentado != null)
                return spawnSofaSentado.position + spawnSofaSentado.forward * 0.78f;

            return new Vector3(-0.84f, 0.02f, -2.92f);
        }

        [ContextMenu("Debug: Pular para Dia 1 (Burnout)")]
        private void DebugGoDay1() => TransitionToDay(GameDay.Day1, forceImmediateOverride: true, instantCut: false);

        [ContextMenu("Debug: Pular para Dia 2 (Depressoras + Notícia 94)")]
        private void DebugGoDay2() => TransitionToDay(GameDay.Day2, forceImmediateOverride: true, instantCut: false);

        [ContextMenu("Debug: Pular para Dia 3 (Perturbadoras + Cofre 1009)")]
        private void DebugGoDay3() => TransitionToDay(GameDay.Day3, forceImmediateOverride: true, instantCut: false);

        [ContextMenu("Debug: Pular para Dia 4 (Prelúdio Quarto das Filhas)")]
        private void DebugGoDay4() => TransitionToDay(GameDay.Day4_Prelude, forceImmediateOverride: true, instantCut: true);
    }
}
