using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Advanced Map_Cloudy decorator. Places terrain-aware foliage, flower gardens,
/// a lake, campfire areas, medieval market stalls, and farm crop patches.
/// All items are tinted with a cloudy orange theme via MaterialPropertyBlock.
/// </summary>
public static class CloudyMapDecorator
{
    // ─── Cloudy theme tint ───────────────────────────────────────────────────
    static readonly Color CLOUDY_TINT = new Color(1.0f, 0.55f, 0.15f); // warm orange

    // ─── Terrain helpers ────────────────────────────────────────────────────
    static Terrain _terrain;
    static TerrainData _td;

    static void InitTerrain()
    {
        _terrain = Terrain.activeTerrain;
        _td = _terrain != null ? _terrain.terrainData : null;
    }

    /// <summary>Returns true if the position is mostly on grass (not dirt path).</summary>
    static bool IsGrass(Vector3 pos)
    {
        if (_terrain == null || _td == null) return true;
        Vector3 tp = _terrain.transform.position;
        float nx = (pos.x - tp.x) / _td.size.x;
        float nz = (pos.z - tp.z) / _td.size.z;
        int mx = Mathf.Clamp(Mathf.RoundToInt(nx * _td.alphamapWidth),  0, _td.alphamapWidth  - 1);
        int mz = Mathf.Clamp(Mathf.RoundToInt(nz * _td.alphamapHeight), 0, _td.alphamapHeight - 1);
        float[,,] alpha = _td.GetAlphamaps(mx, mz, 1, 1);
        // Layer 1 = dirt path; reject if > 30 %
        float dirtWeight = (alpha.GetLength(2) > 1) ? alpha[0, 0, 1] : 0f;
        return dirtWeight < 0.3f;
    }

    /// <summary>Sample terrain height at XZ.</summary>
    static float GroundY(float x, float z) =>
        _terrain != null ? _terrain.SampleHeight(new Vector3(x, 0, z)) : 0f;

    // ─── House bounds (to keep a clear zone) ────────────────────────────────
    static Bounds GetHouseBounds()
    {
        GameObject house = GameObject.Find("Baker_house");
        if (house == null) return new Bounds(Vector3.zero, Vector3.zero);
        Bounds b = new Bounds(house.transform.position, Vector3.zero);
        foreach (var r in house.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        b.Expand(6f); // safety margin
        return b;
    }

    // ─── Collider overlap check ──────────────────────────────────────────────
    static bool HasOverlap(Vector3 pos, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(pos, radius);
        foreach (var h in hits)
        {
            string n = h.name.ToLower();
            if (n.Contains("house") || n.Contains("fence") || n.Contains("path")) return true;
        }
        return false;
    }

    // ─── Placement helper ────────────────────────────────────────────────────
    static bool TryGetGrassPos(float minR, float maxR, Bounds houseBounds, out Vector3 pos)
    {
        pos = Vector3.zero;
        for (int t = 0; t < 15; t++)
        {
            Vector2 c = Random.insideUnitCircle.normalized * Random.Range(minR, maxR);
            float y = GroundY(c.x, c.y);
            pos = new Vector3(c.x, y, c.y);
            if (houseBounds.Contains(pos)) continue;
            if (!IsGrass(pos)) continue;
            if (HasOverlap(pos, 1.5f)) continue;
            return true;
        }
        return false;
    }

    // ─── Tinting ────────────────────────────────────────────────────────────
    static void ApplyTint(GameObject go, Color tint)
    {
        if (tint == Color.white) return;
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", tint);
            mpb.SetColor("_Color", tint);
            r.SetPropertyBlock(mpb);
        }
    }

