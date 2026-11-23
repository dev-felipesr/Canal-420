#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Canal420.Events;

namespace Canal420.EditorTools
{
    public static partial class Canal420GreyboxBuilder
    {
        private const string CharacterTexFolder = "Assets/Textures/Characters";
        private const string CharacterModelsFolder = "Assets/Models/Characters";
        private const string CharacterAnimsFolder = "Assets/Models/Characters/Animations";
        private const string CharacterAnimatorControllerPath = "Assets/Models/Characters/AC_Canal420_Humanoid.controller";

        private struct CharacterMaterials
        {
            public Material margaretFace;
            public Material childFace;
            public Material cianFace;
            public Material hairDark;
            public Material nightgownCloth;
            public Material childGownCloth;
        }

        private class SkinnedMeshAccumulator
        {
            public readonly List<Vector3> verts = new List<Vector3>(1600);
            public readonly List<Vector2> uvs = new List<Vector2>(1600);
            public readonly List<Color> colors = new List<Color>(1600);
            public readonly List<BoneWeight> weights = new List<BoneWeight>(1600);
            public readonly List<List<int>> submeshTris = new List<List<int>>();

            public SkinnedMeshAccumulator(int submeshCount)
            {
                for (int i = 0; i < submeshCount; i++)
                {
                    submeshTris.Add(new List<int>(1024));
                }
            }

            public int AddVertex(Vector3 pos, Vector2 uv, Color col, BoneWeight bw)
            {
                int idx = verts.Count;
                verts.Add(pos);
                uvs.Add(uv);
                colors.Add(col);
                weights.Add(NormalizeBoneWeight(bw));
                return idx;
            }

            public void AddTri(int submesh, int i0, int i1, int i2)
            {
                submeshTris[submesh].Add(i0);
                submeshTris[submesh].Add(i1);
                submeshTris[submesh].Add(i2);
            }
        }

        private static BoneWeight MakeBoneWeight(int b0, float w0, int b1 = 0, float w1 = 0f, int b2 = 0, float w2 = 0f)
        {
            return NormalizeBoneWeight(new BoneWeight
            {
                boneIndex0 = b0,
                weight0 = Mathf.Max(0f, w0),
                boneIndex1 = b1,
                weight1 = Mathf.Max(0f, w1),
                boneIndex2 = b2,
                weight2 = Mathf.Max(0f, w2),
                boneIndex3 = 0,
                weight3 = 0f
            });
        }

        private static BoneWeight NormalizeBoneWeight(BoneWeight bw)
        {
            float sum = bw.weight0 + bw.weight1 + bw.weight2 + bw.weight3;
            if (sum <= 1e-5f)
            {
                bw.boneIndex0 = 0;
                bw.weight0 = 1f;
                bw.weight1 = 0f;
                bw.weight2 = 0f;
                bw.weight3 = 0f;
                return bw;
            }
            float inv = 1f / sum;
            bw.weight0 *= inv;
            bw.weight1 *= inv;
            bw.weight2 *= inv;
            bw.weight3 *= inv;
            return bw;
        }

        private static void EnsureCharacterAssetFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Textures"))
                AssetDatabase.CreateFolder("Assets", "Textures");
            if (!AssetDatabase.IsValidFolder(CharacterTexFolder))
                AssetDatabase.CreateFolder("Assets/Textures", "Characters");
            if (!AssetDatabase.IsValidFolder("Assets/Models"))
                AssetDatabase.CreateFolder("Assets", "Models");
            if (!AssetDatabase.IsValidFolder(CharacterModelsFolder))
                AssetDatabase.CreateFolder("Assets/Models", "Characters");
            if (!AssetDatabase.IsValidFolder(CharacterAnimsFolder))
                AssetDatabase.CreateFolder(CharacterModelsFolder, "Animations");
        }

        private static int cachedCharacterMaterialsFrame = -1;
        private static CharacterMaterials cachedCharacterMaterials;

        private static CharacterMaterials EnsureCharacterMaterials(GreyboxPalette pal)
        {
            int curFrame = Time.frameCount;
            if (cachedCharacterMaterialsFrame == curFrame &&
                cachedCharacterMaterials.margaretFace != null &&
                cachedCharacterMaterials.childFace != null &&
                cachedCharacterMaterials.cianFace != null &&
                cachedCharacterMaterials.hairDark != null &&
                cachedCharacterMaterials.nightgownCloth != null &&
                cachedCharacterMaterials.childGownCloth != null)
            {
                return cachedCharacterMaterials;
            }

            EnsureCharacterAssetFolders();

            Shader litShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/LowPolyDirtyLit.shader");
            if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            Shader texShader = Shader.Find("Universal Render Pipeline/Lit");
            if (texShader == null) texShader = litShader;

            Texture2D margFaceTex = CreateOrLoadPS1FaceTexture("Tex_Char_Margaret_Face.png",
                skinBase: new Color(0.76f, 0.72f, 0.68f),
                socketDark: new Color(0.11f, 0.09f, 0.10f),
                lipTint: new Color(0.36f, 0.27f, 0.27f),
                cheekHollow: 0.24f,
                seed: 4211);

            Texture2D childFaceTex = CreateOrLoadPS1FaceTexture("Tex_Char_Child_Face.png",
                skinBase: new Color(0.74f, 0.71f, 0.68f),
                socketDark: new Color(0.13f, 0.11f, 0.12f),
                lipTint: new Color(0.42f, 0.34f, 0.34f),
                cheekHollow: 0.14f,
                seed: 4212);

            Texture2D cianFaceTex = CreateOrLoadPS1FaceTexture("Tex_Char_Cian_Face.png",
                skinBase: new Color(0.68f, 0.62f, 0.57f),
                socketDark: new Color(0.16f, 0.13f, 0.13f),
                lipTint: new Color(0.38f, 0.30f, 0.29f),
                cheekHollow: 0.22f,
                seed: 4213);

            Texture2D hairTex = CreateOrLoadSimpleStripeTexture("Tex_Char_Hair_Dark.png",
                baseCol: new Color(0.07f, 0.06f, 0.06f),
                streakCol: new Color(0.14f, 0.12f, 0.11f),
                seed: 4214);

            Texture2D gownTex = CreateOrLoadSimpleClothTexture("Tex_Char_Nightgown_PS1.png",
                baseCol: new Color(0.56f, 0.55f, 0.53f),
                foldCol: new Color(0.34f, 0.33f, 0.32f),
                seed: 4215);

            Texture2D childGownTex = CreateOrLoadSimpleClothTexture("Tex_Char_ChildGown_PS1.png",
                baseCol: new Color(0.52f, 0.50f, 0.52f),
                foldCol: new Color(0.33f, 0.31f, 0.33f),
                seed: 4216);

            cachedCharacterMaterials = new CharacterMaterials
            {
                margaretFace = CreateOrUpdateCharacterMaterial("M_Char_Margaret_Face", texShader, margFaceTex, Color.white, 0.12f),
                childFace = CreateOrUpdateCharacterMaterial("M_Char_Child_Face", texShader, childFaceTex, Color.white, 0.12f),
                cianFace = CreateOrUpdateCharacterMaterial("M_Char_Cian_Face", texShader, cianFaceTex, Color.white, 0.14f),
                hairDark = CreateOrUpdateCharacterMaterial("M_Char_Hair_Dark", texShader, hairTex, Color.white, 0.16f),
                nightgownCloth = CreateOrUpdateCharacterMaterial("M_Char_Nightgown_PS1", texShader, gownTex, Color.white, 0.08f),
                childGownCloth = CreateOrUpdateCharacterMaterial("M_Char_ChildGown_PS1", texShader, childGownTex, Color.white, 0.08f)
            };
            cachedCharacterMaterialsFrame = curFrame;
            return cachedCharacterMaterials;
        }

