using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Canal420.Audio;
using Canal420.Core;
using Canal420.Effects;
using Canal420.Interaction;
using Canal420.Player;
using Canal420.Puzzles;

namespace Canal420.Events
{
    public class PsychologicalHorrorDirector : MonoBehaviour
    {
        private GameDay activeDay = GameDay.Day1;
        private PlayerController player;
        private Canal420AudioManager audioManager;

        private DoorInteractable lockedCorridorDoor;
        private TVController cachedTvController;
        private MirrorHauntController cachedMirrorHaunt;
        private FlashlightController cachedFlashlight;
        private FallingPictureFrame day3MargaretPortrait;
        private Renderer day3UnpluggedLedRenderer;
        private Material originalUnpluggedLedMaterial;
        private Material runtimeUnpluggedLedMaterial;
        private Vector3 day3UnpluggedMachineWorldPos = new Vector3(-3.06f, 0.85f, 1.825f);
        private bool hasDay3UnpluggedMachine = false;
        private Coroutine unpluggedRelayClickCoroutine;
        private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropEmissionColor = Shader.PropertyToID("_EmissionColor");

        private Vector3 showerBoxWorldPos = new Vector3(-4.8f, 1.3f, 5.1f);
        private bool hasShowerBox = false;
        private Vector3 alarmClockWorldPos = new Vector3(-2.0f, 0.6f, 5.5f);
        private bool hasAlarmClock = false;
        private Vector3 tvSalaWorldPos = new Vector3(-0.84f, 0.88f, -5.45f);
        private bool hasTvSala = false;

        private Transform officeChairTransform;
        private bool officeChairInitialPoseCaptured = false;
        private Vector3 officeChairInitialLocalPos;
        private Quaternion officeChairInitialLocalRot;

        // Phase 3.1 Event 1: Silent dining chair rotation toward corridor on Day 2 when out of view
        private Transform diningChairTransform;
        private bool diningChairInitialPoseCaptured = false;
        private Vector3 diningChairInitialLocalPos;
        private Quaternion diningChairInitialLocalRot;
        private bool day2DiningChairShifted = false;

        // Phase 3.1 Event 4: Margaret's Day 3 portrait tilts further + plaster chips appear out of view
        private bool day3PortraitSeenBeforeShift = false;
        private bool day3PortraitShiftedOffscreen = false;

        // Phase 3.1 Event 5 (Zero Phone Ringing): Unplugged answering machine phantom red LED + dry mechanical relay click after Day 3 newspaper
        private bool day3NewspaperRead = false;
        private bool day3UnpluggedLedTriggered = false;
        private bool day3UnpluggedLedLit = false;
        private float day3UnpluggedLedObservedTimer = 0f;

        private readonly RaycastHit[] occlusionHitBuffer = new RaycastHit[24];

        private float globalHauntCooldown = 0f;
        private float domesticBeatCooldown = 0f;
        private float day2TapeElapsed = 0f;
        private float day3ActiveElapsed = 0f;

        private bool day1OfficePapersRead = false;
        private bool day1ChairShifted = false;
        private bool day1TvThermalTickPlayed = false;
        private float day1AnomalyCooldown = 0f;

        private bool showerTapPlayedThisDay = false;
        private bool alarmCracklePlayedDay3 = false;
        private bool internalLatchPlayedThisDay = false;
        private bool day2TapeHeard = false;
        private bool day3FlashlightCollected = false;
        private bool day3PowerRestored = false;
        private int day3ClueEscalation = 0;
        private bool day3SafeUnlocked = false;
        private bool day3DoorTrapOpened = false;
        private bool isBusyRoutine = false;
        private Light activeHauntedLight;
        private float activeHauntedLightSavedIntensity = 0f;
        private Color activeHauntedLightSavedColor = Color.white;

        public bool HasDay2DiningChairShifted => day2DiningChairShifted;
        public bool HasDay3PortraitShiftedOffscreen => day3PortraitShiftedOffscreen;
        public bool HasDay3UnpluggedLedTriggered => day3UnpluggedLedTriggered;
        public bool IsDay3UnpluggedLedEmissiveActive =>
            day3UnpluggedLedLit
            && day3UnpluggedLedRenderer != null
            && day3UnpluggedLedRenderer.sharedMaterial != null
            && day3UnpluggedLedRenderer.sharedMaterial.IsKeywordEnabled("_EMISSION");

