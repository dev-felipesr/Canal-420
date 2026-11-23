using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Canal420.Audio;
using Canal420.Core;
using Canal420.Effects;
using Canal420.Interaction;
using Canal420.Player;
using Canal420.UI;

namespace Canal420.Puzzles
{
    public class SafeBoxPuzzle : MonoBehaviour, IInteractable
    {
        public static event Action OnSafeNoPowerExamined;
        public static event Action OnSafeUnlockedGlobal;

        [Header("Configuração da Senha e Energia")]
        [SerializeField] private string correctCode = "1009";
        [SerializeField] private bool isRevealed = false;
        [SerializeField] private bool requiresCircuitPower = true;
        [SerializeField] private string requiredPowerStateItemId = "energia_corredor_ativa";

        [Header("Textos e Feedback")]
        [SerializeField] private string noPowerPrompt = "Olhar o cofre";
        [SerializeField] private string interactPrompt = "Digitar senha no cofre";
        [TextArea(2, 4)]
        [SerializeField] private string noPowerSubtitle = "O teclado tá apagado. Sem força nessa parede... tenho que olhar o quadro de força na cozinha.";
        [TextArea(2, 4)]
        [SerializeField] private string unlockedSubtitle = "Uma chave... É a da porta do fundo.";
        [SerializeField] private float subtitleDuration = 6.0f;

        [Header("Referências Físicas")]
        [SerializeField] private Transform safeDoorPivot;
        [SerializeField] private Transform safeHandlePivot;
        [SerializeField] private Renderer digitalVisorRenderer;
        [SerializeField] private Light digitalVisorLight;
        [SerializeField] private GameObject brassKeyVisualInside;
        [SerializeField] private float safeDoorOpenAngle = -100f;
        [SerializeField] private UnityEvent onSafeUnlocked;
        private bool isUnlocked = false;
        private bool isKeypadOpen = false;
        private bool isValidating = false;
        private string currentInput = "";
        private Coroutine validateCoroutine;

        private int keypadOpenedFrame = -1;
        private float keypadOpenUnscaledTime = 0f;

        private PlayerController activePlayer;
        private GameObject keypadCanvasRoot;
        private CanvasGroup keypadCanvasGroup;
        private RectTransform keypadBoxRect;
        private Text displayCodeText;
        private Text statusHintText;
        private MaterialPropertyBlock visorPropBlock;

        private static SafeBoxPuzzle activeOpenSafe;

        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProp = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

        public bool CanInteract => isRevealed && !isUnlocked && !isKeypadOpen;
        public bool IsUnlocked => isUnlocked;

        private bool HasCircuitPower => !requiresCircuitPower || InventoryState.HasItem(requiredPowerStateItemId);

        public static void CloseAnyOpenKeypad()
        {
            if (activeOpenSafe != null && activeOpenSafe.isKeypadOpen)
            {
                activeOpenSafe.CloseKeypad(restoreControls: false);
            }
            activeOpenSafe = null;
        }

