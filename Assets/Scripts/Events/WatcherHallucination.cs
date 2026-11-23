using System.Collections;
using UnityEngine;
using Canal420.Audio;
using Canal420.Core;
using Canal420.Effects;
using Canal420.Player;
using Canal420.UI;

namespace Canal420.Events
{
    public class WatcherHallucination : MonoBehaviour
    {
        [Header("Alvo Visual e Posições")]
        [SerializeField] private GameObject silhouetteObject;
        [SerializeField] private Transform peekPoint;
        [SerializeField] private Transform hidePoint;
        [SerializeField] private Light[] nearbyLightsToPulse;

        [Header("Detecção de Olhar & Fallback Anti-Quebra")]
        [SerializeField] private float eyeHeightOffset = 1.46f;
        [SerializeField] private float maxDetectDistance = 9.0f;
        [SerializeField] private float directGazeDotThreshold = 0.86f;
        [SerializeField] private float flankFailSafeDistance = 2.25f;
        [SerializeField] private float triggerDelayOnGaze = 0.09f;
        [SerializeField] private float retreatDuration = 0.24f;
        [SerializeField] private bool hideViaDarkBlackout = false;

        private bool isArmed = false;
        private bool isRetreating = false;
        private bool hasTriggered = false;
        private bool hasSuppressedCorridorSputter = false;
        private float gazeTimer = 0f;
        private float armedElapsed = 0f;

        private HumanoidSpasmAnimator spasmAnim;
        private PlayerController cachedPlayer;
        private FlashlightController cachedFlashlight;
        private readonly RaycastHit[] losHitBuffer = new RaycastHit[32];
        private bool[] pulsedLightsWereLit = null;
        private bool hasUnrestoredPulsedLights = false;
        private bool hasUnrestoredFlashlightBlackout = false;

        public bool HasTriggered => hasTriggered;
        public float EyeHeightOffset => eyeHeightOffset;

        private void Awake()
        {
            cachedPlayer = FindAnyObjectByType<PlayerController>();
            if (silhouetteObject != null)
            {
                spasmAnim = silhouetteObject.GetComponent<HumanoidSpasmAnimator>();
                silhouetteObject.SetActive(false);
            }
        }

        public void ConfigureWatcher(
            GameObject silhouette,
            Transform peekPt,
            Transform hidePt,
            Light[] pulseLights = null,
            bool blackoutRetreat = false,
            float targetEyeHeight = 1.46f)
        {
            silhouetteObject = silhouette;
            peekPoint = peekPt;
            hidePoint = hidePt;
            nearbyLightsToPulse = pulseLights;
            hideViaDarkBlackout = blackoutRetreat;
            eyeHeightOffset = Mathf.Clamp(targetEyeHeight, 0.45f, 2.20f);

            if (silhouetteObject != null)
            {
                spasmAnim = silhouetteObject.GetComponent<HumanoidSpasmAnimator>();
                silhouetteObject.SetActive(false);
            }
        }

        public void SetLightsToPulse(Light[] lights)
        {
            nearbyLightsToPulse = lights;
        }

        private void SetCorridorSputterSuppressed(bool suppressed)
        {
            if (hasSuppressedCorridorSputter == suppressed) return;
            hasSuppressedCorridorSputter = suppressed;
            if (DayManager.Instance != null)
            {
                DayManager.Instance.SetCorridorAutoSputterSuppressed(suppressed);
            }
        }

