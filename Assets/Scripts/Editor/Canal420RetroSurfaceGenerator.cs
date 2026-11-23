#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Canal420.EditorTools
{
    internal struct RetroSurfaceTextures
    {
        public Texture2D plaster;
        public Texture2D floorWood;
        public Texture2D woodGrain;
        public Texture2D tileGrid;
        public Texture2D sofaUpholstery;
        public Texture2D rugCarpet;
        public Texture2D bedQuilt;
        public Texture2D curtainLinen;
        public Texture2D woolCoat;
        public Texture2D mattePaperCarpet;
    }

    /// <summary>
    /// Generates distinct, seamless, low-resolution (128x128) retro PS1/VHS diffuse surface textures
    /// tailored per physical material category (plaster, floorboards, wood veneer, ceramic tile,
    /// woven upholstery, tufted rug, quilted bed blanket, draped curtain linen, brushed wool cloth, matte paper).
    /// All textures are strictly normalized to linear 0.50 mean so LowPolyDirtyLit preserves room exposure
    /// without requiring complex PBR maps.
    /// </summary>
    internal static class Canal420RetroSurfaceGenerator
    {
        public const string Folder = "Assets/Textures/RetroSurface";
        private const int Size = 128;

        public static RetroSurfaceTextures GenerateAllAndLoad()
        {
            EnsureFolder();

            Texture2D plaster = SaveLinearTilePng("Tex_Retro_Plaster", BuildPlasterField());
            Texture2D floorWood = SaveLinearTilePng("Tex_Retro_FloorWood", BuildFloorWoodField());
            Texture2D woodGrain = SaveLinearTilePng("Tex_Retro_WoodGrain", BuildWoodGrainField());
            Texture2D tileGrid = SaveLinearTilePng("Tex_Retro_TileGrid", BuildTileGridField());
            Texture2D sofaUpholstery = SaveLinearTilePng("Tex_Retro_SofaUpholstery", BuildSofaUpholsteryField());
            Texture2D rugCarpet = SaveLinearTilePng("Tex_Retro_RugCarpet", BuildRugCarpetField());
            Texture2D bedQuilt = SaveLinearTilePng("Tex_Retro_BedQuilt", BuildBedQuiltField());
            Texture2D curtainLinen = SaveLinearTilePng("Tex_Retro_CurtainLinen", BuildCurtainLinenField());
            Texture2D woolCoat = SaveLinearTilePng("Tex_Retro_WoolCoat", BuildWoolCoatField());
            Texture2D mattePaper = SaveLinearTilePng("Tex_Retro_MattePaperCarpet", BuildMattePaperCarpetField());

            // Keep legacy Tex_Retro_FabricWeave updated cleanly with soft upholstery if referenced anywhere
            SaveLinearTilePng("Tex_Retro_FabricWeave", BuildSofaUpholsteryField());

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            return new RetroSurfaceTextures
            {
                plaster = plaster,
                floorWood = floorWood,
                woodGrain = woodGrain,
                tileGrid = tileGrid,
                sofaUpholstery = sofaUpholstery,
                rugCarpet = rugCarpet,
                bedQuilt = bedQuilt,
                curtainLinen = curtainLinen,
                woolCoat = woolCoat,
                mattePaperCarpet = mattePaper
            };
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Textures"))
                AssetDatabase.CreateFolder("Assets", "Textures");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Textures", "RetroSurface");
        }

        private static Texture2D SaveLinearTilePng(string fileName, float[] values)
        {
            NormalizeMeanToHalf(values);

            Color32[] pixels = new Color32[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)Mathf.Clamp(Mathf.RoundToInt(values[i] * 255f), 0, 255);
                pixels[i] = new Color32(v, v, v, 255);
            }

            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, linear: true);
            tex.name = fileName;
            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string assetPath = $"{Folder}/{fileName}.png";
            string fullPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
            File.WriteAllBytes(fullPath, png);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; dirty = true; }
                if (importer.sRGBTexture) { importer.sRGBTexture = false; dirty = true; }
                if (importer.alphaSource != TextureImporterAlphaSource.None) { importer.alphaSource = TextureImporterAlphaSource.None; dirty = true; }
                if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; dirty = true; }
                if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
                if (importer.wrapMode != TextureWrapMode.Repeat) { importer.wrapMode = TextureWrapMode.Repeat; dirty = true; }
                if (importer.maxTextureSize != Size) { importer.maxTextureSize = Size; dirty = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
                if (dirty) importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static void NormalizeMeanToHalf(float[] data)
        {
            double sum = 0.0;
            for (int i = 0; i < data.Length; i++)
                sum += data[i];
            float delta = 0.50f - (float)(sum / data.Length);
            for (int i = 0; i < data.Length; i++)
                data[i] = Mathf.Clamp(data[i] + delta, 0.06f, 0.94f);
        }

        private static float TileNoise(int x, int y, int periodX, int periodY, int seed)
        {
            int ix = ((x % periodX) + periodX) % periodX;
            int iy = ((y % periodY) + periodY) % periodY;
            unchecked
            {
                uint h = (uint)(ix * 374761393 + iy * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static float SmoothTileNoise(float u, float v, int cellsX, int cellsY, int seed)
        {
            float fx = u * cellsX;
            float fy = v * cellsY;
            int x0 = Mathf.FloorToInt(fx);
            int y0 = Mathf.FloorToInt(fy);
            float tx = fx - x0;
            float ty = fy - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);

            float n00 = TileNoise(x0, y0, cellsX, cellsY, seed);
            float n10 = TileNoise(x0 + 1, y0, cellsX, cellsY, seed);
            float n01 = TileNoise(x0, y0 + 1, cellsX, cellsY, seed);
            float n11 = TileNoise(x0 + 1, y0 + 1, cellsX, cellsY, seed);

            float nx0 = Mathf.Lerp(n00, n10, tx);
            float nx1 = Mathf.Lerp(n01, n11, tx);
            return Mathf.Lerp(nx0, nx1, ty);
        }

        // 1. Matte roller-painted 1990s Brazilian plaster wall (fine stucco stipple + soft aged clouding)
        internal static float[] BuildPlasterField()
        {
            float[] f = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                float v = y / (float)Size;
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float cloud = SmoothTileNoise(u, v, 3, 3, 101) * 0.45f
                                + SmoothTileNoise(u, v, 7, 7, 103) * 0.35f
                                + SmoothTileNoise(u, v, 16, 16, 107) * 0.20f;
                    float stipple = SmoothTileNoise(u, v, 32, 32, 109) - 0.5f;
                    float microSand = TileNoise(x, y, Size, Size, 113) - 0.5f;

                    f[y * Size + x] = 0.50f
                                    + (cloud - 0.5f) * 0.28f
                                    + stipple * 0.16f
                                    + microSand * 0.14f;
                }
            }
            return f;
        }

        // 2. Staggered Brazilian hardwood floorboards (tábua corrida) with clean bevel joints & fine wood fiber
        internal static float[] BuildFloorWoodField()
        {
            float[] f = new float[Size * Size];
            const int plankRows = 4;
            int rowH = Size / plankRows;

            for (int y = 0; y < Size; y++)
            {
                float v = y / (float)Size;
                int row = y / rowH;
                int localY = y % rowH;
                int jointX = ((row * 49 + 21) % Size);
                float rowTone = (TileNoise(row, 1, 16, 16, 201) - 0.5f) * 0.22f;

                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    bool isHorizSeam = localY == 0;
                    bool isHorizBevel = localY == 1 || localY == rowH - 1;
                    int dxJoint = Mathf.Min(Mathf.Abs(x - jointX), Size - Mathf.Abs(x - jointX));
                    bool isVertSeam = dxJoint == 0;

                    float segTone = (x >= jointX) ? 0.055f : -0.055f;
                    float warp = (SmoothTileNoise(u, v, 4, 8, 209) - 0.5f) * 0.045f;
                    float grain = SmoothTileNoise(u, v + warp, 3, 28, 211) * 0.62f
                                + SmoothTileNoise(u, v, 6, 56, 217) * 0.38f;
                    float pore = TileNoise(x, y, Size, Size, 227) - 0.5f;

                    float val = 0.52f + rowTone + segTone
                              + (grain - 0.5f) * 0.26f
                              + pore * 0.05f;

                    if (isHorizSeam) val -= 0.34f;
                    else if (isVertSeam) val -= 0.26f;
                    else if (isHorizBevel) val -= 0.06f;

                    f[y * Size + x] = val;
                }
            }
            return f;
        }

        // 3. Crisp vertical imbuia/walnut wood veneer grain for doors, cabinets, tables, baseboards
        internal static float[] BuildWoodGrainField()
        {
            float[] f = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                float v = y / (float)Size;
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float warp = (SmoothTileNoise(u, v, 3, 2, 307) - 0.5f) * 0.065f
                               + (SmoothTileNoise(u, v, 7, 4, 309) - 0.5f) * 0.025f;
                    float uw = u + warp;
                    float ringWave = Mathf.Sin(uw * Mathf.PI * 18f + Mathf.Sin(v * Mathf.PI * 2f) * 0.65f);
                    // Sharper latewood bands with smoother earlywood between them
                    float ring = Mathf.Pow(0.5f + 0.5f * ringWave, 1.55f);
                    float fineFiber = SmoothTileNoise(uw, v, 56, 6, 311);
                    float fineStreak = (TileNoise(x, y / 4, Size, Size / 4, 317) - 0.5f) * 0.09f;
                    float microPore = (TileNoise(x, y, Size, Size, 313) - 0.5f) * 0.055f;

                    f[y * Size + x] = 0.50f
                                    + (ring - 0.45f) * 0.24f
                                    + (fineFiber - 0.5f) * 0.18f
                                    + fineStreak
                                    + microPore;
                }
            }
            return f;
        }

        // 4. Clean 4x4 1990s ceramic square tile grid with thin recessed grout & subtle porcelain glaze
        internal static float[] BuildTileGridField()
        {
            float[] f = new float[Size * Size];
            const int tiles = 4;
            int cell = Size / tiles;

            for (int y = 0; y < Size; y++)
            {
                float v = y / (float)Size;
                int ty = y / cell;
                int ly = y % cell;
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    int tx = x / cell;
                    int lx = x % cell;

                    bool grout = lx == 0 || ly == 0;
                    bool bevel = !grout && (lx == 1 || ly == 1 || lx == cell - 1 || ly == cell - 1);

                    float tileTone = (TileNoise(tx, ty, tiles, tiles, 401) - 0.5f) * 0.09f;
                    float glaze = (SmoothTileNoise(u, v, 6, 6, 409) - 0.5f) * 0.12f;
                    float micro = (TileNoise(x, y, Size, Size, 419) - 0.5f) * 0.035f;

                    float val = 0.53f + tileTone + glaze + micro;
                    if (grout) val = 0.18f + micro * 0.5f;
                    else if (bevel) val -= 0.05f;

                    f[y * Size + x] = val;
                }
            }
            return f;
        }

        // 5. Genuine soft woven sofa/armchair chenille-tweed upholstery (fine irregular micro-threads, zero blocky zigzag)
        internal static float[] BuildSofaUpholsteryField()
        {
            float[] f = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                float v = y / (float)Size;
                float weftSlub = (TileNoise(1, y, 4, Size, 521) - 0.5f) * 0.13f;
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float warpSlub = (TileNoise(x, 1, Size, 4, 523) - 0.5f) * 0.11f;

                    // Authentic fine 2px plain linen/chenille weave modulated by organic slub threads
                    bool cross = ((x + y) & 1) == 0;
                    float microWeave = cross ? 0.045f : -0.045f;
                    float softWarp = (SmoothTileNoise(u, v, 48, 8, 531) - 0.5f) * 0.14f;
                    float softWeft = (SmoothTileNoise(u, v, 8, 48, 541) - 0.5f) * 0.14f;
                    float clothWear = (SmoothTileNoise(u, v, 5, 5, 547) - 0.5f) * 0.22f;
                    float nap = (TileNoise(x, y, Size, Size, 557) - 0.5f) * 0.08f;

                    f[y * Size + x] = 0.50f + microWeave + weftSlub * 0.6f + warpSlub * 0.6f
                                    + softWarp + softWeft + clothWear + nap;
                }
            }
            return f;
        }

        // 6. Authentic low-pile tufted/braided woven rug carpet (distinct horizontal yarn rows + pile tuft speckle)
        internal static float[] BuildRugCarpetField()
        {
            float[] f = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                float v = y / (float)Size;
                int rowBand = y % 4;
                float braidedRow = rowBand == 0 ? -0.10f : (rowBand == 2 ? 0.08f : 0.01f);
                float rowShade = (TileNoise(3, y / 4, 8, Size / 4, 601) - 0.5f) * 0.10f;
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    int knot = (x + (y / 4) * 2) % 6;
                    float knotBump = (knot == 0 ? -0.07f : (knot == 3 ? 0.06f : 0f));
                    float pileNoise = (SmoothTileNoise(u, v, 32, 32, 607) - 0.5f) * 0.18f;
                    float trafficWear = (SmoothTileNoise(u, v, 4, 4, 613) - 0.5f) * 0.22f;
                    float fuzz = (TileNoise(x, y, Size, Size, 617) - 0.5f) * 0.14f;

                    f[y * Size + x] = 0.50f + braidedRow + rowShade + knotBump + pileNoise + trafficWear + fuzz;
                }
            }
            return f;
        }

        // 7. Soft quilted cotton bed blanket / bedspread (subtle stitched diamond quilting + gentle fabric folds)
        internal static float[] BuildBedQuiltField()
        {
            float[] f = new float[Size * Size];
            const int cell = 32;
            for (int y = 0; y < Size; y++)
            {
                float v = y / (float)Size;
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    // Diagonal quilt seam coordinates (seamless on 128x128 with period 32)
                    int d1 = ((x + y) % cell + cell) % cell;
                    int d2 = ((x - y) % cell + cell) % cell;
                    float dist1 = Mathf.Min(d1, cell - d1) / (cell * 0.5f); // 0 at seam, 1 at diamond center
                    float dist2 = Mathf.Min(d2, cell - d2) / (cell * 0.5f);
                    float seamDist = Mathf.Min(dist1, dist2);

                    // Soft pillowing puff inside each diamond + symmetric dashed stitch along BOTH diagonals
                    float puff = Mathf.Sin(Mathf.Clamp01(dist1) * Mathf.PI * 0.5f)
                               * Mathf.Sin(Mathf.Clamp01(dist2) * Mathf.PI * 0.5f);
                    int alongCoord = dist1 < dist2 ? (x - y) : (x + y);
                    bool stitchDash = ((alongCoord & 3) < 2);
                    float stitchCrease = seamDist < 0.075f ? (stitchDash ? -0.14f : -0.045f) : 0f;

                    float wrinkle = (SmoothTileNoise(u, v, 5, 5, 701) - 0.5f) * 0.24f;
                    float cotton = (SmoothTileNoise(u, v, 36, 36, 709) - 0.5f) * 0.10f
                                 + (TileNoise(x, y, Size, Size, 719) - 0.5f) * 0.055f;

                    f[y * Size + x] = 0.45f + puff * 0.14f + stitchCrease + wrinkle + cotton;
                }
            }
            return f;
        }

        // 8. Vertical woven linen curtain drape (soft vertical gravity folds + fine vertical slub threads)
        internal static float[] BuildCurtainLinenField()
        {
            float[] f = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                float v = y / (float)Size;
                float crossThread = (TileNoise(2, y, 4, Size, 809) - 0.5f) * 0.06f;
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float verticalFold = Mathf.Sin(u * Mathf.PI * 6f + (SmoothTileNoise(u, v, 4, 4, 811) - 0.5f) * 0.9f) * 0.14f;
                    float verticalSlub = (SmoothTileNoise(u, v, 42, 6, 821) - 0.5f) * 0.22f;
                    float linenWeave = ((x & 1) == 0 ? 0.03f : -0.03f) + crossThread;
                    float grain = (TileNoise(x, y, Size, Size, 827) - 0.5f) * 0.05f;

                    f[y * Size + x] = 0.50f + verticalFold + verticalSlub + linenWeave + grain;
                }
            }
            return f;
        }

        // 9. Brushed heathered wool / worn cotton garment cloth (for Margaret's coat, shirts, trousers, nightgown)
        internal static float[] BuildWoolCoatField()
        {
            float[] f = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                float v = y / (float)Size;
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    // Subtle diagonal twill + heathered wool fleece mottling
                    int twill = ((x + y) % 3);
                    float twillStep = twill == 0 ? -0.045f : (twill == 1 ? 0.035f : 0.01f);
                    float heather = (SmoothTileNoise(u, v, 14, 14, 907) - 0.5f) * 0.20f
                                  + (SmoothTileNoise(u, v, 32, 32, 911) - 0.5f) * 0.14f;
                    float drape = (SmoothTileNoise(u, v, 4, 6, 919) - 0.5f) * 0.18f;
                    float woolFiber = (TileNoise(x, y, Size, Size, 929) - 0.5f) * 0.10f;

                    f[y * Size + x] = 0.50f + twillStep + heather + drape + woolFiber;
                }
            }
            return f;
        }

        // 10. Delicate matte fibrous paper / kraft folder texture
        internal static float[] BuildMattePaperCarpetField()
        {
            float[] f = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                float v = y / (float)Size;
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float pulp = (SmoothTileNoise(u, v, 8, 8, 1009) - 0.5f) * 0.16f
                               + (SmoothTileNoise(u, v, 24, 24, 1013) - 0.5f) * 0.12f;
                    float fiber = (TileNoise(x, y, Size, Size, 1019) - 0.5f) * 0.12f;

                    f[y * Size + x] = 0.50f + pulp + fiber;
                }
            }
            return f;
        }
    }
}
#endif
