using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Canal420.Audio;
using Canal420.Interaction;
using Canal420.UI;

namespace Canal420.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private Transform cameraTransform;

        [Header("Locomoção (Dia 1 - Normal / Burnout)")]
        [SerializeField] private float normalMoveSpeed = 1.815f;
        [SerializeField] private float normalMoveSmoothTime = 0.085f;

        [Header("Substâncias Depressoras (Dia 2 - Lentidão Psicomotora)")]
        [SerializeField] private bool isDepressantActive = false;
        [SerializeField] private float depressantMoveSpeed = 1.242f;
        [SerializeField] private float depressantMoveSmoothTime = 0.30f;
        [SerializeField] private float depressantLookSmoothTime = 0.16f;
        [SerializeField] private float depressantCameraDrop = 0.11f;

        [Header("Substâncias Ativadoras (Dia 3 - Hiperatividade / Alucinógeno)")]
        [SerializeField] private bool isActivatingDrugActive = false;
        [SerializeField] private float activatingDrugMoveSpeed = 2.42f;

        [Header("Câmera e Sensibilidade")]
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float gamepadSensitivity = 120f;
        [SerializeField] private float normalLookSmoothTime = 0.01f;
        [SerializeField] private float maxLookUpAngle = 80f;
        [SerializeField] private float maxLookDownAngle = 80f;

        [Header("Gravidade")]
        [SerializeField] private float gravity = -19.62f;

        [Header("Passos Físicos e Headbob Sincronizados")]
        [SerializeField] private bool enableHeadbob = true;
        [SerializeField] private float normalStepStride = 1.14f;
        [SerializeField] private float normalBobAmplitude = 0.021f;
        [SerializeField] private float depressantStepStride = 0.86f;
        [SerializeField] private float depressantBobAmplitude = 0.032f;

        [Header("Estados de Controle (Debug / Runtime)")]
        [SerializeField] private bool canMove = true;
        [SerializeField] private bool canLook = true;
        [SerializeField] private bool isRestrictedLook = false;
        [SerializeField] private float restrictedMaxYawOffset = 45f;
        [SerializeField] private float restrictedMinPitch = -25f;
        [SerializeField] private float restrictedMaxPitch = 25f;
        private CharacterController characterController;
        private Camera playerCamera;
        private float baseFieldOfView = 60f;

        private float targetYaw;
        private float currentYaw;
        private float yawVelocity;

        private float targetPitch;
        private float currentPitch;
        private float pitchVelocity;

        private float restrictedCenterYaw;

        private Vector2 currentHorizontalVelocity;
        private Vector2 horizontalVelocityRef;
        private float verticalVelocity;

        private Vector3 standingCameraLocalPos = new Vector3(0f, 1.65f, 0f);
        private float activeBaseCameraHeight = 1.65f;
        private float smoothedBaseEyeHeight = 1.65f;
        private bool hasWakeupEyeHeightOverride = false;
        private float wakeupEyeHeightOverride = 1.65f;

        private float stepCyclePhase = Mathf.PI * 0.92f;
        private float bobWeight = 0f;
        private float currentStepRoll = 0f;
        private float lastFrameHorizontalDist = 0f;
        private float lastFrameActualHorizSpeed = 0f;

        private float shakeTimer = 0f;
        private float shakeDuration = 0f;
        private float shakeIntensity = 0f;
        private float externalPitchOffset = 0f;
        private float externalRollOffset = 0f;
        private float externalFovOffset = 0f;
        private float ambientRollOffset = 0f;
        private float ambientFovOffset = 0f;
        private float recoilFovOffset = 0f;
        private float stimulantSpeedBlend = 0f;
        private float chemicalSpeedMultiplier = 1f;
        private float chemicalLookDrag01 = 0f;
        private float chemicalEyeDrop = 0f;
        private float chemicalPitchOffset = 0f;
        private float chemicalRollOffset = 0f;
        private float chemicalFovOffset = 0f;
        private Coroutine quickGlanceCoroutine;

        private bool altFreeCursorHeld = false;

        private struct FloorSlabInfo
        {
            public Collider Collider;
            public Bounds Bounds;
            public bool IsColdTile;
        }

        private readonly List<FloorSlabInfo> cachedFloorSlabs = new List<FloorSlabInfo>(8);
        private bool floorSlabsCached = false;
        private readonly RaycastHit[] footstepHits = new RaycastHit[12];

        public bool IsDepressantActive => isDepressantActive;
        public bool IsActivatingDrugActive => isActivatingDrugActive;
        public float CurrentStimulantSpeedBlend => stimulantSpeedBlend;
        public bool CanMove => canMove;
        public bool CanLook => canLook;
        public bool IsRestrictedLook => isRestrictedLook;
        public bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;
        public Transform CameraTransform => cameraTransform;
        public CharacterController Controller => characterController;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (transform.localScale != Vector3.one)
            {
                transform.localScale = Vector3.one;
            }

            Collider[] childColliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < childColliders.Length; i++)
            {
                if (childColliders[i] != characterController)
                {
                    childColliders[i].enabled = false;
                }
            }

            if (cameraTransform == null)
            {
                Camera childCam = GetComponentInChildren<Camera>();
                if (childCam != null)
                {
                    cameraTransform = childCam.transform;
                }
                else if (Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                    cameraTransform.SetParent(transform, false);
                    cameraTransform.localPosition = new Vector3(0f, 1.65f, 0f);
                    cameraTransform.localRotation = Quaternion.identity;
                }
            }

            if (cameraTransform != null)
            {
                if (cameraTransform.localPosition.y < 0.5f)
                {
                    cameraTransform.localPosition = new Vector3(0f, 1.65f, 0f);
                }
                standingCameraLocalPos = cameraTransform.localPosition;
                activeBaseCameraHeight = standingCameraLocalPos.y;
                smoothedBaseEyeHeight = activeBaseCameraHeight;

                playerCamera = cameraTransform.GetComponent<Camera>();
                if (playerCamera != null)
                {
                    baseFieldOfView = playerCamera.fieldOfView;
                }
            }

            targetYaw = currentYaw = transform.eulerAngles.y;
            restrictedCenterYaw = currentYaw;
            targetPitch = currentPitch = 0f;
            stepCyclePhase = Mathf.PI * 0.92f;
            stimulantSpeedBlend = isActivatingDrugActive ? 1f : 0f;
        }

        private void Start()
        {
            SetCursorLocked(true);
        }

        private void Update()
        {
            HandleEditorCursorRelock();
            HandleCameraLook();
            HandleMovement();
            HandleHeadbobAndFootsteps();
            ApplyFinalCameraRotationAndFov();
        }

        private float shakeClock = 0f;

        private void HandleEditorCursorRelock()
        {
            if (ObjectiveHUD.IsBlockingModalOpen || ObjectiveHUD.WasModalClosedRecently(2) || Time.timeScale <= 0.01f)
            {
                altFreeCursorHeld = false;
                return;
            }

#if UNITY_EDITOR
            Keyboard kb = Keyboard.current;
            bool altHeld = kb != null && (kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);
            if (altHeld && canLook)
            {
                if (!altFreeCursorHeld)
                {
                    altFreeCursorHeld = true;
                    SetCursorLocked(false);
                }
                return;
            }
            else if (altFreeCursorHeld)
            {
                altFreeCursorHeld = false;
                if (canLook && !ObjectiveHUD.IsBlockingModalOpen)
                {
                    SetCursorLocked(true);
                }
                return;
            }
#endif

            if (canLook && Cursor.lockState != CursorLockMode.Locked)
            {
                bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (!pointerOverUI && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    SetCursorLocked(true);
                    if (InteractionSystem.Instance != null)
                    {
                        InteractionSystem.Instance.SuppressInteractionBriefly(0.18f);
                    }
                }
            }
        }

        private void HandleCameraLook()
        {
            if (!canLook || cameraTransform == null || Cursor.lockState != CursorLockMode.Locked)
                return;

            Vector2 lookDelta = ReadLookInput();

            targetYaw += lookDelta.x;
            targetPitch -= lookDelta.y;

            if (isRestrictedLook)
            {
                targetYaw = Mathf.Clamp(
                    targetYaw,
                    restrictedCenterYaw - restrictedMaxYawOffset,
                    restrictedCenterYaw + restrictedMaxYawOffset
                );
                targetPitch = Mathf.Clamp(targetPitch, restrictedMinPitch, restrictedMaxPitch);
            }
            else
            {
                targetPitch = Mathf.Clamp(targetPitch, -maxLookUpAngle, maxLookDownAngle);
            }

            float lookSmooth = isDepressantActive
                ? depressantLookSmoothTime
                : Mathf.Lerp(normalLookSmoothTime, 0.115f, chemicalLookDrag01);

            currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, lookSmooth);
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, lookSmooth);

            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        }

        private void HandleMovement()
        {
            lastFrameHorizontalDist = 0f;
            lastFrameActualHorizSpeed = 0f;

            if (Time.deltaTime <= 0f || characterController == null || !characterController.enabled)
                return;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            Vector2 moveInput = (canMove && !isRestrictedLook) ? ReadMoveInput() : Vector2.zero;

            float targetSpeed = GetCurrentTargetMoveSpeed();
            float moveSmooth = isDepressantActive ? depressantMoveSmoothTime : normalMoveSmoothTime;

            Vector2 targetVelocity = moveInput * targetSpeed;
            currentHorizontalVelocity = Vector2.SmoothDamp(
                currentHorizontalVelocity,
                targetVelocity,
                ref horizontalVelocityRef,
                moveSmooth
            );

            Vector3 worldMove = (transform.right * currentHorizontalVelocity.x) +
                                (transform.forward * currentHorizontalVelocity.y);

            verticalVelocity += gravity * Time.deltaTime;
            worldMove.y = verticalVelocity;

            Vector3 beforePos = transform.position;
            characterController.Move(worldMove * Time.deltaTime);
            Vector3 afterPos = transform.position;

            float dx = afterPos.x - beforePos.x;
            float dz = afterPos.z - beforePos.z;
            lastFrameHorizontalDist = Mathf.Sqrt(dx * dx + dz * dz);
            if (Time.deltaTime > 0.0001f)
            {
                lastFrameActualHorizSpeed = lastFrameHorizontalDist / Time.deltaTime;
            }
        }

        private void HandleHeadbobAndFootsteps()
        {
            if (Time.deltaTime <= 0f || cameraTransform == null)
                return;

            float desiredEyeY;
            if (hasWakeupEyeHeightOverride)
            {
                desiredEyeY = wakeupEyeHeightOverride;
            }
            else
            {
                desiredEyeY = activeBaseCameraHeight;
                if (isDepressantActive && !isRestrictedLook)
                {
                    desiredEyeY -= depressantCameraDrop;
                }
                else if (chemicalEyeDrop > 0.0001f && !isRestrictedLook)
                {
                    desiredEyeY -= chemicalEyeDrop;
                }
            }

            smoothedBaseEyeHeight = Mathf.Lerp(smoothedBaseEyeHeight, desiredEyeY, Time.deltaTime * 7f);

            bool groundedAndMobile = canMove
                && !isRestrictedLook
                && characterController != null
                && characterController.enabled
                && characterController.isGrounded;

            bool activelyStepping = groundedAndMobile && lastFrameActualHorizSpeed > 0.22f && currentHorizontalVelocity.magnitude > 0.20f;

            if (activelyStepping)
            {
                float stride = isDepressantActive ? depressantStepStride : normalStepStride;
                float amp = isDepressantActive ? depressantBobAmplitude : normalBobAmplitude;

                bobWeight = Mathf.MoveTowards(bobWeight, 1f, Time.deltaTime * 8.0f);

                float prevPhase = stepCyclePhase;
                stepCyclePhase += (lastFrameHorizontalDist / Mathf.Max(0.30f, stride)) * (Mathf.PI * 2f);

                int prevFootfallCount = Mathf.FloorToInt((prevPhase - Mathf.PI * 1.5f) / (Mathf.PI * 2f));
                int currFootfallCount = Mathf.FloorToInt((stepCyclePhase - Mathf.PI * 1.5f) / (Mathf.PI * 2f));
                if (currFootfallCount > prevFootfallCount)
                {
                    TriggerSurfaceFootstep();
                }

                if (stepCyclePhase > Mathf.PI * 100f)
                {
                    stepCyclePhase -= Mathf.PI * 80f;
                }

                if (enableHeadbob)
                {
                    float sinVertical = Mathf.Sin(stepCyclePhase);
                    float bobOffsetY = sinVertical * amp * bobWeight;
                    float bobOffsetX = Mathf.Sin(stepCyclePhase * 0.5f) * (amp * 0.52f) * bobWeight;
                    float rollTarget = Mathf.Cos(stepCyclePhase * 0.5f) * (isDepressantActive ? 1.05f : 0.42f) * bobWeight;
                    currentStepRoll = Mathf.Lerp(currentStepRoll, rollTarget, Time.deltaTime * 14f);

                    cameraTransform.localPosition = new Vector3(
                        standingCameraLocalPos.x + bobOffsetX,
                        smoothedBaseEyeHeight + bobOffsetY,
                        standingCameraLocalPos.z
                    );
                }
                else
                {
                    currentStepRoll = Mathf.Lerp(currentStepRoll, 0f, Time.deltaTime * 10f);
                    cameraTransform.localPosition = new Vector3(standingCameraLocalPos.x, smoothedBaseEyeHeight, standingCameraLocalPos.z);
                }
            }
            else
            {
                bobWeight = Mathf.MoveTowards(bobWeight, 0f, Time.deltaTime * 6.5f);
                currentStepRoll = Mathf.Lerp(currentStepRoll, 0f, Time.deltaTime * 8f);

                if (bobWeight <= 0.02f)
                {
                    stepCyclePhase = Mathf.PI * 0.92f;
                }

                float breathFreq = isDepressantActive ? 1.35f : 1.75f;
                float breathAmp = isDepressantActive ? 0.0065f : 0.0038f;
                float breathOffsetY = Mathf.Sin(Time.time * breathFreq) * breathAmp * (1f - bobWeight);
                float residualBobY = enableHeadbob
                    ? Mathf.Sin(stepCyclePhase) * (isDepressantActive ? depressantBobAmplitude : normalBobAmplitude) * bobWeight
                    : 0f;

                cameraTransform.localPosition = new Vector3(
                    standingCameraLocalPos.x,
                    smoothedBaseEyeHeight + breathOffsetY + residualBobY,
                    standingCameraLocalPos.z
                );
            }
        }

        private void ApplyFinalCameraRotationAndFov()
        {
            if (cameraTransform == null)
                return;

            float shakePitch = 0f;
            float shakeYaw = 0f;
            float shakeRoll = 0f;

            if (shakeTimer > 0f && Time.deltaTime > 0f)
            {
                shakeTimer -= Time.deltaTime;
                shakeClock += Time.deltaTime;
                float norm = (shakeDuration > 0.001f) ? Mathf.Clamp01(shakeTimer / shakeDuration) : 0f;
                float damper = norm * norm;
                float t = shakeClock * 34f;
                shakePitch = (Mathf.PerlinNoise(t, 0.13f) - 0.5f) * 2f * shakeIntensity * damper;
                shakeYaw = (Mathf.PerlinNoise(0.71f, t) - 0.5f) * 2f * shakeIntensity * 0.75f * damper;
                shakeRoll = (Mathf.PerlinNoise(t * 0.8f, t * 0.4f) - 0.5f) * 2f * shakeIntensity * 1.15f * damper;
            }

            float finalPitch = Mathf.Clamp(currentPitch + externalPitchOffset + chemicalPitchOffset + shakePitch, -88f, 88f);
            float finalRoll = currentStepRoll + externalRollOffset + ambientRollOffset + chemicalRollOffset + shakeRoll;

            cameraTransform.localRotation = Quaternion.Euler(finalPitch, shakeYaw, finalRoll);

            if (playerCamera != null)
            {
                float desiredFov = Mathf.Clamp(baseFieldOfView + externalFovOffset + ambientFovOffset + chemicalFovOffset + recoilFovOffset, 34f, 82f);
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, desiredFov, Time.deltaTime * 12f);
            }
        }

        private void CacheFloorSlabsIfNeeded()
        {
            if (floorSlabsCached)
                return;

            floorSlabsCached = true;
            cachedFloorSlabs.Clear();

            GameObject casaBase = GameObject.Find("Casa_Base");
            Collider[] candidateColliders = casaBase != null
                ? casaBase.GetComponentsInChildren<Collider>(true)
                : FindObjectsByType<Collider>(FindObjectsInactive.Include);

            for (int i = 0; i < candidateColliders.Length; i++)
            {
                Collider col = candidateColliders[i];
                if (col == null || col.isTrigger)
                    continue;

                if (TryClassifyFloorSlab(col, out bool coldTile))
                {
                    cachedFloorSlabs.Add(new FloorSlabInfo
                    {
                        Collider = col,
                        Bounds = col.bounds,
                        IsColdTile = coldTile
                    });
                }
            }
        }

        private static bool TryClassifyFloorSlab(Collider col, out bool isColdTile)
        {
            isColdTile = false;
            if (col == null)
                return false;

            string objName = col.gameObject.name;
            if (objName.StartsWith("Piso_Frio"))
            {
                isColdTile = true;
                return true;
            }
            if (objName.StartsWith("Piso_Madeira") || objName == "Base1")
            {
                isColdTile = false;
                return true;
            }

            if (col.TryGetComponent<Renderer>(out Renderer r) && r.sharedMaterial != null)
            {
                string matName = r.sharedMaterial.name;
                if (matName.Contains("Floor_Tile"))
                {
                    isColdTile = true;
                    return true;
                }
                if (matName.Contains("Floor_Wood"))
                {
                    isColdTile = false;
                    return true;
                }
            }

            return false;
        }

        private void TriggerSurfaceFootstep()
        {
            if (Canal420AudioManager.Instance == null)
                return;

            CacheFloorSlabsIfNeeded();
            Vector3 p = transform.position;
            bool resolved = false;
            bool isColdTile = false;

            int hitCount = Physics.RaycastNonAlloc(
                p + Vector3.up * 0.25f,
                Vector3.down,
                footstepHits,
                0.75f,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            float bestFloorDist = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = footstepHits[i].collider;
                if (col == null || col == characterController || col.transform.IsChildOf(transform))
                    continue;

                if (TryClassifyFloorSlab(col, out bool cold) && footstepHits[i].distance < bestFloorDist)
                {
                    bestFloorDist = footstepHits[i].distance;
                    isColdTile = cold;
                    resolved = true;
                }
            }

            if (!resolved && cachedFloorSlabs.Count > 0)
            {
                for (int i = 0; i < cachedFloorSlabs.Count; i++)
                {
                    FloorSlabInfo slab = cachedFloorSlabs[i];
                    if (slab.Collider == null || !slab.Collider.enabled)
                        continue;

                    Bounds b = slab.Bounds;
                    if (p.x >= b.min.x - 0.002f && p.x <= b.max.x + 0.002f &&
                        p.z >= b.min.z - 0.002f && p.z <= b.max.z + 0.002f)
                    {
                        isColdTile = slab.IsColdTile;
                        resolved = true;
                        break;
                    }
                }
            }

            if (!resolved)
            {
                // Fallback estrito às lajes da planta (Cozinha [-6.70..-2.86]x[-6.00..0.46], Banheiro [-6.70..-3.44]x[2.10..6.20])
                isColdTile = (p.x >= -6.70f && p.x <= -2.86f && p.z >= -6.00f && p.z <= 0.46f)
                          || (p.x >= -6.70f && p.x <= -3.44f && p.z >= 2.10f && p.z <= 6.20f);
            }

            Canal420AudioManager.Instance.PlayFootstep(isColdTile, isDepressantActive);
        }

        private Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
            }

            if (Gamepad.current != null && input.sqrMagnitude < 0.01f)
            {
                input = Gamepad.current.leftStick.ReadValue();
            }

            return Vector2.ClampMagnitude(input, 1f);
        }

        private Vector2 ReadLookInput()
        {
            Vector2 delta = Vector2.zero;

            if (Mouse.current != null)
            {
                delta += Mouse.current.delta.ReadValue() * mouseSensitivity;
            }

            if (Gamepad.current != null)
            {
                delta += Gamepad.current.rightStick.ReadValue() * (gamepadSensitivity * Time.deltaTime);
            }

            return delta;
        }

        /// <summary>
        /// Resolves the target horizontal speed according to the active pharmacological/day state.
        /// </summary>
        private float GetCurrentTargetMoveSpeed()
        {
            if (isDepressantActive)
            {
                return depressantMoveSpeed;
            }

            float baseSpeed = Mathf.Lerp(normalMoveSpeed, activatingDrugMoveSpeed, stimulantSpeedBlend);
            return baseSpeed * chemicalSpeedMultiplier;
        }

        public void SetDepressantEffect(bool active)
        {
            isDepressantActive = active;
            if (active)
            {
                isActivatingDrugActive = false;
                stimulantSpeedBlend = 0f;
                ResetChemicalEffectsImmediate();
            }
        }

        /// <summary>
        /// Activates or deactivates the Day 3 stimulant locomotion speed while preserving normal posture/look responsiveness.
        /// </summary>
        public void SetActivatingDrugEffect(bool active)
        {
            isActivatingDrugActive = active;
            stimulantSpeedBlend = active ? 1f : 0f;
            chemicalSpeedMultiplier = 1f;
            if (active)
            {
                isDepressantActive = false;
            }
            else
            {
                chemicalLookDrag01 = 0f;
                chemicalEyeDrop = 0f;
                chemicalPitchOffset = 0f;
                chemicalRollOffset = 0f;
                chemicalFovOffset = 0f;
            }
        }

        /// <summary>
        /// Applies continuous zero-allocation stimulant/rebound modifiers (speed blend, camera weight, subtle FOV narrowing).
        /// </summary>
        public void SetChemicalEffectModifiers(
            float speedBlend01,
            float speedMultiplier,
            float lookDrag01,
            float eyeDropMeters,
            float pitchOffsetDeg,
            float rollOffsetDeg,
            float fovOffsetDeg)
        {
            stimulantSpeedBlend = Mathf.Clamp01(speedBlend01);
            isActivatingDrugActive = stimulantSpeedBlend > 0.01f && !isDepressantActive;
            chemicalSpeedMultiplier = Mathf.Clamp(speedMultiplier, 0.72f, 1.15f);
            chemicalLookDrag01 = Mathf.Clamp01(lookDrag01);
            chemicalEyeDrop = Mathf.Clamp(eyeDropMeters, 0f, 0.08f);
            chemicalPitchOffset = Mathf.Clamp(pitchOffsetDeg, -4f, 5f);
            chemicalRollOffset = Mathf.Clamp(rollOffsetDeg, -3.5f, 3.5f);
            chemicalFovOffset = Mathf.Clamp(fovOffsetDeg, -8f, 4f);
        }

        public void ResetChemicalEffectsImmediate()
        {
            stimulantSpeedBlend = isActivatingDrugActive ? 1f : 0f;
            chemicalSpeedMultiplier = 1f;
            chemicalLookDrag01 = 0f;
            chemicalEyeDrop = 0f;
            chemicalPitchOffset = 0f;
            chemicalRollOffset = 0f;
            chemicalFovOffset = 0f;
        }

        public void TriggerCameraShake(float intensityDegrees = 2.2f, float duration = 0.35f)
        {
            shakeIntensity = Mathf.Max(shakeIntensity * 0.5f, intensityDegrees);
            shakeDuration = Mathf.Max(0.05f, duration);
            shakeTimer = shakeDuration;
        }

        public void SetPostureAndFovOffset(float pitchOffsetDeg, float rollOffsetDeg, float fovOffsetDeg, bool snapFov = false)
        {
            externalPitchOffset = pitchOffsetDeg;
            externalRollOffset = rollOffsetDeg;
            externalFovOffset = fovOffsetDeg;

            if (snapFov && playerCamera != null)
            {
                playerCamera.fieldOfView = Mathf.Clamp(baseFieldOfView + externalFovOffset, 38f, 82f);
            }
        }

        public void ResetPostureAndFovOffsets(bool snap = false)
        {
            externalPitchOffset = 0f;
            externalRollOffset = 0f;
            externalFovOffset = 0f;
            if (snap && playerCamera != null)
            {
                ambientRollOffset = 0f;
                ambientFovOffset = 0f;
                recoilFovOffset = 0f;
                playerCamera.fieldOfView = baseFieldOfView;
            }
        }

        public void ResetTransientCameraEffects()
        {
            if (quickGlanceCoroutine != null)
            {
                StopCoroutine(quickGlanceCoroutine);
                quickGlanceCoroutine = null;
            }

            shakeTimer = 0f;
            shakeDuration = 0.05f;
            shakeIntensity = 0f;
            recoilFovOffset = 0f;
            bobWeight = 0f;
            currentStepRoll = 0f;
            yawVelocity = 0f;
            pitchVelocity = 0f;
            currentHorizontalVelocity = Vector2.zero;
            horizontalVelocityRef = Vector2.zero;
            ambientRollOffset = 0f;
            ambientFovOffset = 0f;
            ResetChemicalEffectsImmediate();
            ResetPostureAndFovOffsets(snap: true);
        }

        public void SetCharacterControllerEnabledForCutscene(bool enabled)
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
            if (characterController != null)
            {
                characterController.enabled = enabled;
                if (enabled)
                {
                    Physics.SyncTransforms();
                }
            }
        }

        public void SetHorrorCameraInfluence(float rollDeg, float fovOffsetDeg)
        {
            ambientRollOffset = Mathf.Clamp(rollDeg, -3.5f, 3.5f);
            ambientFovOffset = Mathf.Clamp(fovOffsetDeg, -14f, 6f);
        }

        public void TriggerQuickRecoilGlance(Vector3 worldLookPoint, float duration = 0.13f, float fovPunchDelta = -4.5f)
        {
            if (isRestrictedLook) return;
            if (quickGlanceCoroutine != null)
            {
                StopCoroutine(quickGlanceCoroutine);
            }
            quickGlanceCoroutine = StartCoroutine(QuickRecoilGlanceRoutine(worldLookPoint, duration, fovPunchDelta));
        }

        private System.Collections.IEnumerator QuickRecoilGlanceRoutine(Vector3 worldLookPoint, float duration, float fovPunchDelta)
        {
            Vector3 eyeOrigin = (cameraTransform != null)
                ? cameraTransform.position
                : (transform.position + Vector3.up * activeBaseCameraHeight);

            Vector3 dir = worldLookPoint - eyeOrigin;
            if (dir.sqrMagnitude < 0.002f)
            {
                quickGlanceCoroutine = null;
                yield break;
            }

            Quaternion worldRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            float goalYaw = worldRot.eulerAngles.y;
            float goalPitch = worldRot.eulerAngles.x;
            if (goalPitch > 180f) goalPitch -= 360f;
            goalPitch = Mathf.Clamp(goalPitch, -maxLookUpAngle, maxLookDownAngle);

            float startYaw = currentYaw;
            float startPitch = currentPitch;
            float totalDur = Mathf.Clamp(duration, 0.05f, 0.28f);
            float elapsed = 0f;

            while (elapsed < totalDur)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / totalDur);
                float eased = 1f - Mathf.Pow(1f - u, 3f);

                currentYaw = Mathf.LerpAngle(startYaw, goalYaw, eased);
                targetYaw = currentYaw;
                currentPitch = Mathf.LerpAngle(startPitch, goalPitch, eased);
                targetPitch = currentPitch;
                recoilFovOffset = Mathf.Lerp(0f, fovPunchDelta, eased);
                transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
                yield return null;
            }

            // Relax fov punch back smoothly without locking controls
            float settle = 0f;
            float settleDur = 0.25f;
            while (settle < settleDur)
            {
                settle += Time.deltaTime;
                recoilFovOffset = Mathf.Lerp(fovPunchDelta, 0f, settle / settleDur);
                yield return null;
            }
            recoilFovOffset = 0f;
            quickGlanceCoroutine = null;
        }

        public IEnumerator SmoothLookAtRoutine(Vector3 worldLookPoint, float duration, float targetFovDelta = 0f)
        {
            Vector3 eyeOrigin = (cameraTransform != null)
                ? cameraTransform.position
                : transform.position + new Vector3(0f, smoothedBaseEyeHeight, 0f);

            Vector3 dir = worldLookPoint - eyeOrigin;
            if (dir.sqrMagnitude < 0.0001f)
                yield break;

            Quaternion lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            float rawYaw = lookRot.eulerAngles.y;
            float rawPitch = lookRot.eulerAngles.x;
            if (rawPitch > 180f) rawPitch -= 360f;
            rawPitch = Mathf.Clamp(rawPitch, -maxLookUpAngle, maxLookDownAngle);

            float startYaw = currentYaw;
            float endYaw = startYaw + Mathf.DeltaAngle(startYaw, rawYaw);
            float startPitch = currentPitch;
            float startFovOffset = externalFovOffset;

            yawVelocity = 0f;
            pitchVelocity = 0f;

            float dur = Mathf.Max(0.05f, duration);
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / dur);

                float eased = u * u * (3f - 2f * u);

                currentYaw = targetYaw = Mathf.Lerp(startYaw, endYaw, eased);
                currentPitch = targetPitch = Mathf.Lerp(startPitch, rawPitch, eased);
                restrictedCenterYaw = currentYaw;
                externalFovOffset = Mathf.Lerp(startFovOffset, targetFovDelta, eased);

                transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
                yield return null;
            }

            currentYaw = targetYaw = endYaw;
            currentPitch = targetPitch = rawPitch;
            restrictedCenterYaw = currentYaw;
            externalFovOffset = targetFovDelta;
            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        }

        public IEnumerator SmoothSitAtPointRoutine(Transform seatPoint, Vector3 worldLookPoint, float seatedEyeHeight, float duration = 0.65f)
        {
            if (seatPoint == null)
                yield break;

            canMove = false;
            canLook = false;
            isRestrictedLook = true;
            currentHorizontalVelocity = Vector2.zero;
            horizontalVelocityRef = Vector2.zero;
            verticalVelocity = 0f;

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.ClothInspect, 0.78f, 0.92f);
            }

            Vector3 startPos = transform.position;
            Vector3 endPos = seatPoint.position;
            float startEye = smoothedBaseEyeHeight;

            Vector3 finalEyeOrigin = endPos + new Vector3(0f, seatedEyeHeight, 0f);
            Vector3 lookDir = worldLookPoint - finalEyeOrigin;
            Quaternion lookRot = lookDir.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(lookDir.normalized, Vector3.up)
                : seatPoint.rotation;

            float rawYaw = lookRot.eulerAngles.y;
            float rawPitch = lookRot.eulerAngles.x;
            if (rawPitch > 180f) rawPitch -= 360f;
            rawPitch = Mathf.Clamp(rawPitch, -30f, 30f);

            float startYaw = currentYaw;
            float endYaw = startYaw + Mathf.DeltaAngle(startYaw, rawYaw);
            float startPitch = currentPitch;

            yawVelocity = 0f;
            pitchVelocity = 0f;

            float dur = Mathf.Max(0.15f, duration);
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / dur);
                float eased = u * u * (3f - 2f * u);

                float cushionSink = Mathf.Sin(u * Mathf.PI) * -0.035f * u;

                transform.position = Vector3.Lerp(startPos, endPos, eased);
                activeBaseCameraHeight = Mathf.Lerp(startEye, seatedEyeHeight, eased) + cushionSink;
                smoothedBaseEyeHeight = activeBaseCameraHeight;

                currentYaw = targetYaw = Mathf.Lerp(startYaw, endYaw, eased);
                currentPitch = targetPitch = Mathf.Lerp(startPitch, rawPitch, eased);
                restrictedCenterYaw = currentYaw;

                transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
                if (cameraTransform != null)
                {
                    cameraTransform.localPosition = new Vector3(standingCameraLocalPos.x, smoothedBaseEyeHeight, standingCameraLocalPos.z);
                }

                yield return null;
            }

            transform.position = endPos;
            activeBaseCameraHeight = seatedEyeHeight;
            smoothedBaseEyeHeight = seatedEyeHeight;
            currentYaw = targetYaw = endYaw;
            currentPitch = targetPitch = rawPitch;
            restrictedCenterYaw = currentYaw;
            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
            Physics.SyncTransforms();
        }

        public void SetControlsEnabled(bool moveEnabled, bool lookEnabled, bool lockCursor = true)
        {
            canMove = isRestrictedLook ? false : moveEnabled;
            canLook = lookEnabled;

            if (canMove && !isRestrictedLook && characterController != null)
            {
                characterController.enabled = true;
            }

            if (!canMove)
            {
                currentHorizontalVelocity = Vector2.zero;
                horizontalVelocityRef = Vector2.zero;
            }

            SetCursorLocked(lockCursor);
        }

        public void EnableRestrictedLook(float maxYawOffset = 45f, float minPitch = -25f, float maxPitch = 25f, float customEyeHeight = -1f)
        {
            canMove = false;
            canLook = true;
            isRestrictedLook = true;

            currentHorizontalVelocity = Vector2.zero;
            horizontalVelocityRef = Vector2.zero;
            verticalVelocity = 0f;

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            restrictedCenterYaw = currentYaw;
            restrictedMaxYawOffset = maxYawOffset;
            restrictedMinPitch = minPitch;
            restrictedMaxPitch = maxPitch;

            if (customEyeHeight > 0.2f)
            {
                activeBaseCameraHeight = customEyeHeight;
                smoothedBaseEyeHeight = customEyeHeight;
            }
        }

        public void DisableRestrictedLook()
        {
            isRestrictedLook = false;
            canMove = true;
            canLook = true;
            hasWakeupEyeHeightOverride = false;
            activeBaseCameraHeight = standingCameraLocalPos.y;

            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }

        public void SetWakeupEyeHeightImmediate(float eyeHeight)
        {
            float clamped = Mathf.Clamp(eyeHeight, 0.22f, 1.85f);
            hasWakeupEyeHeightOverride = true;
            wakeupEyeHeightOverride = clamped;
            activeBaseCameraHeight = clamped;
            smoothedBaseEyeHeight = clamped;

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = new Vector3(standingCameraLocalPos.x, smoothedBaseEyeHeight, standingCameraLocalPos.z);
            }
        }

        public void SetWakeupEyeHeightTarget(float eyeHeight, bool snapCameraY = false)
        {
            float clamped = Mathf.Clamp(eyeHeight, 0.22f, 1.85f);
            hasWakeupEyeHeightOverride = true;
            wakeupEyeHeightOverride = clamped;
            activeBaseCameraHeight = clamped;
            if (snapCameraY)
            {
                smoothedBaseEyeHeight = clamped;
                if (cameraTransform != null)
                {
                    cameraTransform.localPosition = new Vector3(standingCameraLocalPos.x, smoothedBaseEyeHeight, standingCameraLocalPos.z);
                }
            }
        }

        public void UpdateWakeupStandEase(float seatedEyeHeight, float progress01)
        {
            float standingTarget = isDepressantActive
                ? (standingCameraLocalPos.y - depressantCameraDrop)
                : standingCameraLocalPos.y;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress01));
            float blended = Mathf.Lerp(seatedEyeHeight, standingTarget, eased);

            hasWakeupEyeHeightOverride = true;
            wakeupEyeHeightOverride = blended;
            activeBaseCameraHeight = blended;
        }

        public void FreezeCurrentEyeHeightForFade()
        {
            float clamped = Mathf.Clamp(smoothedBaseEyeHeight, 0.22f, 1.85f);
            hasWakeupEyeHeightOverride = true;
            wakeupEyeHeightOverride = clamped;
            activeBaseCameraHeight = clamped;
        }

        public void ClearWakeupEyeHeightOverride()
        {
            hasWakeupEyeHeightOverride = false;
            activeBaseCameraHeight = standingCameraLocalPos.y;
        }

        public void FaceWorldTarget(Vector3 worldLookPoint)
        {
            Vector3 eyeOrigin = transform.position + new Vector3(0f, activeBaseCameraHeight, 0f);
            Vector3 dir = worldLookPoint - eyeOrigin;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            float yaw = lookRot.eulerAngles.y;
            float pitch = lookRot.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            targetYaw = currentYaw = yaw;
            restrictedCenterYaw = yaw;
            targetPitch = currentPitch = Mathf.Clamp(pitch, -maxLookDownAngle, maxLookDownAngle);
            yawVelocity = 0f;
            pitchVelocity = 0f;

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
            }
        }

        public void TeleportTo(Transform targetSpawnPoint)
        {
            if (targetSpawnPoint == null)
                return;

            TeleportTo(targetSpawnPoint.position, targetSpawnPoint.rotation);
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            if (characterController == null) characterController = GetComponent<CharacterController>();

            bool wasEnabled = characterController.enabled;
            characterController.enabled = false;

            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);

            targetYaw = currentYaw = rotation.eulerAngles.y;
            restrictedCenterYaw = currentYaw;

            float initialPitch = rotation.eulerAngles.x;
            if (initialPitch > 180f) initialPitch -= 360f;
            targetPitch = currentPitch = Mathf.Clamp(initialPitch, -maxLookUpAngle, maxLookDownAngle);

            yawVelocity = 0f;
            pitchVelocity = 0f;
            currentHorizontalVelocity = Vector2.zero;
            horizontalVelocityRef = Vector2.zero;
            verticalVelocity = 0f;
            hasWakeupEyeHeightOverride = false;
            activeBaseCameraHeight = standingCameraLocalPos.y;
            smoothedBaseEyeHeight = isDepressantActive
                ? (standingCameraLocalPos.y - depressantCameraDrop)
                : standingCameraLocalPos.y;
            stepCyclePhase = Mathf.PI * 0.92f;
            bobWeight = 0f;
            currentStepRoll = 0f;

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
                cameraTransform.localPosition = new Vector3(standingCameraLocalPos.x, smoothedBaseEyeHeight, standingCameraLocalPos.z);
            }

            Physics.SyncTransforms();
            characterController.enabled = wasEnabled;
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
