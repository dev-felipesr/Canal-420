using System;
using System.Collections.Generic;
using UnityEngine;
using Canal420.Audio;
using Canal420.Core;
using Canal420.Player;

namespace Canal420.Interaction
{
    public enum ContainerMotionType
    {
        SlideDrawer = 0,
        SwingHinge = 1
    }

    public class AnimatedContainerInteractable : MonoBehaviour, IInteractable
    {
        public static event Action<string> OnContainerFirstOpened;
        public static event Action<string> OnLockedContainerAttempted;

        [Header("Identificação e Tipo de Movimento")]
        [SerializeField] private string containerId = "";
        [SerializeField] private ContainerMotionType motionType = ContainerMotionType.SlideDrawer;
        [SerializeField] private Transform movingPart;
        [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, 0f, 0.28f);
        [SerializeField] private Vector3 openLocalEuler = new Vector3(0f, 80f, 0f);

        [Header("Folha Secundária Opcional (Armários de 2 Portas)")]
        [SerializeField] private Transform secondaryMovingPart;
        [SerializeField] private Vector3 secondaryOpenLocalEuler = new Vector3(0f, -80f, 0f);

        [Header("Pose Base Fechada (Serializada)")]
        [SerializeField] private bool hasStoredBasePose = false;
        [SerializeField] private Vector3 baseLocalPos;
        [SerializeField] private Quaternion baseLocalRot = Quaternion.identity;
        [SerializeField] private Vector3 secondaryBaseLocalPos;
        [SerializeField] private Quaternion secondaryBaseLocalRot = Quaternion.identity;

        [Header("Velocidade e Textos de Mira")]
        [SerializeField] private float animationSpeed = 3.1f;
        [SerializeField] private string openPrompt = "Abrir";
        [SerializeField] private string closePrompt = "Fechar";
        [TextArea(2, 4)]
        [SerializeField] private string firstOpenThought = "";

        [Header("Tranca Permanente ou por Chave")]
        [SerializeField] private bool isPermanentlyLocked = false;
        [SerializeField] private string requiredKeyItemId = "";
        [SerializeField] private string lockedPrompt = "Tentar abrir";
        [SerializeField] private string unlockAndOpenPrompt = "";
        [TextArea(2, 4)]
        [SerializeField] private string lockedSubtitle = "";
        [TextArea(2, 4)]
        [SerializeField] private string repeatLockedSubtitle = "";
        [TextArea(2, 4)]
        [SerializeField] private string unlockedSubtitle = "";

        [Header("Estado Especial Dia 2 (Porta Entreaberta Sozinha)")]
        [SerializeField] private bool startAjarOnDay2 = false;
        [SerializeField] private float day2AjarFraction = 0.28f;
        [SerializeField] private string day2AjarPrompt = "Encostar a porta entreaberta";
        [TextArea(2, 4)]
        [SerializeField] private string day2AjarCloseThought = "";
        [SerializeField] private string day2AjarTaskId = "";

        [Header("Áudio Tátil (Foley)")]
        [SerializeField] private FoleySoundId openSound = FoleySoundId.DrawerSlide;
        [SerializeField] private FoleySoundId closeSound = FoleySoundId.DrawerSlide;
        [SerializeField] private FoleySoundId lockedSound = FoleySoundId.DrawerLocked;
        [SerializeField] private FoleySoundId unlockSound = FoleySoundId.DoorUnlockKey;

        [Header("Conteúdo Interno (Só Interagível Quando Aberto)")]
        [SerializeField] private List<InspectableObject> interiorInspectables = new List<InspectableObject>();
        [SerializeField] private List<Transform> syncedDynamicItems = new List<Transform>();
        [SerializeField] private List<Vector3> syncedLocalOffsets = new List<Vector3>();
        private bool isOpen = false;
        private bool isUnlockedByKey = false;
        private bool hasAttemptedLocked = false;
        private bool hasOpenedOnce = false;
        private bool isDay2AjarActive = false;