        private void RestorePulsedLightsIfNeeded()
        {
            if (hasUnrestoredFlashlightBlackout)
            {
                hasUnrestoredFlashlightBlackout = false;
                if (cachedFlashlight != null && (DayManager.Instance == null || !DayManager.Instance.IsTransitioning))
                {
                    cachedFlashlight.ForceBlackout(false);
                }
            }

            if (!hasUnrestoredPulsedLights) return;
            hasUnrestoredPulsedLights = false;
            if (nearbyLightsToPulse == null || pulsedLightsWereLit == null) return;
            if (DayManager.Instance != null && DayManager.Instance.IsTransitioning) return;

            int count = Mathf.Min(nearbyLightsToPulse.Length, pulsedLightsWereLit.Length);
            for (int i = 0; i < count; i++)
            {
                if (nearbyLightsToPulse[i] != null && pulsedLightsWereLit[i])
                {
                    nearbyLightsToPulse[i].enabled = true;
                    Canal420.Core.DayManager.SyncLampFixtureVisual(nearbyLightsToPulse[i], true, nearbyLightsToPulse[i].color, 0.85f);
                }
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            RestorePulsedLightsIfNeeded();
            SetCorridorSputterSuppressed(false);
            isRetreating = false;
        }

        public void ArmWatcher()
        {
            if (silhouetteObject == null || peekPoint == null) return;

            StopAllCoroutines();
            RestorePulsedLightsIfNeeded();
            SetCorridorSputterSuppressed(false);
            hasTriggered = false;
            isRetreating = false;
            isArmed = true;
            gazeTimer = 0f;
            armedElapsed = 0f;

            silhouetteObject.transform.position = peekPoint.position;
            silhouetteObject.transform.rotation = peekPoint.rotation;
            silhouetteObject.SetActive(true);

            if (spasmAnim == null) spasmAnim = silhouetteObject.GetComponent<HumanoidSpasmAnimator>();
            if (spasmAnim != null) spasmAnim.SetMode(SpasmAnimMode.LurkPeek);
        }

        public void DisarmAndHide()
        {
            StopAllCoroutines();
            RestorePulsedLightsIfNeeded();
            SetCorridorSputterSuppressed(false);
            isArmed = false;
            isRetreating = false;
            gazeTimer = 0f;
            if (spasmAnim != null) spasmAnim.SetMode(SpasmAnimMode.Idle);
            if (silhouetteObject != null) silhouetteObject.SetActive(false);
        }

        private void Update()
        {
            if (!isArmed || isRetreating || hasTriggered || silhouetteObject == null)
                return;

            if (ObjectiveHUD.IsBlockingModalOpen ||
                (DayManager.Instance != null && (DayManager.Instance.IsTransitioning || DayManager.Instance.IsDemoEnded)))
            {
                gazeTimer = 0f;
                return;
            }

            armedElapsed += Time.deltaTime;
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 eyeTarget = peekPoint.position + Vector3.up * eyeHeightOffset;
            Vector3 toTarget = eyeTarget - cam.transform.position;
            float dist = toTarget.magnitude;

            if (dist > maxDetectDistance)
            {
                gazeTimer = 0f;
                return;
            }

            Vector3 dir = toTarget / Mathf.Max(0.01f, dist);
            float dot = Vector3.Dot(cam.transform.forward, dir);

            bool unobstructed = IsLineOfSightClear(cam.transform.position, eyeTarget);
            bool inViewport = IsInCameraViewport(cam, eyeTarget);
            Vector3 hideEyeTarget = hidePoint != null ? (hidePoint.position + Vector3.up * eyeHeightOffset) : eyeTarget;
            bool hidePointOccluded = hidePoint != null && !IsLineOfSightClear(cam.transform.position, hideEyeTarget);

            // Prevent open-corridor disappearances: a wall-occlusion watcher must NEVER retreat and pop out
            // while the player has direct line-of-sight to hidePoint (unless retreating inside a dark blackout).
            if (!hideViaDarkBlackout && !hidePointOccluded)
            {
                gazeTimer = 0f;
                // If player circled behind the wall and isn't looking at the figure, silently clear it off-screen
                if (armedElapsed > 0.5f && dist < 4.5f && dot < 0.10f)
                {
                    DisarmAndHide();
                }
                return;
            }

            // Garante que o observador (incluindo a criança do Dia 3) NUNCA dispare/passe fora do campo de visão do jogador
            if (!unobstructed || !inViewport)
            {
                gazeTimer = Mathf.MoveTowards(gazeTimer, 0f, Time.deltaTime * 2f);
                return;
            }

            // Case 1: Jogador avista o observador claramente no campo de visão (olhar direto)
            if (dot >= directGazeDotThreshold)
            {
                gazeTimer += Time.deltaTime;
                if (gazeTimer >= triggerDelayOnGaze)
                {
                    StartCoroutine(RetreatRoutine(wasSidewaysBypass: false, eyeTarget));
                }
                return;
            }
            else
            {
                gazeTimer = Mathf.MoveTowards(gazeTimer, 0f, Time.deltaTime * 2f);
            }

            // Case 2: Jogador se aproxima mantendo o observador visível dentro do enquadramento da câmera (nunca fora do FOV)
            if (armedElapsed > 0.45f && dist <= flankFailSafeDistance && dot >= 0.58f && IsInSameArea(cam.transform.position, eyeTarget))
            {
                StartCoroutine(RetreatRoutine(wasSidewaysBypass: true, eyeTarget));
            }
        }

        private static bool IsInCameraViewport(Camera cam, Vector3 worldPos)
        {
            if (cam == null) return false;
            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            return vp.z > 0.25f && vp.x >= 0.08f && vp.x <= 0.92f && vp.y >= 0.08f && vp.y <= 0.92f;
        }

        private bool IsLineOfSightClear(Vector3 origin, Vector3 target)
        {
            Vector3 seg = target - origin;
            float dist = seg.magnitude;
            if (dist < 0.25f) return true;

            int hitCount = Physics.RaycastNonAlloc(origin, seg / dist, losHitBuffer, dist - 0.12f, ~0, QueryTriggerInteraction.Ignore);
            int clampedCount = Mathf.Min(hitCount, losHitBuffer.Length);
            for (int i = 0; i < clampedCount; i++)
            {
                Collider col = losHitBuffer[i].collider;
                if (col == null) continue;
                if (silhouetteObject != null && col.transform.IsChildOf(silhouetteObject.transform)) continue;
                if (cachedPlayer != null && col.transform.IsChildOf(cachedPlayer.transform)) continue;
                // Keep strict on walls/doors so watchers never trigger through rooms
                return false;
            }
            return true;
        }

        private static bool IsInSameArea(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.y - b.y) < 1.5f;
        }

