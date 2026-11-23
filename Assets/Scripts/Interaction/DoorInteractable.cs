using UnityEngine;
using UnityEngine.Events;
using Canal420.Audio;
using Canal420.Core;
using Canal420.Player;

namespace Canal420.Interaction
{
    public class DoorInteractable : MonoBehaviour, IInteractable
    {
        [Header("Estrutura da Porta")]
        [SerializeField] private Transform doorPivot;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float swingSpeed = 132f;
        [SerializeField] private float leafWidth = 0.914f;

        [Header("Estado de Tranca")]
        [SerializeField] private bool isLocked = false;
        [SerializeField] private bool canUnlockWithSafeKey = false;
        [SerializeField] private string requiredKeyItemId = "";
        [SerializeField] private string openPrompt = "Abrir porta";
        [SerializeField] private string closePrompt = "Fechar porta";
        [SerializeField] private string lockedPrompt = "Abrir porta";
        [TextArea(2, 3)]
        [SerializeField] private string lockedSubtitle = "Está trancada.";
        [TextArea(2, 3)]
        [SerializeField] private string unlockedSubtitle = "Destranquei a porta.";

        [Header("Eventos Narrativos")]
        [SerializeField] private UnityEvent onLockedAttempt;
        [SerializeField] private UnityEvent onUnlockedAndOpened;
        [SerializeField] private UnityEvent onDoorOpened;
        [SerializeField] private UnityEvent onDoorClosed;
        public static bool PlayerHasSafeKey { get; set; } = false;

        public event System.Action OnLockedAttemptEvent;
        public event System.Action OnUnlockedAndOpenedEvent;

        private bool isOpen = false;
        private bool canInteract = true;
        private bool isPartiallyAjar = false;
        private float closedYaw;
        private float currentYaw;
        private float defaultOpenAngle = 90f;
        private float defaultSwingSpeed = 132f;
        private float lockedRattleTimer = 0f;
        private bool yawInitialized = false;
        private bool ignoringPlayerCollision = false;
        private bool isSettledIdle = false;

        private Collider[] doorColliders;
        private PlayerController cachedPlayer;
        private Transform handlePivotA;
        private Transform handlePivotB;
        private float handlePressTimer = 0f;
        private float currentHandleAngle = 0f;
        private float slowLatchTimer = 0f;
        private float slowLatchDuration = 1.28f;

        public bool CanInteract => canInteract;
        public bool IsOpen => isOpen;
        public bool IsLocked => isLocked;
        public float LeafWidth => leafWidth;
        public Transform DoorPivot => doorPivot != null ? doorPivot : transform;
        public float ClosedYaw { get { EnsureInitialized(); return closedYaw; } }
        public float DefaultOpenAngle { get { EnsureInitialized(); return defaultOpenAngle; } }
        public float CurrentOpenDegrees { get { EnsureInitialized(); return Mathf.Abs(Mathf.DeltaAngle(closedYaw, currentYaw)); } }

        private void Awake()
        {
            EnsureInitialized();
            CacheDoorColliders();
        }

        private void EnsureInitialized()
        {
            if (doorPivot == null)
            {
                doorPivot = transform;
            }

            if (!yawInitialized)
            {
                defaultOpenAngle = openAngle;
                defaultSwingSpeed = swingSpeed;
                closedYaw = doorPivot.localEulerAngles.y;
                currentYaw = closedYaw;
                yawInitialized = true;
            }
        }

        private void CacheDoorColliders()
        {
            doorColliders = GetComponentsInChildren<Collider>(true);
            if (doorPivot != null)
            {
                Transform decor = doorPivot.Find("Detalhes_Folha_Porta");
                if (decor != null)
                {
                    handlePivotA = decor.Find("Alavanca_Pivo_A");
                    handlePivotB = decor.Find("Alavanca_Pivo_B");
                }
            }
        }

        private void TriggerHandlePress(float duration = 0.28f)
        {
            handlePressTimer = Mathf.Max(handlePressTimer, duration);
        }

