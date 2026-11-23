using System;
using UnityEngine;
using UnityEngine.Events;
using Canal420.Audio;
using Canal420.Core;
using Canal420.Effects;

namespace Canal420.Interaction
{
    public class InspectableObject : MonoBehaviour, IInteractable
    {
        public static event Action<string> OnTaskCompleted;
        public static event Action<string> OnLockedRequirementAttempted;

        [Header("Identificação e Fluxo")]
        [SerializeField] private string taskId = "";
        [SerializeField] private bool isLinearObjective = false;
        [SerializeField] private string objectiveHudLabel = "";

        [Header("Mira e Monólogo Interno")]
        [SerializeField] private string interactionPrompt = "Examinar";
        [TextArea(2, 5)]
        [SerializeField] private string thoughtSubtitle = "";
        [SerializeField] private string suppressedTruthFlash = "";
        [SerializeField] private float subtitleDuration = 5.8f;

        [Header("Áudio Tátil (Foley)")]
        [SerializeField] private FoleySoundId interactSound = FoleySoundId.GenericTouch;

        [Header("Coleta de Item Doméstico / Mudança Visual (Opcional)")]
        [SerializeField] private string grantsItemId = "";
        [SerializeField] private GameObject visualToHideOnPickup;

        [Header("Requisito de Item para Usar / Destrancar (Opcional)")]
        [SerializeField] private string requiredItemId = "";
        [SerializeField] private string lockedInteractionPrompt = "";
        [TextArea(2, 4)]
        [SerializeField] private string missingRequirementSubtitle = "";
        [SerializeField] private FoleySoundId missingRequirementSound = FoleySoundId.DrawerLocked;
        [SerializeField] private GameObject visualToRevealWhenUnlocked;

        [Header("Sinalizador Diegético (Bipe 3D + LED Piscante Opcional)")]
        [SerializeField] private bool emitAlertBeep = false;
        [SerializeField] private float alertBeepInterval = 2.65f;
        [SerializeField] private Renderer alertLedRenderer;
        [SerializeField] private Transform alertReelLeft;
        [SerializeField] private Transform alertReelRight;

        [Header("Leitura de Documento / Carta / Jornal (Opcional)")]
        [SerializeField] private bool hasReadableDocument = false;
        [SerializeField] private DocumentStyle documentStyle = DocumentStyle.HandwrittenNote;
        [SerializeField] private string documentTitle = "";
        [TextArea(4, 12)]
        [SerializeField] private string documentBody = "";

        [Header("Comportamento Pós-Interação")]
        [SerializeField] private bool disableAfterFirstUse = false;
        [TextArea(2, 4)]
        [SerializeField] private string repeatThoughtSubtitle = "";

        [Header("Consumo Múltiplo / Cápsulas (Opcional)")]
        [SerializeField] private bool isMultiUseConsumable = false;
        [SerializeField] private GameObject[] consumableStepVisuals;
        [SerializeField] private string[] consumableUseSubtitles;
        [SerializeField] private string consumableCooldownSubtitle = "";

        [Header("Eventos Extras")]
        [SerializeField] private FaucetWaterController linkedFaucet;
        [SerializeField] private float maxInteractDistanceOverride = 0f;
        [SerializeField] private bool requireFacingDirection = false;
        [SerializeField] private Vector3 requiredFacingWorldDir = Vector3.forward;
        [SerializeField] private float requiredFacingMinDot = 0.45f;
        [SerializeField] private UnityEvent onFirstInteract;
        [SerializeField] private UnityEvent onAnyInteract;
        private bool hasInteracted = false;
        private int consumedUseCount = 0;
        private bool interactionEnabled = true;
        private bool blockedByClosedContainer = false;

        private float alertCycleTimer = 0f;
        private bool alertCycleBeepPlayed = false;
        private bool alertSilencedApplied = false;
        private float tapeSpinTimer = 0f;
        private MaterialPropertyBlock ledPropBlock;

        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProp = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

        public bool CanInteract => interactionEnabled && !blockedByClosedContainer && tapeSpinTimer <= 0f;
        public bool HasInteracted => hasInteracted;
        public bool IsLinearObjective => isLinearObjective;
        public string InteractionPrompt => interactionPrompt;

        public string ObjectiveHudLabel => string.IsNullOrWhiteSpace(objectiveHudLabel)
            ? interactionPrompt
            : objectiveHudLabel.Trim();

        public string NormalizedTaskId
        {
            get
            {
                string raw = string.IsNullOrWhiteSpace(taskId) ? gameObject.name : taskId;
                return raw.Trim().ToLowerInvariant();
            }
        }

