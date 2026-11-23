using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Canal420.Audio;
using Canal420.Core;
using Canal420.Effects;
using Canal420.Interaction;
using Canal420.Player;
using Canal420.UI;

namespace Canal420.Events
{
    public enum HallucinationType
    {
        ShadowPass = 0,
        SwapObjects = 1,
        WhispersOrAudio = 2
    }

    public class HallucinationTrigger : MonoBehaviour
    {
        public static event Action<string> OnHallucinationCompleted;

        [Header("Identificação e Armamento")]
        [SerializeField] private string hallucinationId = "alucinacao_dia2";
        [SerializeField] private HallucinationType hallucinationType = HallucinationType.ShadowPass;
        [SerializeField] private bool triggerOnPlayerEnter = true;
        [SerializeField] private bool startArmed = true;
        [SerializeField] private bool notifyObjectiveCompletion = true;

        [Header("Direção de Câmera Scriptada")]
        [SerializeField] private bool scriptCameraLook = false;
        [SerializeField] private Vector3 customLookWorldPoint = Vector3.zero;
        [SerializeField] private Light[] corridorLightsToFlicker;

        [Header("Feedback Clínico / Narrativo")]
        [TextArea(2, 3)]
        [SerializeField] private string reactionSubtitle = "";
        [SerializeField] private float subtitleDuration = 5.8f;
        [Range(0f, 1f)]
        [SerializeField] private float glitchSpikeIntensity = 0.78f;
        [SerializeField] private float glitchSpikeDuration = 0.88f;
        [SerializeField] private AudioSource optionalAudioSource;

        [Header("Configuração 1: Vulto no Corredor (ShadowPass)")]
        [SerializeField] private GameObject shadowObject;
        [SerializeField] private Transform shadowStartPoint;
        [SerializeField] private Transform shadowEndPoint;
        [SerializeField] private float shadowMoveSpeed = 7.4f;

        [Header("Configuração 2: Quadros Alterados (SwapObjects)")]
        [SerializeField] private GameObject[] normalObjectsToHide;
        [SerializeField] private GameObject[] alteredObjectsToShow;
        private bool isArmed = true;
        private bool hasTriggered = false;
        private bool suppressedCorridorSputter = false;
        private bool pushedHudEventHide = false;
        private float[] activeSavedIntensities;
        private Coroutine activeSequenceCoroutine;
        private readonly List<Coroutine> activeSubCoroutines = new List<Coroutine>();
        private readonly List<GameObject> spawnedGlassDebris = new List<GameObject>();
        private Material runtimeGlassShardMaterial;
        private bool cachedSwapTransforms = false;
        private Vector3[] normalBaseLocalPos;
        private Quaternion[] normalBaseLocalRot;
        private Vector3[] alteredBaseLocalPos;
        private Quaternion[] alteredBaseLocalRot;

        public bool IsArmed => isArmed;
        public bool HasTriggered => hasTriggered;

        public string NormalizedId => string.IsNullOrWhiteSpace(hallucinationId)
            ? gameObject.name.Trim().ToLowerInvariant()
            : hallucinationId.Trim().ToLowerInvariant();

        private void Awake()
        {
            isArmed = startArmed;
            CacheSwapObjectTransforms();

            if (triggerOnPlayerEnter)
            {
                if (TryGetComponent<Collider>(out Collider col))
                {
                    col.isTrigger = true;
                }

                if (shadowObject != gameObject && TryGetComponent<MeshRenderer>(out MeshRenderer mr))
                {
                    mr.enabled = false;
                }
            }
        }

        private void OnDestroy()
        {
            ClearSpawnedGlassDebris();
            if (runtimeGlassShardMaterial != null)
            {
                Destroy(runtimeGlassShardMaterial);
                runtimeGlassShardMaterial = null;
            }
        }