        private static Material CreateOrUpdateCharacterMaterial(string matName, Shader sh, Texture2D tex, Color tint, float smoothness)
        {
            string path = $"{MaterialsFolder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(sh) { name = matName };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != sh)
            {
                mat.shader = sh;
            }

            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// Simple low-detail 128x128 PS1-style anatomical face texture mapped onto the sculpted cranium UV
        /// (u=0.5 front center, v=0..1 chin to crown). Sunken shaded sockets, pale lips, zero cartoon shapes.
        /// </summary>
        private static Texture2D CreateOrLoadPS1FaceTexture(
            string fileName, Color skinBase, Color socketDark, Color lipTint, float cheekHollow, int seed)
        {
            string assetPath = $"{CharacterTexFolder}/{fileName}";
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[size * size];
            var rng = new System.Random(seed);

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)(size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);

                    float frontDist = Mathf.Abs(u - 0.5f);
                    float frontMask = 1f - SmoothStep01(0.18f, 0.34f, frontDist);

                    Color col = Color.Lerp(skinBase * 0.62f, skinBase, frontMask);

                    // Recessed dark eye sockets around u = 0.5 +- 0.068, v = 0.545
                    float dxL = (u - 0.432f) / 0.036f;
                    float dxR = (u - 0.568f) / 0.036f;
                    float dyEye = (v - 0.545f) / 0.048f;
                    float sockL = Mathf.Exp(-(dxL * dxL + dyEye * dyEye) * 1.45f);
                    float sockR = Mathf.Exp(-(dxR * dxR + dyEye * dyEye) * 1.45f);
                    float sockets = Mathf.Clamp01(sockL + sockR);

                    // Deep eyelid crease (zero glowing white dots or cartoon shapes)
                    float lidL = Mathf.Exp(-(((u - 0.432f) / 0.024f) * ((u - 0.432f) / 0.024f) + ((v - 0.540f) / 0.012f) * ((v - 0.540f) / 0.012f)));
                    float lidR = Mathf.Exp(-(((u - 0.568f) / 0.024f) * ((u - 0.568f) / 0.024f) + ((v - 0.540f) / 0.012f) * ((v - 0.540f) / 0.012f)));
                    float lids = Mathf.Clamp01(lidL + lidR);

                    // Subtle brow ridge overhang shadow above sockets
                    float browL = Mathf.Exp(-(((u - 0.432f) / 0.042f) * ((u - 0.432f) / 0.042f) + ((v - 0.605f) / 0.016f) * ((v - 0.605f) / 0.016f)));
                    float browR = Mathf.Exp(-(((u - 0.568f) / 0.042f) * ((u - 0.568f) / 0.042f) + ((v - 0.605f) / 0.016f) * ((v - 0.605f) / 0.016f)));
                    float brows = Mathf.Clamp01(browL + browR);

                    // Nasal bridge subtle shadow on sides
                    float noseSide = (Mathf.Exp(-(((u - 0.478f) / 0.014f) * ((u - 0.478f) / 0.014f)))
                                    + Mathf.Exp(-(((u - 0.522f) / 0.014f) * ((u - 0.522f) / 0.014f))))
                                    * Mathf.Exp(-(((v - 0.455f) / 0.065f) * ((v - 0.455f) / 0.065f)));

                    // Gaunt cheek hollows
                    float cheekL = Mathf.Exp(-(((u - 0.415f) / 0.042f) * ((u - 0.415f) / 0.042f) + ((v - 0.375f) / 0.068f) * ((v - 0.375f) / 0.068f)));
                    float cheekR = Mathf.Exp(-(((u - 0.585f) / 0.042f) * ((u - 0.585f) / 0.042f) + ((v - 0.375f) / 0.068f) * ((v - 0.375f) / 0.068f)));
                    float cheeks = Mathf.Clamp01(cheekL + cheekR) * cheekHollow;

                    // Pale somber mouth shadow at v = 0.285
                    float mx = (u - 0.50f) / 0.044f;
                    float my = (v - 0.285f) / 0.016f;
                    float mouth = Mathf.Exp(-(mx * mx + my * my) * 1.8f);

                    float scalpMask = SmoothStep01(0.76f, 0.88f, v);
                    float backHair = SmoothStep01(0.24f, 0.36f, frontDist);

                    col = Color.Lerp(col, socketDark, Mathf.Clamp01(sockets * 0.78f + lids * 0.58f + brows * 0.22f));
                    col = Color.Lerp(col, col * 0.74f, Mathf.Clamp01(noseSide * 0.32f + cheeks));
                    col = Color.Lerp(col, lipTint, mouth * 0.82f);
                    col = Color.Lerp(col, new Color(0.08f, 0.07f, 0.07f), Mathf.Clamp01(scalpMask * 0.90f + backHair * 0.78f));

                    float grain = ((float)rng.NextDouble() - 0.5f) * 0.055f;
                    col.r = Mathf.Clamp01(col.r + grain);
                    col.g = Mathf.Clamp01(col.g + grain);
                    col.b = Mathf.Clamp01(col.b + grain);
                    col.a = 1f;
                    px[y * size + x] = col;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Texture2D CreateOrLoadSimpleStripeTexture(string fileName, Color baseCol, Color streakCol, int seed)
        {
            string assetPath = $"{CharacterTexFolder}/{fileName}";
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[size * size];
            var rng = new System.Random(seed);

            float[] colWeights = new float[size];
            for (int x = 0; x < size; x++)
                colWeights[x] = (float)rng.NextDouble();

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)(size - 1);
                float tipDark = Mathf.Lerp(0.75f, 1.0f, v);
                for (int x = 0; x < size; x++)
                {
                    float w = colWeights[x] * 0.7f + (float)rng.NextDouble() * 0.3f;
                    Color c = Color.Lerp(baseCol, streakCol, w) * tipDark;
                    c.a = 1f;
                    px[y * size + x] = c;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Texture2D CreateOrLoadSimpleClothTexture(string fileName, Color baseCol, Color foldCol, int seed)
        {
            string assetPath = $"{CharacterTexFolder}/{fileName}";
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[size * size];
            var rng = new System.Random(seed);

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)(size - 1);
                float hemShade = Mathf.Lerp(0.74f, 1.0f, SmoothStep01(0f, 0.38f, v));
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float fold = 0.5f + 0.5f * Mathf.Sin(u * Mathf.PI * 10f + Mathf.Sin(v * 6f) * 0.35f);
                    float weave = ((x + y) % 2 == 0 ? 0.02f : -0.02f) + ((float)rng.NextDouble() - 0.5f) * 0.05f;
                    Color c = Color.Lerp(foldCol, baseCol, 0.35f + fold * 0.65f) * hemShade;
                    c.r = Mathf.Clamp01(c.r + weave);
                    c.g = Mathf.Clamp01(c.g + weave);
                    c.b = Mathf.Clamp01(c.b + weave);
                    c.a = 1f;
                    px[y * size + x] = c;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        // =========================================================================
        // REAL SKELETAL ANIMATION CLIPS + ANIMATOR CONTROLLER (Assets/Models/Characters/)
        // =========================================================================

        private static int lastControllerBuiltFrame = -1;
        private static AnimatorController cachedBuiltController;

        private static AnimatorController EnsureHumanoidAnimatorController()
        {
            if (cachedBuiltController != null && lastControllerBuiltFrame == Time.frameCount)
            {
                return cachedBuiltController;
            }

            EnsureCharacterAssetFolders();

            AnimationClip clipIdle = CreateOrUpdateHumanoidClip("Anim_Char_Idle", loop: true, duration: 2.4f, BuildIdleClipCurves);
            AnimationClip clipLurk = CreateOrUpdateHumanoidClip("Anim_Char_LurkPeek", loop: true, duration: 3.2f, BuildLurkPeekClipCurves);
            AnimationClip clipRun = CreateOrUpdateHumanoidClip("Anim_Char_RunRetreat", loop: true, duration: 0.44f, BuildRunRetreatClipCurves);
            AnimationClip clipFrenzy = CreateOrUpdateHumanoidClip("Anim_Char_FrenziedLunge", loop: true, duration: 0.40f, BuildFrenziedLungeClipCurves);
            AnimationClip clipBrokenNeck = CreateOrUpdateHumanoidClip("Anim_Char_BrokenNeckStill", loop: true, duration: 2.0f, BuildBrokenNeckStillClipCurves);
            AnimationClip clipAttackLunge = CreateOrUpdateHumanoidClip("Anim_Char_VisceralAttackLunge", loop: false, duration: 0.22f, BuildVisceralAttackLungeClipCurves);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CharacterAnimatorControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(CharacterAnimatorControllerPath);
            }
            else
            {
                // Update in-place without deleting asset so GUID stays intact across rebuilds and multiple character bindings
                while (controller.parameters.Length > 0)
                {
                    controller.RemoveParameter(0);
                }
                if (controller.layers.Length == 0)
                {
                    controller.AddLayer("Base Layer");
                }
                AnimatorStateMachine existingSm = controller.layers[0].stateMachine;
                AnimatorStateTransition[] oldTransitions = existingSm.anyStateTransitions;
                for (int i = 0; i < oldTransitions.Length; i++)
                {
                    if (oldTransitions[i] != null)
                    {
                        existingSm.RemoveAnyStateTransition(oldTransitions[i]);
                    }
                }
                ChildAnimatorState[] oldStates = existingSm.states;
                for (int i = 0; i < oldStates.Length; i++)
                {
                    existingSm.RemoveState(oldStates[i].state);
                }
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(CharacterAnimatorControllerPath);
                for (int i = 0; i < subAssets.Length; i++)
                {
                    if (subAssets[i] is AnimatorStateTransition orphanTr)
                    {
                        Object.DestroyImmediate(orphanTr, true);
                    }
                }
            }

            controller.AddParameter("AnimMode", AnimatorControllerParameterType.Int);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState stIdle = sm.AddState("Idle");
            stIdle.motion = clipIdle;
            sm.defaultState = stIdle;

            AnimatorState stLurk = sm.AddState("LurkPeek");
            stLurk.motion = clipLurk;

            AnimatorState stRun = sm.AddState("RunRetreat");
            stRun.motion = clipRun;

            AnimatorState stFrenzy = sm.AddState("FrenziedLunge");
            stFrenzy.motion = clipFrenzy;

            AnimatorState stBrokenNeck = sm.AddState("DeadStillBrokenNeck");
            stBrokenNeck.motion = clipBrokenNeck;

            AnimatorState stAttackLunge = sm.AddState("VisceralAttackLunge");
            stAttackLunge.motion = clipAttackLunge;

            AddAnyStateModeTransition(sm, stIdle, (int)SpasmAnimMode.Idle, 0.08f);
            AddAnyStateModeTransition(sm, stLurk, (int)SpasmAnimMode.LurkPeek, 0.10f);
            AddAnyStateModeTransition(sm, stRun, (int)SpasmAnimMode.TwitchDash, 0.05f);
            AddAnyStateModeTransition(sm, stFrenzy, (int)SpasmAnimMode.FrenziedLunge, 0.05f);
            AddAnyStateModeTransition(sm, stBrokenNeck, (int)SpasmAnimMode.DeadStillBrokenNeck, 0.06f);
            AddAnyStateModeTransition(sm, stAttackLunge, (int)SpasmAnimMode.VisceralImpactSnap, 0.04f);

            EditorUtility.SetDirty(controller);
            cachedBuiltController = controller;
            lastControllerBuiltFrame = Time.frameCount;
            return controller;
        }

        private static void AddAnyStateModeTransition(AnimatorStateMachine sm, AnimatorState targetState, int modeValue, float duration)
        {
            AnimatorStateTransition tr = sm.AddAnyStateTransition(targetState);
            tr.hasExitTime = false;
            tr.exitTime = 0f;
            tr.duration = duration;
            tr.canTransitionToSelf = false;
            tr.AddCondition(AnimatorConditionMode.Equals, modeValue, "AnimMode");
        }

        private static AnimationClip CreateOrUpdateHumanoidClip(
            string clipName,
            bool loop,
            float duration,
            System.Action<AnimationClip, float> populateCurves)
        {
            string assetPath = $"{CharacterAnimsFolder}/{clipName}.anim";
            AnimationClip clip = new AnimationClip { name = clipName, frameRate = 30f };
            populateCurves(clip, duration);
            clip.EnsureQuaternionContinuity();
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(clip, existing);
                Object.DestroyImmediate(clip);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(clip, assetPath);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void AddQuatTrack(AnimationClip clip, string relativePath, params (float t, Vector3 euler)[] keys)
        {
            var cx = new AnimationCurve();
            var cy = new AnimationCurve();
            var cz = new AnimationCurve();
            var cw = new AnimationCurve();

            Quaternion prev = Quaternion.identity;
            for (int i = 0; i < keys.Length; i++)
            {
                Quaternion q = Quaternion.Euler(keys[i].euler);
                if (i > 0 && Quaternion.Dot(prev, q) < 0f)
                {
                    q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                }
                prev = q;
                float t = keys[i].t;
                cx.AddKey(t, q.x);
                cy.AddKey(t, q.y);
                cz.AddKey(t, q.z);
                cw.AddKey(t, q.w);
            }

            clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.x", cx);
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.y", cy);
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.z", cz);
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.w", cw);
        }

        private static void AddScaleTrack(AnimationClip clip, string relativePath, params (float t, Vector3 scale)[] keys)
        {
            var cx = new AnimationCurve();
            var cy = new AnimationCurve();
            var cz = new AnimationCurve();
            for (int i = 0; i < keys.Length; i++)
            {
                cx.AddKey(keys[i].t, keys[i].scale.x);
                cy.AddKey(keys[i].t, keys[i].scale.y);
                cz.AddKey(keys[i].t, keys[i].scale.z);
            }
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalScale.x", cx);
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalScale.y", cy);
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalScale.z", cz);
        }

        private static void BuildIdleClipCurves(AnimationClip clip, float d)
        {
            float m = d * 0.5f;
            AddQuatTrack(clip, "Torso_Esguio", (0f, Vector3.zero), (m, new Vector3(1.0f, 0f, 0f)), (d, Vector3.zero));
            AddQuatTrack(clip, "Ombros_Curvados", (0f, Vector3.zero), (m, new Vector3(0.6f, 0f, 0f)), (d, Vector3.zero));
            AddQuatTrack(clip, "Pescoco_Inclinado", (0f, Vector3.zero), (m, new Vector3(0.5f, 0f, 0f)), (d, Vector3.zero));
            AddQuatTrack(clip, "Cabeca_Silhueta", (0f, new Vector3(2f, 0f, -2f)), (m, new Vector3(2.8f, 0f, -2.4f)), (d, new Vector3(2f, 0f, -2f)));
            AddQuatTrack(clip, "Braco_Esq", (0f, new Vector3(2f, 0f, -4f)), (d, new Vector3(2f, 0f, -4f)));
            AddQuatTrack(clip, "Braco_Esq/Antebraco_Esq", (0f, new Vector3(-8f, 0f, 0f)), (d, new Vector3(-8f, 0f, 0f)));
            AddQuatTrack(clip, "Braco_Dir", (0f, new Vector3(2f, 0f, 4f)), (d, new Vector3(2f, 0f, 4f)));
            AddQuatTrack(clip, "Braco_Dir/Antebraco_Dir", (0f, new Vector3(-8f, 0f, 0f)), (d, new Vector3(-8f, 0f, 0f)));
            AddQuatTrack(clip, "Perna_Esq", (0f, Vector3.zero), (d, Vector3.zero));
            AddQuatTrack(clip, "Perna_Esq/Canela_Esq", (0f, Vector3.zero), (d, Vector3.zero));
            AddQuatTrack(clip, "Perna_Dir", (0f, Vector3.zero), (d, Vector3.zero));
            AddQuatTrack(clip, "Perna_Dir/Canela_Dir", (0f, Vector3.zero), (d, Vector3.zero));
            AddScaleTrack(clip, "Cabeca_Silhueta/Boca_Escura", (0f, Vector3.one), (d, Vector3.one));
        }

        private static void BuildLurkPeekClipCurves(AnimationClip clip, float d)
        {
            float q1 = d * 0.25f, q2 = d * 0.50f, q3 = d * 0.75f;
            AddQuatTrack(clip, "Torso_Esguio",
                (0f, new Vector3(1.5f, 0f, -0.5f)), (q2, new Vector3(2.8f, 0f, 0.5f)), (d, new Vector3(1.5f, 0f, -0.5f)));
            AddQuatTrack(clip, "Ombros_Curvados",
                (0f, new Vector3(0.5f, 0f, -1f)), (q2, new Vector3(1.8f, 0f, -0.5f)), (d, new Vector3(0.5f, 0f, -1f)));
            AddQuatTrack(clip, "Pescoco_Inclinado",
                (0f, new Vector3(2.0f, 0f, -2.5f)), (q2, new Vector3(2.5f, 0f, -3.5f)), (d, new Vector3(2.0f, 0f, -2.5f)));
            AddQuatTrack(clip, "Cabeca_Silhueta",
                (0f, new Vector3(3.5f, 0f, -6.0f)), (q1, new Vector3(4.2f, 0.6f, -7.2f)), (q2, new Vector3(3.0f, -0.5f, -5.2f)), (q3, new Vector3(4.0f, 0.2f, -6.8f)), (d, new Vector3(3.5f, 0f, -6.0f)));
            AddQuatTrack(clip, "Braco_Esq", (0f, new Vector3(3f, 0f, -5f)), (q2, new Vector3(5f, 0f, -5.5f)), (d, new Vector3(3f, 0f, -5f)));
            AddQuatTrack(clip, "Braco_Esq/Antebraco_Esq", (0f, new Vector3(-14f, 0f, 0f)), (q2, new Vector3(-16f, 0f, 0f)), (d, new Vector3(-14f, 0f, 0f)));
            AddQuatTrack(clip, "Braco_Dir", (0f, new Vector3(3f, 0f, 5f)), (q2, new Vector3(5f, 0f, 5.5f)), (d, new Vector3(3f, 0f, 5f)));
            AddQuatTrack(clip, "Braco_Dir/Antebraco_Dir", (0f, new Vector3(-14f, 0f, 0f)), (q2, new Vector3(-16f, 0f, 0f)), (d, new Vector3(-14f, 0f, 0f)));
            AddQuatTrack(clip, "Perna_Esq", (0f, Vector3.zero), (d, Vector3.zero));
            AddQuatTrack(clip, "Perna_Esq/Canela_Esq", (0f, Vector3.zero), (d, Vector3.zero));
            AddQuatTrack(clip, "Perna_Dir", (0f, Vector3.zero), (d, Vector3.zero));
            AddQuatTrack(clip, "Perna_Dir/Canela_Dir", (0f, Vector3.zero), (d, Vector3.zero));
            AddQuatTrack(clip, "Cabeca_Silhueta/Cabelo_Caido_Esq", (0f, new Vector3(1f, 0f, -2f)), (q2, new Vector3(2.2f, 0f, -3.5f)), (d, new Vector3(1f, 0f, -2f)));
            AddScaleTrack(clip, "Cabeca_Silhueta/Boca_Escura", (0f, Vector3.one), (d, Vector3.one));
        }

        private static void BuildRunRetreatClipCurves(AnimationClip clip, float d)
        {
            float q1 = d * 0.25f, q2 = d * 0.50f, q3 = d * 0.75f;
            // Genuine brisk running stride: hips/torso lean forward, knees bend backward (+X), thighs & arms counter-swing
            AddQuatTrack(clip, "Bone_Hips",
                (0f, new Vector3(4f, -6f, -2f)), (q2, new Vector3(4f, 6f, 2f)), (d, new Vector3(4f, -6f, -2f)));
            AddQuatTrack(clip, "Torso_Esguio",
                (0f, new Vector3(13f, 6f, 2f)), (q1, new Vector3(15f, 0f, 0f)), (q2, new Vector3(13f, -6f, -2f)), (q3, new Vector3(15f, 0f, 0f)), (d, new Vector3(13f, 6f, 2f)));
            AddQuatTrack(clip, "Ombros_Curvados",
                (0f, new Vector3(5f, 4f, 0f)), (q2, new Vector3(5f, -4f, 0f)), (d, new Vector3(5f, 4f, 0f)));
            AddQuatTrack(clip, "Pescoco_Inclinado",
                (0f, new Vector3(6f, 0f, -3f)), (q2, new Vector3(8f, 0f, -3f)), (d, new Vector3(6f, 0f, -3f)));
            AddQuatTrack(clip, "Cabeca_Silhueta",
                (0f, new Vector3(4f, 0f, -4f)), (q1, new Vector3(7f, 0f, -3f)), (q2, new Vector3(4f, 0f, -4f)), (q3, new Vector3(7f, 0f, -3f)), (d, new Vector3(4f, 0f, -4f)));

            // Left leg forward (-X) at t=0, trailing (+X) at t=q2; knee bends (+X) when trailing/lifting
            AddQuatTrack(clip, "Perna_Esq",
                (0f, new Vector3(-36f, 0f, 0f)), (q1, new Vector3(4f, 0f, 0f)), (q2, new Vector3(34f, 0f, 0f)), (q3, new Vector3(-8f, 0f, 0f)), (d, new Vector3(-36f, 0f, 0f)));
            AddQuatTrack(clip, "Perna_Esq/Canela_Esq",
                (0f, new Vector3(14f, 0f, 0f)), (q1, new Vector3(12f, 0f, 0f)), (q2, new Vector3(56f, 0f, 0f)), (q3, new Vector3(62f, 0f, 0f)), (d, new Vector3(14f, 0f, 0f)));

            // Right leg opposite phase
            AddQuatTrack(clip, "Perna_Dir",
                (0f, new Vector3(34f, 0f, 0f)), (q1, new Vector3(-8f, 0f, 0f)), (q2, new Vector3(-36f, 0f, 0f)), (q3, new Vector3(4f, 0f, 0f)), (d, new Vector3(34f, 0f, 0f)));
            AddQuatTrack(clip, "Perna_Dir/Canela_Dir",
                (0f, new Vector3(56f, 0f, 0f)), (q1, new Vector3(62f, 0f, 0f)), (q2, new Vector3(14f, 0f, 0f)), (q3, new Vector3(12f, 0f, 0f)), (d, new Vector3(56f, 0f, 0f)));

            // Arms counter-swinging opposite legs with bent elbows (-X)
            AddQuatTrack(clip, "Braco_Esq",
                (0f, new Vector3(24f, 0f, -6f)), (q2, new Vector3(-42f, 0f, -6f)), (d, new Vector3(24f, 0f, -6f)));
            AddQuatTrack(clip, "Braco_Esq/Antebraco_Esq",
                (0f, new Vector3(-36f, 0f, 0f)), (q2, new Vector3(-68f, 0f, 0f)), (d, new Vector3(-36f, 0f, 0f)));

            AddQuatTrack(clip, "Braco_Dir",
                (0f, new Vector3(-42f, 0f, 6f)), (q2, new Vector3(24f, 0f, 6f)), (d, new Vector3(-42f, 0f, 6f)));
            AddQuatTrack(clip, "Braco_Dir/Antebraco_Dir",
                (0f, new Vector3(-68f, 0f, 0f)), (q2, new Vector3(-36f, 0f, 0f)), (d, new Vector3(-68f, 0f, 0f)));

            AddQuatTrack(clip, "Cabeca_Silhueta/Cabelo_Caido_Esq", (0f, new Vector3(-12f, 0f, 4f)), (q2, new Vector3(-16f, 0f, -4f)), (d, new Vector3(-12f, 0f, 4f)));
            AddScaleTrack(clip, "Cabeca_Silhueta/Boca_Escura", (0f, Vector3.one), (d, Vector3.one));
        }

        private static void BuildFrenziedLungeClipCurves(AnimationClip clip, float d)
        {
            float q1 = d * 0.25f, q2 = d * 0.50f, q3 = d * 0.75f;
            AddQuatTrack(clip, "Torso_Esguio",
                (0f, new Vector3(11f, 0f, -2f)), (q2, new Vector3(14f, 0f, 2f)), (d, new Vector3(11f, 0f, -2f)));
            AddQuatTrack(clip, "Ombros_Curvados",
                (0f, new Vector3(6f, 0f, -3f)), (q2, new Vector3(8f, 0f, 3f)), (d, new Vector3(6f, 0f, -3f)));
            AddQuatTrack(clip, "Pescoco_Inclinado",
                (0f, new Vector3(-4f, 0f, -5f)), (q2, new Vector3(-6f, 0f, 4f)), (d, new Vector3(-4f, 0f, -5f)));
            AddQuatTrack(clip, "Cabeca_Silhueta",
                (0f, new Vector3(-6f, -5f, -14f)), (q1, new Vector3(4f, 5f, 10f)), (q2, new Vector3(-8f, -4f, -12f)), (q3, new Vector3(2f, 6f, 8f)), (d, new Vector3(-6f, -5f, -14f)));
            AddQuatTrack(clip, "Braco_Esq", (0f, new Vector3(-70f, -12f, -10f)), (q2, new Vector3(-84f, -10f, -8f)), (d, new Vector3(-70f, -12f, -10f)));
            AddQuatTrack(clip, "Braco_Esq/Antebraco_Esq", (0f, new Vector3(-34f, 0f, 0f)), (q2, new Vector3(-22f, 0f, 0f)), (d, new Vector3(-34f, 0f, 0f)));
            AddQuatTrack(clip, "Braco_Dir", (0f, new Vector3(-82f, 12f, 10f)), (q2, new Vector3(-68f, 10f, 8f)), (d, new Vector3(-82f, 12f, 10f)));
            AddQuatTrack(clip, "Braco_Dir/Antebraco_Dir", (0f, new Vector3(-22f, 0f, 0f)), (q2, new Vector3(-36f, 0f, 0f)), (d, new Vector3(-22f, 0f, 0f)));
            AddScaleTrack(clip, "Cabeca_Silhueta/Boca_Escura",
                (0f, new Vector3(1.14f, 1.55f, 1f)), (q2, new Vector3(1.18f, 1.80f, 1f)), (d, new Vector3(1.14f, 1.55f, 1f)));
        }

        private static void BuildBrokenNeckStillClipCurves(AnimationClip clip, float d)
        {
            float q1 = d * 0.25f, q2 = d * 0.50f, q3 = d * 0.75f;
            AddQuatTrack(clip, "Torso_Esguio",
                (0f, new Vector3(4.5f, 0f, -2.5f)), (q2, new Vector3(5.2f, 0f, -2.8f)), (d, new Vector3(4.5f, 0f, -2.5f)));
            AddQuatTrack(clip, "Ombros_Curvados",
                (0f, new Vector3(4.5f, 0f, -4.5f)), (q2, new Vector3(5.0f, 0f, -5.0f)), (d, new Vector3(4.5f, 0f, -4.5f)));
            AddQuatTrack(clip, "Pescoco_Inclinado",
                (0f, new Vector3(5.5f, 0f, -6.5f)), (q2, new Vector3(6.0f, 0f, -7.0f)), (d, new Vector3(5.5f, 0f, -6.5f)));
            AddQuatTrack(clip, "Cabeca_Silhueta",
                (0f, new Vector3(9.0f, -4.0f, -22.0f)), (q1, new Vector3(9.4f, -4.2f, -22.8f)), (q2, new Vector3(8.8f, -3.8f, -21.6f)), (q3, new Vector3(9.3f, -4.1f, -22.6f)), (d, new Vector3(9.0f, -4.0f, -22.0f)));
            AddQuatTrack(clip, "Braco_Esq", (0f, new Vector3(-22f, -4f, -5f)), (q2, new Vector3(-24f, -4f, -5.5f)), (d, new Vector3(-22f, -4f, -5f)));
            AddQuatTrack(clip, "Braco_Esq/Antebraco_Esq", (0f, new Vector3(-26f, 0f, 0f)), (q2, new Vector3(-28f, 0f, 0f)), (d, new Vector3(-26f, 0f, 0f)));
            AddQuatTrack(clip, "Braco_Dir", (0f, new Vector3(-18f, 4f, 6f)), (q2, new Vector3(-20f, 4f, 6.5f)), (d, new Vector3(-18f, 4f, 6f)));
            AddQuatTrack(clip, "Braco_Dir/Antebraco_Dir", (0f, new Vector3(-22f, 0f, 0f)), (q2, new Vector3(-24f, 0f, 0f)), (d, new Vector3(-22f, 0f, 0f)));
            AddQuatTrack(clip, "Perna_Esq", (0f, new Vector3(-4f, 0f, 0f)), (d, new Vector3(-4f, 0f, 0f)));
            AddQuatTrack(clip, "Perna_Esq/Canela_Esq", (0f, new Vector3(6f, 0f, 0f)), (d, new Vector3(6f, 0f, 0f)));
            AddQuatTrack(clip, "Perna_Dir", (0f, new Vector3(5f, 0f, 0f)), (d, new Vector3(5f, 0f, 0f)));
            AddQuatTrack(clip, "Perna_Dir/Canela_Dir", (0f, new Vector3(4f, 0f, 0f)), (d, new Vector3(4f, 0f, 0f)));
            AddQuatTrack(clip, "Cabeca_Silhueta/Cabelo_Caido_Esq", (0f, new Vector3(4f, 0f, -10f)), (q2, new Vector3(4.5f, 0f, -11f)), (d, new Vector3(4f, 0f, -10f)));
            AddScaleTrack(clip, "Cabeca_Silhueta/Boca_Escura",
                (0f, new Vector3(1.04f, 1.24f, 1f)), (q2, new Vector3(1.05f, 1.30f, 1f)), (d, new Vector3(1.04f, 1.24f, 1f)));
        }

        private static void BuildVisceralAttackLungeClipCurves(AnimationClip clip, float d)
        {
            float tMid = d * 0.42f;
            // Fluid skeletal attack lunge: coil -> violent torso pitch + arms lunging outward with flexing elbows + head/jaw snap
            AddQuatTrack(clip, "Bone_Hips",
                (0f, new Vector3(4f, 0f, 0f)), (tMid, new Vector3(10f, 0f, 0f)), (d, new Vector3(12f, 0f, 0f)));
            AddQuatTrack(clip, "Torso_Esguio",
                (0f, new Vector3(5f, 0f, -2f)), (tMid, new Vector3(14f, 0f, 1f)), (d, new Vector3(16f, 0f, 2f)));
            AddQuatTrack(clip, "Ombros_Curvados",
                (0f, new Vector3(4f, 0f, -3f)), (tMid, new Vector3(9f, 0f, 1.5f)), (d, new Vector3(11f, 0f, 2.5f)));
            AddQuatTrack(clip, "Pescoco_Inclinado",
                (0f, new Vector3(6f, 0f, -6f)), (tMid, new Vector3(12f, 0f, -4f)), (d, new Vector3(14f, 0f, -3f)));
            AddQuatTrack(clip, "Cabeca_Silhueta",
                (0f, new Vector3(8f, -3f, -20f)), (tMid, new Vector3(-6f, 2f, -12f)), (d, new Vector3(-10f, 3f, -10f)));

            // Arms launch forward (-X) toward the player lens while elbows extend smoothly
            AddQuatTrack(clip, "Braco_Esq",
                (0f, new Vector3(-24f, -4f, -6f)), (tMid, new Vector3(-68f, -12f, -12f)), (d, new Vector3(-86f, -14f, -8f)));
            AddQuatTrack(clip, "Braco_Esq/Antebraco_Esq",
                (0f, new Vector3(-34f, 0f, 0f)), (tMid, new Vector3(-40f, 0f, 0f)), (d, new Vector3(-18f, 0f, 0f)));
            AddQuatTrack(clip, "Braco_Esq/Antebraco_Esq/Mao_Esq",
                (0f, Vector3.zero), (d, new Vector3(-18f, 0f, -8f)));

            AddQuatTrack(clip, "Braco_Dir",
                (0f, new Vector3(-20f, 4f, 6f)), (tMid, new Vector3(-72f, 12f, 12f)), (d, new Vector3(-90f, 14f, 8f)));
            AddQuatTrack(clip, "Braco_Dir/Antebraco_Dir",
                (0f, new Vector3(-30f, 0f, 0f)), (tMid, new Vector3(-38f, 0f, 0f)), (d, new Vector3(-16f, 0f, 0f)));
            AddQuatTrack(clip, "Braco_Dir/Antebraco_Dir/Mao_Dir",
                (0f, Vector3.zero), (d, new Vector3(-18f, 0f, 8f)));

            // Stride drive through legs/skirt hem
            AddQuatTrack(clip, "Perna_Esq", (0f, new Vector3(-6f, 0f, 0f)), (d, new Vector3(-28f, 0f, 0f)));
            AddQuatTrack(clip, "Perna_Esq/Canela_Esq", (0f, new Vector3(8f, 0f, 0f)), (d, new Vector3(24f, 0f, 0f)));
            AddQuatTrack(clip, "Perna_Dir", (0f, new Vector3(6f, 0f, 0f)), (d, new Vector3(26f, 0f, 0f)));
            AddQuatTrack(clip, "Perna_Dir/Canela_Dir", (0f, new Vector3(6f, 0f, 0f)), (d, new Vector3(36f, 0f, 0f)));

            // Hair trails dramatically behind head snap
            AddQuatTrack(clip, "Cabeca_Silhueta/Cabelo_Caido_Esq",
                (0f, new Vector3(4f, 0f, -8f)), (tMid, new Vector3(-16f, 0f, 6f)), (d, new Vector3(-24f, 0f, 10f)));

            AddScaleTrack(clip, "Cabeca_Silhueta/Boca_Escura",
                (0f, new Vector3(1.04f, 1.24f, 1f)), (tMid, new Vector3(1.16f, 1.32f, 1f)), (d, new Vector3(1.18f, 1.32f, 1f)));
        }

        // =========================================================================
        // WATERTIGHT SKINNED MESH APPENDERS (All vertices built in character root space)
        // =========================================================================

        private static void AppendSculptedLoftToSkinned(
            SkinnedMeshAccumulator acc,
            int submesh,
            int sides,
            (float y, float cx, float cz, float rx, float rz, float foldAmp, BoneWeight bw)[] rings)
        {
            int startBase = acc.verts.Count;
            float minY = rings[0].y;
            float maxY = rings[rings.Length - 1].y;
            float invH = 1f / Mathf.Max(0.001f, maxY - minY);
            int stride = sides + 1;

            for (int r = 0; r < rings.Length; r++)
            {
                var ring = rings[r];
                float v = (ring.y - minY) * invH;
                for (int s = 0; s <= sides; s++)
                {
                    float u = s / (float)sides;
                    float ang = (s == sides) ? 0f : u * Mathf.PI * 2f;
                    float fold = ring.foldAmp * (Mathf.Sin(ang * 5f + v * 1.4f) * 0.65f + Mathf.Cos(ang * 3f - v * 0.8f) * 0.35f);
                    float x = ring.cx + Mathf.Sin(ang) * (ring.rx + fold);
                    float z = ring.cz + Mathf.Cos(ang) * (ring.rz + fold * 0.85f);
                    float ao = Mathf.Lerp(0.74f, 1.0f, SmoothStep01(0f, 0.35f, v));
                    acc.AddVertex(new Vector3(x, ring.y, z), new Vector2(u * 2f, v), new Color(ao, ao, ao, 1f), ring.bw);
                }
            }

            // Side wall quads: outward winding
            for (int r = 0; r < rings.Length - 1; r++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int i0 = startBase + r * stride + s;
                    int i1 = i0 + 1;
                    int i2 = startBase + (r + 1) * stride + s;
                    int i3 = i2 + 1;
                    acc.AddTri(submesh, i0, i1, i2);
                    acc.AddTri(submesh, i1, i3, i2);
                }
            }

            // Split bottom cap (-Y normal)
            int botCapStart = acc.verts.Count;
            for (int s = 0; s <= sides; s++)
            {
                int src = startBase + s;
                acc.AddVertex(acc.verts[src], new Vector2(0.5f, 0f), new Color(0.58f, 0.58f, 0.58f, 1f), rings[0].bw);
            }
            int botCenter = acc.AddVertex(
                new Vector3(rings[0].cx, rings[0].y, rings[0].cz),
                new Vector2(0.5f, 0f),
                new Color(0.55f, 0.55f, 0.55f, 1f),
                rings[0].bw);
            for (int s = 0; s < sides; s++)
            {
                acc.AddTri(submesh, botCenter, botCapStart + s + 1, botCapStart + s);
            }

            // Split top cap (+Y normal)
            int topRingSrc = startBase + (rings.Length - 1) * stride;
            int topCapStart = acc.verts.Count;
            for (int s = 0; s <= sides; s++)
            {
                int src = topRingSrc + s;
                acc.AddVertex(acc.verts[src], new Vector2(0.5f, 1f), Color.white, rings[rings.Length - 1].bw);
            }
            int topCenter = acc.AddVertex(
                new Vector3(rings[rings.Length - 1].cx, rings[rings.Length - 1].y, rings[rings.Length - 1].cz),
                new Vector2(0.5f, 1f),
                Color.white,
                rings[rings.Length - 1].bw);
            for (int s = 0; s < sides; s++)
            {
                acc.AddTri(submesh, topCenter, topCapStart + s, topCapStart + s + 1);
            }
        }

        private static void AppendSculptedFootOrShoeToSkinned(
            SkinnedMeshAccumulator acc,
            int submesh,
            Vector3 heelCenter,
            float halfW,
            float height,
            float length,
            float yawDeg,
            BoneWeight bw)
        {
            const int sides = 8;
            Quaternion yawRot = Quaternion.Euler(0f, yawDeg, 0f);
            (float y, float zOff, float rx, float rz)[] profile =
            {
                (0.000f,          0.018f * (length / 0.16f), halfW * 0.94f, length * 0.48f),
                (height * 0.35f,  0.014f * (length / 0.16f), halfW * 1.00f, length * 0.50f),
                (height * 0.78f,  0.002f,                    halfW * 0.88f, length * 0.42f),
                (height,         -0.012f * (length / 0.16f), halfW * 0.68f, length * 0.28f)
            };

            int startBase = acc.verts.Count;
            int stride = sides + 1;
            for (int r = 0; r < profile.Length; r++)
            {
                var p = profile[r];
                float v = r / (float)(profile.Length - 1);
                for (int s = 0; s <= sides; s++)
                {
                    float u = s / (float)sides;
                    float ang = (s == sides) ? 0f : u * Mathf.PI * 2f;
                    Vector3 local = new Vector3(Mathf.Sin(ang) * p.rx, p.y, Mathf.Cos(ang) * p.rz + p.zOff);
                    Vector3 pos = heelCenter + yawRot * local;
                    float ao = Mathf.Lerp(0.72f, 0.96f, v);
                    acc.AddVertex(pos, new Vector2(u, v), new Color(ao, ao, ao, 1f), bw);
                }
            }

            for (int r = 0; r < profile.Length - 1; r++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int i0 = startBase + r * stride + s;
                    int i1 = i0 + 1;
                    int i2 = startBase + (r + 1) * stride + s;
                    int i3 = i2 + 1;
                    acc.AddTri(submesh, i0, i1, i2);
                    acc.AddTri(submesh, i1, i3, i2);
                }
            }

            // Bottom cap
            int botStart = acc.verts.Count;
            for (int s = 0; s <= sides; s++)
                acc.AddVertex(acc.verts[startBase + s], new Vector2(0.5f, 0f), new Color(0.55f, 0.55f, 0.55f, 1f), bw);
            int botCenter = acc.AddVertex(heelCenter + yawRot * new Vector3(0f, profile[0].y, profile[0].zOff), new Vector2(0.5f, 0f), new Color(0.55f, 0.55f, 0.55f, 1f), bw);
            for (int s = 0; s < sides; s++)
                acc.AddTri(submesh, botCenter, botStart + s + 1, botStart + s);

            // Top cap
            int topSrc = startBase + (profile.Length - 1) * stride;
            int topStart = acc.verts.Count;
            for (int s = 0; s <= sides; s++)
                acc.AddVertex(acc.verts[topSrc + s], new Vector2(0.5f, 1f), Color.white, bw);
            int topCenter = acc.AddVertex(heelCenter + yawRot * new Vector3(0f, profile[profile.Length - 1].y, profile[profile.Length - 1].zOff), new Vector2(0.5f, 1f), Color.white, bw);
            for (int s = 0; s < sides; s++)
                acc.AddTri(submesh, topCenter, topStart + s, topStart + s + 1);
        }

