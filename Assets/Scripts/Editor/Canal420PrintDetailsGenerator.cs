#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Canal420.EditorTools
{
    internal struct PrintDetailsPalette
    {
        public Material calendarOct1995;
        public Material calendarSep1994;
        public Material pillBottleLabel;
        public Material blisterPackBack;
        public Material stimulantBottleLabel;
        public Material stimulantBlisterBack;
        public Material antacidBox;
        public Material cassetteLabel;
        public Material vhsSpine0;
        public Material vhsSpine1;
        public Material vhsSpine2;
        public Material aaBatteryWrap;
        public Material detergentLabel;
        public Material bleachLabel;
        public Material coffeeTinLabel;
        public Material sugarJarLabel;
        public Material matchboxCover;
        public Material shampooLabel;
        public Material clockDigit2;
        public Material clockDigit3;
        public Material clockDigit4;
        public Material clockDigit0;
        public Material clockColon;
        public Material analogClockDial;
        public Material memoTavares;
        public Material agendaMargaret;
        public Material noteMargaret;
        public Material fridgeList;
        public Material fridgeBillNotice;
        public Material newspaper1994;
        public Material mailEnvelope;
        public Material childDrawingHouse;
        public Material childDrawingFamily;
        public Material frameFamily1;
        public Material frameFamily2;
        public Material frameCorrupted1;
        public Material frameCorrupted2;
        public Material frameMargaretNormal;
        public Material frameMargaretCorrupted;
        public Material clearGlass;
        public Material frostedGlass;
        public Mesh labelCylinderSleeveMesh;
    }

    internal static class Canal420PrintDetailsGenerator
    {
        public const string TexturesFolder = "Assets/Textures/Generated/Details";
        private const string MaterialsFolder = "Assets/Materials/Greybox";
        private const string DetailMaterialsFolder = "Assets/Materials/Greybox/Details";
        private const string MeshesFolder = "Assets/Meshes/Greybox";

        private static readonly Color32 PaperCream = ClampCol(222, 217, 204);
        private static readonly Color32 PaperAged = ClampCol(214, 206, 184);
        private static readonly Color32 PaperNews = ClampCol(186, 184, 176);
        private static readonly Color32 PaperWhiteCrisp = ClampCol(232, 229, 222);
        private static readonly Color32 InkBlack = ClampCol(28, 30, 34);
        private static readonly Color32 InkDark = ClampCol(48, 52, 58);
        private static readonly Color32 InkSoft = ClampCol(96, 102, 110);
        private static readonly Color32 RuleLineBlue = ClampCol(156, 168, 182);
        private static readonly Color32 RedMuted90s = ClampCol(168, 94, 94);
        private static readonly Color32 BlueMuted90s = ClampCol(92, 112, 134);
        private static readonly Color32 YellowMuted90s = ClampCol(174, 152, 98);
        private static readonly Color32 GreenMuted90s = ClampCol(102, 136, 110);
        private static readonly Color32 TealMuted90s = ClampCol(100, 146, 146);
        private static readonly Color32 CopperMuted = ClampCol(172, 126, 96);
        private static readonly Color32 FoilSilver = ClampCol(178, 182, 186);

        private static Color32 ClampCol(byte r, byte g, byte b, byte a = 255)
        {
            Color c = new Color(r / 255f, g / 255f, b / 255f, 1f);
            Color.RGBToHSV(c, out float h, out float s, out float v);
            if (s > 0.44f)
            {
                c = Color.HSVToRGB(h, 0.44f, v);
            }
            return new Color32(
                (byte)Mathf.RoundToInt(c.r * 255f),
                (byte)Mathf.RoundToInt(c.g * 255f),
                (byte)Mathf.RoundToInt(c.b * 255f),
                a
            );
        }

        public static PrintDetailsPalette GenerateAllAndLoad()
        {
            EnsureFolders();

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Texture2D texOct95 = SavePng("Tex_Calendario_Out1995", BuildCalendarOct1995());
            Texture2D texSep94 = SavePng("Tex_Calendario_Set1994", BuildCalendarSep1994());
            Texture2D texPill = SavePng("Tex_Rotulo_TarjaPreta", BuildPillBottleLabel());
            Texture2D texBlister = SavePng("Tex_Cartela_Sedativo", BuildBlisterBack());
            Texture2D texStimPill = SavePng("Tex_Rotulo_Manipulado_D3", BuildStimulantBottleLabel());
            Texture2D texStimBlister = SavePng("Tex_Cartela_Estimulante_D3", BuildStimulantBlisterBack());
            Texture2D texAntacid = SavePng("Tex_Caixa_Antiacido", BuildAntacidBoxCover());
            Texture2D texCassette = SavePng("Tex_Fita_Cassete_Etiqueta", BuildCassetteLabel());
            Texture2D texVhs0 = SavePng("Tex_Lombada_VHS_0", BuildVhsSpine("NATAL 93 - PRAIA", RedMuted90s));
            Texture2D texVhs1 = SavePng("Tex_Lombada_VHS_1", BuildVhsSpine("ANIV. MENINAS 94", BlueMuted90s));
            Texture2D texVhs2 = SavePng("Tex_Lombada_VHS_2", BuildVhsSpine("NAO APAGAR / 94", InkDark));
            Texture2D texBat = SavePng("Tex_Pilha_AA", BuildAABatteryTexture());
            Texture2D texDet = SavePng("Tex_Rotulo_Detergente", BuildCylinderLabel("DETERGENTE", "NEUTRO 500ML", YellowMuted90s, PaperWhiteCrisp, InkBlack));
            Texture2D texBleach = SavePng("Tex_Rotulo_AguaSanitaria", BuildCylinderLabel("AGUA SANITARIA", "CLORO ATIVO 2%", BlueMuted90s, PaperWhiteCrisp, InkBlack));
            Texture2D texCoffee = SavePng("Tex_Rotulo_Lata_Cafe", BuildCylinderLabel("CAFE TORRADO", "TRADICIONAL 500G", RedMuted90s, PaperCream, InkBlack));
            Texture2D texSugar = SavePng("Tex_Rotulo_Pote_Acucar", BuildCylinderLabel("ACUCAR", "REFINADO ESPECIAL", BlueMuted90s, PaperWhiteCrisp, InkBlack));
            Texture2D texMatches = SavePng("Tex_Caixa_Fosforos", BuildMatchboxTexture());
            Texture2D texShampoo = SavePng("Tex_Rotulo_Shampoo", BuildCylinderLabel("SHAMPOO SUAVE", "USO DIARIO 350ML", TealMuted90s, PaperWhiteCrisp, InkBlack));

            Texture2D texClk2 = SavePng("Tex_Relogio_Digito_2", BuildSevenSegmentTexture('2'));
            Texture2D texClk3 = SavePng("Tex_Relogio_Digito_3", BuildSevenSegmentTexture('3'));
            Texture2D texClk4 = SavePng("Tex_Relogio_Digito_4", BuildSevenSegmentTexture('4'));
            Texture2D texClk0 = SavePng("Tex_Relogio_Digito_0", BuildSevenSegmentTexture('0'));
            Texture2D texClkCol = SavePng("Tex_Relogio_Colon", BuildSevenSegmentTexture(':'));
            Texture2D texAnalogDial = SavePng("Tex_Mostrador_Relogio_Analogico", BuildAnalogClockDialTexture());

            Texture2D texMemo = SavePng("Tex_Memorando_Tavares", BuildCorporateMemoTexture());
            Texture2D texAgenda = SavePng("Tex_Agenda_Margaret", BuildAgendaTexture());
            Texture2D texNote = SavePng("Tex_Bilhete_Margaret", BuildMargaretNoteTexture());
            Texture2D texList = SavePng("Tex_Lista_Geladeira", BuildFridgeListTexture());
            Texture2D texBill = SavePng("Tex_Aviso_Conta", BuildFridgeBillTexture());
            Texture2D texNews = SavePng("Tex_Jornal_Baixada_1994", BuildNewspaperTexture());
            Texture2D texEnv = SavePng("Tex_Envelope_Carta", BuildEnvelopeTexture());
            Texture2D texHouse = SavePng("Tex_Desenho_Casa", BuildChildHouseDrawing());
            Texture2D texFamilyDraw = SavePng("Tex_Desenho_Familia", BuildChildFamilyDrawing());

            Texture2D texFam1 = SavePng("Tex_Quadro_Familia_1", BuildFamilyPortraitArt(corrupted: false));
            Texture2D texFam2 = SavePng("Tex_Quadro_Familia_2", BuildBeachLandscapeArt(corrupted: false));
            Texture2D texDist1 = SavePng("Tex_Quadro_Distorcido_1", BuildFamilyPortraitArt(corrupted: true));
            Texture2D texDist2 = SavePng("Tex_Quadro_Distorcido_2", BuildBeachLandscapeArt(corrupted: true));
            Texture2D texMargNorm = SavePng("Tex_Quadro_Margaret_Normal", BuildMargaretPortraitArt(corrupted: false));
            Texture2D texMargCorr = SavePng("Tex_Quadro_Margaret_Corrupted", BuildMargaretPortraitArt(corrupted: true));

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            return new PrintDetailsPalette
            {
                calendarOct1995 = CreatePrintedMat(urpLit, "M_Print_Calendario_Out1995", texOct95, 0.14f),
                calendarSep1994 = CreatePrintedMat(urpLit, "M_Print_Calendario_Set1994", texSep94, 0.14f),
                pillBottleLabel = CreatePrintedMat(urpLit, "M_Print_Rotulo_TarjaPreta", texPill, 0.22f),
                blisterPackBack = CreatePrintedMat(urpLit, "M_Print_Cartela_Sedativo", texBlister, 0.68f, metallic: 0.72f),
                stimulantBottleLabel = CreatePrintedMat(urpLit, "M_Print_Rotulo_Manipulado_D3", texStimPill, 0.22f),
                stimulantBlisterBack = CreatePrintedMat(urpLit, "M_Print_Cartela_Estimulante_D3", texStimBlister, 0.68f, metallic: 0.72f),
                antacidBox = CreatePrintedMat(urpLit, "M_Print_Caixa_Antiacido", texAntacid, 0.22f),
                cassetteLabel = CreatePrintedMat(urpLit, "M_Print_Fita_Cassete", texCassette, 0.18f),
                vhsSpine0 = CreatePrintedMat(urpLit, "M_Print_Lombada_VHS_0", texVhs0, 0.18f),
                vhsSpine1 = CreatePrintedMat(urpLit, "M_Print_Lombada_VHS_1", texVhs1, 0.18f),
                vhsSpine2 = CreatePrintedMat(urpLit, "M_Print_Lombada_VHS_2", texVhs2, 0.18f),
                aaBatteryWrap = CreatePrintedMat(urpLit, "M_Print_Pilha_AA", texBat, 0.62f, metallic: 0.45f),
                detergentLabel = CreatePrintedMat(urpLit, "M_Print_Rotulo_Detergente", texDet, 0.35f),
                bleachLabel = CreatePrintedMat(urpLit, "M_Print_Rotulo_AguaSanitaria", texBleach, 0.28f),
                coffeeTinLabel = CreatePrintedMat(urpLit, "M_Print_Rotulo_Lata_Cafe", texCoffee, 0.48f, metallic: 0.35f),
                sugarJarLabel = CreatePrintedMat(urpLit, "M_Print_Rotulo_Pote_Acucar", texSugar, 0.30f),
                matchboxCover = CreatePrintedMat(urpLit, "M_Print_Caixa_Fosforos", texMatches, 0.16f),
                shampooLabel = CreatePrintedMat(urpLit, "M_Print_Rotulo_Shampoo", texShampoo, 0.38f),
                clockDigit2 = CreateEmissiveClockMat(urpLit, "M_Print_Clock_Digit_2", texClk2),
                clockDigit3 = CreateEmissiveClockMat(urpLit, "M_Print_Clock_Digit_3", texClk3),
                clockDigit4 = CreateEmissiveClockMat(urpLit, "M_Print_Clock_Digit_4", texClk4),
                clockDigit0 = CreateEmissiveClockMat(urpLit, "M_Print_Clock_Digit_0", texClk0),
                clockColon = CreateEmissiveClockMat(urpLit, "M_Print_Clock_Colon", texClkCol),
                analogClockDial = CreatePrintedMat(urpLit, "M_Print_Mostrador_Relogio", texAnalogDial, 0.22f),
                memoTavares = CreatePrintedMat(urpLit, "M_Print_Memorando_Tavares", texMemo, 0.10f),
                agendaMargaret = CreatePrintedMat(urpLit, "M_Print_Agenda_Margaret", texAgenda, 0.10f),
                noteMargaret = CreatePrintedMat(urpLit, "M_Print_Bilhete_Margaret", texNote, 0.10f),
                fridgeList = CreatePrintedMat(urpLit, "M_Print_Lista_Geladeira", texList, 0.12f),
                fridgeBillNotice = CreatePrintedMat(urpLit, "M_Print_Aviso_Conta", texBill, 0.12f),
                newspaper1994 = CreatePrintedMat(urpLit, "M_Print_Jornal_Baixada", texNews, 0.08f),
                mailEnvelope = CreatePrintedMat(urpLit, "M_Print_Envelope", texEnv, 0.12f),
                childDrawingHouse = CreatePrintedMat(urpLit, "M_Print_Desenho_Casa", texHouse, 0.08f),
                childDrawingFamily = CreatePrintedMat(urpLit, "M_Print_Desenho_Familia", texFamilyDraw, 0.08f),
                frameFamily1 = CreatePrintedMat(urpLit, "M_Art_Quadro_Familia_1", texFam1, 0.24f),
                frameFamily2 = CreatePrintedMat(urpLit, "M_Art_Quadro_Familia_2", texFam2, 0.24f),
                frameCorrupted1 = CreatePrintedMat(urpLit, "M_Art_Quadro_Distorcido_1", texDist1, 0.30f),
                frameCorrupted2 = CreatePrintedMat(urpLit, "M_Art_Quadro_Distorcido_2", texDist2, 0.30f),
                frameMargaretNormal = CreatePrintedMat(urpLit, "M_Art_Quadro_Margaret_Normal", texMargNorm, 0.24f),
                frameMargaretCorrupted = CreatePrintedMat(urpLit, "M_Art_Quadro_Margaret_Corrupted", texMargCorr, 0.30f),
                clearGlass = CreateTransparentGlassMat(urpLit, "M_Clear_Glass", new Color(0.84f, 0.90f, 0.92f, 0.22f), smoothness: 0.90f, metallic: 0.05f),
                frostedGlass = CreateTransparentGlassMat(urpLit, "M_Frosted_Glass", new Color(0.86f, 0.89f, 0.90f, 0.45f), smoothness: 0.55f, metallic: 0.0f),
                labelCylinderSleeveMesh = GetOrCreateLabelCylinderSleeveMesh()
            };
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Textures"))
                AssetDatabase.CreateFolder("Assets", "Textures");
            if (!AssetDatabase.IsValidFolder("Assets/Textures/Generated"))
                AssetDatabase.CreateFolder("Assets/Textures", "Generated");
            if (!AssetDatabase.IsValidFolder(TexturesFolder))
                AssetDatabase.CreateFolder("Assets/Textures/Generated", "Details");
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
                AssetDatabase.CreateFolder("Assets/Materials", "Greybox");
            if (!AssetDatabase.IsValidFolder(DetailMaterialsFolder))
                AssetDatabase.CreateFolder(MaterialsFolder, "Details");
            if (!AssetDatabase.IsValidFolder("Assets/Meshes"))
                AssetDatabase.CreateFolder("Assets", "Meshes");
            if (!AssetDatabase.IsValidFolder(MeshesFolder))
                AssetDatabase.CreateFolder("Assets/Meshes", "Greybox");
        }

        private static Texture2D SavePng(string fileName, PixelCanvas canvas)
        {
            string assetPath = $"{TexturesFolder}/{fileName}.png";
            Texture2D tex = canvas.ToTexture2D(fileName);
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string fullPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
            File.WriteAllBytes(fullPath, png);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; dirty = true; }
                if (!importer.sRGBTexture) { importer.sRGBTexture = true; dirty = true; }
                if (importer.alphaSource != TextureImporterAlphaSource.None) { importer.alphaSource = TextureImporterAlphaSource.None; dirty = true; }
                if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; dirty = true; }
                if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
                if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
                if (importer.maxTextureSize != 1024) { importer.maxTextureSize = 1024; dirty = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
                if (dirty) importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Material CreatePrintedMat(Shader shader, string matName, Texture2D tex, float smoothness, float metallic = 0f)
        {
            string path = $"{DetailMaterialsFolder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = matName };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.shader = shader;
            mat.DisableKeyword("_NORMALMAP");
            mat.DisableKeyword("_METALLICSPECGLOSSMAP");
            mat.DisableKeyword("_OCCLUSIONMAP");
            mat.DisableKeyword("_EMISSION");
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);
            mat.renderQueue = -1;
            mat.SetOverrideTag("RenderType", "Opaque");

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            else mat.color = Color.white;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", null);
            if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", null);
            if (mat.HasProperty("_OcclusionMap")) mat.SetTexture("_OcclusionMap", null);
            if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", null);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            mat.enableInstancing = true;

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateEmissiveClockMat(Shader shader, string matName, Texture2D digitTex)
        {
            string path = $"{DetailMaterialsFolder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = matName };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.shader = shader;
            mat.DisableKeyword("_NORMALMAP");
            mat.DisableKeyword("_METALLICSPECGLOSSMAP");
            mat.DisableKeyword("_OCCLUSIONMAP");
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            Color ledRed = new Color(0.86f, 0.48f, 0.48f, 1f);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", ledRed);
            else mat.color = ledRed;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", digitTex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", digitTex);
            if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", digitTex);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", ledRed * 1.25f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            mat.enableInstancing = true;

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateTransparentGlassMat(Shader shader, string matName, Color tint, float smoothness, float metallic)
        {
            string path = $"{MaterialsFolder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = matName };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.shader = shader;
            mat.DisableKeyword("_NORMALMAP");
            mat.DisableKeyword("_METALLICSPECGLOSSMAP");
            mat.DisableKeyword("_OCCLUSIONMAP");
            mat.DisableKeyword("_EMISSION");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;

            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", null);
            if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", null);
            if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", null);
            if (mat.HasProperty("_OcclusionMap")) mat.SetTexture("_OcclusionMap", null);
            if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", null);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            else mat.color = tint;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            mat.enableInstancing = true;

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Mesh GetOrCreateLabelCylinderSleeveMesh()
        {
            const int sides = 20;
            float r = 0.5f;
            float yBot = -0.5f;
            float yTop = 0.5f;

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // Barrel side vertices: +u left-to-right when viewed from outside, +v bottom-to-top
            int sideStart = 0;
            for (int i = 0; i <= sides; i++)
            {
                float u = (float)i / sides;
                float theta = u * Mathf.PI * 2f;
                float x = r * Mathf.Sin(theta);
                float z = -r * Mathf.Cos(theta);

                verts.Add(new Vector3(x, yBot, z));
                uvs.Add(new Vector2(u, 0f));

                verts.Add(new Vector3(x, yTop, z));
                uvs.Add(new Vector2(u, 1f));
            }

            for (int i = 0; i < sides; i++)
            {
                int b0 = sideStart + i * 2;
                int t0 = b0 + 1;
                int b1 = b0 + 2;
                int t1 = b0 + 3;

                tris.Add(b0); tris.Add(t0); tris.Add(t1);
                tris.Add(b0); tris.Add(t1); tris.Add(b1);
            }

            // Solid watertight top and bottom caps mapped to margin pixel (0.02, 0.02)
            Vector2 marginUv = new Vector2(0.02f, 0.02f);
            int topCenter = verts.Count;
            verts.Add(new Vector3(0f, yTop, 0f));
            uvs.Add(marginUv);
            int topRingStart = verts.Count;
            for (int i = 0; i <= sides; i++)
            {
                float u = (float)i / sides;
                float theta = u * Mathf.PI * 2f;
                verts.Add(new Vector3(r * Mathf.Sin(theta), yTop, -r * Mathf.Cos(theta)));
                uvs.Add(marginUv);
            }
            for (int i = 0; i < sides; i++)
            {
                tris.Add(topCenter);
                tris.Add(topRingStart + i + 1);
                tris.Add(topRingStart + i);
            }

            int botCenter = verts.Count;
            verts.Add(new Vector3(0f, yBot, 0f));
            uvs.Add(marginUv);
            int botRingStart = verts.Count;
            for (int i = 0; i <= sides; i++)
            {
                float u = (float)i / sides;
                float theta = u * Mathf.PI * 2f;
                verts.Add(new Vector3(r * Mathf.Sin(theta), yBot, -r * Mathf.Cos(theta)));
                uvs.Add(marginUv);
            }
            for (int i = 0; i < sides; i++)
            {
                tris.Add(botCenter);
                tris.Add(botRingStart + i);
                tris.Add(botRingStart + i + 1);
            }

            Mesh generated = new Mesh { name = "Mesh_LabelCylinder_Sleeve" };
            generated.SetVertices(verts);
            generated.SetUVs(0, uvs);
            generated.SetTriangles(tris, 0);
            generated.RecalculateNormals();
            generated.RecalculateBounds();

            string path = $"{MeshesFolder}/Mesh_LabelCylinder_Sleeve.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                existing.vertices = generated.vertices;
                existing.triangles = generated.triangles;
                existing.uv = generated.uv;
                existing.normals = generated.normals;
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(generated);
                return existing;
            }

            AssetDatabase.CreateAsset(generated, path);
            EditorUtility.SetDirty(generated);
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        // =====================================================================
        // ESCOPO 1: CALENDÁRIOS OUTUBRO 1995 & SETEMBRO 1994
        // =====================================================================

        private static PixelCanvas BuildCalendarOct1995()
        {
            var c = new PixelCanvas(512, 680, PaperCream);
            c.DrawRectOutline(0, 0, 512, 680, 4, InkSoft);
            c.FillRect(16, 16, 480, 152, RedMuted90s);
            c.DrawCenteredText(256, 52, "OUTUBRO", 6, PaperWhiteCrisp);
            c.DrawCenteredText(256, 118, "1995", 5, PaperCream);

            string[] dow = { "D", "S", "T", "Q", "Q", "S", "S" };
            int gx = 28;
            int gy = 186;
            int cellW = 65;
            int cellH = 82;

            c.FillRect(gx, gy, cellW * 7, 38, InkDark);
            for (int col = 0; col < 7; col++)
            {
                Color32 hdrCol = col == 0 ? ClampCol(210, 146, 146) : PaperWhiteCrisp;
                c.DrawCenteredText(gx + col * cellW + cellW / 2, gy + 9, dow[col], 3, hdrCol);
            }

            int gridTop = gy + 44;
            for (int r = 0; r <= 5; r++)
                c.FillRect(gx, gridTop + r * cellH, cellW * 7, 2, InkSoft);
            for (int col = 0; col <= 7; col++)
                c.FillRect(gx + col * cellW, gridTop, 2, cellH * 5, InkSoft);

            // Outubro de 1995 começou no Domingo (col 0), 31 dias
            for (int day = 1; day <= 31; day++)
            {
                int idx = day - 1;
                int row = idx / 7;
                int col = idx % 7;
                int cx = gx + col * cellW;
                int cy = gridTop + row * cellH;

                Color32 numCol = (col == 0) ? RedMuted90s : InkBlack;
                c.DrawCenteredText(cx + cellW / 2, cy + 26, day.ToString(), 4, numCol);

                if (day <= 28)
                {
                    c.DrawLine(cx + 12, cy + 14, cx + cellW - 12, cy + cellH - 14, RedMuted90s, 4);
                    c.DrawLine(cx + cellW - 12, cy + 14, cx + 12, cy + cellH - 14, RedMuted90s, 4);
                }
                else if (day == 29)
                {
                    c.DrawCircleOutline(cx + cellW / 2, cy + cellH / 2, 26, 4, RedMuted90s);
                }
            }

            c.ApplyDirtyPaperPatina(seed: 1995, grainAmount: 0.045f, edgeGrime: 0.14f, foldYNorm: -1f);
            return c;
        }

        private static PixelCanvas BuildCalendarSep1994()
        {
            // A metade superior (y: 0..380) fica coberta atrás de Folha_Base;
            // a metade inferior (y: 392..740) fica visível logo abaixo na parede.
            var c = new PixelCanvas(512, 760, PaperAged);
            c.DrawRectOutline(0, 0, 512, 760, 4, InkSoft);

            // Topo escondido neutro com pequeno detalhe de papel grampeado
            c.FillRect(20, 20, 472, 340, ClampCol(204, 198, 178));
            c.FillRect(20, 382, 472, 4, InkSoft);

            // Cabeçalho na zona visível inferior
            c.FillRect(20, 396, 472, 54, InkDark);
            c.DrawCenteredText(256, 412, "SETEMBRO / 1994", 4, PaperWhiteCrisp);

            string[] dow = { "D", "S", "T", "Q", "Q", "S", "S" };
            int gx = 32;
            int gy = 458;
            int cellW = 64;
            int cellH = 46;

            for (int col = 0; col < 7; col++)
            {
                c.DrawCenteredText(gx + col * cellW + cellW / 2, gy + 4, dow[col], 2, col == 0 ? RedMuted90s : InkDark);
            }

            int gridTop = gy + 26;
            for (int r = 0; r <= 5; r++)
                c.FillRect(gx, gridTop + r * cellH, cellW * 7, 2, InkSoft);
            for (int col = 0; col <= 7; col++)
                c.FillRect(gx + col * cellW, gridTop, 2, cellH * 5, InkSoft);

            // Setembro de 1994 começou numa Quinta-feira (col 4), 30 dias -> 09=Sex (col 5), 10=Sáb (col 6)
            for (int day = 1; day <= 30; day++)
            {
                int idx = day - 1 + 4;
                int row = idx / 7;
                int col = idx % 7;
                int cx = gx + col * cellW;
                int cy = gridTop + row * cellH;

                Color32 numCol = (col == 0) ? RedMuted90s : InkBlack;
                string txt = day < 10 ? "0" + day : day.ToString();
                c.DrawCenteredText(cx + cellW / 2, cy + 12, txt, 3, numCol);

                if (day <= 9)
                {
                    int thick = (day == 9) ? 4 : 3;
                    c.DrawLine(cx + 10, cy + 8, cx + cellW - 10, cy + cellH - 8, RedMuted90s, thick);
                    c.DrawLine(cx + cellW - 10, cy + 8, cx + 10, cy + cellH - 8, RedMuted90s, thick);
                }
            }

            c.DrawCenteredText(256, 728, "SANTOS - SP  1994", 2, InkSoft);
            c.ApplyDirtyPaperPatina(seed: 1994, grainAmount: 0.060f, edgeGrime: 0.22f, foldYNorm: 0.50f);
            return c;
        }

        // =====================================================================
        // ESCOPO 2: RÓTULOS, EMBALAGENS E VISOR DO DESPERTADOR
        // =====================================================================

        private static PixelCanvas BuildPillBottleLabel()
        {
            var c = new PixelCanvas(512, 256, PaperWhiteCrisp);
            for (int panel = 0; panel < 2; panel++)
            {
                int ox = panel * 256;
                c.DrawRectOutline(ox + 8, 12, 240, 232, 3, InkSoft);
                c.FillRect(ox + 16, 22, 224, 24, BlueMuted90s);
                c.DrawCenteredText(ox + 128, 27, "FARMACIA CENTRAL", 2, PaperWhiteCrisp);
                c.DrawText(ox + 20, 58, "CLONAZEPAM 2MG", 2, InkBlack);
                c.DrawText(ox + 20, 82, "30 COMPRIMIDOS", 2, InkSoft);
                c.DrawText(ox + 20, 104, "USO ADULTO - RECEITA B1", 1, InkSoft);
                c.FillRect(ox + 20, 120, 200, 3, InkSoft);
                c.FillRect(ox + 20, 130, 165, 3, InkSoft);
                // Faixa horizontal TARJA PRETA
                c.FillRect(ox + 10, 152, 236, 72, InkBlack);
                c.DrawCenteredText(ox + 128, 168, "TARJA PRETA", 3, PaperWhiteCrisp);
                c.DrawCenteredText(ox + 128, 200, "VENDA SOB PRESCRICAO", 1, PaperCream);
            }
            return c;
        }

        private static PixelCanvas BuildBlisterBack()
        {
            var c = new PixelCanvas(256, 160, FoilSilver);
            c.DrawRectOutline(4, 4, 248, 152, 2, InkSoft);
            for (int r = 0; r < 3; r++)
            {
                c.DrawText(16, 20 + r * 44, "CLONAZEPAM 2MG", 2, InkDark);
                c.DrawText(16, 40 + r * 44, "LOTE 094/B   VAL 96", 1, InkSoft);
            }
            return c;
        }

        private static PixelCanvas BuildStimulantBottleLabel()
        {
            var c = new PixelCanvas(512, 256, PaperCream);
            for (int panel = 0; panel < 2; panel++)
            {
                int ox = panel * 256;
                c.DrawRectOutline(ox + 8, 12, 240, 232, 3, InkSoft);
                c.FillRect(ox + 16, 20, 224, 26, RedMuted90s);
                c.DrawCenteredText(ox + 128, 26, "LAB. MANIPULACAO", 2, PaperWhiteCrisp);
                c.DrawText(ox + 18, 56, "FENPROPOREX 25MG", 2, InkBlack);
                c.DrawText(ox + 18, 78, "+ CAFEINA 200MG", 2, InkBlack);
                c.DrawText(ox + 18, 102, "30 CAPSULAS - FORMULA", 1, InkSoft);
                c.DrawText(ox + 18, 118, "PAC: CIAN NABS  N 4182", 1, InkDark);
                c.FillRect(ox + 18, 134, 210, 3, InkSoft);
                c.FillRect(ox + 10, 152, 236, 72, InkBlack);
                c.DrawCenteredText(ox + 128, 166, "USO RESTRITO", 3, PaperWhiteCrisp);
                c.DrawCenteredText(ox + 128, 200, "NAO EXCEDER 1 CAPS/DIA", 1, PaperCream);
            }
            return c;
        }

        private static PixelCanvas BuildStimulantBlisterBack()
        {
            var c = new PixelCanvas(256, 160, FoilSilver);
            c.DrawRectOutline(4, 4, 248, 152, 2, InkSoft);
            for (int r = 0; r < 3; r++)
            {
                c.DrawText(12, 20 + r * 44, "FENPROPOREX 25MG", 2, InkDark);
                c.DrawText(14, 40 + r * 44, "CAFEINA 200MG  L.418", 1, InkSoft);
            }
            return c;
        }

        private static PixelCanvas BuildAntacidBoxCover()
        {
            var c = new PixelCanvas(256, 200, PaperWhiteCrisp);
            c.FillRect(0, 0, 256, 54, BlueMuted90s);
            c.DrawCenteredText(128, 18, "ANTIACIDO", 3, PaperWhiteCrisp);
            c.DrawCenteredText(128, 84, "ALIVIO GASTRICO", 2, InkBlack);
            c.DrawCenteredText(128, 118, "10 COMPRIMIDOS", 2, InkSoft);
            c.FillRect(24, 154, 208, 22, GreenMuted90s);
            c.DrawCenteredText(128, 159, "USO ORAL", 2, PaperWhiteCrisp);
            return c;
        }

        private static PixelCanvas BuildCassetteLabel()
        {
            var c = new PixelCanvas(512, 256, PaperCream);
            c.DrawRectOutline(6, 6, 500, 244, 4, InkSoft);
            c.FillRect(12, 14, 488, 42, RedMuted90s);
            c.DrawText(26, 25, "MICROCASSETE  C-60", 3, PaperWhiteCrisp);
            c.DrawText(28, 74, "SEC. - SET/94", 5, InkBlack);
            c.FillRect(24, 120, 464, 3, RuleLineBlue);
            c.DrawText(28, 134, "GRAVACAO AUTOMATICA", 3, InkDark);
            c.FillRect(24, 166, 464, 3, RuleLineBlue);
            // Recorte escuro central para combinar com janela e carretéis abaixo
            c.FillRect(84, 182, 344, 58, InkBlack);
            c.DrawCenteredText(256, 204, "LADO A   REC 10/09", 2, PaperAged);
            return c;
        }

        private static PixelCanvas BuildVhsSpine(string title, Color32 stripeCol)
        {
            var c = new PixelCanvas(512, 96, PaperCream);
            c.DrawRectOutline(4, 4, 504, 88, 3, InkSoft);
            c.FillRect(10, 10, 48, 76, stripeCol);
            c.DrawCenteredText(34, 38, "VHS", 2, PaperWhiteCrisp);
            c.DrawText(74, 30, title, 4, InkBlack);
            return c;
        }

        private static PixelCanvas BuildAABatteryTexture()
        {
            var c = new PixelCanvas(512, 256, InkDark);
            // Em torno do cilindro, faixa cobre no terço superior/direito (+polo) e escuro no corpo
            for (int panel = 0; panel < 2; panel++)
            {
                int ox = panel * 256;
                c.FillRect(ox + 8, 14, 240, 68, CopperMuted);
                c.FillRect(ox + 8, 82, 240, 8, FoilSilver);
                c.DrawCenteredText(ox + 128, 32, "+", 5, InkBlack);
                c.DrawCenteredText(ox + 128, 114, "1.5V AA", 4, PaperWhiteCrisp);
                c.DrawCenteredText(ox + 128, 164, "ALCALINA", 2, CopperMuted);
                c.DrawCenteredText(ox + 128, 204, "-", 5, FoilSilver);
            }
            return c;
        }

        private static PixelCanvas BuildCylinderLabel(string title, string sub, Color32 bandColor, Color32 bgColor, Color32 textColor)
        {
            var c = new PixelCanvas(512, 256, bgColor);
            for (int panel = 0; panel < 2; panel++)
            {
                int ox = panel * 256;
                c.DrawRectOutline(ox + 10, 18, 236, 220, 3, InkSoft);
                c.FillRect(ox + 14, 26, 228, 66, bandColor);
                c.DrawCenteredText(ox + 128, 48, title, 2, PaperWhiteCrisp);
                c.DrawCenteredText(ox + 128, 122, sub, 2, textColor);
                c.FillRect(ox + 32, 168, 192, 4, bandColor);
                c.DrawCenteredText(ox + 128, 192, "IND. BRASILEIRA", 1, InkSoft);
            }
            return c;
        }

        private static PixelCanvas BuildMatchboxTexture()
        {
            var c = new PixelCanvas(256, 192, RedMuted90s);
            c.DrawRectOutline(8, 8, 240, 176, 4, PaperCream);
            c.FillRect(22, 24, 212, 144, PaperCream);
            c.FillRect(22, 24, 212, 44, InkBlack);
            c.DrawCenteredText(128, 36, "FOSFOROS", 3, PaperWhiteCrisp);
            // Ícone geométrico simples de chama
            c.FillCircle(128, 108, 18, YellowMuted90s);
            c.FillCircle(128, 112, 10, RedMuted90s);
            c.DrawCenteredText(128, 142, "40 UNIDADES", 2, InkBlack);
            return c;
        }

        private static PixelCanvas BuildSevenSegmentTexture(char ch)
        {
            int w = (ch == ':') ? 48 : 96;
            int h = 160;
            var c = new PixelCanvas(w, h, new Color32(10, 10, 12, 255));
            Color32 on = new Color32(240, 236, 236, 255);

            if (ch == ':')
            {
                c.FillRect(16, 46, 16, 16, on);
                c.FillRect(16, 98, 16, 16, on);
                return c;
            }

            // Segmentos A(topo), B(sup dir), C(inf dir), D(base), E(inf esq), F(sup esq), G(meio)
            bool a = ch == '2' || ch == '3' || ch == '0';
            bool b = ch == '2' || ch == '3' || ch == '4' || ch == '0';
            bool segC = ch == '3' || ch == '4' || ch == '0';
            bool d = ch == '2' || ch == '3' || ch == '0';
            bool e = ch == '2' || ch == '0';
            bool f = ch == '4' || ch == '0';
            bool g = ch == '2' || ch == '3' || ch == '4';

            int pad = 14;
            int thick = 14;
            int midY = h / 2 - thick / 2;

            if (a) c.FillRect(pad + 6, pad, w - 2 * pad - 12, thick, on);
            if (g) c.FillRect(pad + 6, midY, w - 2 * pad - 12, thick, on);
            if (d) c.FillRect(pad + 6, h - pad - thick, w - 2 * pad - 12, thick, on);
            if (f) c.FillRect(pad, pad + 6, thick, midY - pad - 4, on);
            if (b) c.FillRect(w - pad - thick, pad + 6, thick, midY - pad - 4, on);
            if (e) c.FillRect(pad, midY + 8, thick, h - pad - midY - 14, on);
            if (segC) c.FillRect(w - pad - thick, midY + 8, thick, h - pad - midY - 14, on);

            return c;
        }

        private static PixelCanvas BuildAnalogClockDialTexture()
        {
            const int size = 512;
            const int cx = size / 2;
            const int cy = size / 2;
            var c = new PixelCanvas(size, size, PaperCream);

            c.DrawCircleOutline(cx, cy, 240, 6, InkDark);
            c.DrawCircleOutline(cx, cy, 224, 2, InkSoft);
            c.DrawCircleOutline(cx, cy, 134, 2, RuleLineBlue);

            for (int m = 0; m < 60; m++)
            {
                float rad = m * 6f * Mathf.Deg2Rad;
                float dx = Mathf.Sin(rad);
                float dy = -Mathf.Cos(rad);
                bool isHour = (m % 5 == 0);
                int rOuter = 216;
                int rInner = isHour ? 188 : 204;
                int thick = isHour ? 6 : 2;
                Color32 tickCol = isHour ? InkBlack : InkSoft;

                c.DrawLine(
                    cx + Mathf.RoundToInt(dx * rInner),
                    cy + Mathf.RoundToInt(dy * rInner),
                    cx + Mathf.RoundToInt(dx * rOuter),
                    cy + Mathf.RoundToInt(dy * rOuter),
                    tickCol,
                    thick);
            }

            for (int h = 1; h <= 12; h++)
            {
                float rad = h * 30f * Mathf.Deg2Rad;
                int nx = cx + Mathf.RoundToInt(Mathf.Sin(rad) * 158f);
                int ny = cy + Mathf.RoundToInt(-Mathf.Cos(rad) * 158f);
                c.DrawCenteredText(nx, ny - 10, h.ToString(), 3, InkBlack);
            }

            c.DrawCenteredText(cx, cy - 72, "QUARTZ", 2, InkSoft);
            c.DrawCenteredText(cx, cy + 56, "1994", 2, InkSoft);
            c.FillCircle(cx, cy, 8, InkDark);
            return c;
        }

        // =====================================================================
        // ESCOPO 3: PAPÉIS, BILHETES, JORNAL, ENVELOPES E DESENHOS INFANTIS
        // =====================================================================

        private static PixelCanvas BuildCorporateMemoTexture()
        {
            var c = new PixelCanvas(512, 680, PaperWhiteCrisp);
            c.DrawRectOutline(16, 16, 480, 648, 2, InkSoft);
            c.FillRect(28, 28, 456, 56, InkDark);
            c.DrawCenteredText(256, 46, "COMUNICACAO INTERNA 048/95", 2, PaperWhiteCrisp);

            c.DrawText(34, 106, "PARA: CIAN NABS (CONTABIL)", 2, InkBlack);
            c.DrawText(34, 134, "DE:   A. TAVARES (GERENCIA)", 2, InkBlack);
            c.FillRect(34, 166, 444, 3, InkBlack);

            int[] widths = { 420, 405, 430, 320, 0, 425, 410, 395, 290, 0, 420, 370, 240 };
            int ly = 194;
            for (int i = 0; i < widths.Length; i++)
            {
                if (widths[i] > 0)
                    c.FillRect(34, ly, widths[i], 6, InkSoft);
                ly += 24;
            }

            // Carimbo discreto vermelho no canto inferior direito
            c.DrawRectOutline(300, 540, 166, 56, 4, RedMuted90s);
            c.DrawCenteredText(383, 560, "URGENTE", 3, RedMuted90s);
            c.DrawText(34, 616, "SEGUNDA 08H00 NA MINHA MESA.", 2, BlueMuted90s);
            c.ApplyDirtyPaperPatina(seed: 8301, grainAmount: 0.040f, edgeGrime: 0.14f, foldYNorm: 0.48f);
            return c;
        }

        private static PixelCanvas BuildAgendaTexture()
        {
            var c = new PixelCanvas(512, 680, PaperCream);
            c.FillRect(34, 24, 444, 62, BlueMuted90s);
            c.DrawCenteredText(256, 42, "SAB  10/SET", 4, PaperWhiteCrisp);

            for (int y = 114; y < 600; y += 38)
                c.FillRect(34, y, 444, 2, RuleLineBlue);
            c.FillRect(64, 96, 2, 520, RedMuted90s);

            string[] items =
            {
                "- MENINAS NA DONA LUCIA 10H",
                "- TERNO CINZA LAVANDERIA",
                "- FARMACIA: XAROPE",
                "- LIGAR P/ MAE",
                "",
                "CONVERSAR COM O CIAN HOJE."
            };
            for (int i = 0; i < items.Length; i++)
            {
                if (!string.IsNullOrEmpty(items[i]))
                    c.DrawText(76, 122 + i * 38, items[i], 2, InkDark);
            }
            for (int k = 0; k < 3; k++)
                c.FillRect(76, 390 + k * 38, 340 - k * 55, 5, InkSoft);

            // Borda inferior simulando folha rasgada com risco forte
            c.DrawLine(34, 626, 476, 614, InkBlack, 6);
            c.FillRect(0, 650, 512, 30, PaperAged);
            c.ApplyDirtyPaperPatina(seed: 8302, grainAmount: 0.055f, edgeGrime: 0.20f, foldYNorm: -1f);
            return c;
        }

        private static PixelCanvas BuildMargaretNoteTexture()
        {
            var c = new PixelCanvas(512, 680, PaperCream);
            for (int y = 96; y < 620; y += 40)
                c.FillRect(34, y, 444, 2, RuleLineBlue);

            c.DrawText(42, 64, "CIAN,", 3, InkDark);
            int[] widths = { 410, 390, 420, 310, 0, 405, 420, 370, 260 };
            for (int i = 0; i < widths.Length; i++)
            {
                if (widths[i] > 0)
                    c.FillRect(42, 114 + i * 40, widths[i], 5, InkDark);
            }
            c.DrawText(290, 540, "MARGARET", 3, BlueMuted90s);
            c.ApplyDirtyPaperPatina(seed: 8303, grainAmount: 0.050f, edgeGrime: 0.18f, foldYNorm: 0.50f);
            return c;
        }

        private static PixelCanvas BuildFridgeListTexture()
        {
            var c = new PixelCanvas(384, 512, PaperWhiteCrisp);
            c.FillRect(20, 20, 344, 48, YellowMuted90s);
            c.DrawCenteredText(192, 34, "LISTA DA SEMANA", 2, InkBlack);
            for (int y = 98; y < 470; y += 44)
                c.FillRect(24, y, 336, 2, RuleLineBlue);

            string[] items = { "- LEITE", "- MACA", "- SABONETE INF.", "- PAO DE FORMA", "- CAFE" };
            for (int i = 0; i < items.Length; i++)
                c.DrawText(32, 110 + i * 44, items[i], 2, InkDark);
            c.ApplyDirtyPaperPatina(seed: 8304, grainAmount: 0.045f, edgeGrime: 0.15f, foldYNorm: -1f);
            return c;
        }

        private static PixelCanvas BuildFridgeBillTexture()
        {
            var c = new PixelCanvas(384, 480, PaperAged);
            c.DrawRectOutline(14, 14, 356, 452, 3, InkDark);
            c.FillRect(24, 24, 336, 48, InkDark);
            c.DrawCenteredText(192, 40, "AVISO DE DEBITO", 2, PaperWhiteCrisp);
            c.DrawText(32, 96, "CONTA LUZ / AGUA", 2, InkBlack);
            for (int i = 0; i < 5; i++)
                c.FillRect(32, 142 + i * 34, 300 - i * 25, 6, InkSoft);
            c.DrawRectOutline(100, 348, 184, 54, 4, RedMuted90s);
            c.DrawCenteredText(192, 366, "VENCIDA", 3, RedMuted90s);
            c.ApplyDirtyPaperPatina(seed: 8305, grainAmount: 0.055f, edgeGrime: 0.22f, foldYNorm: 0.52f);
            return c;
        }

        private static PixelCanvas BuildNewspaperTexture()
        {
            Color32 paperBase = ClampCol(188, 183, 169);
            Color32 paperDark = ClampCol(156, 150, 137);
            Color32 inkDeep = ClampCol(26, 26, 28);
            Color32 inkMid = ClampCol(56, 56, 58);
            Color32 inkMuted = ClampCol(96, 94, 90);
            Color32 redPrint = ClampCol(142, 76, 74);

            var c = new PixelCanvas(512, 640, paperBase);

            // Uneven aged newsprint border and inner masthead rules
            c.DrawRectOutline(10, 10, 492, 620, 2, inkMid);
            c.FillRect(18, 18, 476, 5, inkDeep);
            c.FillRect(18, 26, 476, 2, inkDeep);

            // Classic newspaper masthead
            c.DrawCenteredText(256, 36, "JORNAL DA BAIXADA", 4, inkDeep);
            c.FillRect(18, 74, 476, 2, inkDeep);
            c.DrawText(22, 82, "DOM, 11 DE SETEMBRO DE 1994", 2, inkMid);
            c.DrawText(426, 82, "N.614", 2, inkMid);
            c.FillRect(18, 102, 476, 3, inkDeep);

            // Main headline (high contrast newsprint typography)
            c.DrawCenteredText(256, 114, "TRAGEDIA NA RUA", 4, inkDeep);
            c.DrawCenteredText(256, 150, "DAS ACACIAS", 4, inkDeep);
            c.FillRect(26, 186, 460, 2, inkMid);
            c.DrawCenteredText(256, 194, "POLICIA ISOLOU CASA NA NOITE DE SABADO (10/09)", 1, redPrint);
            c.FillRect(26, 208, 460, 2, inkMid);

            // Halftone low-poly press photograph of the sobrado house behind police tape (upper-left box)
            int px = 22;
            int py = 220;
            int pw = 258;
            int ph = 176;
            c.FillRect(px - 2, py - 2, pw + 4, ph + 4, inkDeep);
            RenderHalftoneSobradoPhoto(c, px, py, pw, ph);
            c.DrawText(px + 4, py + ph + 6, "FACHADA ISOLADA - R. ACACIAS", 1, inkMid);

            // Right column beside photo: dense realistic broken newsprint word-lines
            DrawNewsColumnLines(c, 294, 224, 196, 16, seed: 9401, inkMid, inkDeep);
            c.FillRect(286, 220, 2, 392, inkMuted);

            // Lower left column under photo
            c.FillRect(22, 412, 256, 2, inkMuted);
            c.DrawText(24, 420, "PORTA TRANCADA POR FORA", 1, inkDeep);
            DrawNewsColumnLines(c, 24, 436, 250, 7, seed: 9402, inkMid, inkDeep);

            // Ambiguous narrative pull-line + dark damp smudge/tear obscuring outcome
            c.FillRect(22, 536, 256, 2, inkDeep);
            c.DrawText(24, 544, "LEVADO NA MADRUGADA", 1, inkDeep);
            c.DrawText(24, 558, "PARA...", 1, inkMid);
            DrawNewsColumnLines(c, 24, 574, 230, 3, seed: 9403, inkMuted, inkMid);

            // Lower right column
            c.FillRect(294, 426, 196, 2, inkMuted);
            c.DrawText(294, 434, "CAUSA NAO INFORMADA", 1, inkDeep);
            DrawNewsColumnLines(c, 294, 450, 196, 11, seed: 9404, inkMid, inkDeep);

            // Damp water stain + rubbed smudge over the ambiguous outcome line & torn lower-left edge
            c.FillRadialBlob(192f, 566f, 66f, 34f, ClampCol(118, 108, 92), 0.56f, 1.5f);
            c.FillRadialBlob(122f, 594f, 88f, 36f, paperDark, 0.48f, 1.4f);

            // Subtle torn ragged dark border along the very bottom of the clipping
            Vector2[] tearPoly =
            {
                new Vector2(0, 620), new Vector2(54, 628), new Vector2(102, 616), new Vector2(158, 630),
                new Vector2(218, 618), new Vector2(276, 629), new Vector2(338, 621), new Vector2(408, 631),
                new Vector2(466, 620), new Vector2(512, 626), new Vector2(512, 640), new Vector2(0, 640)
            };
            c.FillPolygon(tearPoly, ClampCol(78, 74, 66), 0.90f);

            // Heavy aged newsprint grain, fold crease, and yellow corner grime
            c.ApplyDirtyPaperPatina(seed: 100994, grainAmount: 0.095f, edgeGrime: 0.30f, foldYNorm: 0.51f);
            return c;
        }

        private static void RenderHalftoneSobradoPhoto(PixelCanvas c, int ox, int oy, int w, int h)
        {
            // Render low-poly night-flash scene into temporary grayscale canvas, then screen with 4x4 Bayer halftone
            var p = new PixelCanvas(w, h, ClampCol(24, 24, 24));

            // Night sky gradient with faint camera-flash halo behind house
            p.FillGradientRect(0, 0, w, h, ClampCol(16, 16, 18), ClampCol(42, 42, 44));
            p.FillRadialBlob(w * 0.48f, h * 0.54f, w * 0.52f, h * 0.48f, ClampCol(96, 94, 90), 0.35f, 1.6f);

            // Neighbor wall shadow on left + street curb & sidewalk at bottom
            p.FillPolygon(new[]
            {
                new Vector2(0, 56), new Vector2(34, 52), new Vector2(34, 146), new Vector2(0, 152)
            }, ClampCol(86, 84, 80));
            p.FillPolygon(new[]
            {
                new Vector2(0, 144), new Vector2(w, 134), new Vector2(w, h), new Vector2(0, h)
            }, ClampCol(132, 128, 122));
            p.FillPolygon(new[]
            {
                new Vector2(0, 160), new Vector2(w, 152), new Vector2(w, h), new Vector2(0, h)
            }, ClampCol(56, 56, 56));

            // Low-poly 2-story Brazilian sobrado: roof, side wall (shadow), front facade (flash-lit)
            p.FillPolygon(new[]
            {
                new Vector2(44, 34), new Vector2(72, 16), new Vector2(186, 36), new Vector2(174, 48), new Vector2(54, 40)
            }, ClampCol(42, 42, 42));
            p.FillPolygon(new[]
            {
                new Vector2(48, 38), new Vector2(78, 24), new Vector2(78, 138), new Vector2(48, 134)
            }, ClampCol(104, 102, 98));
            p.FillPolygon(new[]
            {
                new Vector2(78, 24), new Vector2(176, 40), new Vector2(176, 136), new Vector2(78, 138)
            }, ClampCol(178, 174, 166));
            // Concrete floor slab shadow ledge between 1st & 2nd floor
            p.FillPolygon(new[]
            {
                new Vector2(46, 84), new Vector2(178, 88), new Vector2(178, 94), new Vector2(46, 90)
            }, ClampCol(78, 76, 74));

            // Upper recessed balcony + dark shuttered windows
            p.FillPolygon(new[]
            {
                new Vector2(128, 44), new Vector2(168, 50), new Vector2(168, 86), new Vector2(128, 84)
            }, ClampCol(52, 52, 54));
            p.FillPolygon(new[]
            {
                new Vector2(138, 49), new Vector2(158, 52), new Vector2(158, 85), new Vector2(138, 84)
            }, ClampCol(18, 18, 20));
            // Upper-left window
            p.FillPolygon(new[]
            {
                new Vector2(90, 44), new Vector2(116, 47), new Vector2(116, 74), new Vector2(90, 72)
            }, ClampCol(24, 24, 26));
            // Ground-floor dark windows
            p.FillPolygon(new[]
            {
                new Vector2(90, 98), new Vector2(118, 99), new Vector2(118, 126), new Vector2(90, 126)
            }, ClampCol(22, 22, 24));
            p.FillPolygon(new[]
            {
                new Vector2(134, 99), new Vector2(160, 100), new Vector2(160, 124), new Vector2(134, 124)
            }, ClampCol(24, 24, 26));

            // Front concrete fence pillars + iron gate bars
            p.FillRect(42, 92, 12, 54, ClampCol(148, 144, 138));
            p.FillRect(120, 94, 10, 48, ClampCol(142, 138, 132));
            p.FillRect(184, 96, 10, 44, ClampCol(122, 118, 114));
            p.FillRect(128, 116, 58, 26, ClampCol(126, 122, 116));
            for (int gx = 56; gx < 184; gx += 6)
            {
                p.DrawLine(gx, 96, gx, 142, ClampCol(30, 30, 32), 1);
            }
            p.DrawLine(52, 100, 186, 100, ClampCol(32, 32, 34), 2);
            p.DrawLine(52, 136, 186, 136, ClampCol(32, 32, 34), 2);

            // Diagonal striped police cordon tape across the front gate
            for (int seg = 0; seg < 24; seg++)
            {
                float t0 = seg / 24f;
                float t1 = (seg + 1) / 24f;
                int x0 = Mathf.RoundToInt(Mathf.Lerp(6, 226, t0));
                int y0 = Mathf.RoundToInt(Mathf.Lerp(116, 128, t0));
                int x1 = Mathf.RoundToInt(Mathf.Lerp(6, 226, t1));
                int y1 = Mathf.RoundToInt(Mathf.Lerp(116, 128, t1));
                Color32 tapeCol = (seg % 2 == 0) ? ClampCol(216, 212, 202) : ClampCol(26, 26, 28);
                p.DrawLine(x0, y0, x1, y1, tapeCol, 4);
            }

            // Boxy 1990s patrol car rear silhouette parked at bottom-right curb
            p.FillPolygon(new[]
            {
                new Vector2(188, 132), new Vector2(210, 116), new Vector2(256, 114),
                new Vector2(258, 162), new Vector2(186, 158)
            }, ClampCol(28, 28, 30));
            p.FillPolygon(new[]
            {
                new Vector2(204, 126), new Vector2(218, 118), new Vector2(252, 118), new Vector2(252, 128)
            }, ClampCol(78, 78, 82));
            // Rear flash-reflected license plate & tail-lights
            p.FillRect(204, 138, 22, 9, ClampCol(212, 208, 198));
            p.FillRect(190, 136, 10, 7, ClampCol(156, 152, 146));
            p.FillRect(232, 138, 14, 7, ClampCol(156, 152, 146));

            // Light blotchy flash falloff & newsprint 4x4 Bayer halftone screen
            int[,] bayer4 =
            {
                {  0,  8,  2, 10 },
                { 12,  4, 14,  6 },
                {  3, 11,  1,  9 },
                { 15,  7, 13,  5 }
            };

            Color32 paperInk = ClampCol(182, 178, 166);
            Color32 midInk = ClampCol(104, 102, 96);
            Color32 darkInk = ClampCol(28, 28, 30);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 src = p.GetPixelTopDown(x, y);
                    float lum = (src.r * 0.299f + src.g * 0.587f + src.b * 0.114f) / 255f;
                    float grain = (Hash2D(x, y, 771) - 0.5f) * 0.14f;
                    lum = Mathf.Clamp01(lum + grain);
                    float thresh = (bayer4[y & 3, x & 3] + 0.5f) / 16f;

                    Color32 outCol;
                    if (lum > thresh * 0.92f + 0.08f) outCol = paperInk;
                    else if (lum > thresh * 0.62f) outCol = midInk;
                    else outCol = darkInk;

                    c.SetPixelTopDown(ox + x, oy + y, outCol);
                }
            }
        }

        private static void DrawNewsColumnLines(PixelCanvas c, int startX, int startY, int colW, int rows, int seed, Color32 colA, Color32 colB)
        {
            var rng = new System.Random(seed);
            for (int r = 0; r < rows; r++)
            {
                int y = startY + r * 12;
                int x = startX;
                bool isParaEnd = (r % 5 == 4);
                int maxX = startX + (isParaEnd ? (int)(colW * 0.68f) : colW);
                while (x < maxX - 10)
                {
                    int wordW = rng.Next(12, 34);
                    if (x + wordW > maxX) wordW = maxX - x;
                    Color32 ink = rng.NextDouble() > 0.35 ? colA : colB;
                    c.FillRect(x, y, wordW, 4, ink);
                    x += wordW + rng.Next(4, 9);
                }
            }
        }

        private static PixelCanvas BuildEnvelopeTexture()
        {
            var c = new PixelCanvas(512, 256, PaperCream);
            c.DrawRectOutline(8, 8, 496, 240, 3, InkSoft);
            // Selo no canto superior direito
            c.DrawRectOutline(418, 24, 64, 72, 3, BlueMuted90s);
            c.FillRect(426, 32, 48, 56, BlueMuted90s);
            c.DrawCenteredText(450, 52, "95", 2, PaperWhiteCrisp);

            // Endereço
            c.DrawText(44, 112, "CIAN NABS", 3, InkDark);
            c.DrawText(44, 148, "RUA DAS ACACIAS, 420", 2, InkSoft);
            c.FillRect(44, 180, 210, 4, InkSoft);

            // Carimbo vermelho pequeno
            c.DrawRectOutline(310, 174, 166, 46, 3, RedMuted90s);
            c.DrawCenteredText(393, 189, "VENCIDO", 2, RedMuted90s);
            c.ApplyDirtyPaperPatina(seed: 8306, grainAmount: 0.055f, edgeGrime: 0.20f, foldYNorm: -1f);
            return c;
        }

        private static PixelCanvas BuildChildHouseDrawing()
        {
            Color32 paper = ClampCol(224, 217, 200);
            Color32 roofRed = ClampCol(156, 78, 66);
            Color32 roofDark = ClampCol(118, 56, 48);
            Color32 crayonBlack = ClampCol(46, 43, 40);
            Color32 doorBrown = ClampCol(124, 86, 58);
            Color32 grassGreen = ClampCol(86, 118, 68);
            Color32 grassDark = ClampCol(62, 90, 50);
            Color32 sunOchre = ClampCol(174, 142, 72);
            Color32 skyBlue = ClampCol(98, 120, 142);

            var c = new PixelCanvas(512, 400, paper);

            // Sketchy ochre sun in top-right corner with uneven hand-drawn rays
            c.FillCrayonCircle(414, 82, 34, sunOchre, seed: 7101);
            c.DrawCrayonCircleOutline(414, 82, 35, ClampCol(142, 112, 54), 3.5f, seed: 7102);
            float[] rayAngles = { -0.15f, 0.58f, 1.32f, 2.04f, 2.76f, 3.45f, 4.18f, 4.92f, 5.56f };
            for (int i = 0; i < rayAngles.Length; i++)
            {
                float a = rayAngles[i];
                float rIn = 42f + (i % 2) * 3f;
                float rOut = 64f + ((i * 7) % 11);
                c.DrawCrayonStroke(
                    414f + Mathf.Cos(a) * rIn, 82f + Mathf.Sin(a) * rIn,
                    414f + Mathf.Cos(a) * rOut, 82f + Mathf.Sin(a) * rOut,
                    sunOchre, 4.2f, seed: 7110 + i);
            }

            // Crooked little chimney + faint grey curly smoke puffs
            Vector2[] chimney =
            {
                new Vector2(268, 112), new Vector2(296, 108), new Vector2(298, 162), new Vector2(266, 162)
            };
            c.FillCrayonPolygon(chimney, roofDark, seed: 7120, strokeAngleDeg: 18f);
            c.DrawCrayonPolygonOutline(chimney, crayonBlack, 3.5f, seed: 7121);
            c.DrawCrayonCircleOutline(288, 92, 11, InkSoft, 2.8f, seed: 7122);
            c.DrawCrayonCircleOutline(304, 72, 14, InkSoft, 2.5f, seed: 7123);

            // Slightly crooked child-drawn house body (off-white warm paper inside)
            Vector2[] houseWall =
            {
                new Vector2(138, 198), new Vector2(318, 192), new Vector2(326, 336), new Vector2(146, 342)
            };
            c.FillCrayonPolygon(houseWall, ClampCol(232, 228, 216), seed: 7130, strokeAngleDeg: 85f);
            c.DrawCrayonPolygonOutline(houseWall, crayonBlack, 4.5f, seed: 7131);

            // Rough wax-filled pitched red-brown roof
            Vector2[] roof =
            {
                new Vector2(104, 206), new Vector2(226, 94), new Vector2(354, 196)
            };
            c.FillCrayonPolygon(roof, roofRed, seed: 7140, strokeAngleDeg: -26f);
            c.DrawCrayonPolygonOutline(roof, crayonBlack, 4.8f, seed: 7141);

            // Crooked 4-pane window
            Vector2[] win =
            {
                new Vector2(166, 224), new Vector2(212, 221), new Vector2(215, 266), new Vector2(168, 269)
            };
            c.FillCrayonPolygon(win, skyBlue, seed: 7150, strokeAngleDeg: 35f);
            c.DrawCrayonPolygonOutline(win, crayonBlack, 3.8f, seed: 7151);
            c.DrawCrayonStroke(190, 222, 192, 267, crayonBlack, 3.2f, seed: 7152);
            c.DrawCrayonStroke(167, 246, 214, 244, crayonBlack, 3.2f, seed: 7153);

            // Uneven brown wax-crayon door + round knob
            Vector2[] door =
            {
                new Vector2(236, 242), new Vector2(288, 239), new Vector2(292, 338), new Vector2(239, 340)
            };
            c.FillCrayonPolygon(door, doorBrown, seed: 7160, strokeAngleDeg: 82f);
            c.DrawCrayonPolygonOutline(door, crayonBlack, 4.0f, seed: 7161);
            c.FillCrayonCircle(277, 294, 6, crayonBlack, seed: 7162);

            // Messy hand-scribbled green wax-crayon grass across the bottom
            var grassRng = new System.Random(7170);
            for (int gx = 32; gx <= 478; gx += 9)
            {
                int topY = 320 + grassRng.Next(-16, 14);
                int lean = grassRng.Next(-13, 14);
                Color32 gc = grassRng.NextDouble() > 0.35 ? grassGreen : grassDark;
                c.DrawCrayonStroke(gx, 354 + grassRng.Next(-5, 6), gx + lean, topY, gc, 3.8f, seed: 7200 + gx);
            }
            c.DrawCrayonStroke(34, 349, 474, 345, grassGreen, 6.0f, seed: 7180);
            c.DrawCrayonStroke(44, 355, 462, 352, grassDark, 5.0f, seed: 7181);

            // Tiny naive flower beside house on left
            c.DrawCrayonStroke(82, 336, 78, 292, grassDark, 3.5f, seed: 7185);
            c.FillCrayonCircle(78, 286, 11, roofRed, seed: 7186);
            c.FillCrayonCircle(78, 286, 5, sunOchre, seed: 7187);

            // Dirty creased paper patina + light thumbprint smudges
            c.FillRadialBlob(460f, 342f, 44f, 36f, ClampCol(174, 166, 148), 0.24f, 1.8f);
            c.FillRadialBlob(48f, 52f, 52f, 42f, ClampCol(178, 170, 152), 0.20f, 1.8f);
            c.ApplyDirtyPaperPatina(seed: 7199, grainAmount: 0.075f, edgeGrime: 0.24f, foldYNorm: 0.50f);
            return c;
        }

        private static PixelCanvas BuildChildFamilyDrawing()
        {
            Color32 paper = ClampCol(224, 217, 200);
            Color32 crayonBlack = ClampCol(46, 43, 40);
            Color32 dadBlue = ClampCol(84, 116, 148);
            Color32 dadPants = ClampCol(96, 90, 82);
            Color32 momRose = ClampCol(162, 102, 100);
            Color32 kidOchre = ClampCol(168, 138, 78);
            Color32 kidTeal = ClampCol(96, 124, 124);
            Color32 hairBrown = ClampCol(84, 58, 42);
            Color32 hairDark = ClampCol(54, 42, 36);
            Color32 grassGreen = ClampCol(88, 116, 66);
            Color32 grassDark = ClampCol(64, 90, 48);

            var c = new PixelCanvas(512, 400, paper);

            // Rough scribbled green crayon grass along base
            var rng = new System.Random(7301);
            for (int gx = 34; gx <= 478; gx += 9)
            {
                int topY = 324 + rng.Next(-14, 12);
                int lean = rng.Next(-12, 13);
                Color32 gc = rng.NextDouble() > 0.35 ? grassGreen : grassDark;
                c.DrawCrayonStroke(gx, 354 + rng.Next(-4, 6), gx + lean, topY, gc, 3.8f, seed: 7310 + gx);
            }
            c.DrawCrayonStroke(38, 349, 474, 347, grassGreen, 5.8f, seed: 7302);

            // 1. Dad (tallest, left): blue crayon sweater + trousers + short brown scribble hair
            Vector2[] dadTorso =
            {
                new Vector2(88, 174), new Vector2(138, 172), new Vector2(142, 248), new Vector2(84, 250)
            };
            Vector2[] dadLegL =
            {
                new Vector2(86, 250), new Vector2(110, 250), new Vector2(106, 336), new Vector2(84, 336)
            };
            Vector2[] dadLegR =
            {
                new Vector2(114, 250), new Vector2(140, 250), new Vector2(142, 336), new Vector2(118, 336)
            };
            c.FillCrayonPolygon(dadTorso, dadBlue, seed: 7401, strokeAngleDeg: 24f);
            c.FillCrayonPolygon(dadLegL, dadPants, seed: 7402, strokeAngleDeg: 85f);
            c.FillCrayonPolygon(dadLegR, dadPants, seed: 7403, strokeAngleDeg: 85f);
            c.DrawCrayonPolygonOutline(dadTorso, crayonBlack, 3.6f, seed: 7404);
            c.DrawCrayonPolygonOutline(dadLegL, crayonBlack, 3.2f, seed: 7405);
            c.DrawCrayonPolygonOutline(dadLegR, crayonBlack, 3.2f, seed: 7406);
            c.DrawCrayonStroke(86, 186, 58, 224, dadBlue, 9.0f, seed: 7407);
            c.DrawCrayonStroke(138, 186, 172, 222, dadBlue, 9.0f, seed: 7408);
            c.DrawCrayonStroke(86, 186, 58, 224, crayonBlack, 3.2f, seed: 7409);
            c.DrawCrayonStroke(138, 186, 172, 222, crayonBlack, 3.2f, seed: 7410);
            DrawChildCrayonHead(c, 113, 144, 27, hairBrown, isLongHair: false, hasPigtails: false, seed: 7411);

            // 2. Mom (center-left): dusty-rose triangular crayon dress + dark shoulder-length scribble hair
            Vector2[] momDress =
            {
                new Vector2(204, 184), new Vector2(236, 184), new Vector2(262, 286), new Vector2(180, 288)
            };
            c.FillCrayonPolygon(momDress, momRose, seed: 7420, strokeAngleDeg: -22f);
            c.DrawCrayonPolygonOutline(momDress, crayonBlack, 3.6f, seed: 7421);
            c.DrawCrayonStroke(208, 288, 206, 336, crayonBlack, 3.8f, seed: 7422);
            c.DrawCrayonStroke(234, 288, 236, 336, crayonBlack, 3.8f, seed: 7423);
            c.DrawCrayonStroke(204, 198, 172, 222, crayonBlack, 3.5f, seed: 7424); // holding dad's hand
            c.DrawCrayonStroke(236, 198, 276, 238, crayonBlack, 3.5f, seed: 7425); // holding older daughter's hand
            DrawChildCrayonHead(c, 220, 156, 25, hairDark, isLongHair: true, hasPigtails: false, seed: 7426);

            // 3. Older daughter (~8 yrs, center-right): ochre dress + wavy hair
            Vector2[] kid1Dress =
            {
                new Vector2(304, 228), new Vector2(330, 228), new Vector2(348, 298), new Vector2(288, 300)
            };
            c.FillCrayonPolygon(kid1Dress, kidOchre, seed: 7440, strokeAngleDeg: 30f);
            c.DrawCrayonPolygonOutline(kid1Dress, crayonBlack, 3.4f, seed: 7441);
            c.DrawCrayonStroke(308, 299, 306, 338, crayonBlack, 3.4f, seed: 7442);
            c.DrawCrayonStroke(328, 299, 330, 338, crayonBlack, 3.4f, seed: 7443);
            c.DrawCrayonStroke(304, 238, 276, 238, crayonBlack, 3.2f, seed: 7444); // holding mom's hand
            c.DrawCrayonStroke(330, 238, 368, 254, crayonBlack, 3.2f, seed: 7445); // holding sister's hand
            DrawChildCrayonHead(c, 317, 204, 20, hairBrown, isLongHair: false, hasPigtails: true, seed: 7446);

            // 4. Younger daughter (~6 yrs, right): teal dress + short bob hair
            Vector2[] kid2Dress =
            {
                new Vector2(394, 246), new Vector2(418, 246), new Vector2(434, 304), new Vector2(380, 305)
            };
            c.FillCrayonPolygon(kid2Dress, kidTeal, seed: 7460, strokeAngleDeg: -28f);
            c.DrawCrayonPolygonOutline(kid2Dress, crayonBlack, 3.4f, seed: 7461);
            c.DrawCrayonStroke(398, 304, 396, 338, crayonBlack, 3.4f, seed: 7462);
            c.DrawCrayonStroke(416, 304, 418, 338, crayonBlack, 3.4f, seed: 7463);
            c.DrawCrayonStroke(394, 256, 368, 254, crayonBlack, 3.2f, seed: 7464); // holding sister's hand
            c.DrawCrayonStroke(418, 256, 444, 272, crayonBlack, 3.2f, seed: 7465);
            DrawChildCrayonHead(c, 406, 224, 18, hairDark, isLongHair: true, hasPigtails: false, seed: 7466);

            // Childlike slightly uneven crayon header
            c.DrawCenteredText(254, 36, "MINHA FAMILIA", 2, dadBlue);
            c.DrawCenteredText(255, 37, "MINHA FAMILIA", 2, dadBlue);

            // Aged smudged paper patina
            c.FillRadialBlob(174f, 140f, 34f, 30f, ClampCol(174, 164, 146), 0.22f, 1.8f);
            c.FillRadialBlob(446f, 64f, 48f, 38f, ClampCol(172, 164, 148), 0.24f, 1.8f);
            c.ApplyDirtyPaperPatina(seed: 7499, grainAmount: 0.075f, edgeGrime: 0.24f, foldYNorm: 0.52f);
            return c;
        }

        private static void DrawChildCrayonHead(PixelCanvas c, int cx, int cy, int r, Color32 hairCol, bool isLongHair, bool hasPigtails, int seed)
        {
            Color32 crayonBlack = ClampCol(46, 43, 40);
            Color32 headPaper = ClampCol(234, 228, 214);

            if (isLongHair)
            {
                Vector2[] hairBack =
                {
                    new Vector2(cx - r - 8, cy - r * 0.25f),
                    new Vector2(cx - r * 0.7f, cy - r - 6),
                    new Vector2(cx, cy - r - 11),
                    new Vector2(cx + r * 0.7f, cy - r - 6),
                    new Vector2(cx + r + 8, cy - r * 0.25f),
                    new Vector2(cx + r + 9, cy + r + 10),
                    new Vector2(cx - r - 9, cy + r + 10)
                };
                c.FillCrayonPolygon(hairBack, hairCol, seed + 1, strokeAngleDeg: 78f);
            }
            else if (hasPigtails)
            {
                c.FillCrayonCircle(cx - r - 5, cy - 2, (int)(r * 0.45f), hairCol, seed + 2);
                c.FillCrayonCircle(cx + r + 5, cy - 2, (int)(r * 0.45f), hairCol, seed + 3);
            }

            // Solid opaque paper base for head so dark back-hair never bleeds across face
            c.FillCircle(cx, cy, r - 1, headPaper);
            c.DrawCrayonCircleOutline(cx, cy, r, crayonBlack, 3.2f, seed + 5);

            // Rough top hair cap scribble
            Vector2[] hairTop =
            {
                new Vector2(cx - r + 2, cy - r * 0.35f),
                new Vector2(cx - r * 0.5f, cy - r - 5),
                new Vector2(cx + r * 0.5f, cy - r - 5),
                new Vector2(cx + r - 2, cy - r * 0.35f),
                new Vector2(cx + r * 0.2f, cy - r * 0.55f),
                new Vector2(cx - r * 0.3f, cy - r * 0.45f)
            };
            c.FillCrayonPolygon(hairTop, hairCol, seed + 6, strokeAngleDeg: 14f);

            // Simple naive eyes & small curved smile
            c.FillCrayonCircle(cx - r / 3, cy - 2, Mathf.Max(2, r / 9), crayonBlack, seed + 7);
            c.FillCrayonCircle(cx + r / 3, cy - 2, Mathf.Max(2, r / 9), crayonBlack, seed + 8);
            c.DrawCrayonStroke(cx - r * 0.32f, cy + r * 0.28f, cx, cy + r * 0.42f, crayonBlack, 2.4f, seed + 9);
            c.DrawCrayonStroke(cx, cy + r * 0.42f, cx + r * 0.32f, cy + r * 0.26f, crayonBlack, 2.4f, seed + 10);
        }

        // =====================================================================
        // ESCOPO 4: QUADROS LOW-POLY ESTILIZADOS SUJOS (FAMÍLIA, PRAIA, MARGARET)
        // =====================================================================

        private static PixelCanvas BuildFamilyPortraitArt(bool corrupted)
        {
            var c = new PixelCanvas(512, 640, ClampCol(216, 208, 190));

            // Passe-partout inner bevel
            c.FillRect(22, 22, 468, 596, ClampCol(92, 86, 78));
            c.FillRect(28, 28, 456, 584, ClampCol(156, 149, 134));

            // 1990s Brazilian interior background (faceted plaster corner, open doorway shadow, baseboard)
            c.FillPolygon(new[]
            {
                new Vector2(28, 28), new Vector2(268, 28), new Vector2(268, 560), new Vector2(28, 560)
            }, ClampCol(166, 158, 142));
            c.FillPolygon(new[]
            {
                new Vector2(268, 28), new Vector2(484, 28), new Vector2(484, 560), new Vector2(268, 560)
            }, ClampCol(138, 132, 120));
            // Door frame & dark corridor in back-right
            c.FillPolygon(new[]
            {
                new Vector2(316, 74), new Vector2(438, 74), new Vector2(438, 558), new Vector2(316, 558)
            }, ClampCol(94, 80, 66));
            c.FillPolygon(new[]
            {
                new Vector2(330, 88), new Vector2(424, 88), new Vector2(424, 558), new Vector2(330, 558)
            }, ClampCol(46, 44, 42));
            // Subtle shadow gradient cast by flash along left wall
            c.FillPolygon(new[]
            {
                new Vector2(28, 28), new Vector2(96, 28), new Vector2(72, 612), new Vector2(28, 612)
            }, ClampCol(118, 112, 102), 0.45f);
            // Wood baseboard & warm terracotta floor strip
            c.FillRect(28, 540, 456, 24, ClampCol(76, 58, 44));
            c.FillRect(28, 564, 456, 48, ClampCol(96, 68, 54));

            // --- 1. CIAN (back-left, tallest, muted collared beige-olive work shirt) ---
            DrawLowPolyBust(
                c,
                cx: 178f, headCenterY: 192f, scale: 1.02f,
                hairBase: ClampCol(34, 31, 30), hairLit: ClampCol(54, 49, 46),
                skinLit: ClampCol(186, 162, 142), skinMid: ClampCol(164, 142, 124), skinShadow: ClampCol(124, 104, 92),
                clothLit: ClampCol(162, 150, 134), clothMid: ClampCol(134, 124, 110), clothShadow: ClampCol(98, 90, 80),
                bottomCol: ClampCol(62, 60, 58), waistY: 458f,
                hairStyle: 0, isChild: false, stripedShirt: false);

            // --- 2. MARGARET (back-right, shoulder-length dark hair, muted slate-cream blouse) ---
            DrawLowPolyBust(
                c,
                cx: 326f, headCenterY: 224f, scale: 0.95f,
                hairBase: ClampCol(32, 28, 28), hairLit: ClampCol(54, 46, 44),
                skinLit: ClampCol(194, 174, 158), skinMid: ClampCol(172, 152, 136), skinShadow: ClampCol(132, 114, 102),
                clothLit: ClampCol(176, 174, 168), clothMid: ClampCol(146, 144, 138), clothShadow: ClampCol(108, 106, 102),
                bottomCol: ClampCol(66, 72, 82), waistY: 472f,
                hairStyle: 2, isChild: false, stripedShirt: false);

            // --- 3. OLDER DAUGHTER (~8 yrs, front-left, muted navy striped t-shirt + denim shorts) ---
            DrawLowPolyBust(
                c,
                cx: 198f, headCenterY: 324f, scale: 0.77f,
                hairBase: ClampCol(56, 44, 36), hairLit: ClampCol(82, 66, 54),
                skinLit: ClampCol(196, 176, 158), skinMid: ClampCol(174, 154, 138), skinShadow: ClampCol(136, 118, 104),
                clothLit: ClampCol(68, 82, 104), clothMid: ClampCol(52, 64, 84), clothShadow: ClampCol(38, 46, 62),
                bottomCol: ClampCol(114, 128, 144), waistY: 506f,
                hairStyle: 2, isChild: true, stripedShirt: true);

            // --- 4. YOUNGER DAUGHTER (~6 yrs, front-right, bob hair, dusty-terracotta shirt + cream shorts) ---
            DrawLowPolyBust(
                c,
                cx: 324f, headCenterY: 378f, scale: 0.69f,
                hairBase: ClampCol(44, 34, 30), hairLit: ClampCol(68, 54, 46),
                skinLit: ClampCol(198, 178, 160), skinMid: ClampCol(176, 156, 140), skinShadow: ClampCol(136, 118, 104),
                clothLit: ClampCol(146, 86, 84), clothMid: ClampCol(122, 68, 66), clothShadow: ClampCol(88, 48, 48),
                bottomCol: ClampCol(178, 172, 156), waistY: 534f,
                hairStyle: 1, isChild: true, stripedShirt: false);

            if (corrupted)
            {
                c.DarkenAndDesaturatePhotoRegion(28, 28, 456, 584, darkScale: 0.56f, coldShift: 0.28f);
                // Damp/mold emulsion decay organically smudging out all four faces (NO cartoon red X!)
                c.ApplyFaceEmulsionDecay(178f, 190f, 38f, 48f, seed: 4101, darkness: 0.84f);
                c.ApplyFaceEmulsionDecay(326f, 222f, 36f, 46f, seed: 4102, darkness: 0.84f);
                c.ApplyFaceEmulsionDecay(196f, 328f, 32f, 40f, seed: 4103, darkness: 0.86f);
                c.ApplyFaceEmulsionDecay(326f, 382f, 29f, 36f, seed: 4104, darkness: 0.86f);
                c.ApplyDirtyPhotoPatina(28, 28, 456, 584, seed: 4199, corrupted: true, grainAmount: 0.095f, dampAmount: 0.52f, vignetteAmount: 0.52f);
                c.DrawHairlineGlassCrack(466, 44, new[] { 344, 178, 244, 268, 168, 356 }, 0.52f);
                c.DrawHairlineGlassCrack(344, 178, new[] { 392, 332, 456, 514 }, 0.36f);
            }
            else
            {
                c.ApplyDirtyPhotoPatina(28, 28, 456, 584, seed: 4100, corrupted: false, grainAmount: 0.055f, dampAmount: 0.18f, vignetteAmount: 0.28f);
            }

            // Re-stamp crisp outer passe-partout frame border with subtle dust patina
            c.DrawRectOutline(26, 26, 460, 588, 3, ClampCol(62, 58, 52));
            return c;
        }

        private static void DrawLowPolyBust(
            PixelCanvas c, float cx, float headCenterY, float scale,
            Color32 hairBase, Color32 hairLit,
            Color32 skinLit, Color32 skinMid, Color32 skinShadow,
            Color32 clothLit, Color32 clothMid, Color32 clothShadow,
            Color32 bottomCol, float waistY,
            int hairStyle, bool isChild, bool stripedShirt)
        {
            float s = scale;
            float headTopY = headCenterY - 40f * s;
            float chinY = headCenterY + 38f * s;
            float shoulderY = chinY + 18f * s;
            float bodyBot = 612f;

            // Rounded-crown back hair layer (hairStyle: 0=short, 1=child bob, 2=shoulder-length)
            if (hairStyle >= 1)
            {
                float hairLen = hairStyle == 2 ? (chinY + 46f * s) : (chinY + 18f * s);
                float sideW = hairStyle == 2 ? (46f * s) : (42f * s);
                Vector2[] backHair =
                {
                    new Vector2(cx - sideW, headCenterY - 4f * s),
                    new Vector2(cx - 38f * s, headTopY + 4f * s),
                    new Vector2(cx - 22f * s, headTopY - 12f * s),
                    new Vector2(cx, headTopY - 18f * s),
                    new Vector2(cx + 22f * s, headTopY - 12f * s),
                    new Vector2(cx + 38f * s, headTopY + 4f * s),
                    new Vector2(cx + sideW, headCenterY - 4f * s),
                    new Vector2(cx + sideW + 4f * s, hairLen),
                    new Vector2(cx - sideW - 4f * s, hairLen)
                };
                c.FillPolygon(backHair, hairBase);
            }

            // Sloped multi-polygon shoulders, sleeves & torso down to waist, then shorts/trousers
            float shHalfW = (isChild ? 70f : 82f) * s;
            float shirtEnd = Mathf.Min(bodyBot, waistY);
            Vector2[] torsoLeft =
            {
                new Vector2(cx - 22f * s, shoulderY - 5f * s),
                new Vector2(cx - shHalfW * 0.72f, shoulderY + 10f * s),
                new Vector2(cx - shHalfW, shoulderY + 52f * s),
                new Vector2(cx - shHalfW * 0.78f, shirtEnd),
                new Vector2(cx + 4f * s, shirtEnd),
                new Vector2(cx + 5f * s, shoulderY + 8f * s)
            };
            Vector2[] torsoRight =
            {
                new Vector2(cx + 4f * s, shoulderY + 8f * s),
                new Vector2(cx + 22f * s, shoulderY - 5f * s),
                new Vector2(cx + shHalfW * 0.74f, shoulderY + 12f * s),
                new Vector2(cx + shHalfW, shoulderY + 56f * s),
                new Vector2(cx + shHalfW * 0.78f, shirtEnd),
                new Vector2(cx + 4f * s, shirtEnd)
            };
            c.FillPolygon(torsoLeft, clothMid);
            c.FillPolygon(torsoRight, clothShadow);
            // Left shoulder catch-light facet
            c.FillPolygon(new[]
            {
                new Vector2(cx - 22f * s, shoulderY - 5f * s),
                new Vector2(cx - shHalfW * 0.72f, shoulderY + 10f * s),
                new Vector2(cx - shHalfW * 0.88f, shoulderY + 54f * s),
                new Vector2(cx - shHalfW * 0.34f, shoulderY + 64f * s),
                new Vector2(cx - 12f * s, shoulderY + 16f * s)
            }, clothLit, 0.75f);

            if (stripedShirt)
            {
                for (float sy = shoulderY + 22f * s; sy < shirtEnd - 6f; sy += 15f * s)
                {
                    c.FillRect(
                        Mathf.RoundToInt(cx - shHalfW * 0.72f),
                        Mathf.RoundToInt(sy),
                        Mathf.RoundToInt(shHalfW * 1.44f),
                        Mathf.Max(2, Mathf.RoundToInt(3.5f * s)),
                        ClampCol(164, 170, 178));
                }
            }

            // Hanging arms & subtle sleeve crease shadows
            c.FillPolygon(new[]
            {
                new Vector2(cx - shHalfW * 0.94f, shoulderY + 50f * s),
                new Vector2(cx - shHalfW * 0.68f, shoulderY + 54f * s),
                new Vector2(cx - shHalfW * 0.66f, Mathf.Min(bodyBot, shirtEnd + 28f * s)),
                new Vector2(cx - shHalfW * 0.92f, Mathf.Min(bodyBot, shirtEnd + 28f * s))
            }, skinMid, 0.88f);
            c.FillPolygon(new[]
            {
                new Vector2(cx + shHalfW * 0.68f, shoulderY + 54f * s),
                new Vector2(cx + shHalfW * 0.94f, shoulderY + 50f * s),
                new Vector2(cx + shHalfW * 0.92f, Mathf.Min(bodyBot, shirtEnd + 28f * s)),
                new Vector2(cx + shHalfW * 0.66f, Mathf.Min(bodyBot, shirtEnd + 28f * s))
            }, skinShadow, 0.85f);

            // Waistband & trousers/shorts below shirt
            if (shirtEnd < bodyBot - 4f)
            {
                float bw = shHalfW * 0.76f;
                c.FillPolygon(new[]
                {
                    new Vector2(cx - bw, shirtEnd), new Vector2(cx + bw, shirtEnd),
                    new Vector2(cx + bw * 1.02f, bodyBot), new Vector2(cx - bw * 1.02f, bodyBot)
                }, bottomCol);
                c.FillPolygon(new[]
                {
                    new Vector2(cx + 2f * s, shirtEnd), new Vector2(cx + bw, shirtEnd),
                    new Vector2(cx + bw * 1.02f, bodyBot), new Vector2(cx + 2f * s, bodyBot)
                }, ClampCol(28, 28, 30), 0.24f);
                c.DrawThinAlphaLine(cx - bw, shirtEnd, cx + bw, shirtEnd, ClampCol(36, 34, 34), 2.5f, 0.55f);
            }

            // Collar / neckline V-shadow
            c.FillPolygon(new[]
            {
                new Vector2(cx - 20f * s, shoulderY - 3f * s),
                new Vector2(cx + 20f * s, shoulderY - 3f * s),
                new Vector2(cx, shoulderY + 22f * s)
            }, skinMid);
            c.FillPolygon(new[]
            {
                new Vector2(cx - 16f * s, chinY - 4f * s),
                new Vector2(cx + 16f * s, chinY - 4f * s),
                new Vector2(cx + 18f * s, shoulderY + 4f * s),
                new Vector2(cx - 18f * s, shoulderY + 4f * s)
            }, skinMid);
            // Chin cast shadow onto neck
            c.FillPolygon(new[]
            {
                new Vector2(cx - 15f * s, chinY - 2f * s),
                new Vector2(cx + 15f * s, chinY - 2f * s),
                new Vector2(cx + 11f * s, chinY + 13f * s),
                new Vector2(cx - 9f * s, chinY + 11f * s)
            }, skinShadow, 0.76f);

            // Sculpted polygonal face (10-vertex jaw/cheek/forehead silhouette — NO circle!)
            float fw = 32f * s;
            Vector2[] headPoly =
            {
                new Vector2(cx - fw * 0.78f, headTopY + 10f * s),
                new Vector2(cx, headTopY),
                new Vector2(cx + fw * 0.78f, headTopY + 10f * s),
                new Vector2(cx + fw, headCenterY - 2f * s),
                new Vector2(cx + fw * 0.88f, headCenterY + 18f * s),
                new Vector2(cx + fw * 0.44f, chinY - 4f * s),
                new Vector2(cx, chinY + 2f * s),
                new Vector2(cx - fw * 0.44f, chinY - 4f * s),
                new Vector2(cx - fw * 0.88f, headCenterY + 18f * s),
                new Vector2(cx - fw, headCenterY - 2f * s)
            };
            c.FillPolygon(headPoly, skinMid);

            // Left lit cheek/forehead plane + right shadowed cheek/jaw plane
            c.FillPolygon(new[]
            {
                new Vector2(cx - fw * 0.78f, headTopY + 10f * s),
                new Vector2(cx + 2f * s, headTopY + 2f * s),
                new Vector2(cx - 2f * s, chinY - 2f * s),
                new Vector2(cx - fw * 0.44f, chinY - 4f * s),
                new Vector2(cx - fw * 0.88f, headCenterY + 18f * s),
                new Vector2(cx - fw, headCenterY - 2f * s)
            }, skinLit, 0.58f);
            c.FillPolygon(new[]
            {
                new Vector2(cx + 6f * s, headTopY + 6f * s),
                new Vector2(cx + fw * 0.78f, headTopY + 10f * s),
                new Vector2(cx + fw, headCenterY - 2f * s),
                new Vector2(cx + fw * 0.88f, headCenterY + 18f * s),
                new Vector2(cx + fw * 0.44f, chinY - 4f * s),
                new Vector2(cx + 4f * s, chinY)
            }, skinShadow, 0.48f);

            // Subtle polygonal brow/eye sockets with warm sclera & dark irises
            float eyeY = headCenterY - 2f * s;
            float eyeOffX = 12.5f * s;
            c.FillPolygon(new[]
            {
                new Vector2(cx - eyeOffX - 8f * s, eyeY - 4f * s),
                new Vector2(cx - eyeOffX + 7f * s, eyeY - 3f * s),
                new Vector2(cx - eyeOffX + 6f * s, eyeY + 5f * s),
                new Vector2(cx - eyeOffX - 7f * s, eyeY + 4f * s)
            }, skinShadow, 0.58f);
            c.FillPolygon(new[]
            {
                new Vector2(cx + eyeOffX - 7f * s, eyeY - 3f * s),
                new Vector2(cx + eyeOffX + 8f * s, eyeY - 4f * s),
                new Vector2(cx + eyeOffX + 7f * s, eyeY + 4f * s),
                new Vector2(cx + eyeOffX - 6f * s, eyeY + 5f * s)
            }, skinShadow, 0.64f);
            Color32 scleraCol = ClampCol(194, 188, 178);
            Color32 darkFeature = ClampCol(38, 34, 34);
            // Left eye sclera + iris + lash line
            c.FillPolygon(new[]
            {
                new Vector2(cx - eyeOffX - 6f * s, eyeY), new Vector2(cx - eyeOffX, eyeY - 2.6f * s),
                new Vector2(cx - eyeOffX + 6f * s, eyeY), new Vector2(cx - eyeOffX, eyeY + 2.8f * s)
            }, scleraCol, 0.82f);
            c.FillPolygon(new[]
            {
                new Vector2(cx - eyeOffX - 2.6f * s, eyeY - 2.2f * s), new Vector2(cx - eyeOffX + 2.6f * s, eyeY - 2.2f * s),
                new Vector2(cx - eyeOffX + 2.6f * s, eyeY + 2.4f * s), new Vector2(cx - eyeOffX - 2.6f * s, eyeY + 2.4f * s)
            }, darkFeature, 0.90f);
            c.DrawThinAlphaLine(cx - eyeOffX - 6f * s, eyeY - 1.2f * s, cx - eyeOffX + 6f * s, eyeY - 1.2f * s, darkFeature, 1.8f * s, 0.82f);
            // Right eye sclera + iris + lash line
            c.FillPolygon(new[]
            {
                new Vector2(cx + eyeOffX - 6f * s, eyeY), new Vector2(cx + eyeOffX, eyeY - 2.6f * s),
                new Vector2(cx + eyeOffX + 6f * s, eyeY), new Vector2(cx + eyeOffX, eyeY + 2.8f * s)
            }, scleraCol, 0.78f);
            c.FillPolygon(new[]
            {
                new Vector2(cx + eyeOffX - 2.6f * s, eyeY - 2.2f * s), new Vector2(cx + eyeOffX + 2.6f * s, eyeY - 2.2f * s),
                new Vector2(cx + eyeOffX + 2.6f * s, eyeY + 2.4f * s), new Vector2(cx + eyeOffX - 2.6f * s, eyeY + 2.4f * s)
            }, darkFeature, 0.90f);
            c.DrawThinAlphaLine(cx + eyeOffX - 6f * s, eyeY - 1.2f * s, cx + eyeOffX + 6f * s, eyeY - 1.2f * s, darkFeature, 1.8f * s, 0.82f);

            // Eyebrow facets
            c.DrawThinAlphaLine(cx - eyeOffX - 7f * s, eyeY - 7f * s, cx - eyeOffX + 6f * s, eyeY - 6f * s, hairBase, 2.4f * s, 0.76f);
            c.DrawThinAlphaLine(cx + eyeOffX - 6f * s, eyeY - 6f * s, cx + eyeOffX + 7f * s, eyeY - 7f * s, hairBase, 2.4f * s, 0.76f);
            // Nose shadow plane & base
            c.FillPolygon(new[]
            {
                new Vector2(cx + 1f * s, eyeY + 1f * s),
                new Vector2(cx + 5.5f * s, headCenterY + 13f * s),
                new Vector2(cx - 5f * s, headCenterY + 14f * s),
                new Vector2(cx - 1f * s, headCenterY + 11f * s)
            }, skinShadow, 0.60f);
            // Quiet mouth polygon
            Color32 lipMuted = ClampCol(124, 92, 88);
            c.FillPolygon(new[]
            {
                new Vector2(cx - 10f * s, headCenterY + 23f * s),
                new Vector2(cx, headCenterY + 21.5f * s),
                new Vector2(cx + 10f * s, headCenterY + 23f * s),
                new Vector2(cx + 6f * s, headCenterY + 26.5f * s),
                new Vector2(cx - 6f * s, headCenterY + 26.5f * s)
            }, lipMuted, 0.76f);

            // Rounded foreground hair crown & bangs (with soft bangs for child bob)
            Vector2[] crownHair =
            {
                new Vector2(cx - fw * 1.06f, headCenterY - 8f * s),
                new Vector2(cx - fw * 0.96f, headTopY + 2f * s),
                new Vector2(cx - fw * 0.46f, headTopY - 12f * s),
                new Vector2(cx, headTopY - 16f * s),
                new Vector2(cx + fw * 0.46f, headTopY - 12f * s),
                new Vector2(cx + fw * 0.98f, headTopY + 2f * s),
                new Vector2(cx + fw * 1.06f, headCenterY - 8f * s),
                new Vector2(cx + fw * 0.80f, headTopY + (hairStyle == 1 ? 18f : 12f) * s),
                new Vector2(cx + fw * 0.25f, headTopY + (hairStyle == 1 ? 16f : 8f) * s),
                new Vector2(cx - fw * 0.30f, headTopY + (hairStyle == 1 ? 18f : 10f) * s),
                new Vector2(cx - fw * 0.80f, headTopY + (hairStyle == 1 ? 20f : 14f) * s)
            };
            c.FillPolygon(crownHair, hairBase);
            c.FillPolygon(new[]
            {
                new Vector2(cx - fw * 0.88f, headTopY + 4f * s),
                new Vector2(cx - fw * 0.36f, headTopY - 10f * s),
                new Vector2(cx + fw * 0.14f, headTopY - 11f * s),
                new Vector2(cx - fw * 0.24f, headTopY + 6f * s)
            }, hairLit, 0.65f);
        }

        private static PixelCanvas BuildBeachLandscapeArt(bool corrupted)
        {
            var c = new PixelCanvas(512, 640, ClampCol(216, 208, 190));

            // Passe-partout mat recess
            c.FillRect(22, 22, 468, 596, ClampCol(92, 86, 78));

            // 1. Layered overcast Baixada Santista sky gradient
            c.FillGradientRect(28, 28, 456, 310, ClampCol(132, 142, 152), ClampCol(190, 192, 188));

            // Faceted low-poly overcast cloud banks
            c.FillPolygon(new[]
            {
                new Vector2(28, 66), new Vector2(148, 46), new Vector2(256, 78),
                new Vector2(368, 52), new Vector2(484, 72), new Vector2(484, 152),
                new Vector2(320, 134), new Vector2(180, 158), new Vector2(28, 138)
            }, ClampCol(156, 162, 168), 0.72f);
            c.FillPolygon(new[]
            {
                new Vector2(28, 164), new Vector2(166, 146), new Vector2(286, 174),
                new Vector2(412, 150), new Vector2(484, 168), new Vector2(484, 246), new Vector2(28, 246)
            }, ClampCol(174, 178, 178), 0.62f);

            // Soft pale hazy sun glow peeking through overcast sky (NO flat yellow dot!)
            c.FillRadialBlob(338f, 166f, 92f, 66f, ClampCol(222, 218, 204), 0.45f, 1.8f);
            c.FillRadialBlob(338f, 166f, 34f, 28f, ClampCol(234, 230, 216), 0.55f, 1.5f);

            // 2. Distant misty coastal mountain ridges (Serra do Mar / Guarujá headlands)
            c.FillPolygon(new[]
            {
                new Vector2(28, 336), new Vector2(124, 326), new Vector2(216, 332),
                new Vector2(298, 316), new Vector2(382, 304), new Vector2(448, 288),
                new Vector2(484, 294), new Vector2(484, 342), new Vector2(28, 342)
            }, ClampCol(114, 124, 132), 0.78f);
            c.FillPolygon(new[]
            {
                new Vector2(336, 338), new Vector2(414, 308), new Vector2(468, 296),
                new Vector2(484, 302), new Vector2(484, 344), new Vector2(336, 344)
            }, ClampCol(94, 104, 112), 0.85f);

            // 3. Overcast Atlantic sea + faceted wave planes
            c.FillGradientRect(28, 336, 456, 136, ClampCol(86, 102, 114), ClampCol(118, 128, 134));
            c.FillPolygon(new[]
            {
                new Vector2(28, 358), new Vector2(246, 354), new Vector2(332, 366),
                new Vector2(212, 374), new Vector2(28, 372)
            }, ClampCol(70, 84, 96), 0.65f);
            c.FillPolygon(new[]
            {
                new Vector2(28, 392), new Vector2(288, 386), new Vector2(314, 404),
                new Vector2(180, 412), new Vector2(28, 408)
            }, ClampCol(142, 150, 154), 0.55f);

            // 4. Diagonal perspective sandy beach + wet reflective shoreline + surf foam
            c.FillPolygon(new[]
            {
                new Vector2(28, 466), new Vector2(342, 342), new Vector2(484, 344),
                new Vector2(484, 612), new Vector2(28, 612)
            }, ClampCol(156, 148, 132));
            // Wet mirror-sand strip hugging water edge
            c.FillPolygon(new[]
            {
                new Vector2(28, 444), new Vector2(330, 342), new Vector2(370, 344),
                new Vector2(214, 612), new Vector2(28, 612)
            }, ClampCol(124, 122, 116), 0.85f);
            // Irregular low-poly pale surf foam line
            c.FillPolygon(new[]
            {
                new Vector2(28, 446), new Vector2(168, 402), new Vector2(292, 364),
                new Vector2(344, 344), new Vector2(332, 342), new Vector2(266, 360),
                new Vector2(144, 396), new Vector2(28, 434)
            }, ClampCol(202, 204, 200), 0.72f);
            // Subtle foreground dune shadow & dry grass tufts at right edge
            c.FillPolygon(new[]
            {
                new Vector2(382, 348), new Vector2(448, 332), new Vector2(484, 336),
                new Vector2(484, 364), new Vector2(396, 360)
            }, ClampCol(84, 88, 74), 0.88f);

            // 5. Distant backlit low-poly family silhouettes walking along shoreline (mother + 2 small daughters)
            Color32 silCol = ClampCol(34, 36, 40);
            Color32 reflCol = ClampCol(62, 66, 70);
            // Wet-sand reflections beneath feet
            c.FillPolygon(new[] { new Vector2(319, 444), new Vector2(329, 444), new Vector2(327, 476), new Vector2(321, 476) }, reflCol, 0.36f);
            c.FillPolygon(new[] { new Vector2(294, 444), new Vector2(302, 444), new Vector2(300, 464), new Vector2(296, 464) }, reflCol, 0.32f);
            c.FillPolygon(new[] { new Vector2(346, 444), new Vector2(354, 444), new Vector2(352, 462), new Vector2(348, 462) }, reflCol, 0.32f);

            // Mother silhouette (center: distinct head, sloped shoulders, slim waist, two legs)
            c.FillCircle(324, 393, 5, silCol);
            c.FillPolygon(new[]
            {
                new Vector2(316, 401), new Vector2(332, 401), new Vector2(330, 424), new Vector2(318, 424)
            }, silCol);
            c.DrawThinAlphaLine(321, 423, 320, 445, silCol, 3.2f, 0.96f);
            c.DrawThinAlphaLine(327, 423, 328, 445, silCol, 3.2f, 0.96f);
            c.DrawThinAlphaLine(317, 404, 311, 422, silCol, 2.2f, 0.94f);
            c.DrawThinAlphaLine(331, 404, 337, 422, silCol, 2.2f, 0.94f);

            // Older daughter silhouette (left, holding mother's hand)
            c.FillCircle(298, 412, 4, silCol);
            c.FillPolygon(new[]
            {
                new Vector2(293, 418), new Vector2(303, 418), new Vector2(304, 432), new Vector2(292, 432)
            }, silCol);
            c.DrawThinAlphaLine(296, 431, 295, 445, silCol, 2.5f, 0.95f);
            c.DrawThinAlphaLine(300, 431, 301, 445, silCol, 2.5f, 0.95f);
            c.DrawThinAlphaLine(303, 421, 311, 422, silCol, 1.9f, 0.90f);

            // Younger daughter silhouette (right, holding mother's hand)
            c.FillCircle(350, 416, 4, silCol);
            c.FillPolygon(new[]
            {
                new Vector2(345, 421), new Vector2(354, 421), new Vector2(355, 434), new Vector2(344, 434)
            }, silCol);
            c.DrawThinAlphaLine(347, 433, 347, 445, silCol, 2.4f, 0.95f);
            c.DrawThinAlphaLine(351, 433, 352, 445, silCol, 2.4f, 0.95f);
            c.DrawThinAlphaLine(337, 422, 345, 424, silCol, 1.9f, 0.90f);

            if (corrupted)
            {
                c.DarkenAndDesaturatePhotoRegion(28, 28, 456, 584, darkScale: 0.48f, coldShift: 0.34f);
                // Bleached damp water-ring eating upper overcast sky + smeared dark shadows dissolving the shoreline figures
                c.FillRadialBlob(276f, 118f, 152f, 92f, ClampCol(132, 128, 118), 0.38f, 1.5f);
                c.FillRadialBlob(338f, 166f, 84f, 62f, ClampCol(32, 32, 36), 0.46f, 1.6f);
                c.ApplyFaceEmulsionDecay(324f, 434f, 52f, 46f, seed: 4202, darkness: 0.80f);
                c.ApplyDirtyPhotoPatina(28, 28, 456, 584, seed: 4299, corrupted: true, grainAmount: 0.10f, dampAmount: 0.54f, vignetteAmount: 0.56f);
                c.DrawHairlineGlassCrack(464, 52, new[] { 338, 212, 232, 316, 174, 496 }, 0.50f);
            }
            else
            {
                c.ApplyDirtyPhotoPatina(28, 28, 456, 584, seed: 4200, corrupted: false, grainAmount: 0.055f, dampAmount: 0.18f, vignetteAmount: 0.28f);
            }

            c.DrawRectOutline(26, 26, 460, 588, 3, ClampCol(62, 58, 52));
            return c;
        }

        private static PixelCanvas BuildMargaretPortraitArt(bool corrupted)
        {
            var c = new PixelCanvas(512, 640, ClampCol(216, 208, 190));

            // Passe-partout inner bevel
            c.FillRect(22, 22, 468, 596, ClampCol(92, 86, 78));

            // 1. Moody 1990s plaster wall backdrop (soft directional window light from left, shadow plane on right)
            c.FillGradientRect(28, 28, 456, 584, ClampCol(142, 139, 132), ClampCol(118, 114, 108));
            c.FillPolygon(new[]
            {
                new Vector2(28, 28), new Vector2(246, 28), new Vector2(182, 612), new Vector2(28, 612)
            }, ClampCol(162, 158, 148), 0.52f);
            // Faint doorframe trim on far right & window jamb edge on far left
            c.FillPolygon(new[]
            {
                new Vector2(430, 28), new Vector2(484, 28), new Vector2(484, 612), new Vector2(430, 612)
            }, ClampCol(92, 86, 78), 0.72f);
            c.FillPolygon(new[]
            {
                new Vector2(28, 28), new Vector2(54, 28), new Vector2(54, 612), new Vector2(28, 612)
            }, ClampCol(78, 74, 68), 0.65f);

            // Palette for Margaret's portrait (muted 90s sepia/slate tones)
            Color32 hairDeep = ClampCol(26, 23, 24);
            Color32 hairMid = ClampCol(44, 38, 36);
            Color32 hairLit = ClampCol(68, 59, 54);
            Color32 skinLit = ClampCol(196, 178, 162);
            Color32 skinMid = ClampCol(174, 156, 140);
            Color32 skinShade = ClampCol(132, 116, 104);
            Color32 coatLit = ClampCol(124, 130, 132);
            Color32 coatMid = ClampCol(98, 104, 108);
            Color32 coatDark = ClampCol(66, 72, 78);
            Color32 blouseDark = ClampCol(42, 46, 54);

            // 2. Back hair volume (asymmetric wavy multi-polygon shoulder-length silhouette)
            c.FillPolygon(new[]
            {
                new Vector2(154, 258), new Vector2(166, 176), new Vector2(204, 130),
                new Vector2(258, 118), new Vector2(314, 134), new Vector2(348, 184),
                new Vector2(362, 268), new Vector2(374, 356), new Vector2(354, 412),
                new Vector2(156, 408), new Vector2(138, 344)
            }, hairMid);
            // Deep inner shadow behind neck/jaw
            c.FillPolygon(new[]
            {
                new Vector2(174, 274), new Vector2(338, 274), new Vector2(344, 406), new Vector2(168, 404)
            }, hairDeep, 0.82f);

            // 3. Grey wool winter coat (structured shoulders, notched lapels, sleeves, inner dark blouse)
            c.FillPolygon(new[]
            {
                new Vector2(184, 374), new Vector2(118, 402), new Vector2(86, 492),
                new Vector2(76, 612), new Vector2(436, 612), new Vector2(422, 488),
                new Vector2(392, 402), new Vector2(326, 374)
            }, coatMid);
            // Left lit shoulder & sleeve facet
            c.FillPolygon(new[]
            {
                new Vector2(186, 376), new Vector2(118, 402), new Vector2(86, 492),
                new Vector2(76, 612), new Vector2(198, 612), new Vector2(226, 480), new Vector2(206, 396)
            }, coatLit, 0.72f);
            // Right shadowed shoulder & arm facet
            c.FillPolygon(new[]
            {
                new Vector2(288, 408), new Vector2(326, 374), new Vector2(392, 402),
                new Vector2(422, 488), new Vector2(436, 612), new Vector2(304, 612), new Vector2(294, 494)
            }, coatDark, 0.78f);
            // Inner dark collared shirt V-neck
            c.FillPolygon(new[]
            {
                new Vector2(212, 374), new Vector2(300, 374), new Vector2(262, 502), new Vector2(246, 502)
            }, blouseDark);
            // Structured coat lapels (left & right faceted folds)
            c.FillPolygon(new[]
            {
                new Vector2(206, 374), new Vector2(178, 412), new Vector2(194, 442),
                new Vector2(182, 460), new Vector2(248, 524), new Vector2(252, 500), new Vector2(218, 396)
            }, coatLit, 0.88f);
            c.FillPolygon(new[]
            {
                new Vector2(306, 374), new Vector2(334, 412), new Vector2(316, 444),
                new Vector2(328, 464), new Vector2(262, 528), new Vector2(258, 500), new Vector2(294, 396)
            }, coatDark, 0.88f);
            // Center coat closure fold & two subtle dark buttons
            c.DrawThinAlphaLine(254, 512, 248, 612, ClampCol(44, 48, 52), 3.0f, 0.78f);
            c.FillCircle(238, 550, 5, ClampCol(46, 50, 56));
            c.FillCircle(236, 592, 5, ClampCol(46, 50, 56));

            // 4. Tapered neck + chin cast shadow
            c.FillPolygon(new[]
            {
                new Vector2(228, 326), new Vector2(284, 326), new Vector2(290, 392),
                new Vector2(256, 414), new Vector2(222, 392)
            }, skinMid);
            c.FillPolygon(new[]
            {
                new Vector2(228, 330), new Vector2(284, 330), new Vector2(276, 360),
                new Vector2(252, 366), new Vector2(234, 352)
            }, skinShade, 0.80f);
            c.FillPolygon(new[]
            {
                new Vector2(262, 332), new Vector2(284, 332), new Vector2(290, 392), new Vector2(264, 406)
            }, skinShade, 0.45f);

            // 5. Sculpted 12-vertex low-poly head (forehead, cheekbones, tapered jaw, chin — NO circle!)
            Vector2[] facePoly =
            {
                new Vector2(204, 186), new Vector2(230, 162), new Vector2(282, 162), new Vector2(308, 186),
                new Vector2(318, 228), new Vector2(314, 268), new Vector2(298, 304), new Vector2(274, 332),
                new Vector2(256, 340), new Vector2(238, 332), new Vector2(214, 304), new Vector2(198, 268),
                new Vector2(194, 228)
            };
            c.FillPolygon(facePoly, skinMid);

            // Left lit plane (forehead + left cheekbone)
            c.FillPolygon(new[]
            {
                new Vector2(204, 186), new Vector2(230, 162), new Vector2(262, 164),
                new Vector2(254, 254), new Vector2(246, 334), new Vector2(214, 304),
                new Vector2(198, 268), new Vector2(194, 228)
            }, skinLit, 0.62f);

            // Right shadow plane (temple + right jaw + cheek hollow)
            c.FillPolygon(new[]
            {
                new Vector2(272, 164), new Vector2(308, 186), new Vector2(318, 228),
                new Vector2(314, 268), new Vector2(298, 304), new Vector2(274, 332),
                new Vector2(258, 338), new Vector2(266, 272), new Vector2(282, 232)
            }, skinShade, 0.56f);

            // Subtle under-cheekbone hollow facets
            c.FillPolygon(new[]
            {
                new Vector2(202, 264), new Vector2(234, 274), new Vector2(232, 304), new Vector2(212, 296)
            }, skinShade, 0.18f);

            // Recessed brow ridge & almond eye sockets (layered low-poly realism, NOT dot eyes!)
            c.FillPolygon(new[]
            {
                new Vector2(208, 232), new Vector2(230, 224), new Vector2(246, 232),
                new Vector2(244, 248), new Vector2(226, 252), new Vector2(210, 246)
            }, skinShade, 0.65f);
            c.FillPolygon(new[]
            {
                new Vector2(266, 232), new Vector2(284, 224), new Vector2(304, 232),
                new Vector2(302, 246), new Vector2(286, 252), new Vector2(268, 248)
            }, skinShade, 0.72f);

            // Sclera almond polygons (muted warm grey-white) + dark irises + angled upper lash/lid shadow
            Color32 sclera = ClampCol(190, 182, 172);
            Color32 iris = ClampCol(42, 36, 35);
            Color32 lidLine = ClampCol(28, 24, 24);
            // Left eye
            c.FillPolygon(new[]
            {
                new Vector2(214, 240), new Vector2(226, 235), new Vector2(240, 239),
                new Vector2(238, 244), new Vector2(226, 246), new Vector2(216, 243)
            }, sclera, 0.85f);
            c.FillPolygon(new[]
            {
                new Vector2(222, 236), new Vector2(232, 236), new Vector2(232, 245), new Vector2(222, 245)
            }, iris, 0.92f);
            c.DrawThinAlphaLine(213, 239, 226, 234, lidLine, 2.8f, 0.90f);
            c.DrawThinAlphaLine(226, 234, 241, 239, lidLine, 2.6f, 0.88f);
            // Right eye
            c.FillPolygon(new[]
            {
                new Vector2(272, 239), new Vector2(286, 235), new Vector2(298, 240),
                new Vector2(296, 244), new Vector2(286, 246), new Vector2(274, 244)
            }, sclera, 0.80f);
            c.FillPolygon(new[]
            {
                new Vector2(280, 236), new Vector2(290, 236), new Vector2(290, 245), new Vector2(280, 245)
            }, iris, 0.92f);
            c.DrawThinAlphaLine(271, 239, 286, 234, lidLine, 2.6f, 0.88f);
            c.DrawThinAlphaLine(286, 234, 299, 239, lidLine, 2.8f, 0.90f);

            // Soft arched eyebrow polygons
            c.FillPolygon(new[]
            {
                new Vector2(209, 227), new Vector2(228, 221), new Vector2(245, 225),
                new Vector2(244, 229), new Vector2(228, 226), new Vector2(211, 231)
            }, hairMid, 0.84f);
            c.FillPolygon(new[]
            {
                new Vector2(267, 225), new Vector2(284, 221), new Vector2(303, 227),
                new Vector2(301, 231), new Vector2(284, 226), new Vector2(268, 229)
            }, hairMid, 0.84f);

            // Faceted nose bridge catch-light + right shadow plane + underside nostril triangle
            c.FillPolygon(new[]
            {
                new Vector2(252, 238), new Vector2(258, 238), new Vector2(256, 274), new Vector2(249, 274)
            }, ClampCol(208, 190, 174), 0.52f);
            c.FillPolygon(new[]
            {
                new Vector2(258, 238), new Vector2(266, 272), new Vector2(256, 276)
            }, skinShade, 0.56f);
            c.FillPolygon(new[]
            {
                new Vector2(244, 274), new Vector2(256, 272), new Vector2(268, 274), new Vector2(256, 281)
            }, ClampCol(104, 88, 78), 0.72f);

            // Quiet somber lips (upper darker facet, lower soft highlight facet, chin dimple shadow)
            c.FillPolygon(new[]
            {
                new Vector2(236, 300), new Vector2(250, 296), new Vector2(256, 298),
                new Vector2(262, 296), new Vector2(276, 300), new Vector2(256, 303)
            }, ClampCol(118, 86, 82), 0.85f);
            c.FillPolygon(new[]
            {
                new Vector2(238, 301), new Vector2(274, 301), new Vector2(266, 309),
                new Vector2(256, 311), new Vector2(246, 309)
            }, ClampCol(154, 118, 110), 0.76f);
            c.DrawThinAlphaLine(238, 301, 274, 301, ClampCol(68, 52, 50), 1.6f, 0.75f);
            c.FillPolygon(new[]
            {
                new Vector2(246, 315), new Vector2(266, 315), new Vector2(256, 321)
            }, skinShade, 0.42f);

            // 6. Foreground layered wavy hair framing temples & spilling over coat collar
            c.FillPolygon(new[]
            {
                new Vector2(182, 164), new Vector2(216, 134), new Vector2(262, 128),
                new Vector2(244, 168), new Vector2(212, 186), new Vector2(198, 232),
                new Vector2(204, 292), new Vector2(194, 366), new Vector2(212, 412),
                new Vector2(174, 424), new Vector2(150, 364), new Vector2(160, 270)
            }, hairMid);
            c.FillPolygon(new[]
            {
                new Vector2(262, 128), new Vector2(308, 140), new Vector2(340, 186),
                new Vector2(354, 280), new Vector2(346, 378), new Vector2(318, 422),
                new Vector2(296, 408), new Vector2(314, 344), new Vector2(312, 266),
                new Vector2(304, 194), new Vector2(276, 168)
            }, hairDeep, 0.92f);
            // Left crown & wavy side-lock highlight facets
            c.FillPolygon(new[]
            {
                new Vector2(190, 162), new Vector2(226, 136), new Vector2(256, 136),
                new Vector2(236, 164), new Vector2(204, 184)
            }, hairLit, 0.72f);
            c.FillPolygon(new[]
            {
                new Vector2(162, 294), new Vector2(182, 268), new Vector2(194, 336), new Vector2(172, 374)
            }, hairLit, 0.48f);

            if (corrupted)
            {
                // Cold damp charcoal-sepia deterioration + wet emulsion decay obliterating her eyes/face (NO cartoon red X!)
                c.DarkenAndDesaturatePhotoRegion(28, 28, 456, 584, darkScale: 0.54f, coldShift: 0.30f);
                c.ApplyFaceEmulsionDecay(256f, 242f, 64f, 68f, seed: 4301, darkness: 0.90f);
                // Vertical wet-emulsion drip trails down cheeks & coat
                c.FillPolygon(new[]
                {
                    new Vector2(224, 240), new Vector2(238, 240), new Vector2(235, 362), new Vector2(226, 358)
                }, ClampCol(20, 18, 20), 0.38f);
                c.FillPolygon(new[]
                {
                    new Vector2(276, 238), new Vector2(290, 238), new Vector2(286, 348), new Vector2(278, 342)
                }, ClampCol(20, 18, 20), 0.42f);
                c.ApplyDirtyPhotoPatina(28, 28, 456, 584, seed: 4399, corrupted: true, grainAmount: 0.10f, dampAmount: 0.58f, vignetteAmount: 0.58f);
                // Subtle delicate hairline fracture in upper-right frame glass
                c.DrawHairlineGlassCrack(468, 46, new[] { 352, 166, 290, 268, 224, 396 }, 0.54f);
                c.DrawHairlineGlassCrack(352, 166, new[] { 408, 314, 462, 512 }, 0.38f);
            }
            else
            {
                c.ApplyDirtyPhotoPatina(28, 28, 456, 584, seed: 4300, corrupted: false, grainAmount: 0.060f, dampAmount: 0.22f, vignetteAmount: 0.34f);
            }

            c.DrawRectOutline(26, 26, 460, 588, 3, ClampCol(62, 58, 52));
            return c;
        }

        private static float Hash2D(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0x00FFFFFFu) / 16777215f;
            }
        }

        // =====================================================================
        // PIXEL CANVAS + POLÍGONOS + GIZ DE CERA + PATINA SUJA + FONTE 5x7
        // =====================================================================

        private sealed class PixelCanvas
        {
            public readonly int Width;
            public readonly int Height;
            private readonly Color32[] pixels;

            public PixelCanvas(int width, int height, Color32 background)
            {
                Width = width;
                Height = height;
                pixels = new Color32[width * height];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = background;
            }

            public Color32 GetPixelTopDown(int x, int y)
            {
                x = Mathf.Clamp(x, 0, Width - 1);
                y = Mathf.Clamp(y, 0, Height - 1);
                return pixels[(Height - 1 - y) * Width + x];
            }

            public void SetPixelTopDown(int x, int y, Color32 col)
            {
                if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
                int unityY = Height - 1 - y;
                pixels[unityY * Width + x] = col;
            }

            public void BlendPixelTopDown(int x, int y, Color32 col, float alpha)
            {
                if ((uint)x >= (uint)Width || (uint)y >= (uint)Height || alpha <= 0.001f) return;
                if (alpha >= 0.999f) { SetPixelTopDown(x, y, col); return; }
                int idx = (Height - 1 - y) * Width + x;
                Color32 dst = pixels[idx];
                float inv = 1f - alpha;
                pixels[idx] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(dst.r * inv + col.r * alpha), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(dst.g * inv + col.g * alpha), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(dst.b * inv + col.b * alpha), 0, 255),
                    255);
            }

            public void FillRect(int x, int y, int w, int h, Color32 col)
            {
                int xEnd = Mathf.Min(Width, x + w);
                int yEnd = Mathf.Min(Height, y + h);
                int xStart = Mathf.Max(0, x);
                int yStart = Mathf.Max(0, y);
                for (int py = yStart; py < yEnd; py++)
                {
                    int row = (Height - 1 - py) * Width;
                    for (int px = xStart; px < xEnd; px++)
                        pixels[row + px] = col;
                }
            }

            public void FillGradientRect(int x, int y, int w, int h, Color32 topCol, Color32 botCol)
            {
                int xEnd = Mathf.Min(Width, x + w);
                int yEnd = Mathf.Min(Height, y + h);
                int xStart = Mathf.Max(0, x);
                int yStart = Mathf.Max(0, y);
                float denom = Mathf.Max(1f, h - 1f);
                for (int py = yStart; py < yEnd; py++)
                {
                    float t = Mathf.Clamp01((py - y) / denom);
                    Color32 rowCol = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Lerp(topCol.r, botCol.r, t)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(topCol.g, botCol.g, t)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(topCol.b, botCol.b, t)),
                        255);
                    int row = (Height - 1 - py) * Width;
                    for (int px = xStart; px < xEnd; px++)
                        pixels[row + px] = rowCol;
                }
            }

            public void FillPolygon(Vector2[] pts, Color32 col, float alpha = 1f)
            {
                if (pts == null || pts.Length < 3 || alpha <= 0.001f) return;

                float minY = float.MaxValue;
                float maxY = float.MinValue;
                for (int i = 0; i < pts.Length; i++)
                {
                    if (pts[i].y < minY) minY = pts[i].y;
                    if (pts[i].y > maxY) maxY = pts[i].y;
                }

                int yStart = Mathf.Max(0, Mathf.FloorToInt(minY));
                int yEnd = Mathf.Min(Height - 1, Mathf.CeilToInt(maxY));
                var nodes = new List<float>(16);

                for (int y = yStart; y <= yEnd; y++)
                {
                    nodes.Clear();
                    float sy = y + 0.5f;
                    int j = pts.Length - 1;
                    for (int i = 0; i < pts.Length; i++)
                    {
                        float yi = pts[i].y;
                        float yj = pts[j].y;
                        if ((yi < sy && yj >= sy) || (yj < sy && yi >= sy))
                        {
                            float ix = pts[i].x + (sy - yi) / (yj - yi) * (pts[j].x - pts[i].x);
                            nodes.Add(ix);
                        }
                        j = i;
                    }

                    nodes.Sort();
                    for (int n = 0; n + 1 < nodes.Count; n += 2)
                    {
                        int x0 = Mathf.Max(0, Mathf.FloorToInt(nodes[n]));
                        int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt(nodes[n + 1]));
                        if (alpha >= 0.999f)
                        {
                            int row = (Height - 1 - y) * Width;
                            for (int x = x0; x <= x1; x++)
                                pixels[row + x] = col;
                        }
                        else
                        {
                            for (int x = x0; x <= x1; x++)
                                BlendPixelTopDown(x, y, col, alpha);
                        }
                    }
                }
            }

            public void FillRadialBlob(float cx, float cy, float rx, float ry, Color32 col, float maxAlpha, float falloffPow = 1.6f)
            {
                int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
                int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt(cx + rx));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
                int y1 = Mathf.Min(Height - 1, Mathf.CeilToInt(cy + ry));
                float invRx = 1f / Mathf.Max(1f, rx);
                float invRy = 1f / Mathf.Max(1f, ry);

                for (int y = y0; y <= y1; y++)
                {
                    float dy = (y - cy) * invRy;
                    float dy2 = dy * dy;
                    for (int x = x0; x <= x1; x++)
                    {
                        float dx = (x - cx) * invRx;
                        float d2 = dx * dx + dy2;
                        if (d2 >= 1f) continue;
                        float a = Mathf.Pow(1f - Mathf.Sqrt(d2), falloffPow) * maxAlpha;
                        BlendPixelTopDown(x, y, col, a);
                    }
                }
            }

            public void DrawThinAlphaLine(float x0, float y0, float x1, float y1, Color32 col, float width, float alpha)
            {
                float dist = Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1));
                int steps = Mathf.Max(1, Mathf.CeilToInt(dist * 1.5f));
                float rad = Mathf.Max(0.5f, width * 0.5f);
                int iRad = Mathf.CeilToInt(rad);

                for (int s = 0; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    float px = Mathf.Lerp(x0, x1, t);
                    float py = Mathf.Lerp(y0, y1, t);
                    int cx = Mathf.RoundToInt(px);
                    int cy = Mathf.RoundToInt(py);
                    for (int dy = -iRad; dy <= iRad; dy++)
                    {
                        for (int dx = -iRad; dx <= iRad; dx++)
                        {
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            if (d <= rad)
                            {
                                float edge = Mathf.Clamp01(1f - Mathf.Max(0f, d - (rad - 0.85f)));
                                BlendPixelTopDown(cx + dx, cy + dy, col, alpha * edge);
                            }
                        }
                    }
                }
            }

            public void DrawCrayonStroke(float x0, float y0, float x1, float y1, Color32 col, float thickness, int seed)
            {
                float dx = x1 - x0;
                float dy = y1 - y0;
                float len = Mathf.Sqrt(dx * dx + dy * dy);
                if (len < 0.5f) return;

                float nx = -dy / len;
                float ny = dx / len;
                int steps = Mathf.Max(4, Mathf.CeilToInt(len * 1.25f));
                float rad = Mathf.Max(1.0f, thickness * 0.5f);
                int iRad = Mathf.CeilToInt(rad);

                for (int s = 0; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    float wobble = (Mathf.Sin(t * Mathf.PI * 3.0f + seed * 0.37f) * 1.45f
                                 + (Hash2D(s, seed, 911) - 0.5f) * 1.2f);
                    float px = Mathf.Lerp(x0, x1, t) + nx * wobble;
                    float py = Mathf.Lerp(y0, y1, t) + ny * wobble;
                    int cx = Mathf.RoundToInt(px);
                    int cy = Mathf.RoundToInt(py);

                    for (int oy = -iRad; oy <= iRad; oy++)
                    {
                        for (int ox = -iRad; ox <= iRad; ox++)
                        {
                            float d = Mathf.Sqrt(ox * ox + oy * oy);
                            if (d > rad) continue;
                            // Paper-tooth wax grain leaves authentic micro-specks of paper showing through
                            float tooth = Hash2D(cx + ox, cy + oy, seed + 17);
                            float pressure = (1f - d / (rad + 0.3f)) * (0.55f + 0.65f * tooth);
                            if (tooth < 0.16f) continue;
                            BlendPixelTopDown(cx + ox, cy + oy, col, Mathf.Clamp01(pressure * 0.88f));
                        }
                    }
                }
            }

            public void DrawCrayonPolygonOutline(Vector2[] pts, Color32 col, float thickness, int seed)
            {
                if (pts == null || pts.Length < 2) return;
                for (int i = 0; i < pts.Length; i++)
                {
                    Vector2 a = pts[i];
                    Vector2 b = pts[(i + 1) % pts.Length];
                    DrawCrayonStroke(a.x, a.y, b.x, b.y, col, thickness, seed + i * 19);
                }
            }

            public void DrawCrayonCircleOutline(int cx, int cy, int r, Color32 col, float thickness, int seed)
            {
                const int segs = 20;
                Vector2[] pts = new Vector2[segs];
                for (int i = 0; i < segs; i++)
                {
                    float a = (i / (float)segs) * Mathf.PI * 2f;
                    float jitter = 1f + (Hash2D(i, seed, 613) - 0.5f) * 0.10f;
                    pts[i] = new Vector2(cx + Mathf.Cos(a) * r * jitter, cy + Mathf.Sin(a) * r * jitter);
                }
                DrawCrayonPolygonOutline(pts, col, thickness, seed + 50);
            }

            public void FillCrayonCircle(int cx, int cy, int r, Color32 col, int seed)
            {
                const int segs = 18;
                Vector2[] pts = new Vector2[segs];
                for (int i = 0; i < segs; i++)
                {
                    float a = (i / (float)segs) * Mathf.PI * 2f;
                    float jitter = 1f + (Hash2D(i, seed, 617) - 0.5f) * 0.10f;
                    pts[i] = new Vector2(cx + Mathf.Cos(a) * r * jitter, cy + Mathf.Sin(a) * r * jitter);
                }
                FillCrayonPolygon(pts, col, seed + 11, strokeAngleDeg: 34f);
            }

            public void FillCrayonPolygon(Vector2[] pts, Color32 col, int seed, float strokeAngleDeg = 28f)
            {
                if (pts == null || pts.Length < 3) return;
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                for (int i = 0; i < pts.Length; i++)
                {
                    if (pts[i].y < minY) minY = pts[i].y;
                    if (pts[i].y > maxY) maxY = pts[i].y;
                }

                int yStart = Mathf.Max(0, Mathf.FloorToInt(minY));
                int yEnd = Mathf.Min(Height - 1, Mathf.CeilToInt(maxY));
                var nodes = new List<float>(12);
                float rad = strokeAngleDeg * Mathf.Deg2Rad;
                float dirX = Mathf.Cos(rad);
                float dirY = Mathf.Sin(rad);

                for (int y = yStart; y <= yEnd; y++)
                {
                    nodes.Clear();
                    float sy = y + 0.5f;
                    int j = pts.Length - 1;
                    for (int i = 0; i < pts.Length; i++)
                    {
                        float yi = pts[i].y;
                        float yj = pts[j].y;
                        if ((yi < sy && yj >= sy) || (yj < sy && yi >= sy))
                            nodes.Add(pts[i].x + (sy - yi) / (yj - yi) * (pts[j].x - pts[i].x));
                        j = i;
                    }
                    nodes.Sort();
                    for (int n = 0; n + 1 < nodes.Count; n += 2)
                    {
                        int x0 = Mathf.Max(0, Mathf.FloorToInt(nodes[n]));
                        int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt(nodes[n + 1]));
                        for (int x = x0; x <= x1; x++)
                        {
                            // Organic directional wax pressure noise + paper tooth speckling (no periodic zebra bands)
                            float along = x * dirX + y * dirY;
                            float across = -x * dirY + y * dirX;
                            float strokePressure = SmoothValueNoise(along * 0.045f, across * 0.24f, seed + 7) * 0.55f
                                                 + SmoothValueNoise(along * 0.11f, across * 0.48f, seed + 23) * 0.45f;
                            float tooth = Hash2D(x, y, seed + 31);
                            float grain = Hash2D(x >> 1, y >> 1, seed + 53);
                            float coverage = strokePressure * 0.52f + tooth * 0.32f + grain * 0.26f;
                            if (coverage < 0.28f) continue;
                            float alpha = Mathf.Clamp01((coverage - 0.20f) * 1.18f);
                            BlendPixelTopDown(x, y, col, alpha);
                        }
                    }
                }
            }

            public void DarkenAndDesaturatePhotoRegion(int x0, int y0, int w, int h, float darkScale, float coldShift)
            {
                int xEnd = Mathf.Min(Width, x0 + w);
                int yEnd = Mathf.Min(Height, y0 + h);
                for (int y = Mathf.Max(0, y0); y < yEnd; y++)
                {
                    int row = (Height - 1 - y) * Width;
                    for (int x = Mathf.Max(0, x0); x < xEnd; x++)
                    {
                        Color32 src = pixels[row + x];
                        float gray = src.r * 0.30f + src.g * 0.59f + src.b * 0.11f;
                        float r = Mathf.Lerp(src.r, gray * 0.94f, 0.72f) * darkScale;
                        float g = Mathf.Lerp(src.g, gray * 0.96f, 0.72f) * darkScale;
                        float b = Mathf.Lerp(src.b, gray * (1.0f + coldShift * 0.12f), 0.72f) * darkScale;
                        pixels[row + x] = new Color32(
                            (byte)Mathf.Clamp(Mathf.RoundToInt(r), 0, 255),
                            (byte)Mathf.Clamp(Mathf.RoundToInt(g), 0, 255),
                            (byte)Mathf.Clamp(Mathf.RoundToInt(b), 0, 255),
                            255);
                    }
                }
            }

            public void ApplyFaceEmulsionDecay(float cx, float cy, float rx, float ry, int seed, float darkness)
            {
                int x0 = Mathf.Max(1, Mathf.FloorToInt(cx - rx * 1.25f));
                int x1 = Mathf.Min(Width - 2, Mathf.CeilToInt(cx + rx * 1.25f));
                int y0 = Mathf.Max(1, Mathf.FloorToInt(cy - ry * 1.25f));
                int y1 = Mathf.Min(Height - 2, Mathf.CeilToInt(cy + ry * 1.45f));

                Color32[] copy = (Color32[])pixels.Clone();
                for (int y = y0; y <= y1; y++)
                {
                    float ny = (y - cy) / Mathf.Max(1f, ry);
                    for (int x = x0; x <= x1; x++)
                    {
                        float nx = (x - cx) / Mathf.Max(1f, rx);
                        float blotch = SmoothValueNoise(x * 0.045f, y * 0.045f, seed) * 0.65f
                                     + SmoothValueNoise(x * 0.11f, y * 0.09f, seed + 19) * 0.35f;
                        // Slightly elongates downward like wet smeared photographic emulsion
                        float effNy = ny > 0f ? ny * 0.84f : ny;
                        float dist = Mathf.Sqrt(nx * nx + effNy * effNy) + (blotch - 0.5f) * 0.52f;
                        if (dist >= 1.0f) continue;

                        float mask = Mathf.Clamp01(Mathf.Pow(1f - dist, 1.15f) * 1.35f);
                        // Sample vertically smeared / blurred neighborhood
                        int smearY = Mathf.Clamp(y - Mathf.RoundToInt(mask * 9f * blotch), 0, Height - 1);
                        int rSum = 0, gSum = 0, bSum = 0, count = 0;
                        for (int oy = -4; oy <= 4; oy += 2)
                        {
                            int sy = Mathf.Clamp(smearY + oy, 0, Height - 1);
                            int sRow = (Height - 1 - sy) * Width;
                            for (int ox = -4; ox <= 4; ox += 2)
                            {
                                int sx = Mathf.Clamp(x + ox, 0, Width - 1);
                                Color32 sc = copy[sRow + sx];
                                rSum += sc.r; gSum += sc.g; bSum += sc.b; count++;
                            }
                        }
                        float br = (rSum / (float)count);
                        float bg = (gSum / (float)count);
                        float bb = (bSum / (float)count);

                        // Moldy wet ink darkening inside the decayed emulsion patch
                        float dampDark = 1f - mask * darkness * (0.65f + 0.35f * blotch);
                        int idx = (Height - 1 - y) * Width + x;
                        Color32 orig = copy[idx];
                        pixels[idx] = new Color32(
                            (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(orig.r, br * dampDark, mask)), 0, 255),
                            (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(orig.g, bg * dampDark * 0.97f, mask)), 0, 255),
                            (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(orig.b, bb * dampDark * 0.95f, mask)), 0, 255),
                            255);
                    }
                }
            }

            public void DrawHairlineGlassCrack(int startX, int startY, int[] xyPairs, float alpha)
            {
                if (xyPairs == null || xyPairs.Length < 2) return;
                int prevX = startX;
                int prevY = startY;
                Color32 crackDark = ClampCol(24, 24, 26);
                Color32 crackGlint = ClampCol(214, 218, 220);

                for (int i = 0; i + 1 < xyPairs.Length; i += 2)
                {
                    int nx = xyPairs[i];
                    int ny = xyPairs[i + 1];
                    DrawThinAlphaLine(prevX + 1, prevY + 1, nx + 1, ny + 1, crackDark, 1.4f, alpha * 0.65f);
                    DrawThinAlphaLine(prevX, prevY, nx, ny, crackGlint, 1.1f, alpha * 0.72f);
                    prevX = nx;
                    prevY = ny;
                }
            }

            public void ApplyDirtyPhotoPatina(int x0, int y0, int w, int h, int seed, bool corrupted, float grainAmount, float dampAmount, float vignetteAmount)
            {
                int xEnd = Mathf.Min(Width, x0 + w);
                int yEnd = Mathf.Min(Height, y0 + h);
                float cx = x0 + w * 0.5f;
                float cy = y0 + h * 0.5f;
                float invRx = 1f / Mathf.Max(1f, w * 0.5f);
                float invRy = 1f / Mathf.Max(1f, h * 0.5f);

                int[,] bayer4 =
                {
                    {  0,  8,  2, 10 },
                    { 12,  4, 14,  6 },
                    {  3, 11,  1,  9 },
                    { 15,  7, 13,  5 }
                };

                // Apply subtle aged yellowing/dust across the whole canvas (including passe-partout border)
                for (int y = 0; y < Height; y++)
                {
                    int row = (Height - 1 - y) * Width;
                    for (int x = 0; x < Width; x++)
                    {
                        bool inPhoto = x >= x0 && x < xEnd && y >= y0 && y < yEnd;
                        Color32 src = pixels[row + x];

                        float blotch = SmoothValueNoise(x * 0.014f, y * 0.014f, seed + 3) * 0.62f
                                     + SmoothValueNoise(x * 0.042f, y * 0.042f, seed + 11) * 0.38f;
                        float fineGrain = (Hash2D(x, y, seed + 23) - 0.5f) * 2f;
                        float dither = ((bayer4[y & 3, x & 3] / 15f) - 0.5f) * 0.022f;

                        float r = src.r / 255f;
                        float g = src.g / 255f;
                        float b = src.b / 255f;

                        if (inPhoto)
                        {
                            float dx = (x - cx) * invRx;
                            float dy = (y - cy) * invRy;
                            float edgeDist = Mathf.Sqrt(dx * dx + dy * dy);
                            float vig = Mathf.Clamp01((edgeDist - 0.52f) / 0.85f);
                            float vigMul = 1f - vig * vig * vignetteAmount;

                            // Organic damp mold creeping from borders (stronger on corrupted photos)
                            float dampEdge = Mathf.Clamp01((edgeDist - (corrupted ? 0.42f : 0.72f)) * 1.45f) * blotch;
                            float dampShade = 1f - dampEdge * dampAmount;

                            // Warm sepia yellowing on normal photos, cold damp mildew on corrupted
                            float sepiaMul = corrupted ? 0.0f : 0.045f * blotch;
                            r = r * vigMul * dampShade + sepiaMul + fineGrain * grainAmount + dither;
                            g = g * vigMul * dampShade + sepiaMul * 0.65f + fineGrain * grainAmount + dither;
                            b = b * vigMul * dampShade - sepiaMul * 0.50f + fineGrain * grainAmount + dither;

                            // Faint vintage dust specks
                            float speck = Hash2D(x >> 1, y >> 1, seed + 71);
                            if (speck > 0.9965f)
                            {
                                float dust = corrupted ? 0.14f : 0.10f;
                                r += dust; g += dust * 0.95f; b += dust * 0.86f;
                            }
                        }
                        else
                        {
                            // Dirty dusty passe-partout frame border
                            float borderNoise = (blotch - 0.5f) * 0.14f + fineGrain * (grainAmount * 0.6f);
                            float borderDark = corrupted ? 0.82f : 0.96f;
                            r = r * borderDark + borderNoise;
                            g = g * borderDark + borderNoise * 0.94f;
                            b = b * borderDark + borderNoise * 0.82f;
                        }

                        pixels[row + x] = new Color32(
                            (byte)Mathf.Clamp(Mathf.RoundToInt(r * 255f), 0, 255),
                            (byte)Mathf.Clamp(Mathf.RoundToInt(g * 255f), 0, 255),
                            (byte)Mathf.Clamp(Mathf.RoundToInt(b * 255f), 0, 255),
                            255);
                    }
                }

                // Subtle vertical 1990s film/emulsion micro-scratches
                var rng = new System.Random(seed + 404);
                int scratches = corrupted ? 9 : 3;
                for (int s = 0; s < scratches; s++)
                {
                    int sx = rng.Next(x0 + 18, xEnd - 18);
                    int sy0 = rng.Next(y0 + 10, y0 + h / 2);
                    int sy1 = Mathf.Min(yEnd - 10, sy0 + rng.Next(60, 220));
                    Color32 scCol = rng.NextDouble() > 0.5 ? ClampCol(198, 192, 180) : ClampCol(32, 30, 30);
                    DrawThinAlphaLine(sx, sy0, sx + rng.Next(-4, 5), sy1, scCol, 1.0f, corrupted ? 0.24f : 0.14f);
                }
            }

            public void ApplyDirtyPaperPatina(int seed, float grainAmount, float edgeGrime, float foldYNorm)
            {
                int foldY = foldYNorm > 0f ? Mathf.RoundToInt(Height * foldYNorm) : -100;
                for (int y = 0; y < Height; y++)
                {
                    float ny = y / (float)Mathf.Max(1, Height - 1);
                    float edgeY = Mathf.Min(ny, 1f - ny) * 2f;
                    float foldDist = Mathf.Abs(y - foldY);
                    float foldShade = (foldY > 0 && foldDist < 6f)
                        ? (foldDist < 2f ? -0.085f : 0.032f * (1f - foldDist / 6f))
                        : 0f;

                    int row = (Height - 1 - y) * Width;
                    for (int x = 0; x < Width; x++)
                    {
                        float nx = x / (float)Mathf.Max(1, Width - 1);
                        float edgeX = Mathf.Min(nx, 1f - nx) * 2f;
                        float edgeFactor = 1f - Mathf.Clamp01(Mathf.Min(edgeX, edgeY) / 0.28f);

                        float blotch = SmoothValueNoise(x * 0.018f, y * 0.018f, seed) * 0.65f
                                     + SmoothValueNoise(x * 0.055f, y * 0.055f, seed + 13) * 0.35f;
                        float fiber = (Hash2D(x, y, seed + 29) - 0.5f) * 2f * grainAmount;
                        float grimeMul = 1f - edgeFactor * edgeFactor * edgeGrime * (0.55f + 0.45f * blotch);
                        float ageShift = (blotch - 0.5f) * 0.07f;

                        Color32 src = pixels[row + x];
                        float r = (src.r / 255f) * grimeMul + ageShift + fiber + foldShade;
                        float g = (src.g / 255f) * grimeMul + ageShift * 0.86f + fiber + foldShade;
                        float b = (src.b / 255f) * grimeMul + ageShift * 0.58f + fiber + foldShade;

                        pixels[row + x] = new Color32(
                            (byte)Mathf.Clamp(Mathf.RoundToInt(r * 255f), 0, 255),
                            (byte)Mathf.Clamp(Mathf.RoundToInt(g * 255f), 0, 255),
                            (byte)Mathf.Clamp(Mathf.RoundToInt(b * 255f), 0, 255),
                            255);
                    }
                }
            }

            private static float SmoothValueNoise(float x, float y, int seed)
            {
                int ix = Mathf.FloorToInt(x);
                int iy = Mathf.FloorToInt(y);
                float fx = x - ix;
                float fy = y - iy;
                fx = fx * fx * (3f - 2f * fx);
                fy = fy * fy * (3f - 2f * fy);

                float n00 = Hash2D(ix, iy, seed);
                float n10 = Hash2D(ix + 1, iy, seed);
                float n01 = Hash2D(ix, iy + 1, seed);
                float n11 = Hash2D(ix + 1, iy + 1, seed);
                return Mathf.Lerp(Mathf.Lerp(n00, n10, fx), Mathf.Lerp(n01, n11, fx), fy);
            }

            public void DrawRectOutline(int x, int y, int w, int h, int thickness, Color32 col)
            {
                FillRect(x, y, w, thickness, col);
                FillRect(x, y + h - thickness, w, thickness, col);
                FillRect(x, y, thickness, h, col);
                FillRect(x + w - thickness, y, thickness, h, col);
            }

            public void FillCircle(int cx, int cy, int r, Color32 col)
            {
                int r2 = r * r;
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (dx * dx + dy * dy <= r2)
                            SetPixelTopDown(cx + dx, cy + dy, col);
                    }
                }
            }

            public void DrawCircleOutline(int cx, int cy, int r, int thickness, Color32 col)
            {
                int outer2 = r * r;
                int inner = Mathf.Max(0, r - thickness);
                int inner2 = inner * inner;
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int d2 = dx * dx + dy * dy;
                        if (d2 <= outer2 && d2 >= inner2)
                            SetPixelTopDown(cx + dx, cy + dy, col);
                    }
                }
            }

            public void DrawLine(int x0, int y0, int x1, int y1, Color32 col, int thickness = 2)
            {
                int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
                if (steps == 0)
                {
                    FillCircle(x0, y0, Mathf.Max(1, thickness / 2), col);
                    return;
                }

                int rad = Mathf.Max(0, thickness / 2);
                for (int s = 0; s <= steps; s++)
                {
                    float t = (float)s / steps;
                    int px = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                    int py = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                    if (rad == 0)
                        SetPixelTopDown(px, py, col);
                    else
                        FillRect(px - rad, py - rad, thickness, thickness, col);
                }
            }

            public void DrawTriangleFilled(int x0, int y0, int x1, int y1, int x2, int y2, Color32 col)
            {
                int minX = Mathf.Min(x0, Mathf.Min(x1, x2));
                int maxX = Mathf.Max(x0, Mathf.Max(x1, x2));
                int minY = Mathf.Min(y0, Mathf.Min(y1, y2));
                int maxY = Mathf.Max(y0, Mathf.Max(y1, y2));

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        float d1 = EdgeSign(x, y, x0, y0, x1, y1);
                        float d2 = EdgeSign(x, y, x1, y1, x2, y2);
                        float d3 = EdgeSign(x, y, x2, y2, x0, y0);
                        bool hasNeg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
                        bool hasPos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
                        if (!(hasNeg && hasPos))
                            SetPixelTopDown(x, y, col);
                    }
                }
            }

            private static float EdgeSign(int px, int py, int ax, int ay, int bx, int by)
            {
                return (px - bx) * (float)(ay - by) - (ax - bx) * (float)(py - by);
            }

            public void DrawCenteredText(int centerX, int topY, string text, int scale, Color32 col)
            {
                if (string.IsNullOrEmpty(text)) return;
                int totalWidth = text.Length * (6 * scale) - scale;
                DrawText(centerX - totalWidth / 2, topY, text, scale, col);
            }

            public void DrawText(int startX, int topY, string text, int scale, Color32 col)
            {
                if (string.IsNullOrEmpty(text)) return;
                int cursorX = startX;
                for (int i = 0; i < text.Length; i++)
                {
                    char ch = NormalizeChar(char.ToUpperInvariant(text[i]));
                    if (BitmapFont5x7.TryGetValue(ch, out string[] glyph))
                    {
                        for (int gy = 0; gy < 7; gy++)
                        {
                            string row = glyph[gy];
                            for (int gx = 0; gx < 5; gx++)
                            {
                                if (row[gx] == '#')
                                    FillRect(cursorX + gx * scale, topY + gy * scale, scale, scale, col);
                            }
                        }
                    }
                    cursorX += 6 * scale;
                }
            }

            private static char NormalizeChar(char ch)
            {
                return ch switch
                {
                    'Á' or 'À' or 'Â' or 'Ã' => 'A',
                    'É' or 'Ê' => 'E',
                    'Í' => 'I',
                    'Ó' or 'Ô' or 'Õ' => 'O',
                    'Ú' => 'U',
                    'Ç' => 'C',
                    'º' => 'O',
                    _ => ch
                };
            }

            public Texture2D ToTexture2D(string name)
            {
                Texture2D tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                tex.name = name;
                tex.SetPixels32(pixels);
                tex.Apply(false, false);
                return tex;
            }
        }

        private static readonly Dictionary<char, string[]> BitmapFont5x7 = new Dictionary<char, string[]>
        {
            [' '] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." },
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
            ['C'] = new[] { ".####", "#....", "#....", "#....", "#....", "#....", ".####" },
            ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
            ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
            ['G'] = new[] { ".####", "#....", "#....", "#.###", "#...#", "#...#", ".####" },
            ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['I'] = new[] { ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['J'] = new[] { "..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.." },
            ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
            ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
            ['M'] = new[] { "#...#", "##.##", "#.#.#", "#...#", "#...#", "#...#", "#...#" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
            ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
            ['Q'] = new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" },
            ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." },
            ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" },
            ['X'] = new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" },
            ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
            ['Z'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },
            ['0'] = new[] { ".###.", "#..##", "#.#.#", "#.#.#", "##..#", "#...#", ".###." },
            ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['2'] = new[] { ".###.", "#...#", "....#", "..##.", ".#...", "#....", "#####" },
            ['3'] = new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." },
            ['4'] = new[] { "#...#", "#...#", "#...#", "#####", "....#", "....#", "....#" },
            ['5'] = new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
            ['6'] = new[] { ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###." },
            ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
            ['8'] = new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
            ['9'] = new[] { ".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##.." },
            ['-'] = new[] { ".....", ".....", ".....", ".###.", ".....", ".....", "....." },
            ['+'] = new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." },
            ['/'] = new[] { "....#", "...#.", "...#.", "..#..", ".#...", ".#...", "#...." },
            [':'] = new[] { ".....", "..#..", "..#..", ".....", "..#..", "..#..", "....." },
            ['.'] = new[] { ".....", ".....", ".....", ".....", ".....", "..#..", "..#.." },
            [','] = new[] { ".....", ".....", ".....", ".....", ".....", "..#..", ".#..." },
            ['('] = new[] { "...#.", "..#..", ".#...", ".#...", ".#...", "..#..", "...#." },
            [')'] = new[] { ".#...", "..#..", "...#.", "...#.", "...#.", "..#..", ".#..." },
            ['%'] = new[] { "##..#", "##.#.", "..#..", ".#...", "#.##.", "#..##", "....." }
        };
    }
}
#endif
