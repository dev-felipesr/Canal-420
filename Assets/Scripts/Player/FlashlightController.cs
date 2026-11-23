using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Canal420.Audio;
using Canal420.Core;
using Canal420.UI;

namespace Canal420.Player
{
    public class FlashlightController : MonoBehaviour
    {
        [Header("Configuração do Feixe (Estética Retro Anos 90)")]
        [SerializeField] private Color beamColor = new Color(1.00f, 0.90f, 0.72f);
        [SerializeField] private float baseIntensity = 2.55f;
        [SerializeField] private float beamRange = 10.5f;
        [SerializeField] private float spotAngle = 48f;
        [SerializeField] private float innerSpotAngle = 19f;
        [SerializeField] private Vector3 handLocalOffset = new Vector3(0.18f, -0.17f, 0.22f);

        [Header("Atraso e Balanço Manual")]
        [SerializeField] private float aimFollowSpeed = 13.5f;
        [SerializeField] private float idleSwayAmplitudeDeg = 0.45f;
        [SerializeField] private float idleSwayFrequency = 1.45f;

        private PlayerController player;
        private Transform cameraTransform;
        private Transform pivotTransform;
        private Light spotLight;

        private bool hasFlashlight = false;
        private bool isLightOn = false;
        private bool forceBlackedOut = false;
        private bool inClimaxOverride = false;
        private bool softShadows = true;
        private float currentFlickerMult = 1f;
        private Quaternion smoothedWorldRot = Quaternion.identity;
        private bool hasInitializedRotation = false;
        private Coroutine flickerCoroutine;
        private Coroutine climaxTrembleCoroutine;

        // First-pickup instruction pill positioned right in front of the flashlight side ("[F] Ligar/Desligar")
        private GameObject hintCanvasObj;
        private CanvasGroup hintCanvasGroup;
        private bool hasShownFirstPickupHint = false;
        private float hintRemainingTimer = 0f;
        private float hintCurrentAlpha = 0f;

        public bool HasFlashlight => hasFlashlight;
        public bool IsLightOn => hasFlashlight && isLightOn && !forceBlackedOut;
        public bool IsClimaxOverrideActive => inClimaxOverride;
        public Light FlashlightSpotLight => spotLight;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            ResolveCameraAndEnsureRig();
            ApplyLightStateImmediate();
        }