        public void ConfigureForDay(
            GameDay day,
            PlayerController playerRef,
            Canal420AudioManager audioRef,
            Light[] ceilingLights,
            Light eastCorridor,
            DoorInteractable lockedDoor)
        {
            StopAllCoroutines();
            unpluggedRelayClickCoroutine = null;
            ClearTrackedHauntedLight(restoreIfValid: false);
            ClearDay3UnpluggedLedGlow();
            activeDay = day;
            player = playerRef;
            cachedFlashlight = player != null ? player.GetComponent<FlashlightController>() : null;
            audioManager = audioRef;
            lockedCorridorDoor = lockedDoor;

            globalHauntCooldown = 10f;
            domesticBeatCooldown = 0f;
            day2TapeElapsed = 0f;
            day3ActiveElapsed = 0f;
            day1OfficePapersRead = false;
            day1ChairShifted = false;
            day1TvThermalTickPlayed = false;
            day1AnomalyCooldown = 0f;
            day2DiningChairShifted = false;
            day3PortraitSeenBeforeShift = false;
            day3PortraitShiftedOffscreen = false;
            day3NewspaperRead = false;
            day3UnpluggedLedTriggered = false;
            day3UnpluggedLedLit = false;
            day3UnpluggedLedObservedTimer = 0f;
            showerTapPlayedThisDay = false;
            alarmCracklePlayedDay3 = false;
            internalLatchPlayedThisDay = false;
            day2TapeHeard = false;
            day3FlashlightCollected = false;
            day3PowerRestored = false;
            day3ClueEscalation = 0;
            day3SafeUnlocked = false;
            day3DoorTrapOpened = false;
            isBusyRoutine = false;

            CacheSceneInteractiveProps();
            RestoreOfficeChairPoseImmediate();
            RestoreDiningChairPoseImmediate();
        }

        public void NotifyDay1OfficePapersRead()
        {
            if (activeDay != GameDay.Day1 || day1OfficePapersRead) return;
            day1OfficePapersRead = true;
            day1AnomalyCooldown = 1.4f;
        }

        private void RestoreOfficeChairPoseImmediate()
        {
            if (officeChairTransform != null && officeChairInitialPoseCaptured)
            {
                officeChairTransform.localPosition = officeChairInitialLocalPos;
                officeChairTransform.localRotation = officeChairInitialLocalRot;
                Physics.SyncTransforms();
            }
        }

        private void RestoreDiningChairPoseImmediate()
        {
            if (diningChairTransform != null && diningChairInitialPoseCaptured)
            {
                diningChairTransform.localPosition = diningChairInitialLocalPos;
                diningChairTransform.localRotation = diningChairInitialLocalRot;
                Physics.SyncTransforms();
            }
        }

        public void NotifyDay2TapeHeard()
        {
            if (!day2TapeHeard)
            {
                day2TapeElapsed = 0f;
            }
            day2TapeHeard = true;
            domesticBeatCooldown = Mathf.Min(domesticBeatCooldown, 1.8f);
            globalHauntCooldown = Mathf.Max(globalHauntCooldown, 6.4f);

            // Event 2 (Day 2): Arm subtle bathroom mirror door discrepancy only after the tape is heard
            // (so it never overlaps with the earlier cassette-pickup doorway apparition).
            if (cachedMirrorHaunt == null)
            {
                cachedMirrorHaunt = FindAnyObjectByType<MirrorHauntController>(FindObjectsInactive.Include);
            }
            if (cachedMirrorHaunt != null)
            {
                cachedMirrorHaunt.ArmMirrorDoorDiscrepancy();
            }
        }

        public void NotifyDay3FlashlightCollected()
        {
            day3FlashlightCollected = true;
            if (cachedMirrorHaunt == null)
            {
                cachedMirrorHaunt = FindAnyObjectByType<MirrorHauntController>(FindObjectsInactive.Include);
            }
            if (cachedMirrorHaunt != null)
            {
                cachedMirrorHaunt.ArmMirrorDoorDiscrepancy();
            }
        }

        public void NotifyDay3PowerRestored()
        {
            day3PowerRestored = true;
            globalHauntCooldown = 9f;
            if (cachedMirrorHaunt == null)
            {
                cachedMirrorHaunt = FindAnyObjectByType<MirrorHauntController>(FindObjectsInactive.Include);
            }
            if (cachedMirrorHaunt != null)
            {
                cachedMirrorHaunt.ArmMirrorDoorDiscrepancy();
            }
        }

        public void NotifyDay3NewspaperRead()
        {
            if (activeDay != GameDay.Day3) return;
            day3NewspaperRead = true;
        }

        public void NotifyDay3ClueDiscovered(int clueCount)
        {
            day3ClueEscalation = Mathf.Max(day3ClueEscalation, clueCount);
            float maxWait = Mathf.Lerp(14f, 7.5f, Mathf.Clamp01(day3ClueEscalation / 4f));
            globalHauntCooldown = Mathf.Min(globalHauntCooldown, maxWait);
        }