        private static void AppendSculptedHeadToSkinned(
            SkinnedMeshAccumulator acc,
            int submesh,
            Vector3 headCenter,
            float rx,
            float ry,
            float rz,
            int headBoneIdx,
            int jawBoneIdx)
        {
            const int rings = 12;
            const int segs = 16;

            int startBase = acc.verts.Count;
            for (int r = 1; r < rings; r++)
            {
                float v = r / (float)rings;
                float phi = (v - 0.5f) * Mathf.PI;
                float cosPhi = Mathf.Cos(phi);
                float sinPhi = Mathf.Sin(phi);

                for (int s = 0; s <= segs; s++)
                {
                    float u = s / (float)segs;
                    float theta = (s == segs) ? -Mathf.PI : (u - 0.5f) * Mathf.PI * 2f;
                    float sinT = Mathf.Sin(theta);
                    float cosT = Mathf.Cos(theta);

                    float jawTaperX = v < 0.45f ? Mathf.Lerp(0.64f, 1.0f, v / 0.45f) : Mathf.Lerp(1.0f, 0.96f, (v - 0.45f) / 0.55f);
                    float backSkullZ = cosT < 0f && v > 0.35f ? 1.10f : 1.0f;

                    float x = sinT * cosPhi * rx * jawTaperX;
                    float y = sinPhi * ry;
                    float z = cosT * cosPhi * rz * backSkullZ;

                    float jawWeight = 0f;
                    if (cosT > 0.2f)
                    {
                        float frontWeight = SmoothStep01(0.2f, 0.7f, cosT);

                        float brow = Mathf.Exp(-((v - 0.62f) * (v - 0.62f)) / 0.0045f) * frontWeight;
                        z += brow * rz * 0.10f;

                        float eyeL = Mathf.Exp(-(((u - 0.432f) / 0.042f) * ((u - 0.432f) / 0.042f) + ((v - 0.545f) / 0.055f) * ((v - 0.545f) / 0.055f)));
                        float eyeR = Mathf.Exp(-(((u - 0.568f) / 0.042f) * ((u - 0.568f) / 0.042f) + ((v - 0.545f) / 0.055f) * ((v - 0.545f) / 0.055f)));
                        z -= Mathf.Clamp01(eyeL + eyeR) * rz * 0.14f;

                        float nose = Mathf.Exp(-(((u - 0.50f) / 0.026f) * ((u - 0.50f) / 0.026f) + ((v - 0.44f) / 0.068f) * ((v - 0.44f) / 0.068f)));
                        z += nose * rz * 0.18f;

                        float cheek = (Mathf.Exp(-(((u - 0.40f) / 0.045f) * ((u - 0.40f) / 0.045f)))
                                     + Mathf.Exp(-(((u - 0.60f) / 0.045f) * ((u - 0.60f) / 0.045f))))
                                     * Mathf.Exp(-(((v - 0.44f) / 0.055f) * ((v - 0.44f) / 0.055f)));
                        x *= (1f + cheek * 0.07f);
                        z += cheek * rz * 0.05f;

                        // Subtle mouth cavity recess + jaw bone weight for attack lunge mouth opening
                        float mouthRecess = Mathf.Exp(-(((u - 0.50f) / 0.048f) * ((u - 0.50f) / 0.048f) + ((v - 0.285f) / 0.032f) * ((v - 0.285f) / 0.032f)));
                        z -= mouthRecess * rz * 0.07f;

                        float chin = Mathf.Exp(-(((u - 0.50f) / 0.06f) * ((u - 0.50f) / 0.06f) + ((v - 0.15f) / 0.065f) * ((v - 0.15f) / 0.065f)));
                        z += chin * rz * 0.09f;

                        if (cosT > 0.55f && v < 0.35f)
                        {
                            jawWeight = Mathf.Clamp01((mouthRecess * 0.55f + chin * 0.35f) * frontWeight) * 0.55f;
                        }
                    }

                    BoneWeight bw = jawWeight > 0.01f
                        ? MakeBoneWeight(headBoneIdx, 1f - jawWeight, jawBoneIdx, jawWeight)
                        : MakeBoneWeight(headBoneIdx, 1f);

                    float ao = Mathf.Lerp(0.76f, 1.0f, SmoothStep01(0f, 0.35f, v));
                    acc.AddVertex(headCenter + new Vector3(x, y, z), new Vector2(u, v), new Color(ao, ao, ao, 1f), bw);
                }
            }

            int stride = segs + 1;
            int innerRingCount = rings - 1;
            for (int r = 0; r < innerRingCount - 1; r++)
            {
                for (int s = 0; s < segs; s++)
                {
                    int i0 = startBase + r * stride + s;
                    int i1 = i0 + 1;
                    int i2 = startBase + (r + 1) * stride + s;
                    int i3 = i2 + 1;
                    acc.AddTri(submesh, i0, i1, i2);
                    acc.AddTri(submesh, i1, i3, i2);
                }
            }

            BoneWeight headOnly = MakeBoneWeight(headBoneIdx, 1f);
            int botPole = acc.AddVertex(headCenter + new Vector3(0f, -ry, 0f), new Vector2(0.5f, 0f), new Color(0.74f, 0.74f, 0.74f, 1f), headOnly);
            for (int s = 0; s < segs; s++)
                acc.AddTri(submesh, botPole, startBase + s + 1, startBase + s);

            int topRingStart = startBase + (innerRingCount - 1) * stride;
            int topPole = acc.AddVertex(headCenter + new Vector3(0f, ry, 0f), new Vector2(0.5f, 1f), Color.white, headOnly);
            for (int s = 0; s < segs; s++)
                acc.AddTri(submesh, topPole, topRingStart + s, topRingStart + s + 1);
        }