        private float currentProgress = 0f;
        private float targetProgress = 0f;
        private float currentSecondaryProgress = 0f;
        private float targetSecondaryProgress = 0f;
        private float transitionStartPrimary = 0f;
        private float transitionStartSecondary = 0f;
        private float transitionElapsed = 0f;
        private float transitionDuration = 0f;
        private bool isAnimatingTransition = false;
        private float lockedRattleTimer = 0f;

        private Collider[] movingColliders;
        private CharacterController cachedPlayerCC;
        private bool ignoringPlayerCollision = false;

        public bool CanInteract => true;
        public bool IsOpen => isOpen && !isDay2AjarActive;
        public bool IsContentsAccessible => isOpen && !isDay2AjarActive && currentProgress >= 0.72f;

        public string NormalizedId => string.IsNullOrWhiteSpace(containerId)
            ? gameObject.name.Trim().ToLowerInvariant()
            : containerId.Trim().ToLowerInvariant();

        public bool HasActiveInteriorItem
        {
            get
            {
                if (!IsContentsAccessible || interiorInspectables == null)
                    return false;

                for (int i = 0; i < interiorInspectables.Count; i++)
                {
                    InspectableObject item = interiorInspectables[i];
                    if (item != null && item.gameObject.activeInHierarchy && item.CanInteract)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private void Awake()
        {
            if (movingPart == null)
            {
                movingPart = transform;
            }

            CaptureBasePoseIfNeeded();
            CacheMovingColliders();
            DiscoverChildInspectables();
            ApplyPose(currentProgress, currentSecondaryProgress, rattleOffset: 0f);
            RefreshInteriorAccess();
        }

        private void OnDisable()
        {
            SetIgnorePlayerCollision(false);
        }

        private void CaptureBasePoseIfNeeded()
        {
            if (hasStoredBasePose)
                return;

            if (movingPart != null)
            {
                baseLocalPos = movingPart.localPosition;
                baseLocalRot = movingPart.localRotation;
            }

            if (secondaryMovingPart != null)
            {
                secondaryBaseLocalPos = secondaryMovingPart.localPosition;
                secondaryBaseLocalRot = secondaryMovingPart.localRotation;
            }

            hasStoredBasePose = true;
        }

        private void CacheMovingColliders()
        {
            List<Collider> cols = new List<Collider>();
            if (movingPart != null)
            {
                cols.AddRange(movingPart.GetComponentsInChildren<Collider>(true));
            }
            if (secondaryMovingPart != null)
            {
                cols.AddRange(secondaryMovingPart.GetComponentsInChildren<Collider>(true));
            }

            for (int i = cols.Count - 1; i >= 0; i--)
            {
                if (cols[i] == null || cols[i].GetComponentInParent<InspectableObject>() != null)
                {
                    cols.RemoveAt(i);
                }
            }

            movingColliders = cols.ToArray();
        }

        private void DiscoverChildInspectables()
        {
            InspectableObject[] children = GetComponentsInChildren<InspectableObject>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && !interiorInspectables.Contains(children[i]))
                {
                    interiorInspectables.Add(children[i]);
                }
            }
        }

        private void Update()
        {
            bool needsPoseUpdate = false;
            float rattle = 0f;

            if (lockedRattleTimer > 0f)
            {
                lockedRattleTimer = Mathf.Max(0f, lockedRattleTimer - Time.deltaTime);
                float normalized = lockedRattleTimer / 0.28f;
                rattle = Mathf.Sin((1f - normalized) * 54f) * normalized * normalized;
                needsPoseUpdate = true;
            }

            if (isAnimatingTransition)
            {
                if (motionType == ContainerMotionType.SwingHinge)
                {
                    SetIgnorePlayerCollision(true);
                }

                transitionElapsed += Time.deltaTime;
                float rawT = transitionDuration > 0.0001f ? Mathf.Clamp01(transitionElapsed / transitionDuration) : 1f;
                float easedT = Mathf.SmoothStep(0f, 1f, rawT);

                currentProgress = Mathf.Lerp(transitionStartPrimary, targetProgress, easedT);
                currentSecondaryProgress = Mathf.Lerp(transitionStartSecondary, targetSecondaryProgress, easedT);
                needsPoseUpdate = true;

                if (rawT >= 1f)
                {
                    currentProgress = targetProgress;
                    currentSecondaryProgress = targetSecondaryProgress;
                    isAnimatingTransition = false;
                }
            }
            else if (ignoringPlayerCollision)
            {
                SetIgnorePlayerCollision(false);
            }

            if (needsPoseUpdate)
            {
                ApplyPose(currentProgress, currentSecondaryProgress, rattle);
                RefreshInteriorAccess();
            }
        }

        private void StartSmoothTransition(float newPrimaryTarget, float newSecondaryTarget, float minDuration = 0.24f)
        {
            transitionStartPrimary = currentProgress;
            transitionStartSecondary = currentSecondaryProgress;
            targetProgress = Mathf.Clamp01(newPrimaryTarget);
            targetSecondaryProgress = Mathf.Clamp01(newSecondaryTarget);

            float maxDelta = Mathf.Max(
                Mathf.Abs(targetProgress - transitionStartPrimary),
                Mathf.Abs(targetSecondaryProgress - transitionStartSecondary)
            );

            if (maxDelta <= 0.0005f)
            {
                currentProgress = targetProgress;
                currentSecondaryProgress = targetSecondaryProgress;
                isAnimatingTransition = false;
                ApplyPose(currentProgress, currentSecondaryProgress, 0f);
                return;
            }

            float speed = Mathf.Max(0.8f, animationSpeed);
            transitionDuration = Mathf.Max(minDuration, maxDelta / speed);
            transitionElapsed = 0f;
            isAnimatingTransition = true;
        }

        private void ApplyPose(float primaryRaw, float secondaryRaw, float rattleOffset)
        {
            float easedPrimary = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(primaryRaw));
            float easedSecondary = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(secondaryRaw));

