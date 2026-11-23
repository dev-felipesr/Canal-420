using UnityEngine;
using UnityEngine.UI;

namespace Canal420.Effects
{
    public enum SubliminalType
    {
        HollowEyes = 0,
        Date1009 = 1,
        OpenDoorway = 2,
        StaticTear = 3,
        MargaretImpact = 4
    }

    public class HorrorScreenOverlay : MonoBehaviour
    {
        public static HorrorScreenOverlay Instance { get; private set; }

        private Canvas overlayCanvas;
        private RawImage subliminalImage;
        private RawImage panicEdgeImage;

        private Texture2D texHollowEyes;
        private Texture2D texDate1009;
        private Texture2D texOpenDoorway;
        private Texture2D texStaticTear;
        private Texture2D texMargaretImpact;
        private Texture2D texPanicEdge;

        private const int MinFlashVisibleFrames = 4;

        private float flashTimer = 0f;
        private float flashDuration = 0.095f;
        private float flashMaxAlpha = 0.42f;
        private int flashRemainingMinFrames = 0;
        private Color flashTint = Color.white;
        private float targetPanicWeight = 0f;
        private float currentPanicWeight = 0f;

        public static HorrorScreenOverlay EnsureExists()
        {
            if (Instance != null) return Instance;

            HorrorScreenOverlay existing = FindAnyObjectByType<HorrorScreenOverlay>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            GameObject go = new GameObject("HorrorScreenOverlay");
            return go.AddComponent<HorrorScreenOverlay>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildTextures();
            BuildOverlayUI();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            DestroyTex(ref texHollowEyes);
            DestroyTex(ref texDate1009);
            DestroyTex(ref texOpenDoorway);
            DestroyTex(ref texStaticTear);
            DestroyTex(ref texMargaretImpact);
            DestroyTex(ref texPanicEdge);
        }

        private static void DestroyTex(ref Texture2D tex)
        {
            if (tex != null)
            {
                if (Application.isPlaying) Destroy(tex);
                else DestroyImmediate(tex);
                tex = null;
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0.001f)
                return;

            if (subliminalImage != null)
            {
                if (flashTimer > 0f || flashRemainingMinFrames > 0)
                {
                    float timeU = flashTimer > 0f
                        ? Mathf.Clamp01(flashTimer / Mathf.Max(0.01f, flashDuration))
                        : 0f;
                    float frameFloorU = flashRemainingMinFrames > 0
                        ? (flashRemainingMinFrames / (float)MinFlashVisibleFrames) * 0.45f
                        : 0f;
                    float u = Mathf.Max(timeU, frameFloorU);
                    // Sharp instant hit, tight crisp tail so it reads as an involuntary neural flash
                    float alpha = flashMaxAlpha * Mathf.Pow(u, 0.42f);
                    Color c = flashTint;
                    c.a = alpha;
                    subliminalImage.color = c;
                    subliminalImage.enabled = true;

                    float microJitterX = Random.Range(-4f, 4f);
                    float microJitterY = Random.Range(-3f, 3f);
                    subliminalImage.rectTransform.anchoredPosition = new Vector2(microJitterX, microJitterY);

                    flashTimer = Mathf.Max(0f, flashTimer - Time.unscaledDeltaTime);
                    if (flashRemainingMinFrames > 0)
                    {
                        flashRemainingMinFrames--;
                    }
                }
                else if (subliminalImage.enabled)
                {
                    subliminalImage.enabled = false;
                    subliminalImage.color = Color.clear;
                    subliminalImage.rectTransform.anchoredPosition = Vector2.zero;
                }
            }

            if (panicEdgeImage != null)
            {
                currentPanicWeight = Mathf.MoveTowards(currentPanicWeight, targetPanicWeight, Time.unscaledDeltaTime * 2.4f);
                // Threshold prevents murky darkening during calm exploration
                float activePanic = Mathf.InverseLerp(0.22f, 1.0f, currentPanicWeight);
                if (activePanic > 0.005f)
                {
                    float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 7.5f);
                    float alpha = Mathf.Clamp(activePanic * 0.16f * pulse, 0f, 0.18f);
                    panicEdgeImage.color = new Color(0.05f, 0.05f, 0.055f, alpha);
                    panicEdgeImage.enabled = true;
                }
                else if (panicEdgeImage.enabled)
                {
                    panicEdgeImage.enabled = false;
                }
            }
        }