        private void Awake()
        {
            EnsureEventSystemExists();
            BuildKeypadUIIfNeeded();
            if (keypadCanvasRoot != null)
            {
                keypadCanvasRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            InventoryState.OnItemAdded += HandleItemStateChanged;
            ResetSafeState();
        }

        private void OnDisable()
        {
            InventoryState.OnItemAdded -= HandleItemStateChanged;
            if (isKeypadOpen)
            {
                CloseKeypad(restoreControls: false);
            }
        }

        private void OnDestroy()
        {
            if (activeOpenSafe == this)
            {
                activeOpenSafe = null;
            }
            if (keypadCanvasRoot != null)
            {
                Destroy(keypadCanvasRoot);
            }
        }

        private void HandleItemStateChanged(string itemId)
        {
            Refresh3DVisorGlow();
        }

        public void ResetSafeState()
        {
            StopAllCoroutines();
            validateCoroutine = null;
            bool wasOpen = isKeypadOpen || activeOpenSafe == this;
            isUnlocked = false;
            isKeypadOpen = false;
            isValidating = false;
            currentInput = "";
            if (wasOpen)
            {
                if (activeOpenSafe == this)
                {
                    activeOpenSafe = null;
                }
                ObjectiveHUD.IsBlockingModalOpen = false;
            }

            if (keypadCanvasRoot != null)
            {
                keypadCanvasRoot.SetActive(false);
            }

            if (safeDoorPivot != null)
            {
                safeDoorPivot.localRotation = Quaternion.identity;
            }
            if (safeHandlePivot != null)
            {
                safeHandlePivot.localRotation = Quaternion.identity;
            }

            if (brassKeyVisualInside != null)
            {
                brassKeyVisualInside.SetActive(true);
            }

            Refresh3DVisorGlow();
        }

        private void Refresh3DVisorGlow()
        {
            bool lit = isRevealed && HasCircuitPower;

            if (digitalVisorLight != null)
            {
                digitalVisorLight.type = LightType.Point;
                digitalVisorLight.color = new Color(0.24f, 0.78f, 0.42f);
                digitalVisorLight.range = 0.72f;
                digitalVisorLight.intensity = lit ? 0.14f : 0f;
                digitalVisorLight.shadows = LightShadows.None;
                digitalVisorLight.enabled = lit;
            }

            if (digitalVisorRenderer == null) return;

            if (visorPropBlock == null)
            {
                visorPropBlock = new MaterialPropertyBlock();
            }

            Color c = lit ? new Color(0.14f, 0.44f, 0.22f, 1f) : new Color(0.02f, 0.03f, 0.03f, 1f);
            Color emis = lit ? c * 1.85f : Color.black;

            digitalVisorRenderer.GetPropertyBlock(visorPropBlock);
            visorPropBlock.SetColor(BaseColorProp, c);
            visorPropBlock.SetColor(ColorProp, c);
            visorPropBlock.SetColor(EmissionColorProp, emis);
            digitalVisorRenderer.SetPropertyBlock(visorPropBlock);
        }

        private void Update()
        {
            if (!isKeypadOpen)
                return;

            if (keypadCanvasGroup != null)
            {
                keypadCanvasGroup.alpha = Mathf.MoveTowards(keypadCanvasGroup.alpha, 1f, Time.unscaledDeltaTime * 7.0f);
                if (keypadBoxRect != null)
                {
                    float s = Mathf.Lerp(0.96f, 1.0f, keypadCanvasGroup.alpha);
                    keypadBoxRect.localScale = new Vector3(s, s, 1f);
                }
            }

            if (Time.frameCount <= keypadOpenedFrame + 2 || (Time.unscaledTime - keypadOpenUnscaledTime) < 0.22f)
                return;

            HandleKeyboardShortcuts();
        }

        public string GetInteractionPrompt()
        {
            return HasCircuitPower ? interactPrompt : noPowerPrompt;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract)
                return;

            if (!HasCircuitPower)
            {
                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.MetalInspect, 0.75f);
                }
                if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(noPowerSubtitle))
                {
                    InteractionSystem.Instance.ShowSubtitle(noPowerSubtitle, subtitleDuration);
                }
                OnSafeNoPowerExamined?.Invoke();
                return;
            }

            activePlayer = interactor != null
                ? interactor.GetComponent<PlayerController>()
                : FindAnyObjectByType<PlayerController>();

            OpenKeypad();
        }

        public void SetRevealed(bool revealed)
        {
            isRevealed = revealed;
            Refresh3DVisorGlow();
        }

        private const string DefaultStatusHint = "MOD. CR-90  |  [0-9] Digitar   [Backspace] Apagar   [E / ESC] Voltar";
        private static readonly Color DisplayNormalColor = new Color(0.94f, 0.94f, 0.94f);
        private static readonly Color DisplayErrorColor = new Color(0.62f, 0.62f, 0.62f);
        private static readonly Color DisplayOpenColor = Color.white;
        private static readonly Color MutedHintColor = new Color(0.60f, 0.60f, 0.60f);

        private void ResetStatusHintToDefault()
        {
            if (statusHintText != null)
            {
                statusHintText.color = MutedHintColor;
                statusHintText.text = DefaultStatusHint;
            }
        }

        private void OpenKeypad()
        {
            EnsureEventSystemExists();
            BuildKeypadUIIfNeeded();

            isKeypadOpen = true;
            activeOpenSafe = this;
            isValidating = false;
            currentInput = "";
            keypadOpenedFrame = Time.frameCount;
            keypadOpenUnscaledTime = Time.unscaledTime;
            ObjectiveHUD.IsBlockingModalOpen = true;
            RefreshDisplayText(DisplayNormalColor);

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.MetalInspect, 0.70f);
            }

            ResetStatusHintToDefault();

            if (keypadCanvasRoot != null)
            {
                keypadCanvasRoot.SetActive(true);
            }
            if (keypadCanvasGroup != null)
            {
                keypadCanvasGroup.alpha = 0f;
            }
            if (keypadBoxRect != null)
            {
                keypadBoxRect.localScale = new Vector3(0.96f, 0.96f, 1f);
            }

            if (activePlayer != null)
            {
                activePlayer.SetControlsEnabled(moveEnabled: false, lookEnabled: false, lockCursor: false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void CloseKeypad(bool restoreControls = true)
        {
            if (restoreControls && isUnlocked && validateCoroutine != null)
                return;

            if (validateCoroutine != null)
            {
                StopCoroutine(validateCoroutine);
                validateCoroutine = null;
            }

            isKeypadOpen = false;
            isValidating = false;
            if (activeOpenSafe == this)
            {
                activeOpenSafe = null;
            }
            ObjectiveHUD.IsBlockingModalOpen = false;
            ObjectiveHUD.NotifyModalClosedThisFrame();

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if (InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.SuppressInteractionBriefly(0.26f);
            }

            if (keypadCanvasRoot != null)
            {
                keypadCanvasRoot.SetActive(false);
            }

            if (restoreControls)
            {
                if (activePlayer == null) activePlayer = FindAnyObjectByType<PlayerController>();
                if (activePlayer != null)
                {
                    bool allowMove = !activePlayer.IsRestrictedLook;
                    activePlayer.SetControlsEnabled(moveEnabled: allowMove, lookEnabled: true, lockCursor: true);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        public void PressDigit(int digit)
        {
            if (!isKeypadOpen || isValidating || currentInput.Length >= correctCode.Length)
                return;

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.KeypadBeep, 0.75f, 0.96f + (digit * 0.012f));
            }

            if (currentInput.Length == 0)
            {
                ResetStatusHintToDefault();
            }

            currentInput += digit.ToString();
            RefreshDisplayText(DisplayNormalColor);

            if (currentInput.Length == correctCode.Length)
            {
                if (validateCoroutine != null)
                {
                    StopCoroutine(validateCoroutine);
                }
                validateCoroutine = StartCoroutine(ValidateCodeRoutine());
            }
        }

        public void ClearDigits()
        {
            if (!isKeypadOpen || isValidating)
                return;

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.KeypadBeep, 0.65f, 0.82f);
            }

            currentInput = "";
            ResetStatusHintToDefault();
            RefreshDisplayText(DisplayNormalColor);
        }

        private void BackspaceDigit()
        {
            if (!isKeypadOpen || isValidating || currentInput.Length == 0)
                return;

            if (Canal420AudioManager.Instance != null)
            {
                Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.KeypadBeep, 0.65f, 0.85f);
            }

            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            RefreshDisplayText(DisplayNormalColor);
        }

        private IEnumerator ValidateCodeRoutine()
        {
            isValidating = true;
            string attempted = currentInput;
            yield return new WaitForSeconds(0.28f);

            if (attempted == correctCode.Trim())
            {
                isUnlocked = true;
                DoorInteractable.PlayerHasSafeKey = true;
                InventoryState.AddItem("chave_porta_corredor");

                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.KeypadUnlock, 0.95f);
                }

                if (displayCodeText != null)
                {
                    displayCodeText.color = DisplayOpenColor;
                    displayCodeText.text = "A B E R T O";
                }

                if (statusHintText != null)
                {
                    statusHintText.color = DisplayOpenColor;
                    statusHintText.text = "Trava liberada.";
                }

                // Let the player read "A B E R T O", then dismiss the modal so the 3D handle turn, door swing, and brass key reveal are clearly visible.
                yield return new WaitForSeconds(0.36f);

                if (keypadCanvasRoot != null)
                {
                    keypadCanvasRoot.SetActive(false);
                }
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
                if (activePlayer == null) activePlayer = FindAnyObjectByType<PlayerController>();
                if (activePlayer != null)
                {
                    activePlayer.SetControlsEnabled(moveEnabled: false, lookEnabled: true, lockCursor: true);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                if (safeHandlePivot != null)
                {
                    if (Canal420AudioManager.Instance != null)
                    {
                        Canal420AudioManager.Instance.PlayFoley3D(FoleySoundId.DoorUnlockKey, transform.position, 0.85f);
                    }
                    float handleDur = 0.24f;
                    float hTime = 0f;
                    while (hTime < handleDur)
                    {
                        hTime += Time.deltaTime;
                        float hu = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(hTime / handleDur));
                        safeHandlePivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -90f, hu));
                        yield return null;
                    }
                    safeHandlePivot.localRotation = Quaternion.Euler(0f, 0f, -90f);
                }

                float swingDur = 0.56f;
                float sTime = 0f;
                while (sTime < swingDur)
                {
                    sTime += Time.deltaTime;
                    float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(sTime / swingDur));
                    if (safeDoorPivot != null)
                    {
                        safeDoorPivot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, safeDoorOpenAngle, u), 0f);
                    }
                    yield return null;
                }

                if (safeDoorPivot != null)
                {
                    safeDoorPivot.localRotation = Quaternion.Euler(0f, safeDoorOpenAngle, 0f);
                }

                // Brief beat so the player clearly sees the brass key sitting on the felt inside the open safe before taking it.
                yield return new WaitForSeconds(0.38f);

                if (brassKeyVisualInside != null)
                {
                    brassKeyVisualInside.SetActive(false);
                }

                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.KeyPickup, 0.82f);
                }

                OnSafeUnlockedGlobal?.Invoke();
                onSafeUnlocked?.Invoke();

                validateCoroutine = null;
                CloseKeypad(restoreControls: true);

                if (InteractionSystem.Instance != null && !string.IsNullOrWhiteSpace(unlockedSubtitle))
                {
                    InteractionSystem.Instance.ShowSubtitle(unlockedSubtitle, subtitleDuration);
                }
            }
            else
            {
                if (Canal420AudioManager.Instance != null)
                {
                    Canal420AudioManager.Instance.PlayFoley2D(FoleySoundId.KeypadError, 0.85f);
                }

                if (SanityEffects.Instance != null)
                {
                    SanityEffects.Instance.TriggerHallucinationSpike(0.30f, 0.4f);
                }

                if (displayCodeText != null)
                {
                    displayCodeText.color = DisplayErrorColor;
                    displayCodeText.text = "E R R O";
                }

                bool isYearAttempt = attempted == "1994" || attempted == "1995" || attempted == "1992";
                if (statusHintText != null)
                {
                    if (isYearAttempt)
                    {
                        statusHintText.color = new Color(0.95f, 0.95f, 0.95f);
                        statusHintText.text = "Não é o ano... São quatro números. Dia e mês.";
                    }
                    else
                    {
                        statusHintText.color = MutedHintColor;
                        statusHintText.text = "Código incorreto.";
                    }
                }

                yield return new WaitForSeconds(0.75f);

                currentInput = "";
                RefreshDisplayText(DisplayNormalColor);
                isValidating = false;
                validateCoroutine = null;
            }
        }

        private void RefreshDisplayText(Color color)
        {
            if (displayCodeText == null)
                return;

            displayCodeText.color = color;

            int len = Mathf.Max(1, correctCode.Trim().Length);
            char[] slots = new char[len];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = i < currentInput.Length ? currentInput[i] : '-';
            }

            displayCodeText.text = string.Join(" ", slots);
        }

        private readonly Button[] keypadButtons = new Button[12];
        private int gamepadSelectedIndex = 0;
        private float lastGamepadNavTime = -10f;

        private void HandleKeyboardShortcuts()
        {
            if (isValidating) return;

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.buttonEast.wasPressedThisFrame)
                {
                    CloseKeypad(restoreControls: true);
                    return;
                }

                if (pad.buttonWest.wasPressedThisFrame)
                {
                    BackspaceDigit();
                    return;
                }

                Vector2 nav = pad.dpad.ReadValue();
                if (nav.sqrMagnitude < 0.25f)
                {
                    nav = pad.leftStick.ReadValue();
                }

                if (nav.sqrMagnitude >= 0.36f && (Time.unscaledTime - lastGamepadNavTime) >= 0.19f)
                {
                    int col = gamepadSelectedIndex % 3;
                    int row = gamepadSelectedIndex / 3;
                    if (Mathf.Abs(nav.x) > Mathf.Abs(nav.y))
                    {
                        col = (col + (nav.x > 0f ? 1 : 2)) % 3;
                    }
                    else
                    {
                        row = (row + (nav.y > 0f ? 3 : 1)) % 4;
                    }
                    gamepadSelectedIndex = row * 3 + col;
                    lastGamepadNavTime = Time.unscaledTime;
                    if (EventSystem.current != null && keypadButtons[gamepadSelectedIndex] != null)
                    {
                        EventSystem.current.SetSelectedGameObject(keypadButtons[gamepadSelectedIndex].gameObject);
                    }
                }

                if (pad.buttonSouth.wasPressedThisFrame)
                {
                    Button targetBtn = keypadButtons[Mathf.Clamp(gamepadSelectedIndex, 0, keypadButtons.Length - 1)];
                    if (targetBtn != null)
                    {
                        targetBtn.onClick.Invoke();
                    }
                    return;
                }
            }

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                CloseKeypad(restoreControls: true);
                return;
            }

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame ||
                kb.eKey.wasPressedThisFrame ||
                kb.tabKey.wasPressedThisFrame ||
                kb.qKey.wasPressedThisFrame)
            {
                CloseKeypad(restoreControls: true);
                return;
            }

            if (kb.backspaceKey.wasPressedThisFrame)
            {
                BackspaceDigit();
                return;
            }

            if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame) PressDigit(0);
            else if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) PressDigit(1);
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) PressDigit(2);
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) PressDigit(3);
            else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) PressDigit(4);
            else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) PressDigit(5);
            else if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) PressDigit(6);
            else if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) PressDigit(7);
            else if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) PressDigit(8);
            else if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame) PressDigit(9);
        }

        private void EnsureEventSystemExists()
        {
            if (EventSystem.current != null)
                return;

            GameObject esObj = new GameObject("EventSystem_Auto");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildKeypadUIIfNeeded()
        {
            if (keypadCanvasRoot != null)
                return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            keypadCanvasRoot = new GameObject("HUD_SafeBoxKeypad_Auto");
            Canvas canvas = keypadCanvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;

            CanvasScaler scaler = keypadCanvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            keypadCanvasGroup = keypadCanvasRoot.AddComponent<CanvasGroup>();
            keypadCanvasRoot.AddComponent<GraphicRaycaster>();

            GameObject dimBg = new GameObject("Dim_Background");
            dimBg.transform.SetParent(keypadCanvasRoot.transform, false);
            Image dimImg = dimBg.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.78f);
            RectTransform dimRect = dimImg.rectTransform;
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            GameObject boxPanel = new GameObject("Keypad_Body");
            boxPanel.transform.SetParent(dimBg.transform, false);
            Image boxImg = boxPanel.AddComponent<Image>();
            boxImg.color = new Color(0.055f, 0.055f, 0.06f, 0.98f);

            keypadBoxRect = boxImg.rectTransform;
            keypadBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            keypadBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            keypadBoxRect.anchoredPosition = Vector2.zero;
            keypadBoxRect.sizeDelta = new Vector2(430f, 570f);

            CreateLabel(boxPanel.transform, font, "COFRE CR-90", 19, FontStyle.Bold,
                new Color(0.86f, 0.86f, 0.86f), new Vector2(0f, 236f), new Vector2(370f, 36f));

            GameObject screenObj = new GameObject("Digital_Screen");
            screenObj.transform.SetParent(boxPanel.transform, false);
            Image screenImg = screenObj.AddComponent<Image>();
            screenImg.color = new Color(0.025f, 0.025f, 0.028f, 1f);
            RectTransform screenRect = screenImg.rectTransform;
            screenRect.anchorMin = new Vector2(0.5f, 0.5f);
            screenRect.anchorMax = new Vector2(0.5f, 0.5f);
            screenRect.anchoredPosition = new Vector2(0f, 168f);
            screenRect.sizeDelta = new Vector2(340f, 74f);

            displayCodeText = CreateLabel(screenObj.transform, font, "- - - -", 40, FontStyle.Bold,
                DisplayNormalColor, Vector2.zero, new Vector2(320f, 68f));

            GameObject gridObj = new GameObject("Buttons_Grid");
            gridObj.transform.SetParent(boxPanel.transform, false);
            RectTransform gridRect = gridObj.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(0f, -52f);
            gridRect.sizeDelta = new Vector2(340f, 330f);

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(102f, 72f);
            grid.spacing = new Vector2(16f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (int n = 1; n <= 9; n++)
            {
                int captured = n;
                keypadButtons[n - 1] = CreateKeypadButton(gridObj.transform, font, captured.ToString(), new Color(0.12f, 0.12f, 0.13f), () => PressDigit(captured));
            }

            keypadButtons[9] = CreateKeypadButton(gridObj.transform, font, "LIMPAR", new Color(0.17f, 0.17f, 0.18f), ClearDigits);
            keypadButtons[10] = CreateKeypadButton(gridObj.transform, font, "0", new Color(0.12f, 0.12f, 0.13f), () => PressDigit(0));
            keypadButtons[11] = CreateKeypadButton(gridObj.transform, font, "SAIR", new Color(0.17f, 0.17f, 0.18f), () =>
            {
                if (!isValidating)
                {
                    CloseKeypad(true);
                }
            });

            statusHintText = CreateLabel(boxPanel.transform, font,
                DefaultStatusHint,
                14, FontStyle.Normal, MutedHintColor, new Vector2(0f, -250f), new Vector2(396f, 40f));
        }

        private Text CreateLabel(Transform parent, Font font, string content, int size, FontStyle style, Color color, Vector2 pos, Vector2 sizeDelta)
        {
            GameObject obj = new GameObject("Label");
            obj.transform.SetParent(parent, false);
            Text txt = obj.AddComponent<Text>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            RectTransform rect = txt.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = sizeDelta;
            return txt;
        }

        private Button CreateKeypadButton(Transform parent, Font font, string label, Color bgColor, UnityAction onClick)
        {
            GameObject btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.22f, 1.22f, 1.22f, 1f);
            cb.selectedColor = new Color(1.30f, 1.30f, 1.30f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(onClick);

            CreateLabel(btnObj.transform, font, label, label.Length > 1 ? 16 : 28, FontStyle.Bold,
                new Color(0.94f, 0.94f, 0.94f), Vector2.zero, new Vector2(100f, 68f));
            return btn;
        }

        public void ConfigureSafe(string code, Transform doorPivot, Renderer visorRenderer, bool needsPower = true, GameObject brassKeyVisual = null, Light visorLight = null, Transform handlePivot = null)
        {
            correctCode = code;
            isRevealed = false;
            safeDoorPivot = doorPivot;
            safeHandlePivot = handlePivot;
            digitalVisorRenderer = visorRenderer;
            digitalVisorLight = visorLight;
            requiresCircuitPower = needsPower;
            brassKeyVisualInside = brassKeyVisual;
        }
    }
}
