#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Canal420.Audio;
using Canal420.Core;
using Canal420.Effects;
using Canal420.Events;
using Canal420.Interaction;
using Canal420.Player;
using Canal420.Puzzles;
using Canal420.UI;

namespace Canal420.EditorTools
{
    [InitializeOnLoad]
    public static partial class Canal420GreyboxBuilder
    {
        private static readonly string[] LegacyVersionMarkers =
        {
            "Greybox_Arquitetura_v2",
            "Greybox_Arquitetura_v3",
            "Greybox_Arquitetura_v4",
            "Greybox_Arquitetura_v5",
            "Greybox_Arquitetura_v6",
            "Greybox_Arquitetura_v7",
            "Greybox_Arquitetura_v8",
            "Greybox_Arquitetura_v9",
            "Greybox_Arquitetura_v10",
            "Greybox_Arquitetura_v11",
            "Greybox_Arquitetura_v12",
            "Greybox_Arquitetura_v13",
            "Greybox_Arquitetura_v14",
            "Greybox_Arquitetura_v15",
            "Greybox_Arquitetura_v16",
            "Greybox_Arquitetura_v17",
            "Greybox_Arquitetura_v18",
            "Greybox_Arquitetura_v19",
            "Greybox_Arquitetura_v20",
            "Greybox_Arquitetura_v21",
            "Greybox_Arquitetura_v22",
            "Greybox_Arquitetura_v23",
            "Greybox_Arquitetura_v24",
            "Greybox_Arquitetura_v25",
            "Greybox_Arquitetura_v26",
            "Greybox_Arquitetura_v27",
            "Greybox_Arquitetura_v28",
            "Greybox_Arquitetura_v29",
            "Greybox_Arquitetura_v30",
            "Greybox_Arquitetura_v31",
            "Greybox_Arquitetura_v32",
            "Greybox_Arquitetura_v33",
            "Greybox_Arquitetura_v34",
            "Greybox_Arquitetura_v35",
            "Greybox_Arquitetura_v36",
            "Greybox_Arquitetura_v37",
            "Greybox_Arquitetura_v38",
            "Greybox_Arquitetura_v39",
            "Greybox_Arquitetura_v40",
            "Greybox_Arquitetura_v41",
            "Greybox_Arquitetura_v42",
            "Greybox_Arquitetura_v43",
            "Greybox_Arquitetura_v44",
            "Greybox_Arquitetura_v45"
        };
        private const string VersionMarkerName = "Greybox_Arquitetura_v46";
        private const string MaterialsFolder = "Assets/Materials/Greybox";

        private static Material activeBaseboardMaterial;

        private const float H = 2.74f;
        private const float T = 0.16f;
        private const float DoorH = 2.14f;
        private const float ArchH = 2.26f;

        private struct Opening
        {
            public float a;
            public float b;
            public float bottom;
            public float top;

            public Opening(float a, float b, float top)
            {
                this.a = a;
                this.b = b;
                this.bottom = 0f;
                this.top = top;
            }

            public Opening(float a, float b, float bottom, float top)
            {
                this.a = a;
                this.b = b;
                this.bottom = bottom;
                this.top = top;
            }
        }

        private struct HouseContainers
        {
            public AnimatedContainerInteractable kitchenUtilityDrawer;
            public AnimatedContainerInteractable kitchenBreakerBox;
            public AnimatedContainerInteractable bedroomWardrobe;
            public FaucetWaterController bathroomFaucet;
            public MirrorHauntController bathroomMirrorHaunt;
        }

        static Canal420GreyboxBuilder()
        {
            EditorApplication.delayCall += TryAutoBuildOnceOnLoad;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += TryAutoBuildOnceOnLoad;
            }
        }

        private static void TryAutoBuildOnceOnLoad()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
                return;

            GameObject casaBase = FindInScene(activeScene, "Casa_Base");
            if (casaBase == null)
                return;

            if (casaBase.transform.Find(VersionMarkerName) != null)
            {
                Shader dirtyLitShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/LowPolyDirtyLit.shader");
                if (dirtyLitShader == null) dirtyLitShader = Shader.Find("Canal420/LowPolyDirtyLit");
                Shader crtShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/CRTPhosphorDisplay.shader");
                if (crtShader == null) crtShader = Shader.Find("Canal420/CRTPhosphorDisplay");

                bool phase2AssetsReady = true;
                if (dirtyLitShader != null && crtShader != null)
                {
                    Material wallMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/M_Wall_Plaster.mat");
                    Material tvMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/M_TV_Screen.mat");
                    Material visorMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/M_Visor_Digital.mat");
                    Mesh cylMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshesFolder}/Mesh_LowPoly_Cylinder8.asset");
                    Mesh icoMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshesFolder}/Mesh_LowPoly_Icosphere1.asset");

                    phase2AssetsReady = wallMat != null && wallMat.shader == dirtyLitShader
                        && wallMat.HasProperty("_SurfaceTex") && wallMat.GetTexture("_SurfaceTex") != null
                        && tvMat != null && tvMat.shader == crtShader
                        && visorMat != null && visorMat.IsKeywordEnabled("_EMISSION")
                        && cylMesh != null
                        && icoMesh != null;
                }

                bool phase3SceneReady = true;
                GameObject existingD2 = FindInScene(activeScene, "Props_Dia2");
                GameObject existingD3 = FindInScene(activeScene, "Props_Dia3");
                Transform archTransform = casaBase.transform.Find(VersionMarkerName);
                Transform officeChairSeatTr = archTransform != null ? archTransform.Find("Cadeira_Escritorio/Assento") : null;
                bool officeChairDynamic = officeChairSeatTr != null
                    && (GameObjectUtility.GetStaticEditorFlags(officeChairSeatTr.gameObject) & StaticEditorFlags.BatchingStatic) == 0;
                Transform diningChairTr = archTransform != null ? FindRecursive(archTransform, "Cadeira_Jantar_Norte", false) : null;
                bool diningChairDynamic = diningChairTr != null
                    && (GameObjectUtility.GetStaticEditorFlags(diningChairTr.gameObject) & StaticEditorFlags.BatchingStatic) == 0;
                Transform secD3Tr = existingD3 != null ? FindRecursive(existingD3.transform, "Secretaria_Eletronica_D3", false) : null;
                Transform secD3LedTr = secD3Tr != null ? secD3Tr.Find("LED_Mensagens") : null;
                bool secD3LedDynamic = secD3LedTr != null
                    && (GameObjectUtility.GetStaticEditorFlags(secD3LedTr.gameObject) & StaticEditorFlags.BatchingStatic) == 0;
                Transform margaretTr = existingD3 != null ? FindRecursive(existingD3.transform, "Margaret_Jumpscare", false) : null;
                bool riggedCharactersReady = margaretTr != null
                    && margaretTr.GetComponentInChildren<SkinnedMeshRenderer>(true) != null
                    && margaretTr.GetComponent<Animator>() != null
                    && margaretTr.GetComponent<Animator>().runtimeAnimatorController != null
                    && AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CharacterAnimatorControllerPath) != null;
                Transform stimBottleTr = existingD3 != null ? FindRecursive(existingD3.transform, "Frasco_Estimulante_Dia3", false) : null;
                InspectableObject stimBottleInsp = stimBottleTr != null ? stimBottleTr.GetComponent<InspectableObject>() : null;
                bool stimBottleDocUpdated = false;
                if (stimBottleInsp != null)
                {
                    string existingBottleDoc = new SerializedObject(stimBottleInsp).FindProperty("documentBody")?.stringValue ?? string.Empty;
                    stimBottleDocUpdated = existingBottleDoc.Contains("rótulo borrado de suor")
                        && !existingBottleDoc.Contains("Restam apenas duas");
                }
                bool hasPhase3Props = existingD3 != null
                    && FindRecursive(existingD3.transform, "Estimulante_Dia3_Mesa", false) != null
                    && FindRecursive(existingD3.transform, "Lanterna_Dia3", false) != null
                    && FindRecursive(existingD3.transform, "Espelho_Dia3", false) != null
                    && FindRecursive(existingD3.transform, "Cabelo_Caido_Esq", false) != null
                    && existingD2 != null
                    && FindRecursive(existingD2.transform, "Gravata_Frouxa", false) != null
                    && FindRecursive(existingD2.transform, "Aparicao_Porta_Banheiro_D2", false) != null
                    && stimBottleDocUpdated;
                phase3SceneReady = officeChairDynamic && diningChairDynamic && secD3LedDynamic && riggedCharactersReady && hasPhase3Props;

                GameObject existingFilhas = FindInScene(activeScene, "Quarto_Filhas");
                Transform perfilBoxTr = archTransform != null ? FindRecursive(archTransform, "Perfil_Porta_Box", false) : null;
                bool phase4UpgradeReady = archTransform != null
                    && FindRecursive(archTransform, "Borda_Bandeja_N", false) != null
                    && FindRecursive(archTransform, "Aro_Trempe_0_0_N", false) != null
                    && perfilBoxTr != null && perfilBoxTr.localPosition.y > 1.040f
                    && existingFilhas != null
                    && FindRecursive(existingFilhas.transform, "Cadeira_Infantil_1", false) != null
                    && AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshesFolder}/Mesh_TV_TuboTraseiro.asset") != null
                    && AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshesFolder}/Mesh_Frasco_Farmacia.asset") != null
                    && AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshesFolder}/Mesh_Coberta_Infantil.asset") != null;

                DayManager existingDm = FindInScene(activeScene, "GameManager")?.GetComponent<DayManager>();
                if (phase2AssetsReady && phase3SceneReady && phase4UpgradeReady && existingDm != null)
                {
                    SerializedObject dmSo = new SerializedObject(existingDm);
                    SerializedProperty tvProp = dmSo.FindProperty("tvController");
                    SerializedProperty probesProp = dmSo.FindProperty("interiorReflectionProbes");
                    bool allProbesValid = probesProp != null && probesProp.arraySize == 8;
                    if (allProbesValid)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            if (probesProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                            {
                                allProbesValid = false;
                                break;
                            }
                        }
                    }

                    if (allProbesValid && tvProp != null && tvProp.objectReferenceValue != null)
                    {
                        Canal420AudioManager existingAudio = existingDm.GetComponent<Canal420AudioManager>();
                        if (existingAudio != null && NeedsAudioClipReassign(existingAudio))
                        {
                            existingAudio.AutoAssignImportedClips();
                            EditorSceneManager.MarkSceneDirty(activeScene);
                            EditorSceneManager.SaveScene(activeScene);
                            Debug.Log("[Canal 420] Clipes de áudio (passos .wav, fita cassete Dia 2 e canção de ninar) reconectados e salvos na cena.");
                        }
                        EnsureKenomaLogoAndAppIconOptimized();
                        return;
                    }
                }
            }

            BuildAndFixCompleteResidence(saveScene: true, logBanner: true);
        }

        private static void EnsureKenomaLogoAndAppIconOptimized()
        {
            const string kenomaPath = "Assets/Resources/UI/KENOMA.png";
            TextureImporter ti = AssetImporter.GetAtPath(kenomaPath) as TextureImporter;
            if (ti != null && (ti.maxTextureSize > 2048 || ti.textureCompression == TextureImporterCompression.Uncompressed))
            {
                ti.maxTextureSize = 2048;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.SaveAndReimport();
                Debug.Log("[Canal 420] Assets/Resources/UI/KENOMA.png reimportada (2048px CompressedHQ) para reduzir ~23 MB da build.");
            }

            Texture2D[] existingIcons = PlayerSettings.GetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, IconKind.Any);
            bool hasDefaultIcon = existingIcons != null && existingIcons.Length > 0 && existingIcons[0] != null;
            if (!hasDefaultIcon)
            {
                Texture2D kenomaTex = AssetDatabase.LoadAssetAtPath<Texture2D>(kenomaPath);
                if (kenomaTex != null)
                {
                    PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { kenomaTex }, IconKind.Any);
                    Debug.Log("[Canal 420] Ícone padrão do aplicativo configurado com KENOMA.png.");
                }
            }
        }

        private static bool NeedsAudioClipReassign(Canal420AudioManager audioManager)
        {
            SerializedObject so = new SerializedObject(audioManager);
            SerializedProperty woodProp = so.FindProperty("customWoodSteps");
            SerializedProperty tileProp = so.FindProperty("customTileSteps");
            SerializedProperty heavyProp = so.FindProperty("customHeavySteps");
            if (woodProp == null || woodProp.arraySize == 0 ||
                tileProp == null || tileProp.arraySize == 0 ||
                heavyProp == null || heavyProp.arraySize == 0)
            {
                return true;
            }

            SerializedProperty overridesProp = so.FindProperty("foleyOverrides");
            if (overridesProp != null)
            {
                for (int i = 0; i < overridesProp.arraySize; i++)
                {
                    SerializedProperty elem = overridesProp.GetArrayElementAtIndex(i);
                    SerializedProperty idProp = elem.FindPropertyRelative("soundId");
                    SerializedProperty clipProp = elem.FindPropertyRelative("clip");
                    if (idProp != null && idProp.intValue == (int)FoleySoundId.CassetteTapeHauntD2 &&
                        clipProp != null && clipProp.objectReferenceValue is AudioClip clip &&
                        clip.name == "answering_machine_play")
                    {
                        return true;
                    }
                    if (idProp != null && idProp.intValue == (int)FoleySoundId.EerieLullabyHum &&
                        clipProp != null && clipProp.objectReferenceValue is AudioClip lullabyClip &&
                        lullabyClip.name == "eerie_lullaby_hum")
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        [MenuItem("Canal 420/Construir e Corrigir Residência Greybox Completa", priority = 1)]
        public static void MenuBuildResidence()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Canal 420] Saia do Play Mode antes de reconstruir a cena Greybox.");
                return;
            }

            BuildAndFixCompleteResidence(saveScene: true, logBanner: true);
        }

        [MenuItem("Canal 420/Assar Occlusion Culling (Umbra)", priority = 16)]
        public static void MenuBakeOcclusionCulling()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Canal 420] Saia do Play Mode antes de assar o Occlusion Culling.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            GameObject casaBase = FindInScene(scene, "Casa_Base");
            Transform archRoot = casaBase != null ? casaBase.transform.Find(VersionMarkerName) : null;
            if (casaBase == null || archRoot == null)
            {
                Debug.LogWarning("[Canal 420] Casa_Base ou arquitetura atual não encontrada. Execute Construir e Corrigir Residência primeiro.");
                return;
            }

            EnsureHouseOcclusionArea(archRoot);
            GameObject propsDia1 = FindInScene(scene, "Props_Dia1");
            GameObject propsDia2 = FindInScene(scene, "Props_Dia2");
            GameObject propsDia3 = FindInScene(scene, "Props_Dia3");
            GameObject quartoFilhas = FindInScene(scene, "Quarto_Filhas");
            ConfigureStaticFlagsAndBatching(
                casaBase.transform,
                archRoot,
                propsDia1 != null ? propsDia1.transform : null,
                propsDia2 != null ? propsDia2.transform : null,
                propsDia3 != null ? propsDia3.transform : null,
                quartoFilhas != null ? quartoFilhas.transform : null);
            ConfigureOcclusionBakeSettings();
            EditorSceneManager.MarkSceneDirty(scene);

            if (StaticOcclusionCulling.Compute())
            {
                EditorSceneManager.SaveOpenScenes();
                Debug.Log($"[Canal 420] Occlusion Culling (Umbra) assado e salvo ({StaticOcclusionCulling.umbraDataSize} bytes).");
            }
            else
            {
                Debug.LogError("[Canal 420] Falha ao assar o Occlusion Culling (Umbra).");
            }
        }

        public static void BuildAndFixCompleteResidence(bool saveScene, bool logBanner)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            EnsureMaterialFolders();
            GreyboxPalette pal = CreateOrLoadPalette();
            activeBaseboardMaterial = pal.baseboardWood;

            GameObject casaBase = GetOrCreateRoot(scene, "Casa_Base");
            GameObject propsDia1 = GetOrCreateRoot(scene, "Props_Dia1");
            GameObject propsDia2 = GetOrCreateRoot(scene, "Props_Dia2");
            GameObject propsDia3 = GetOrCreateRoot(scene, "Props_Dia3");
            GameObject quartoFilhas = FindInSceneCaseInsensitive(scene, "Quarto_Filhas") ?? GetOrCreateRoot(scene, "Quarto_Filhas");
            quartoFilhas.name = "Quarto_Filhas";
            quartoFilhas.transform.SetParent(null);
            ResetRootTransform(quartoFilhas.transform);

            GameObject spawnRoot = GetOrCreateRoot(scene, "SpawnPoints");

            foreach (string marker in LegacyVersionMarkers)
            {
                DestroyChildIfExists(casaBase.transform, marker);
            }
            for (int c = casaBase.transform.childCount - 1; c >= 0; c--)
            {
                Transform ch = casaBase.transform.GetChild(c);
                if (ch != null && ch.name.StartsWith("Greybox_Arquitetura_", System.StringComparison.Ordinal))
                {
                    Object.DestroyImmediate(ch.gameObject);
                }
            }
            DestroyChildIfExists(casaBase.transform, VersionMarkerName);
            DestroyChildIfExists(casaBase.transform, VersionMarkerName + "_InProgress");
            DestroyChildIfExists(casaBase.transform, "Mobilia_Fixa_Greybox");
            DestroyChildIfExists(casaBase.transform, "Iluminacao_Interna");
            DestroyChildIfExists(casaBase.transform, "Porta_Quarto_Pivot");
            DestroyChildIfExists(casaBase.transform, "TV_Sala");
            DestroyAllChildren(propsDia1.transform);
            DestroyAllChildren(propsDia2.transform);
            DestroyAllChildren(propsDia3.transform);
            DestroyChildIfExists(quartoFilhas.transform, "Ambiente_QuartoFilhas_Greybox");
            DestroyChildIfExists(quartoFilhas.transform, "Luz_Fresta_Janela_Filhas");
            DestroyChildIfExists(quartoFilhas.transform, "Cube");
            DestroyChildIfExists(quartoFilhas.transform, "Cube (1)");

            SetupBaseFloor(casaBase.transform, pal);
            GameObject archRoot = CreateContainer(casaBase.transform, VersionMarkerName + "_InProgress");

            BuildWalls(archRoot.transform, pal);
            DoorInteractable officeDoor = BuildInteriorDoors(archRoot.transform, pal);
            DoorInteractable lockedCorridorDoor = SetupLockedCorridorDoor(casaBase.transform, pal);

            HouseContainers containers = default;
            TVController tvController = BuildLivingRoom(archRoot.transform, casaBase.transform, pal);
            BuildKitchen(archRoot.transform, pal, ref containers);
            BuildBathroom(archRoot.transform, pal, ref containers);
            BuildBedroom(archRoot.transform, pal, ref containers);
            BuildCorridorAndEntrance(archRoot.transform, pal);
            BuildOffice(archRoot.transform, pal);

            Light[] interiorLights = BuildInteriorLights(archRoot.transform, pal);
            Light circuitDeskLight = BuildCircuitDeskLight(archRoot.transform);

            SetupDay1Props(propsDia1.transform, pal, containers);
            SetupDay2Props(propsDia2.transform, pal, containers);
            GameObject margaretJumpscareObj = SetupDay3Props(propsDia3.transform, pal, containers);

            Light daughtersRoomLight = SetupQuartoFilhas(quartoFilhas.transform, pal);
            List<Light> allManagedInteriorLights = new List<Light>(interiorLights);
            if (daughtersRoomLight != null) allManagedInteriorLights.Add(daughtersRoomLight);

            Light[] secondaryLights = BuildSecondaryMotivatedLights(archRoot.transform, quartoFilhas.transform);
            Light[] fillLights = BuildArchitecturalFillLights(archRoot.transform, quartoFilhas.transform);
            ReflectionProbe[] interiorProbes = BuildInternalReflectionProbes(archRoot.transform, quartoFilhas.transform);

            EnsureShadowContainmentAndLampShades(casaBase.transform, quartoFilhas.transform);
            ApplyEditModeDay1Preview(allManagedInteriorLights.ToArray(), circuitDeskLight, secondaryLights, fillLights);

            Transform spDia1 = EnsureSpawnPoint(spawnRoot.transform, "Spawn_Dia1",
                new Vector3(-2.22f, 0.02f, 4.20f), Quaternion.Euler(0f, 165f, 0f));

            Transform spSofaSit = EnsureSpawnPoint(spawnRoot.transform, "Spawn_Sofa_Sentado",
                new Vector3(-0.84f, 0.02f, -2.14f), Quaternion.Euler(0f, 180f, 0f));

            Transform spSofaStand = EnsureSpawnPoint(spawnRoot.transform, "Spawn_Sofa_Levantar",
                new Vector3(-0.84f, 0.02f, -2.92f), Quaternion.Euler(0f, 180f, 0f));

            Transform spDia2Sofa = EnsureSpawnPoint(spawnRoot.transform, "Spawn_Dia2",
                new Vector3(0.22f, 0.02f, -3.32f), Quaternion.Euler(0f, 240f, 0f));

            Transform spDia3 = EnsureSpawnPoint(spawnRoot.transform, "Spawn_Dia3",
                new Vector3(-0.84f, 0.02f, -4.60f), Quaternion.Euler(0f, 180f, 0f));

            Transform spDia4Filhas = EnsureSpawnPoint(spawnRoot.transform, "Spawn_Dia4_QuartoFilhas",
                new Vector3(3.18f, 0.02f, 3.29f), Quaternion.Euler(4f, 90f, 0f));

            PlayerController player = SetupAndFixPlayer(scene, spDia1, pal);

            SanityEffects sanityEffects = SetupGlobalVolumeAndSanity(scene);
            Light mainDirectionalLight = SetupMainLightAndAmbient(scene);

            GameObject gmObj = FindInScene(scene, "GameManager") ?? GetOrCreateRoot(scene, "GameManager");
            DayManager dayManager = GetOrAdd<DayManager>(gmObj);
            ObjectiveHUD objectiveHUD = GetOrAdd<ObjectiveHUD>(gmObj);
            GameSettingsUI settingsUI = GetOrAdd<GameSettingsUI>(gmObj);
            Canal420AudioManager audioManager = GetOrAdd<Canal420AudioManager>(gmObj);
            GetOrAdd<PsychologicalHorrorDirector>(gmObj);

            dayManager.ConfigureManagerReferences(
                casa: casaBase,
                p1: propsDia1,
                p2: propsDia2,
                p3: propsDia3,
                qFilhas: quartoFilhas,
                sp1: spDia1,
                spSofaSit: spSofaSit,
                sp2Sofa: spDia2Sofa,
                sp3: spDia3,
                sp4Filhas: spDia4Filhas,
                pl: player,
                sanity: sanityEffects,
                tv: tvController,
                hud: objectiveHUD,
                audioMgr: audioManager,
                offDoor: officeDoor,
                lockedDoor: lockedCorridorDoor,
                mainLight: mainDirectionalLight,
                roomLights: allManagedInteriorLights.ToArray(),
                day3CircuitLight: circuitDeskLight,
                margaretObj: margaretJumpscareObj
            );
            dayManager.ConfigureExtendedLightingReferences(
                secondaryLights: secondaryLights,
                architecturalFills: fillLights,
                reflectionProbes: interiorProbes
            );
            dayManager.ConfigureSofaReferences(archRoot.transform.Find("Sofa_Sala"), spSofaStand);
            dayManager.ConfigureStudioLogoTexture(
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/UI/KENOMA.png"));

            casaBase.SetActive(true);
            propsDia1.SetActive(true);
            propsDia2.SetActive(false);
            propsDia3.SetActive(false);
            quartoFilhas.SetActive(false);

            AnimatedContainerInteractable[] allContainers = Object.FindObjectsByType<AnimatedContainerInteractable>(FindObjectsInactive.Include);
            for (int i = 0; i < allContainers.Length; i++)
            {
                if (allContainers[i] != null)
                {
                    allContainers[i].ResetForDay(GameDay.Day1);
                    EditorUtility.SetDirty(allContainers[i]);
                }
            }

            audioManager.AutoAssignImportedClips();
            EditorUtility.SetDirty(objectiveHUD);
            EditorUtility.SetDirty(settingsUI);
            EditorUtility.SetDirty(audioManager);
            EditorUtility.SetDirty(dayManager);
            EnsureHouseOcclusionArea(archRoot.transform);
            ConfigureStaticFlagsAndBatching(
                casaBase.transform,
                archRoot.transform,
                propsDia1.transform,
                propsDia2.transform,
                propsDia3.transform,
                quartoFilhas.transform);
            ConfigureOcclusionBakeSettings();
            archRoot.name = VersionMarkerName;
            EditorSceneManager.MarkSceneDirty(scene);

            if (saveScene)
            {
                AssetDatabase.SaveAssets();
                if (!Application.isPlaying)
                {
                    StaticOcclusionCulling.Compute();
                }
                EditorSceneManager.SaveOpenScenes();
            }

            if (logBanner)
            {
                Debug.Log($"[Canal 420] Residência {VersionMarkerName} reconstruída: geometria, colisores, nicho do cofre, janelas ShadowBox, câmera Dia 4 e navegação calibrados.");
            }
        }

        private static InspectableObject MakeSpot(
            Transform parent, string name, Vector3 pos, Vector3 colliderSize, Vector3 colliderCenter = default, Vector3 euler = default)
        {
            GameObject go = CreateContainer(parent, name);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);

            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = false;
            bc.center = colliderCenter;
            bc.size = colliderSize;

            InspectableObject insp = go.AddComponent<InspectableObject>();
            EditorUtility.SetDirty(insp);
            return insp;
        }

        private static void SetSerializedBool(Object target, string propertyName, bool value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                prop.boolValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetupBaseFloor(Transform casaBase, GreyboxPalette pal)
        {
            DestroyChildIfExists(casaBase, "Base1");
            DestroyChildIfExists(casaBase, "Piso_Frio_Cozinha");
            DestroyChildIfExists(casaBase, "Piso_Frio_Banheiro");
            DestroyChildIfExists(casaBase, "Piso_Madeira_Corredor");
            DestroyChildIfExists(casaBase, "Piso_Madeira_NorteLeste");

            // 5 lajes estritamente disjuntas em XZ, todas com superfície superior exata em Y = 0.000m (sem Z-fighting e sem degraus)
            CreateBox(casaBase, "Base1",
                new Vector3(1.82f, -0.07f, -2.77f),
                new Vector3(9.36f, 0.14f, 6.46f),
                pal.floorWood, hasCollider: true);

            CreateBox(casaBase, "Piso_Frio_Cozinha",
                new Vector3(-4.78f, -0.07f, -2.77f),
                new Vector3(3.84f, 0.14f, 6.46f),
                pal.floorTile, hasCollider: true);

            CreateBox(casaBase, "Piso_Frio_Banheiro",
                new Vector3(-5.07f, -0.07f, 4.15f),
                new Vector3(3.26f, 0.14f, 4.10f),
                pal.floorTile, hasCollider: true);

            CreateBox(casaBase, "Piso_Madeira_Corredor",
                new Vector3(-0.10f, -0.07f, 1.28f),
                new Vector3(13.20f, 0.14f, 1.64f),
                pal.floorWood, hasCollider: true);

            CreateBox(casaBase, "Piso_Madeira_NorteLeste",
                new Vector3(1.53f, -0.07f, 4.15f),
                new Vector3(9.94f, 0.14f, 4.10f),
                pal.floorWood, hasCollider: true);
        }

        private static readonly HashSet<string> SkippedTopArchitectureRoots = new HashSet<string>(System.StringComparer.Ordinal)
        {
            "Porta_Banheiro_Pivot",
            "Porta_Escritorio_Pivot",
            "Porta_Quarto_Cian_Pivot",
            "Porta_Quarto_Pivot",
            "Iluminacao_Interna",
            "Sondas_Reflexao_Internas",
            "TV_Sala",
            "Espelho_Banheiro",
            "Despertador_CriadoMudo",
            "Cadeira_Escritorio"
        };

        private static bool IsOcclusionBlockerName(string objectName)
        {
            return objectName == "Base1"
                || objectName == "Teto_Residencia"
                || objectName.StartsWith("Piso_", System.StringComparison.Ordinal)
                || objectName.StartsWith("Parede_", System.StringComparison.Ordinal);
        }

        private static bool IsDynamicSubtreeName(string objectName)
        {
            return objectName == "Visor_VCR"
                || objectName == "LED_Standby_TV"
                || objectName == "LED_Mensagens"
                || objectName == "DoisPontos_LED"
                || objectName == "Relogio_Estante_Parado"
                || objectName == "Torneira_Banheiro"
                || objectName == "Interruptor_Quarto_Filhas"
                || objectName == "Quadro_Normal_1"
                || objectName == "Quadro_Normal_2"
                || objectName == "Quadro_Distorcido_1"
                || objectName == "Quadro_Distorcido_2"
                || objectName == "Vulto_Sombra_Negra"
                || objectName == "Margaret_Jumpscare"
                || objectName == "Aparicao_Porta_Banheiro_D2"
                || objectName == "Watcher_Figura_D2"
                || objectName == "Watcher_Figura_D3"
                || objectName == "Corpo_Humanoide_Skinned"
                || objectName == "Cadeira_Escritorio"
                || objectName == "Cadeira_Jantar_Norte"
                || objectName == "Quadro_Margaret_Parede"
                || objectName == "Lascas_Gesso_Quadro_1"
                || objectName == "Lascas_Gesso_Quadro_2"
                || objectName == "Capsulas_Estimulante_Dia3"
                || objectName.StartsWith("Estado_Ligado", System.StringComparison.Ordinal)
                || objectName.StartsWith("Estado_Desligado", System.StringComparison.Ordinal)
                || objectName.StartsWith("Abajur_", System.StringComparison.Ordinal)
                || objectName.StartsWith("Luminaria_", System.StringComparison.Ordinal)
                || objectName.StartsWith("Arandela_", System.StringComparison.Ordinal)
                || objectName.StartsWith("Digito_LED_", System.StringComparison.Ordinal)
                || objectName.StartsWith("Disjuntor_", System.StringComparison.Ordinal);
        }

        private static bool HasNonNullObjectRef(SerializedProperty prop)
        {
            return prop != null && prop.objectReferenceValue != null;
        }

        private static bool IsDynamicInspectable(InspectableObject insp)
        {
            if (insp == null) return false;
            SerializedObject so = new SerializedObject(insp);
            SerializedProperty multiUseProp = so.FindProperty("isMultiUseConsumable");
            SerializedProperty stepVisualsProp = so.FindProperty("consumableStepVisuals");
            return HasNonNullObjectRef(so.FindProperty("visualToHideOnPickup"))
                || HasNonNullObjectRef(so.FindProperty("visualToRevealWhenUnlocked"))
                || HasNonNullObjectRef(so.FindProperty("alertLedRenderer"))
                || HasNonNullObjectRef(so.FindProperty("alertReelLeft"))
                || HasNonNullObjectRef(so.FindProperty("alertReelRight"))
                || HasNonNullObjectRef(so.FindProperty("linkedFaucet"))
                || (multiUseProp != null && multiUseProp.boolValue)
                || (stepVisualsProp != null && stepVisualsProp.isArray && stepVisualsProp.arraySize > 0);
        }

        private static bool HasDynamicOrInteractiveComponent(Transform target)
        {
            if (target.GetComponent<Light>() != null
                || target.GetComponent<ReflectionProbe>() != null
                || target.GetComponent<Animator>() != null
                || target.GetComponent<ParticleSystem>() != null)
            {
                return true;
            }

            MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour mb = behaviours[i];
                if (mb == null || mb is SofaTVProxy)
                    continue;

                if (mb is InspectableObject insp && !IsDynamicInspectable(insp))
                    continue;

                return true;
            }

            return false;
        }

        private static HashSet<Transform> CollectDynamicMovingTargets(Transform casaBase, params Transform[] additionalScopes)
        {
            var excluded = new HashSet<Transform>();
            if (casaBase != null)
                CollectDynamicTargetsInScope(casaBase, excluded);

            if (additionalScopes != null)
            {
                for (int i = 0; i < additionalScopes.Length; i++)
                {
                    if (additionalScopes[i] != null)
                        CollectDynamicTargetsInScope(additionalScopes[i], excluded);
                }
            }

            return excluded;
        }

        private static void CollectDynamicTargetsInScope(Transform rootScope, HashSet<Transform> excluded)
        {
            if (rootScope == null || excluded == null) return;

            DoorInteractable[] doors = rootScope.GetComponentsInChildren<DoorInteractable>(true);
            for (int i = 0; i < doors.Length; i++)
            {
                DoorInteractable d = doors[i];
                if (d == null) continue;
                excluded.Add(d.transform);
                SerializedObject so = new SerializedObject(d);
                AddSerializedTransformRef(so.FindProperty("doorPivot"), excluded);
                AddSerializedTransformRef(so.FindProperty("handlePivotA"), excluded);
                AddSerializedTransformRef(so.FindProperty("handlePivotB"), excluded);
            }

            AnimatedContainerInteractable[] containers = rootScope.GetComponentsInChildren<AnimatedContainerInteractable>(true);
            for (int i = 0; i < containers.Length; i++)
            {
                AnimatedContainerInteractable c = containers[i];
                if (c == null) continue;
                excluded.Add(c.transform);

                SerializedObject so = new SerializedObject(c);
                AddSerializedTransformRef(so.FindProperty("movingPart"), excluded);
                AddSerializedTransformRef(so.FindProperty("secondaryMovingPart"), excluded);
                AddSerializedTransformArrayRefs(so.FindProperty("syncedDynamicItems"), excluded);
            }

            FallingPictureFrame[] fallingFrames = rootScope.GetComponentsInChildren<FallingPictureFrame>(true);
            for (int i = 0; i < fallingFrames.Length; i++)
            {
                FallingPictureFrame f = fallingFrames[i];
                if (f == null) continue;
                excluded.Add(f.transform);
                SerializedObject so = new SerializedObject(f);
                AddSerializedTransformRef(so.FindProperty("hiddenSafeBox"), excluded);
                AddSerializedTransformRef(so.FindProperty("fallenTargetPoint"), excluded);
            }

            SafeBoxPuzzle[] safes = rootScope.GetComponentsInChildren<SafeBoxPuzzle>(true);
            for (int i = 0; i < safes.Length; i++)
            {
                SafeBoxPuzzle s = safes[i];
                if (s == null) continue;
                excluded.Add(s.transform);
                SerializedObject so = new SerializedObject(s);
                AddSerializedTransformRef(so.FindProperty("safeDoorPivot"), excluded);
                AddSerializedTransformRef(so.FindProperty("safeHandlePivot"), excluded);
                AddSerializedTransformRef(so.FindProperty("digitalVisorRenderer"), excluded);
                AddSerializedTransformRef(so.FindProperty("brassKeyVisualInside"), excluded);
            }

            HallucinationTrigger[] triggers = rootScope.GetComponentsInChildren<HallucinationTrigger>(true);
            for (int i = 0; i < triggers.Length; i++)
            {
                HallucinationTrigger ht = triggers[i];
                if (ht == null) continue;
                excluded.Add(ht.transform);
                SerializedObject so = new SerializedObject(ht);
                AddSerializedTransformRef(so.FindProperty("shadowObject"), excluded);
                AddSerializedTransformArrayRefs(so.FindProperty("normalObjectsToHide"), excluded);
                AddSerializedTransformArrayRefs(so.FindProperty("alteredObjectsToShow"), excluded);
            }

            WatcherHallucination[] watchers = rootScope.GetComponentsInChildren<WatcherHallucination>(true);
            for (int i = 0; i < watchers.Length; i++)
            {
                WatcherHallucination wh = watchers[i];
                if (wh == null) continue;
                excluded.Add(wh.transform);
                SerializedObject so = new SerializedObject(wh);
                AddSerializedTransformRef(so.FindProperty("silhouetteObject"), excluded);
            }

            HumanoidSpasmAnimator[] spasms = rootScope.GetComponentsInChildren<HumanoidSpasmAnimator>(true);
            for (int i = 0; i < spasms.Length; i++)
            {
                if (spasms[i] != null) excluded.Add(spasms[i].transform);
            }

            AnalogClockController[] clocks = rootScope.GetComponentsInChildren<AnalogClockController>(true);
            for (int i = 0; i < clocks.Length; i++)
            {
                AnalogClockController clk = clocks[i];
                if (clk == null) continue;
                excluded.Add(clk.transform);
                SerializedObject so = new SerializedObject(clk);
                AddSerializedTransformRef(so.FindProperty("hourHandPivot"), excluded);
                AddSerializedTransformRef(so.FindProperty("minuteHandPivot"), excluded);
                AddSerializedTransformRef(so.FindProperty("secondHandPivot"), excluded);
            }

            InspectableObject[] inspectables = rootScope.GetComponentsInChildren<InspectableObject>(true);
            for (int i = 0; i < inspectables.Length; i++)
            {
                InspectableObject insp = inspectables[i];
                if (!IsDynamicInspectable(insp)) continue;
                excluded.Add(insp.transform);
                SerializedObject so = new SerializedObject(insp);
                AddSerializedTransformRef(so.FindProperty("visualToHideOnPickup"), excluded);
                AddSerializedTransformRef(so.FindProperty("visualToRevealWhenUnlocked"), excluded);
                AddSerializedTransformRef(so.FindProperty("alertLedRenderer"), excluded);
                AddSerializedTransformRef(so.FindProperty("alertReelLeft"), excluded);
                AddSerializedTransformRef(so.FindProperty("alertReelRight"), excluded);
                AddSerializedTransformRef(so.FindProperty("linkedFaucet"), excluded);
                AddSerializedTransformArrayRefs(so.FindProperty("consumableStepVisuals"), excluded);
            }
        }

        private static void AddSerializedTransformRef(SerializedProperty prop, HashSet<Transform> set)
        {
            if (prop == null || set == null || prop.objectReferenceValue == null)
                return;

            if (prop.objectReferenceValue is Transform tr && tr != null)
            {
                set.Add(tr);
            }
            else if (prop.objectReferenceValue is GameObject go && go != null)
            {
                set.Add(go.transform);
            }
            else if (prop.objectReferenceValue is Component comp && comp != null)
            {
                set.Add(comp.transform);
            }
        }

        private static void AddSerializedTransformArrayRefs(SerializedProperty arrayProp, HashSet<Transform> set)
        {
            if (arrayProp == null || !arrayProp.isArray || set == null)
                return;

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                AddSerializedTransformRef(arrayProp.GetArrayElementAtIndex(i), set);
            }
        }

        private static void ClearBatchAndOcclusionFlagsRecursive(Transform node)
        {
            if (node == null) return;

            if ((node.GetComponent<MeshFilter>() != null || node.GetComponent<MeshRenderer>() != null)
                && node.GetComponent<Light>() == null
                && node.GetComponent<ReflectionProbe>() == null)
            {
                StaticEditorFlags cur = GameObjectUtility.GetStaticEditorFlags(node.gameObject);
                StaticEditorFlags cleared = cur & ~(StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                if (cleared != cur)
                {
                    GameObjectUtility.SetStaticEditorFlags(node.gameObject, cleared);
                }
            }

            for (int i = 0; i < node.childCount; i++)
            {
                ClearBatchAndOcclusionFlagsRecursive(node.GetChild(i));
            }
        }

        private static void MarkSubtreeStaticFlagsRecursive(Transform node, StaticEditorFlags flags, HashSet<Transform> excluded)
        {
            if (node == null)
            {
                return;
            }

            if ((excluded != null && excluded.Contains(node))
                || IsDynamicSubtreeName(node.name)
                || HasDynamicOrInteractiveComponent(node))
            {
                ClearBatchAndOcclusionFlagsRecursive(node);
                return;
            }

            if (node.GetComponent<MeshFilter>() != null || node.GetComponent<MeshRenderer>() != null)
            {
                GameObjectUtility.SetStaticEditorFlags(node.gameObject, flags);
            }

            for (int i = 0; i < node.childCount; i++)
            {
                MarkSubtreeStaticFlagsRecursive(node.GetChild(i), flags, excluded);
            }
        }

        private static void ConfigureStaticFlagsAndBatching(Transform casaBase, Transform archRoot, params Transform[] dailyRoots)
        {
            const StaticEditorFlags OccluderFlags =
                StaticEditorFlags.OccluderStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;
            const StaticEditorFlags StaticFurnitureFlags =
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;
            const StaticEditorFlags DailyStaticFlags =
                StaticEditorFlags.BatchingStatic;

            HashSet<Transform> excluded = CollectDynamicMovingTargets(casaBase, dailyRoots);

            if (casaBase != null)
            {
                for (int i = 0; i < casaBase.childCount; i++)
                {
                    Transform child = casaBase.GetChild(i);
                    if (child == archRoot || SkippedTopArchitectureRoots.Contains(child.name))
                        continue;

                    if (IsOcclusionBlockerName(child.name))
                    {
                        MarkSubtreeStaticFlagsRecursive(child, OccluderFlags, excluded);
                    }
                }
            }

            if (archRoot != null)
            {
                for (int i = 0; i < archRoot.childCount; i++)
                {
                    Transform child = archRoot.GetChild(i);
                    if (SkippedTopArchitectureRoots.Contains(child.name))
                    {
                        ClearBatchAndOcclusionFlagsRecursive(child);
                        continue;
                    }

                    StaticEditorFlags targetFlags = IsOcclusionBlockerName(child.name)
                        ? OccluderFlags
                        : StaticFurnitureFlags;
                    MarkSubtreeStaticFlagsRecursive(child, targetFlags, excluded);
                }
            }

            if (dailyRoots != null)
            {
                for (int i = 0; i < dailyRoots.Length; i++)
                {
                    Transform dailyRoot = dailyRoots[i];
                    if (dailyRoot != null)
                    {
                        ClearBatchAndOcclusionFlagsRecursive(dailyRoot);
                        MarkSubtreeStaticFlagsRecursive(dailyRoot, DailyStaticFlags, excluded);
                    }
                }
            }
        }

        private static void EnsureHouseOcclusionArea(Transform archRoot)
        {
            if (archRoot == null) return;

            Transform existing = archRoot.Find("Occlusion_Volume_Casa");
            GameObject volumeObj = existing != null ? existing.gameObject : CreateContainer(archRoot, "Occlusion_Volume_Casa");
            volumeObj.transform.localPosition = Vector3.zero;
            volumeObj.transform.localRotation = Quaternion.identity;
            volumeObj.transform.localScale = Vector3.one;

            OcclusionArea area = GetOrAdd<OcclusionArea>(volumeObj);
            area.center = new Vector3(-0.10f, 1.40f, 0.10f);
            area.size = new Vector3(13.60f, 3.40f, 12.60f);
            SetSerializedBool(area, "m_IsViewVolume", true);
            EditorUtility.SetDirty(area);
        }

        private static void ConfigureOcclusionBakeSettings()
        {
            StaticOcclusionCulling.smallestOccluder = 0.50f;
            StaticOcclusionCulling.smallestHole = 0.10f;
            StaticOcclusionCulling.backfaceThreshold = 100f;
        }

        private static void BuildWallXWithOpenings(
            Transform parent, string name, float xMin, float xMax, float z, Material mat, params Opening[] openings)
        {
            float cursor = xMin;
            for (int i = 0; i < openings.Length; i++)
            {
                if (openings[i].a > cursor)
                    CreateWallX(parent, $"{name}_{i}", cursor, openings[i].a, z, H, T, mat);
                if (openings[i].bottom > 0.005f)
                    CreateLintelX(parent, $"{name}_Peitoril_{i}", openings[i].a, openings[i].b, z, 0f, openings[i].bottom, T, mat);
                if (openings[i].top < H - 0.005f)
                    CreateLintelX(parent, $"{name}_Verga_{i}", openings[i].a, openings[i].b, z, openings[i].top, H, T, mat);
                cursor = openings[i].b;
            }

            if (xMax > cursor)
                CreateWallX(parent, $"{name}_{openings.Length}", cursor, xMax, z, H, T, mat);
        }

        private static void BuildWallZWithOpenings(
            Transform parent, string name, float x, float zMin, float zMax, Material mat, params Opening[] openings)
        {
            float cursor = zMin;
            for (int i = 0; i < openings.Length; i++)
            {
                if (openings[i].a > cursor)
                    CreateWallZ(parent, $"{name}_{i}", x, cursor, openings[i].a, H, T, mat);
                if (openings[i].bottom > 0.005f)
                    CreateLintelZ(parent, $"{name}_Peitoril_{i}", x, openings[i].a, openings[i].b, 0f, openings[i].bottom, T, mat);
                if (openings[i].top < H - 0.005f)
                    CreateLintelZ(parent, $"{name}_Verga_{i}", x, openings[i].a, openings[i].b, openings[i].top, H, T, mat);
                cursor = openings[i].b;
            }

            if (zMax > cursor)
                CreateWallZ(parent, $"{name}_{openings.Length}", x, cursor, zMax, H, T, mat);
        }

        private static void BuildWalls(Transform root, GreyboxPalette pal)
        {
            Material wall = pal.wallPlaster;
            Material rodapeMat = activeBaseboardMaterial != null ? activeBaseboardMaterial : pal.baseboardWood;

            // Laje única do teto selando exatamente o perímetro retangular X: [-6.70..+6.50] x Z: [-6.00..+6.20]
            CreateBox(root, "Teto_Residencia", new Vector3(-0.10f, H + 0.07f, 0.10f), new Vector3(13.20f, 0.14f, 12.20f), pal.ceiling);

            // Envelope externo retangular selado com vãos reais de porta e janelas (0 sobreposição de quinas)
            BuildWallZWithOpenings(root, "Parede_Oeste", -6.62f, -6.00f, 6.20f, wall,
                new Opening(-3.76f, -2.44f, 1.02f, 2.06f),
                new Opening(0.78f, 1.78f, DoorH));
            BuildWallZWithOpenings(root, "Parede_Leste", 6.42f, -6.00f, 6.20f, wall,
                new Opening(2.59f, 3.99f, 1.03f, 2.13f));
            BuildWallXWithOpenings(root, "Parede_Sul", -6.54f, 6.34f, -5.92f, wall,
                new Opening(-0.34f, 0.90f, 1.05f, 2.03f));
            BuildWallXWithOpenings(root, "Parede_Norte", -6.54f, 6.34f, 6.12f, wall,
                new Opening(-2.90f, -1.66f, 1.05f, 2.03f));

            // Paredes estruturais do corredor em L e divisórias internas (0 T-junction overlap)
            BuildWallXWithOpenings(root, "Parede_CorredorSul", -6.54f, 6.34f, 0.46f, wall,
                new Opening(-5.40f, -4.40f, DoorH),
                new Opening(-1.48f, 0.20f, ArchH),
                new Opening(1.50f, 2.50f, DoorH));

            BuildWallXWithOpenings(root, "Parede_CorredorNorte", -6.54f, 1.04f, 2.10f, wall,
                new Opening(-5.42f, -4.44f, DoorH),
                new Opening(-2.26f, -1.24f, DoorH));

            CreateWallZ(root, "Parede_Divisoria_Banheiro_Quarto", -3.44f, 2.18f, 6.04f, H, T, wall);
            CreateWallZ(root, "Parede_Quarto_Corredor", 0.96f, 2.18f, 6.04f, H, T, wall);
            BuildWallZWithOpenings(root, "Parede_Corredor_Filhas", 2.60f, 0.54f, 6.04f, wall,
                new Opening(1.04f, 1.52f, 1.27f, 1.69f),
                new Opening(4.10f, 5.10f, DoorH));
            // Chapa de fundo selada do nicho do cofre na face leste (X = 2.64..2.68f, zero vazamento para Quarto_Filhas)
            CreateBox(root, "Parede_Corredor_Filhas_NichoFundo",
                new Vector3(2.660f, 1.480f, 1.280f),
                new Vector3(0.040f, 0.420f, 0.480f),
                wall, hasCollider: true);

            BuildWallZWithOpenings(root, "Parede_Cozinha_Sala", -2.86f, -5.84f, 0.38f, wall,
                new Opening(-3.70f, -1.62f, ArchH));
            CreateWallZ(root, "Parede_Sala_Escritorio", 1.40f, -5.84f, 0.38f, H, T, wall);

            // Batentes embutidos nos vãos com guarnição limpa acima do rodapé, rebaixo para folha e contra-testa metálica
            BuildDoorFrameX(root, "Batente_Banheiro", -5.42f, -4.44f, 2.10f, DoorH, pal.baseboardWood, pal.metalBrass, swingDirSign: 1f, latchOnMaxSide: true);
            BuildDoorFrameX(root, "Batente_Quarto", -2.26f, -1.24f, 2.10f, DoorH, pal.baseboardWood, pal.metalBrass, swingDirSign: 1f, latchOnMaxSide: true);
            BuildDoorFrameX(root, "Batente_Escritorio", 1.50f, 2.50f, 0.46f, DoorH, pal.baseboardWood, pal.metalBrass, swingDirSign: -1f, latchOnMaxSide: true, minSideTrimWidth: 0.018f);
            BuildDoorFrameX(root, "Batente_Copa", -5.40f, -4.40f, 0.46f, DoorH, pal.baseboardWood, pal.metalBrass, isArchway: true);
            BuildDoorFrameX(root, "Batente_ArcoSala", -1.48f, 0.20f, 0.46f, ArchH, pal.baseboardWood, pal.metalBrass, isArchway: true);
            BuildDoorFrameZ(root, "Batente_ArcoCozinha", -2.86f, -3.70f, -1.62f, ArchH, pal.baseboardWood, pal.metalBrass, isArchway: true);
            BuildDoorFrameZ(root, "Batente_PortaTrancada", 2.60f, 4.10f, 5.10f, DoorH, pal.baseboardWood, pal.metalBrass, swingDirSign: 1f, latchOnMaxSide: true);
            BuildFlushExteriorDoorFrameZ(root, "Batente_PortaRua", -6.700f, -6.530f, 0.78f, 1.78f, DoorH, pal.baseboardWood, pal.metalBrass);

            // Soleiras niveladas em 3mm (0.000..0.003m) sob todas as portas e passagens, mantendo 3mm livres sob as folhas (0.006m)
            CreateBox(root, "Soleira_PortaRua", new Vector3(-6.62f, 0.0015f, 1.28f), new Vector3(0.160f, 0.003f, 0.920f), pal.baseboardWood, hasCollider: false);
            CreateBox(root, "Soleira_Banheiro", new Vector3(-4.93f, 0.0015f, 2.10f), new Vector3(0.90f, 0.003f, 0.16f), pal.baseboardWood, hasCollider: false);
            CreateBox(root, "Soleira_Quarto", new Vector3(-1.75f, 0.0015f, 2.10f), new Vector3(0.94f, 0.003f, 0.16f), pal.baseboardWood, hasCollider: false);
            CreateBox(root, "Soleira_Copa", new Vector3(-4.90f, 0.0015f, 0.46f), new Vector3(0.92f, 0.003f, 0.16f), pal.baseboardWood, hasCollider: false);
            CreateBox(root, "Soleira_Escritorio", new Vector3(2.00f, 0.0015f, 0.46f), new Vector3(0.92f, 0.003f, 0.16f), pal.baseboardWood, hasCollider: false);
            CreateBox(root, "Soleira_PortaTrancada", new Vector3(2.60f, 0.0015f, 4.60f), new Vector3(0.16f, 0.003f, 0.92f), pal.baseboardWood, hasCollider: false);
            CreateBox(root, "Soleira_ArcoCozinha", new Vector3(-2.86f, 0.0015f, -2.66f), new Vector3(0.16f, 0.003f, 2.00f), pal.baseboardWood, hasCollider: false);

            // Rodapés internos por cômodo (0 Z-fighting nas quinas, 0 vazamento na fachada externa)
            BuildRoomWallBaseboards(root, "Rodape_Sala", -2.78f, 1.32f, -5.84f, 0.38f, rodapeMat,
                southOpenings: null,
                northOpenings: new[] { new Opening(-1.48f, 0.20f, ArchH) },
                westOpenings: new[] { new Opening(-3.70f, -1.62f, ArchH) },
                eastOpenings: null);
            BuildRoomWallBaseboards(root, "Rodape_Cozinha", -6.54f, -2.94f, -5.84f, 0.38f, rodapeMat,
                southOpenings: null,
                northOpenings: new[] { new Opening(-5.40f, -4.40f, DoorH) },
                westOpenings: null,
                eastOpenings: new[] { new Opening(-3.70f, -1.62f, ArchH) });
            BuildRoomWallBaseboards(root, "Rodape_Escritorio", 1.48f, 6.34f, -5.84f, 0.38f, rodapeMat,
                southOpenings: null,
                northOpenings: new[] { new Opening(1.50f, 2.50f, DoorH) },
                westOpenings: null,
                eastOpenings: null);
            BuildRoomWallBaseboards(root, "Rodape_Banheiro", -6.54f, -3.52f, 2.18f, 6.04f, rodapeMat,
                southOpenings: new[] { new Opening(-5.42f, -4.44f, DoorH) },
                northOpenings: new[] { new Opening(-6.54f, -5.410f, H) },
                westOpenings: new[] { new Opening(4.910f, 6.04f, H) },
                eastOpenings: null);
            BuildRoomWallBaseboards(root, "Rodape_Quarto", -3.36f, 0.88f, 2.18f, 6.04f, rodapeMat,
                southOpenings: new[] { new Opening(-2.26f, -1.24f, DoorH) },
                northOpenings: null,
                westOpenings: null,
                eastOpenings: null);
            BuildRoomWallBaseboards(root, "Rodape_QuartoFilhas", 2.68f, 6.34f, 0.54f, 6.04f, rodapeMat,
                southOpenings: null,
                northOpenings: null,
                westOpenings: new[] { new Opening(4.10f, 5.10f, DoorH) },
                eastOpenings: null);

            // Rodapés do corredor em L (cada trecho aparado contra paredes Z perpendiculares)
            BuildBaseboardSegmentWithOpeningsZ(root, "Rodape_Corredor_Oeste", -6.54f, 1f, 0.54f, 2.02f, rodapeMat, new Opening(0.78f, 1.78f, DoorH));
            BuildBaseboardSegmentWithOpeningsX(root, "Rodape_Corredor_Sul", -6.526f, 2.506f, 0.54f, 1f, rodapeMat,
                new Opening(-5.40f, -4.40f, DoorH), new Opening(-1.48f, 0.20f, ArchH), new Opening(1.50f, 2.50f, DoorH));
            BuildBaseboardSegmentWithOpeningsX(root, "Rodape_Corredor_Norte", -6.526f, 1.04f, 2.02f, -1f, rodapeMat,
                new Opening(-5.42f, -4.44f, DoorH), new Opening(-2.26f, -1.24f, DoorH));
            BuildBaseboardSegmentWithOpeningsZ(root, "Rodape_Corredor_AlaOeste", 1.04f, 1f, 2.02f, 6.04f, rodapeMat);
            BuildBaseboardSegmentWithOpeningsX(root, "Rodape_Corredor_AlaNorte", 1.054f, 2.506f, 6.04f, -1f, rodapeMat);
            BuildBaseboardSegmentWithOpeningsZ(root, "Rodape_Corredor_AlaLeste", 2.52f, -1f, 0.54f, 6.04f, rodapeMat, new Opening(4.10f, 5.10f, DoorH));

            // Janelas ancoradas diretamente nas faces internas das paredes externas + batentes nos vãos e ShadowBoxes externas estanques
            BuildShutteredWindowX(root, "Persiana_Sala_Sul", 0.28f, -5.84f, 1f, 1.54f, pal, withCurtains: true);
            BuildWindowRecessAndShadowBoxX(root, "SalaSul", -0.34f, 0.90f, 1.05f, 2.03f, -5.92f, -1f, pal);

            BuildShutteredWindowX(root, "Persiana_Quarto_Norte", -2.28f, 6.04f, -1f, 1.54f, pal, withCurtains: true);
            BuildWindowRecessAndShadowBoxX(root, "QuartoNorte", -2.90f, -1.66f, 1.05f, 2.03f, 6.12f, 1f, pal);

            BuildShutteredWindowZ(root, "Persiana_Cozinha_Oeste", -6.54f, 1f, -3.10f, 1.54f, pal);
            BuildWindowRecessAndShadowBoxZ(root, "CozinhaOeste", -3.76f, -2.44f, 1.02f, 2.06f, -6.62f, -1f, pal);

            BuildWindowRecessAndShadowBoxZ(root, "QuartoFilhas", 2.59f, 3.99f, 1.03f, 2.13f, 6.42f, 1f, pal);

            // Sancas aparadas por cômodo (0 Z-fighting nas quinas, e corredor L segue apenas paredes reais)
            BuildCrownMouldingRoom(root, "Sanca_Sala", -2.78f, 1.32f, -5.84f, 0.38f, H, pal.ceiling);
            BuildCrownMouldingRoom(root, "Sanca_Cozinha", -6.54f, -2.94f, -5.84f, 0.38f, H, pal.ceiling);
            BuildCrownMouldingRoom(root, "Sanca_Quarto", -3.36f, 0.88f, 2.18f, 6.04f, H, pal.ceiling);
            BuildCrownMouldingRoom(root, "Sanca_Banheiro", -6.54f, -3.52f, 2.18f, 6.04f, H, pal.ceiling);
            BuildCrownMouldingRoom(root, "Sanca_Escritorio", 1.48f, 6.34f, -5.84f, 0.38f, H, pal.ceiling);
            BuildCorridorLCrownMoulding(root, "Sanca_Corredor", H, pal.ceiling);

            // Interruptores e tomadas rentes às novas faces internas
            BuildWallLightSwitch(root, "Interruptor_Entrada", new Vector3(-6.54f, 1.15f, 0.68f), 90f, pal, doubleGang: true);
            BuildWallLightSwitch(root, "Interruptor_Corredor_Meio", new Vector3(0.42f, 1.15f, 0.54f), 0f, pal, doubleGang: true);
            BuildWallLightSwitch(root, "Interruptor_Sala", new Vector3(0.46f, 1.15f, 0.38f), 180f, pal);
            BuildWallLightSwitch(root, "Interruptor_Cozinha", new Vector3(-4.18f, 1.15f, 0.38f), 180f, pal);
            BuildWallLightSwitch(root, "Interruptor_Banheiro", new Vector3(-4.22f, 1.15f, 2.18f), 0f, pal);
            BuildWallLightSwitch(root, "Interruptor_Quarto", new Vector3(-1.02f, 1.15f, 2.18f), 0f, pal);
            BuildWallLightSwitch(root, "Interruptor_Escritorio", new Vector3(2.72f, 1.15f, 0.38f), 180f, pal);
            BuildWallLightSwitch(root, "Interruptor_Quarto_Filhas", new Vector3(2.68f, 1.15f, 3.92f), 90f, pal);

            BuildWallPowerOutlet(root, "Tomada_TV_Sala", new Vector3(-0.24f, 0.34f, -5.84f), 0f, pal, hasPlugConnected: true, hasRoutedCable: true);
            BuildRoutedWallCable(root, "Cabo_Tomada_TV_Sala", "Mesh_Cabo_TV_Sala", new[]
            {
                new Vector3(-0.240f, 0.322f, -5.814f),
                new Vector3(-0.240f, 0.270f, -5.816f),
                new Vector3(-0.240f, 0.024f, -5.816f),
                new Vector3(-0.258f, 0.006f, -5.816f),
                new Vector3(-0.760f, 0.006f, -5.816f),
                new Vector3(-0.780f, 0.024f, -5.800f),
                new Vector3(-0.780f, 0.720f, -5.785f),
                new Vector3(-0.780f, 0.725f, -5.670f)
            }, pal.shadowBlack);

            BuildWallPowerOutlet(root, "Tomada_Luminaria_Sala", new Vector3(1.32f, 0.34f, -3.85f), -90f, pal, hasPlugConnected: true, hasRoutedCable: true);
            BuildRoutedWallCable(root, "Cabo_Tomada_Luminaria_Sala", "Mesh_Cabo_Luminaria_Sala", new[]
            {
                new Vector3(1.294f, 0.322f, -3.850f),
                new Vector3(1.296f, 0.270f, -3.850f),
                new Vector3(1.296f, 0.024f, -3.850f),
                new Vector3(1.296f, 0.006f, -3.830f),
                new Vector3(1.296f, 0.006f, -3.720f),
                new Vector3(1.276f, 0.006f, -3.695f),
                new Vector3(1.195f, 0.008f, -3.665f),
                new Vector3(1.170f, 0.014f, -3.660f)
            }, pal.shadowBlack);

            BuildWallPowerOutlet(root, "Tomada_Telefone_Corredor", new Vector3(-2.78f, 0.34f, 2.02f), 180f, pal, hasPlugConnected: true, hasRoutedCable: true);
            BuildRoutedWallCable(root, "Cabo_Tomada_Telefone_Corredor", "Mesh_Cabo_Telefone_Corredor", new[]
            {
                new Vector3(-2.780f, 0.322f, 1.994f),
                new Vector3(-2.780f, 0.270f, 1.996f),
                new Vector3(-2.780f, 0.024f, 1.996f),
                new Vector3(-2.798f, 0.006f, 1.996f),
                new Vector3(-3.057f, 0.006f, 1.996f),
                new Vector3(-3.075f, 0.024f, 1.996f),
                new Vector3(-3.075f, 0.752f, 1.996f),
                new Vector3(-3.075f, 0.789f, 1.982f),
                new Vector3(-3.075f, 0.789f, 1.912f)
            }, pal.metalDark);

            BuildWallPowerOutlet(root, "Tomada_Mesa_Escritorio", new Vector3(6.34f, 0.34f, -3.65f), -90f, pal, hasPlugConnected: true, hasRoutedCable: true);
            BuildRoutedWallCable(root, "Cabo_Tomada_Mesa_Escritorio", "Mesh_Cabo_Mesa_Escritorio", new[]
            {
                new Vector3(6.314f, 0.322f, -3.650f),
                new Vector3(6.316f, 0.270f, -3.650f),
                new Vector3(6.316f, 0.024f, -3.650f),
                new Vector3(6.316f, 0.006f, -3.632f),
                new Vector3(6.316f, 0.006f, -3.208f),
                new Vector3(6.298f, 0.010f, -3.190f),
                new Vector3(6.235f, 0.016f, -3.190f),
                new Vector3(6.158f, 0.016f, -3.190f),
                new Vector3(6.158f, 0.036f, -3.190f),
                new Vector3(6.158f, 0.785f, -3.190f),
                new Vector3(6.135f, 0.790f, -3.190f),
                new Vector3(6.008f, 0.790f, -3.190f)
            }, pal.shadowBlack);

            BuildWallPowerOutlet(root, "Tomada_Cabeceira_Cian", new Vector3(0.02f, 0.34f, 6.04f), 180f, pal, hasPlugConnected: true, hasRoutedCable: true);
            BuildRoutedWallCable(root, "Cabo_Tomada_Cabeceira_Cian", "Mesh_Cabo_Cabeceira_Cian", new[]
            {
                new Vector3(0.020f, 0.322f, 6.014f),
                new Vector3(0.020f, 0.270f, 6.016f),
                new Vector3(0.020f, 0.024f, 6.016f),
                new Vector3(0.038f, 0.006f, 6.016f),
                new Vector3(0.442f, 0.006f, 6.016f),
                new Vector3(0.460f, 0.024f, 6.016f),
                new Vector3(0.460f, 0.548f, 6.016f),
                new Vector3(0.460f, 0.566f, 6.002f),
                new Vector3(0.460f, 0.566f, 5.915f)
            }, pal.metalDark);

            BuildWallPowerOutlet(root, "Tomada_Geladeira", new Vector3(-6.5295f, 0.42f, -4.55f), 90f, pal, hasPlugConnected: true, hasRoutedCable: true);
            BuildRoutedWallCable(root, "Cabo_Tomada_Geladeira", "Mesh_Cabo_Geladeira", new[]
            {
                new Vector3(-6.503f, 0.402f, -4.550f),
                new Vector3(-6.508f, 0.350f, -4.550f),
                new Vector3(-6.512f, 0.024f, -4.550f),
                new Vector3(-6.512f, 0.006f, -4.568f),
                new Vector3(-6.512f, 0.006f, -5.682f),
                new Vector3(-6.512f, 0.024f, -5.700f),
                new Vector3(-6.512f, 0.160f, -5.700f),
                new Vector3(-6.480f, 0.160f, -5.720f)
            }, pal.shadowBlack);
        }

        private static DoorInteractable BuildInteriorDoors(Transform root, GreyboxPalette pal)
        {
            // Vão Banheiro [-5.42..-4.44] -> luz [-5.38..-4.48] = 0.900m -> folha 0.894m
            CreateFunctionalDoor(root, "Porta_Banheiro_Pivot", new Vector3(-5.377f, 0f, 2.10f), 0f, -86f, false, false, pal, 0.894f);
            BuildStyledDoorStop(root, "Batedor_Porta_Banheiro", new Vector3(-5.375f, 0f, 2.88f), pal);

            // Vão Quarto Casal [-2.26..-1.24] -> luz [-2.22..-1.28] = 0.940m -> folha 0.934m
            CreateFunctionalDoor(root, "Porta_Quarto_Cian_Pivot", new Vector3(-2.217f, 0f, 2.10f), 0f, -86f, false, false, pal, 0.934f);
            BuildStyledDoorStop(root, "Batedor_Porta_Quarto", new Vector3(-2.215f, 0f, 2.88f), pal);

            // Vão Escritório [+1.50..+2.50] -> luz [+1.54..+2.46] = 0.920m -> folha 0.914m
            DoorInteractable officeDoor = CreateFunctionalDoor(
                root, "Porta_Escritorio_Pivot", new Vector3(1.543f, 0f, 0.46f), 0f, 86f, false, false, pal, 0.914f);

            officeDoor.ConfigureDoor(
                pivot: officeDoor.transform,
                targetOpenAngle: 86f,
                locked: false,
                allowSafeKey: false,
                lockedMsg: "Trancada. Passei a chave ontem à noite e não lembro onde deixei.",
                unlockedMsg: "Cheiro de papel velho...",
                keyItemId: "chave_escritorio",
                doorLeafWidth: 0.914f
            );
            EditorUtility.SetDirty(officeDoor);
            BuildRubberDoorstop(root, "Batedor_Porta_Escritorio", new Vector3(1.555f, 0f, -0.42f), pal);
            return officeDoor;
        }

        private static DoorInteractable SetupLockedCorridorDoor(Transform casaBase, GreyboxPalette pal)
        {
            // Vão Quarto Filhas em Parede_Corredor_Filhas (X=+2.60, Z=[+4.10..+5.10]) -> luz [+4.14..+5.06] = 0.920m -> folha 0.914m
            const float leafW = 0.914f;
            float leafH = DoorH - 0.050f; // 6mm folga do piso (3mm sobre soleira) + 4mm abaixo do batente superior
            float leafCenterY = 0.006f + leafH * 0.5f;

            GameObject pivotObj = new GameObject("Porta_Quarto_Pivot");
            pivotObj.transform.SetParent(casaBase, false);
            pivotObj.transform.localPosition = new Vector3(2.60f, 0f, 4.143f);
            pivotObj.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            pivotObj.transform.localScale = Vector3.one;

            GameObject leafObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leafObj.name = "Folha_Porta";
            leafObj.transform.SetParent(pivotObj.transform, false);
            leafObj.transform.localPosition = new Vector3(leafW * 0.5f, leafCenterY, 0f);
            leafObj.transform.localRotation = Quaternion.identity;
            leafObj.transform.localScale = new Vector3(leafW, leafH, 0.048f);
            AssignMaterial(leafObj, pal.doorWood);

            BoxCollider leafCol = GetOrAdd<BoxCollider>(leafObj);
            leafCol.isTrigger = false;
            leafCol.center = Vector3.zero;
            leafCol.size = new Vector3(1.02f, 1.00f, 2.4f);

            DecoratePanelDoorLeaf(pivotObj.transform, pal, includeKeyhole: true, leafWidth: leafW, leafHeight: leafH, leafCenterY: leafCenterY, leafThickness: 0.048f);

            DoorInteractable door = GetOrAdd<DoorInteractable>(pivotObj);
            door.ConfigureDoor(
                pivot: pivotObj.transform,
                targetOpenAngle: 92f,
                locked: true,
                allowSafeKey: false,
                lockedMsg: "Trancada por fora faz tempo. Não tenho nada pra pegar aí dentro.",
                unlockedMsg: "...",
                doorLeafWidth: leafW
            );
            EditorUtility.SetDirty(door);
            return door;
        }

        private static TVController BuildLivingRoom(Transform root, Transform casaBase, GreyboxPalette pal)
        {
            // Tapete centralizado sob a mesa de centro (Z=-4.61..-2.91), camadas contínuas sem Z-fighting até Y=0.0090m sob os pés da mesa
            GameObject rug = CreateContainer(root, "Tapete_Sala");
            rug.transform.localPosition = new Vector3(-0.84f, 0f, -3.76f);
            CreateDetailBox(rug.transform, "Borda_Externa", new Vector3(0f, 0.0020f, 0f), new Vector3(2.14f, 0.0040f, 1.70f), pal.rugBorderBurgundy);
            CreateDetailBox(rug.transform, "Debrum_Sala_N", new Vector3(0f, 0.0052f,  0.71f), new Vector3(1.94f, 0.0024f, 0.08f), pal.rugAccentOlive);
            CreateDetailBox(rug.transform, "Debrum_Sala_S", new Vector3(0f, 0.0052f, -0.71f), new Vector3(1.94f, 0.0024f, 0.08f), pal.rugAccentOlive);
            CreateDetailBox(rug.transform, "Debrum_Sala_O", new Vector3(-0.93f, 0.0052f, 0f), new Vector3(0.08f, 0.0024f, 1.34f), pal.rugAccentOlive);
            CreateDetailBox(rug.transform, "Debrum_Sala_L", new Vector3( 0.93f, 0.0052f, 0f), new Vector3(0.08f, 0.0024f, 1.34f), pal.rugAccentOlive);
            CreateDetailBox(rug.transform, "Campo_Tecido", new Vector3(0f, 0.0077f, 0f), new Vector3(1.78f, 0.0026f, 1.34f), pal.rugFabric);
            CreateDetailBox(rug.transform, "Medallao_Centro", new Vector3(0f, 0.0102f, 0f), new Vector3(0.86f, 0.0024f, 0.40f), pal.rugAccentOlive);
            for (int rf = -4; rf <= 4; rf++)
            {
                float fz = rf * 0.19f;
                CreateDetailBox(rug.transform, $"Franja_Tapete_Sala_O_{rf + 4}", new Vector3(-1.090f, 0.0014f, fz), new Vector3(0.040f, 0.0028f, 0.024f), pal.paperWhite);
                CreateDetailBox(rug.transform, $"Franja_Tapete_Sala_L_{rf + 4}", new Vector3( 1.090f, 0.0014f, fz), new Vector3(0.040f, 0.0028f, 0.024f), pal.paperWhite);
            }

            // Rack de TV alinhado ao sul (Z=-5.49f) com portas animadas emolduradas, VCR detalhado e interior vivo
            GameObject rack = CreateContainer(root, "Rack_TV");
            rack.transform.localPosition = new Vector3(-0.84f, 0f, -5.49f);
            CreateBox(rack.transform, "Base_Rodape_Rack", new Vector3(0f, 0.040f, 0f), new Vector3(1.64f, 0.080f, 0.48f), pal.woodMahogany);
            CreateBox(rack.transform, "Lat_Esq_Rack", new Vector3(-0.8225f, 0.305f, 0f), new Vector3(0.035f, 0.450f, 0.50f), pal.woodMahogany);
            CreateBox(rack.transform, "Lat_Dir_Rack", new Vector3(0.8225f, 0.305f, 0f), new Vector3(0.035f, 0.450f, 0.50f), pal.woodMahogany);
            CreateBox(rack.transform, "Fundo_Rack", new Vector3(0f, 0.305f, -0.2375f), new Vector3(1.610f, 0.450f, 0.025f), pal.woodMahogany);
            CreateBox(rack.transform, "Div_Esq_Rack", new Vector3(-0.400f, 0.305f, 0.010f), new Vector3(0.028f, 0.450f, 0.470f), pal.woodMahogany, hasCollider: false);
            CreateBox(rack.transform, "Div_Dir_Rack", new Vector3(0.400f, 0.305f, 0.010f), new Vector3(0.028f, 0.450f, 0.470f), pal.woodMahogany, hasCollider: false);
            CreateBox(rack.transform, "Prateleira_VCR", new Vector3(0f, 0.305f, 0.005f), new Vector3(0.772f, 0.024f, 0.460f), pal.woodMahogany, hasCollider: false);
            Mesh rackTopMesh = CreateChamferedRectLoftMesh("Mesh_Tampo_Rack_TV", 1.72f, 0.030f, 0.54f, 0.014f, 0.985f, 0.005f);
            GameObject rackTopObj = CreateMeshObject(rack.transform, "Tampo_Rack", rackTopMesh, pal.woodMahogany, new Vector3(0f, 0.530f, 0f), castShadows: true);
            BoxCollider rackTopCol = rackTopObj.AddComponent<BoxCollider>();
            rackTopCol.center = new Vector3(0f, 0.015f, 0f);
            rackTopCol.size = new Vector3(1.72f, 0.030f, 0.54f);

            // Compartimentos internos das portas laterais do Rack
            CreateBox(rack.transform, "Prateleira_Int_Rack_E", new Vector3(-0.609f, 0.295f, 0.005f), new Vector3(0.380f, 0.020f, 0.430f), pal.furnitureWood, hasCollider: false);
            CreateBox(rack.transform, "Prateleira_Int_Rack_D", new Vector3( 0.609f, 0.295f, 0.005f), new Vector3(0.380f, 0.020f, 0.430f), pal.furnitureWood, hasCollider: false);
            CreateDetailBox(rack.transform, "Album_Fotos_Rack", new Vector3(-0.62f, 0.098f, 0.02f), new Vector3(0.24f, 0.036f, 0.30f), pal.bookBurgundy, new Vector3(0f, 6f, 0f), castShadows: true);
            BuildUnopenedMailBundle(rack.transform, "Contas_Antigas_Rack", new Vector3(-0.59f, 0.305f, 0.03f), -12f, pal, count: 3);
            for (int rv = 0; rv < 2; rv++)
            {
                CreateDetailBox(rack.transform, $"Fita_Arquivo_Rack_{rv}", new Vector3(0.58f, 0.0925f + rv * 0.025f, 0.02f), new Vector3(0.19f, 0.025f, 0.11f), pal.metalDark, new Vector3(0f, rv * 7f - 4f, 0f));
            }
            BuildStackedBooks(rack.transform, "Guias_TV_Rack", new Vector3(0.60f, 0.305f, 0.01f), 8f, pal, count: 2);

            InspectableObject rackInsideInsp = MakeSpot(rack.transform, "Compartimento_Rack_Interagir", new Vector3(0.61f, 0.26f, 0.06f), new Vector3(0.34f, 0.38f, 0.34f));
            rackInsideInsp.ConfigureTask(
                id: "compartimento_rack_opc",
                prompt: "Olhar dentro do rack",
                subtitle: "Fitas velhas da família e carnês vencidos no fundo do rack.",
                repeatSubtitle: "Só poeira e papel velho.",
                linear: false,
                sound: FoleySoundId.WoodInspect
            );

            // Portas duplas articuladas do Rack de TV
            GameObject rackDoorsHost = CreateContainer(rack.transform, "Portas_Rack_TV");
            float[] rackHingeYs = { -0.145f, 0.145f };

            GameObject rackLeftPivot = CreateContainer(rackDoorsHost.transform, "Porta_Rack_Esq_Pivot");
            rackLeftPivot.transform.localPosition = new Vector3(-0.803f, 0.305f, 0.252f);
            CreateBox(rackLeftPivot.transform, "Folha_Porta_Rack_Esq", new Vector3(0.193f, 0f, 0f), new Vector3(0.386f, 0.444f, 0.022f), pal.furnitureWood, hasCollider: true);
            BuildOpenFrameTrimBars(rackLeftPivot.transform, "Moldura_Rack_Esq", new Vector3(0.193f, 0f, 0.014f), 0.326f, 0.384f, 0.024f, 0.006f, pal.baseboardWood);
            CreateDetailBox(rackLeftPivot.transform, "Painel_Rebaixado_Rack_Esq", new Vector3(0.193f, 0f, 0.012f), new Vector3(0.278f, 0.336f, 0.002f), pal.woodMahogany);
            CreateDetailCylinder(rackLeftPivot.transform, "Roseta_Puxador_Rack_E", new Vector3(0.369f, 0.035f, 0.012f), new Vector3(0.020f, 0.002f, 0.020f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(rackLeftPivot.transform, "Haste_Puxador_Rack_E", new Vector3(0.369f, 0.035f, 0.018f), new Vector3(0.010f, 0.005f, 0.010f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(rackLeftPivot.transform, "Puxador_Rack_E", new Vector3(0.369f, 0.035f, 0.026f), new Vector3(0.022f, 0.006f, 0.022f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            for (int rhi = 0; rhi < rackHingeYs.Length; rhi++)
                CreateDetailCylinder(rackLeftPivot.transform, $"Dobradica_Rack_E_{rhi}", new Vector3(0f, rackHingeYs[rhi], 0.011f), new Vector3(0.012f, 0.018f, 0.012f), pal.metalBrass);

            GameObject rackRightPivot = CreateContainer(rackDoorsHost.transform, "Porta_Rack_Dir_Pivot");
            rackRightPivot.transform.localPosition = new Vector3(0.803f, 0.305f, 0.252f);
            CreateBox(rackRightPivot.transform, "Folha_Porta_Rack_Dir", new Vector3(-0.193f, 0f, 0f), new Vector3(0.386f, 0.444f, 0.022f), pal.furnitureWood, hasCollider: true);
            BuildOpenFrameTrimBars(rackRightPivot.transform, "Moldura_Rack_Dir", new Vector3(-0.193f, 0f, 0.014f), 0.326f, 0.384f, 0.024f, 0.006f, pal.baseboardWood);
            CreateDetailBox(rackRightPivot.transform, "Painel_Rebaixado_Rack_Dir", new Vector3(-0.193f, 0f, 0.012f), new Vector3(0.278f, 0.336f, 0.002f), pal.woodMahogany);
            CreateDetailCylinder(rackRightPivot.transform, "Roseta_Puxador_Rack_D", new Vector3(-0.369f, 0.035f, 0.012f), new Vector3(0.020f, 0.002f, 0.020f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(rackRightPivot.transform, "Haste_Puxador_Rack_D", new Vector3(-0.369f, 0.035f, 0.018f), new Vector3(0.010f, 0.005f, 0.010f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(rackRightPivot.transform, "Puxador_Rack_D", new Vector3(-0.369f, 0.035f, 0.026f), new Vector3(0.022f, 0.006f, 0.022f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            for (int rhi = 0; rhi < rackHingeYs.Length; rhi++)
                CreateDetailCylinder(rackRightPivot.transform, $"Dobradica_Rack_D_{rhi}", new Vector3(0f, rackHingeYs[rhi], 0.011f), new Vector3(0.012f, 0.018f, 0.012f), pal.metalBrass);

            AnimatedContainerInteractable rackAnim = rackDoorsHost.AddComponent<AnimatedContainerInteractable>();
            rackAnim.ConfigureSwingCabinet(
                id: "portas_rack_tv",
                primaryDoorPivot: rackLeftPivot.transform,
                primaryOpenEuler: new Vector3(0f, -84f, 0f),
                promptOpen: "Abrir portas do rack",
                promptClose: "Fechar portas do rack",
                doorSfx: FoleySoundId.CupboardDoor,
                secondDoorPivot: rackRightPivot.transform,
                secondOpenEuler: new Vector3(0f, 84f, 0f)
            );
            rackAnim.RegisterInteriorObject(rackInsideInsp);
            EditorUtility.SetDirty(rackAnim);

            // Videocassete sobre Prateleira_VCR (topo Y=0.317m) com pés, botões, dial e cabos RCA até a TV
            float[] vcrFootXs = { -0.25f, 0.11f };
            float[] vcrFootZs = { -0.11f, 0.15f };
            for (int fx = 0; fx < 2; fx++)
                for (int fz = 0; fz < 2; fz++)
                    CreateDetailCylinder(rack.transform, $"Pe_VCR_{fx}_{fz}", new Vector3(vcrFootXs[fx], 0.319f, vcrFootZs[fz]), new Vector3(0.024f, 0.002f, 0.024f), pal.shadowBlack);
            Mesh vcrMesh = CreateChamferedRectLoftMesh("Mesh_Aparelho_VCR", 0.42f, 0.076f, 0.32f, 0.008f, 0.96f, 0.004f);
            CreateMeshObject(rack.transform, "Aparelho_VCR", vcrMesh, pal.metalDark, new Vector3(-0.07f, 0.321f, 0.02f), castShadows: true);
            CreateDetailBox(rack.transform, "Friso_Frontal_VCR", new Vector3(-0.07f, 0.388f, 0.181f), new Vector3(0.408f, 0.006f, 0.004f), pal.stainlessSteel);
            CreateDetailBox(rack.transform, "Boca_Fita_VHS", new Vector3(-0.145f, 0.365f, 0.182f), new Vector3(0.20f, 0.024f, 0.008f), pal.shadowBlack);
            CreateDetailBox(rack.transform, "Visor_VCR", new Vector3(0.055f, 0.367f, 0.182f), new Vector3(0.105f, 0.022f, 0.006f), pal.ledGreenDim);
            CreateDetailBox(rack.transform, "Botao_Power_VCR", new Vector3(-0.262f, 0.336f, 0.182f), new Vector3(0.018f, 0.012f, 0.006f), pal.calendarRed);
            for (int vb = 0; vb < 4; vb++)
            {
                CreateDetailBox(rack.transform, $"Botao_Transp_VCR_{vb}", new Vector3(-0.185f + vb * 0.026f, 0.336f, 0.182f), new Vector3(0.018f, 0.012f, 0.006f), pal.plasticBeige90s);
            }
            CreateDetailCylinder(rack.transform, "Dial_Tracking_VCR", new Vector3(0.122f, 0.342f, 0.182f), new Vector3(0.018f, 0.005f, 0.018f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));

            // Cabos RCA ligando a traseira do VCR (Z=-5.63) até Entrada_Video_VCR / Entrada_Audio_VCR da TV (-0.935/-0.898, 0.675, -5.682)
            BuildRoutedWallCable(root, "Cabo_VCR_TV_Video", "Mesh_Cabo_VCR_Video", new[]
            {
                new Vector3(-0.985f, 0.352f, -5.630f),
                new Vector3(-0.985f, 0.352f, -5.735f),
                new Vector3(-0.960f, 0.560f, -5.735f),
                new Vector3(-0.935f, 0.675f, -5.720f),
                new Vector3(-0.935f, 0.675f, -5.682f)
            }, pal.shadowBlack, 0.0038f);
            BuildRoutedWallCable(root, "Cabo_VCR_TV_Audio", "Mesh_Cabo_VCR_Audio", new[]
            {
                new Vector3(-0.945f, 0.352f, -5.630f),
                new Vector3(-0.945f, 0.352f, -5.742f),
                new Vector3(-0.920f, 0.560f, -5.742f),
                new Vector3(-0.898f, 0.675f, -5.720f),
                new Vector3(-0.898f, 0.675f, -5.682f)
            }, pal.metalDark, 0.0038f);

            for (int v = 0; v < 3; v++)
            {
                float vhsYaw = v * 4f - 3f;
                float vhsRad = vhsYaw * Mathf.Deg2Rad;
                float vhsX = -0.16f + v * 0.01f;
                float vhsY = 0.0925f + v * 0.025f;
                float vhsZ = 0.05f;
                CreateDetailBox(rack.transform, $"Fita_VHS_{v}", new Vector3(vhsX, vhsY, vhsZ),
                    new Vector3(0.19f, 0.025f, 0.11f), pal.metalDark, new Vector3(0f, vhsYaw, 0f));
                CreateDetailBox(rack.transform, $"Janela_Rolos_VHS_{v}", new Vector3(vhsX, vhsY + 0.013f, vhsZ),
                    new Vector3(0.105f, 0.0012f, 0.048f), pal.shadowBlack, new Vector3(0f, vhsYaw, 0f));
                CreateDetailBox(rack.transform, $"Lombada_VHS_{v}",
                    new Vector3(vhsX + 0.0565f * Mathf.Sin(vhsRad), vhsY, vhsZ + 0.0565f * Mathf.Cos(vhsRad)),
                    new Vector3(0.14f, 0.016f, 0.003f), pal.paperWhite, new Vector3(0f, vhsYaw, 0f));
                Material vhsMat = v == 0 ? pal.details.vhsSpine0 : (v == 1 ? pal.details.vhsSpine1 : pal.details.vhsSpine2);
                CreatePrintedOverlayQuad(rack.transform, $"Impressao_Lombada_VHS_{v}",
                    new Vector3(vhsX + 0.0585f * Mathf.Sin(vhsRad), vhsY, vhsZ + 0.0585f * Mathf.Cos(vhsRad)),
                    new Vector2(0.138f, 0.015f), vhsMat, new Vector3(0f, 180f + vhsYaw, 0f));
            }

            Material[] magCovers = { pal.bookSlateBlue, pal.bookBurgundy, pal.folderKraft };
            for (int mg = 0; mg < 3; mg++)
            {
                float mgY = 0.085f + mg * 0.011f;
                float mgYaw = -12f + mg * 5f;
                CreateDetailBox(rack.transform, $"Revista_Velha_{mg}", new Vector3(0.16f + mg * 0.006f, mgY, 0.04f - mg * 0.004f), new Vector3(0.22f, 0.010f, 0.28f), magCovers[mg], new Vector3(0f, mgYaw, 0f));
                CreateDetailBox(rack.transform, $"Miolo_Revista_{mg}", new Vector3(0.165f + mg * 0.006f, mgY, 0.04f - mg * 0.004f), new Vector3(0.212f, 0.007f, 0.270f), pal.paperWhite, new Vector3(0f, mgYaw, 0f));
            }

            // TV_Sala com localScale unitário e âncora exata (-0.84, 0.560, -5.45) apoiada diretamente sobre Tampo_Rack
            GameObject tvObj = CreateContainer(casaBase, "TV_Sala");
            tvObj.transform.localPosition = new Vector3(-0.84f, 0.560f, -5.45f);
            tvObj.transform.localRotation = Quaternion.identity;

            BoxCollider tvCol = GetOrAdd<BoxCollider>(tvObj);
            tvCol.isTrigger = false;
            tvCol.center = new Vector3(0f, 0.32f, 0.04f);
            tvCol.size = new Vector3(0.94f, 0.74f, 0.60f);

            float[] tvFootXs = { -0.30f, 0.30f };
            float[] tvFootZs = { -0.15f, 0.15f };
            for (int tx = 0; tx < 2; tx++)
                for (int tz = 0; tz < 2; tz++)
                {
                    CreateDetailCylinder(tvObj.transform, $"Pe_TV_{tx}_{tz}", new Vector3(tvFootXs[tx], 0.0055f, tvFootZs[tz]), new Vector3(0.056f, 0.0055f, 0.056f), pal.shadowBlack, castShadows: true);
                    CreateDetailCylinder(tvObj.transform, $"Arruela_Pe_TV_{tx}_{tz}", new Vector3(tvFootXs[tx], 0.0112f, tvFootZs[tz]), new Vector3(0.062f, 0.0012f, 0.062f), pal.stainlessSteel);
                }
            Mesh tvCabinetMesh = CreateChamferedRectLoftMesh("Mesh_TV_Carcaca", 0.76f, 0.588f, 0.420f, 0.018f, 0.985f, 0.010f);
            CreateMeshObject(tvObj.transform, "Carcaca_TV", tvCabinetMesh, pal.metalDark, new Vector3(0f, 0.012f, -0.010f), castShadows: true);
            CreateDetailBox(tvObj.transform, "Recorte_Mao_TV_E", new Vector3(-0.382f, 0.320f, -0.035f), new Vector3(0.006f, 0.044f, 0.130f), pal.shadowBlack);
            CreateDetailBox(tvObj.transform, "Recorte_Mao_TV_D", new Vector3( 0.382f, 0.320f, -0.035f), new Vector3(0.006f, 0.044f, 0.130f), pal.shadowBlack);
            for (int tvTopV = 0; tvTopV < 4; tvTopV++)
            {
                CreateDetailBox(tvObj.transform, $"Grade_Topo_TV_{tvTopV}", new Vector3(0f, 0.6015f, -0.145f - tvTopV * 0.016f), new Vector3(0.42f, 0.003f, 0.006f), pal.shadowBlack);
            }

            GameObject screenObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screenObj.name = "Tela_TV";
            screenObj.transform.SetParent(tvObj.transform, false);
            Object.DestroyImmediate(screenObj.GetComponent<Collider>());
            screenObj.transform.localPosition = new Vector3(0f, 0.336f, 0.202f);
            screenObj.transform.localScale = new Vector3(0.520f, 0.396f, 0.012f);
            AssignMaterial(screenObj, pal.tvScreen);
            GetOrAdd<ProceduralCRTFeed>(screenObj);

            // Aro frontal escalonado/chanfrado recuando o tubo para dentro + vidro plano liso transparente na boca
            Mesh tvBezelMesh = CreateSteppedBezelFrameMesh(
                "Mesh_TV_Bezel_Step",
                innerHalfW: 0.258f, innerHalfH: 0.196f,
                stepHalfW: 0.278f, stepHalfH: 0.212f,
                outerHalfW: 0.284f, outerHalfH: 0.220f,
                zBack: 0.194f, zScreenPlane: 0.204f, zStep: 0.232f, zFront: 0.244f);
            CreateMeshObject(tvObj.transform, "Aro_Frontal_TV", tvBezelMesh, pal.metalDark, new Vector3(0f, 0.336f, 0f), Vector3.zero, castShadows: true);

            GameObject tvGlass = CreateBox(tvObj.transform, "Vidro_Frontal_TV",
                new Vector3(0f, 0.336f, 0.228f), new Vector3(0.548f, 0.418f, 0.004f), pal.details.clearGlass, hasCollider: false);
            if (tvGlass.TryGetComponent<MeshRenderer>(out MeshRenderer tvGlassMr))
                tvGlassMr.shadowCastingMode = ShadowCastingMode.Off;

            // Moldura superior, painel de controles inferior e grades laterais em tiras paralelas limpas
            CreateDetailBox(tvObj.transform, "Moldura_Sup_TV", new Vector3(0f, 0.577f, 0.219f), new Vector3(0.755f, 0.041f, 0.044f), pal.metalDark);
            CreateDetailBox(tvObj.transform, "Painel_Inf_TV", new Vector3(0f, 0.065f, 0.219f), new Vector3(0.755f, 0.100f, 0.044f), pal.metalDark);
            CreateDetailBox(tvObj.transform, "Moldura_Lat_TV_Esq", new Vector3(-0.371f, 0.336f, 0.219f), new Vector3(0.013f, 0.436f, 0.042f), pal.metalDark);
            CreateDetailBox(tvObj.transform, "Moldura_Lat_TV_Dir", new Vector3(0.371f, 0.336f, 0.219f), new Vector3(0.013f, 0.436f, 0.042f), pal.metalDark);

            CreateDetailBox(tvObj.transform, "Fundo_Falante_Esq", new Vector3(-0.324f, 0.336f, 0.208f), new Vector3(0.076f, 0.432f, 0.020f), pal.shadowBlack);
            CreateDetailBox(tvObj.transform, "Fundo_Falante_Dir", new Vector3(0.324f, 0.336f, 0.208f), new Vector3(0.076f, 0.432f, 0.020f), pal.shadowBlack);
            for (int sl = 0; sl < 11; sl++)
            {
                float slatY = 0.156f + sl * 0.036f;
                CreateDetailBox(tvObj.transform, $"Tira_Falante_Esq_{sl}", new Vector3(-0.324f, slatY, 0.231f), new Vector3(0.078f, 0.011f, 0.014f), pal.metalDark);
                CreateDetailBox(tvObj.transform, $"Tira_Falante_Dir_{sl}", new Vector3(0.324f, slatY, 0.231f), new Vector3(0.078f, 0.011f, 0.014f), pal.metalDark);
            }

            // Controles frontais nítidos: barra recuada, botão power com colar, LED standby, dial de volume/sintonia e conector de fone
            CreateDetailBox(tvObj.transform, "Faixa_Controles_TV", new Vector3(0f, 0.065f, 0.2428f), new Vector3(0.680f, 0.062f, 0.004f), pal.shadowBlack);
            CreateDetailBox(tvObj.transform, "Botao_Power_TV", new Vector3(0.275f, 0.065f, 0.2495f), new Vector3(0.038f, 0.024f, 0.011f), pal.bookBurgundy);
            CreateDetailBox(tvObj.transform, "Friso_Power_TV", new Vector3(0.275f, 0.065f, 0.2562f), new Vector3(0.016f, 0.004f, 0.003f), pal.porcelainWhite);
            CreateDetailBox(tvObj.transform, "Colar_LED_TV", new Vector3(0.215f, 0.065f, 0.2478f), new Vector3(0.018f, 0.016f, 0.004f), pal.metalDark);
            CreateDetailBox(tvObj.transform, "LED_Standby_TV", new Vector3(0.215f, 0.065f, 0.2518f), new Vector3(0.010f, 0.009f, 0.004f), pal.ledRedGlow);
            for (int b = 0; b < 4; b++)
            {
                float bx = -0.205f + b * 0.044f;
                CreateDetailBox(tvObj.transform, $"Btn_Canal_TV_{b}", new Vector3(bx, 0.065f, 0.2490f), new Vector3(0.028f, 0.018f, 0.010f), pal.plasticBeige90s);
                CreateDetailBox(tvObj.transform, $"Detalhe_Canal_TV_{b}", new Vector3(bx, 0.065f, 0.2555f), new Vector3(0.016f, 0.004f, 0.003f), pal.shadowBlack);
            }
            CreateDetailBox(tvObj.transform, "Placa_Marca_TV", new Vector3(-0.048f, 0.118f, 0.2425f), new Vector3(0.105f, 0.012f, 0.003f), pal.stainlessSteel);
            CreateDetailCylinder(tvObj.transform, "Dial_Volume_TV", new Vector3(0.025f, 0.065f, 0.248f), new Vector3(0.024f, 0.008f, 0.024f), pal.metalDark, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(tvObj.transform, "Aro_Dial_Volume_TV", new Vector3(0.025f, 0.065f, 0.2545f), new Vector3(0.018f, 0.002f, 0.018f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(tvObj.transform, "Dial_Sintonia_TV", new Vector3(0.082f, 0.065f, 0.248f), new Vector3(0.024f, 0.008f, 0.024f), pal.metalDark, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(tvObj.transform, "Aro_Dial_Sintonia_TV", new Vector3(0.082f, 0.065f, 0.2545f), new Vector3(0.018f, 0.002f, 0.018f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(tvObj.transform, "Conector_Fone_TV", new Vector3(-0.292f, 0.065f, 0.246f), new Vector3(0.016f, 0.003f, 0.016f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(tvObj.transform, "Miolo_Fone_TV", new Vector3(-0.292f, 0.065f, 0.249f), new Vector3(0.008f, 0.002f, 0.008f), pal.shadowBlack, new Vector3(90f, 0f, 0f));

            // Traseira CRT piramidal afunilada com ventilação, entradas de antena RF/VHF + RCA VCR e passa-fio ligado à tomada
            Mesh crtTubeMesh = CreateClosedPolygonLoftMesh("Mesh_TV_TuboTraseiro", new[]
            {
                new Vector2( 0.24f, -0.155f),
                new Vector2( 0.27f, -0.125f),
                new Vector2( 0.27f,  0.125f),
                new Vector2( 0.24f,  0.155f),
                new Vector2(-0.24f,  0.155f),
                new Vector2(-0.27f,  0.125f),
                new Vector2(-0.27f, -0.125f),
                new Vector2(-0.24f, -0.155f)
            }, new[]
            {
                (0.000f, 1.00f, 1.00f, 0f, 0f),
                (0.068f, 0.72f, 0.76f, 0f, 0f),
                (0.105f, 0.65f, 0.72f, 0f, 0f)
            });
            CreateMeshObject(tvObj.transform, "Tubo_Traseiro_CRT", crtTubeMesh, pal.metalDark, new Vector3(0f, 0.335f, -0.215f), new Vector3(-90f, 0f, 0f), castShadows: true);
            for (int gv = 0; gv < 5; gv++)
            {
                CreateDetailBox(tvObj.transform, $"Grade_Vent_TV_{gv}", new Vector3(0f, 0.255f + gv * 0.036f, -0.322f), new Vector3(0.28f, 0.010f, 0.004f), pal.shadowBlack);
            }
            CreateDetailBox(tvObj.transform, "Placa_Entradas_TV", new Vector3(-0.11f, 0.135f, -0.223f), new Vector3(0.18f, 0.095f, 0.008f), pal.shadowBlack);
            CreateDetailCylinder(tvObj.transform, "Conector_Antena_RF", new Vector3(-0.155f, 0.152f, -0.233f), new Vector3(0.016f, 0.008f, 0.016f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(tvObj.transform, "Entrada_Video_VCR", new Vector3(-0.095f, 0.115f, -0.232f), new Vector3(0.014f, 0.006f, 0.014f), pal.bookMustard, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(tvObj.transform, "Entrada_Audio_VCR", new Vector3(-0.058f, 0.115f, -0.232f), new Vector3(0.014f, 0.006f, 0.014f), pal.porcelainWhite, new Vector3(90f, 0f, 0f));
            CreateDetailBox(tvObj.transform, "Terminal_VHF_UHF", new Vector3(-0.075f, 0.154f, -0.230f), new Vector3(0.046f, 0.018f, 0.006f), pal.metalDark);
            CreateDetailCylinder(tvObj.transform, "Passa_Fio_TV", new Vector3(0.060f, 0.165f, -0.225f), new Vector3(0.022f, 0.010f, 0.022f), pal.metalDark, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(tvObj.transform, "Base_Antena_V", new Vector3(0f, 0.612f, -0.04f), new Vector3(0.12f, 0.012f, 0.10f), pal.metalDark);
            CreateDetailSphere(tvObj.transform, "Junta_Antena_E", new Vector3(-0.032f, 0.625f, -0.04f), new Vector3(0.022f, 0.022f, 0.022f), pal.metalBrass, castShadows: false);
            CreateDetailSphere(tvObj.transform, "Junta_Antena_D", new Vector3( 0.032f, 0.625f, -0.04f), new Vector3(0.022f, 0.022f, 0.022f), pal.metalBrass, castShadows: false);
            CreateDetailCylinder(tvObj.transform, "Haste_Antena_Esq", new Vector3(-0.11f, 0.725f, -0.04f), new Vector3(0.008f, 0.14f, 0.008f), pal.stainlessSteel, new Vector3(0f, 0f, 34f));
            CreateDetailCylinder(tvObj.transform, "Haste_Antena_Dir", new Vector3(0.11f, 0.725f, -0.04f), new Vector3(0.008f, 0.14f, 0.008f), pal.stainlessSteel, new Vector3(0f, 0f, -34f));
            CreateDetailSphere(tvObj.transform, "Ponta_Antena_E", new Vector3(-0.188f, 0.840f, -0.04f), new Vector3(0.016f, 0.016f, 0.016f), pal.stainlessSteel, castShadows: false);
            CreateDetailSphere(tvObj.transform, "Ponta_Antena_D", new Vector3( 0.188f, 0.840f, -0.04f), new Vector3(0.016f, 0.016f, 0.016f), pal.stainlessSteel, castShadows: false);
            BuildRoutedWallCable(tvObj.transform, "Fio_Coax_Antena_TV", "Mesh_Fio_Coax_Antena_TV", new[]
            {
                new Vector3(-0.042f, 0.614f, -0.088f),
                new Vector3(-0.095f, 0.612f, -0.218f),
                new Vector3(-0.155f, 0.500f, -0.236f),
                new Vector3(-0.155f, 0.240f, -0.246f),
                new Vector3(-0.155f, 0.152f, -0.235f)
            }, pal.shadowBlack, 0.0036f);

            GameObject tvLightObj = new GameObject("Spot_Light_TV");
            tvLightObj.transform.SetParent(tvObj.transform, false);
            tvLightObj.transform.localPosition = new Vector3(0f, 0.34f, 0.32f);
            tvLightObj.transform.localRotation = Quaternion.Euler(-4f, 0f, 0f);
            Light tvGlow = tvLightObj.AddComponent<Light>();
            tvGlow.type = LightType.Spot;
            tvGlow.range = 5.8f;
            tvGlow.spotAngle = 136f;
            tvGlow.innerSpotAngle = 62f;
            tvGlow.intensity = 0f;
            tvGlow.shadows = LightShadows.Soft;
            tvGlow.shadowNearPlane = 0.12f;
            tvGlow.shadowBias = 0.02f;
            tvGlow.shadowNormalBias = 0.25f;
            ConfigureLightURPShadowTier(tvGlow, 1);
            tvGlow.enabled = false;

            TVController tvController = GetOrAdd<TVController>(tvObj);
            tvController.SetVisualReferences(screenObj.GetComponent<Renderer>(), tvGlow);
            EditorUtility.SetDirty(tvController);

            // Mesa de centro apoiada exatamente sobre o Campo_Tecido (Y=0.0090m -> topo em Y=0.434m) com tampo/pernas chanfrados e grade ripada real
            GameObject cTable = CreateContainer(root, "Mesa_Centro");
            cTable.transform.localPosition = new Vector3(-0.84f, 0f, -3.76f);
            Mesh mcTopMesh = CreateChamferedRectLoftMesh("Mesh_Tampo_MesaCentro", 1.16f, 0.036f, 0.60f, 0.018f, 0.98f, 0.006f);
            GameObject mcTopObj = CreateMeshObject(cTable.transform, "Tampo_MesaCentro", mcTopMesh, pal.furnitureWood, new Vector3(0f, 0.398f, 0f), castShadows: true);
            BoxCollider mcTopCol = mcTopObj.AddComponent<BoxCollider>();
            mcTopCol.center = new Vector3(0f, 0.018f, 0f);
            mcTopCol.size = new Vector3(1.16f, 0.036f, 0.60f);
            CreateDetailBox(cTable.transform, "Saia_Mesa_N", new Vector3(0f, 0.382f,  0.240f), new Vector3(0.976f, 0.032f, 0.022f), pal.baseboardWood);
            CreateDetailBox(cTable.transform, "Saia_Mesa_S", new Vector3(0f, 0.382f, -0.240f), new Vector3(0.976f, 0.032f, 0.022f), pal.baseboardWood);
            CreateDetailBox(cTable.transform, "Saia_Mesa_O", new Vector3(-0.510f, 0.382f, 0f), new Vector3(0.022f, 0.032f, 0.436f), pal.baseboardWood);
            CreateDetailBox(cTable.transform, "Saia_Mesa_L", new Vector3( 0.510f, 0.382f, 0f), new Vector3(0.022f, 0.032f, 0.436f), pal.baseboardWood);
            Mesh mcLegMesh = CreateChamferedRectLoftMesh("Mesh_Perna_MesaCentro", 0.044f, 0.365f, 0.044f, 0.006f, 0.95f, 0.004f);
            (float lx, float lz, string tag)[] mcLegs =
            {
                (-0.51f, -0.24f, "1"),
                ( 0.51f, -0.24f, "2"),
                (-0.51f,  0.24f, "3"),
                ( 0.51f,  0.24f, "4")
            };
            for (int mi = 0; mi < mcLegs.Length; mi++)
            {
                CreateDetailBox(cTable.transform, $"Pont_Pe_MC_{mcLegs[mi].tag}", new Vector3(mcLegs[mi].lx, 0.021f, mcLegs[mi].lz), new Vector3(0.040f, 0.024f, 0.040f), pal.metalBrass);
                CreateMeshObject(cTable.transform, $"Perna_MC_{mcLegs[mi].tag}", mcLegMesh, pal.furnitureWood, new Vector3(mcLegs[mi].lx, 0.033f, mcLegs[mi].lz), castShadows: true);
            }
            CreateDetailBox(cTable.transform, "Travessa_Revisteiro_O", new Vector3(-0.469f, 0.145f, 0f), new Vector3(0.038f, 0.020f, 0.436f), pal.baseboardWood);
            CreateDetailBox(cTable.transform, "Travessa_Revisteiro_L", new Vector3( 0.469f, 0.145f, 0f), new Vector3(0.038f, 0.020f, 0.436f), pal.baseboardWood);
            for (int sr = -2; sr <= 2; sr++)
            {
                CreateDetailBox(cTable.transform, $"Ripa_Revisteiro_{sr + 2}", new Vector3(0f, 0.149f, sr * 0.088f), new Vector3(0.900f, 0.012f, 0.046f), pal.furnitureWood);
            }
            GameObject centerTablePaper = BuildDetailedPaperSheet(cTable.transform, "Jornal_Dobra_Inferior", new Vector3(-0.20f, 0.155f, 0.02f), 11f, new Vector2(0.32f, 0.26f), pal, isNewspaper: true);
            SnapToSupportWorldY(centerTablePaper, 0.155f);

            GameObject remoteObj = BuildDetailedRemoteControl(root, "Controle_Remoto_Decor", new Vector3(-1.23f, 0.434f, -3.63f), -22f, pal);
            SnapToSupportTop(remoteObj, cTable);
            GameObject ashtrayObj = BuildDetailedAshtray(root, "Cinzeiro_MesaCentro", new Vector3(-0.44f, 0.434f, -3.66f), 28f, pal);
            SnapToSupportTop(ashtrayObj, cTable);
            GameObject coasterObj = CreateDetailCylinder(root, "Porta_Copo_Cortica", new Vector3(-1.11f, 0.435f, -3.87f), new Vector3(0.095f, 0.0015f, 0.095f), pal.woodLight);
            SnapToSupportTop(coasterObj, cTable);
            GameObject glassLiving = BuildDrinkingGlass(root, "Copo_Agua_Sala", new Vector3(-1.11f, 0.437f, -3.87f), pal);
            SnapToSupportTop(glassLiving, coasterObj);

            // Sofá em frente à TV com almofadas chanfradas, manta contínua e colisores compostos intactos (Z=-2.54..-1.66)
            GameObject sofaRoot = CreateContainer(root, "Sofa_Sala");
            sofaRoot.transform.localPosition = new Vector3(-0.84f, 0f, -2.08f);
            BuildLivingRoomSofaUpgrades(sofaRoot.transform, pal);

            BoxCollider seatCol = sofaRoot.AddComponent<BoxCollider>();
            seatCol.center = new Vector3(0f, 0.195f, -0.08f);
            seatCol.size = new Vector3(1.64f, 0.39f, 0.74f);

            BoxCollider backCol = sofaRoot.AddComponent<BoxCollider>();
            backCol.center = new Vector3(0f, 0.48f, 0.28f);
            backCol.size = new Vector3(1.64f, 0.88f, 0.28f);

            BoxCollider leftArmCol = sofaRoot.AddComponent<BoxCollider>();
            leftArmCol.center = new Vector3(-0.91f, 0.32f, -0.02f);
            leftArmCol.size = new Vector3(0.18f, 0.64f, 0.88f);

            BoxCollider rightArmCol = sofaRoot.AddComponent<BoxCollider>();
            rightArmCol.center = new Vector3(0.91f, 0.32f, -0.02f);
            rightArmCol.size = new Vector3(0.18f, 0.64f, 0.88f);

            SofaTVProxy sofaProxy = GetOrAdd<SofaTVProxy>(sofaRoot);
            sofaProxy.LinkTV(tvController);
            EditorUtility.SetDirty(sofaProxy);

            // Estante encostada no canto nordeste da sala (X=+1.115f deixa 11mm livres do Rodape_Sala_Leste_0 em X=1.306)
            GameObject shelf = CreateContainer(root, "Estante_Sala");
            shelf.transform.localPosition = new Vector3(1.115f, 0f, -0.68f);
            shelf.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            CreateBox(shelf.transform, "Base_Estante", new Vector3(0f, 0.030f, 0.010f), new Vector3(0.89f, 0.060f, 0.330f), pal.furnitureWood, hasCollider: false);
            CreateBox(shelf.transform, "Lat_Esq", new Vector3(-0.4625f, 0.825f, 0f), new Vector3(0.035f, 1.65f, 0.36f), pal.furnitureWood);
            CreateBox(shelf.transform, "Lat_Dir", new Vector3(0.4625f, 0.825f, 0f), new Vector3(0.035f, 1.65f, 0.36f), pal.furnitureWood);
            CreateBox(shelf.transform, "Fundo_Estante", new Vector3(0f, 0.825f, -0.1675f), new Vector3(0.89f, 1.65f, 0.025f), pal.furnitureWood);
            Mesh shelfTopMesh = CreateChamferedRectLoftMesh("Mesh_Topo_Estante_Sala", 0.98f, 0.040f, 0.38f, 0.014f, 0.97f, 0.006f);
            GameObject shelfTopObj = CreateMeshObject(shelf.transform, "Topo_Estante", shelfTopMesh, pal.woodMahogany, new Vector3(0f, 1.650f, 0f), castShadows: true);
            BoxCollider shelfTopCol = shelfTopObj.AddComponent<BoxCollider>();
            shelfTopCol.center = new Vector3(0f, 0.020f, 0f);
            shelfTopCol.size = new Vector3(0.98f, 0.040f, 0.38f);
            CreateDetailBox(shelf.transform, "Friso_Topo_Estante_Frente", new Vector3(0f, 1.640f, 0.181f), new Vector3(0.886f, 0.020f, 0.016f), pal.baseboardWood);
            CreateDetailBox(shelf.transform, "Apoio_Livro_Sala_V", new Vector3(0.012f, 0.945f, -0.01f), new Vector3(0.006f, 0.140f, 0.14f), pal.metalBrass);
            CreateDetailBox(shelf.transform, "Apoio_Livro_Sala_B", new Vector3(0.032f, 0.878f, -0.01f), new Vector3(0.036f, 0.006f, 0.14f), pal.metalBrass);
            float[] shelfYs = { 0.44f, 0.86f, 1.28f };
            for (int s = 0; s < shelfYs.Length; s++)
            {
                CreateBox(shelf.transform, $"Prateleira_{s}", new Vector3(0f, shelfYs[s], 0.012f), new Vector3(0.89f, 0.028f, 0.335f), pal.furnitureWood, hasCollider: false);
            }

            CreateDetailBox(shelf.transform, "Caixa_Arquivo_Estante", new Vector3(-0.22f, 0.112f, 0.01f), new Vector3(0.28f, 0.104f, 0.24f), pal.folderKraft, new Vector3(0f, 4f, 0f), castShadows: true);
            CreateDetailBox(shelf.transform, "Fitas_Estante_0", new Vector3(0.18f, 0.0725f, 0.02f), new Vector3(0.19f, 0.025f, 0.11f), pal.metalDark, new Vector3(0f, -6f, 0f));
            CreateDetailBox(shelf.transform, "Fitas_Estante_1", new Vector3(0.19f, 0.0975f, 0.01f), new Vector3(0.19f, 0.025f, 0.11f), pal.metalDark, new Vector3(0f, 4f, 0f));

            Material[] bookMats = { pal.bookSlateBlue, pal.bookBurgundy, pal.bookMustard, pal.bookTeal, pal.bookSage, pal.bookTaupe };
            for (int b = 0; b < 7; b++)
            {
                float bh = 0.21f + (b % 3) * 0.03f;
                float bz = 0.178f + (b % 2) * 0.014f;
                float bx = -0.34f + b * 0.055f;
                CreateDetailBox(shelf.transform, $"Livro_P2_{b}", new Vector3(bx, 0.874f + bh * 0.5f, -0.02f),
                    new Vector3(0.042f, bh, bz), bookMats[b % bookMats.Length]);
                CreateDetailBox(shelf.transform, $"Faixa_Lombada_P2_{b}", new Vector3(bx, 0.874f + bh * 0.66f, -0.02f + bz * 0.5f + 0.0015f),
                    new Vector3(0.034f, 0.024f, 0.003f), (b % 2 == 0) ? pal.paperWhite : pal.bookTaupe);
            }
            for (int b = 0; b < 6; b++)
            {
                float bh = 0.22f + (b % 2) * 0.04f;
                float bz = 0.195f + ((b + 1) % 2) * 0.015f;
                float bx = 0.06f + b * 0.058f;
                float tiltZ = (b == 5) ? -12f : 0f;
                GameObject p1Book = CreateDetailBox(shelf.transform, $"Livro_P1_{b}", new Vector3(bx + (b == 5 ? 0.026f : 0f), 0.454f + bh * 0.5f, -0.02f),
                    new Vector3(0.046f, bh, bz), bookMats[(b + 2) % bookMats.Length], new Vector3(0f, 0f, tiltZ));
                if (b == 5)
                {
                    SnapToSupportWorldY(p1Book, 0.454f);
                }
                else
                {
                    CreateDetailBox(shelf.transform, $"Faixa_Lombada_P1_{b}", new Vector3(bx, 0.454f + bh * 0.64f, -0.02f + bz * 0.5f + 0.0015f),
                        new Vector3(0.036f, 0.022f, 0.003f), pal.paperWhite);
                }
            }
            CreateMeshObject(shelf.transform, "Adorno_Ceramica", GetOrBuildLivingRoomCeramicVaseMesh(), pal.terracotta, new Vector3(-0.22f, 0.454f, 0.01f), Vector3.zero, castShadows: true);

            // Vaso com samambaia seca posicionado sobre o topo da estante (Topo_Estante em Y=1.690f) + relógio parado e livros
            GameObject shelfPlant = BuildPottedWiltedPlant(shelf.transform, "Samambaia_Seca_Sala", new Vector3(-0.24f, 1.690f, 0.00f), 0.78f, pal);
            SnapToSupportWorldY(shelfPlant, 1.690f);
            GameObject shelfBooks = BuildStackedBooks(shelf.transform, "Livros_Topo_Estante", new Vector3(0.04f, 1.690f, 0.02f), -9f, pal, count: 2);
            SnapToSupportWorldY(shelfBooks, 1.690f);
            GameObject shelfClock = BuildStoppedMantleClock(shelf.transform, "Relogio_Estante_Parado", new Vector3(0.28f, 1.690f, 0.02f), 16f, pal);
            SnapToSupportWorldY(shelfClock, 1.690f);

            GameObject turnedFrame = CreateContainer(shelf.transform, "Porta_Retrato_Virado");
            turnedFrame.transform.localPosition = new Vector3(0.18f, 1.294f, 0.02f);
            turnedFrame.transform.localRotation = Quaternion.Euler(14f, 162f, 0f);
            BuildOpenFrameTrimBars(turnedFrame.transform, "Moldura_Retrato", new Vector3(0f, 0.105f, 0.003f), 0.16f, 0.21f, 0.018f, 0.016f, pal.woodMahogany);
            CreateDetailBox(turnedFrame.transform, "Fundo_Eucatex", new Vector3(0f, 0.105f, -0.002f), new Vector3(0.124f, 0.174f, 0.006f), pal.baseboardWood);
            CreateDetailBox(turnedFrame.transform, "Foto_Oculta_Retrato", new Vector3(0f, 0.105f, 0.002f), new Vector3(0.120f, 0.170f, 0.002f), pal.paperWhite);
            CreateDetailBox(turnedFrame.transform, "Trava_Retrato_E", new Vector3(-0.056f, 0.105f, -0.0065f), new Vector3(0.018f, 0.010f, 0.003f), pal.metalBrass);
            CreateDetailBox(turnedFrame.transform, "Trava_Retrato_D", new Vector3( 0.056f, 0.105f, -0.0065f), new Vector3(0.018f, 0.010f, 0.003f), pal.metalBrass);
            CreateDetailCylinder(turnedFrame.transform, "Dobradica_Gravata", new Vector3(0f, 0.118f, -0.006f), new Vector3(0.008f, 0.012f, 0.008f), pal.metalBrass, new Vector3(0f, 0f, 90f));
            CreateDetailBox(turnedFrame.transform, "Gravata_Apoio", new Vector3(0f, 0.064f, -0.032f), new Vector3(0.035f, 0.122f, 0.008f), pal.baseboardWood, new Vector3(-24f, 0f, 0f));
            SnapToSupportWorldY(turnedFrame, 1.294f);

            InspectableObject shelfInsp = MakeSpot(root, "Estante_Sala_Interagir", new Vector3(1.10f, 0.96f, -0.68f), new Vector3(0.46f, 1.94f, 0.98f));
            shelfInsp.ConfigureTask(
                id: "estante_sala_opc",
                prompt: "Olhar a estante",
                subtitle: "Virei esse retrato pra parede faz meses. Nem sei por que ainda não tirei daqui.",
                repeatSubtitle: "Deixa virado.",
                linear: false,
                sound: FoleySoundId.WoodInspect
            );

            // Poltrona no canto sudeste da sala voltada para o sofá e mesa de centro (rotY = +148°), manta contínua e Livro_Aberto_Poltrona em Y=0.395m
            GameObject armchair = CreateContainer(root, "Poltrona_Canto");
            armchair.transform.localPosition = new Vector3(0.62f, 0f, -4.82f);
            armchair.transform.localRotation = Quaternion.Euler(0f, 148f, 0f);
            BuildLivingRoomArmchairUpgrades(armchair.transform, pal);
            GameObject armBook = BuildOpenBook(armchair.transform, "Livro_Aberto_Poltrona", new Vector3(0.04f, 0.395f, -0.04f), -16f, pal, pal.bookSlateBlue);
            SnapToSupportWorldY(armBook, 0.395f);

            GameObject floorLamp = CreateContainer(root, "Luminaria_Coluna_Sala");
            floorLamp.transform.localPosition = new Vector3(1.06f, 0f, -3.65f);
            BuildLivingRoomFloorLamp(floorLamp.transform, pal);

            InspectableObject armInsp = MakeSpot(root, "Poltrona_Interagir", new Vector3(0.62f, 0.44f, -4.82f), new Vector3(0.86f, 0.86f, 0.86f));
            armInsp.ConfigureTask(
                id: "poltrona_opc",
                prompt: "Olhar a poltrona",
                subtitle: "A manta dela continua dobrada no braço. Ninguém senta aqui faz tempo.",
                repeatSubtitle: "Cheiro de pano guardado.",
                linear: false,
                sound: FoleySoundId.ClothInspect
            );

            return tvController;
        }

        private static void BuildKitchen(Transform root, GreyboxPalette pal, ref HouseContainers containers)
        {
            // Azulejos aparados nos cantos sem Z-fighting com o friso perpendicular
            BuildTileWainscotingZ(root, "Azulejo_Cozinha_Oeste_Sul", -6.535f, -5.84f, -3.875f, 1.52f, pal);
            BuildTileWainscotingZ(root, "Azulejo_Cozinha_Oeste_JanelaBase", -6.535f, -3.875f, -2.325f, 0.922f, pal);
            BuildTileWainscotingZ(root, "Azulejo_Cozinha_Oeste_Norte", -6.535f, -2.325f, 0.38f, 1.52f, pal);
            BuildTileWainscotingX(root, "Azulejo_Cozinha_Sul", -6.525f, -2.94f, -5.835f, 1.52f, pal);
            BuildFloorDrain(root, "Ralo_Cozinha", new Vector3(-4.74f, 0f, -4.45f), pal);

            // Geladeira no canto sudoeste voltada para o centro (+Z), rente aos azulejos sul
            GameObject fridge = CreateContainer(root, "Geladeira_Copa");
            fridge.transform.localPosition = new Vector3(-6.13f, 0f, -5.49f);
            BuildKitchenFridgeDetails(fridge.transform, pal);

            InspectableObject fridgeInsp = MakeSpot(root, "Geladeira_Bilhetes_Interagir", new Vector3(-6.13f, 1.40f, -5.09f), new Vector3(0.66f, 0.48f, 0.22f));
            fridgeInsp.ConfigureTask(
                id: "geladeira_bilhetes_opc",
                prompt: "Ler os papéis na geladeira",
                subtitle: "Boletos vencidos e a lista dela no ímã: leite, maçã, sabonete infantil. Nunca tirei daí.",
                repeatSubtitle: "A tinta da caneta já tá desbotada.",
                linear: false,
                sound: FoleySoundId.PaperRustle
            );

            // Balcão sul da cozinha: X = [-5.74 .. -3.76] (largura 1.98m, profundidade Z = [-5.81 .. -5.23], tampo Y = 0.890m)
            GameObject counter = CreateContainer(root, "Balcao_Cozinha_Sul");

            CreateBox(counter.transform, "Soculo_Recuado", new Vector3(-4.75f, 0.045f, -5.54f), new Vector3(1.94f, 0.090f, 0.50f), pal.metalDark);
            GameObject counterFloor = CreateBox(counter.transform, "Piso_Interno_Armario", new Vector3(-4.75f, 0.100f, -5.512f), new Vector3(1.916f, 0.020f, 0.540f), pal.formicaCream);
            CreateBox(counter.transform, "Lateral_Oeste", new Vector3(-5.725f, 0.470f, -5.52f), new Vector3(0.030f, 0.760f, 0.58f), pal.furnitureWood);
            CreateBox(counter.transform, "Divisoria_Central", new Vector3(-4.545f, 0.480f, -5.5075f), new Vector3(0.030f, 0.740f, 0.555f), pal.furnitureWood);
            CreateBox(counter.transform, "Lateral_Leste", new Vector3(-3.775f, 0.470f, -5.52f), new Vector3(0.030f, 0.760f, 0.58f), pal.furnitureWood);
            CreateBox(counter.transform, "Fundo_Balcao", new Vector3(-4.75f, 0.470f, -5.7975f), new Vector3(1.92f, 0.760f, 0.025f), pal.furnitureWood);
            CreateDetailBox(counter.transform, "Cavidade_Escura_Pia", new Vector3(-5.135f, 0.440f, -5.7815f), new Vector3(1.13f, 0.68f, 0.008f), pal.shadowBlack);

            // Tampo de granito contornando a cuba inox em X = -5.14f (sem painéis sobrepostos)
            CreateBox(counter.transform, "Granito_Oeste_Cuba", new Vector3(-5.59f, 0.870f, -5.52f), new Vector3(0.30f, 0.040f, 0.60f), pal.graniteDark);
            GameObject counterEastGranite = CreateBox(counter.transform, "Granito_Leste_Seco", new Vector3(-4.29f, 0.870f, -5.52f), new Vector3(1.06f, 0.040f, 0.60f), pal.graniteDark);
            CreateBox(counter.transform, "Granito_Tras_Cuba", new Vector3(-5.13f, 0.870f, -5.76f), new Vector3(0.62f, 0.040f, 0.12f), pal.graniteDark);
            CreateBox(counter.transform, "Granito_Frente_Cuba", new Vector3(-5.13f, 0.870f, -5.28f), new Vector3(0.62f, 0.040f, 0.12f), pal.graniteDark);
            CreateDetailBox(counter.transform, "Rodabanca_Granito", new Vector3(-4.75f, 0.935f, -5.81f), new Vector3(1.98f, 0.090f, 0.020f), pal.graniteDark);
            GameObject coffeePourOver = BuildPourOverCoffeeSet(counter.transform, "Conjunto_Coador_Cafe_Balcao", new Vector3(-4.00f, 0.890f, -5.55f), 14f, pal);
            SnapToSupportTop(coffeePourOver, counterEastGranite);

            GameObject sinkBottom = CreateDetailBox(counter.transform, "Cuba_Inox_Fundo", new Vector3(-5.13f, 0.742f, -5.52f), new Vector3(0.608f, 0.016f, 0.348f), pal.stainlessSteel);
            CreateDetailBox(counter.transform, "Cuba_Inox_ParedeO", new Vector3(-5.4325f, 0.818f, -5.52f), new Vector3(0.015f, 0.136f, 0.328f), pal.stainlessSteel);
            CreateDetailBox(counter.transform, "Cuba_Inox_ParedeL", new Vector3(-4.8275f, 0.818f, -5.52f), new Vector3(0.015f, 0.136f, 0.328f), pal.stainlessSteel);
            CreateDetailBox(counter.transform, "Cuba_Inox_ParedeS", new Vector3(-5.13f, 0.818f, -5.6925f), new Vector3(0.620f, 0.136f, 0.015f), pal.stainlessSteel);
            CreateDetailBox(counter.transform, "Cuba_Inox_ParedeN", new Vector3(-5.13f, 0.818f, -5.3475f), new Vector3(0.620f, 0.136f, 0.015f), pal.stainlessSteel);
            BuildOpenFrameTrimBarsXZ(counter.transform, "Aro_Cuba_Inox", new Vector3(-5.13f, 0.8920f, -5.52f), 0.652f, 0.380f, 0.016f, 0.004f, pal.stainlessSteel);
            GameObject sinkDrain = CreateDetailCylinder(counter.transform, "Ralo_Cuba_Inox", new Vector3(-5.13f, 0.753f, -5.55f), new Vector3(0.085f, 0.003f, 0.085f), pal.metalDark);
            SnapToSupportTop(sinkDrain, sinkBottom);
            GameObject soakingPot = BuildSoakingPot(counter.transform, "Panela_De_Molho_Pia", new Vector3(-4.98f, 0.750f, -5.47f), 152f, pal);
            SnapToSupportTop(soakingPot, sinkBottom);

            BuildKitchenFaucetAndDetergentDetails(counter.transform, pal);

            Mesh spongeMesh = CreateChamferedRectLoftMesh("Mesh_Esponja_Cozinha", 0.095f, 0.016f, 0.065f, 0.010f, 0.92f, 0.003f);
            CreateMeshObject(counter.transform, "Esponja_Amarela", spongeMesh, pal.metalBrass, new Vector3(-5.57f, 0.890f, -5.62f), new Vector3(0f, 16f, 0f), castShadows: false);
            CreateDetailBox(counter.transform, "Esponja_FibraVerde", new Vector3(-5.57f, 0.909f, -5.62f), new Vector3(0.088f, 0.006f, 0.059f), pal.fabricOlive, new Vector3(0f, 16f, 0f));

            GameObject drainer = CreateContainer(counter.transform, "Escorredor_Louca");
            drainer.transform.localPosition = new Vector3(-4.52f, 0.890f, -5.52f);
            GameObject drainerTray = CreateDetailBox(drainer.transform, "Bandeja_Escorredor", new Vector3(0f, 0.006f, 0f), new Vector3(0.42f, 0.012f, 0.34f), pal.stainlessSteel);
            BuildKitchenDishDrainerDetails(drainer.transform, drainerTray, pal);

            CreateBox(counter.transform, "Painel_Falso_Cuba", new Vector3(-5.135f, 0.782f, -5.24f), new Vector3(1.15f, 0.132f, 0.024f), pal.formicaCream);
            CreateDetailBox(counter.transform, "Friso_Madeira_Cuba", new Vector3(-5.135f, 0.714f, -5.235f), new Vector3(1.16f, 0.014f, 0.028f), pal.furnitureWood);

            GameObject sinkSupplyVisual = CreateContainer(counter.transform, "Produtos_Pia_Cozinha_Visual");
            sinkSupplyVisual.transform.localPosition = new Vector3(-5.135f, 0.110f, -5.52f);
            CreateDetailCylinder(sinkSupplyVisual.transform, "Porca_Sifao_Sup", new Vector3(0f, 0.618f, -0.03f), new Vector3(0.066f, 0.010f, 0.066f), pal.stainlessSteel);
            CreateDetailCylinder(sinkSupplyVisual.transform, "Sifao_Tubo_Vertical", new Vector3(0f, 0.48f, -0.03f), new Vector3(0.046f, 0.15f, 0.046f), pal.porcelainWhite);
            CreateDetailCylinder(sinkSupplyVisual.transform, "Sifao_Copo", new Vector3(0f, 0.32f, -0.03f), new Vector3(0.078f, 0.055f, 0.078f), pal.porcelainWhite);
            CreateDetailCylinder(sinkSupplyVisual.transform, "Sifao_Saida_Parede", new Vector3(0f, 0.34f, -0.15f), new Vector3(0.044f, 0.12f, 0.044f), pal.porcelainWhite, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(sinkSupplyVisual.transform, "Canopla_Sifao_Parede", new Vector3(0f, 0.34f, -0.252f), new Vector3(0.072f, 0.004f, 0.072f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            GameObject sinkNewspaper = BuildDetailedPaperSheet(sinkSupplyVisual.transform, "Jornal_Forro_Umido", new Vector3(-0.20f, 0f, 0.04f), 6f, new Vector2(0.40f, 0.30f), pal, isNewspaper: true, printedFaceMat: pal.details.newspaper1994, printAngleY: 180f);
            GameObject bleachBottle = BuildBleachJug(sinkSupplyVisual.transform, "Garrafa_AguaSanitaria", new Vector3(-0.25f, 0.0024f, 0.05f), 85f, pal);
            SnapToSupportTop(bleachBottle, sinkNewspaper);

            // Balde plástico vazado real com alça aramada apoiada na borda
            GameObject bucketGrp = CreateContainer(sinkSupplyVisual.transform, "Balde_Plastico");
            bucketGrp.transform.localPosition = new Vector3(0.24f, 0f, 0.02f);
            Mesh bucketMesh = CreateFacetedLatheVesselMesh("Mesh_KitchenBucket", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.092f),
                (0.218f, 0.124f),
                (0.232f, 0.128f),
                (0.232f, 0.118f),
                (0.214f, 0.116f),
                (0.008f, 0.086f),
                (0.008f, 0.000f)
            });
            CreateMeshObject(bucketGrp.transform, "Corpo_Balde_Plastico", bucketMesh, pal.plasticSlate, Vector3.zero, castShadows: true);
            Mesh bailMesh = CreateFacetedCShapeHandleMesh("Mesh_Alca_Balde_Cozinha", new[]
            {
                new Vector3(-0.122f, 0.222f, 0.000f),
                new Vector3(-0.128f, 0.200f, 0.055f),
                new Vector3( 0.000f, 0.178f, 0.132f),
                new Vector3( 0.128f, 0.200f, 0.055f),
                new Vector3( 0.122f, 0.222f, 0.000f)
            }, halfThicknessY: 0.004f, halfThicknessZ: 0.004f, sideAxis: Vector3.up);
            CreateMeshObject(bucketGrp.transform, "Alca_Balde_Metal", bailMesh, pal.stainlessSteel, Vector3.zero, castShadows: true);
            SnapToSupportTop(bucketGrp, counterFloor);

            InspectableObject sinkCabinetContents = MakeSpot(counter.transform, "Produtos_Pia_Cozinha_Interno", new Vector3(-5.135f, 0.29f, -5.48f), new Vector3(0.76f, 0.38f, 0.36f));
            sinkCabinetContents.ConfigureTask(
                id: "armario_pia_cozinha_opc",
                prompt: "Olhar embaixo da pia",
                subtitle: "Resto de detergente e jornal velho cobrindo a madeira úmida.",
                repeatSubtitle: "Cheiro de madeira molhada.",
                linear: false,
                sound: FoleySoundId.GenericTouch
            );

            GameObject sinkCabDoorsObj = CreateContainer(counter.transform, "Armario_Pia_Cozinha_Portas");
            GameObject sinkDoorWestPivot = CreateContainer(sinkCabDoorsObj.transform, "Porta_Esq_Pivot");
            sinkDoorWestPivot.transform.localPosition = new Vector3(-5.705f, 0.402f, -5.24f);
            CreateBox(sinkDoorWestPivot.transform, "Folha_Porta_Esq", new Vector3(0.282f, 0f, 0f), new Vector3(0.564f, 0.596f, 0.024f), pal.formicaCream, hasCollider: true);
            BuildKitchenFramedDoorFront(sinkDoorWestPivot.transform, "Esq", 0.282f, 0.564f, 0.596f, 0.008f, 0.51f, pal);

            GameObject sinkDoorEastPivot = CreateContainer(sinkCabDoorsObj.transform, "Porta_Dir_Pivot");
            sinkDoorEastPivot.transform.localPosition = new Vector3(-4.565f, 0.402f, -5.24f);
            CreateBox(sinkDoorEastPivot.transform, "Folha_Porta_Dir", new Vector3(-0.282f, 0f, 0f), new Vector3(0.564f, 0.596f, 0.024f), pal.formicaCream, hasCollider: true);
            BuildKitchenFramedDoorFront(sinkDoorEastPivot.transform, "Dir", -0.282f, 0.564f, 0.596f, -0.008f, -0.51f, pal);

            AnimatedContainerInteractable kitchenSinkCab = sinkCabDoorsObj.AddComponent<AnimatedContainerInteractable>();
            kitchenSinkCab.ConfigureSwingCabinet(
                id: "armario_pia_cozinha",
                primaryDoorPivot: sinkDoorWestPivot.transform,
                primaryOpenEuler: new Vector3(0f, -78f, 0f),
                promptOpen: "Abrir o armário da pia",
                promptClose: "Fechar o armário da pia",
                doorSfx: FoleySoundId.CupboardDoor,
                secondDoorPivot: sinkDoorEastPivot.transform,
                secondOpenEuler: new Vector3(0f, 78f, 0f)
            );
            kitchenSinkCab.RegisterInteriorObject(sinkCabinetContents);
            EditorUtility.SetDirty(kitchenSinkCab);

            // Gaveteiro leste do balcão: vão interno X = [-4.530 .. -3.790] = 0.740m (centro -4.160f)
            CreateDetailBox(counter.transform, "Travessa_Gav_1", new Vector3(-4.160f, 0.682f, -5.254f), new Vector3(0.744f, 0.012f, 0.036f), pal.furnitureWood);
            CreateDetailBox(counter.transform, "Travessa_Gav_2", new Vector3(-4.160f, 0.508f, -5.254f), new Vector3(0.744f, 0.012f, 0.036f), pal.furnitureWood);
            CreateDetailBox(counter.transform, "Travessa_Gav_3", new Vector3(-4.160f, 0.318f, -5.254f), new Vector3(0.744f, 0.012f, 0.036f), pal.furnitureWood);
            CreateDetailBox(counter.transform, "Corredica_Esq_Gav1", new Vector3(-4.514f, 0.745f, -5.50f), new Vector3(0.036f, 0.022f, 0.490f), pal.furnitureWood);
            CreateDetailBox(counter.transform, "Corredica_Dir_Gav1", new Vector3(-3.806f, 0.745f, -5.50f), new Vector3(0.036f, 0.022f, 0.490f), pal.furnitureWood);
            CreateDetailBox(counter.transform, "Corredica_Esq_Gav2", new Vector3(-4.514f, 0.572f, -5.50f), new Vector3(0.036f, 0.022f, 0.490f), pal.furnitureWood);
            CreateDetailBox(counter.transform, "Corredica_Dir_Gav2", new Vector3(-3.806f, 0.572f, -5.50f), new Vector3(0.036f, 0.022f, 0.490f), pal.furnitureWood);

            GameObject utilDrawerRoot = CreateContainer(counter.transform, "Gaveta_Utilitaria_Cozinha");
            utilDrawerRoot.transform.localPosition = new Vector3(-4.160f, 0.768f, -5.51f);

            GameObject utilBottom = CreateBox(utilDrawerRoot.transform, "Fundo_Gaveta", new Vector3(0f, -0.058f, 0.024f), new Vector3(0.652f, 0.016f, 0.476f), pal.woodLight, hasCollider: true);
            CreateDetailBox(utilDrawerRoot.transform, "Lateral_Esq_Gaveta", new Vector3(-0.33f, -0.016f, 0.022f), new Vector3(0.020f, 0.108f, 0.490f), pal.woodLight);
            CreateDetailBox(utilDrawerRoot.transform, "Lateral_Dir_Gaveta", new Vector3(0.33f, -0.016f, 0.022f), new Vector3(0.020f, 0.108f, 0.490f), pal.woodLight);
            CreateDetailBox(utilDrawerRoot.transform, "Traseira_Gaveta", new Vector3(0f, -0.012f, -0.210f), new Vector3(0.644f, 0.092f, 0.018f), pal.woodLight);
            CreateBox(utilDrawerRoot.transform, "Frente_Gaveta_Utilitaria", new Vector3(0f, 0f, 0.27f), new Vector3(0.72f, 0.156f, 0.024f), pal.formicaCream, hasCollider: true);
            BuildKitchenFramedDrawerFront(utilDrawerRoot.transform, "Utilitaria", new Vector3(0f, 0f, 0.27f), 0.72f, 0.156f, pal);

            GameObject flashlight = BuildDetailedCaplessFlashlight(utilDrawerRoot.transform, "Lanterna_Sem_Tampa", new Vector3(-0.14f, -0.058f, 0.09f), 198f, pal);
            SnapToSupportTop(flashlight, utilBottom);
            GameObject twineGrp = CreateContainer(utilDrawerRoot.transform, "Rolo_Barbante");
            twineGrp.transform.localPosition = new Vector3(0.22f, -0.050f, -0.09f);
            CreateDetailCylinder(twineGrp.transform, "Cone_Papelao_Barbante", new Vector3(0f, 0.026f, 0f), new Vector3(0.032f, 0.026f, 0.032f), pal.folderKraft);
            CreateDetailCylinder(twineGrp.transform, "Miolo_Escuro_Barbante", new Vector3(0f, 0.027f, 0f), new Vector3(0.018f, 0.026f, 0.018f), pal.shadowBlack);
            CreateDetailCylinder(twineGrp.transform, "Enrolamento_Barbante", new Vector3(0f, 0.022f, 0f), new Vector3(0.068f, 0.018f, 0.068f), pal.paperWhite, castShadows: true);
            CreateDetailBox(twineGrp.transform, "Ponta_Barbante", new Vector3(-0.042f, 0.002f, 0.024f), new Vector3(0.045f, 0.004f, 0.004f), pal.paperWhite, new Vector3(0f, 22f, 0f));
            SnapToSupportTop(twineGrp, utilBottom);
            GameObject tapeGrp = CreateContainer(utilDrawerRoot.transform, "Fita_Isolante_Grp");
            GameObject tape = CreateDetailCylinder(tapeGrp.transform, "Fita_Isolante", new Vector3(0.06f, -0.049f, -0.11f), new Vector3(0.072f, 0.009f, 0.072f), pal.shadowBlack);
            SnapToSupportTop(tape, utilBottom);
            CreateDetailCylinder(tapeGrp.transform, "Miolo_Fita_Isolante", new Vector3(0.06f, tape.transform.localPosition.y + 0.0025f, -0.11f), new Vector3(0.036f, 0.009f, 0.036f), pal.paperWhite);
            GameObject matches = CreateContainer(utilDrawerRoot.transform, "Caixa_Fosforos");
            matches.transform.localPosition = new Vector3(-0.22f, -0.050f, -0.04f);
            matches.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
            CreateDetailBox(matches.transform, "Corpo_Fosforos", Vector3.zero, new Vector3(0.052f, 0.016f, 0.038f), pal.calendarRed);
            CreatePrintedOverlayQuad(matches.transform, "Capa_Fosforos", new Vector3(0f, 0.0086f, 0f), new Vector2(0.048f, 0.034f), pal.details.matchboxCover, new Vector3(90f, 180f, 0f));
            SnapToSupportTop(matches, utilBottom);

            AnimatedContainerInteractable utilDrawer = utilDrawerRoot.AddComponent<AnimatedContainerInteractable>();
            utilDrawer.ConfigureSlideDrawer(
                id: "gaveta_utilitaria_cozinha",
                drawerTransform: utilDrawerRoot.transform,
                slideOffset: new Vector3(0f, 0f, 0.35f),
                promptOpen: "Abrir a primeira gaveta da cozinha",
                promptClose: "Fechar a primeira gaveta",
                slideSfx: FoleySoundId.DrawerSlide
            );
            EditorUtility.SetDirty(utilDrawer);
            containers.kitchenUtilityDrawer = utilDrawer;

            GameObject cutleryDrawerRoot = CreateContainer(counter.transform, "Gaveta_Talheres_Cozinha");
            cutleryDrawerRoot.transform.localPosition = new Vector3(-4.160f, 0.595f, -5.51f);
            GameObject cutleryBottom = CreateBox(cutleryDrawerRoot.transform, "Fundo_Gaveta", new Vector3(0f, -0.058f, 0.024f), new Vector3(0.652f, 0.016f, 0.476f), pal.woodLight, hasCollider: true);
            CreateDetailBox(cutleryDrawerRoot.transform, "Lateral_Esq", new Vector3(-0.33f, -0.016f, 0.022f), new Vector3(0.020f, 0.108f, 0.490f), pal.woodLight);
            CreateDetailBox(cutleryDrawerRoot.transform, "Lateral_Dir", new Vector3(0.33f, -0.016f, 0.022f), new Vector3(0.020f, 0.108f, 0.490f), pal.woodLight);
            CreateDetailBox(cutleryDrawerRoot.transform, "Traseira", new Vector3(0f, -0.012f, -0.210f), new Vector3(0.644f, 0.092f, 0.018f), pal.woodLight);
            CreateBox(cutleryDrawerRoot.transform, "Frente_Gaveta_Talheres", new Vector3(0f, 0f, 0.27f), new Vector3(0.72f, 0.162f, 0.024f), pal.formicaCream, hasCollider: true);
            BuildKitchenFramedDrawerFront(cutleryDrawerRoot.transform, "Talheres", new Vector3(0f, 0f, 0.27f), 0.72f, 0.162f, pal);

            CreateDetailBox(cutleryDrawerRoot.transform, "Divisor_Talheres_1", new Vector3(-0.11f, -0.038f, 0.08f), new Vector3(0.012f, 0.04f, 0.30f), pal.furnitureWood);
            CreateDetailBox(cutleryDrawerRoot.transform, "Divisor_Talheres_2", new Vector3(0.04f, -0.038f, 0.08f), new Vector3(0.012f, 0.04f, 0.30f), pal.furnitureWood);
            BuildKitchenCutleryDrawerUtensils(cutleryDrawerRoot.transform, cutleryBottom, pal);

            InspectableObject cutleryInsp = MakeSpot(cutleryDrawerRoot.transform, "Talheres_Gaveta_Interno", new Vector3(0f, -0.02f, 0.08f), new Vector3(0.56f, 0.10f, 0.28f));
            cutleryInsp.ConfigureTask(
                id: "gaveta_talheres_opc",
                prompt: "Olhar os talheres",
                subtitle: "Só uso o mesmo garfo e a mesma caneca todo dia.",
                linear: false,
                sound: FoleySoundId.MetalInspect
            );

            AnimatedContainerInteractable cutleryAnim = cutleryDrawerRoot.AddComponent<AnimatedContainerInteractable>();
            cutleryAnim.ConfigureSlideDrawer(
                id: "gaveta_talheres_cozinha",
                drawerTransform: cutleryDrawerRoot.transform,
                slideOffset: new Vector3(0f, 0f, 0.33f),
                promptOpen: "Abrir a gaveta de talheres",
                promptClose: "Fechar a gaveta de talheres",
                slideSfx: FoleySoundId.DrawerSlide
            );
            cutleryAnim.RegisterInteriorObject(cutleryInsp);
            EditorUtility.SetDirty(cutleryAnim);

            CreateBox(counter.transform, "Frente_Gaveta_3", new Vector3(-4.160f, 0.413f, -5.24f), new Vector3(0.72f, 0.178f, 0.024f), pal.formicaCream);
            BuildKitchenFramedDrawerFront(counter.transform, "3", new Vector3(-4.160f, 0.413f, -5.24f), 0.72f, 0.178f, pal);

            CreateBox(counter.transform, "Frente_Gaveta_4_Funda", new Vector3(-4.160f, 0.2045f, -5.24f), new Vector3(0.72f, 0.210f, 0.024f), pal.formicaCream);
            BuildKitchenFramedDrawerFront(counter.transform, "4", new Vector3(-4.160f, 0.2045f, -5.24f), 0.72f, 0.210f, pal);

            GameObject upperCab = CreateContainer(root, "Armario_Aereo_Cozinha");
            upperCab.transform.localPosition = new Vector3(-4.75f, 1.94f, -5.67f);
            CreateBox(upperCab.transform, "Corpo_Aereo", Vector3.zero, new Vector3(1.96f, 0.58f, 0.32f), pal.furnitureWood);
            BuildKitchenUpperCabinetAndSpiceDetails(upperCab.transform, pal);

            GameObject stoveObj = CreateContainer(root, "Fogao");
            stoveObj.transform.localPosition = new Vector3(-3.31f, 0f, -5.495f);
            BuildKitchenStoveDetails(stoveObj.transform, pal);

            InspectableObject stove = MakeSpot(root, "Fogao_Interagir", new Vector3(-3.31f, 0.60f, -5.46f), new Vector3(0.60f, 1.05f, 0.64f));
            stove.ConfigureTask(
                id: "fogao_opc",
                prompt: "Olhar o fogão",
                subtitle: "Faz dias que só uso uma boca pra esquentar água pro café.",
                repeatSubtitle: "Frio.",
                linear: false,
                sound: FoleySoundId.MetalInspect
            );

            // Aparador na parede leste da cozinha (entre arco -1.62 e corredor +0.38 -> voltado para -X)
            GameObject sideboard = CreateContainer(root, "Aparador_Copa");
            sideboard.transform.localPosition = new Vector3(-3.20f, 0f, -0.78f);
            BuildKitchenSideboardDetails(sideboard.transform, pal);
            GameObject sideboardRunner = CreateDetailBox(sideboard.transform, "Trilho_Bordado", new Vector3(-0.01f, 0.8415f, 0f), new Vector3(0.28f, 0.003f, 0.94f), pal.paperWhite);
            CreateDetailBox(sideboard.transform, "Borda_Renda_Sul", new Vector3(-0.01f, 0.8415f, -0.478f), new Vector3(0.24f, 0.003f, 0.016f), pal.bedPastelBlue);
            CreateDetailBox(sideboard.transform, "Borda_Renda_Norte", new Vector3(-0.01f, 0.8415f,  0.478f), new Vector3(0.24f, 0.003f, 0.016f), pal.bedPastelBlue);

            // Pratinho de chaves reconstruído em malha octogonal fechada e sem frestas
            Mesh keyTrayMesh = CreateFacetedLatheVesselMesh("Mesh_KeyTray", 8, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.105f),
                (0.006f, 0.122f),
                (0.018f, 0.130f),
                (0.018f, 0.120f),
                (0.006f, 0.106f),
                (0.006f, 0.000f)
            });
            GameObject keyTray = CreateMeshObject(root, "Pratinho_Chaves_Copa", keyTrayMesh, pal.porcelainWhite, new Vector3(-3.22f, 0.8430f, -0.78f), new Vector3(0f, 22.5f, 0f), castShadows: true);
            SnapToSupportTop(keyTray, sideboardRunner);

            GameObject gateKey = BuildDetailedKeyWithTag(root, "Chaveiro_Portao", new Vector3(-3.260f, 0.8490f, -0.735f), 220f, pal.stainlessSteel, pal.metalDark, pal);
            SnapToSupportWorldY(gateKey, 0.8490f);
            GameObject padlock = BuildKitchenPadlockWithShackle(root, "Cadeado_Velho", new Vector3(-3.265f, 0.8490f, -0.825f), -20f, pal);
            SnapToSupportWorldY(padlock, 0.8490f);
            GameObject coin1 = CreateDetailCylinder(root, "Moeda_1", new Vector3(-3.175f, 0.8505f, -0.810f), new Vector3(0.022f, 0.0015f, 0.022f), pal.stainlessSteel);
            SnapToSupportWorldY(coin1, 0.8490f);
            GameObject coin2 = CreateDetailCylinder(root, "Moeda_2", new Vector3(-3.210f, 0.8505f, -0.850f), new Vector3(0.020f, 0.0015f, 0.020f), pal.metalBrass);
            SnapToSupportWorldY(coin2, 0.8490f);
            GameObject sideboardMail = BuildUnopenedMailBundle(root, "Correspondencia_Aparador", new Vector3(-3.21f, 0.843f, -1.16f), 6f, pal, count: 3, envelopePrintMat: pal.details.mailEnvelope, printAngleY: 90f);
            SnapToSupportTop(sideboardMail, sideboardRunner);

            // Mesa de jantar com tampo chanfrado e pés quadrados afunilados com ponteiras de latão
            GameObject dining = CreateContainer(root, "Mesa_Jantar_Vazia");
            dining.transform.localPosition = new Vector3(-4.74f, 0f, -2.95f);
            Mesh diningTopMesh = CreateChamferedRectLoftMesh("Mesh_Tampo_Jantar_Copa", 1.04f, 0.050f, 0.86f, 0.020f, 0.985f, 0.006f);
            GameObject diningTop = CreateMeshObject(dining.transform, "Tampo_Jantar", diningTopMesh, pal.furnitureWood, new Vector3(0f, 0.715f, 0f), castShadows: true);
            BoxCollider diningTopCol = diningTop.AddComponent<BoxCollider>();
            diningTopCol.center = new Vector3(0f, 0.025f, 0f);
            diningTopCol.size = new Vector3(1.04f, 0.050f, 0.86f);
            CreateDetailBox(dining.transform, "Saia_Norte", new Vector3(0f, 0.685f, 0.35f), new Vector3(0.78f, 0.06f, 0.03f), pal.furnitureWood);
            CreateDetailBox(dining.transform, "Saia_Sul", new Vector3(0f, 0.685f, -0.35f), new Vector3(0.78f, 0.06f, 0.03f), pal.furnitureWood);
            CreateDetailBox(dining.transform, "Saia_Leste", new Vector3(0.42f, 0.685f, 0f), new Vector3(0.03f, 0.06f, 0.64f), pal.furnitureWood);
            CreateDetailBox(dining.transform, "Saia_Oeste", new Vector3(-0.42f, 0.685f, 0f), new Vector3(0.03f, 0.06f, 0.64f), pal.furnitureWood);

            Mesh tableLegMesh = CreateChamferedRectLoftMesh("Mesh_Pe_Mesa_Jantar", 0.054f, 0.703f, 0.054f, 0.008f, 0.96f, 0.004f);
            float[] tlx = { -0.42f, 0.42f, -0.42f, 0.42f };
            float[] tlz = { -0.35f, -0.35f, 0.35f, 0.35f };
            for (int tl = 0; tl < 4; tl++)
            {
                GameObject legObj = CreateMeshObject(dining.transform, $"Pe_Jantar_{tl + 1}", tableLegMesh, pal.furnitureWood, new Vector3(tlx[tl], 0.012f, tlz[tl]), castShadows: true);
                BoxCollider legCol = legObj.AddComponent<BoxCollider>();
                legCol.center = new Vector3(0f, 0.3515f, 0f);
                legCol.size = new Vector3(0.054f, 0.703f, 0.054f);
                CreateDetailBox(dining.transform, $"Ponteira_Pe_Jantar_{tl + 1}", new Vector3(tlx[tl], 0.0075f, tlz[tl]), new Vector3(0.058f, 0.015f, 0.058f), pal.metalBrass);
            }

            BuildDetailedDiningChair(dining.transform, "Cadeira_Jantar_Norte", new Vector3(0f, 0f, 0.44f), 180f, pal);
            BuildDetailedDiningChair(dining.transform, "Cadeira_Jantar_Sul", new Vector3(0f, 0f, -0.44f), 0f, pal);
            BuildDetailedDiningChair(dining.transform, "Cadeira_Jantar_Leste", new Vector3(0.52f, 0f, 0f), -90f, pal);
            BuildDetailedDiningChair(dining.transform, "Cadeira_Jantar_Cian", new Vector3(-0.60f, 0f, 0.08f), 76f, pal);

            GameObject diningBills = BuildUnopenedMailBundle(dining.transform, "Contas_Fechadas_Mesa", new Vector3(-0.02f, 0.765f, -0.05f), 14f, pal, count: 4, envelopePrintMat: pal.details.mailEnvelope, printAngleY: 180f);
            SnapToSupportTop(diningBills, diningTop);
            GameObject dirtyPlate = BuildDirtyDishAndCutlery(dining.transform, "Prato_Miojo_Cian", new Vector3(-0.24f, 0.765f, 0.24f), -18f, pal);
            SnapToSupportTop(dirtyPlate, diningTop);
            GameObject diningMug = BuildDetailedCoffeeMug(dining.transform, "Caneca_Cafe_Velho", new Vector3(-0.28f, 0.765f, -0.18f), -35f, pal, withSaucer: true);
            SnapToSupportTop(diningMug, diningTop);
            GameObject napkinHolder = BuildKitchenNapkinHolder(dining.transform, "Porta_Guardanapo", new Vector3(0.16f, 0.765f, 0.04f), -8f, pal);
            SnapToSupportTop(napkinHolder, diningTop);

            // Lixeira com pedal no canto noroeste da copa (fora da passagem de Batente_Copa)
            BuildKitchenPedalBin(root, new Vector3(-6.35f, 0f, 0.18f), pal);

            InspectableObject diningInsp = MakeSpot(root, "Mesa_Jantar_Interagir", new Vector3(-4.74f, 0.50f, -2.95f), new Vector3(1.32f, 1.00f, 1.20f));
            diningInsp.ConfigureTask(
                id: "mesa_jantar_opc",
                prompt: "Olhar a mesa da copa",
                subtitle: "Cobrança e sujeira.",
                repeatSubtitle: "Já esteve cheia.",
                linear: false,
                sound: FoleySoundId.WoodInspect
            );

            // Quadro de força na parede oeste acima do friso/azulejo (Y=1.84m) com colar metálico contínuo e fiação interna
            GameObject breaker = CreateContainer(root, "Quadro_Forca");
            breaker.transform.localPosition = new Vector3(-6.508f, 1.84f, -1.05f);
            CreateBox(breaker.transform, "Corpo_Quadro_Fundo", new Vector3(-0.017f, 0f, 0f), new Vector3(0.030f, 0.58f, 0.44f), pal.metalDark, hasCollider: false);
            CreateDetailBox(breaker.transform, "Moldura_Embutida", new Vector3(0.002f, 0f, 0f), new Vector3(0.020f, 0.60f, 0.46f), pal.metalDark);
            CreateDetailBox(breaker.transform, "Cavidade_Escura", new Vector3(0.0075f, 0f, 0f), new Vector3(0.015f, 0.50f, 0.36f), pal.shadowBlack);
            CreateDetailCylinder(breaker.transform, "Eletroduto_Superior", new Vector3(-0.01f, 0.56f, -0.08f), new Vector3(0.028f, 0.27f, 0.028f), pal.metalDark);
            CreateDetailCylinder(breaker.transform, "Eletroduto_Superior_2", new Vector3(-0.01f, 0.56f, 0.08f), new Vector3(0.024f, 0.27f, 0.024f), pal.metalDark);

            CreateDetailBox(breaker.transform, "Placa_Barramento", new Vector3(0.021f, 0.09f, 0f), new Vector3(0.012f, 0.16f, 0.28f), pal.plasticBeige90s);
            for (int dj = -1; dj <= 1; dj++)
            {
                CreateDetailBox(breaker.transform, $"Disjuntor_{dj + 2}", new Vector3(0.032f, 0.10f, dj * 0.08f), new Vector3(0.024f, 0.085f, 0.048f), pal.metalDark);
                CreateDetailBox(breaker.transform, $"Alavanca_Disj_{dj + 2}", new Vector3(0.046f, 0.105f, dj * 0.08f), new Vector3(0.014f, 0.018f, 0.022f), pal.porcelainWhite, new Vector3(0f, 0f, dj == 1 ? -18f : 18f));
                CreateDetailBox(breaker.transform, $"Etiqueta_Fase_{dj + 2}", new Vector3(0.016f, 0.20f, dj * 0.08f), new Vector3(0.004f, 0.026f, 0.056f), pal.paperWhite);
            }
            CreateDetailBox(breaker.transform, "Bloco_Porcelana_Fusivel", new Vector3(0.022f, -0.10f, 0f), new Vector3(0.022f, 0.10f, 0.12f), pal.porcelainWhite);
            CreateDetailCylinder(breaker.transform, "Base_Soquete_Fusivel", new Vector3(0.032f, -0.10f, 0f), new Vector3(0.052f, 0.010f, 0.052f), pal.metalBrass, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(breaker.transform, "Fundo_Escuro_Soquete", new Vector3(0.042f, -0.10f, 0f), new Vector3(0.032f, 0.005f, 0.032f), pal.shadowBlack, new Vector3(0f, 0f, 90f));
            BuildKitchenBreakerBoxEnclosures(breaker.transform, pal);

            GameObject breakerDoorHost = CreateContainer(breaker.transform, "Porta_Quadro_Interagir");
            GameObject breakerDoorPivot = CreateContainer(breakerDoorHost.transform, "Porta_Quadro_Pivot");
            breakerDoorPivot.transform.localPosition = new Vector3(0.068f, 0f, -0.195f);
            CreateBox(breakerDoorPivot.transform, "Folha_Porta_Quadro", new Vector3(0f, 0f, 0.195f), new Vector3(0.020f, 0.54f, 0.40f), pal.porcelainWhite, hasCollider: true);
            CreateDetailBox(breakerDoorPivot.transform, "Etiqueta_Quadro", new Vector3(0.014f, 0.14f, 0.195f), new Vector3(0.008f, 0.09f, 0.24f), pal.paperWhite);
            for (int v = 0; v < 4; v++)
            {
                CreateDetailBox(breakerDoorPivot.transform, $"Veneziana_Quadro_{v}", new Vector3(0.014f, -0.09f - v * 0.028f, 0.195f), new Vector3(0.008f, 0.010f, 0.22f), pal.metalDark);
            }
            CreateDetailBox(breakerDoorPivot.transform, "Trava_Metal", new Vector3(0.017f, -0.01f, 0.355f), new Vector3(0.016f, 0.065f, 0.022f), pal.metalBrass);

            AnimatedContainerInteractable breakerContainer = breakerDoorHost.AddComponent<AnimatedContainerInteractable>();
            breakerContainer.ConfigureSwingCabinet(
                id: "quadro_forca_porta",
                primaryDoorPivot: breakerDoorPivot.transform,
                primaryOpenEuler: new Vector3(0f, 84f, 0f),
                promptOpen: "Abrir o quadro de força",
                promptClose: "Fechar o quadro de força",
                doorSfx: FoleySoundId.MetalInspect
            );
            EditorUtility.SetDirty(breakerContainer);
            containers.kitchenBreakerBox = breakerContainer;
        }

        private static void BuildBathroom(Transform root, GreyboxPalette pal, ref HouseContainers containers)
        {
            // Azulejos aparados nas quinas NW e NE sem Z-fighting com paredes laterais nem com frisos perpendiculares
            BuildTileWainscotingZ(root, "Azulejo_Banheiro_Oeste", -6.535f, 2.18f, 6.04f, 1.68f, pal);
            BuildTileWainscotingZ(root, "Azulejo_Banheiro_Leste", -3.5295f, 2.18f, 6.04f, 1.68f, pal);
            BuildTileWainscotingX(root, "Azulejo_Banheiro_Norte", -6.524f, -3.540f, 6.035f, 1.68f, pal);
            BuildFloorDrain(root, "Ralo_Banheiro_Seco", new Vector3(-4.62f, 0f, 4.42f), pal);

            GameObject bathRug = CreateContainer(root, "Tapete_Banheiro");
            bathRug.transform.localPosition = new Vector3(-4.02f, 0f, 5.06f);
            CreateDetailBox(bathRug.transform, "Tapete_Banheiro_Borda", new Vector3(0f, 0.0025f, 0f), new Vector3(0.74f, 0.005f, 0.46f), pal.porcelainWhite);
            CreateDetailBox(bathRug.transform, "Tapete_Banheiro_Centro", new Vector3(0f, 0.0068f, 0f), new Vector3(0.66f, 0.0036f, 0.38f), pal.bedPastelBlue);
            CreateDetailBox(bathRug.transform, "Tapete_Banheiro_Listra", new Vector3(0f, 0.0098f, 0f), new Vector3(0.58f, 0.0024f, 0.08f), pal.paperWhite);
            for (int bf = -2; bf <= 2; bf++)
            {
                float fz = bf * 0.085f;
                CreateDetailBox(bathRug.transform, $"Franja_Tapete_O_{bf + 2}", new Vector3(-0.388f, 0.0015f, fz), new Vector3(0.036f, 0.003f, 0.022f), pal.paperWhite);
                CreateDetailBox(bathRug.transform, $"Franja_Tapete_L_{bf + 2}", new Vector3( 0.388f, 0.0015f, fz), new Vector3(0.036f, 0.003f, 0.022f), pal.paperWhite);
            }

            // Porta-toalha ancorado rente ao azulejo da parede leste (X=-3.5345f, voltado para -X) com toalha dobrada contínua
            BuildBathroomTowelRack(root, pal);

            // Gabinete da pia no nordeste do banheiro: X=[-4.50..-3.54], Z=[+5.45..+6.01], bancada em Y=0.890m
            GameObject vanity = CreateContainer(root, "Gabinete_Pia_Banheiro");
            CreateBox(vanity.transform, "Soculo_Gabinete", new Vector3(-4.02f, 0.040f, 5.75f), new Vector3(0.92f, 0.080f, 0.50f), pal.baseboardWood);
            GameObject vanityBase = CreateBox(vanity.transform, "Base_Gabinete", new Vector3(-4.02f, 0.0925f, 5.73f), new Vector3(0.90f, 0.025f, 0.54f), pal.furnitureWood);
            CreateBox(vanity.transform, "Lateral_Esq_Gabinete", new Vector3(-4.485f, 0.4775f, 5.73f), new Vector3(0.030f, 0.745f, 0.56f), pal.furnitureWood);
            CreateBox(vanity.transform, "Lateral_Dir_Gabinete", new Vector3(-3.555f, 0.4775f, 5.73f), new Vector3(0.030f, 0.745f, 0.56f), pal.furnitureWood);
            CreateBox(vanity.transform, "Fundo_Gabinete", new Vector3(-4.02f, 0.4775f, 5.9975f), new Vector3(0.90f, 0.745f, 0.025f), pal.furnitureWood);
            CreateDetailBox(vanity.transform, "Interior_Escuro_Gabinete", new Vector3(-4.02f, 0.44f, 5.9815f), new Vector3(0.86f, 0.66f, 0.008f), pal.shadowBlack);

            CreateBox(vanity.transform, "Painel_Falso_Bacia", new Vector3(-4.02f, 0.798f, 5.46f), new Vector3(0.90f, 0.104f, 0.024f), pal.baseboardWood);
            CreateDetailBox(vanity.transform, "Friso_Avental_Pia", new Vector3(-4.02f, 0.742f, 5.455f), new Vector3(0.92f, 0.014f, 0.028f), pal.furnitureWood);

            CreateDetailCylinder(vanity.transform, "Sifao_Tubo_Pia", new Vector3(-4.02f, 0.62f, 5.74f), new Vector3(0.036f, 0.14f, 0.036f), pal.stainlessSteel);
            CreateDetailCylinder(vanity.transform, "Porca_Sifao_Sup", new Vector3(-4.02f, 0.72f, 5.74f), new Vector3(0.054f, 0.010f, 0.054f), pal.stainlessSteel);
            CreateDetailCylinder(vanity.transform, "Sifao_Copo_Pia", new Vector3(-4.02f, 0.47f, 5.74f), new Vector3(0.064f, 0.045f, 0.064f), pal.stainlessSteel);
            CreateDetailCylinder(vanity.transform, "Porca_Sifao_Copo", new Vector3(-4.02f, 0.525f, 5.74f), new Vector3(0.072f, 0.008f, 0.072f), pal.stainlessSteel);
            CreateDetailCylinder(vanity.transform, "Sifao_Parede_Pia", new Vector3(-4.02f, 0.49f, 5.87f), new Vector3(0.034f, 0.12f, 0.034f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(vanity.transform, "Canopla_Sifao_Fundo", new Vector3(-4.02f, 0.49f, 5.974f), new Vector3(0.068f, 0.004f, 0.068f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));

            GameObject bleachJug = BuildBleachJug(vanity.transform, "Frasco_Agua_Sanitaria", new Vector3(-4.22f, 0.105f, 5.70f), 112f, pal);
            SnapToSupportTop(bleachJug, vanityBase);
            GameObject foldedCloth = BuildFoldedClothMeshObject(vanity.transform, "Pano_Seco", "Mesh_Pano_Dobrado",
                new Vector3(-3.84f, 0.105f, 5.67f), new Vector3(0f, 14f, 0f), new Vector3(0.84f, 0.56f, 2.2f), pal.paperWhite);
            SnapToSupportTop(foldedCloth, vanityBase);
            GameObject reserveRoll = BuildToiletPaperRoll(vanity.transform, "Rolo_Papel_Reserva", new Vector3(-3.80f, 0.105f, 5.84f), Vector3.zero, pal);
            SnapToSupportTop(reserveRoll, vanityBase);

            InspectableObject vanityInside = MakeSpot(vanity.transform, "Produtos_Gabinete_Banheiro_Opc", new Vector3(-4.02f, 0.28f, 5.68f), new Vector3(0.60f, 0.42f, 0.30f));
            vanityInside.ConfigureTask(
                id: "gabinete_banheiro_opc",
                prompt: "Olhar embaixo da pia",
                subtitle: "Pano encardido e água sanitária. Faz tempo que não limpo essa casa.",
                repeatSubtitle: "Nada de útil aqui.",
                linear: false,
                sound: FoleySoundId.GenericTouch
            );

            GameObject vanityDoorsHost = CreateContainer(vanity.transform, "Portas_Gabinete_Banheiro");
            float[] vanHingeYs = { -0.22f, 0f, 0.22f };

            GameObject vanWestPivot = CreateContainer(vanityDoorsHost.transform, "Porta_Oeste_Pivot");
            vanWestPivot.transform.localPosition = new Vector3(-4.47f, 0.423f, 5.46f);
            CreateBox(vanWestPivot.transform, "Folha_Porta_Oeste", new Vector3(0.223f, 0f, 0f), new Vector3(0.446f, 0.626f, 0.024f), pal.furnitureWood, hasCollider: true);
            BuildOpenFrameTrimBars(vanWestPivot.transform, "Quadro_Veneziana_O", new Vector3(0.223f, 0f, -0.015f), 0.36f, 0.50f, 0.028f, 0.008f, pal.baseboardWood);
            CreateDetailBox(vanWestPivot.transform, "Fundo_Veneziana_O", new Vector3(0.223f, 0f, -0.013f), new Vector3(0.304f, 0.444f, 0.002f), pal.woodMahogany);
            for (int lv = -3; lv <= 3; lv++)
            {
                CreateDetailBox(vanWestPivot.transform, $"Aleta_O_{lv + 4}", new Vector3(0.223f, lv * 0.058f, -0.0185f), new Vector3(0.300f, 0.022f, 0.006f), pal.furnitureWood, new Vector3(20f, 0f, 0f));
            }
            CreateDetailCylinder(vanWestPivot.transform, "Roseta_Puxador_O", new Vector3(0.39f, 0.08f, -0.014f), new Vector3(0.022f, 0.002f, 0.022f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(vanWestPivot.transform, "Haste_Puxador_O", new Vector3(0.39f, 0.08f, -0.020f), new Vector3(0.010f, 0.005f, 0.010f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(vanWestPivot.transform, "Puxador_Oeste", new Vector3(0.39f, 0.08f, -0.029f), new Vector3(0.026f, 0.006f, 0.026f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            for (int hi = 0; hi < vanHingeYs.Length; hi++)
                CreateDetailCylinder(vanWestPivot.transform, $"Dobradica_Gab_O_{hi}", new Vector3(0f, vanHingeYs[hi], -0.012f), new Vector3(0.012f, 0.018f, 0.012f), pal.stainlessSteel);

            GameObject vanEastPivot = CreateContainer(vanityDoorsHost.transform, "Porta_Leste_Pivot");
            vanEastPivot.transform.localPosition = new Vector3(-3.57f, 0.423f, 5.46f);
            CreateBox(vanEastPivot.transform, "Folha_Porta_Leste", new Vector3(-0.223f, 0f, 0f), new Vector3(0.446f, 0.626f, 0.024f), pal.furnitureWood, hasCollider: true);
            BuildOpenFrameTrimBars(vanEastPivot.transform, "Quadro_Veneziana_L", new Vector3(-0.223f, 0f, -0.015f), 0.36f, 0.50f, 0.028f, 0.008f, pal.baseboardWood);
            CreateDetailBox(vanEastPivot.transform, "Fundo_Veneziana_L", new Vector3(-0.223f, 0f, -0.013f), new Vector3(0.304f, 0.444f, 0.002f), pal.woodMahogany);
            for (int lv = -3; lv <= 3; lv++)
            {
                CreateDetailBox(vanEastPivot.transform, $"Aleta_L_{lv + 4}", new Vector3(-0.223f, lv * 0.058f, -0.0185f), new Vector3(0.300f, 0.022f, 0.006f), pal.furnitureWood, new Vector3(20f, 0f, 0f));
            }
            CreateDetailCylinder(vanEastPivot.transform, "Roseta_Puxador_L", new Vector3(-0.39f, 0.08f, -0.014f), new Vector3(0.022f, 0.002f, 0.022f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(vanEastPivot.transform, "Haste_Puxador_L", new Vector3(-0.39f, 0.08f, -0.020f), new Vector3(0.010f, 0.005f, 0.010f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(vanEastPivot.transform, "Puxador_Leste", new Vector3(-0.39f, 0.08f, -0.029f), new Vector3(0.026f, 0.006f, 0.026f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            for (int hi = 0; hi < vanHingeYs.Length; hi++)
                CreateDetailCylinder(vanEastPivot.transform, $"Dobradica_Gab_L_{hi}", new Vector3(0f, vanHingeYs[hi], -0.012f), new Vector3(0.012f, 0.018f, 0.012f), pal.stainlessSteel);

            AnimatedContainerInteractable vanityContainer = vanityDoorsHost.AddComponent<AnimatedContainerInteractable>();
            vanityContainer.ConfigureSwingCabinet(
                id: "gabinete_pia_banheiro",
                primaryDoorPivot: vanWestPivot.transform,
                primaryOpenEuler: new Vector3(0f, 78f, 0f),
                promptOpen: "Abrir o gabinete da pia",
                promptClose: "Fechar o gabinete da pia",
                doorSfx: FoleySoundId.CupboardDoor,
                secondDoorPivot: vanEastPivot.transform,
                secondOpenEuler: new Vector3(0f, -78f, 0f)
            );
            vanityContainer.RegisterInteriorObject(vanityInside);
            EditorUtility.SetDirty(vanityContainer);

            GameObject sinkTop = CreateContainer(root, "Bancada_Porcelana");
            GameObject sinkRimW = CreateBox(sinkTop.transform, "Borda_Oeste_Pia", new Vector3(-4.40f, 0.870f, 5.73f), new Vector3(0.22f, 0.040f, 0.58f), pal.porcelainWhite);
            GameObject sinkRimE = CreateBox(sinkTop.transform, "Borda_Leste_Pia", new Vector3(-3.64f, 0.870f, 5.73f), new Vector3(0.22f, 0.040f, 0.58f), pal.porcelainWhite);
            CreateBox(sinkTop.transform, "Borda_Tras_Pia", new Vector3(-4.02f, 0.870f, 5.935f), new Vector3(0.54f, 0.040f, 0.17f), pal.porcelainWhite);
            CreateBox(sinkTop.transform, "Borda_Frente_Pia", new Vector3(-4.02f, 0.870f, 5.49f), new Vector3(0.54f, 0.040f, 0.10f), pal.porcelainWhite);
            CreateDetailBox(sinkTop.transform, "Frontao_Porcelana", new Vector3(-4.02f, 0.925f, 6.019f), new Vector3(0.98f, 0.070f, 0.022f), pal.porcelainWhite);

            GameObject bathBasinFloor = CreateDetailBox(sinkTop.transform, "Cuba_Pia_Fundo", new Vector3(-4.02f, 0.766f, 5.69f), new Vector3(0.530f, 0.016f, 0.290f), pal.porcelainWhite);
            CreateDetailBox(sinkTop.transform, "Cuba_Pia_ParO", new Vector3(-4.282f, 0.828f, 5.69f), new Vector3(0.016f, 0.116f, 0.266f), pal.porcelainWhite);
            CreateDetailBox(sinkTop.transform, "Cuba_Pia_ParL", new Vector3(-3.758f, 0.828f, 5.69f), new Vector3(0.016f, 0.116f, 0.266f), pal.porcelainWhite);
            CreateDetailBox(sinkTop.transform, "Cuba_Pia_ParN", new Vector3(-4.02f, 0.828f, 5.832f), new Vector3(0.540f, 0.114f, 0.016f), pal.porcelainWhite);
            CreateDetailCylinder(sinkTop.transform, "Ladrao_Pia_Cromado", new Vector3(-4.02f, 0.858f, 5.822f), new Vector3(0.022f, 0.003f, 0.022f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(sinkTop.transform, "Miolo_Ladrao_Pia", new Vector3(-4.02f, 0.858f, 5.819f), new Vector3(0.014f, 0.002f, 0.014f), pal.shadowBlack, new Vector3(90f, 0f, 0f));
            CreateDetailBox(sinkTop.transform, "Cuba_Pia_ParS", new Vector3(-4.02f, 0.828f, 5.548f), new Vector3(0.540f, 0.114f, 0.016f), pal.porcelainWhite);
            GameObject bathDrain = CreateDetailCylinder(sinkTop.transform, "Ralo_Pia_Cromado", new Vector3(-4.02f, 0.778f, 5.73f), new Vector3(0.056f, 0.003f, 0.056f), pal.stainlessSteel);
            SnapToSupportTop(bathDrain, bathBasinFloor);

            containers.bathroomFaucet = BuildBathroomSinkDetails(sinkTop.transform, sinkRimW, sinkRimE, pal);

            // Espelho colado rente ao azulejo norte (Z=6.030f -> verso em +6.030f), moldura em alto-relevo sem bloquear MirrorHauntController
            GameObject mirrorFrame = CreateContainer(root, "Espelho_Banheiro");
            mirrorFrame.transform.localPosition = new Vector3(-4.02f, 1.62f, 6.018f);
            CreateDetailBox(mirrorFrame.transform, "Fundo_Espelho", new Vector3(0f, 0f, 0.006f), new Vector3(0.78f, 0.84f, 0.012f), pal.woodMahogany);
            CreateDetailBox(mirrorFrame.transform, "Moldura_Madeira_Espelho_Sup", new Vector3(0f, 0.445f, -0.0045f), new Vector3(0.88f, 0.050f, 0.033f), pal.woodMahogany, castShadows: true);
            CreateDetailBox(mirrorFrame.transform, "Moldura_Madeira_Espelho_Inf", new Vector3(0f, -0.445f, -0.0045f), new Vector3(0.88f, 0.050f, 0.033f), pal.woodMahogany);
            CreateDetailBox(mirrorFrame.transform, "Moldura_Madeira_Espelho_O", new Vector3(-0.415f, 0f, -0.0045f), new Vector3(0.050f, 0.84f, 0.033f), pal.woodMahogany);
            CreateDetailBox(mirrorFrame.transform, "Moldura_Madeira_Espelho_L", new Vector3( 0.415f, 0f, -0.0045f), new Vector3(0.050f, 0.84f, 0.033f), pal.woodMahogany);
            float[] mrX = { -0.415f, 0.415f };
            float[] mrY = { -0.445f, 0.445f };
            for (int mx = 0; mx < 2; mx++)
                for (int my = 0; my < 2; my++)
                    CreateDetailBox(mirrorFrame.transform, $"Roseta_Moldura_Espelho_{mx}_{my}", new Vector3(mrX[mx], mrY[my], -0.0235f), new Vector3(0.044f, 0.044f, 0.005f), pal.baseboardWood);
            BuildOpenFrameTrimBars(mirrorFrame.transform, "Filete_Espelho", new Vector3(0f, 0f, -0.0155f), 0.78f, 0.84f, 0.008f, 0.007f, pal.metalBrass);
            GameObject mirrorGlass = CreateDetailBox(mirrorFrame.transform, "Lamina_Espelho", new Vector3(0f, 0f, -0.014f), new Vector3(0.764f, 0.824f, 0.008f), pal.mirrorGlass);
            Shader planarMirrorShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/PlanarMirror.shader")
                ?? Shader.Find("Canal420/PlanarMirror");
            MirrorHauntController mirrorHaunt = GetOrAdd<MirrorHauntController>(mirrorFrame);
            mirrorHaunt.ConfigureMirrorAssets(mirrorGlass.GetComponent<Renderer>(), planarMirrorShader);
            DoorInteractable bathDoor = root.Find("Porta_Banheiro_Pivot")?.GetComponent<DoorInteractable>();
            if (bathDoor != null)
            {
                mirrorHaunt.LinkBathroomDoor(bathDoor);
            }
            EditorUtility.SetDirty(mirrorHaunt);
            containers.bathroomMirrorHaunt = mirrorHaunt;
            CreateDetailBox(mirrorFrame.transform, "Suporte_Arandela", new Vector3(0f, 0.54f, 0.012f), new Vector3(0.14f, 0.044f, 0.024f), pal.stainlessSteel, castShadows: false);
            CreateDetailBox(mirrorFrame.transform, "Bocal_Arandela_E", new Vector3(-0.175f, 0.54f, -0.030f), new Vector3(0.022f, 0.058f, 0.084f), pal.stainlessSteel, castShadows: false);
            CreateDetailBox(mirrorFrame.transform, "Bocal_Arandela_D", new Vector3( 0.175f, 0.54f, -0.030f), new Vector3(0.022f, 0.058f, 0.084f), pal.stainlessSteel, castShadows: false);

            Mesh sconceShadeMesh = CreateChamferedRectLoftMesh("Mesh_Arandela_Banheiro", 0.328f, 0.052f, 0.080f, 0.012f, 0.88f, 0.010f);
            GameObject sconceOn = CreateContainer(mirrorFrame.transform, "Estado_Ligado");
            CreateMeshObject(sconceOn.transform, "Arandela_Acima_Espelho", sconceShadeMesh, pal.lampEmissive, new Vector3(0f, 0.514f, -0.03f), Vector3.zero, castShadows: false);
            CreateDetailCylinder(sconceOn.transform, "Tubo_Arandela_Aceso", new Vector3(0f, 0.514f, -0.034f), new Vector3(0.028f, 0.165f, 0.028f), pal.lampBulbCore, new Vector3(0f, 0f, 90f), castShadows: false);

            GameObject sconceOff = CreateContainer(mirrorFrame.transform, "Estado_Desligado");
            CreateMeshObject(sconceOff.transform, "Arandela_Acima_Espelho_Apagada", sconceShadeMesh, pal.lampUnlitGlass, new Vector3(0f, 0.514f, -0.03f), Vector3.zero, castShadows: false);
            CreateDetailCylinder(sconceOff.transform, "Tubo_Arandela_Apagado", new Vector3(0f, 0.514f, -0.034f), new Vector3(0.028f, 0.165f, 0.028f), pal.metalDark, new Vector3(0f, 0f, 90f), castShadows: false);
            sconceOff.SetActive(false);

            // Armário de remédios encostado exatamente na parede leste (X=-3.520f, voltado para -X)
            GameObject medCab = CreateContainer(root, "Armario_Remedios_Banheiro");
            medCab.transform.localPosition = new Vector3(-3.600f, 1.52f, 4.56f);
            medCab.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            CreateBox(medCab.transform, "Fundo_Armario", new Vector3(0f, 0f, 0.0675f), new Vector3(0.46f, 0.56f, 0.025f), pal.porcelainWhite, hasCollider: false);
            CreateBox(medCab.transform, "Topo_Armario", new Vector3(0f, 0.2675f, -0.0075f), new Vector3(0.46f, 0.025f, 0.125f), pal.porcelainWhite, hasCollider: false);
            CreateBox(medCab.transform, "Base_Armario", new Vector3(0f, -0.2675f, -0.0075f), new Vector3(0.46f, 0.025f, 0.125f), pal.porcelainWhite, hasCollider: false);
            CreateBox(medCab.transform, "Lat_Esq_Armario", new Vector3(-0.2175f, 0f, -0.0075f), new Vector3(0.025f, 0.510f, 0.125f), pal.porcelainWhite, hasCollider: false);
            CreateBox(medCab.transform, "Lat_Dir_Armario", new Vector3(0.2175f, 0f, -0.0075f), new Vector3(0.025f, 0.510f, 0.125f), pal.porcelainWhite, hasCollider: false);
            CreateBox(medCab.transform, "Prateleira_Meio", new Vector3(0f, -0.02f, 0.01f), new Vector3(0.41f, 0.018f, 0.12f), pal.porcelainWhite, hasCollider: false);
            CreateDetailBox(medCab.transform, "Trinco_Magnetico_Armario", new Vector3(0.185f, 0.002f, -0.056f), new Vector3(0.026f, 0.014f, 0.020f), pal.metalDark);

            CreateDetailedPillBottle(medCab.transform, "Frasco_Interno_1", new Vector3(0.10f, -0.255f, 0.01f), 180f, pal);
            CreateDetailedPillBottle(medCab.transform, "Frasco_Interno_2", new Vector3(-0.02f, -0.255f, 0.02f), 155f, pal);
            GameObject antacidBox = CreateContainer(medCab.transform, "Caixa_Antiacido");
            antacidBox.transform.localPosition = new Vector3(-0.13f, -0.220f, 0.01f);
            antacidBox.transform.localRotation = Quaternion.Euler(0f, 10f, 0f);
            CreateDetailBox(antacidBox.transform, "Corpo_Antiacido", Vector3.zero, new Vector3(0.085f, 0.070f, 0.045f), pal.paperWhite);
            CreatePrintedOverlayQuad(antacidBox.transform, "Frente_Caixa_Antiacido", new Vector3(0f, 0f, -0.0232f), new Vector2(0.082f, 0.066f), pal.details.antacidBox, Vector3.zero);
            CreateDetailedPillBottle(medCab.transform, "Frasco_Interno_3", new Vector3(0.10f, -0.011f, 0.01f), -165f, pal);
            CreateDetailBox(medCab.transform, "Caixa_Curativos", new Vector3(-0.11f, 0.016f, 0.01f), new Vector3(0.086f, 0.054f, 0.032f), pal.porcelainWhite, new Vector3(0f, -8f, 0f));
            CreateDetailBox(medCab.transform, "Faixa_Curativos", new Vector3(-0.11f, 0.016f, -0.007f), new Vector3(0.028f, 0.009f, 0.002f), pal.calendarRed, new Vector3(0f, -8f, 0f));
            CreateDetailBox(medCab.transform, "Cruz_Curativos_Sup", new Vector3(-0.11f, 0.0252f, -0.007f), new Vector3(0.009f, 0.0095f, 0.002f), pal.calendarRed, new Vector3(0f, -8f, 0f));
            CreateDetailBox(medCab.transform, "Cruz_Curativos_Inf", new Vector3(-0.11f, 0.0068f, -0.007f), new Vector3(0.009f, 0.0095f, 0.002f), pal.calendarRed, new Vector3(0f, -8f, 0f));
            Mesh ointmentTubeMesh = CreateClosedPolygonLoftMesh("Mesh_Tubo_Pomada", new[]
            {
                new Vector2( 0.012f, -0.009f),
                new Vector2( 0.015f,  0.000f),
                new Vector2( 0.012f,  0.009f),
                new Vector2(-0.012f,  0.009f),
                new Vector2(-0.015f,  0.000f),
                new Vector2(-0.012f, -0.009f)
            }, new[]
            {
                (-0.044f, 0.42f, 0.52f, 0f, 0f),
                (-0.032f, 0.85f, 1.00f, 0f, 0f),
                ( 0.016f, 1.00f, 0.82f, 0f, 0f),
                ( 0.044f, 1.12f, 0.24f, 0f, 0f)
            });
            CreateMeshObject(medCab.transform, "Pomada_Tubo", ointmentTubeMesh, pal.porcelainWhite, new Vector3(0.00f, 0.000f, 0.00f), new Vector3(90f, 22f, 0f), castShadows: false);
            CreateDetailCylinder(medCab.transform, "Tampa_Pomada", new Vector3(-0.019f, 0.000f, -0.046f), new Vector3(0.012f, 0.007f, 0.012f), pal.plasticSlate, new Vector3(90f, 22f, 0f));

            InspectableObject medInsp = MakeSpot(medCab.transform, "Frascos_Remedios_Internos", new Vector3(0f, -0.04f, 0.01f), new Vector3(0.36f, 0.44f, 0.12f));
            medInsp.ConfigureTask(
                id: "armario_remedios_opc",
                prompt: "Olhar o armário de remédios",
                subtitle: "Tomo quase todo dia pra conseguir dormir.",
                repeatSubtitle: "Metade das caixas já acabou.",
                linear: false,
                sound: FoleySoundId.PillBottleRattle
            );

            GameObject medDoorHost = CreateContainer(medCab.transform, "Porta_Armario_Remedios_Interagir");
            GameObject medDoorPivot = CreateContainer(medDoorHost.transform, "Porta_Remedios_Pivot");
            medDoorPivot.transform.localPosition = new Vector3(-0.22f, 0f, -0.082f);
            CreateBox(medDoorPivot.transform, "Folha_Porta_Remedios", new Vector3(0.22f, 0f, 0f), new Vector3(0.44f, 0.54f, 0.024f), pal.porcelainWhite, hasCollider: true);
            BuildOpenFrameTrimBars(medDoorPivot.transform, "Moldura_Espelho_Porta", new Vector3(0.22f, 0f, -0.015f), 0.39f, 0.49f, 0.020f, 0.006f, pal.stainlessSteel);
            CreateDetailBox(medDoorPivot.transform, "Espelho_Porta", new Vector3(0.22f, 0f, -0.0135f), new Vector3(0.348f, 0.448f, 0.003f), pal.stainlessSteel);
            CreateDetailBox(medDoorPivot.transform, "Base_Puxador_Sup", new Vector3(0.415f, 0.012f, -0.016f), new Vector3(0.012f, 0.010f, 0.010f), pal.stainlessSteel);
            CreateDetailBox(medDoorPivot.transform, "Base_Puxador_Inf", new Vector3(0.415f, -0.052f, -0.016f), new Vector3(0.012f, 0.010f, 0.010f), pal.stainlessSteel);
            CreateDetailBox(medDoorPivot.transform, "Puxador_Remedios", new Vector3(0.415f, -0.02f, -0.024f), new Vector3(0.014f, 0.078f, 0.008f), pal.stainlessSteel);
            CreateDetailCylinder(medDoorPivot.transform, "Dobradica_Remedios_Sup", new Vector3(0f, 0.18f, -0.012f), new Vector3(0.012f, 0.020f, 0.012f), pal.stainlessSteel);
            CreateDetailCylinder(medDoorPivot.transform, "Dobradica_Remedios_Inf", new Vector3(0f, -0.18f, -0.012f), new Vector3(0.012f, 0.020f, 0.012f), pal.stainlessSteel);

            AnimatedContainerInteractable medCabContainer = medDoorHost.AddComponent<AnimatedContainerInteractable>();
            medCabContainer.ConfigureSwingCabinet(
                id: "armario_remedios_banheiro",
                primaryDoorPivot: medDoorPivot.transform,
                primaryOpenEuler: new Vector3(0f, 82f, 0f),
                promptOpen: "Abrir o armário de remédios",
                promptClose: "Fechar o armário de remédios",
                doorSfx: FoleySoundId.CupboardDoor
            );
            medCabContainer.RegisterInteriorObject(medInsp);
            EditorUtility.SetDirty(medCabContainer);

            // Box do chuveiro no canto noroeste alinhado rente aos azulejos Oeste (-6.530 -> local -0.560) e Norte (+6.030 -> local +0.560)
            GameObject shower = CreateContainer(root, "Box_Chuveiro");
            shower.transform.localPosition = new Vector3(-5.97f, 0f, 5.47f);
            CreateDetailBox(shower.transform, "Piso_Box", new Vector3(-0.022f, 0.008f, 0.022f), new Vector3(1.076f, 0.016f, 1.076f), pal.porcelainWhite);
            CreateBox(shower.transform, "Guia_Marmore_Leste", new Vector3(0.538f, 0.035f, 0.001f), new Vector3(0.044f, 0.070f, 1.118f), pal.graniteDark);
            CreateBox(shower.transform, "Guia_Marmore_Sul", new Vector3(-0.022f, 0.035f, -0.538f), new Vector3(1.076f, 0.070f, 0.044f), pal.graniteDark);
            BuildFloorDrain(shower.transform, "Ralo_Interno_Box", new Vector3(-0.24f, 0.016f, 0.24f), pal);

            CreateBox(shower.transform, "Vidro_Leste_Fixo", new Vector3(0.528f, 1.03375f, 0.290f), new Vector3(0.014f, 1.9275f, 0.540f), pal.details.clearGlass, hasCollider: true);
            CreateBox(shower.transform, "Porta_Box_Correr", new Vector3(0.546f, 1.03375f, 0.260f), new Vector3(0.014f, 1.9275f, 0.540f), pal.details.clearGlass, hasCollider: true);
            CreateDetailBox(shower.transform, "Perfil_Porta_Box", new Vector3(0.546f, 1.0435f, -0.022f), new Vector3(0.022f, 1.905f, 0.024f), pal.stainlessSteel);
            CreateDetailCylinder(shower.transform, "Roldana_Box_0", new Vector3(0.546f, 2.008f, 0.08f), new Vector3(0.022f, 0.006f, 0.022f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(shower.transform, "Roldana_Box_1", new Vector3(0.546f, 2.008f, 0.44f), new Vector3(0.022f, 0.006f, 0.022f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            CreateBox(shower.transform, "Vidro_Sul", new Vector3(-0.022f, 1.03375f, -0.538f), new Vector3(1.076f, 1.9275f, 0.018f), pal.details.clearGlass, hasCollider: true);
            CreateDetailBox(shower.transform, "Trilho_Superior_Leste", new Vector3(0.538f, 2.020f, 0.022f), new Vector3(0.042f, 0.045f, 1.076f), pal.stainlessSteel);
            CreateDetailBox(shower.transform, "Trilho_Superior_Sul", new Vector3(-0.022f, 2.020f, -0.538f), new Vector3(1.076f, 0.045f, 0.042f), pal.stainlessSteel);
            CreateDetailBox(shower.transform, "Coluna_Canto_Aluminio", new Vector3(0.538f, 1.05625f, -0.538f), new Vector3(0.042f, 1.9725f, 0.042f), pal.stainlessSteel);
            CreateDetailBox(shower.transform, "Canal_Parede_Box_N", new Vector3(0.538f, 1.034f, 0.548f), new Vector3(0.044f, 1.916f, 0.016f), pal.stainlessSteel);
            CreateDetailBox(shower.transform, "Canal_Parede_Box_O", new Vector3(-0.548f, 1.034f, -0.538f), new Vector3(0.016f, 1.916f, 0.044f), pal.stainlessSteel);
            CreateDetailBox(shower.transform, "Trilho_Inf_Box_Leste", new Vector3(0.538f, 0.081f, 0.016f), new Vector3(0.044f, 0.016f, 1.044f), pal.stainlessSteel);
            CreateDetailCylinder(shower.transform, "Puxador_Box_Ext", new Vector3(0.562f, 1.02f, 0.045f), new Vector3(0.026f, 0.010f, 0.026f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(shower.transform, "Puxador_Box_Int", new Vector3(0.530f, 1.02f, 0.045f), new Vector3(0.026f, 0.010f, 0.026f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));

            BuildBathroomShowerDetails(shower.transform, pal);

            InspectableObject showerInsp = MakeSpot(root, "Box_Chuveiro_Interagir", new Vector3(-5.97f, 1.10f, 5.47f), new Vector3(1.02f, 1.95f, 1.02f));
            showerInsp.ConfigureTask(
                id: "chuveiro_opc",
                prompt: "Olhar o box",
                subtitle: "Não tomo banho. Sinto que sou observado quando tomo.",
                repeatSubtitle: "O ralo tá seco.",
                linear: false,
                sound: FoleySoundId.GenericTouch
            );

            // Vaso sanitário escultórico, papeleira de parede e lixeira com pedal articulado
            BuildBathroomToiletAndFixtures(root, pal);

            // Estante de utilidades na parede leste (X=-3.72f, Z=+3.45f -> livre do batente da porta) com ponteiras sem Z-fighting
            GameObject utilShelf = CreateContainer(root, "Estante_Utilidades_Banheiro");
            utilShelf.transform.localPosition = new Vector3(-3.72f, 0f, 3.45f);
            float[] postXs = { -0.17f, 0.17f };
            float[] postZs = { -0.40f, 0.40f };
            for (int px = 0; px < 2; px++)
            {
                for (int pz = 0; pz < 2; pz++)
                {
                    CreateDetailBox(utilShelf.transform, $"Sapata_Estante_{px}_{pz}", new Vector3(postXs[px], 0.006f, postZs[pz]), new Vector3(0.036f, 0.012f, 0.036f), pal.shadowBlack);
                    CreateDetailBox(utilShelf.transform, $"Montante_{px}_{pz}", new Vector3(postXs[px], 0.586f, postZs[pz]), new Vector3(0.032f, 1.148f, 0.032f), pal.metalDark, castShadows: true);
                    CreateDetailBox(utilShelf.transform, $"Tampa_Montante_{px}_{pz}", new Vector3(postXs[px], 1.164f, postZs[pz]), new Vector3(0.036f, 0.008f, 0.036f), pal.shadowBlack);
                }
            }

            GameObject shelfLow = CreateBox(utilShelf.transform, "Prateleira_Baixa", new Vector3(0f, 0.16f, 0f), new Vector3(0.36f, 0.026f, 0.84f), pal.metalDark, hasCollider: false);
            GameObject shelfMid = CreateBox(utilShelf.transform, "Prateleira_Media", new Vector3(0f, 0.62f, 0f), new Vector3(0.36f, 0.026f, 0.84f), pal.metalDark, hasCollider: false);
            CreateBox(utilShelf.transform, "Prateleira_Alta", new Vector3(0f, 1.10f, 0f), new Vector3(0.36f, 0.030f, 0.84f), pal.metalDark, hasCollider: true);
            CreateDetailBox(utilShelf.transform, "Trava_X_Estante_1", new Vector3(0.172f, 0.62f, 0f), new Vector3(0.008f, 1.06f, 0.014f), pal.metalDark, new Vector3(38f, 0f, 0f));
            CreateDetailBox(utilShelf.transform, "Trava_X_Estante_2", new Vector3(0.172f, 0.62f, 0f), new Vector3(0.008f, 1.06f, 0.014f), pal.metalDark, new Vector3(-38f, 0f, 0f));

            GameObject towel1 = BuildFoldedClothMeshObject(utilShelf.transform, "Toalha_Dobrada_1", "Mesh_Toalha_Dobrada_C",
                new Vector3(0f, 0.633f, 0.14f), new Vector3(0f, -90f, 0f), new Vector3(1.30f, 1.12f, 4.9f), pal.paperWhite, pal.fabricAsh);
            SnapToSupportTop(towel1, shelfMid);
            GameObject towel2 = BuildFoldedClothMeshObject(utilShelf.transform, "Toalha_Dobrada_2", "Mesh_Toalha_Dobrada_C",
                new Vector3(0.01f, 0.700f, 0.14f), new Vector3(0f, -94f, 0f), new Vector3(1.18f, 0.96f, 4.4f), pal.bedPastelBlue);
            SnapToSupportTop(towel2, towel1);
            GameObject soapBox = CreateDetailBox(utilShelf.transform, "Caixa_Sabonetes", new Vector3(-0.02f, 0.658f, -0.22f), new Vector3(0.14f, 0.050f, 0.18f), pal.folderKraft, new Vector3(0f, 12f, 0f));
            SnapToSupportTop(soapBox, shelfMid);
            GameObject stockRoll1 = BuildToiletPaperRoll(utilShelf.transform, "Rolos_Estoque_1", new Vector3(-0.04f, 0.173f, -0.18f), Vector3.zero, pal);
            SnapToSupportTop(stockRoll1, shelfLow);
            GameObject stockRoll2 = BuildToiletPaperRoll(utilShelf.transform, "Rolos_Estoque_2", new Vector3(0.06f, 0.173f, -0.04f), Vector3.zero, pal);
            SnapToSupportTop(stockRoll2, shelfLow);
        }

        private static void BuildBedroom(Transform root, GreyboxPalette pal, ref HouseContainers containers)
        {
            // Tapete aos pés da cama com franjas laterais (base em Y=0.000m, livre da peseira e do desgaste)
            GameObject rug = CreateContainer(root, "Tapete_Quarto");
            rug.transform.localPosition = new Vector3(-0.88f, 0f, 3.30f);
            CreateDetailBox(rug.transform, "Borda_Tapete", new Vector3(0f, 0.0025f, 0f), new Vector3(1.68f, 0.005f, 0.88f), pal.woodMahogany);
            CreateDetailBox(rug.transform, "Centro_Tapete", new Vector3(0f, 0.0070f, 0f), new Vector3(1.48f, 0.004f, 0.70f), pal.rugFabric);
            for (int f = -3; f <= 3; f++)
            {
                float fz = f * 0.115f;
                CreateDetailBox(rug.transform, $"Franja_Oeste_{f + 3}", new Vector3(-0.865f, 0.0015f, fz), new Vector3(0.050f, 0.003f, 0.028f), pal.paperWhite);
                CreateDetailBox(rug.transform, $"Franja_Leste_{f + 3}", new Vector3( 0.865f, 0.0015f, fz), new Vector3(0.050f, 0.003f, 0.028f), pal.paperWhite);
            }

            // Chinelos anatômicos apoiados diretamente no piso de madeira ao lado da cama (Y=0.000m)
            BuildBedroomSlipper(root, "Chinelo_Cian_Esq", new Vector3(-1.91f, 0f, 4.44f), 18f, isRightFoot: false, pal);
            BuildBedroomSlipper(root, "Chinelo_Cian_Dir", new Vector3(-1.76f, 0f, 4.35f), -9f, isRightFoot: true, pal);

            // Cama de casal com pés elevados, cabeceira almofadada com pináculos, colchão chanfrado, colcha e travesseiros moldados
            GameObject bed = CreateContainer(root, "Cama_Casal_Cian");
            bed.transform.localPosition = new Vector3(-0.88f, 0f, 4.86f);
            float[] bedFootXs = { -0.73f, 0.73f };
            float[] bedFootZs = { -0.92f, 0.92f };
            for (int bx = 0; bx < 2; bx++)
            {
                for (int bz = 0; bz < 2; bz++)
                {
                    CreateDetailBox(bed.transform, $"Pe_Cama_{bx}_{bz}",
                        new Vector3(bedFootXs[bx], 0.040f, bedFootZs[bz]),
                        new Vector3(0.080f, 0.080f, 0.080f), pal.woodMahogany, castShadows: true);
                }
            }
            CreateBox(bed.transform, "Base_Estrado", new Vector3(0f, 0.180f, 0f), new Vector3(1.56f, 0.200f, 1.96f), pal.furnitureWood);
            CreateDetailBox(bed.transform, "Friso_Estrado_O", new Vector3(-0.785f, 0.215f, 0f), new Vector3(0.010f, 0.070f, 1.92f), pal.woodMahogany);
            CreateDetailBox(bed.transform, "Friso_Estrado_L", new Vector3( 0.785f, 0.215f, 0f), new Vector3(0.010f, 0.070f, 1.92f), pal.woodMahogany);

            CreateDetailBox(bed.transform, "Coluna_Cab_E", new Vector3(-0.79f, 0.58f, 0.99f), new Vector3(0.072f, 1.16f, 0.072f), pal.woodMahogany, castShadows: true);
            CreateDetailBox(bed.transform, "Coluna_Cab_D", new Vector3( 0.79f, 0.58f, 0.99f), new Vector3(0.072f, 1.16f, 0.072f), pal.woodMahogany, castShadows: true);
            Mesh bedFinialMesh = CreateFacetedLatheVesselMesh("Mesh_Pinaculo_Cama", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.028f),
                (0.010f, 0.018f),
                (0.028f, 0.032f),
                (0.048f, 0.026f),
                (0.060f, 0.010f),
                (0.068f, 0.012f),
                (0.074f, 0.000f)
            });
            CreateMeshObject(bed.transform, "Pinaculo_Cab_E", bedFinialMesh, pal.metalBrass, new Vector3(-0.79f, 1.160f, 0.99f), Vector3.zero, castShadows: true);
            CreateMeshObject(bed.transform, "Pinaculo_Cab_D", bedFinialMesh, pal.metalBrass, new Vector3( 0.79f, 1.160f, 0.99f), Vector3.zero, castShadows: true);
            CreateBox(bed.transform, "Cabeceira", new Vector3(0f, 0.615f, 0.99f), new Vector3(1.508f, 0.91f, 0.058f), pal.furnitureWood);
            Mesh headCrownMesh = CreateChamferedRectLoftMesh("Mesh_Cornija_Cabeceira", 1.508f, 0.050f, 0.076f, 0.010f, 0.94f, 0.008f);
            CreateMeshObject(bed.transform, "Cornija_Cab", headCrownMesh, pal.woodMahogany, new Vector3(0f, 1.070f, 0.99f), castShadows: true);
            BuildOpenFrameTrimBars(bed.transform, "Moldura_Cab_E", new Vector3(-0.37f, 0.68f, 0.955f), 0.62f, 0.54f, 0.028f, 0.010f, pal.baseboardWood);
            CreateDetailBox(bed.transform, "Almofada_Madeira_Cab_E", new Vector3(-0.37f, 0.68f, 0.956f), new Vector3(0.54f, 0.46f, 0.008f), pal.woodMahogany);
            BuildOpenFrameTrimBars(bed.transform, "Moldura_Cab_D", new Vector3( 0.37f, 0.68f, 0.955f), 0.62f, 0.54f, 0.028f, 0.010f, pal.baseboardWood);
            CreateDetailBox(bed.transform, "Almofada_Madeira_Cab_D", new Vector3( 0.37f, 0.68f, 0.956f), new Vector3(0.54f, 0.46f, 0.008f), pal.woodMahogany);

            CreateDetailBox(bed.transform, "Coluna_Pes_E", new Vector3(-0.79f, 0.28f, -1.01f), new Vector3(0.068f, 0.56f, 0.068f), pal.woodMahogany, castShadows: true);
            CreateDetailBox(bed.transform, "Coluna_Pes_D", new Vector3( 0.79f, 0.28f, -1.01f), new Vector3(0.068f, 0.56f, 0.068f), pal.woodMahogany, castShadows: true);
            CreateMeshObject(bed.transform, "Pinaculo_Pes_E", bedFinialMesh, pal.metalBrass, new Vector3(-0.79f, 0.560f, -1.01f), Vector3.zero, castShadows: true);
            CreateMeshObject(bed.transform, "Pinaculo_Pes_D", bedFinialMesh, pal.metalBrass, new Vector3( 0.79f, 0.560f, -1.01f), Vector3.zero, castShadows: true);
            CreateBox(bed.transform, "Peseira_Baixa", new Vector3(0f, 0.290f, -1.01f), new Vector3(1.512f, 0.420f, 0.056f), pal.woodMahogany);
            Mesh footTopMesh = CreateChamferedRectLoftMesh("Mesh_Topo_Peseira", 1.512f, 0.030f, 0.074f, 0.010f, 0.94f, 0.006f);
            CreateMeshObject(bed.transform, "Topo_Peseira", footTopMesh, pal.furnitureWood, new Vector3(0f, 0.500f, -1.01f), castShadows: true);

            // Colchão chanfrado apoiado exatamente sobre Base_Estrado (Y=0.280m..0.460m)
            Mesh mattressMesh = CreateChamferedRectLoftMesh("Mesh_Colchao_Casal", 1.50f, 0.180f, 1.92f, 0.055f, 0.96f, 0.026f);
            CreateMeshObject(bed.transform, "Colchao_Lencol", mattressMesh, pal.paperWhite, new Vector3(0f, 0.280f, -0.01f), castShadows: true);

            // Coberta única contínua anatômica (lado do Cian mais baixo ligado por meia-parábola suave ao lado alto de Margaret)
            BuildUnifiedBedBlanketAndSheet(bed.transform, pal);

            // Travesseiros macios e chanfrados apoiados diretamente em Y=0.460m
            Mesh pillowMesh = CreateChamferedRectLoftMesh("Mesh_Travesseiro_Macio", 0.54f, 0.102f, 0.34f, 0.055f, 0.72f, 0.036f);
            CreateMeshObject(bed.transform, "Travesseiro_Cian", pillowMesh, pal.porcelainWhite, new Vector3(-0.41f, 0.456f, 0.67f), new Vector3(2f, 9f, -2f), castShadows: true);
            CreateMeshObject(bed.transform, "Travesseiro_Intocado_Margaret", pillowMesh, pal.porcelainWhite, new Vector3(0.41f, 0.460f, 0.71f), Vector3.zero, castShadows: true);

            InspectableObject bedSide = MakeSpot(root, "Lado_Cama_Margaret_Interagir", new Vector3(-0.51f, 0.60f, 4.65f), new Vector3(0.74f, 0.42f, 1.36f));
            bedSide.ConfigureTask(
                id: "lado_cama_margaret_opc",
                prompt: "Olhar o outro lado da cama",
                subtitle: "Mesmo sozinho, eu só durmo na minha ponta.",
                repeatSubtitle: "Não passo da metade.",
                linear: false,
                sound: FoleySoundId.ClothInspect
            );

            // Criado-mudo de Cian (Oeste da cama, parede norte Z=+6.04 -> centro Z=+5.78f)
            GameObject cianStand = CreateContainer(root, "Criado_Mudo_Cian");
            cianStand.transform.localPosition = new Vector3(-2.10f, 0f, 5.78f);
            BuildNightstandFrame(cianStand.transform, pal);
            BuildDetailedClosedBook(cianStand.transform, "Livro_Criado_Cian", new Vector3(-0.06f, 0.144f, 0.02f), -12f, new Vector3(0.18f, 0.038f, 0.24f), pal.bookSlateBlue, pal.calendarRed, pal);

            GameObject cianDrawer = CreateContainer(cianStand.transform, "Gaveta_Criado_Cian");
            cianDrawer.transform.localPosition = new Vector3(0f, 0.448f, -0.01f);
            GameObject cianDrBottom = CreateBox(cianDrawer.transform, "Fundo_Gaveta", new Vector3(0f, -0.081f, -0.008f), new Vector3(0.390f, 0.016f, 0.340f), pal.woodLight, hasCollider: true);
            CreateDetailBox(cianDrawer.transform, "Lat_Esq_Gav", new Vector3(-0.203f, -0.026f, -0.008f), new Vector3(0.016f, 0.104f, 0.376f), pal.woodLight);
            CreateDetailBox(cianDrawer.transform, "Lat_Dir_Gav", new Vector3(0.203f, -0.026f, -0.008f), new Vector3(0.016f, 0.104f, 0.376f), pal.woodLight);
            CreateDetailBox(cianDrawer.transform, "Traseira_Gav", new Vector3(0f, -0.026f, 0.172f), new Vector3(0.390f, 0.104f, 0.016f), pal.woodLight);
            CreateBox(cianDrawer.transform, "Frente_Gaveta", new Vector3(0f, 0f, -0.222f), new Vector3(0.456f, 0.168f, 0.024f), pal.furnitureWood, hasCollider: true);
            BuildOpenFrameTrimBars(cianDrawer.transform, "Moldura_Frente_Cian", new Vector3(0f, 0f, -0.236f), 0.39f, 0.118f, 0.016f, 0.006f, pal.baseboardWood);
            CreateDetailBox(cianDrawer.transform, "Almofada_Frente_Cian", new Vector3(0f, 0f, -0.236f), new Vector3(0.352f, 0.080f, 0.005f), pal.woodMahogany);
            CreateDetailCylinder(cianDrawer.transform, "Base_Puxador_Cian", new Vector3(0f, 0f, -0.241f), new Vector3(0.032f, 0.002f, 0.032f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(cianDrawer.transform, "Pescoco_Puxador_Cian", new Vector3(0f, 0f, -0.247f), new Vector3(0.012f, 0.006f, 0.012f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailSphere(cianDrawer.transform, "Puxador", new Vector3(0f, 0f, -0.256f), new Vector3(0.024f, 0.024f, 0.018f), pal.metalBrass, castShadows: false);

            GameObject blister1 = BuildBlisterPack(cianDrawer.transform, "Cartela_Aluminio_1", new Vector3(-0.09f, -0.072f, -0.02f), 18f, pal, poppedCount: 6, printAngleY: 0f);
            SnapToSupportTop(blister1, cianDrBottom);
            GameObject blister2 = BuildBlisterPack(cianDrawer.transform, "Cartela_Aluminio_2", new Vector3(0.10f, -0.072f, 0.02f), -14f, pal, poppedCount: 4, printAngleY: 0f);
            SnapToSupportTop(blister2, cianDrBottom);
            GameObject pharmacyReceipt = BuildDetailedPaperSheet(cianDrawer.transform, "Recibo_Farmacia", new Vector3(-0.01f, -0.072f, -0.11f), 24f, new Vector2(0.08f, 0.11f), pal);
            SnapToSupportTop(pharmacyReceipt, cianDrBottom);

            InspectableObject cianDrawerItem = MakeSpot(cianDrawer.transform, "Cartela_Analgesico_Cian", new Vector3(0f, -0.02f, -0.06f), new Vector3(0.32f, 0.09f, 0.22f));
            cianDrawerItem.ConfigureTask(
                id: "gaveta_criado_cian_opc",
                prompt: "Mexer na gaveta",
                subtitle: "Cartelas vazias e recibo velho.",
                repeatSubtitle: "Só lixo de remédio.",
                linear: false,
                sound: FoleySoundId.GenericTouch
            );

            AnimatedContainerInteractable cianDrawerAnim = cianDrawer.AddComponent<AnimatedContainerInteractable>();
            cianDrawerAnim.ConfigureSlideDrawer(
                id: "gaveta_criado_cian",
                drawerTransform: cianDrawer.transform,
                slideOffset: new Vector3(0f, 0f, -0.25f),
                promptOpen: "Abrir a gaveta do criado-mudo",
                promptClose: "Fechar a gaveta do criado-mudo",
                slideSfx: FoleySoundId.NightstandDrawer
            );
            cianDrawerAnim.RegisterInteriorObject(cianDrawerItem);
            EditorUtility.SetDirty(cianDrawerAnim);

            BuildDetailedAlarmClock(root, "Despertador_CriadoMudo", new Vector3(-2.10f, 0.560f, 5.80f), -14f, pal);
            BuildDrinkingGlass(root, "Copo_Cabeceira_Cian", new Vector3(-2.28f, 0.560f, 5.68f), pal, upsideDown: false, hasWaterLeft: true);
            BuildReadingGlasses(root, "Oculos_Leitura_Cian", new Vector3(-1.95f, 0.560f, 5.68f), 22f, pal);

            InspectableObject alarmInsp = MakeSpot(root, "Despertador_Interagir", new Vector3(-2.10f, 0.63f, 5.79f), new Vector3(0.24f, 0.16f, 0.18f));
            alarmInsp.ConfigureTask(
                id: "despertador_quarto_opc",
                prompt: "Olhar o despertador",
                subtitle: "Cinco e quarenta. Olho pra esse relógio toda hora esperando dar segunda.",
                repeatSubtitle: "Cinco e quarenta.",
                linear: false,
                sound: FoleySoundId.MetalInspect
            );

            // Criado-mudo de Margaret (âncora fixa em +0.32, 0, +5.78; topo = 0.560f)
            GameObject margStand = CreateContainer(root, "Criado_Mudo_Margaret");
            margStand.transform.localPosition = new Vector3(0.32f, 0f, 5.78f);
            BuildNightstandFrame(margStand.transform, pal);
            GameObject bookM1 = BuildDetailedClosedBook(margStand.transform, "Romance_Antigo_1", new Vector3(0.02f, 0.144f, 0.01f), 8f, new Vector3(0.17f, 0.034f, 0.23f), pal.bookTaupe, pal.paperWhite, pal);
            GameObject bookM2 = BuildDetailedClosedBook(margStand.transform, "Romance_Antigo_2", new Vector3(0.01f, 0.178f, 0.01f), -5f, new Vector3(0.16f, 0.030f, 0.22f), pal.bookBurgundy, null, pal);
            SnapToSupportTop(bookM2, bookM1);

            BuildBedroomTableLamp(margStand.transform, new Vector3(0.14f, 0.560f, 0.09f), pal);
            BuildPorcelainRingDishAndRing(margStand.transform, new Vector3(-0.14f, 0.560f, 0.11f), pal);

            GameObject margDrawer = CreateContainer(margStand.transform, "Gaveta_Criado_Margaret");
            margDrawer.transform.localPosition = new Vector3(0f, 0.448f, -0.01f);
            CreateBox(margDrawer.transform, "Fundo_Gaveta", new Vector3(0f, -0.081f, -0.008f), new Vector3(0.390f, 0.016f, 0.340f), pal.woodLight, hasCollider: true);
            CreateDetailBox(margDrawer.transform, "Lat_Esq_Gav", new Vector3(-0.203f, -0.026f, -0.008f), new Vector3(0.016f, 0.104f, 0.376f), pal.woodLight);
            CreateDetailBox(margDrawer.transform, "Lat_Dir_Gav", new Vector3(0.203f, -0.026f, -0.008f), new Vector3(0.016f, 0.104f, 0.376f), pal.woodLight);
            CreateDetailBox(margDrawer.transform, "Traseira_Gav", new Vector3(0f, -0.026f, 0.172f), new Vector3(0.390f, 0.104f, 0.016f), pal.woodLight);
            CreateBox(margDrawer.transform, "Frente_Gaveta_Margaret", new Vector3(0f, 0f, -0.222f), new Vector3(0.456f, 0.168f, 0.024f), pal.furnitureWood, hasCollider: true);
            BuildOpenFrameTrimBars(margDrawer.transform, "Moldura_Frente_Margaret", new Vector3(0f, 0f, -0.236f), 0.39f, 0.118f, 0.016f, 0.006f, pal.baseboardWood);
            CreateDetailBox(margDrawer.transform, "Almofada_Frente_Margaret", new Vector3(0f, 0f, -0.236f), new Vector3(0.352f, 0.080f, 0.005f), pal.woodMahogany);
            Mesh margHandleMesh = CreateFacetedCShapeHandleMesh("Mesh_Puxador_Margaret", new[]
            {
                new Vector3(-0.034f, -0.014f, -0.238f),
                new Vector3(-0.028f, -0.014f, -0.256f),
                new Vector3( 0.000f, -0.014f, -0.260f),
                new Vector3( 0.028f, -0.014f, -0.256f),
                new Vector3( 0.034f, -0.014f, -0.238f)
            }, halfThicknessY: 0.005f, halfThicknessZ: 0.005f, sideAxis: Vector3.up);
            CreateMeshObject(margDrawer.transform, "Puxador_Margaret", margHandleMesh, pal.metalBrass, Vector3.zero, castShadows: false);
            CreateDetailBox(margDrawer.transform, "Espelho_Fechadura_Margaret", new Vector3(0f, 0.052f, -0.2405f), new Vector3(0.022f, 0.032f, 0.005f), pal.metalBrass);
            CreateDetailCylinder(margDrawer.transform, "Miolo_Chave_Margaret", new Vector3(0f, 0.052f, -0.2440f), new Vector3(0.010f, 0.0015f, 0.010f), pal.shadowBlack, new Vector3(90f, 0f, 0f));

            AnimatedContainerInteractable margLockedDrawer = margDrawer.AddComponent<AnimatedContainerInteractable>();
            margLockedDrawer.ConfigureSlideDrawer(
                id: "gaveta_margaret_trancada",
                drawerTransform: margDrawer.transform,
                slideOffset: new Vector3(0f, 0f, -0.24f),
                promptOpen: "Abrir a gaveta",
                promptClose: "Fechar a gaveta",
                slideSfx: FoleySoundId.NightstandDrawer
            );
            margLockedDrawer.ConfigurePermanentLock(
                promptWhenLocked: "Tentar abrir a gaveta",
                firstLockedLine: "Trancada. Nunca tive a chave desse lado.",
                repeatLockedLine: "Continua trancada.",
                sfx: FoleySoundId.DrawerLocked
            );
            EditorUtility.SetDirty(margLockedDrawer);

            // Guarda-roupa na parede oeste do quarto (X=-3.04f, Z=+4.10f -> 1.01m livre até a cama)
            GameObject wardrobe = CreateContainer(root, "Guarda_Roupa_Cian");
            wardrobe.transform.localPosition = new Vector3(-3.04f, 0f, 4.10f);
            CreateBox(wardrobe.transform, "Soculo_GuardaRoupa", new Vector3(-0.01f, 0.045f, 0f), new Vector3(0.52f, 0.090f, 1.78f), pal.baseboardWood);
            CreateBox(wardrobe.transform, "Base_GuardaRoupa", new Vector3(0f, 0.105f, 0f), new Vector3(0.56f, 0.030f, 1.78f), pal.furnitureWood);
            CreateBox(wardrobe.transform, "Lateral_Sul", new Vector3(0f, 1.130f, -0.905f), new Vector3(0.58f, 2.020f, 0.030f), pal.furnitureWood);
            CreateBox(wardrobe.transform, "Lateral_Norte", new Vector3(0f, 1.130f, 0.905f), new Vector3(0.58f, 2.020f, 0.030f), pal.furnitureWood);
            CreateBox(wardrobe.transform, "Fundo_GuardaRoupa", new Vector3(-0.275f, 1.130f, 0f), new Vector3(0.025f, 2.020f, 1.78f), pal.furnitureWood);
            CreateBox(wardrobe.transform, "Topo_GuardaRoupa", new Vector3(0f, 2.160f, 0f), new Vector3(0.58f, 0.040f, 1.84f), pal.furnitureWood);
            Mesh wdCrownMesh = CreateChamferedRectLoftMesh("Mesh_Cornija_GuardaRoupa", 0.62f, 0.050f, 1.90f, 0.016f, 0.96f, 0.008f);
            CreateMeshObject(wardrobe.transform, "Cornija_Superior", wdCrownMesh, pal.woodMahogany, new Vector3(0.02f, 2.180f, 0f), castShadows: true);
            CreateBox(wardrobe.transform, "Prateleira_Maleiro", new Vector3(0.005f, 1.78f, 0f), new Vector3(0.52f, 0.026f, 1.78f), pal.furnitureWood, hasCollider: false);
            CreateDetailBox(wardrobe.transform, "Sombra_Interna", new Vector3(-0.2590f, 1.13f, 0f), new Vector3(0.008f, 1.98f, 1.76f), pal.shadowBlack);
            CreateDetailCylinder(wardrobe.transform, "Vara_Cabideiro", new Vector3(-0.03f, 1.68f, 0f), new Vector3(0.024f, 0.895f, 0.024f), pal.metalBrass, new Vector3(90f, 0f, 0f));

            BuildWardrobeInteriorProps(wardrobe.transform, pal);

            InspectableObject cianSuitsInsp = MakeSpot(wardrobe.transform, "Ternos_Cian_Internos", new Vector3(-0.02f, 1.18f, -0.41f), new Vector3(0.36f, 0.96f, 0.56f));
            cianSuitsInsp.ConfigureTask(
                id: "guarda_roupa_ternos_opc",
                prompt: "Olhar os ternos",
                subtitle: "Cheiro de mofo e naftalina. Uso o mesmo terno toda semana.",
                repeatSubtitle: "Sempre a mesma roupa.",
                linear: false,
                sound: FoleySoundId.ClothInspect
            );

            // Portas bipartidas menores com dobradiças em latão, almofadas vazadas e puxadores espaçados
            CreateBox(wardrobe.transform, "Montante_Central_Portas", new Vector3(0.278f, 1.130f, 0f), new Vector3(0.024f, 2.01f, 0.024f), pal.furnitureWood, hasCollider: true);

            GameObject wardrobeSouthHost = CreateContainer(wardrobe.transform, "Portas_GuardaRoupa_Sul_Interagir");
            GameObject wdSouthExtPivot = CreateContainer(wardrobeSouthHost.transform, "Porta_Sul_Esq_Pivot");
            wdSouthExtPivot.transform.localPosition = new Vector3(0.28f, 1.130f, -0.888f);
            BuildWardrobeDoorLeaf(wdSouthExtPivot.transform, "Sul_Esq", +0.217f, +0.375f, 0.008f, pal);

            GameObject wdSouthIntPivot = CreateContainer(wardrobeSouthHost.transform, "Porta_Sul_Dir_Pivot");
            wdSouthIntPivot.transform.localPosition = new Vector3(0.28f, 1.130f, -0.012f);
            BuildWardrobeDoorLeaf(wdSouthIntPivot.transform, "Sul_Dir", -0.217f, -0.375f, -0.008f, pal);

            AnimatedContainerInteractable wardrobeSouthContainer = wardrobeSouthHost.AddComponent<AnimatedContainerInteractable>();
            wardrobeSouthContainer.ConfigureSwingCabinet(
                id: "guarda_roupa_cian_ternos",
                primaryDoorPivot: wdSouthExtPivot.transform,
                primaryOpenEuler: new Vector3(0f, 64f, 0f),
                promptOpen: "Abrir as portas dos ternos",
                promptClose: "Fechar as portas dos ternos",
                doorSfx: FoleySoundId.WardrobeDoor,
                secondDoorPivot: wdSouthIntPivot.transform,
                secondOpenEuler: new Vector3(0f, -64f, 0f),
                speed: 2.6f
            );
            wardrobeSouthContainer.RegisterInteriorObject(cianSuitsInsp);
            EditorUtility.SetDirty(wardrobeSouthContainer);

            GameObject wardrobeDoorsHost = CreateContainer(wardrobe.transform, "Portas_GuardaRoupa_Interagir");
            GameObject wdNorthIntPivot = CreateContainer(wardrobeDoorsHost.transform, "Porta_Norte_Sul_Pivot");
            wdNorthIntPivot.transform.localPosition = new Vector3(0.28f, 1.130f, 0.012f);
            BuildWardrobeDoorLeaf(wdNorthIntPivot.transform, "Norte_Sul", +0.217f, +0.375f, +0.008f, pal);

            GameObject wdNorthExtPivot = CreateContainer(wardrobeDoorsHost.transform, "Porta_Norte_Pivot");
            wdNorthExtPivot.transform.localPosition = new Vector3(0.28f, 1.130f, 0.888f);
            BuildWardrobeDoorLeaf(wdNorthExtPivot.transform, "Norte", -0.217f, -0.375f, -0.008f, pal);

            AnimatedContainerInteractable wardrobeContainer = wardrobeDoorsHost.AddComponent<AnimatedContainerInteractable>();
            wardrobeContainer.ConfigureSwingCabinet(
                id: "guarda_roupa_cian",
                primaryDoorPivot: wdNorthExtPivot.transform,
                primaryOpenEuler: new Vector3(0f, -64f, 0f),
                promptOpen: "Abrir o guarda-roupa",
                promptClose: "Fechar o guarda-roupa",
                doorSfx: FoleySoundId.WardrobeDoor,
                secondDoorPivot: wdNorthIntPivot.transform,
                secondOpenEuler: new Vector3(0f, 64f, 0f),
                speed: 2.6f
            );
            wardrobeContainer.ConfigureDay2AjarBehavior(
                ajarFraction: 0.28f,
                ajarPrompt: "Fechar a porta do guarda-roupa",
                closeThought: "Deixei tudo fechado ontem... por que essa porta tá aberta?",
                optionalTaskId: "guarda_roupa_d2_opc"
            );
            EditorUtility.SetDirty(wardrobeContainer);
            containers.bedroomWardrobe = wardrobeContainer;

            // Cômoda na parede sul do quarto, leste da porta (voltada para +Z em direção à cama -> rotY=180f)
            GameObject dresser = CreateContainer(root, "Comoda_Quarto");
            dresser.transform.localPosition = new Vector3(0.06f, 0f, 2.45f);
            dresser.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            CreateBox(dresser.transform, "Soculo_Comoda", new Vector3(0f, 0.045f, 0.01f), new Vector3(1.04f, 0.090f, 0.44f), pal.baseboardWood);
            CreateBox(dresser.transform, "Lat_Esq_Comoda", new Vector3(-0.535f, 0.460f, 0f), new Vector3(0.030f, 0.740f, 0.48f), pal.furnitureWood);
            CreateBox(dresser.transform, "Lat_Dir_Comoda", new Vector3(0.535f, 0.460f, 0f), new Vector3(0.030f, 0.740f, 0.48f), pal.furnitureWood);
            CreateBox(dresser.transform, "Fundo_Comoda", new Vector3(0f, 0.460f, 0.2275f), new Vector3(1.04f, 0.740f, 0.025f), pal.furnitureWood);
            CreateDetailBox(dresser.transform, "Travessa_Comoda_1", new Vector3(0f, 0.585f, -0.22f), new Vector3(1.04f, 0.016f, 0.04f), pal.woodMahogany);
            CreateDetailBox(dresser.transform, "Travessa_Comoda_2", new Vector3(0f, 0.340f, -0.22f), new Vector3(1.04f, 0.016f, 0.04f), pal.woodMahogany);
            CreateDetailBox(dresser.transform, "Corredica_Comoda_E_Sup", new Vector3(-0.506f, 0.678f, 0f), new Vector3(0.036f, 0.022f, 0.42f), pal.furnitureWood);
            CreateDetailBox(dresser.transform, "Corredica_Comoda_D_Sup", new Vector3(0.506f, 0.678f, 0f), new Vector3(0.036f, 0.022f, 0.42f), pal.furnitureWood);
            CreateDetailBox(dresser.transform, "Corredica_Comoda_E_Inf", new Vector3(-0.506f, 0.433f, 0f), new Vector3(0.036f, 0.022f, 0.42f), pal.furnitureWood);
            CreateDetailBox(dresser.transform, "Corredica_Comoda_D_Inf", new Vector3(0.506f, 0.433f, 0f), new Vector3(0.036f, 0.022f, 0.42f), pal.furnitureWood);
            CreateDetailBox(dresser.transform, "Batente_Gaveta_3_E", new Vector3(-0.509f, 0.212f, -0.205f), new Vector3(0.020f, 0.040f, 0.022f), pal.furnitureWood);
            CreateDetailBox(dresser.transform, "Batente_Gaveta_3_D", new Vector3( 0.509f, 0.212f, -0.205f), new Vector3(0.020f, 0.040f, 0.022f), pal.furnitureWood);
            Mesh dresserTopMesh = CreateChamferedRectLoftMesh("Mesh_Tampo_Comoda", 1.14f, 0.030f, 0.50f, 0.014f, 0.98f, 0.005f);
            GameObject dresserTop = CreateMeshObject(dresser.transform, "Tampo_Comoda", dresserTopMesh, pal.woodMahogany, new Vector3(0f, 0.830f, 0f), castShadows: true);
            BoxCollider dresserTopCol = dresserTop.AddComponent<BoxCollider>();
            dresserTopCol.center = new Vector3(0f, 0.015f, 0f);
            dresserTopCol.size = new Vector3(1.14f, 0.030f, 0.50f);

            GameObject drTop = CreateContainer(dresser.transform, "Gaveta_Comoda_Superior");
            drTop.transform.localPosition = new Vector3(0f, 0.708f, -0.01f);
            BuildDresserDrawerBox(drTop.transform, 0.226f, pal);

            InspectableObject dresserInsp = MakeSpot(drTop.transform, "Roupas_Comoda_Interno", new Vector3(0f, -0.02f, -0.08f), new Vector3(0.68f, 0.12f, 0.24f));
            dresserInsp.ConfigureTask(
                id: "comoda_quarto_opc",
                prompt: "Olhar a gaveta da cômoda",
                subtitle: "Faz meses que só revezo as mesmas três camisas sociais.",
                repeatSubtitle: "Nada que preste aqui.",
                linear: false,
                sound: FoleySoundId.ClothInspect
            );

            AnimatedContainerInteractable drTopAnim = drTop.AddComponent<AnimatedContainerInteractable>();
            drTopAnim.ConfigureSlideDrawer(
                id: "gaveta_comoda_superior",
                drawerTransform: drTop.transform,
                slideOffset: new Vector3(0f, 0f, -0.28f),
                promptOpen: "Abrir a gaveta de cima da cômoda",
                promptClose: "Fechar a gaveta de cima",
                slideSfx: FoleySoundId.DrawerSlide
            );
            drTopAnim.RegisterInteriorObject(dresserInsp);
            EditorUtility.SetDirty(drTopAnim);

            GameObject drBottom = CreateContainer(dresser.transform, "Gaveta_Comoda_Inferior");
            drBottom.transform.localPosition = new Vector3(0f, 0.463f, -0.01f);
            BuildDresserDrawerBox(drBottom.transform, 0.226f, pal);

            BuildDresserTopAndDrawerCraft(dresser.transform, dresserTop.transform, drTop.transform, drBottom.transform, pal);

            AnimatedContainerInteractable drBottomAnim = drBottom.AddComponent<AnimatedContainerInteractable>();
            drBottomAnim.ConfigureSlideDrawer(
                id: "gaveta_comoda_inferior",
                drawerTransform: drBottom.transform,
                slideOffset: new Vector3(0f, 0f, -0.25f),
                promptOpen: "Abrir a gaveta do meio da cômoda",
                promptClose: "Fechar a gaveta do meio",
                slideSfx: FoleySoundId.DrawerSlide,
                thoughtOnFirstOpen: "Inchada de umidade. Só cobertor velho guardado aí embaixo."
            );
            EditorUtility.SetDirty(drBottomAnim);

            GameObject drFixedLow = CreateContainer(dresser.transform, "Gaveta_Comoda_Base_Fixa");
            drFixedLow.transform.localPosition = new Vector3(0f, 0.212f, -0.01f);
            CreateBox(drFixedLow.transform, "Frente_Gaveta_3", new Vector3(0f, 0f, -0.23f), new Vector3(1.03f, 0.236f, 0.024f), pal.furnitureWood);
            BuildOpenFrameTrimBars(drFixedLow.transform, "Moldura_Gav_3", new Vector3(0f, 0f, -0.245f), 0.94f, 0.170f, 0.018f, 0.006f, pal.baseboardWood);
            CreateDetailBox(drFixedLow.transform, "Almofada_Gav_3", new Vector3(0f, 0f, -0.245f), new Vector3(0.896f, 0.126f, 0.005f), pal.woodMahogany);
            BuildCabinetBarHandle(drFixedLow.transform, "Puxador_Esq_3", new Vector3(-0.26f, 0f, -0.258f), 0.11f, horizontal: true, pal);
            BuildCabinetBarHandle(drFixedLow.transform, "Puxador_Dir_3", new Vector3( 0.26f, 0f, -0.258f), 0.11f, horizontal: true, pal);
        }

        private static void BuildWardrobeDoorLeaf(
            Transform pivot, string tag, float centerZ, float handleZ, float hingeZOffset, GreyboxPalette pal)
        {
            CreateBox(pivot, $"Folha_Porta_{tag}", new Vector3(0f, 0f, centerZ), new Vector3(0.028f, 2.01f, 0.434f), pal.furnitureWood, hasCollider: true);
            BuildOpenFrameTrimBarsRotatedZ(pivot, $"Moldura_Alta_{tag}", new Vector3(0.0165f, 0.42f, centerZ), 0.34f, 0.92f, 0.022f, 0.009f, pal.baseboardWood);
            CreateDetailBox(pivot, $"Painel_Alto_{tag}", new Vector3(0.016f, 0.42f, centerZ), new Vector3(0.004f, 0.86f, 0.28f), pal.woodMahogany);
            BuildOpenFrameTrimBarsRotatedZ(pivot, $"Moldura_Baixa_{tag}", new Vector3(0.0165f, -0.52f, centerZ), 0.34f, 0.72f, 0.022f, 0.009f, pal.baseboardWood);
            CreateDetailBox(pivot, $"Painel_Baixo_{tag}", new Vector3(0.016f, -0.52f, centerZ), new Vector3(0.004f, 0.66f, 0.28f), pal.woodMahogany);
            BuildCabinetBarHandle(pivot, $"Puxador_{tag}", new Vector3(0.026f, -0.04f, handleZ), 0.14f, horizontal: false, pal);
            CreateDetailBox(pivot, $"Espelho_Chave_GuardaRoupa_{tag}", new Vector3(0.016f, -0.14f, handleZ), new Vector3(0.004f, 0.036f, 0.018f), pal.metalBrass);
            CreateDetailBox(pivot, $"Furo_Chave_GuardaRoupa_{tag}", new Vector3(0.0188f, -0.14f, handleZ), new Vector3(0.002f, 0.014f, 0.004f), pal.shadowBlack);

            float[] hingeYs = { 0.76f, 0f, -0.76f };
            for (int i = 0; i < hingeYs.Length; i++)
            {
                CreateDetailCylinder(pivot, $"Dobradica_{tag}_{i}", new Vector3(0.014f, hingeYs[i], hingeZOffset), new Vector3(0.012f, 0.026f, 0.012f), pal.metalBrass);
            }
        }

        private static void BuildCabinetBarHandle(
            Transform parent, string name, Vector3 pos, float spanLen, bool horizontal, GreyboxPalette pal)
        {
            GameObject h = CreateContainer(parent, name);
            h.transform.localPosition = pos;
            if (horizontal)
            {
                CreateDetailBox(h.transform, "Barra", new Vector3(0f, 0f, -0.008f), new Vector3(spanLen, 0.012f, 0.010f), pal.metalBrass);
                CreateDetailCylinder(h.transform, "Pino_E", new Vector3(-spanLen * 0.38f, 0f, 0f), new Vector3(0.010f, 0.007f, 0.010f), pal.metalBrass, new Vector3(90f, 0f, 0f));
                CreateDetailCylinder(h.transform, "Pino_D", new Vector3( spanLen * 0.38f, 0f, 0f), new Vector3(0.010f, 0.007f, 0.010f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            }
            else
            {
                CreateDetailBox(h.transform, "Barra", new Vector3(0.008f, 0f, 0f), new Vector3(0.010f, spanLen, 0.012f), pal.metalBrass);
                CreateDetailCylinder(h.transform, "Pino_Sup", new Vector3(0f,  spanLen * 0.38f, 0f), new Vector3(0.010f, 0.007f, 0.010f), pal.metalBrass, new Vector3(0f, 0f, 90f));
                CreateDetailCylinder(h.transform, "Pino_Inf", new Vector3(0f, -spanLen * 0.38f, 0f), new Vector3(0.010f, 0.007f, 0.010f), pal.metalBrass, new Vector3(0f, 0f, 90f));
            }
        }

        private static void BuildNightstandFrame(Transform stand, GreyboxPalette pal)
        {
            float lx = 0.22f, lz = 0.18f;
            string[] legTags = { "FE", "FD", "TE", "TD" };
            Vector3[] legPos =
            {
                new Vector3(-lx, 0.175f, -lz),
                new Vector3( lx, 0.175f, -lz),
                new Vector3(-lx, 0.175f,  lz),
                new Vector3( lx, 0.175f,  lz)
            };
            Mesh nsLegMesh = CreateChamferedRectLoftMesh("Mesh_Perna_Criado", 0.042f, 0.326f, 0.042f, 0.006f, 0.94f, 0.004f);
            for (int i = 0; i < 4; i++)
            {
                CreateMeshObject(stand, $"Perna_{legTags[i]}", nsLegMesh, pal.furnitureWood, new Vector3(legPos[i].x, 0.024f, legPos[i].z), castShadows: true);
                CreateDetailBox(stand, $"Ponteira_Pe_{legTags[i]}", new Vector3(legPos[i].x, 0.012f, legPos[i].z), new Vector3(0.044f, 0.024f, 0.044f), pal.metalBrass);
            }
            CreateBox(stand, "Prateleira_Revistas", new Vector3(0f, 0.132f, 0f), new Vector3(0.436f, 0.024f, 0.39f), pal.furnitureWood);

            // Caixa da gaveta com folga exata de 1mm abaixo do fundo da gaveta (topo em Y=0.358m)
            CreateBox(stand, "Piso_Caixa_Gaveta", new Vector3(0f, 0.348f, -0.012f), new Vector3(0.446f, 0.020f, 0.390f), pal.furnitureWood);
            CreateBox(stand, "Lat_Esq_Criado", new Vector3(-0.241f, 0.4335f, 0f), new Vector3(0.032f, 0.187f, 0.42f), pal.furnitureWood);
            CreateBox(stand, "Lat_Dir_Criado", new Vector3(0.241f, 0.4335f, 0f), new Vector3(0.032f, 0.187f, 0.42f), pal.furnitureWood);
            CreateBox(stand, "Fundo_Criado", new Vector3(0f, 0.4335f, 0.1975f), new Vector3(0.45f, 0.187f, 0.025f), pal.furnitureWood);
            CreateDetailBox(stand, "Friso_Sob_Tampo", new Vector3(0f, 0.531f, 0f), new Vector3(0.520f, 0.008f, 0.436f), pal.baseboardWood);
            Mesh nsTopMesh = CreateChamferedRectLoftMesh("Mesh_Tampo_Criado", 0.54f, 0.025f, 0.45f, 0.012f, 0.98f, 0.005f);
            GameObject nsTopObj = CreateMeshObject(stand, "Tampo_Criado", nsTopMesh, pal.woodMahogany, new Vector3(0f, 0.535f, 0f), castShadows: true);
            BoxCollider nsTopCol = nsTopObj.AddComponent<BoxCollider>();
            nsTopCol.center = new Vector3(0f, 0.0125f, 0f);
            nsTopCol.size = new Vector3(0.54f, 0.025f, 0.45f);
        }

        private static void BuildDresserDrawerBox(Transform dr, float frontHeight, GreyboxPalette pal)
        {
            CreateBox(dr, "Fundo_Gaveta", new Vector3(0f, -0.085f, -0.019f), new Vector3(0.964f, 0.016f, 0.408f), pal.woodLight, hasCollider: true);
            CreateDetailBox(dr, "Lat_Esq", new Vector3(-0.485f, -0.030f, -0.015f), new Vector3(0.018f, 0.136f, 0.424f), pal.woodLight);
            CreateDetailBox(dr, "Lat_Dir", new Vector3(0.485f, -0.030f, -0.015f), new Vector3(0.018f, 0.136f, 0.424f), pal.woodLight);
            CreateDetailBox(dr, "Traseira", new Vector3(0f, -0.025f, 0.184f), new Vector3(0.960f, 0.116f, 0.018f), pal.woodLight);
            CreateBox(dr, "Frente_Gaveta", new Vector3(0f, 0f, -0.23f), new Vector3(1.03f, frontHeight, 0.024f), pal.furnitureWood, hasCollider: true);
            BuildOpenFrameTrimBars(dr, "Moldura_Gaveta", new Vector3(0f, 0f, -0.245f), 0.94f, frontHeight - 0.060f, 0.018f, 0.006f, pal.baseboardWood);
            CreateDetailBox(dr, "Almofada_Gaveta", new Vector3(0f, 0f, -0.245f), new Vector3(0.896f, frontHeight - 0.104f, 0.005f), pal.woodMahogany);
            BuildCabinetBarHandle(dr, "Puxador_Esq", new Vector3(-0.26f, 0f, -0.258f), 0.11f, horizontal: true, pal);
            BuildCabinetBarHandle(dr, "Puxador_Dir", new Vector3( 0.26f, 0f, -0.258f), 0.11f, horizontal: true, pal);
        }

        private static void BuildCorridorAndEntrance(Transform root, GreyboxPalette pal)
        {
            // Passadeira encorpada com base antiderrapante, bordas duplas, filetes e franjas, mantendo folga de 13cm das lascas de gesso no Dia 3
            GameObject runner = CreateContainer(root, "Passadeira_Corredor");
            runner.transform.localPosition = new Vector3(-1.56f, 0f, 1.28f);
            CreateDetailBox(runner.transform, "Base_Antiderrapante", new Vector3(0f, 0.001f, 0f), new Vector3(7.14f, 0.002f, 0.74f), pal.shadowBlack);
            CreateDetailBox(runner.transform, "Borda_Passadeira", new Vector3(0f, 0.0035f, 0f), new Vector3(7.18f, 0.003f, 0.78f), pal.rugBorderBurgundy);
            CreateDetailBox(runner.transform, "Campo_Passadeira", new Vector3(0f, 0.0065f, 0f), new Vector3(6.96f, 0.003f, 0.66f), pal.rugFabric);
            CreateDetailBox(runner.transform, "Filete_Interno_S", new Vector3(0f, 0.0092f, -0.275f), new Vector3(6.78f, 0.0024f, 0.024f), pal.rugBorderNavy);
            CreateDetailBox(runner.transform, "Filete_Interno_N", new Vector3(0f, 0.0092f,  0.275f), new Vector3(6.78f, 0.0024f, 0.024f), pal.rugBorderNavy);
            CreateDetailBox(runner.transform, "Filete_Interno_O", new Vector3(-3.38f, 0.0092f, 0f), new Vector3(0.024f, 0.0024f, 0.526f), pal.rugBorderNavy);
            CreateDetailBox(runner.transform, "Filete_Interno_L", new Vector3( 3.38f, 0.0092f, 0f), new Vector3(0.024f, 0.0024f, 0.526f), pal.rugBorderNavy);
            CreateDetailBox(runner.transform, "Listra_Central", new Vector3(0f, 0.0093f, 0f), new Vector3(6.68f, 0.0026f, 0.12f), pal.rugAccentOlive);
            for (int f = -3; f <= 3; f++)
            {
                float fz = f * 0.105f;
                CreateDetailBox(runner.transform, $"Franja_Oeste_{f + 3}", new Vector3(-3.612f, 0.0015f, fz), new Vector3(0.044f, 0.003f, 0.028f), pal.paperWhite);
                CreateDetailBox(runner.transform, $"Franja_Leste_{f + 3}", new Vector3( 3.612f, 0.0015f, fz), new Vector3(0.044f, 0.003f, 0.028f), pal.paperWhite);
            }

            // Capacho emoldurado com miolo fibroso texturizado e correspondência apoiada exatamente sobre o topo
            GameObject doormat = CreateContainer(root, "Capacho_Entrada");
            doormat.transform.localPosition = new Vector3(-6.16f, 0f, 1.28f);
            GameObject doormatCore = CreateDetailBox(doormat.transform, "Base_Capacho", new Vector3(0f, 0.005f, 0f), new Vector3(0.58f, 0.010f, 0.72f), pal.woodLight);
            CreateDetailBox(doormat.transform, "Moldura_Capacho_O", new Vector3(-0.315f, 0.0065f, 0f), new Vector3(0.050f, 0.013f, 0.82f), pal.baseboardWood);
            CreateDetailBox(doormat.transform, "Moldura_Capacho_L", new Vector3( 0.315f, 0.0065f, 0f), new Vector3(0.050f, 0.013f, 0.82f), pal.baseboardWood);
            CreateDetailBox(doormat.transform, "Moldura_Capacho_S", new Vector3(0f, 0.0065f, -0.385f), new Vector3(0.58f, 0.013f, 0.050f), pal.baseboardWood);
            CreateDetailBox(doormat.transform, "Moldura_Capacho_N", new Vector3(0f, 0.0065f,  0.385f), new Vector3(0.58f, 0.013f, 0.050f), pal.baseboardWood);
            GameObject doormatCenterRib = doormatCore;
            for (int r = -2; r <= 2; r++)
            {
                GameObject rib = CreateDetailBox(doormat.transform, $"Nervura_Capacho_{r + 2}", new Vector3(0f, 0.011f, r * 0.12f), new Vector3(0.54f, 0.002f, 0.072f), pal.furnitureWood);
                if (r == 0) doormatCenterRib = rib;
            }
            GameObject doormatMail = BuildUnopenedMailBundle(root, "Contas_Debaixo_Porta", new Vector3(-6.32f, 0.012f, 1.26f), 84f, pal, count: 3, envelopePrintMat: pal.details.mailEnvelope, printAngleY: -90f);
            SnapToSupportTop(doormatMail, doormatCenterRib);

            // Folha da porta da rua encaixada no vão Z=[+0.82..+1.74] de Parede_Oeste (X=-6.62), folga limpa de 3mm sobre soleira e nas laterais
            CreateBox(root, "Porta_Rua_Folha", new Vector3(-6.614f, 1.051f, 1.28f), new Vector3(0.048f, 2.090f, 0.914f), pal.doorWood, hasCollider: false);
            BuildOpenFrameTrimBarsRotatedZ(root, "Moldura_Rua_Sup", new Vector3(-6.586f, 1.48f, 1.28f), 0.54f, 0.80f, 0.024f, 0.010f, pal.baseboardWood);
            CreateDetailBox(root, "Painel_Rua_Sup", new Vector3(-6.5865f, 1.48f, 1.28f), new Vector3(0.007f, 0.72f, 0.46f), pal.doorWood);
            BuildOpenFrameTrimBarsRotatedZ(root, "Moldura_Rua_Inf", new Vector3(-6.586f, 0.57f, 1.28f), 0.54f, 0.78f, 0.024f, 0.010f, pal.baseboardWood);
            CreateDetailBox(root, "Painel_Rua_Inf", new Vector3(-6.5865f, 0.57f, 1.28f), new Vector3(0.007f, 0.70f, 0.46f), pal.doorWood);
            CreateDetailBox(root, "Travessa_Rua_Reforco", new Vector3(-6.585f, 1.03f, 1.28f), new Vector3(0.010f, 0.055f, 0.58f), pal.baseboardWood);
            CreateDetailBox(root, "Barra_Rua_Base", new Vector3(-6.585f, 0.068f, 1.28f), new Vector3(0.008f, 0.110f, 0.86f), pal.metalDark);
            BuildOpenFrameTrimBarsZ(root, "Moldura_Fenda_Correio", new Vector3(-6.580f, 0.32f, 1.28f), 0.24f, 0.064f, 0.010f, 0.006f, pal.metalBrass);
            CreateDetailBox(root, "Aba_Correio_Porta", new Vector3(-6.579f, 0.32f, 1.28f), new Vector3(0.005f, 0.042f, 0.216f), pal.metalBrass, new Vector3(0f, 0f, -8f));
            CreateDetailCylinder(root, "Olho_Magico", new Vector3(-6.585f, 1.60f, 1.28f), new Vector3(0.028f, 0.0085f, 0.028f), pal.metalBrass, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(root, "Vidro_Olho_Magico", new Vector3(-6.575f, 1.60f, 1.28f), new Vector3(0.014f, 0.0015f, 0.014f), pal.details.clearGlass, new Vector3(0f, 0f, 90f));
            CreateDetailBox(root, "Espelho_Trinco", new Vector3(-6.584f, 1.03f, 1.66f), new Vector3(0.012f, 0.21f, 0.068f), pal.metalBrass);
            CreateDetailBox(root, "Miolo_Espelho_Trinco", new Vector3(-6.578f, 1.03f, 1.66f), new Vector3(0.004f, 0.185f, 0.052f), pal.woodMahogany);
            CreateDetailCylinder(root, "Cilindro_Tranca", new Vector3(-6.574f, 1.17f, 1.66f), new Vector3(0.032f, 0.014f, 0.032f), pal.metalBrass, new Vector3(0f, 0f, 90f));
            CreateDetailBox(root, "Trava_Interna_Borboleta", new Vector3(-6.556f, 1.17f, 1.66f), new Vector3(0.014f, 0.030f, 0.007f), pal.metalBrass);
            CreateDetailBox(root, "Trilho_Corrente", new Vector3(-6.582f, 1.50f, 1.63f), new Vector3(0.016f, 0.028f, 0.12f), pal.metalBrass);
            CreateDetailBox(root, "Base_Corrente_Batente", new Vector3(-6.535f, 1.50f, 1.755f), new Vector3(0.018f, 0.030f, 0.024f), pal.metalBrass);
            CreateBox(root, "Porta_Rua_Corrente", new Vector3(-6.562f, 1.482f, 1.64f), new Vector3(0.008f, 0.008f, 0.016f), pal.metalBrass, hasCollider: false);
            for (int c = 0; c < 5; c++)
            {
                float frac = (c + 0.5f) / 5f;
                float ez = Mathf.Lerp(1.59f, 1.75f, frac);
                float sagY = 1.495f - Mathf.Sin(frac * Mathf.PI) * 0.032f;
                CreateDetailCylinder(root, $"Corrente_Elo_{c}", new Vector3(-6.558f, sagY, ez), new Vector3(0.014f, 0.004f, 0.014f), pal.metalBrass, new Vector3(0f, 0f, (c % 2 == 0) ? 90f : 0f));
            }
            CreateDetailCylinder(root, "Roseta_Rua_Macaneta", new Vector3(-6.577f, 0.99f, 1.66f), new Vector3(0.038f, 0.004f, 0.038f), pal.metalBrass, new Vector3(0f, 0f, 90f));
            CreateCylinder(root, "Porta_Rua_Macaneta", new Vector3(-6.564f, 0.99f, 1.66f), new Vector3(0.020f, 0.016f, 0.020f), pal.metalBrass, localEuler: new Vector3(0f, 0f, 90f));
            CreateDetailBox(root, "Alavanca_Porta_Rua", new Vector3(-6.548f, 0.99f, 1.608f), new Vector3(0.015f, 0.016f, 0.118f), pal.metalBrass);
            CreateDetailBox(root, "Ponta_Alavanca_Rua", new Vector3(-6.562f, 0.99f, 1.557f), new Vector3(0.013f, 0.012f, 0.012f), pal.metalBrass);

            InspectableObject frontDoor = MakeSpot(root, "Porta_Rua_Interagir", new Vector3(-6.48f, 1.08f, 1.28f), new Vector3(0.24f, 2.08f, 0.92f));
            frontDoor.ConfigureTask(
                id: "porta_rua_opc",
                prompt: "Olhar a porta da rua",
                subtitle: "Duas voltas na chave e corrente passada. Não abro essa porta pra ninguém.",
                repeatSubtitle: "Cheia de folheto acumulado por baixo.",
                linear: false,
                sound: FoleySoundId.DoorLocked
            );

            // Cabideiro posicionado livre do capacho, da passadeira (início em X=-5.15, borda N em Z=1.67), do rodapé (Z=2.006) e da parede norte
            GameObject rack = CreateContainer(root, "Cabideiro_Entrada");
            rack.transform.localPosition = new Vector3(-5.60f, 0f, 1.835f);
            rack.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            BuildHallwayCoatRackProps(rack.transform, pal);

            InspectableObject rackInsp = MakeSpot(root, "Cabideiro_Interagir", new Vector3(-5.60f, 1.00f, 1.835f), new Vector3(0.46f, 1.90f, 0.46f));
            rackInsp.ConfigureTask(
                id: "cabideiro_opc",
                prompt: "Olhar o cabideiro",
                subtitle: "Esse guarda-chuva vermelho tá aí pendurado desde o ano passado.",
                repeatSubtitle: "Ninguém mexe nisso.",
                linear: false,
                sound: FoleySoundId.ClothInspect
            );

            // Aparador do telefone deslocado para Z=1.825f livre do rodapé/parede norte e da passadeira, com tampo/pernas chanfrados e ponteiras metálicas
            GameObject phoneStand = CreateContainer(root, "Aparador_Telefone_Corredor");
            phoneStand.transform.localPosition = new Vector3(-3.35f, 0f, 1.825f);

            Mesh phoneTopMesh = CreateChamferedRectLoftMesh("Mesh_Tampo_Aparador_Tel", 0.84f, 0.050f, 0.33f, 0.014f, 0.98f, 0.006f);
            GameObject phoneStandTop = CreateMeshObject(phoneStand.transform, "Tampo_Aparador", phoneTopMesh, pal.woodMahogany, new Vector3(0f, 0.715f, 0f), castShadows: true);
            BoxCollider phoneTopCol = phoneStandTop.AddComponent<BoxCollider>();
            phoneTopCol.center = new Vector3(0f, 0.025f, 0f);
            phoneTopCol.size = new Vector3(0.84f, 0.050f, 0.33f);
            CreateDetailBox(phoneStand.transform, "Friso_Sob_Tampo_Aparador", new Vector3(0f, 0.711f, 0f), new Vector3(0.80f, 0.008f, 0.29f), pal.baseboardWood);
            float apLx = 0.37f, apLz = 0.116f;
            string[] apTags = { "FE", "FD", "TE", "TD" };
            Vector2[] apXZ = { new(-apLx, -apLz), new(apLx, -apLz), new(-apLx, apLz), new(apLx, apLz) };
            Mesh apLegMesh = CreateChamferedRectLoftMesh("Mesh_Perna_Aparador_Tel", 0.042f, 0.683f, 0.042f, 0.006f, 0.95f, 0.004f);
            for (int li = 0; li < 4; li++)
            {
                GameObject legObj = CreateMeshObject(phoneStand.transform, $"Perna_Aparador_{apTags[li]}", apLegMesh, pal.furnitureWood, new Vector3(apXZ[li].x, 0.024f, apXZ[li].y), castShadows: true);
                BoxCollider legCol = legObj.AddComponent<BoxCollider>();
                legCol.center = new Vector3(0f, 0.3415f, 0f);
                legCol.size = new Vector3(0.042f, 0.683f, 0.042f);
                CreateDetailBox(phoneStand.transform, $"Ponteira_Aparador_{apTags[li]}", new Vector3(apXZ[li].x, 0.012f, apXZ[li].y), new Vector3(0.044f, 0.024f, 0.044f), pal.metalBrass);
            }
            CreateDetailBox(phoneStand.transform, "Avental_Frente", new Vector3(0f, 0.637f, -apLz), new Vector3(0.698f, 0.14f, 0.024f), pal.furnitureWood);
            CreateDetailBox(phoneStand.transform, "Travessa_Traseira", new Vector3(0f, 0.637f, apLz), new Vector3(0.698f, 0.14f, 0.024f), pal.furnitureWood);
            CreateDetailBox(phoneStand.transform, "Avental_Esq", new Vector3(-apLx, 0.637f, 0f), new Vector3(0.024f, 0.14f, 0.190f), pal.furnitureWood);
            CreateDetailBox(phoneStand.transform, "Avental_Dir", new Vector3(apLx, 0.637f, 0f), new Vector3(0.024f, 0.14f, 0.190f), pal.furnitureWood);
            BuildOpenFrameTrimBars(phoneStand.transform, "Moldura_Frente_Aparador", new Vector3(0f, 0.637f, -0.132f), 0.62f, 0.104f, 0.014f, 0.006f, pal.baseboardWood);
            CreateDetailBox(phoneStand.transform, "Almofada_Frente_Aparador", new Vector3(0f, 0.637f, -0.131f), new Vector3(0.58f, 0.068f, 0.004f), pal.woodMahogany);
            CreateDetailCylinder(phoneStand.transform, "Puxador_Aparador_Esq", new Vector3(-0.20f, 0.637f, -0.139f), new Vector3(0.018f, 0.006f, 0.018f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(phoneStand.transform, "Puxador_Aparador_Dir", new Vector3(0.20f, 0.637f, -0.139f), new Vector3(0.018f, 0.006f, 0.018f), pal.metalBrass, new Vector3(90f, 0f, 0f));

            CreateDetailBox(phoneStand.transform, "Travessa_Inf_Esq", new Vector3(-apLx, 0.190f, 0f), new Vector3(0.024f, 0.022f, 0.190f), pal.furnitureWood);
            CreateDetailBox(phoneStand.transform, "Travessa_Inf_Dir", new Vector3(apLx, 0.190f, 0f), new Vector3(0.024f, 0.022f, 0.190f), pal.furnitureWood);
            GameObject phoneLowShelf = CreateBox(phoneStand.transform, "Prateleira_Inferior", new Vector3(0f, 0.190f, 0f), new Vector3(0.698f, 0.022f, 0.256f), pal.furnitureWood, hasCollider: false);

            GameObject phoneBook = CreateContainer(phoneStand.transform, "Lista_Telefonica");
            phoneBook.transform.localPosition = new Vector3(-0.10f, 0.201f, 0f);
            phoneBook.transform.localRotation = Quaternion.Euler(0f, 6f, 0f);
            CreateDetailBox(phoneBook.transform, "Capa_Inf_Lista", new Vector3(0f, 0.0025f, 0f), new Vector3(0.204f, 0.005f, 0.264f), pal.bookMustard);
            CreateDetailBox(phoneBook.transform, "Paginas_Lista", new Vector3(0.004f, 0.039f, 0f), new Vector3(0.192f, 0.068f, 0.252f), pal.paperWhite);
            CreateDetailBox(phoneBook.transform, "Capa_Sup_Lista", new Vector3(0f, 0.0755f, 0f), new Vector3(0.204f, 0.005f, 0.264f), pal.bookMustard);
            CreateDetailBox(phoneBook.transform, "Lombada_Lista", new Vector3(-0.098f, 0.039f, 0f), new Vector3(0.008f, 0.078f, 0.264f), pal.bookBurgundy);
            CreateDetailBox(phoneBook.transform, "Etiqueta_Lombada_Lista", new Vector3(-0.103f, 0.042f, 0f), new Vector3(0.002f, 0.044f, 0.092f), pal.paperWhite);
            SnapToSupportTop(phoneBook, phoneLowShelf);

            GameObject landline = BuildDetailedLandlinePhone(phoneStand.transform, "Telefone_Fixo", new Vector3(-0.26f, 0.765f, 0f), 0f, pal);
            SnapToSupportTop(landline, phoneStandTop);

            GameObject notePad = CreateContainer(phoneStand.transform, "Bloco_Recados");
            notePad.transform.localPosition = new Vector3(-0.02f, 0.765f, -0.03f);
            notePad.transform.localRotation = Quaternion.Euler(0f, -10f, 0f);
            GameObject padBody = CreateDetailBox(notePad.transform, "Folhas_Bloco", new Vector3(0f, 0.004f, 0f), new Vector3(0.11f, 0.008f, 0.15f), pal.paperWhite);
            CreateDetailBox(notePad.transform, "Cola_Topo_Bloco", new Vector3(0f, 0.004f, 0.076f), new Vector3(0.11f, 0.008f, 0.006f), pal.folderKraft);
            CreateDetailBox(notePad.transform, "Folha_Solta_Bloco", new Vector3(0.004f, 0.009f, -0.006f), new Vector3(0.104f, 0.0015f, 0.138f), pal.porcelainWhite, new Vector3(0f, 5f, 0f));
            SnapToSupportTop(notePad, phoneStandTop);

            GameObject padPen = CreateContainer(phoneStand.transform, "Caneta_Bloco");
            padPen.transform.localPosition = new Vector3(-0.02f, 0.775f, -0.03f);
            padPen.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
            CreateDetailCylinder(padPen.transform, "Corpo_Caneta", Vector3.zero, new Vector3(0.009f, 0.056f, 0.009f), pal.plasticSlate, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(padPen.transform, "Ponta_Caneta", new Vector3(0f, 0f, -0.064f), new Vector3(0.006f, 0.008f, 0.006f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailBox(padPen.transform, "Clipe_Caneta", new Vector3(0f, 0.004f, 0.034f), new Vector3(0.002f, 0.004f, 0.032f), pal.metalBrass);
            SnapToSupportTop(padPen, padBody);

            BuildRoutedWallCable(phoneStand.transform, "Cabo_Linha_Telefone", "Mesh_Cabo_Linha_Telefone", new[]
            {
                new Vector3(-0.280f, 0.779f, 0.099f),
                new Vector3(-0.240f, 0.770f, 0.132f),
                new Vector3( 0.250f, 0.770f, 0.132f),
                new Vector3( 0.275f, 0.789f, 0.092f)
            }, pal.metalDark, radius: 0.0038f);
        }

        private static void BuildOffice(Transform root, GreyboxPalette pal)
        {
            // v12 Escritório: X [+1.48..+6.34] (4.86m), Z [-5.84..+0.38] (6.22m)
            // Tapete detalhado cobrindo a área da escrivaninha e cadeira; topo exato em Y = 0.010f
            BuildOfficeRugDetails(root, pal);

            GameObject desk = CreateContainer(root, "Mesa_Escritorio");
            desk.transform.localPosition = new Vector3(5.72f, 0f, -2.55f);
            GameObject deskTop = BuildOfficeDeskStructureAndLamp(desk.transform, pal);
            BuildOfficeDeskAccessories(desk.transform, deskTop, pal);

            // Caixa da gaveta da escrivaninha sem interpenetrar Pe_Mesa_1 (-0.34, -0.78)
            CreateBox(desk.transform, "Suporte_Gaveta_Esq", new Vector3(-0.15f, 0.640f, -0.720f), new Vector3(0.50f, 0.140f, 0.03f), pal.furnitureWood, hasCollider: false);
            CreateBox(desk.transform, "Suporte_Gaveta_Dir", new Vector3(-0.15f, 0.640f, -0.300f), new Vector3(0.50f, 0.140f, 0.03f), pal.furnitureWood, hasCollider: false);
            CreateDetailBox(desk.transform, "Corredica_Mesa_Sul", new Vector3(-0.15f, 0.635f, -0.695f), new Vector3(0.48f, 0.022f, 0.030f), pal.furnitureWood);
            CreateDetailBox(desk.transform, "Corredica_Mesa_Norte", new Vector3(-0.15f, 0.635f, -0.325f), new Vector3(0.48f, 0.022f, 0.030f), pal.furnitureWood);
            CreateDetailBox(desk.transform, "Piso_Caixa_Gaveta", new Vector3(-0.15f, 0.563f, -0.510f), new Vector3(0.50f, 0.014f, 0.39f), pal.furnitureWood);
            CreateDetailBox(desk.transform, "Teto_Caixa_Gaveta", new Vector3(-0.15f, 0.715f, -0.510f), new Vector3(0.50f, 0.010f, 0.39f), pal.furnitureWood);

            GameObject drawer = CreateContainer(desk.transform, "Gaveta_Formulario_Medico");
            drawer.transform.localPosition = new Vector3(-0.15f, 0.640f, -0.510f);
            GameObject deskDrawerBottom = CreateBox(drawer.transform, "Fundo_Gaveta", new Vector3(-0.008f, -0.048f, 0f), new Vector3(0.448f, 0.016f, 0.344f), pal.woodLight, hasCollider: true);
            CreateDetailBox(drawer.transform, "Lateral_Sul_Gaveta", new Vector3(-0.006f, -0.005f, -0.174f), new Vector3(0.456f, 0.110f, 0.016f), pal.woodLight);
            CreateDetailBox(drawer.transform, "Lateral_Norte_Gaveta", new Vector3(-0.006f, -0.005f, 0.174f), new Vector3(0.456f, 0.110f, 0.016f), pal.woodLight);
            CreateDetailBox(drawer.transform, "Traseira_Gaveta", new Vector3(0.212f, -0.001f, 0f), new Vector3(0.016f, 0.094f, 0.356f), pal.woodLight);
            CreateBox(drawer.transform, "Frente_Gaveta", new Vector3(-0.2425f, 0f, 0f), new Vector3(0.025f, 0.130f, 0.44f), pal.baseboardWood, hasCollider: true);
            BuildOfficeDeskDrawerPull(drawer.transform, pal);

            GameObject formPaper = CreateContainer(drawer.transform, "Folha_Afastamento_Interagir");
            formPaper.transform.localPosition = new Vector3(-0.06f, -0.040f, 0f);
            formPaper.transform.localRotation = Quaternion.Euler(0f, 6f, 0f);
            GameObject formMesh = BuildDetailedPaperSheet(formPaper.transform, "Folha_Afastamento_Mesh", Vector3.zero, 0f, new Vector2(0.21f, 0.30f), pal, hasRedHeader: true);
            GameObject formPen = BuildDetailedBallpointPen(formPaper.transform, "Caneta_Sobre_Formulario", new Vector3(0.05f, 0.004f, 0.04f), 28f, pal.calendarRed, pal);
            SnapToSupportTop(formPen, formMesh);
            BoxCollider formCol = formPaper.AddComponent<BoxCollider>();
            formCol.center = new Vector3(0f, 0.02f, 0f);
            formCol.size = new Vector3(0.32f, 0.07f, 0.30f);

            InspectableObject drawerDoc = formPaper.AddComponent<InspectableObject>();
            drawerDoc.ConfigureDocument(
                id: "gaveta_afastamento_opc",
                prompt: "Ler o formulário na gaveta",
                docTitle: "SOLICITAÇÃO DE AFASTAMENTO MÉDICO - DP",
                docBody:
                    "COLABORADOR: Cian Nabs | ÁREA: Contabilidade / Auditoria\n" +
                    "DATA: 14/08/1995\n\n" +
                    "MOTIVO INFORMADO PELO EMPREGADO:\n" +
                    "Crises graves de insônia há mais de quatro meses, dores constantes de cabeça,\n" +
                    "lapsos de memória durante conferências e tremores nas mãos ao fim do expediente.\n\n" +
                    "ASSINATURA DO EMPREGADO: ___________________________\n" +
                    "VISTO DA GERÊNCIA:        ___________________________",
                afterReadThought: "Escrevi isso em agosto e larguei no fundo da gaveta. Se eu entregar pro Tavares, tô na rua.",
                repeatThought: "Quem pede licença lá dentro perde a mesa.",
                linear: false,
                hudLabel: "",
                style: DocumentStyle.CorporateMemo,
                extraSound: FoleySoundId.PaperRustle
            );

            AnimatedContainerInteractable deskDrawerAnim = drawer.AddComponent<AnimatedContainerInteractable>();
            deskDrawerAnim.ConfigureSlideDrawer(
                id: "gaveta_mesa_escritorio",
                drawerTransform: drawer.transform,
                slideOffset: new Vector3(-0.34f, 0f, 0f),
                promptOpen: "Abrir a gaveta da escrivaninha",
                promptClose: "Fechar a gaveta da escrivaninha",
                slideSfx: FoleySoundId.DrawerSlide
            );
            deskDrawerAnim.RegisterInteriorObject(drawerDoc);
            EditorUtility.SetDirty(deskDrawerAnim);

            // Cadeira giratória com rodízios verticais reais em Y = 0.010f (sobre o tapete), concha traseira e encosto/assento chanfrados
            BuildOfficeChairDetails(root, pal);

            // Cesto de lixo vazado (10 lados) diretamente no piso de madeira com aro reforçado e bolinhas de papel
            GameObject bin = CreateContainer(root, "Cesto_Lixo_Escritorio");
            bin.transform.localPosition = new Vector3(5.30f, 0f, -4.16f);
            Mesh trashBinMesh = CreateFacetedLatheVesselMesh("Mesh_OfficeTrashBin", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.105f),
                (0.006f, 0.108f),
                (0.295f, 0.132f),
                (0.305f, 0.136f),
                (0.305f, 0.128f),
                (0.295f, 0.124f),
                (0.008f, 0.098f),
                (0.008f, 0.000f)
            });
            CreateMeshObject(bin.transform, "Corpo_Arame", trashBinMesh, pal.metalDark, Vector3.zero, Vector3.zero, castShadows: true);
            CreateDetailCylinder(bin.transform, "Anel_Reforco_Cesto", new Vector3(0f, 0.155f, 0f), new Vector3(0.242f, 0.008f, 0.242f), pal.stainlessSteel);
            GameObject ball1 = CreateMeshObject(bin.transform, "Papel_Amassado_1", CreateCrumpledPaperMesh("Mesh_TrashPaper_1", 0.038f, 11), pal.paperWhite, new Vector3(0.032f, 0.008f, 0.018f), new Vector3(12f, 34f, 0f), castShadows: true);
            SnapToSupportWorldY(ball1, 0.008f);
            GameObject ball2 = CreateMeshObject(bin.transform, "Papel_Amassado_2", CreateCrumpledPaperMesh("Mesh_TrashPaper_2", 0.035f, 23), pal.paperWhite, new Vector3(-0.036f, 0.008f, -0.024f), new Vector3(-8f, 118f, 14f), castShadows: true);
            SnapToSupportWorldY(ball2, 0.008f);
            GameObject ball4 = CreateMeshObject(bin.transform, "Papel_Amassado_4", CreateCrumpledPaperMesh("Mesh_TrashPaper_4", 0.036f, 51), pal.paperWhite, new Vector3(0.004f, 0.044f, -0.004f), new Vector3(16f, 74f, -12f), castShadows: true);
            SnapToSupportWorldY(ball4, 0.044f);
            GameObject ball3 = CreateMeshObject(bin.transform, "Papel_Amassado_3", CreateCrumpledPaperMesh("Mesh_TrashPaper_3", 0.036f, 37), pal.paperWhite, new Vector3(0.18f, 0f, 0.17f), new Vector3(0f, 64f, 0f), castShadows: true);
            SnapToSupportWorldY(ball3, 0f);

            // Estante na parede Sul do Escritório (Z = -5.84f; livre do rodapé em -5.824f) com painéis disjuntos
            GameObject shelf = CreateContainer(root, "Estante_Escritorio");
            shelf.transform.localPosition = new Vector3(3.30f, 0f, -5.63f);
            CreateBox(shelf.transform, "Soculo_Estante_Esc", new Vector3(0f, 0.04f, 0f), new Vector3(1.64f, 0.08f, 0.34f), pal.baseboardWood);
            CreateBox(shelf.transform, "Lat_Esq_Esc", new Vector3(-0.835f, 0.945f, 0f), new Vector3(0.03f, 1.89f, 0.34f), pal.furnitureWood);
            CreateBox(shelf.transform, "Lat_Dir_Esc", new Vector3(0.835f, 0.945f, 0f), new Vector3(0.03f, 1.89f, 0.34f), pal.furnitureWood);
            CreateBox(shelf.transform, "Fundo_Estante_Esc", new Vector3(0f, 0.985f, -0.155f), new Vector3(1.64f, 1.81f, 0.03f), pal.furnitureWood);
            Mesh offShelfTopMesh = CreateChamferedRectLoftMesh("Mesh_Topo_Estante_Esc", 1.74f, 0.040f, 0.36f, 0.014f, 0.97f, 0.006f);
            GameObject offShelfTopObj = CreateMeshObject(shelf.transform, "Topo_Estante_Esc", offShelfTopMesh, pal.woodMahogany, new Vector3(0f, 1.890f, 0f), castShadows: true);
            BoxCollider offShelfTopCol = offShelfTopObj.AddComponent<BoxCollider>();
            offShelfTopCol.center = new Vector3(0f, 0.020f, 0f);
            offShelfTopCol.size = new Vector3(1.74f, 0.040f, 0.36f);
            float[] officeShelfYs = { 0.10f, 0.58f, 1.06f, 1.50f };
            for (int s = 0; s < officeShelfYs.Length; s++)
            {
                CreateBox(shelf.transform, $"Prateleira_Esc_{s}", new Vector3(0f, officeShelfYs[s], 0.015f), new Vector3(1.64f, 0.026f, 0.31f), pal.furnitureWood, hasCollider: false);
            }

            Material[] binderMats = { pal.bookBurgundy, pal.bookSlateBlue, pal.bookTeal, pal.bookSage, pal.bookMustard, pal.metalDark };
            Material[] offBookMats = { pal.bookSlateBlue, pal.bookMustard, pal.bookBurgundy, pal.bookTeal, pal.bookTaupe, pal.bookSage };
            float[] binderBaseYs = { 0.113f, 0.593f, 1.073f };
            for (int row = 0; row < binderBaseYs.Length; row++)
            {
                for (int i = 0; i < 12; i++)
                {
                    float bh = 0.30f + ((i + row) % 3) * 0.025f;
                    float bx = -0.75f + i * 0.096f;
                    CreateDetailBox(shelf.transform, $"Fichario_{row}_{i}", new Vector3(bx, binderBaseYs[row] + bh * 0.5f, 0.02f), new Vector3(0.075f, bh, 0.25f), binderMats[(i + row) % binderMats.Length]);
                    CreateDetailBox(shelf.transform, $"Etiqueta_Fichario_{row}_{i}", new Vector3(bx, binderBaseYs[row] + bh * 0.62f, 0.1465f), new Vector3(0.050f, 0.075f, 0.003f), pal.paperWhite);
                    CreateDetailCylinder(shelf.transform, $"Anel_Fichario_{row}_{i}", new Vector3(bx, binderBaseYs[row] + bh * 0.25f, 0.1475f), new Vector3(0.022f, 0.002f, 0.022f), pal.metalBrass, new Vector3(90f, 0f, 0f));
                }

                int offBooks = row == 2 ? 6 : 7;
                for (int b = 0; b < offBooks; b++)
                {
                    float bh = 0.22f + ((b + row) % 3) * 0.026f;
                    float bz = 0.188f + (b % 2) * 0.018f;
                    float bx = 0.42f + b * 0.056f;
                    CreateDetailBox(shelf.transform, $"Livro_Esc_{row}_{b}", new Vector3(bx, binderBaseYs[row] + bh * 0.5f, -0.01f), new Vector3(0.042f, bh, bz), offBookMats[(b + row) % offBookMats.Length]);
                    CreateDetailBox(shelf.transform, $"Faixa_Livro_Esc_{row}_{b}", new Vector3(bx, binderBaseYs[row] + bh * 0.68f, -0.01f + bz * 0.5f + 0.0015f), new Vector3(0.032f, 0.020f, 0.003f), pal.bookTaupe);
                }
            }
            BuildOfficeBookshelfDetails(shelf.transform, binderBaseYs, pal);

            InspectableObject shelfInsp = MakeSpot(root, "Estante_Escritorio_Interagir", new Vector3(3.30f, 0.96f, -5.60f), new Vector3(1.76f, 1.94f, 0.44f));
            shelfInsp.ConfigureTask(
                id: "estante_escritorio_opc",
                prompt: "Olhar as pastas na estante",
                subtitle: "Cinco anos de livro-caixa em ordem. Aqui dentro pelo menos os números fazem sentido.",
                repeatSubtitle: "Os números batem. O resto não importa.",
                linear: false,
                sound: FoleySoundId.WoodInspect
            );

            // Calendário rente à face interna da parede Leste (X = +6.34f) com alça de arame presa ao prego
            GameObject calendar = CreateContainer(root, "Calendario_Parede_Visual");
            calendar.transform.localPosition = new Vector3(6.328f, 1.56f, -1.05f);
            CreateBox(calendar.transform, "Folha_Base", Vector3.zero, new Vector3(0.024f, 0.48f, 0.36f), pal.paperWhite, hasCollider: false);
            CreatePrintedOverlayQuad(calendar.transform, "Impressao_Folha_Out1995", new Vector3(-0.0148f, 0f, 0f), new Vector2(0.354f, 0.474f), pal.details.calendarOct1995, new Vector3(0f, 90f, 0f));

            for (int sp = -4; sp <= 4; sp++)
            {
                CreateDetailCylinder(calendar.transform, $"Espiral_{sp + 5}", new Vector3(-0.015f, 0.235f, sp * 0.036f), new Vector3(0.014f, 0.010f, 0.014f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            }
            BuildRoutedWallCable(calendar.transform, "Arame_Pendurar_Calendario", "Mesh_Arame_Calendario", new[]
            {
                new Vector3(-0.015f, 0.236f, -0.085f),
                new Vector3(-0.004f, 0.266f,  0.000f),
                new Vector3(-0.015f, 0.236f,  0.085f)
            }, pal.stainlessSteel, radius: 0.0022f);
            CreateDetailCylinder(calendar.transform, "Prego_Calendario", new Vector3(0.004f, 0.266f, 0f), new Vector3(0.010f, 0.014f, 0.010f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(calendar.transform, "Arruela_Prego_Calendario", new Vector3(0.010f, 0.266f, 0f), new Vector3(0.018f, 0.002f, 0.018f), pal.metalDark, new Vector3(0f, 0f, 90f));

            // Arquivo metálico na parede Leste ao sul da tomada (X = +6.08f -> trás +6.32f, 4mm livre do rodapé +6.324f)
            BuildOfficeFilingCabinet(root, pal);
        }

        private static Light[] BuildInteriorLights(Transform root, GreyboxPalette pal)
        {
            GameObject lightsRoot = CreateContainer(root, "Iluminacao_Interna");
            float lampY = H - 0.125f; // Canopla topo encostada no teto (2.742f) sem frestas
            List<Light> list = new List<Light>
            {
                CreateCeilingLamp(lightsRoot.transform, "Luz_Quarto_Cian", new Vector3(-1.24f, lampY, 4.11f), 5.0f, pal, castShadows: true, spotAngle: 134f, innerSpotAngle: 64f, shadowTier: 2),
                CreateCeilingLamp(lightsRoot.transform, "Luz_Banheiro", new Vector3(-5.03f, lampY, 4.11f), 4.8f, pal, castShadows: true, spotAngle: 132f, innerSpotAngle: 62f, shadowTier: 1),
                CreateCeilingLamp(lightsRoot.transform, "Luz_Corredor_Oeste", new Vector3(-4.00f, lampY, 1.28f), 4.8f, pal, castShadows: true, spotAngle: 132f, innerSpotAngle: 60f, shadowTier: 1),
                CreateCeilingLamp(lightsRoot.transform, "Luz_Corredor_Centro", new Vector3(-0.42f, lampY, 1.28f), 5.2f, pal, castShadows: true, spotAngle: 138f, innerSpotAngle: 66f, shadowTier: 2),
                CreateCeilingLamp(lightsRoot.transform, "Luz_Corredor_Leste", new Vector3(1.78f, lampY, 4.03f), 5.0f, pal, castShadows: true, spotAngle: 136f, innerSpotAngle: 64f, shadowTier: 1),
                CreateCeilingLamp(lightsRoot.transform, "Luz_Cozinha", new Vector3(-4.74f, lampY, -2.73f), 5.6f, pal, castShadows: true, spotAngle: 136f, innerSpotAngle: 66f, shadowTier: 2),
                CreateCeilingLamp(lightsRoot.transform, "Luz_Sala_Estar", new Vector3(-0.73f, lampY, -2.73f), 5.8f, pal, castShadows: true, spotAngle: 136f, innerSpotAngle: 66f, shadowTier: 2),
                CreateCeilingLamp(lightsRoot.transform, "Luz_Escritorio", new Vector3(3.91f, lampY, -2.73f), 5.8f, pal, castShadows: true, spotAngle: 138f, innerSpotAngle: 68f, shadowTier: 2)
            };
            return list.ToArray();
        }

        private static Light BuildCircuitDeskLight(Transform archRoot)
        {
            Transform desk = archRoot.Find("Mesa_Escritorio");
            GameObject go = new GameObject("Luz_Mesa_Escritorio_Circuito");
            if (desk != null)
            {
                go.transform.SetParent(desk, false);
                go.transform.localPosition = new Vector3(0.195f, 0.948f, -0.64f);
            }
            else
            {
                go.transform.SetParent(archRoot, false);
                go.transform.localPosition = new Vector3(5.915f, 0.948f, -3.19f);
            }
            go.transform.rotation = Quaternion.LookRotation(new Vector3(-0.46f, -0.82f, 0.34f).normalized);

            Light l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 2.9f;
            l.spotAngle = 114f;
            l.innerSpotAngle = 56f;
            l.intensity = 0.95f;
            l.color = new Color(1.00f, 0.89f, 0.70f);
            l.shadows = LightShadows.Soft;
            l.shadowNearPlane = 0.08f;
            l.shadowBias = 0.02f;
            l.shadowNormalBias = 0.25f;
            ConfigureLightURPShadowTier(l, 1);
            l.enabled = true;
            return l;
        }

        private static Light[] BuildSecondaryMotivatedLights(Transform archRoot, Transform quartoFilhasRoot)
        {
            List<Light> list = new List<Light>();

            Transform abajurParent = archRoot.Find("Criado_Mudo_Margaret/Abajur_Margaret");
            if (abajurParent != null)
            {
                GameObject go = new GameObject("Luz_Abajur_Margaret");
                go.transform.SetParent(abajurParent, false);
                go.transform.localPosition = new Vector3(0.14f, 0.745f, 0.09f);
                go.transform.localRotation = Quaternion.identity;

                Light l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 2.55f;
                l.color = new Color(1.00f, 0.83f, 0.58f);
                l.intensity = 0.95f;
                l.shadows = LightShadows.Soft;
                l.shadowNearPlane = 0.08f;
                l.shadowBias = 0.02f;
                l.shadowNormalBias = 0.25f;
                ConfigureLightURPShadowTier(l, 1);
                list.Add(l);
            }

            Transform floorLampParent = archRoot.Find("Luminaria_Coluna_Sala");
            if (floorLampParent != null)
            {
                GameObject go = new GameObject("Luz_Luminaria_Coluna_Sala");
                go.transform.SetParent(floorLampParent, false);
                go.transform.localPosition = new Vector3(0f, 1.56f, 0f);
                go.transform.localRotation = Quaternion.identity;

                Light l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 3.25f;
                l.color = new Color(1.00f, 0.84f, 0.60f);
                l.intensity = 1.02f;
                l.shadows = LightShadows.Soft;
                l.shadowNearPlane = 0.10f;
                l.shadowBias = 0.02f;
                l.shadowNormalBias = 0.25f;
                ConfigureLightURPShadowTier(l, 1);
                list.Add(l);
            }

            Transform mirrorParent = archRoot.Find("Espelho_Banheiro");
            if (mirrorParent != null)
            {
                GameObject go = new GameObject("Luz_Arandela_Banheiro");
                go.transform.SetParent(mirrorParent, false);
                go.transform.localPosition = new Vector3(0f, 0.506f, -0.035f);
                go.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -0.92f, -0.39f).normalized);

                Light l = go.AddComponent<Light>();
                l.type = LightType.Spot;
                l.range = 2.6f;
                l.spotAngle = 112f;
                l.innerSpotAngle = 56f;
                l.color = new Color(0.92f, 0.94f, 0.96f);
                l.intensity = 0.90f;
                l.shadows = LightShadows.Soft;
                l.shadowNearPlane = 0.08f;
                l.shadowBias = 0.02f;
                l.shadowNormalBias = 0.25f;
                ConfigureLightURPShadowTier(l, 1);
                list.Add(l);
            }

            return list.ToArray();
        }

        private static Light[] BuildArchitecturalFillLights(Transform archRoot, Transform quartoFilhasRoot)
        {
            Transform lightsRoot = archRoot.Find("Iluminacao_Interna") ?? archRoot;
            GameObject fillsRoot = CreateContainer(lightsRoot, "Rebote_e_Recorte");

            // Derramamento motivado do arco da cozinha acesa para a sala escura durante o apagão
            List<Light> list = new List<Light>
            {
                CreateAimedSpot(fillsRoot.transform, "Fill_Spill_ArcoCozinha_Sala", new Vector3(-2.95f, 1.95f, -2.66f), new Vector3(-0.45f, 0.55f, -2.95f), 4.1f, 84f, 0.22f, new Color(0.93f, 0.95f, 0.93f))
            };

            return list.ToArray();
        }

        private static ReflectionProbe[] BuildInternalReflectionProbes(Transform archRoot, Transform quartoFilhasRoot)
        {
            GameObject probesRoot = CreateContainer(archRoot, "Sondas_Reflexao_Internas");

            List<ReflectionProbe> probes = new List<ReflectionProbe>
            {
                CreateBoxReflectionProbe(probesRoot.transform, "Probe_Quarto_Cian", new Vector3(-1.24f, 1.37f, 4.11f), new Vector3(4.24f, 2.74f, 3.86f), 0.25f),
                CreateBoxReflectionProbe(probesRoot.transform, "Probe_Banheiro", new Vector3(-5.03f, 1.37f, 4.11f), new Vector3(3.02f, 2.74f, 3.86f), 0.25f),
                CreateBoxReflectionProbe(probesRoot.transform, "Probe_Corredor_Principal", new Vector3(-2.01f, 1.37f, 1.28f), new Vector3(9.06f, 2.74f, 1.48f), 0.35f),
                CreateBoxReflectionProbe(probesRoot.transform, "Probe_Corredor_AlaNorte", new Vector3(1.78f, 1.37f, 3.29f), new Vector3(1.48f, 2.74f, 5.50f), 0.35f),
                CreateBoxReflectionProbe(probesRoot.transform, "Probe_Cozinha", new Vector3(-4.74f, 1.37f, -2.73f), new Vector3(3.60f, 2.74f, 6.22f), 0.35f),
                CreateBoxReflectionProbe(probesRoot.transform, "Probe_Sala", new Vector3(-0.73f, 1.37f, -2.73f), new Vector3(4.10f, 2.74f, 6.22f), 0.35f),
                CreateBoxReflectionProbe(probesRoot.transform, "Probe_Escritorio", new Vector3(3.91f, 1.37f, -2.73f), new Vector3(4.86f, 2.74f, 6.22f), 0.25f)
            };

            Transform d4Root = quartoFilhasRoot.Find("Ambiente_QuartoFilhas_Greybox") ?? quartoFilhasRoot;
            probes.Add(CreateBoxReflectionProbe(d4Root, "Probe_Quarto_Filhas", new Vector3(4.51f, 1.37f, 3.29f), new Vector3(3.66f, 2.74f, 5.50f), 0.25f));

            return probes.ToArray();
        }

        private static Light CreateAimedSpot(
            Transform parent, string name, Vector3 pos, Vector3 lookTarget, float range, float spotAngle, float intensity, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            Vector3 dir = lookTarget - pos;
            go.transform.rotation = Quaternion.LookRotation(dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.down);

            Light l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = range;
            l.spotAngle = spotAngle;
            l.innerSpotAngle = spotAngle * 0.48f;
            l.intensity = intensity;
            l.color = color;
            l.shadows = LightShadows.None;
            return l;
        }

        private static ReflectionProbe CreateBoxReflectionProbe(Transform parent, string name, Vector3 centerPos, Vector3 boxSize, float blendDistance)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centerPos;

            ReflectionProbe probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.boxProjection = true;
            probe.center = Vector3.zero;
            probe.size = boxSize;
            probe.blendDistance = blendDistance;
            probe.resolution = 128;
            probe.intensity = 0.85f;
            probe.nearClipPlane = 0.10f;
            probe.farClipPlane = 14f;
            probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = new Color(0.014f, 0.014f, 0.014f, 1f);
            probe.cullingMask = ~0;
            return probe;
        }

        private static void ConfigureLightURPShadowTier(Light l, int shadowTier)
        {
            if (l == null) return;
            UniversalAdditionalLightData lightData = GetOrAdd<UniversalAdditionalLightData>(l.gameObject);
            SerializedObject lso = new SerializedObject(lightData);
            SerializedProperty tierProp = lso.FindProperty("m_AdditionalLightsShadowResolutionTier");
            if (tierProp != null)
            {
                tierProp.intValue = Mathf.Clamp(shadowTier, 0, 2);
                lso.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(lightData);
        }

        private static void EnsureShadowContainmentAndLampShades(Transform casaBase, Transform quartoFilhasRoot)
        {
            ApplyShadowModesRecursive(casaBase);
            ApplyShadowModesRecursive(quartoFilhasRoot);
        }

        private static void ApplyShadowModesRecursive(Transform root)
        {
            if (root == null) return;
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer mr = renderers[i];
                if (mr == null) continue;
                string n = mr.gameObject.name;

                if (n == "Plafon_Teto" ||
                    n == "Plafon_Teto_Apagado" ||
                    n == "Cupula_Fosca" ||
                    n == "Cupula_Fosca_Apagada" ||
                    n == "Bulbo_Interno" ||
                    n == "Canopla_Teto" ||
                    n == "Anel_Latao_Plafon" ||
                    n == "Pinaculo_Plafon" ||
                    n == "Pinaculo_Base_Plafon" ||
                    n.StartsWith("Presilha_Plafon_") ||
                    n == "Abajur_Margaret_Base" ||
                    n == "Abajur_Margaret_Haste" ||
                    n == "Abajur_Margaret_Cupula" ||
                    n == "Abajur_Margaret_Cupula_Apagada" ||
                    n == "Bulbo_Abajur_Aceso" ||
                    n == "Bulbo_Abajur_Apagado" ||
                    n == "Base_Coluna" ||
                    n == "Haste_Coluna" ||
                    n == "Cordao_Interruptor_Luminaria" ||
                    n == "Cupula_Tecido" ||
                    n == "Cupula_Tecido_Apagada" ||
                    n == "Bulbo_Coluna_Aceso" ||
                    n == "Bulbo_Coluna_Apagado" ||
                    n == "Luminaria_Base" ||
                    n == "Luminaria_Haste" ||
                    n == "Luminaria_Cupula" ||
                    n == "Luminaria_Cupula_Apagada" ||
                    n == "Luminaria_Vidro_Verde" ||
                    n == "Luminaria_Vidro_Apagado" ||
                    n == "Bulbo_Luminaria_Escritorio" ||
                    n == "Arandela_Acima_Espelho" ||
                    n == "Arandela_Acima_Espelho_Apagada" ||
                    n == "Tubo_Arandela_Aceso" ||
                    n == "Tubo_Arandela_Apagado" ||
                    n == "Suporte_Arandela" ||
                    n == "Visor_Digital" ||
                    n == "Tela_TV")
                {
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    EditorUtility.SetDirty(mr);
                    continue;
                }

                if (n.StartsWith("Parede_") ||
                    n.StartsWith("Batente_Janela_") ||
                    n.StartsWith("ShadowBox_") ||
                    n == "Teto_Residencia" ||
                    n == "Base1" ||
                    n.StartsWith("Piso_") ||
                    n == "Folha_Porta" ||
                    n == "Porta_Rua_Folha" ||
                    n == "Porta_Entrada_Frente")
                {
                    mr.shadowCastingMode = ShadowCastingMode.TwoSided;
                    EditorUtility.SetDirty(mr);
                }
            }
        }

        private static void ApplyEditModeDay1Preview(Light[] roomLights, Light deskLight, Light[] secondaryLights, Light[] fillLights)
        {
            if (roomLights != null)
            {
                for (int i = 0; i < roomLights.Length; i++)
                {
                    Light l = roomLights[i];
                    if (l == null) continue;
                    string id = (l.transform.parent != null ? l.transform.parent.name : l.name).ToLowerInvariant();
                    if (id.Contains("quartofilhas"))
                    {
                        l.enabled = false;
                        l.intensity = 0f;
                        DayManager.SyncLampFixtureVisual(l, false, Color.black, 0f);
                    }
                    else if (id.Contains("corredor_leste"))
                    {
                        l.enabled = true;
                        l.color = new Color(0.94f, 0.76f, 0.54f);
                        l.intensity = 0.70f;
                        DayManager.SyncLampFixtureVisual(l, true, l.color, 0.55f);
                    }
                    else if (id.Contains("banheiro"))
                    {
                        l.enabled = true;
                        l.color = new Color(0.91f, 0.94f, 0.96f);
                        l.intensity = 1.42f;
                        DayManager.SyncLampFixtureVisual(l, true, l.color, 0.84f);
                    }
                    else if (id.Contains("cozinha"))
                    {
                        l.enabled = true;
                        l.color = new Color(0.93f, 0.95f, 0.93f);
                        l.intensity = 1.54f;
                        DayManager.SyncLampFixtureVisual(l, true, l.color, 0.88f);
                    }
                    else if (id.Contains("escritorio"))
                    {
                        l.enabled = true;
                        l.color = new Color(0.93f, 0.94f, 0.93f);
                        l.intensity = 1.38f;
                        DayManager.SyncLampFixtureVisual(l, true, l.color, 0.78f);
                    }
                    else
                    {
                        l.enabled = true;
                        l.color = new Color(1.00f, 0.85f, 0.64f);
                        l.intensity = 1.28f;
                        DayManager.SyncLampFixtureVisual(l, true, l.color, 0.78f);
                    }
                    EditorUtility.SetDirty(l);
                }
            }

            if (deskLight != null)
            {
                deskLight.enabled = true;
                deskLight.color = new Color(1.00f, 0.89f, 0.70f);
                deskLight.intensity = 0.95f;
                DayManager.SyncBankerLampVisual(deskLight, true, 0.68f);
                EditorUtility.SetDirty(deskLight);
            }

            if (secondaryLights != null)
            {
                for (int i = 0; i < secondaryLights.Length; i++)
                {
                    Light sec = secondaryLights[i];
                    if (sec == null) continue;
                    bool isWindowSlit = sec.name.ToLowerInvariant().Contains("fresta_janela_filhas");
                    sec.enabled = !isWindowSlit;
                    DayManager.SyncSecondaryFixtureVisual(sec, !isWindowSlit, sec.color, isWindowSlit ? 0f : 0.85f);
                    EditorUtility.SetDirty(sec);
                }
            }

            if (fillLights != null)
            {
                for (int i = 0; i < fillLights.Length; i++)
                {
                    if (fillLights[i] == null) continue;
                    fillLights[i].enabled = true;
                    EditorUtility.SetDirty(fillLights[i]);
                }
            }
        }

        private static void SetupDay1Props(Transform propsDia1, GreyboxPalette pal, HouseContainers containers)
        {
            InspectableObject sink = MakeSpot(propsDia1, "Pia_Teste", new Vector3(-4.02f, 0.96f, 5.69f), new Vector3(0.88f, 0.22f, 0.52f));
            sink.ConfigureTask(
                id: "lavar_rosto",
                prompt: "Lavar o rosto",
                subtitle: "Água gelada. Essa dor atrás do olho não sai por nada.",
                repeatSubtitle: "Mais água não vai resolver dormir duas horas por noite.",
                linear: true,
                hudLabel: "Lavar o rosto na pia do banheiro",
                sound: FoleySoundId.WaterSplash
            );
            sink.ConfigureLinkedFaucet(containers.bathroomFaucet);
            EditorUtility.SetDirty(sink);

            GameObject sec = BuildAnsweringMachine(propsDia1, "Secretaria_Eletronica_D1", new Vector3(-3.06f, 0.765f, 1.825f), pal, blinkingRedLed: true, hasTapeInside: true);
            InspectableObject secInsp = GetOrAdd<InspectableObject>(sec);
            secInsp.ConfigureTask(
                id: "checar_secretaria",
                prompt: "Ouvir a secretária eletrônica",
                subtitle: "[BIP] \"Cian? Tavares aqui. Liguei no seu ramal e nada... O lote de outubro voltou inteiro errado. Segunda, oito em ponto, resolve isso na minha sala.\" Cortei antes do segundo recado.",
                repeatSubtitle: "Não vou ficar ouvindo cobrança num domingo à noite.",
                linear: true,
                hudLabel: "Ouvir a secretária eletrônica no corredor",
                sound: FoleySoundId.AnsweringMachinePlay
            );
            Renderer secLedD1 = sec.transform.Find("LED_Mensagens")?.GetComponent<Renderer>();
            secInsp.ConfigureAlertIndicator(secLedD1, emitSound: false, intervalSeconds: 2.35f);
            secInsp.ConfigureAnsweringReels(
                sec.transform.Find("Microfita_Inserida/Rolo_Esq"),
                sec.transform.Find("Microfita_Inserida/Rolo_Dir"));
            EditorUtility.SetDirty(secInsp);

            InspectableObject keyBowl = MakeSpot(propsDia1, "Pratinho_Chaves_Copa", new Vector3(-3.22f, 0.8490f, -0.78f), new Vector3(0.34f, 0.24f, 0.36f), new Vector3(0f, 0.10f, 0f));
            GameObject keyMesh = BuildDetailedKeyWithTag(keyBowl.transform, "Chave_Escritorio_Mesh", new Vector3(0.038f, 0f, 0.028f), 25f, pal.metalBrass, pal.plasticSlate, pal);
            SnapToSupportWorldY(keyMesh, 0.8490f);
            keyBowl.ConfigurePickup(
                id: "pegar_chave_escritorio",
                itemToGrant: "chave_escritorio",
                prompt: "Pegar a chave no pratinho",
                subtitle: "Tá aqui. A da fita azul é do escritório.",
                meshToHide: keyMesh,
                sound: FoleySoundId.KeyPickup,
                linear: false,
                hudLabel: "Achar a chave do escritório na cozinha"
            );

            InspectableObject battPickup = MakeSpot(propsDia1, "Pilhas_Controle_D1", new Vector3(-4.020f, 0.7180f, -5.42f), new Vector3(0.28f, 0.11f, 0.24f), new Vector3(0f, 0.04f, 0f));
            GameObject battVisuals = CreateContainer(battPickup.transform, "Pilhas_AA_Visual");
            BuildDetailedAABattery(battVisuals.transform, "Pilha_AA_1", new Vector3(-0.030f, 0f, 0f), 12f, pal);
            BuildDetailedAABattery(battVisuals.transform, "Pilha_AA_2", new Vector3(0.030f, 0f, 0.012f), -18f, pal);
            SnapToSupportWorldY(battVisuals, 0.7180f);
            battPickup.ConfigurePickup(
                id: "pilhas_controle_task",
                itemToGrant: "pilhas_controle",
                prompt: "Pegar as pilhas",
                subtitle: "Duas pilhas. Serve pro controle.",
                meshToHide: battVisuals,
                sound: FoleySoundId.BatterySnap,
                linear: false,
                hudLabel: "Pegar pilhas na cozinha"
            );
            containers.kitchenUtilityDrawer?.RegisterInteriorObject(battPickup, followMovingPartPosition: true);

            InspectableObject breakerD1 = MakeSpot(propsDia1, "Quadro_Forca_Dia1", new Vector3(-6.475f, 1.84f, -1.05f), new Vector3(0.06f, 0.44f, 0.30f));
            breakerD1.ConfigureTask(
                id: "quadro_forca_opc",
                prompt: "Olhar os disjuntores",
                subtitle: "Corredor, sala, cozinha...",
                repeatSubtitle: "Tudo armado.",
                linear: false,
                sound: FoleySoundId.FuseClackSpark
            );
            containers.kitchenBreakerBox?.RegisterInteriorObject(breakerD1);

            GameObject papers = CreateContainer(propsDia1, "Papeis_Teste");
            papers.transform.localPosition = new Vector3(5.68f, 0.783f, -2.51f);
            papers.transform.localRotation = Quaternion.Euler(0f, -8f, 0f);
            GameObject folderBase = CreateDetailBox(papers.transform, "Pasta_Base_Kraft", new Vector3(0f, 0.008f, 0f), new Vector3(0.34f, 0.016f, 0.44f), pal.folderKraft);
            SnapToSupportWorldY(folderBase, 0.783f);
            GameObject folderTop = CreateDetailBox(papers.transform, "Pasta_Superior_Azul", new Vector3(0.012f, 0.0235f, -0.01f), new Vector3(0.32f, 0.015f, 0.42f), pal.plasticSlate, new Vector3(0f, 9f, 0f));
            SnapToSupportTop(folderTop, folderBase);
            GameObject memoSheet = BuildDetailedPaperSheet(papers.transform, "Memorando_Solto", new Vector3(0.00f, 0.031f, 0.01f), 14f, new Vector2(0.35f, 0.26f), pal, printedFaceMat: pal.details.memoTavares, printAngleY: 90f);
            SnapToSupportTop(memoSheet, folderTop);
            GameObject memoPen = BuildDetailedBallpointPen(papers.transform, "Caneta_Vermelha", new Vector3(-0.08f, 0.036f, 0.05f), 28f, pal.calendarRed, pal);
            SnapToSupportTop(memoPen, memoSheet);

            BoxCollider papersCol = papers.AddComponent<BoxCollider>();
            papersCol.center = new Vector3(0f, 0.06f, 0f);
            papersCol.size = new Vector3(0.54f, 0.20f, 0.60f);

            InspectableObject papersTask = papers.AddComponent<InspectableObject>();
            papersTask.ConfigureDocument(
                id: "olhar_papeis",
                prompt: "Ler os papéis na mesa",
                docTitle: "COMUNICAÇÃO INTERNA Nº 048/95",
                docBody:
                    "PARA: Cian Nabs - Analista Contábil Pleno\n" +
                    "DE: A. Tavares - Gerência Operacional\n\n" +
                    "Cian, os lotes 03, 07 e 11 da conciliação de outubro voltaram da\n" +
                    "diretoria com divergência de saldo de novo. Não sei o que está\n" +
                    "acontecendo com você nas últimas semanas.\n\n" +
                    "Segunda-feira, às 08h00 em ponto, quero as três pastas refeitas\n" +
                    "na minha mesa. Outro atraso desses vai pro Departamento Pessoal.",
                afterReadThought: "Terceira vez que devolvem esse fechamento...",
                repeatThought: "Os números estão todos embaralhados.",
                linear: true,
                hudLabel: "Conferir os papéis na mesa",
                style: DocumentStyle.CorporateMemo
            );

            InspectableObject calTask = MakeSpot(propsDia1, "Calendario_Dia1_Interagir", new Vector3(6.27f, 1.56f, -1.05f), new Vector3(0.12f, 0.48f, 0.40f));
            calTask.ConfigureTask(
                id: "checar_calendario",
                prompt: "Olhar o calendário",
                subtitle: "Domingo, 29 de outubro de 1995. Mais uma semana inteira riscada.",
                repeatSubtitle: "29 de outubro. Amanhã cedo começa tudo de novo.",
                linear: true,
                hudLabel: "Confirmar a data no calendário",
                sound: FoleySoundId.PaperRustle
            );

            InspectableObject mirror = MakeSpot(propsDia1, "Espelho_Dia1", new Vector3(-4.02f, 1.62f, 5.98f), new Vector3(0.86f, 0.88f, 0.10f));
            mirror.ConfigureTask(
                id: "espelho_dia1_opc",
                prompt: "Olhar no espelho",
                subtitle: "Olheira funda, barba por fazer. Pareço mais velho do que sou.",
                repeatSubtitle: "Melhor sair da frente do vidro.",
                linear: false,
                sound: FoleySoundId.GlassInspect
            );

            // Fotografias da família na parede Oeste do braço Norte do corredor L (X = +1.04f, voltadas +X)
            GameObject frames = CreateContainer(propsDia1, "Quadros_Familia_D1");
            CreateFramedPicture(frames.transform, "Quadro_Familia_1_D1", new Vector3(1.057f, 1.56f, 2.80f), Quaternion.Euler(0f, 90f, 0f), pal.furnitureWood, pal.details.frameFamily1);
            CreateFramedPicture(frames.transform, "Quadro_Familia_2_D1", new Vector3(1.057f, 1.56f, 3.80f), Quaternion.Euler(0f, 90f, 0f), pal.furnitureWood, pal.details.frameFamily2);
            InspectableObject framesInsp = MakeSpot(propsDia1, "Quadros_Familia_D1_Interagir", new Vector3(1.09f, 1.56f, 3.30f), new Vector3(0.14f, 0.74f, 1.65f));
            framesInsp.ConfigureTask(
                id: "quadros_familia_d1_opc",
                prompt: "Olhar as fotografias do corredor",
                subtitle: "Eu bati essa foto num domingo qualquer no quintal. Nunca paro aqui pra olhar.",
                repeatSubtitle: "Passo reto por esse corredor todo dia.",
                linear: false,
                sound: FoleySoundId.WoodInspect
            );

            // Retrato de Margaret no fundo Leste do corredor principal (X = +2.52f, voltado -X)
            CreateFramedPicture(propsDia1, "Quadro_Margaret_D1", new Vector3(2.503f, 1.52f, 1.28f), Quaternion.Euler(0f, -90f, 0f), pal.furnitureWood, pal.details.frameMargaretNormal);
            InspectableObject margaretPortrait = MakeSpot(propsDia1, "Quadro_Margaret_D1_Interagir", new Vector3(2.47f, 1.52f, 1.28f), new Vector3(0.14f, 0.78f, 0.64f));
            margaretPortrait.ConfigureTask(
                id: "retrato_margaret_d1_opc",
                prompt: "Olhar o retrato de Margaret",
                subtitle: "A parede em volta encardiu toda, menos atrás da moldura. Faz tempo que ninguém tira isso pra limpar.",
                repeatSubtitle: "O vidro tá opaco de poeira.",
                linear: false,
                sound: FoleySoundId.WoodInspect
            );

            InspectableObject coatD1 = MakeSpot(propsDia1, "Casaco_Margaret_GuardaRoupa_D1", new Vector3(-3.07f, 1.13f, 4.54f), new Vector3(0.36f, 0.96f, 0.30f));
            coatD1.ConfigureTask(
                id: "guarda_roupa_d1_opc",
                prompt: "Olhar o casaco cinza",
                subtitle: "As roupas dela continuam penduradas no mesmo canto. Não mexo aí.",
                repeatSubtitle: "Cheiro forte.",
                linear: false,
                sound: FoleySoundId.ClothInspect
            );
            containers.bedroomWardrobe?.RegisterInteriorObject(coatD1);
        }

        private static void SetupDay2Props(Transform propsDia2, GreyboxPalette pal, HouseContainers containers)
        {
            GameObject d2Root = CreateContainer(propsDia2, "Eventos_Dia2_Greybox");

            // Frascos vazios no lado central/leste da Mesa de Centro (Y = 0.434f) livres do cinzeiro (-0.44, -3.66) e do copo (-1.11, -3.87)
            GameObject bottles = CreateContainer(d2Root.transform, "Frascos_MesaCentro");
            bottles.transform.localPosition = new Vector3(-0.75f, 0.434f, -3.77f);
            SnapToSupportWorldY(CreateDetailedPillBottle(bottles.transform, "Frasco_1", new Vector3(-0.12f, 0f, 0.14f), 20f, pal), 0.434f);
            SnapToSupportWorldY(CreateDetailedPillBottle(bottles.transform, "Frasco_2", new Vector3(-0.23f, 0f, 0.08f), -35f, pal), 0.434f);
            SnapToSupportWorldY(CreateSpilledPillBottle(bottles.transform, "Frasco_Caido", new Vector3(0.06f, 0f, -0.14f), 205f, pal), 0.434f);
            SnapToSupportWorldY(BuildDetailedPaperSheet(bottles.transform, "Bula_Dobrada", new Vector3(-0.15f, 0f, -0.11f), -16f, new Vector2(0.16f, 0.13f), pal), 0.434f);
            SnapToSupportWorldY(BuildBlisterPack(bottles.transform, "Cartela_Sedativo_Vazia", new Vector3(0.09f, 0f, 0.12f), 32f, pal, poppedCount: 6, printAngleY: 180f), 0.434f);

            BoxCollider bottlesCol = bottles.AddComponent<BoxCollider>();
            bottlesCol.center = new Vector3(0f, 0.16f, 0f);
            bottlesCol.size = new Vector3(0.72f, 0.34f, 0.58f);

            InspectableObject bottlesTask = bottles.AddComponent<InspectableObject>();
            bottlesTask.ConfigureDocument(
                id: "frascos_calmantes_dia2",
                prompt: "Olhar os frascos e a receita",
                docTitle: "NOTIFICAÇÃO DE RECEITA B1 - CONTROLE ESPECIAL",
                docBody:
                    "PACIENTE: Cian Nabs\n" +
                    "MEDICAMENTO: Clonazepam 2 mg - 02 caixas (60 comprimidos)\n\n" +
                    "POSOLOGIA: Tomar 01 comprimido antes de deitar.\n" +
                    "NÃO REPETIR SEM NOVA AVALIAÇÃO MÉDICA.\n\n" +
                    "DISPENSADO EM: 24/10/1994  |  FARMÁCIA CENTRAL - SANTOS/SP",
                afterReadThought: "A receita venceu faz quase um ano.",
                repeatThought: "Os dois frascos tão vazios.",
                linear: true,
                hudLabel: "Ver os frascos na mesa da sala",
                style: DocumentStyle.MedicalPrescription,
                extraSound: FoleySoundId.PillBottleRattle
            );

            SnapToSupportWorldY(CreateDetailedPillBottle(d2Root.transform, "Frasco_MesaTrab_1", new Vector3(5.40f, 0.780f, -2.92f), 40f, pal), 0.780f);
            SnapToSupportWorldY(CreateSpilledPillBottle(d2Root.transform, "Frasco_MesaTrab_2", new Vector3(5.54f, 0.780f, -2.92f), 215f, pal), 0.780f);
            SnapToSupportWorldY(BuildBlisterPack(d2Root.transform, "Cartela_MesaTrab_D2", new Vector3(5.38f, 0.780f, -2.72f), -24f, pal, poppedCount: 5, printAngleY: 90f), 0.780f);

            InspectableObject tape = MakeSpot(d2Root.transform, "Fita_Cassete_D2", new Vector3(-3.64f, 0.890f, 5.66f), new Vector3(0.32f, 0.22f, 0.28f), new Vector3(0f, 0.04f, 0f));
            GameObject tapeMesh = BuildDetailedCassetteTape(tape.transform, "Fita_Cassete_Mesh", Vector3.zero, 18f, pal);
            SnapToSupportWorldY(tapeMesh, 0.890f);
            tape.ConfigurePickup(
                id: "pegar_fita_banheiro_dia2",
                itemToGrant: "fita_secretaria",
                prompt: "Pegar a fita na borda da pia",
                subtitle: "Tava jogada aqui na pia molhada. Devo ter arrancado da secretária ontem de noite sem perceber.",
                meshToHide: tapeMesh,
                sound: FoleySoundId.KeyPickup,
                linear: false,
                hudLabel: "Achar a fita da secretária"
            );
            // Exige ficar bem de frente para a pia/espelho para pegar a fita, garantindo o reflexo da aparição 3D na porta atrás de Cian
            tape.ConfigureProximityRequirement(maxDistanceMeters: 1.48f, requireFacing: true, facingWorldDir: Vector3.forward, minFacingDot: 0.48f);

            // Aparição 3D real parada na porta do banheiro atrás de Cian (visível no reflexo planar real do espelho até ele virar)
            GameObject mirrorApparition = BuildSculptedPS1Humanoid(
                d2Root.transform, "Aparicao_Porta_Banheiro_D2", SilhouetteArchetype.Margaret,
                pal.margaretNightgown, pal.margaretPallor, pal);
            mirrorApparition.transform.localPosition = new Vector3(-4.82f, 0f, 2.38f);
            mirrorApparition.transform.localRotation = Quaternion.identity;
            GetOrAdd<HumanoidSpasmAnimator>(mirrorApparition);
            mirrorApparition.SetActive(false);
            if (containers.bathroomMirrorHaunt != null)
            {
                containers.bathroomMirrorHaunt.LinkDoorwayApparition(mirrorApparition);
                EditorUtility.SetDirty(containers.bathroomMirrorHaunt);
            }

            GameObject sec = BuildAnsweringMachine(d2Root.transform, "Secretaria_Eletronica_D2", new Vector3(-3.06f, 0.765f, 1.825f), pal, blinkingRedLed: true, hasTapeInside: false);
            InspectableObject secInsp = GetOrAdd<InspectableObject>(sec);
            GameObject tapeInsertD2 = sec.transform.Find("Microfita_Inserida")?.gameObject;
            secInsp.ConfigureItemReceiver(
                id: "secretaria_fita_dia2",
                neededItem: "fita_secretaria",
                lockedPromptText: "Olhar a secretária apitando",
                unlockedPromptText: "Colocar a fita e ouvir a gravação",
                missingThought: "Tá sem fita, só apitando. Devo ter deixado em algum canto ontem à noite.",
                completedThought: "[BIP] Sexta-feira, 23h58. Só chiado e alguém respirando perto do bocal... Tem um choro abafado de criança lá no fundo antes da linha cair.",
                repeatThought: "Aquele choro baixo no fundo da gravação... não era recado de trabalho.",
                lockedSfx: FoleySoundId.AnsweringMachineEmpty,
                successSfx: FoleySoundId.CassetteTapeHauntD2,
                revealObj: tapeInsertD2,
                alertBeep: true,
                linear: true,
                hudLabel: "Checar a secretária apitando no corredor"
            );
            Renderer secLedD2 = sec.transform.Find("LED_Mensagens")?.GetComponent<Renderer>();
            secInsp.ConfigureAlertIndicator(secLedD2, emitSound: true, intervalSeconds: 2.25f);
            secInsp.ConfigureAnsweringReels(
                sec.transform.Find("Microfita_Inserida/Rolo_Esq"),
                sec.transform.Find("Microfita_Inserida/Rolo_Dir"));
            EditorUtility.SetDirty(secInsp);

            InspectableObject mirror = MakeSpot(d2Root.transform, "Espelho_Dia2", new Vector3(-4.02f, 1.62f, 5.98f), new Vector3(0.86f, 0.88f, 0.10f));
            mirror.ConfigureTask(
                id: "espelho_dia2_opc",
                prompt: "Olhar no espelho",
                subtitle: "Pálpebra caída, boca pastosa de remédio. O reflexo demorou meio segundo pra acompanhar.",
                repeatSubtitle: "Não quero ficar olhando pra isso.",
                linear: false,
                sound: FoleySoundId.GlassInspect
            );
            mirror.SetDissociationTruthFlash("A PORTA DO FUNDO");

            InspectableObject cal = MakeSpot(d2Root.transform, "Calendario_Dia2_Interagir", new Vector3(6.27f, 1.56f, -1.05f), new Vector3(0.12f, 0.48f, 0.40f));
            cal.ConfigureTask(
                id: "calendario_d2_opc",
                prompt: "Olhar o calendário",
                subtitle: "Outubro de 1995. Como já é segunda se eu não saí do sofá?",
                repeatSubtitle: "Outubro de 1995.",
                linear: false,
                sound: FoleySoundId.PaperRustle
            );

            InspectableObject emptyFlashD2 = MakeSpot(d2Root.transform, "Lanterna_Vazia_Gaveta_D2", new Vector3(-4.300f, 0.735f, -5.42f), new Vector3(0.26f, 0.10f, 0.24f));
            emptyFlashD2.ConfigureTask(
                id: "lanterna_vazia_d2_opc",
                prompt: "Olhar a lanterna",
                subtitle: "Sem pilha nenhuma. Tirei ontem de noite.",
                repeatSubtitle: "Vazia.",
                linear: false,
                sound: FoleySoundId.MetalInspect
            );
            containers.kitchenUtilityDrawer?.RegisterInteriorObject(emptyFlashD2, followMovingPartPosition: true);

            InspectableObject breakerD2 = MakeSpot(d2Root.transform, "Quadro_Forca_Dia2", new Vector3(-6.475f, 1.84f, -1.05f), new Vector3(0.06f, 0.44f, 0.30f));
            breakerD2.ConfigureTask(
                id: "quadro_forca_d2_opc",
                prompt: "Olhar os disjuntores",
                subtitle: "Tem um chiado elétrico dentro dessa caixa.",
                repeatSubtitle: "Melhor não mexer nisso agora.",
                linear: false,
                sound: FoleySoundId.FuseClackSpark
            );
            containers.kitchenBreakerBox?.RegisterInteriorObject(breakerD2);

            InspectableObject coatD2 = MakeSpot(d2Root.transform, "Casaco_Margaret_GuardaRoupa_D2", new Vector3(-3.07f, 1.13f, 4.54f), new Vector3(0.36f, 0.96f, 0.30f));
            coatD2.ConfigureTask(
                id: "casaco_margaret_d2_opc",
                prompt: "Olhar o casaco cinza",
                subtitle: "A manga do casaco encostou no meu braço...",
                repeatSubtitle: "Deixa a porta fechada.",
                linear: false,
                sound: FoleySoundId.ClothInspect
            );
            containers.bedroomWardrobe?.RegisterInteriorObject(coatD2);

            // Alucinação 1: vulto na curva cega em L (+1.90, +1.38 -> +1.26, +2.88 dobrando atrás da quina de Parede_Quarto_Corredor)
            GameObject shadowRig = CreateContainer(d2Root.transform, "Alucinacao_1_Vulto");
            GameObject shadowFigure = BuildSculptedPS1Humanoid(shadowRig.transform, "Vulto_Sombra_Negra", SilhouetteArchetype.ShadowDash, pal.shadowBlack, pal.shadowBlack, pal);
            GetOrAdd<HumanoidSpasmAnimator>(shadowFigure);
            shadowFigure.SetActive(false);

            Transform shStart = new GameObject("Vulto_StartPoint").transform;
            shStart.SetParent(shadowRig.transform, false);
            shStart.localPosition = new Vector3(1.90f, 0f, 1.38f);
            shStart.localRotation = Quaternion.identity;

            Transform shEnd = new GameObject("Vulto_EndPoint").transform;
            shEnd.SetParent(shadowRig.transform, false);
            shEnd.localPosition = new Vector3(1.26f, 0f, 2.88f);
            shEnd.localRotation = Quaternion.identity;

            GameObject trig1 = CreateTriggerZone(d2Root.transform, "Trigger_1_VultoCorredor", new Vector3(0.05f, 1.15f, 1.28f), new Vector3(1.45f, 2.30f, 1.48f));
            HallucinationTrigger ht1 = GetOrAdd<HallucinationTrigger>(trig1);
            ht1.ConfigureShadowPass(
                id: "vulto_corredor",
                subtitle: "Passou alguém dobrando pro fundo do corredor... Não. Deve ser o efeito do remédio na vista.",
                shadow: shadowFigure,
                startPt: shStart,
                endPt: shEnd,
                speed: 5.8f,
                armedByDefault: false,
                lookPoint: new Vector3(1.86f, 1.38f, 1.85f),
                enableCameraLook: false
            );
            EditorUtility.SetDirty(ht1);

            GameObject framesRig = CreateContainer(d2Root.transform, "Alucinacao_2_Quadros");
            GameObject normal1 = CreateFramedPicture(framesRig.transform, "Quadro_Normal_1", new Vector3(1.057f, 1.56f, 2.80f), Quaternion.Euler(0f, 90f, 0f), pal.furnitureWood, pal.details.frameFamily1);
            GameObject normal2 = CreateFramedPicture(framesRig.transform, "Quadro_Normal_2", new Vector3(1.057f, 1.56f, 3.80f), Quaternion.Euler(0f, 90f, 0f), pal.furnitureWood, pal.details.frameFamily2);
            CreateDetailCylinder(framesRig.transform, "Prego_Parede_Familia_1_D2", new Vector3(1.048f, 1.882f, 2.80f), new Vector3(0.010f, 0.012f, 0.010f), pal.metalDark, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(framesRig.transform, "Prego_Parede_Familia_2_D2", new Vector3(1.048f, 1.882f, 3.80f), new Vector3(0.010f, 0.012f, 0.010f), pal.metalDark, new Vector3(0f, 0f, 90f));
            GameObject altered1 = CreateFramedPicture(framesRig.transform, "Quadro_Distorcido_1", new Vector3(1.057f, 1.54f, 2.80f), Quaternion.Euler(0f, 90f, -14f), pal.metalDark, pal.details.frameCorrupted1, includeWallNail: false, glassCracked: true);
            GameObject altered2 = CreateFramedPicture(framesRig.transform, "Quadro_Distorcido_2", new Vector3(1.057f, 1.52f, 3.80f), Quaternion.Euler(0f, 90f, 17f), pal.metalDark, pal.details.frameCorrupted2, includeWallNail: false, glassCracked: true);
            altered1.SetActive(false);
            altered2.SetActive(false);

            CreateFramedPicture(d2Root.transform, "Quadro_Margaret_D2", new Vector3(2.503f, 1.52f, 1.28f), Quaternion.Euler(0f, -90f, 0f), pal.furnitureWood, pal.details.frameMargaretNormal);

            GameObject trig2 = CreateTriggerZone(d2Root.transform, "Trigger_2_QuadrosFamilia", new Vector3(1.78f, 1.15f, 3.125f), new Vector3(1.48f, 2.30f, 1.75f));
            HallucinationTrigger ht2 = GetOrAdd<HallucinationTrigger>(trig2);
            ht2.ConfigureSwapObjects(
                id: "quadros_familia_alterados",
                subtitle: "Os quadros racharam...",
                normals: new[] { normal1, normal2 },
                altereds: new[] { altered1, altered2 },
                armedByDefault: false,
                lookPoint: new Vector3(1.06f, 1.54f, 3.30f),
                enableCameraLook: false
            );
            EditorUtility.SetDirty(ht2);

            GameObject trig3 = CreateTriggerZone(d2Root.transform, "Trigger_3_SussurrosPorta", new Vector3(1.78f, 1.15f, 4.75f), new Vector3(1.48f, 2.30f, 1.40f));
            HallucinationTrigger ht3 = GetOrAdd<HallucinationTrigger>(trig3);
            ht3.ConfigureWhispers(
                id: "pre_sussurros_corredor",
                subtitle: "",
                armedByDefault: false,
                completeObjective: false,
                lookPoint: Vector3.zero
            );
            EditorUtility.SetDirty(ht3);

            // Observador silencioso espiando da borda oeste do Arco da Sala e recuando para trás da parede sólida Parede_CorredorSul
            GameObject watcherD2Rig = CreateContainer(d2Root.transform, "Watcher_Dia2_Corredor");
            Transform peekWatcherD2 = new GameObject("Peek_Watcher_D2").transform;
            peekWatcherD2.SetParent(watcherD2Rig.transform, false);
            peekWatcherD2.localPosition = new Vector3(-1.26f, 0f, -0.10f);
            peekWatcherD2.localRotation = Quaternion.Euler(0f, -24f, 0f);

            Transform hideWatcherD2 = new GameObject("Hide_Watcher_D2").transform;
            hideWatcherD2.SetParent(watcherD2Rig.transform, false);
            hideWatcherD2.localPosition = new Vector3(-2.35f, 0f, -0.05f);
            hideWatcherD2.localRotation = Quaternion.Euler(0f, -24f, 0f);

            GameObject watcherD2Fig = BuildSculptedPS1Humanoid(watcherD2Rig.transform, "Watcher_Figura_D2", SilhouetteArchetype.SlumpedCian, pal.cianShirtWrinkled, pal.cianSkinWeary, pal);
            GetOrAdd<HumanoidSpasmAnimator>(watcherD2Fig);
            WatcherHallucination watcherD2 = GetOrAdd<WatcherHallucination>(watcherD2Rig);
            watcherD2.ConfigureWatcher(watcherD2Fig, peekWatcherD2, hideWatcherD2, blackoutRetreat: false, targetEyeHeight: 1.46f);
            EditorUtility.SetDirty(watcherD2);
        }

        private static GameObject SetupDay3Props(Transform propsDia3, GreyboxPalette pal, HouseContainers containers)
        {
            GameObject d3Root = CreateContainer(propsDia3, "Eventos_Dia3_Greybox");

            // Cadeira tombada na passagem da Sala (rotZ=88.4° apoiada em 3 pontos estruturais e ancorada no piso Y=0.000f, com passagem livre em Spawn_Dia3)
            GameObject fallenChair = CreateContainer(d3Root.transform, "Cadeira_Tombada_Caos");
            fallenChair.transform.localPosition = new Vector3(0.62f, 0.210f, -1.42f);
            fallenChair.transform.localRotation = Quaternion.Euler(0f, 38f, 0f);
            BuildDetailedDiningChair(fallenChair.transform, "Cadeira_Jantar_Caida", Vector3.zero, 0f, pal);
            fallenChair.transform.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, 88.4f);
            SnapToSupportWorldY(fallenChair, 0f);
            BoxCollider fallenCol = fallenChair.AddComponent<BoxCollider>();
            fallenCol.center = new Vector3(-0.44f, 0f, 0f);
            fallenCol.size = new Vector3(0.92f, 0.42f, 0.44f);

            // Lanterna obrigatória do Dia 3 dentro da primeira gaveta da cozinha (Gaveta_Utilitaria_Cozinha)
            InspectableObject flashlightPickupD3 = MakeSpot(d3Root.transform, "Lanterna_Dia3", new Vector3(-4.300f, 0.735f, -5.42f), new Vector3(0.28f, 0.12f, 0.26f));
            flashlightPickupD3.ConfigurePickup(
                id: "pegar_lanterna_dia3",
                itemToGrant: "lanterna_dia3",
                prompt: "Pegar a lanterna",
                subtitle: "Jurava que eu tinha tirado as pilhas dessa lanterna... Pelo menos tá acendendo.",
                meshToHide: null,
                sound: FoleySoundId.FlashlightClick,
                linear: true,
                hudLabel: "Pegar a lanterna na gaveta da cozinha"
            );
            containers.kitchenUtilityDrawer?.RegisterInteriorObject(flashlightPickupD3, followMovingPartPosition: true);

            // Âncora material do estimulante obrigatório do Dia 3 sobre a Mesa de Centro (Y = 0.434f), livre do controle (-1.23,-3.63), copo (-1.11,-3.87) e cinzeiro (-0.44,-3.66)
            GameObject stimRoot = CreateContainer(d3Root.transform, "Estimulante_Dia3_Mesa");
            stimRoot.transform.localPosition = new Vector3(-0.74f, 0.434f, -3.76f);

            GameObject stimBottleHost = CreateContainer(stimRoot.transform, "Frasco_Estimulante_Dia3");
            stimBottleHost.transform.localPosition = new Vector3(-0.10f, 0f, 0.06f);
            SnapToSupportWorldY(CreateSpilledStimulantBottle(stimBottleHost.transform, "Frasco_Manipulado_Caido", Vector3.zero, 208f, pal), 0.434f);
            BoxCollider stimBottleCol = stimBottleHost.AddComponent<BoxCollider>();
            stimBottleCol.center = new Vector3(0.01f, 0.045f, 0.01f);
            stimBottleCol.size = new Vector3(0.19f, 0.11f, 0.19f);
            InspectableObject stimBottleDoc = stimBottleHost.AddComponent<InspectableObject>();
            stimBottleDoc.ConfigureDocument(
                id: "estimulante_frasco_dia3_opc",
                prompt: "Olhar o frasco manipulado",
                docTitle: "FÓRMULA MANIPULADA — REGISTRO / FARMÁCIA CENTRAL",
                docBody:
                    "PACIENTE: Cian Nabs  |  PRONTUÁRIO / PEDIDO Nº 4182\n" +
                    "COMPOSIÇÃO POR CÁPSULA:\n" +
                    "Fenproporex 25 mg + Cafeína Anidra 200 mg (30 cáps.)\n\n" +
                    "INDICAÇÃO: 01 cápsula pela manhã.\n" +
                    "ADVERTÊNCIA: Não associar a outros estimulantes.\n" +
                    "Risco de taquicardia, tremor, agitação psicomotora,\n" +
                    "insônia severa e confusão sensorial.\n\n" +
                    "[O frasco está derrubado sobre a mesa, com o rótulo borrado de suor e a data ilegível.]",
                afterReadThought: "Fenproporex com cafeína. A data tá raspada... parece etiqueta de farmácia ou de clínica. Preciso engolir uma cápsula antes de apagar de novo.",
                repeatThought: "Fenproporex com cafeína. O corpo corre, mas a cabeça não desliga.",
                linear: false,
                hudLabel: "",
                style: DocumentStyle.MedicalPrescription,
                extraSound: FoleySoundId.PillBottleRattle
            );
            EditorUtility.SetDirty(stimBottleDoc);

            GameObject stimCapsHost = CreateContainer(stimRoot.transform, "Capsulas_Estimulante_Dia3");
            stimCapsHost.transform.localPosition = new Vector3(0.11f, 0f, -0.07f);
            SnapToSupportWorldY(BuildBlisterPack(stimCapsHost.transform, "Cartela_Rebite_D3", new Vector3(0.02f, 0f, -0.01f), 26f, pal, poppedCount: 6, printAngleY: 180f, customBackMat: pal.details.stimulantBlisterBack), 0.434f);
            GameObject looseCap1 = BuildLooseStimulantCapsule(stimCapsHost.transform, "Capsula_Solta_1", new Vector3(-0.042f, 0f, 0.024f), 42f, pal);
            SnapToSupportWorldY(looseCap1, 0.434f);
            GameObject looseCap2 = BuildLooseStimulantCapsule(stimCapsHost.transform, "Capsula_Solta_2", new Vector3(-0.020f, 0f, 0.048f), -34f, pal);
            SnapToSupportWorldY(looseCap2, 0.434f);
            BoxCollider stimCapsCol = stimCapsHost.AddComponent<BoxCollider>();
            stimCapsCol.center = new Vector3(0f, 0.032f, 0.012f);
            stimCapsCol.size = new Vector3(0.18f, 0.08f, 0.16f);
            InspectableObject stimCapsUse = stimCapsHost.AddComponent<InspectableObject>();
            stimCapsUse.ConfigureMultiUseConsumable(
                id: "engolir_capsula_dia3",
                prompt: "Engolir cápsula a seco",
                stepVisuals: new[] { looseCap1, looseCap2 },
                useSubtitles: new[]
                {
                    "Desceu rasgando sem água... O coração disparou e a vista firmou. Agora consigo checar o quadro de força.",
                    "A última cápsula. A boca tá seca feito poeira e o peito vai rebentar."
                },
                cooldownSubtitle: "O coração já tá disparado demais. Espera baixar antes de engolir outra.",
                sound: FoleySoundId.DryPillSwallow
            );
            EditorUtility.SetDirty(stimCapsUse);

            InspectableObject mirrorD3 = MakeSpot(d3Root.transform, "Espelho_Dia3", new Vector3(-4.02f, 1.62f, 5.98f), new Vector3(0.86f, 0.88f, 0.10f));
            mirrorD3.ConfigureTask(
                id: "espelho_dia3_opc",
                prompt: "Olhar no espelho",
                subtitle: "Pupila aberta demais na penumbra. A veia do pescoço bate no mesmo compasso da lâmpada... Parece outra pessoa encarando de volta.",
                repeatSubtitle: "O vidro escuro devolve mais sombra do que rosto.",
                linear: false,
                sound: FoleySoundId.GlassInspect
            );
            mirrorD3.SetDissociationTruthFlash("NÃO SOU EU");

            // Quadros tortos na parede Oeste do braço Norte do corredor L (X = +1.04f, voltados +X)
            CreateDetailCylinder(d3Root.transform, "Prego_Parede_Familia_1_D3", new Vector3(1.048f, 1.882f, 2.80f), new Vector3(0.010f, 0.012f, 0.010f), pal.metalDark, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(d3Root.transform, "Prego_Parede_Familia_2_D3", new Vector3(1.048f, 1.882f, 3.80f), new Vector3(0.010f, 0.012f, 0.010f), pal.metalDark, new Vector3(0f, 0f, 90f));
            CreateFramedPicture(d3Root.transform, "Quadro_Torto_Corredor_1", new Vector3(1.057f, 1.50f, 2.80f), Quaternion.Euler(0f, 90f, 24f), pal.metalDark, pal.details.frameCorrupted1, includeWallNail: false, glassCracked: true);
            CreateFramedPicture(d3Root.transform, "Quadro_Torto_Corredor_2", new Vector3(1.057f, 1.48f, 3.80f), Quaternion.Euler(0f, 90f, -19f), pal.metalDark, pal.details.frameCorrupted2, includeWallNail: false, glassCracked: true);

            // Caixa de ferramentas apoiada exatamente na Prateleira Alta da estante do banheiro (Y = 1.115f)
            GameObject toolboxRoot = CreateContainer(d3Root.transform, "Caixa_Ferramentas_Banheiro");
            toolboxRoot.transform.localPosition = new Vector3(-3.72f, 1.115f, 3.45f);
            GameObject tbBottom = CreateBox(toolboxRoot.transform, "Fundo_Caixa", new Vector3(0f, 0.010f, 0f), new Vector3(0.26f, 0.020f, 0.36f), pal.calendarRed, hasCollider: true);
            CreateBox(toolboxRoot.transform, "Parede_Frente", new Vector3(-0.12f, 0.070f, 0f), new Vector3(0.02f, 0.100f, 0.36f), pal.calendarRed, hasCollider: false);
            CreateBox(toolboxRoot.transform, "Parede_Tras", new Vector3(0.12f, 0.070f, 0f), new Vector3(0.02f, 0.100f, 0.36f), pal.calendarRed, hasCollider: false);
            CreateBox(toolboxRoot.transform, "Parede_Sul", new Vector3(0f, 0.070f, -0.17f), new Vector3(0.218f, 0.100f, 0.02f), pal.calendarRed, hasCollider: false);
            CreateBox(toolboxRoot.transform, "Parede_Norte", new Vector3(0f, 0.070f, 0.17f), new Vector3(0.218f, 0.100f, 0.02f), pal.calendarRed, hasCollider: false);
            SnapToSupportTop(BuildDetailedPliers(toolboxRoot.transform, "Alicate_Interno", new Vector3(-0.06f, 0.020f, -0.06f), 18f, pal), tbBottom);
            GameObject tapeRollGrp = CreateContainer(toolboxRoot.transform, "Fita_Isolante_Caixa_Grp");
            GameObject tapeRoll = CreateDetailCylinder(tapeRollGrp.transform, "Fita_Isolante_Caixa", new Vector3(0.06f, 0.029f, -0.10f), new Vector3(0.066f, 0.009f, 0.066f), pal.shadowBlack);
            SnapToSupportTop(tapeRoll, tbBottom);
            CreateDetailCylinder(tapeRollGrp.transform, "Miolo_Fita_Isolante_Caixa", new Vector3(0.06f, tapeRoll.transform.localPosition.y + 0.0025f, -0.10f), new Vector3(0.032f, 0.009f, 0.032f), pal.paperWhite);
            GameObject sdGrp = CreateContainer(toolboxRoot.transform, "Chave_Fenda_Caixa");
            sdGrp.transform.localPosition = new Vector3(0.055f, 0.020f, 0.045f);
            sdGrp.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            Mesh sdHandleMesh = CreateFacetedLatheVesselMesh("Mesh_Cabo_ChaveFenda", 8, new[]
            {
                (-0.044f, 0.000f),
                (-0.044f, 0.008f),
                (-0.034f, 0.013f),
                ( 0.018f, 0.013f),
                ( 0.032f, 0.009f),
                ( 0.044f, 0.011f),
                ( 0.044f, 0.000f)
            });
            CreateMeshObject(sdGrp.transform, "Cabo_Chave_Fenda", sdHandleMesh, pal.calendarRed, new Vector3(0f, 0.013f, 0f), new Vector3(90f, 0f, 0f), castShadows: true);
            CreateDetailCylinder(sdGrp.transform, "Haste_Chave_Fenda", new Vector3(0f, 0.013f, 0.074f), new Vector3(0.006f, 0.032f, 0.006f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailBox(sdGrp.transform, "Ponta_Chave_Fenda", new Vector3(0f, 0.013f, 0.109f), new Vector3(0.009f, 0.0024f, 0.010f), pal.stainlessSteel);
            SnapToSupportTop(sdGrp, tbBottom);

            InspectableObject fusePickup = MakeSpot(toolboxRoot.transform, "Fusivel_Ceramico_Pickup_D3", new Vector3(-0.04f, 0.055f, 0.09f), new Vector3(0.16f, 0.09f, 0.16f));
            GameObject fuseMesh = BuildDetailedCeramicFuse(fusePickup.transform, "Fusivel_Ceramico_D3", Vector3.zero, 90f, pal);
            SnapToSupportTop(fuseMesh, tbBottom);
            fusePickup.ConfigurePickup(
                id: "pegar_fusivel_dia3",
                itemToGrant: "fusivel_reserva",
                prompt: "Pegar o fusível",
                subtitle: "Achei um fusível no fundo da caixa.",
                meshToHide: fuseMesh,
                sound: FoleySoundId.KeyPickup,
                linear: false,
                hudLabel: "Procurar um fusível no banheiro"
            );

            GameObject toolboxLidHost = CreateContainer(toolboxRoot.transform, "Tampa_Caixa_Ferramentas_Interagir");
            CreateDetailBox(toolboxRoot.transform, "Fecho_Base_Caixa", new Vector3(-0.132f, 0.108f, 0f), new Vector3(0.008f, 0.022f, 0.054f), pal.stainlessSteel);
            CreateDetailCylinder(toolboxRoot.transform, "Dobradica_Caixa_S", new Vector3(0.125f, 0.120f, -0.11f), new Vector3(0.010f, 0.018f, 0.010f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(toolboxRoot.transform, "Dobradica_Caixa_N", new Vector3(0.125f, 0.120f,  0.11f), new Vector3(0.010f, 0.018f, 0.010f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));

            GameObject toolboxLidPivot = CreateContainer(toolboxLidHost.transform, "Tampa_Caixa_Pivot");
            toolboxLidPivot.transform.localPosition = new Vector3(0.125f, 0.120f, 0f);
            Mesh tbLidMesh = CreateChamferedRectLoftMesh("Mesh_Tampa_Caixa_Ferramentas", 0.27f, 0.025f, 0.37f, 0.010f, 0.93f, 0.005f);
            GameObject tbLidObj = CreateMeshObject(toolboxLidPivot.transform, "Folha_Tampa", tbLidMesh, pal.calendarRed, new Vector3(-0.125f, 0f, 0f), castShadows: true);
            BoxCollider tbLidCol = tbLidObj.AddComponent<BoxCollider>();
            tbLidCol.center = new Vector3(0f, 0.0125f, 0f);
            tbLidCol.size = new Vector3(0.27f, 0.025f, 0.37f);
            CreateDetailBox(toolboxLidPivot.transform, "Fecho_Tampa_Caixa", new Vector3(-0.258f, 0.006f, 0f), new Vector3(0.008f, 0.018f, 0.044f), pal.stainlessSteel);
            Mesh tbHandleMesh = CreateFacetedCShapeHandleMesh("Mesh_Alca_Caixa_Ferramentas", new[]
            {
                new Vector3(-0.125f, 0.024f, -0.068f),
                new Vector3(-0.125f, 0.052f, -0.062f),
                new Vector3(-0.125f, 0.056f,  0.000f),
                new Vector3(-0.125f, 0.052f,  0.062f),
                new Vector3(-0.125f, 0.024f,  0.068f)
            }, halfThicknessY: 0.006f, halfThicknessZ: 0.008f, sideAxis: Vector3.right);
            CreateMeshObject(toolboxLidPivot.transform, "Alca_Tampa", tbHandleMesh, pal.metalDark, Vector3.zero, castShadows: true);

            AnimatedContainerInteractable toolboxAnim = toolboxLidHost.AddComponent<AnimatedContainerInteractable>();
            toolboxAnim.ConfigureSwingCabinet(
                id: "caixa_ferramentas_d3",
                primaryDoorPivot: toolboxLidPivot.transform,
                primaryOpenEuler: new Vector3(0f, 0f, -82f),
                promptOpen: "Abrir a caixa de ferramentas",
                promptClose: "Fechar a caixa de ferramentas",
                doorSfx: FoleySoundId.MetalInspect
            );
            toolboxAnim.RegisterInteriorObject(fusePickup);
            EditorUtility.SetDirty(toolboxAnim);

            InspectableObject breaker = MakeSpot(d3Root.transform, "Quadro_Forca_Dia3", new Vector3(-6.475f, 1.84f, -1.05f), new Vector3(0.06f, 0.44f, 0.30f));
            GameObject installedFuseVisual = CreateContainer(breaker.transform, "Fusivel_Instalado_Mesh");
            installedFuseVisual.transform.localPosition = new Vector3(0.004f, -0.10f, 0f);
            CreateDetailCylinder(installedFuseVisual.transform, "Corpo_Fusivel_Inst", Vector3.zero, new Vector3(0.028f, 0.012f, 0.028f), pal.porcelainWhite, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(installedFuseVisual.transform, "Tampa_Latao_Fusivel_Inst", new Vector3(0.010f, 0f, 0f), new Vector3(0.024f, 0.004f, 0.024f), pal.metalBrass, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(installedFuseVisual.transform, "Selo_Fusivel_Inst", new Vector3(0.0155f, 0f, 0f), new Vector3(0.008f, 0.0015f, 0.008f), pal.calendarRed, new Vector3(0f, 0f, 90f));
            installedFuseVisual.SetActive(false);
            breaker.ConfigureItemReceiver(
                id: "restaurar_energia_dia3",
                neededItem: "fusivel_reserva",
                lockedPromptText: "Olhar o fusível queimado",
                unlockedPromptText: "Trocar o fusível queimado",
                missingThought: "O cartucho torrou na chave do corredor. Tem que ter reserva na caixa de ferramentas.",
                completedThought: "Estalou. A luz do corredor voltou.",
                repeatThought: "Já tá ligado.",
                lockedSfx: FoleySoundId.DrawerLocked,
                successSfx: FoleySoundId.FuseClackSpark,
                revealObj: installedFuseVisual,
                alertBeep: false,
                linear: true,
                hudLabel: "Trocar o fusível no quadro de força"
            );
            containers.kitchenBreakerBox?.RegisterInteriorObject(breaker);

            InspectableObject coats = MakeSpot(d3Root.transform, "Guarda_Roupa_Casacos_D3", new Vector3(-3.07f, 1.13f, 4.54f), new Vector3(0.36f, 0.96f, 0.30f));
            coats.ConfigurePickup(
                id: "chave_arquivo_pessoal_dia3",
                itemToGrant: "chave_arquivo_pessoal",
                prompt: "Vasculhar o bolso do casaco",
                subtitle: "Uma chavinha escondida no fundo do bolso dela. É da gaveta alta do meu arquivo no escritório.",
                meshToHide: null,
                sound: FoleySoundId.KeyPickup,
                linear: false,
                hudLabel: ""
            );
            containers.bedroomWardrobe?.RegisterInteriorObject(coats);

            // Folha de agenda sobre o topo livre do Criado-Mudo de Margaret (+0.32, 0, +5.78; topo = 0.560f)
            GameObject planner = CreateContainer(d3Root.transform, "Agenda_Margaret_D3");
            planner.transform.localPosition = new Vector3(0.24f, 0.560f, 5.68f);
            planner.transform.localRotation = Quaternion.Euler(0f, 10f, 0f);
            SnapToSupportWorldY(BuildDetailedPaperSheet(planner.transform, "Folha_Agenda_Rasgada", Vector3.zero, 0f, new Vector2(0.18f, 0.24f), pal, printedFaceMat: pal.details.agendaMargaret, printAngleY: 0f), 0.560f);
            BoxCollider plannerCol = planner.AddComponent<BoxCollider>();
            plannerCol.center = new Vector3(0f, 0.02f, 0f);
            plannerCol.size = new Vector3(0.28f, 0.05f, 0.30f);
            InspectableObject plannerDoc = planner.AddComponent<InspectableObject>();
            plannerDoc.ConfigureDocument(
                id: "agenda_margaret_dia3_opc",
                prompt: "Ler a folha rasgada na cabeceira",
                docTitle: "AGENDA PESSOAL",
                docBody:
                    "SÁBADO - 10 DE SETEMBRO\n\n" +
                    "- Deixar as meninas na Dona Lúcia às 10h\n" +
                    "- Buscar o terno cinza do Cian na lavanderia até meio-dia\n" +
                    "- Farmácia: xarope infantil e termômetro\n" +
                    "- Ligar pra minha mãe à tarde\n\n" +
                    "Conversar com o Cian hoje de noite.",
                afterReadThought: "Ela escreveu isso no sábado de manhã...",
                repeatThought: "Ela só queria conversar comigo naquela noite.",
                linear: false,
                hudLabel: "",
                style: DocumentStyle.HandwrittenNote
            );
            plannerDoc.SetDissociationTruthFlash("10 DE SETEMBRO - EU TRANQUEI");

            // Bilhete dobrado no mata-borrão da escrivaninha do escritório (Y = 0.783f)
            GameObject letter = CreateContainer(d3Root.transform, "Bilhete_Margaret_Dia3");
            letter.transform.localPosition = new Vector3(5.62f, 0.783f, -2.52f);
            letter.transform.localRotation = Quaternion.Euler(0f, 16f, 0f);
            BuildDetailedPaperSheet(letter.transform, "Folha_Carta_Dobrada", Vector3.zero, 0f, new Vector2(0.32f, 0.25f), pal, printedFaceMat: pal.details.noteMargaret, printAngleY: 90f);
            CreateDetailBox(letter.transform, "Vinco_Dobra", new Vector3(0f, 0.0044f, 0f), new Vector3(0.004f, 0.0020f, 0.252f), pal.metalDark);
            BoxCollider letterCol = letter.AddComponent<BoxCollider>();
            letterCol.center = new Vector3(0f, 0.05f, 0f);
            letterCol.size = new Vector3(0.42f, 0.14f, 0.46f);
            InspectableObject letterDoc = letter.AddComponent<InspectableObject>();
            letterDoc.ConfigureDocument(
                id: "bilhete_margaret_dia3_opc",
                prompt: "Abrir o bilhete dobrado",
                docTitle: "BILHETE DE MARGARET",
                docBody:
                    "Cian,\n\n" +
                    "Deixei o seu prato tampado no forno desligado, é só esquentar.\n" +
                    "Fiquei te esperando até onze e meia, mas as meninas não\n" +
                    "aguentaram ficar acordadas.\n\n" +
                    "Quando você terminar aí, encosta a porta do corredor\n" +
                    "devagar. Amanhã cedo a gente conversa direito, sem grito na\n" +
                    "frente delas.\n\n" +
                    "Margaret",
                afterReadThought: "Tava dobrado debaixo das pastas desde aquela sexta... Nem li o que ela pediu.",
                repeatThought: "Ela deixou o jantar no forno e foi deitar com as meninas.",
                linear: false,
                hudLabel: "",
                style: DocumentStyle.HandwrittenNote
            );

            InspectableObject cal = MakeSpot(d3Root.transform, "Calendario_Dia3_Interagir", new Vector3(6.27f, 1.56f, -1.05f), new Vector3(0.12f, 0.48f, 0.40f));
            cal.ConfigureTask(
                id: "calendario_d3_opc",
                prompt: "Olhar o calendário",
                subtitle: "Um mês inteiro riscado em cima do outro... Eu só troquei a folha pra fingir que o ano continuou.",
                repeatSubtitle: "Outubro por cima de setembro.",
                linear: false,
                sound: FoleySoundId.PaperRustle
            );

            // Folha grampeada de Setembro/94 liberada somente no Dia 3 (evita vazamento e evita queimar hasInteracted antes da hora)
            CreateBox(d3Root.transform, "Folha_Anterior_Grampeada", new Vector3(6.333f, 1.31f, -1.05f), new Vector3(0.009f, 0.50f, 0.34f), pal.paperWhite, hasCollider: false);
            CreatePrintedOverlayQuad(d3Root.transform, "Impressao_Folha_Set1994", new Vector3(6.3258f, 1.31f, -1.05f), new Vector2(0.334f, 0.494f), pal.details.calendarSep1994, new Vector3(0f, 90f, 0f));
            InspectableObject backSheet = MakeSpot(d3Root.transform, "Calendario_FolhaAnterior_Interagir", new Vector3(6.27f, 1.18f, -1.05f), new Vector3(0.12f, 0.22f, 0.38f));
            backSheet.ConfigureDocument(
                id: "calendario_folha_anterior_opc",
                prompt: "Olhar a folha de baixo do calendário",
                docTitle: "SETEMBRO / 94",
                docBody:
                    "SEG 05    fechamento mensal               x\n" +
                    "TER 06    revisar lote B                  x\n" +
                    "QUA 07    feriado                         x\n" +
                    "QUI 08    reunião Tavares                 x\n" +
                    "SEX 09                                    x\n" +
                    "SÁB 10\n" +
                    "DOM 11\n" +
                    "SEG 12\n" +
                    "TER 13",
                afterReadThought: "Folha de setembro do ano passado embaixo do bloco novo... Risquei tudo até sexta-feira dia nove, e parei.",
                repeatThought: "O último risco quase furou o papel no dia nove.",
                linear: false,
                hudLabel: "",
                style: DocumentStyle.HandwrittenNote
            );

            GameObject sec = BuildAnsweringMachine(d3Root.transform, "Secretaria_Eletronica_D3", new Vector3(-3.06f, 0.765f, 1.825f), pal, blinkingRedLed: false, hasTapeInside: false);
            InspectableObject secInsp = GetOrAdd<InspectableObject>(sec);
            secInsp.ConfigureTask(
                id: "secretaria_d3_opc",
                prompt: "Olhar a secretária",
                subtitle: "Arranquei da tomada. Não aguentava mais esse apito.",
                repeatSubtitle: "Fora da tomada.",
                linear: false,
                sound: FoleySoundId.MetalInspect
            );
            EditorUtility.SetDirty(secInsp);

            // Cofre embutido no nicho da parede Leste do corredor principal (X = +2.52..+2.64f, Z = +1.28f), voltado -X
            GameObject safeRoot = CreateContainer(d3Root.transform, "Cofre_Parede_1009");
            safeRoot.transform.localPosition = new Vector3(2.580f, 1.48f, 1.28f);
            safeRoot.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            CreateBox(safeRoot.transform, "Corpo_Cofre_Metal", new Vector3(0f, 0f, -0.048f), new Vector3(0.46f, 0.38f, 0.016f), pal.metalDark, hasCollider: false);
            CreateBox(safeRoot.transform, "Cavidade_Interna_Cofre", new Vector3(0f, 0f, -0.0385f), new Vector3(0.398f, 0.318f, 0.003f), pal.shadowBlack, hasCollider: false);
            CreateDetailBox(safeRoot.transform, "Moldura_Cofre_Sup", new Vector3(0f, 0.175f, 0.009f), new Vector3(0.46f, 0.030f, 0.098f), pal.metalDark);
            CreateDetailBox(safeRoot.transform, "Moldura_Cofre_Inf", new Vector3(0f, -0.175f, 0.009f), new Vector3(0.46f, 0.030f, 0.098f), pal.metalDark);
            CreateDetailBox(safeRoot.transform, "Moldura_Cofre_Esq", new Vector3(-0.215f, 0f, 0.009f), new Vector3(0.030f, 0.320f, 0.098f), pal.metalDark);
            CreateDetailBox(safeRoot.transform, "Moldura_Cofre_Dir", new Vector3(0.215f, 0f, 0.009f), new Vector3(0.030f, 0.320f, 0.098f), pal.metalDark);
            Vector2[] safeRivets = { new(-0.215f, 0.175f), new(0.215f, 0.175f), new(-0.215f, -0.175f), new(0.215f, -0.175f) };
            for (int sr = 0; sr < safeRivets.Length; sr++)
            {
                CreateDetailCylinder(safeRoot.transform, $"Rebite_Cofre_{sr}", new Vector3(safeRivets[sr].x, safeRivets[sr].y, 0.059f), new Vector3(0.010f, 0.002f, 0.010f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            }
            GameObject safeShelf = CreateDetailBox(safeRoot.transform, "Prateleira_Cofre", new Vector3(0f, -0.153f, 0.004f), new Vector3(0.396f, 0.010f, 0.072f), pal.metalDark);

            GameObject safeKeyVisual = CreateContainer(safeRoot.transform, "Chave_Latao_PortaFilhas");
            safeKeyVisual.transform.localPosition = new Vector3(0.03f, -0.148f, 0.004f);
            safeKeyVisual.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
            GameObject safeFelt = CreateDetailBox(safeKeyVisual.transform, "Feltro_Chave", new Vector3(0.012f, 0.001f, 0f), new Vector3(0.14f, 0.002f, 0.05f), pal.fabricOlive);
            SnapToSupportTop(safeFelt, safeShelf);
            GameObject safeKeyMesh = BuildDetailedKeyWithTag(safeKeyVisual.transform, "Chave_Latao_Detalhada", Vector3.zero, 0f, pal.metalBrass, null, pal);
            SnapToSupportTop(safeKeyMesh, safeFelt);

            GameObject safeDoorPivot = CreateContainer(safeRoot.transform, "Porta_Cofre_Pivot");
            safeDoorPivot.transform.localPosition = new Vector3(-0.20f, 0f, 0.044f);

            Mesh safeDoorMesh = CreateChamferedRectLoftMesh("Mesh_Porta_Cofre_Aco", 0.398f, 0.018f, 0.318f, 0.010f, 0.96f, 0.004f);
            CreateMeshObject(safeDoorPivot.transform, "Folha_Porta_Cofre", safeDoorMesh, pal.metalDark, new Vector3(0.20f, 0f, -0.001f), new Vector3(90f, 0f, 0f), castShadows: true);
            for (int sb = -1; sb <= 1; sb++)
            {
                CreateDetailCylinder(safeDoorPivot.transform, $"Pino_Trava_Cofre_{sb + 1}", new Vector3(0.402f, sb * 0.095f, 0.006f), new Vector3(0.014f, 0.008f, 0.014f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            }
            BuildOpenFrameTrimBars(safeDoorPivot.transform, "Moldura_Teclado_Cofre", new Vector3(0.276f, 0.002f, 0.0208f), 0.196f, 0.236f, 0.008f, 0.0052f, pal.stainlessSteel);
            CreateDetailBox(safeDoorPivot.transform, "Placa_Teclado_Cofre", new Vector3(0.276f, 0.002f, 0.0190f), new Vector3(0.180f, 0.220f, 0.002f), pal.shadowBlack);
            GameObject visor = CreateBox(safeDoorPivot.transform, "Visor_Digital", new Vector3(0.276f, 0.072f, 0.0230f), new Vector3(0.156f, 0.054f, 0.006f), pal.visorScreen, hasCollider: false);
            CreateDetailCylinder(safeDoorPivot.transform, "LED_Cofre_Status_Red", new Vector3(0.228f, 0.036f, 0.0215f), new Vector3(0.007f, 0.002f, 0.007f), pal.calendarRed, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(safeDoorPivot.transform, "LED_Cofre_Status_Green", new Vector3(0.244f, 0.036f, 0.0215f), new Vector3(0.007f, 0.002f, 0.007f), pal.ledGreenDim, new Vector3(90f, 0f, 0f));
            GameObject safeHandlePivot = CreateContainer(safeDoorPivot.transform, "Trava_Giratoria_Pivot");
            safeHandlePivot.transform.localPosition = new Vector3(0.10f, -0.02f, 0.021f);
            CreateCylinder(safeHandlePivot.transform, "Trava_Giratoria", Vector3.zero, new Vector3(0.056f, 0.005f, 0.056f), pal.metalBrass, localEuler: new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(safeHandlePivot.transform, "Cubo_Trava_Cofre", new Vector3(0f, 0f, 0.012f), new Vector3(0.024f, 0.010f, 0.024f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            for (int sp = 0; sp < 3; sp++)
            {
                float spAng = sp * 120f;
                GameObject spokeHolder = CreateContainer(safeHandlePivot.transform, $"Alavanca_Trava_Cofre_{sp}");
                spokeHolder.transform.localPosition = new Vector3(0f, 0f, 0.016f);
                spokeHolder.transform.localRotation = Quaternion.Euler(0f, 0f, spAng);
                CreateDetailCylinder(spokeHolder.transform, "Haste_Raio", new Vector3(0.024f, 0f, 0f), new Vector3(0.009f, 0.024f, 0.009f), pal.metalBrass, new Vector3(0f, 0f, 90f));
                CreateDetailSphere(spokeHolder.transform, "Ponta_Raio", new Vector3(0.048f, 0f, 0f), new Vector3(0.014f, 0.014f, 0.014f), pal.metalBrass, castShadows: false);
            }
            for (int kr = 0; kr < 4; kr++)
            {
                for (int kc = 0; kc < 3; kc++)
                {
                    CreateDetailBox(safeDoorPivot.transform, $"Tecla_Cofre_{kr}_{kc}", new Vector3(0.240f + kc * 0.036f, 0.005f - kr * 0.034f, 0.0225f), new Vector3(0.026f, 0.024f, 0.005f), pal.plasticBeige90s);
                }
            }
            CreateDetailCylinder(safeDoorPivot.transform, "Dobradica_Cofre_Sup", new Vector3(0.005f, 0.11f, 0.010f), new Vector3(0.014f, 0.040f, 0.014f), pal.stainlessSteel);
            CreateDetailCylinder(safeDoorPivot.transform, "Dobradica_Cofre_Inf", new Vector3(0.005f, -0.11f, 0.010f), new Vector3(0.014f, 0.040f, 0.014f), pal.stainlessSteel);

            BoxCollider safeCol = safeRoot.AddComponent<BoxCollider>();
            safeCol.isTrigger = false;
            safeCol.center = new Vector3(0f, 0f, 0.025f);
            safeCol.size = new Vector3(0.50f, 0.42f, 0.09f);

            GameObject visorLightObj = new GameObject("Luz_Visor_Cofre");
            visorLightObj.transform.SetParent(safeDoorPivot.transform, false);
            visorLightObj.transform.localPosition = new Vector3(0.276f, 0.072f, 0.055f);
            Light visorGlowLight = visorLightObj.AddComponent<Light>();
            visorGlowLight.type = LightType.Point;
            visorGlowLight.color = new Color(0.24f, 0.78f, 0.42f);
            visorGlowLight.range = 0.72f;
            visorGlowLight.intensity = 0f;
            visorGlowLight.shadows = LightShadows.None;
            visorGlowLight.enabled = false;

            SafeBoxPuzzle safePuzzle = safeRoot.AddComponent<SafeBoxPuzzle>();
            safePuzzle.ConfigureSafe("1009", safeDoorPivot.transform, visor.GetComponent<Renderer>(), needsPower: true, brassKeyVisual: safeKeyVisual, visorLight: visorGlowLight, handlePivot: safeHandlePivot.transform);
            EditorUtility.SetDirty(safePuzzle);

            // Prego fixo com cabeça chanfrada na parede X=2.520f acima do cofre (permanece na parede quando a moldura cai)
            CreateDetailCylinder(d3Root.transform, "Prego_Quadro_Margaret", new Vector3(2.512f, 1.885f, 1.28f), new Vector3(0.010f, 0.014f, 0.010f), pal.metalDark, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(d3Root.transform, "Cabeca_Prego_Margaret", new Vector3(2.498f, 1.885f, 1.28f), new Vector3(0.016f, 0.002f, 0.016f), pal.metalDark, new Vector3(0f, 0f, 90f));

            // Quadro de Margaret pendurado rente à parede no mesmo plano de D1/D2 (X = 2.503f), cobrindo o cofre embutido no nicho
            // Começa levemente torto (-5.0°) e inclina mais (-15.5°) + revela lascas de gesso no chão quando o jogador passa e desvia o olhar
            GameObject margaretFrame = CreateFramedPicture(d3Root.transform, "Quadro_Margaret_Parede", new Vector3(2.503f, 1.48f, 1.28f), Quaternion.Euler(0f, -90f, -5.0f),
                pal.furnitureWood, pal.details.frameMargaretCorrupted, scaleMultiplier: 1.25f, includeWallNail: false, glassCracked: true);

            BoxCollider frameCol = margaretFrame.AddComponent<BoxCollider>();
            frameCol.isTrigger = false;
            frameCol.center = new Vector3(0f, 0f, 0.02f);
            frameCol.size = new Vector3(0.68f, 0.82f, 0.12f);

            // Lascas de gesso sob o retrato frouxo (em frente ao apoio do quadro caído e antes da passadeira) + bola extra de papel perto do cesto no dia 3
            GameObject plasterChip1 = CreateDetailBox(d3Root.transform, "Lascas_Gesso_Quadro_1", new Vector3(2.185f, 0.0018f, 1.04f), new Vector3(0.048f, 0.0036f, 0.038f), pal.wallPlaster, new Vector3(0f, 28f, 0f));
            GameObject plasterChip2 = CreateDetailBox(d3Root.transform, "Lascas_Gesso_Quadro_2", new Vector3(2.180f, 0.0014f, 1.48f), new Vector3(0.034f, 0.0028f, 0.042f), pal.wallPlaster, new Vector3(0f, -19f, 0f));

            FallingPictureFrame fallingScript = margaretFrame.AddComponent<FallingPictureFrame>();
            fallingScript.LinkHiddenSafe(safePuzzle);
            fallingScript.LinkPlasterChips(new[] { plasterChip1, plasterChip2 });
            EditorUtility.SetDirty(fallingScript);
            Mesh trashPaperD3Mesh = CreateCrumpledPaperMesh("Mesh_TrashPaper_D3", 0.036f, 41);
            GameObject trashPaperD3 = CreateMeshObject(d3Root.transform, "Papel_Amassado_D3", trashPaperD3Mesh, pal.paperWhite, new Vector3(5.08f, 0.036f, -4.06f), new Vector3(8f, 42f, 0f), castShadows: true);
            SnapToSupportWorldY(trashPaperD3, 0f);

            // Rig da Margaret dentro do Quarto das Filhas (X = +3.50f, Z = +4.60f, atrás da Porta_Quarto_Pivot)
            GameObject margaretJumpscare = BuildSculptedPS1Humanoid(d3Root.transform, "Margaret_Jumpscare", SilhouetteArchetype.Margaret, pal.margaretNightgown, pal.margaretPallor, pal);
            margaretJumpscare.transform.localPosition = new Vector3(3.50f, 0f, 4.60f);
            GetOrAdd<HumanoidSpasmAnimator>(margaretJumpscare);
            margaretJumpscare.SetActive(false);

            // Observadora espiando na quina em L do corredor (X=1.04, Z=2.10) e recuando atrás de Parede_Quarto_Corredor
            GameObject watcherD3Rig = CreateContainer(d3Root.transform, "Watcher_Dia3_Corredor");
            Transform peekWatcherD3 = new GameObject("Peek_Watcher_D3").transform;
            peekWatcherD3.SetParent(watcherD3Rig.transform, false);
            peekWatcherD3.localPosition = new Vector3(1.36f, 0f, 1.94f);
            peekWatcherD3.localRotation = Quaternion.Euler(0f, -98f, 0f);

            Transform hideWatcherD3 = new GameObject("Hide_Watcher_D3").transform;
            hideWatcherD3.SetParent(watcherD3Rig.transform, false);
            hideWatcherD3.localPosition = new Vector3(1.26f, 0f, 2.95f);
            hideWatcherD3.localRotation = Quaternion.Euler(0f, -98f, 0f);

            GameObject watcherD3Fig = BuildSculptedPS1Humanoid(watcherD3Rig.transform, "Watcher_Figura_D3", SilhouetteArchetype.ChildWatcher, pal.childNightgownDusty, pal.margaretPallor, pal);
            GetOrAdd<HumanoidSpasmAnimator>(watcherD3Fig);
            WatcherHallucination watcherD3 = GetOrAdd<WatcherHallucination>(watcherD3Rig);
            watcherD3.ConfigureWatcher(watcherD3Fig, peekWatcherD3, hideWatcherD3, blackoutRetreat: true, targetEyeHeight: 0.98f);
            EditorUtility.SetDirty(watcherD3);

            return margaretJumpscare;
        }

        private static Light SetupQuartoFilhas(Transform quartoFilhas, GreyboxPalette pal)
        {
            // v12 Quarto das Filhas: X [+2.68..+6.34] (3.66m), Z [+0.54..+6.04] (5.50m)
            // Paredes externas, piso e rodapé já estão selados na Casa_Base.
            GameObject qRoot = CreateContainer(quartoFilhas, "Ambiente_QuartoFilhas_Greybox");

            GameObject kidsRug = CreateContainer(qRoot.transform, "Tapete_Infantil");
            kidsRug.transform.localPosition = new Vector3(4.24f, 0f, 3.29f);
            CreateDetailBox(kidsRug.transform, "Borda_Tapete_Infantil", new Vector3(0f, 0.005f, 0f), new Vector3(1.86f, 0.010f, 2.04f), pal.bedPastelPink);
            CreateDetailBox(kidsRug.transform, "Campo_Tapete_Infantil", new Vector3(0f, 0.009f, 0f), new Vector3(1.64f, 0.008f, 1.82f), pal.bedPastelBlue);
            CreateDetailCylinder(kidsRug.transform, "Circulo_Central_Tapete", new Vector3(0f, 0.014f, 0f), new Vector3(1.04f, 0.004f, 1.04f), pal.paperWhite);

            BuildCrownMouldingRoom(qRoot.transform, "Sanca_QuartoFilhas", 2.68f, 6.34f, 0.54f, 6.04f, H, pal.ceiling);

            // Janela real recortada na parede Leste (X = +6.34f, no eixo de visão do Spawn_Dia4), com moldura vazada revelando o vão + ShadowBox por trás das tábuas
            GameObject boardedWin = CreateContainer(qRoot.transform, "Janela_Lacrada_Tabuas");
            boardedWin.transform.localPosition = new Vector3(6.340f, 1.58f, 3.29f);
            CreateBox(boardedWin.transform, "Moldura_Filhas_Top", new Vector3(-0.012f, 0.58f, 0f), new Vector3(0.024f, 0.06f, 1.52f), pal.baseboardWood, hasCollider: false);
            CreateBox(boardedWin.transform, "Moldura_Filhas_Base", new Vector3(-0.012f, -0.58f, 0f), new Vector3(0.024f, 0.06f, 1.52f), pal.baseboardWood, hasCollider: false);
            CreateBox(boardedWin.transform, "Moldura_Filhas_Sul", new Vector3(-0.012f, 0f, -0.73f), new Vector3(0.024f, 1.10f, 0.06f), pal.baseboardWood, hasCollider: false);
            CreateBox(boardedWin.transform, "Moldura_Filhas_Norte", new Vector3(-0.012f, 0f, 0.73f), new Vector3(0.024f, 1.10f, 0.06f), pal.baseboardWood, hasCollider: false);
            Mesh sillMesh = CreateChamferedRectLoftMesh("Mesh_Peitoril_Janela_Filhas", 0.096f, 0.032f, 1.60f, 0.008f, 0.96f, 0.005f);
            CreateMeshObject(boardedWin.transform, "Peitoril_Janela_Filhas", sillMesh, pal.woodMahogany, new Vector3(-0.048f, -0.642f, 0f), castShadows: true);
            Mesh boardPlankMesh = CreateChamferedRectLoftMesh("Mesh_Tabua_Janela_Filhas", 0.030f, 0.16f, 1.62f, 0.006f, 0.94f, 0.008f);
            CreateMeshObject(boardedWin.transform, "Tabua_Pregada_1", boardPlankMesh, pal.doorWood, new Vector3(-0.042f, 0.28f, 0f), new Vector3(4f, 0f, 0f), castShadows: true);
            CreateMeshObject(boardedWin.transform, "Tabua_Pregada_2", boardPlankMesh, pal.furnitureWood, new Vector3(-0.042f, -0.06f, 0f), new Vector3(-5.5f, 0f, 0f), castShadows: true);
            CreateMeshObject(boardedWin.transform, "Tabua_Pregada_3", boardPlankMesh, pal.doorWood, new Vector3(-0.042f, -0.42f, 0f), new Vector3(2.5f, 0f, 0f), castShadows: true);
            float[] nailYs = { 0.36f, 0.02f, -0.34f };
            for (int ny = 0; ny < nailYs.Length; ny++)
            {
                CreateDetailCylinder(boardedWin.transform, $"Prego_Esq_{ny}", new Vector3(-0.060f, nailYs[ny], -0.66f), new Vector3(0.026f, 0.006f, 0.026f), pal.metalDark, new Vector3(0f, 0f, 90f));
                CreateDetailCylinder(boardedWin.transform, $"Prego_Dir_{ny}", new Vector3(-0.060f, nailYs[ny], 0.66f), new Vector3(0.026f, 0.006f, 0.026f), pal.metalDark, new Vector3(0f, 0f, 90f));
            }

            BuildChildBed(qRoot.transform, "Cama_Filha_1", new Vector3(5.38f, 0f, 5.26f), pal.bedPastelPink, pal);
            BuildChildBed(qRoot.transform, "Cama_Filha_2", new Vector3(5.38f, 0f, 1.32f), pal.bedPastelBlue, pal);

            // Mesinha infantil chanfrada + 2 cadeirinhas de madeira livres do tapete e camas
            GameObject kidsTable = CreateContainer(qRoot.transform, "Mesinha_Infantil");
            kidsTable.transform.localPosition = new Vector3(5.98f, 0f, 3.29f);
            Mesh kidsTableTopMesh = CreateChamferedRectLoftMesh("Mesh_Tampo_Mesinha_Infantil", 0.48f, 0.030f, 0.62f, 0.018f, 0.97f, 0.005f);
            GameObject kidsTableTop = CreateMeshObject(kidsTable.transform, "Tampo_Mesinha", kidsTableTopMesh, pal.formicaCream, new Vector3(0f, 0.490f, 0f), castShadows: true);
            BoxCollider kidsTableTopCol = kidsTableTop.AddComponent<BoxCollider>();
            kidsTableTopCol.center = new Vector3(0f, 0.015f, 0f);
            kidsTableTopCol.size = new Vector3(0.48f, 0.030f, 0.62f);
            Mesh kidsTableLegMesh = CreateChamferedRectLoftMesh("Mesh_Perna_Mesinha_Infantil", 0.040f, 0.490f, 0.040f, 0.006f, 0.94f, 0.004f);
            float[] ktx = { -0.20f, 0.20f };
            float[] ktz = { -0.26f, 0.26f };
            for (int kx = 0; kx < 2; kx++)
                for (int kz = 0; kz < 2; kz++)
                {
                    GameObject ktLeg = CreateMeshObject(kidsTable.transform, $"Perna_Mesinha_{kx}_{kz}", kidsTableLegMesh, pal.furnitureWood, new Vector3(ktx[kx], 0f, ktz[kz]), castShadows: true);
                    BoxCollider ktLegCol = ktLeg.AddComponent<BoxCollider>();
                    ktLegCol.center = new Vector3(0f, 0.245f, 0f);
                    ktLegCol.size = new Vector3(0.040f, 0.490f, 0.040f);
                }
            CreateDetailBox(kidsTable.transform, "Saia_Mesinha_E", new Vector3(0f, 0.455f, -0.26f), new Vector3(0.36f, 0.05f, 0.016f), pal.furnitureWood);
            CreateDetailBox(kidsTable.transform, "Saia_Mesinha_D", new Vector3(0f, 0.455f, 0.26f), new Vector3(0.36f, 0.05f, 0.016f), pal.furnitureWood);

            BuildDetailedChildChair(qRoot.transform, "Cadeira_Infantil_1", new Vector3(5.56f, 0f, 3.07f), 78f, pal.soapPastelPink, pal);
            BuildDetailedChildChair(qRoot.transform, "Cadeira_Infantil_2", new Vector3(5.58f, 0f, 3.51f), 102f, pal.tileBorderBlue, pal);

            Material[] blockMats = { pal.soapPastelPink, pal.tileBorderBlue, pal.woodLight };
            Mesh toyBlockLarge = CreateChamferedRectLoftMesh("Mesh_Cubo_Brinquedo_G", 0.080f, 0.080f, 0.080f, 0.008f, 0.92f, 0.006f);
            Mesh toyBlockSmall = CreateChamferedRectLoftMesh("Mesh_Cubo_Brinquedo_P", 0.070f, 0.070f, 0.070f, 0.007f, 0.92f, 0.005f);
            GameObject cubeARoot = CreateContainer(kidsTable.transform, "Cubo_Brinquedo_Rosa_Grp");
            cubeARoot.transform.localPosition = new Vector3(-0.11f, 0.520f, -0.14f);
            cubeARoot.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            GameObject cubeA = CreateMeshObject(cubeARoot.transform, "Cubo_Brinquedo_Rosa", toyBlockLarge, blockMats[0], Vector3.zero, castShadows: true);
            CreateDetailBox(cubeARoot.transform, "Letra_Cubo_Rosa", new Vector3(0f, 0.040f, -0.0410f), new Vector3(0.040f, 0.040f, 0.003f), pal.paperWhite);
            SnapToSupportTop(cubeARoot, kidsTableTop);
            GameObject cubeB = CreateMeshObject(kidsTable.transform, "Cubo_Brinquedo_Azul", toyBlockLarge, blockMats[1], new Vector3(0.03f, 0.520f, -0.07f), new Vector3(0f, -22f, 0f), castShadows: true);
            SnapToSupportTop(cubeB, kidsTableTop);
            GameObject cubeTop = CreateMeshObject(kidsTable.transform, "Cubo_Empilhado", toyBlockSmall, blockMats[2], new Vector3(-0.105f, 0.600f, -0.135f), new Vector3(0f, 34f, 0f), castShadows: true);
            SnapToSupportTop(cubeTop, cubeA);
            GameObject crayonBox = CreateDetailBox(kidsTable.transform, "Caixa_Giz_Cera", new Vector3(0.15f, 0.5275f, -0.18f), new Vector3(0.11f, 0.015f, 0.065f), pal.folderKraft, new Vector3(0f, 12f, 0f));
            SnapToSupportTop(crayonBox, kidsTableTop);
            Mesh crayonTipMesh = CreateFacetedLatheVesselMesh("Mesh_Ponta_Giz_Cera", 8, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.0050f),
                (0.014f, 0.0012f),
                (0.014f, 0.000f)
            });
            for (int gc = 0; gc < 3; gc++)
            {
                GameObject crayonGrp = CreateContainer(kidsTable.transform, $"Giz_Cera_{gc}");
                crayonGrp.transform.localPosition = new Vector3(0.12f + gc * 0.03f, 0.520f, -0.08f - gc * 0.018f);
                crayonGrp.transform.localRotation = Quaternion.Euler(0f, 20f + gc * 25f, 0f);
                CreateDetailCylinder(crayonGrp.transform, "Corpo_Giz", new Vector3(0f, 0.005f, 0f), new Vector3(0.010f, 0.036f, 0.010f), blockMats[gc], new Vector3(90f, 0f, 0f));
                CreateMeshObject(crayonGrp.transform, "Ponta_Giz", crayonTipMesh, blockMats[gc], new Vector3(0f, 0.005f, 0.036f), new Vector3(90f, 0f, 0f), castShadows: false);
                SnapToSupportTop(crayonGrp, kidsTableTop);
            }
            SnapToSupportTop(BuildDetailedPaperSheet(kidsTable.transform, "Desenho_Mesa", new Vector3(0.06f, 0.520f, 0.16f), -14f, new Vector2(0.22f, 0.17f), pal, printedFaceMat: pal.details.childDrawingFamily, printAngleY: 90f), kidsTableTop);
            SnapToSupportTop(BuildDetailedRagDoll(kidsTable.transform, "Boneca_Greybox", new Vector3(-0.13f, 0.520f, 0.16f), 155f, pal), kidsTableTop);

            // Cômoda infantil detalhada na parede Sul (Z = +0.54f; pés reais em Y=0, gavetas emolduradas, puxadores torneados, caixinha de música e livros)
            GameObject kidsDresser = CreateContainer(qRoot.transform, "Comoda_Infantil");
            kidsDresser.transform.localPosition = new Vector3(3.26f, 0f, 0.80f);
            kidsDresser.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Mesh kdFootMesh = CreateChamferedRectLoftMesh("Mesh_Pe_Comoda_Infantil", 0.052f, 0.060f, 0.052f, 0.008f, 0.90f, 0.005f);
            float[] kdfX = { -0.50f, 0.50f };
            float[] kdfZ = { -0.17f, 0.17f };
            for (int fx = 0; fx < 2; fx++)
                for (int fz = 0; fz < 2; fz++)
                    CreateMeshObject(kidsDresser.transform, $"Pe_Comoda_Inf_{fx}_{fz}", kdFootMesh, pal.furnitureWood, new Vector3(kdfX[fx], 0f, kdfZ[fz]), castShadows: true);
            CreateBox(kidsDresser.transform, "Corpo_Comoda_Infantil", new Vector3(0f, 0.400f, 0f), new Vector3(1.10f, 0.680f, 0.44f), pal.formicaCream);
            Mesh kdTopMesh = CreateChamferedRectLoftMesh("Mesh_Tampo_Comoda_Infantil", 1.14f, 0.030f, 0.48f, 0.014f, 0.98f, 0.005f);
            GameObject kdTopObj = CreateMeshObject(kidsDresser.transform, "Tampo_Comoda_Infantil", kdTopMesh, pal.furnitureWood, new Vector3(0f, 0.740f, 0f), castShadows: true);
            BoxCollider kdTopCol = kdTopObj.AddComponent<BoxCollider>();
            kdTopCol.center = new Vector3(0f, 0.015f, 0f);
            kdTopCol.size = new Vector3(1.14f, 0.030f, 0.48f);
            Mesh kdKnobMesh = CreateFacetedLatheVesselMesh("Mesh_Puxador_Comoda_Infantil", 8, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.008f),
                (0.008f, 0.007f),
                (0.016f, 0.015f),
                (0.022f, 0.013f),
                (0.025f, 0.000f)
            });
            for (int cd = 0; cd < 3; cd++)
            {
                float cy = 0.185f + cd * 0.215f;
                CreateDetailBox(kidsDresser.transform, $"Frente_Gaveta_Infantil_{cd}", new Vector3(0f, cy, -0.228f), new Vector3(1.02f, 0.184f, 0.014f), pal.formicaCream);
                BuildOpenFrameTrimBars(kidsDresser.transform, $"Moldura_Gaveta_Infantil_{cd}", new Vector3(0f, cy, -0.238f), 0.94f, 0.144f, 0.014f, 0.006f, pal.furnitureWood);
                CreateMeshObject(kidsDresser.transform, $"Puxador_Inf_E_{cd}", kdKnobMesh, pal.woodMahogany, new Vector3(-0.24f, cy, -0.235f), new Vector3(-90f, 0f, 0f), castShadows: false);
                CreateMeshObject(kidsDresser.transform, $"Puxador_Inf_D_{cd}", kdKnobMesh, pal.woodMahogany, new Vector3( 0.24f, cy, -0.235f), new Vector3(-90f, 0f, 0f), castShadows: false);
            }
            Mesh hangingSockMesh = CreateClosedPolygonLoftMesh("Mesh_Meia_Pendurada_Infantil", new[]
            {
                new Vector2(-0.028f, -0.008f),
                new Vector2( 0.028f, -0.008f),
                new Vector2( 0.030f,  0.008f),
                new Vector2(-0.030f,  0.008f)
            }, new[]
            {
                (-0.062f, 1.15f, 1.25f,  0.008f, -0.006f),
                (-0.020f, 0.95f, 1.00f,  0.002f, -0.002f),
                ( 0.044f, 0.85f, 0.70f,  0.000f,  0.002f)
            });
            CreateMeshObject(kidsDresser.transform, "Meia_Pendurada_Gaveta", hangingSockMesh, pal.bedPastelBlue, new Vector3(0.36f, 0.675f, -0.242f), new Vector3(0f, 0f, 8f), castShadows: false);

            // Caixinha de música com chave de corda + livros infantis sobre Tampo_Comoda_Infantil (Y=0.770f)
            GameObject musicBox = CreateContainer(kidsDresser.transform, "Caixinha_Musica_Infantil");
            musicBox.transform.localPosition = new Vector3(-0.28f, 0.770f, -0.02f);
            musicBox.transform.localRotation = Quaternion.Euler(0f, -14f, 0f);
            Mesh mbBaseMesh = CreateChamferedRectLoftMesh("Mesh_Caixinha_Musica_Base", 0.140f, 0.062f, 0.105f, 0.008f, 0.96f, 0.004f);
            CreateMeshObject(musicBox.transform, "Base_Caixinha_Musica", mbBaseMesh, pal.woodMahogany, Vector3.zero, castShadows: true);
            Mesh mbLidMesh = CreateChamferedRectLoftMesh("Mesh_Caixinha_Musica_Tampa", 0.146f, 0.018f, 0.110f, 0.008f, 0.90f, 0.005f);
            CreateMeshObject(musicBox.transform, "Tampa_Caixinha_Musica", mbLidMesh, pal.baseboardWood, new Vector3(0f, 0.062f, 0f), castShadows: true);
            CreateDetailBox(musicBox.transform, "Fecho_Caixinha_Musica", new Vector3(0f, 0.060f, -0.055f), new Vector3(0.020f, 0.014f, 0.005f), pal.metalBrass);
            CreateDetailCylinder(musicBox.transform, "Eixo_Corda_Musica", new Vector3(0.076f, 0.030f, 0f), new Vector3(0.006f, 0.008f, 0.006f), pal.metalBrass, new Vector3(0f, 0f, 90f));
            CreateDetailBox(musicBox.transform, "Chave_Corda_Musica", new Vector3(0.085f, 0.030f, 0f), new Vector3(0.003f, 0.022f, 0.014f), pal.metalBrass);
            SnapToSupportTop(musicBox, kdTopObj);

            GameObject kidsBooks = BuildStackedBooks(kidsDresser.transform, "Livros_Comoda_Infantil", new Vector3(0.24f, 0.770f, 0.02f), 11f, pal, count: 2);
            SnapToSupportTop(kidsBooks, kdTopObj);

            // Desenhos infantis em containers unitários com arte 2D minimalista nítida + fita adesiva no topo
            GameObject drw1 = CreateContainer(qRoot.transform, "Desenho_Parede_1");
            drw1.transform.localPosition = new Vector3(3.04f, 1.42f, 0.548f);
            drw1.transform.localRotation = Quaternion.Euler(0f, 180f, -4f);
            CreateDetailBox(drw1.transform, "Papel_Desenho_1", Vector3.zero, new Vector3(0.28f, 0.22f, 0.008f), pal.paperWhite);
            CreatePrintedOverlayQuad(drw1.transform, "Impressao_Desenho_1", new Vector3(0f, 0f, -0.0046f), new Vector2(0.276f, 0.216f), pal.details.childDrawingHouse, Vector3.zero);
            CreateDetailBox(drw1.transform, "Fita_Adesiva_2", new Vector3(0f, 0.105f, -0.0056f), new Vector3(0.050f, 0.022f, 0.0022f), pal.folderKraft);

            GameObject drw2 = CreateContainer(qRoot.transform, "Desenho_Parede_2");
            drw2.transform.localPosition = new Vector3(3.48f, 1.46f, 0.548f);
            drw2.transform.localRotation = Quaternion.Euler(0f, 180f, 6f);
            CreateDetailBox(drw2.transform, "Papel_Desenho_2", Vector3.zero, new Vector3(0.26f, 0.20f, 0.008f), pal.paperWhite);
            CreatePrintedOverlayQuad(drw2.transform, "Impressao_Desenho_2", new Vector3(0f, 0f, -0.0046f), new Vector2(0.256f, 0.196f), pal.details.childDrawingFamily, Vector3.zero);
            CreateDetailBox(drw2.transform, "Fita_Adesiva_1", new Vector3(0f, 0.095f, -0.0056f), new Vector3(0.047f, 0.020f, 0.0022f), pal.folderKraft);

            return CreateCeilingLamp(qRoot.transform, "Luz_QuartoFilhas", new Vector3(4.51f, H - 0.125f, 3.29f), 5.4f, pal, castShadows: true, spotAngle: 136f, innerSpotAngle: 64f, shadowTier: 2);
        }

        private static PlayerController SetupAndFixPlayer(Scene scene, Transform startSpawn, GreyboxPalette pal)
        {
            GameObject playerObj = FindInScene(scene, "Player");
            if (playerObj == null)
            {
                playerObj = new GameObject("Player");
            }

            playerObj.tag = "Player";
            playerObj.transform.SetParent(null);
            playerObj.transform.localScale = Vector3.one;
            playerObj.transform.SetPositionAndRotation(startSpawn.position, startSpawn.rotation);

            CharacterController cc = GetOrAdd<CharacterController>(playerObj);
            cc.height = 1.80f;
            cc.radius = 0.30f;
            cc.center = new Vector3(0f, 0.90f, 0f);
            cc.stepOffset = 0.25f;
            cc.skinWidth = 0.03f;

            Camera cam = playerObj.GetComponentInChildren<Camera>(true);
            if (cam == null)
            {
                GameObject mainCamObj = FindInScene(scene, "Main Camera") ?? new GameObject("Main Camera");
                cam = GetOrAdd<Camera>(mainCamObj);
                GetOrAdd<AudioListener>(mainCamObj);
                mainCamObj.tag = "MainCamera";
                mainCamObj.transform.SetParent(playerObj.transform, false);
            }
            cam.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            cam.transform.localRotation = Quaternion.identity;
            cam.transform.localScale = Vector3.one;
            cam.nearClipPlane = 0.06f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.010f, 0.010f, 0.010f, 1f);

            UniversalAdditionalCameraData camData = GetOrAdd<UniversalAdditionalCameraData>(cam.gameObject);
            camData.renderPostProcessing = true;
            camData.volumeLayerMask = ~0;
            camData.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
            camData.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
            EditorUtility.SetDirty(camData);

            Transform adaptTr = cam.transform.Find("Luz_Adaptacao_Escuro");
            if (adaptTr != null)
            {
                Object.DestroyImmediate(adaptTr.gameObject);
            }

            Transform bodyTr = playerObj.transform.Find("Corpo_Greybox");
            if (bodyTr != null)
            {
                if (bodyTr.TryGetComponent<Collider>(out Collider bodyCol))
                {
                    Object.DestroyImmediate(bodyCol);
                }
                bodyTr.localPosition = new Vector3(0f, 0.85f, 0f);
                bodyTr.localRotation = Quaternion.identity;
                bodyTr.localScale = new Vector3(0.42f, 0.82f, 0.32f);
                if (bodyTr.TryGetComponent<MeshRenderer>(out MeshRenderer mr))
                {
                    mr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    mr.sharedMaterial = pal.metalDark;
                }
            }

            PlayerController pc = GetOrAdd<PlayerController>(playerObj);
            FlashlightController flashlightCtrl = GetOrAdd<FlashlightController>(playerObj);
            InteractionSystem interaction = GetOrAdd<InteractionSystem>(playerObj);

            SerializedObject pcSO = new SerializedObject(pc);
            SerializedProperty camProp = pcSO.FindProperty("cameraTransform");
            if (camProp != null) camProp.objectReferenceValue = cam.transform;
            SerializedProperty normSpdProp = pcSO.FindProperty("normalMoveSpeed");
            if (normSpdProp != null) normSpdProp.floatValue = 1.815f;
            SerializedProperty depActiveProp = pcSO.FindProperty("isDepressantActive");
            if (depActiveProp != null) depActiveProp.boolValue = false;
            SerializedProperty depSpdProp = pcSO.FindProperty("depressantMoveSpeed");
            if (depSpdProp != null) depSpdProp.floatValue = 1.242f;
            SerializedProperty actActiveProp = pcSO.FindProperty("isActivatingDrugActive");
            if (actActiveProp != null) actActiveProp.boolValue = false;
            SerializedProperty actSpdProp = pcSO.FindProperty("activatingDrugMoveSpeed");
            if (actSpdProp != null) actSpdProp.floatValue = 2.42f;
            SerializedProperty normStrideProp = pcSO.FindProperty("normalStepStride");
            if (normStrideProp != null) normStrideProp.floatValue = 1.14f;
            SerializedProperty depStrideProp = pcSO.FindProperty("depressantStepStride");
            if (depStrideProp != null) depStrideProp.floatValue = 0.86f;
            pcSO.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject intSO = new SerializedObject(interaction);
            intSO.FindProperty("raycastOrigin").objectReferenceValue = cam.transform;
            intSO.FindProperty("interactionDistance").floatValue = 3.25f;
            intSO.FindProperty("assistSphereRadius").floatValue = 0.24f;
            intSO.FindProperty("maxAssistAngle").floatValue = 14f;
            intSO.FindProperty("targetRetentionGrace").floatValue = 0.12f;
            intSO.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject fcSO = new SerializedObject(flashlightCtrl);
            SetFloatProp(fcSO, "baseIntensity", 2.55f);
            SetFloatProp(fcSO, "beamRange", 10.5f);
            SetFloatProp(fcSO, "spotAngle", 48f);
            SetFloatProp(fcSO, "innerSpotAngle", 19f);
            fcSO.ApplyModifiedPropertiesWithoutUndo();

            flashlightCtrl.ResolveCameraAndEnsureRig();
            flashlightCtrl.SetEquipped(false, turnOnImmediately: false, playSound: false);

            EditorUtility.SetDirty(pc);
            EditorUtility.SetDirty(flashlightCtrl);
            EditorUtility.SetDirty(interaction);
            return pc;
        }

        private static SanityEffects SetupGlobalVolumeAndSanity(Scene scene)
        {
            GameObject volObj = FindInScene(scene, "Global Volume");
            if (volObj == null)
            {
                volObj = new GameObject("Global Volume");
            }

            Volume vol = GetOrAdd<Volume>(volObj);
            vol.isGlobal = true;
            if (vol.sharedProfile == null)
            {
                vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset")
                                 ?? AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/DefaultVolumeProfile.asset");
            }

            if (vol.sharedProfile != null)
            {
                if (vol.sharedProfile.components != null)
                {
                    vol.sharedProfile.components.RemoveAll(c => c == null);
                }

                Tonemapping tone = GetOrAddPersistedVolumeComponent<Tonemapping>(vol.sharedProfile);
                tone.active = true;
                tone.mode.Override(TonemappingMode.Neutral);

                Bloom bloom = GetOrAddPersistedVolumeComponent<Bloom>(vol.sharedProfile);
                bloom.active = true;
                bloom.threshold.Override(1.18f);
                bloom.intensity.Override(0.14f);
                bloom.scatter.Override(0.42f);

                ColorAdjustments ca = GetOrAddPersistedVolumeComponent<ColorAdjustments>(vol.sharedProfile);
                ca.active = true;
                ca.saturation.Override(-14f);
                ca.colorFilter.Override(new Color(0.992f, 0.988f, 0.982f, 1f));
                ca.postExposure.Override(0.02f);
                ca.contrast.Override(9f);

                LiftGammaGain lgg = GetOrAddPersistedVolumeComponent<LiftGammaGain>(vol.sharedProfile);
                lgg.active = true;
                lgg.lift.Override(new Vector4(1f, 1f, 1f, 0.0012f));
                lgg.gamma.Override(new Vector4(1f, 1f, 1f, 0f));
                lgg.gain.Override(new Vector4(1f, 1f, 1f, 0f));

                Vignette vig = GetOrAddPersistedVolumeComponent<Vignette>(vol.sharedProfile);
                vig.active = true;
                vig.intensity.Override(0.26f);
                vig.smoothness.Override(0.52f);
                vig.rounded.Override(false);

                EditorUtility.SetDirty(vol.sharedProfile);
            }

            SanityEffects sanity = GetOrAdd<SanityEffects>(volObj);
            SerializedObject so = new SerializedObject(sanity);
            so.FindProperty("globalVolume").objectReferenceValue = vol;

            SetFloatProp(so, "burnoutSaturation", -14f);
            SetColorProp(so, "burnoutColorFilter", new Color(0.992f, 0.988f, 0.982f, 1f));
            SetFloatProp(so, "burnoutExposure", 0.02f);
            SetFloatProp(so, "burnoutVignette", 0.26f);
            SetFloatProp(so, "burnoutContrast", 9f);
            SetFloatProp(so, "burnoutFilmGrain", 0.17f);
            SetFloatProp(so, "burnoutShadowLift", 0.0012f);
            SetFloatProp(so, "burnoutBloom", 0.14f);

            SetFloatProp(so, "depressantSaturation", -22f);
            SetColorProp(so, "depressantColorFilter", new Color(0.972f, 0.974f, 0.978f, 1f));
            SetFloatProp(so, "depressantExposure", -0.02f);
            SetFloatProp(so, "depressantVignette", 0.35f);
            SetFloatProp(so, "depressantBaseChromatic", 0.07f);
            SetFloatProp(so, "depressantContrast", 11f);
            SetFloatProp(so, "depressantFilmGrain", 0.24f);
            SetFloatProp(so, "depressantShadowLift", 0.0016f);
            SetFloatProp(so, "depressantBloom", 0.16f);

            SetFloatProp(so, "hallucinogenSaturation", -31f);
            SetColorProp(so, "hallucinogenColorFilter", new Color(0.946f, 0.948f, 0.958f, 1f));
            SetFloatProp(so, "hallucinogenExposure", -0.05f);
            SetFloatProp(so, "hallucinogenVignette", 0.42f);
            SetFloatProp(so, "hallucinogenContrast", 13.5f);
            SetFloatProp(so, "hallucinogenFilmGrain", 0.30f);
            SetFloatProp(so, "hallucinogenShadowLift", 0.0018f);
            SetFloatProp(so, "hallucinogenBloom", 0.16f);
            SetFloatProp(so, "chromaticWaveFrequency", 1.55f);
            SetVector2Prop(so, "chromaticMinMax", new Vector2(0.18f, 0.72f));
            SetFloatProp(so, "lensWaveFrequency", 1.15f);
            SetVector2Prop(so, "lensDistortionMinMax", new Vector2(-0.30f, 0.12f));

            SetFloatProp(so, "preludeSaturation", -28f);
            SetColorProp(so, "preludeColorFilter", new Color(0.958f, 0.962f, 0.968f, 1f));
            SetFloatProp(so, "preludeExposure", -0.01f);
            SetFloatProp(so, "preludeVignette", 0.34f);
            SetFloatProp(so, "preludeContrast", 11f);
            SetFloatProp(so, "preludeFilmGrain", 0.20f);
            SetFloatProp(so, "preludeShadowLift", 0.0014f);
            SetFloatProp(so, "preludeBloom", 0.14f);

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(sanity);
            return sanity;
        }

        private static void SetFloatProp(SerializedObject so, string propName, float val)
        {
            SerializedProperty p = so.FindProperty(propName);
            if (p != null) p.floatValue = val;
        }

        private static void SetColorProp(SerializedObject so, string propName, Color val)
        {
            SerializedProperty p = so.FindProperty(propName);
            if (p != null) p.colorValue = val;
        }

        private static void SetVector2Prop(SerializedObject so, string propName, Vector2 val)
        {
            SerializedProperty p = so.FindProperty(propName);
            if (p != null) p.vector2Value = val;
        }

        private static T GetOrAddPersistedVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T existing) && existing != null)
            {
                if (AssetDatabase.Contains(profile) && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(existing)))
                {
                    existing.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(existing, profile);
                }
                EditorUtility.SetDirty(existing);
                return existing;
            }

            T created = profile.Add<T>(true);
            created.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            if (AssetDatabase.Contains(profile) && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(created)))
            {
                AssetDatabase.AddObjectToAsset(created, profile);
            }
            EditorUtility.SetDirty(created);
            return created;
        }

        private static Texture EnsureInteriorNeutralCubemapAsset()
        {
            const string assetPath = "Assets/Settings/InteriorNeutralCubemap.asset";
            Cubemap cube = AssetDatabase.LoadAssetAtPath<Cubemap>(assetPath);
            if (cube == null)
            {
                cube = new Cubemap(16, TextureFormat.RGBA32, false);
                cube.name = "InteriorNeutralCubemap";
                AssetDatabase.CreateAsset(cube, assetPath);
            }

            Color wallColor = new Color(0.038f, 0.037f, 0.036f, 1f);
            Color ceilingColor = new Color(0.030f, 0.029f, 0.028f, 1f);
            Color floorColor = new Color(0.022f, 0.021f, 0.020f, 1f);
            for (int f = 0; f < 6; f++)
            {
                CubemapFace face = (CubemapFace)f;
                Color faceTint = face == CubemapFace.PositiveY ? ceilingColor : (face == CubemapFace.NegativeY ? floorColor : wallColor);
                Color[] pixels = new Color[16 * 16];
                for (int p = 0; p < pixels.Length; p++) pixels[p] = faceTint;
                cube.SetPixels(pixels, face);
            }
            cube.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            EditorUtility.SetDirty(cube);
            return cube;
        }

        private static Light SetupMainLightAndAmbient(Scene scene)
        {
            RenderSettings.skybox = null;
            RenderSettings.sun = null;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = EnsureInteriorNeutralCubemapAsset();
            RenderSettings.reflectionIntensity = 0.28f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.056f, 0.054f, 0.052f);
            RenderSettings.ambientEquatorColor = new Color(0.072f, 0.070f, 0.067f);
            RenderSettings.ambientGroundColor = new Color(0.034f, 0.032f, 0.030f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.020f, 0.019f, 0.018f);
            RenderSettings.fogDensity = 0.007f;

            GameObject dirObj = FindInScene(scene, "Directional Light");
            if (dirObj == null) return null;

            Light l = dirObj.GetComponent<Light>();
            if (l != null)
            {
                l.intensity = 0f;
                l.enabled = false;
                l.shadows = LightShadows.None;
                EditorUtility.SetDirty(l);
            }
            return l;
        }

        private struct GreyboxPalette
        {
            public Material floorWood;
            public Material floorTile;
            public Material wallPlaster;
            public Material ceiling;
            public Material baseboardWood;
            public Material doorWood;
            public Material metalBrass;
            public Material metalDark;
            public Material sofaFabric;
            public Material furnitureWood;
            public Material porcelainWhite;
            public Material mirrorGlass;
            public Material paperWhite;
            public Material calendarRed;
            public Material tvScreen;
            public Material visorScreen;
            public Material pillBottleAmber;
            public Material portraitNormal;
            public Material portraitCorrupted;
            public Material margaretPallor;
            public Material margaretNightgown;
            public Material cianShirtWrinkled;
            public Material cianTrousersDark;
            public Material cianSkinWeary;
            public Material childNightgownDusty;
            public Material shadowBlack;
            public Material bedPastelPink;
            public Material bedPastelBlue;
            public Material rugFabric;
            public Material lampEmissive;
            public Material lampUnlitGlass;
            public Material lampGreenGlass;
            public Material lampBulbCore;
            public Material woodLight;
            public Material woodMahogany;
            public Material formicaCream;
            public Material graniteDark;
            public Material stainlessSteel;
            public Material ceramicAzulejo;
            public Material plasticBeige90s;
            public Material fabricOlive;
            public Material terracotta;
            public Material foliageDead;
            public Material folderKraft;
            public Material ledRedGlow;
            public Material ledGreenDim;
            public Material coffeeDark;
            public Material wallDampStain;
            public Material floorPlankWear;
            public Material tileGrime;
            public Material rustMetal;
            public Material fabricAsh;
            public Material bookSlateBlue;
            public Material bookBurgundy;
            public Material bookMustard;
            public Material bookTeal;
            public Material bookSage;
            public Material bookTaupe;
            public Material magnetCoral;
            public Material magnetBlue;
            public Material magnetOchre;
            public Material officeCabinetMetal;
            public Material bedBlanket;
            public Material throwBlanket;
            public Material margaretCoatWool;
            public Material curtainFabric;
            public Material rugAccentOlive;
            public Material rugBorderNavy;
            public Material rugBorderBurgundy;
            public Material plasticSlate;
            public Material tileBorderBlue;
            public Material soapPastelPink;
            public Material lampMetalOlive;
            public PrintDetailsPalette details;
        }

        private static void EnsureMaterialFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
                AssetDatabase.CreateFolder("Assets/Materials", "Greybox");
        }

        private static GreyboxPalette CreateOrLoadPalette()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader dirtyLit = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/LowPolyDirtyLit.shader")
                ?? Shader.Find("Canal420/LowPolyDirtyLit")
                ?? urpLit;
            Shader crtPhosphor = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/CRTPhosphorDisplay.shader")
                ?? Shader.Find("Canal420/CRTPhosphorDisplay")
                ?? urpLit;

            RetroSurfaceTextures retro = Canal420RetroSurfaceGenerator.GenerateAllAndLoad();

            var pal = new GreyboxPalette
            {
                floorWood = GetOrCreateMat(dirtyLit, "M_Floor_Wood", new Color(0.23f, 0.17f, 0.12f), 0.34f, 0f),
                floorTile = GetOrCreateMat(dirtyLit, "M_Floor_Tile", new Color(0.48f, 0.52f, 0.54f), 0.62f, 0f),
                wallPlaster = GetOrCreateMat(dirtyLit, "M_Wall_Plaster", new Color(0.55f, 0.54f, 0.50f), 0.13f, 0f),
                ceiling = GetOrCreateMat(dirtyLit, "M_Ceiling", new Color(0.58f, 0.58f, 0.56f), 0.09f, 0f),
                baseboardWood = GetOrCreateMat(dirtyLit, "M_Baseboard_Wood", new Color(0.18f, 0.13f, 0.09f), 0.28f, 0f),
                doorWood = GetOrCreateMat(dirtyLit, "M_Door_Wood", new Color(0.32f, 0.21f, 0.14f), 0.33f, 0f),
                metalBrass = GetOrCreateMat(urpLit, "M_Metal_Brass", new Color(0.72f, 0.58f, 0.26f), 0.72f, 0.78f),
                metalDark = GetOrCreateMat(urpLit, "M_Metal_Dark", new Color(0.14f, 0.15f, 0.17f), 0.43f, 0.52f),
                sofaFabric = GetOrCreateMat(dirtyLit, "M_Sofa_Fabric", new Color(0.23f, 0.27f, 0.32f), 0.11f, 0f),
                furnitureWood = GetOrCreateMat(dirtyLit, "M_Furniture_Wood", new Color(0.30f, 0.20f, 0.13f), 0.31f, 0f),
                porcelainWhite = GetOrCreateMat(urpLit, "M_Porcelain_White", new Color(0.81f, 0.83f, 0.85f), 0.75f, 0f),
                mirrorGlass = GetOrCreateMat(urpLit, "M_Mirror_Glass", new Color(0.58f, 0.68f, 0.74f), 0.92f, 0.66f),
                paperWhite = GetOrCreateMat(dirtyLit, "M_Paper_White", new Color(0.88f, 0.86f, 0.81f), 0.10f, 0f),
                calendarRed = GetOrCreateMat(urpLit, "M_Calendar_Red", new Color(0.66f, 0.14f, 0.15f), 0.18f, 0f),
                tvScreen = GetOrCreateMat(crtPhosphor, "M_TV_Screen", new Color(0.035f, 0.045f, 0.055f), 0.88f, 0.12f),
                visorScreen = GetOrCreateMat(urpLit, "M_Visor_Digital", new Color(0.035f, 0.045f, 0.055f), 0.88f, 0.12f),
                pillBottleAmber = GetOrCreateMat(urpLit, "M_PillBottle_Amber", new Color(0.71f, 0.36f, 0.08f), 0.75f, 0f),
                portraitNormal = GetOrCreateMat(urpLit, "M_Portrait_Normal", new Color(0.49f, 0.54f, 0.61f), 0.28f, 0f),
                portraitCorrupted = GetOrCreateMat(urpLit, "M_Portrait_Corrupted", new Color(0.44f, 0.05f, 0.07f), 0.33f, 0f),
                margaretPallor = GetOrCreateMat(urpLit, "M_Margaret_Pallor", new Color(0.54f, 0.52f, 0.50f), 0.20f, 0f),
                margaretNightgown = GetOrCreateMat(urpLit, "M_Margaret_Nightgown", new Color(0.28f, 0.30f, 0.34f), 0.10f, 0f),
                cianShirtWrinkled = GetOrCreateMat(dirtyLit, "M_Cian_Shirt_Wrinkled", new Color(0.46f, 0.45f, 0.41f), 0.14f, 0f),
                cianTrousersDark = GetOrCreateMat(dirtyLit, "M_Cian_Trousers_Dark", new Color(0.11f, 0.12f, 0.14f), 0.12f, 0f),
                cianSkinWeary = GetOrCreateMat(urpLit, "M_Cian_Skin_Weary", new Color(0.40f, 0.37f, 0.35f), 0.18f, 0f),
                childNightgownDusty = GetOrCreateMat(dirtyLit, "M_Child_Nightgown_Dusty", new Color(0.45f, 0.49f, 0.53f), 0.10f, 0f),
                shadowBlack = GetOrCreateMat(urpLit, "M_Shadow_Black", new Color(0.015f, 0.015f, 0.022f), 0.04f, 0f),
                bedPastelPink = GetOrCreateMat(dirtyLit, "M_Bed_PastelPink", new Color(0.64f, 0.47f, 0.53f), 0.13f, 0f),
                bedPastelBlue = GetOrCreateMat(dirtyLit, "M_Bed_PastelBlue", new Color(0.46f, 0.55f, 0.67f), 0.13f, 0f),
                rugFabric = GetOrCreateMat(dirtyLit, "M_Rug_Fabric", new Color(0.32f, 0.28f, 0.26f), 0.08f, 0f),
                lampEmissive = GetOrCreateMat(urpLit, "M_Lamp_Emissive", new Color(0.95f, 0.91f, 0.83f), 0.38f, 0f, emissive: true),
                lampUnlitGlass = GetOrCreateMat(urpLit, "M_Lamp_Unlit", new Color(0.19f, 0.20f, 0.21f), 0.24f, 0f, emissive: false),
                lampGreenGlass = GetOrCreateMat(urpLit, "M_Lamp_GreenGlass", new Color(0.14f, 0.44f, 0.20f), 0.72f, 0f, emissive: true),
                lampBulbCore = GetOrCreateMat(urpLit, "M_Lamp_BulbCore", new Color(1.00f, 0.95f, 0.84f), 0.50f, 0f, emissive: true),
                woodLight = GetOrCreateMat(dirtyLit, "M_Wood_Light", new Color(0.52f, 0.40f, 0.27f), 0.30f, 0f),
                woodMahogany = GetOrCreateMat(dirtyLit, "M_Wood_Mahogany", new Color(0.22f, 0.10f, 0.07f), 0.42f, 0f),
                formicaCream = GetOrCreateMat(urpLit, "M_Formica_Cream", new Color(0.70f, 0.66f, 0.55f), 0.45f, 0f),
                graniteDark = GetOrCreateMat(urpLit, "M_Granite_Dark", new Color(0.09f, 0.09f, 0.10f), 0.68f, 0.05f),
                stainlessSteel = GetOrCreateMat(urpLit, "M_Stainless_Steel", new Color(0.62f, 0.64f, 0.66f), 0.80f, 0.90f),
                ceramicAzulejo = GetOrCreateMat(dirtyLit, "M_Ceramic_Azulejo", new Color(0.50f, 0.58f, 0.58f), 0.78f, 0f),
                plasticBeige90s = GetOrCreateMat(urpLit, "M_Plastic_Beige90s", new Color(0.62f, 0.58f, 0.46f), 0.40f, 0f),
                fabricOlive = GetOrCreateMat(dirtyLit, "M_Fabric_Olive", new Color(0.26f, 0.28f, 0.14f), 0.10f, 0f),
                terracotta = GetOrCreateMat(urpLit, "M_Terracotta", new Color(0.48f, 0.23f, 0.14f), 0.16f, 0f),
                foliageDead = GetOrCreateMat(urpLit, "M_Foliage_Dead", new Color(0.30f, 0.26f, 0.12f), 0.08f, 0f),
                folderKraft = GetOrCreateMat(dirtyLit, "M_Folder_Kraft", new Color(0.55f, 0.44f, 0.28f), 0.12f, 0f),
                ledRedGlow = GetOrCreateMat(urpLit, "M_LED_Red_Glow", new Color(0.95f, 0.10f, 0.08f), 0.30f, 0f, emissive: true),
                ledGreenDim = GetOrCreateMat(urpLit, "M_LED_Green_Dim", new Color(0.18f, 0.55f, 0.22f), 0.30f, 0f, emissive: true),
                coffeeDark = GetOrCreateMat(urpLit, "M_Coffee_Dark", new Color(0.06f, 0.035f, 0.02f), 0.72f, 0f),
                wallDampStain = GetOrCreateMat(dirtyLit, "M_Wall_DampStain", new Color(0.41f, 0.42f, 0.38f), 0.06f, 0f),
                floorPlankWear = GetOrCreateMat(dirtyLit, "M_Floor_PlankWear", new Color(0.16f, 0.12f, 0.085f), 0.20f, 0f),
                tileGrime = GetOrCreateMat(dirtyLit, "M_Tile_Grime", new Color(0.36f, 0.39f, 0.38f), 0.22f, 0f),
                rustMetal = GetOrCreateMat(urpLit, "M_Rust_Metal", new Color(0.28f, 0.14f, 0.09f), 0.18f, 0.25f),
                fabricAsh = GetOrCreateMat(dirtyLit, "M_Fabric_Ash", new Color(0.35f, 0.34f, 0.32f), 0.08f, 0f),
                bookSlateBlue = GetOrCreateMat(urpLit, "M_Book_SlateBlue", new Color(0.24f, 0.30f, 0.38f), 0.22f, 0f),
                bookBurgundy = GetOrCreateMat(urpLit, "M_Book_Burgundy", new Color(0.40f, 0.24f, 0.25f), 0.22f, 0f),
                bookMustard = GetOrCreateMat(urpLit, "M_Book_Mustard", new Color(0.48f, 0.41f, 0.28f), 0.22f, 0f),
                bookTeal = GetOrCreateMat(urpLit, "M_Book_Teal", new Color(0.23f, 0.35f, 0.34f), 0.22f, 0f),
                bookSage = GetOrCreateMat(urpLit, "M_Book_Sage", new Color(0.32f, 0.37f, 0.25f), 0.22f, 0f),
                bookTaupe = GetOrCreateMat(urpLit, "M_Book_Taupe", new Color(0.42f, 0.38f, 0.32f), 0.20f, 0f),
                magnetCoral = GetOrCreateMat(urpLit, "M_Magnet_Coral", new Color(0.56f, 0.34f, 0.34f), 0.52f, 0f),
                magnetBlue = GetOrCreateMat(urpLit, "M_Magnet_Blue", new Color(0.36f, 0.46f, 0.56f), 0.52f, 0f),
                magnetOchre = GetOrCreateMat(urpLit, "M_Magnet_Ochre", new Color(0.55f, 0.48f, 0.33f), 0.52f, 0f),
                officeCabinetMetal = GetOrCreateMat(urpLit, "M_Office_Cabinet_Metal", new Color(0.45f, 0.49f, 0.50f), 0.38f, 0.22f),
                bedBlanket = GetOrCreateMat(dirtyLit, "M_Bed_Blanket", new Color(0.28f, 0.31f, 0.37f), 0.09f, 0f),
                throwBlanket = GetOrCreateMat(dirtyLit, "M_Throw_Blanket", new Color(0.39f, 0.32f, 0.27f), 0.09f, 0f),
                margaretCoatWool = GetOrCreateMat(dirtyLit, "M_Margaret_Coat_Wool", new Color(0.42f, 0.44f, 0.46f), 0.09f, 0f),
                curtainFabric = GetOrCreateMat(dirtyLit, "M_Curtain_Linen", new Color(0.34f, 0.36f, 0.28f), 0.08f, 0f),
                rugAccentOlive = GetOrCreateMat(dirtyLit, "M_Rug_Accent_Olive", new Color(0.25f, 0.27f, 0.16f), 0.08f, 0f),
                rugBorderNavy = GetOrCreateMat(dirtyLit, "M_Rug_Border_Navy", new Color(0.20f, 0.24f, 0.29f), 0.08f, 0f),
                rugBorderBurgundy = GetOrCreateMat(dirtyLit, "M_Rug_Border_Burgundy", new Color(0.28f, 0.14f, 0.14f), 0.08f, 0f),
                plasticSlate = GetOrCreateMat(urpLit, "M_Plastic_Slate", new Color(0.24f, 0.32f, 0.41f), 0.48f, 0f),
                tileBorderBlue = GetOrCreateMat(urpLit, "M_Tile_Border_Blue", new Color(0.46f, 0.56f, 0.65f), 0.76f, 0f),
                soapPastelPink = GetOrCreateMat(urpLit, "M_Soap_Pastel_Pink", new Color(0.68f, 0.52f, 0.57f), 0.38f, 0f),
                lampMetalOlive = GetOrCreateMat(urpLit, "M_Lamp_Metal_Olive", new Color(0.18f, 0.24f, 0.16f), 0.44f, 0.15f),
                details = Canal420PrintDetailsGenerator.GenerateAllAndLoad()
            };

            // Phase 2.1: Distinct low-detail PS1/VHS diffuse surface textures per physical surface family (zero PBR maps)
            ApplyRetroSurface(pal.wallPlaster, retro.plaster, worldScale: 0.65f, strength: 0.16f);
            ApplyRetroSurface(pal.ceiling, retro.plaster, worldScale: 0.55f, strength: 0.13f);
            ApplyRetroSurface(pal.wallDampStain, retro.plaster, worldScale: 0.70f, strength: 0.18f, dampStrength: 0.36f);

            ApplyRetroSurface(pal.floorWood, retro.floorWood, worldScale: 0.72f, strength: 0.25f);
            ApplyRetroSurface(pal.floorPlankWear, retro.floorWood, worldScale: 0.72f, strength: 0.26f, dampStrength: 0.20f);

            ApplyRetroSurface(pal.doorWood, retro.woodGrain, worldScale: 0.95f, strength: 0.20f);
            ApplyRetroSurface(pal.furnitureWood, retro.woodGrain, worldScale: 1.10f, strength: 0.20f);
            ApplyRetroSurface(pal.baseboardWood, retro.woodGrain, worldScale: 1.20f, strength: 0.18f);
            ApplyRetroSurface(pal.woodLight, retro.woodGrain, worldScale: 1.15f, strength: 0.18f);
            ApplyRetroSurface(pal.woodMahogany, retro.woodGrain, worldScale: 1.15f, strength: 0.18f);

            ApplyRetroSurface(pal.floorTile, retro.tileGrid, worldScale: 0.85f, strength: 0.26f);
            ApplyRetroSurface(pal.ceramicAzulejo, retro.tileGrid, worldScale: 1.15f, strength: 0.24f, dampStrength: 0.18f);
            ApplyRetroSurface(pal.tileGrime, retro.tileGrid, worldScale: 0.85f, strength: 0.26f, dampStrength: 0.24f);

            // Estofados (sofá, poltrona, cadeira do escritório, almofadas) — trama fina de linho/chenille
            ApplyRetroSurface(pal.sofaFabric, retro.sofaUpholstery, worldScale: 2.00f, strength: 0.20f, dampStrength: 0.08f);
            ApplyRetroSurface(pal.fabricOlive, retro.sofaUpholstery, worldScale: 2.10f, strength: 0.19f, dampStrength: 0.08f);

            // Tapetes e passadeiras — trama trançada/tuftada horizontal própria de carpete
            ApplyRetroSurface(pal.rugFabric, retro.rugCarpet, worldScale: 1.95f, strength: 0.24f, dampStrength: 0.10f);
            ApplyRetroSurface(pal.rugAccentOlive, retro.rugCarpet, worldScale: 1.95f, strength: 0.24f, dampStrength: 0.10f);
            ApplyRetroSurface(pal.rugBorderNavy, retro.rugCarpet, worldScale: 1.95f, strength: 0.24f, dampStrength: 0.10f);
            ApplyRetroSurface(pal.rugBorderBurgundy, retro.rugCarpet, worldScale: 1.95f, strength: 0.24f, dampStrength: 0.10f);

            // Colchas, cobertores e roupa de cama — matelassê macio de algodão
            ApplyRetroSurface(pal.bedBlanket, retro.bedQuilt, worldScale: 1.45f, strength: 0.20f, dampStrength: 0.08f);
            ApplyRetroSurface(pal.bedPastelPink, retro.bedQuilt, worldScale: 1.65f, strength: 0.19f, dampStrength: 0.06f);
            ApplyRetroSurface(pal.bedPastelBlue, retro.bedQuilt, worldScale: 1.65f, strength: 0.19f, dampStrength: 0.06f);

            // Cortinas — caimento vertical de linho
            ApplyRetroSurface(pal.curtainFabric, retro.curtainLinen, worldScale: 1.45f, strength: 0.22f, dampStrength: 0.08f);

            // Vestuário, casaco de lã de Margaret e manta — sarja/lã escovada
            ApplyRetroSurface(pal.margaretCoatWool, retro.woolCoat, worldScale: 2.40f, strength: 0.20f, dampStrength: 0.10f);
            ApplyRetroSurface(pal.throwBlanket, retro.woolCoat, worldScale: 2.10f, strength: 0.20f, dampStrength: 0.08f);
            ApplyRetroSurface(pal.fabricAsh, retro.woolCoat, worldScale: 2.20f, strength: 0.19f, dampStrength: 0.08f);
            ApplyRetroSurface(pal.cianShirtWrinkled, retro.woolCoat, worldScale: 2.60f, strength: 0.18f, dampStrength: 0.12f);
            ApplyRetroSurface(pal.cianTrousersDark, retro.woolCoat, worldScale: 2.60f, strength: 0.16f, dampStrength: 0.12f);
            ApplyRetroSurface(pal.childNightgownDusty, retro.woolCoat, worldScale: 2.40f, strength: 0.18f, dampStrength: 0.12f);

            ApplyRetroSurface(pal.paperWhite, retro.mattePaperCarpet, worldScale: 2.40f, strength: 0.13f, dampStrength: 0.04f, microVarStrength: 0.06f);
            ApplyRetroSurface(pal.folderKraft, retro.mattePaperCarpet, worldScale: 2.00f, strength: 0.16f, dampStrength: 0.06f, microVarStrength: 0.08f);

            return pal;
        }

        private static void ApplyRetroSurface(
            Material mat, Texture2D tex, float worldScale, float strength,
            float dampStrength = -1f, float microVarStrength = -1f)
        {
            if (mat == null) return;
            if (mat.HasProperty("_SurfaceTex")) mat.SetTexture("_SurfaceTex", tex);
            if (mat.HasProperty("_SurfaceTexScale")) mat.SetFloat("_SurfaceTexScale", worldScale);
            if (mat.HasProperty("_SurfaceTexStrength")) mat.SetFloat("_SurfaceTexStrength", Mathf.Clamp(strength, 0f, 0.28f));
            if (dampStrength >= 0f && mat.HasProperty("_DampStrength")) mat.SetFloat("_DampStrength", dampStrength);
            if (microVarStrength >= 0f && mat.HasProperty("_MicroVarStrength")) mat.SetFloat("_MicroVarStrength", microVarStrength);
            EditorUtility.SetDirty(mat);
        }

        private static Material GetOrCreateMat(Shader shader, string matName, Color color, float smoothness, float metallic, bool emissive = false)
        {
            string path = $"{MaterialsFolder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = matName };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null && mat.shader != shader)
            {
                mat.shader = shader;
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (matName == "M_TV_Screen")
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", null);
                if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", null);
                if (mat.HasProperty("_CRTActive")) mat.SetFloat("_CRTActive", 0f);
            }

            if (mat.HasProperty("_EmissionColor"))
            {
                if (emissive)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    mat.SetColor("_EmissionColor", color * 1.4f);
                }
                else if (matName == "M_Visor_Digital")
                {
                    // SafeBoxPuzzle drives Visor_Digital emission via MaterialPropertyBlock,
                    // which URP Lit only evaluates when the _EMISSION shader keyword is active.
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    mat.SetColor("_EmissionColor", Color.black);
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                }
            }

            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject BuildStaticWallCalendar(Transform parent, string name, Vector3 pos, GreyboxPalette pal)
        {
            GameObject calObj = CreateContainer(parent, name);
            calObj.transform.localPosition = pos;
            // Painel de parede no eixo YZ apoiado rente à face interna X (folha 18mm + topo 6mm à frente sem Z-fighting)
            CreateBox(calObj.transform, "Folha_Base", new Vector3(-0.009f, 0f, 0f), new Vector3(0.018f, 0.48f, 0.36f), pal.paperWhite, hasCollider: false);
            CreateBox(calObj.transform, "Topo_Vermelho", new Vector3(-0.021f, 0.17f, 0f), new Vector3(0.006f, 0.11f, 0.34f), pal.calendarRed, hasCollider: false);

            BoxCollider col = GetOrAdd<BoxCollider>(calObj);
            col.isTrigger = false;
            col.center = new Vector3(-0.04f, 0f, 0f);
            col.size = new Vector3(0.16f, 0.62f, 0.50f);
            return calObj;
        }

        private static void BuildShutteredWindowZ(
            Transform parent, string name, float xWallFace, float xInwardSign, float zCenter, float yCenter, GreyboxPalette pal, bool withCurtains = false)
        {
            GameObject w = CreateContainer(parent, name);
            w.transform.localPosition = new Vector3(xWallFace, yCenter, zCenter);
            float dir = Mathf.Sign(xInwardSign);

            // Moldura interna vazada (0.000..0.024m na face interna da parede); o fundo abre para o batente real + ShadowBox noturna externa
            CreateBox(w.transform, "Moldura_Top", new Vector3(dir * 0.012f, 0.545f, 0f), new Vector3(0.024f, 0.05f, 1.42f), pal.baseboardWood, hasCollider: false);
            CreateBox(w.transform, "Moldura_Base", new Vector3(dir * 0.012f, -0.545f, 0f), new Vector3(0.024f, 0.05f, 1.42f), pal.baseboardWood, hasCollider: false);
            CreateBox(w.transform, "Moldura_Esq", new Vector3(dir * 0.012f, 0f, -0.685f), new Vector3(0.024f, 1.04f, 0.05f), pal.baseboardWood, hasCollider: false);
            CreateBox(w.transform, "Moldura_Dir", new Vector3(dir * 0.012f, 0f, 0.685f), new Vector3(0.024f, 1.04f, 0.05f), pal.baseboardWood, hasCollider: false);

            CreateDetailBox(w.transform, "Peitoril_Janela", new Vector3(dir * 0.048f, -0.5855f, 0f), new Vector3(0.096f, 0.030f, 1.54f), pal.baseboardWood);
            CreateDetailBox(w.transform, "Friso_Peitoril", new Vector3(dir * 0.056f, -0.608f, 0f), new Vector3(0.080f, 0.014f, 1.48f), pal.woodMahogany);
            CreateDetailBox(w.transform, "Caixa_Persiana", new Vector3(dir * 0.030f, 0.610f, 0f), new Vector3(0.060f, 0.080f, 1.38f), pal.porcelainWhite);
            for (int i = -3; i <= 3; i++)
            {
                CreateBox(w.transform, $"Lamina_{i + 4}", new Vector3(dir * 0.030f, i * 0.135f, 0f), new Vector3(0.016f, 0.105f, 1.30f), pal.porcelainWhite, hasCollider: false, localEuler: new Vector3(0f, 0f, 14f));
            }
            CreateDetailBox(w.transform, "Barra_Inferior_Persiana", new Vector3(dir * 0.030f, -0.478f, 0f), new Vector3(0.022f, 0.020f, 1.32f), pal.porcelainWhite);
            CreateDetailCylinder(w.transform, "Colar_Bastao_Persiana", new Vector3(dir * 0.055f, 0.562f, -0.58f), new Vector3(0.014f, 0.014f, 0.014f), pal.porcelainWhite);
            CreateDetailCylinder(w.transform, "Bastao_Giro_Persiana", new Vector3(dir * 0.055f, 0.22f, -0.58f), new Vector3(0.010f, 0.36f, 0.010f), pal.details.clearGlass != null ? pal.details.clearGlass : pal.porcelainWhite);
            CreateDetailCylinder(w.transform, "Ponteira_Bastao_Persiana", new Vector3(dir * 0.055f, -0.145f, -0.58f), new Vector3(0.013f, 0.012f, 0.013f), pal.porcelainWhite);
            CreateDetailCylinder(w.transform, "Cordao_Persiana", new Vector3(dir * 0.062f, 0.12f, 0.60f), new Vector3(0.006f, 0.48f, 0.006f), pal.paperWhite);
            CreateDetailSphere(w.transform, "Pingente_Cordao", new Vector3(dir * 0.062f, -0.37f, 0.60f), new Vector3(0.020f, 0.020f, 0.020f), pal.paperWhite);
            CreateDetailBox(w.transform, "Cadarco_Persiana_Sul", new Vector3(dir * 0.045f, 0.065f, -0.44f), new Vector3(0.004f, 1.07f, 0.010f), pal.paperWhite);
            CreateDetailBox(w.transform, "Cadarco_Persiana_Norte", new Vector3(dir * 0.045f, 0.065f, 0.44f), new Vector3(0.004f, 1.07f, 0.010f), pal.paperWhite);

            if (withCurtains)
            {
                Mesh ringMesh = GetOrBuildCurtainRingMesh();
                CreateDetailCylinder(w.transform, "Varao_Cortina", new Vector3(dir * 0.128f, 0.70f, 0f), new Vector3(0.018f, 0.90f, 0.018f), pal.metalBrass, new Vector3(90f, 0f, 0f));
                CreateDetailSphere(w.transform, "Ponteira_Varao_Sul", new Vector3(dir * 0.128f, 0.70f, -0.92f), new Vector3(0.036f, 0.036f, 0.036f), pal.metalBrass, castShadows: false);
                CreateDetailSphere(w.transform, "Ponteira_Varao_Norte", new Vector3(dir * 0.128f, 0.70f, 0.92f), new Vector3(0.036f, 0.036f, 0.036f), pal.metalBrass, castShadows: false);
                CreateDetailBox(w.transform, "Roseta_Varao_Sul", new Vector3(dir * 0.005f, 0.70f, -0.78f), new Vector3(0.010f, 0.038f, 0.038f), pal.metalBrass);
                CreateDetailBox(w.transform, "Roseta_Varao_Norte", new Vector3(dir * 0.005f, 0.70f, 0.78f), new Vector3(0.010f, 0.038f, 0.038f), pal.metalBrass);
                CreateDetailBox(w.transform, "Suporte_Varao_Sul", new Vector3(dir * 0.069f, 0.70f, -0.78f), new Vector3(0.118f, 0.018f, 0.018f), pal.metalBrass);
                CreateDetailBox(w.transform, "Suporte_Varao_Norte", new Vector3(dir * 0.069f, 0.70f, 0.78f), new Vector3(0.118f, 0.018f, 0.018f), pal.metalBrass);
                for (int r = 0; r < 4; r++)
                {
                    float zS = -0.74f + r * 0.08f;
                    float zN = 0.50f + r * 0.08f;
                    CreateMeshObject(w.transform, $"Argola_Cortina_Sul_{r}", ringMesh, pal.metalBrass, new Vector3(dir * 0.128f, 0.700f, zS), new Vector3(90f, 0f, 0f), castShadows: false);
                    CreateMeshObject(w.transform, $"Argola_Cortina_Norte_{r}", ringMesh, pal.metalBrass, new Vector3(dir * 0.128f, 0.700f, zN), new Vector3(90f, 0f, 0f), castShadows: false);
                    CreateDetailBox(w.transform, $"Presilha_Cortina_Sul_{r}", new Vector3(dir * 0.128f, 0.684f, zS), new Vector3(0.006f, 0.014f, 0.006f), pal.metalBrass);
                    CreateDetailBox(w.transform, $"Presilha_Cortina_Norte_{r}", new Vector3(dir * 0.128f, 0.684f, zN), new Vector3(0.006f, 0.014f, 0.006f), pal.metalBrass);
                }
                GameObject cSul = BuildPleatedCurtainPanel(w.transform, "Cortina_Sul", new Vector3(dir * 0.128f, 0.01f, -0.62f), 0.34f, 1.38f, pal.curtainFabric);
                cSul.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                GameObject cNorte = BuildPleatedCurtainPanel(w.transform, "Cortina_Norte", new Vector3(dir * 0.128f, 0.01f, 0.62f), 0.34f, 1.38f, pal.curtainFabric);
                cNorte.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }
        }

        private static void BuildShutteredWindowX(
            Transform parent, string name, float xCenter, float zWallFace, float zInwardSign, float yCenter, GreyboxPalette pal, bool withCurtains = false)
        {
            GameObject w = CreateContainer(parent, name);
            w.transform.localPosition = new Vector3(xCenter, yCenter, zWallFace);
            float dir = Mathf.Sign(zInwardSign);

            CreateBox(w.transform, "Moldura_Top", new Vector3(0f, 0.515f, dir * 0.012f), new Vector3(1.34f, 0.05f, 0.024f), pal.baseboardWood, hasCollider: false);
            CreateBox(w.transform, "Moldura_Base", new Vector3(0f, -0.515f, dir * 0.012f), new Vector3(1.34f, 0.05f, 0.024f), pal.baseboardWood, hasCollider: false);
            CreateBox(w.transform, "Moldura_Esq", new Vector3(-0.645f, 0f, dir * 0.012f), new Vector3(0.05f, 0.98f, 0.024f), pal.baseboardWood, hasCollider: false);
            CreateBox(w.transform, "Moldura_Dir", new Vector3(0.645f, 0f, dir * 0.012f), new Vector3(0.05f, 0.98f, 0.024f), pal.baseboardWood, hasCollider: false);

            CreateDetailBox(w.transform, "Peitoril_Janela", new Vector3(0f, -0.5555f, dir * 0.048f), new Vector3(1.46f, 0.030f, 0.096f), pal.baseboardWood);
            CreateDetailBox(w.transform, "Friso_Peitoril", new Vector3(0f, -0.578f, dir * 0.056f), new Vector3(1.40f, 0.014f, 0.080f), pal.woodMahogany);
            CreateDetailBox(w.transform, "Caixa_Persiana", new Vector3(0f, 0.580f, dir * 0.030f), new Vector3(1.30f, 0.080f, 0.060f), pal.porcelainWhite);
            for (int i = -3; i <= 3; i++)
            {
                CreateBox(w.transform, $"Lamina_{i + 4}", new Vector3(0f, i * 0.128f, dir * 0.030f), new Vector3(1.22f, 0.098f, 0.016f), pal.porcelainWhite, hasCollider: false, localEuler: new Vector3(14f, 0f, 0f));
            }
            CreateDetailBox(w.transform, "Barra_Inferior_Persiana", new Vector3(0f, -0.456f, dir * 0.030f), new Vector3(1.24f, 0.020f, 0.022f), pal.porcelainWhite);
            CreateDetailCylinder(w.transform, "Colar_Bastao_Persiana", new Vector3(-0.54f, 0.538f, dir * 0.055f), new Vector3(0.014f, 0.014f, 0.014f), pal.porcelainWhite);
            CreateDetailCylinder(w.transform, "Bastao_Giro_Persiana", new Vector3(-0.54f, 0.20f, dir * 0.055f), new Vector3(0.010f, 0.36f, 0.010f), pal.details.clearGlass != null ? pal.details.clearGlass : pal.porcelainWhite);
            CreateDetailCylinder(w.transform, "Ponteira_Bastao_Persiana", new Vector3(-0.54f, -0.165f, dir * 0.055f), new Vector3(0.013f, 0.012f, 0.013f), pal.porcelainWhite);
            CreateDetailCylinder(w.transform, "Cordao_Persiana", new Vector3(0.56f, 0.11f, dir * 0.062f), new Vector3(0.006f, 0.48f, 0.006f), pal.paperWhite);
            CreateDetailSphere(w.transform, "Pingente_Cordao", new Vector3(0.56f, -0.37f, dir * 0.062f), new Vector3(0.020f, 0.020f, 0.020f), pal.paperWhite);
            CreateDetailBox(w.transform, "Cadarco_Persiana_Oeste", new Vector3(-0.42f, 0.052f, dir * 0.045f), new Vector3(0.010f, 1.00f, 0.004f), pal.paperWhite);
            CreateDetailBox(w.transform, "Cadarco_Persiana_Leste", new Vector3(0.42f, 0.052f, dir * 0.045f), new Vector3(0.010f, 1.00f, 0.004f), pal.paperWhite);

            if (withCurtains)
            {
                Mesh ringMesh = GetOrBuildCurtainRingMesh();
                CreateDetailCylinder(w.transform, "Varao_Cortina", new Vector3(0f, 0.67f, dir * 0.128f), new Vector3(0.018f, 0.86f, 0.018f), pal.metalBrass, new Vector3(0f, 0f, 90f));
                CreateDetailSphere(w.transform, "Ponteira_Varao_Oeste", new Vector3(-0.88f, 0.67f, dir * 0.128f), new Vector3(0.036f, 0.036f, 0.036f), pal.metalBrass, castShadows: false);
                CreateDetailSphere(w.transform, "Ponteira_Varao_Leste", new Vector3(0.88f, 0.67f, dir * 0.128f), new Vector3(0.036f, 0.036f, 0.036f), pal.metalBrass, castShadows: false);
                CreateDetailBox(w.transform, "Roseta_Varao_Oeste", new Vector3(-0.74f, 0.67f, dir * 0.005f), new Vector3(0.038f, 0.038f, 0.010f), pal.metalBrass);
                CreateDetailBox(w.transform, "Roseta_Varao_Leste", new Vector3( 0.74f, 0.67f, dir * 0.005f), new Vector3(0.038f, 0.038f, 0.010f), pal.metalBrass);
                CreateDetailBox(w.transform, "Suporte_Varao_Oeste", new Vector3(-0.74f, 0.67f, dir * 0.069f), new Vector3(0.018f, 0.018f, 0.118f), pal.metalBrass);
                CreateDetailBox(w.transform, "Suporte_Varao_Leste", new Vector3(0.74f, 0.67f, dir * 0.069f), new Vector3(0.018f, 0.018f, 0.118f), pal.metalBrass);
                for (int r = 0; r < 4; r++)
                {
                    float xW = -0.70f + r * 0.08f;
                    float xE = 0.46f + r * 0.08f;
                    CreateMeshObject(w.transform, $"Argola_Cortina_Oeste_{r}", ringMesh, pal.metalBrass, new Vector3(xW, 0.670f, dir * 0.128f), new Vector3(0f, 0f, 90f), castShadows: false);
                    CreateMeshObject(w.transform, $"Argola_Cortina_Leste_{r}", ringMesh, pal.metalBrass, new Vector3(xE, 0.670f, dir * 0.128f), new Vector3(0f, 0f, 90f), castShadows: false);
                    CreateDetailBox(w.transform, $"Presilha_Cortina_Oeste_{r}", new Vector3(xW, 0.654f, dir * 0.128f), new Vector3(0.006f, 0.014f, 0.006f), pal.metalBrass);
                    CreateDetailBox(w.transform, $"Presilha_Cortina_Leste_{r}", new Vector3(xE, 0.654f, dir * 0.128f), new Vector3(0.006f, 0.014f, 0.006f), pal.metalBrass);
                }
                BuildPleatedCurtainPanel(w.transform, "Cortina_Oeste", new Vector3(-0.58f, -0.01f, dir * 0.128f), 0.34f, 1.34f, pal.curtainFabric);
                BuildPleatedCurtainPanel(w.transform, "Cortina_Leste", new Vector3(0.58f, -0.01f, dir * 0.128f), 0.34f, 1.34f, pal.curtainFabric);
            }
        }

        private static void BuildWindowRecessAndShadowBoxX(
            Transform parent, string tag, float xMin, float xMax, float yMin, float yMax, float wallCenterZ, float exteriorDir, GreyboxPalette pal)
        {
            float dir = Mathf.Sign(exteriorDir);
            float width = xMax - xMin;
            float height = yMax - yMin;
            float xCenter = (xMin + xMax) * 0.5f;
            float yCenter = (yMin + yMax) * 0.5f;
            const float jambT = 0.022f;
            float innerHeight = Mathf.Max(0.10f, height - jambT * 2f);
            float innerWidth = Mathf.Max(0.10f, width - jambT * 2f);

            // Batente real nos 0.16m de espessura da parede
            GameObject jambRoot = CreateContainer(parent, $"Batente_Janela_{tag}");
            CreateBox(jambRoot.transform, $"Batente_Janela_{tag}_Sup", new Vector3(xCenter, yMax - jambT * 0.5f, wallCenterZ), new Vector3(width, jambT, T), pal.baseboardWood, hasCollider: false);
            CreateBox(jambRoot.transform, $"Batente_Janela_{tag}_Inf", new Vector3(xCenter, yMin + jambT * 0.5f, wallCenterZ), new Vector3(width, jambT, T), pal.baseboardWood, hasCollider: false);
            CreateBox(jambRoot.transform, $"Batente_Janela_{tag}_Min", new Vector3(xMin + jambT * 0.5f, yCenter, wallCenterZ), new Vector3(jambT, innerHeight, T), pal.baseboardWood, hasCollider: false);
            CreateBox(jambRoot.transform, $"Batente_Janela_{tag}_Max", new Vector3(xMax - jambT * 0.5f, yCenter, wallCenterZ), new Vector3(jambT, innerHeight, T), pal.baseboardWood, hasCollider: false);

            // ShadowBox noturna externa estanque (TwoSided via prefixo ShadowBox_), sem vazamento de luz/céu
            const float cavityDepth = 0.24f;
            const float wallThick = 0.04f;
            const float margin = 0.08f;
            float extFaceZ = wallCenterZ + dir * (T * 0.5f);
            float cavityMidZ = extFaceZ + dir * (cavityDepth * 0.5f);
            float backMidZ = extFaceZ + dir * (cavityDepth + wallThick * 0.5f);

            GameObject shadowRoot = CreateContainer(parent, $"ShadowBox_{tag}");
            CreateBox(shadowRoot.transform, $"ShadowBox_{tag}_Fundo", new Vector3(xCenter, yCenter, backMidZ), new Vector3(width + margin * 2f, height + wallThick * 2f, wallThick), pal.shadowBlack, hasCollider: false);
            CreateBox(shadowRoot.transform, $"ShadowBox_{tag}_Sup", new Vector3(xCenter, yMax + wallThick * 0.5f, cavityMidZ), new Vector3(width + margin * 2f, wallThick, cavityDepth), pal.shadowBlack, hasCollider: false);
            CreateBox(shadowRoot.transform, $"ShadowBox_{tag}_Inf", new Vector3(xCenter, yMin - wallThick * 0.5f, cavityMidZ), new Vector3(width + margin * 2f, wallThick, cavityDepth), pal.shadowBlack, hasCollider: false);
            CreateBox(shadowRoot.transform, $"ShadowBox_{tag}_Min", new Vector3(xMin - margin * 0.5f, yCenter, cavityMidZ), new Vector3(margin, height, cavityDepth), pal.shadowBlack, hasCollider: false);
            CreateBox(shadowRoot.transform, $"ShadowBox_{tag}_Max", new Vector3(xMax + margin * 0.5f, yCenter, cavityMidZ), new Vector3(margin, height, cavityDepth), pal.shadowBlack, hasCollider: false);

            // Vidro interno da janela (M_Clear_Glass sem colisor/sombra) + Caixilho em profundidade + grade + veneziana
            float glassZ = extFaceZ + dir * 0.011f;
            CreateDetailBox(shadowRoot.transform, $"Vidro_Janela_{tag}", new Vector3(xCenter, yCenter, glassZ), new Vector3(innerWidth - 0.004f, innerHeight - 0.004f, 0.004f), pal.details.clearGlass);

            float sashZ = extFaceZ + dir * 0.025f;
            CreateDetailBox(shadowRoot.transform, $"Caixilho_{tag}_Vert", new Vector3(xCenter, yCenter, sashZ), new Vector3(0.032f, innerHeight, 0.020f), pal.woodMahogany);
            float halfLeafW = (innerWidth - 0.032f) * 0.5f;
            CreateDetailBox(shadowRoot.transform, $"Caixilho_{tag}_Horiz_Min", new Vector3(xCenter - 0.016f - halfLeafW * 0.5f, yCenter, sashZ), new Vector3(halfLeafW, 0.024f, 0.016f), pal.woodMahogany);
            CreateDetailBox(shadowRoot.transform, $"Caixilho_{tag}_Horiz_Max", new Vector3(xCenter + 0.016f + halfLeafW * 0.5f, yCenter, sashZ), new Vector3(halfLeafW, 0.024f, 0.016f), pal.woodMahogany);

            float barZ = extFaceZ + dir * 0.075f;
            float railZ = extFaceZ + dir * 0.092f;
            for (int b = -2; b <= 2; b++)
            {
                float bx = xCenter + b * (innerWidth * 0.18f);
                CreateDetailBox(shadowRoot.transform, $"Grade_Janela_{tag}_V{b + 3}", new Vector3(bx, yCenter, barZ), new Vector3(0.014f, innerHeight, 0.014f), pal.metalDark);
            }
            CreateDetailBox(shadowRoot.transform, $"Grade_Janela_{tag}_H_Sup", new Vector3(xCenter, yCenter + innerHeight * 0.30f, railZ), new Vector3(innerWidth, 0.016f, 0.012f), pal.metalDark);
            CreateDetailBox(shadowRoot.transform, $"Grade_Janela_{tag}_H_Inf", new Vector3(xCenter, yCenter - innerHeight * 0.30f, railZ), new Vector3(innerWidth, 0.016f, 0.012f), pal.metalDark);

            float louverZ = extFaceZ + dir * 0.165f;
            for (int v = -3; v <= 3; v++)
            {
                float vy = yCenter + v * (innerHeight * 0.125f);
                CreateDetailBox(shadowRoot.transform, $"Veneziana_Ext_{tag}_{v + 4}", new Vector3(xCenter, vy, louverZ), new Vector3(innerWidth * 0.94f, 0.042f, 0.012f), pal.shadowBlack, new Vector3(18f * -dir, 0f, 0f));
            }
        }

        private static void BuildWindowRecessAndShadowBoxZ(
            Transform parent, string tag, float zMin, float zMax, float yMin, float yMax, float wallCenterX, float exteriorDir, GreyboxPalette pal)
        {
            float dir = Mathf.Sign(exteriorDir);
            float width = zMax - zMin;
            float height = yMax - yMin;
            float zCenter = (zMin + zMax) * 0.5f;
            float yCenter = (yMin + yMax) * 0.5f;
            const float jambT = 0.022f;
            float innerHeight = Mathf.Max(0.10f, height - jambT * 2f);
            float innerWidth = Mathf.Max(0.10f, width - jambT * 2f);

            // Batente real nos 0.16m de espessura da parede
            GameObject jambRoot = CreateContainer(parent, $"Batente_Janela_{tag}");
            CreateBox(jambRoot.transform, $"Batente_Janela_{tag}_Sup", new Vector3(wallCenterX, yMax - jambT * 0.5f, zCenter), new Vector3(T, jambT, width), pal.baseboardWood, hasCollider: false);
            CreateBox(jambRoot.transform, $"Batente_Janela_{tag}_Inf", new Vector3(wallCenterX, yMin + jambT * 0.5f, zCenter), new Vector3(T, jambT, width), pal.baseboardWood, hasCollider: false);
            CreateBox(jambRoot.transform, $"Batente_Janela_{tag}_Min", new Vector3(wallCenterX, yCenter, zMin + jambT * 0.5f), new Vector3(T, innerHeight, jambT), pal.baseboardWood, hasCollider: false);
            CreateBox(jambRoot.transform, $"Batente_Janela_{tag}_Max", new Vector3(wallCenterX, yCenter, zMax - jambT * 0.5f), new Vector3(T, innerHeight, jambT), pal.baseboardWood, hasCollider: false);

            // ShadowBox noturna externa estanque (TwoSided via prefixo ShadowBox_), sem vazamento de luz/céu
            const float cavityDepth = 0.24f;
            const float wallThick = 0.04f;
            const float margin = 0.08f;
            float extFaceX = wallCenterX + dir * (T * 0.5f);
            float cavityMidX = extFaceX + dir * (cavityDepth * 0.5f);
            float backMidX = extFaceX + dir * (cavityDepth + wallThick * 0.5f);

            GameObject shadowRoot = CreateContainer(parent, $"ShadowBox_{tag}");
            CreateBox(shadowRoot.transform, $"ShadowBox_{tag}_Fundo", new Vector3(backMidX, yCenter, zCenter), new Vector3(wallThick, height + wallThick * 2f, width + margin * 2f), pal.shadowBlack, hasCollider: false);
            CreateBox(shadowRoot.transform, $"ShadowBox_{tag}_Sup", new Vector3(cavityMidX, yMax + wallThick * 0.5f, zCenter), new Vector3(cavityDepth, wallThick, width + margin * 2f), pal.shadowBlack, hasCollider: false);
            CreateBox(shadowRoot.transform, $"ShadowBox_{tag}_Inf", new Vector3(cavityMidX, yMin - wallThick * 0.5f, zCenter), new Vector3(cavityDepth, wallThick, width + margin * 2f), pal.shadowBlack, hasCollider: false);
            CreateBox(shadowRoot.transform, $"ShadowBox_{tag}_Min", new Vector3(cavityMidX, yCenter, zMin - margin * 0.5f), new Vector3(cavityDepth, height, margin), pal.shadowBlack, hasCollider: false);
            CreateBox(shadowRoot.transform, $"ShadowBox_{tag}_Max", new Vector3(cavityMidX, yCenter, zMax + margin * 0.5f), new Vector3(cavityDepth, height, margin), pal.shadowBlack, hasCollider: false);

            // Vidro interno da janela (M_Clear_Glass sem colisor/sombra) + Caixilho em profundidade + grade + veneziana
            float glassX = extFaceX + dir * 0.011f;
            CreateDetailBox(shadowRoot.transform, $"Vidro_Janela_{tag}", new Vector3(glassX, yCenter, zCenter), new Vector3(0.004f, innerHeight - 0.004f, innerWidth - 0.004f), pal.details.clearGlass);

            float sashX = extFaceX + dir * 0.025f;
            CreateDetailBox(shadowRoot.transform, $"Caixilho_{tag}_Vert", new Vector3(sashX, yCenter, zCenter), new Vector3(0.020f, innerHeight, 0.032f), pal.woodMahogany);
            float halfLeafW = (innerWidth - 0.032f) * 0.5f;
            CreateDetailBox(shadowRoot.transform, $"Caixilho_{tag}_Horiz_Min", new Vector3(sashX, yCenter, zCenter - 0.016f - halfLeafW * 0.5f), new Vector3(0.016f, 0.024f, halfLeafW), pal.woodMahogany);
            CreateDetailBox(shadowRoot.transform, $"Caixilho_{tag}_Horiz_Max", new Vector3(sashX, yCenter, zCenter + 0.016f + halfLeafW * 0.5f), new Vector3(0.016f, 0.024f, halfLeafW), pal.woodMahogany);

            float barX = extFaceX + dir * 0.075f;
            float railX = extFaceX + dir * 0.092f;
            for (int b = -2; b <= 2; b++)
            {
                float bz = zCenter + b * (innerWidth * 0.18f);
                CreateDetailBox(shadowRoot.transform, $"Grade_Janela_{tag}_V{b + 3}", new Vector3(barX, yCenter, bz), new Vector3(0.014f, innerHeight, 0.014f), pal.metalDark);
            }
            CreateDetailBox(shadowRoot.transform, $"Grade_Janela_{tag}_H_Sup", new Vector3(railX, yCenter + innerHeight * 0.30f, zCenter), new Vector3(0.012f, 0.016f, innerWidth), pal.metalDark);
            CreateDetailBox(shadowRoot.transform, $"Grade_Janela_{tag}_H_Inf", new Vector3(railX, yCenter - innerHeight * 0.30f, zCenter), new Vector3(0.012f, 0.016f, innerWidth), pal.metalDark);

            float louverX = extFaceX + dir * 0.165f;
            for (int v = -3; v <= 3; v++)
            {
                float vy = yCenter + v * (innerHeight * 0.125f);
                CreateDetailBox(shadowRoot.transform, $"Veneziana_Ext_{tag}_{v + 4}", new Vector3(louverX, vy, zCenter), new Vector3(0.012f, 0.042f, innerWidth * 0.94f), pal.shadowBlack, new Vector3(0f, 0f, 18f * dir));
            }
        }

        private static void CreateWallX(Transform parent, string name, float xMin, float xMax, float zCenter, float height, float thickness, Material mat)
        {
            float length = Mathf.Abs(xMax - xMin);
            if (length < 0.01f) return;
            float xCenter = (xMin + xMax) * 0.5f;
            CreateBox(parent, name, new Vector3(xCenter, height * 0.5f, zCenter), new Vector3(length, height, thickness), mat);
        }

        private static void CreateWallZ(Transform parent, string name, float xCenter, float zMin, float zMax, float height, float thickness, Material mat)
        {
            float length = Mathf.Abs(zMax - zMin);
            if (length < 0.01f) return;
            float zCenter = (zMin + zMax) * 0.5f;
            CreateBox(parent, name, new Vector3(xCenter, height * 0.5f, zCenter), new Vector3(thickness, height, length), mat);
        }

        private static void BuildRoomWallBaseboards(
            Transform parent, string roomName, float xMin, float xMax, float zMin, float zMax, Material mat,
            Opening[] southOpenings, Opening[] northOpenings, Opening[] westOpenings, Opening[] eastOpenings)
        {
            const float cornerTrim = 0.014f;
            BuildBaseboardSegmentWithOpeningsZ(parent, $"{roomName}_Oeste", xMin, 1f, zMin, zMax, mat, westOpenings);
            BuildBaseboardSegmentWithOpeningsZ(parent, $"{roomName}_Leste", xMax, -1f, zMin, zMax, mat, eastOpenings);
            BuildBaseboardSegmentWithOpeningsX(parent, $"{roomName}_Sul", xMin + cornerTrim, xMax - cornerTrim, zMin, 1f, mat, southOpenings);
            BuildBaseboardSegmentWithOpeningsX(parent, $"{roomName}_Norte", xMin + cornerTrim, xMax - cornerTrim, zMax, -1f, mat, northOpenings);
        }

        private static void BuildBaseboardSegmentWithOpeningsX(
            Transform parent, string name, float xMin, float xMax, float zWallFace, float zInwardSign, Material mat, params Opening[] openings)
        {
            float cursor = xMin;
            float zCenter = zWallFace + zInwardSign * 0.007f;
            if (openings != null)
            {
                for (int i = 0; i < openings.Length; i++)
                {
                    float segEnd = Mathf.Min(openings[i].a, xMax);
                    if (segEnd - cursor >= 0.02f)
                    {
                        CreateBox(parent, $"{name}_{i}", new Vector3((cursor + segEnd) * 0.5f, 0.050f, zCenter), new Vector3(segEnd - cursor, 0.100f, 0.014f), mat, hasCollider: false);
                    }
                    cursor = Mathf.Max(cursor, openings[i].b);
                }
            }
            if (xMax - cursor >= 0.02f)
            {
                int idx = openings != null ? openings.Length : 0;
                CreateBox(parent, $"{name}_{idx}", new Vector3((cursor + xMax) * 0.5f, 0.050f, zCenter), new Vector3(xMax - cursor, 0.100f, 0.014f), mat, hasCollider: false);
            }
        }

        private static void BuildBaseboardSegmentWithOpeningsZ(
            Transform parent, string name, float xWallFace, float xInwardSign, float zMin, float zMax, Material mat, params Opening[] openings)
        {
            float cursor = zMin;
            float xCenter = xWallFace + xInwardSign * 0.007f;
            if (openings != null)
            {
                for (int i = 0; i < openings.Length; i++)
                {
                    float segEnd = Mathf.Min(openings[i].a, zMax);
                    if (segEnd - cursor >= 0.02f)
                    {
                        CreateBox(parent, $"{name}_{i}", new Vector3(xCenter, 0.050f, (cursor + segEnd) * 0.5f), new Vector3(0.014f, 0.100f, segEnd - cursor), mat, hasCollider: false);
                    }
                    cursor = Mathf.Max(cursor, openings[i].b);
                }
            }
            if (zMax - cursor >= 0.02f)
            {
                int idx = openings != null ? openings.Length : 0;
                CreateBox(parent, $"{name}_{idx}", new Vector3(xCenter, 0.050f, (cursor + zMax) * 0.5f), new Vector3(0.014f, 0.100f, zMax - cursor), mat, hasCollider: false);
            }
        }

        private static void BuildCorridorLCrownMoulding(Transform parent, string name, float ceilingY, Material mat)
        {
            GameObject grp = CreateContainer(parent, name);
            float y = ceilingY - 0.027f;
            // Cada peça acompanha estritamente uma parede real sem cruzar vãos vazios e com 0 sobreposição de quina
            CreateDetailBox(grp.transform, "Sanca_Oeste", new Vector3(-6.52f, y, 1.28f), new Vector3(0.04f, 0.056f, 1.40f), mat);
            CreateDetailBox(grp.transform, "Sanca_Sul", new Vector3(-2.01f, y, 0.56f), new Vector3(9.06f, 0.056f, 0.04f), mat);
            CreateDetailBox(grp.transform, "Sanca_Norte_Main", new Vector3(-2.75f, y, 2.00f), new Vector3(7.58f, 0.056f, 0.04f), mat);
            CreateDetailBox(grp.transform, "Sanca_Ala_Oeste", new Vector3(1.06f, y, 4.01f), new Vector3(0.04f, 0.056f, 3.98f), mat);
            CreateDetailBox(grp.transform, "Sanca_Ala_Norte", new Vector3(1.78f, y, 6.02f), new Vector3(1.48f, 0.056f, 0.04f), mat);
            CreateDetailBox(grp.transform, "Sanca_Ala_Leste", new Vector3(2.50f, y, 3.29f), new Vector3(0.04f, 0.056f, 5.42f), mat);
        }

        private static void CreateLintelX(Transform parent, string name, float xMin, float xMax, float zCenter, float bottomY, float topY, float thickness, Material mat)
        {
            float h = topY - bottomY;
            CreateBox(parent, name, new Vector3((xMin + xMax) * 0.5f, bottomY + h * 0.5f, zCenter), new Vector3(Mathf.Abs(xMax - xMin), h, thickness), mat);
        }

        private static void CreateLintelZ(Transform parent, string name, float xCenter, float zMin, float zMax, float bottomY, float topY, float thickness, Material mat)
        {
            float h = topY - bottomY;
            CreateBox(parent, name, new Vector3(xCenter, bottomY + h * 0.5f, (zMin + zMax) * 0.5f), new Vector3(thickness, h, Mathf.Abs(zMax - zMin)), mat);
        }

        private static void BuildOpenFrameTrimBarsRotatedZ(
            Transform parent, string prefix, Vector3 center, float widthZ, float heightY, float barWidth, float depthX, Material mat)
        {
            float innerH = Mathf.Max(0.04f, heightY - barWidth * 2f);
            CreateDetailBox(parent, $"{prefix}_Sup", new Vector3(center.x, center.y + (heightY - barWidth) * 0.5f, center.z), new Vector3(depthX, barWidth, widthZ), mat);
            CreateDetailBox(parent, $"{prefix}_Inf", new Vector3(center.x, center.y - (heightY - barWidth) * 0.5f, center.z), new Vector3(depthX, barWidth, widthZ), mat);
            CreateDetailBox(parent, $"{prefix}_Sul", new Vector3(center.x, center.y, center.z - (widthZ - barWidth) * 0.5f), new Vector3(depthX, innerH, barWidth), mat);
            CreateDetailBox(parent, $"{prefix}_Norte", new Vector3(center.x, center.y, center.z + (widthZ - barWidth) * 0.5f), new Vector3(depthX, innerH, barWidth), mat);
        }

        private static void BuildDoorFrameX(
            Transform parent, string name, float xMin, float xMax, float zCenter, float doorH, Material mat,
            Material brassMat = null, float swingDirSign = 1f, bool latchOnMaxSide = true, bool isArchway = false,
            float minSideTrimWidth = 0.044f, float maxSideTrimWidth = 0.044f)
        {
            const float jambT = 0.040f;
            float jambH = doorH - jambT;
            float innerW = Mathf.Abs(xMax - xMin) - jambT * 2f;
            float xCenter = (xMin + xMax) * 0.5f;

            CreateBox(parent, $"{name}_Esq", new Vector3(xMin + jambT * 0.5f, jambH * 0.5f, zCenter), new Vector3(jambT, jambH, 0.184f), mat, hasCollider: false);
            CreateBox(parent, $"{name}_Dir", new Vector3(xMax - jambT * 0.5f, jambH * 0.5f, zCenter), new Vector3(jambT, jambH, 0.184f), mat, hasCollider: false);
            CreateBox(parent, $"{name}_Sup", new Vector3(xCenter, doorH - jambT * 0.5f, zCenter), new Vector3(Mathf.Abs(xMax - xMin), jambT, 0.184f), mat, hasCollider: false);

            // Guarnições em ambas as faces da parede acima do rodapé (Y >= 0.101m, zero Z-fighting)
            float trimBot = 0.101f;
            float trimH = doorH - trimBot;
            float trimCenterY = trimBot + trimH * 0.5f;
            float headerW = Mathf.Abs(xMax - xMin) + minSideTrimWidth + maxSideTrimWidth;
            float headerCenterX = ((xMin - minSideTrimWidth) + (xMax + maxSideTrimWidth)) * 0.5f;
            float[] wallFaces = { -1f, 1f };
            for (int f = 0; f < wallFaces.Length; f++)
            {
                string side = f == 0 ? "Sul" : "Norte";
                float zTrim = zCenter + wallFaces[f] * (T * 0.5f + 0.008f);
                CreateDetailBox(parent, $"{name}_Guarnicao_{side}_Esq", new Vector3(xMin - minSideTrimWidth * 0.5f, trimCenterY, zTrim), new Vector3(minSideTrimWidth, trimH, 0.016f), mat);
                CreateDetailBox(parent, $"{name}_Guarnicao_{side}_Dir", new Vector3(xMax + maxSideTrimWidth * 0.5f, trimCenterY, zTrim), new Vector3(maxSideTrimWidth, trimH, 0.016f), mat);
                CreateDetailBox(parent, $"{name}_Guarnicao_{side}_Sup", new Vector3(headerCenterX, doorH + 0.022f, zTrim), new Vector3(headerW, 0.044f, 0.016f), mat);
            }

            if (!isArchway)
            {
                // Rebaixo batente da folha no lado oposto ao giro + contra-testa em latão na fechadura
                float stopZ = zCenter - Mathf.Sign(swingDirSign) * 0.0325f;
                float stopBottom = 0.006f;
                float stopH = jambH - stopBottom;
                CreateDetailBox(parent, $"{name}_Rebaixo_Esq", new Vector3(xMin + jambT + 0.006f, stopBottom + stopH * 0.5f, stopZ), new Vector3(0.012f, stopH, 0.014f), mat);
                CreateDetailBox(parent, $"{name}_Rebaixo_Dir", new Vector3(xMax - jambT - 0.006f, stopBottom + stopH * 0.5f, stopZ), new Vector3(0.012f, stopH, 0.014f), mat);
                CreateDetailBox(parent, $"{name}_Rebaixo_Sup", new Vector3(xCenter, jambH - 0.006f, stopZ), new Vector3(innerW - 0.024f, 0.012f, 0.014f), mat);

                if (brassMat != null)
                {
                    float strikeX = latchOnMaxSide ? (xMax - jambT - 0.001f) : (xMin + jambT + 0.001f);
                    CreateDetailBox(parent, $"{name}_ContraTesta", new Vector3(strikeX, 1.00f, zCenter), new Vector3(0.002f, 0.145f, 0.028f), brassMat);
                }
            }
        }

        private static void BuildDoorFrameZ(
            Transform parent, string name, float xCenter, float zMin, float zMax, float doorH, Material mat,
            Material brassMat = null, float swingDirSign = 1f, bool latchOnMaxSide = true, bool isArchway = false,
            float minSideTrimWidth = 0.044f, float maxSideTrimWidth = 0.044f)
        {
            const float jambT = 0.040f;
            float jambH = doorH - jambT;
            float innerW = Mathf.Abs(zMax - zMin) - jambT * 2f;
            float zCenter = (zMin + zMax) * 0.5f;

            CreateBox(parent, $"{name}_Sul", new Vector3(xCenter, jambH * 0.5f, zMin + jambT * 0.5f), new Vector3(0.184f, jambH, jambT), mat, hasCollider: false);
            CreateBox(parent, $"{name}_Norte", new Vector3(xCenter, jambH * 0.5f, zMax - jambT * 0.5f), new Vector3(0.184f, jambH, jambT), mat, hasCollider: false);
            CreateBox(parent, $"{name}_Sup", new Vector3(xCenter, doorH - jambT * 0.5f, zCenter), new Vector3(0.184f, jambT, Mathf.Abs(zMax - zMin)), mat, hasCollider: false);

            float trimBot = 0.101f;
            float trimH = doorH - trimBot;
            float trimCenterY = trimBot + trimH * 0.5f;
            float headerW = Mathf.Abs(zMax - zMin) + minSideTrimWidth + maxSideTrimWidth;
            float headerCenterZ = ((zMin - minSideTrimWidth) + (zMax + maxSideTrimWidth)) * 0.5f;
            float[] wallFaces = { -1f, 1f };
            for (int f = 0; f < wallFaces.Length; f++)
            {
                string side = f == 0 ? "Oeste" : "Leste";
                float xTrim = xCenter + wallFaces[f] * (T * 0.5f + 0.008f);
                CreateDetailBox(parent, $"{name}_Guarnicao_{side}_Sul", new Vector3(xTrim, trimCenterY, zMin - minSideTrimWidth * 0.5f), new Vector3(0.016f, trimH, minSideTrimWidth), mat);
                CreateDetailBox(parent, $"{name}_Guarnicao_{side}_Norte", new Vector3(xTrim, trimCenterY, zMax + maxSideTrimWidth * 0.5f), new Vector3(0.016f, trimH, maxSideTrimWidth), mat);
                CreateDetailBox(parent, $"{name}_Guarnicao_{side}_Sup", new Vector3(xTrim, doorH + 0.022f, headerCenterZ), new Vector3(0.016f, 0.044f, headerW), mat);
            }

            if (!isArchway)
            {
                float stopX = xCenter - Mathf.Sign(swingDirSign) * 0.0325f;
                float stopBottom = 0.006f;
                float stopH = jambH - stopBottom;
                CreateDetailBox(parent, $"{name}_Rebaixo_Sul", new Vector3(stopX, stopBottom + stopH * 0.5f, zMin + jambT + 0.006f), new Vector3(0.014f, stopH, 0.012f), mat);
                CreateDetailBox(parent, $"{name}_Rebaixo_Norte", new Vector3(stopX, stopBottom + stopH * 0.5f, zMax - jambT - 0.006f), new Vector3(0.014f, stopH, 0.012f), mat);
                CreateDetailBox(parent, $"{name}_Rebaixo_Sup", new Vector3(stopX, jambH - 0.006f, zCenter), new Vector3(0.014f, 0.012f, innerW - 0.024f), mat);

                if (brassMat != null)
                {
                    float strikeZ = latchOnMaxSide ? (zMax - jambT - 0.001f) : (zMin + jambT + 0.001f);
                    CreateDetailBox(parent, $"{name}_ContraTesta", new Vector3(xCenter, 1.00f, strikeZ), new Vector3(0.028f, 0.145f, 0.002f), brassMat);
                }
            }
        }

        private static void BuildFlushExteriorDoorFrameZ(
            Transform parent, string name, float xMinFlush, float xMax, float zMin, float zMax, float doorH, Material mat, Material brassMat = null)
        {
            const float jambT = 0.040f;
            float jambH = doorH - jambT;
            float widthX = Mathf.Abs(xMax - xMinFlush);
            float xCenter = (xMinFlush + xMax) * 0.5f;
            float zCenter = (zMin + zMax) * 0.5f;

            CreateBox(parent, $"{name}_Sul", new Vector3(xCenter, jambH * 0.5f, zMin + jambT * 0.5f), new Vector3(widthX, jambH, jambT), mat, hasCollider: false);
            CreateBox(parent, $"{name}_Norte", new Vector3(xCenter, jambH * 0.5f, zMax - jambT * 0.5f), new Vector3(widthX, jambH, jambT), mat, hasCollider: false);
            CreateBox(parent, $"{name}_Sup", new Vector3(xCenter, doorH - jambT * 0.5f, zCenter), new Vector3(widthX, jambT, Mathf.Abs(zMax - zMin)), mat, hasCollider: false);

            // Guarnição apenas na face interna (X >= -6.540m) para preservar o envelope externo em X=-6.700m
            const float trimW = 0.042f;
            float trimBot = 0.101f;
            float trimH = doorH - trimBot;
            float xTrim = -6.540f + 0.008f;
            CreateDetailBox(parent, $"{name}_Guarnicao_Int_Sul", new Vector3(xTrim, trimBot + trimH * 0.5f, zMin - trimW * 0.5f), new Vector3(0.016f, trimH, trimW), mat);
            CreateDetailBox(parent, $"{name}_Guarnicao_Int_Norte", new Vector3(xTrim, trimBot + trimH * 0.5f, zMax + trimW * 0.5f), new Vector3(0.016f, trimH, trimW), mat);
            CreateDetailBox(parent, $"{name}_Guarnicao_Int_Sup", new Vector3(xTrim, doorH + 0.021f, zCenter), new Vector3(0.016f, 0.042f, Mathf.Abs(zMax - zMin) + trimW * 2f), mat);

            // Rebaixo no lado externo (-X) atrás da folha em X=-6.614 ± 0.024 -> [-6.654..-6.639]
            float stopX = -6.647f;
            float stopBottom = 0.006f;
            float stopH = jambH - stopBottom;
            CreateDetailBox(parent, $"{name}_Rebaixo_Sul", new Vector3(stopX, stopBottom + stopH * 0.5f, zMin + jambT + 0.006f), new Vector3(0.014f, stopH, 0.012f), mat);
            CreateDetailBox(parent, $"{name}_Rebaixo_Norte", new Vector3(stopX, stopBottom + stopH * 0.5f, zMax - jambT - 0.006f), new Vector3(0.014f, stopH, 0.012f), mat);
            CreateDetailBox(parent, $"{name}_Rebaixo_Sup", new Vector3(stopX, jambH - 0.006f, zCenter), new Vector3(0.014f, 0.012f, Mathf.Abs(zMax - zMin) - jambT * 2f - 0.024f), mat);
            if (brassMat != null)
            {
                CreateDetailBox(parent, $"{name}_ContraTesta", new Vector3(-6.614f, 1.00f, zMax - jambT - 0.001f), new Vector3(0.028f, 0.145f, 0.002f), brassMat);
            }
        }

        private static DoorInteractable CreateFunctionalDoor(
            Transform parent, string name, Vector3 hingePos, float closedYaw, float openAngle, bool locked, bool allowSafeKey, GreyboxPalette pal, float leafWidth = 0.80f)
        {
            float leafHeight = DoorH - 0.050f; // 6mm folga piso (3mm livres acima da soleira de 3mm) + 4mm abaixo do batente superior
            float leafCenterY = 0.006f + leafHeight * 0.5f;

            GameObject pivotObj = CreateContainer(parent, name);
            pivotObj.transform.localPosition = hingePos;
            pivotObj.transform.localRotation = Quaternion.Euler(0f, closedYaw, 0f);

            GameObject leaf = CreateBox(pivotObj.transform, "Folha_Porta",
                new Vector3(leafWidth * 0.5f, leafCenterY, 0f),
                new Vector3(leafWidth, leafHeight, 0.048f),
                pal.doorWood, hasCollider: true);

            if (leaf.TryGetComponent<BoxCollider>(out BoxCollider leafCol))
            {
                leafCol.size = new Vector3(1.02f, 1.00f, 2.2f);
            }

            DecoratePanelDoorLeaf(pivotObj.transform, pal, includeKeyhole: true, leafWidth: leafWidth, leafHeight: leafHeight, leafCenterY: leafCenterY, leafThickness: 0.048f);

            DoorInteractable door = GetOrAdd<DoorInteractable>(pivotObj);
            door.ConfigureDoor(pivotObj.transform, openAngle, locked, allowSafeKey, doorLeafWidth: leafWidth);
            EditorUtility.SetDirty(door);
            return door;
        }

        private static Light CreateCeilingLamp(
            Transform parent,
            string name,
            Vector3 pos,
            float range,
            GreyboxPalette pal,
            bool castShadows = true,
            float spotAngle = 136f,
            float innerSpotAngle = 64f,
            int shadowTier = 2)
        {
            GameObject root = CreateContainer(parent, name);
            root.transform.localPosition = pos;

            CreateDetailCylinder(root.transform, "Canopla_Teto", new Vector3(0f, 0.119f, 0f), new Vector3(0.36f, 0.008f, 0.36f), pal.metalBrass, castShadows: false);
            CreateDetailCylinder(root.transform, "Anel_Latao_Plafon", new Vector3(0f, 0.104f, 0f), new Vector3(0.38f, 0.007f, 0.38f), pal.metalBrass, castShadows: false);
            for (int cl = 0; cl < 3; cl++)
            {
                float a = cl * 120f * Mathf.Deg2Rad;
                CreateDetailBox(root.transform, $"Presilha_Plafon_{cl}",
                    new Vector3(Mathf.Cos(a) * 0.175f, 0.088f, Mathf.Sin(a) * 0.175f),
                    new Vector3(0.014f, 0.022f, 0.018f), pal.metalBrass,
                    new Vector3(0f, -cl * 120f, 0f), castShadows: false);
            }
            CreateDetailCylinder(root.transform, "Pinaculo_Base_Plafon", new Vector3(0f, 0.012f, 0f), new Vector3(0.040f, 0.004f, 0.040f), pal.metalBrass, castShadows: false);
            CreateDetailCylinder(root.transform, "Pinaculo_Plafon", new Vector3(0f, 0.005f, 0f), new Vector3(0.028f, 0.006f, 0.028f), pal.metalBrass, castShadows: false);

            GameObject onState = CreateContainer(root.transform, "Estado_Ligado");
            CreateDetailCylinder(onState.transform, "Plafon_Teto", new Vector3(0f, 0.100f, 0f), new Vector3(0.33f, 0.004f, 0.33f), pal.lampBulbCore, castShadows: false);
            CreateDetailSphere(onState.transform, "Cupula_Fosca", new Vector3(0f, 0.059f, 0f), new Vector3(0.32f, 0.092f, 0.32f), pal.lampEmissive, castShadows: false);
            CreateDetailSphere(onState.transform, "Bulbo_Interno", new Vector3(0f, 0.056f, 0f), new Vector3(0.11f, 0.050f, 0.11f), pal.lampBulbCore, castShadows: false);

            GameObject haloObj = new GameObject($"{name}_HaloLight");
            haloObj.transform.SetParent(onState.transform, false);
            haloObj.transform.localPosition = new Vector3(0f, 0.022f, 0f);
            haloObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Light halo = haloObj.AddComponent<Light>();
            halo.type = LightType.Spot;
            halo.range = 2.6f;
            halo.spotAngle = 154f;
            halo.innerSpotAngle = 72f;
            halo.intensity = 0.32f;
            halo.color = new Color(1.00f, 0.85f, 0.64f);
            halo.shadows = LightShadows.None;

            GameObject offState = CreateContainer(root.transform, "Estado_Desligado");
            CreateDetailCylinder(offState.transform, "Plafon_Teto_Apagado", new Vector3(0f, 0.100f, 0f), new Vector3(0.33f, 0.004f, 0.33f), pal.metalDark, castShadows: false);
            CreateDetailSphere(offState.transform, "Cupula_Fosca_Apagada", new Vector3(0f, 0.059f, 0f), new Vector3(0.32f, 0.092f, 0.32f), pal.lampUnlitGlass, castShadows: false);
            offState.SetActive(false);

            GameObject lightObj = new GameObject($"{name}_SpotLight");
            lightObj.transform.SetParent(root.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.036f, 0f);
            lightObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = range;
            l.spotAngle = spotAngle;
            l.innerSpotAngle = innerSpotAngle;
            l.intensity = 1.28f;
            l.color = new Color(1.00f, 0.85f, 0.64f);
            l.shadows = castShadows ? LightShadows.Soft : LightShadows.None;
            l.shadowNearPlane = 0.08f;
            l.shadowBias = 0.02f;
            l.shadowNormalBias = 0.25f;
            ConfigureLightURPShadowTier(l, shadowTier);
            return l;
        }

        private static GameObject CreateFramedPicture(
            Transform parent, string name, Vector3 pos, Quaternion rot, Material frameMat, Material canvasMat, float scaleMultiplier = 1f, bool includeWallNail = true, bool glassCracked = false)
        {
            GameObject root = CreateContainer(parent, name);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            float w = 0.50f * scaleMultiplier;
            float h = 0.62f * scaleMultiplier;

            float bar = 0.045f * scaleMultiplier;
            float innerW = w - bar * 2f;
            float innerH = h - bar * 2f;
            float fillet = 0.008f * scaleMultiplier;
            Material paperMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/M_Paper_White.mat") ?? frameMat;
            Material brassMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/M_Metal_Brass.mat") ?? frameMat;
            Material darkMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/M_Shadow_Black.mat") ?? frameMat;
            Material glassMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/Details/M_Clear_Glass.mat");

            CreateDetailBox(root.transform, "Moldura_Sup", new Vector3(0f, h * 0.5f - bar * 0.5f, 0f), new Vector3(w, bar, 0.034f), frameMat, castShadows: true);
            CreateDetailBox(root.transform, "Moldura_Inf", new Vector3(0f, -h * 0.5f + bar * 0.5f, 0f), new Vector3(w, bar, 0.034f), frameMat);
            CreateDetailBox(root.transform, "Moldura_Esq", new Vector3(-w * 0.5f + bar * 0.5f, 0f, 0f), new Vector3(bar, innerH, 0.034f), frameMat);
            CreateDetailBox(root.transform, "Moldura_Dir", new Vector3(w * 0.5f - bar * 0.5f, 0f, 0f), new Vector3(bar, innerH, 0.034f), frameMat);
            float pinX = w * 0.5f - bar * 0.5f;
            float pinY = h * 0.5f - bar * 0.5f;
            CreateDetailCylinder(root.transform, "Cantoneira_Quadro_NO", new Vector3(-pinX,  pinY, 0.0185f), new Vector3(0.010f * scaleMultiplier, 0.0015f, 0.010f * scaleMultiplier), brassMat, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(root.transform, "Cantoneira_Quadro_NL", new Vector3( pinX,  pinY, 0.0185f), new Vector3(0.010f * scaleMultiplier, 0.0015f, 0.010f * scaleMultiplier), brassMat, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(root.transform, "Cantoneira_Quadro_SO", new Vector3(-pinX, -pinY, 0.0185f), new Vector3(0.010f * scaleMultiplier, 0.0015f, 0.010f * scaleMultiplier), brassMat, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(root.transform, "Cantoneira_Quadro_SL", new Vector3( pinX, -pinY, 0.0185f), new Vector3(0.010f * scaleMultiplier, 0.0015f, 0.010f * scaleMultiplier), brassMat, new Vector3(90f, 0f, 0f));

            // Filete interno recuado emoldurando o passe-partout sem Z-fighting (Z=[0.003..0.013])
            CreateDetailBox(root.transform, "Filete_Sup", new Vector3(0f, innerH * 0.5f - fillet * 0.5f, 0.008f), new Vector3(innerW, fillet, 0.010f), brassMat);
            CreateDetailBox(root.transform, "Filete_Inf", new Vector3(0f, -innerH * 0.5f + fillet * 0.5f, 0.008f), new Vector3(innerW, fillet, 0.010f), brassMat);
            CreateDetailBox(root.transform, "Filete_Esq", new Vector3(-innerW * 0.5f + fillet * 0.5f, 0f, 0.008f), new Vector3(fillet, innerH - fillet * 2f, 0.010f), brassMat);
            CreateDetailBox(root.transform, "Filete_Dir", new Vector3(innerW * 0.5f - fillet * 0.5f, 0f, 0.008f), new Vector3(fillet, innerH - fillet * 2f, 0.010f), brassMat);

            CreateDetailBox(root.transform, "Passe_Partout", new Vector3(0f, 0f, -0.001f), new Vector3(innerW - fillet * 2f, innerH - fillet * 2f, 0.006f), paperMat);
            CreateBox(root.transform, "Tela_Retrato", new Vector3(0f, 0f, 0.0045f), new Vector3(w * 0.76f, h * 0.76f, 0.003f), paperMat, hasCollider: false);
            CreatePrintedOverlayQuad(root.transform, "Arte_Quadro", new Vector3(0f, 0f, 0.0070f), new Vector2(w * 0.755f, h * 0.755f), canvasMat, new Vector3(0f, 180f, 0f));

            if (glassMat != null)
            {
                CreateDetailBox(root.transform, "Vidro_Quadro", new Vector3(0f, 0f, 0.0105f), new Vector3(innerW - fillet * 2f, innerH - fillet * 2f, 0.0015f), glassMat);
            }
            if (glassCracked)
            {
                CreateDetailBox(root.transform, "Fissura_Vidro_1", new Vector3(-0.04f * scaleMultiplier, 0.03f * scaleMultiplier, 0.0124f), new Vector3(0.0028f, 0.34f * scaleMultiplier, 0.0014f), darkMat, new Vector3(0f, 0f, -29f));
                CreateDetailBox(root.transform, "Fissura_Vidro_2", new Vector3(0.03f * scaleMultiplier, -0.05f * scaleMultiplier, 0.0124f), new Vector3(0.0026f, 0.28f * scaleMultiplier, 0.0014f), darkMat, new Vector3(0f, 0f, 44f));
                CreateDetailBox(root.transform, "Fissura_Vidro_3", new Vector3(-0.01f * scaleMultiplier, 0.07f * scaleMultiplier, 0.0124f), new Vector3(0.0022f, 0.19f * scaleMultiplier, 0.0014f), darkMat, new Vector3(0f, 0f, 72f));
            }

            CreateDetailBox(root.transform, "Arame_Suspensao_Esq", new Vector3(-w * 0.09f, h * 0.5f + 0.004f, -0.0120f), new Vector3(0.003f, 0.034f, 0.004f), frameMat, new Vector3(0f, 0f, -48f));
            CreateDetailBox(root.transform, "Arame_Suspensao_Dir", new Vector3(w * 0.09f, h * 0.5f + 0.004f, -0.0120f), new Vector3(0.003f, 0.034f, 0.004f), frameMat, new Vector3(0f, 0f, 48f));
            if (includeWallNail)
            {
                CreateDetailCylinder(root.transform, "Prego_Suspensao", new Vector3(0f, h * 0.5f + 0.012f, -0.011f), new Vector3(0.010f, 0.012f, 0.010f), frameMat, new Vector3(90f, 0f, 0f));
            }
            return root;
        }

        private static void BuildChildBed(Transform parent, string name, Vector3 pos, Material blanketMat, GreyboxPalette pal)
        {
            GameObject root = CreateContainer(parent, name);
            root.transform.localPosition = pos;

            // Colunas esculpidas com pináculos torneados aterradas exatamente em Y=0 + painéis estruturais disjuntos
            CreateBox(root.transform, "Coluna_Cab_S", new Vector3(0.88f, 0.46f, -0.45f), new Vector3(0.072f, 0.92f, 0.072f), pal.woodMahogany);
            CreateBox(root.transform, "Coluna_Cab_N", new Vector3(0.88f, 0.46f,  0.45f), new Vector3(0.072f, 0.92f, 0.072f), pal.woodMahogany);
            CreateBox(root.transform, "Coluna_Pes_S", new Vector3(-0.88f, 0.31f, -0.45f), new Vector3(0.068f, 0.62f, 0.068f), pal.woodMahogany);
            CreateBox(root.transform, "Coluna_Pes_N", new Vector3(-0.88f, 0.31f,  0.45f), new Vector3(0.068f, 0.62f, 0.068f), pal.woodMahogany);

            Mesh childFinialMesh = CreateFacetedLatheVesselMesh("Mesh_Pinaculo_Cama_Infantil", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.024f),
                (0.012f, 0.016f),
                (0.032f, 0.028f),
                (0.052f, 0.022f),
                (0.062f, 0.000f)
            });
            CreateMeshObject(root.transform, "Pinaculo_Cab_S", childFinialMesh, pal.woodMahogany, new Vector3( 0.88f, 0.920f, -0.45f), Vector3.zero, castShadows: true);
            CreateMeshObject(root.transform, "Pinaculo_Cab_N", childFinialMesh, pal.woodMahogany, new Vector3( 0.88f, 0.920f,  0.45f), Vector3.zero, castShadows: true);
            CreateMeshObject(root.transform, "Pinaculo_Pes_S", childFinialMesh, pal.woodMahogany, new Vector3(-0.88f, 0.620f, -0.45f), Vector3.zero, castShadows: true);
            CreateMeshObject(root.transform, "Pinaculo_Pes_N", childFinialMesh, pal.woodMahogany, new Vector3(-0.88f, 0.620f,  0.45f), Vector3.zero, castShadows: true);

            CreateBox(root.transform, "Cabeceira_Leste", new Vector3(0.88f, 0.50f, 0f), new Vector3(0.056f, 0.72f, 0.824f), pal.furnitureWood);
            CreateBox(root.transform, "Peseira_Oeste", new Vector3(-0.88f, 0.35f, 0f), new Vector3(0.052f, 0.44f, 0.824f), pal.furnitureWood);
            CreateBox(root.transform, "Estrutura_Madeira", new Vector3(0f, 0.22f, 0f), new Vector3(1.685f, 0.22f, 0.90f), pal.furnitureWood);

            for (int r = -3; r <= 3; r++)
            {
                CreateDetailBox(root.transform, $"Ripa_Cabeceira_{r + 4}", new Vector3(0.842f, 0.61f, r * 0.115f), new Vector3(0.018f, 0.46f, 0.028f), pal.woodMahogany);
                CreateDetailBox(root.transform, $"Ripa_Peseira_{r + 4}", new Vector3(-0.844f, 0.43f, r * 0.115f), new Vector3(0.018f, 0.22f, 0.028f), pal.woodMahogany);
            }
            Mesh headRailMesh = CreateChamferedRectLoftMesh("Mesh_Travessa_Cab_Infantil", 0.092f, 0.048f, 0.824f, 0.010f, 0.92f, 0.008f);
            CreateMeshObject(root.transform, "Travessa_Cabeceira_Sup", headRailMesh, pal.woodMahogany, new Vector3(0.88f, 0.860f, 0f), castShadows: true);
            Mesh footRailMesh = CreateChamferedRectLoftMesh("Mesh_Travessa_Pes_Infantil", 0.084f, 0.040f, 0.824f, 0.010f, 0.92f, 0.006f);
            CreateMeshObject(root.transform, "Travessa_Peseira_Sup", footRailMesh, pal.woodMahogany, new Vector3(-0.88f, 0.568f, 0f), castShadows: true);

            // Colchão chanfrado (topo em Y=0.470f)
            Mesh childMattressMesh = CreateChamferedRectLoftMesh("Mesh_Colchao_Infantil", 1.66f, 0.14f, 0.84f, 0.042f, 0.95f, 0.022f);
            GameObject mattress = CreateMeshObject(root.transform, "Colchao_Infantil", childMattressMesh, pal.formicaCream, new Vector3(-0.005f, 0.330f, 0f), castShadows: true);

            // Coberta contínua anatômica abraçando o colchão e caindo suavemente nas laterais Norte/Sul
            const int cbU = 8;
            const int cbV = 10;
            var cbOut = new Vector3[cbU + 1, cbV + 1];
            var cbIn = new Vector3[cbU + 1, cbV + 1];
            float[] profZ = { -0.436f, -0.434f, -0.426f, -0.398f, -0.260f, 0.000f, 0.260f, 0.398f, 0.426f, 0.434f, 0.436f };
            float[] profY = {  0.318f,  0.395f,  0.456f,  0.472f,  0.473f, 0.474f, 0.473f, 0.472f, 0.456f, 0.395f, 0.318f };
            Vector3[] profN =
            {
                Vector3.back,
                Vector3.back,
                new Vector3(0f, 0.55f, -0.83f).normalized,
                new Vector3(0f, 0.90f, -0.44f).normalized,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                new Vector3(0f, 0.90f,  0.44f).normalized,
                new Vector3(0f, 0.55f,  0.83f).normalized,
                Vector3.forward,
                Vector3.forward
            };
            for (int i = 0; i <= cbU; i++)
            {
                float u = (float)i / cbU;
                float x = Mathf.Lerp(-0.79f, 0.36f, u);
                for (int j = 0; j <= cbV; j++)
                {
                    float v = (float)j / cbV;
                    float ripple = (j >= 3 && j <= 7) ? 0.0045f * Mathf.Sin(u * Mathf.PI * 3.0f + v * 2.4f) : 0f;
                    float hemWave = (j == 0 || j == cbV) ? 0.006f * Mathf.Sin(u * Mathf.PI * 3.0f) : 0f;
                    Vector3 basePt = new Vector3(x, profY[j] + ripple + hemWave, profZ[j]);
                    float thick = (j == 0 || j == cbV) ? 0.011f : 0.017f;
                    cbIn[i, j] = basePt;
                    cbOut[i, j] = basePt + profN[j] * thick;
                }
            }
            Mesh childBlanketMesh = CreateClosedThickGridShellMesh("Mesh_Coberta_Infantil", cbOut, cbIn);
            CreateMeshObject(root.transform, "Cobertor_Colorido", childBlanketMesh, blanketMat, Vector3.zero, castShadows: true);

            Mesh foldMesh = CreateChamferedRectLoftMesh("Mesh_Dobra_Lencol_Infantil", 0.11f, 0.044f, 0.86f, 0.018f, 0.82f, 0.012f);
            CreateMeshObject(root.transform, "Dobra_Cobertor", foldMesh, pal.formicaCream, new Vector3(0.39f, 0.464f, 0f), new Vector3(0f, 0f, 5f), castShadows: true);

            Mesh quiltFootMesh = CreateChamferedRectLoftMesh("Mesh_Manta_Pe_Infantil", 0.24f, 0.042f, 0.86f, 0.018f, 0.88f, 0.012f);
            CreateMeshObject(root.transform, "Manta_Dobrada_Pe", quiltFootMesh, pal.fabricAsh, new Vector3(-0.62f, 0.486f, 0f), Vector3.zero, castShadows: true);

            Mesh childPillowMesh = CreateChamferedRectLoftMesh("Mesh_Travesseiro_Infantil", 0.30f, 0.092f, 0.56f, 0.045f, 0.72f, 0.030f);
            GameObject pillow = CreateMeshObject(root.transform, "Travesseiro", childPillowMesh, pal.paperWhite, new Vector3(0.64f, 0.468f, 0f), Vector3.zero, castShadows: true);
            SnapToSupportTop(pillow, mattress);
            GameObject foldedSheetA = CreateDetailBox(root.transform, "Lencol_Dobrado_Base", new Vector3(0.62f, 0.572f, 0.07f), new Vector3(0.17f, 0.024f, 0.16f), pal.formicaCream, new Vector3(0f, 11f, 0f));
            SnapToSupportTop(foldedSheetA, pillow);
            GameObject foldedSheetB = CreateDetailBox(root.transform, "Lencol_Dobrado_Topo", new Vector3(0.62f, 0.596f, 0.07f), new Vector3(0.15f, 0.018f, 0.14f), pal.fabricAsh, new Vector3(0f, -8f, 0f));
            SnapToSupportTop(foldedSheetB, foldedSheetA);
        }

        private static GameObject CreateTriggerZone(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            BoxCollider col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            return go;
        }

        private static GameObject CreateBox(
            Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat, bool hasCollider = true, Vector3 localEuler = default)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            if (!hasCollider && go.TryGetComponent<Collider>(out Collider col))
            {
                Object.DestroyImmediate(col);
            }
            AssignMaterial(go, mat);
            return go;
        }

        private static GameObject CreateCylinder(
            Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat, bool hasCollider = false, Vector3 localEuler = default)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = GetOrBuildLowPolyCylinder8Mesh();
            go.AddComponent<MeshRenderer>();
            if (hasCollider)
            {
                CapsuleCollider col = go.AddComponent<CapsuleCollider>();
                col.center = Vector3.zero;
                col.radius = 0.5f;
                col.height = 2.0f;
                col.direction = 1;
            }
            AssignMaterial(go, mat);
            return go;
        }

        private static GameObject GetOrCreateChildPrimitive(Transform parent, string name, PrimitiveType type)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Transform EnsureSpawnPoint(Transform parent, string name, Vector3 pos, Quaternion rot)
        {
            Transform tr = parent.Find(name);
            if (tr == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(parent, false);
                tr = go.transform;
            }
            tr.localPosition = pos;
            tr.localRotation = rot;
            tr.localScale = Vector3.one;
            return tr;
        }

        private static GameObject CreateContainer(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ResetRootTransform(go.transform);
            return go;
        }

        private static GameObject GetOrCreateRoot(Scene scene, string exactName)
        {
            GameObject found = FindInScene(scene, exactName);
            if (found == null)
            {
                found = new GameObject(exactName);
            }
            found.transform.SetParent(null);
            ResetRootTransform(found.transform);
            return found;
        }

        private static GameObject FindInScene(Scene scene, string exactName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform match = FindRecursive(roots[i].transform, exactName, ignoreCase: false);
                if (match != null) return match.gameObject;
            }
            return null;
        }

        private static GameObject FindInSceneCaseInsensitive(Scene scene, string targetName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform match = FindRecursive(roots[i].transform, targetName, ignoreCase: true);
                if (match != null) return match.gameObject;
            }
            return null;
        }

        private static Transform FindRecursive(Transform current, string targetName, bool ignoreCase)
        {
            bool matches = ignoreCase
                ? string.Equals(current.name, targetName, System.StringComparison.OrdinalIgnoreCase)
                : current.name == targetName;

            if (matches) return current;

            for (int i = 0; i < current.childCount; i++)
            {
                Transform childMatch = FindRecursive(current.GetChild(i), targetName, ignoreCase);
                if (childMatch != null) return childMatch;
            }
            return null;
        }

        private static void DestroyChildIfExists(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void DestroyAllChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static void ResetRootTransform(Transform tr)
        {
            tr.localPosition = Vector3.zero;
            tr.localRotation = Quaternion.identity;
            tr.localScale = Vector3.one;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            if (!go.TryGetComponent<T>(out T comp))
            {
                comp = go.AddComponent<T>();
            }
            return comp;
        }

        private static void AssignMaterial(GameObject go, Material mat)
        {
            if (mat != null && go.TryGetComponent<Renderer>(out Renderer r))
            {
                r.sharedMaterial = mat;
                if (mat.renderQueue >= 3000)
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }
    }

    internal sealed class KenomaLogoTexturePostprocessor : AssetPostprocessor
    {
        private const string KenomaAssetPath = "Assets/Resources/UI/KENOMA.png";

        private void OnPreprocessTexture()
        {
            if (!string.Equals(assetPath, KenomaAssetPath, System.StringComparison.OrdinalIgnoreCase))
                return;

            TextureImporter importer = assetImporter as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.mipMapBias = -0.5f;
            importer.ignoreMipmapLimit = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 1;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
#endif
