using UnityEngine;
using UnityEngine.Rendering;

namespace Canal420.Effects
{
    public class FaucetWaterController : MonoBehaviour
    {
        [Header("Referências da Torneira")]
        [SerializeField] private Transform nozzleOrigin;
        [SerializeField] private Transform coldHandleTransform;
        [SerializeField] private float basinFloorY = 0.774f;
        [SerializeField] private float streamForwardZOffset = -0.028f;
        [SerializeField] private float runDuration = 2.15f;

        private const float JetDropDuration = 0.11f;
        private const float TailDropDuration = 0.16f;
        private const float HandleTurnDuration = 0.18f;
        private const float HandleTurnAngle = 72f;

        private Vector3 handleClosedEuler;
        private bool handleEulerCached = false;

        private GameObject streamObject;
        private MeshRenderer streamRenderer;
        private Material streamMaterial;
        private Mesh streamMesh;

        private GameObject puddleObject;
        private MeshRenderer puddleRenderer;
        private Material puddleMaterial;

        private readonly GameObject[] rippleObjects = new GameObject[2];
        private readonly MeshRenderer[] rippleRenderers = new MeshRenderer[2];
        private readonly Material[] rippleMaterials = new Material[2];

        private ParticleSystem splashParticles;
        private Material particleMaterial;

        private Texture2D streamTexture;
        private Texture2D rippleTexture;
        private Texture2D puddleTexture;
        private Texture2D dropletTexture;

        private bool visualsBuilt = false;
        private bool isRunning = false;
        private float elapsedTime = 0f;
        private float handleOpen01 = 0f;
        private float puddleFill01 = 0f;
        private float uvScrollY = 0f;
        private float rippleClock = 0f;

        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProp = Shader.PropertyToID("_Color");
        private static readonly int BaseMapProp = Shader.PropertyToID("_BaseMap");
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

        public bool IsRunning => isRunning;

        public void ConfigureReferences(
            Transform nozzle,
            Transform coldHandle,
            float sinkBasinFloorY = 0.774f,
            float forwardArcZ = -0.028f,
            float duration = 2.15f)
        {
            nozzleOrigin = nozzle;
            coldHandleTransform = coldHandle;
            basinFloorY = sinkBasinFloorY;
            streamForwardZOffset = forwardArcZ;
            runDuration = Mathf.Max(0.8f, duration);
            CacheHandleRestPose();
        }

        private void Awake()
        {
            CacheHandleRestPose();
            EnsureVisualsBuilt();
            HideAllEffectsImmediate();
        }

        private void OnDisable()
        {
            ResetImmediate();
        }

        private void OnDestroy()
        {
            if (streamMesh != null) Destroy(streamMesh);
            if (streamMaterial != null) Destroy(streamMaterial);
            if (puddleMaterial != null) Destroy(puddleMaterial);
            if (particleMaterial != null) Destroy(particleMaterial);
            for (int i = 0; i < rippleMaterials.Length; i++)
            {
                if (rippleMaterials[i] != null) Destroy(rippleMaterials[i]);
            }

            if (streamTexture != null) Destroy(streamTexture);
            if (rippleTexture != null) Destroy(rippleTexture);
            if (puddleTexture != null) Destroy(puddleTexture);
            if (dropletTexture != null) Destroy(dropletTexture);
        }

        private void CacheHandleRestPose()
        {
            if (coldHandleTransform != null && !handleEulerCached)
            {
                handleClosedEuler = coldHandleTransform.localEulerAngles;
                handleEulerCached = true;
            }
        }

        public void TriggerRunWater()
        {
            CacheHandleRestPose();
            EnsureVisualsBuilt();

            if (isRunning && elapsedTime > JetDropDuration && elapsedTime < (runDuration - TailDropDuration - 0.15f))
            {
                // Already in steady stream: extend run cleanly without snapping shut
                elapsedTime = JetDropDuration;
                return;
            }

            isRunning = true;
            elapsedTime = 0f;
            rippleClock = 0f;
            if (puddleFill01 < 0.05f) puddleFill01 = 0f;
        }

        public void ResetImmediate()
        {
            isRunning = false;
            elapsedTime = 0f;
            handleOpen01 = 0f;
            puddleFill01 = 0f;
            ApplyHandleRotation();
            HideAllEffectsImmediate();
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f)
                return;

            if (!isRunning && puddleFill01 <= 0.001f && handleOpen01 <= 0.001f)
                return;

            EnsureVisualsBuilt();

            Vector3 nozzlePos = GetNozzleWorldPosition();
            Vector3 impactPos = GetBasinImpactWorldPosition(nozzlePos);