        private static void AppendSculptedHairCurtainToSkinned(
            SkinnedMeshAccumulator acc,
            int submesh,
            Vector3 headCenter,
            float headRx,
            float headRy,
            float headRz,
            float dropLength,
            int rings,
            int segs,
            int headBoneIdx,
            int hairSwayBoneIdx)
        {
            const float shellThick = 0.011f;

            Vector3[,] outerPos = new Vector3[rings + 1, segs + 1];
            Vector3[,] innerPos = new Vector3[rings + 1, segs + 1];
            Vector2[,] gridUv = new Vector2[rings + 1, segs + 1];
            Color[,] gridCol = new Color[rings + 1, segs + 1];
            BoneWeight[,] gridBw = new BoneWeight[rings + 1, segs + 1];

            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                float yOut = t < 0.35f
                    ? Mathf.Lerp(headRy * 1.09f, headRy * 0.20f, t / 0.35f)
                    : Mathf.Lerp(headRy * 0.20f, -dropLength, (t - 0.35f) / 0.65f);
                float yIn = yOut - (t < 0.25f ? Mathf.Lerp(0.009f, 0.002f, t / 0.25f) : 0f);

                float radScale = t < 0.35f
                    ? Mathf.Lerp(0.24f, 1.15f, Mathf.Sin((t / 0.35f) * Mathf.PI * 0.5f))
                    : Mathf.Lerp(1.15f, 1.44f, (t - 0.35f) / 0.65f);

                float halfGapDeg = t < 0.20f ? Mathf.Lerp(10f, 24f, t / 0.20f) : Mathf.Lerp(24f, 44f, (t - 0.20f) / 0.80f);
                float startAng = halfGapDeg * Mathf.Deg2Rad;
                float endAng = (360f - halfGapDeg) * Mathf.Deg2Rad;

                float drapeBlend = SmoothStep01(0.32f, 0.92f, t);
                BoneWeight rowBw = hairSwayBoneIdx != headBoneIdx && drapeBlend > 0.01f
                    ? MakeBoneWeight(headBoneIdx, 1f - drapeBlend, hairSwayBoneIdx, drapeBlend)
                    : MakeBoneWeight(headBoneIdx, 1f);

                for (int s = 0; s <= segs; s++)
                {
                    float su = s / (float)segs;
                    float ang = Mathf.Lerp(startAng, endAng, su);
                    float sinA = Mathf.Sin(ang);
                    float cosA = Mathf.Cos(ang);
                    float wave = (t > 0.35f) ? Mathf.Sin(su * Mathf.PI * 6f + t * 2.5f) * 0.007f : 0f;
                    float zBackFactor = cosA < 0f ? 1.14f : 1.04f;
                    float drapeBackZ = t > 0.45f ? -((t - 0.45f) / 0.55f) * 0.032f : 0f;

                    float rxOut = headRx * radScale + wave;
                    float rzOut = headRz * radScale * zBackFactor + wave;
                    float rxIn = Mathf.Max(0.008f, rxOut - shellThick);
                    float rzIn = Mathf.Max(0.008f, rzOut - shellThick);

                    outerPos[r, s] = headCenter + new Vector3(sinA * rxOut, yOut, cosA * rzOut + drapeBackZ);
                    innerPos[r, s] = headCenter + new Vector3(sinA * rxIn, yIn, cosA * rzIn + drapeBackZ);
                    gridUv[r, s] = new Vector2(su * 2f, 1f - t);
                    float ao = Mathf.Lerp(0.95f, 0.68f, t);
                    gridCol[r, s] = new Color(ao, ao, ao, 1f);
                    gridBw[r, s] = rowBw;
                }
            }