        private void Update()
        {
            if (tapeSpinTimer > 0f)
            {
                tapeSpinTimer = Mathf.Max(0f, tapeSpinTimer - Time.deltaTime);
                float spinDeg = -170f * Time.deltaTime;
                if (alertReelLeft != null) alertReelLeft.Rotate(0f, spinDeg, 0f, Space.Self);
                if (alertReelRight != null) alertReelRight.Rotate(0f, spinDeg, 0f, Space.Self);
            }

            if (!emitAlertBeep && alertLedRenderer == null)
                return;

            if (tapeSpinTimer > 0f && alertLedRenderer != null)
            {
                bool pulse = (Mathf.FloorToInt(tapeSpinTimer * 4f) % 2) == 0;
                SetAlertLedVisual(lit: pulse, steadyDim: false);
                alertSilencedApplied = false;
                return;
            }

            if (!interactionEnabled || hasInteracted)
            {
                if (!alertSilencedApplied)
                {
                    SetAlertLedVisual(lit: false, steadyDim: true);
                    alertSilencedApplied = true;
                }
                return;
            }

            if (Time.timeScale <= 0.01f ||
                Canal420.UI.ObjectiveHUD.IsBlockingModalOpen ||
                (DayManager.Instance != null && (DayManager.Instance.IsTransitioning || DayManager.Instance.IsDemoEnded)))
            {
                return;
            }

            alertSilencedApplied = false;
            float interval = Mathf.Max(0.85f, alertBeepInterval);
            alertCycleTimer += Time.deltaTime;
            if (alertCycleTimer >= interval)
            {
                alertCycleTimer -= interval;
                alertCycleBeepPlayed = false;
            }

            bool isLitWindow = alertCycleTimer < 0.36f;
            SetAlertLedVisual(lit: isLitWindow, steadyDim: false);

            if (isLitWindow && !alertCycleBeepPlayed && emitAlertBeep)
            {
                alertCycleBeepPlayed = true;
                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley3D(
                        FoleySoundId.AnsweringMachineBeep,
                        transform.position + Vector3.up * 0.14f,
                        0.82f
                    );
                }
            }
        }

        private void SetAlertLedVisual(bool lit, bool steadyDim)
        {
            if (alertLedRenderer == null)
                return;

            if (ledPropBlock == null)
            {
                ledPropBlock = new MaterialPropertyBlock();
            }

            Color c = steadyDim
                ? new Color(0.18f, 0.05f, 0.04f, 1f)
                : (lit ? new Color(1.0f, 0.14f, 0.08f, 1f) : new Color(0.12f, 0.02f, 0.02f, 1f));
            Color emis = (!steadyDim && lit) ? new Color(1.8f, 0.18f, 0.10f, 1f) : Color.black;

            alertLedRenderer.GetPropertyBlock(ledPropBlock);
            ledPropBlock.SetColor(BaseColorProp, c);
            ledPropBlock.SetColor(ColorProp, c);
            ledPropBlock.SetColor(EmissionColorProp, emis);
            alertLedRenderer.SetPropertyBlock(ledPropBlock);
        }

        private bool MeetsRequirement()
        {
            if (string.IsNullOrWhiteSpace(requiredItemId))
                return true;

            return InventoryState.HasItem(requiredItemId);
        }

        public string GetInteractionPrompt()
        {
            if (!hasInteracted && !MeetsRequirement() && !string.IsNullOrWhiteSpace(lockedInteractionPrompt))
            {
                return lockedInteractionPrompt;
            }

            if (hasInteracted && !disableAfterFirstUse)
            {
                return hasReadableDocument ? $"Reler: {interactionPrompt}" : interactionPrompt;
            }

            return interactionPrompt;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract)
                return;

