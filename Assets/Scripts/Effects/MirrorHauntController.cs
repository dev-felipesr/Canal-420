using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Canal420.Audio;
using Canal420.Core;
using Canal420.Events;
using Canal420.Interaction;
using Canal420.Player;
using Canal420.UI;

namespace Canal420.Effects
{
    public class MirrorHauntController : MonoBehaviour
    {
        private const int ReflectionWidth = 512;

        [Header("Aparição 3D Real na Porta do Banheiro (Dia 2)")]
        [SerializeField] private GameObject doorwayApparitionObject;
        [SerializeField] private Renderer mirrorGlassRenderer;
        [SerializeField] private Shader planarMirrorShader;
        [SerializeField] private DoorInteractable bathroomDoor;

        private Camera reflectionCamera;
        private RenderTexture reflectionRT;
        private Material runtimeMirrorMaterial;
        private readonly Plane[] mainFrustumPlanes = new Plane[6];
        private int lastRtWidth = 0;
        private int lastRtHeight = 0;

        private Light bathroomSconceLight = null;
        private Light bathroomCeilingLight = null;

        private GameDay activeDay = GameDay.Day1;
        private bool day2Triggered = false;
        private bool day3Triggered = false;
        private bool isRunningSequence = false;
        private float peripheralShowTimer = 0f;
        private bool dippedSconcePendingRestore = false;
        private bool dippedCeilingPendingRestore = false;

        private bool doorwayApparitionActive = false;
        private bool day2DoorwayApparitionTriggered = false;
        private bool playerSawApparitionInMirror = false;
        private float doorwayApparitionAge = 0f;
        private bool doorwayEmptyThoughtPending = false;
        private Coroutine emptyDoorwayThoughtCoroutine;

        // Phase 3.1 Event 2: Mirror-only bathroom door discrepancy (ajar in reflection, closed in reality)
        private bool isMirrorDoorDiscrepancyArmed = false;
        private bool isMirrorDoorDiscrepancyShowing = false;
        private bool playerSawDoorDiscrepancyInMirror = false;
        private float mirrorDoorDiscrepancyGazeTimer = 0f;
        private bool hasResolvedMirrorDoorDiscrepancy = false;
        private bool isOverridingBathroomDoorForReflection = false;
        private Quaternion savedBathroomDoorLocalRot = Quaternion.identity;
        private Coroutine mirrorDoorDiscrepancyThoughtCoroutine;
        private FlashlightController cachedFlashlight;

        private static readonly int ReflectionTexProp = Shader.PropertyToID("_ReflectionTex");

        public bool IsDoorwayApparitionActive => doorwayApparitionActive;
        public GameObject DoorwayApparitionObject => doorwayApparitionObject;
        public DoorInteractable BathroomDoor => bathroomDoor;
        public bool HasTriggeredDay2 => day2Triggered;
        public bool HasTriggeredDay3 => day3Triggered;
        public float PeripheralShowTimer => peripheralShowTimer;
        public bool IsMirrorDoorDiscrepancyArmed => isMirrorDoorDiscrepancyArmed;
        public bool IsMirrorDoorDiscrepancyShowing => isMirrorDoorDiscrepancyShowing;
        public bool HasResolvedMirrorDoorDiscrepancy => hasResolvedMirrorDoorDiscrepancy;

        private void Awake()
        {
            CleanupLegacyOverlayQuad();
            EnsureMirrorRendererReference();
            EnsureBathroomDoorReference();
            EnsurePlanarReflectionRig();
            HideDoorwayApparitionImmediate();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
                RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            }
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            RestoreBathroomDoorOverrideIfNeeded();
            StopAllCoroutines();
            emptyDoorwayThoughtCoroutine = null;
            mirrorDoorDiscrepancyThoughtCoroutine = null;
            isMirrorDoorDiscrepancyShowing = false;
            RestoreDippedBathroomLightsIfPending();
            isRunningSequence = false;
            HideDoorwayApparitionImmediate();
            if (reflectionCamera != null)
            {
                reflectionCamera.enabled = false;
            }
        }