        public void SetPanicWeight(float weight01)
        {
            targetPanicWeight = Mathf.Clamp01(weight01);
        }

        public void ClearImmediate()
        {
            flashTimer = 0f;
            flashRemainingMinFrames = 0;
            flashDuration = 0.095f;
            flashMaxAlpha = 0f;
            targetPanicWeight = 0f;
            currentPanicWeight = 0f;

            if (subliminalImage != null)
            {
                subliminalImage.enabled = false;
                subliminalImage.color = Color.clear;
                subliminalImage.rectTransform.anchoredPosition = Vector2.zero;
            }

            if (panicEdgeImage != null)
            {
                panicEdgeImage.enabled = false;
                panicEdgeImage.color = Color.clear;
            }
        }

        public void TriggerSubliminalFlash(SubliminalType type, float duration = 0.095f, float maxAlpha = 0.40f)
        {
            if (texHollowEyes == null) BuildTextures();
            if (subliminalImage == null) BuildOverlayUI();

            Texture2D tex = GetTextureForType(type);
            if (tex == null) return;

            subliminalImage.texture = tex;
            flashDuration = Mathf.Clamp(duration, 0.075f, 0.25f);
            flashTimer = flashDuration;
            flashRemainingMinFrames = MinFlashVisibleFrames;
            flashMaxAlpha = Mathf.Clamp(maxAlpha, 0.10f, 0.85f);
            flashTint = (type == SubliminalType.Date1009)
                ? new Color(0.92f, 0.84f, 0.82f, 1f)
                : new Color(0.94f, 0.94f, 0.95f, 1f);
        }

        public void TriggerHighContrastStrobeFrame(float duration = 0.14f)
        {
            if (texStaticTear == null) BuildTextures();
            if (subliminalImage == null) BuildOverlayUI();
            if (texStaticTear == null) return;

            subliminalImage.texture = texStaticTear;
            flashDuration = Mathf.Clamp(duration, 0.08f, 0.28f);
            flashTimer = flashDuration;
            flashRemainingMinFrames = MinFlashVisibleFrames;
            flashMaxAlpha = 0.24f;
            flashTint = new Color(0.90f, 0.90f, 0.92f, 1f);
        }

        private Texture2D GetTextureForType(SubliminalType type)
        {
            switch (type)
            {
                case SubliminalType.HollowEyes: return texHollowEyes;
                case SubliminalType.Date1009: return texDate1009;
                case SubliminalType.OpenDoorway: return texOpenDoorway;
                case SubliminalType.StaticTear: return texStaticTear;
                case SubliminalType.MargaretImpact: return texMargaretImpact;
                default: return texStaticTear;
            }
        }

        private void BuildOverlayUI()
        {
            if (overlayCanvas != null) return;

            GameObject canvasGo = new GameObject("Canvas_HorrorScreenOverlay");
            canvasGo.transform.SetParent(transform, false);
            overlayCanvas = canvasGo.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 55;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            panicEdgeImage = CreateFullscreenRawImage(canvasGo.transform, "PanicEdge", texPanicEdge);
            panicEdgeImage.enabled = false;

            subliminalImage = CreateFullscreenRawImage(canvasGo.transform, "SubliminalFlash", texStaticTear);
            subliminalImage.enabled = false;
        }