        public void NotifyDay3SafeUnlocked()
        {
            day3SafeUnlocked = true;
            day3ClueEscalation = Mathf.Max(day3ClueEscalation, 4);
            StopAllCoroutines();
            unpluggedRelayClickCoroutine = null;
            ClearTrackedHauntedLight(restoreIfValid: day3PowerRestored);
            ClearDay3UnpluggedLedGlow();
            isBusyRoutine = false;
            globalHauntCooldown = 6f;
        }

        public void NotifyDay3DoorTrapOpened()
        {
            day3DoorTrapOpened = true;
            StopAllCoroutines();
            unpluggedRelayClickCoroutine = null;
            ClearTrackedHauntedLight(restoreIfValid: false);
            ClearDay3UnpluggedLedGlow();
            isBusyRoutine = false;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            unpluggedRelayClickCoroutine = null;
            ClearTrackedHauntedLight(restoreIfValid: DayManager.Instance == null || !DayManager.Instance.IsTransitioning);
            ClearDay3UnpluggedLedGlow();
            isBusyRoutine = false;
        }

        private void OnDestroy()
        {
            ClearDay3UnpluggedLedGlow();
            if (runtimeUnpluggedLedMaterial != null)
            {
                if (Application.isPlaying) Destroy(runtimeUnpluggedLedMaterial);
                else DestroyImmediate(runtimeUnpluggedLedMaterial);
                runtimeUnpluggedLedMaterial = null;
            }
        }

        private void ClearTrackedHauntedLight(bool restoreIfValid)
        {
            if (activeHauntedLight != null)
            {
                if (restoreIfValid && (DayManager.Instance == null || !DayManager.Instance.IsTransitioning))
                {
                    activeHauntedLight.enabled = true;
                    activeHauntedLight.intensity = activeHauntedLightSavedIntensity;
                    DayManager.SyncLampFixtureVisual(activeHauntedLight, true, activeHauntedLightSavedColor, 0.80f);
                }
                activeHauntedLight = null;
            }
        }

        public void TriggerFuseGrabKnockCue()
        {
            if (activeDay != GameDay.Day3 || isBusyRoutine) return;
            StartCoroutine(FuseGrabKnockRoutine());
        }

        private IEnumerator FuseGrabKnockRoutine()
        {
            isBusyRoutine = true;
            yield return new WaitForSeconds(0.55f);

            if (DayManager.Instance != null && (DayManager.Instance.IsTransitioning || DayManager.Instance.IsDemoEnded))
            {
                isBusyRoutine = false;
                yield break;
            }

            if (activeDay == GameDay.Day3 && audioManager != null)
            {
                Vector3 knockPos = lockedCorridorDoor != null
                    ? lockedCorridorDoor.GetClosedDoorCenterWorld() + Vector3.up * 1.32f
                    : new Vector3(2.60f, 1.32f, 4.60f);

                audioManager.PulseSilenceDread(0.30f, 1.2f);
                audioManager.PlayFoley3DPanned(FoleySoundId.DoorKnockWood, knockPos, 0.92f, 1.2f, 22f, 0.94f);
                yield return new WaitForSeconds(0.22f);
                if (activeDay == GameDay.Day3 && (DayManager.Instance == null || !DayManager.Instance.IsTransitioning))
                {
                    audioManager.PlayFoley3DPanned(FoleySoundId.DoorKnockWood, knockPos, 0.98f, 1.2f, 22f, 0.88f);
                }
            }

            globalHauntCooldown = 15f;
            isBusyRoutine = false;
        }