        private void OnDestroy()
        {
            RestoreBathroomDoorOverrideIfNeeded();
            if (reflectionCamera != null)
            {
                reflectionCamera.targetTexture = null;
                if (Application.isPlaying) Destroy(reflectionCamera.gameObject);
                else DestroyImmediate(reflectionCamera.gameObject);
                reflectionCamera = null;
            }

            if (reflectionRT != null)
            {
                reflectionRT.Release();
                if (Application.isPlaying) Destroy(reflectionRT);
                else DestroyImmediate(reflectionRT);
                reflectionRT = null;
            }

            if (runtimeMirrorMaterial != null)
            {
                if (Application.isPlaying) Destroy(runtimeMirrorMaterial);
                else DestroyImmediate(runtimeMirrorMaterial);
                runtimeMirrorMaterial = null;
            }
        }

        public void ConfigureMirrorAssets(Renderer glassRenderer, Shader mirrorShader, GameObject apparitionObj = null)
        {
            if (glassRenderer != null) mirrorGlassRenderer = glassRenderer;
            if (mirrorShader != null) planarMirrorShader = mirrorShader;
            if (apparitionObj != null) LinkDoorwayApparition(apparitionObj);
        }

        public void LinkDoorwayApparition(GameObject apparitionObj)
        {
            doorwayApparitionObject = apparitionObj;
            if (doorwayApparitionObject != null)
            {
                doorwayApparitionObject.SetActive(false);
            }
        }

        public void LinkBathroomDoor(DoorInteractable door)
        {
            bathroomDoor = door;
        }

        public void ArmMirrorDoorDiscrepancy()
        {
            if (activeDay != GameDay.Day2 && activeDay != GameDay.Day3)
                return;
            if (hasResolvedMirrorDoorDiscrepancy)
                return;
            isMirrorDoorDiscrepancyArmed = true;
        }

        public void ConfigureForDay(GameDay day)
        {
            RestoreBathroomDoorOverrideIfNeeded();
            StopAllCoroutines();
            emptyDoorwayThoughtCoroutine = null;
            mirrorDoorDiscrepancyThoughtCoroutine = null;
            dippedSconcePendingRestore = false;
            dippedCeilingPendingRestore = false;
            activeDay = day;
            day2Triggered = false;
            day3Triggered = false;
            isRunningSequence = false;
            peripheralShowTimer = 0f;
            doorwayApparitionActive = false;
            day2DoorwayApparitionTriggered = false;
            playerSawApparitionInMirror = false;
            doorwayApparitionAge = 0f;
            doorwayEmptyThoughtPending = false;
            isMirrorDoorDiscrepancyArmed = false;
            isMirrorDoorDiscrepancyShowing = false;
            playerSawDoorDiscrepancyInMirror = false;
            mirrorDoorDiscrepancyGazeTimer = 0f;
            hasResolvedMirrorDoorDiscrepancy = false;

            HideDoorwayApparitionImmediate();
            EnsureMirrorRendererReference();
            EnsureBathroomDoorReference();
            if (Application.isPlaying)
            {
                EnsurePlanarReflectionRig();
            }
        }