        private void CacheSwapObjectTransforms()
        {
            if (cachedSwapTransforms || hallucinationType != HallucinationType.SwapObjects)
                return;

            cachedSwapTransforms = true;
            if (normalObjectsToHide != null)
            {
                normalBaseLocalPos = new Vector3[normalObjectsToHide.Length];
                normalBaseLocalRot = new Quaternion[normalObjectsToHide.Length];
                for (int i = 0; i < normalObjectsToHide.Length; i++)
                {
                    if (normalObjectsToHide[i] != null)
                    {
                        normalBaseLocalPos[i] = normalObjectsToHide[i].transform.localPosition;
                        normalBaseLocalRot[i] = normalObjectsToHide[i].transform.localRotation;
                    }
                }
            }

            if (alteredObjectsToShow != null)
            {
                alteredBaseLocalPos = new Vector3[alteredObjectsToShow.Length];
                alteredBaseLocalRot = new Quaternion[alteredObjectsToShow.Length];
                for (int i = 0; i < alteredObjectsToShow.Length; i++)
                {
                    if (alteredObjectsToShow[i] != null)
                    {
                        alteredBaseLocalPos[i] = alteredObjectsToShow[i].transform.localPosition;
                        alteredBaseLocalRot[i] = alteredObjectsToShow[i].transform.localRotation;
                    }
                }
            }
        }

        private void RestoreSwapObjectTransforms()
        {
            if (!cachedSwapTransforms) return;

            if (normalObjectsToHide != null && normalBaseLocalPos != null)
            {
                int count = Mathf.Min(normalObjectsToHide.Length, normalBaseLocalPos.Length);
                for (int i = 0; i < count; i++)
                {
                    if (normalObjectsToHide[i] != null)
                    {
                        normalObjectsToHide[i].transform.localPosition = normalBaseLocalPos[i];
                        normalObjectsToHide[i].transform.localRotation = normalBaseLocalRot[i];
                    }
                }
            }

            if (alteredObjectsToShow != null && alteredBaseLocalPos != null)
            {
                int count = Mathf.Min(alteredObjectsToShow.Length, alteredBaseLocalPos.Length);
                for (int i = 0; i < count; i++)
                {
                    if (alteredObjectsToShow[i] != null)
                    {
                        alteredObjectsToShow[i].transform.localPosition = alteredBaseLocalPos[i];
                        alteredObjectsToShow[i].transform.localRotation = alteredBaseLocalRot[i];
                    }
                }
            }
        }

        private void StopSubCoroutines()
        {
            for (int i = 0; i < activeSubCoroutines.Count; i++)
            {
                if (activeSubCoroutines[i] != null)
                {
                    StopCoroutine(activeSubCoroutines[i]);
                }
            }
            activeSubCoroutines.Clear();
        }

        private void ClearSpawnedGlassDebris()
        {
            for (int i = 0; i < spawnedGlassDebris.Count; i++)
            {
                if (spawnedGlassDebris[i] != null)
                {
                    Destroy(spawnedGlassDebris[i]);
                }
            }
            spawnedGlassDebris.Clear();
        }

        private void OnEnable()
        {
            ResetHallucinationState();
        }

        public void SetArmed(bool armed)
        {
            isArmed = armed;
        }

        public void SetLightsToFlicker(Light[] lights)
        {
            corridorLightsToFlicker = lights;
        }

        public void ResetHallucinationState()
        {
            hasTriggered = false;
            isArmed = startArmed;

            if (activeSequenceCoroutine != null)
            {
                StopCoroutine(activeSequenceCoroutine);
                activeSequenceCoroutine = null;
            }
            StopSubCoroutines();
            ClearSpawnedGlassDebris();
            RestoreInterruptedFlickerAndSputter();

            if (hallucinationType == HallucinationType.ShadowPass && shadowObject != null)
            {
                shadowObject.SetActive(false);
            }
            else if (hallucinationType == HallucinationType.SwapObjects)
            {
                CacheSwapObjectTransforms();
                RestoreSwapObjectTransforms();
                SetObjectsActive(normalObjectsToHide, true);
                SetObjectsActive(alteredObjectsToShow, false);
            }
        }

        private void SetOwnedCorridorSputterSuppressed(bool suppressed)
        {
            if (suppressed == suppressedCorridorSputter) return;
            suppressedCorridorSputter = suppressed;
            if (DayManager.Instance != null)
            {
                DayManager.Instance.SetCorridorAutoSputterSuppressed(suppressed);
            }
        }

        private void SetOwnedHudEventHidden(bool hidden)
        {
            if (hidden == pushedHudEventHide) return;
            pushedHudEventHide = hidden;
            if (ObjectiveHUD.Instance != null)
            {
                if (hidden) ObjectiveHUD.Instance.PushEventHide(immediate: false);
                else ObjectiveHUD.Instance.PopEventHide();
            }
        }