        private void Update()
        {
            if (doorPivot == null)
                return;

            float targetYaw = isOpen ? (closedYaw + openAngle) : closedYaw;
            float delta = Mathf.Abs(Mathf.DeltaAngle(currentYaw, targetYaw));

            if (delta <= 0.05f
                && !ignoringPlayerCollision
                && lockedRattleTimer <= 0f
                && handlePressTimer <= 0f
                && slowLatchTimer <= 0f
                && Mathf.Abs(currentHandleAngle) <= 0.01f)
            {
                if (isSettledIdle)
                    return;

                currentYaw = targetYaw;
                currentHandleAngle = 0f;
                if (handlePivotA != null) handlePivotA.localEulerAngles = Vector3.zero;
                if (handlePivotB != null) handlePivotB.localEulerAngles = Vector3.zero;
                Vector3 settledEuler = doorPivot.localEulerAngles;
                settledEuler.y = currentYaw;
                doorPivot.localEulerAngles = settledEuler;
                isSettledIdle = true;
                return;
            }

            isSettledIdle = false;

            if (delta > 0.05f)
            {
                SetIgnorePlayerCollision(true);
                float easeFactor = Mathf.Clamp(0.32f + delta / 34f, 0.32f, 1.18f);
                currentYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, swingSpeed * easeFactor * Time.deltaTime);
            }
            else if (ignoringPlayerCollision)
            {
                currentYaw = targetYaw;
                SetIgnorePlayerCollision(false);
            }

            float rattleOffset = 0f;
            float targetHandleAngle = 0f;
            if (lockedRattleTimer > 0f)
            {
                lockedRattleTimer -= Time.deltaTime;
                float env = Mathf.Clamp01(lockedRattleTimer / 0.22f);
                rattleOffset = Mathf.Sin(Time.time * 62f) * 1.45f * env;
                targetHandleAngle = (9f + Mathf.Abs(Mathf.Sin(Time.time * 48f)) * 9f) * env;
            }
            else if (slowLatchTimer > 0f)
            {
                slowLatchTimer = Mathf.Max(0f, slowLatchTimer - Time.deltaTime);
                float u = 1f - Mathf.Clamp01(slowLatchTimer / Mathf.Max(0.25f, slowLatchDuration));
                if (u < 0.36f)
                {
                    // Slow downward press from inside against the locked bolt
                    targetHandleAngle = Mathf.SmoothStep(0f, 22f, u / 0.36f);
                }
                else if (u < 0.78f)
                {
                    // Held down, straining subtly twice against the exterior bolt
                    float strain = Mathf.Sin(((u - 0.36f) / 0.42f) * Mathf.PI * 2f);
                    targetHandleAngle = 21f + Mathf.Abs(strain) * 3.5f;
                    rattleOffset = strain * 0.42f;
                }
                else
                {
                    // Slowly released back up to rest
                    targetHandleAngle = Mathf.SmoothStep(21f, 0f, (u - 0.78f) / 0.22f);
                }
            }
            else if (handlePressTimer > 0f)
            {
                handlePressTimer -= Time.deltaTime;
                float env = Mathf.Sin(Mathf.Clamp01(handlePressTimer / 0.28f) * Mathf.PI);
                targetHandleAngle = 19f * env;
            }

            if (handlePivotA != null || handlePivotB != null)
            {
                float handleLerpSpeed = slowLatchTimer > 0f ? 68f : 220f;
                currentHandleAngle = Mathf.MoveTowards(currentHandleAngle, targetHandleAngle, handleLerpSpeed * Time.deltaTime);
                Vector3 handleRot = new Vector3(0f, 0f, currentHandleAngle);
                if (handlePivotA != null) handlePivotA.localEulerAngles = handleRot;
                if (handlePivotB != null) handlePivotB.localEulerAngles = handleRot;
            }