        /// <summary>
        /// Spawns the real 3D humanoid figure standing motionless in the bathroom doorway behind Cian
        /// right when he grabs the cassette tape in front of the sink, visible in the real planar mirror
        /// until he turns around to face the doorway.
        /// </summary>
        public void TriggerDay2DoorwayApparition()
        {
            if (activeDay != GameDay.Day2 || day2DoorwayApparitionTriggered) return;
            day2DoorwayApparitionTriggered = true;
            day2Triggered = true;

            if (!Application.isPlaying || !isActiveAndEnabled) return;

            if (doorwayApparitionObject == null)
            {
                Transform found = FindSceneObjectByName("Aparicao_Porta_Banheiro_D2");
                if (found != null) doorwayApparitionObject = found.gameObject;
            }

            if (doorwayApparitionObject == null) return;

            doorwayApparitionObject.SetActive(true);
            HumanoidSpasmAnimator anim = doorwayApparitionObject.GetComponent<HumanoidSpasmAnimator>();
            if (anim != null) anim.SetMode(SpasmAnimMode.LurkPeek);

            doorwayApparitionActive = true;
            playerSawApparitionInMirror = false;
            doorwayApparitionAge = 0f;
            doorwayEmptyThoughtPending = true;

            // Single restrained wood floorboard creak right at the doorway behind Cian (no cheap hiss/flicker)
            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley3DPanned(
                    FoleySoundId.DistantFloorCreak,
                    doorwayApparitionObject.transform.position + Vector3.up * 0.08f,
                    0.55f,
                    0.7f,
                    10f,
                    0.92f
                );
            }
        }

        public void TriggerImmediateMirrorFlash()
        {
            if (isRunningSequence) return;
            if (Application.isPlaying && !isActiveAndEnabled) return;
            if ((DayManager.Instance != null && (DayManager.Instance.IsTransitioning || DayManager.Instance.IsDemoEnded)) || ObjectiveHUD.IsBlockingModalOpen)
                return;

            if (activeDay == GameDay.Day2)
            {
                day2Triggered = true;
            }
            else if (activeDay == GameDay.Day3)
            {
                day3Triggered = true;
                peripheralShowTimer = 0f;
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            UpdatePlanarMirrorCamera(mainCam);
            UpdateDoorwayApparitionTurnVanish(mainCam);
            UpdateMirrorDoorDiscrepancy(mainCam);
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderingCam)
        {
            if (!Application.isPlaying || reflectionCamera == null || renderingCam != reflectionCamera)
                return;

            Camera mainCam = Camera.main;
            if (mainCam == null || mirrorGlassRenderer == null)
                return;

            SyncReflectionCameraToMain(mainCam);
            ApplyBathroomDoorMirrorDiscrepancyOverrideIfActive();
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera renderingCam)
        {
            if (reflectionCamera != null && renderingCam == reflectionCamera)
            {
                RestoreBathroomDoorOverrideIfNeeded();
            }
        }

        private void ApplyBathroomDoorMirrorDiscrepancyOverrideIfActive()
        {
            RestoreBathroomDoorOverrideIfNeeded();

            if (!isMirrorDoorDiscrepancyShowing || doorwayApparitionActive || bathroomDoor == null)
                return;

            Transform pivot = bathroomDoor.DoorPivot;
            if (pivot == null)
                return;

            savedBathroomDoorLocalRot = pivot.localRotation;
            isOverridingBathroomDoorForReflection = true;

            float closedYaw = bathroomDoor.ClosedYaw;
            float openSign = Mathf.Abs(bathroomDoor.DefaultOpenAngle) > 0.01f
                ? Mathf.Sign(bathroomDoor.DefaultOpenAngle)
                : -1f;
            // When physically closed, show door unmistakably cracked ~36 deg ajar ONLY in the mirror.
            // If player left it physically wide open, show it almost shut (~6 deg) in the mirror for unmistakable contrast.
            float discrepancyYaw = !bathroomDoor.IsOpen
                ? closedYaw + openSign * 36f
                : closedYaw + openSign * 6f;

            Vector3 e = savedBathroomDoorLocalRot.eulerAngles;
            pivot.localRotation = Quaternion.Euler(e.x, discrepancyYaw, e.z);
        }

        private void RestoreBathroomDoorOverrideIfNeeded()
        {
            if (!isOverridingBathroomDoorForReflection)
                return;

            isOverridingBathroomDoorForReflection = false;
            if (bathroomDoor != null && bathroomDoor.DoorPivot != null)
            {
                bathroomDoor.DoorPivot.localRotation = savedBathroomDoorLocalRot;
            }
        }

        private bool IsBathroomMirrorVisibleInCurrentLighting()
        {
            if (activeDay != GameDay.Day3)
                return true;

            if (InventoryState.HasItem("energia_corredor_ativa"))
                return true;

            if (cachedFlashlight == null)
            {
                cachedFlashlight = FindAnyObjectByType<FlashlightController>(FindObjectsInactive.Include);
            }
            if (cachedFlashlight != null && cachedFlashlight.IsLightOn)
                return true;

            if (bathroomSconceLight != null && bathroomSconceLight.enabled && bathroomSconceLight.intensity > 0.05f)
                return true;
            if (bathroomCeilingLight != null && bathroomCeilingLight.enabled && bathroomCeilingLight.intensity > 0.05f)
                return true;

            return false;
        }

        private void UpdateMirrorDoorDiscrepancy(Camera mainCam)
        {
            if (!isMirrorDoorDiscrepancyArmed
                || hasResolvedMirrorDoorDiscrepancy
                || doorwayApparitionActive
                || (activeDay != GameDay.Day2 && activeDay != GameDay.Day3)
                || (DayManager.Instance != null && (DayManager.Instance.IsTransitioning || DayManager.Instance.IsDemoEnded)))
            {
                isMirrorDoorDiscrepancyShowing = false;
                return;
            }

            EnsureBathroomDoorReference();
            if (bathroomDoor == null)
            {
                isMirrorDoorDiscrepancyShowing = false;
                return;
            }

            Vector3 camPos = mainCam.transform.position;
            bool insideBathroom = camPos.x < -3.45f && camPos.z > 2.25f;
            if (!insideBathroom)
            {
                isMirrorDoorDiscrepancyShowing = false;
                mirrorDoorDiscrepancyGazeTimer = 0f;
                return;
            }

            Vector3 camFwd = mainCam.transform.forward;
            Vector3 camFwdHoriz = new Vector3(camFwd.x, 0f, camFwd.z);
            if (camFwdHoriz.sqrMagnitude > 0.0001f) camFwdHoriz.Normalize();

            if (camFwdHoriz.z > 0.15f)
            {
                isMirrorDoorDiscrepancyShowing = true;
                Vector3 mirrorPos = mirrorGlassRenderer != null ? mirrorGlassRenderer.transform.position : transform.position;
                Vector3 toMirrorVec = mirrorPos - camPos;
                float distToMirror = toMirrorVec.magnitude;
                bool closeEnoughToReadReflection = camPos.z >= 3.35f && distToMirror <= 3.05f && distToMirror > 0.15f;
                bool lookingAtMirror = closeEnoughToReadReflection
                    && Vector3.Dot(camFwd, toMirrorVec / distToMirror) > 0.70f
                    && IsBathroomMirrorVisibleInCurrentLighting();

                if (lookingAtMirror)
                {
                    mirrorDoorDiscrepancyGazeTimer += Time.deltaTime;
                    if (mirrorDoorDiscrepancyGazeTimer >= 0.24f)
                    {
                        playerSawDoorDiscrepancyInMirror = true;
                    }
                }
                else
                {
                    mirrorDoorDiscrepancyGazeTimer = Mathf.MoveTowards(mirrorDoorDiscrepancyGazeTimer, 0f, Time.deltaTime * 1.5f);
                }
            }
            else
            {
                isMirrorDoorDiscrepancyShowing = false;

                // Resolve and surface Cian's dry reaction ONLY once he genuinely turns back toward the physical bathroom door
                if (playerSawDoorDiscrepancyInMirror && camFwdHoriz.z < -0.42f)
                {
                    Vector3 doorCenter = bathroomDoor.GetClosedDoorCenterWorld();
                    Vector3 toDoorXZ = new Vector3(doorCenter.x - camPos.x, 0f, doorCenter.z - camPos.z);
                    bool lookingTowardDoor = toDoorXZ.sqrMagnitude > 0.01f
                        && Vector3.Dot(camFwdHoriz, toDoorXZ.normalized) > 0.50f;
                    if (lookingTowardDoor)
                    {
                        hasResolvedMirrorDoorDiscrepancy = true;
                        bool physicallyClosed = !bathroomDoor.IsOpen;
                        if (mirrorDoorDiscrepancyThoughtCoroutine != null)
                        {
                            StopCoroutine(mirrorDoorDiscrepancyThoughtCoroutine);
                        }
                        mirrorDoorDiscrepancyThoughtCoroutine = StartCoroutine(ShowMirrorDoorDiscrepancyThoughtRoutine(physicallyClosed));
                    }
                }
            }
        }

        private IEnumerator ShowMirrorDoorDiscrepancyThoughtRoutine(bool doorWasPhysicallyClosed)
        {
            yield return new WaitForSeconds(0.42f);
            if ((activeDay == GameDay.Day2 || activeDay == GameDay.Day3)
                && (DayManager.Instance == null || !DayManager.Instance.IsTransitioning))
            {
                if (InteractionSystem.Instance != null)
                {
                    InteractionSystem.Instance.ShowSubtitle(
                        doorWasPhysicallyClosed
                            ? "Ué... no espelho ela tava aberta."
                            : "No espelho a porta tava quase fechada...",
                        4.2f
                    );
                }
            }
            mirrorDoorDiscrepancyThoughtCoroutine = null;
        }

        private void EnsureBathroomDoorReference()
        {
            if (bathroomDoor != null) return;
            Transform found = FindSceneObjectByName("Porta_Banheiro_Pivot");
            if (found != null)
            {
                bathroomDoor = found.GetComponent<DoorInteractable>();
            }
        }

        private void UpdatePlanarMirrorCamera(Camera mainCam)
        {
            EnsureMirrorRendererReference();
            if (mirrorGlassRenderer == null) return;

            EnsureReflectionTextureMatchesAspect(mainCam.aspect);
            if (reflectionCamera == null || reflectionRT == null)
            {
                EnsurePlanarReflectionRig();
            }
            if (reflectionCamera == null || reflectionRT == null) return;

            // Pause, day transitions, Day 4 epilogue, or demo end never need live mirror rendering.
            bool blockedByGameState = Time.timeScale <= 0.001f
                || (DayManager.Instance != null &&
                    (DayManager.Instance.IsTransitioning ||
                     DayManager.Instance.IsDemoEnded ||
                     DayManager.Instance.CurrentDay == GameDay.Day4_Prelude));
            if (blockedByGameState)
            {
                if (reflectionCamera.enabled) reflectionCamera.enabled = false;
                return;
            }

            Vector3 camPos = mainCam.transform.position;
            Vector3 mirrorPos = mirrorGlassRenderer.transform.position;
            // Mirror front normal points into the bathroom (-Z in world space)
            Vector3 mirrorNormal = -transform.forward;

            Bounds mirrorBounds = mirrorGlassRenderer.bounds;
            float sideDist = Vector3.Dot(camPos - mirrorPos, mirrorNormal);
            bool canSeeMirrorThroughArchitecture = CanCameraSeeMirrorThroughBathroomPortal(camPos, mirrorPos, mirrorBounds);

            bool shouldRender = false;
            if (canSeeMirrorThroughArchitecture && sideDist > 0.05f)
            {
                GeometryUtility.CalculateFrustumPlanes(mainCam, mainFrustumPlanes);
                shouldRender = GeometryUtility.TestPlanesAABB(mainFrustumPlanes, mirrorBounds);
            }

            if (!shouldRender)
            {
                if (reflectionCamera.enabled) reflectionCamera.enabled = false;
                return;
            }

            if (!reflectionCamera.enabled) reflectionCamera.enabled = true;
            SyncReflectionCameraToMain(mainCam);
        }

        private bool CanCameraSeeMirrorThroughBathroomPortal(Vector3 camPos, Vector3 mirrorPos, Bounds mirrorBounds)
        {
            // Inside the bathroom (X: -6.54..-3.44, Z: 2.10..6.04) with small doorway margin
            if (camPos.x <= -3.36f && camPos.z >= 2.02f)
            {
                return true;
            }

            // Behind solid north partition wall (master bedroom, north corridor, daughters' room)
            if (camPos.z >= 2.10f)
            {
                return false;
            }

            // Distance cap outside bathroom so far rooms never incur extra HDR+shadow+SSAO mirror pass
            float dx = mirrorPos.x - camPos.x;
            float dz = mirrorPos.z - camPos.z;
            if (dx * dx + dz * dz > 11.5f * 11.5f)
            {
                return false;
            }

            // If standing outside the bathroom while the solid wooden door is completely shut, mirror is occluded
            EnsureBathroomDoorReference();
            if (bathroomDoor != null && !bathroomDoor.IsOpen && bathroomDoor.CurrentOpenDegrees < 2.5f)
            {
                return false;
            }

            // Exact 2D portal visibility test through Batente_Banheiro on Parede_CorredorNorte (Z = 2.10, X: -5.42..-4.44)
            if (!DoesSightIntervalOverlapPortalX(camPos.x, camPos.z, mirrorBounds.min.x, mirrorBounds.max.x, mirrorPos.z, 2.10f, -5.52f, -4.34f))
            {
                return false;
            }

            // If south of Parede_CorredorSul (Z = 0.46), sightline must also pass through Batente_Copa (X: -5.40..-4.40)
            if (camPos.z < 0.46f)
            {
                if (camPos.x > -2.86f)
                {
                    return false;
                }
                if (!DoesSightIntervalOverlapPortalX(camPos.x, camPos.z, mirrorBounds.min.x, mirrorBounds.max.x, mirrorPos.z, 0.46f, -5.50f, -4.30f))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool DoesSightIntervalOverlapPortalX(
            float camX, float camZ,
            float targetMinX, float targetMaxX, float targetZ,
            float portalZ, float portalMinX, float portalMaxX)
        {
            float denom = targetZ - camZ;
            if (denom <= 0.001f)
            {
                return false;
            }

            float t = (portalZ - camZ) / denom;
            if (t <= 0f || t >= 1f)
            {
                return true;
            }

            float xAtPortalA = Mathf.Lerp(camX, targetMinX, t);
            float xAtPortalB = Mathf.Lerp(camX, targetMaxX, t);
            float spanMin = Mathf.Min(xAtPortalA, xAtPortalB);
            float spanMax = Mathf.Max(xAtPortalA, xAtPortalB);
            return spanMax >= portalMinX && spanMin <= portalMaxX;
        }

        private void SyncReflectionCameraToMain(Camera mainCam)
        {
            if (reflectionCamera == null || mirrorGlassRenderer == null || mainCam == null)
                return;

            Vector3 camPos = mainCam.transform.position;
            Vector3 mirrorPos = mirrorGlassRenderer.transform.position;
            Vector3 mirrorNormal = -transform.forward;

            // Exact positive-determinant virtual reflection camera:
            // Reflects position, forward, and up across the mirror front glass surface (offset 4mm from center).
            // Because Right = Up x Forward flips sign under reflection, PlanarMirror.shader flips screenUV.x
            // to reproduce the exact planar mirror perspective from inside OR outside the bathroom.
            Vector3 planeOrigin = mirrorPos + mirrorNormal * 0.004f;
            Vector3 mirroredPos = planeOrigin + Vector3.Reflect(camPos - planeOrigin, mirrorNormal);
            Vector3 mirroredFwd = Vector3.Reflect(mainCam.transform.forward, mirrorNormal).normalized;
            Vector3 mirroredUp = Vector3.Reflect(mainCam.transform.up, mirrorNormal).normalized;

            reflectionCamera.transform.SetPositionAndRotation(
                mirroredPos,
                Quaternion.LookRotation(mirroredFwd, mirroredUp)
            );

            reflectionCamera.ResetProjectionMatrix();
            reflectionCamera.fieldOfView = mainCam.fieldOfView;
            reflectionCamera.aspect = mainCam.aspect;
            reflectionCamera.nearClipPlane = mainCam.nearClipPlane;
            reflectionCamera.farClipPlane = 28f;

            // Oblique near clip plane aligned just in front of the mirror frame so Parede_Norte & frame never occlude reflection
            Vector3 clipPointWorld = planeOrigin + mirrorNormal * 0.012f;
            Vector3 cpos = reflectionCamera.worldToCameraMatrix.MultiplyPoint(clipPointWorld);
            Vector3 cnormal = reflectionCamera.worldToCameraMatrix.MultiplyVector(mirrorNormal).normalized;
            Vector4 clipPlane = new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
            reflectionCamera.projectionMatrix = reflectionCamera.CalculateObliqueMatrix(clipPlane);
        }

        private void EnsureReflectionTextureMatchesAspect(float aspect)
        {
            if (!Application.isPlaying) return;
            float clampedAspect = Mathf.Clamp(aspect, 0.75f, 2.6f);
            int targetW = ReflectionWidth;
            int targetH = Mathf.Clamp(Mathf.RoundToInt(ReflectionWidth / clampedAspect), 224, 512);
            // Round to multiple of 4 for clean GPU alignment
            targetH = Mathf.Max(224, (targetH / 4) * 4);

            if (reflectionRT != null && lastRtWidth == targetW && lastRtHeight == targetH)
                return;

            if (reflectionRT != null)
            {
                if (reflectionCamera != null) reflectionCamera.targetTexture = null;
                reflectionRT.Release();
                Destroy(reflectionRT);
                reflectionRT = null;
            }

            reflectionRT = new RenderTexture(targetW, targetH, 16, RenderTextureFormat.DefaultHDR);
            reflectionRT.name = "RT_Espelho_Banheiro";
            reflectionRT.filterMode = FilterMode.Bilinear;
            reflectionRT.wrapMode = TextureWrapMode.Clamp;
            reflectionRT.useMipMap = false;
            reflectionRT.Create();
            lastRtWidth = targetW;
            lastRtHeight = targetH;

            if (reflectionCamera != null)
            {
                reflectionCamera.targetTexture = reflectionRT;
            }
            if (runtimeMirrorMaterial != null)
            {
                runtimeMirrorMaterial.SetTexture(ReflectionTexProp, reflectionRT);
            }
        }

        private void UpdateDoorwayApparitionTurnVanish(Camera mainCam)
        {
            if (!doorwayApparitionActive || doorwayApparitionObject == null)
                return;

            if (activeDay != GameDay.Day2 || (DayManager.Instance != null && DayManager.Instance.IsTransitioning))
            {
                HideDoorwayApparitionImmediate();
                return;
            }

            doorwayApparitionAge += Time.deltaTime;

            Vector3 camPos = mainCam.transform.position;
            Vector3 camFwd = mainCam.transform.forward;
            Vector3 camFwdHoriz = new Vector3(camFwd.x, 0f, camFwd.z);
            if (camFwdHoriz.sqrMagnitude > 0.0001f)
            {
                camFwdHoriz.Normalize();
            }
            Vector3 mirrorPos = mirrorGlassRenderer != null ? mirrorGlassRenderer.transform.position : transform.position;
            Vector3 toMirror = (mirrorPos - camPos).normalized;
            float dotMirror = Vector3.Dot(camFwd, toMirror);

            // Confirm player actually looked up into the mirror while the figure stood motionless behind them
            if (dotMirror > 0.50f && doorwayApparitionAge >= 0.10f)
            {
                playerSawApparitionInMirror = true;
            }

            // Keep the figure standing motionless in the mirror while Cian is still facing the sink/mirror
            // (even if he is momentarily looking down at the sink rim right after picking up the tape!).
            // Vanish cleanly off-screen at the exact midpoint of turning around (~83 deg sideways, camFwdHoriz.z < 0.12f),
            // when neither the mirror nor the doorway behind him is in the forward view cone, or if he walks away from the sink.
            bool turnedAwayFromMirrorTowardDoorway = camFwdHoriz.z < 0.12f && doorwayApparitionAge > 0.18f;
            bool walkedAwayFromSink = camPos.z < 3.55f;

            if (turnedAwayFromMirrorTowardDoorway || walkedAwayFromSink)
            {
                HideDoorwayApparitionImmediate();
                if (playerSawApparitionInMirror && doorwayEmptyThoughtPending)
                {
                    doorwayEmptyThoughtPending = false;
                    if (emptyDoorwayThoughtCoroutine != null) StopCoroutine(emptyDoorwayThoughtCoroutine);
                    emptyDoorwayThoughtCoroutine = StartCoroutine(ShowEmptyDoorwayReactionRoutine());
                }
            }
        }

        private IEnumerator ShowEmptyDoorwayReactionRoutine()
        {
            yield return new WaitForSeconds(0.55f);
            if (activeDay == GameDay.Day2 && (DayManager.Instance == null || !DayManager.Instance.IsTransitioning))
            {
                if (InteractionSystem.Instance != null)
                {
                    InteractionSystem.Instance.ShowSubtitle(
                        "Espera... tinha alguém na porta.",
                        4.6f
                    );
                }
            }
            emptyDoorwayThoughtCoroutine = null;
        }

        private void HideDoorwayApparitionImmediate()
        {
            doorwayApparitionActive = false;
            if (doorwayApparitionObject != null)
            {
                HumanoidSpasmAnimator anim = doorwayApparitionObject.GetComponent<HumanoidSpasmAnimator>();
                if (anim != null)
                {
                    anim.ResetPose();
                    anim.SetMode(SpasmAnimMode.Idle);
                }
                doorwayApparitionObject.SetActive(false);
            }
        }

        private void CleanupLegacyOverlayQuad()
        {
            Transform legacy = transform.Find("Overlay_Reflexo_Assombrado");
            if (legacy != null)
            {
                if (Application.isPlaying) Destroy(legacy.gameObject);
                else DestroyImmediate(legacy.gameObject);
            }
        }

        private void EnsureMirrorRendererReference()
        {
            if (mirrorGlassRenderer == null)
            {
                Transform lamina = transform.Find("Lamina_Espelho");
                if (lamina != null)
                {
                    mirrorGlassRenderer = lamina.GetComponent<MeshRenderer>();
                }
            }
        }

        private void EnsurePlanarReflectionRig()
        {
            if (!Application.isPlaying) return;
            EnsureMirrorRendererReference();
            if (mirrorGlassRenderer == null) return;

            if (reflectionRT == null)
            {
                float initialAspect = Camera.main != null ? Camera.main.aspect : (16f / 9f);
                EnsureReflectionTextureMatchesAspect(initialAspect);
            }

            if (reflectionCamera == null)
            {
                Transform existingCamTr = transform.Find("Camera_Reflexo_Espelho");
                GameObject camGo = existingCamTr != null ? existingCamTr.gameObject : new GameObject("Camera_Reflexo_Espelho");
                camGo.transform.SetParent(transform, false);
                camGo.hideFlags = HideFlags.DontSave;

                reflectionCamera = camGo.GetComponent<Camera>();
                if (reflectionCamera == null)
                {
                    reflectionCamera = camGo.AddComponent<Camera>();
                }
                reflectionCamera.enabled = false;
                reflectionCamera.targetTexture = reflectionRT;
                reflectionCamera.depth = -10f;
                reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
                reflectionCamera.backgroundColor = new Color(0.012f, 0.012f, 0.014f, 1f);
                reflectionCamera.allowHDR = true;
                reflectionCamera.allowMSAA = false;
                reflectionCamera.useOcclusionCulling = false;

                UniversalAdditionalCameraData urpData = camGo.GetComponent<UniversalAdditionalCameraData>();
                if (urpData == null)
                {
                    urpData = camGo.AddComponent<UniversalAdditionalCameraData>();
                }
                urpData.renderPostProcessing = false;
                urpData.renderShadows = true;
            }

            Shader mirrorShader = planarMirrorShader != null ? planarMirrorShader : Shader.Find("Canal420/PlanarMirror");
            if (mirrorShader != null)
            {
                if (runtimeMirrorMaterial == null)
                {
                    runtimeMirrorMaterial = new Material(mirrorShader) { name = "M_Espelho_Planar_Runtime" };
                }
                runtimeMirrorMaterial.SetTexture(ReflectionTexProp, reflectionRT);
                mirrorGlassRenderer.sharedMaterial = runtimeMirrorMaterial;
            }
        }

        private void RestoreDippedBathroomLightsIfPending()
        {
            if (!dippedSconcePendingRestore && !dippedCeilingPendingRestore) return;
            bool allowRestore = DayManager.Instance == null || !DayManager.Instance.IsTransitioning;
            if (allowRestore)
            {
                if (dippedSconcePendingRestore && bathroomSconceLight != null)
                {
                    bathroomSconceLight.enabled = true;
                    DayManager.SyncSecondaryFixtureVisual(bathroomSconceLight, true, bathroomSconceLight.color, 0.80f);
                }
                if (dippedCeilingPendingRestore && bathroomCeilingLight != null)
                {
                    bathroomCeilingLight.enabled = true;
                    DayManager.SyncLampFixtureVisual(bathroomCeilingLight, true, bathroomCeilingLight.color, 0.80f);
                }
            }
            dippedSconcePendingRestore = false;
            dippedCeilingPendingRestore = false;
        }

        private static Transform FindSceneObjectByName(string exactName)
        {
            Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == exactName)
                {
                    return all[i];
                }
            }
            return null;
        }
    }
}