            if (isRunning)
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime >= runDuration)
                {
                    isRunning = false;
                    elapsedTime = runDuration;
                }
            }

            float shutoffStart = Mathf.Max(JetDropDuration + 0.25f, runDuration - TailDropDuration - 0.08f);
            float targetHandleOpen;
            if (!isRunning)
            {
                targetHandleOpen = 0f;
            }
            else if (elapsedTime < HandleTurnDuration)
            {
                targetHandleOpen = Mathf.SmoothStep(0f, 1f, elapsedTime / HandleTurnDuration);
            }
            else if (elapsedTime >= shutoffStart)
            {
                float closeU = Mathf.Clamp01((elapsedTime - shutoffStart) / Mathf.Max(0.08f, runDuration - shutoffStart));
                targetHandleOpen = 1f - Mathf.SmoothStep(0f, 1f, closeU);
            }
            else
            {
                targetHandleOpen = 1f;
            }

            handleOpen01 = Mathf.MoveTowards(handleOpen01, targetHandleOpen, Time.deltaTime * 8.5f);
            ApplyHandleRotation();

            UpdateStreamAndImpact(nozzlePos, impactPos);
        }

        private void UpdateStreamAndImpact(Vector3 nozzlePos, Vector3 impactPos)
        {
            float tailStart = Mathf.Max(JetDropDuration + 0.15f, runDuration - TailDropDuration);
            bool streamActive = isRunning && elapsedTime < runDuration;
            bool hittingBasin = false;

            if (streamActive)
            {
                float topProgress = elapsedTime >= tailStart
                    ? Mathf.Clamp01((elapsedTime - tailStart) / TailDropDuration)
                    : 0f;
                float bottomProgress = elapsedTime < JetDropDuration
                    ? Mathf.Clamp01(elapsedTime / JetDropDuration)
                    : 1f;

                hittingBasin = bottomProgress >= 0.96f && topProgress < 0.92f;

                Vector3 topPos = Vector3.Lerp(nozzlePos, impactPos, topProgress);
                Vector3 bottomPos = Vector3.Lerp(nozzlePos, impactPos, bottomProgress);
                Vector3 delta = bottomPos - topPos;
                float len = delta.magnitude;

                if (len > 0.004f && streamObject != null && streamRenderer != null && streamMaterial != null)
                {
                    streamObject.SetActive(true);
                    streamRenderer.enabled = true;
                    streamObject.transform.position = topPos;
                    streamObject.transform.rotation = Quaternion.FromToRotation(Vector3.down, delta / len);

                    float pulse = 1f + 0.07f * Mathf.Sin(Time.time * 42f) + 0.04f * Mathf.Cos(Time.time * 67f);
                    float tailTaper = Mathf.Lerp(1f, 0.35f, topProgress);
                    float widthScale = pulse * tailTaper * Mathf.Lerp(0.75f, 1f, Mathf.Min(1f, elapsedTime / 0.08f));
                    streamObject.transform.localScale = new Vector3(widthScale, len, widthScale);

                    uvScrollY = (uvScrollY + Time.deltaTime * 4.8f) % 1f;
                    streamMaterial.mainTextureOffset = new Vector2(0f, uvScrollY);
                    if (streamMaterial.HasProperty(BaseMapProp))
                    {
                        streamMaterial.SetTextureOffset(BaseMapProp, new Vector2(0f, uvScrollY));
                    }

                    float alpha = 0.78f * (1f - topProgress * 0.7f);
                    SetMaterialAlphaColor(streamMaterial, new Color(0.84f, 0.93f, 0.98f, alpha), new Color(0.11f, 0.17f, 0.21f) * alpha);
                }
                else if (streamObject != null)
                {
                    streamObject.SetActive(false);
                }
            }
            else if (streamObject != null)
            {
                streamObject.SetActive(false);
            }

            // Basin puddle fills while stream strikes and drains smoothly afterward
            float targetPuddle = hittingBasin ? 1f : 0f;
            float puddleSpeed = hittingBasin ? 3.2f : 1.35f;
            puddleFill01 = Mathf.MoveTowards(puddleFill01, targetPuddle, Time.deltaTime * puddleSpeed);

            if (puddleObject != null && puddleRenderer != null && puddleMaterial != null)
            {
                if (puddleFill01 > 0.005f)
                {
                    puddleObject.SetActive(true);
                    puddleRenderer.enabled = true;
                    Vector3 drainTarget = new Vector3(impactPos.x, basinFloorY + 0.0020f, impactPos.z - 0.020f);
                    puddleObject.transform.position = Vector3.Lerp(drainTarget, impactPos + Vector3.up * 0.0020f, puddleFill01);
                    float puddleSize = Mathf.Lerp(0.03f, 0.18f, Mathf.SmoothStep(0f, 1f, puddleFill01));
                    puddleObject.transform.localScale = new Vector3(puddleSize, puddleSize, 1f);
                    SetMaterialAlphaColor(puddleMaterial, new Color(0.80f, 0.90f, 0.95f, puddleFill01 * 0.42f), new Color(0.08f, 0.13f, 0.16f) * puddleFill01);
                }
                else
                {
                    puddleObject.SetActive(false);
                }
            }

            // Concentric splash ripples on basin surface
            if (hittingBasin)
            {
                rippleClock += Time.deltaTime * 2.15f;
            }

            for (int i = 0; i < rippleObjects.Length; i++)
            {
                GameObject rObj = rippleObjects[i];
                MeshRenderer rRen = rippleRenderers[i];
                Material rMat = rippleMaterials[i];
                if (rObj == null || rRen == null || rMat == null) continue;

                if (hittingBasin || puddleFill01 > 0.15f)
                {
                    float phase = (rippleClock + i * 0.5f) % 1f;
                    float rippleVisibility = hittingBasin ? 1f : Mathf.InverseLerp(0.15f, 0.70f, puddleFill01);
                    float alpha = Mathf.Sin(phase * Mathf.PI) * 0.52f * rippleVisibility;
                    if (alpha > 0.01f)
                    {
                        rObj.SetActive(true);
                        rRen.enabled = true;
                        rObj.transform.position = impactPos + Vector3.up * (0.0030f + i * 0.0005f);
                        float scale = Mathf.Lerp(0.032f, 0.21f, phase);
                        rObj.transform.localScale = new Vector3(scale, scale, 1f);
                        SetMaterialAlphaColor(rMat, new Color(0.88f, 0.95f, 1.0f, alpha), new Color(0.14f, 0.20f, 0.24f) * alpha);
                    }
                    else
                    {
                        rObj.SetActive(false);
                    }
                }
                else
                {
                    rObj.SetActive(false);
                }
            }

            // Splash droplets particle burst
            if (splashParticles != null)
            {
                splashParticles.transform.position = impactPos + Vector3.up * 0.004f;
                var emission = splashParticles.emission;
                emission.enabled = hittingBasin;
                if (hittingBasin && !splashParticles.isPlaying)
                {
                    splashParticles.Play();
                }
            }
        }

        private void ApplyHandleRotation()
        {
            if (coldHandleTransform == null)
                return;

            Vector3 rot = handleClosedEuler;
            rot.y += HandleTurnAngle * handleOpen01;
            coldHandleTransform.localEulerAngles = rot;
        }

        private Vector3 GetNozzleWorldPosition()
        {
            if (nozzleOrigin != null)
                return nozzleOrigin.position;

            return new Vector3(-4.02f, 0.976f, 5.786f);
        }

        private Vector3 GetBasinImpactWorldPosition(Vector3 nozzleWorld)
        {
            return new Vector3(nozzleWorld.x, basinFloorY, nozzleWorld.z + streamForwardZOffset);
        }

        private void HideAllEffectsImmediate()
        {
            if (streamObject != null) streamObject.SetActive(false);
            if (puddleObject != null) puddleObject.SetActive(false);
            for (int i = 0; i < rippleObjects.Length; i++)
            {
                if (rippleObjects[i] != null) rippleObjects[i].SetActive(false);
            }
            if (splashParticles != null)
            {
                var emission = splashParticles.emission;
                emission.enabled = false;
                splashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void EnsureVisualsBuilt()
        {
            if (visualsBuilt)
                return;

            visualsBuilt = true;
            BuildProceduralWaterTextures();

            // 1. Flowing stream column (origin at top y=0 down to y=-1 in local space)
            streamObject = new GameObject("Jato_Agua_Torneira");
            streamObject.transform.SetParent(transform, false);
            streamMesh = CreateStreamColumnMesh();
            MeshFilter mf = streamObject.AddComponent<MeshFilter>();
            mf.sharedMesh = streamMesh;
            streamRenderer = streamObject.AddComponent<MeshRenderer>();
            streamRenderer.shadowCastingMode = ShadowCastingMode.Off;
            streamRenderer.receiveShadows = false;
            streamMaterial = CreateWaterLitMaterial("M_Faucet_Stream", streamTexture, smoothness: 0.94f);
            streamRenderer.sharedMaterial = streamMaterial;

            // 2. Basin puddle disc
            puddleObject = CreateWaterOverlayQuad("Poca_Agua_Pia", puddleTexture, out puddleRenderer, out puddleMaterial);

            // 3. Expanding ripple rings
            for (int i = 0; i < rippleObjects.Length; i++)
            {
                rippleObjects[i] = CreateWaterOverlayQuad($"Onda_Agua_Pia_{i}", rippleTexture, out rippleRenderers[i], out rippleMaterials[i]);
            }

            // 4. Droplet splash particle system
            BuildSplashParticleSystem();
        }

        private GameObject CreateWaterOverlayQuad(string name, Texture2D tex, out MeshRenderer mr, out Material mat)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            q.transform.SetParent(transform, false);
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            if (q.TryGetComponent<Collider>(out Collider col))
            {
                Destroy(col);
            }

            mr = q.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mat = CreateWaterLitMaterial($"M_{name}", tex, smoothness: 0.90f);
            mr.sharedMaterial = mat;
            q.SetActive(false);
            return q;
        }

        private void BuildSplashParticleSystem()
        {
            GameObject psObj = new GameObject("Particulas_Respingos_Agua");
            psObj.transform.SetParent(transform, false);
            psObj.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            splashParticles = psObj.AddComponent<ParticleSystem>();
            splashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = splashParticles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 2.0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.30f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.011f);
            main.gravityModifier = 0.85f;
            main.maxParticles = 60;
            main.startColor = new Color(0.88f, 0.95f, 1.0f, 0.72f);

            var emission = splashParticles.emission;
            emission.enabled = false;
            emission.rateOverTime = 55f;

            var shape = splashParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 52f;
            shape.radius = 0.012f;

            var sizeOverLifetime = splashParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));

            ParticleSystemRenderer psRen = psObj.GetComponent<ParticleSystemRenderer>();
            psRen.renderMode = ParticleSystemRenderMode.Billboard;
            psRen.shadowCastingMode = ShadowCastingMode.Off;
            psRen.receiveShadows = false;

            Shader particleShader = Shader.Find("Sprites/Default")
                                 ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (particleShader != null)
            {
                particleMaterial = new Material(particleShader) { name = "M_Faucet_SplashDroplet" };
                ConfigureTransparentBlend(particleMaterial, dropletTexture, new Color(0.88f, 0.95f, 1f, 0.78f));
                psRen.sharedMaterial = particleMaterial;
            }
        }

        private static Material CreateWaterLitMaterial(string matName, Texture2D tex, float smoothness)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Sprites/Default");
            if (sh == null)
                return null;

            Material mat = new Material(sh) { name = matName };
            ConfigureTransparentBlend(mat, tex, new Color(0.90f, 0.96f, 1.0f, 0.78f));
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.05f);
            return mat;
        }

        private static void ConfigureTransparentBlend(Material mat, Texture2D tex, Color tint)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;

            mat.mainTexture = tex;
            if (mat.HasProperty(BaseMapProp)) mat.SetTexture(BaseMapProp, tex);
            mat.color = tint;
            if (mat.HasProperty(BaseColorProp)) mat.SetColor(BaseColorProp, tint);
            if (mat.HasProperty(ColorProp)) mat.SetColor(ColorProp, tint);
        }

        private static void SetMaterialAlphaColor(Material mat, Color baseTint, Color emissionTint)
        {
            mat.color = baseTint;
            if (mat.HasProperty(BaseColorProp)) mat.SetColor(BaseColorProp, baseTint);
            if (mat.HasProperty(ColorProp)) mat.SetColor(ColorProp, baseTint);
            if (mat.HasProperty(EmissionColorProp)) mat.SetColor(EmissionColorProp, emissionTint);
        }

        /// <summary>
        /// Tapered vertical cylinder from y=0 (top, uv.y=1) to y=-1 (bottom, uv.y=0).
        /// </summary>
        private static Mesh CreateStreamColumnMesh()
        {
            const int radial = 12;
            const int rings = 6;
            const float topRadius = 0.0085f;
            const float bottomRadius = 0.0110f;

            int vertCount = (rings + 1) * (radial + 1);
            Vector3[] verts = new Vector3[vertCount];
            Vector3[] norms = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] tris = new int[rings * radial * 6];

            int vi = 0;
            for (int r = 0; r <= rings; r++)
            {
                float vFrac = r / (float)rings; // 0 at top, 1 at bottom
                float y = -vFrac;
                float radius = Mathf.Lerp(topRadius, bottomRadius, vFrac);
                float uvV = 1f - vFrac;

                for (int s = 0; s <= radial; s++)
                {
                    float uFrac = s / (float)radial;
                    float ang = uFrac * Mathf.PI * 2f;
                    float cx = Mathf.Cos(ang);
                    float cz = Mathf.Sin(ang);

                    verts[vi] = new Vector3(cx * radius, y, cz * radius);
                    norms[vi] = new Vector3(cx, 0f, cz);
                    uvs[vi] = new Vector2(uFrac * 2f, uvV);
                    vi++;
                }
            }

            int ti = 0;
            int stride = radial + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < radial; s++)
                {
                    int i0 = r * stride + s;
                    int i1 = i0 + 1;
                    int i2 = (r + 1) * stride + s;
                    int i3 = i2 + 1;

                    tris[ti++] = i0;
                    tris[ti++] = i2;
                    tris[ti++] = i1;

                    tris[ti++] = i1;
                    tris[ti++] = i2;
                    tris[ti++] = i3;
                }
            }

            Mesh m = new Mesh { name = "Mesh_Jato_Agua_Runtime" };
            m.vertices = verts;
            m.normals = norms;
            m.uv = uvs;
            m.triangles = tris;
            m.RecalculateBounds();
            return m;
        }

        private void BuildProceduralWaterTextures()
        {
            streamTexture = BuildStreamStreakTexture(64, 128);
            rippleTexture = BuildRippleRingTexture(128, 128);
            puddleTexture = BuildPuddleTexture(64, 64);
            dropletTexture = BuildDropletTexture(32, 32);
        }

        private static Texture2D BuildStreamStreakTexture(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Water_Stream_Streaks",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color32[] px = new Color32[w * h];
            var rng = new System.Random(4209);
            float[] colPhase = new float[w];
            float[] colWeight = new float[w];
            for (int x = 0; x < w; x++)
            {
                colPhase[x] = (float)rng.NextDouble() * Mathf.PI * 2f;
                colWeight[x] = 0.55f + 0.45f * (float)rng.NextDouble();
            }

            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)h;
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)w;
                    float streakA = 0.5f + 0.5f * Mathf.Sin(ny * Mathf.PI * 8f + colPhase[x]);
                    float streakB = 0.5f + 0.5f * Mathf.Sin(ny * Mathf.PI * 16f - colPhase[(x * 3) % w]);
                    float foamBand = Mathf.Pow(streakA * 0.65f + streakB * 0.35f, 1.4f) * colWeight[x];
                    float rib = 0.72f + 0.28f * Mathf.Sin(nx * Mathf.PI * 12f);

                    float lum = Mathf.Clamp01(0.68f + foamBand * 0.32f);
                    float alpha = Mathf.Clamp01((0.44f + foamBand * 0.50f) * rib);
                    byte v = (byte)Mathf.RoundToInt(lum * 255f);
                    byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                    px[y * w + x] = new Color32((byte)(v * 0.90f), (byte)(v * 0.97f), v, a);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        private static Texture2D BuildRippleRingTexture(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Water_Ripple_Ring",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float dy = (y / (float)(h - 1)) * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    float dx = (x / (float)(w - 1)) * 2f - 1f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.Exp(-Mathf.Pow((dist - 0.76f) / 0.085f, 2f));
                    float innerEcho = Mathf.Exp(-Mathf.Pow((dist - 0.52f) / 0.075f, 2f)) * 0.45f;
                    float borderFade = 1f - Mathf.SmoothStep(0.84f, 0.98f, dist);
                    float alpha = Mathf.Clamp01((ring + innerEcho) * borderFade);

                    byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                    px[y * w + x] = new Color32(232, 245, 252, a);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        private static Texture2D BuildPuddleTexture(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Water_Basin_Puddle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float dy = (y / (float)(h - 1)) * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    float dx = (x / (float)(w - 1)) * 2f - 1f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float pool = 1f - Mathf.SmoothStep(0.32f, 0.94f, dist);
                    float sheen = Mathf.Exp(-Mathf.Pow((dist - 0.18f) / 0.20f, 2f)) * 0.35f;
                    float alpha = Mathf.Clamp01(pool * 0.65f + sheen * 0.35f);
                    byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                    px[y * w + x] = new Color32(210, 234, 244, a);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        private static Texture2D BuildDropletTexture(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Water_Splash_Droplet",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float dy = (y / (float)(h - 1)) * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    float dx = (x / (float)(w - 1)) * 2f - 1f;
                    float d2 = dx * dx + dy * dy;
                    float alpha = Mathf.Exp(-d2 * 3.4f) * (1f - Mathf.SmoothStep(0.65f, 1.0f, Mathf.Sqrt(d2)));
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
                    px[y * w + x] = new Color32(236, 246, 255, a);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }
    }
}
