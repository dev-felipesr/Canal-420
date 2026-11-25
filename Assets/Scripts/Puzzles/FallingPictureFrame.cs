using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Canal420.Audio;
using Canal420.Core;
using Canal420.Effects;
using Canal420.Interaction;
using Canal420.Player;

namespace Canal420.Puzzles
{
    public class FallingPictureFrame : MonoBehaviour, IInteractable
    {
        public static event System.Action OnPictureFrameFell;

        [Header("Referência do Cofre Escondido Atrás")]
        [SerializeField] private SafeBoxPuzzle hiddenSafeBox;

        [Header("Textos Narrativos")]
        [SerializeField] private string interactionPrompt = "Ajeitar o retrato";
        [TextArea(2, 4)]
        [SerializeField] private string fallSubtitle = "O arame soltou. Eu não queria me lembrar desse cofre.";
        [SerializeField] private float subtitleDuration = 5.8f;

        [Header("Animação de Queda")]
        [SerializeField] private Transform fallenTargetPoint;
        [SerializeField] private float restingFloorY = 0.001f;
        [SerializeField] private float leanBackAngleDeg = 17f;
        [SerializeField] private float fallDuration = 0.36f;
        [SerializeField] private AudioSource fallSound;
        [SerializeField] private UnityEvent onFrameFell;

        [Header("Deslocamento Psicológico Fora de Visão (Dia 3)")]
        [SerializeField] private GameObject[] plasterChipsToReveal;
        [SerializeField] private float offscreenExtraRollDeg = -10.5f;
        [SerializeField] private float offscreenDropY = 0.018f;

        private Vector3 initialLocalPos;
        private Quaternion initialLocalRot;
        private bool hasInitializedTransform = false;
        private bool hasFallen = false;
        private bool hasShiftedOffscreen = false;
        private Collider[] frameColliders;

        public bool CanInteract => !hasFallen;
        public bool HasFallen => hasFallen;
        public bool HasShiftedOffscreen => hasShiftedOffscreen;

        private void Awake()
        {
            CacheInitialState();
            if (Application.isPlaying && !hasFallen && !hasShiftedOffscreen)
            {
                SetPlasterChipsActive(false);
            }
        }

        private void OnEnable()
        {
            CacheInitialState();
            ResetFrameState();
        }

        private void CacheInitialState()
        {
            if (hasInitializedTransform) return;
            initialLocalPos = transform.localPosition;
            initialLocalRot = transform.localRotation;
            frameColliders = GetComponentsInChildren<Collider>(true);
            hasInitializedTransform = true;
        }

        public void ResetFrameState()
        {
            CacheInitialState();
            StopAllCoroutines();
            hasFallen = false;
            hasShiftedOffscreen = false;
            transform.localPosition = initialLocalPos;
            transform.localRotation = initialLocalRot;

            if (Application.isPlaying)
            {
                SetPlasterChipsActive(false);
            }

            if (frameColliders != null)
            {
                for (int i = 0; i < frameColliders.Length; i++)
                {
                    if (frameColliders[i] != null) frameColliders[i].enabled = true;
                }
            }

            if (hiddenSafeBox != null)
            {
                hiddenSafeBox.SetRevealed(false);
            }
        }

        public void LinkPlasterChips(GameObject[] chips)
        {
            plasterChipsToReveal = chips;
        }

        public bool ApplyOffscreenCrookedShift()
        {
            CacheInitialState();
            if (hasFallen || hasShiftedOffscreen)
                return false;

            hasShiftedOffscreen = true;
            transform.localPosition = initialLocalPos + new Vector3(0f, -Mathf.Abs(offscreenDropY), 0f);
            transform.localRotation = initialLocalRot * Quaternion.Euler(0f, 0f, offscreenExtraRollDeg);
            SetPlasterChipsActive(true);
            Physics.SyncTransforms();
            return true;
        }

        private void SetPlasterChipsActive(bool active)
        {
            if (plasterChipsToReveal == null) return;
            for (int i = 0; i < plasterChipsToReveal.Length; i++)
            {
                if (plasterChipsToReveal[i] != null && plasterChipsToReveal[i].activeSelf != active)
                {
                    plasterChipsToReveal[i].SetActive(active);
                }
            }
        }

        public string GetInteractionPrompt()
        {
            return interactionPrompt;
        }