        private void OnDisable()
        {
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }
            if (climaxTrembleCoroutine != null)
            {
                StopCoroutine(climaxTrembleCoroutine);
                climaxTrembleCoroutine = null;
            }
            inClimaxOverride = false;
            currentFlickerMult = 1f;
            HideHintImmediate();
        }

        private void OnDestroy()
        {
            if (hintCanvasObj != null)
            {
                if (Application.isPlaying) Destroy(hintCanvasObj);
                else DestroyImmediate(hintCanvasObj);
                hintCanvasObj = null;
            }
        }

        public void ResolveCameraAndEnsureRig()
        {
            if (player == null)
            {
                player = GetComponent<PlayerController>();
            }

            if (cameraTransform == null)
            {
                if (player != null && player.CameraTransform != null)
                {
                    cameraTransform = player.CameraTransform;
                }
                else
                {
                    Camera childCam = GetComponentInChildren<Camera>(true);
                    if (childCam != null) cameraTransform = childCam.transform;
                }
            }

            if (cameraTransform == null) return;

            Transform existingPivot = cameraTransform.Find("Lanterna_Mao_Pivot");
            if (existingPivot != null)
            {
                pivotTransform = existingPivot;
            }
            else
            {
                GameObject pivotGo = new GameObject("Lanterna_Mao_Pivot");
                pivotTransform = pivotGo.transform;
                pivotTransform.SetParent(cameraTransform, false);
            }

            pivotTransform.localPosition = handLocalOffset;
            pivotTransform.localRotation = Quaternion.identity;
            pivotTransform.localScale = Vector3.one;

            spotLight = pivotTransform.GetComponent<Light>();
            if (spotLight == null)
            {
                spotLight = pivotTransform.gameObject.AddComponent<Light>();
            }

            spotLight.type = LightType.Spot;
            spotLight.color = beamColor;
            spotLight.range = beamRange;
            spotLight.spotAngle = spotAngle;
            spotLight.innerSpotAngle = innerSpotAngle;
            spotLight.shadows = softShadows ? LightShadows.Soft : LightShadows.Hard;
            spotLight.shadowNearPlane = 0.08f;
            spotLight.shadowBias = 0.03f;
            spotLight.shadowNormalBias = 0.25f;
            spotLight.intensity = 0f;
            spotLight.enabled = false;
        }

        public void SetEquipped(bool equipped, bool turnOnImmediately = false, bool playSound = false)
        {
            ResolveCameraAndEnsureRig();

            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }
            if (climaxTrembleCoroutine != null)
            {
                StopCoroutine(climaxTrembleCoroutine);
                climaxTrembleCoroutine = null;
            }

            bool newlyEquipped = equipped && !hasFlashlight;
            hasFlashlight = equipped;
            forceBlackedOut = false;
            inClimaxOverride = false;
            currentFlickerMult = 1f;
            isLightOn = equipped && turnOnImmediately;

            if (!equipped)
            {
                hasShownFirstPickupHint = false;
                HideHintImmediate();
            }
            else if (newlyEquipped && !hasShownFirstPickupHint && Application.isPlaying)
            {
                hasShownFirstPickupHint = true;
                ShowFirstPickupHint();
            }

            if (cameraTransform != null)
            {
                smoothedWorldRot = cameraTransform.rotation;
                hasInitializedRotation = true;
            }

            ApplyLightStateImmediate();

            if (playSound && Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.FlashlightClick, 0.85f, isLightOn ? 1.04f : 0.92f);
            }
        }

        public void SetLightOn(bool on, bool playSound = true)
        {
            if (!hasFlashlight || forceBlackedOut) return;

            ResolveCameraAndEnsureRig();
            bool changed = isLightOn != on;
            isLightOn = on;
            ApplyLightStateImmediate();

            if (changed && hintRemainingTimer > 1.2f)
            {
                hintRemainingTimer = 1.2f;
            }

            if (changed && playSound && Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.FlashlightClick, 0.82f, isLightOn ? 1.04f : 0.90f);
            }
        }

        public void SetShadowQuality(bool useSoftShadows)
        {
            softShadows = useSoftShadows;
            if (spotLight != null)
            {
                spotLight.shadows = softShadows ? LightShadows.Soft : LightShadows.Hard;
            }
        }

        public void ForceBlackout(bool blackedOut)
        {
            forceBlackedOut = blackedOut;
            if (blackedOut)
            {
                if (flickerCoroutine != null)
                {
                    StopCoroutine(flickerCoroutine);
                    flickerCoroutine = null;
                }
                if (climaxTrembleCoroutine != null)
                {
                    StopCoroutine(climaxTrembleCoroutine);
                    climaxTrembleCoroutine = null;
                }
                HideHintImmediate();
            }
            else
            {
                currentFlickerMult = 1f;
            }
            ApplyLightStateImmediate();
        }

        public void TriggerFlickerBurst(float duration = 0.65f, bool endOff = false)
        {
            if (!hasFlashlight || !isActiveAndEnabled) return;
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
            }
            flickerCoroutine = StartCoroutine(FlickerBurstRoutine(duration, endOff));
        }

        private IEnumerator FlickerBurstRoutine(float duration, bool endOff)
        {
            float elapsed = 0f;
            float total = Mathf.Max(0.12f, duration);
            while (elapsed < total)
            {
                elapsed += Time.deltaTime;
                float roll = Mathf.PerlinNoise(elapsed * 26f, 1.37f);
                currentFlickerMult = roll > 0.42f ? Mathf.Lerp(0.18f, 1.05f, roll) : 0f;
                ApplyLightStateImmediate();
                yield return null;
            }

            currentFlickerMult = 1f;
            if (endOff)
            {
                isLightOn = false;
            }
            ApplyLightStateImmediate();
            flickerCoroutine = null;
        }

        /// <summary>
        /// Phase 3 climax choreography: flashlight sputters twice with realistic filament drop-outs
        /// and then dies into total blackout for blackoutDuration seconds (default 0.80s).
        /// </summary>
        public IEnumerator RunClimaxDoubleFlickerAndBlackoutRoutine(float blackoutDuration = 0.80f)
        {
            ResolveCameraAndEnsureRig();
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }
            if (climaxTrembleCoroutine != null)
            {
                StopCoroutine(climaxTrembleCoroutine);
                climaxTrembleCoroutine = null;
            }

            inClimaxOverride = true;
            HideHintImmediate();

            if (!hasFlashlight)
            {
                forceBlackedOut = true;
                ApplyLightStateImmediate();
                yield return new WaitForSeconds(Mathf.Max(0.10f, blackoutDuration));
                yield break;
            }

            forceBlackedOut = false;
            isLightOn = true;

            // Flicker 1: sudden dip -> drop -> partial recovery
            currentFlickerMult = 0.16f;
            ApplyLightStateImmediate();
            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.BulbFlickerBuzz, 0.36f, 1.18f);
            }
            yield return new WaitForSeconds(0.065f);

            currentFlickerMult = 0f;
            ApplyLightStateImmediate();
            yield return new WaitForSeconds(0.075f);

            currentFlickerMult = 0.84f;
            ApplyLightStateImmediate();
            yield return new WaitForSeconds(0.135f);

            // Flicker 2: double sputter -> total cut
            currentFlickerMult = 0.22f;
            ApplyLightStateImmediate();
            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.FlashlightClick, 0.42f, 0.82f);
            }
            yield return new WaitForSeconds(0.055f);

            currentFlickerMult = 0f;
            ApplyLightStateImmediate();
            yield return new WaitForSeconds(0.060f);

            currentFlickerMult = 0.48f;
            ApplyLightStateImmediate();
            yield return new WaitForSeconds(0.050f);

            // Total flashlight blackout for blackoutDuration (0.80s)
            currentFlickerMult = 0f;
            forceBlackedOut = true;
            ApplyLightStateImmediate();
            yield return new WaitForSeconds(Mathf.Max(0.10f, blackoutDuration));
        }

        /// <summary>
        /// Brings the flashlight back in a weak, trembling dying beam during the climax suspense hold.
        /// </summary>
        public void StartClimaxWeakTremble(float minMult = 0.24f, float maxMult = 0.56f)
        {
            ResolveCameraAndEnsureRig();
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }
            if (climaxTrembleCoroutine != null)
            {
                StopCoroutine(climaxTrembleCoroutine);
                climaxTrembleCoroutine = null;
            }

            inClimaxOverride = true;
            HideHintImmediate();

            if (!hasFlashlight)
            {
                forceBlackedOut = true;
                ApplyLightStateImmediate();
                return;
            }

            forceBlackedOut = false;
            isLightOn = true;
            currentFlickerMult = Mathf.Lerp(minMult, maxMult, 0.65f);
            ApplyLightStateImmediate();
            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.FlashlightClick, 0.48f, 0.94f);
            }

            if (Application.isPlaying && isActiveAndEnabled)
            {
                climaxTrembleCoroutine = StartCoroutine(ClimaxWeakTrembleRoutine(minMult, maxMult));
            }
        }

        private IEnumerator ClimaxWeakTrembleRoutine(float minMult, float maxMult)
        {
            float lo = Mathf.Clamp(minMult, 0.08f, 0.90f);
            float hi = Mathf.Clamp(maxMult, lo + 0.05f, 1.0f);
            float t = 0f;
            while (inClimaxOverride && hasFlashlight && !forceBlackedOut)
            {
                t += Time.deltaTime;
                float n = Mathf.PerlinNoise(t * 17.5f, 4.21f);
                float dip = Mathf.PerlinNoise(t * 29.0f, 9.73f) < 0.14f ? 0.12f : 1.0f;
                currentFlickerMult = Mathf.Lerp(lo, hi, n) * dip;
                ApplyLightStateImmediate();
                yield return null;
            }
            climaxTrembleCoroutine = null;
        }

        public void StopClimaxEffectsAndBlackout()
        {
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }
            if (climaxTrembleCoroutine != null)
            {
                StopCoroutine(climaxTrembleCoroutine);
                climaxTrembleCoroutine = null;
            }
            inClimaxOverride = false;
            currentFlickerMult = 1f;
            forceBlackedOut = true;
            HideHintImmediate();
            ApplyLightStateImmediate();
        }

        private void Update()
        {
            UpdateFirstPickupHintUI();

            if (!hasFlashlight) return;

            HandleToggleInput();
            UpdateHandAimSway();
        }

        private void HandleToggleInput()
        {
            if (Time.timeScale <= 0.01f || forceBlackedOut || inClimaxOverride ||
                ObjectiveHUD.IsBlockingModalOpen || ObjectiveHUD.WasModalClosedRecently(2) ||
                Cursor.lockState != CursorLockMode.Locked)
                return;

            if (DayManager.Instance != null && (DayManager.Instance.IsTransitioning || DayManager.Instance.IsDemoEnded))
                return;

            if (player != null && !player.CanLook)
                return;

            Keyboard kb = Keyboard.current;
            bool keyPressed = kb != null && kb.fKey.wasPressedThisFrame;
            bool padPressed = Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
            if (keyPressed || padPressed)
            {
                SetLightOn(!isLightOn, playSound: true);
            }
        }

        private void UpdateHandAimSway()
        {
            if (cameraTransform == null || pivotTransform == null) return;

            if (!hasInitializedRotation)
            {
                smoothedWorldRot = cameraTransform.rotation;
                hasInitializedRotation = true;
            }

            float followT = 1f - Mathf.Exp(-aimFollowSpeed * Time.deltaTime);
            smoothedWorldRot = Quaternion.Slerp(smoothedWorldRot, cameraTransform.rotation, followT);

            float t = Time.time * idleSwayFrequency;
            float swayYaw = Mathf.Sin(t * 0.85f) * idleSwayAmplitudeDeg;
            float swayPitch = Mathf.Cos(t * 1.15f) * (idleSwayAmplitudeDeg * 0.72f);

            Quaternion targetWorldRot = smoothedWorldRot * Quaternion.Euler(swayPitch, swayYaw, 0f);
            pivotTransform.rotation = targetWorldRot;
        }

        private void ApplyLightStateImmediate()
        {
            if (spotLight == null) return;

            bool shouldEnable = hasFlashlight && isLightOn && !forceBlackedOut && currentFlickerMult > 0.02f;
            spotLight.enabled = shouldEnable;
            spotLight.color = beamColor;
            spotLight.intensity = shouldEnable ? (baseIntensity * currentFlickerMult) : 0f;
        }

        private void ShowFirstPickupHint()
        {
            EnsureHintUI();
            hintRemainingTimer = 8.0f;
            if (hintCanvasObj != null)
            {
                hintCanvasObj.SetActive(true);
            }
        }

        private void HideHintImmediate()
        {
            hintRemainingTimer = 0f;
            hintCurrentAlpha = 0f;
            if (hintCanvasGroup != null) hintCanvasGroup.alpha = 0f;
            if (hintCanvasObj != null) hintCanvasObj.SetActive(false);
        }

        private void UpdateFirstPickupHintUI()
        {
            if (hintCanvasObj == null || !hintCanvasObj.activeSelf || hintCanvasGroup == null)
                return;

            bool blocked = forceBlackedOut
                || ObjectiveHUD.IsBlockingModalOpen
                || (DayManager.Instance != null && (DayManager.Instance.IsTransitioning || DayManager.Instance.IsDemoEnded));

            if (!blocked && hintRemainingTimer > 0f)
            {
                hintRemainingTimer = Mathf.Max(0f, hintRemainingTimer - Time.unscaledDeltaTime);
            }

            float targetAlpha = (!blocked && hintRemainingTimer > 0f) ? 1f : 0f;
            hintCurrentAlpha = Mathf.MoveTowards(hintCurrentAlpha, targetAlpha, Time.unscaledDeltaTime * (targetAlpha > 0f ? 4.5f : 2.2f));
            hintCanvasGroup.alpha = hintCurrentAlpha;

            if (hintRemainingTimer <= 0f && hintCurrentAlpha <= 0.01f)
            {
                hintCanvasObj.SetActive(false);
            }
        }

        private void EnsureHintUI()
        {
            if (hintCanvasObj != null) return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            hintCanvasObj = new GameObject("HUD_Lanterna_Instrucao");
            Canvas canvas = hintCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 48;

            CanvasScaler scaler = hintCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Positioned in front of the right-hand flashlight quadrant (lower-right front view)
            GameObject pillObj = new GameObject("Lanterna_Hint_Pill");
            pillObj.transform.SetParent(hintCanvasObj.transform, false);
            RectTransform pillRect = pillObj.AddComponent<RectTransform>();
            pillRect.anchorMin = new Vector2(0.5f, 0.5f);
            pillRect.anchorMax = new Vector2(0.5f, 0.5f);
            pillRect.pivot = new Vector2(0.5f, 0.5f);
            pillRect.anchoredPosition = new Vector2(280f, -150f);
            pillRect.sizeDelta = new Vector2(280f, 44f);

            Image bg = pillObj.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.045f, 0.72f);
            bg.raycastTarget = false;

            hintCanvasGroup = pillObj.AddComponent<CanvasGroup>();
            hintCanvasGroup.alpha = 0f;
            hintCanvasGroup.blocksRaycasts = false;
            hintCanvasGroup.interactable = false;

            GameObject textObj = new GameObject("Lanterna_Hint_Text");
            textObj.transform.SetParent(pillObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 4f);
            textRect.offsetMax = new Vector2(-12f, -4f);

            Text txt = textObj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = 18;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.95f, 0.93f, 0.86f, 0.96f);
            txt.text = "[F] Ligar/Desligar";
            txt.raycastTarget = false;

            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);

            hintCanvasObj.SetActive(false);
        }
    }
}