            int stride = segs + 1;
            int outBase = acc.verts.Count;
            for (int r = 0; r <= rings; r++)
                for (int s = 0; s <= segs; s++)
                    acc.AddVertex(outerPos[r, s], gridUv[r, s], gridCol[r, s], gridBw[r, s]);
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segs; s++)
                {
                    int o00 = outBase + r * stride + s;
                    int o01 = o00 + 1;
                    int o10 = outBase + (r + 1) * stride + s;
                    int o11 = o10 + 1;
                    acc.AddTri(submesh, o00, o10, o01);
                    acc.AddTri(submesh, o01, o10, o11);
                }

            int inBase = acc.verts.Count;
            for (int r = 0; r <= rings; r++)
                for (int s = 0; s <= segs; s++)
                    acc.AddVertex(innerPos[r, s], gridUv[r, s], gridCol[r, s] * 0.78f, gridBw[r, s]);
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segs; s++)
                {
                    int i00 = inBase + r * stride + s;
                    int i01 = i00 + 1;
                    int i10 = inBase + (r + 1) * stride + s;
                    int i11 = i10 + 1;
                    acc.AddTri(submesh, i00, i01, i10);
                    acc.AddTri(submesh, i01, i11, i10);
                }

            void AddRimQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uv, Color col, BoneWeight bwA, BoneWeight bwB, BoneWeight bwC, BoneWeight bwD)
            {
                int idx = acc.verts.Count;
                acc.AddVertex(a, uv, col, bwA);
                acc.AddVertex(b, uv, col, bwB);
                acc.AddVertex(c, uv, col, bwC);
                acc.AddVertex(d, uv, col, bwD);
                acc.AddTri(submesh, idx, idx + 1, idx + 2);
                acc.AddTri(submesh, idx, idx + 2, idx + 3);
            }

            for (int s = 0; s < segs; s++)
                AddRimQuad(outerPos[0, s], outerPos[0, s + 1], innerPos[0, s + 1], innerPos[0, s], gridUv[0, s], gridCol[0, s], gridBw[0, s], gridBw[0, s + 1], gridBw[0, s + 1], gridBw[0, s]);

            for (int s = 0; s < segs; s++)
                AddRimQuad(outerPos[rings, s + 1], outerPos[rings, s], innerPos[rings, s], innerPos[rings, s + 1], gridUv[rings, s], gridCol[rings, s], gridBw[rings, s + 1], gridBw[rings, s], gridBw[rings, s], gridBw[rings, s + 1]);

            for (int r = 0; r < rings; r++)
                AddRimQuad(outerPos[r + 1, 0], outerPos[r, 0], innerPos[r, 0], innerPos[r + 1, 0], gridUv[r, 0], gridCol[r, 0], gridBw[r + 1, 0], gridBw[r, 0], gridBw[r, 0], gridBw[r + 1, 0]);

            for (int r = 0; r < rings; r++)
                AddRimQuad(outerPos[r, segs], outerPos[r + 1, segs], innerPos[r + 1, segs], innerPos[r, segs], gridUv[r, segs], gridCol[r, segs], gridBw[r, segs], gridBw[r + 1, segs], gridBw[r + 1, segs], gridBw[r, segs]);
        }

        private static void AppendClosedBoxToSkinned(
            SkinnedMeshAccumulator acc,
            int submesh,
            Vector3 center,
            Vector3 size,
            Vector3 eulerDeg,
            BoneWeight bw)
        {
            Quaternion rot = Quaternion.Euler(eulerDeg);
            Vector3 h = size * 0.5f;
            Vector3[] c =
            {
                new Vector3(-h.x, -h.y, -h.z), new Vector3( h.x, -h.y, -h.z),
                new Vector3( h.x,  h.y, -h.z), new Vector3(-h.x,  h.y, -h.z),
                new Vector3(-h.x, -h.y,  h.z), new Vector3( h.x, -h.y,  h.z),
                new Vector3( h.x,  h.y,  h.z), new Vector3(-h.x,  h.y,  h.z)
            };
            for (int i = 0; i < 8; i++) c[i] = center + rot * c[i];

            int[,] faces =
            {
                { 0, 3, 2, 1 }, // -Z
                { 5, 6, 7, 4 }, // +Z
                { 4, 7, 3, 0 }, // -X
                { 1, 2, 6, 5 }, // +X
                { 3, 7, 6, 2 }, // +Y
                { 4, 0, 1, 5 }  // -Y
            };

            for (int f = 0; f < 6; f++)
            {
                int b = acc.verts.Count;
                acc.AddVertex(c[faces[f, 0]], new Vector2(0f, 0f), Color.white, bw);
                acc.AddVertex(c[faces[f, 1]], new Vector2(0f, 1f), Color.white, bw);
                acc.AddVertex(c[faces[f, 2]], new Vector2(1f, 1f), Color.white, bw);
                acc.AddVertex(c[faces[f, 3]], new Vector2(1f, 0f), Color.white, bw);
                acc.AddTri(submesh, b, b + 1, b + 2);
                acc.AddTri(submesh, b, b + 2, b + 3);
            }
        }

        private static Mesh FinalizeAndSaveSkinnedMesh(
            SkinnedMeshAccumulator acc,
            string assetName,
            Matrix4x4[] bindposes,
            float yGroundShift)
        {
            if (Mathf.Abs(yGroundShift) > 1e-6f)
            {
                for (int i = 0; i < acc.verts.Count; i++)
                {
                    Vector3 v = acc.verts[i];
                    v.y += yGroundShift;
                    acc.verts[i] = v;
                }
            }

            Mesh m = new Mesh { name = assetName };
            m.subMeshCount = acc.submeshTris.Count;
            m.SetVertices(acc.verts);
            m.SetUVs(0, acc.uvs);
            m.SetColors(acc.colors);
            m.boneWeights = acc.weights.ToArray();
            for (int sm = 0; sm < acc.submeshTris.Count; sm++)
            {
                m.SetTriangles(acc.submeshTris[sm], sm);
            }
            m.RecalculateNormals();

            // Guarantee zero flipped normals across all submeshes (AuditCustomMeshesWatertightness invariant)
            Vector3[] vArr = m.vertices;
            Vector3[] nArr = m.normals;
            bool anyFlipped = false;
            for (int sm = 0; sm < acc.submeshTris.Count && !anyFlipped; sm++)
            {
                List<int> tris = acc.submeshTris[sm];
                for (int t = 0; t < tris.Count; t += 3)
                {
                    int r0 = tris[t], r1 = tris[t + 1], r2 = tris[t + 2];
                    Vector3 cross = Vector3.Cross((vArr[r1] - vArr[r0]) * 1000f, (vArr[r2] - vArr[r0]) * 1000f);
                    if (cross.sqrMagnitude <= 1e-12f) continue;
                    Vector3 fn = cross.normalized;
                    if (Vector3.Dot(fn, nArr[r0]) <= 0.01f || Vector3.Dot(fn, nArr[r1]) <= 0.01f || Vector3.Dot(fn, nArr[r2]) <= 0.01f)
                    {
                        anyFlipped = true;
                        break;
                    }
                }
            }

            if (anyFlipped)
            {
                var flatVerts = new List<Vector3>();
                var flatNorms = new List<Vector3>();
                var flatUvs = new List<Vector2>();
                var flatCols = new List<Color>();
                var flatWeights = new List<BoneWeight>();
                var flatSubTris = new List<List<int>>();

                for (int sm = 0; sm < acc.submeshTris.Count; sm++)
                {
                    List<int> src = acc.submeshTris[sm];
                    var dst = new List<int>(src.Count);
                    for (int t = 0; t < src.Count; t += 3)
                    {
                        int r0 = src[t], r1 = src[t + 1], r2 = src[t + 2];
                        Vector3 p0 = vArr[r0], p1 = vArr[r1], p2 = vArr[r2];
                        Vector3 cross = Vector3.Cross((p1 - p0) * 1000f, (p2 - p0) * 1000f);
                        Vector3 fn = cross.sqrMagnitude > 1e-12f ? cross.normalized : Vector3.up;
                        int b = flatVerts.Count;
                        flatVerts.Add(p0); flatVerts.Add(p1); flatVerts.Add(p2);
                        flatNorms.Add(fn); flatNorms.Add(fn); flatNorms.Add(fn);
                        flatUvs.Add(acc.uvs[r0]); flatUvs.Add(acc.uvs[r1]); flatUvs.Add(acc.uvs[r2]);
                        flatCols.Add(acc.colors[r0]); flatCols.Add(acc.colors[r1]); flatCols.Add(acc.colors[r2]);
                        flatWeights.Add(acc.weights[r0]); flatWeights.Add(acc.weights[r1]); flatWeights.Add(acc.weights[r2]);
                        dst.Add(b); dst.Add(b + 1); dst.Add(b + 2);
                    }
                    flatSubTris.Add(dst);
                }

                m.Clear();
                m.subMeshCount = flatSubTris.Count;
                m.SetVertices(flatVerts);
                m.SetNormals(flatNorms);
                m.SetUVs(0, flatUvs);
                m.SetColors(flatCols);
                m.boneWeights = flatWeights.ToArray();
                for (int sm = 0; sm < flatSubTris.Count; sm++)
                {
                    m.SetTriangles(flatSubTris[sm], sm);
                }
            }

            m.bindposes = bindposes;
            m.RecalculateBounds();

            EnsureCharacterAssetFolders();
            string path = $"{CharacterModelsFolder}/{assetName}.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(m, existing);
                Object.DestroyImmediate(m);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(m, path);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Builds a real skeletal rigged PS1/VHS low-poly 3D humanoid (~2,000 triangles) using
        /// a unified SkinnedMeshRenderer (17 articulated bones with smooth multi-bone weights),
        /// a saved AnimatorController, and 6 real .anim AnimationClip assets in Assets/Models/Characters/.
        /// Grounded cleanly at exact local Y = 0.000f.
        /// </summary>
        private static GameObject BuildSculptedPS1Humanoid(
            Transform parent,
            string name,
            SilhouetteArchetype archetype,
            Material overrideGarmentMat,
            Material overrideSkinMat,
            GreyboxPalette pal)
        {
            CharacterMaterials cm = EnsureCharacterMaterials(pal);
            AnimatorController controller = EnsureHumanoidAnimatorController();

            bool isChild = archetype == SilhouetteArchetype.ChildWatcher;
            bool isShadow = archetype == SilhouetteArchetype.ShadowDash;
            bool isMargaret = archetype == SilhouetteArchetype.Margaret;
            bool isSlumpedCian = archetype == SilhouetteArchetype.SlumpedCian;

            Material faceMat = isShadow
                ? pal.shadowBlack
                : (isChild ? cm.childFace : (isSlumpedCian ? cm.cianFace : cm.margaretFace));
            Material skinMat = isShadow ? pal.shadowBlack : (overrideSkinMat != null ? overrideSkinMat : pal.margaretPallor);
            Material garmentMat = isShadow
                ? pal.shadowBlack
                : (isChild ? cm.childGownCloth : (isMargaret ? cm.nightgownCloth : (overrideGarmentMat != null ? overrideGarmentMat : pal.cianShirtWrinkled)));
            Material hairMat = isShadow ? pal.shadowBlack : cm.hairDark;
            Material lowerGarmentMat = isShadow ? pal.shadowBlack : (isSlumpedCian ? pal.cianTrousersDark : garmentMat);
            Material accentMat = isShadow ? pal.shadowBlack : (isSlumpedCian ? pal.bookSlateBlue : hairMat);

            float scale = isChild ? 0.68f : (isSlumpedCian ? 1.02f : 1.0f);

            // Key anatomical heights (grounded at Y = 0.000m)
            float hipsY = 0.86f * scale;
            float kneeY = 0.44f * scale;
            float torsoBaseY = (isMargaret || isChild) ? 0.92f * scale : 0.86f * scale;
            float shoulderY = torsoBaseY + 0.43f * scale;
            float elbowY = shoulderY - 0.27f * scale;
            float wristY = shoulderY - 0.51f * scale;
            float neckY = shoulderY + 0.042f * scale;
            float headRx = (isChild ? 0.080f : 0.092f) * (isChild ? 1f : scale);
            float headRy = (isChild ? 0.094f : 0.118f) * (isChild ? 1f : scale);
            float headRz = (isChild ? 0.088f : 0.104f) * (isChild ? 1f : scale);
            float headCenterY = neckY + 0.074f * scale + headRy * 0.86f;
            float armX = 0.196f * scale;
            float legX = 0.088f * scale;

            // 1. Construct exact 17-bone skeletal rig under root
            GameObject root = CreateContainer(parent, name);

            Transform boneHips = CreateContainer(root.transform, "Bone_Hips").transform;
            boneHips.localPosition = new Vector3(0f, hipsY, 0f);

            Transform boneTorso = CreateContainer(root.transform, "Torso_Esguio").transform;
            boneTorso.localPosition = new Vector3(0f, torsoBaseY, 0f);

            Transform boneShoulders = CreateContainer(root.transform, "Ombros_Curvados").transform;
            boneShoulders.localPosition = new Vector3(0f, shoulderY, 0.008f * scale);

            Transform boneNeck = CreateContainer(root.transform, "Pescoco_Inclinado").transform;
            boneNeck.localPosition = new Vector3(0f, neckY, 0.014f * scale);

            Transform boneHead = CreateContainer(root.transform, "Cabeca_Silhueta").transform;
            boneHead.localPosition = new Vector3(0f, headCenterY, 0.020f * scale);

            Transform boneMouth = CreateContainer(boneHead, "Boca_Escura").transform;
            boneMouth.localPosition = new Vector3(0f, -headRy * 0.44f, headRz * 0.84f);
            boneMouth.localScale = Vector3.one;

            Transform boneArmL = CreateContainer(root.transform, "Braco_Esq").transform;
            boneArmL.localPosition = new Vector3(-armX, shoulderY - 0.014f * scale, 0.008f * scale);
            Transform boneForearmL = CreateContainer(boneArmL, "Antebraco_Esq").transform;
            boneForearmL.localPosition = new Vector3(0f, elbowY - (shoulderY - 0.014f * scale), 0.004f * scale);
            Transform boneHandL = CreateContainer(boneForearmL, "Mao_Esq").transform;
            boneHandL.localPosition = new Vector3(0f, wristY - elbowY, 0.008f * scale);

            Transform boneArmR = CreateContainer(root.transform, "Braco_Dir").transform;
            boneArmR.localPosition = new Vector3(armX, shoulderY - 0.014f * scale, 0.008f * scale);
            Transform boneForearmR = CreateContainer(boneArmR, "Antebraco_Dir").transform;
            boneForearmR.localPosition = new Vector3(0f, elbowY - (shoulderY - 0.014f * scale), 0.004f * scale);
            Transform boneHandR = CreateContainer(boneForearmR, "Mao_Dir").transform;
            boneHandR.localPosition = new Vector3(0f, wristY - elbowY, 0.008f * scale);

            Transform boneLegL = CreateContainer(root.transform, "Perna_Esq").transform;
            boneLegL.localPosition = new Vector3(-legX, hipsY, 0f);
            Transform boneShinL = CreateContainer(boneLegL, "Canela_Esq").transform;
            boneShinL.localPosition = new Vector3(0f, kneeY - hipsY, 0.006f * scale);

            Transform boneLegR = CreateContainer(root.transform, "Perna_Dir").transform;
            boneLegR.localPosition = new Vector3(legX, hipsY, 0f);
            Transform boneShinR = CreateContainer(boneLegR, "Canela_Dir").transform;
            boneShinR.localPosition = new Vector3(0f, kneeY - hipsY, 0.006f * scale);

            Transform boneSecondary;
            if (isMargaret || isChild)
            {
                boneSecondary = CreateContainer(boneHead, "Cabelo_Caido_Esq").transform;
                boneSecondary.localPosition = new Vector3(0f, -headRy * 0.35f, -headRz * 0.25f);
            }
            else if (isSlumpedCian)
            {
                boneSecondary = CreateContainer(boneTorso, "Gravata_Frouxa").transform;
                boneSecondary.localPosition = new Vector3(0f, 0.26f * scale, 0.108f * scale);
            }
            else
            {
                boneSecondary = CreateContainer(boneHips, "Bone_Secondary").transform;
                boneSecondary.localPosition = Vector3.zero;
            }

            Transform[] bones =
            {
                boneHips,      // 0
                boneTorso,     // 1
                boneShoulders, // 2
                boneNeck,      // 3
                boneHead,      // 4
                boneMouth,     // 5
                boneArmL,      // 6
                boneForearmL,  // 7
                boneHandL,     // 8
                boneArmR,      // 9
                boneForearmR,  // 10
                boneHandR,     // 11
                boneLegL,      // 12
                boneShinL,     // 13
                boneLegR,      // 14
                boneShinR,     // 15
                boneSecondary  // 16
            };

            // 2. Build unified watertight low-poly PS1 skinned geometry (~2,000 tris)
            // Submeshes: 0=Face, 1=Skin(Neck/Forearms/Hands/Feet), 2=UpperGarment, 3=LowerGarment/Shoes, 4=Hair/Tie
            var acc = new SkinnedMeshAccumulator(submeshCount: 5);

            // 2A. Continuous Anatomical Torso + Shoulders + Neck Collar (Submesh 2 = UpperGarment)
            AppendSculptedLoftToSkinned(acc, submesh: 2, sides: 14, new[]
            {
                (torsoBaseY - 0.03f * scale, 0f, 0.000f,        0.130f * scale, 0.092f * scale, 0.002f * scale, MakeBoneWeight(0, 0.65f, 1, 0.35f)),
                (torsoBaseY + 0.07f * scale, 0f, 0.002f * scale, 0.124f * scale, 0.088f * scale, 0.003f * scale, MakeBoneWeight(0, 0.25f, 1, 0.75f)),
                (torsoBaseY + 0.18f * scale, 0f, 0.006f * scale, 0.146f * scale, 0.104f * scale, 0.004f * scale, MakeBoneWeight(1, 0.85f, 2, 0.15f)),
                (torsoBaseY + 0.29f * scale, 0f, 0.008f * scale, 0.168f * scale, 0.108f * scale, 0.003f * scale, MakeBoneWeight(1, 0.45f, 2, 0.55f)),
                (torsoBaseY + 0.38f * scale, 0f, 0.008f * scale, 0.196f * scale, 0.096f * scale, 0.002f * scale, MakeBoneWeight(1, 0.15f, 2, 0.85f)),
                (shoulderY + 0.005f * scale, 0f, 0.008f * scale, 0.204f * scale, 0.088f * scale, 0.002f * scale, MakeBoneWeight(2, 1.00f)),
                (shoulderY + 0.044f * scale, 0f, 0.011f * scale, 0.118f * scale, 0.070f * scale, 0.001f * scale, MakeBoneWeight(2, 0.65f, 3, 0.35f)),
                (neckY + 0.010f * scale,     0f, 0.013f * scale, 0.064f * scale, 0.060f * scale, 0f,             MakeBoneWeight(2, 0.35f, 3, 0.65f))
            });

            // 2B. Anatomical Neck (Submesh 1 = Skin) smoothly blending Shoulders -> Neck -> Head
            AppendSculptedLoftToSkinned(acc, submesh: 1, sides: 10, new[]
            {
                (neckY - 0.018f * scale, 0f, 0.012f * scale, 0.056f * scale, 0.056f * scale, 0f, MakeBoneWeight(2, 0.35f, 3, 0.65f)),
                (neckY + 0.030f * scale, 0f, 0.015f * scale, 0.046f * scale, 0.048f * scale, 0f, MakeBoneWeight(3, 0.85f, 4, 0.15f)),
                (neckY + 0.082f * scale, 0f, 0.018f * scale, 0.044f * scale, 0.046f * scale, 0f, MakeBoneWeight(3, 0.35f, 4, 0.65f)),
                (headCenterY - headRy * 0.40f, 0f, 0.020f * scale, 0.048f * scale, 0.050f * scale, 0f, MakeBoneWeight(4, 1.00f))
            });

            // 2C. Lower Body: Pleated Flowing Nightgown + Bare Feet (Margaret/Child) OR Articulated Legs + Shoes (Cian/Shadow)
            if (isMargaret || isChild)
            {
                // Grounded bare feet peeking cleanly beneath the gown hem (Y = 0.000m)
                AppendSculptedFootOrShoeToSkinned(acc, submesh: 1, new Vector3(-0.072f * scale, 0f, 0.016f * scale), 0.036f * scale, 0.044f * scale, 0.152f * scale, -6f, MakeBoneWeight(13, 1f));
                AppendSculptedFootOrShoeToSkinned(acc, submesh: 1, new Vector3( 0.072f * scale, 0f, 0.012f * scale), 0.036f * scale, 0.044f * scale, 0.152f * scale,  5f, MakeBoneWeight(15, 1f));

                // Continuous pleated nightgown from ankles to under-bust with natural stride sway weights
                AppendSculptedLoftToSkinned(acc, submesh: 3, sides: 16, new[]
                {
                    (0.036f * scale, 0f, -0.010f * scale, 0.218f * scale, 0.168f * scale, 0.016f * scale, MakeBoneWeight(0, 0.52f, 12, 0.24f, 14, 0.24f)),
                    (0.180f * scale, 0f, -0.008f * scale, 0.206f * scale, 0.156f * scale, 0.015f * scale, MakeBoneWeight(0, 0.62f, 12, 0.19f, 14, 0.19f)),
                    (0.360f * scale, 0f, -0.006f * scale, 0.192f * scale, 0.144f * scale, 0.013f * scale, MakeBoneWeight(0, 0.76f, 12, 0.12f, 14, 0.12f)),
                    (0.560f * scale, 0f, -0.003f * scale, 0.174f * scale, 0.130f * scale, 0.011f * scale, MakeBoneWeight(0, 0.88f, 1, 0.12f)),
                    (0.760f * scale, 0f, -0.001f * scale, 0.154f * scale, 0.114f * scale, 0.008f * scale, MakeBoneWeight(0, 0.72f, 1, 0.28f)),
                    (0.920f * scale, 0f,  0.002f * scale, 0.134f * scale, 0.098f * scale, 0.005f * scale, MakeBoneWeight(0, 0.30f, 1, 0.70f)),
                    (1.080f * scale, 0f,  0.005f * scale, 0.148f * scale, 0.106f * scale, 0.003f * scale, MakeBoneWeight(1, 1.00f))
                });
            }
            else
            {
                // Pelvis trousers cap connecting both thighs smoothly
                AppendSculptedLoftToSkinned(acc, submesh: 3, sides: 12, new[]
                {
                    (0.74f * scale, 0f, 0.000f, 0.148f * scale, 0.100f * scale, 0.002f * scale, MakeBoneWeight(0, 1.00f)),
                    (0.84f * scale, 0f, 0.002f, 0.144f * scale, 0.098f * scale, 0.002f * scale, MakeBoneWeight(0, 0.80f, 1, 0.20f)),
                    (0.94f * scale, 0f, 0.004f, 0.134f * scale, 0.092f * scale, 0.001f * scale, MakeBoneWeight(0, 0.40f, 1, 0.60f))
                });

                // Articulated Left & Right Trousers + Knees + Shoes (with smooth thigh-to-shin skinning!)
                for (int side = -1; side <= 1; side += 2)
                {
                    int thighBone = side < 0 ? 12 : 14;
                    int shinBone = side < 0 ? 13 : 15;
                    float lx = side * legX;
                    AppendSculptedLoftToSkinned(acc, submesh: 3, sides: 10, new[]
                    {
                        (0.044f * scale, lx, 0.002f * scale, 0.046f * scale, 0.052f * scale, 0.002f * scale, MakeBoneWeight(shinBone, 1.00f)),
                        (0.240f * scale, lx, 0.000f,         0.054f * scale, 0.058f * scale, 0.002f * scale, MakeBoneWeight(shinBone, 1.00f)),
                        (0.390f * scale, lx, 0.006f * scale, 0.058f * scale, 0.062f * scale, 0.002f * scale, MakeBoneWeight(shinBone, 0.72f, thighBone, 0.28f)),
                        (0.450f * scale, lx, 0.009f * scale, 0.060f * scale, 0.064f * scale, 0.002f * scale, MakeBoneWeight(shinBone, 0.35f, thighBone, 0.65f)),
                        (0.640f * scale, lx, 0.004f * scale, 0.072f * scale, 0.076f * scale, 0.003f * scale, MakeBoneWeight(thighBone, 0.90f, 0, 0.10f)),
                        (0.840f * scale, lx, 0.000f,         0.084f * scale, 0.086f * scale, 0.002f * scale, MakeBoneWeight(thighBone, 0.65f, 0, 0.35f))
                    });

                    AppendSculptedFootOrShoeToSkinned(
                        acc, submesh: 4,
                        new Vector3(lx, 0f, 0.022f * scale),
                        0.042f * scale, 0.050f * scale, 0.178f * scale,
                        side * 4f,
                        MakeBoneWeight(shinBone, 1f));
                }

                if (isSlumpedCian)
                {
                    // Sculpted loose office tie weighted to Gravata_Frouxa bone (bone 16)
                    AppendClosedBoxToSkinned(acc, submesh: 4,
                        new Vector3(0f, torsoBaseY + 0.25f * scale, 0.112f * scale),
                        new Vector3(0.032f * scale, 0.22f * scale, 0.008f * scale),
                        new Vector3(2f, 0f, -4f),
                        MakeBoneWeight(16, 0.85f, 1, 0.15f));
                }
            }

            // 2D. Articulated Left & Right Arms (Continuous shoulder->bicep->elbow->forearm->wrist->hand + sleeves)
            for (int side = -1; side <= 1; side += 2)
            {
                int upperBone = side < 0 ? 6 : 9;
                int foreBone = side < 0 ? 7 : 10;
                int handBone = side < 0 ? 8 : 11;
                float ax = side * armX;
                float sFlare = side * 0.022f * scale;

                // Continuous skinned arm + hand (Submesh 1 = Skin)
                AppendSculptedLoftToSkinned(acc, submesh: 1, sides: 10, new[]
                {
                    (wristY - 0.112f * scale, ax + sFlare * 1.15f, 0.024f * scale, 0.018f * scale, 0.026f * scale, 0f, MakeBoneWeight(handBone, 1.00f)),
                    (wristY - 0.052f * scale, ax + sFlare * 1.10f, 0.020f * scale, 0.026f * scale, 0.030f * scale, 0f, MakeBoneWeight(handBone, 0.85f, foreBone, 0.15f)),
                    (wristY + 0.004f * scale, ax + sFlare * 1.02f, 0.016f * scale, 0.024f * scale, 0.026f * scale, 0f, MakeBoneWeight(foreBone, 0.70f, handBone, 0.30f)),
                    (elbowY - 0.120f * scale, ax + sFlare * 0.82f, 0.012f * scale, 0.032f * scale, 0.034f * scale, 0f, MakeBoneWeight(foreBone, 1.00f)),
                    (elbowY - 0.020f * scale, ax + sFlare * 0.60f, 0.010f * scale, 0.037f * scale, 0.039f * scale, 0f, MakeBoneWeight(foreBone, 0.68f, upperBone, 0.32f)),
                    (elbowY + 0.030f * scale, ax + sFlare * 0.45f, 0.009f * scale, 0.039f * scale, 0.041f * scale, 0f, MakeBoneWeight(foreBone, 0.25f, upperBone, 0.75f)),
                    (shoulderY - 0.130f * scale, ax + sFlare * 0.20f, 0.008f * scale, 0.044f * scale, 0.046f * scale, 0f, MakeBoneWeight(upperBone, 0.90f, 2, 0.10f)),
                    (shoulderY - 0.010f * scale, ax,                  0.008f * scale, 0.048f * scale, 0.050f * scale, 0f, MakeBoneWeight(upperBone, 0.55f, 2, 0.45f))
                });

                // Draped Sleeve over upper arm (Submesh 2 = UpperGarment)
                AppendSculptedLoftToSkinned(acc, submesh: 2, sides: 10, new[]
                {
                    (elbowY - 0.035f * scale,    ax + sFlare * 0.65f, 0.010f * scale, 0.048f * scale, 0.050f * scale, 0.003f * scale, MakeBoneWeight(foreBone, 0.55f, upperBone, 0.45f)),
                    (shoulderY - 0.140f * scale, ax + sFlare * 0.22f, 0.008f * scale, 0.052f * scale, 0.054f * scale, 0.003f * scale, MakeBoneWeight(upperBone, 0.88f, 2, 0.12f)),
                    (shoulderY + 0.004f * scale, ax,                  0.008f * scale, 0.054f * scale, 0.056f * scale, 0.002f * scale, MakeBoneWeight(upperBone, 0.50f, 2, 0.50f))
                });
            }

            // 2E. Sculpted Anatomical PS1 Head (Submesh 0 = Face) + Volumetric Draped Hair (Submesh 4 = Hair)
            Vector3 headCenter = new Vector3(0f, headCenterY, 0.020f * scale);
            AppendSculptedHeadToSkinned(acc, submesh: 0, headCenter, headRx, headRy, headRz, headBoneIdx: 4, jawBoneIdx: 5);

            if (isMargaret || isChild)
            {
                float hairDrop = isChild ? 0.23f : 0.39f;
                AppendSculptedHairCurtainToSkinned(
                    acc, submesh: 4, headCenter, headRx, headRy, headRz, hairDrop,
                    rings: 8, segs: 12, headBoneIdx: 4, hairSwayBoneIdx: 16);
            }
            else
            {
                AppendSculptedHairCurtainToSkinned(
                    acc, submesh: 4, headCenter, headRx, headRy, headRz, dropLength: 0.038f * scale,
                    rings: 5, segs: 12, headBoneIdx: 4, hairSwayBoneIdx: 4);
            }

            // 3. Guarantee exact Y = 0.000m floor grounding on both mesh vertices and bones before computing bindposes
            float minMeshY = float.MaxValue;
            for (int i = 0; i < acc.verts.Count; i++)
            {
                if (acc.verts[i].y < minMeshY) minMeshY = acc.verts[i].y;
            }
            float yShift = (minMeshY < float.MaxValue) ? -minMeshY : 0f;
            if (Mathf.Abs(yShift) > 1e-6f)
            {
                for (int b = 0; b < bones.Length; b++)
                {
                    if (bones[b].parent == root.transform)
                    {
                        Vector3 lp = bones[b].localPosition;
                        lp.y += yShift;
                        bones[b].localPosition = lp;
                    }
                }
            }

            // 4. Create SkinnedMeshRenderer holder directly at root origin and compute exact bindposes
            GameObject skinnedGo = CreateContainer(root.transform, "Corpo_Humanoide_Skinned");
            Matrix4x4 meshLocalToWorld = skinnedGo.transform.localToWorldMatrix;
            Matrix4x4[] bindposes = new Matrix4x4[bones.Length];
            for (int b = 0; b < bones.Length; b++)
            {
                bindposes[b] = bones[b].worldToLocalMatrix * meshLocalToWorld;
            }

            string meshAssetName = isMargaret
                ? "Mesh_Rigged_Margaret"
                : (isChild ? "Mesh_Rigged_ChildWatcher" : (isSlumpedCian ? "Mesh_Rigged_SlumpedCian" : "Mesh_Rigged_ShadowDash"));

            Mesh riggedMesh = FinalizeAndSaveSkinnedMesh(acc, meshAssetName, bindposes, yShift);

            SkinnedMeshRenderer smr = skinnedGo.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = riggedMesh;
            smr.bones = bones;
            smr.rootBone = boneHips;
            smr.sharedMaterials = new[] { faceMat, skinMat, garmentMat, lowerGarmentMat, isSlumpedCian ? accentMat : hairMat };
            if (isSlumpedCian)
            {
                // Submesh 4 has both shoes/tie/short hair; use dark hairMat (dark charcoal) so shoes, tie, and hair read cleanly without tint clash
                smr.sharedMaterials = new[] { faceMat, skinMat, garmentMat, lowerGarmentMat, hairMat };
            }
            smr.shadowCastingMode = ShadowCastingMode.On;
            smr.receiveShadows = true;
            smr.updateWhenOffscreen = true;
            smr.localBounds = new Bounds(new Vector3(0f, 0.90f * scale, 0.08f * scale), new Vector3(1.10f * scale, 2.05f * scale, 1.35f * scale));

            // 5. Wire real Unity Animator + saved AnimatorController (plays real .anim clips at runtime)
            Animator animator = GetOrAdd<Animator>(root);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            GetOrAdd<HumanoidSpasmAnimator>(root);
            EditorUtility.SetDirty(root);
            return root;
        }
    }
}
#endif