        public void Interact(GameObject interactor)
        {
            if (hasFallen)
                return;

            if (DayManager.Instance != null && DayManager.Instance.IsDay3PrePillGateBlocked(out string prePillReason))
            {
                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.WoodInspect, 0.65f);
                }
                if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(prePillReason))
                {
                    InteractionSystem.Instance.ShowSubtitle(prePillReason, 5.2f);
                }
                return;
            }

            hasFallen = true;

            if (frameColliders != null)
            {
                for (int i = 0; i < frameColliders.Length; i++)
                {
                    if (frameColliders[i] != null) frameColliders[i].enabled = false;
                }
            }

            PlayerController player = interactor != null
                ? interactor.GetComponentInParent<PlayerController>()
                : FindAnyObjectByType<PlayerController>();

            StartCoroutine(FallRoutine(player));
        }

        private IEnumerator FallRoutine(PlayerController player)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            Vector3 endPos;
            Quaternion endRot;

            if (fallenTargetPoint != null)
            {
                endPos = fallenTargetPoint.position;
                endRot = fallenTargetPoint.rotation;
            }
            else
            {
                Vector3 outward = transform.forward;
                outward.y = 0f;
                if (outward.sqrMagnitude < 0.01f) outward = Vector3.left;
                outward.Normalize();

                Vector3 lateralDir = Vector3.Cross(Vector3.up, outward).normalized;
                float startYaw = Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg;
                // Inclina para trás (-X local) para que a face pintada (+Z local) fique voltada para cima/fora, recostada na parede acima do rodapé
                endRot = Quaternion.Euler(-Mathf.Clamp(leanBackAngleDeg, 8f, 30f), startYaw + 3f, 0f);

                Bounds startBounds = ComputeHierarchyBoundsAtPose(startPos, startRot);
                float wallBackProj = GetExtremeProjection(startBounds, -outward);

                Vector3 guessPos = new Vector3(startPos.x, 0.40f, startPos.z) + lateralDir * 0.02f;
                Bounds rotBounds = ComputeHierarchyBoundsAtPose(guessPos, endRot);

                float deltaY = Mathf.Max(0f, restingFloorY) - rotBounds.min.y;
                float rotBackProj = GetExtremeProjection(rotBounds, -outward);
                float deltaAlongOutward = rotBackProj - (wallBackProj - 0.018f);

                endPos = guessPos + new Vector3(0f, deltaY, 0f) + outward * deltaAlongOutward;
            }

            if (hiddenSafeBox != null)
            {
                hiddenSafeBox.SetRevealed(true);
            }
            SetPlasterChipsActive(true);

            float elapsed = 0f;
            while (elapsed < fallDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fallDuration);
                float gravityCurve = t * t;

                transform.position = Vector3.Lerp(startPos, endPos, gravityCurve);
                transform.rotation = Quaternion.Slerp(startRot, endRot, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            transform.position = endPos;
            transform.rotation = endRot;

            if (fallSound != null && fallSound.clip != null)
            {
                fallSound.Play();
            }
            else if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.FrameCrash, endPos, 1.0f);
            }

            if (SanityEffects.Instance != null)
            {
                SanityEffects.Instance.TriggerHallucinationSpike(0.52f, 0.65f);
            }

            if (player != null)
            {
                player.TriggerCameraShake(1.75f, 0.28f);
            }

            float bounceDur = 0.11f;
            float bTime = 0f;
            while (bTime < bounceDur)
            {
                bTime += Time.deltaTime;
                float u = Mathf.Clamp01(bTime / bounceDur);
                float hop = Mathf.Sin(u * Mathf.PI) * 0.022f;
                transform.position = endPos + new Vector3(0f, hop, 0f);
                yield return null;
            }
            transform.position = endPos;

            if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(fallSubtitle))
            {
                InteractionSystem.Instance.ShowSubtitle(fallSubtitle, subtitleDuration);
            }

            OnPictureFrameFell?.Invoke();
            onFrameFell?.Invoke();

            // Delayed heavy thud coming from behind the locked daughters' room wall
            yield return new WaitForSeconds(0.62f);

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley3DPanned(
                    FoleySoundId.DoorKnockWood,
                    new Vector3(3.15f, 1.38f, 4.14f),
                    0.96f,
                    minDistance: 1.0f,
                    maxDistance: 15f,
                    pitchMultiplier: 0.84f
                );
            }
            if (player != null)
            {
                player.TriggerCameraShake(0.65f, 0.14f);
            }
        }

        public void LinkHiddenSafe(SafeBoxPuzzle safe)
        {
            hiddenSafeBox = safe;
        }

        private Bounds ComputeHierarchyBoundsAtPose(Vector3 rootWorldPos, Quaternion rootWorldRot)
        {
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
            Matrix4x4 targetRootMatrix = Matrix4x4.TRS(rootWorldPos, rootWorldRot, transform.lossyScale);
            Matrix4x4 currentWorldToRoot = transform.worldToLocalMatrix;

            bool hasAny = false;
            Bounds total = new Bounds(rootWorldPos, Vector3.zero);

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer mr = renderers[i];
                if (mr == null || !mr.enabled) continue;
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                Matrix4x4 childToWorld = targetRootMatrix * (currentWorldToRoot * mr.transform.localToWorldMatrix);
                Bounds lb = mf.sharedMesh.bounds;
                Vector3 c = lb.center;
                Vector3 e = lb.extents;

                for (int sx = -1; sx <= 1; sx += 2)
                {
                    for (int sy = -1; sy <= 1; sy += 2)
                    {
                        for (int sz = -1; sz <= 1; sz += 2)
                        {
                            Vector3 wp = childToWorld.MultiplyPoint3x4(c + new Vector3(e.x * sx, e.y * sy, e.z * sz));
                            if (!hasAny)
                            {
                                total = new Bounds(wp, Vector3.zero);
                                hasAny = true;
                            }
                            else
                            {
                                total.Encapsulate(wp);
                            }
                        }
                    }
                }
            }

            return total;
        }

        private static float GetExtremeProjection(Bounds b, Vector3 dir)
        {
            Vector3 c = b.center;
            Vector3 e = b.extents;
            return Vector3.Dot(c, dir)
                 + Mathf.Abs(dir.x) * e.x
                 + Mathf.Abs(dir.y) * e.y
                 + Mathf.Abs(dir.z) * e.z;
        }
    }
}
