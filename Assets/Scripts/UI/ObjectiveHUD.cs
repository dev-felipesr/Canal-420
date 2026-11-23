using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Canal420.Audio;

namespace Canal420.UI
{
    public class ObjectiveHUD : MonoBehaviour
    {
        public static ObjectiveHUD Instance { get; private set; }

        private class ObjectiveEntry
        {
            public string id;
            public string label;
            public bool completed;
            public bool visible;
        }

        [Header("Comportamento")]
        [SerializeField] private bool startExpanded = true;
        [SerializeField] private float toggleSmoothTime = 0.16f;
        private readonly List<ObjectiveEntry> entries = new List<ObjectiveEntry>();

        private GameObject canvasRoot;
        private CanvasGroup masterCanvasGroup;
        private RectTransform expandedPanelRect;
        private CanvasGroup expandedGroup;
        private CanvasGroup collapsedGroup;
        private CanvasGroup activeTaskRowGroup;
        private Text headerTitleText;
        private Text tabHintText;
        private Text activeTaskText;
        private Text collapsedSummaryText;
        private Font uiFont;

        private bool isExpanded = true;
        private bool hudVisible = false;
        private int eventHideRefCount = 0;
        private float expandBlend = 1f;
        private float expandVelocity = 0f;
        private float visibilityBlend = 0f;
        private float visibilityVelocity = 0f;
        private float collapsedPulseTimer = 0f;
        private float lastAppliedMasterAlpha = -1f;
        private float lastAppliedExpandAlpha = -1f;
        private float lastAppliedCollapsedAlpha = -1f;
        private float lastAppliedPanelX = float.NaN;
        private bool lastSummaryWasPulse = true;

        private string currentlyDisplayedId = string.Empty;
        private string lastKnownPendingId = string.Empty;
        private Coroutine stepTransitionCoroutine;

        private static readonly Color PanelBackgroundColor = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color HeaderTextColor = new Color(0.80f, 0.80f, 0.80f, 0.90f);
        private static readonly Color MutedTabColor = new Color(0.62f, 0.62f, 0.62f, 0.82f);
        private static readonly Color ActiveTaskTextColor = new Color(0.96f, 0.96f, 0.96f, 0.98f);
        private static readonly Color CompletedTaskColor = new Color(0.62f, 0.62f, 0.62f, 0.84f);

        private static int lastModalClosedFrame = -100;

        public static bool IsBlockingModalOpen { get; set; } = false;
        public bool IsSuppressedByEvent => eventHideRefCount > 0;

        /// <summary>
        /// Registra o frame em que um modal foi fechado para evitar que o mesmo clique/tecla
        /// (como ESC ou TAB) acione outro menu no mesmo frame.
        /// </summary>
        public static void NotifyModalClosedThisFrame()
        {
            lastModalClosedFrame = Time.frameCount;
        }

        public static bool WasModalClosedRecently(int frameWindow = 1)
        {
            return (Time.frameCount - lastModalClosedFrame) <= frameWindow;
        }

        private void Awake()
        {
            Instance = this;
            IsBlockingModalOpen = false;
            lastModalClosedFrame = -100;
            isExpanded = startExpanded;
            expandBlend = isExpanded ? 1f : 0f;
            hudVisible = false;
            visibilityBlend = 0f;
            visibilityVelocity = 0f;
            BuildHUDIfNeeded();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                IsBlockingModalOpen = false;
            }
            if (canvasRoot != null) Destroy(canvasRoot);
        }

        private void Update()
        {
            HandleToggleInput();
            AnimateHUD();
        }