        private void RestoreInterruptedFlickerAndSputter()
        {
            if (activeSavedIntensities != null)
            {
                if (DayManager.Instance == null || !DayManager.Instance.IsTransitioning)
                {
                    RestoreLightIntensities(activeSavedIntensities);
                }
                activeSavedIntensities = null;
            }
            SetOwnedCorridorSputterSuppressed(false);
            SetOwnedHudEventHidden(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHandlePlayerOverlap(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryHandlePlayerOverlap(other);
        }

        private void TryHandlePlayerOverlap(Collider other)
        {
            if (!triggerOnPlayerEnter || !isArmed || hasTriggered || ObjectiveHUD.IsBlockingModalOpen)
                return;

            if (DayManager.Instance != null && (DayManager.Instance.IsTransitioning || DayManager.Instance.IsDemoEnded))
                return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null && !other.CompareTag("Player"))
                return;

            if (player != null && !player.CanMove)
                return;

            Trigger(player);
        }

        public void Trigger(PlayerController explicitPlayer = null)
        {
            if (hasTriggered)
                return;

            if (DayManager.Instance != null && (DayManager.Instance.IsTransitioning || DayManager.Instance.IsDemoEnded))
                return;

            hasTriggered = true;

            if (explicitPlayer == null)
            {
                explicitPlayer = FindAnyObjectByType<PlayerController>();
            }

            switch (hallucinationType)
            {
                case HallucinationType.ShadowPass:
                    activeSequenceCoroutine = StartCoroutine(RunScriptedShadowSequence(explicitPlayer));
                    break;

                case HallucinationType.SwapObjects:
                    activeSequenceCoroutine = StartCoroutine(RunScriptedSwapSequence(explicitPlayer));
                    break;

                case HallucinationType.WhispersOrAudio:
                    activeSequenceCoroutine = StartCoroutine(RunScriptedWhispersSequence(explicitPlayer));
                    break;
            }
        }

        private IEnumerator RunScriptedShadowSequence(PlayerController player)
        {
            if (shadowObject != null)
            {
                shadowObject.SetActive(false);
            }

            Vector3 startPos = shadowStartPoint != null ? shadowStartPoint.position : transform.position + Vector3.right * 3.5f;
            Vector3 endPos = shadowEndPoint != null ? shadowEndPoint.position : startPos + Vector3.back * 2.0f;

            SetOwnedCorridorSputterSuppressed(true);
            SetOwnedHudEventHidden(true);

            float[] savedIntensities = CaptureLightIntensities();
            activeSavedIntensities = savedIntensities;
            // Subtle dim dip — never chaotic 44Hz light strobing or camera quake
            SetFlickerMultiplier(0.42f, savedIntensities);

            HumanoidSpasmAnimator spasmAnim = null;
            if (shadowObject != null && shadowStartPoint != null)
            {
                shadowObject.transform.position = startPos;
                Vector3 crossDir = endPos - startPos;
                crossDir.y = 0f;
                if (crossDir.sqrMagnitude > 0.01f)
                {
                    shadowObject.transform.rotation = Quaternion.LookRotation(crossDir.normalized, Vector3.up);
                }

                spasmAnim = shadowObject.GetComponent<HumanoidSpasmAnimator>();
                if (spasmAnim == null) spasmAnim = shadowObject.AddComponent<HumanoidSpasmAnimator>();
                shadowObject.SetActive(true);
                spasmAnim.SetMode(SpasmAnimMode.TwitchDash);
            }

            if (optionalAudioSource != null && optionalAudioSource.clip != null)
            {
                optionalAudioSource.Play();
            }
            else if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley3DPanned(FoleySoundId.PhantomStepBehind, startPos, 0.85f, 0.9f, 14f, 0.98f);
                Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.ShadowWhoosh, Vector3.Lerp(startPos, endPos, 0.5f), 0.72f);
            }