            if (isMultiUseConsumable)
            {
                int maxUses = (consumableStepVisuals != null && consumableStepVisuals.Length > 0)
                    ? consumableStepVisuals.Length
                    : 1;
                if (consumedUseCount >= maxUses)
                {
                    interactionEnabled = false;
                    RefreshColliderEnabledState();
                    return;
                }

                if (DayManager.Instance != null && !DayManager.Instance.CanConsumeStimulantCapsule(out string blockReason))
                {
                    if (Canal420AudioManager.Instance != null)
                    {
                        Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.PillBottleRattle, 0.45f);
                    }
                    string msg = !string.IsNullOrWhiteSpace(blockReason) ? blockReason : consumableCooldownSubtitle;
                    if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(msg))
                    {
                        InteractionSystem.Instance.ShowSubtitle(msg, 4.4f);
                    }
                    return;
                }

                if (consumableStepVisuals != null && consumedUseCount < consumableStepVisuals.Length && consumableStepVisuals[consumedUseCount] != null)
                {
                    consumableStepVisuals[consumedUseCount].SetActive(false);
                }

                string useThought = (consumableUseSubtitles != null && consumedUseCount < consumableUseSubtitles.Length && !string.IsNullOrWhiteSpace(consumableUseSubtitles[consumedUseCount]))
                    ? consumableUseSubtitles[consumedUseCount]
                    : thoughtSubtitle;

                bool firstConsume = consumedUseCount == 0;
                consumedUseCount++;

                if (Canal420AudioManager.Instance != null && interactSound != FoleySoundId.None)
                {
                    Canal420AudioManager.Instance.PlayFoley2D(interactSound, 0.90f);
                }

                if (firstConsume)
                {
                    onFirstInteract?.Invoke();
                }
                onAnyInteract?.Invoke();
                OnTaskCompleted?.Invoke(NormalizedTaskId);

                if (consumedUseCount >= maxUses)
                {
                    hasInteracted = true;
                    interactionEnabled = false;
                    RefreshColliderEnabledState();
                }

                if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(useThought))
                {
                    InteractionSystem.Instance.ShowSubtitle(useThought, subtitleDuration);
                }
                return;
            }

            if (!hasInteracted && NormalizedTaskId == "restaurar_energia_dia3"
                && DayManager.Instance != null
                && DayManager.Instance.IsDay3PrePillGateBlocked(out string prePillReason))
            {
                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley2D(missingRequirementSound, 0.72f);
                }
                if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(prePillReason))
                {
                    InteractionSystem.Instance.ShowSubtitle(prePillReason, 5.2f);
                }
                return;
            }

            if (!hasInteracted && !MeetsRequirement())
            {
                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley2D(missingRequirementSound, 0.80f);
                }

                if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(missingRequirementSubtitle))
                {
                    InteractionSystem.Instance.ShowSubtitle(missingRequirementSubtitle, subtitleDuration);
                }

                OnLockedRequirementAttempted?.Invoke(NormalizedTaskId);
                return;
            }

            bool firstTime = !hasInteracted;
            string thoughtToShow = (firstTime || string.IsNullOrWhiteSpace(repeatThoughtSubtitle))
                ? thoughtSubtitle
                : repeatThoughtSubtitle;

            if (!hasReadableDocument && Canal420AudioManager.Instance != null && interactSound != FoleySoundId.None)
            {
                Canal420AudioManager.Instance.PlayFoley2D(interactSound, 0.85f);
            }

            if (interactSound == FoleySoundId.AnsweringMachinePlay || interactSound == FoleySoundId.CassetteTapeHauntD2)
            {
                tapeSpinTimer = interactSound == FoleySoundId.CassetteTapeHauntD2 ? 6.2f : 4.2f;
            }

            if (firstTime)
            {
                hasInteracted = true;

                if (!string.IsNullOrWhiteSpace(grantsItemId))
                {
                    InventoryState.AddItem(grantsItemId);
                }

                if (visualToHideOnPickup != null)
                {
                    visualToHideOnPickup.SetActive(false);
                }

                if (visualToRevealWhenUnlocked != null)
                {
                    visualToRevealWhenUnlocked.SetActive(true);
                }

                onFirstInteract?.Invoke();
                OnTaskCompleted?.Invoke(NormalizedTaskId);

                if (disableAfterFirstUse)
                {
                    interactionEnabled = false;
                }

                RefreshColliderEnabledState();
            }

            onAnyInteract?.Invoke();

            if (linkedFaucet != null)
            {
                linkedFaucet.TriggerRunWater();
            }
            else if (NormalizedTaskId == "lavar_rosto")
            {
                linkedFaucet = FindAnyObjectByType<FaucetWaterController>();
                if (linkedFaucet != null) linkedFaucet.TriggerRunWater();
            }

            if (hasReadableDocument && InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(documentBody))
            {
                if (firstTime && interactSound != FoleySoundId.None && interactSound != FoleySoundId.PaperRustle && Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley2D(interactSound, 0.78f);
                }

                InteractionSystem.Instance.ShowDocument(documentTitle, documentBody, () =>
                {
                    if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(thoughtToShow))
                    {
                        if (firstTime && !string.IsNullOrWhiteSpace(suppressedTruthFlash))
                        {
                            InteractionSystem.Instance.ShowDissociationGlitchSubtitle(suppressedTruthFlash, thoughtToShow, subtitleDuration);
                        }
                        else
                        {
                            InteractionSystem.Instance.ShowSubtitle(thoughtToShow, subtitleDuration);
                        }
                    }
                }, documentStyle);
            }
            else if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(thoughtToShow))
            {
                if (firstTime && !string.IsNullOrWhiteSpace(suppressedTruthFlash))
                {
                    InteractionSystem.Instance.ShowDissociationGlitchSubtitle(suppressedTruthFlash, thoughtToShow, subtitleDuration);
                }
                else
                {
                    InteractionSystem.Instance.ShowSubtitle(thoughtToShow, subtitleDuration);
                }
            }
        }

        public void SetDissociationTruthFlash(string suppressedTruth)
        {
            suppressedTruthFlash = suppressedTruth ?? string.Empty;
        }

        public void ConfigureProximityRequirement(
            float maxDistanceMeters,
            bool requireFacing = false,
            Vector3 facingWorldDir = default,
            float minFacingDot = 0.45f)
        {
            maxInteractDistanceOverride = Mathf.Max(0f, maxDistanceMeters);
            requireFacingDirection = requireFacing;
            requiredFacingWorldDir = facingWorldDir.sqrMagnitude > 0.001f ? facingWorldDir.normalized : Vector3.forward;
            requiredFacingMinDot = Mathf.Clamp(minFacingDot, -1f, 0.99f);
        }

        public bool IsWithinInteractionConstraints(Vector3 eyeOrigin, Vector3 eyeForward, Vector3 targetWorldPoint)
        {
            if (maxInteractDistanceOverride > 0.05f)
            {
                Vector3 delta = targetWorldPoint - eyeOrigin;
                float horizDist = new Vector2(delta.x, delta.z).magnitude;
                if (delta.magnitude > maxInteractDistanceOverride || horizDist > maxInteractDistanceOverride)
                {
                    return false;
                }
            }

            if (requireFacingDirection && requiredFacingWorldDir.sqrMagnitude > 0.001f)
            {
                Vector3 flatFwd = new Vector3(eyeForward.x, 0f, eyeForward.z);
                if (flatFwd.sqrMagnitude > 0.001f)
                {
                    float dot = Vector3.Dot(flatFwd.normalized, requiredFacingWorldDir.normalized);
                    if (dot < requiredFacingMinDot)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void SetContainerAccessBlocked(bool blocked)
        {
            blockedByClosedContainer = blocked;
            RefreshColliderEnabledState();
        }

        private void RefreshColliderEnabledState()
        {
            if (TryGetComponent<Collider>(out Collider ownCol))
            {
                if (blockedByClosedContainer || (disableAfterFirstUse && hasInteracted) || (isMultiUseConsumable && !interactionEnabled))
                {
                    ownCol.enabled = false;
                }
                else if (interactionEnabled)
                {
                    ownCol.enabled = true;
                }
            }
        }

        public void SetInteractableState(bool enabledState, bool resetUsedState = false)
        {
            interactionEnabled = enabledState;
            if (resetUsedState)
            {
                hasInteracted = false;
                consumedUseCount = 0;
                alertSilencedApplied = false;
                alertCycleTimer = 0f;
                alertCycleBeepPlayed = false;
                tapeSpinTimer = 0f;
                if (visualToHideOnPickup != null) visualToHideOnPickup.SetActive(true);
                if (visualToRevealWhenUnlocked != null) visualToRevealWhenUnlocked.SetActive(false);
                if (consumableStepVisuals != null)
                {
                    for (int i = 0; i < consumableStepVisuals.Length; i++)
                    {
                        if (consumableStepVisuals[i] != null) consumableStepVisuals[i].SetActive(true);
                    }
                }
            }

            RefreshColliderEnabledState();
        }

        public void ConfigureAlertIndicator(Renderer ledRenderer, bool emitSound = true, float intervalSeconds = 2.65f)
        {
            alertLedRenderer = ledRenderer;
            emitAlertBeep = emitSound;
            alertBeepInterval = Mathf.Max(0.85f, intervalSeconds);
        }

        public void ConfigureAnsweringReels(Transform leftReel, Transform rightReel)
        {
            alertReelLeft = leftReel;
            alertReelRight = rightReel;
        }

        public void ConfigureLinkedFaucet(FaucetWaterController faucet)
        {
            linkedFaucet = faucet;
        }

        public void SetVisualTransitions(GameObject hideOnFirstUse, GameObject revealOnFirstUse)
        {
            visualToHideOnPickup = hideOnFirstUse;
            visualToRevealWhenUnlocked = revealOnFirstUse;
        }

        public void ConfigureTask(
            string id,
            string prompt,
            string subtitle,
            string repeatSubtitle = "",
            bool linear = true,
            string hudLabel = "",
            FoleySoundId sound = FoleySoundId.GenericTouch)
        {
            taskId = id;
            interactionPrompt = prompt;
            thoughtSubtitle = subtitle;
            repeatThoughtSubtitle = repeatSubtitle;
            isLinearObjective = linear;
            objectiveHudLabel = hudLabel;
            interactSound = sound;
            hasReadableDocument = false;
            grantsItemId = "";
            requiredItemId = "";
            disableAfterFirstUse = false;
            isMultiUseConsumable = false;
        }

        public void ConfigurePickup(
            string id,
            string itemToGrant,
            string prompt,
            string subtitle,
            GameObject meshToHide,
            FoleySoundId sound = FoleySoundId.KeyPickup,
            bool linear = false,
            string hudLabel = "")
        {
            taskId = id;
            grantsItemId = itemToGrant;
            interactionPrompt = prompt;
            thoughtSubtitle = subtitle;
            repeatThoughtSubtitle = "";
            visualToHideOnPickup = meshToHide;
            interactSound = sound;
            isLinearObjective = linear;
            objectiveHudLabel = hudLabel;
            hasReadableDocument = false;
            requiredItemId = "";
            disableAfterFirstUse = true;
            isMultiUseConsumable = false;
        }

        public void ConfigureItemReceiver(
            string id,
            string neededItem,
            string lockedPromptText,
            string unlockedPromptText,
            string missingThought,
            string completedThought,
            string repeatThought = "",
            FoleySoundId lockedSfx = FoleySoundId.DrawerLocked,
            FoleySoundId successSfx = FoleySoundId.FuseClackSpark,
            GameObject revealObj = null,
            bool alertBeep = false,
            bool linear = true,
            string hudLabel = "")
        {
            taskId = id;
            requiredItemId = neededItem;
            lockedInteractionPrompt = lockedPromptText;
            interactionPrompt = unlockedPromptText;
            missingRequirementSubtitle = missingThought;
            thoughtSubtitle = completedThought;
            repeatThoughtSubtitle = repeatThought;
            missingRequirementSound = lockedSfx;
            interactSound = successSfx;
            visualToRevealWhenUnlocked = revealObj;
            emitAlertBeep = alertBeep;
            isLinearObjective = linear;
            objectiveHudLabel = hudLabel;
            hasReadableDocument = false;
            grantsItemId = "";
            disableAfterFirstUse = false;
            isMultiUseConsumable = false;
        }

        public void ConfigureDocument(
            string id,
            string prompt,
            string docTitle,
            string docBody,
            string afterReadThought,
            string repeatThought = "",
            bool linear = false,
            string hudLabel = "",
            DocumentStyle style = DocumentStyle.HandwrittenNote,
            FoleySoundId extraSound = FoleySoundId.PaperRustle,
            string neededItem = "",
            string lockedPromptText = "",
            string missingThought = "")
        {
            taskId = id;
            interactionPrompt = prompt;
            hasReadableDocument = true;
            documentStyle = style;
            documentTitle = docTitle;
            documentBody = docBody;
            thoughtSubtitle = afterReadThought;
            repeatThoughtSubtitle = repeatThought;
            isLinearObjective = linear;
            objectiveHudLabel = hudLabel;
            interactSound = extraSound;
            requiredItemId = neededItem;
            lockedInteractionPrompt = lockedPromptText;
            missingRequirementSubtitle = missingThought;
            missingRequirementSound = FoleySoundId.DrawerLocked;
            grantsItemId = "";
            disableAfterFirstUse = false;
            isMultiUseConsumable = false;
        }

        public void ConfigureMultiUseConsumable(
            string id,
            string prompt,
            GameObject[] stepVisuals,
            string[] useSubtitles,
            string cooldownSubtitle,
            FoleySoundId sound = FoleySoundId.DryPillSwallow)
        {
            taskId = id;
            interactionPrompt = prompt;
            isMultiUseConsumable = true;
            consumableStepVisuals = stepVisuals;
            consumableUseSubtitles = useSubtitles;
            consumableCooldownSubtitle = cooldownSubtitle ?? string.Empty;
            thoughtSubtitle = (useSubtitles != null && useSubtitles.Length > 0) ? useSubtitles[0] : string.Empty;
            repeatThoughtSubtitle = string.Empty;
            interactSound = sound;
            isLinearObjective = false;
            objectiveHudLabel = string.Empty;
            hasReadableDocument = false;
            grantsItemId = string.Empty;
            requiredItemId = string.Empty;
            disableAfterFirstUse = false;
            consumedUseCount = 0;
        }
    }
}