            Vector3 euler = doorPivot.localEulerAngles;
            euler.y = currentYaw + rattleOffset;
            doorPivot.localEulerAngles = euler;
        }

        private void SetIgnorePlayerCollision(bool ignore)
        {
            if (ignoringPlayerCollision == ignore)
                return;

            if (cachedPlayer == null)
            {
                cachedPlayer = FindAnyObjectByType<PlayerController>();
            }

            if (cachedPlayer == null || cachedPlayer.Controller == null)
            {
                if (!ignore) ignoringPlayerCollision = false;
                return;
            }

            if (ignore && !cachedPlayer.Controller.enabled)
            {
                return;
            }

            if (doorColliders == null || doorColliders.Length == 0)
            {
                CacheDoorColliders();
            }

            for (int i = 0; i < doorColliders.Length; i++)
            {
                Collider col = doorColliders[i];
                if (col != null && !col.isTrigger && (!ignore || col.enabled))
                {
                    Physics.IgnoreCollision(col, cachedPlayer.Controller, ignore);
                }
            }

            ignoringPlayerCollision = ignore;
        }

        private bool PlayerHasMatchingKey()
        {
            if (canUnlockWithSafeKey && PlayerHasSafeKey)
                return true;

            if (!string.IsNullOrWhiteSpace(requiredKeyItemId) && InventoryState.HasItem(requiredKeyItemId))
                return true;

            return false;
        }

        public string GetInteractionPrompt()
        {
            if (isLocked)
            {
                return PlayerHasMatchingKey() ? "Destrancar porta" : lockedPrompt;
            }

            return (isOpen && !isPartiallyAjar) ? closePrompt : openPrompt;
        }

        public void Interact(GameObject interactor)
        {
            if (!canInteract)
                return;

            EnsureInitialized();
            Vector3 soundPos = doorPivot != null ? doorPivot.position + Vector3.up * 1.1f : transform.position;

            if (isLocked)
            {
                if (PlayerHasMatchingKey())
                {
                    isLocked = false;
                    slowLatchTimer = 0f;

                    if (Canal420AudioManager.Instance != null)
                    {
                        Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.DoorUnlockKey, soundPos, 0.90f);
                    }

                    if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(unlockedSubtitle))
                    {
                        InteractionSystem.Instance.ShowSubtitle(unlockedSubtitle, 4.4f);
                    }

                    if (canUnlockWithSafeKey)
                    {
                        OpenToPartialAngle(88f, 26f, playSound: true);
                        canInteract = false;
                    }
                    else
                    {
                        openAngle = defaultOpenAngle;
                        isPartiallyAjar = false;
                        ResetSwingSpeed();
                        SetDoorOpen(true, playSound: true);
                    }

                    onUnlockedAndOpened?.Invoke();
                    OnUnlockedAndOpenedEvent?.Invoke();
                    return;
                }