            if (shadowObject != null && shadowEndPoint != null)
            {
                float dashElapsed = 0f;
                bool secondStepPlayed = false;
                while (Vector3.Distance(shadowObject.transform.position, endPos) > 0.05f && dashElapsed < 1.8f)
                {
                    dashElapsed += Time.deltaTime;
                    shadowObject.transform.position = Vector3.MoveTowards(
                        shadowObject.transform.position,
                        endPos,
                        shadowMoveSpeed * Time.deltaTime
                    );

                    if (!secondStepPlayed && dashElapsed >= 0.16f)
                    {
                        secondStepPlayed = true;
                        if (Canal420AudioManager.Instance != null)
                        {
                            Canal420AudioManager.Instance.PlayFoley3DPanned(FoleySoundId.PhantomStepBehind, endPos, 0.72f, 0.9f, 14f, 1.05f);
                        }
                    }
                    yield return null;
                }

                if (spasmAnim != null)
                {
                    spasmAnim.ResetPose();
                    spasmAnim.SetMode(SpasmAnimMode.Idle);
                }
                shadowObject.SetActive(false);
            }

            RestoreLightIntensities(savedIntensities);
            activeSavedIntensities = null;
            SetOwnedCorridorSputterSuppressed(false);
            SetOwnedHudEventHidden(false);

            if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(reactionSubtitle))
            {
                InteractionSystem.Instance.ShowSubtitle(reactionSubtitle, subtitleDuration);
            }

            yield return new WaitForSeconds(0.14f);