        private void HandleToggleInput()
        {
            if (!hudVisible || IsBlockingModalOpen || eventHideRefCount > 0 || WasModalClosedRecently(2) || Time.timeScale <= 0.01f)
                return;

            if (GetCurrentPendingEntry() == null)
                return;

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.tabKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame)
            {
                isExpanded = !isExpanded;
                collapsedPulseTimer = 0f;
            }
        }

        private void AnimateHUD()
        {
            if (masterCanvasGroup == null) return;

            bool hasActiveStep = GetCurrentPendingEntry() != null || stepTransitionCoroutine != null;
            float targetVis = (hudVisible && !IsBlockingModalOpen && eventHideRefCount <= 0 && hasActiveStep) ? 1f : 0f;
            float smooth = targetVis < visibilityBlend ? 0.09f : 0.22f;
            visibilityBlend = Mathf.SmoothDamp(visibilityBlend, targetVis, ref visibilityVelocity, smooth, Mathf.Infinity, Time.unscaledDeltaTime);
            if (Mathf.Abs(visibilityBlend - targetVis) <= 0.001f && Mathf.Abs(visibilityVelocity) <= 0.001f)
            {
                visibilityBlend = targetVis;
                visibilityVelocity = 0f;
            }
            if (Mathf.Abs(lastAppliedMasterAlpha - visibilityBlend) > 0.0005f)
            {
                lastAppliedMasterAlpha = visibilityBlend;
                masterCanvasGroup.alpha = visibilityBlend;
            }

            float targetExpand = isExpanded ? 1f : 0f;
            expandBlend = Mathf.SmoothDamp(expandBlend, targetExpand, ref expandVelocity, toggleSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            if (Mathf.Abs(expandBlend - targetExpand) <= 0.001f && Mathf.Abs(expandVelocity) <= 0.001f)
            {
                expandBlend = targetExpand;
                expandVelocity = 0f;
            }

            if (expandedGroup != null)
            {
                if (Mathf.Abs(lastAppliedExpandAlpha - expandBlend) > 0.0005f)
                {
                    lastAppliedExpandAlpha = expandBlend;
                    expandedGroup.alpha = expandBlend;
                }
                bool shouldShowExpanded = expandBlend > 0.01f;
                if (expandedGroup.gameObject.activeSelf != shouldShowExpanded)
                {
                    expandedGroup.gameObject.SetActive(shouldShowExpanded);
                }
            }

            if (expandedPanelRect != null)
            {
                float targetX = Mathf.Lerp(-16f, 24f, expandBlend);
                if (float.IsNaN(lastAppliedPanelX) || Mathf.Abs(lastAppliedPanelX - targetX) > 0.05f)
                {
                    lastAppliedPanelX = targetX;
                    expandedPanelRect.anchoredPosition = new Vector2(targetX, -24f);
                }
            }

            if (collapsedGroup != null)
            {
                float colAlpha = 1f - expandBlend;
                if (Mathf.Abs(lastAppliedCollapsedAlpha - colAlpha) > 0.0005f)
                {
                    lastAppliedCollapsedAlpha = colAlpha;
                    collapsedGroup.alpha = colAlpha;
                }
                bool shouldShowCollapsed = colAlpha > 0.01f;
                if (collapsedGroup.gameObject.activeSelf != shouldShowCollapsed)
                {
                    collapsedGroup.gameObject.SetActive(shouldShowCollapsed);
                }
            }

            if (collapsedPulseTimer > 0f)
            {
                collapsedPulseTimer = Mathf.Max(0f, collapsedPulseTimer - Time.unscaledDeltaTime);
                float pulse = Mathf.Sin(collapsedPulseTimer * Mathf.PI * 2.5f) * 0.5f + 0.5f;
                float fade = Mathf.Clamp01(collapsedPulseTimer / 1.4f);

                if (collapsedSummaryText != null)
                {
                    collapsedSummaryText.color = Color.Lerp(HeaderTextColor, Color.white, pulse * fade * 0.40f);
                    lastSummaryWasPulse = true;
                }
            }
            else if (lastSummaryWasPulse)
            {
                if (collapsedSummaryText != null) collapsedSummaryText.color = HeaderTextColor;
                lastSummaryWasPulse = false;
            }
        }

        public void SetHUDVisible(bool visible, bool immediate = false)
        {
            hudVisible = visible;
            if (immediate && masterCanvasGroup != null)
            {
                bool effectiveVisible = visible && !IsBlockingModalOpen && eventHideRefCount <= 0;
                visibilityBlend = effectiveVisible ? 1f : 0f;
                visibilityVelocity = 0f;
                lastAppliedMasterAlpha = visibilityBlend;
                masterCanvasGroup.alpha = visibilityBlend;
            }
        }

        /// <summary>
        /// Ref-counted HUD suppression during scare moments, wake-up animations, or climax sequences.
        /// Keeps hudVisible intent intact while fading/hiding the objective panel cleanly.
        /// </summary>
        public void PushEventHide(bool immediate = false)
        {
            eventHideRefCount++;
            if (immediate && masterCanvasGroup != null)
            {
                visibilityBlend = 0f;
                visibilityVelocity = 0f;
                lastAppliedMasterAlpha = 0f;
                masterCanvasGroup.alpha = 0f;
            }
        }

        public void PopEventHide()
        {
            if (eventHideRefCount > 0)
            {
                eventHideRefCount--;
            }
        }

        public void ClearEventHides()
        {
            eventHideRefCount = 0;
        }

        public void ConfigureDayHeader(string dayHeader, Color accentColor)
        {
            BuildHUDIfNeeded();
            if (headerTitleText != null) headerTitleText.text = dayHeader;
            ClearObjectives();
        }

        public void ClearObjectives()
        {
            if (stepTransitionCoroutine != null)
            {
                StopCoroutine(stepTransitionCoroutine);
                stepTransitionCoroutine = null;
            }

            entries.Clear();
            currentlyDisplayedId = string.Empty;
            lastKnownPendingId = string.Empty;
            collapsedPulseTimer = 0f;
            if (activeTaskRowGroup != null) activeTaskRowGroup.alpha = 1f;
            RefreshDisplayedSingleTaskImmediate();
        }

        public void AddOrUpdateObjective(string id, string label, bool completed = false, bool visible = true)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            BuildHUDIfNeeded();

            string normId = id.Trim().ToLowerInvariant();
            ObjectiveEntry existing = entries.Find(e => e.id == normId);

            if (existing == null)
            {
                entries.Add(new ObjectiveEntry
                {
                    id = normId,
                    label = label,
                    completed = completed,
                    visible = visible
                });
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(label)) existing.label = label;
                existing.completed = completed;
                existing.visible = visible;
            }

            if (stepTransitionCoroutine == null)
            {
                RefreshDisplayedSingleTaskImmediate();
            }
        }

        public void MarkCompleted(string id)
        {
            MarkCompleted(id, silentCompletion: false);
        }

        public void MarkCompleted(string id, bool silentCompletion)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            string normId = id.Trim().ToLowerInvariant();
            ObjectiveEntry entry = entries.Find(e => e.id == normId);
            if (entry == null || entry.completed)
                return;

            entry.completed = true;

            if (!silentCompletion && Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.ObjectiveStepTick, 0.55f);
            }

            if (currentlyDisplayedId == normId && gameObject.activeInHierarchy && !silentCompletion)
            {
                if (stepTransitionCoroutine != null) StopCoroutine(stepTransitionCoroutine);
                stepTransitionCoroutine = StartCoroutine(AnimateSingleTaskAdvanceRoutine(entry.label));
            }
            else
            {
                if (silentCompletion && stepTransitionCoroutine != null)
                {
                    StopCoroutine(stepTransitionCoroutine);
                    stepTransitionCoroutine = null;
                    if (activeTaskRowGroup != null) activeTaskRowGroup.alpha = 1f;
                }

                if (stepTransitionCoroutine == null)
                {
                    RefreshDisplayedSingleTaskImmediate();
                }
            }
        }

        public string CurrentPendingObjectiveId => GetCurrentPendingEntry()?.id ?? string.Empty;

        public void SetObjectiveVisible(string id, bool visible)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            string normId = id.Trim().ToLowerInvariant();
            ObjectiveEntry entry = entries.Find(e => e.id == normId);
            if (entry != null)
            {
                entry.visible = visible;
                if (stepTransitionCoroutine == null)
                {
                    RefreshDisplayedSingleTaskImmediate();
                }
            }
        }

        public void SetObjectiveLabel(string id, string newLabel)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(newLabel)) return;
            string normId = id.Trim().ToLowerInvariant();
            ObjectiveEntry entry = entries.Find(e => e.id == normId);
            if (entry != null)
            {
                entry.label = newLabel;
                if (stepTransitionCoroutine == null)
                {
                    RefreshDisplayedSingleTaskImmediate();
                }
            }
        }

        public void PromoteObjectiveToFront(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            string normId = id.Trim().ToLowerInvariant();
            int idx = entries.FindIndex(e => e.id == normId);
            if (idx > 0)
            {
                ObjectiveEntry entry = entries[idx];
                entries.RemoveAt(idx);
                entries.Insert(0, entry);
                if (stepTransitionCoroutine == null)
                {
                    RefreshDisplayedSingleTaskImmediate();
                }
            }
        }

        private ObjectiveEntry GetCurrentPendingEntry()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].visible && !entries[i].completed)
                {
                    return entries[i];
                }
            }
            return null;
        }

        private IEnumerator AnimateSingleTaskAdvanceRoutine(string justCompletedLabel)
        {
            if (activeTaskText != null)
            {
                activeTaskText.text = $"- {justCompletedLabel}";
                activeTaskText.color = CompletedTaskColor;
                activeTaskText.fontStyle = FontStyle.Italic;
            }

            yield return new WaitForSeconds(0.55f);

            if (activeTaskRowGroup != null)
            {
                while (activeTaskRowGroup.alpha > 0.02f)
                {
                    activeTaskRowGroup.alpha = Mathf.MoveTowards(activeTaskRowGroup.alpha, 0f, Time.deltaTime * 4.2f);
                    yield return null;
                }
                activeTaskRowGroup.alpha = 0f;
            }

            RefreshDisplayedSingleTaskImmediate(restoreRowAlpha: false);

            if (activeTaskRowGroup != null)
            {
                if (GetCurrentPendingEntry() != null)
                {
                    while (activeTaskRowGroup.alpha < 0.99f)
                    {
                        activeTaskRowGroup.alpha = Mathf.MoveTowards(activeTaskRowGroup.alpha, 1f, Time.deltaTime * 3.8f);
                        yield return null;
                    }
                    activeTaskRowGroup.alpha = 1f;
                }
                else
                {
                    // Never leave activeTaskRowGroup stuck at 0 when the pending queue empties,
                    // otherwise the next dynamically added task appears invisible.
                    if (activeTaskText != null) activeTaskText.text = string.Empty;
                    activeTaskRowGroup.alpha = 1f;
                }
            }

            stepTransitionCoroutine = null;
        }

        private void RefreshDisplayedSingleTaskImmediate(bool restoreRowAlpha = true)
        {
            ObjectiveEntry pending = GetCurrentPendingEntry();

            if (pending == null)
            {
                currentlyDisplayedId = string.Empty;
                if (activeTaskText != null && (entries.Count == 0 || visibilityBlend <= 0.01f))
                {
                    activeTaskText.text = string.Empty;
                }
                if (restoreRowAlpha && activeTaskRowGroup != null)
                {
                    activeTaskRowGroup.alpha = 1f;
                }
                return;
            }

            bool taskChanged = lastKnownPendingId != pending.id;
            currentlyDisplayedId = pending.id;
            lastKnownPendingId = pending.id;

            if (taskChanged && !isExpanded && hudVisible)
            {
                collapsedPulseTimer = 1.6f;
            }

            if (activeTaskText != null)
            {
                activeTaskText.text = $"- {pending.label}";
                activeTaskText.color = ActiveTaskTextColor;
                activeTaskText.fontStyle = FontStyle.Normal;
            }

            if (restoreRowAlpha && activeTaskRowGroup != null)
            {
                activeTaskRowGroup.alpha = 1f;
            }

            if (collapsedSummaryText != null)
            {
                collapsedSummaryText.text = "[TAB] Tarefa";
            }

            if (expandedPanelRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(expandedPanelRect);
            }
        }

        private void BuildHUDIfNeeded()
        {
            if (canvasRoot != null) return;

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            canvasRoot = new GameObject("HUD_Objetivos_Auto");
            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;

            CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            masterCanvasGroup = canvasRoot.AddComponent<CanvasGroup>();
            masterCanvasGroup.blocksRaycasts = false;
            masterCanvasGroup.interactable = false;
            masterCanvasGroup.alpha = 0f;

            GameObject expObj = new GameObject("Expanded_Panel");
            expObj.transform.SetParent(canvasRoot.transform, false);
            Image bgImg = expObj.AddComponent<Image>();
            bgImg.color = PanelBackgroundColor;
            bgImg.raycastTarget = false;

            expandedGroup = expObj.AddComponent<CanvasGroup>();
            expandedPanelRect = bgImg.rectTransform;
            expandedPanelRect.anchorMin = new Vector2(0f, 1f);
            expandedPanelRect.anchorMax = new Vector2(0f, 1f);
            expandedPanelRect.pivot = new Vector2(0f, 1f);
            expandedPanelRect.anchoredPosition = new Vector2(24f, -24f);
            expandedPanelRect.sizeDelta = new Vector2(342f, 0f);

            VerticalLayoutGroup vLayout = expObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(14, 14, 10, 11);
            vLayout.spacing = 6f;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = expObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            GameObject headerRow = new GameObject("Header_Row");
            headerRow.transform.SetParent(expObj.transform, false);
            HorizontalLayoutGroup hLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;

            GameObject titleObj = new GameObject("Header_Title");
            titleObj.transform.SetParent(headerRow.transform, false);
            headerTitleText = titleObj.AddComponent<Text>();
            headerTitleText.font = uiFont;
            headerTitleText.fontSize = 11;
            headerTitleText.fontStyle = FontStyle.Bold;
            headerTitleText.color = HeaderTextColor;
            headerTitleText.text = "DIA 1";
            headerTitleText.raycastTarget = false;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1f;
            titleLE.minHeight = 15f;

            GameObject tabHintObj = new GameObject("Tab_Hint");
            tabHintObj.transform.SetParent(headerRow.transform, false);
            tabHintText = tabHintObj.AddComponent<Text>();
            tabHintText.font = uiFont;
            tabHintText.fontSize = 11;
            tabHintText.alignment = TextAnchor.MiddleRight;
            tabHintText.color = MutedTabColor;
            tabHintText.text = "[TAB]";
            tabHintText.raycastTarget = false;
            LayoutElement hintLE = tabHintObj.AddComponent<LayoutElement>();
            hintLE.preferredWidth = 42f;
            hintLE.minHeight = 15f;

            GameObject taskRowObj = new GameObject("Active_Task_Row");
            taskRowObj.transform.SetParent(expObj.transform, false);
            activeTaskRowGroup = taskRowObj.AddComponent<CanvasGroup>();
            HorizontalLayoutGroup rowLayout = taskRowObj.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 0f;
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            GameObject taskTxtObj = new GameObject("Task_Label");
            taskTxtObj.transform.SetParent(taskRowObj.transform, false);
            activeTaskText = taskTxtObj.AddComponent<Text>();
            activeTaskText.font = uiFont;
            activeTaskText.fontSize = 15;
            activeTaskText.lineSpacing = 1.18f;
            activeTaskText.alignment = TextAnchor.UpperLeft;
            activeTaskText.horizontalOverflow = HorizontalWrapMode.Wrap;
            activeTaskText.verticalOverflow = VerticalWrapMode.Overflow;
            activeTaskText.color = ActiveTaskTextColor;
            activeTaskText.raycastTarget = false;

            Shadow sh = taskTxtObj.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.82f);
            sh.effectDistance = new Vector2(1f, -1f);

            LayoutElement taskLE = taskTxtObj.AddComponent<LayoutElement>();
            taskLE.minHeight = 20f;
            taskLE.preferredWidth = 314f;
            taskLE.flexibleWidth = 1f;

            GameObject colObj = new GameObject("Collapsed_Label");
            colObj.transform.SetParent(canvasRoot.transform, false);
            collapsedGroup = colObj.AddComponent<CanvasGroup>();
            collapsedSummaryText = colObj.AddComponent<Text>();
            collapsedSummaryText.font = uiFont;
            collapsedSummaryText.fontSize = 13;
            collapsedSummaryText.alignment = TextAnchor.UpperLeft;
            collapsedSummaryText.horizontalOverflow = HorizontalWrapMode.Overflow;
            collapsedSummaryText.verticalOverflow = VerticalWrapMode.Overflow;
            collapsedSummaryText.color = HeaderTextColor;
            collapsedSummaryText.text = "[TAB] Tarefa";
            collapsedSummaryText.raycastTarget = false;

            Shadow colShadow = colObj.AddComponent<Shadow>();
            colShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            colShadow.effectDistance = new Vector2(1f, -1f);

            RectTransform colRect = collapsedSummaryText.rectTransform;
            colRect.anchorMin = new Vector2(0f, 1f);
            colRect.anchorMax = new Vector2(0f, 1f);
            colRect.pivot = new Vector2(0f, 1f);
            colRect.anchoredPosition = new Vector2(24f, -24f);
            colRect.sizeDelta = new Vector2(180f, 24f);
        }
    }
}