                slowLatchTimer = 0f;
                lockedRattleTimer = 0.22f;
                TriggerHandlePress(0.24f);

                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.DoorLocked, soundPos, 0.85f);
                }

                if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(lockedSubtitle))
                {
                    InteractionSystem.Instance.ShowSubtitle(lockedSubtitle, 4.8f);
                }

                onLockedAttempt?.Invoke();
                OnLockedAttemptEvent?.Invoke();
                return;
            }

            if (isPartiallyAjar)
            {
                openAngle = defaultOpenAngle;
                isPartiallyAjar = false;
                ResetSwingSpeed();
                isOpen = true;
                TriggerHandlePress();
                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.DoorOpen, soundPos, 0.72f);
                }
                return;
            }

            openAngle = defaultOpenAngle;
            ResetSwingSpeed();
            SetDoorOpen(!isOpen, playSound: true);
        }

        public void OpenToPartialAngle(float ajarMagnitudeDeg, float customSwingSpeed = -1f, bool playSound = true)
        {
            EnsureInitialized();
            slowLatchTimer = 0f;
            float sign = Mathf.Abs(defaultOpenAngle) > 0.01f ? Mathf.Sign(defaultOpenAngle) : 1f;
            float clampedMag = Mathf.Clamp(Mathf.Abs(ajarMagnitudeDeg), 4f, Mathf.Max(26f, Mathf.Abs(defaultOpenAngle) + 6f));
            openAngle = sign * clampedMag;
            isPartiallyAjar = clampedMag < (Mathf.Abs(defaultOpenAngle) - 2f);
            if (customSwingSpeed > 0f)
            {
                swingSpeed = customSwingSpeed;
            }
            isOpen = true;

            if (playSound && Canal420AudioManager.Instance != null)
            {
                Vector3 soundPos = doorPivot != null ? doorPivot.position + Vector3.up * 1.1f : transform.position;
                Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.DoorOpen, soundPos, 0.45f);
            }
        }

        public void OpenFullForce(float customSwingSpeed = 420f, bool playSound = true)
        {
            EnsureInitialized();
            slowLatchTimer = 0f;
            openAngle = defaultOpenAngle;
            isPartiallyAjar = false;
            swingSpeed = Mathf.Max(20f, customSwingSpeed);
            isOpen = true;

            if (playSound && Canal420AudioManager.Instance != null)
            {
                Vector3 soundPos = doorPivot != null ? doorPivot.position + Vector3.up * 1.1f : transform.position;
                Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.DoorOpen, soundPos, 1.0f);
            }
        }

        public void SlamShutAndLock(float customSlamSpeed = 680f, bool playSound = true)
        {
            EnsureInitialized();
            canInteract = false;
            isLocked = true;
            isPartiallyAjar = false;
            swingSpeed = Mathf.Max(120f, customSlamSpeed);
            isOpen = false;
            slowLatchTimer = 0f;
            SetIgnorePlayerCollision(true);

            if (playSound && Canal420AudioManager.Instance != null)
            {
                Vector3 soundPos = doorPivot != null ? doorPivot.position + Vector3.up * 1.1f : transform.position;
                Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.HeavyDoorSlamTrap, soundPos, 1.0f);
                Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.DoorClose, soundPos, 0.92f);
            }
        }

        public void TriggerLockedRattle(float duration = 0.22f)
        {
            lockedRattleTimer = Mathf.Max(lockedRattleTimer, duration);
            isSettledIdle = false;
        }

        public void TriggerSlowInternalLatchStrain(float duration = 1.28f)
        {
            if (isOpen) return;
            slowLatchDuration = Mathf.Clamp(duration, 0.60f, 2.20f);
            slowLatchTimer = slowLatchDuration;
            isSettledIdle = false;
        }

        public void PoltergeistCreakAjar(float ajarMagnitudeDeg = 18f, bool playSound = true)
        {
            if (isLocked || isOpen) return;
            OpenToPartialAngle(ajarMagnitudeDeg, 16f, playSound);
        }

        public void SetSwingSpeed(float degreesPerSecond)
        {
            EnsureInitialized();
            swingSpeed = Mathf.Max(10f, degreesPerSecond);
        }

        public void ResetSwingSpeed()
        {
            EnsureInitialized();
            swingSpeed = defaultSwingSpeed > 10f ? defaultSwingSpeed : 132f;
        }

        public void SetDoorOpen(bool openState, bool playSound = false)
        {
            EnsureInitialized();
            if (isOpen == openState)
                return;

            isOpen = openState;
            slowLatchTimer = 0f;
            if (playSound)
            {
                TriggerHandlePress();
            }

            if (playSound && Canal420AudioManager.Instance != null)
            {
                Vector3 soundPos = doorPivot != null ? doorPivot.position + Vector3.up * 1.1f : transform.position;
                Canal420AudioManager.Instance.PlayFoley3D(
                    isOpen ? FoleySoundId.DoorOpen : FoleySoundId.DoorClose,
                    soundPos,
                    0.78f
                );
            }

            if (isOpen)
            {
                onDoorOpened?.Invoke();
            }
            else
            {
                onDoorClosed?.Invoke();
            }
        }

        public void ConfigureLockState(
            bool locked,
            bool allowSafeKey = false,
            bool forceCloseImmediately = false,
            string keyItemId = "")
        {
            EnsureInitialized();
            canInteract = true;
            isLocked = locked;
            canUnlockWithSafeKey = allowSafeKey;
            requiredKeyItemId = keyItemId ?? string.Empty;
            openAngle = defaultOpenAngle;
            isPartiallyAjar = false;
            ResetSwingSpeed();

            if (forceCloseImmediately)
            {
                isOpen = false;
                currentYaw = closedYaw;
                lockedRattleTimer = 0f;
                slowLatchTimer = 0f;
                SetIgnorePlayerCollision(false);
                if (doorPivot != null)
                {
                    Vector3 euler = doorPivot.localEulerAngles;
                    euler.y = closedYaw;
                    doorPivot.localEulerAngles = euler;
                }
            }
            isSettledIdle = false;
        }

        public void ConfigureDoor(
            Transform pivot,
            float targetOpenAngle,
            bool locked,
            bool allowSafeKey,
            string lockedMsg = "Está trancada.",
            string unlockedMsg = "Destranquei a porta.",
            string keyItemId = "",
            float doorLeafWidth = -1f)
        {
            doorPivot = pivot != null ? pivot : transform;
            openAngle = targetOpenAngle;
            defaultOpenAngle = targetOpenAngle;
            isPartiallyAjar = false;
            isLocked = locked;
            canUnlockWithSafeKey = allowSafeKey;
            requiredKeyItemId = keyItemId ?? string.Empty;
            lockedSubtitle = lockedMsg;
            unlockedSubtitle = unlockedMsg;
            if (doorLeafWidth > 0.05f)
            {
                leafWidth = doorLeafWidth;
            }
            else
            {
                Transform leafChild = doorPivot.Find("Folha_Porta");
                if (leafChild != null && leafChild.localScale.x > 0.25f)
                {
                    leafWidth = leafChild.localScale.x;
                }
                else if (leafWidth <= 0.05f)
                {
                    leafWidth = 0.914f;
                }
            }
            yawInitialized = false;
            isSettledIdle = false;
            EnsureInitialized();
            CacheDoorColliders();
        }

        public Vector3 GetClosedDoorCenterWorld()
        {
            return GetClosedDoorCenterWorld(leafWidth);
        }

        public Vector3 GetClosedDoorCenterWorld(float customLeafWidth)
        {
            EnsureInitialized();
            if (doorPivot == null) return transform.position;
            float resolvedWidth = customLeafWidth > 0.05f
                ? customLeafWidth
                : (leafWidth > 0.05f ? leafWidth : 0.914f);
            Quaternion closedRot = doorPivot.parent != null
                ? doorPivot.parent.rotation * Quaternion.Euler(0f, closedYaw, 0f)
                : Quaternion.Euler(0f, closedYaw, 0f);
            return doorPivot.position + (closedRot * Vector3.right) * (resolvedWidth * 0.5f);
        }

        public Vector3 GetInwardNormalWorld()
        {
            EnsureInitialized();
            if (doorPivot == null) return Vector3.forward;
            Quaternion closedRot = doorPivot.parent != null
                ? doorPivot.parent.rotation * Quaternion.Euler(0f, closedYaw, 0f)
                : Quaternion.Euler(0f, closedYaw, 0f);
            Vector3 closedFwd = closedRot * Vector3.forward;
            closedFwd.y = 0f;
            if (closedFwd.sqrMagnitude < 0.001f) return Vector3.forward;
            closedFwd.Normalize();
            return defaultOpenAngle >= 0f ? -closedFwd : closedFwd;
        }

        public void SetInteractable(bool enabledState)
        {
            canInteract = enabledState;
        }
    }
}