            activeSequenceCoroutine = null;
            if (notifyObjectiveCompletion)
            {
                OnHallucinationCompleted?.Invoke(NormalizedId);
            }
        }

        private IEnumerator RunScriptedSwapSequence(PlayerController player)
        {
            CacheSwapObjectTransforms();
            Vector3 lookFocus = customLookWorldPoint.sqrMagnitude > 0.01f
                ? customLookWorldPoint
                : new Vector3(1.06f, 1.54f, 3.30f);

            if (player != null && scriptCameraLook)
            {
                player.SetControlsEnabled(moveEnabled: false, lookEnabled: false, lockCursor: true);
                yield return player.SmoothLookAtRoutine(lookFocus, 0.38f, targetFovDelta: -3.5f);
                yield return new WaitForSeconds(0.08f);
            }

            SetOwnedCorridorSputterSuppressed(true);
            SetOwnedHudEventHidden(true);

            float[] savedIntensities = CaptureLightIntensities();
            activeSavedIntensities = savedIntensities;

            // Baixa a luz do corredor sem apagar totalmente para o jogador VER as molduras rachando e balançando
            SetFlickerMultiplier(0.46f, savedIntensities);
            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.BulbFlickerBuzz, lookFocus + Vector3.up * 0.9f, 0.52f);
            }

            int pairCount = Mathf.Max(
                normalObjectsToHide != null ? normalObjectsToHide.Length : 0,
                alteredObjectsToShow != null ? alteredObjectsToShow.Length : 0);

            if (pairCount <= 0)
            {
                SetObjectsActive(normalObjectsToHide, false);
                SetObjectsActive(alteredObjectsToShow, true);
                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley3DPanned(FoleySoundId.FrameCrash, lookFocus, 0.92f, 0.9f, 14f, 1.0f);
                }
            }
            else
            {
                for (int i = 0; i < pairCount; i++)
                {
                    GameObject normalObj = (normalObjectsToHide != null && i < normalObjectsToHide.Length) ? normalObjectsToHide[i] : null;
                    GameObject alteredObj = (alteredObjectsToShow != null && i < alteredObjectsToShow.Length) ? alteredObjectsToShow[i] : null;
                    Vector3 nPos = (normalBaseLocalPos != null && i < normalBaseLocalPos.Length) ? normalBaseLocalPos[i] : (normalObj != null ? normalObj.transform.localPosition : Vector3.zero);
                    Quaternion nRot = (normalBaseLocalRot != null && i < normalBaseLocalRot.Length) ? normalBaseLocalRot[i] : (normalObj != null ? normalObj.transform.localRotation : Quaternion.identity);
                    Vector3 aPos = (alteredBaseLocalPos != null && i < alteredBaseLocalPos.Length) ? alteredBaseLocalPos[i] : (alteredObj != null ? alteredObj.transform.localPosition : Vector3.zero);
                    Quaternion aRot = (alteredBaseLocalRot != null && i < alteredBaseLocalRot.Length) ? alteredBaseLocalRot[i] : (alteredObj != null ? alteredObj.transform.localRotation : Quaternion.identity);

                    float staggerDelay = i * 0.17f;
                    Coroutine c = StartCoroutine(AnimateSingleFrameBreakRoutine(
                        i, staggerDelay, normalObj, alteredObj, nPos, nRot, aPos, aRot, player));
                    activeSubCoroutines.Add(c);
                }

                // Tremulação elétrica sutil sincronizada com a quebra dos quadros, sempre mantendo visibilidade
                yield return new WaitForSeconds(0.11f);
                SetFlickerMultiplier(0.86f, savedIntensities);
                yield return new WaitForSeconds(0.09f);
                SetFlickerMultiplier(0.36f, savedIntensities);
                yield return new WaitForSeconds(0.10f);
                SetFlickerMultiplier(0.92f, savedIntensities);
                yield return new WaitForSeconds(0.08f);
                RestoreLightIntensities(savedIntensities);
                activeSavedIntensities = null;

                // Aguarda o balanço amortecido das molduras terminar antes de encerrar a sequência
                yield return new WaitForSeconds(0.56f);
            }

            if (activeSavedIntensities != null)
            {
                RestoreLightIntensities(activeSavedIntensities);
                activeSavedIntensities = null;
            }
            SetOwnedCorridorSputterSuppressed(false);
            SetOwnedHudEventHidden(false);

            if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(reactionSubtitle))
            {
                InteractionSystem.Instance.ShowSubtitle(reactionSubtitle, subtitleDuration);
            }

            yield return new WaitForSeconds(0.16f);

            if (player != null && scriptCameraLook)
            {
                player.ResetPostureAndFovOffsets(snap: false);
                if (!ObjectiveHUD.IsBlockingModalOpen)
                {
                    player.SetControlsEnabled(moveEnabled: true, lookEnabled: true, lockCursor: true);
                }
            }

            activeSequenceCoroutine = null;
            if (notifyObjectiveCompletion)
            {
                OnHallucinationCompleted?.Invoke(NormalizedId);
            }
        }

        private IEnumerator AnimateSingleFrameBreakRoutine(
            int index,
            float startDelay,
            GameObject normalObj,
            GameObject alteredObj,
            Vector3 normalBasePos,
            Quaternion normalBaseRot,
            Vector3 alteredBasePos,
            Quaternion alteredBaseRot,
            PlayerController player)
        {
            if (startDelay > 0.001f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            Vector3 breakWorldPos = alteredObj != null
                ? alteredObj.transform.position
                : (normalObj != null ? normalObj.transform.position : transform.position);

            // Direção final do pêndulo baseada no ângulo Z final do quadro quebrado (-14° ou +17°)
            float targetZ = alteredBaseRot.eulerAngles.z;
            if (targetZ > 180f) targetZ -= 360f;
            float tiltSign = Mathf.Abs(targetZ) > 0.5f ? Mathf.Sign(targetZ) : (index % 2 == 0 ? -1f : 1f);

            // 1) Estalo de tensão: moldura intacta treme e cede no prego antes de rachar (0.095s)
            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley3DPanned(
                    FoleySoundId.TVGlassThermalCrack,
                    breakWorldPos,
                    0.78f,
                    0.85f,
                    12f,
                    index % 2 == 0 ? 1.08f : 0.94f);
            }

            if (normalObj != null)
            {
                const float preCrackDuration = 0.095f;
                float elapsed = 0f;
                Vector3 wallOutward = normalObj.transform.forward;
                while (elapsed < preCrackDuration)
                {
                    elapsed += Time.deltaTime;
                    float u = Mathf.Clamp01(elapsed / preCrackDuration);
                    float rattle = Mathf.Sin(u * Mathf.PI * 5f) * 3.2f * (1f - u * 0.4f);
                    float preTilt = tiltSign * (7.5f * u * u) + rattle;
                    normalObj.transform.localRotation = normalBaseRot * Quaternion.Euler(0f, 0f, preTilt);
                    normalObj.transform.localPosition = normalBasePos
                        + Vector3.down * (0.014f * u * u)
                        + wallOutward * (0.016f * Mathf.Sin(u * Mathf.PI));
                    yield return null;
                }
                normalObj.SetActive(false);
            }

            // 2) Impacto: vidro estilhaça, moldura cai torta com som de moldura quebrando + estilhaços caindo no piso
            if (alteredObj != null)
            {
                alteredObj.SetActive(true);
                breakWorldPos = alteredObj.transform.position;
            }

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley3DPanned(
                    FoleySoundId.FrameCrash,
                    breakWorldPos,
                    index == 0 ? 0.96f : 0.88f,
                    0.95f,
                    15f,
                    index % 2 == 0 ? 1.03f : 0.92f);
                Canal420AudioManager.Instance.PlayFoley3DPanned(
                    FoleySoundId.GlassTap,
                    breakWorldPos,
                    0.84f,
                    0.90f,
                    13f,
                    index % 2 == 0 ? 0.96f : 1.09f);
            }

            if (SanityEffects.Instance != null && glitchSpikeIntensity > 0f && index == 0)
            {
                SanityEffects.Instance.TriggerHallucinationSpike(
                    Mathf.Min(0.42f, glitchSpikeIntensity),
                    glitchSpikeDuration * 0.65f,
                    allowSubliminalFlash: false);
            }

            if (player != null)
            {
                player.TriggerCameraShake(index == 0 ? 0.48f : 0.34f, 0.15f);
            }

            if (alteredObj != null)
            {
                Coroutine shardCoroutine = StartCoroutine(SpawnAndDropGlassShardsRoutine(
                    breakWorldPos,
                    alteredObj.transform.forward,
                    index));
                activeSubCoroutines.Add(shardCoroutine);

                // 3) Balanço pendular amortecido da moldura trincada pendurada no prego (0.76s)
                const float swingDuration = 0.76f;
                float swingElapsed = 0f;
                Vector3 kickForward = alteredObj.transform.forward;
                while (swingElapsed < swingDuration)
                {
                    swingElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(swingElapsed / swingDuration);
                    float decay = Mathf.Exp(-5.0f * t);
                    // Overshoot imediato além do ângulo torto final, quicando e assentando no prego
                    float angleDelta = tiltSign * 12.5f * decay * Mathf.Cos(t * Mathf.PI * 3.5f);
                    float wallBounce = 0.018f * Mathf.Exp(-9.5f * t) * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2.5f));
                    float dropBounce = -0.012f * Mathf.Exp(-8.0f * t) * Mathf.Cos(t * Mathf.PI * 3.0f);

                    alteredObj.transform.localRotation = alteredBaseRot * Quaternion.Euler(0f, 0f, angleDelta);
                    alteredObj.transform.localPosition = alteredBasePos + kickForward * wallBounce + Vector3.up * dropBounce;
                    yield return null;
                }

                alteredObj.transform.localPosition = alteredBasePos;
                alteredObj.transform.localRotation = alteredBaseRot;
            }
        }

        private Material GetOrCreateGlassShardMaterial()
        {
            if (runtimeGlassShardMaterial != null)
                return runtimeGlassShardMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
                return null;
            runtimeGlassShardMaterial = new Material(shader)
            {
                name = "M_Runtime_FrameGlassShard"
            };
            Color glassTint = new Color(0.68f, 0.76f, 0.78f, 1f);
            if (runtimeGlassShardMaterial.HasProperty("_BaseColor"))
                runtimeGlassShardMaterial.SetColor("_BaseColor", glassTint);
            else
                runtimeGlassShardMaterial.color = glassTint;
            if (runtimeGlassShardMaterial.HasProperty("_Smoothness"))
                runtimeGlassShardMaterial.SetFloat("_Smoothness", 0.92f);
            if (runtimeGlassShardMaterial.HasProperty("_Metallic"))
                runtimeGlassShardMaterial.SetFloat("_Metallic", 0.35f);
            return runtimeGlassShardMaterial;
        }

        private IEnumerator SpawnAndDropGlassShardsRoutine(Vector3 origin, Vector3 outwardDir, int seedOffset)
        {
            const int shardCount = 6;
            Transform[] shards = new Transform[shardCount];
            Vector3[] vel = new Vector3[shardCount];
            Vector3[] spin = new Vector3[shardCount];
            bool[] settled = new bool[shardCount];
            Material shardMat = GetOrCreateGlassShardMaterial();
            Transform parentRoot = transform.parent != null ? transform.parent : transform;

            Vector3 rightDir = Vector3.Cross(Vector3.up, outwardDir).normalized;
            if (rightDir.sqrMagnitude < 0.01f) rightDir = Vector3.forward;

            for (int i = 0; i < shardCount; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"Estilhaco_Vidro_D2_{seedOffset}_{i}";
                if (shard.TryGetComponent<Collider>(out Collider col))
                {
                    Destroy(col);
                }
                if (shard.TryGetComponent<MeshRenderer>(out MeshRenderer mr))
                {
                    mr.sharedMaterial = shardMat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                float sFrac = (i / (float)(shardCount - 1)) - 0.5f;
                float sizeX = 0.014f + ((i * 7 + seedOffset * 3) % 4) * 0.0045f;
                float sizeZ = 0.022f + ((i * 5 + seedOffset * 2) % 3) * 0.006f;
                shard.transform.SetParent(parentRoot, true);
                shard.transform.localScale = new Vector3(sizeX, 0.0028f, sizeZ);
                shard.transform.position = origin
                    + outwardDir * 0.026f
                    + rightDir * (sFrac * 0.24f)
                    + Vector3.up * (((i % 3) - 1) * 0.11f);
                shard.transform.rotation = Quaternion.Euler(i * 47f, i * 63f, i * 31f);

                shards[i] = shard.transform;
                vel[i] = outwardDir * (0.28f + (i % 3) * 0.14f)
                    + rightDir * (sFrac * 0.36f)
                    + Vector3.up * (0.18f + (i % 2) * 0.16f);
                spin[i] = new Vector3(240f + i * 55f, 180f - i * 40f, 320f + i * 35f);
                spawnedGlassDebris.Add(shard);
            }

            float elapsed = 0f;
            const float maxSimTime = 1.15f;
            while (elapsed < maxSimTime)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                bool anyMoving = false;
                for (int i = 0; i < shardCount; i++)
                {
                    if (settled[i] || shards[i] == null) continue;
                    vel[i].y -= 9.8f * dt;
                    Vector3 p = shards[i].position + vel[i] * dt;
                    if (p.y <= 0.0045f)
                    {
                        p.y = 0.0045f;
                        if (Mathf.Abs(vel[i].y) > 0.45f)
                        {
                            vel[i].y = -vel[i].y * 0.22f;
                            vel[i].x *= 0.48f;
                            vel[i].z *= 0.48f;
                            anyMoving = true;
                        }
                        else
                        {
                            settled[i] = true;
                            Vector3 e = shards[i].eulerAngles;
                            shards[i].rotation = Quaternion.Euler(0f, e.y, 0f);
                        }
                    }
                    else
                    {
                        shards[i].Rotate(spin[i] * dt, Space.Self);
                        anyMoving = true;
                    }
                    shards[i].position = p;
                }

                if (!anyMoving) break;
                yield return null;
            }
        }

        private IEnumerator RunScriptedWhispersSequence(PlayerController player)
        {
            SetOwnedHudEventHidden(true);
            Vector3 doorSoundPos = customLookWorldPoint.sqrMagnitude > 0.01f
                ? customLookWorldPoint
                : transform.position + Vector3.right * 1.4f;

            if (optionalAudioSource != null && optionalAudioSource.clip != null)
            {
                optionalAudioSource.Play();
            }
            else if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.CorridorWhispers, doorSoundPos, 1.0f);
                Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.EerieLullabyHum, doorSoundPos + Vector3.right * 0.6f, 0.55f);
            }

            if (SanityEffects.Instance != null && glitchSpikeIntensity > 0f)
            {
                SanityEffects.Instance.TriggerHallucinationSpike(glitchSpikeIntensity * 0.75f, glitchSpikeDuration);
            }

            if (player != null && scriptCameraLook && customLookWorldPoint.sqrMagnitude > 0.01f)
            {
                player.SetControlsEnabled(moveEnabled: false, lookEnabled: false, lockCursor: true);
                yield return player.SmoothLookAtRoutine(customLookWorldPoint, 0.42f, targetFovDelta: -2.5f);
                player.ResetPostureAndFovOffsets(snap: false);
                if (!ObjectiveHUD.IsBlockingModalOpen)
                {
                    player.SetControlsEnabled(moveEnabled: true, lookEnabled: true, lockCursor: true);
                }
            }

            if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(reactionSubtitle))
            {
                InteractionSystem.Instance.ShowSubtitle(reactionSubtitle, subtitleDuration);
            }

            SetOwnedHudEventHidden(false);
            activeSequenceCoroutine = null;
            if (notifyObjectiveCompletion)
            {
                OnHallucinationCompleted?.Invoke(NormalizedId);
            }
        }

        private float[] CaptureLightIntensities()
        {
            if (corridorLightsToFlicker == null || corridorLightsToFlicker.Length == 0)
                return null;

            float[] vals = new float[corridorLightsToFlicker.Length];
            for (int i = 0; i < corridorLightsToFlicker.Length; i++)
            {
                vals[i] = corridorLightsToFlicker[i] != null ? corridorLightsToFlicker[i].intensity : 0f;
            }
            return vals;
        }

        private void SetFlickerMultiplier(float mult, float[] baseIntensities)
        {
            if (corridorLightsToFlicker == null || baseIntensities == null)
                return;

            for (int i = 0; i < corridorLightsToFlicker.Length && i < baseIntensities.Length; i++)
            {
                Light l = corridorLightsToFlicker[i];
                if (l == null) continue;

                bool baseWasActive = baseIntensities[i] > 0.02f;
                if (!baseWasActive)
                {
                    l.enabled = false;
                    l.intensity = 0f;
                    DayManager.SyncLampFixtureVisual(l, false, Color.black, 0f);
                    continue;
                }

                bool lit = mult > 0.02f;
                l.enabled = lit;
                l.intensity = lit ? baseIntensities[i] * mult : 0f;
                DayManager.SyncLampFixtureVisual(l, lit, lit ? l.color : Color.black, lit ? Mathf.Clamp01(mult) * 0.85f : 0f);
            }
        }

        private void OnDisable()
        {
            if (activeSequenceCoroutine != null)
            {
                StopCoroutine(activeSequenceCoroutine);
                activeSequenceCoroutine = null;
            }
            StopSubCoroutines();
            ClearSpawnedGlassDebris();
            RestoreInterruptedFlickerAndSputter();
        }

        private void RestoreLightIntensities(float[] baseIntensities)
        {
            SetFlickerMultiplier(1f, baseIntensities);
        }

        private void SetObjectsActive(GameObject[] targets, bool state)
        {
            if (targets == null) return;

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    targets[i].SetActive(state);
                }
            }
        }

        public void ConfigureShadowPass(
            string id,
            string subtitle,
            GameObject shadow,
            Transform startPt,
            Transform endPt,
            float speed = 7.4f,
            bool armedByDefault = false,
            Vector3 lookPoint = default,
            bool enableCameraLook = false)
        {
            hallucinationId = id;
            hallucinationType = HallucinationType.ShadowPass;
            triggerOnPlayerEnter = true;
            startArmed = armedByDefault;
            isArmed = armedByDefault;
            notifyObjectiveCompletion = true;
            scriptCameraLook = enableCameraLook;
            customLookWorldPoint = lookPoint;
            reactionSubtitle = subtitle;
            shadowObject = shadow;
            shadowStartPoint = startPt;
            shadowEndPoint = endPt;
            shadowMoveSpeed = speed;
        }

        public void ConfigureSwapObjects(
            string id,
            string subtitle,
            GameObject[] normals,
            GameObject[] altereds,
            bool armedByDefault = false,
            Vector3 lookPoint = default,
            bool enableCameraLook = false)
        {
            hallucinationId = id;
            hallucinationType = HallucinationType.SwapObjects;
            triggerOnPlayerEnter = true;
            startArmed = armedByDefault;
            isArmed = armedByDefault;
            notifyObjectiveCompletion = true;
            scriptCameraLook = enableCameraLook;
            customLookWorldPoint = lookPoint;
            reactionSubtitle = subtitle;
            normalObjectsToHide = normals;
            alteredObjectsToShow = altereds;
        }

        public void ConfigureWhispers(
            string id,
            string subtitle,
            bool armedByDefault = false,
            bool completeObjective = false,
            Vector3 lookPoint = default)
        {
            hallucinationId = id;
            hallucinationType = HallucinationType.WhispersOrAudio;
            triggerOnPlayerEnter = true;
            startArmed = armedByDefault;
            isArmed = armedByDefault;
            notifyObjectiveCompletion = completeObjective;
            scriptCameraLook = lookPoint.sqrMagnitude > 0.01f;
            customLookWorldPoint = lookPoint;
            reactionSubtitle = subtitle;
        }
    }
}