        private static RawImage CreateFullscreenRawImage(Transform parent, string name, Texture2D tex)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-12f, -12f);
            rt.offsetMax = new Vector2(12f, 12f);

            RawImage img = go.AddComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;
            img.color = Color.clear;
            return img;
        }

        private void BuildTextures()
        {
            // Analog VHS tracking/static textures only — zero cartoon eyes, bitmap text, or flat 2D doors
            texStaticTear = BuildStaticTearTexture(256, 256);
            texHollowEyes = BuildAnalogTrackingTexture(256, 256, seed: 4201, heavyBands: false);
            texDate1009 = BuildAnalogTrackingTexture(256, 256, seed: 1009, heavyBands: true);
            texOpenDoorway = BuildAnalogTrackingTexture(256, 256, seed: 7703, heavyBands: false);
            texMargaretImpact = BuildAnalogTrackingTexture(256, 256, seed: 9104, heavyBands: true);
            texPanicEdge = BuildPanicEdgeTexture(128, 128);
        }

        private static Texture2D CreateConfiguredTex(int w, int h, string name)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        private static Texture2D BuildAnalogTrackingTexture(int w, int h, int seed, bool heavyBands)
        {
            Texture2D tex = CreateConfiguredTex(w, h, $"Subliminal_AnalogVHS_{seed}");
            Color32[] px = new Color32[w * h];
            var rng = new System.Random(seed);

            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                bool trackingBand = (heavyBands && ((y % 19) < 4 || (ny > 0.42f && ny < 0.56f)))
                    || (!heavyBands && ((y % 29) < 3))
                    || (rng.NextDouble() < (heavyBands ? 0.14 : 0.08));
                float scanMask = ((y % 3) == 0) ? 0.72f : 1.0f;
                float bandAlpha = trackingBand ? (heavyBands ? 0.52f : 0.36f) : 0.08f;

                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    float edge = Mathf.Abs(nx - 0.5f) * 2f;
                    float edgeVignette = Mathf.SmoothStep(0.35f, 1.0f, edge) * 0.25f;
                    float noise = (float)rng.NextDouble();
                    float lum = trackingBand
                        ? (noise > 0.62f ? 0.82f : 0.08f) * scanMask
                        : (0.04f + noise * 0.14f) * scanMask;
                    float alpha = Mathf.Clamp01(bandAlpha + edgeVignette);
                    byte lv = (byte)Mathf.RoundToInt(Mathf.Clamp01(lum) * 255f);
                    byte av = (byte)Mathf.RoundToInt(alpha * 255f);
                    px[y * w + x] = new Color32(lv, lv, lv, av);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        private static Texture2D BuildStaticTearTexture(int w, int h)
        {
            Texture2D tex = CreateConfiguredTex(w, h, "Subliminal_StaticTear");
            Color32[] px = new Color32[w * h];
            var rng = new System.Random(4209);

            for (int y = 0; y < h; y++)
            {
                bool strongBand = (y % 13 < 3) || (rng.NextDouble() < 0.14);
                float bandAlpha = strongBand ? 0.65f : 0.16f;
                for (int x = 0; x < w; x++)
                {
                    float lum = (float)rng.NextDouble();
                    byte lv = (byte)Mathf.RoundToInt((lum > 0.55f ? 0.86f : 0.06f) * 255f);
                    byte av = (byte)Mathf.RoundToInt(bandAlpha * 255f);
                    px[y * w + x] = new Color32(lv, lv, lv, av);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        private static Texture2D BuildPanicEdgeTexture(int w, int h)
        {
            Texture2D tex = CreateConfiguredTex(w, h, "Subliminal_PanicEdge");
            Color32[] px = new Color32[w * h];
            var rng = new System.Random(313);

            for (int y = 0; y < h; y++)
            {
                float ny = (y / (float)(h - 1)) * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    float nx = (x / (float)(w - 1)) * 2f - 1f;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);
                    float edge = Mathf.SmoothStep(0.58f, 1.32f, dist);
                    float grain = 0.78f + (float)rng.NextDouble() * 0.22f;
                    byte av = (byte)Mathf.RoundToInt(Mathf.Clamp01(edge * grain) * 255f);
                    px[y * w + x] = new Color32(10, 10, 12, av);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }
    }
}
