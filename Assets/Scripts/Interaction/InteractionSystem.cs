using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Canal420.Audio;
using Canal420.Effects;
using Canal420.Player;
using Canal420.UI;

namespace Canal420.Interaction
{
    public enum DocumentStyle
    {
        HandwrittenNote = 0,
        CorporateMemo = 1,
        MedicalPrescription = 2,
        NewspaperClipping = 3
    }

    public class InteractionSystem : MonoBehaviour
    {
        public static InteractionSystem Instance { get; private set; }

        [Header("Alcance e Magnetismo de Mira")]
        [SerializeField] private Transform raycastOrigin;
        [SerializeField] private float interactionDistance = 3.25f;
        [SerializeField] private float assistSphereRadius = 0.24f;
        [SerializeField] private float maxAssistAngle = 14f;
        [SerializeField] private float targetRetentionGrace = 0.12f;
        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField] private bool allowLeftMouseClick = true;

        [Header("Velocidade de Digitação da Fala")]
        [SerializeField] private float subtitleCharsPerSecond = 40f;

        [Header("HUD Opcional (Criado automaticamente em Runtime)")]
        [SerializeField] private bool autoCreateGreyboxHUD = true;
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Text promptText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private CanvasGroup subtitleCanvasGroup;
        private PlayerController playerController;
        private TVController cachedTvController;
        private IInteractable currentInteractable;
        private Collider currentInteractableCollider;
        private float lastValidHitTime = -10f;
        private GameObject runtimeCanvasObject;
        private CanvasGroup promptCanvasGroup;
        private RectTransform promptTextRect;
        private string lastFormattedPrompt = string.Empty;
        private string cachedRawPromptLabel = null;
        private string cachedFormattedPromptString = "[E] Examinar";

        private readonly RaycastHit[] directHitBuffer = new RaycastHit[32];
        private readonly RaycastHit[] sphereHitBuffer = new RaycastHit[48];
        private readonly RaycastHit[] wallHitBuffer = new RaycastHit[24];
        private readonly RaycastHit[] losHitBuffer = new RaycastHit[24];