    // ─── Generic spawn ───────────────────────────────────────────────────────
    static GameObject Spawn(string prefabPath, Transform parent, Vector3 pos,
                            float yRot = 0f, float scale = 1f, Color? tint = null)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogWarning("[Decorator] Missing: " + prefabPath); return null; }
        var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, yRot, 0);
        go.transform.localScale = Vector3.one * scale;
        ApplyTint(go, tint ?? CLOUDY_TINT);
        return go;
    }

    // ════════════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Decorate Map Cloudy (Full)")]
    public static void DecorateCloudyMap()
    {
        InitTerrain();
        Bounds houseBounds = GetHouseBounds();

        // Root container
        GameObject root = new GameObject("Environment_Cloudy");

        int placed = 0;

        // ── 1. FOLIAGE (trees, bushes, rocks, mushrooms) ──────────────────
        placed += SpawnFoliage(root, houseBounds);

        // ── 2. FLOWER GARDENS ─────────────────────────────────────────────
        placed += SpawnFlowerGardens(root, houseBounds);

        // ── 3. LAKE (water tile cluster) ──────────────────────────────────
        placed += SpawnLake(root);

        // ── 4. CAMPFIRE AREA ──────────────────────────────────────────────
        placed += SpawnCampfireArea(root, houseBounds);

        // ── 5. FARM PATCH (crops near house perimeter) ────────────────────
        placed += SpawnFarmPatch(root, houseBounds);

        // ── 6. MARKET STALLS ─────────────────────────────────────────────
        placed += SpawnMarketStalls(root, houseBounds);

        // ── 7. HAYSTACKS & BARRELS (scattered props) ──────────────────────
        placed += SpawnScatteredProps(root, houseBounds);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[CloudyMapDecorator] Done! Placed {placed} objects total.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // SECTION 1 – FOLIAGE
    // ════════════════════════════════════════════════════════════════════════
    static int SpawnFoliage(GameObject root, Bounds houseBounds)
    {
        var parent = new GameObject("Foliage").transform;
        parent.SetParent(root.transform);

        string[] treePaths = {
            "Assets/SimpleNaturePack/Prefabs/Tree_01.prefab",
            "Assets/SimpleNaturePack/Prefabs/Tree_02.prefab",
            "Assets/SimpleNaturePack/Prefabs/Tree_03.prefab",
            "Assets/SimpleNaturePack/Prefabs/Tree_04.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/Tree_01_Fall.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/Tree_03_Fall.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/Tree_05_Fall.prefab",
        };
        string[] groundPaths = {
            "Assets/SimpleNaturePack/Prefabs/Bush_01.prefab",
            "Assets/SimpleNaturePack/Prefabs/Rock_01.prefab",
            "Assets/SimpleNaturePack/Prefabs/Rock_03.prefab",
            "Assets/SimpleNaturePack/Prefabs/Mushroom_01.prefab",
            "Assets/SimpleNaturePack/Prefabs/Grass_01.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/Flower_01.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/Flower_05.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/Mashroom_01.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/SoftRock_01.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/Log_01.prefab",
        };

        int count = 0;
        // Trees – sparse
        for (int i = 0; i < 200; i++)
        {
            if (count >= 80) break;
            if (!TryGetGrassPos(14f, 65f, houseBounds, out Vector3 pos)) continue;
            float s = Random.Range(0.7f, 1.3f);
            string path = treePaths[Random.Range(0, treePaths.Length)];
            if (Spawn(path, parent, pos, Random.Range(0f, 360f), s) != null) count++;
        }
        // Ground cover – denser
        for (int i = 0; i < 500; i++)
        {
            if (count >= 200) break;
            if (!TryGetGrassPos(10f, 65f, houseBounds, out Vector3 pos)) continue;
            float s = Random.Range(0.5f, 1.2f);
            string path = groundPaths[Random.Range(0, groundPaths.Length)];
            if (Spawn(path, parent, pos, Random.Range(0f, 360f), s) != null) count++;
        }
        return count;
    }

    // ════════════════════════════════════════════════════════════════════════
    // SECTION 2 – FLOWER GARDENS (3 circular clusters)
    // ════════════════════════════════════════════════════════════════════════
    static int SpawnFlowerGardens(GameObject root, Bounds houseBounds)
    {
        var parent = new GameObject("FlowerGardens").transform;
        parent.SetParent(root.transform);

        string[] flowerPaths = {
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/Flower_02.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/Flower_06.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/Flower_10.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/Flower_14.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/Flower_18.prefab",
            "Assets/SimpleNaturePack/Prefabs/Flowers_01.prefab",
            "Assets/SimpleNaturePack/Prefabs/Flowers_02.prefab",
        };

        // Garden center positions (on opposite corners of the map)
        Vector3[] centers = {
            new Vector3(20f, 0, 20f),
            new Vector3(-25f, 0, 15f),
            new Vector3(10f, 0, 35f),
        };

        int count = 0;
        foreach (var center in centers)
        {
            // Replace Y with terrain height
            Vector3 c = new Vector3(center.x, GroundY(center.x, center.z), center.z);
            if (houseBounds.Contains(c)) continue;

            for (int i = 0; i < 30; i++)
            {
                Vector2 r2 = Random.insideUnitCircle * 5f;
                float x = c.x + r2.x, z = c.z + r2.y;
                float y = GroundY(x, z);
                Vector3 pos = new Vector3(x, y, z);
                if (!IsGrass(pos)) continue;
                string path = flowerPaths[Random.Range(0, flowerPaths.Length)];
                if (Spawn(path, parent, pos, Random.Range(0f, 360f), Random.Range(0.4f, 0.8f)) != null)
                    count++;
            }
        }
        return count;
    }

    // ════════════════════════════════════════════════════════════════════════
    // SECTION 3 – LAKE
    // ════════════════════════════════════════════════════════════════════════
    static int SpawnLake(GameObject root)
    {
        var parent = new GameObject("Lake").transform;
        parent.SetParent(root.transform);

        string[] waterTiles = {
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/TileWater_01.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/TileWater_02.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/TileWater_03.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/TileWater_04.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/TileWater_05.prefab",
        };

        // Lake center far from house
        Vector3 lakeCenter = new Vector3(35f, 0f, 30f);
        lakeCenter.y = GroundY(lakeCenter.x, lakeCenter.z) - 0.3f; // slightly sunken

        int count = 0;
        // 3x3 grid of water tiles
        for (int xi = -2; xi <= 2; xi++)
        {
            for (int zi = -2; zi <= 2; zi++)
            {
                Vector3 pos = new Vector3(lakeCenter.x + xi * 3f, lakeCenter.y, lakeCenter.z + zi * 3f);
                string path = waterTiles[Random.Range(0, waterTiles.Length)];
                if (Spawn(path, parent, pos, 0f, 1f, Color.white) != null) count++;
            }
        }

        // Surrounding reeds / rocks
        string[] bankDeco = {
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/Foliage_01.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Nature Environment Pack/Prefabs/SoftRock_05.prefab",
            "Assets/SimpleNaturePack/Prefabs/Rock_02.prefab",
        };
        for (int i = 0; i < 16; i++)
        {
            float angle = i * (360f / 16f);
            float rad = Random.Range(7f, 10f);
            float x = lakeCenter.x + Mathf.Cos(angle * Mathf.Deg2Rad) * rad;
            float z = lakeCenter.z + Mathf.Sin(angle * Mathf.Deg2Rad) * rad;
            float y = GroundY(x, z);
            string path = bankDeco[Random.Range(0, bankDeco.Length)];
            if (Spawn(path, parent, new Vector3(x, y, z), Random.Range(0f, 360f), Random.Range(0.6f, 1.2f)) != null)
                count++;
        }
        return count;
    }

    // ════════════════════════════════════════════════════════════════════════
    // SECTION 4 – CAMPFIRE AREA
    // ════════════════════════════════════════════════════════════════════════
    static int SpawnCampfireArea(GameObject root, Bounds houseBounds)
    {
        var parent = new GameObject("CampfireArea").transform;
        parent.SetParent(root.transform);

        // Find a clear spot
        Vector3 campPos = new Vector3(-20f, 0, 20f);
        campPos.y = GroundY(campPos.x, campPos.z);

        int count = 0;
        // Campfire
        if (Spawn("Assets/URP GanzSe Free Camping Props/Prefabs/FCP_Campfire_Type1_Color1.prefab", parent, campPos, 0f, 1.2f, Color.white) != null) count++;

        // Tent
        Vector3 tentPos = new Vector3(campPos.x + 4f, GroundY(campPos.x + 4f, campPos.z + 2f), campPos.z + 2f);
        if (Spawn("Assets/URP GanzSe Free Camping Props/Prefabs/FCP_Tent_Type1_Color1.prefab", parent, tentPos, 45f, 1.2f, Color.white) != null) count++;

        // Stools around fire
        string[] campProps = {
            "Assets/URP GanzSe Free Camping Props/Prefabs/FCP_CampStool_Type1_Color1.prefab",
            "Assets/URP GanzSe Free Camping Props/Prefabs/FCP_Barrel_Type1_Color1.prefab",
            "Assets/URP GanzSe Free Camping Props/Prefabs/FCP_WoodenCrate_Type1__Color1.prefab",
        };
        for (int i = 0; i < 5; i++)
        {
            float ang = i * 72f;
            float r = Random.Range(2f, 3.5f);
            float x = campPos.x + Mathf.Cos(ang * Mathf.Deg2Rad) * r;
            float z = campPos.z + Mathf.Sin(ang * Mathf.Deg2Rad) * r;
            string path = campProps[Random.Range(0, campProps.Length)];
            if (Spawn(path, parent, new Vector3(x, GroundY(x, z), z), ang + 90f, 0.9f, Color.white) != null) count++;
        }

        // Logs & lantern
        if (Spawn("Assets/URP GanzSe Free Camping Props/Prefabs/FCP_BundleOfWood_Type1_Color1.prefab", parent,
            new Vector3(campPos.x - 3f, GroundY(campPos.x - 3f, campPos.z - 1f), campPos.z - 1f), 30f, 1f, Color.white) != null) count++;
        if (Spawn("Assets/URP GanzSe Free Camping Props/Prefabs/FCP_Lantern_Type1_Color1.prefab", parent,
            new Vector3(campPos.x + 1f, GroundY(campPos.x + 1f, campPos.z - 2.5f), campPos.z - 2.5f), 0f, 1f, Color.white) != null) count++;

        return count;
    }

    // ════════════════════════════════════════════════════════════════════════
    // SECTION 5 – FARM PATCH
    // ════════════════════════════════════════════════════════════════════════
    static int SpawnFarmPatch(GameObject root, Bounds houseBounds)
    {
        var parent = new GameObject("FarmPatch").transform;
        parent.SetParent(root.transform);

        string[] crops = {
            "Assets/Cartoon_Farm_Crops/Prefabs/Standard/Corn_Plant.prefab",
            "Assets/Cartoon_Farm_Crops/Prefabs/Standard/Pumpkin_Plant.prefab",
            "Assets/Cartoon_Farm_Crops/Prefabs/Standard/Carrot_Plant.prefab",
            "Assets/Cartoon_Farm_Crops/Prefabs/Standard/Tomato_Plant.prefab",
            "Assets/Cartoon_Farm_Crops/Prefabs/Standard/Eggplant_Plant.prefab",
        };

        // Farm is a 4x5 grid behind the house (positive X side)
        Vector3 farmOrigin = new Vector3(12f, 0, -10f);
        int count = 0;
        for (int xi = 0; xi < 5; xi++)
        {
            for (int zi = 0; zi < 4; zi++)
            {
                float x = farmOrigin.x + xi * 1.6f;
                float z = farmOrigin.z + zi * 1.6f;
                float y = GroundY(x, z);
                Vector3 pos = new Vector3(x, y, z);
                if (!IsGrass(pos)) continue;
                string crop = crops[Random.Range(0, crops.Length)];
                if (Spawn(crop, parent, pos, Random.Range(0f, 360f), 0.6f, CLOUDY_TINT) != null) count++;
            }
        }

        // Haystack decorations around the farm
        string[] haystacks = {
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Farm Ranch Pack/Prefabs/Prop_Haystack_01.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Farm Ranch Pack/Prefabs/Prop_Haystack_03.prefab",
        };
        for (int i = 0; i < 4; i++)
        {
            float x = farmOrigin.x + Random.Range(-1f, 8f);
            float z = farmOrigin.z + Random.Range(-2f, 7f);
            float y = GroundY(x, z);
            if (Spawn(haystacks[Random.Range(0, haystacks.Length)], parent,
                new Vector3(x, y, z), Random.Range(0f, 360f), 0.8f, CLOUDY_TINT) != null) count++;
        }
        return count;
    }

    // ════════════════════════════════════════════════════════════════════════
    // SECTION 6 – MARKET STALLS
    // ════════════════════════════════════════════════════════════════════════
    static int SpawnMarketStalls(GameObject root, Bounds houseBounds)
    {
        var parent = new GameObject("MarketArea").transform;
        parent.SetParent(root.transform);

        // Place a small market row to the left of the house
        Vector3 marketOrigin = new Vector3(-18f, 0, -8f);
        int count = 0;

        string[] stalls = {
            "Assets/Low-Poly Medieval Market/Prefabs/BakeryMarket.prefab",
            "Assets/Low-Poly Medieval Market/Prefabs/VegetableMarket.prefab",
            "Assets/Low-Poly Medieval Market/Prefabs/MeatMarket.prefab",
        };

        for (int i = 0; i < stalls.Length; i++)
        {
            float x = marketOrigin.x + i * 6f;
            float y = GroundY(x, marketOrigin.z);
            Vector3 pos = new Vector3(x, y, marketOrigin.z);
            if (Spawn(stalls[i], parent, pos, 0f, 0.7f, CLOUDY_TINT) != null) count++;
        }

        // Lamp posts along the row
        for (int i = 0; i < 4; i++)
        {
            float x = marketOrigin.x + i * 4.5f - 1f;
            float y = GroundY(x, marketOrigin.z + 3f);
            if (Spawn("Assets/Low-Poly Medieval Market/Prefabs/lamp_post.prefab", parent,
                new Vector3(x, y, marketOrigin.z + 3f), 0f, 0.8f, CLOUDY_TINT) != null) count++;
        }
        return count;
    }

    // ════════════════════════════════════════════════════════════════════════
    // SECTION 7 – SCATTERED PROPS (barrels, crates, fences)
    // ════════════════════════════════════════════════════════════════════════
    static int SpawnScatteredProps(GameObject root, Bounds houseBounds)
    {
        var parent = new GameObject("ScatteredProps").transform;
        parent.SetParent(root.transform);

        string[] props = {
            "Assets/LowPolyMedievalPropsLite/Prefabs/Barrel_01.prefab",
            "Assets/LowPolyMedievalPropsLite/Prefabs/Box_01.prefab",
            "Assets/LowPolyMedievalPropsLite/Prefabs/Fence_01.prefab",
            "Assets/LowPolyMedievalPropsLite/Prefabs/Fence_02.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Farm Ranch Pack/Prefabs/Prop_WoodenBox_01.prefab",
            "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Farm Ranch Pack/Prefabs/Prop_Berrel_03.prefab",
            "Assets/URP GanzSe Free Camping Props/Prefabs/FCP_Barrel_Type1_Color1.prefab",
            "Assets/URP GanzSe Free Camping Props/Prefabs/FCP_WoodenCrate_Type1__Color1.prefab",
        };

        int count = 0;
        for (int attempt = 0; attempt < 300; attempt++)
        {
            if (count >= 30) break;
            if (!TryGetGrassPos(8f, 50f, houseBounds, out Vector3 pos)) continue;
            string path = props[Random.Range(0, props.Length)];
            if (Spawn(path, parent, pos, Random.Range(0f, 360f), Random.Range(0.7f, 1.1f), CLOUDY_TINT) != null) count++;
        }
        return count;
    }
}