        private void Update()
        {
            if (activeDay == GameDay.Day4_Prelude || isBusyRoutine || day3DoorTrapOpened)
                return;

            if (player == null || audioManager == null)
                return;

            if (DayManager.Instance != null && (DayManager.Instance.IsTransitioning || DayManager.Instance.IsDemoEnded))
                return;

            if (Canal420.UI.ObjectiveHUD.IsBlockingModalOpen || player.IsRestrictedLook)
                return;

            if (activeDay == GameDay.Day1)
            {
                UpdateDay1SilentMicroAnomalies();
                return;
            }

            if (globalHauntCooldown > 0f)
            {
                globalHauntCooldown -= Time.deltaTime;
            }
            if (domesticBeatCooldown > 0f)
            {
                domesticBeatCooldown -= Time.deltaTime;
            }
            if (activeDay == GameDay.Day2 && day2TapeHeard)
            {
                day2TapeElapsed += Time.deltaTime;
            }
            else if (activeDay == GameDay.Day3 && day3PowerRestored)
            {
                day3ActiveElapsed += Time.deltaTime;
            }

            Vector3 p = player.transform.position;
            Camera cam = Camera.main;
            if (cam == null) return;

            // Phase 3.1 Occlusion-gated Continuity & Gaslighting Events (zero phone ringing, zero random poltergeist RNG)
            UpdateDay2SilentDiningChairShift(p, cam);
            UpdateDay3OffscreenPortraitShift(p, cam);
            UpdateDay3UnpluggedMachineContinuityBeat(p, cam);

            // Deliberate, milestone-gated psychological beats (zero phone ringing, zero random poltergeist spam)
            if (domesticBeatCooldown <= 0f)
            {
                // Contextual Beat: Internal brass latch on Porta_Quarto_Pivot slowly pressed from inside
                if (!internalLatchPlayedThisDay
                    && lockedCorridorDoor != null
                    && lockedCorridorDoor.IsLocked
                    && !lockedCorridorDoor.IsOpen
                    && IsInCorridorForLockedDoor(p)
                    && ((activeDay == GameDay.Day2 && day2TapeHeard && day2TapeElapsed >= 4.5f)
                        || (activeDay == GameDay.Day3 && day3PowerRestored && !day3SafeUnlocked && day3ActiveElapsed >= 4.0f)))
                {
                    Vector3 doorCenter = lockedCorridorDoor.GetClosedDoorCenterWorld();
                    Vector3 latchPos = doorCenter + Vector3.up * 1.02f;
                    float distToDoor = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(doorCenter.x, doorCenter.z));
                    if (distToDoor >= 1.15f && distToDoor <= 4.85f)
                    {
                        internalLatchPlayedThisDay = true;
                        lockedCorridorDoor.TriggerSlowInternalLatchStrain(1.28f);
                        audioManager.PlayFoley3DPanned(
                            FoleySoundId.InternalLatchForcedSlow,
                            latchPos,
                            0.85f,
                            minDistance: 0.85f,
                            maxDistance: 15f,
                            pitchMultiplier: activeDay == GameDay.Day3 ? 0.95f : 1.0f);
                        domesticBeatCooldown = 5.0f;
                        globalHauntCooldown = Mathf.Max(globalHauntCooldown, 11f);
                        return;
                    }
                }
            }

            if (globalHauntCooldown > 0f)
                return;

            // Once the safe is unlocked on Day 3, draw player strictly to the locked door (dead silence right at the threshold)
            if (activeDay == GameDay.Day3 && day3SafeUnlocked)
            {
                Vector3 doorCenter = lockedCorridorDoor != null
                    ? lockedCorridorDoor.GetClosedDoorCenterWorld()
                    : new Vector3(2.60f, 0f, 4.60f);
                float distToLockedDoor = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(doorCenter.x, doorCenter.z));
                if (distToLockedDoor > 2.80f)
                {
                    audioManager.PlayFoley3DPanned(
                        FoleySoundId.DoorKnockWood,
                        doorCenter + Vector3.up * 1.30f + Vector3.right * 0.25f,
                        0.84f,
                        1.0f,
                        18f,
                        0.76f);
                    globalHauntCooldown = 11.5f;
                }
                else
                {
                    globalHauntCooldown = 3.0f;
                }
                return;
            }

            // Contextual Beat C: Subtle shower glass tap once when back is turned inside bathroom (Day 2 post-tape or Day 3 post-power)
            if (!showerTapPlayedThisDay
                && hasShowerBox
                && ((activeDay == GameDay.Day2 && day2TapeHeard) || (activeDay == GameDay.Day3 && day3PowerRestored)))
            {
                Vector3 toShower = showerBoxWorldPos - cam.transform.position;
                float dist = toShower.magnitude;
                if (dist < 2.25f && Vector3.Dot(cam.transform.forward, toShower.normalized) < 0.20f)
                {
                    showerTapPlayedThisDay = true;
                    audioManager.PlayFoley3DPanned(FoleySoundId.GlassTap, showerBoxWorldPos, 0.82f, 0.5f, 9f, 0.96f);
                    globalHauntCooldown = 16f;
                    return;
                }
            }