        private sealed class RaycastHitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }

        private static readonly RaycastHitDistanceComparer HitDistanceComparer = new RaycastHitDistanceComparer();

        private string overridePromptMessage = string.Empty;

        private float suppressInteractUntilUnscaledTime = -1f;
        private int suppressInteractFrame = -1;

        private RectTransform subtitleContainerRect;
        private Image subtitleBackdropImage;
        private LayoutElement subtitleTextLayoutElement;
        private string fullSubtitleMessage = string.Empty;
        private int revealedCharCount = 0;
        private float charRevealAccumulator = 0f;
        private SpeechVoiceType currentSubtitleVoice = SpeechVoiceType.CianThought;
        private float postRevealHoldTimer = 0f;
        private float subtitleCurrentAlpha = 0f;
        private bool isShowingSubtitle = false;
        private Coroutine dissociationSubtitleCoroutine;
        private static readonly Color DefaultSubtitleColor = new Color(0.95f, 0.95f, 0.94f, 1f);
        private static readonly Color DissociationTruthColor = new Color(0.94f, 0.80f, 0.78f, 1f);

        private GameObject documentPanelObj;
        private CanvasGroup documentCanvasGroup;
        private Image paperSheetImage;
        private Image paperRuleImage;
        private Text documentHeaderStampText;
        private Text documentTitleText;
        private Text documentBodyText;
        private ScrollRect documentScrollRect;
        private bool isDocumentOpen = false;
        private int documentOpenedFrame = -1;
        private float documentOpenUnscaledTime = 0f;
        private bool savedPreDocCanMove = true;
        private bool savedPreDocCanLook = true;
        private bool savedPreDocCursorLocked = true;
        private Action onDocumentClosedCallback;

        public bool IsDocumentOpen => isDocumentOpen;
        public bool IsShowingSubtitle => isShowingSubtitle;
        public bool IsInteractionSuppressed => Time.frameCount <= suppressInteractFrame || Time.unscaledTime < suppressInteractUntilUnscaledTime;

        private void Awake()
        {
            Instance = this;
            playerController = GetComponent<PlayerController>();

            if (raycastOrigin == null)
            {
                if (playerController != null && playerController.CameraTransform != null)
                {
                    raycastOrigin = playerController.CameraTransform;
                }
                else
                {
                    Camera cam = GetComponentInChildren<Camera>();
                    if (cam != null) raycastOrigin = cam.transform;
                }
            }

            if (interactionDistance < 3.1f)
            {
                interactionDistance = 3.25f;
            }

            interactableLayers &= ~(1 << 2);

            if (autoCreateGreyboxHUD && (promptText == null || subtitleText == null))
            {
                EnsureEventSystemExists();
                BuildRuntimeGreyboxHUD();
            }
        }

        private void OnDestroy()
        {
            if (isDocumentOpen)
            {
                isDocumentOpen = false;
                ObjectiveHUD.IsBlockingModalOpen = false;
            }
            if (Instance == this) Instance = null;
            if (runtimeCanvasObject != null) Destroy(runtimeCanvasObject);
        }

        public void SuppressInteractionBriefly(float durationSeconds = 0.24f)
        {
            suppressInteractFrame = Time.frameCount;
            suppressInteractUntilUnscaledTime = Mathf.Max(suppressInteractUntilUnscaledTime, Time.unscaledTime + durationSeconds);
        }

        private void Update()
        {
            UpdateSubtitleTypewriterAndFade();

            if (isDocumentOpen)
            {
                ClearCurrentInteractableImmediate();
                SetCrosshairVisible(false);
                UpdateDocumentViewer();
                return;
            }

            if (!CanUseInteractionRay())
            {
                ClearCurrentInteractableImmediate();
                SetCrosshairVisible(false);
                return;
            }

            bool inTvCutscene = IsTvCutsceneActive();

            if (!string.IsNullOrEmpty(overridePromptMessage))
            {
                SetCrosshairVisible(!inTvCutscene);
                currentInteractable = null;
                ShowPromptSmooth(overridePromptMessage, highlightedCrosshair: false);
                return;
            }

            if (inTvCutscene)
            {
                ClearCurrentInteractableImmediate();
                SetCrosshairVisible(false);
                return;
            }

            SetCrosshairVisible(true);
            PerformSmartInteractionSearch();
            HandleInteractInput();
        }

        private bool IsTvCutsceneActive()
        {
            if (cachedTvController == null)
            {
                cachedTvController = FindAnyObjectByType<TVController>();
            }
            return cachedTvController != null && cachedTvController.IsCutsceneActive;
        }

        private bool CanUseInteractionRay()
        {
            if (raycastOrigin == null) return false;
            if (ObjectiveHUD.IsBlockingModalOpen) return false;
            if (Canal420.Core.DayManager.Instance != null &&
                (Canal420.Core.DayManager.Instance.IsTransitioning ||
                 Canal420.Core.DayManager.Instance.IsDemoEnded ||
                 Canal420.Core.DayManager.Instance.CurrentDay == Canal420.Core.GameDay.Day4_Prelude))
            {
                return false;
            }
            if (Cursor.lockState != CursorLockMode.Locked) return false;
            if (playerController != null && !playerController.CanLook) return false;
            return true;
        }

        private void PerformSmartInteractionSearch()
        {
            Vector3 origin = raycastOrigin.position;
            Vector3 forward = raycastOrigin.forward;
            Ray centerRay = new Ray(origin, forward);

            int directHitCount = Physics.RaycastNonAlloc(
                centerRay,
                directHitBuffer,
                interactionDistance,
                interactableLayers,
                QueryTriggerInteraction.Ignore
            );

            if (directHitCount > 1)
            {
                Array.Sort(directHitBuffer, 0, directHitCount, HitDistanceComparer);
            }

            IInteractable deferredOpenContainer = null;
            Collider deferredContainerCol = null;

            for (int i = 0; i < directHitCount; i++)
            {
                Collider hitCol = directHitBuffer[i].collider;
                if (hitCol == null || hitCol.transform.IsChildOf(transform))
                    continue;

                IInteractable directInteractable = hitCol.GetComponentInParent<IInteractable>();
                if (directInteractable == null || !directInteractable.CanInteract)
                {
                    if (deferredOpenContainer != null)
                    {
                        break;
                    }
                    break;
                }

                if (directInteractable is InspectableObject directInsp &&
                    !directInsp.IsWithinInteractionConstraints(origin, forward, directHitBuffer[i].point))
                {
                    continue;
                }

                if (IsBlockedByWallOrObstacle(origin, hitCol, directInteractable))
                {
                    continue;
                }

                // If the direct center ray grazed an open drawer/cabinet that currently exposes an interior item,
                // keep scanning along the same ray for a direct hit on an interior non-container interactable.
                if (directInteractable is AnimatedContainerInteractable openContainer && openContainer.HasActiveInteriorItem)
                {
                    if (deferredOpenContainer == null)
                    {
                        deferredOpenContainer = directInteractable;
                        deferredContainerCol = hitCol;
                    }
                    continue;
                }

                SelectInteractable(directInteractable, hitCol);
                return;
            }

            if (deferredOpenContainer != null)
            {
                SelectInteractable(deferredOpenContainer, deferredContainerCol);
                return;
            }

            int sphereHitCount = Physics.SphereCastNonAlloc(
                origin,
                assistSphereRadius,
                forward,
                sphereHitBuffer,
                interactionDistance,
                interactableLayers,
                QueryTriggerInteraction.Ignore
            );

            IInteractable bestCandidate = null;
            Collider bestCollider = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < sphereHitCount; i++)
            {
                Collider col = sphereHitBuffer[i].collider;
                if (col == null || col.transform.IsChildOf(transform))
                    continue;

                IInteractable candidate = col.GetComponentInParent<IInteractable>();
                if (candidate == null || !candidate.CanInteract)
                    continue;

                Vector3 targetPoint = col.ClosestPoint(origin + forward * Mathf.Max(0.35f, sphereHitBuffer[i].distance));
                Vector3 toTarget = targetPoint - origin;
                float dist = toTarget.magnitude;
                if (dist > interactionDistance + 0.12f || dist < 0.02f)
                    continue;

                float angle = Vector3.Angle(forward, toTarget / dist);
                if (angle > maxAssistAngle)
                    continue;

                if (candidate is InspectableObject candInsp &&
                    !candInsp.IsWithinInteractionConstraints(origin, forward, targetPoint))
                {
                    continue;
                }

                if (IsBlockedByWallOrObstacle(origin, col, candidate))
                    continue;

                float score = angle + (dist * 1.6f);

                if (candidate is AnimatedContainerInteractable openContainer && openContainer.HasActiveInteriorItem)
                {
                    score += 5.0f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                    bestCollider = col;
                }
            }

            if (bestCandidate != null)
            {
                SelectInteractable(bestCandidate, bestCollider);
                return;
            }

            if (currentInteractable != null &&
                currentInteractable.CanInteract &&
                (Time.time - lastValidHitTime) <= targetRetentionGrace &&
                (currentInteractableCollider == null || !IsBlockedByWallOrObstacle(origin, currentInteractableCollider, currentInteractable)))
            {
                RefreshHoveredVisuals(currentInteractable);
                return;
            }

            ClearCurrentInteractableImmediate();
        }

        private static bool IsArchitecturalWallOrRoomDoor(Collider col)
        {
            if (col == null) return false;
            string n = col.gameObject.name;
            if (n.StartsWith("Parede_", StringComparison.Ordinal) ||
                n.StartsWith("Teto_", StringComparison.Ordinal) ||
                n == "Base1")
            {
                return true;
            }

            if (col.GetComponentInParent<DoorInteractable>() != null)
            {
                return true;
            }

            return false;
        }

        private bool IsBlockedByWallOrObstacle(Vector3 origin, Collider targetCollider, IInteractable targetInteractable)
        {
            if (targetCollider == null)
                return true;

            Bounds bounds = targetCollider.bounds;
            Vector3 center = bounds.center;

            Vector3 toCenter = center - origin;
            float distToCenter = toCenter.magnitude;
            if (distToCenter > 0.025f)
            {
                int wallHitCount = Physics.RaycastNonAlloc(
                    origin,
                    toCenter / distToCenter,
                    wallHitBuffer,
                    distToCenter - 0.008f,
                    interactableLayers,
                    QueryTriggerInteraction.Ignore
                );

                for (int i = 0; i < wallHitCount; i++)
                {
                    Collider c = wallHitBuffer[i].collider;
                    if (c == null || c == targetCollider || c.transform.IsChildOf(transform))
                        continue;

                    IInteractable hitInter = c.GetComponentInParent<IInteractable>();
                    if (ReferenceEquals(hitInter, targetInteractable))
                        continue;

                    if (IsArchitecturalWallOrRoomDoor(c))
                        return true;
                }
            }

            Vector3 closestInward = Vector3.MoveTowards(targetCollider.ClosestPoint(origin), center, 0.04f);
            Vector3 upperSample = center + Vector3.up * Mathf.Min(0.14f, bounds.extents.y * 0.45f);

            if (HasUnobstructedRayToTargetSurface(origin, center, targetCollider, targetInteractable))
                return false;

            if (HasUnobstructedRayToTargetSurface(origin, closestInward, targetCollider, targetInteractable))
                return false;

            if (HasUnobstructedRayToTargetSurface(origin, upperSample, targetCollider, targetInteractable))
                return false;

            return true;
        }

        private bool HasUnobstructedRayToTargetSurface(
            Vector3 origin,
            Vector3 samplePoint,
            Collider targetCollider,
            IInteractable targetInteractable)
        {
            Vector3 vec = samplePoint - origin;
            float sampleDist = vec.magnitude;
            if (sampleDist < 0.03f)
                return true;

            Vector3 dir = vec / sampleDist;
            Ray sampleRay = new Ray(origin, dir);

            float entryDist = targetCollider.Raycast(sampleRay, out RaycastHit selfHit, sampleDist + 0.30f)
                ? selfHit.distance
                : sampleDist;

            if (entryDist <= 0.012f)
                return true;

            int losHitCount = Physics.RaycastNonAlloc(
                origin,
                dir,
                losHitBuffer,
                entryDist - 0.006f,
                interactableLayers,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < losHitCount; i++)
            {
                Collider c = losHitBuffer[i].collider;
                if (c == null || c == targetCollider || c.transform.IsChildOf(transform))
                    continue;

                IInteractable hitInter = c.GetComponentInParent<IInteractable>();
                if (ReferenceEquals(hitInter, targetInteractable))
                    continue;

                if (targetInteractable is InspectableObject &&
                    hitInter is AnimatedContainerInteractable openContainer &&
                    openContainer.IsContentsAccessible)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void SelectInteractable(IInteractable interactable, Collider hitCollider)
        {
            currentInteractable = interactable;
            currentInteractableCollider = hitCollider;
            lastValidHitTime = Time.time;
            RefreshHoveredVisuals(interactable);
        }

        private void RefreshHoveredVisuals(IInteractable interactable)
        {
            string label = interactable.GetInteractionPrompt();
            string formatted;
            if (string.IsNullOrEmpty(label))
            {
                formatted = "[E] Examinar";
            }
            else if (string.Equals(label, cachedRawPromptLabel, StringComparison.Ordinal))
            {
                formatted = cachedFormattedPromptString;
            }
            else
            {
                cachedRawPromptLabel = label;
                cachedFormattedPromptString = $"[E] {label}";
                formatted = cachedFormattedPromptString;
            }
            ShowPromptSmooth(formatted, highlightedCrosshair: true);
        }

        private void ShowPromptSmooth(string text, bool highlightedCrosshair)
        {
            string raw = text ?? string.Empty;

            if (promptText != null)
            {
                promptText.enabled = true;
                if (lastFormattedPrompt != raw)
                {
                    lastFormattedPrompt = raw;
                    ApplyPromptFormatted(raw);
                }
            }

            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = Mathf.MoveTowards(promptCanvasGroup.alpha, 1f, Time.unscaledDeltaTime * 11f);
            }

            if (crosshairImage != null)
            {
                Vector2 targetSize = highlightedCrosshair ? new Vector2(6f, 6f) : new Vector2(4f, 4f);
                Color targetColor = highlightedCrosshair ? new Color(1f, 1f, 1f, 0.96f) : new Color(1f, 1f, 1f, 0.54f);
                crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(crosshairImage.rectTransform.sizeDelta, targetSize, Time.unscaledDeltaTime * 18f);
                crosshairImage.color = Color.Lerp(crosshairImage.color, targetColor, Time.unscaledDeltaTime * 18f);
            }
        }

        private void ApplyPromptFormatted(string raw)
        {
            if (promptText == null)
                return;

            promptText.text = raw.Trim();
            if (promptTextRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(promptTextRect);
            }
        }

        private void ClearCurrentInteractableImmediate()
        {
            currentInteractable = null;
            currentInteractableCollider = null;

            bool forceClearInstant = ObjectiveHUD.IsBlockingModalOpen || Time.timeScale <= 0.01f;

            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = forceClearInstant
                    ? 0f
                    : Mathf.MoveTowards(promptCanvasGroup.alpha, 0f, Time.unscaledDeltaTime * 14f);

                if (promptCanvasGroup.alpha <= 0.01f && promptText != null)
                {
                    promptText.text = string.Empty;
                    promptText.enabled = false;
                    lastFormattedPrompt = string.Empty;
                }
            }
            else if (promptText != null)
            {
                promptText.text = string.Empty;
                promptText.enabled = false;
                lastFormattedPrompt = string.Empty;
            }

            if (crosshairImage != null)
            {
                crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(crosshairImage.rectTransform.sizeDelta, new Vector2(4f, 4f), Time.unscaledDeltaTime * 14f);
                crosshairImage.color = Color.Lerp(crosshairImage.color, new Color(1f, 1f, 1f, 0.54f), Time.unscaledDeltaTime * 14f);
            }
        }

        private void HandleInteractInput()
        {
            if (Time.frameCount <= suppressInteractFrame || Time.unscaledTime < suppressInteractUntilUnscaledTime)
                return;

            if (currentInteractable == null || !currentInteractable.CanInteract)
                return;

            if (WasInteractPressedThisFrame())
            {
                IInteractable target = currentInteractable;
                lastValidHitTime = -10f;
                SuppressInteractionBriefly(0.16f);
                target.Interact(gameObject);
            }
        }

        private bool WasInteractPressedThisFrame()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                return true;
            if (allowLeftMouseClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
                return true;
            return false;
        }

        private void SetCrosshairVisible(bool visible)
        {
            if (crosshairImage != null && crosshairImage.enabled != visible)
            {
                crosshairImage.enabled = visible;
            }
        }

        public void SetOverridePrompt(string message)
        {
            overridePromptMessage = message ?? string.Empty;
            if (string.IsNullOrEmpty(overridePromptMessage))
            {
                ClearCurrentInteractableImmediate();
            }
        }

        public void ClearOverridePrompt()
        {
            SetOverridePrompt(string.Empty);
        }

        public void ClearSubtitleImmediate()
        {
            if (dissociationSubtitleCoroutine != null)
            {
                StopCoroutine(dissociationSubtitleCoroutine);
                dissociationSubtitleCoroutine = null;
            }
            isShowingSubtitle = false;
            subtitleCurrentAlpha = 0f;
            fullSubtitleMessage = string.Empty;
            revealedCharCount = 0;
            postRevealHoldTimer = 0f;
            if (subtitleText != null) subtitleText.text = string.Empty;
            if (subtitleCanvasGroup != null) subtitleCanvasGroup.alpha = 0f;
        }

        public void ShowSubtitle(string message, float duration = 5.6f)
        {
            if (dissociationSubtitleCoroutine != null)
            {
                StopCoroutine(dissociationSubtitleCoroutine);
                dissociationSubtitleCoroutine = null;
            }

            ApplySubtitleImmediateOrTyped(message, duration, instantFull: false, DefaultSubtitleColor, FontStyle.Normal);
        }

        public void ShowDissociationGlitchSubtitle(string suppressedTruth, string denialLine, float duration = 5.6f, float truthFlashDuration = 0.48f, bool playAudioBurst = true)
        {
            if (subtitleText == null)
                return;

            if (string.IsNullOrWhiteSpace(suppressedTruth))
            {
                ShowSubtitle(denialLine, duration);
                return;
            }

            if (dissociationSubtitleCoroutine != null)
            {
                StopCoroutine(dissociationSubtitleCoroutine);
            }

            dissociationSubtitleCoroutine = StartCoroutine(DissociationGlitchSubtitleRoutine(suppressedTruth, denialLine, duration, truthFlashDuration, playAudioBurst));
        }

        private IEnumerator DissociationGlitchSubtitleRoutine(string suppressedTruth, string denialLine, float duration, float truthFlashDuration, bool playAudioBurst)
        {
            while (IsSubtitleSuppressedByModal())
            {
                yield return null;
            }

            // Guarantee at least one clean frame after modal dismissal before displaying the flash so it is never eaten by a transition hitch.
            if (ObjectiveHUD.WasModalClosedRecently(1))
            {
                yield return null;
            }

            ApplySubtitleImmediateOrTyped(suppressedTruth.Trim().ToUpperInvariant(), 1.2f, instantFull: true, DissociationTruthColor, FontStyle.Bold);

            if (playAudioBurst && Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayBinauralCue(FoleySoundId.TVSubliminalBurst, 0f, 0.28f, 1.32f);
            }
            if (SanityEffects.Instance != null)
            {
                SanityEffects.Instance.TriggerHallucinationSpike(0.42f, 0.28f, allowSubliminalFlash: false);
            }

            float waitTarget = Mathf.Clamp(truthFlashDuration, 0.30f, 0.72f);
            float elapsed = 0f;
            int visibleFrames = 0;
            while (elapsed < waitTarget || visibleFrames < 8)
            {
                if (!IsSubtitleSuppressedByModal())
                {
                    elapsed += Mathf.Min(Time.deltaTime, 0.05f);
                    visibleFrames++;
                }
                yield return null;
            }

            dissociationSubtitleCoroutine = null;
            ApplySubtitleImmediateOrTyped(denialLine, duration, instantFull: false, DefaultSubtitleColor, FontStyle.Normal);
        }

        private void ApplySubtitleImmediateOrTyped(string message, float duration, bool instantFull, Color textColor, FontStyle fontStyle = FontStyle.Normal)
        {
            if (string.IsNullOrWhiteSpace(message) || subtitleText == null)
                return;

            fullSubtitleMessage = message.Trim();
            currentSubtitleVoice = InferVoiceFromText(fullSubtitleMessage);
            postRevealHoldTimer = Mathf.Max(3.4f, duration * 0.72f);
            subtitleText.color = textColor;
            subtitleText.fontStyle = fontStyle;
            subtitleText.enabled = true;
            isShowingSubtitle = true;

            if (instantFull)
            {
                revealedCharCount = fullSubtitleMessage.Length;
                charRevealAccumulator = revealedCharCount;
                subtitleCurrentAlpha = 1f;
                UpdateSubtitleBoxSizeForVisibleText(fullSubtitleMessage);
                if (subtitleCanvasGroup != null)
                {
                    subtitleCanvasGroup.alpha = IsSubtitleSuppressedByModal() ? 0f : 1f;
                }
            }
            else
            {
                revealedCharCount = 0;
                charRevealAccumulator = 0f;
                subtitleCurrentAlpha = 0f;
                PreconfigureSubtitleBoxWidthForFullMessage(fullSubtitleMessage);
                subtitleText.text = string.Empty;
                if (subtitleCanvasGroup != null) subtitleCanvasGroup.alpha = 0f;
            }
        }

        private void PreconfigureSubtitleBoxWidthForFullMessage(string fullMessage)
        {
            if (subtitleText == null)
                return;

            if (string.IsNullOrEmpty(fullMessage))
            {
                UpdateSubtitleBoxSizeForVisibleText(string.Empty);
                return;
            }

            subtitleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            subtitleText.text = fullMessage;
            float rawWidth = subtitleText.preferredWidth;
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitleText.text = string.Empty;

            float innerWidth = Mathf.Clamp(rawWidth + 12f, 24f, 860f);
            float boxWidth = innerWidth + 36f;

            if (subtitleTextLayoutElement != null)
            {
                subtitleTextLayoutElement.preferredWidth = innerWidth;
                subtitleTextLayoutElement.preferredHeight = -1f;
                subtitleTextLayoutElement.minHeight = -1f;
            }

            if (subtitleContainerRect != null)
            {
                subtitleContainerRect.sizeDelta = new Vector2(boxWidth, 0f);
                LayoutRebuilder.ForceRebuildLayoutImmediate(subtitleContainerRect);
            }
        }

        private void UpdateSubtitleBoxSizeForVisibleText(string visibleText)
        {
            if (subtitleText == null)
                return;

            if (string.IsNullOrEmpty(visibleText))
            {
                subtitleText.text = string.Empty;
                if (subtitleTextLayoutElement != null)
                {
                    subtitleTextLayoutElement.preferredWidth = 0f;
                    subtitleTextLayoutElement.preferredHeight = 0f;
                    subtitleTextLayoutElement.minHeight = 0f;
                }
                if (subtitleContainerRect != null)
                {
                    subtitleContainerRect.sizeDelta = Vector2.zero;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(subtitleContainerRect);
                }
                return;
            }

            subtitleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            subtitleText.text = visibleText;
            float rawWidth = subtitleText.preferredWidth;
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;

            float innerWidth = Mathf.Clamp(rawWidth + 12f, 24f, 860f);
            float boxWidth = innerWidth + 36f;

            if (subtitleTextLayoutElement != null)
            {
                subtitleTextLayoutElement.preferredWidth = innerWidth;
                subtitleTextLayoutElement.preferredHeight = -1f;
                subtitleTextLayoutElement.minHeight = -1f;
            }

            if (subtitleContainerRect != null)
            {
                subtitleContainerRect.sizeDelta = new Vector2(boxWidth, 0f);
                LayoutRebuilder.ForceRebuildLayoutImmediate(subtitleContainerRect);
            }
        }

        private bool IsSubtitleSuppressedByModal()
        {
            return isDocumentOpen ||
                   ObjectiveHUD.IsBlockingModalOpen ||
                   (GameSettingsUI.Instance != null && GameSettingsUI.Instance.IsOpen);
        }

        private static SpeechVoiceType InferVoiceFromText(string msg)
        {
            if (msg.StartsWith("[CANAL", StringComparison.OrdinalIgnoreCase) ||
                msg.StartsWith("[PLANTÃO", StringComparison.OrdinalIgnoreCase) ||
                msg.StartsWith("[ARQUIVO", StringComparison.OrdinalIgnoreCase) ||
                msg.StartsWith("[SINAL", StringComparison.OrdinalIgnoreCase) ||
                msg.StartsWith("[TV", StringComparison.OrdinalIgnoreCase))
            {
                return SpeechVoiceType.BroadcastTV;
            }

            if (msg.StartsWith("[BIP]", StringComparison.OrdinalIgnoreCase) ||
                msg.StartsWith("[GRAVAÇÃO", StringComparison.OrdinalIgnoreCase) ||
                msg.StartsWith("[SECRETÁRIA", StringComparison.OrdinalIgnoreCase))
            {
                return SpeechVoiceType.AnsweringMachine;
            }

            if (msg.StartsWith("[", StringComparison.Ordinal))
            {
                return SpeechVoiceType.None;
            }

            return SpeechVoiceType.CianThought;
        }

        private void UpdateSubtitleTypewriterAndFade()
        {
            if (subtitleCanvasGroup == null)
                return;

            if (IsSubtitleSuppressedByModal())
            {
                subtitleCanvasGroup.alpha = 0f;
                return;
            }

            if (!isShowingSubtitle)
            {
                subtitleCanvasGroup.alpha = 0f;
                return;
            }

            if (revealedCharCount < fullSubtitleMessage.Length)
            {
                charRevealAccumulator += Time.deltaTime * Mathf.Max(22f, subtitleCharsPerSecond);
                int targetCount = Mathf.Min(fullSubtitleMessage.Length, Mathf.FloorToInt(charRevealAccumulator));

                if (targetCount > revealedCharCount)
                {
                    bool shouldBlip = false;
                    for (int c = revealedCharCount; c < targetCount; c++)
                    {
                        char ch = fullSubtitleMessage[c];
                        if (char.IsLetterOrDigit(ch) && (c % 3 == 0))
                        {
                            shouldBlip = true;
                        }
                    }

                    revealedCharCount = targetCount;
                    if (subtitleText != null)
                    {
                        subtitleText.text = fullSubtitleMessage.Substring(0, revealedCharCount);
                    }

                    if (shouldBlip && Canal420AudioManager.Instance != null)
                    {
                        Canal420AudioManager.Instance.PlaySpeechBlip(currentSubtitleVoice);
                    }
                }

                if (revealedCharCount > 0)
                {
                    subtitleCurrentAlpha = Mathf.MoveTowards(subtitleCurrentAlpha, 1f, Time.deltaTime * 6.5f);
                    subtitleCanvasGroup.alpha = subtitleCurrentAlpha;
                }
                else
                {
                    subtitleCanvasGroup.alpha = 0f;
                }
            }
            else if (postRevealHoldTimer > 0f || dissociationSubtitleCoroutine != null)
            {
                subtitleCurrentAlpha = Mathf.MoveTowards(subtitleCurrentAlpha, 1f, Time.deltaTime * 6.5f);
                subtitleCanvasGroup.alpha = subtitleCurrentAlpha;
                if (dissociationSubtitleCoroutine == null)
                {
                    postRevealHoldTimer -= Time.deltaTime;
                }
            }
            else
            {
                subtitleCurrentAlpha = Mathf.MoveTowards(subtitleCurrentAlpha, 0f, Time.deltaTime * 1.4f);
                subtitleCanvasGroup.alpha = subtitleCurrentAlpha;

                if (subtitleCurrentAlpha <= 0.01f)
                {
                    subtitleCurrentAlpha = 0f;
                    subtitleCanvasGroup.alpha = 0f;
                    UpdateSubtitleBoxSizeForVisibleText(string.Empty);
                    isShowingSubtitle = false;
                }
            }
        }

        public void ShowDocument(
            string title,
            string body,
            Action onClosed = null,
            DocumentStyle style = DocumentStyle.HandwrittenNote)
        {
            EnsureEventSystemExists();
            if (documentPanelObj == null)
            {
                BuildRuntimeGreyboxHUD();
            }

            if (documentPanelObj == null)
                return;

            isDocumentOpen = true;
            documentOpenedFrame = Time.frameCount;
            documentOpenUnscaledTime = Time.unscaledTime;
            onDocumentClosedCallback = onClosed;
            ObjectiveHUD.IsBlockingModalOpen = true;

            ApplyDocumentVisualStyle(style);

            if (documentTitleText != null) documentTitleText.text = title ?? string.Empty;
            if (documentBodyText != null) documentBodyText.text = body ?? string.Empty;

            documentPanelObj.SetActive(true);
            documentCanvasGroup.alpha = 0f;
            if (paperSheetImage != null)
            {
                paperSheetImage.rectTransform.localScale = new Vector3(0.95f, 0.95f, 1f);
            }

            if (documentBodyText != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(documentBodyText.rectTransform);
            }
            if (documentScrollRect != null)
            {
                documentScrollRect.verticalNormalizedPosition = 1f;
            }

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.PaperRustle, 0.85f);
            }

            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (playerController != null)
            {
                savedPreDocCanMove = playerController.CanMove;
                savedPreDocCanLook = playerController.CanLook;
                savedPreDocCursorLocked = playerController.IsCursorLocked;
                playerController.SetControlsEnabled(moveEnabled: false, lookEnabled: false, lockCursor: false);
            }
            else
            {
                savedPreDocCanMove = true;
                savedPreDocCanLook = true;
                savedPreDocCursorLocked = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void ApplyDocumentVisualStyle(DocumentStyle style)
        {
            if (paperSheetImage == null) return;

            switch (style)
            {
                case DocumentStyle.HandwrittenNote:
                    paperSheetImage.color = new Color(0.89f, 0.86f, 0.77f, 0.99f);
                    if (paperRuleImage != null) paperRuleImage.color = new Color(0.36f, 0.30f, 0.24f, 0.35f);
                    if (documentHeaderStampText != null) documentHeaderStampText.text = "ANOTAÇÃO PESSOAL";
                    if (documentBodyText != null) documentBodyText.fontStyle = FontStyle.Italic;
                    break;

                case DocumentStyle.CorporateMemo:
                    paperSheetImage.color = new Color(0.92f, 0.93f, 0.94f, 0.99f);
                    if (paperRuleImage != null) paperRuleImage.color = new Color(0.18f, 0.24f, 0.32f, 0.55f);
                    if (documentHeaderStampText != null) documentHeaderStampText.text = "COMUNICAÇÃO INTERNA";
                    if (documentBodyText != null) documentBodyText.fontStyle = FontStyle.Normal;
                    break;

                case DocumentStyle.MedicalPrescription:
                    paperSheetImage.color = new Color(0.90f, 0.94f, 0.93f, 0.99f);
                    if (paperRuleImage != null) paperRuleImage.color = new Color(0.58f, 0.16f, 0.16f, 0.55f);
                    if (documentHeaderStampText != null) documentHeaderStampText.text = "RECEITUÁRIO DE CONTROLE ESPECIAL";
                    if (documentBodyText != null) documentBodyText.fontStyle = FontStyle.Normal;
                    break;

                case DocumentStyle.NewspaperClipping:
                    paperSheetImage.color = new Color(0.82f, 0.80f, 0.75f, 0.99f);
                    if (paperRuleImage != null) paperRuleImage.color = new Color(0.15f, 0.15f, 0.15f, 0.65f);
                    if (documentHeaderStampText != null) documentHeaderStampText.text = "RECORTE DE JORNAL";
                    if (documentBodyText != null) documentBodyText.fontStyle = FontStyle.Normal;
                    break;
            }
        }

        public void CloseDocument(bool restorePlayerControls = true)
        {
            if (!isDocumentOpen)
                return;

            isDocumentOpen = false;
            ObjectiveHUD.IsBlockingModalOpen = false;
            ObjectiveHUD.NotifyModalClosedThisFrame();
            SuppressInteractionBriefly(0.26f);

            if (documentPanelObj != null)
            {
                documentPanelObj.SetActive(false);
            }

            if (restorePlayerControls && Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.PaperPutAway, 0.70f);
            }

            if (restorePlayerControls)
            {
                if (playerController == null) playerController = GetComponent<PlayerController>();
                if (playerController != null)
                {
                    bool allowMove = savedPreDocCanMove && !playerController.IsRestrictedLook && !IsTvCutsceneActive();
                    bool allowLook = savedPreDocCanLook;
                    bool lockCursor = savedPreDocCursorLocked || allowLook;
                    playerController.SetControlsEnabled(moveEnabled: allowMove, lookEnabled: allowLook, lockCursor: lockCursor);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            Action cb = onDocumentClosedCallback;
            onDocumentClosedCallback = null;
            cb?.Invoke();
        }

        private bool CanCloseDocumentByUserInput()
        {
            return isDocumentOpen
                && Time.frameCount > documentOpenedFrame + 2
                && (Time.unscaledTime - documentOpenUnscaledTime) >= 0.25f;
        }

        private void UpdateDocumentViewer()
        {
            if (!isDocumentOpen || documentCanvasGroup == null)
                return;

            documentCanvasGroup.alpha = Mathf.MoveTowards(documentCanvasGroup.alpha, 1f, Time.unscaledDeltaTime * 6.5f);
            if (paperSheetImage != null)
            {
                float s = Mathf.Lerp(0.95f, 1.0f, documentCanvasGroup.alpha);
                paperSheetImage.rectTransform.localScale = new Vector3(s, s, 1f);
            }

            if (!CanCloseDocumentByUserInput())
                return;

            bool closePressed = false;
            Keyboard kb = Keyboard.current;
            if (kb != null &&
                (kb.eKey.wasPressedThisFrame ||
                 kb.qKey.wasPressedThisFrame ||
                 kb.spaceKey.wasPressedThisFrame ||
                 kb.enterKey.wasPressedThisFrame ||
                 kb.tabKey.wasPressedThisFrame ||
                 kb.escapeKey.wasPressedThisFrame))
            {
                closePressed = true;
            }
            else if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                closePressed = true;
            }
            else if (Gamepad.current != null &&
                     (Gamepad.current.buttonEast.wasPressedThisFrame || Gamepad.current.buttonSouth.wasPressedThisFrame))
            {
                closePressed = true;
            }

            if (closePressed)
            {
                CloseDocument(restorePlayerControls: true);
            }
        }

        private static void EnsureEventSystemExists()
        {
            if (EventSystem.current != null)
                return;

            GameObject esObj = new GameObject("EventSystem_Auto");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildRuntimeGreyboxHUD()
        {
            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            runtimeCanvasObject = new GameObject("HUD_Interacao_Auto");
            Canvas canvas = runtimeCanvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;

            CanvasScaler scaler = runtimeCanvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            runtimeCanvasObject.AddComponent<GraphicRaycaster>();

            if (crosshairImage == null)
            {
                GameObject dotObj = new GameObject("Crosshair_Dot");
                dotObj.transform.SetParent(runtimeCanvasObject.transform, false);
                crosshairImage = dotObj.AddComponent<Image>();
                crosshairImage.raycastTarget = false;
                crosshairImage.color = new Color(1f, 1f, 1f, 0.54f);

                Shadow dotShadow = dotObj.AddComponent<Shadow>();
                dotShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
                dotShadow.effectDistance = new Vector2(1f, -1f);

                RectTransform dotRect = crosshairImage.rectTransform;
                dotRect.anchorMin = new Vector2(0.5f, 0.5f);
                dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                dotRect.anchoredPosition = Vector2.zero;
                dotRect.sizeDelta = new Vector2(4f, 4f);
            }

            if (promptText == null)
            {
                GameObject promptObj = new GameObject("Prompt_Text");
                promptObj.transform.SetParent(runtimeCanvasObject.transform, false);
                promptCanvasGroup = promptObj.AddComponent<CanvasGroup>();
                promptCanvasGroup.alpha = 0f;
                promptCanvasGroup.blocksRaycasts = false;

                promptText = promptObj.AddComponent<Text>();
                promptText.font = builtinFont;
                promptText.fontSize = 16;
                promptText.alignment = TextAnchor.MiddleCenter;
                promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
                promptText.verticalOverflow = VerticalWrapMode.Overflow;
                promptText.color = Color.white;
                promptText.raycastTarget = false;
                promptText.enabled = false;

                Outline promptOutline = promptObj.AddComponent<Outline>();
                promptOutline.effectColor = new Color(0f, 0f, 0f, 0.72f);
                promptOutline.effectDistance = new Vector2(1f, -1f);

                promptTextRect = promptText.rectTransform;
                promptTextRect.anchorMin = new Vector2(0.5f, 0.5f);
                promptTextRect.anchorMax = new Vector2(0.5f, 0.5f);
                promptTextRect.pivot = new Vector2(0.5f, 0.5f);
                promptTextRect.anchoredPosition = new Vector2(0f, -44f);
                promptTextRect.sizeDelta = new Vector2(480f, 28f);
            }

            if (subtitleText == null)
            {
                GameObject subContainer = new GameObject("Subtitle_Container");
                subContainer.transform.SetParent(runtimeCanvasObject.transform, false);
                subtitleCanvasGroup = subContainer.AddComponent<CanvasGroup>();
                subtitleCanvasGroup.alpha = 0f;
                subtitleCanvasGroup.blocksRaycasts = false;

                subtitleBackdropImage = subContainer.AddComponent<Image>();
                subtitleBackdropImage.color = new Color(0f, 0f, 0f, 0.66f);
                subtitleBackdropImage.raycastTarget = false;

                VerticalLayoutGroup subLayout = subContainer.AddComponent<VerticalLayoutGroup>();
                subLayout.padding = new RectOffset(18, 18, 9, 10);
                subLayout.spacing = 0f;
                subLayout.childAlignment = TextAnchor.MiddleCenter;
                subLayout.childControlWidth = true;
                subLayout.childControlHeight = true;
                subLayout.childForceExpandWidth = false;
                subLayout.childForceExpandHeight = false;

                ContentSizeFitter subFitter = subContainer.AddComponent<ContentSizeFitter>();
                subFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                subFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                subtitleContainerRect = subtitleBackdropImage.rectTransform;
                subtitleContainerRect.anchorMin = new Vector2(0.5f, 0.058f);
                subtitleContainerRect.anchorMax = new Vector2(0.5f, 0.058f);
                subtitleContainerRect.pivot = new Vector2(0.5f, 0f);
                subtitleContainerRect.anchoredPosition = Vector2.zero;
                subtitleContainerRect.sizeDelta = Vector2.zero;

                GameObject subObj = new GameObject("Subtitle_Text");
                subObj.transform.SetParent(subContainer.transform, false);
                subtitleText = subObj.AddComponent<Text>();
                subtitleText.font = builtinFont;
                subtitleText.fontSize = 23;
                subtitleText.lineSpacing = 1.22f;
                subtitleText.alignment = TextAnchor.MiddleCenter;
                subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
                subtitleText.color = DefaultSubtitleColor;
                subtitleText.raycastTarget = false;

                subtitleTextLayoutElement = subObj.AddComponent<LayoutElement>();
                subtitleTextLayoutElement.minHeight = 0f;

                Shadow subShadow = subObj.AddComponent<Shadow>();
                subShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                subShadow.effectDistance = new Vector2(1f, -1f);
            }

            documentPanelObj = new GameObject("Document_Overlay");
            documentPanelObj.transform.SetParent(runtimeCanvasObject.transform, false);
            documentCanvasGroup = documentPanelObj.AddComponent<CanvasGroup>();
            documentCanvasGroup.alpha = 0f;
            documentCanvasGroup.blocksRaycasts = true;
            documentCanvasGroup.interactable = true;

            Image dimImg = documentPanelObj.AddComponent<Image>();
            dimImg.color = new Color(0.015f, 0.015f, 0.02f, 0.86f);
            dimImg.raycastTarget = true;

            Button bgCloseBtn = documentPanelObj.AddComponent<Button>();
            bgCloseBtn.transition = Selectable.Transition.None;
            bgCloseBtn.navigation = new Navigation { mode = Navigation.Mode.None };
            bgCloseBtn.onClick.AddListener(() =>
            {
                if (CanCloseDocumentByUserInput())
                {
                    CloseDocument(restorePlayerControls: true);
                }
            });

            RectTransform dimRect = dimImg.rectTransform;
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            GameObject paperObj = new GameObject("Paper_Sheet");
            paperObj.transform.SetParent(documentPanelObj.transform, false);
            paperSheetImage = paperObj.AddComponent<Image>();
            paperSheetImage.color = new Color(0.89f, 0.86f, 0.77f, 0.99f);
            paperSheetImage.raycastTarget = true;

            // Absorb pointer clicks on the sheet itself so clicking text/paper does not bubble up to bgCloseBtn.
            Button swallowPaperClick = paperObj.AddComponent<Button>();
            swallowPaperClick.transition = Selectable.Transition.None;
            swallowPaperClick.navigation = new Navigation { mode = Navigation.Mode.None };

            Outline paperBorder = paperObj.AddComponent<Outline>();
            paperBorder.effectColor = new Color(0.14f, 0.13f, 0.12f, 0.92f);
            paperBorder.effectDistance = new Vector2(3f, -3f);

            RectTransform paperRect = paperSheetImage.rectTransform;
            paperRect.anchorMin = new Vector2(0.24f, 0.09f);
            paperRect.anchorMax = new Vector2(0.76f, 0.91f);
            paperRect.offsetMin = Vector2.zero;
            paperRect.offsetMax = Vector2.zero;

            GameObject stampObj = new GameObject("Doc_CategoryStamp");
            stampObj.transform.SetParent(paperObj.transform, false);
            documentHeaderStampText = stampObj.AddComponent<Text>();
            documentHeaderStampText.font = builtinFont;
            documentHeaderStampText.fontSize = 13;
            documentHeaderStampText.fontStyle = FontStyle.Bold;
            documentHeaderStampText.alignment = TextAnchor.MiddleLeft;
            documentHeaderStampText.color = new Color(0.36f, 0.34f, 0.30f, 0.85f);
            documentHeaderStampText.raycastTarget = false;
            RectTransform stRect = documentHeaderStampText.rectTransform;
            stRect.anchorMin = new Vector2(0f, 1f);
            stRect.anchorMax = new Vector2(1f, 1f);
            stRect.pivot = new Vector2(0.5f, 1f);
            stRect.offsetMin = new Vector2(46f, -42f);
            stRect.offsetMax = new Vector2(-46f, -18f);

            GameObject docTitleObj = new GameObject("Doc_Title");
            docTitleObj.transform.SetParent(paperObj.transform, false);
            documentTitleText = docTitleObj.AddComponent<Text>();
            documentTitleText.font = builtinFont;
            documentTitleText.fontSize = 22;
            documentTitleText.fontStyle = FontStyle.Bold;
            documentTitleText.alignment = TextAnchor.MiddleLeft;
            documentTitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            documentTitleText.verticalOverflow = VerticalWrapMode.Overflow;
            documentTitleText.color = new Color(0.13f, 0.12f, 0.11f, 1f);
            documentTitleText.raycastTarget = false;
            RectTransform dtRect = documentTitleText.rectTransform;
            dtRect.anchorMin = new Vector2(0f, 1f);
            dtRect.anchorMax = new Vector2(1f, 1f);
            dtRect.pivot = new Vector2(0.5f, 1f);
            dtRect.offsetMin = new Vector2(46f, -104f);
            dtRect.offsetMax = new Vector2(-46f, -44f);

            GameObject lineObj = new GameObject("Doc_Line");
            lineObj.transform.SetParent(paperObj.transform, false);
            paperRuleImage = lineObj.AddComponent<Image>();
            paperRuleImage.color = new Color(0.25f, 0.22f, 0.20f, 0.45f);
            paperRuleImage.raycastTarget = false;
            RectTransform lnRect = paperRuleImage.rectTransform;
            lnRect.anchorMin = new Vector2(0f, 1f);
            lnRect.anchorMax = new Vector2(1f, 1f);
            lnRect.pivot = new Vector2(0.5f, 1f);
            lnRect.offsetMin = new Vector2(46f, -114f);
            lnRect.offsetMax = new Vector2(-46f, -112f);

            GameObject scrollViewport = new GameObject("Body_Viewport");
            scrollViewport.transform.SetParent(paperObj.transform, false);
            Image vpImg = scrollViewport.AddComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.004f);
            Mask vpMask = scrollViewport.AddComponent<Mask>();
            vpMask.showMaskGraphic = false;

            RectTransform vpRect = scrollViewport.GetComponent<RectTransform>();
            vpRect.anchorMin = new Vector2(0f, 0f);
            vpRect.anchorMax = new Vector2(1f, 1f);
            vpRect.offsetMin = new Vector2(46f, 76f);
            vpRect.offsetMax = new Vector2(-46f, -128f);

            GameObject contentObj = new GameObject("Doc_Body_Content");
            contentObj.transform.SetParent(scrollViewport.transform, false);
            documentBodyText = contentObj.AddComponent<Text>();
            documentBodyText.font = builtinFont;
            documentBodyText.fontSize = 20;
            documentBodyText.lineSpacing = 1.38f;
            documentBodyText.alignment = TextAnchor.UpperLeft;
            documentBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            documentBodyText.verticalOverflow = VerticalWrapMode.Overflow;
            documentBodyText.color = new Color(0.16f, 0.15f, 0.14f, 0.98f);
            documentBodyText.raycastTarget = false;

            ContentSizeFitter bodyFitter = contentObj.AddComponent<ContentSizeFitter>();
            bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform contentRect = documentBodyText.rectTransform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            documentScrollRect = scrollViewport.AddComponent<ScrollRect>();
            documentScrollRect.viewport = vpRect;
            documentScrollRect.content = contentRect;
            documentScrollRect.horizontal = false;
            documentScrollRect.vertical = true;
            documentScrollRect.scrollSensitivity = 28f;
            documentScrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject closeBtnObj = new GameObject("Btn_GuardarDocumento");
            closeBtnObj.transform.SetParent(paperObj.transform, false);
            Image closeBtnImg = closeBtnObj.AddComponent<Image>();
            closeBtnImg.color = new Color(0.17f, 0.16f, 0.15f, 0.92f);

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock cb = closeBtn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.22f, 1.22f, 1.22f, 1f);
            cb.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            closeBtn.colors = cb;
            closeBtn.onClick.AddListener(() =>
            {
                if (CanCloseDocumentByUserInput())
                {
                    CloseDocument(restorePlayerControls: true);
                }
            });

            RectTransform cbRect = closeBtnImg.rectTransform;
            cbRect.anchorMin = new Vector2(0.5f, 0f);
            cbRect.anchorMax = new Vector2(0.5f, 0f);
            cbRect.pivot = new Vector2(0.5f, 0f);
            cbRect.anchoredPosition = new Vector2(0f, 18f);
            cbRect.sizeDelta = new Vector2(360f, 42f);

            GameObject btnLblObj = new GameObject("Btn_Label");
            btnLblObj.transform.SetParent(closeBtnObj.transform, false);
            Text btnLbl = btnLblObj.AddComponent<Text>();
            btnLbl.font = builtinFont;
            btnLbl.fontSize = 16;
            btnLbl.fontStyle = FontStyle.Bold;
            btnLbl.alignment = TextAnchor.MiddleCenter;
            btnLbl.color = new Color(0.92f, 0.91f, 0.88f, 1f);
            btnLbl.text = "Guardar  [E / Botão Direito]";
            btnLbl.raycastTarget = false;
            RectTransform blRect = btnLbl.rectTransform;
            blRect.anchorMin = Vector2.zero;
            blRect.anchorMax = Vector2.one;
            blRect.offsetMin = Vector2.zero;
            blRect.offsetMax = Vector2.zero;

            documentPanelObj.SetActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = raycastOrigin != null ? raycastOrigin : transform;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(origin.position, origin.forward * interactionDistance);
        }
    }
}
