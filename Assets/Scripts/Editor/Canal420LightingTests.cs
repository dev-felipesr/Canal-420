#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
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
using Object = UnityEngine.Object;

namespace Canal420.EditorTools
{
    [InitializeOnLoad]
    [TestFixture]
    public class Canal420LightingTests
    {
        private const string SessionKey = "Canal420_LightingTests_Verified_v60";
        private static int pendingRebuildRetries = 10;

        static Canal420LightingTests()
        {
            EditorApplication.delayCall += TryRunDeferredEditorCheck;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                pendingRebuildRetries = 10;
                EditorApplication.delayCall += TryRunDeferredEditorCheck;
            }
        }

        private static void TryRunDeferredEditorCheck()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            if (SessionState.GetBool(SessionKey, false))
                return;

            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || !active.isLoaded)
                return;

            GameObject casa = GameObject.Find("Casa_Base");
            if (casa == null)
                return;

            DayManager dm = Object.FindAnyObjectByType<DayManager>();
            ReflectionProbe[] probes = dm != null ? GetPrivateField<ReflectionProbe[]>(dm, "interiorReflectionProbes") : null;
            TVController wiredTv = dm != null ? GetPrivateField<TVController>(dm, "tvController") : null;
            GameObject d2Root = FindInactiveRoot("Props_Dia2");
            GameObject d3Root = FindInactiveRoot("Props_Dia3");
            Transform bottleTr = d3Root != null ? FindChildRecursive(d3Root.transform, "Frasco_Estimulante_Dia3") : null;
            InspectableObject bottleInsp = bottleTr != null ? bottleTr.GetComponent<InspectableObject>() : null;
            string currentBottleDoc = bottleInsp != null ? GetPrivateField<string>(bottleInsp, "documentBody") : null;
            Transform margaretTr = d3Root != null ? FindChildRecursive(d3Root.transform, "Margaret_Jumpscare") : null;
            bool phase3SceneBuilt = d2Root != null
                && FindChildRecursive(d2Root.transform, "Gravata_Frouxa") != null
                && FindChildRecursive(d2Root.transform, "Aparicao_Porta_Banheiro_D2") != null
                && d3Root != null
                && FindChildRecursive(d3Root.transform, "Estimulante_Dia3_Mesa") != null
                && FindChildRecursive(d3Root.transform, "Espelho_Dia3") != null
                && FindChildRecursive(d3Root.transform, "Cabelo_Caido_Esq") != null
                && margaretTr != null
                && margaretTr.GetComponentInChildren<SkinnedMeshRenderer>(true) != null
                && margaretTr.GetComponent<Animator>() != null
                && margaretTr.GetComponent<Animator>().runtimeAnimatorController != null
                && !string.IsNullOrEmpty(currentBottleDoc)
                && currentBottleDoc.Contains("DATA: --/--/--")
                && !currentBottleDoc.Contains("Restam apenas duas");
            GameObject filhasRoot = FindInactiveRoot("Quarto_Filhas");
            Transform perfilBoxTr = FindChildRecursive(casa.transform, "Perfil_Porta_Box");
            bool phase4SceneBuilt = FindChildRecursive(casa.transform, "Borda_Bandeja_N") != null
                && FindChildRecursive(casa.transform, "Aro_Trempe_0_0_N") != null
                && perfilBoxTr != null && perfilBoxTr.localPosition.y > 1.040f
                && filhasRoot != null
                && FindChildRecursive(filhasRoot.transform, "Cadeira_Infantil_1") != null;
            Canal420AudioManager audioMgr = Object.FindAnyObjectByType<Canal420AudioManager>();
            AudioClip[] woodSteps = audioMgr != null ? GetPrivateField<AudioClip[]>(audioMgr, "customWoodSteps") : null;
            bool audioReady = woodSteps != null && woodSteps.Length > 0;
            if (audioReady && audioMgr != null)
            {
                var ov = GetPrivateField<List<Canal420AudioManager.FoleyClipOverride>>(audioMgr, "foleyOverrides");
                if (ov != null)
                {
                    for (int i = 0; i < ov.Count; i++)
                    {
                        if (ov[i].soundId == FoleySoundId.EerieLullabyHum && ov[i].clip != null && ov[i].clip.name == "eerie_lullaby_hum")
                        {
                            audioReady = false;
                            break;
                        }
                    }
                }
            }
            if (casa.transform.Find("Greybox_Arquitetura_v47") == null || !phase3SceneBuilt || !phase4SceneBuilt || !audioReady || wiredTv == null || probes == null || probes.Length != 8 || probes[0] == null)
            {
                if (pendingRebuildRetries-- > 0)
                {
                    EditorApplication.delayCall += TryRunDeferredEditorCheck;
                }
                return;
            }

            SessionState.SetBool(SessionKey, true);
            RunAllSuiteAndValidators(fromAutoHook: true);
        }

        [MenuItem("Canal 420/Executar Auditoria e Testes de Iluminação", priority = 15)]
        public static void MenuRunAllSuite()
        {
            RunAllSuiteAndValidators(fromAutoHook: false);
        }

        /// <summary>
        /// Abre SampleScene em batchmode, reconstrói a arquitetura ativa e executa testes e auditorias.
        /// </summary>
        public static void RunBatchBuildAndAudit()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Canal420GreyboxBuilder.MenuBuildResidence();
            MenuRunAllSuite();
        }

        private static void RunAllSuiteAndValidators(bool fromAutoHook)
        {
            var tests = new Canal420LightingTests();
            Action[] cases =
            {
                tests.ZeroExternalLightAndZeroSkyboxInScene,
                tests.TwoSidedWallsAndNonOccludingLampShades,
                tests.AllMotivatedLightsCastContainedShadowsAndLowSaturation,
                tests.InternalBoxProjectedReflectionProbesConfigured,
                tests.AllDaysPreserveIndoorRulesAndBlackoutProgression,
                tests.SanityEffectsPreventsCrushedBlacksAndProfileIsValid,
                tests.SafeVisorLightOnlyLitWhenRevealedAndPowered,
                tests.GeometryAnchorsAndDoorsRemainUntouched,
                tests.BrightShadowsAndFullBrightnessSettingsWorkSelectively,
                tests.ResolutionAndVSyncDefaultsAndNotchClamping,
                tests.FaucetSplashParticleSystemBuildsWithoutDurationWarning,
                tests.ProceduralCRTFeedHoistedSamplersPreserveExactPixels,
                tests.HorrorScreenOverlayFlashStaysVisibleForMinFrames,
                tests.AudioAssetsAllHavePreloadAudioDataEnabled,
                tests.ProceduralCRTFeedSingleRenderBurstAndTVDay3MinFrames,
                tests.Phase3StimulantWatchersDomesticHorrorAndTrapIntegrity,
                tests.Phase4VisualBugFixesAndWholeHouse3DModelOverhaul,
                tests.GameplayFlowSequencingAndLightRecoveryInvariants,
                tests.Phase2RetroSurfacesArtworksAndHudSuppressionInvariants,
                tests.Phase3SkinnedRigPsychologicalEventsAndClimaxInvariants,
                tests.P0StabilityAudioPerformanceAndUiInvariants,
                tests.P1PauseProjectorDoorAndAudioMixInvariants
            };

            int passed = 0;
            int failed = 0;
            foreach (Action test in cases)
            {
                try
                {
                    test();
                    passed++;
                }
                catch (Exception ex)
                {
                    failed++;
                    Debug.LogError($"[Canal 420 LightingTests] FALHOU {test.Method.Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }

            if (failed == 0)
            {
                Debug.Log($"[Canal 420 LightingTests] TODOS OS TESTES PASSARAM ({passed}/{cases.Length}) {(fromAutoHook ? "[AutoCheck]" : "[Manual]")}.");
            }

            Canal420SceneValidator.ValidateOpenScene();
            Canal420SceneValidator.AuditSceneGeometry();
        }

        [Test]
        public void ZeroExternalLightAndZeroSkyboxInScene()
        {
            GameObject casa = GameObject.Find("Casa_Base");
            Assert.IsNotNull(casa, "Casa_Base missing from active scene.");
            Assert.IsNotNull(casa.transform.Find("Greybox_Arquitetura_v47"), "SampleScene is not yet rebuilt to Greybox_Arquitetura_v47.");

            Assert.IsNull(RenderSettings.skybox, "RenderSettings.skybox must be null in closed indoor horror scene.");
            Assert.IsNull(RenderSettings.sun, "RenderSettings.sun must be null.");
            Assert.AreEqual(DefaultReflectionMode.Custom, RenderSettings.defaultReflectionMode, "Default reflection mode must be Custom.");
            Assert.IsNotNull(RenderSettings.customReflectionTexture, "Custom dark interior reflection cubemap fallback must be assigned.");
            Assert.AreEqual(AmbientMode.Trilight, RenderSettings.ambientMode, "Ambient mode must be Trilight for non-flat indoor lighting.");
            Assert.IsTrue(RenderSettings.fog, "Atmospheric internal fog must be enabled.");
            Assert.AreEqual(FogMode.ExponentialSquared, RenderSettings.fogMode);

            GameObject dirObj = GameObject.Find("Directional Light");
            if (dirObj != null && dirObj.TryGetComponent<Light>(out Light dirLight))
            {
                Assert.IsFalse(dirLight.enabled, "Exterior Directional Light must be disabled.");
                Assert.LessOrEqual(dirLight.intensity, 0.0001f, "Exterior Directional Light intensity must be zero.");
            }

            Camera mainCam = Camera.main;
            Assert.IsNotNull(mainCam, "Main Camera not found.");
            Assert.AreEqual(CameraClearFlags.SolidColor, mainCam.clearFlags, "Main Camera must use SolidColor clearFlags, never Skybox.");
            Assert.IsNull(mainCam.transform.Find("Luz_Adaptacao_Escuro"), "Camera headlight Luz_Adaptacao_Escuro must be removed.");

            GameObject qFilhas = FindInactiveRoot("Quarto_Filhas");
            Assert.IsNotNull(qFilhas, "Quarto_Filhas missing.");
            Assert.IsNull(FindChildRecursive(qFilhas.transform, "Luz_Fresta_JanelaFilhas"), "Exterior window slit fake light Luz_Fresta_JanelaFilhas must be removed.");

            GameObject propsDia3 = FindInactiveRoot("Props_Dia3");
            Assert.IsNotNull(propsDia3, "Props_Dia3 missing.");
            Assert.IsNull(FindChildRecursive(propsDia3.transform, "Luz_Destaque_RetratoMargaret"), "Unmotivated red floating point light Luz_Destaque_RetratoMargaret must be removed.");
        }

        private static bool IsShadowBoxEnclosureName(string n)
        {
            return n.StartsWith("ShadowBox_") &&
                   (n.EndsWith("_Fundo") || n.EndsWith("_Sup") || n.EndsWith("_Inf") || n.EndsWith("_Min") || n.EndsWith("_Max"));
        }

        [Test]
        public void TwoSidedWallsAndNonOccludingLampShades()
        {
            GameObject casa = GameObject.Find("Casa_Base");
            GameObject qFilhas = FindInactiveRoot("Quarto_Filhas");
            Assert.IsNotNull(casa);
            Assert.IsNotNull(qFilhas);

            int twoSidedChecked = 0;
            int shadesChecked = 0;
            foreach (Transform root in new[] { casa.transform, qFilhas.transform })
            {
                MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    MeshRenderer mr = renderers[i];
                    string n = mr.gameObject.name;
                    if (n.StartsWith("Parede_") ||
                        n.StartsWith("Batente_Janela_") ||
                        IsShadowBoxEnclosureName(n) ||
                        n == "Teto_Residencia" ||
                        n == "Base1" ||
                        n.StartsWith("Piso_") ||
                        n == "Folha_Porta" ||
                        n == "Porta_Rua_Folha" ||
                        n == "Porta_Entrada_Frente")
                    {
                        Assert.AreEqual(ShadowCastingMode.TwoSided, mr.shadowCastingMode, $"Structural surface {n} must cast TwoSided shadows to prevent wall light leaks.");
                        twoSidedChecked++;
                    }
                    else if (n == "Plafon_Teto" || n == "Plafon_Teto_Apagado" ||
                             n == "Cupula_Fosca" || n == "Cupula_Fosca_Apagada" || n == "Bulbo_Interno" ||
                             n == "Abajur_Margaret_Cupula" || n == "Abajur_Margaret_Cupula_Apagada" || n == "Bulbo_Abajur_Aceso" ||
                             n == "Cupula_Tecido" || n == "Cupula_Tecido_Apagada" || n == "Bulbo_Coluna_Aceso" ||
                             n == "Luminaria_Cupula" || n == "Luminaria_Cupula_Apagada" || n == "Luminaria_Vidro_Verde" || n == "Luminaria_Vidro_Apagado" ||
                             n == "Arandela_Acima_Espelho" || n == "Arandela_Acima_Espelho_Apagada" || n == "Tubo_Arandela_Aceso")
                    {
                        Assert.AreEqual(ShadowCastingMode.Off, mr.shadowCastingMode, $"Lamp shade/globe {n} must have ShadowCastingMode.Off so bulb inside does not self-occlude.");
                        shadesChecked++;
                    }
                }
            }

            Assert.GreaterOrEqual(twoSidedChecked, 25, "Expected >= 25 structural surfaces casting TwoSided shadows.");
            Assert.GreaterOrEqual(shadesChecked, 40, "Expected >= 40 ON/OFF lamp shade, bulb & diffuser meshes with shadows disabled.");
        }

        [Test]
        public void AllMotivatedLightsCastContainedShadowsAndLowSaturation()
        {
            DayManager dm = Object.FindAnyObjectByType<DayManager>();
            Assert.IsNotNull(dm);

            Light[] roomLights = GetPrivateField<Light[]>(dm, "houseInteriorLights");
            Light deskLight = GetPrivateField<Light>(dm, "day3RestoredCircuitLight");
            Light[] secondaryLights = GetPrivateField<Light[]>(dm, "secondaryMotivatedLights");
            Light[] fillLights = GetPrivateField<Light[]>(dm, "architecturalFillLights");

            Assert.IsNotNull(roomLights);
            Assert.AreEqual(9, roomLights.Length, "Expected 9 ceiling room lamps (8 main house + Quarto_Filhas).");
            Assert.IsNotNull(deskLight, "day3RestoredCircuitLight must be wired.");
            Assert.IsNotNull(secondaryLights);
            Assert.AreEqual(3, secondaryLights.Length, "Expected 3 true secondary fixture lights (abajur, floor lamp, vanity sconce — zero unmotivated doorway floats).");
            Assert.IsNotNull(fillLights);
            Assert.AreEqual(1, fillLights.Length, "Expected only motivated kitchen-arch doorway spill (zero unmotivated air rim/bounce floats).");

            for (int i = 0; i < roomLights.Length; i++)
            {
                Light l = roomLights[i];
                Assert.IsNotNull(l);
                Assert.AreEqual(LightType.Spot, l.type, $"{l.name} must be a Spot light.");
                Assert.AreNotEqual(LightShadows.None, l.shadows, $"{l.name} must cast shadows.");
                Assert.LessOrEqual(l.range, 6.0f, $"{l.name} range exceeds room bounds.");
                Assert.LessOrEqual(l.spotAngle, 142f, $"{l.name} spotAngle too wide.");
                Assert.LessOrEqual(l.shadowNearPlane, 0.12f, $"{l.name} shadowNearPlane too large.");

                Transform fixtureRoot = l.transform.parent;
                Assert.IsNotNull(fixtureRoot);
                Transform onState = fixtureRoot.Find("Estado_Ligado");
                Transform offState = fixtureRoot.Find("Estado_Desligado");
                Assert.IsNotNull(onState, $"{fixtureRoot.name} missing Estado_Ligado visual variant.");
                Assert.IsNotNull(offState, $"{fixtureRoot.name} missing Estado_Desligado visual variant.");

                Transform cupula = onState.Find("Cupula_Fosca");
                Assert.IsNotNull(cupula, $"{fixtureRoot.name} missing Cupula_Fosca inside Estado_Ligado.");
                Assert.LessOrEqual(Vector3.Distance(l.transform.position, cupula.position), 0.05f,
                    $"{l.name} must sit inside Cupula_Fosca, not floating below it.");

                Light halo = onState.GetComponentInChildren<Light>(true);
                Assert.IsNotNull(halo, $"{fixtureRoot.name} missing HaloLight inside Estado_Ligado.");
                Assert.AreEqual(LightShadows.None, halo.shadows, $"{halo.name} ceiling halo must stay shadowless.");

                Color.RGBToHSV(l.color, out _, out float sat, out _);
                Assert.LessOrEqual(sat, 0.45f, $"{l.name} saturation {sat:F2} exceeds natural low-key cap (0.45).");
            }

            // Desk lamp tucked under hood with ON/OFF versions
            Assert.AreEqual(LightType.Spot, deskLight.type);
            Assert.AreNotEqual(LightShadows.None, deskLight.shadows);
            Transform deskOn = deskLight.transform.parent.Find("Estado_Ligado_Luminaria");
            Transform deskOff = deskLight.transform.parent.Find("Estado_Desligado_Luminaria");
            Assert.IsNotNull(deskOn, "Mesa_Escritorio missing Estado_Ligado_Luminaria.");
            Assert.IsNotNull(deskOff, "Mesa_Escritorio missing Estado_Desligado_Luminaria.");
            Transform deskGlass = deskOn.Find("Luminaria_Vidro_Verde");
            Assert.IsNotNull(deskGlass);
            Assert.LessOrEqual(Vector3.Distance(deskLight.transform.position, deskGlass.position), 0.05f,
                "Luz_Mesa_Escritorio_Circuito must be tucked under banker diffuser.");

            // Secondary lights inside physical bulbs/shades
            for (int i = 0; i < secondaryLights.Length; i++)
            {
                Light sec = secondaryLights[i];
                Assert.IsNotNull(sec);
                Assert.AreNotEqual(LightShadows.None, sec.shadows, $"{sec.name} must cast shadows.");
                string sn = sec.name.ToLowerInvariant();
                Transform onState = sec.transform.parent.Find("Estado_Ligado");
                Transform offState = sec.transform.parent.Find("Estado_Desligado");
                Assert.IsNotNull(onState, $"{sec.transform.parent.name} missing Estado_Ligado.");
                Assert.IsNotNull(offState, $"{sec.transform.parent.name} missing Estado_Desligado.");

                if (sn.Contains("abajur_margaret"))
                {
                    Assert.AreEqual(LightType.Point, sec.type, "Abajur_Margaret must radiate omni light inside open shade.");
                    Transform bulb = onState.Find("Bulbo_Abajur_Aceso");
                    Assert.LessOrEqual(Vector3.Distance(sec.transform.position, bulb.position), 0.02f);
                }
                else if (sn.Contains("luminaria_coluna_sala"))
                {
                    Assert.AreEqual(LightType.Point, sec.type, "Luminaria_Coluna_Sala must radiate omni light centered in shade.");
                    Transform bulb = onState.Find("Bulbo_Coluna_Aceso");
                    Assert.LessOrEqual(Vector3.Distance(sec.transform.position, bulb.position), 0.02f);
                }
                else if (sn.Contains("arandela_banheiro"))
                {
                    Assert.AreEqual(LightType.Spot, sec.type);
                    Transform tube = onState.Find("Tubo_Arandela_Aceso");
                    Assert.LessOrEqual(Vector3.Distance(sec.transform.position, tube.position), 0.03f);
                }

                Color.RGBToHSV(sec.color, out _, out float sat, out _);
                Assert.LessOrEqual(sat, 0.45f, $"{sec.name} saturation {sat:F2} too high.");
            }

            TVController tv = Object.FindAnyObjectByType<TVController>();
            Assert.IsNotNull(tv);
            Light tvLight = GetPrivateField<Light>(tv, "tvGlowLight");
            Assert.IsNotNull(tvLight);
            Assert.AreEqual(LightType.Spot, tvLight.type);
            Assert.AreNotEqual(LightShadows.None, tvLight.shadows);

            for (int i = 0; i < fillLights.Length; i++)
            {
                Light fill = fillLights[i];
                Assert.IsNotNull(fill);
                Assert.AreEqual(LightType.Spot, fill.type, $"Architectural fill {fill.name} must be an inward-aimed Spot.");
                Assert.AreEqual(LightShadows.None, fill.shadows);
                Color.RGBToHSV(fill.color, out _, out float sat, out _);
                Assert.LessOrEqual(sat, 0.35f);
            }
        }

        [Test]
        public void InternalBoxProjectedReflectionProbesConfigured()
        {
            DayManager dm = Object.FindAnyObjectByType<DayManager>();
            Assert.IsNotNull(dm);

            ReflectionProbe[] probes = GetPrivateField<ReflectionProbe[]>(dm, "interiorReflectionProbes");
            Assert.IsNotNull(probes);
            Assert.AreEqual(8, probes.Length, "Expected 8 box-projected internal room ReflectionProbes.");

            for (int i = 0; i < probes.Length; i++)
            {
                ReflectionProbe p = probes[i];
                Assert.IsNotNull(p);
                Assert.IsTrue(p.boxProjection, $"{p.name} must enable boxProjection.");
                Assert.AreEqual(ReflectionProbeClearFlags.SolidColor, p.clearFlags, $"{p.name} must clear with dark SolidColor, never Skybox.");
                Assert.AreEqual(ReflectionProbeMode.Realtime, p.mode, $"{p.name} should update dynamically across day/circuit state.");
                Assert.Greater(p.size.x, 1.0f);
                Assert.Greater(p.size.y, 2.0f);
                Assert.Greater(p.size.z, 1.0f);
            }
        }

        [Test]
        public void AllDaysPreserveIndoorRulesAndBlackoutProgression()
        {
            DayManager dm = Object.FindAnyObjectByType<DayManager>();
            Assert.IsNotNull(dm);

            MethodInfo applyLighting = typeof(DayManager).GetMethod("ApplyAtmosphericLightingForDay", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo setCircuit = typeof(DayManager).GetMethod("SetDay3CircuitLightsState", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo jumpscareBlackout = typeof(DayManager).GetMethod("SetJumpscareBlackoutLighting", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(applyLighting);
            Assert.IsNotNull(setCircuit);
            Assert.IsNotNull(jumpscareBlackout);

            Light mainLight = GetPrivateField<Light>(dm, "houseMainLight");
            Light[] roomLights = GetPrivateField<Light[]>(dm, "houseInteriorLights");
            Light deskLight = GetPrivateField<Light>(dm, "day3RestoredCircuitLight");
            Light[] secondaryLights = GetPrivateField<Light[]>(dm, "secondaryMotivatedLights");
            Light[] fillLights = GetPrivateField<Light[]>(dm, "architecturalFillLights");
            TVController tv = Object.FindAnyObjectByType<TVController>();
            Light tvLight = GetPrivateField<Light>(tv, "tvGlowLight");

            try
            {
                InventoryState.ClearAll();

                // Day 1..4 atmospheric rules: zero exaggerated blue cast or heavy opaque fog
                foreach (GameDay day in new[] { GameDay.Day1, GameDay.Day2, GameDay.Day3, GameDay.Day4_Prelude })
                {
                    applyLighting.Invoke(dm, new object[] { day });
                    Assert.IsNull(RenderSettings.skybox, $"Skybox must stay null on {day}.");
                    Assert.AreEqual(AmbientMode.Trilight, RenderSettings.ambientMode, $"Trilight ambient required on {day}.");
                    Assert.GreaterOrEqual(RenderSettings.ambientEquatorColor.maxColorComponent, 0.045f, $"Ambient too dark (black crush risk) on {day}.");
                    Assert.LessOrEqual(RenderSettings.ambientEquatorColor.b,
                        Mathf.Max(RenderSettings.ambientEquatorColor.r, RenderSettings.ambientEquatorColor.g) * 1.08f,
                        $"Ambient on {day} still has exaggerated blue bias.");
                    Assert.LessOrEqual(RenderSettings.fogDensity, 0.015f, $"Indoor fog too thick/opaque on {day}.");
                    Assert.LessOrEqual(RenderSettings.fogColor.b,
                        Mathf.Max(RenderSettings.fogColor.r, RenderSettings.fogColor.g) + 0.005f,
                        $"Fog on {day} still tinted blue.");
                    if (mainLight != null)
                    {
                        Assert.IsFalse(mainLight.enabled, $"houseMainLight must remain disabled on {day}.");
                        Assert.AreEqual(0f, mainLight.intensity, $"houseMainLight intensity must remain 0 on {day}.");
                    }
                }

                // Day 3 Total Blackout (Apagão Geral): ALL room & secondary lamps strictly OFF before breaker restore
                applyLighting.Invoke(dm, new object[] { GameDay.Day3 });
                AssertFixtureVisualState(deskLight, expectedOn: false, onName: "Estado_Ligado_Luminaria", offName: "Estado_Desligado_Luminaria", "Desk lamp during Day 3 total blackout");
                AssertCeilingLampState(FindLight(roomLights, "luz_corredor_centro"), expectedOn: false, "Corridor center during Day 3 total blackout");
                AssertCeilingLampState(FindLight(roomLights, "luz_corredor_leste"), expectedOn: false, "Corridor east during Day 3 total blackout");
                AssertCeilingLampState(FindLight(roomLights, "luz_sala_estar"), expectedOn: false, "Living room during Day 3 total blackout");
                AssertCeilingLampState(FindLight(roomLights, "luz_escritorio"), expectedOn: false, "Office during Day 3 total blackout");
                AssertCeilingLampState(FindLight(roomLights, "luz_cozinha"), expectedOn: false, "Kitchen during Day 3 total blackout");
                AssertCeilingLampState(FindLight(roomLights, "luz_banheiro"), expectedOn: false, "Bathroom during Day 3 total blackout");
                AssertCeilingLampState(FindLight(roomLights, "luz_quarto_cian"), expectedOn: false, "Bedroom during Day 3 total blackout");
                AssertFixtureVisualState(FindLight(secondaryLights, "luz_luminaria_coluna_sala"), expectedOn: false, "Estado_Ligado", "Estado_Desligado", "Floor lamp during Day 3 total blackout");
                AssertFixtureVisualState(FindLight(secondaryLights, "luz_abajur_margaret"), expectedOn: false, "Estado_Ligado", "Estado_Desligado", "Margaret bedside lamp during Day 3 total blackout");
                AssertFixtureVisualState(FindLight(secondaryLights, "luz_arandela_banheiro"), expectedOn: false, "Estado_Ligado", "Estado_Desligado", "Bathroom sconce during Day 3 total blackout");
                Assert.IsFalse(FindLight(fillLights, "fill_spill_arcocozinha").enabled, "Kitchen doorway spill must stay OFF during Day 3 total blackout.");

                // Day 3 Breaker Restore: restored fixtures switch to Estado_Ligado & transmit light
                InventoryState.AddItem("energia_corredor_ativa");
                setCircuit.Invoke(dm, new object[] { true, 1f });
                AssertFixtureVisualState(deskLight, expectedOn: true, onName: "Estado_Ligado_Luminaria", offName: "Estado_Desligado_Luminaria", "Desk lamp after Day 3 breaker restore");
                AssertCeilingLampState(FindLight(roomLights, "luz_corredor_centro"), expectedOn: true, "Corridor center after Day 3 breaker restore");
                AssertCeilingLampState(FindLight(roomLights, "luz_escritorio"), expectedOn: true, "Office after Day 3 breaker restore");
                AssertCeilingLampState(FindLight(roomLights, "luz_cozinha"), expectedOn: true, "Kitchen after Day 3 breaker restore");
                AssertCeilingLampState(FindLight(roomLights, "luz_banheiro"), expectedOn: true, "Bathroom after Day 3 breaker restore");
                AssertFixtureVisualState(FindLight(secondaryLights, "luz_abajur_margaret"), expectedOn: true, "Estado_Ligado", "Estado_Desligado", "Abajur after Day 3 breaker restore");
                AssertFixtureVisualState(FindLight(secondaryLights, "luz_arandela_banheiro"), expectedOn: true, "Estado_Ligado", "Estado_Desligado", "Bathroom sconce after Day 3 breaker restore");
                Assert.IsTrue(FindLight(fillLights, "fill_spill_arcocozinha").enabled, "Kitchen spill must turn ON after Day 3 breaker restore.");

                // Day 3 Safe Unlocked Brownout: peripheral rooms dim into brownout but remain enabled & navigable (> 0.10f)
                FieldInfo safeUnlockedField = typeof(DayManager).GetField("day3SafeUnlocked", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo curDayField = typeof(DayManager).GetField("currentDay", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(safeUnlockedField);
                Assert.IsNotNull(curDayField);
                curDayField.SetValue(dm, GameDay.Day3);
                safeUnlockedField.SetValue(dm, true);
                applyLighting.Invoke(dm, new object[] { GameDay.Day3 });
                AssertCeilingLampState(FindLight(roomLights, "luz_cozinha"), expectedOn: true, "Kitchen during Day 3 safe-unlocked brownout");
                AssertCeilingLampState(FindLight(roomLights, "luz_banheiro"), expectedOn: true, "Bathroom during Day 3 safe-unlocked brownout");
                AssertCeilingLampState(FindLight(roomLights, "luz_quarto_cian"), expectedOn: true, "Bedroom during Day 3 safe-unlocked brownout");
                AssertCeilingLampState(FindLight(roomLights, "luz_corredor_oeste"), expectedOn: true, "West corridor during Day 3 safe-unlocked brownout");
                AssertCeilingLampState(FindLight(roomLights, "luz_escritorio"), expectedOn: true, "Office during Day 3 safe-unlocked brownout");
                AssertFixtureVisualState(deskLight, expectedOn: true, onName: "Estado_Ligado_Luminaria", offName: "Estado_Desligado_Luminaria", "Desk lamp during Day 3 safe-unlocked brownout");
                AssertFixtureVisualState(FindLight(secondaryLights, "luz_abajur_margaret"), expectedOn: true, "Estado_Ligado", "Estado_Desligado", "Abajur during Day 3 safe-unlocked brownout");
                AssertFixtureVisualState(FindLight(secondaryLights, "luz_arandela_banheiro"), expectedOn: true, "Estado_Ligado", "Estado_Desligado", "Bathroom sconce during Day 3 safe-unlocked brownout");
                Assert.AreEqual(0.76f * 0.28f, FindLight(roomLights, "luz_cozinha").intensity, 0.001f, "Kitchen brownout exact multiplier at 50% brightness.");
                Assert.AreEqual(0.76f * 0.22f, FindLight(roomLights, "luz_banheiro").intensity, 0.001f, "Bathroom brownout exact multiplier at 50% brightness.");
                Assert.AreEqual(0.62f * 0.22f, FindLight(roomLights, "luz_quarto_cian").intensity, 0.001f, "Bedroom brownout exact multiplier at 50% brightness.");
                Assert.AreEqual(0.18f, FindLight(roomLights, "luz_corredor_oeste").intensity, 0.001f, "West corridor brownout exact multiplier at 50% brightness.");
                Assert.AreEqual(0.14f, FindLight(roomLights, "luz_escritorio").intensity, 0.001f, "Office brownout exact multiplier at 50% brightness.");
                Assert.AreEqual(0.42f, deskLight.intensity, 0.001f, "Office banker lamp brownout exact multiplier at 50% brightness.");
                Assert.AreEqual(0.26f, FindLight(secondaryLights, "luz_abajur_margaret").intensity, 0.001f, "Abajur brownout exact multiplier at 50% brightness.");
                Assert.AreEqual(0.24f, FindLight(secondaryLights, "luz_arandela_banheiro").intensity, 0.001f, "Bathroom sconce brownout exact multiplier at 50% brightness.");
                Assert.AreEqual(0.09f, FindLight(fillLights, "fill_spill_arcocozinha").intensity, 0.001f, "Kitchen spill brownout exact multiplier at 50% brightness.");

                // Adjusting user brightness while day3SafeUnlocked == true must preserve exact brownout ratios scaled by lampMult/ambMult
                dm.SetUserBrightnessNormalized(0.75f);
                float lampMult75 = Mathf.Lerp(1.0f, 1.70f, 0.5f);
                float ambMult75 = Mathf.Lerp(1.0f, 2.42f, 0.5f);
                Assert.AreEqual(0.76f * 0.28f * lampMult75, FindLight(roomLights, "luz_cozinha").intensity, 0.001f, "Kitchen brownout scaled at 75% brightness.");
                Assert.AreEqual(0.42f * lampMult75, deskLight.intensity, 0.001f, "Banker lamp brownout scaled at 75% brightness.");
                Assert.AreEqual(0.09f * ambMult75, FindLight(fillLights, "fill_spill_arcocozinha").intensity, 0.001f, "Kitchen spill brownout scaled at 75% brightness.");
                dm.SetUserBrightnessNormalized(0.50f);
                safeUnlockedField.SetValue(dm, false);
                curDayField.SetValue(dm, GameDay.Day1);

                // Day 4 Daughters Room: only QuartoFilhas ON; all others strictly OFF
                applyLighting.Invoke(dm, new object[] { GameDay.Day4_Prelude });
                AssertCeilingLampState(FindLight(roomLights, "luz_quartofilhas"), expectedOn: true, "QuartoFilhas lamp in Day 4");
                AssertCeilingLampState(FindLight(roomLights, "luz_cozinha"), expectedOn: false, "Kitchen in Day 4");
                AssertFixtureVisualState(FindLight(secondaryLights, "luz_abajur_margaret"), expectedOn: false, "Estado_Ligado", "Estado_Desligado", "Abajur in Day 4");
                Assert.IsFalse(FindLight(fillLights, "fill_spill_arcocozinha").enabled, "Kitchen spill must turn off in Day 4.");

                // Jumpscare Total Blackout
                jumpscareBlackout.Invoke(dm, null);
                AssertCeilingLampState(FindLight(roomLights, "luz_cozinha"), expectedOn: false, "Kitchen during jumpscare blackout");
                AssertCeilingLampState(FindLight(roomLights, "luz_banheiro"), expectedOn: false, "Bathroom during jumpscare blackout");
                AssertFixtureVisualState(FindLight(secondaryLights, "luz_abajur_margaret"), expectedOn: false, "Estado_Ligado", "Estado_Desligado", "Abajur during jumpscare blackout");

                // Quality tiers: Low (false -> Hard) vs High (true -> Soft), NEVER LightShadows.None on primary lights
                dm.ApplyShadowQualityToInteriorLights(false);
                for (int i = 0; i < roomLights.Length; i++)
                    Assert.AreEqual(LightShadows.Hard, roomLights[i].shadows, $"{roomLights[i].name} must cast Hard shadows on Low quality.");
                Assert.AreEqual(LightShadows.Hard, tvLight.shadows, "TV light must cast Hard shadows on Low quality.");

                dm.ApplyShadowQualityToInteriorLights(true);
                for (int i = 0; i < roomLights.Length; i++)
                    Assert.AreEqual(LightShadows.Soft, roomLights[i].shadows, $"{roomLights[i].name} must cast Soft shadows on Medium..Ultra quality.");
                Assert.AreEqual(LightShadows.Soft, tvLight.shadows, "TV light must cast Soft shadows on Medium..Ultra quality.");
            }
            finally
            {
                typeof(DayManager).GetField("day3SafeUnlocked", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(dm, false);
                InventoryState.ClearAll();
                applyLighting.Invoke(dm, new object[] { GameDay.Day1 });
                dm.ApplyShadowQualityToInteriorLights(true);
            }
        }

        [Test]
        public void SanityEffectsPreventsCrushedBlacksAndProfileIsValid()
        {
            SanityEffects sanity = Object.FindAnyObjectByType<SanityEffects>();
            Assert.IsNotNull(sanity);

            float bLift = GetPrivateField<float>(sanity, "burnoutShadowLift");
            float dLift = GetPrivateField<float>(sanity, "depressantShadowLift");
            float hLift = GetPrivateField<float>(sanity, "hallucinogenShadowLift");
            float pLift = GetPrivateField<float>(sanity, "preludeShadowLift");

            Assert.GreaterOrEqual(bLift, 0.0008f);
            Assert.Greater(dLift, bLift);
            Assert.Greater(hLift, dLift);
            Assert.LessOrEqual(hLift, 0.005f, "Shadow lift must stay subtle (<= 0.005) so shadows never turn into opaque grey haze.");
            Assert.GreaterOrEqual(pLift, 0.0008f);

            Color bFilter = GetPrivateField<Color>(sanity, "burnoutColorFilter");
            Color dFilter = GetPrivateField<Color>(sanity, "depressantColorFilter");
            Color hFilter = GetPrivateField<Color>(sanity, "hallucinogenColorFilter");
            Color pFilter = GetPrivateField<Color>(sanity, "preludeColorFilter");
            Assert.LessOrEqual(bFilter.b, Mathf.Max(bFilter.r, bFilter.g) + 0.015f, "Day 1 ColorFilter must not have exaggerated blue shift.");
            Assert.LessOrEqual(dFilter.b, Mathf.Max(dFilter.r, dFilter.g) + 0.015f, "Day 2 ColorFilter must not have exaggerated blue shift.");
            Assert.LessOrEqual(hFilter.b, Mathf.Max(hFilter.r, hFilter.g) + 0.015f, "Day 3 ColorFilter must not have exaggerated blue shift.");
            Assert.LessOrEqual(pFilter.b, Mathf.Max(pFilter.r, pFilter.g) + 0.015f, "Day 4 ColorFilter must not have exaggerated blue shift.");

            Assert.LessOrEqual(GetPrivateField<float>(sanity, "burnoutContrast"), 12f);
            Assert.LessOrEqual(GetPrivateField<float>(sanity, "hallucinogenContrast"), 14f);
            Assert.GreaterOrEqual(GetPrivateField<float>(sanity, "hallucinogenExposure"), -0.10f);

            Volume vol = GetPrivateField<Volume>(sanity, "globalVolume");
            Assert.IsNotNull(vol);
            Assert.IsNotNull(vol.sharedProfile);
            foreach (VolumeComponent comp in vol.sharedProfile.components)
            {
                Assert.IsNotNull(comp, "VolumeProfile contains a null/unpersisted VolumeComponent sub-asset.");
            }
            Assert.IsTrue(vol.sharedProfile.TryGet<Tonemapping>(out _), "VolumeProfile missing Tonemapping.");
            Assert.IsTrue(vol.sharedProfile.TryGet<Bloom>(out Bloom bloomComp), "VolumeProfile missing Bloom.");
            Assert.GreaterOrEqual(bloomComp.threshold.value, 1.10f, "Bloom threshold must stay >= 1.10 so regular surfaces never fog/wash out.");
            Assert.IsTrue(vol.sharedProfile.TryGet<ColorAdjustments>(out _), "VolumeProfile missing ColorAdjustments.");
            Assert.IsTrue(vol.sharedProfile.TryGet<LiftGammaGain>(out LiftGammaGain lggComp), "VolumeProfile missing LiftGammaGain.");
            Assert.LessOrEqual(lggComp.lift.value.z, lggComp.lift.value.x + 0.005f, "VolumeProfile LiftGammaGain.lift must not boost blue shadow haze.");
            Assert.LessOrEqual(Mathf.Abs(lggComp.gamma.value.w), 0.005f, "VolumeProfile LiftGammaGain.gamma.w must stay neutral (no milky wash).");
            Assert.IsTrue(vol.sharedProfile.TryGet<Vignette>(out _), "VolumeProfile missing Vignette.");

            Camera mainCam = Camera.main;
            Assert.IsNotNull(mainCam, "Main Camera missing.");
            UniversalAdditionalCameraData camData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            Assert.IsNotNull(camData, "Main Camera missing UniversalAdditionalCameraData.");
            Assert.IsTrue(camData.renderPostProcessing, "Main Camera UniversalAdditionalCameraData.renderPostProcessing must be true so Day 1-4 SanityEffects render.");
        }

        private static void AssertCeilingLampState(Light l, bool expectedOn, string context)
        {
            AssertFixtureVisualState(l, expectedOn, "Estado_Ligado", "Estado_Desligado", context);
            Light halo = l.transform.parent.Find("Estado_Ligado")?.GetComponentInChildren<Light>(true);
            Assert.IsNotNull(halo, $"Missing HaloLight under {l.transform.parent.name}");
            Assert.AreEqual(expectedOn, halo.enabled, $"HaloLight enabled mismatch for {context}");
            if (!expectedOn) Assert.AreEqual(0f, halo.intensity, $"HaloLight intensity must be 0 when off ({context})");
        }

        private static void AssertFixtureVisualState(Light l, bool expectedOn, string onName, string offName, string context)
        {
            Assert.IsNotNull(l, $"Light missing for {context}");
            Assert.AreEqual(expectedOn, l.enabled, $"Light.enabled mismatch for {context}");
            if (!expectedOn) Assert.AreEqual(0f, l.intensity, $"Light.intensity must be 0 when off ({context})");
            Transform parent = l.transform.parent;
            Assert.IsNotNull(parent);
            Transform onTr = parent.Find(onName);
            Transform offTr = parent.Find(offName);
            Assert.IsNotNull(onTr, $"{parent.name} missing {onName}");
            Assert.IsNotNull(offTr, $"{parent.name} missing {offName}");
            Assert.AreEqual(expectedOn, onTr.gameObject.activeSelf, $"{parent.name}/{onName}.activeSelf mismatch for {context}");
            Assert.AreEqual(!expectedOn, offTr.gameObject.activeSelf, $"{parent.name}/{offName}.activeSelf mismatch for {context}");
        }

        [Test]
        public void SafeVisorLightOnlyLitWhenRevealedAndPowered()
        {
            SafeBoxPuzzle safe = Object.FindAnyObjectByType<SafeBoxPuzzle>(FindObjectsInactive.Include);
            Assert.IsNotNull(safe, "SafeBoxPuzzle not found.");

            Light visorLight = GetPrivateField<Light>(safe, "digitalVisorLight");
            Assert.IsNotNull(visorLight, "Safe visor light Luz_Visor_Cofre must be wired.");

            try
            {
                InventoryState.ClearAll();
                safe.ResetSafeState();
                Assert.IsFalse(visorLight.enabled, "Visor light must stay OFF before safe is revealed.");

                safe.SetRevealed(true);
                Assert.IsFalse(visorLight.enabled, "Visor light must stay OFF without circuit power.");

                InventoryState.AddItem("energia_corredor_ativa");
                MethodInfo refresh = typeof(SafeBoxPuzzle).GetMethod("Refresh3DVisorGlow", BindingFlags.Instance | BindingFlags.NonPublic);
                refresh.Invoke(safe, null);
                Assert.IsTrue(visorLight.enabled, "Visor light must turn ON when revealed and powered.");
                Assert.LessOrEqual(visorLight.range, 0.80f, "Visor light range must remain strictly localized to keypad.");
            }
            finally
            {
                InventoryState.ClearAll();
                safe.ResetSafeState();
                safe.SetRevealed(false);
            }
        }

        [Test]
        public void GeometryAnchorsAndDoorsRemainUntouched()
        {
            AssertWorldPosApprox("Teto_Residencia", new Vector3(-0.10f, 2.81f, 0.10f));
            AssertWorldPosApprox("Base1", new Vector3(1.82f, -0.07f, -2.77f));
            AssertWorldPosApprox("TV_Sala", new Vector3(-0.84f, 0.56f, -5.45f));
            AssertWorldPosApprox("Mesa_Escritorio", new Vector3(5.72f, 0f, -2.55f));
            AssertWorldPosApprox("Criado_Mudo_Margaret", new Vector3(0.32f, 0f, 5.78f));
            AssertWorldPosApprox("Espelho_Banheiro", new Vector3(-4.02f, 1.62f, 6.018f));
            AssertWorldPosApprox("Porta_Quarto_Pivot", new Vector3(2.60f, 0f, 4.143f));
        }

        [Test]
        public void BrightShadowsAndFullBrightnessSettingsWorkSelectively()
        {
            Assert.AreEqual(1.00f, GameSettingsUI.MaxBrightness, 0.0001f, "MaxBrightness must allow up to 100%.");

            DayManager dm = Object.FindAnyObjectByType<DayManager>();
            SanityEffects sanity = Object.FindAnyObjectByType<SanityEffects>();
            Assert.IsNotNull(dm, "DayManager missing.");
            Assert.IsNotNull(sanity, "SanityEffects missing.");

            Light[] roomLights = GetPrivateField<Light[]>(dm, "houseInteriorLights");
            Light livingLamp = FindLight(roomLights, "luz_sala_estar");
            Light kitchenLamp = FindLight(roomLights, "luz_cozinha");

            try
            {
                InventoryState.ClearAll();

                // 100% brightness reaches 1.0f without being capped at 0.80f
                dm.SetBrightShadowsEnabled(false);
                dm.SetUserBrightnessNormalized(1.00f);
                sanity.SetUserBrightnessNormalized(1.00f, applyImmediately: false);
                Assert.AreEqual(1.00f, GetPrivateField<float>(dm, "userBrightnessNormalized"), 0.001f);
                Assert.AreEqual(1.00f, GetPrivateField<float>(sanity, "userBrightnessNormalized"), 0.001f);
                Assert.AreEqual(1.85f, GetPrivateField<float>(sanity, "userExposureOffsetEV"), 0.02f);

                // Reset to default 50% and test "Sombras claras" selectivity across Day 1 and Day 3 blackout
                dm.SetUserBrightnessNormalized(0.50f);
                MethodInfo applyLighting = typeof(DayManager).GetMethod("ApplyAtmosphericLightingForDay", BindingFlags.Instance | BindingFlags.NonPublic);
                applyLighting.Invoke(dm, new object[] { GameDay.Day1 });
                float day1LampBefore = livingLamp.intensity;
                float day1KitchenBefore = kitchenLamp.intensity;
                float day1EqBefore = RenderSettings.ambientEquatorColor.grayscale;
                float day1GroundBefore = RenderSettings.ambientGroundColor.grayscale;

                dm.SetBrightShadowsEnabled(true);
                float day1LampAfter = livingLamp.intensity;
                float day1KitchenAfter = kitchenLamp.intensity;
                float day1EqAfter = RenderSettings.ambientEquatorColor.grayscale;
                float day1GroundAfter = RenderSettings.ambientGroundColor.grayscale;

                Assert.AreEqual(day1LampBefore, day1LampAfter, 0.0001f, "Sombras claras must NOT boost direct ceiling lamps on lit surfaces.");
                Assert.AreEqual(day1KitchenBefore, day1KitchenAfter, 0.0001f, "Sombras claras must NOT boost direct ceiling lamps.");
                Assert.Greater(day1EqAfter, day1EqBefore * 2.0f, "Sombras claras must lift unlit corner ambient bounce noticeably.");
                Assert.Greater(day1GroundAfter, day1GroundBefore * 2.5f, "Sombras claras must lift shadowed lower corners/crevices.");
                Assert.GreaterOrEqual(RenderSettings.reflectionIntensity, 0.45f, "Sombras claras must lift indirect specular corner definition.");
                Assert.LessOrEqual(RenderSettings.ambientEquatorColor.b,
                    Mathf.Max(RenderSettings.ambientEquatorColor.r, RenderSettings.ambientEquatorColor.g) * 1.08f,
                    "Sombras claras ambient must remain warm-neutral without blue tint.");

                // Day 3 blackout: dark corners still lift cleanly while keeping scene contrast
                dm.SetBrightShadowsEnabled(false);
                applyLighting.Invoke(dm, new object[] { GameDay.Day3 });
                float day3EqBefore = RenderSettings.ambientEquatorColor.grayscale;
                dm.SetBrightShadowsEnabled(true);
                float day3EqAfter = RenderSettings.ambientEquatorColor.grayscale;
                Assert.Greater(day3EqAfter, day3EqBefore * 2.5f, "Sombras claras must keep Day 3 dark corners distinguishable.");

                sanity.SetBrightShadowsEnabled(true, applyImmediately: false);
                Assert.IsTrue(GetPrivateField<bool>(sanity, "brightShadowsEnabled"));
            }
            finally
            {
                InventoryState.ClearAll();
                dm.SetBrightShadowsEnabled(false);
                dm.SetUserBrightnessNormalized(0.50f);
                sanity.SetBrightShadowsEnabled(false, applyImmediately: false);
                sanity.SetUserBrightnessNormalized(0.50f, applyImmediately: false);
                MethodInfo applyDay1 = typeof(DayManager).GetMethod("ApplyAtmosphericLightingForDay", BindingFlags.Instance | BindingFlags.NonPublic);
                applyDay1?.Invoke(dm, new object[] { GameDay.Day1 });
            }
        }

        [Test]
        public void ResolutionAndVSyncDefaultsAndNotchClamping()
        {
            FieldInfo vsyncDefaultField = typeof(GameSettingsUI).GetField("DefaultVSyncEnabled", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(vsyncDefaultField);
            Assert.IsTrue((bool)vsyncDefaultField.GetValue(null), "VSync must default to enabled.");

            FieldInfo resDefaultField = typeof(GameSettingsUI).GetField("DefaultResolutionPreset", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(resDefaultField);
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            Assert.AreEqual(0, (int)resDefaultField.GetValue(null), "macOS default resolution preset must be 2560x1664 (0).");
#endif

            Resolution[] macCandidates =
            {
                new Resolution { width = 1470, height = 956, refreshRateRatio = new RefreshRate { numerator = 60, denominator = 1 } },
                new Resolution { width = 2560, height = 1664, refreshRateRatio = new RefreshRate { numerator = 60, denominator = 1 } },
                new Resolution { width = 2940, height = 1846, refreshRateRatio = new RefreshRate { numerator = 60, denominator = 1 } }
            };

            GameSettingsUI.ResolveTargetResolutionFromCandidates(0, macCandidates, 2940, 1912, 2940, 1846, out int w0, out int h0);
            Assert.AreEqual(2560, w0);
            Assert.AreEqual(1664, h0);

            GameSettingsUI.ResolveTargetResolutionFromCandidates(3, macCandidates, 2940, 1912, 2940, 1846, out int wNat, out int hNat);
            Assert.AreEqual(2940, wNat);
            Assert.AreEqual(1846, hNat, "Nativa preset should pick usable Screen.resolutions height (2940x1846), not notched systemHeight (1912).");

            GameSettingsUI.ResolveTargetResolutionFromCandidates(3, Array.Empty<Resolution>(), 1920, 1080, 1280, 720, out int wFallback, out int hFallback);
            Assert.AreEqual(1920, wFallback);
            Assert.AreEqual(1080, hFallback);

            // Async SetResolution dedupe: Awake requests (2560,1664), Start runs while Screen is still (3420,2148)
            Assert.IsTrue(GameSettingsUI.ShouldCallSetResolution(
                3420, 2148, FullScreenMode.FullScreenWindow,
                2560, 1664, FullScreenMode.FullScreenWindow,
                false, 0, 0, FullScreenMode.FullScreenWindow, false));
            Assert.IsFalse(GameSettingsUI.ShouldCallSetResolution(
                3420, 2148, FullScreenMode.FullScreenWindow,
                2560, 1664, FullScreenMode.FullScreenWindow,
                true, 2560, 1664, FullScreenMode.FullScreenWindow, false));
            Assert.IsTrue(GameSettingsUI.ShouldCallSetResolution(
                3420, 2148, FullScreenMode.FullScreenWindow,
                2560, 1664, FullScreenMode.FullScreenWindow,
                true, 2560, 1664, FullScreenMode.FullScreenWindow, true));
            Assert.IsFalse(GameSettingsUI.ShouldCallSetResolution(
                2560, 1664, FullScreenMode.FullScreenWindow,
                2560, 1664, FullScreenMode.FullScreenWindow,
                true, 2560, 1664, FullScreenMode.FullScreenWindow, true));

            UniversalRenderPipelineAsset pcRp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            Assert.IsNotNull(pcRp, "PC_RPAsset.asset must exist.");
            Assert.AreEqual(1.0f, pcRp.renderScale, 0.0001f, "PC_RPAsset renderScale must remain 1.00.");
            Assert.AreEqual(26f, pcRp.shadowDistance, 0.001f, "PC_RPAsset shadowDistance must remain 26m.");
            Assert.AreEqual(4096, pcRp.additionalLightsShadowmapResolution, "PC_RPAsset additional light shadow atlas must remain 4096.");

            // Verify GameSettingsUI Ultra tier configures runtime pipeline clone to exact Ultra specs (0 visual loss)
            RenderPipelineAsset savedPipe = QualitySettings.renderPipeline;
            var testGo = new GameObject("TempSettingsTest");
            try
            {
                QualitySettings.renderPipeline = pcRp;
                var settings = testGo.AddComponent<GameSettingsUI>();
                FieldInfo tierField = typeof(GameSettingsUI).GetField("currentQualityTier", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo applyQuality = typeof(GameSettingsUI).GetMethod("ApplyQualitySetting", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(tierField);
                Assert.IsNotNull(applyQuality);
                tierField.SetValue(settings, 3);
                applyQuality.Invoke(settings, null);

                var activeUrp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
                Assert.IsNotNull(activeUrp, "Ultra quality must apply a UniversalRenderPipelineAsset.");
                Assert.AreEqual(1.0f, activeUrp.renderScale, 0.0001f, "Ultra renderScale must remain 1.00.");
                Assert.AreEqual(4, activeUrp.msaaSampleCount, "Ultra MSAA must remain 4x.");
                Assert.AreEqual(26f, activeUrp.shadowDistance, 0.001f, "Ultra shadowDistance must remain 26m.");
                Assert.AreEqual(4096, activeUrp.additionalLightsShadowmapResolution, "Ultra additional light shadow atlas must remain 4096.");
            }
            finally
            {
                Object.DestroyImmediate(testGo);
                QualitySettings.renderPipeline = savedPipe;
            }
        }

        [Test]
        public void FaucetSplashParticleSystemBuildsWithoutDurationWarning()
        {
            var go = new GameObject("TempFaucetTest");
            var captured = new List<string>();
            void Handler(string cond, string trace, LogType type)
            {
                if (type == LogType.Warning || type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
                    captured.Add($"{type}: {cond}");
            }

            Application.logMessageReceived += Handler;
            try
            {
                var faucet = go.AddComponent<FaucetWaterController>();
                MethodInfo buildSplash = typeof(FaucetWaterController).GetMethod("BuildSplashParticleSystem", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(buildSplash);
                buildSplash.Invoke(faucet, null);

                ParticleSystem ps = go.GetComponentInChildren<ParticleSystem>(true);
                Assert.IsNotNull(ps, "Splash ParticleSystem should be created.");
                Assert.AreEqual(2.0f, ps.main.duration, 0.001f);
                Assert.IsFalse(ps.main.playOnAwake);

                foreach (string msg in captured)
                {
                    Assert.IsFalse(msg.Contains("Setting the duration"), $"Unexpected ParticleSystem duration warning: {msg}");
                }
            }
            finally
            {
                Application.logMessageReceived -= Handler;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ProceduralCRTFeedHoistedSamplersPreserveExactPixels()
        {
            var go = new GameObject("TempCRTTest");
            try
            {
                var crt = go.AddComponent<ProceduralCRTFeed>();
                MethodInfo samplePsy = typeof(ProceduralCRTFeed).GetMethod("SampleChannel420PsychedelicHorror", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo sampleBleed = typeof(ProceduralCRTFeed).GetMethod("SampleCanal420Bleed", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo sampleNews = typeof(ProceduralCRTFeed).GetMethod("SampleNews1994", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(samplePsy);
                Assert.IsNotNull(sampleBleed);
                Assert.IsNotNull(sampleNews);

                float[] progresses = { 0.20f, 0.55f, 0.85f, 0.89f };
                float[] times = { 0.23f, 1.35f }; // 0.23f*4=0.92 > 0.88 (strobe); 1.35f*4=5.40 -> 0.40 (non-strobe)
                object[] psyArgs = new object[8];
                object[] newsArgs = new object[8];

                foreach (float p in progresses)
                {
                    crt.SetHauntProgress(p);
                    foreach (float t in times)
                    {
                        for (int py = 12; py < 170; py += 31)
                        {
                            float ny = py / 179f;
                            for (int px = 15; px < 225; px += 41)
                            {
                                float nx = px / 239f;
                                ReferencePsychedelic(p, nx, ny, t, out float er, out float eg, out float eb);
                                psyArgs[0] = px; psyArgs[1] = py; psyArgs[2] = nx; psyArgs[3] = ny; psyArgs[4] = t;
                                samplePsy.Invoke(crt, psyArgs);
                                Assert.AreEqual(er, (float)psyArgs[5], 1e-5f, $"Psy R mismatch p={p} t={t} ({px},{py})");
                                Assert.AreEqual(eg, (float)psyArgs[6], 1e-5f, $"Psy G mismatch p={p} t={t} ({px},{py})");
                                Assert.AreEqual(eb, (float)psyArgs[7], 1e-5f, $"Psy B mismatch p={p} t={t} ({px},{py})");
                            }
                        }
                    }
                }

                // Verify Bleed against reference blend
                foreach (float t in times)
                {
                    for (int py = 10; py < 175; py += 27)
                    {
                        float ny = py / 179f;
                        for (int px = 20; px < 220; px += 39)
                        {
                            float nx = px / 239f;
                            float bandWave = Mathf.Sin(ny * 34f - t * 15f) + 0.55f * Mathf.Cos(ny * 78f + t * 23f);
                            float bleedPulse = 0.45f + 0.55f * Mathf.Sin(t * 4.8f);
                            float sliceThresh = Mathf.Lerp(0.75f, -0.35f, bleedPulse);
                            ReferencePsychedelic(0.89f, nx, ny, t, out float hr, out float hg, out float hb);
                            newsArgs[0] = px; newsArgs[1] = py; newsArgs[2] = nx; newsArgs[3] = ny; newsArgs[4] = t;
                            sampleNews.Invoke(crt, newsArgs);
                            float nr = (float)newsArgs[5], ng = (float)newsArgs[6], nb = (float)newsArgs[7];
                            float er, eg, eb;
                            if (bandWave > sliceThresh)
                            {
                                float mix = Mathf.Clamp01((bandWave - sliceThresh) * 1.85f);
                                er = mix >= 0.9999f ? hr : Mathf.Lerp(nr, hr, mix);
                                eg = mix >= 0.9999f ? hg : Mathf.Lerp(ng, hg, mix);
                                eb = mix >= 0.9999f ? hb : Mathf.Lerp(nb, hb, mix);
                            }
                            else
                            {
                                er = nr * 0.88f + hr * 0.14f;
                                eg = ng * 0.72f + hg * 0.08f;
                                eb = nb * 0.82f + hb * 0.16f;
                            }

                            psyArgs[0] = px; psyArgs[1] = py; psyArgs[2] = nx; psyArgs[3] = ny; psyArgs[4] = t;
                            sampleBleed.Invoke(crt, psyArgs);
                            Assert.AreEqual(er, (float)psyArgs[5], 1e-5f, $"Bleed R mismatch t={t} ({px},{py})");
                            Assert.AreEqual(eg, (float)psyArgs[6], 1e-5f, $"Bleed G mismatch t={t} ({px},{py})");
                            Assert.AreEqual(eb, (float)psyArgs[7], 1e-5f, $"Bleed B mismatch t={t} ({px},{py})");
                        }
                    }
                }

                // Shock frame min visible frames survives while shockMinVisibleFrames > 0
                crt.TriggerShockFrame(0.01f, residualOnly: true);
                Assert.AreEqual(CRTFeedMode.DeadGlassResidualPulse, crt.ActiveMode);
                MethodInfo upd = typeof(ProceduralCRTFeed).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(upd);
                for (int f = 0; f < 3; f++)
                {
                    upd.Invoke(crt, null);
                    Assert.AreEqual(CRTFeedMode.DeadGlassResidualPulse, crt.ActiveMode, $"Shock mode cleared too early on frame {f + 1}");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HorrorScreenOverlayFlashStaysVisibleForMinFrames()
        {
            var go = new GameObject("TempOverlayTest");
            try
            {
                var overlay = go.AddComponent<HorrorScreenOverlay>();
                overlay.TriggerSubliminalFlash(SubliminalType.Date1009, 0.095f, 0.42f);
                var img = GetPrivateField<UnityEngine.UI.RawImage>(overlay, "subliminalImage");
                Assert.IsNotNull(img);

                MethodInfo upd = typeof(HorrorScreenOverlay).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(upd);

                // Force timer near zero on frame 2 to simulate a huge dt hitch
                FieldInfo timerField = typeof(HorrorScreenOverlay).GetField("flashTimer", BindingFlags.Instance | BindingFlags.NonPublic);
                upd.Invoke(overlay, null);
                Assert.IsTrue(img.enabled, "Flash must render on frame 1.");
                Assert.Greater(img.color.a, 0.30f, "Frame 1 must render near peak intensity before decrementing.");

                timerField.SetValue(overlay, 0f);
                for (int f = 1; f < 4; f++)
                {
                    upd.Invoke(overlay, null);
                    Assert.IsTrue(img.enabled, $"Flash must stay visible for minimum frames (frame {f + 1}).");
                }

                overlay.ClearImmediate();
                Assert.IsFalse(img.enabled, "ClearImmediate must disable subliminal image immediately.");

                // Verify TriggerHighContrastStrobeFrame uses subtle static tear (<= 0.25f alpha) and not a 0.78f 2D face mask
                overlay.TriggerHighContrastStrobeFrame(0.085f);
                Texture2D staticTearTex = GetPrivateField<Texture2D>(overlay, "texStaticTear");
                Assert.AreSame(staticTearTex, img.texture, "TriggerHighContrastStrobeFrame must bind analog texStaticTear, not a 2D face mask.");
                Assert.LessOrEqual(GetPrivateField<float>(overlay, "flashMaxAlpha"), 0.25f);
                overlay.ClearImmediate();

                // Verify Margaret_Jumpscare HumanoidSpasmAnimator keeps upper-body joints kinematically coupled and resets cleanly
                DayManager dm = Object.FindAnyObjectByType<DayManager>();
                GameObject margaretObj = dm != null ? GetPrivateField<GameObject>(dm, "margaretJumpscareObject") : null;
                Assert.IsNotNull(margaretObj, "Margaret_Jumpscare must be wired on DayManager.");
                HumanoidSpasmAnimator spasm = margaretObj.GetComponent<HumanoidSpasmAnimator>();
                Assert.IsNotNull(spasm, "Margaret_Jumpscare must have HumanoidSpasmAnimator.");

                Transform head = margaretObj.transform.Find("Cabeca_Silhueta");
                Transform neck = margaretObj.transform.Find("Pescoco_Inclinado");
                Transform shoulders = margaretObj.transform.Find("Ombros_Curvados");
                Transform mouth = head != null ? head.Find("Boca_Escura") : null;
                Assert.IsNotNull(head);
                Assert.IsNotNull(neck);
                Assert.IsNotNull(shoulders);
                Assert.IsNotNull(mouth);

                Vector3 baseHeadPos = head.localPosition;
                Vector3 baseNeckPos = neck.localPosition;
                Vector3 baseShouldersPos = shoulders.localPosition;
                Vector3 baseMouthScale = mouth.localScale;

                MethodInfo lateUpd = typeof(HumanoidSpasmAnimator).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(lateUpd);

                spasm.SetMode(SpasmAnimMode.VisceralImpactSnap);
                lateUpd.Invoke(spasm, null);
                Assert.Greater(shoulders.localPosition.z, baseShouldersPos.z + 0.01f, "Shoulders must translate forward with torso pitch in VisceralImpactSnap.");
                Assert.Greater(neck.localPosition.z, baseNeckPos.z + 0.01f, "Neck must translate forward with torso pitch in VisceralImpactSnap.");
                Assert.Greater(head.localPosition.z, baseHeadPos.z + 0.02f, "Head must translate forward with neck/torso pitch in VisceralImpactSnap.");
                Assert.AreEqual(1.32f, mouth.localScale.y / baseMouthScale.y, 0.005f, "VisceralImpactSnap mouth vertical scale must be 1.32x.");

                spasm.SetMode(SpasmAnimMode.Idle);
                spasm.ResetPose();
                Assert.AreEqual(0f, Vector3.Distance(head.localPosition, baseHeadPos), 0.0001f);
                Assert.AreEqual(0f, Vector3.Distance(neck.localPosition, baseNeckPos), 0.0001f);
                Assert.AreEqual(0f, Vector3.Distance(shoulders.localPosition, baseShouldersPos), 0.0001f);

                // Verify Margaret chiaroscuro rim (1.35f) and face graze (0.035f) lights
                MethodInfo ensureChiaroscuro = typeof(DayManager).GetMethod("EnsureMargaretChiaroscuroLights", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo setChiaroscuro = typeof(DayManager).GetMethod("SetMargaretChiaroscuroLightsActive", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(ensureChiaroscuro);
                Assert.IsNotNull(setChiaroscuro);
                ensureChiaroscuro.Invoke(dm, null);
                setChiaroscuro.Invoke(dm, new object[] { true, 1.0f });
                Light rimLight = GetPrivateField<Light>(dm, "margaretFlashLight");
                Light grazeLight = GetPrivateField<Light>(dm, "margaretFaceGrazeLight");
                Assert.IsNotNull(rimLight);
                Assert.IsNotNull(grazeLight);
                Assert.IsTrue(rimLight.enabled);
                Assert.IsTrue(grazeLight.enabled);
                Assert.AreEqual(1.35f, rimLight.intensity, 0.001f, "Margaret backlight intensity must be 1.35f.");
                Assert.AreEqual(0.035f, grazeLight.intensity, 0.0005f, "Margaret face graze light intensity must be 0.035f.");
                setChiaroscuro.Invoke(dm, new object[] { false, 0f });
                Assert.IsFalse(rimLight.enabled);
                Assert.IsFalse(grazeLight.enabled);

                // Verify Day 3 ObjectiveHUD guidance for Phase 1/2 ("entrar_quarto_dia3")
                var hudGo = new GameObject("TempHUDTest");
                try
                {
                    var tempHud = hudGo.AddComponent<ObjectiveHUD>();
                    FieldInfo hudField = typeof(DayManager).GetField("objectiveHUD", BindingFlags.Instance | BindingFlags.NonPublic);
                    ObjectiveHUD origHud = (ObjectiveHUD)hudField.GetValue(dm);
                    try
                    {
                        hudField.SetValue(dm, tempHud);
                        MethodInfo popObj = typeof(DayManager).GetMethod("PopulateObjectivesForDay", BindingFlags.Instance | BindingFlags.NonPublic);
                        Assert.IsNotNull(popObj);
                        popObj.Invoke(dm, new object[] { GameDay.Day3 });

                        Assert.AreEqual("pegar_lanterna_dia3", GetPrivateField<string>(tempHud, "currentlyDisplayedId"));
                        tempHud.MarkCompleted("pegar_lanterna_dia3", silentCompletion: true);
                        tempHud.SetObjectiveVisible("engolir_capsula_dia3", true);
                        Assert.AreEqual("engolir_capsula_dia3", GetPrivateField<string>(tempHud, "currentlyDisplayedId"));
                        tempHud.MarkCompleted("engolir_capsula_dia3", silentCompletion: true);
                        tempHud.SetObjectiveVisible("restaurar_energia_dia3", true);
                        Assert.AreEqual("restaurar_energia_dia3", GetPrivateField<string>(tempHud, "currentlyDisplayedId"));

                        tempHud.MarkCompleted("pegar_fusivel_dia3", silentCompletion: true);
                        tempHud.MarkCompleted("restaurar_energia_dia3", silentCompletion: true);
                        tempHud.SetObjectiveVisible("retrato_margaret_dia3", true);
                        Assert.AreEqual("retrato_margaret_dia3", GetPrivateField<string>(tempHud, "currentlyDisplayedId"));

                        tempHud.MarkCompleted("retrato_margaret_dia3", silentCompletion: true);
                        tempHud.MarkCompleted("cofre_1009_dia3", silentCompletion: true);
                        tempHud.SetObjectiveVisible("destrancar_porta_dia3", true);
                        Assert.AreEqual("destrancar_porta_dia3", GetPrivateField<string>(tempHud, "currentlyDisplayedId"));

                        tempHud.MarkCompleted("destrancar_porta_dia3", silentCompletion: true);
                        tempHud.SetObjectiveVisible("entrar_quarto_dia3", true);
                        Assert.AreEqual("entrar_quarto_dia3", GetPrivateField<string>(tempHud, "currentlyDisplayedId"));
                    }
                    finally
                    {
                        hudField.SetValue(dm, origHud);
                    }
                }
                finally
                {
                    Object.DestroyImmediate(hudGo);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AudioAssetsAllHavePreloadAudioDataEnabled()
        {
            string audioRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "Audio"));
            Assert.IsTrue(Directory.Exists(audioRoot), "Assets/Audio directory must exist.");

            string[] metaFiles = Directory.GetFiles(audioRoot, "*.meta", SearchOption.AllDirectories);
            int checkedClips = 0;
            foreach (string metaPath in metaFiles)
            {
                if (!metaPath.EndsWith(".wav.meta", StringComparison.OrdinalIgnoreCase) &&
                    !metaPath.EndsWith(".mp3.meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                checkedClips++;
                string content = File.ReadAllText(metaPath);
                Assert.IsTrue(
                    content.Contains("preloadAudioData: 1"),
                    $"Audio clip meta must have preloadAudioData: 1 to prevent main-thread decode stalls: {metaPath}");
            }

            Assert.GreaterOrEqual(checkedClips, 60, "Expected at least 60 audio clip .meta files under Assets/Audio.");
        }

        [Test]
        public void ProceduralCRTFeedSingleRenderBurstAndTVDay3MinFrames()
        {
            var goA = new GameObject("TempCRT_Double");
            var goB = new GameObject("TempCRT_Single");
            var goTv = new GameObject("TempTV_Day3");
            try
            {
                var crtA = goA.AddComponent<ProceduralCRTFeed>();
                var crtB = goB.AddComponent<ProceduralCRTFeed>();

                FieldInfo rngField = typeof(ProceduralCRTFeed).GetField("rngState", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(rngField);
                uint initialSeed = (uint)rngField.GetValue(crtA);

                crtA.SetMode(CRTFeedMode.BroadcastChannel04);
                Assert.AreNotEqual(initialSeed, (uint)rngField.GetValue(crtA), "SetMode(renderImmediately:true) should advance rngState via RenderFrameNow.");
                rngField.SetValue(crtA, initialSeed);
                crtA.TriggerChannelSwitchBurst(0.26f);

                crtB.SetMode(CRTFeedMode.BroadcastChannel04, renderImmediately: false);
                Assert.AreEqual((uint)rngField.GetValue(crtB), initialSeed, "SetMode(renderImmediately:false) must skip redundant RenderFrameNow.");
                crtB.TriggerChannelSwitchBurst(0.26f);

                Texture2D texA = GetPrivateField<Texture2D>(crtA, "texture");
                Texture2D texB = GetPrivateField<Texture2D>(crtB, "texture");
                Assert.IsNotNull(texA);
                Assert.IsNotNull(texB);

                Color32[] pxA = texA.GetPixels32();
                Color32[] pxB = texB.GetPixels32();
                Assert.AreEqual(pxA.Length, pxB.Length);
                for (int i = 0; i < pxA.Length; i += 37)
                {
                    Assert.AreEqual(pxA[i], pxB[i], $"Single-render channel burst diverged at pixel {i}.");
                }

                // Verify TV Day 3 residual flash respects MinDay3ShockVisibleFrames even if timer hits 0
                var tv = goTv.AddComponent<TVController>();
                var lightGo = new GameObject("TempTvLight");
                lightGo.transform.SetParent(goTv.transform);
                Light tvLight = lightGo.AddComponent<Light>();
                tvLight.enabled = true;

                FieldInfo lightField = typeof(TVController).GetField("tvGlowLight", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo timerField = typeof(TVController).GetField("day3ShockFlashTimer", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo framesField = typeof(TVController).GetField("day3ShockFlashRemainingFrames", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo pulseMethod = typeof(TVController).GetMethod("UpdateDay3DeadGlassResidualPulse", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(lightField);
                Assert.IsNotNull(timerField);
                Assert.IsNotNull(framesField);
                Assert.IsNotNull(pulseMethod);

                lightField.SetValue(tv, tvLight);
                timerField.SetValue(tv, 0f);
                framesField.SetValue(tv, 4);

                for (int f = 0; f < 3; f++)
                {
                    pulseMethod.Invoke(tv, null);
                    Assert.IsTrue(tvLight.enabled, $"Day 3 TV flash turned off too early on frame {f + 1}.");
                }

                pulseMethod.Invoke(tv, null);
                Assert.IsFalse(tvLight.enabled, "Day 3 TV flash should turn off after minimum frames and zero timer.");
            }
            finally
            {
                Object.DestroyImmediate(goA);
                Object.DestroyImmediate(goB);
                Object.DestroyImmediate(goTv);
            }
        }

        [Test]
        public void Phase3StimulantWatchersDomesticHorrorAndTrapIntegrity()
        {
            GameObject casa = GameObject.Find("Casa_Base");
            GameObject propsD2 = FindInactiveRoot("Props_Dia2");
            GameObject propsD3 = FindInactiveRoot("Props_Dia3");
            DayManager dm = Object.FindAnyObjectByType<DayManager>();
            PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
            SanityEffects sanity = Object.FindAnyObjectByType<SanityEffects>();
            SafeBoxPuzzle safe = Object.FindAnyObjectByType<SafeBoxPuzzle>(FindObjectsInactive.Include);

            Assert.IsNotNull(casa, "Casa_Base missing.");
            Assert.IsNotNull(propsD2, "Props_Dia2 missing.");
            Assert.IsNotNull(propsD3, "Props_Dia3 missing.");
            Assert.IsNotNull(dm, "DayManager missing.");
            Assert.IsNotNull(pc, "PlayerController missing.");
            Assert.IsNotNull(sanity, "SanityEffects missing.");
            Assert.IsNotNull(safe, "SafeBoxPuzzle missing.");

            // 1. SafeBoxPuzzle keeps code 1009 free without artificial clue gates
            Assert.AreEqual("1009", GetPrivateField<string>(safe, "correctCode"));
            Assert.IsTrue(GetPrivateField<bool>(safe, "requiresCircuitPower"));

            // 2. Day 3 stimulant physical anchor + dry-swallow consumable (excluded from BatchingStatic)
            Transform stimBottleTr = FindChildRecursive(propsD3.transform, "Frasco_Estimulante_Dia3");
            Transform stimCapsTr = FindChildRecursive(propsD3.transform, "Capsulas_Estimulante_Dia3");
            Assert.IsNotNull(stimBottleTr, "Frasco_Estimulante_Dia3 missing from Props_Dia3.");
            Assert.IsNotNull(stimCapsTr, "Capsulas_Estimulante_Dia3 missing from Props_Dia3.");

            InspectableObject bottleInsp = stimBottleTr.GetComponent<InspectableObject>();
            Assert.IsNotNull(bottleInsp);
            string bottleDoc = GetPrivateField<string>(bottleInsp, "documentBody");
            Assert.IsNotNull(bottleDoc, "Stimulant bottle label document body must not be null.");
            Assert.IsTrue(bottleDoc.Contains("Fenproporex 25 mg") && bottleDoc.Contains("Cafeína"),
                "Stimulant bottle label document must specify Fenproporex 25 mg + Cafeína.");
            Assert.IsTrue(bottleDoc.Contains("DATA: --/--/--"),
                "Stimulant bottle label document must use the updated timeless redacted date.");
            Assert.IsFalse(bottleDoc.Contains("Restam apenas duas") || bottleDoc.Contains("duas cápsulas soltas"),
                "Stimulant bottle label document must stay timeless even after swallowing capsules.");

            InspectableObject capsInsp = stimCapsTr.GetComponent<InspectableObject>();
            Assert.IsNotNull(capsInsp);
            Assert.AreEqual("engolir_capsula_dia3", capsInsp.NormalizedTaskId);
            Assert.AreEqual("Engolir cápsula a seco", capsInsp.InteractionPrompt);
            Assert.IsTrue(GetPrivateField<bool>(capsInsp, "isMultiUseConsumable"));
            Assert.AreEqual(FoleySoundId.DryPillSwallow, GetPrivateField<FoleySoundId>(capsInsp, "interactSound"));
            GameObject[] stepVisuals = GetPrivateField<GameObject[]>(capsInsp, "consumableStepVisuals");
            Assert.IsNotNull(stepVisuals);
            Assert.AreEqual(2, stepVisuals.Length, "Expected 2 loose stimulant capsules for dry swallow.");
            AssertSubtreeNotBatchingStatic(stimCapsTr, "Capsulas_Estimulante_Dia3");

            Transform bottleLabelTr = FindChildRecursive(stimBottleTr, "Rotulo_Manipulado_D3");
            Assert.IsNotNull(bottleLabelTr, "Rotulo_Manipulado_D3 missing under Frasco_Estimulante_Dia3.");
            Assert.IsNotNull(bottleLabelTr.GetComponent<MeshRenderer>()?.sharedMaterial?.mainTexture, "Rotulo_Manipulado_D3 texture missing.");

            Transform blisterTr = FindChildRecursive(stimCapsTr, "Cartela_Rebite_D3");
            Assert.IsNotNull(blisterTr, "Cartela_Rebite_D3 missing under Capsulas_Estimulante_Dia3.");
            Transform blisterPrintTr = blisterTr.Find("Impressao_Cartela");
            Assert.IsNotNull(blisterPrintTr?.GetComponent<MeshRenderer>()?.sharedMaterial?.mainTexture, "Cartela_Rebite_D3 printed back texture missing.");
            for (int b = 1; b <= 6; b++)
            {
                Transform blisterCell = blisterTr.Find($"Bolha_{b}");
                Assert.IsNotNull(blisterCell, $"Cartela_Rebite_D3 missing Bolha_{b}.");
                Assert.AreEqual("M_Metal_Dark", blisterCell.GetComponent<MeshRenderer>()?.sharedMaterial?.name,
                    $"Cartela_Rebite_D3 Bolha_{b} should be popped (M_Metal_Dark) to match the two loose capsules.");
            }

            // 3. PlayerController continuous chemical modifiers & SanityEffects rebound modulation
            MethodInfo targetSpeedMethod = typeof(PlayerController).GetMethod("GetCurrentTargetMoveSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(targetSpeedMethod);
            bool savedActivating = pc.IsActivatingDrugActive;
            bool savedDepressant = pc.IsDepressantActive;
            try
            {
                pc.SetDepressantEffect(false);
                pc.SetActivatingDrugEffect(true);
                Assert.AreEqual(2.42f, (float)targetSpeedMethod.Invoke(pc, null), 0.005f, "Base Day 3 stimulant speed must be 2.42 m/s.");

                pc.SetChemicalEffectModifiers(0.5f, 0.90f, 0.70f, 0.04f, 1.5f, 1.0f, -3.0f);
                float midSpeed = (float)targetSpeedMethod.Invoke(pc, null);
                Assert.Greater(midSpeed, 1.815f * 0.89f);
                Assert.Less(midSpeed, 2.42f);

                pc.SetActivatingDrugEffect(false);
                pc.ResetChemicalEffectsImmediate();
                Assert.AreEqual(1.815f, (float)targetSpeedMethod.Invoke(pc, null), 0.005f, "Reset normal speed must return to 1.815 m/s.");

                sanity.SetStimulantModulation(0f, 0.85f, 0.65f);
                Assert.AreEqual(0.85f, sanity.CurrentChemicalRebound01, 0.001f);
                sanity.ClearStimulantModulationImmediate();
                Assert.AreEqual(0f, sanity.CurrentChemicalRebound01, 0.001f);
            }
            finally
            {
                pc.SetDepressantEffect(savedDepressant);
                pc.SetActivatingDrugEffect(savedActivating);
                pc.ResetChemicalEffectsImmediate();
                pc.SetHorrorCameraInfluence(0f, 0f);
                sanity.ClearStimulantModulationImmediate();
            }

            // 4. Watcher differentiation: SlumpedCian (D2) vs ChildWatcher (~1.18m, D3)
            Transform watcherD2Rig = FindChildRecursive(propsD2.transform, "Watcher_Dia2_Corredor");
            Transform watcherD2Fig = FindChildRecursive(propsD2.transform, "Watcher_Figura_D2");
            Transform watcherD3Rig = FindChildRecursive(propsD3.transform, "Watcher_Dia3_Corredor");
            Transform watcherD3Fig = FindChildRecursive(propsD3.transform, "Watcher_Figura_D3");
            Assert.IsNotNull(watcherD2Rig);
            Assert.IsNotNull(watcherD2Fig);
            Assert.IsNotNull(watcherD3Rig);
            Assert.IsNotNull(watcherD3Fig);

            WatcherHallucination whD2 = watcherD2Rig.GetComponent<WatcherHallucination>();
            WatcherHallucination whD3 = watcherD3Rig.GetComponent<WatcherHallucination>();
            Assert.IsNotNull(whD2);
            Assert.IsNotNull(whD3);
            Assert.AreEqual(1.46f, whD2.EyeHeightOffset, 0.01f, "Watcher_D2 eyeHeightOffset must be 1.46m.");
            Assert.AreEqual(0.98f, whD3.EyeHeightOffset, 0.02f, "Watcher_D3 eyeHeightOffset must align with child head (~0.98m).");
            Assert.IsTrue(GetPrivateField<bool>(whD3, "hideViaDarkBlackout"), "Watcher_D3 must retreat via 0.22s dark blackout.");

            Assert.IsNotNull(FindChildRecursive(watcherD2Fig, "Gravata_Frouxa"), "Watcher_Figura_D2 must exhibit SlumpedCian shirt/tie details.");
            Assert.IsNotNull(FindChildRecursive(watcherD3Fig, "Cabelo_Caido_Esq"), "Watcher_Figura_D3 must exhibit ChildWatcher shoulder hair.");
            AssertSubtreeNotBatchingStatic(watcherD2Fig, "Watcher_Figura_D2");
            AssertSubtreeNotBatchingStatic(watcherD3Fig, "Watcher_Figura_D3");

            string[] reqJoints = { "Torso_Esguio", "Ombros_Curvados", "Braco_Esq", "Braco_Dir", "Pescoco_Inclinado", "Cabeca_Silhueta" };
            foreach (Transform fig in new[] { watcherD2Fig, watcherD3Fig })
            {
                for (int j = 0; j < reqJoints.Length; j++)
                {
                    Assert.IsNotNull(fig.Find(reqJoints[j]), $"{fig.name} missing required joint {reqJoints[j]}.");
                }
                Assert.IsNotNull(fig.Find("Cabeca_Silhueta/Boca_Escura"), $"{fig.name} missing Cabeca_Silhueta/Boca_Escura.");

                Renderer[] renderers = fig.GetComponentsInChildren<Renderer>(true);
                Assert.Greater(renderers.Length, 0, $"{fig.name} must have at least one Renderer (SkinnedMeshRenderer).");
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer r = renderers[i];
                    if (r == null) continue;
                    Material[] mats = r.sharedMaterials;
                    if (mats != null)
                    {
                        for (int m = 0; m < mats.Length; m++)
                        {
                            if (mats[m] != null)
                            {
                                Assert.AreNotEqual("M_Porcelain_White", mats[m].name, $"{fig.name} must not use bright M_Porcelain_White eyes.");
                            }
                        }
                    }
                    Bounds b = ComputeSafeWorldBoundsForTest(r);
                    if (b.min.y < minY) minY = b.min.y;
                    if (b.max.y > maxY) maxY = b.max.y;
                }
                Assert.GreaterOrEqual(minY, -0.005f, $"{fig.name} sinks below floor (min.y={minY:F4}).");
                Assert.LessOrEqual(minY, 0.015f, $"{fig.name} floats above floor (min.y={minY:F4}).");
                if (fig == watcherD3Fig)
                {
                    Assert.GreaterOrEqual(maxY, 1.08f, $"ChildWatcher height too short ({maxY:F3}m).");
                    Assert.LessOrEqual(maxY, 1.26f, $"ChildWatcher height too tall ({maxY:F3}m, expected ~1.18m).");
                }
            }

            Material childGownMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Greybox/M_Child_Nightgown_Dusty.mat");
            Assert.IsNotNull(childGownMat, "M_Child_Nightgown_Dusty.mat missing.");
            Color.RGBToHSV(childGownMat.color, out _, out float childSat, out _);
            Assert.LessOrEqual(childSat, 0.45f, $"M_Child_Nightgown_Dusty saturation {childSat:F2} exceeds 0.45.");

            // 5. Domestic psychological horror: non-batched office chair, room guards, Espelho_Dia3, evolving Porta_Rua_Interagir, procedural audio beats
            Transform officeChair = FindChildRecursive(casa.transform, "Cadeira_Escritorio");
            Assert.IsNotNull(officeChair, "Cadeira_Escritorio missing.");
            AssertSubtreeNotBatchingStatic(officeChair, "Cadeira_Escritorio");

            Assert.IsTrue(PsychologicalHorrorDirector.IsInLivingRoomForTvAnomaly(new Vector3(-0.84f, 0f, -2.20f)),
                "Day 1 TV thermal anomaly must allow Living Room positions.");
            Assert.IsFalse(PsychologicalHorrorDirector.IsInLivingRoomForTvAnomaly(new Vector3(-4.02f, 0f, -5.42f)),
                "Day 1 TV thermal anomaly must NOT fire from inside Kitchen behind Parede_Cozinha_Sala.");
            Assert.IsFalse(PsychologicalHorrorDirector.IsInLivingRoomForTvAnomaly(new Vector3(4.00f, 0f, -2.50f)),
                "Day 1 TV thermal anomaly must NOT fire from inside Office.");

            Assert.IsTrue(PsychologicalHorrorDirector.IsInCorridorForLockedDoor(new Vector3(1.78f, 0f, 3.20f)),
                "Internal latch beat must fire inside north corridor approaching Porta_Quarto_Pivot.");
            Assert.IsFalse(PsychologicalHorrorDirector.IsInCorridorForLockedDoor(new Vector3(0.32f, 0f, 5.78f)),
                "Internal latch beat must NOT fire from inside Master Bedroom behind Parede_Quarto_Corredor.");

            Transform mirrorD3 = FindChildRecursive(propsD3.transform, "Espelho_Dia3");
            Assert.IsNotNull(mirrorD3, "Espelho_Dia3 missing from Props_Dia3.");
            Assert.AreEqual("espelho_dia3_opc", mirrorD3.GetComponent<InspectableObject>()?.NormalizedTaskId);

            // Day 2 reflective planar mirror + 3D doorway apparition + close-range sink tape guard + zero corridor perspective warp + no phone ringing
            Transform mirrorApparitionTr = FindChildRecursive(propsD2.transform, "Aparicao_Porta_Banheiro_D2");
            Assert.IsNotNull(mirrorApparitionTr, "Aparicao_Porta_Banheiro_D2 missing from Props_Dia2.");
            AssertSubtreeNotBatchingStatic(mirrorApparitionTr, "Aparicao_Porta_Banheiro_D2");
            Assert.IsNotNull(mirrorApparitionTr.Find("Cabeca_Silhueta/Boca_Escura"), "Aparicao_Porta_Banheiro_D2 missing Cabeca_Silhueta/Boca_Escura.");
            Assert.IsNotNull(mirrorApparitionTr.GetComponent<HumanoidSpasmAnimator>(), "Aparicao_Porta_Banheiro_D2 must have HumanoidSpasmAnimator.");

            Transform tapeD2Tr = FindChildRecursive(propsD2.transform, "Fita_Cassete_D2");
            Assert.IsNotNull(tapeD2Tr, "Fita_Cassete_D2 missing from Props_Dia2.");
            InspectableObject tapeD2Insp = tapeD2Tr.GetComponent<InspectableObject>();
            Assert.IsNotNull(tapeD2Insp);
            Vector3 tapeWorldPos = tapeD2Tr.position;
            Assert.IsFalse(tapeD2Insp.IsWithinInteractionConstraints(new Vector3(-4.50f, 1.54f, 3.20f), Vector3.forward, tapeWorldPos),
                "Fita_Cassete_D2 must reject interaction from far away (~2.5m from doorway).");
            Assert.IsFalse(tapeD2Insp.IsWithinInteractionConstraints(new Vector3(-3.90f, 1.54f, 4.95f), Vector3.back, tapeWorldPos),
                "Fita_Cassete_D2 must reject interaction when facing away from sink/mirror.");
            Assert.IsTrue(tapeD2Insp.IsWithinInteractionConstraints(new Vector3(-3.90f, 1.54f, 4.95f), Vector3.forward, tapeWorldPos),
                "Fita_Cassete_D2 must allow interaction when standing directly in front of sink facing mirror.");

            // Verify corridor proximity dread never warps FOV, camera roll, or lens distortion (corridor perspective bug permanently fixed)
            FieldInfo proxDreadFi = typeof(SanityEffects).GetField("currentProximityDread", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo spikeFi = typeof(SanityEffects).GetField("spikeWeight", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo applyEffectsMi = typeof(SanityEffects).GetMethod("ApplyEffectsToVolume", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo pcRollFi = typeof(PlayerController).GetField("ambientRollOffset", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo pcFovFi = typeof(PlayerController).GetField("ambientFovOffset", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo lensDistFi = typeof(SanityEffects).GetField("lensDistortion", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(proxDreadFi, "SanityEffects.currentProximityDread missing.");
            Assert.IsNotNull(spikeFi, "SanityEffects.spikeWeight missing.");
            Assert.IsNotNull(applyEffectsMi, "SanityEffects.ApplyEffectsToVolume missing.");
            Assert.IsNotNull(pcRollFi, "PlayerController.ambientRollOffset missing.");
            Assert.IsNotNull(pcFovFi, "PlayerController.ambientFovOffset missing.");
            float savedProxDread = (float)proxDreadFi.GetValue(sanity);
            float savedSpike = (float)spikeFi.GetValue(sanity);
            try
            {
                sanity.ClearStimulantModulationImmediate();
                spikeFi.SetValue(sanity, 0f);
                proxDreadFi.SetValue(sanity, 1.0f);
                applyEffectsMi.Invoke(sanity, null);
                Assert.AreEqual(0f, (float)pcFovFi.GetValue(pc), 0.001f,
                    "Corridor proximity dread must NEVER warp FOV (perspective bug regression).");
                Assert.AreEqual(0f, (float)pcRollFi.GetValue(pc), 0.001f,
                    "Corridor proximity dread must NEVER tilt camera roll (perspective bug regression).");
                LensDistortion ld = lensDistFi?.GetValue(sanity) as LensDistortion;
                if (ld != null)
                {
                    Assert.AreEqual(0f, ld.intensity.value, 0.001f,
                        "Corridor proximity dread must NEVER add fish-eye LensDistortion.");
                    Assert.AreEqual(1.0f, ld.scale.value, 0.001f,
                        "Idle LensDistortion scale must remain 1.00 (no constant 3% zoom warp).");
                }
            }
            finally
            {
                proxDreadFi.SetValue(sanity, savedProxDread);
                spikeFi.SetValue(sanity, savedSpike);
                applyEffectsMi.Invoke(sanity, null);
            }

            Assert.IsNull(typeof(PsychologicalHorrorDirector).GetField("phoneHalfRingPlayedThisDay", BindingFlags.Instance | BindingFlags.NonPublic),
                "Phone ringing beat must stay removed from PsychologicalHorrorDirector across all days.");

            // Day 2 & Day 3 mirror immediate flash must update state cleanly without starting EditMode coroutines or mutating bathroom lights
            MethodInfo applyLighting = typeof(DayManager).GetMethod("ApplyAtmosphericLightingForDay", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(applyLighting);
            MirrorHauntController mirrorHaunt = Object.FindAnyObjectByType<MirrorHauntController>(FindObjectsInactive.Include);
            Assert.IsNotNull(mirrorHaunt, "MirrorHauntController missing.");
            Assert.AreEqual(mirrorApparitionTr.gameObject, mirrorHaunt.DoorwayApparitionObject,
                "MirrorHauntController must link Aparicao_Porta_Banheiro_D2.");
            Shader wiredMirrorShader = GetPrivateField<Shader>(mirrorHaunt, "planarMirrorShader");
            Assert.IsNotNull(wiredMirrorShader, "MirrorHauntController must have planarMirrorShader assigned.");
            Assert.AreEqual("Canal420/PlanarMirror", wiredMirrorShader.name, "Planar mirror shader mismatch.");
            FieldInfo mirrorDay2TriggeredField = typeof(MirrorHauntController).GetField("day2Triggered", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo mirrorDay3TriggeredField = typeof(MirrorHauntController).GetField("day3Triggered", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo mirrorRunningField = typeof(MirrorHauntController).GetField("isRunningSequence", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo mirrorPeriphTimerField = typeof(MirrorHauntController).GetField("peripheralShowTimer", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mirrorDay2TriggeredField);
            Assert.IsNotNull(mirrorDay3TriggeredField);
            Assert.IsNotNull(mirrorRunningField);
            Assert.IsNotNull(mirrorPeriphTimerField);
            Light bathroomCeiling = FindLight(GetPrivateField<Light[]>(dm, "houseInteriorLights"), "luz_banheiro");
            Light bathroomSconce = FindLight(GetPrivateField<Light[]>(dm, "secondaryMotivatedLights"), "luz_arandela_banheiro");
            try
            {
                applyLighting.Invoke(dm, new object[] { GameDay.Day1 });
                Assert.IsTrue(bathroomCeiling.enabled, "Precondition: bathroom ceiling light must be ON before mirror EditMode check.");
                Assert.IsTrue(bathroomSconce.enabled, "Precondition: bathroom sconce light must be ON before mirror EditMode check.");

                mirrorHaunt.ConfigureForDay(GameDay.Day2);
                mirrorHaunt.TriggerImmediateMirrorFlash();
                Assert.IsTrue((bool)mirrorDay2TriggeredField.GetValue(mirrorHaunt),
                    "TriggerImmediateMirrorFlash on Day 2 must mark day2Triggered.");
                if (!Application.isPlaying)
                {
                    Assert.IsFalse((bool)mirrorRunningField.GetValue(mirrorHaunt),
                        "TriggerImmediateMirrorFlash on Day 2 must not start coroutines in EditMode.");
                    Assert.IsTrue(bathroomCeiling.enabled,
                        "TriggerImmediateMirrorFlash on Day 2 in EditMode must not mutate bathroom ceiling light.");
                    Assert.IsTrue(bathroomSconce.enabled,
                        "TriggerImmediateMirrorFlash on Day 2 in EditMode must not mutate bathroom sconce light.");
                }
                mirrorHaunt.TriggerImmediateMirrorFlash();
                if (!Application.isPlaying)
                {
                    Assert.IsFalse((bool)mirrorRunningField.GetValue(mirrorHaunt),
                        "Repeat TriggerImmediateMirrorFlash on Day 2 must not start coroutines in EditMode.");
                    Assert.IsTrue(bathroomCeiling.enabled,
                        "Repeat TriggerImmediateMirrorFlash on Day 2 in EditMode must not mutate bathroom ceiling light.");
                    Assert.IsTrue(bathroomSconce.enabled,
                        "Repeat TriggerImmediateMirrorFlash on Day 2 in EditMode must not mutate bathroom sconce light.");
                }

                mirrorHaunt.ConfigureForDay(GameDay.Day3);
                mirrorPeriphTimerField.SetValue(mirrorHaunt, 0.85f);
                mirrorHaunt.TriggerImmediateMirrorFlash();
                Assert.IsTrue((bool)mirrorDay3TriggeredField.GetValue(mirrorHaunt),
                    "TriggerImmediateMirrorFlash on Day 3 must mark day3Triggered.");
                Assert.AreEqual(0f, (float)mirrorPeriphTimerField.GetValue(mirrorHaunt), 0.0001f,
                    "TriggerImmediateMirrorFlash on Day 3 must clear peripheralShowTimer to avoid duplicate GlassTap.");
                if (!Application.isPlaying)
                {
                    Assert.IsFalse((bool)mirrorRunningField.GetValue(mirrorHaunt),
                        "TriggerImmediateMirrorFlash on Day 3 must not start coroutines in EditMode.");
                    Assert.IsTrue(bathroomCeiling.enabled,
                        "TriggerImmediateMirrorFlash on Day 3 in EditMode must not mutate bathroom ceiling light.");
                    Assert.IsTrue(bathroomSconce.enabled,
                        "TriggerImmediateMirrorFlash on Day 3 in EditMode must not mutate bathroom sconce light.");
                }
            }
            finally
            {
                mirrorHaunt.ConfigureForDay(GameDay.Day1);
                applyLighting.Invoke(dm, new object[] { GameDay.Day1 });
            }

            // DayManager Day 3 linear gate: flashlight -> mandatory pills -> breaker/portrait + Day 2 end pose continuity
            Transform flashlightD3Tr = FindChildRecursive(propsD3.transform, "Lanterna_Dia3");
            Assert.IsNotNull(flashlightD3Tr, "Lanterna_Dia3 pickup missing from Props_Dia3.");
            Assert.AreEqual("pegar_lanterna_dia3", flashlightD3Tr.GetComponent<InspectableObject>()?.NormalizedTaskId);
            Assert.IsNotNull(pc.GetComponent<FlashlightController>(), "Player must carry FlashlightController.");

            FieldInfo curDayField = typeof(DayManager).GetField("currentDay", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo boostActiveField = typeof(DayManager).GetField("isStimulantBoostOrReboundActive", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo trapWaitField = typeof(DayManager).GetField("day3DoorTrapWaitingForEntry", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo mandatoryPillField = typeof(DayManager).GetField("day3MandatoryPillTaken", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(curDayField);
            Assert.IsNotNull(boostActiveField);
            Assert.IsNotNull(trapWaitField);
            Assert.IsNotNull(mandatoryPillField);
            try
            {
                InventoryState.ClearAll();
                curDayField.SetValue(dm, GameDay.Day1);
                boostActiveField.SetValue(dm, false);
                trapWaitField.SetValue(dm, false);
                mandatoryPillField.SetValue(dm, false);
                Assert.IsFalse(dm.CanConsumeStimulantCapsule(out _), "Stimulant capsule must not be consumable on Day 1.");

                curDayField.SetValue(dm, GameDay.Day3);
                Assert.IsFalse(dm.CanConsumeStimulantCapsule(out _), "Stimulant capsule must wait until Day 3 flashlight is collected.");
                Assert.IsTrue(dm.IsDay3PrePillGateBlocked(out _), "Breaker/portrait must be blocked before Day 3 flashlight.");

                InventoryState.AddItem("lanterna_dia3");
                Assert.IsTrue(dm.CanConsumeStimulantCapsule(out _), "Stimulant capsule must be consumable on Day 3 after getting flashlight.");
                Assert.IsTrue(dm.IsDay3PrePillGateBlocked(out _), "Breaker/portrait must stay blocked until mandatory Day 3 pill is swallowed.");

                mandatoryPillField.SetValue(dm, true);
                Assert.IsFalse(dm.IsDay3PrePillGateBlocked(out _), "Breaker/portrait gate must unlock after flashlight + mandatory pill.");

                boostActiveField.SetValue(dm, true);
                Assert.IsFalse(dm.CanConsumeStimulantCapsule(out _), "Stimulant capsule must respect cooldown while boost/rebound is active.");

                boostActiveField.SetValue(dm, false);
                trapWaitField.SetValue(dm, true);
                Assert.IsFalse(dm.CanConsumeStimulantCapsule(out _), "Stimulant capsule must be blocked during Quarto_Filhas door trap.");
            }
            finally
            {
                InventoryState.ClearAll();
                curDayField.SetValue(dm, GameDay.Day1);
                boostActiveField.SetValue(dm, false);
                trapWaitField.SetValue(dm, false);
                mandatoryPillField.SetValue(dm, false);
            }

            Transform frontDoorTr = FindChildRecursive(casa.transform, "Porta_Rua_Interagir");
            Assert.IsNotNull(frontDoorTr, "Porta_Rua_Interagir missing from Casa_Base.");
            InspectableObject frontDoorInsp = frontDoorTr.GetComponent<InspectableObject>();
            Assert.IsNotNull(frontDoorInsp);

            MethodInfo syncFrontDoor = typeof(DayManager).GetMethod("SyncFrontDoorForDay", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo safeUnlockedField = typeof(DayManager).GetField("day3SafeUnlocked", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(syncFrontDoor);
            Assert.IsNotNull(safeUnlockedField);
            try
            {
                safeUnlockedField.SetValue(dm, false);
                syncFrontDoor.Invoke(dm, new object[] { GameDay.Day1 });
                string d1Sub = GetPrivateField<string>(frontDoorInsp, "thoughtSubtitle");
                syncFrontDoor.Invoke(dm, new object[] { GameDay.Day2 });
                string d2Sub = GetPrivateField<string>(frontDoorInsp, "thoughtSubtitle");
                syncFrontDoor.Invoke(dm, new object[] { GameDay.Day3 });
                string d3PreSub = GetPrivateField<string>(frontDoorInsp, "thoughtSubtitle");
                safeUnlockedField.SetValue(dm, true);
                syncFrontDoor.Invoke(dm, new object[] { GameDay.Day3 });
                string d3PostSub = GetPrivateField<string>(frontDoorInsp, "thoughtSubtitle");

                Assert.AreNotEqual(d1Sub, d2Sub, "Porta_Rua_Interagir must evolve between Day 1 and Day 2.");
                Assert.AreNotEqual(d2Sub, d3PreSub, "Porta_Rua_Interagir must evolve between Day 2 and Day 3.");
                Assert.AreNotEqual(d3PreSub, d3PostSub, "Porta_Rua_Interagir must evolve on Day 3 after unlocking the safe.");
            }
            finally
            {
                safeUnlockedField.SetValue(dm, false);
                syncFrontDoor.Invoke(dm, new object[] { GameDay.Day1 });
            }

            // Procedural Phase 3 audio clips synthesize with valid non-zero waveforms (zero asset preload cost)
            string[] synthMethods =
            {
                "CreateDryPillSwallow",
                "CreateHeavyHeartbeatThud",
                "CreateTVGlassThermalCrack",
                "CreatePhoneHalfRingAborted",
                "CreateInternalLatchForcedSlow"
            };
            var rng = new System.Random(4209);
            for (int i = 0; i < synthMethods.Length; i++)
            {
                MethodInfo mi = typeof(Canal420AudioManager).GetMethod(synthMethods[i], BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(mi, $"Missing procedural audio synthesizer {synthMethods[i]}.");
                AudioClip clip = mi.Invoke(null, new object[] { rng }) as AudioClip;
                try
                {
                    Assert.IsNotNull(clip, $"{synthMethods[i]} returned null AudioClip.");
                    Assert.Greater(clip.samples, 1000, $"{synthMethods[i]} clip too short.");
                    float[] data = new float[clip.samples];
                    Assert.IsTrue(clip.GetData(data, 0));
                    float peak = 0f;
                    for (int s = 0; s < data.Length; s++)
                    {
                        float a = Mathf.Abs(data[s]);
                        if (a > peak) peak = a;
                    }
                    Assert.Greater(peak, 0.10f, $"{synthMethods[i]} synthesized silent waveform.");
                }
                finally
                {
                    if (clip != null) Object.DestroyImmediate(clip);
                }
            }

            // 6. Margaret trap pitch-black darkness helper toggles cleanly and restores cleanly
            MethodInfo setPitchBlack = typeof(DayManager).GetMethod("SetPitchBlackTrapDarkness", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo restoreTrapAmb = typeof(DayManager).GetMethod("RestoreJumpscareBlackoutAmbient", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(setPitchBlack);
            Assert.IsNotNull(restoreTrapAmb);
            try
            {
                setPitchBlack.Invoke(dm, null);
                Assert.AreEqual(Color.black, RenderSettings.ambientSkyColor);
                Assert.AreEqual(Color.black, RenderSettings.ambientEquatorColor);
                Assert.AreEqual(Color.black, RenderSettings.ambientGroundColor);
                Assert.AreEqual(0f, RenderSettings.reflectionIntensity, 0.0001f);

                restoreTrapAmb.Invoke(dm, null);
                Assert.Greater(RenderSettings.ambientEquatorColor.maxColorComponent, 0.01f);
                Assert.Greater(RenderSettings.reflectionIntensity, 0.20f);
            }
            finally
            {
                applyLighting.Invoke(dm, new object[] { GameDay.Day1 });
            }
        }

        private static void AssertSubtreeNotBatchingStatic(Transform root, string label)
        {
            Transform[] nodes = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] == null) continue;
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(nodes[i].gameObject);
                Assert.AreEqual(0, (int)(flags & StaticEditorFlags.BatchingStatic),
                    $"{label} child '{nodes[i].name}' must NOT have BatchingStatic flag.");
            }
        }

        private static Bounds ComputeSafeWorldBoundsForTest(Renderer r)
        {
            if (r == null) return new Bounds(Vector3.zero, Vector3.zero);
            Mesh sharedMesh = null;
            if (r is SkinnedMeshRenderer smr)
            {
                sharedMesh = smr.sharedMesh;
            }
            else
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null) sharedMesh = mf.sharedMesh;
            }

            if (sharedMesh != null)
            {
                Vector3[] verts = sharedMesh.vertices;
                Matrix4x4 m = r.transform.localToWorldMatrix;
                if (verts != null && verts.Length > 0)
                {
                    Bounds wb = new Bounds(m.MultiplyPoint3x4(verts[0]), Vector3.zero);
                    for (int i = 1; i < verts.Length; i++)
                    {
                        wb.Encapsulate(m.MultiplyPoint3x4(verts[i]));
                    }
                    return wb;
                }
            }
            return r.bounds;
        }

        private static void ReferencePsychedelic(float p, float nx, float ny, float t, out float r, out float g, out float b)
        {
            float dx = (nx - 0.5f) * 1.333f;
            float dy = ny - 0.5f;
            float rad = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = Mathf.Atan2(dy, dx);
            float meltY = p * 0.24f * Mathf.Sin(nx * 24f - t * 3.4f) * (0.5f + 0.5f * Mathf.Cos(nx * 9f + t));
            float warpAng = ang + 0.42f * Mathf.Sin(rad * 11f - t * 2.2f);
            float warpRad = rad + meltY + 0.065f * Mathf.Sin(warpAng * 6f + t * 2.8f);
            float blobA = Mathf.Exp(-(((nx - (0.5f + 0.19f * Mathf.Sin(t * 1.4f))) * 3.1f) * ((nx - (0.5f + 0.19f * Mathf.Sin(t * 1.4f))) * 3.1f) +
                                      ((ny - (0.5f + 0.16f * Mathf.Cos(t * 1.1f))) * 3.4f) * ((ny - (0.5f + 0.16f * Mathf.Cos(t * 1.1f))) * 3.4f)));
            float blobB = Mathf.Exp(-(((nx - (0.5f - 0.22f * Mathf.Cos(t * 1.7f))) * 3.4f) * ((nx - (0.5f - 0.22f * Mathf.Cos(t * 1.7f))) * 3.4f) +
                                      ((ny - (0.5f - 0.14f * Mathf.Sin(t * 1.5f))) * 3.0f) * ((ny - (0.5f - 0.14f * Mathf.Sin(t * 1.5f))) * 3.0f)));
            float ring = Mathf.Sin(warpRad * (22f + p * 8f) - t * (3.8f + p * 4.2f) + warpAng * 2f + (blobA - blobB) * 3.2f);
            float ribbon = Mathf.Cos(dx * 11f + Mathf.Sin(dy * 9f - t * 2.5f) * 2.2f + t * 1.9f);
            float oil = 0.5f + 0.5f * Mathf.Sin((ring + ribbon) * 2.0f + (blobA + blobB) * 2.4f + Mathf.Cos(warpAng * 5f - t * 1.7f));
            float hue = Mathf.Repeat(oil * 0.72f + t * 0.18f + rad * 0.52f + blobA * 0.14f, 1f);
            Color acid = Color.HSVToRGB(hue, 0.94f, 0.48f + 0.50f * oil);
            if (p < 0.38f) { r = acid.r; g = acid.g; b = acid.b; return; }
            if (p < 0.76f)
            {
                float rot = Mathf.Lerp(0f, 0.82f, (p - 0.38f) / 0.38f);
                r = Mathf.Lerp(acid.r, acid.r * 0.76f + 0.24f * oil, rot);
                g = Mathf.Lerp(acid.g, acid.g * 0.28f, rot);
                b = Mathf.Lerp(acid.b, acid.b * 0.52f, rot);
                float figureSway = 0.50f + 0.025f * Mathf.Sin(t * 2.1f + ny * 5f);
                float figureWidth = 0.085f + (0.72f - ny) * 0.11f;
                if (ny < 0.78f && Mathf.Abs(nx - figureSway) < figureWidth && rot > 0.25f)
                {
                    float bodyMask = 0.48f * Mathf.Clamp01((rot - 0.25f) / 0.50f);
                    r = Mathf.Lerp(r, 0.04f, bodyMask);
                    g = Mathf.Lerp(g, 0.02f, bodyMask);
                    b = Mathf.Lerp(b, 0.06f, bodyMask);
                }
                float edgeDark = Mathf.SmoothStep(0.38f, 0.86f, rad) * rot * 0.55f;
                r *= (1f - edgeDark);
                g *= (1f - edgeDark);
                b *= (1f - edgeDark);
                return;
            }
            float strobeBeat = Mathf.Repeat(t * 4.0f, 1f);
            if (strobeBeat > 0.88f)
            {
                r = 0.04f; g = 0.02f; b = 0.02f;
                bool openDoor = nx > 0.37f && nx < 0.63f && ny > 0.10f && ny < 0.88f;
                bool doorThreshold = nx > 0.42f && nx < 0.58f && ny > 0.12f && ny < 0.84f;
                bool doorwayShadow = Mathf.Abs(nx - 0.50f) < 0.07f && ny > 0.12f && ny < 0.68f;
                if (doorwayShadow) { r = 0.01f; g = 0.01f; b = 0.02f; }
                else if (doorThreshold) { r = 0.96f; g = 0.16f; b = 0.14f; }
                else if (openDoor) { r = 0.52f; g = 0.08f; b = 0.08f; }
                else if (ny < 0.12f) { r = 0.24f; g = 0.04f; b = 0.04f; }
                return;
            }
            float vortex = Mathf.Pow(oil, 1.55f);
            float pulseWave = 0.5f + 0.5f * Mathf.Sin(t * 7.0f + rad * 11f);
            r = 0.16f + 0.62f * vortex + 0.12f * acid.r;
            g = 0.02f + 0.07f * acid.g;
            b = 0.08f + 0.36f * pulseWave + 0.14f * acid.b;
            float doorWidth = 0.11f + 0.04f * Mathf.Sin(t * 2.6f);
            bool inDoorVoid = Mathf.Abs(nx - 0.50f) < doorWidth && ny > 0.12f && ny < 0.86f;
            if (inDoorVoid)
            {
                float depthShade = Mathf.SmoothStep(0.0f, doorWidth, Mathf.Abs(nx - 0.50f));
                float voidMask = Mathf.Lerp(0.92f, 0.42f, depthShade);
                r = Mathf.Lerp(r, 0.02f, voidMask);
                g = Mathf.Lerp(g, 0.01f, voidMask);
                b = Mathf.Lerp(b, 0.03f, voidMask);
            }
        }

        [Test]
        public void Phase4VisualBugFixesAndWholeHouse3DModelOverhaul()
        {
            GameObject casa = GameObject.Find("Casa_Base");
            Assert.IsNotNull(casa, "Casa_Base not found.");

            // 1. Bug #V1 fixed: all 4 stove burner grates must lie horizontally (XZ), never vertically (XY)
            Transform stoveTr = FindChildRecursive(casa.transform, "Fogao");
            Assert.IsNotNull(stoveTr, "Fogao not found.");
            for (int ix = 0; ix < 2; ix++)
            {
                for (int iz = 0; iz < 2; iz++)
                {
                    Assert.IsNull(FindChildRecursive(stoveTr, $"Aro_Trempe_{ix}_{iz}_Sup"),
                        $"Vertical XY bar Aro_Trempe_{ix}_{iz}_Sup must not exist on horizontal stove grate.");
                    Transform barN = FindChildRecursive(stoveTr, $"Aro_Trempe_{ix}_{iz}_N");
                    Transform barO = FindChildRecursive(stoveTr, $"Aro_Trempe_{ix}_{iz}_O");
                    Assert.IsNotNull(barN, $"Horizontal stove grate bar Aro_Trempe_{ix}_{iz}_N missing.");
                    Assert.IsNotNull(barO, $"Horizontal stove grate bar Aro_Trempe_{ix}_{iz}_O missing.");
                    Bounds bN = barN.GetComponent<MeshRenderer>().bounds;
                    Assert.LessOrEqual(bN.size.y, 0.020f,
                        $"Stove grate Aro_Trempe_{ix}_{iz}_N is too tall ({bN.size.y:F3}m) — expected flat horizontal grate!");
                }
            }

            // 2. Bug #V2 fixed: Margaret's mirrored vanity tray rim on Comoda_Quarto must lie horizontally (XZ)
            Transform dresserTr = FindChildRecursive(casa.transform, "Comoda_Quarto");
            Assert.IsNotNull(dresserTr, "Comoda_Quarto not found.");
            Assert.IsNull(FindChildRecursive(dresserTr, "Borda_Bandeja_Sup"),
                "Vertical XY bar Borda_Bandeja_Sup must not exist on horizontal vanity tray.");
            Transform trayBarN = FindChildRecursive(dresserTr, "Borda_Bandeja_N");
            Transform trayBarL = FindChildRecursive(dresserTr, "Borda_Bandeja_L");
            Assert.IsNotNull(trayBarN, "Horizontal vanity tray rim Borda_Bandeja_N missing.");
            Assert.IsNotNull(trayBarL, "Horizontal vanity tray rim Borda_Bandeja_L missing.");
            Bounds trayBounds = trayBarN.GetComponent<MeshRenderer>().bounds;
            Assert.LessOrEqual(trayBounds.size.y, 0.025f,
                $"Vanity tray rim Borda_Bandeja_N must be flat horizontal (actual height {trayBounds.size.y:F3}m).");

            // 3. Bug #V3 fixed: text/prompt divergences corrected
            Transform rackInspTr = FindChildRecursive(casa.transform, "Compartimento_Rack_Interagir");
            Assert.IsNotNull(rackInspTr, "Compartimento_Rack_Interagir not found.");
            InspectableObject rackInsp = rackInspTr.GetComponent<InspectableObject>();
            Assert.IsNotNull(rackInsp, "Compartimento_Rack_Interagir must carry an InspectableObject.");
            string rackSub = GetPrivateField<string>(rackInsp, "thoughtSubtitle") ?? string.Empty;
            Assert.IsTrue(rackSub.Contains("fitas velhas") && !rackSub.Contains("gaveta do fundo"),
                $"Rack subtitle still references wrong drawer wording: '{rackSub}'");

            Transform wdTr = FindChildRecursive(casa.transform, "Portas_GuardaRoupa_Interagir");
            Assert.IsNotNull(wdTr, "Portas_GuardaRoupa_Interagir not found.");
            AnimatedContainerInteractable wdAnim = wdTr.GetComponent<AnimatedContainerInteractable>();
            string ajarThought = GetPrivateField<string>(wdAnim, "day2AjarCloseThought");
            Assert.IsTrue(!string.IsNullOrWhiteSpace(ajarThought) && ajarThought.Contains("fechado") && !ajarThought.Contains("ontem...por que"),
                $"Wardrobe Day 2 ajar thought invalid: '{ajarThought}'");

            GameObject d1Root = FindInactiveRoot("Props_Dia1");
            Assert.IsNotNull(d1Root, "Props_Dia1 not found.");
            Transform battTr = FindChildRecursive(d1Root.transform, "Pilhas_Controle_D1");
            Assert.IsNotNull(battTr, "Pilhas_Controle_D1 not found.");
            string battPrompt = GetPrivateField<string>(battTr.GetComponent<InspectableObject>(), "interactionPrompt");
            Assert.AreEqual("Pegar as pilhas", battPrompt, "Battery pickup prompt must not have trailing period.");

            // 4. Upgraded sculpted meshes wired across house and Daughters' Room
            AssertMeshAssetAssigned(casa.transform, "TV_Sala/Carcaca_TV", "Mesh_TV_Carcaca");
            AssertMeshAssetAssigned(casa.transform, "TV_Sala/Tubo_Traseiro_CRT", "Mesh_TV_TuboTraseiro");
            AssertMeshAssetAssigned(casa.transform, "Vaso_Sanitario/Vaso_CaixaAcoplada", "Mesh_Vaso_CaixaAcoplada");
            AssertMeshAssetAssigned(casa.transform, "Fogao/Pano_Forno_Frente", "Mesh_Pano_Forno");
            AssertMeshAssetAssigned(casa.transform, "Fogao/Bule_Cafe/Bule_Bico", "Mesh_Bico_Bule");
            AssertMeshAssetAssigned(casa.transform, "Armario_Remedios_Banheiro/Frasco_Interno_1/Corpo_Ambar", "Mesh_Frasco_Farmacia");

            GameObject filhasRoot = FindInactiveRoot("Quarto_Filhas");
            Assert.IsNotNull(filhasRoot, "Quarto_Filhas not found.");
            AssertMeshAssetAssigned(filhasRoot.transform, "Cama_Filha_1/Cobertor_Colorido", "Mesh_Coberta_Infantil");
            AssertMeshAssetAssigned(filhasRoot.transform, "Cama_Filha_2/Cobertor_Colorido", "Mesh_Coberta_Infantil");
            Assert.IsNotNull(FindChildRecursive(filhasRoot.transform, "Caixinha_Musica_Infantil"), "Music box missing on Comoda_Infantil.");
            Assert.IsNotNull(FindChildRecursive(filhasRoot.transform, "Pe_Comoda_Inf_0_0"), "Feet missing on Comoda_Infantil.");

            string[] childChairs = { "Cadeira_Infantil_1", "Cadeira_Infantil_2" };
            foreach (string chairName in childChairs)
            {
                Transform chTr = FindChildRecursive(filhasRoot.transform, chairName);
                Assert.IsNotNull(chTr, $"Upgraded child chair '{chairName}' missing in Quarto_Filhas.");
                MeshRenderer[] mrs = chTr.GetComponentsInChildren<MeshRenderer>(true);
                float minY = float.MaxValue;
                foreach (MeshRenderer mr in mrs)
                {
                    if (mr == null) continue;
                    MeshFilter mf = mr.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        Matrix4x4 m = mr.transform.localToWorldMatrix;
                        Vector3[] vs = mf.sharedMesh.vertices;
                        for (int i = 0; i < vs.Length; i++)
                        {
                            float wy = m.MultiplyPoint3x4(vs[i]).y;
                            if (wy < minY) minY = wy;
                        }
                    }
                }
                Assert.LessOrEqual(Mathf.Abs(minY), 0.005f,
                    $"Child chair '{chairName}' must be grounded at Y=0.000m (actual min.y={minY:F4}m).");
            }
        }

        [Test]
        public void GameplayFlowSequencingAndLightRecoveryInvariants()
        {
            // 1. ObjectiveHUD dynamic priority promotion & label updates
            GameObject hudHost = new GameObject("__Test_ObjectiveHUD_Flow");
            try
            {
                ObjectiveHUD hud = hudHost.AddComponent<ObjectiveHUD>();
                hud.ConfigureDayHeader("TESTE", Color.white);
                hud.AddOrUpdateObjective("task_a", "Tarefa A", completed: false, visible: true);
                hud.AddOrUpdateObjective("task_b", "Tarefa B", completed: false, visible: true);
                Assert.AreEqual("task_a", hud.CurrentPendingObjectiveId);

                hud.AddOrUpdateObjective("task_urgent", "Urgente", completed: false, visible: true);
                hud.PromoteObjectiveToFront("task_urgent");
                Assert.AreEqual("task_urgent", hud.CurrentPendingObjectiveId,
                    "PromoteObjectiveToFront must surface urgent unlocked subtask ahead of blocked parent objective.");

                hud.SetObjectiveLabel("task_b", "Tarefa B Atualizada");
                hud.MarkCompleted("task_urgent", silentCompletion: true);
                hud.MarkCompleted("task_a", silentCompletion: true);
                Assert.AreEqual("task_b", hud.CurrentPendingObjectiveId);
            }
            finally
            {
                Object.DestroyImmediate(hudHost);
            }

            // 2. SafeBoxPuzzle ResetSafeState cleans up blocking modal flag cleanly
            GameObject d3Root = FindInactiveRoot("Props_Dia3");
            Assert.IsNotNull(d3Root, "Props_Dia3 not found.");
            Transform safeTr = FindChildRecursive(d3Root.transform, "Cofre_Parede_1009");
            Assert.IsNotNull(safeTr, "Cofre_Parede_1009 not found.");
            SafeBoxPuzzle safe = safeTr.GetComponent<SafeBoxPuzzle>();
            Assert.IsNotNull(safe, "SafeBoxPuzzle component missing.");
            bool prevModalFlag = ObjectiveHUD.IsBlockingModalOpen;
            try
            {
                FieldInfo openField = typeof(SafeBoxPuzzle).GetField("isKeypadOpen", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(openField);
                openField.SetValue(safe, true);
                ObjectiveHUD.IsBlockingModalOpen = true;
                safe.ResetSafeState();
                Assert.IsFalse(ObjectiveHUD.IsBlockingModalOpen,
                    "SafeBoxPuzzle.ResetSafeState must clear ObjectiveHUD.IsBlockingModalOpen if reset while open.");
            }
            finally
            {
                ObjectiveHUD.IsBlockingModalOpen = prevModalFlag;
            }

            // 3. Light & sputter recovery on WatcherHallucination, HallucinationTrigger, PsychologicalHorrorDirector, MirrorHauntController
            GameObject lightHost = new GameObject("__Test_LightRecovery");
            try
            {
                Light testLight = lightHost.AddComponent<Light>();
                testLight.type = LightType.Point;
                testLight.intensity = 1.75f;
                testLight.color = new Color(0.95f, 0.85f, 0.72f);

                // WatcherHallucination blackout pulse recovery on DisarmAndHide (lights are pulsed via Light.enabled)
                WatcherHallucination watcher = lightHost.AddComponent<WatcherHallucination>();
                FieldInfo pulsedLightsFi = typeof(WatcherHallucination).GetField("nearbyLightsToPulse", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo wereLitFi = typeof(WatcherHallucination).GetField("pulsedLightsWereLit", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo hasUnrestoredFi = typeof(WatcherHallucination).GetField("hasUnrestoredPulsedLights", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(pulsedLightsFi, "WatcherHallucination.nearbyLightsToPulse missing.");
                Assert.IsNotNull(wereLitFi, "WatcherHallucination.pulsedLightsWereLit missing.");
                Assert.IsNotNull(hasUnrestoredFi, "WatcherHallucination.hasUnrestoredPulsedLights missing.");
                pulsedLightsFi.SetValue(watcher, new[] { testLight });
                wereLitFi.SetValue(watcher, new[] { true });
                hasUnrestoredFi.SetValue(watcher, true);
                testLight.enabled = false;
                watcher.DisarmAndHide();
                Assert.IsTrue(testLight.enabled,
                    "WatcherHallucination.DisarmAndHide must re-enable pulsed lights if interrupted mid-blackout.");
                Assert.IsFalse((bool)hasUnrestoredFi.GetValue(watcher),
                    "WatcherHallucination must clear its pending-restore flag after restoring.");

                // PsychologicalHorrorDirector: interrupted light haunt is restored when the director is
                // disabled/stopped mid-routine, but a day reconfiguration must NOT force the light back on
                // (DayManager re-applies the new day's lighting, e.g. Day 3 blackout).
                PsychologicalHorrorDirector dir = lightHost.AddComponent<PsychologicalHorrorDirector>();
                FieldInfo activeLightFi = typeof(PsychologicalHorrorDirector).GetField("activeHauntedLight", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo activeIntFi = typeof(PsychologicalHorrorDirector).GetField("activeHauntedLightSavedIntensity", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo activeColFi = typeof(PsychologicalHorrorDirector).GetField("activeHauntedLightSavedColor", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo clearTrackedMi = typeof(PsychologicalHorrorDirector).GetMethod("ClearTrackedHauntedLight", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(activeLightFi);
                Assert.IsNotNull(activeIntFi);
                Assert.IsNotNull(activeColFi);
                Assert.IsNotNull(clearTrackedMi);

                activeLightFi.SetValue(dir, testLight);
                activeIntFi.SetValue(dir, 1.75f);
                activeColFi.SetValue(dir, new Color(0.95f, 0.85f, 0.72f));
                testLight.enabled = false;
                testLight.intensity = 0.01f;
                clearTrackedMi.Invoke(dir, new object[] { true });
                Assert.IsTrue(testLight.enabled, "Interrupted haunted light must be re-enabled on restore.");
                Assert.AreEqual(1.75f, testLight.intensity, 0.001f,
                    "PsychologicalHorrorDirector must restore interrupted haunted light intensity.");
                Assert.IsNull(activeLightFi.GetValue(dir), "Tracked haunted light must be cleared after restore.");

                activeLightFi.SetValue(dir, testLight);
                activeIntFi.SetValue(dir, 1.75f);
                testLight.enabled = false;
                dir.ConfigureForDay(GameDay.Day3, null, null, null, null, null);
                Assert.IsFalse(testLight.enabled,
                    "ConfigureForDay must drop the tracked haunted light without overriding the new day's lighting.");
                Assert.IsNull(activeLightFi.GetValue(dir), "ConfigureForDay must clear the tracked haunted light.");
                testLight.enabled = true;
                testLight.intensity = 1.75f;

                // MirrorHauntController bathroom light restoration when disabled mid-flicker
                MirrorHauntController mh = lightHost.AddComponent<MirrorHauntController>();
                FieldInfo mhSconceFi = typeof(MirrorHauntController).GetField("bathroomSconceLight", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo mhSconcePendFi = typeof(MirrorHauntController).GetField("dippedSconcePendingRestore", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo mhRestoreMi = typeof(MirrorHauntController).GetMethod("RestoreDippedBathroomLightsIfPending", BindingFlags.Instance | BindingFlags.NonPublic);
                mhSconceFi.SetValue(mh, testLight);
                mhSconcePendFi.SetValue(mh, true);
                testLight.enabled = false;
                mhRestoreMi.Invoke(mh, null);
                Assert.IsTrue(testLight.enabled,
                    "MirrorHauntController must re-enable dipped bathroom light if interrupted mid-apparition.");

                // HallucinationTrigger corridor sputter refcount & light restoration on ResetHallucinationState
                HallucinationTrigger ht = lightHost.AddComponent<HallucinationTrigger>();
                FieldInfo htSavedFi = typeof(HallucinationTrigger).GetField("activeSavedIntensities", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo htOwnedSuppFi = typeof(HallucinationTrigger).GetField("suppressedCorridorSputter", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo setSuppMi = typeof(HallucinationTrigger).GetMethod("SetOwnedCorridorSputterSuppressed", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(htSavedFi);
                Assert.IsNotNull(htOwnedSuppFi);
                Assert.IsNotNull(setSuppMi);
                ht.SetLightsToFlicker(new[] { testLight });
                htSavedFi.SetValue(ht, new[] { 1.75f });
                setSuppMi.Invoke(ht, new object[] { true });
                testLight.intensity = 0f;
                Assert.IsTrue((bool)htOwnedSuppFi.GetValue(ht), "Expected owned sputter suppression active during test flicker.");
                ht.ResetHallucinationState();
                Assert.IsFalse((bool)htOwnedSuppFi.GetValue(ht),
                    "HallucinationTrigger.ResetHallucinationState must release owned corridor sputter suppression.");
                Assert.AreEqual(1.75f, testLight.intensity, 0.001f,
                    "HallucinationTrigger.ResetHallucinationState must restore interrupted flicker light intensity.");
            }
            finally
            {
                Object.DestroyImmediate(lightHost);
            }
        }

        [Test]
        public void Phase2RetroSurfacesArtworksAndHudSuppressionInvariants()
        {
            // 1. All 10 distinct 128x128 low-detail retro surface textures exist with repeat wrap & linear import
            string[] expectedRetroTex =
            {
                "Assets/Textures/RetroSurface/Tex_Retro_Plaster.png",
                "Assets/Textures/RetroSurface/Tex_Retro_FloorWood.png",
                "Assets/Textures/RetroSurface/Tex_Retro_WoodGrain.png",
                "Assets/Textures/RetroSurface/Tex_Retro_TileGrid.png",
                "Assets/Textures/RetroSurface/Tex_Retro_SofaUpholstery.png",
                "Assets/Textures/RetroSurface/Tex_Retro_RugCarpet.png",
                "Assets/Textures/RetroSurface/Tex_Retro_BedQuilt.png",
                "Assets/Textures/RetroSurface/Tex_Retro_CurtainLinen.png",
                "Assets/Textures/RetroSurface/Tex_Retro_WoolCoat.png",
                "Assets/Textures/RetroSurface/Tex_Retro_MattePaperCarpet.png"
            };
            for (int i = 0; i < expectedRetroTex.Length; i++)
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(expectedRetroTex[i]);
                Assert.IsNotNull(tex, $"Missing Phase 2 retro surface texture: {expectedRetroTex[i]}");
                Assert.AreEqual(128, tex.width, $"{tex.name} must stay low-res 128x128.");
                Assert.AreEqual(128, tex.height, $"{tex.name} must stay low-res 128x128.");
                Assert.AreEqual(TextureWrapMode.Repeat, tex.wrapMode, $"{tex.name} must tile seamlessly (Repeat).");
            }

            // 2. Structural & distinct fabric/ceramic materials use LowPolyDirtyLit + subtle _SurfaceTex (zero PBR maps)
            string[] retroMats =
            {
                "Assets/Materials/Greybox/M_Wall_Plaster.mat",
                "Assets/Materials/Greybox/M_Floor_Wood.mat",
                "Assets/Materials/Greybox/M_Floor_Tile.mat",
                "Assets/Materials/Greybox/M_Ceiling.mat",
                "Assets/Materials/Greybox/M_Door_Wood.mat",
                "Assets/Materials/Greybox/M_Furniture_Wood.mat",
                "Assets/Materials/Greybox/M_Sofa_Fabric.mat",
                "Assets/Materials/Greybox/M_Rug_Fabric.mat",
                "Assets/Materials/Greybox/M_Bed_Blanket.mat",
                "Assets/Materials/Greybox/M_Curtain_Linen.mat",
                "Assets/Materials/Greybox/M_Margaret_Coat_Wool.mat",
                "Assets/Materials/Greybox/M_Ceramic_Azulejo.mat"
            };
            string[] forbiddenPbr = { "_BaseMap", "_MainTex", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" };
            for (int i = 0; i < retroMats.Length; i++)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(retroMats[i]);
                Assert.IsNotNull(mat, $"Material missing: {retroMats[i]}");
                Assert.AreEqual("Canal420/LowPolyDirtyLit", mat.shader.name, $"{mat.name} must use Canal420/LowPolyDirtyLit.");
                Assert.IsNotNull(mat.GetTexture("_SurfaceTex"), $"{mat.name} missing retro _SurfaceTex.");
                float strength = mat.GetFloat("_SurfaceTexStrength");
                Assert.GreaterOrEqual(strength, 0.10f, $"{mat.name} _SurfaceTexStrength too low.");
                Assert.LessOrEqual(strength, 0.30f, $"{mat.name} _SurfaceTexStrength exceeds subtle cap (0.30).");
                for (int p = 0; p < forbiddenPbr.Length; p++)
                {
                    if (mat.HasProperty(forbiddenPbr[p]))
                    {
                        Assert.IsNull(mat.GetTexture(forbiddenPbr[p]), $"{mat.name} must NOT bind complex PBR map '{forbiddenPbr[p]}'.");
                    }
                }
            }

            // 2b. Sofa, rug, bed blanket, curtains, and Margaret's coat each use a DISTINCT retro cloth texture
            Texture sofaTex = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Greybox/M_Sofa_Fabric.mat").GetTexture("_SurfaceTex");
            Texture rugTex = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Greybox/M_Rug_Fabric.mat").GetTexture("_SurfaceTex");
            Texture bedTex = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Greybox/M_Bed_Blanket.mat").GetTexture("_SurfaceTex");
            Texture curtainTex = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Greybox/M_Curtain_Linen.mat").GetTexture("_SurfaceTex");
            Texture coatTex = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Greybox/M_Margaret_Coat_Wool.mat").GetTexture("_SurfaceTex");
            Assert.AreNotEqual(sofaTex, rugTex, "Sofa and rug must use distinct retro cloth textures.");
            Assert.AreNotEqual(sofaTex, bedTex, "Sofa and bed blanket must use distinct retro cloth textures.");
            Assert.AreNotEqual(rugTex, bedTex, "Rug and bed blanket must use distinct retro cloth textures.");
            Assert.AreNotEqual(curtainTex, sofaTex, "Curtains and sofa must use distinct retro cloth textures.");
            Assert.AreNotEqual(coatTex, sofaTex, "Margaret's wool coat and sofa must use distinct retro cloth textures.");

            // 2c. Office filing cabinet drawers use flat painted metal (M_Office_Cabinet_Metal, zero tile texture)
            Material cabMetalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Greybox/M_Office_Cabinet_Metal.mat");
            Assert.IsNotNull(cabMetalMat, "M_Office_Cabinet_Metal.mat missing.");
            Assert.AreEqual("Universal Render Pipeline/Lit", cabMetalMat.shader.name, "M_Office_Cabinet_Metal must be flat URP Lit painted metal.");
            Assert.IsNull(cabMetalMat.GetTexture("_BaseMap"), "M_Office_Cabinet_Metal must have no diffuse texture.");

            GameObject casa = GameObject.Find("Casa_Base");
            GameObject filhas = FindInactiveRoot("Quarto_Filhas");
            Assert.IsNotNull(casa, "Casa_Base missing.");
            Transform arquivoTr = FindChildRecursive(casa.transform, "Arquivo_Metalico");
            Assert.IsNotNull(arquivoTr, "Arquivo_Metalico missing.");
            Transform lowDrawerTr = FindChildRecursive(arquivoTr, "Gaveta_Baixa_Fixa");
            Assert.IsNotNull(lowDrawerTr, "Gaveta_Baixa_Fixa missing.");
            Assert.AreEqual("M_Office_Cabinet_Metal", lowDrawerTr.GetComponent<MeshRenderer>().sharedMaterial.name,
                "Office filing cabinet drawers must use flat M_Office_Cabinet_Metal instead of floor tile.");

            // 2d. No flat floor wear slabs or wall damp stain slabs remain anywhere in the house
            Transform[] allCasaTransforms = casa.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allCasaTransforms.Length; i++)
            {
                string n = allCasaTransforms[i].name;
                Assert.IsFalse(n.StartsWith("Desgaste_Piso_", StringComparison.Ordinal), $"Flat floor wear block '{n}' must be removed.");
                Assert.IsFalse(n.StartsWith("Mancha_Umidade_", StringComparison.Ordinal), $"Flat wall damp block '{n}' must be removed.");
                Assert.AreNotEqual("Sujeira_Azulejo_Fogao", n, "Flat wall grime block Sujeira_Azulejo_Fogao must be removed.");
                Assert.AreNotEqual("Mancha_Queimado_Fogao", n, "Flat stovetop stain block Mancha_Queimado_Fogao must be removed.");
            }
            if (filhas != null)
            {
                Transform[] allFilhasTransforms = filhas.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < allFilhasTransforms.Length; i++)
                {
                    Assert.IsFalse(allFilhasTransforms[i].name.StartsWith("Mancha_Umidade_", StringComparison.Ordinal),
                        $"Flat wall damp block '{allFilhasTransforms[i].name}' in Quarto_Filhas must be removed.");
                }
            }

            // 2e. Stove grates are grounded with Pe_Trempe feet and coffee dripper grounds clear paper rim cleanly
            Assert.IsNotNull(FindChildRecursive(casa.transform, "Pe_Trempe_0_0_0"), "Stove grates must have grounded support feet Pe_Trempe_*");
            Transform coffeeSet = FindChildRecursive(casa.transform, "Conjunto_Coador_Cafe_Balcao");
            Assert.IsNotNull(coffeeSet, "Conjunto_Coador_Cafe_Balcao missing.");
            Transform filterCone = FindChildRecursive(coffeeSet, "Filtro_Papel_Cone");
            Transform coffeeGrounds = FindChildRecursive(coffeeSet, "Borra_Cafe_Filtro");
            Assert.IsNotNull(filterCone, "Filtro_Papel_Cone missing.");
            Assert.IsNotNull(coffeeGrounds, "Borra_Cafe_Filtro missing.");
            Bounds filterBounds = filterCone.GetComponent<MeshRenderer>().bounds;
            Bounds groundsBounds = coffeeGrounds.GetComponent<MeshRenderer>().bounds;
            Assert.Greater(filterBounds.max.y - groundsBounds.max.y, 0.015f,
                "Coffee grounds must sit cleanly recessed below the hollow paper filter rim (zero Z-fighting).");

            // 3. Upgraded dirty low-poly 2D artworks are assigned on all framed pictures, newspaper & child drawings
            string[] upgradedDetailMats =
            {
                "Assets/Materials/Greybox/Details/M_Art_Quadro_Margaret_Normal.mat",
                "Assets/Materials/Greybox/Details/M_Art_Quadro_Margaret_Corrupted.mat",
                "Assets/Materials/Greybox/Details/M_Art_Quadro_Familia_1.mat",
                "Assets/Materials/Greybox/Details/M_Art_Quadro_Familia_2.mat",
                "Assets/Materials/Greybox/Details/M_Art_Quadro_Distorcido_1.mat",
                "Assets/Materials/Greybox/Details/M_Art_Quadro_Distorcido_2.mat",
                "Assets/Materials/Greybox/Details/M_Print_Jornal_Baixada.mat",
                "Assets/Materials/Greybox/Details/M_Print_Desenho_Casa.mat",
                "Assets/Materials/Greybox/Details/M_Print_Desenho_Familia.mat"
            };
            for (int i = 0; i < upgradedDetailMats.Length; i++)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(upgradedDetailMats[i]);
                Assert.IsNotNull(mat, $"Missing Phase 2 detail material: {upgradedDetailMats[i]}");
                Assert.IsNotNull(mat.mainTexture, $"Missing upgraded texture on {mat.name}.");
            }

            // 4. ObjectiveHUD preserves LegacyRuntime.ttf and ref-counted event suppression hides/restores cleanly
            GameObject hudHost = new GameObject("__Test_ObjectiveHUD_EventHide");
            try
            {
                ObjectiveHUD hud = hudHost.AddComponent<ObjectiveHUD>();
                hud.ConfigureDayHeader("DIA 3", Color.white);
                hud.AddOrUpdateObjective("test_step", "Passo teste", completed: false, visible: true);
                hud.SetHUDVisible(true, immediate: true);

                Font font = GetPrivateField<Font>(hud, "uiFont");
                Assert.IsNotNull(font, "ObjectiveHUD uiFont must not be null.");
                Assert.AreEqual("LegacyRuntime", font.name, "ObjectiveHUD must preserve LegacyRuntime.ttf.");

                Assert.IsFalse(hud.IsSuppressedByEvent);
                hud.PushEventHide(immediate: true);
                Assert.IsTrue(hud.IsSuppressedByEvent);
                hud.PushEventHide(immediate: false);
                hud.PopEventHide();
                Assert.IsTrue(hud.IsSuppressedByEvent, "Nested PushEventHide must stay suppressed until all callers pop.");
                hud.PopEventHide();
                Assert.IsFalse(hud.IsSuppressedByEvent, "ObjectiveHUD must restore once all event hides pop.");
                hud.PushEventHide(immediate: true);
                hud.ClearEventHides();
                Assert.IsFalse(hud.IsSuppressedByEvent, "ClearEventHides must reset refcount cleanly.");
            }
            finally
            {
                Object.DestroyImmediate(hudHost);
            }

            // 5. Flashlight beam calibrated for Day 3 contrast (focused halogen range & angle)
            FlashlightController fc = Object.FindAnyObjectByType<FlashlightController>(FindObjectsInactive.Include);
            Assert.IsNotNull(fc, "FlashlightController missing on Player.");
            Assert.LessOrEqual(GetPrivateField<float>(fc, "beamRange"), 11.5f, "Flashlight beamRange should stay focused for Day 3 contrast.");
            Assert.LessOrEqual(GetPrivateField<float>(fc, "spotAngle"), 50f, "Flashlight spotAngle should stay focused for Day 3 contrast.");

            SanityEffects sanity = Object.FindAnyObjectByType<SanityEffects>();
            Assert.IsNotNull(sanity, "SanityEffects missing.");
            Assert.GreaterOrEqual(GetPrivateField<float>(sanity, "burnoutFilmGrain"), 0.16f, "Phase 2 VHS film grain baseline must be applied on SanityEffects.");
        }

        [Test]
        public void Phase3SkinnedRigPsychologicalEventsAndClimaxInvariants()
        {
            GameObject casa = GameObject.Find("Casa_Base");
            GameObject propsD2 = FindInactiveRoot("Props_Dia2");
            GameObject propsD3 = FindInactiveRoot("Props_Dia3");
            Assert.IsNotNull(casa, "Casa_Base missing.");
            Assert.IsNotNull(propsD2, "Props_Dia2 missing.");
            Assert.IsNotNull(propsD3, "Props_Dia3 missing.");

            // 1. Saved skeletal AnimatorController & all 6 real AnimationClips in Assets/Models/Characters/
            const string controllerPath = "Assets/Models/Characters/AC_Canal420_Humanoid.controller";
            RuntimeAnimatorController charController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            Assert.IsNotNull(charController, "Missing AC_Canal420_Humanoid.controller in Assets/Models/Characters/.");

            Object[] controllerSubAssets = AssetDatabase.LoadAllAssetsAtPath(controllerPath);
            int validTransitionCount = 0;
            for (int i = 0; i < controllerSubAssets.Length; i++)
            {
                if (controllerSubAssets[i] is AnimatorStateTransition tr)
                {
                    Assert.IsNotNull(tr.destinationState,
                        "Orphaned AnimatorStateTransition sub-asset with null destinationState found in AC_Canal420_Humanoid.controller.");
                    validTransitionCount++;
                }
            }
            Assert.AreEqual(6, validTransitionCount, "AC_Canal420_Humanoid.controller must contain exactly 6 active mode transitions and 0 orphans.");

            string[] expectedClips =
            {
                "Assets/Models/Characters/Animations/Anim_Char_Idle.anim",
                "Assets/Models/Characters/Animations/Anim_Char_LurkPeek.anim",
                "Assets/Models/Characters/Animations/Anim_Char_RunRetreat.anim",
                "Assets/Models/Characters/Animations/Anim_Char_FrenziedLunge.anim",
                "Assets/Models/Characters/Animations/Anim_Char_BrokenNeckStill.anim",
                "Assets/Models/Characters/Animations/Anim_Char_VisceralAttackLunge.anim"
            };
            for (int i = 0; i < expectedClips.Length; i++)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(expectedClips[i]);
                Assert.IsNotNull(clip, $"Missing skeletal AnimationClip: {expectedClips[i]}");
                Assert.Greater(clip.length, 0.15f, $"AnimationClip {clip.name} has invalid short length.");
                if (clip.name == "Anim_Char_VisceralAttackLunge")
                {
                    Assert.LessOrEqual(clip.length, 0.23f,
                        "Anim_Char_VisceralAttackLunge must complete within the 0.22s climax lunge window.");
                }
                var bindings = AnimationUtility.GetCurveBindings(clip);
                Assert.GreaterOrEqual(bindings.Length, 16, $"AnimationClip {clip.name} must drive real skeletal bone curves.");
            }

            // 1b. Verify PS1 face textures render genuine pale skin + dark recessed sockets + dark scalp (regression guard against inverted SmoothStep)
            string[] faceTexPaths =
            {
                "Assets/Textures/Characters/Tex_Char_Margaret_Face.png",
                "Assets/Textures/Characters/Tex_Char_Child_Face.png",
                "Assets/Textures/Characters/Tex_Char_Cian_Face.png"
            };
            for (int i = 0; i < faceTexPaths.Length; i++)
            {
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), faceTexPaths[i]);
                Assert.IsTrue(File.Exists(fullPath), $"Missing PS1 face texture: {faceTexPaths[i]}");
                Texture2D tmpTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.IsTrue(tmpTex.LoadImage(File.ReadAllBytes(fullPath)), $"Failed to decode {faceTexPaths[i]}.");
                    Color centerSkin = tmpTex.GetPixel(tmpTex.width / 2, Mathf.RoundToInt(tmpTex.height * 0.44f));
                    Color leftSocket = tmpTex.GetPixel(Mathf.RoundToInt(tmpTex.width * 0.432f), Mathf.RoundToInt(tmpTex.height * 0.545f));
                    Color topCrown = tmpTex.GetPixel(tmpTex.width / 2, Mathf.RoundToInt(tmpTex.height * 0.94f));
                    float centerLuma = centerSkin.r * 0.299f + centerSkin.g * 0.587f + centerSkin.b * 0.114f;
                    float socketLuma = leftSocket.r * 0.299f + leftSocket.g * 0.587f + leftSocket.b * 0.114f;
                    float crownLuma = topCrown.r * 0.299f + topCrown.g * 0.587f + topCrown.b * 0.114f;
                    Assert.GreaterOrEqual(centerLuma, 0.52f, $"{faceTexPaths[i]} front face skin is too dark (luma={centerLuma:F2}).");
                    Assert.LessOrEqual(socketLuma, centerLuma - 0.25f, $"{faceTexPaths[i]} eye sockets lack depth contrast (socket={socketLuma:F2}, center={centerLuma:F2}).");
                    Assert.LessOrEqual(crownLuma, 0.25f, $"{faceTexPaths[i]} scalp crown should remain dark hair (luma={crownLuma:F2}).");
                }
                finally
                {
                    Object.DestroyImmediate(tmpTex);
                }
            }

            // 2. All 5 horror figures use real SkinnedMeshRenderer + 17-bone skeleton + Animator + PS1 low-poly triangle budget (1200..2500)
            (string Name, Transform Root, string ExpectedMeshPrefix)[] characters =
            {
                ("Margaret_Jumpscare", propsD3.transform, "Mesh_Rigged_Margaret"),
                ("Aparicao_Porta_Banheiro_D2", propsD2.transform, "Mesh_Rigged_Margaret"),
                ("Watcher_Figura_D3", propsD3.transform, "Mesh_Rigged_ChildWatcher"),
                ("Watcher_Figura_D2", propsD2.transform, "Mesh_Rigged_SlumpedCian"),
                ("Vulto_Sombra_Negra", propsD2.transform, "Mesh_Rigged_ShadowDash")
            };
            foreach (var c in characters)
            {
                Transform tr = FindChildRecursive(c.Root, c.Name);
                Assert.IsNotNull(tr, $"Rigged character '{c.Name}' missing.");
                AssertSubtreeNotBatchingStatic(tr, c.Name);

                SkinnedMeshRenderer smr = tr.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Assert.IsNotNull(smr, $"{c.Name} must use SkinnedMeshRenderer.");
                Assert.IsNotNull(smr.sharedMesh, $"{c.Name} SkinnedMeshRenderer.sharedMesh is null.");
                Assert.AreEqual(c.ExpectedMeshPrefix, smr.sharedMesh.name, $"{c.Name} rigged mesh mismatch.");

                string meshPath = AssetDatabase.GetAssetPath(smr.sharedMesh);
                Assert.IsTrue(meshPath.StartsWith("Assets/Models/Characters/", StringComparison.Ordinal),
                    $"{c.Name} skinned mesh must be saved under Assets/Models/Characters/ (actual: '{meshPath}').");

                int triCount = smr.sharedMesh.triangles.Length / 3;
                Assert.GreaterOrEqual(triCount, 1200, $"{c.Name} triangle count too low ({triCount}).");
                Assert.LessOrEqual(triCount, 2500, $"{c.Name} triangle count exceeds PS1/VHS budget ({triCount}).");
                Assert.GreaterOrEqual(smr.bones.Length, 15, $"{c.Name} must bind >= 15 skeletal bones.");
                Assert.AreEqual(smr.bones.Length, smr.sharedMesh.bindposes.Length, $"{c.Name} bindposes count mismatch.");
                Assert.AreEqual(smr.sharedMesh.vertexCount, smr.sharedMesh.boneWeights.Length, $"{c.Name} boneWeights missing.");
                Assert.IsTrue(smr.updateWhenOffscreen, $"{c.Name} must set updateWhenOffscreen=true to prevent close-up lunge culling.");

                // Articulated elbows & knees exist for real skeletal running/lunging deformation
                Assert.IsNotNull(tr.Find("Braco_Esq/Antebraco_Esq"), $"{c.Name} missing left elbow bone Antebraco_Esq.");
                Assert.IsNotNull(tr.Find("Braco_Dir/Antebraco_Dir"), $"{c.Name} missing right elbow bone Antebraco_Dir.");
                Assert.IsNotNull(tr.Find("Perna_Esq/Canela_Esq"), $"{c.Name} missing left knee bone Canela_Esq.");
                Assert.IsNotNull(tr.Find("Perna_Dir/Canela_Dir"), $"{c.Name} missing right knee bone Canela_Dir.");

                Animator anim = tr.GetComponent<Animator>();
                Assert.IsNotNull(anim, $"{c.Name} must have an Animator component.");
                Assert.AreSame(charController, anim.runtimeAnimatorController, $"{c.Name} must bind AC_Canal420_Humanoid.controller.");
                Assert.IsFalse(anim.applyRootMotion, $"{c.Name} Animator must keep applyRootMotion=false.");

                // Guard hair crown skinning on Margaret/Child rigs so scalp vertices stay 100% locked to Cabeca_Silhueta (bone 4)
                if (c.ExpectedMeshPrefix == "Mesh_Rigged_Margaret" || c.ExpectedMeshPrefix == "Mesh_Rigged_ChildWatcher")
                {
                    Vector3[] verts = smr.sharedMesh.vertices;
                    BoneWeight[] bws = smr.sharedMesh.boneWeights;
                    float maxY = smr.sharedMesh.bounds.max.y;
                    for (int vi = 0; vi < verts.Length; vi++)
                    {
                        if (verts[vi].y >= maxY - 0.035f)
                        {
                            BoneWeight bw = bws[vi];
                            float swayWeight = (bw.boneIndex0 == 16 ? bw.weight0 : 0f)
                                             + (bw.boneIndex1 == 16 ? bw.weight1 : 0f)
                                             + (bw.boneIndex2 == 16 ? bw.weight2 : 0f)
                                             + (bw.boneIndex3 == 16 ? bw.weight3 : 0f);
                            Assert.LessOrEqual(swayWeight, 0.001f,
                                $"{c.Name} crown vertex at Y={verts[vi].y:F3} has non-zero hair-sway weight ({swayWeight:F2}), causing scalp detachment.");
                        }
                    }
                }
            }

            // 3. Phase 3.1 Continuity & Gaslighting systems wired + non-batched + zero phone ringing
            Transform diningChair = FindChildRecursive(casa.transform, "Cadeira_Jantar_Norte");
            Assert.IsNotNull(diningChair, "Cadeira_Jantar_Norte missing for Day 2 silent chair shift.");
            AssertSubtreeNotBatchingStatic(diningChair, "Cadeira_Jantar_Norte");

            MirrorHauntController mirrorHaunt = Object.FindAnyObjectByType<MirrorHauntController>(FindObjectsInactive.Include);
            Assert.IsNotNull(mirrorHaunt, "MirrorHauntController missing.");
            Assert.IsNotNull(mirrorHaunt.BathroomDoor, "MirrorHauntController must link Porta_Banheiro_Pivot for Event 2 mirror door discrepancy.");
            Assert.AreEqual("Porta_Banheiro_Pivot", mirrorHaunt.BathroomDoor.name);
            Assert.AreEqual(-86f, mirrorHaunt.BathroomDoor.DefaultOpenAngle, 0.01f,
                "Porta_Banheiro_Pivot.DefaultOpenAngle must return its serialized -86f angle.");

            // Verify DoorInteractable.DefaultOpenAngle initializes from serialized openAngle even BEFORE Awake() runs
            // and is not clobbered if Awake() executes after OpenToPartialAngle().
            GameObject tempDoorGo = new GameObject("__Test_DoorInteractable_PreAwake");
            try
            {
                DoorInteractable tempDoor = tempDoorGo.AddComponent<DoorInteractable>();
                FieldInfo openAngleField = typeof(DoorInteractable).GetField("openAngle", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo swingSpeedField = typeof(DoorInteractable).GetField("swingSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo yawInitField = typeof(DoorInteractable).GetField("yawInitialized", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo awakeMethod = typeof(DoorInteractable).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(openAngleField);
                Assert.IsNotNull(swingSpeedField);
                Assert.IsNotNull(yawInitField);
                Assert.IsNotNull(awakeMethod);

                openAngleField.SetValue(tempDoor, -86f);
                swingSpeedField.SetValue(tempDoor, 148f);
                Assert.IsFalse((bool)yawInitField.GetValue(tempDoor), "Fresh EditMode DoorInteractable should start uninitialized before Awake().");
                Assert.AreEqual(-86f, tempDoor.DefaultOpenAngle, 0.01f,
                    "DefaultOpenAngle must initialize from openAngle (-86f) before Awake() instead of returning fallback +90f.");

                tempDoor.OpenToPartialAngle(18f, 16f, playSound: false);
                awakeMethod.Invoke(tempDoor, null);
                Assert.AreEqual(-86f, tempDoor.DefaultOpenAngle, 0.01f,
                    "Awake() must NOT overwrite defaultOpenAngle when initialized prior to Awake().");
                tempDoor.ResetSwingSpeed();
                Assert.AreEqual(148f, (float)swingSpeedField.GetValue(tempDoor), 0.01f,
                    "ResetSwingSpeed() must restore original defaultSwingSpeed (148f), not temporary partial swingSpeed (16f).");
            }
            finally
            {
                Object.DestroyImmediate(tempDoorGo);
            }

            // Also verify pre-Awake() Interact() on an uninitialized negative-angle door does NOT corrupt openAngle/DefaultOpenAngle to +90f.
            GameObject tempDoorInteractGo = new GameObject("__Test_DoorInteractable_PreAwakeInteract");
            try
            {
                DoorInteractable tempDoor = tempDoorInteractGo.AddComponent<DoorInteractable>();
                FieldInfo openAngleField = typeof(DoorInteractable).GetField("openAngle", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo swingSpeedField = typeof(DoorInteractable).GetField("swingSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
                openAngleField.SetValue(tempDoor, -86f);
                swingSpeedField.SetValue(tempDoor, 148f);

                tempDoor.Interact(null);
                Assert.IsTrue(tempDoor.IsOpen, "Pre-Awake Interact() must open unlocked door.");
                Assert.AreEqual(-86f, tempDoor.DefaultOpenAngle, 0.01f,
                    "Pre-Awake Interact() must NOT overwrite defaultOpenAngle with +90f.");
                Assert.AreEqual(-86f, (float)openAngleField.GetValue(tempDoor), 0.01f,
                    "Pre-Awake Interact() must preserve serialized openAngle (-86f).");
                Assert.AreEqual(148f, (float)swingSpeedField.GetValue(tempDoor), 0.01f,
                    "Pre-Awake Interact() must preserve serialized swingSpeed (148f).");
            }
            finally
            {
                Object.DestroyImmediate(tempDoorInteractGo);
            }

            // Exercise Event 2 Mirror Door Discrepancy transform override & restore on real Porta_Banheiro_Pivot
            DoorInteractable bathDoor = mirrorHaunt.BathroomDoor;
            Transform bathPivot = bathDoor.DoorPivot;
            Quaternion savedBathRot = bathPivot.localRotation;
            FieldInfo doorIsOpenField = typeof(DoorInteractable).GetField("isOpen", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo showingField = typeof(MirrorHauntController).GetField("isMirrorDoorDiscrepancyShowing", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo applyMirrorDoorMi = typeof(MirrorHauntController).GetMethod("ApplyBathroomDoorMirrorDiscrepancyOverrideIfActive", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo restoreMirrorDoorMi = typeof(MirrorHauntController).GetMethod("RestoreBathroomDoorOverrideIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(doorIsOpenField);
            Assert.IsNotNull(showingField);
            Assert.IsNotNull(applyMirrorDoorMi);
            Assert.IsNotNull(restoreMirrorDoorMi);
            bool savedBathIsOpen = (bool)doorIsOpenField.GetValue(bathDoor);
            try
            {
                mirrorHaunt.ConfigureForDay(GameDay.Day2);
                Assert.IsFalse(mirrorHaunt.IsMirrorDoorDiscrepancyArmed);
                mirrorHaunt.ArmMirrorDoorDiscrepancy();
                Assert.IsTrue(mirrorHaunt.IsMirrorDoorDiscrepancyArmed, "Mirror door discrepancy must arm on Day 2.");

                float closedYaw = bathDoor.ClosedYaw;
                doorIsOpenField.SetValue(bathDoor, false);
                showingField.SetValue(mirrorHaunt, true);
                applyMirrorDoorMi.Invoke(mirrorHaunt, null);
                Assert.AreEqual(-36f, Mathf.DeltaAngle(closedYaw, bathPivot.localEulerAngles.y), 0.1f,
                    "Closed bathroom door mirror discrepancy must swing INWARD (-36 deg), never outward (+36 deg).");
                restoreMirrorDoorMi.Invoke(mirrorHaunt, null);
                Assert.AreEqual(0f, Mathf.DeltaAngle(closedYaw, bathPivot.localEulerAngles.y), 0.1f,
                    "RestoreBathroomDoorOverrideIfNeeded must restore closed bathroom door rotation exactly.");

                doorIsOpenField.SetValue(bathDoor, true);
                applyMirrorDoorMi.Invoke(mirrorHaunt, null);
                Assert.AreEqual(-6f, Mathf.DeltaAngle(closedYaw, bathPivot.localEulerAngles.y), 0.1f,
                    "Open bathroom door mirror discrepancy must show nearly shut (-6 deg) in reflection.");
                restoreMirrorDoorMi.Invoke(mirrorHaunt, null);
                Assert.AreEqual(0f, Mathf.DeltaAngle(closedYaw, bathPivot.localEulerAngles.y), 0.1f,
                    "RestoreBathroomDoorOverrideIfNeeded must restore open-state test rotation cleanly.");

                mirrorHaunt.ConfigureForDay(GameDay.Day1);
                Assert.IsFalse(mirrorHaunt.IsMirrorDoorDiscrepancyArmed, "ConfigureForDay(Day1) must reset mirror door discrepancy.");
            }
            finally
            {
                restoreMirrorDoorMi.Invoke(mirrorHaunt, null);
                doorIsOpenField.SetValue(bathDoor, savedBathIsOpen);
                bathPivot.localRotation = savedBathRot;
                mirrorHaunt.ConfigureForDay(GameDay.Day1);
            }

            Transform portraitTr = FindChildRecursive(propsD3.transform, "Quadro_Margaret_Parede");
            Assert.IsNotNull(portraitTr, "Quadro_Margaret_Parede missing.");
            AssertSubtreeNotBatchingStatic(portraitTr, "Quadro_Margaret_Parede");
            FallingPictureFrame portraitFrame = portraitTr.GetComponent<FallingPictureFrame>();
            Assert.IsNotNull(portraitFrame, "FallingPictureFrame missing on Quadro_Margaret_Parede.");
            GameObject[] chips = GetPrivateField<GameObject[]>(portraitFrame, "plasterChipsToReveal");
            Assert.IsNotNull(chips, "FallingPictureFrame must link plasterChipsToReveal.");
            Assert.AreEqual(2, chips.Length, "Expected 2 plaster chips linked to Quadro_Margaret_Parede.");
            Vector3 savedPortraitPos = portraitTr.localPosition;
            Quaternion savedPortraitRot = portraitTr.localRotation;
            try
            {
                portraitFrame.ResetFrameState();
                Assert.IsFalse(portraitFrame.HasShiftedOffscreen);
                Assert.IsTrue(portraitFrame.ApplyOffscreenCrookedShift(), "Off-screen portrait shift must apply cleanly before touch.");
                Assert.IsTrue(portraitFrame.HasShiftedOffscreen);
                Assert.Greater(Quaternion.Angle(savedPortraitRot, portraitTr.localRotation), 8.0f,
                    "Off-screen portrait shift must visibly increase tilt angle.");
                Assert.AreEqual(savedPortraitPos.x, portraitTr.localPosition.x, 0.0005f,
                    "Off-screen portrait shift must stay flush on wall X plane.");
                Assert.IsFalse(portraitFrame.ApplyOffscreenCrookedShift(), "Off-screen portrait shift must only fire once.");
            }
            finally
            {
                portraitFrame.ResetFrameState();
                portraitTr.localPosition = savedPortraitPos;
                portraitTr.localRotation = savedPortraitRot;
                for (int i = 0; i < chips.Length; i++)
                {
                    if (chips[i] != null) chips[i].SetActive(true);
                }
            }

            // Confirm zero phone ringing field/regression on PsychologicalHorrorDirector + real _EMISSION glow on Day 3 unplugged LED
            Assert.IsNull(typeof(PsychologicalHorrorDirector).GetField("phoneHalfRingPlayedThisDay", BindingFlags.Instance | BindingFlags.NonPublic),
                "Phone ringing must remain completely removed across all days.");

            Transform secD3Tr = FindChildRecursive(propsD3.transform, "Secretaria_Eletronica_D3");
            Transform secD3LedTr = secD3Tr != null ? secD3Tr.Find("LED_Mensagens") : null;
            Assert.IsNotNull(secD3LedTr, "Secretaria_Eletronica_D3/LED_Mensagens missing for Phase 3.1 Event 5.");
            AssertSubtreeNotBatchingStatic(secD3LedTr, "Secretaria_Eletronica_D3/LED_Mensagens");
            Renderer secD3LedRenderer = secD3LedTr.GetComponent<Renderer>();
            Assert.IsNotNull(secD3LedRenderer, "Secretaria_Eletronica_D3/LED_Mensagens missing Renderer.");
            Material origSecD3LedMat = secD3LedRenderer.sharedMaterial;
            GameObject tempDirectorGo = new GameObject("TempHorrorDirectorTest");
            try
            {
                PsychologicalHorrorDirector tempDir = tempDirectorGo.AddComponent<PsychologicalHorrorDirector>();
                MethodInfo cachePropsMethod = typeof(PsychologicalHorrorDirector).GetMethod("CacheSceneInteractiveProps", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo setLedMethod = typeof(PsychologicalHorrorDirector).GetMethod("SetDay3UnpluggedLedGlow", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo clearLedMethod = typeof(PsychologicalHorrorDirector).GetMethod("ClearDay3UnpluggedLedGlow", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(cachePropsMethod);
                Assert.IsNotNull(setLedMethod);
                Assert.IsNotNull(clearLedMethod);

                cachePropsMethod.Invoke(tempDir, null);
                setLedMethod.Invoke(tempDir, new object[] { true });
                Assert.IsTrue(tempDir.IsDay3UnpluggedLedEmissiveActive,
                    "Day 3 unplugged answering machine LED must enable _EMISSION keyword so it genuinely glows red in darkness.");
                Assert.Greater(secD3LedRenderer.sharedMaterial.GetColor("_EmissionColor").r, 1.0f,
                    "Day 3 unplugged LED _EmissionColor.r must be HDR red when lit.");

                clearLedMethod.Invoke(tempDir, null);
                Assert.IsFalse(tempDir.IsDay3UnpluggedLedEmissiveActive,
                    "Clearing Day 3 unplugged LED must turn off emissive glow.");
                Assert.AreSame(origSecD3LedMat, secD3LedRenderer.sharedMaterial,
                    "Clearing Day 3 unplugged LED must restore original dark LED material.");

                // Verify Day 3 newspaper clue interrupted immediately by another clue STILL notifies horrorDirector.NotifyDay3NewspaperRead()
                DayManager tempDm = tempDirectorGo.AddComponent<DayManager>();
                FieldInfo tempDmDayFi = typeof(DayManager).GetField("currentDay", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo tempDmDirFi = typeof(DayManager).GetField("horrorDirector", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo handleTaskMi = typeof(DayManager).GetMethod("HandleTaskCompleted", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo dirDayFi = typeof(PsychologicalHorrorDirector).GetField("activeDay", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo dirNewsFi = typeof(PsychologicalHorrorDirector).GetField("day3NewspaperRead", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(tempDmDayFi);
                Assert.IsNotNull(tempDmDirFi);
                Assert.IsNotNull(handleTaskMi);
                Assert.IsNotNull(dirDayFi);
                Assert.IsNotNull(dirNewsFi);

                tempDmDayFi.SetValue(tempDm, GameDay.Day3);
                tempDmDirFi.SetValue(tempDm, tempDir);
                dirDayFi.SetValue(tempDir, GameDay.Day3);
                dirNewsFi.SetValue(tempDir, false);

                handleTaskMi.Invoke(tempDm, new object[] { "arquivo_metalico_opc" });
                handleTaskMi.Invoke(tempDm, new object[] { "bilhete_margaret_dia3_opc" });
                Assert.IsTrue((bool)dirNewsFi.GetValue(tempDir),
                    "Interrupting arquivo_metalico_opc reaction with another Day 3 clue must still notify horrorDirector.NotifyDay3NewspaperRead().");
            }
            finally
            {
                secD3LedRenderer.sharedMaterial = origSecD3LedMat;
                secD3LedRenderer.SetPropertyBlock(null);
                Object.DestroyImmediate(tempDirectorGo);
            }

            // Verify ConfigureStaticFlagsAndBatching clears stale BatchingStatic flags on nested dynamic subtrees (idempotent re-bake safety)
            GameObject tempBatchArch = new GameObject("__Test_BatchArchRoot");
            GameObject tempBatchDaily = new GameObject("__Test_BatchDailyRoot");
            try
            {
                GameObject staticParent = new GameObject("Mesa_Jantar_Vazia");
                staticParent.transform.SetParent(tempBatchArch.transform, false);
                staticParent.AddComponent<MeshFilter>();
                staticParent.AddComponent<MeshRenderer>();

                GameObject dynChair = new GameObject("Cadeira_Jantar_Norte");
                dynChair.transform.SetParent(staticParent.transform, false);
                dynChair.AddComponent<MeshFilter>();
                dynChair.AddComponent<MeshRenderer>();
                GameObjectUtility.SetStaticEditorFlags(dynChair, StaticEditorFlags.BatchingStatic);

                GameObject dailyMachine = new GameObject("Secretaria_Eletronica_D3");
                dailyMachine.transform.SetParent(tempBatchDaily.transform, false);
                GameObject dynLed = new GameObject("LED_Mensagens");
                dynLed.transform.SetParent(dailyMachine.transform, false);
                dynLed.AddComponent<MeshFilter>();
                dynLed.AddComponent<MeshRenderer>();
                GameObjectUtility.SetStaticEditorFlags(dynLed, StaticEditorFlags.BatchingStatic);

                MethodInfo configBatchMi = typeof(Canal420GreyboxBuilder).GetMethod("ConfigureStaticFlagsAndBatching", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(configBatchMi);
                configBatchMi.Invoke(null, new object[] { null, tempBatchArch.transform, new[] { tempBatchDaily.transform } });

                AssertSubtreeNotBatchingStatic(dynChair.transform, "Temp/Cadeira_Jantar_Norte");
                AssertSubtreeNotBatchingStatic(dynLed.transform, "Temp/LED_Mensagens");
                Assert.AreNotEqual(0, (int)(GameObjectUtility.GetStaticEditorFlags(staticParent) & StaticEditorFlags.BatchingStatic),
                    "Static furniture parent must still receive BatchingStatic.");
            }
            finally
            {
                Object.DestroyImmediate(tempBatchArch);
                Object.DestroyImmediate(tempBatchDaily);
            }

            // 4. Phase 3.3 Climax Margaret spawn safety inside Quarto_Filhas obstacle-free zone + FlashlightController climax API
            (Vector3 playerPos, Vector3 camFwd)[] climaxSamples =
            {
                (new Vector3(3.55f, 0f, 4.35f), Vector3.left),
                (new Vector3(3.45f, 0f, 4.50f), Vector3.left),
                (new Vector3(3.45f, 0f, 4.35f), Vector3.right),
                (new Vector3(4.30f, 0f, 3.30f), Vector3.right),
                (new Vector3(3.85f, 0f, 2.80f), Vector3.forward),
                (new Vector3(4.10f, 0f, 4.40f), Vector3.back)
            };
            for (int i = 0; i < climaxSamples.Length; i++)
            {
                Vector3 pos = DayManager.ComputeSafeMargaretClimaxPosition(climaxSamples[i].playerPos, climaxSamples[i].camFwd);
                Assert.AreEqual(0f, pos.y, 0.0001f, $"Climax Margaret pos #{i} must stay grounded at Y=0.");
                Assert.GreaterOrEqual(pos.x, 3.07f, $"Climax Margaret pos #{i} clipped west wall/door.");
                Assert.LessOrEqual(pos.x, 4.79f, $"Climax Margaret pos #{i} clipped east beds/table.");
                Assert.GreaterOrEqual(pos.z, 2.14f, $"Climax Margaret pos #{i} clipped south dresser/bed.");
                Assert.LessOrEqual(pos.z, 4.49f, $"Climax Margaret pos #{i} clipped Cama_Filha_1 footboard corner (Z >= 4.776).");
                float dist = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(climaxSamples[i].playerPos.x, climaxSamples[i].playerPos.z));
                Assert.GreaterOrEqual(dist, 1.04f, $"Climax Margaret pos #{i} spawned too close to player ({dist:F2}m).");
            }

            FlashlightController fc = Object.FindAnyObjectByType<FlashlightController>(FindObjectsInactive.Include);
            Assert.IsNotNull(fc, "FlashlightController missing.");
            try
            {
                fc.SetEquipped(true, turnOnImmediately: true, playSound: false);
                fc.StartClimaxWeakTremble(0.26f, 0.56f);
                Assert.IsTrue(fc.IsClimaxOverrideActive, "StartClimaxWeakTremble must enable climax override.");
                Assert.IsTrue(fc.IsLightOn, "StartClimaxWeakTremble must keep weak beam on.");
                fc.StopClimaxEffectsAndBlackout();
                Assert.IsFalse(fc.IsClimaxOverrideActive, "StopClimaxEffectsAndBlackout must clear climax override.");
                Assert.IsFalse(fc.IsLightOn, "StopClimaxEffectsAndBlackout must extinguish beam.");
            }
            finally
            {
                fc.SetEquipped(false, turnOnImmediately: false, playSound: false);
            }
        }

        private static void AssertMeshAssetAssigned(Transform root, string relativePath, string expectedMeshName)
        {
            Transform tr = root.Find(relativePath);
            if (tr == null)
            {
                string[] parts = relativePath.Split('/');
                Transform cur = FindChildRecursive(root, parts[0]);
                for (int i = 1; i < parts.Length && cur != null; i++)
                {
                    cur = cur.Find(parts[i]);
                }
                tr = cur;
            }
            Assert.IsNotNull(tr, $"Expected upgraded object '{relativePath}' not found under '{root.name}'.");
            MeshFilter mf = tr.GetComponent<MeshFilter>();
            Assert.IsNotNull(mf, $"MeshFilter missing on '{relativePath}'.");
            Assert.IsNotNull(mf.sharedMesh, $"sharedMesh null on '{relativePath}'.");
            Assert.AreEqual(expectedMeshName, mf.sharedMesh.name,
                $"Object '{relativePath}' expected custom mesh '{expectedMeshName}', found '{mf.sharedMesh.name}'.");
        }

        private static void AssertWorldPosApprox(string objName, Vector3 expectedWorld)
        {
            GameObject go = FindInActiveOrInactive(objName);
            Assert.IsNotNull(go, $"Locked geometry object '{objName}' not found.");
            Assert.LessOrEqual(Vector3.Distance(go.transform.position, expectedWorld), 0.01f,
                $"Geometry '{objName}' shifted unexpectedly! Expected {expectedWorld}, actual {go.transform.position}.");
        }

        private static GameObject FindInactiveRoot(string name)
        {
            Scene active = SceneManager.GetActiveScene();
            GameObject[] roots = active.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name) return roots[i];
            }
            return null;
        }

        [Test]
        public void P0StabilityAudioPerformanceAndUiInvariants()
        {
            // 1. AudioManager connects all 22 real footstep .wav clips and leaves CassetteTapeHauntD2 unmapped to 0.6s fallback
            Canal420AudioManager audioMgr = Object.FindAnyObjectByType<Canal420AudioManager>();
            Assert.IsNotNull(audioMgr, "Canal420AudioManager missing in scene.");
            AudioClip[] woodSteps = GetPrivateField<AudioClip[]>(audioMgr, "customWoodSteps");
            AudioClip[] tileSteps = GetPrivateField<AudioClip[]>(audioMgr, "customTileSteps");
            AudioClip[] heavySteps = GetPrivateField<AudioClip[]>(audioMgr, "customHeavySteps");
            Assert.IsNotNull(woodSteps);
            Assert.IsNotNull(tileSteps);
            Assert.IsNotNull(heavySteps);
            Assert.AreEqual(9, woodSteps.Length, "Expected all 9 recorded wood footstep clips connected.");
            Assert.AreEqual(9, tileSteps.Length, "Expected all 9 recorded tile footstep clips connected.");
            Assert.AreEqual(4, heavySteps.Length, "Expected all 4 recorded heavy footstep clips connected.");

            var overrides = GetPrivateField<List<Canal420AudioManager.FoleyClipOverride>>(audioMgr, "foleyOverrides");
            Assert.IsNotNull(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                if (overrides[i].soundId == FoleySoundId.CassetteTapeHauntD2 && overrides[i].clip != null)
                {
                    Assert.AreNotEqual("answering_machine_play", overrides[i].clip.name,
                        "CassetteTapeHauntD2 must not fall back to the 0.60s answering_machine_play clip.");
                }
            }

            // 2. ObjectiveHUD restores activeTaskRowGroup alpha to 1f when a new task arrives after queue empties
            GameObject hudHost = new GameObject("__Test_ObjectiveHUD_P0Alpha");
            try
            {
                ObjectiveHUD hud = hudHost.AddComponent<ObjectiveHUD>();
                hud.ConfigureDayHeader("DIA 2", Color.white);
                hud.AddOrUpdateObjective("t1", "Primeira tarefa", completed: false, visible: true);
                CanvasGroup rowGroup = GetPrivateField<CanvasGroup>(hud, "activeTaskRowGroup");
                Assert.IsNotNull(rowGroup);
                rowGroup.alpha = 0f;
                hud.MarkCompleted("t1", silentCompletion: true);
                hud.AddOrUpdateObjective("t2", "Nova tarefa apos esvaziar", completed: false, visible: true);
                Assert.AreEqual(1f, rowGroup.alpha, 0.001f,
                    "ObjectiveHUD must restore activeTaskRowGroup.alpha to 1f when displaying a newly added task.");
            }
            finally
            {
                Object.DestroyImmediate(hudHost);
            }

            // 3. ProceduralCRTFeed caches black-cleared state so repeated SetMode(Off) does not re-upload every frame
            GameObject crtHost = new GameObject("__Test_CRT_P0OffCache");
            try
            {
                ProceduralCRTFeed crt = crtHost.AddComponent<ProceduralCRTFeed>();
                crt.SetMode(CRTFeedMode.Off);
                Assert.IsTrue(GetPrivateField<bool>(crt, "isClearedToBlack"));
                crt.SetMode(CRTFeedMode.BroadcastChannel04);
                Assert.IsFalse(GetPrivateField<bool>(crt, "isClearedToBlack"));
                crt.SetMode(CRTFeedMode.Off);
                Assert.IsTrue(GetPrivateField<bool>(crt, "isClearedToBlack"));
            }
            finally
            {
                Object.DestroyImmediate(crtHost);
            }

            // 4. MirrorHauntController portal culling allows inside bathroom and culls living room / office / bedroom
            MirrorHauntController mirrorHaunt = Object.FindAnyObjectByType<MirrorHauntController>(FindObjectsInactive.Include);
            Assert.IsNotNull(mirrorHaunt, "MirrorHauntController missing.");
            MethodInfo portalCheck = typeof(MirrorHauntController).GetMethod("CanCameraSeeMirrorThroughBathroomPortal", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(portalCheck);
            Bounds mirrorBounds = new Bounds(new Vector3(-4.02f, 1.62f, 6.018f), new Vector3(0.764f, 0.824f, 0.02f));
            Assert.IsTrue((bool)portalCheck.Invoke(mirrorHaunt, new object[] { new Vector3(-4.5f, 1.6f, 4.0f), mirrorBounds.center, mirrorBounds }),
                "Mirror must render when camera is inside the bathroom.");
            Assert.IsFalse((bool)portalCheck.Invoke(mirrorHaunt, new object[] { new Vector3(0.0f, 1.6f, -3.0f), mirrorBounds.center, mirrorBounds }),
                "Mirror must be culled in the living room.");
            Assert.IsFalse((bool)portalCheck.Invoke(mirrorHaunt, new object[] { new Vector3(4.0f, 1.6f, -2.0f), mirrorBounds.center, mirrorBounds }),
                "Mirror must be culled in the office.");
            Assert.IsFalse((bool)portalCheck.Invoke(mirrorHaunt, new object[] { new Vector3(-0.5f, 1.6f, 4.2f), mirrorBounds.center, mirrorBounds }),
                "Mirror must be culled in the master bedroom.");

            // 5. GameSettingsUI modal includes Restart and Quit actions
            GameObject settingsHost = new GameObject("__Test_Settings_P0Buttons");
            try
            {
                GameSettingsUI settings = settingsHost.AddComponent<GameSettingsUI>();
                MethodInfo buildUi = typeof(GameSettingsUI).GetMethod("BuildUIIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(buildUi);
                buildUi.Invoke(settings, null);
                GameObject canvasRoot = GetPrivateField<GameObject>(settings, "canvasRoot");
                Assert.IsNotNull(canvasRoot);
                Assert.IsNotNull(FindChildRecursive(canvasRoot.transform, "Btn_RestartGame"), "GameSettingsUI must provide Btn_RestartGame.");
                Assert.IsNotNull(FindChildRecursive(canvasRoot.transform, "Btn_QuitGame"), "GameSettingsUI must provide Btn_QuitGame.");
            }
            finally
            {
                Object.DestroyImmediate(settingsHost);
            }
        }

        [Test]
        public void P1PauseProjectorDoorAndAudioMixInvariants()
        {
            // 1. Projector resolution safety: preset 0 (2560x1664) clamps cleanly on 1080p / 720p displays
            Resolution[] projector1080p =
            {
                new Resolution { width = 1280, height = 720, refreshRateRatio = new RefreshRate { numerator = 60, denominator = 1 } },
                new Resolution { width = 1920, height = 1080, refreshRateRatio = new RefreshRate { numerator = 60, denominator = 1 } }
            };
            GameSettingsUI.ResolveTargetResolutionFromCandidates(0, projector1080p, 1920, 1080, 1920, 1080, out int pw, out int ph);
            Assert.AreEqual(1920, pw, "2560x1664 preset must clamp to 1920x1080 on a 1080p projector.");
            Assert.AreEqual(1080, ph, "2560x1664 preset must clamp to 1920x1080 on a 1080p projector.");

            // 2. Partially ajar door prompt must advertise opening (not closing)
            GameObject doorHost = new GameObject("__Test_AjarDoorPrompt");
            try
            {
                DoorInteractable door = doorHost.AddComponent<DoorInteractable>();
                door.OpenToPartialAngle(24f, playSound: false);
                Assert.AreEqual("Abrir porta", door.GetInteractionPrompt(),
                    "Partially ajar door must display openPrompt since Interact() opens it fully.");
            }
            finally
            {
                Object.DestroyImmediate(doorHost);
            }

            // 3. AudioManager does not map EerieLullabyHum to the duplicate TV drone clip
            Canal420AudioManager audioMgr = Object.FindAnyObjectByType<Canal420AudioManager>();
            Assert.IsNotNull(audioMgr);
            var overrides = GetPrivateField<List<Canal420AudioManager.FoleyClipOverride>>(audioMgr, "foleyOverrides");
            Assert.IsNotNull(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                if (overrides[i].soundId == FoleySoundId.EerieLullabyHum && overrides[i].clip != null)
                {
                    Assert.AreNotEqual("eerie_lullaby_hum", overrides[i].clip.name,
                        "EerieLullabyHum must not use the duplicate tv_420_hypnotic.mp3 copy.");
                }
            }

            // 4. SafeBoxPuzzle builds all 12 keypad buttons for gamepad navigation
            GameObject safeHost = new GameObject("__Test_SafeGamepadButtons");
            try
            {
                SafeBoxPuzzle safe = safeHost.AddComponent<SafeBoxPuzzle>();
                MethodInfo buildKeypad = typeof(SafeBoxPuzzle).GetMethod("BuildKeypadUIIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(buildKeypad);
                buildKeypad.Invoke(safe, null);
                UnityEngine.UI.Button[] buttons = GetPrivateField<UnityEngine.UI.Button[]>(safe, "keypadButtons");
                Assert.IsNotNull(buttons);
                Assert.AreEqual(12, buttons.Length);
                for (int i = 0; i < buttons.Length; i++)
                {
                    Assert.IsNotNull(buttons[i], $"SafeBoxPuzzle keypadButtons[{i}] must be wired for gamepad navigation.");
                }
            }
            finally
            {
                GameObject keypadRoot = GetPrivateField<GameObject>(safeHost.GetComponent<SafeBoxPuzzle>(), "keypadCanvasRoot");
                if (keypadRoot != null) Object.DestroyImmediate(keypadRoot);
                Object.DestroyImmediate(safeHost);
            }
        }

        private static GameObject FindInActiveOrInactive(string name)
        {
            Scene active = SceneManager.GetActiveScene();
            GameObject[] roots = active.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindChildRecursive(roots[i].transform, name);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        private static Transform FindChildRecursive(Transform root, string exactName)
        {
            if (root == null) return null;
            if (root.name == exactName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform res = FindChildRecursive(root.GetChild(i), exactName);
                if (res != null) return res;
            }
            return null;
        }

        private static Light FindLight(Light[] lights, string nameFragment)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                Light l = lights[i];
                if (l == null) continue;
                string fullName = l.transform.parent != null ? $"{l.transform.parent.name}_{l.name}" : l.name;
                if (fullName.ToLowerInvariant().Contains(nameFragment.ToLowerInvariant()))
                    return l;
            }
            Assert.Fail($"Light matching '{nameFragment}' not found.");
            return null;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }
    }
}
#endif