            if (movingPart != null)
            {
                if (motionType == ContainerMotionType.SlideDrawer)
                {
                    Vector3 slideDir = openLocalOffset.sqrMagnitude > 0.0001f ? openLocalOffset.normalized : Vector3.forward;
                    Vector3 rattleVec = slideDir * (rattleOffset * 0.012f);
                    movingPart.localPosition = baseLocalPos + (openLocalOffset * easedPrimary) + rattleVec;
                    movingPart.localRotation = baseLocalRot;
                }
                else
                {
                    Vector3 rotAxis = openLocalEuler.sqrMagnitude > 0.001f ? openLocalEuler.normalized : Vector3.up;
                    Quaternion targetRot = baseLocalRot * Quaternion.Euler(openLocalEuler * easedPrimary + rotAxis * (rattleOffset * 1.8f));
                    movingPart.localRotation = targetRot;
                    movingPart.localPosition = baseLocalPos;
                }
            }

            if (secondaryMovingPart != null && motionType == ContainerMotionType.SwingHinge)
            {
                Vector3 secAxis = secondaryOpenLocalEuler.sqrMagnitude > 0.001f ? secondaryOpenLocalEuler.normalized : Vector3.up;
                Quaternion secRot = secondaryBaseLocalRot * Quaternion.Euler(
                    secondaryOpenLocalEuler * easedSecondary + secAxis * (rattleOffset * 1.4f));
                secondaryMovingPart.localRotation = secRot;
                secondaryMovingPart.localPosition = secondaryBaseLocalPos;
            }

            SyncDynamicItemTransforms();
        }

        private void SyncDynamicItemTransforms()
        {
            if (movingPart == null || syncedDynamicItems == null || syncedLocalOffsets == null)
                return;

            int count = Mathf.Min(syncedDynamicItems.Count, syncedLocalOffsets.Count);
            for (int i = 0; i < count; i++)
            {
                Transform itemTr = syncedDynamicItems[i];
                if (itemTr != null)
                {
                    itemTr.position = movingPart.TransformPoint(syncedLocalOffsets[i]);
                }
            }
        }