        private IEnumerator RetreatRoutine(bool wasSidewaysBypass, Vector3 eyeTarget)
        {
            isArmed = false;
            isRetreating = true;
            hasTriggered = true;

            SetCorridorSputterSuppressed(true);

            // Restrained, grounded retreat behind the real architectural corner (zero cheap strobe, camera shake, or head yank)
            if (spasmAnim != null)
            {
                spasmAnim.SetMode(SpasmAnimMode.TwitchDash);
            }

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley3DPanned(FoleySoundId.PhantomStepBehind, eyeTarget, 0.72f, 0.9f, 12f, 1.02f);
            }

            Vector3 startPos = silhouetteObject.transform.position;
            Vector3 endPos = hidePoint != null ? hidePoint.position : (startPos - peekPoint.right * 0.85f);
            Vector3 moveDir = endPos - startPos;
            moveDir.y = 0f;
            Quaternion startRot = silhouetteObject.transform.rotation;
            Quaternion turnRot = moveDir.sqrMagnitude > 0.005f
                ? Quaternion.LookRotation(moveDir.normalized, Vector3.up)
                : startRot;

            float elapsed = 0f;
            float dur = Mathf.Max(0.28f, retreatDuration + 0.12f);
            bool triggeredDarkBlackout = false;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / dur);
                float eased = Mathf.SmoothStep(0f, 1f, u);
                silhouetteObject.transform.position = Vector3.Lerp(startPos, endPos, eased);
                silhouetteObject.transform.rotation = Quaternion.Slerp(startRot, turnRot, Mathf.Clamp01(u * 2.5f));

                if (hideViaDarkBlackout && !triggeredDarkBlackout && u >= 0.55f)
                {
                    triggeredDarkBlackout = true;
                    BeginBriefBlackoutForVanish();
                }
                yield return null;
            }

            if (hideViaDarkBlackout && !triggeredDarkBlackout)
            {
                triggeredDarkBlackout = true;
                BeginBriefBlackoutForVanish();
            }

            if (spasmAnim != null)
            {
                spasmAnim.ResetPose();
                spasmAnim.SetMode(SpasmAnimMode.Idle);
            }
            silhouetteObject.SetActive(false);

            if (triggeredDarkBlackout)
            {
                yield return new WaitForSeconds(0.22f);
            }

            RestorePulsedLightsIfNeeded();
            SetCorridorSputterSuppressed(false);
            isRetreating = false;
        }

        private void BeginBriefBlackoutForVanish()
        {
            if (nearbyLightsToPulse != null && nearbyLightsToPulse.Length > 0)
            {
                if (pulsedLightsWereLit == null || pulsedLightsWereLit.Length != nearbyLightsToPulse.Length)
                {
                    pulsedLightsWereLit = new bool[nearbyLightsToPulse.Length];
                }
                for (int i = 0; i < nearbyLightsToPulse.Length; i++)
                {
                    Light l = nearbyLightsToPulse[i];
                    if (l != null && l.enabled)
                    {
                        pulsedLightsWereLit[i] = true;
                        l.enabled = false;
                        Canal420.Core.DayManager.SyncLampFixtureVisual(l, false, l.color, 0f);
                    }
                    else
                    {
                        pulsedLightsWereLit[i] = false;
                    }
                }
                hasUnrestoredPulsedLights = true;
            }

            if (cachedFlashlight == null)
            {
                cachedFlashlight = FindAnyObjectByType<FlashlightController>(FindObjectsInactive.Include);
            }
            if (cachedFlashlight != null && cachedFlashlight.HasFlashlight && cachedFlashlight.IsLightOn)
            {
                cachedFlashlight.ForceBlackout(true);
                hasUnrestoredFlashlightBlackout = true;
            }

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.BulbFlickerBuzz, 0.34f, 0.92f);
            }
        }
    }
}
