#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
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
    public static class Canal420SceneValidator
    {
        [MenuItem("Canal 420/Validar Referências da Cena Aberta", priority = 10)]
        public static void ValidateOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[Canal 420 Validador] Nenhuma cena válida carregada.");
                return;
            }

            StringBuilder report = new StringBuilder();
            int errorCount = 0;
            int warningCount = 0;

            report.AppendLine($"=== RELATÓRIO DE VALIDAÇÃO: {scene.name} ===");

            DayManager dm = Object.FindAnyObjectByType<DayManager>();
            if (dm == null)
            {
                report.AppendLine("• [ERRO] DayManager não foi encontrado na cena!");
                errorCount++;
            }
            else
            {
                report.AppendLine("✓ DayManager encontrado.");
                CheckSerializedField(dm, "casaBase", ref errorCount, report);
                CheckSerializedField(dm, "propsDia1", ref errorCount, report);
                CheckSerializedField(dm, "propsDia2", ref errorCount, report);
                CheckSerializedField(dm, "propsDia3", ref errorCount, report);
                CheckSerializedField(dm, "quartoFilhas", ref errorCount, report);
                CheckSerializedField(dm, "player", ref errorCount, report);
                CheckSerializedField(dm, "sanityEffects", ref errorCount, report);
                CheckSerializedField(dm, "tvController", ref errorCount, report);
                CheckSerializedField(dm, "objectiveHUD", ref errorCount, report);
                CheckSerializedField(dm, "audioManager", ref errorCount, report);
                CheckSerializedField(dm, "officeDoor", ref errorCount, report);
                CheckSerializedField(dm, "corridorLockedDoor", ref errorCount, report);
                CheckSerializedField(dm, "spawnDia1", ref errorCount, report);
                CheckSerializedField(dm, "spawnSofaSentado", ref errorCount, report);
                CheckSerializedField(dm, "spawnDia2Sofa", ref errorCount, report);
                CheckSerializedField(dm, "spawnDia3", ref errorCount, report);
                CheckSerializedField(dm, "spawnDia4QuartoFilhas", ref errorCount, report);
            }

            PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc == null)
            {
                report.AppendLine("• [ERRO] PlayerController não foi encontrado na cena!");
                errorCount++;
            }
            else
            {
                report.AppendLine("✓ PlayerController encontrado.");
                CheckSerializedField(pc, "cameraTransform", ref errorCount, report);
                CharacterController cc = pc.GetComponent<CharacterController>();
                if (cc == null)
                {
                    report.AppendLine("• [ERRO] CharacterController ausente no Player!");
                    errorCount++;
                }
            }

            InteractionSystem isys = Object.FindAnyObjectByType<InteractionSystem>();
            if (isys == null)
            {
                report.AppendLine("• [ERRO] InteractionSystem não foi encontrado!");
                errorCount++;
            }
            else
            {
                report.AppendLine("✓ InteractionSystem encontrado.");
                CheckSerializedField(isys, "raycastOrigin", ref errorCount, report);
            }

            TVController tv = Object.FindAnyObjectByType<TVController>();
            if (tv == null)
            {
                report.AppendLine("• [ERRO] TVController não foi encontrado!");
                errorCount++;
            }
            else
            {
                report.AppendLine("✓ TVController encontrado.");
                CheckSerializedField(tv, "screenRenderer", ref errorCount, report);
            }

            SafeBoxPuzzle safe = Object.FindAnyObjectByType<SafeBoxPuzzle>(FindObjectsInactive.Include);
            if (safe == null)
            {
                report.AppendLine("• [ERRO] SafeBoxPuzzle não encontrado nem em objetos inativos!");
                errorCount++;
            }
            else
            {
                report.AppendLine("✓ SafeBoxPuzzle encontrado.");
                CheckSerializedField(safe, "safeDoorPivot", ref errorCount, report);
                CheckSerializedField(safe, "digitalVisorRenderer", ref errorCount, report);
                CheckSerializedField(safe, "brassKeyVisualInside", ref errorCount, report);
            }

            HallucinationTrigger[] hTriggers = Object.FindObjectsByType<HallucinationTrigger>(FindObjectsInactive.Include);
            if (hTriggers == null || hTriggers.Length < 3)
            {
                report.AppendLine($"• [ERRO] Esperados ao menos 3 HallucinationTriggers no Dia 2, encontrados: {(hTriggers == null ? 0 : hTriggers.Length)}!");
                errorCount++;
            }
            else
            {
                report.AppendLine($"✓ {hTriggers.Length} HallucinationTriggers encontrados e configurados.");
            }

            FallingPictureFrame frame = Object.FindAnyObjectByType<FallingPictureFrame>(FindObjectsInactive.Include);
            if (frame == null)
            {
                report.AppendLine("• [AVISO] FallingPictureFrame não encontrado!");
                warningCount++;
            }
            else
            {
                report.AppendLine("✓ FallingPictureFrame encontrado.");
                CheckSerializedField(frame, "hiddenSafeBox", ref errorCount, report);
            }

            SanityEffects sanity = Object.FindAnyObjectByType<SanityEffects>();
            if (sanity == null)
            {
                report.AppendLine("• [ERRO] SanityEffects não encontrado!");
                errorCount++;
            }
            else
            {
                report.AppendLine("✓ SanityEffects encontrado.");
                CheckSerializedField(sanity, "globalVolume", ref errorCount, report);
            }

            Canal420AudioManager audioMgr = Object.FindAnyObjectByType<Canal420AudioManager>();
            if (audioMgr == null)
            {
                report.AppendLine("• [ERRO] Canal420AudioManager não encontrado!");
                errorCount++;
            }
            else
            {
                SerializedObject audioSo = new SerializedObject(audioMgr);
                int woodCount = audioSo.FindProperty("customWoodSteps")?.arraySize ?? 0;
                int tileCount = audioSo.FindProperty("customTileSteps")?.arraySize ?? 0;
                int heavyCount = audioSo.FindProperty("customHeavySteps")?.arraySize ?? 0;
                if (woodCount == 0 || tileCount == 0 || heavyCount == 0)
                {
                    report.AppendLine($"• [ERRO] Canal420AudioManager com passos gravados vazios (wood={woodCount}, tile={tileCount}, heavy={heavyCount})!");
                    errorCount++;
                }
                else
                {
                    report.AppendLine($"✓ Canal420AudioManager com passos gravados conectados (wood={woodCount}, tile={tileCount}, heavy={heavyCount}).");
                }

                SerializedProperty foleyProp = audioSo.FindProperty("foleyOverrides");
                if (foleyProp != null)
                {
                    for (int i = 0; i < foleyProp.arraySize; i++)
                    {
                        SerializedProperty elem = foleyProp.GetArrayElementAtIndex(i);
                        SerializedProperty idProp = elem.FindPropertyRelative("soundId");
                        SerializedProperty clipProp = elem.FindPropertyRelative("clip");
                        if (idProp != null && idProp.intValue == (int)FoleySoundId.CassetteTapeHauntD2 &&
                            clipProp != null && clipProp.objectReferenceValue is AudioClip clip &&
                            clip.name == "answering_machine_play")
                        {
                            report.AppendLine("• [ERRO] CassetteTapeHauntD2 está apontando para o fallback curto answering_machine_play (0,60s)!");
                            errorCount++;
                        }
                    }
                }
            }

            AnimatedContainerInteractable[] containers = Object.FindObjectsByType<AnimatedContainerInteractable>(FindObjectsInactive.Include);
            if (containers == null || containers.Length < 10)
            {
                report.AppendLine($"• [ERRO] Esperados ao menos 10 AnimatedContainerInteractables na casa, encontrados: {(containers == null ? 0 : containers.Length)}!");
                errorCount++;
            }
            else
            {
                report.AppendLine($"✓ {containers.Length} gavetas/armários animados encontrados.");
                bool foundMargaretLockedDrawer = false;
                for (int i = 0; i < containers.Length; i++)
                {
                    AnimatedContainerInteractable c = containers[i];
                    if (c == null) continue;
                    CheckSerializedField(c, "movingPart", ref errorCount, report);
                    if (c.NormalizedId == "gaveta_margaret_trancada")
                    {
                        SerializedObject so = new SerializedObject(c);
                        SerializedProperty lockedProp = so.FindProperty("isPermanentlyLocked");
                        if (lockedProp != null && lockedProp.boolValue)
                        {
                            foundMargaretLockedDrawer = true;
                        }
                        else
                        {
                            report.AppendLine("  - [ERRO] Gaveta do criado-mudo de Margaret encontrada, mas NÃO está marcada como permanentemente trancada!");
                            errorCount++;
                        }
                    }
                }

                if (!foundMargaretLockedDrawer)
                {
                    report.AppendLine("• [ERRO] Gaveta permanentemente trancada de Margaret ('gaveta_margaret_trancada') não encontrada!");
                    errorCount++;
                }
                else
                {
                    report.AppendLine("✓ Gaveta do criado-mudo de Margaret confirmada como permanentemente trancada.");
                }
            }

            report.AppendLine($"=== FIM: {errorCount} Erro(s), {warningCount} Aviso(s) ===");

            if (errorCount > 0)
            {
                Debug.LogError(report.ToString());
            }
            else if (warningCount > 0)
            {
                Debug.LogWarning(report.ToString());
            }
            else
            {
                Debug.Log($"[Canal 420 Validador] 100% ÍNTEGRO! Todas as referências da cena estão perfeitamente conectadas.\n{report}");
            }
        }

        private static void CheckSerializedField(Object target, string fieldName, ref int errorCount, StringBuilder report)
        {
            if (target == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (fi == null)
                {
                    report.AppendLine($"  - [AVISO] Campo '{fieldName}' não encontrado no tipo {target.GetType().Name}.");
                    return;
                }
                object val = fi.GetValue(target);
                if (val == null || (val is Object obj && obj == null))
                {
                    report.AppendLine($"  - [ERRO] {target.name}.{fieldName} está NULO!");
                    errorCount++;
                }
                return;
            }

            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (prop.objectReferenceValue == null)
                {
                    report.AppendLine($"  - [ERRO] {target.name}.{fieldName} está NULO!");
                    errorCount++;
                }
            }
        }

        [MenuItem("Canal 420/Auditoria Espacial (Z-Fighting, Frestas e Ancoragem)", priority = 11)]
        public static void AuditSceneGeometry()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[Canal 420 Auditoria Espacial] Nenhuma cena ativa carregada.");
                return;
            }

            StringBuilder report = new StringBuilder();
            int errors = 0;
            int warnings = 0;

            report.AppendLine("=== AUDITORIA ESPACIAL (v15): STAGE 1, MESHES, Z-FIGHTING, ANCORAGEM E CIRCULAÇÃO ===");

            GameObject casaBase = FindRootInScene(scene, "Casa_Base");
            GameObject quartoFilhas = FindRootInScene(scene, "Quarto_Filhas");
            GameObject propsDia1 = FindRootInScene(scene, "Props_Dia1");
            GameObject propsDia2 = FindRootInScene(scene, "Props_Dia2");
            GameObject propsDia3 = FindRootInScene(scene, "Props_Dia3");

            if (casaBase == null)
            {
                Debug.LogError("[Canal 420 Auditoria Espacial] Casa_Base não encontrada na cena.");
                return;
            }

            // 1. Verificar métricas do Jogador (cápsula 1.80m x raio 0.30m; câmera 1.65m)
            PlayerController pc = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null)
            {
                CharacterController cc = pc.GetComponent<CharacterController>();
                Camera cam = pc.GetComponentInChildren<Camera>(true);
                if (cc == null || Mathf.Abs(cc.height - 1.80f) > 0.02f || Mathf.Abs(cc.radius - 0.30f) > 0.02f)
                {
                    report.AppendLine($"  - [ERRO] Cápsula do jogador fora da métrica padrão 1.80m/r=0.30m (atual: h={cc?.height}, r={cc?.radius}).");
                    errors++;
                }
                else
                {
                    report.AppendLine("  - [OK] Métrica humana do Player validada (altura=1.80m, raio=0.30m, olhos=1.65m).");
                }
                if (cam != null && Mathf.Abs(cam.transform.localPosition.y - 1.65f) > 0.05f)
                {
                    report.AppendLine($"  - [AVISO] Altura da câmera difere de 1.65m (atual: {cam.transform.localPosition.y:F2}m).");
                    warnings++;
                }
            }

            // 2. Verificar contenção no envelope estrutural e ausência de vazamentos para o vazio
            Bounds envelope = new Bounds(new Vector3(-0.10f, 1.44f, 0.10f), new Vector3(13.20f, 2.88f, 12.20f));
            List<MeshRenderer> allRenderers = new List<MeshRenderer>();
            CollectRenderersUnder(casaBase.transform, allRenderers);
            if (quartoFilhas != null) CollectRenderersUnder(quartoFilhas.transform, allRenderers);
            if (propsDia1 != null) CollectRenderersUnder(propsDia1.transform, allRenderers);
            if (propsDia2 != null) CollectRenderersUnder(propsDia2.transform, allRenderers);
            if (propsDia3 != null) CollectRenderersUnder(propsDia3.transform, allRenderers);

            int leakedCount = 0;
            foreach (MeshRenderer mr in allRenderers)
            {
                if (mr == null) continue;
                Bounds b = ComputeSafeWorldBounds(mr);
                if (b.min.x < envelope.min.x - 0.01f || b.max.x > envelope.max.x + 0.01f ||
                    b.min.z < envelope.min.z - 0.01f || b.max.z > envelope.max.z + 0.01f)
                {
                    if (!mr.name.StartsWith("Calha_") &&
                        !mr.name.StartsWith("Soleira_Entrada") &&
                        !IsAllowedWindowShadowBoxExtension(mr.name, b, envelope))
                    {
                        report.AppendLine($"  - [AVISO] Geometria '{GetPath(mr.transform)}' excede envelope externo ({b.min:F2}..{b.max:F2}).");
                        leakedCount++;
                        warnings++;
                    }
                }
            }
            if (leakedCount == 0)
            {
                report.AppendLine("  - [OK] Nenhum elemento interno vaza para fora do envelope [-6.70..+6.50] x [-6.00..+6.20].");
            }

            // 3. Varredura de Z-fighting por grupos coexistentes (Casa_Base + cada dia / quarto separado)
            int zFightCount = 0;
            zFightCount += AuditGroupZFighting("Casa_Base + Dia 1", report, casaBase.transform, propsDia1?.transform);
            zFightCount += AuditGroupZFighting("Casa_Base + Dia 2", report, casaBase.transform, propsDia2?.transform);
            zFightCount += AuditGroupZFighting("Casa_Base + Dia 3", report, casaBase.transform, propsDia3?.transform);
            zFightCount += AuditGroupZFighting("Casa_Base + Quarto_Filhas", report, casaBase.transform, quartoFilhas?.transform);
            errors += zFightCount;

            if (zFightCount == 0)
            {
                report.AppendLine("  - [OK] Zero superfícies coplanares em Z-fighting (< 0.5mm) detectadas nos grupos da cena.");
            }

            // 4. Auditoria de ancoragem no chão dos móveis principais e portas
            int groundIssues = AuditFurnitureGrounding(casaBase.transform, report);
            if (quartoFilhas != null) groundIssues += AuditFurnitureGrounding(quartoFilhas.transform, report);
            if (propsDia3 != null) groundIssues += AuditFallenChairGrounding(propsDia3.transform, report);
            errors += groundIssues;

            if (groundIssues == 0)
            {
                report.AppendLine("  - [OK] 100% dos móveis estruturais principais estão corretamente ancorados sem flutuar nem afundar.");
            }

            // 5. Auditoria de Stage 1 (envelope, pé-direito, pisos, portas), escalas válidas (sem NaN/zero/negativo) e folgas de spawn
            int stage1Issues = AuditStage1AndTransforms(report, casaBase.transform, quartoFilhas?.transform, propsDia1?.transform, propsDia2?.transform, propsDia3?.transform);
            errors += stage1Issues;
            if (stage1Issues == 0)
            {
                report.AppendLine("  - [OK] Stage 1 (envelope 13.20x12.20m, H=2.74m, pisos, portas) e transformações (sem NaN/zero/negativo) íntegros.");
            }

            // 6. Auditoria de topologia fechada (watertight / volume > 0) em meshes procedurais salvos em Assets/
            int meshIssues = AuditCustomMeshesWatertightness(report, casaBase.transform, quartoFilhas?.transform, propsDia1?.transform, propsDia2?.transform, propsDia3?.transform);
            errors += meshIssues;
            if (meshIssues == 0)
            {
                report.AppendLine("  - [OK] 100% dos custom meshes procedurais estão fechados (watertight, volume > 0, normais consistentes).");
            }

            // 7. Auditoria de obstrução em spawns e circulação livre de clutter
            int clutterIssues = AuditWalkwayAndSpawnClearance(report, propsDia1?.transform, propsDia2?.transform, propsDia3?.transform);
            errors += clutterIssues;
            if (clutterIssues == 0)
            {
                report.AppendLine("  - [OK] Spawns e áreas de circulação livres de obstruções ou clutter inválido.");
            }

            // 8. Auditoria de vãos reais de janela + ShadowBoxes TwoSided, nicho do cofre 1009, sofá e Dia 4
            int phase1Issues = AuditWindowShadowBoxesAndSafeNiche(report, casaBase.transform, quartoFilhas?.transform, propsDia3?.transform);
            errors += phase1Issues;
            if (phase1Issues == 0)
            {
                report.AppendLine("  - [OK] Vãos reais de janela + ShadowBoxes TwoSided, nicho do cofre 1009, colisor composto do sofá e Dia 4 validados.");
            }

            // 9. Auditoria da Fase 2: materiais estruturais sem ruído PBR, rótulos/papéis legíveis e separação de vidros
            int phase2Issues = AuditPhase2PrintedDetailsAndGlass(report, casaBase.transform, quartoFilhas?.transform, propsDia1?.transform, propsDia2?.transform, propsDia3?.transform);
            errors += phase2Issues;
            if (phase2Issues == 0)
            {
                report.AppendLine("  - [OK] Fase 2 validada: superfícies estruturais lisas sem ruído PBR, impressos nítidos vinculados e vidros M_Clear_Glass / M_Frosted_Glass separados de M_Mirror_Glass.");
            }

            // 10. Auditoria da Fase 3: silhuetas refinadas, acabamento TV_Sala, cabeamento nas paredes e micro-props sóbrios
            int phase3Issues = AuditPhase3GeometryDetails(report, casaBase.transform, propsDia2?.transform, propsDia3?.transform);
            errors += phase3Issues;
            if (phase3Issues == 0)
            {
                report.AppendLine("  - [OK] Fase 3 validada: silhuetas humanas contínuas, TV_Sala recuada c/ vidro, cabos de tomada sem colisor e micro-props sóbrios apoiados.");
            }

            if (errors == 0)
            {
                Debug.Log($"[Canal 420 Auditoria Espacial] APROVADA ({warnings} avisos).\n{report}");
            }
            else
            {
                Debug.LogError($"[Canal 420 Auditoria Espacial] {errors} erro(s) e {warnings} aviso(s):\n{report}");
            }
        }

        private static bool IsAllowedWindowShadowBoxExtension(string objName, Bounds b, Bounds envelope)
        {
            bool isShadowBoxMember =
                objName.StartsWith("ShadowBox_") ||
                objName.StartsWith("Caixilho_") ||
                objName.StartsWith("Grade_Janela_") ||
                objName.StartsWith("Veneziana_Ext_") ||
                objName.StartsWith("Vidro_Janela_");
            if (!isShadowBoxMember) return false;

            return b.min.x >= envelope.min.x - 0.32f && b.max.x <= envelope.max.x + 0.32f &&
                   b.min.z >= envelope.min.z - 0.32f && b.max.z <= envelope.max.z + 0.32f;
        }

        private static Transform FindDescendantExact(Transform root, string exactName)
        {
            if (root == null) return null;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == exactName)
                    return all[i];
            }
            return null;
        }

        private static int AuditWindowShadowBoxesAndSafeNiche(
            StringBuilder report, Transform casaBase, Transform quartoFilhas, Transform propsDia3)
        {
            int issues = 0;

            string[] wallCutouts =
            {
                "Parede_Sul_Peitoril_0", "Parede_Sul_Verga_0",
                "Parede_Norte_Peitoril_0", "Parede_Norte_Verga_0",
                "Parede_Oeste_Peitoril_0", "Parede_Oeste_Verga_0",
                "Parede_Leste_Peitoril_0", "Parede_Leste_Verga_0"
            };
            for (int i = 0; i < wallCutouts.Length; i++)
            {
                if (FindDescendantExact(casaBase, wallCutouts[i]) == null)
                {
                    report.AppendLine($"  - [ERRO] Recorte real de janela ausente: '{wallCutouts[i]}'.");
                    issues++;
                }
            }

            string[] windowTags = { "SalaSul", "QuartoNorte", "CozinhaOeste", "QuartoFilhas" };
            string[] jambSuffixes = { "_Sup", "_Inf", "_Min", "_Max" };
            string[] boxSuffixes = { "_Fundo", "_Sup", "_Inf", "_Min", "_Max" };

            for (int w = 0; w < windowTags.Length; w++)
            {
                string tag = windowTags[w];
                for (int s = 0; s < jambSuffixes.Length; s++)
                {
                    string jambName = $"Batente_Janela_{tag}{jambSuffixes[s]}";
                    if (FindDescendantExact(casaBase, jambName) == null)
                    {
                        report.AppendLine($"  - [ERRO] Batente de janela '{jambName}' ausente na espessura da parede.");
                        issues++;
                    }
                }

                for (int s = 0; s < boxSuffixes.Length; s++)
                {
                    string panelName = $"ShadowBox_{tag}{boxSuffixes[s]}";
                    Transform panelTr = FindDescendantExact(casaBase, panelName);
                    if (panelTr == null || !panelTr.TryGetComponent<MeshRenderer>(out MeshRenderer mr))
                    {
                        report.AppendLine($"  - [ERRO] Fechamento de ShadowBox '{panelName}' ausente.");
                        issues++;
                    }
                    else if (mr.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.TwoSided)
                    {
                        report.AppendLine($"  - [ERRO] ShadowBox '{panelName}' precisa de ShadowCastingMode.TwoSided (atual: {mr.shadowCastingMode}).");
                        issues++;
                    }
                }
            }

            if (quartoFilhas != null)
            {
                Transform boarded = FindDescendantExact(quartoFilhas, "Janela_Lacrada_Tabuas");
                if (boarded == null)
                {
                    report.AppendLine("  - [ERRO] 'Janela_Lacrada_Tabuas' ausente em Quarto_Filhas.");
                    issues++;
                }
                else
                {
                    if (FindDescendantExact(boarded, "Vao_Janela_Escuro") != null)
                    {
                        report.AppendLine("  - [ERRO] 'Janela_Lacrada_Tabuas' ainda contém plano falso 'Vao_Janela_Escuro' bloqueando o vão real.");
                        issues++;
                    }
                    string[] hollowBars = { "Moldura_Filhas_Top", "Moldura_Filhas_Base", "Moldura_Filhas_Sul", "Moldura_Filhas_Norte" };
                    for (int b = 0; b < hollowBars.Length; b++)
                    {
                        if (FindDescendantExact(boarded, hollowBars[b]) == null)
                        {
                            report.AppendLine($"  - [ERRO] Barra vazada '{hollowBars[b]}' ausente em 'Janela_Lacrada_Tabuas'.");
                            issues++;
                        }
                    }
                }
            }

            Transform nichoFundo = FindDescendantExact(casaBase, "Parede_Corredor_Filhas_NichoFundo");
            if (nichoFundo == null || !nichoFundo.TryGetComponent<MeshRenderer>(out MeshRenderer nichoMr))
            {
                report.AppendLine("  - [ERRO] Fundo selado 'Parede_Corredor_Filhas_NichoFundo' do nicho do cofre ausente.");
                issues++;
            }
            else if (nichoMr.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.TwoSided)
            {
                report.AppendLine($"  - [ERRO] 'Parede_Corredor_Filhas_NichoFundo' exige ShadowCastingMode.TwoSided (atual: {nichoMr.shadowCastingMode}).");
                issues++;
            }

            if (propsDia3 != null)
            {
                Transform safeTr = FindDescendantExact(propsDia3, "Cofre_Parede_1009");
                if (safeTr == null || safeTr.position.x < 2.55f)
                {
                    report.AppendLine($"  - [ERRO] 'Cofre_Parede_1009' não está embutido no nicho da parede X=2.52..2.64 (posX={safeTr?.position.x:F3}).");
                    issues++;
                }

                Transform frameTr = FindDescendantExact(propsDia3, "Quadro_Margaret_Parede");
                if (frameTr == null || Mathf.Abs(frameTr.position.x - 2.503f) > 0.005f)
                {
                    report.AppendLine($"  - [ERRO] 'Quadro_Margaret_Parede' desalinhado do plano de parede X=2.503 (posX={frameTr?.position.x:F3}).");
                    issues++;
                }
            }

            Transform sofaTr = FindDescendantExact(casaBase, "Sofa_Sala");
            if (sofaTr != null)
            {
                BoxCollider[] sofaCols = sofaTr.GetComponents<BoxCollider>();
                float minWorldZ = float.MaxValue;
                float maxWorldZ = float.MinValue;
                for (int i = 0; i < sofaCols.Length; i++)
                {
                    Vector3 c = sofaCols[i].center;
                    Vector3 h = sofaCols[i].size * 0.5f;
                    for (int sx = -1; sx <= 1; sx += 2)
                        for (int sy = -1; sy <= 1; sy += 2)
                            for (int sz = -1; sz <= 1; sz += 2)
                            {
                                Vector3 wp = sofaTr.TransformPoint(c + new Vector3(sx * h.x, sy * h.y, sz * h.z));
                                if (wp.z < minWorldZ) minWorldZ = wp.z;
                                if (wp.z > maxWorldZ) maxWorldZ = wp.z;
                            }
                }
                if (sofaCols.Length < 4 || minWorldZ > -2.535f || maxWorldZ < -1.665f)
                {
                    report.AppendLine($"  - [ERRO] Colisor composto de 'Sofa_Sala' incompleto (count={sofaCols.Length}, Z=[{minWorldZ:F3}..{maxWorldZ:F3}] vs esperado [-2.54..-1.66]).");
                    issues++;
                }
            }

            GameObject spSofaSit = GameObject.Find("Spawn_Sofa_Sentado");
            if (spSofaSit != null)
            {
                Vector3 p = spSofaSit.transform.position;
                if (Mathf.Abs(p.x - (-0.84f)) > 0.05f || Mathf.Abs(p.z - (-2.14f)) > 0.06f)
                {
                    report.AppendLine($"  - [ERRO] 'Spawn_Sofa_Sentado' fora do assento central do sofá ({p:F3}, esperado ~(-0.84, 0.02, -2.14)).");
                    issues++;
                }
            }

            GameObject spDia4 = GameObject.Find("Spawn_Dia4_QuartoFilhas");
            if (spDia4 != null && spDia4.transform.position.x > 3.30f)
            {
                report.AppendLine($"  - [ERRO] 'Spawn_Dia4_QuartoFilhas' avançado demais (X={spDia4.transform.position.x:F2}), cortando camas do ângulo restrito.");
                issues++;
            }

            if (FindDescendantExact(casaBase, "Interruptor_Quarto_Filhas") == null)
            {
                report.AppendLine("  - [ERRO] 'Interruptor_Quarto_Filhas' ausente em Quarto_Filhas.");
                issues++;
            }

            Transform medCab = FindDescendantExact(casaBase, "Armario_Remedios_Banheiro");
            if (medCab != null)
            {
                Transform medBack = FindDescendantExact(medCab, "Fundo_Armario");
                if (medBack != null && medBack.TryGetComponent<MeshRenderer>(out MeshRenderer medMr))
                {
                    if (Mathf.Abs(medMr.bounds.max.x - (-3.520f)) > 0.008f)
                    {
                        report.AppendLine($"  - [ERRO] 'Armario_Remedios_Banheiro' não encosta em X=-3.520 (max.x={medMr.bounds.max.x:F4}).");
                        issues++;
                    }
                }
            }

            return issues;
        }

        private static int AuditPhase2PrintedDetailsAndGlass(
            StringBuilder report, Transform casaBase, Transform quartoFilhas, Transform propsDia1, Transform propsDia2, Transform propsDia3)
        {
            int issues = 0;

            string[] smoothStructuralMats =
            {
                "Assets/Materials/Greybox/M_Wall_Plaster.mat",
                "Assets/Materials/Greybox/M_Floor_Wood.mat",
                "Assets/Materials/Greybox/M_Floor_Tile.mat",
                "Assets/Materials/Greybox/M_Ceiling.mat",
                "Assets/Materials/Greybox/M_Baseboard_Wood.mat",
                "Assets/Materials/Greybox/M_Door_Wood.mat",
                "Assets/Materials/Greybox/M_Furniture_Wood.mat",
                "Assets/Materials/Greybox/M_Wood_Light.mat",
                "Assets/Materials/Greybox/M_Wood_Mahogany.mat",
                "Assets/Materials/Greybox/M_TV_Screen.mat"
            };
            string[] forbiddenTexProps = { "_BaseMap", "_MainTex", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" };
            for (int i = 0; i < smoothStructuralMats.Length; i++)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(smoothStructuralMats[i]);
                if (mat == null) continue;
                for (int p = 0; p < forbiddenTexProps.Length; p++)
                {
                    if (mat.HasProperty(forbiddenTexProps[p]) && mat.GetTexture(forbiddenTexProps[p]) != null)
                    {
                        report.AppendLine($"  - [ERRO FASE 2] Material liso/CRT '{mat.name}' não deve conter textura PBR em '{forbiddenTexProps[p]}'.");
                        issues++;
                    }
                }
                if (mat.name != "M_TV_Screen" && mat.HasProperty("_SurfaceTex"))
                {
                    if (mat.GetTexture("_SurfaceTex") == null)
                    {
                        report.AppendLine($"  - [ERRO FASE 2] Material retro '{mat.name}' deveria ter textura simples em '_SurfaceTex'.");
                        issues++;
                    }
                    else if (mat.HasProperty("_SurfaceTexStrength"))
                    {
                        float strength = mat.GetFloat("_SurfaceTexStrength");
                        if (strength < 0.08f || strength > 0.30f)
                        {
                            report.AppendLine($"  - [ERRO FASE 2] '_SurfaceTexStrength' de '{mat.name}' fora do limite sutil [0.08, 0.30] (atual={strength:F2}).");
                            issues++;
                        }
                    }
                }
            }

            List<MeshRenderer> allRenderers = new List<MeshRenderer>();
            CollectRenderersUnder(casaBase, allRenderers);
            CollectRenderersUnder(quartoFilhas, allRenderers);
            CollectRenderersUnder(propsDia1, allRenderers);
            CollectRenderersUnder(propsDia2, allRenderers);
            CollectRenderersUnder(propsDia3, allRenderers);

            int mirrorGlassCount = 0;
            int clearGlassCount = 0;
            int frostedGlassCount = 0;

            for (int i = 0; i < allRenderers.Count; i++)
            {
                MeshRenderer mr = allRenderers[i];
                if (mr == null) continue;
                Material mat = mr.sharedMaterial;
                string name = mr.gameObject.name;

                if (mat != null && mat.name == "M_Mirror_Glass")
                {
                    mirrorGlassCount++;
                    if (name != "Lamina_Espelho")
                    {
                        report.AppendLine($"  - [ERRO FASE 2] 'M_Mirror_Glass' deve ficar exclusivo ao espelho do banheiro (encontrado em '{GetPath(mr.transform)}').");
                        issues++;
                    }
                }
                else if (mat != null && (mat.name == "M_Clear_Glass" || mat.name == "M_Frosted_Glass"))
                {
                    if (mat.name == "M_Clear_Glass") clearGlassCount++;
                    else frostedGlassCount++;

                    if (mat.renderQueue < 3000)
                    {
                        report.AppendLine($"  - [ERRO FASE 2] Vidro '{mat.name}' precisa estar em Surface Transparent (renderQueue={mat.renderQueue}).");
                        issues++;
                    }
                    if (mr.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                    {
                        report.AppendLine($"  - [ERRO FASE 2] Vidro transparente '{GetPath(mr.transform)}' exige ShadowCastingMode.Off.");
                        issues++;
                    }
                }

                bool isPrintedDetail =
                    name.StartsWith("Impressao_") ||
                    name == "Arte_Quadro" ||
                    name.StartsWith("Rotulo_") ||
                    name == "Capa_Fosforos" ||
                    name == "Frente_Caixa_Antiacido" ||
                    name == "Corpo_Pilha_AA" ||
                    name.StartsWith("Digito_LED_") ||
                    name == "DoisPontos_LED";

                if (isPrintedDetail)
                {
                    if (mat == null || mat.mainTexture == null)
                    {
                        report.AppendLine($"  - [ERRO FASE 2] Detalhe impresso/arte '{GetPath(mr.transform)}' está com material ou textura nula.");
                        issues++;
                    }
                }
            }

            if (mirrorGlassCount != 1)
            {
                report.AppendLine($"  - [ERRO FASE 2] Esperado exatamente 1 renderer usando 'M_Mirror_Glass' (Espelho_Banheiro/Lamina_Espelho), encontrado {mirrorGlassCount}.");
                issues++;
            }
            if (clearGlassCount < 8)
            {
                report.AppendLine($"  - [ERRO FASE 2] Esperados >= 8 vidros transparentes usando 'M_Clear_Glass', encontrados {clearGlassCount}.");
                issues++;
            }
            if (frostedGlassCount < 1)
            {
                report.AppendLine("  - [ERRO FASE 2] Esperado 'M_Frosted_Glass' em Porta_Aerea_2 do armário da cozinha.");
                issues++;
            }

            return issues;
        }

        private static int AuditPhase3GeometryDetails(
            StringBuilder report, Transform casaBase, Transform propsDia2, Transform propsDia3)
        {
            int issues = 0;

            // 1. Verificar os 5 cabos das tomadas (sem colisor, sem projeção de sombra, malha válida)
            string[] cables =
            {
                "Cabo_Tomada_TV_Sala",
                "Cabo_Tomada_Telefone_Corredor",
                "Cabo_Tomada_Mesa_Escritorio",
                "Cabo_Tomada_Cabeceira_Cian",
                "Cabo_Tomada_Geladeira"
            };
            foreach (string cableName in cables)
            {
                Transform cable = FindDeepChild(casaBase, cableName);
                if (cable == null)
                {
                    report.AppendLine($"  - [ERRO FASE 3] Cabo de parede '{cableName}' ausente.");
                    issues++;
                    continue;
                }
                if (cable.GetComponent<Collider>() != null)
                {
                    report.AppendLine($"  - [ERRO FASE 3] Cabo '{cableName}' não pode possuir Collider.");
                    issues++;
                }
                if (cable.TryGetComponent<MeshRenderer>(out MeshRenderer mr) &&
                    mr.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    report.AppendLine($"  - [ERRO FASE 3] Cabo '{cableName}' deve ter ShadowCastingMode.Off.");
                    issues++;
                }
            }

            // 2. Verificar acabamento da TV_Sala (aro chanfrado, tela recuada, vidro M_Clear_Glass, grades paralelas e traseira)
            Transform tv = FindDeepChild(casaBase, "TV_Sala");
            if (tv == null)
            {
                report.AppendLine("  - [ERRO FASE 3] 'TV_Sala' ausente.");
                issues++;
            }
            else
            {
                Transform tela = tv.Find("Tela_TV");
                Transform aro = tv.Find("Aro_Frontal_TV");
                Transform vidro = tv.Find("Vidro_Frontal_TV");
                Transform falante = tv.Find("Tira_Falante_Esq_0");
                Transform placaTras = tv.Find("Placa_Entradas_TV");
                if (tela == null || aro == null || vidro == null || falante == null || placaTras == null)
                {
                    report.AppendLine("  - [ERRO FASE 3] Acabamento completo de 'TV_Sala' incompleto (Tela/Aro/Vidro/Tira_Falante/Placa_Entradas).");
                    issues++;
                }
                else
                {
                    if (vidro.position.z <= tela.position.z)
                    {
                        report.AppendLine("  - [ERRO FASE 3] 'Vidro_Frontal_TV' deve estar à frente da 'Tela_TV' recuada.");
                        issues++;
                    }
                    if (vidro.GetComponent<Collider>() != null)
                    {
                        report.AppendLine("  - [ERRO FASE 3] 'Vidro_Frontal_TV' não deve possuir Collider.");
                        issues++;
                    }
                }
            }

            // 3. Verificar modelos 3D humanos com rigging esquelético (SkinnedMeshRenderer + Animator) e juntas obrigatórias
            (string Name, Transform SearchRoot)[] silhouettes =
            {
                ("Vulto_Sombra_Negra", propsDia2),
                ("Watcher_Figura_D2", propsDia2),
                ("Aparicao_Porta_Banheiro_D2", propsDia2),
                ("Watcher_Figura_D3", propsDia3),
                ("Margaret_Jumpscare", propsDia3)
            };
            string[] reqJoints = { "Torso_Esguio", "Ombros_Curvados", "Braco_Esq", "Braco_Dir", "Pescoco_Inclinado", "Cabeca_Silhueta" };
            foreach (var entry in silhouettes)
            {
                Transform sil = FindDeepChild(entry.SearchRoot, entry.Name);
                if (sil == null)
                {
                    report.AppendLine($"  - [ERRO FASE 3] Silhueta '{entry.Name}' não encontrada.");
                    issues++;
                    continue;
                }
                foreach (string j in reqJoints)
                {
                    if (sil.Find(j) == null)
                    {
                        report.AppendLine($"  - [ERRO FASE 3] Silhueta '{entry.Name}' perdeu junta animada '{j}'.");
                        issues++;
                    }
                }
                Transform head = sil.Find("Cabeca_Silhueta");
                if (head != null && head.Find("Boca_Escura") == null)
                {
                    report.AppendLine($"  - [ERRO FASE 3] Silhueta '{entry.Name}/Cabeca_Silhueta' sem 'Boca_Escura'.");
                    issues++;
                }

                SkinnedMeshRenderer smr = sil.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Animator anim = sil.GetComponent<Animator>();
                if (smr == null || smr.sharedMesh == null)
                {
                    report.AppendLine($"  - [ERRO FASE 3] Personagem '{entry.Name}' sem SkinnedMeshRenderer / malha rigged.");
                    issues++;
                }
                else
                {
                    int triCount = smr.sharedMesh.triangles.Length / 3;
                    if (triCount < 1200 || triCount > 2500)
                    {
                        report.AppendLine($"  - [ERRO FASE 3] Personagem '{entry.Name}' fora da faixa PS1 1200..2500 triângulos (atual={triCount}).");
                        issues++;
                    }
                    if (smr.bones == null || smr.bones.Length < 15)
                    {
                        report.AppendLine($"  - [ERRO FASE 3] Personagem '{entry.Name}' com esqueleto incompleto (bones={smr.bones?.Length ?? 0}).");
                        issues++;
                    }
                }
                if (anim == null || anim.runtimeAnimatorController == null)
                {
                    report.AppendLine($"  - [ERRO FASE 3] Personagem '{entry.Name}' sem Animator/AnimatorController vinculado.");
                    issues++;
                }

                Renderer[] renderers = sil.GetComponentsInChildren<Renderer>(true);
                float minY = float.MaxValue;
                foreach (Renderer r in renderers)
                {
                    if (r == null) continue;
                    Bounds sb = ComputeSafeWorldBounds(r);
                    if (sb.min.y < minY) minY = sb.min.y;
                }
                if (minY < -0.005f || minY > 0.015f)
                {
                    report.AppendLine($"  - [ERRO FASE 3] Silhueta '{entry.Name}' mal ancorada no piso (min.y={minY:F4}m).");
                    issues++;
                }
            }

            // 4. Micro-props e saturação <= 0.45f das novas capas/ímãs
            string[] requiredProps =
            {
                "Ima_Geladeira_Coral", "Ima_Geladeira_Azul", "Ima_Geladeira_Ocre",
                "Conjunto_Coador_Cafe_Balcao", "Cinzeiro_MesaCentro",
                "Livro_Aberto_Poltrona", "Grampeador_Escritorio", "Bloco_Notas_Escritorio"
            };
            foreach (string propName in requiredProps)
            {
                if (FindDeepChild(casaBase, propName) == null)
                {
                    report.AppendLine($"  - [ERRO FASE 3] Micro-prop '{propName}' ausente em Casa_Base.");
                    issues++;
                }
            }

            string[] mutedMatPaths =
            {
                "Assets/Materials/Greybox/M_Book_SlateBlue.mat",
                "Assets/Materials/Greybox/M_Book_Burgundy.mat",
                "Assets/Materials/Greybox/M_Book_Mustard.mat",
                "Assets/Materials/Greybox/M_Book_Teal.mat",
                "Assets/Materials/Greybox/M_Book_Sage.mat",
                "Assets/Materials/Greybox/M_Book_Taupe.mat",
                "Assets/Materials/Greybox/M_Magnet_Coral.mat",
                "Assets/Materials/Greybox/M_Magnet_Blue.mat",
                "Assets/Materials/Greybox/M_Magnet_Ochre.mat"
            };
            foreach (string mPath in mutedMatPaths)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(mPath);
                if (mat == null)
                {
                    report.AppendLine($"  - [ERRO FASE 3] Material sóbrio obrigatório ausente em '{mPath}'.");
                    issues++;
                    continue;
                }
                Color.RGBToHSV(mat.color, out _, out float sat, out _);
                if (sat > 0.45f)
                {
                    report.AppendLine($"  - [ERRO FASE 3] Material '{mat.name}' excede saturação máxima 0.45 (sat={sat:F2}).");
                    issues++;
                }
            }

            return issues;
        }

        private static void CollectRenderersUnder(Transform root, List<MeshRenderer> target)
        {
            if (root == null) return;
            target.AddRange(root.GetComponentsInChildren<MeshRenderer>(true));
        }

        private struct AuditBox
        {
            public string Name;
            public string Path;
            public string MaterialName;
            public Vector3 Min;
            public Vector3 Max;
        }

        private static int AuditGroupZFighting(string label, StringBuilder report, params Transform[] roots)
        {
            List<AuditBox> boxes = new List<AuditBox>();
            foreach (Transform r in roots)
            {
                if (r == null) continue;
                MeshRenderer[] mrs = r.GetComponentsInChildren<MeshRenderer>(true);
                foreach (MeshRenderer mr in mrs)
                {
                    if (mr == null) continue;
                    MeshFilter mf = mr.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null || mf.sharedMesh.name != "Cube") continue;
                    // Apenas caixas ortogonais (rotação múltipla de 90°) para precisão exata de faces
                    if (!IsOrthogonalRotation(mr.transform.rotation)) continue;

                    Bounds b = ComputeSafeWorldBounds(mr);
                    boxes.Add(new AuditBox
                    {
                        Name = mr.name,
                        Path = GetPath(mr.transform),
                        MaterialName = mr.sharedMaterial != null ? mr.sharedMaterial.name : "",
                        Min = b.min,
                        Max = b.max
                    });
                }
            }

            int count = 0;
            const float epsPlane = 0.0019f;
            const float minOverlap = 0.004f;

            for (int i = 0; i < boxes.Count; i++)
            {
                AuditBox a = boxes[i];
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    AuditBox b = boxes[j];
                    if (IsStage1Path(a.Path) && IsStage1Path(b.Path)) continue;
                    if (AreMutuallyExclusivePaths(a.Path, b.Path)) continue;
                    if (OverlapLen(a.Min.y, a.Max.y, b.Min.y, b.Max.y) > minOverlap &&
                        OverlapLen(a.Min.z, a.Max.z, b.Min.z, b.Max.z) > minOverlap &&
                        (Mathf.Abs(a.Min.x - b.Min.x) < epsPlane || Mathf.Abs(a.Max.x - b.Max.x) < epsPlane))
                    {
                        report.AppendLine($"  - [Z-FIGHT ({label}) X] '{a.Path}' vs '{b.Path}'");
                        count++;
                    }
                    if (OverlapLen(a.Min.x, a.Max.x, b.Min.x, b.Max.x) > minOverlap &&
                        OverlapLen(a.Min.z, a.Max.z, b.Min.z, b.Max.z) > minOverlap)
                    {
                        bool minHit = Mathf.Abs(a.Min.y - b.Min.y) < epsPlane && !IsExpectedFloorOrCarpetBottom(a, b);
                        bool maxHit = Mathf.Abs(a.Max.y - b.Max.y) < epsPlane && !IsExpectedCeilingTop(a, b);
                        if (minHit || maxHit)
                        {
                            report.AppendLine($"  - [Z-FIGHT ({label}) Y] '{a.Path}' vs '{b.Path}'");
                            count++;
                        }
                    }
                    if (OverlapLen(a.Min.x, a.Max.x, b.Min.x, b.Max.x) > minOverlap &&
                        OverlapLen(a.Min.y, a.Max.y, b.Min.y, b.Max.y) > minOverlap &&
                        (Mathf.Abs(a.Min.z - b.Min.z) < epsPlane || Mathf.Abs(a.Max.z - b.Max.z) < epsPlane))
                    {
                        report.AppendLine($"  - [Z-FIGHT ({label}) Z] '{a.Path}' vs '{b.Path}'");
                        count++;
                    }
                }
            }
            return count;
        }

        private static bool IsExpectedFloorOrCarpetBottom(AuditBox a, AuditBox b)
        {
            if (Mathf.Abs(a.Min.y) < 0.001f && Mathf.Abs(b.Min.y) < 0.001f)
                return true;
            if (Mathf.Abs(a.Min.y - b.Min.y) < 0.0019f &&
                (a.Min.y >= 0.0065f && a.Min.y <= 0.0125f))
                return true;
            return false;
        }

        private static bool IsExpectedCeilingTop(AuditBox a, AuditBox b)
        {
            return Mathf.Abs(a.Max.y - 2.74f) < 0.001f && Mathf.Abs(b.Max.y - 2.74f) < 0.001f &&
                   (a.Name.StartsWith("Parede_") || a.Name.Contains("_Verga_") || a.Name.StartsWith("Sanca_")) &&
                   (b.Name.StartsWith("Parede_") || b.Name.Contains("_Verga_") || b.Name.StartsWith("Sanca_"));
        }

        private static int AuditFurnitureGrounding(Transform root, StringBuilder report)
        {
            string[] baseAnchors =
            {
                "Tapete_Sala", "Rack_TV", "Mesa_Centro", "Sofa_Sala", "Estante_Sala", "Poltrona_Canto", "Luminaria_Coluna_Sala",
                "Geladeira_Copa", "Balcao_Cozinha_Sul", "Fogao", "Aparador_Copa", "Mesa_Jantar_Vazia",
                "Gabinete_Pia_Banheiro", "Box_Chuveiro", "Vaso_Sanitario", "Estante_Utilidades_Banheiro",
                "Tapete_Quarto", "Cama_Casal_Cian", "Criado_Mudo_Cian", "Criado_Mudo_Margaret", "Guarda_Roupa_Cian", "Comoda_Quarto",
                "Passadeira_Corredor", "Capacho_Entrada", "Cabideiro_Entrada", "Aparador_Telefone_Corredor",
                "Tapete_Escritorio", "Mesa_Escritorio", "Cadeira_Escritorio", "Cesto_Lixo_Escritorio", "Estante_Escritorio", "Arquivo_Metalico"
            };
            string[] daughtersAnchors =
            {
                "Tapete_Infantil", "Cama_Filha_1", "Cama_Filha_2", "Mesinha_Infantil", "Comoda_Infantil"
            };
            string[] anchorContainers = root.name == "Quarto_Filhas" ? daughtersAnchors : baseAnchors;

            int issues = 0;
            foreach (string anchorName in anchorContainers)
            {
                Transform target = FindDeepChild(root, anchorName);
                if (target == null) continue;

                MeshRenderer[] parts = target.GetComponentsInChildren<MeshRenderer>(true);
                if (parts.Length == 0) continue;

                float lowestY = float.MaxValue;
                foreach (MeshRenderer p in parts)
                {
                    if (p == null) continue;
                    Bounds pb = ComputeSafeWorldBounds(p);
                    if (pb.min.y < lowestY)
                    {
                        lowestY = pb.min.y;
                    }
                }

                // Aceita piso Y=0.000f ou topo de tapetes (Y=0.008f..0.010f)
                bool validFloorContact = Mathf.Abs(lowestY) <= 0.004f ||
                                         (lowestY >= 0.007f && lowestY <= 0.012f);
                if (!validFloorContact)
                {
                    report.AppendLine($"  - [ERRO ANCORAGEM] '{anchorName}' base mais baixa em Y={lowestY:F4}m (esperado 0.000m ou 0.008..0.010m sobre tapete).");
                    issues++;
                }
            }
            return issues;
        }

        private static int AuditFallenChairGrounding(Transform propsDia3Root, StringBuilder report)
        {
            if (propsDia3Root == null) return 0;
            Transform fallen = FindDeepChild(propsDia3Root, "Cadeira_Tombada_Caos");
            if (fallen == null) return 0;

            MeshRenderer[] parts = fallen.GetComponentsInChildren<MeshRenderer>(true);
            float lowestY = float.MaxValue;
            foreach (MeshRenderer p in parts)
            {
                if (p == null) continue;
                Bounds pb = ComputeSafeWorldBounds(p);
                if (pb.min.y < lowestY) lowestY = pb.min.y;
            }
            if (Mathf.Abs(lowestY) > 0.004f)
            {
                report.AppendLine($"  - [ERRO ANCORAGEM] 'Cadeira_Tombada_Caos' base em Y={lowestY:F4}m (esperado 0.000m).");
                return 1;
            }
            return 0;
        }

        private static int AuditStage1AndTransforms(StringBuilder report, params Transform[] roots)
        {
            int issues = 0;
            foreach (Transform r in roots)
            {
                if (r == null) continue;
                Transform[] all = r.GetComponentsInChildren<Transform>(true);
                foreach (Transform tr in all)
                {
                    Vector3 lp = tr.localPosition;
                    Vector3 ls = tr.localScale;
                    Quaternion lr = tr.localRotation;
                    if (float.IsNaN(lp.x) || float.IsNaN(lp.y) || float.IsNaN(lp.z) ||
                        float.IsInfinity(lp.x) || float.IsInfinity(lp.y) || float.IsInfinity(lp.z) ||
                        float.IsNaN(lr.x) || float.IsNaN(lr.y) || float.IsNaN(lr.z) || float.IsNaN(lr.w))
                    {
                        report.AppendLine($"  - [ERRO TRANSFORM] '{GetPath(tr)}' possui NaN/Infinity em posição ou rotação.");
                        issues++;
                    }
                    if (float.IsNaN(ls.x) || float.IsNaN(ls.y) || float.IsNaN(ls.z) ||
                        ls.x <= 1e-5f || ls.y <= 1e-5f || ls.z <= 1e-5f)
                    {
                        report.AppendLine($"  - [ERRO ESCALA] '{GetPath(tr)}' possui escala nula/negativa/NaN ({ls}).");
                        issues++;
                    }
                }
            }

            Transform casaBase = roots.Length > 0 ? roots[0] : null;
            if (casaBase != null)
            {
                Transform ceiling = FindDeepChild(casaBase, "Teto_Residencia");
                MeshRenderer ceilingMr = ceiling != null ? ceiling.GetComponent<MeshRenderer>() : null;
                if (ceilingMr == null)
                {
                    report.AppendLine("  - [ERRO STAGE 1] 'Teto_Residencia' ausente.");
                    issues++;
                }
                else
                {
                    Bounds cb = ceilingMr.bounds;
                    if (Mathf.Abs(cb.min.y - 2.74f) > 0.01f ||
                        Mathf.Abs(cb.size.x - 13.20f) > 0.02f ||
                        Mathf.Abs(cb.size.z - 12.20f) > 0.02f)
                    {
                        report.AppendLine($"  - [ERRO STAGE 1] 'Teto_Residencia' fora da métrica 13.20x12.20m / H=2.74m (atual: size={cb.size:F3}, min.y={cb.min.y:F3}).");
                        issues++;
                    }
                }

                string[] requiredDoors =
                {
                    "Porta_Rua_Folha", "Porta_Banheiro_Pivot", "Porta_Quarto_Cian_Pivot", "Porta_Escritorio_Pivot", "Porta_Quarto_Pivot"
                };
                foreach (string doorName in requiredDoors)
                {
                    if (FindDeepChild(casaBase, doorName) == null)
                    {
                        report.AppendLine($"  - [ERRO STAGE 1] Pivô de porta obrigatório '{doorName}' ausente.");
                        issues++;
                    }
                }

                MeshRenderer[] baseRenderers = casaBase.GetComponentsInChildren<MeshRenderer>(true);
                foreach (MeshRenderer mr in baseRenderers)
                {
                    if (mr == null) continue;
                    bool isStructuralFloorSlab = mr.name.StartsWith("Piso_Madeira_") || mr.name.StartsWith("Piso_Frio_");
                    if (isStructuralFloorSlab && Mathf.Abs(mr.bounds.max.y) > 0.002f)
                    {
                        report.AppendLine($"  - [ERRO STAGE 1] Piso '{mr.name}' topo em Y={mr.bounds.max.y:F4}m (esperado 0.000m).");
                        issues++;
                    }
                }
            }

            return issues;
        }

        private static int AuditCustomMeshesWatertightness(StringBuilder report, params Transform[] roots)
        {
            int issues = 0;
            HashSet<Mesh> checkedMeshes = new HashSet<Mesh>();
            List<Mesh> meshesToInspect = new List<Mesh>();
            foreach (Transform r in roots)
            {
                if (r == null) continue;
                MeshFilter[] filters = r.GetComponentsInChildren<MeshFilter>(true);
                for (int i = 0; i < filters.Length; i++)
                {
                    if (filters[i] != null && filters[i].sharedMesh != null)
                        meshesToInspect.Add(filters[i].sharedMesh);
                }
                SkinnedMeshRenderer[] skinned = r.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                for (int i = 0; i < skinned.Length; i++)
                {
                    if (skinned[i] != null && skinned[i].sharedMesh != null)
                        meshesToInspect.Add(skinned[i].sharedMesh);
                }
            }

            foreach (Mesh m in meshesToInspect)
            {
                if (m == null || checkedMeshes.Contains(m)) continue;
                string assetPath = AssetDatabase.GetAssetPath(m);
                if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal)) continue;
                checkedMeshes.Add(m);

                Vector3[] verts = m.vertices;
                Vector3[] norms = m.normals;
                int[] tris = m.triangles;
                if (verts == null || tris == null || tris.Length < 12 || tris.Length % 3 != 0)
                {
                    report.AppendLine($"  - [ERRO MESH] '{m.name}' ({assetPath}) geometria inválida.");
                    issues++;
                    continue;
                }

                var posToCanon = new Dictionary<(long, long, long), int>();
                int[] canonIdx = new int[verts.Length];
                for (int vi = 0; vi < verts.Length; vi++)
                {
                    Vector3 v = verts[vi];
                    var key = (
                        (long)System.Math.Round(v.x * 100000.0),
                        (long)System.Math.Round(v.y * 100000.0),
                        (long)System.Math.Round(v.z * 100000.0)
                    );
                    if (!posToCanon.TryGetValue(key, out int ci))
                    {
                        ci = posToCanon.Count;
                        posToCanon[key] = ci;
                    }
                    canonIdx[vi] = ci;
                }

                Dictionary<(int, int), int> directedEdges = new Dictionary<(int, int), int>();
                double signedVol6 = 0.0;
                int flippedNormals = 0;
                for (int t = 0; t < tris.Length; t += 3)
                {
                    int r0 = tris[t], r1 = tris[t + 1], r2 = tris[t + 2];
                    int i0 = canonIdx[r0], i1 = canonIdx[r1], i2 = canonIdx[r2];
                    if (i0 == i1 || i1 == i2 || i2 == i0) continue;

                    Vector3 p0 = verts[r0], p1 = verts[r1], p2 = verts[r2];
                    Vector3 scaledCross = Vector3.Cross((p1 - p0) * 1000f, (p2 - p0) * 1000f);
                    signedVol6 += Vector3.Dot(p0, Vector3.Cross(p1, p2));

                    if (norms != null && norms.Length == verts.Length && scaledCross.sqrMagnitude > 1e-12f)
                    {
                        Vector3 fn = scaledCross / Mathf.Sqrt(scaledCross.sqrMagnitude);
                        if (Vector3.Dot(fn, norms[r0]) <= 0f || Vector3.Dot(fn, norms[r1]) <= 0f || Vector3.Dot(fn, norms[r2]) <= 0f)
                            flippedNormals++;
                    }

                    (int, int)[] e = { (i0, i1), (i1, i2), (i2, i0) };
                    for (int k = 0; k < 3; k++)
                    {
                        directedEdges[e[k]] = directedEdges.TryGetValue(e[k], out int cnt) ? cnt + 1 : 1;
                    }
                }

                int openEdges = 0;
                foreach (var kv in directedEdges)
                {
                    (int a, int b) = kv.Key;
                    if (!directedEdges.TryGetValue((b, a), out int rev) || rev != kv.Value) openEdges++;
                }

                if (signedVol6 <= 0.0 || openEdges > 0 || flippedNormals > 0)
                {
                    report.AppendLine($"  - [ERRO MESH] '{m.name}' ({assetPath}) falha topológica/winding (vol6={signedVol6:E2}, openEdges={openEdges}, flippedNormals={flippedNormals}).");
                    issues++;
                }
            }
            return issues;
        }

        private static int AuditWalkwayAndSpawnClearance(StringBuilder report, params Transform[] dayRoots)
        {
            int issues = 0;
            string[] spawnNames =
            {
                "Spawn_Dia1", "Spawn_Sofa_Sentado", "Spawn_Dia2", "Spawn_Dia3", "Spawn_Dia4_QuartoFilhas"
            };
            Bounds houseEnvelope = new Bounds(new Vector3(-0.10f, 1.44f, 0.10f), new Vector3(13.20f, 2.88f, 12.20f));
            foreach (string spName in spawnNames)
            {
                GameObject sp = GameObject.Find(spName);
                if (sp == null)
                {
                    report.AppendLine($"  - [ERRO SPAWN] '{spName}' não encontrado na cena.");
                    issues++;
                    continue;
                }
                Vector3 p = sp.transform.position;
                if (!houseEnvelope.Contains(new Vector3(p.x, Mathf.Clamp(p.y, 0.1f, 2.5f), p.z)))
                {
                    report.AppendLine($"  - [ERRO SPAWN] '{spName}' fora do envelope da casa ({p:F2}).");
                    issues++;
                }
            }

            foreach (Transform r in dayRoots)
            {
                if (r == null) continue;
                MeshRenderer[] mrs = r.GetComponentsInChildren<MeshRenderer>(true);
                foreach (MeshRenderer mr in mrs)
                {
                    if (mr == null) continue;
                    Bounds mrb = ComputeSafeWorldBounds(mr);
                    if (mrb.min.y < -0.005f)
                    {
                        report.AppendLine($"  - [ERRO CLUTTER] '{GetPath(mr.transform)}' afundado abaixo do piso (min.y={mrb.min.y:F4}m).");
                        issues++;
                    }
                }
            }
            return issues;
        }

        private static GameObject FindRootInScene(Scene scene, string exactName)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == exactName)
                    return roots[i];
            }
            return null;
        }

        private static Bounds ComputeSafeWorldBounds(Renderer r)
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

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static bool IsOrthogonalRotation(Quaternion rot)
        {
            Vector3 e = rot.eulerAngles;
            return IsRightAngle(e.x) && IsRightAngle(e.y) && IsRightAngle(e.z);
        }

        private static bool IsRightAngle(float deg)
        {
            float m = Mathf.Repeat(deg, 90f);
            return m < 0.25f || m > 89.75f;
        }

        private static float OverlapLen(float aMin, float aMax, float bMin, float bMax)
        {
            return Mathf.Min(aMax, bMax) - Mathf.Max(aMin, bMin);
        }

        private static bool IsStage1Path(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.Contains("/Base1/") || path.EndsWith("/Base1") ||
                   path.Contains("/Piso_") || path.Contains("/Parede_") ||
                   path.Contains("/Batente_") || path.Contains("/Soleira_") ||
                   path.Contains("/Sanca_") || path.Contains("Teto_Residencia") ||
                   path.Contains("Porta_Rua") || path.Contains("_Pivot/");
        }

        private static bool AreMutuallyExclusivePaths(string pathA, string pathB)
        {
            if ((pathA.Contains("Quadro_Normal_") && pathB.Contains("Quadro_Distorcido_")) ||
                (pathB.Contains("Quadro_Normal_") && pathA.Contains("Quadro_Distorcido_")))
                return true;
            if (pathA.Contains("Vulto_Sombra_Negra") || pathB.Contains("Vulto_Sombra_Negra"))
                return true;
            if (pathA.Contains("Aparicao_Porta_Banheiro_D2") || pathB.Contains("Aparicao_Porta_Banheiro_D2"))
                return true;
            if (pathA.Contains("Fusivel_Instalado_Mesh") || pathB.Contains("Fusivel_Instalado_Mesh"))
                return true;
            if ((pathA.Contains("Estado_Ligado") && pathB.Contains("Estado_Desligado")) ||
                (pathB.Contains("Estado_Ligado") && pathA.Contains("Estado_Desligado")))
                return true;
            return false;
        }

        private static string GetPath(Transform tr)
        {
            if (tr == null) return "";
            StringBuilder sb = new StringBuilder(tr.name);
            Transform cur = tr.parent;
            while (cur != null)
            {
                sb.Insert(0, '/');
                sb.Insert(0, cur.name);
                cur = cur.parent;
            }
            return sb.ToString();
        }
    }
}
#endif