        public void RefreshInteriorAccess()
        {
            bool blocked = !IsContentsAccessible;
            if (interiorInspectables == null)
                return;

            for (int i = 0; i < interiorInspectables.Count; i++)
            {
                InspectableObject insp = interiorInspectables[i];
                if (insp != null)
                {
                    insp.SetContainerAccessBlocked(blocked);
                }
            }
        }

        private void SetIgnorePlayerCollision(bool ignore)
        {
            if (ignoringPlayerCollision == ignore)
                return;

            if (cachedPlayerCC == null)
            {
                PlayerController pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) cachedPlayerCC = pc.Controller;
            }

            if (cachedPlayerCC != null && movingColliders != null && (!ignore || cachedPlayerCC.enabled))
            {
                for (int i = 0; i < movingColliders.Length; i++)
                {
                    Collider col = movingColliders[i];
                    if (col != null && (!ignore || col.enabled))
                    {
                        Physics.IgnoreCollision(col, cachedPlayerCC, ignore);
                    }
                }
            }

            ignoringPlayerCollision = ignore;
        }

        private Vector3 GetAudioOrigin()
        {
            if (movingPart != null && secondaryMovingPart != null)
            {
                return (movingPart.position + secondaryMovingPart.position) * 0.5f;
            }
            if (movingPart != null)
            {
                return movingPart.position;
            }
            return transform.position;
        }

        private bool IsCurrentlyLocked(out bool canUnlockNow)
        {
            canUnlockNow = false;
            if (isPermanentlyLocked)
                return true;

            if (!string.IsNullOrWhiteSpace(requiredKeyItemId) && !isUnlockedByKey)
            {
                canUnlockNow = InventoryState.HasItem(requiredKeyItemId);
                return !canUnlockNow;
            }

            return false;
        }

        public string GetInteractionPrompt()
        {
            if (isDay2AjarActive)
            {
                return string.IsNullOrWhiteSpace(day2AjarPrompt) ? closePrompt : day2AjarPrompt;
            }

            bool locked = IsCurrentlyLocked(out bool canUnlockNow);
            if (locked)
            {
                return string.IsNullOrWhiteSpace(lockedPrompt) ? openPrompt : lockedPrompt;
            }

            if (!isUnlockedByKey && !string.IsNullOrWhiteSpace(requiredKeyItemId) && canUnlockNow)
            {
                return string.IsNullOrWhiteSpace(unlockAndOpenPrompt) ? openPrompt : unlockAndOpenPrompt;
            }

            return isOpen ? closePrompt : openPrompt;
        }

        public void Interact(GameObject interactor)
        {
            if (isDay2AjarActive)
            {
                isDay2AjarActive = false;
                isOpen = false;
                StartSmoothTransition(0f, 0f, minDuration: 0.45f);

                if (Canal420AudioManager.Instance != null && closeSound != FoleySoundId.None)
                {
                    Canal420AudioManager.Instance.PlayFoley3D(closeSound, GetAudioOrigin(), 0.85f);
                }

                if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(day2AjarCloseThought))
                {
                    InteractionSystem.Instance.ShowSubtitle(day2AjarCloseThought, 5.6f);
                }

                if (!string.IsNullOrWhiteSpace(day2AjarTaskId))
                {
                    OnContainerFirstOpened?.Invoke(day2AjarTaskId.Trim().ToLowerInvariant());
                }

                RefreshInteriorAccess();
                return;
            }

            bool locked = IsCurrentlyLocked(out bool canUnlockNow);
            if (locked)
            {
                lockedRattleTimer = 0.28f;

                if (Canal420AudioManager.Instance != null && lockedSound != FoleySoundId.None)
                {
                    Canal420AudioManager.Instance.PlayFoley3D(lockedSound, GetAudioOrigin(), 0.88f);
                }

                string msg = (!hasAttemptedLocked || string.IsNullOrWhiteSpace(repeatLockedSubtitle))
                    ? lockedSubtitle
                    : repeatLockedSubtitle;

                hasAttemptedLocked = true;

                if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(msg))
                {
                    InteractionSystem.Instance.ShowSubtitle(msg, 5.6f);
                }

                OnLockedContainerAttempted?.Invoke(NormalizedId);
                return;
            }

            if (!isUnlockedByKey && !string.IsNullOrWhiteSpace(requiredKeyItemId) && canUnlockNow)
            {
                isUnlockedByKey = true;
                isOpen = true;
                StartSmoothTransition(1f, 1f);
                hasOpenedOnce = true;

                if (Canal420AudioManager.Instance != null)
                {
                    if (unlockSound != FoleySoundId.None)
                        Canal420AudioManager.Instance.PlayFoley2D(unlockSound, 0.84f);
                    if (openSound != FoleySoundId.None)
                        Canal420AudioManager.Instance.PlayFoley3D(openSound, GetAudioOrigin(), 0.82f);
                }

                if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(unlockedSubtitle))
                {
                    InteractionSystem.Instance.ShowSubtitle(unlockedSubtitle, 5.2f);
                }

                OnContainerFirstOpened?.Invoke(NormalizedId);
                return;
            }

            isOpen = !isOpen;
            float targetVal = isOpen ? 1f : 0f;
            StartSmoothTransition(targetVal, targetVal);

            FoleySoundId sfx = isOpen ? openSound : closeSound;
            if (Canal420AudioManager.Instance != null && sfx != FoleySoundId.None)
            {
                Canal420AudioManager.Instance.PlayFoley3D(sfx, GetAudioOrigin(), 0.84f);
            }

            if (isOpen && !hasOpenedOnce)
            {
                hasOpenedOnce = true;
                if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(firstOpenThought))
                {
                    InteractionSystem.Instance.ShowSubtitle(firstOpenThought, 5.2f);
                }
                OnContainerFirstOpened?.Invoke(NormalizedId);
            }

            RefreshInteriorAccess();
        }

        public void ResetForDay(GameDay day)
        {
            if (movingPart == null) movingPart = transform;
            CaptureBasePoseIfNeeded();
            DiscoverChildInspectables();

            isUnlockedByKey = false;
            hasAttemptedLocked = false;
            hasOpenedOnce = false;
            isAnimatingTransition = false;
            lockedRattleTimer = 0f;
            SetIgnorePlayerCollision(false);

            if (startAjarOnDay2 && day == GameDay.Day2 && !isPermanentlyLocked)
            {
                isDay2AjarActive = true;
                isOpen = true;
                float ajar = Mathf.Clamp(day2AjarFraction, 0.12f, 0.55f);
                currentProgress = ajar;
                targetProgress = ajar;
                currentSecondaryProgress = ajar * 0.40f;
                targetSecondaryProgress = currentSecondaryProgress;
            }
            else
            {
                isDay2AjarActive = false;
                isOpen = false;
                currentProgress = 0f;
                targetProgress = 0f;
                currentSecondaryProgress = 0f;
                targetSecondaryProgress = 0f;
            }

            ApplyPose(currentProgress, currentSecondaryProgress, rattleOffset: 0f);
            RefreshInteriorAccess();
        }

        public void ConfigureSlideDrawer(
            string id,
            Transform drawerTransform,
            Vector3 slideOffset,
            string promptOpen,
            string promptClose,
            FoleySoundId slideSfx = FoleySoundId.DrawerSlide,
            string thoughtOnFirstOpen = "",
            float speed = 3.2f)
        {
            containerId = id;
            motionType = ContainerMotionType.SlideDrawer;
            movingPart = drawerTransform != null ? drawerTransform : transform;
            secondaryMovingPart = null;
            openLocalOffset = slideOffset;
            openPrompt = promptOpen;
            closePrompt = promptClose;
            openSound = slideSfx;
            closeSound = slideSfx;
            firstOpenThought = thoughtOnFirstOpen;
            animationSpeed = speed;
            isPermanentlyLocked = false;
            requiredKeyItemId = "";
            startAjarOnDay2 = false;

            baseLocalPos = movingPart.localPosition;
            baseLocalRot = movingPart.localRotation;
            hasStoredBasePose = true;
        }

        public void ConfigureSwingCabinet(
            string id,
            Transform primaryDoorPivot,
            Vector3 primaryOpenEuler,
            string promptOpen,
            string promptClose,
            FoleySoundId doorSfx = FoleySoundId.CupboardDoor,
            Transform secondDoorPivot = null,
            Vector3 secondOpenEuler = default,
            string thoughtOnFirstOpen = "",
            float speed = 2.9f)
        {
            containerId = id;
            motionType = ContainerMotionType.SwingHinge;
            movingPart = primaryDoorPivot != null ? primaryDoorPivot : transform;
            openLocalEuler = primaryOpenEuler;
            secondaryMovingPart = secondDoorPivot;
            secondaryOpenLocalEuler = secondOpenEuler;
            openPrompt = promptOpen;
            closePrompt = promptClose;
            openSound = doorSfx;
            closeSound = doorSfx;
            firstOpenThought = thoughtOnFirstOpen;
            animationSpeed = speed;
            isPermanentlyLocked = false;
            requiredKeyItemId = "";
            startAjarOnDay2 = false;

            baseLocalPos = movingPart.localPosition;
            baseLocalRot = movingPart.localRotation;
            if (secondaryMovingPart != null)
            {
                secondaryBaseLocalPos = secondaryMovingPart.localPosition;
                secondaryBaseLocalRot = secondaryMovingPart.localRotation;
            }
            hasStoredBasePose = true;
        }

        public void ConfigurePermanentLock(
            string promptWhenLocked,
            string firstLockedLine,
            string repeatLockedLine = "",
            FoleySoundId sfx = FoleySoundId.DrawerLocked)
        {
            isPermanentlyLocked = true;
            requiredKeyItemId = "";
            lockedPrompt = promptWhenLocked;
            lockedSubtitle = firstLockedLine;
            repeatLockedSubtitle = repeatLockedLine;
            lockedSound = sfx;
        }

        public void ConfigureKeyLock(
            string neededKeyId,
            string promptWhenLocked,
            string promptWhenHasKey,
            string lockedLine,
            string unlockedLine = "",
            FoleySoundId lockedSfx = FoleySoundId.DrawerLocked,
            FoleySoundId unlockedSfx = FoleySoundId.DoorUnlockKey)
        {
            isPermanentlyLocked = false;
            requiredKeyItemId = neededKeyId;
            lockedPrompt = promptWhenLocked;
            unlockAndOpenPrompt = promptWhenHasKey;
            lockedSubtitle = lockedLine;
            repeatLockedSubtitle = lockedLine;
            unlockedSubtitle = unlockedLine;
            lockedSound = lockedSfx;
            unlockSound = unlockedSfx;
        }

        public void ConfigureDay2AjarBehavior(
            float ajarFraction,
            string ajarPrompt,
            string closeThought,
            string optionalTaskId = "")
        {
            startAjarOnDay2 = true;
            day2AjarFraction = ajarFraction;
            day2AjarPrompt = ajarPrompt;
            day2AjarCloseThought = closeThought;
            day2AjarTaskId = optionalTaskId;
        }

        public void RegisterInteriorObject(InspectableObject inspectable, bool followMovingPartPosition = false)
        {
            if (inspectable == null)
                return;

            if (!interiorInspectables.Contains(inspectable))
            {
                interiorInspectables.Add(inspectable);
            }

            if (followMovingPartPosition && movingPart != null && !inspectable.transform.IsChildOf(movingPart))
            {
                if (!syncedDynamicItems.Contains(inspectable.transform))
                {
                    syncedDynamicItems.Add(inspectable.transform);
                    syncedLocalOffsets.Add(movingPart.InverseTransformPoint(inspectable.transform.position));
                }
            }

            inspectable.SetContainerAccessBlocked(true);
        }
    }
}