            // Contextual Beat D: Entering Cian's bedroom on Day 3 after breaker restore triggers subtle nightstand clock click
            if (activeDay == GameDay.Day3 && day3PowerRestored && !alarmCracklePlayedDay3 && hasAlarmClock)
            {
                if (Vector3.Distance(p, alarmClockWorldPos) < 2.6f)
                {
                    alarmCracklePlayedDay3 = true;
                    audioManager.PlayFoley3DPanned(FoleySoundId.LightSwitchClick, alarmClockWorldPos, 0.68f, 0.6f, 10f, 1.18f);
                    globalHauntCooldown = 15f;
                }
            }
        }

        public static bool IsInCorridorForLockedDoor(Vector3 p)
        {
            bool inNorthWing = p.x >= 0.96f && p.x <= 2.60f && p.z >= 0.45f && p.z <= 6.05f;
            bool inEastCentralBranch = p.x >= -0.40f && p.x <= 2.60f && p.z >= 0.45f && p.z <= 2.15f;
            return inNorthWing || inEastCentralBranch;
        }

        public static bool IsInLivingRoomForTvAnomaly(Vector3 p)
        {
            return p.x > -2.80f && p.x < 1.35f && p.z > -5.90f && p.z < 0.55f;
        }

        private void UpdateDay1SilentMicroAnomalies()
        {
            if (!day1OfficePapersRead)
                return;

            if (day1ChairShifted && day1TvThermalTickPlayed)
                return;

            if (day1AnomalyCooldown > 0f)
            {
                day1AnomalyCooldown -= Time.deltaTime;
                return;
            }

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 p = player.transform.position;

            // Micro-anomaly 1 (silent, zero stinger): office swivel chair silently rolls back & angles toward door when back is turned (e.g. while checking calendar)
            if (!day1ChairShifted && officeChairTransform != null && officeChairInitialPoseCaptured)
            {
                Vector3 chairWorld = officeChairTransform.position + Vector3.up * 0.65f;
                Vector3 toChair = chairWorld - cam.transform.position;
                float dist = toChair.magnitude;
                Vector3 shiftedLocalPos = officeChairInitialLocalPos + new Vector3(-0.30f, 0f, 0.18f);
                Vector3 shiftedWorldPos = officeChairTransform.parent != null
                    ? officeChairTransform.parent.TransformPoint(shiftedLocalPos)
                    : shiftedLocalPos;
                float playerToDestDist = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(shiftedWorldPos.x, shiftedWorldPos.z));

                if (dist >= 1.55f && dist <= 8.5f && playerToDestDist >= 1.10f && Vector3.Dot(cam.transform.forward, toChair / dist) < -0.22f)
                {
                    day1ChairShifted = true;
                    officeChairTransform.localPosition = shiftedLocalPos;
                    officeChairTransform.localRotation = officeChairInitialLocalRot * Quaternion.Euler(0f, -48f, 0f);
                    Physics.SyncTransforms();
                    day1AnomalyCooldown = 3.2f;
                    return;
                }
            }

            // Micro-anomaly 2 (grounded domestic physics, zero stinger): dry thermal glass tick from the turned-off CRT TV once player is inside the living room (never behind kitchen/office walls)
            if (!day1TvThermalTickPlayed && hasTvSala && IsInLivingRoomForTvAnomaly(p) && (cachedTvController == null || cachedTvController.IsWaitingToSitDay1))
            {
                float distToTv = Vector3.Distance(cam.transform.position, tvSalaWorldPos);
                if (distToTv >= 1.35f && distToTv <= 6.4f)
                {
                    day1TvThermalTickPlayed = true;
                    audioManager.PlayFoley3DPanned(
                        FoleySoundId.TVGlassThermalCrack,
                        tvSalaWorldPos,
                        0.82f,
                        minDistance: 0.9f,
                        maxDistance: 12f,
                        pitchMultiplier: 1.0f);
                    day1AnomalyCooldown = 6.5f;
                }
            }
        }

        private void UpdateDay2SilentDiningChairShift(Vector3 playerPos, Camera cam)
        {
            if (activeDay != GameDay.Day2 || !day2TapeHeard || day2DiningChairShifted)
                return;
            if (diningChairTransform == null || !diningChairInitialPoseCaptured)
                return;

            Vector3 chairWorld = diningChairTransform.position + Vector3.up * 0.78f;
            Vector3 shiftedLocalPos = diningChairInitialLocalPos + new Vector3(0.14f, 0f, 0.42f);
            Vector3 shiftedWorldPos = diningChairTransform.parent != null
                ? diningChairTransform.parent.TransformPoint(shiftedLocalPos)
                : shiftedLocalPos;

            float distToDest = Vector2.Distance(new Vector2(playerPos.x, playerPos.z), new Vector2(shiftedWorldPos.x, shiftedWorldPos.z));
            if (distToDest < 1.25f)
                return;

            // Both the current pose AND the shifted destination must be occluded / out of view so it never pops on screen
            if (!IsPointOccludedOrOutOfView(cam, chairWorld, maxDirectDot: -0.15f, ignoreHierarchyRoot: diningChairTransform))
                return;
            if (!IsPointOccludedOrOutOfView(cam, shiftedWorldPos + Vector3.up * 0.78f, maxDirectDot: -0.15f, ignoreHierarchyRoot: diningChairTransform))
                return;

            day2DiningChairShifted = true;
            diningChairTransform.localPosition = shiftedLocalPos;
            diningChairTransform.localRotation = diningChairInitialLocalRot * Quaternion.Euler(0f, -152f, 0f);
            Physics.SyncTransforms();
        }

        private void UpdateDay3OffscreenPortraitShift(Vector3 playerPos, Camera cam)
        {
            if (activeDay != GameDay.Day3 || day3PortraitShiftedOffscreen || day3SafeUnlocked)
                return;
            if (!day3FlashlightCollected && !day3PowerRestored)
                return;
            if (day3MargaretPortrait == null || !day3MargaretPortrait.gameObject.activeInHierarchy || day3MargaretPortrait.HasFallen)
                return;

            Vector3 portraitPos = day3MargaretPortrait.transform.position;
            Vector3 toPortrait = portraitPos - cam.transform.position;
            float dist = toPortrait.magnitude;
            if (dist < 0.25f) return;

            float dot = Vector3.Dot(cam.transform.forward, toPortrait / dist);
            if (!day3PortraitSeenBeforeShift)
            {
                bool flashlightIlluminating = cachedFlashlight != null && cachedFlashlight.IsLightOn;
                if (!day3PowerRestored && !flashlightIlluminating)
                    return;

                float requiredDot = day3PowerRestored ? 0.62f : 0.80f;
                float maxSeenDist = day3PowerRestored ? 4.80f : 4.50f;
                if (dist <= maxSeenDist && dot >= requiredDot && IsLineOfSightClear(cam.transform.position, portraitPos, day3MargaretPortrait.transform))
                {
                    day3PortraitSeenBeforeShift = true;
                }
                return;
            }

            // Trigger ONLY after player passes/sees it and then turns away or rounds the corridor corner
            bool roundedCornerOrOutOfView = dist >= 1.40f
                && IsPointOccludedOrOutOfView(cam, portraitPos, maxDirectDot: -0.22f, ignoreHierarchyRoot: day3MargaretPortrait.transform);
            if (!roundedCornerOrOutOfView)
                return;

            if (day3MargaretPortrait.ApplyOffscreenCrookedShift())
            {
                day3PortraitShiftedOffscreen = true;
                if (audioManager != null)
                {
                    audioManager.PlayFoley3DPanned(
                        FoleySoundId.DistantFloorCreak,
                        portraitPos,
                        0.34f,
                        minDistance: 0.9f,
                        maxDistance: 12f,
                        pitchMultiplier: 1.28f);
                }
            }
        }

        private void UpdateDay3UnpluggedMachineContinuityBeat(Vector3 playerPos, Camera cam)
        {
            if (activeDay != GameDay.Day3 || !day3NewspaperRead || day3SafeUnlocked)
                return;
            if (!hasDay3UnpluggedMachine || day3UnpluggedLedRenderer == null || !day3UnpluggedLedRenderer.gameObject.activeInHierarchy)
                return;

            Vector3 toMachine = day3UnpluggedMachineWorldPos - cam.transform.position;
            float dist = toMachine.magnitude;
            if (dist < 0.20f) return;
            float dot = Vector3.Dot(cam.transform.forward, toMachine / dist);

            if (!day3UnpluggedLedTriggered)
            {
                // Arm cleanly as soon as Cian steps out of Batente_Escritorio (Z > 0.62) into the corridor
                // while not staring directly at the unplugged unit down the west hallway.
                bool leftOffice = playerPos.z > 0.62f || playerPos.x < 1.35f;
                bool notStaringDirectly = dot < 0.40f || !IsLineOfSightClear(cam.transform.position, day3UnpluggedMachineWorldPos, day3UnpluggedLedRenderer.transform.parent);
                if (leftOffice && notStaringDirectly)
                {
                    day3UnpluggedLedTriggered = true;
                    day3UnpluggedLedLit = true;
                    day3UnpluggedLedObservedTimer = 0f;
                    SetDay3UnpluggedLedGlow(true);

                    // Two-part dry mechanical tape-relay click from the unplugged unit (strictly zero phone ringing)
                    if (audioManager != null)
                    {
                        audioManager.PlayFoley3DPanned(
                            FoleySoundId.LightSwitchClick,
                            day3UnpluggedMachineWorldPos,
                            0.68f,
                            minDistance: 2.0f,
                            maxDistance: 16f,
                            pitchMultiplier: 0.78f);
                    }
                    if (unpluggedRelayClickCoroutine != null) StopCoroutine(unpluggedRelayClickCoroutine);
                    unpluggedRelayClickCoroutine = StartCoroutine(PlayDelayedUnpluggedRelayClackRoutine());
                }
                return;
            }

            if (day3UnpluggedLedLit)
            {
                bool directlyLooking = dist <= 6.0f
                    && dot >= 0.84f
                    && IsLineOfSightClear(cam.transform.position, day3UnpluggedMachineWorldPos, day3UnpluggedLedRenderer.transform.parent);
                if (directlyLooking)
                {
                    day3UnpluggedLedObservedTimer += Time.deltaTime;
                    if (day3UnpluggedLedObservedTimer >= 0.48f)
                    {
                        day3UnpluggedLedLit = false;
                        ClearDay3UnpluggedLedGlow();
                        if (audioManager != null)
                        {
                            audioManager.PlayFoley3DPanned(
                                FoleySoundId.LightSwitchClick,
                                day3UnpluggedMachineWorldPos,
                                0.46f,
                                minDistance: 1.4f,
                                maxDistance: 14f,
                                pitchMultiplier: 0.70f);
                        }
                        if (InteractionSystem.Instance != null)
                        {
                            InteractionSystem.Instance.ShowSubtitle("Mas... ela tá fora da tomada.", 3.6f);
                        }
                    }
                }
                else
                {
                    day3UnpluggedLedObservedTimer = Mathf.MoveTowards(day3UnpluggedLedObservedTimer, 0f, Time.deltaTime * 1.5f);
                }
            }
        }

        private IEnumerator PlayDelayedUnpluggedRelayClackRoutine()
        {
            yield return new WaitForSeconds(0.16f);
            if (activeDay == GameDay.Day3 && day3UnpluggedLedLit && (DayManager.Instance == null || !DayManager.Instance.IsTransitioning))
            {
                if (audioManager != null)
                {
                    audioManager.PlayFoley3DPanned(
                        FoleySoundId.LightSwitchClick,
                        day3UnpluggedMachineWorldPos,
                        0.56f,
                        minDistance: 1.8f,
                        maxDistance: 16f,
                        pitchMultiplier: 0.68f);
                }
            }
            unpluggedRelayClickCoroutine = null;
        }

        private void SetDay3UnpluggedLedGlow(bool lit)
        {
            if (day3UnpluggedLedRenderer == null) return;
            if (!lit)
            {
                ClearDay3UnpluggedLedGlow();
                return;
            }

            if (originalUnpluggedLedMaterial == null)
            {
                originalUnpluggedLedMaterial = day3UnpluggedLedRenderer.sharedMaterial;
            }

            if (runtimeUnpluggedLedMaterial == null)
            {
                Material srcMat = originalUnpluggedLedMaterial;
                if (srcMat != null)
                {
                    runtimeUnpluggedLedMaterial = new Material(srcMat) { name = "M_UnpluggedLed_PhantomGlow_Runtime" };
                }
                else
                {
                    Shader fallbackShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (fallbackShader == null)
                    {
                        fallbackShader = Shader.Find("Canal420/LowPolyDirtyLit");
                    }
                    if (fallbackShader == null)
                    {
                        return;
                    }
                    runtimeUnpluggedLedMaterial = new Material(fallbackShader) { name = "M_UnpluggedLed_PhantomGlow_Runtime" };
                }
            }

            Color redCore = new Color(0.92f, 0.14f, 0.12f, 1f);
            Color hdrRedEmission = new Color(2.40f, 0.16f, 0.10f, 1f);
            runtimeUnpluggedLedMaterial.EnableKeyword("_EMISSION");
            if (runtimeUnpluggedLedMaterial.HasProperty(PropBaseColor)) runtimeUnpluggedLedMaterial.SetColor(PropBaseColor, redCore);
            if (runtimeUnpluggedLedMaterial.HasProperty(PropColor)) runtimeUnpluggedLedMaterial.SetColor(PropColor, redCore);
            if (runtimeUnpluggedLedMaterial.HasProperty(PropEmissionColor)) runtimeUnpluggedLedMaterial.SetColor(PropEmissionColor, hdrRedEmission);

            day3UnpluggedLedRenderer.SetPropertyBlock(null);
            day3UnpluggedLedRenderer.sharedMaterial = runtimeUnpluggedLedMaterial;
            day3UnpluggedLedLit = true;
        }

        private void ClearDay3UnpluggedLedGlow()
        {
            day3UnpluggedLedLit = false;
            if (day3UnpluggedLedRenderer != null)
            {
                day3UnpluggedLedRenderer.SetPropertyBlock(null);
                if (originalUnpluggedLedMaterial != null && day3UnpluggedLedRenderer.sharedMaterial == runtimeUnpluggedLedMaterial)
                {
                    day3UnpluggedLedRenderer.sharedMaterial = originalUnpluggedLedMaterial;
                }
            }
        }

        private bool IsPointOccludedOrOutOfView(Camera cam, Vector3 targetWorldPos, float maxDirectDot, Transform ignoreHierarchyRoot)
        {
            if (cam == null) return false;
            Vector3 toTarget = targetWorldPos - cam.transform.position;
            float dist = toTarget.magnitude;
            if (dist < 0.20f) return false;

            float dot = Vector3.Dot(cam.transform.forward, toTarget / dist);
            if (dot < maxDirectDot)
                return true;

            Vector3 vp = cam.WorldToViewportPoint(targetWorldPos);
            bool outsideViewport = vp.z <= 0.1f || vp.x < -0.08f || vp.x > 1.08f || vp.y < -0.08f || vp.y > 1.08f;
            if (outsideViewport)
                return true;

            return !IsLineOfSightClear(cam.transform.position, targetWorldPos, ignoreHierarchyRoot);
        }

        private bool IsLineOfSightClear(Vector3 origin, Vector3 target, Transform ignoreHierarchyRoot)
        {
            Vector3 seg = target - origin;
            float dist = seg.magnitude;
            if (dist < 0.25f) return true;

            int hitCount = Physics.RaycastNonAlloc(origin, seg / dist, occlusionHitBuffer, dist - 0.14f, ~0, QueryTriggerInteraction.Ignore);
            int clamped = Mathf.Min(hitCount, occlusionHitBuffer.Length);
            for (int i = 0; i < clamped; i++)
            {
                Collider col = occlusionHitBuffer[i].collider;
                if (col == null) continue;
                if (player != null && col.transform.IsChildOf(player.transform)) continue;
                if (ignoreHierarchyRoot != null && col.transform.IsChildOf(ignoreHierarchyRoot)) continue;
                return false;
            }
            return true;
        }

        private void CacheSceneInteractiveProps()
        {
            if (cachedTvController == null)
            {
                cachedTvController = FindAnyObjectByType<TVController>(FindObjectsInactive.Include);
            }
            if (cachedMirrorHaunt == null)
            {
                cachedMirrorHaunt = FindAnyObjectByType<MirrorHauntController>(FindObjectsInactive.Include);
            }
            if (day3MargaretPortrait == null)
            {
                day3MargaretPortrait = FindAnyObjectByType<FallingPictureFrame>(FindObjectsInactive.Include);
            }

            if (cachedFlashlight == null)
            {
                cachedFlashlight = player != null
                    ? player.GetComponent<FlashlightController>()
                    : FindAnyObjectByType<FlashlightController>(FindObjectsInactive.Include);
            }

            hasShowerBox = false;
            hasAlarmClock = false;
            hasTvSala = false;
            hasDay3UnpluggedMachine = false;
            ClearDay3UnpluggedLedGlow();
            day3UnpluggedLedRenderer = null;

            Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform tr = allTransforms[i];
                if (tr == null) continue;
                if (!hasShowerBox && tr.name == "Box_Chuveiro")
                {
                    showerBoxWorldPos = tr.position + Vector3.up * 1.25f;
                    hasShowerBox = true;
                }
                else if (!hasAlarmClock && tr.name == "Despertador_CriadoMudo")
                {
                    alarmClockWorldPos = tr.position;
                    hasAlarmClock = true;
                }
                else if (!hasTvSala && tr.name == "TV_Sala")
                {
                    tvSalaWorldPos = tr.position + Vector3.up * 0.30f;
                    hasTvSala = true;
                }
                else if (officeChairTransform == null && tr.name == "Cadeira_Escritorio")
                {
                    officeChairTransform = tr;
                    if (!officeChairInitialPoseCaptured)
                    {
                        officeChairInitialLocalPos = tr.localPosition;
                        officeChairInitialLocalRot = tr.localRotation;
                        officeChairInitialPoseCaptured = true;
                    }
                }
                else if (diningChairTransform == null && tr.name == "Cadeira_Jantar_Norte")
                {
                    diningChairTransform = tr;
                    if (!diningChairInitialPoseCaptured)
                    {
                        diningChairInitialLocalPos = tr.localPosition;
                        diningChairInitialLocalRot = tr.localRotation;
                        diningChairInitialPoseCaptured = true;
                    }
                }
                else if (!hasDay3UnpluggedMachine && tr.name == "Secretaria_Eletronica_D3")
                {
                    day3UnpluggedMachineWorldPos = tr.position + Vector3.up * 0.08f;
                    Transform ledTr = tr.Find("LED_Mensagens");
                    if (ledTr != null)
                    {
                        day3UnpluggedLedRenderer = ledTr.GetComponent<Renderer>();
                        if (day3UnpluggedLedRenderer != null && day3UnpluggedLedRenderer.sharedMaterial != runtimeUnpluggedLedMaterial)
                        {
                            originalUnpluggedLedMaterial = day3UnpluggedLedRenderer.sharedMaterial;
                        }
                    }
                    hasDay3UnpluggedMachine = day3UnpluggedLedRenderer != null;
                }
            }
        }
    }
}
