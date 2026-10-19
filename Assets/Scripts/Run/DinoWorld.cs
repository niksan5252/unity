using UnityEngine;

// Dino Escape: lush jungle valley, volcano with lava rivers, golden T-rex valley.
// Kenney Nature Kit scenery + animated Quaternius dinosaurs living in the background.
public static class DinoWorld
{
    static RunLibrary lib;
    static System.Random rnd;

    static GameObject P(string n, Vector3 pos, float rot, float h, Transform root) => CandyWorld.Put(lib, n, pos, rot, h, root);

    // play the first clip whose name contains one of the keys, looping
    public static void Loop(Animation a, params string[] keys)
    {
        if (!a) return;
        foreach (var k in keys)
            foreach (AnimationState s in a)
                if (s.name.ToLower().Contains(k))
                {
                    s.wrapMode = WrapMode.Loop;
                    s.time = Random.value * s.length;
                    a.Play(s.name);
                    return;
                }
    }

    // glTF dinos come with metallic 0.4 and dull colors: matte them and make the colors pop
    static readonly System.Collections.Generic.Dictionary<Material, Material> bright = new System.Collections.Generic.Dictionary<Material, Material>();
    public static void Brighten(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var ms = r.sharedMaterials;
            for (int i = 0; i < ms.Length; i++)
            {
                var src = ms[i];
                if (!src || !src.HasProperty("metallicFactor")) continue;
                if (!bright.TryGetValue(src, out var m) || !m)
                {
                    m = new Material(src);
                    m.SetFloat("metallicFactor", 0f);
                    if (m.HasProperty("roughnessFactor")) m.SetFloat("roughnessFactor", 0.75f);
                    Color.RGBToHSV(src.GetColor("baseColorFactor"), out float h, out float sat, out float v);
                    m.SetColor("baseColorFactor", Color.HSVToRGB(h, Mathf.Clamp01(sat * 1.5f), Mathf.Clamp01(v * 1.6f)));
                    bright[src] = m;
                }
                ms[i] = m;
            }
            r.sharedMaterials = ms;
        }
    }

    public static GameObject Dino(string name, Vector3 pos, float rotY, float height, Transform root, params string[] clips)
    {
        var p = lib.Kit(name);
        if (!p) return null;
        var go = WorldBuilder.Spawn(p, pos, rotY, root);
        Brighten(go);
        var a = go.GetComponentInChildren<Animation>();
        Loop(a, clips);
        if (a) a.Sample();
        RunObstacle.FitHeightPosed(go, height);
        go.transform.position += Vector3.up * pos.y;
        foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.updateWhenOffscreen = true;
        return go;
    }

    static Mesh coneMesh;
    static Mesh Cone()
    {
        if (coneMesh) return coneMesh;
        const int n = 24;
        var v = new Vector3[n * 2 + 2]; var t = new int[n * 6];
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2 / n;
            v[i] = new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f);          // base ring
            v[n + i] = new Vector3(Mathf.Cos(a) * 0.12f, 1f, Mathf.Sin(a) * 0.12f);  // crater rim
        }
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            t[i * 6 + 0] = i; t[i * 6 + 1] = n + i; t[i * 6 + 2] = j;
            t[i * 6 + 3] = j; t[i * 6 + 4] = n + i; t[i * 6 + 5] = n + j;
        }
        coneMesh = new Mesh { vertices = v, triangles = t };
        coneMesh.RecalculateNormals();
        return coneMesh;
    }

    static void Volcano(Transform root, Vector3 pos, float size, Material rock, Material lava)
    {
        var g = new GameObject("Volcano", typeof(MeshFilter), typeof(MeshRenderer));
        g.transform.SetParent(root);
        g.transform.position = pos;
        g.transform.localScale = new Vector3(size, size * 0.55f, size);
        g.GetComponent<MeshFilter>().sharedMesh = Cone();
        g.GetComponent<MeshRenderer>().sharedMaterial = rock;
        // glowing crater + lava streams down the side
        WorldBuilder.Box(root, pos + new Vector3(0, size * 0.55f, 0), new Vector3(size * 0.22f, 0.4f, size * 0.22f), lava);
        for (int k = 0; k < 3; k++)
        {
            var s = WorldBuilder.Box(root, pos + new Vector3((k - 1) * size * 0.08f, size * 0.3f, -size * 0.2f), new Vector3(size * 0.035f, size * 0.62f, size * 0.035f), lava);
            s.transform.rotation = Quaternion.Euler(-40f, (k - 1) * 18f, 0);
        }
    }

    public static void Build(RunLibrary l, RunTheme theme, Transform root, float length, System.Random r)
    {
        lib = l; rnd = r;
        float L = length + 160f, half = length * 0.5f + 60f;
        var cam = Camera.main;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        Color sky = theme == RunTheme.JungleValley ? new Color(0.5f, 0.8f, 1f) : theme == RunTheme.VolcanoRun ? new Color(0.95f, 0.5f, 0.3f) : new Color(1f, 0.72f, 0.45f);
        RenderSettings.fogColor = sky;
        RenderSettings.fogStartDistance = theme == RunTheme.VolcanoRun ? 50f : 80f;
        RenderSettings.fogEndDistance = theme == RunTheme.VolcanoRun ? 330f : 260f;
        RenderSettings.ambientLight = theme == RunTheme.JungleValley ? new Color(0.72f, 0.78f, 0.78f) : theme == RunTheme.VolcanoRun ? new Color(0.7f, 0.5f, 0.45f) : new Color(0.8f, 0.68f, 0.6f);
        if (cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = sky; }
        foreach (var dl in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (dl.type == LightType.Directional)
            {
                dl.intensity = theme == RunTheme.VolcanoRun ? 1.0f : 1.3f;
                dl.color = theme == RunTheme.JungleValley ? new Color(1f, 0.97f, 0.88f) : theme == RunTheme.VolcanoRun ? new Color(1f, 0.7f, 0.5f) : new Color(1f, 0.8f, 0.55f);
                dl.transform.rotation = Quaternion.Euler(theme == RunTheme.DinoValley ? 22f : 45f, -35f, 0f);
            }

        // dirt path with rocky edges
        var path = HalloweenWorld.TexMat(theme == RunTheme.VolcanoRun ? lib.basaltTex : lib.pathTex, 3, L / 3, Color.white, 0, 0.15f);
        WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(8.6f, 0.2f, L), path);

        switch (theme)
        {
            case RunTheme.JungleValley: Jungle(root, length, L, half); break;
            case RunTheme.VolcanoRun: Volcanic(root, length, L, half); break;
            default: Valley(root, length, L, half); break;
        }
    }

    static readonly string[] Palms = { "tree_palm", "tree_palmBend", "tree_palmDetailedShort", "tree_palmDetailedTall", "tree_palmShort", "tree_palmTall" };
    static readonly string[] Bushes = { "plant_bush", "plant_bushDetailed", "plant_bushLarge", "plant_bushLargeTriangle", "plant_flatTall", "plant_flatShort", "grass_large", "grass_leafsLarge" };
    static readonly string[] Flowers = { "flower_purpleA", "flower_redA", "flower_yellowA", "flower_purpleB", "flower_redB", "flower_yellowB", "mushroom_red", "mushroom_redGroup", "mushroom_tanGroup" };
    static readonly string[] Rocks = { "rock_largeA", "rock_largeB", "rock_largeC", "rock_largeD", "rock_tallA", "rock_tallB", "rock_tallC" };

    static string Pick(string[] a) => a[rnd.Next(a.Length)];

    static void Edges(Transform root, float length)
    {
        foreach (int s in new[] { -1, 1 })
            for (float z = -20; z < length + 100; z += 1.6f + (float)rnd.NextDouble())
                P(rnd.NextDouble() < 0.5 ? "rock_smallA" : rnd.NextDouble() < 0.5 ? "rock_smallC" : "rock_smallB", new Vector3(s * (4.5f + (float)rnd.NextDouble() * 0.5f), 0, z), rnd.Next(360), 0.35f + (float)rnd.NextDouble() * 0.3f, root);
    }

    static void Herd(Transform root, float length, string[] kinds, float minX, float maxX, float every)
    {
        foreach (int s in new[] { -1, 1 })
            for (float z = 10 + rnd.Next(10); z < length + 120; z += every + rnd.Next((int)every))
            {
                var k = kinds[rnd.Next(kinds.Length)];
                float h = k == "Apatosaurus" ? 9f + rnd.Next(4) : k == "Stegosaurus" ? 4f : k == "Parasaurolophus" ? 4.5f : 3.5f;
                // the long-necks stand far out and parallel to the road so neck and tail never cross it
                bool big = k == "Apatosaurus";
                var x = s * ((big ? Mathf.Max(minX, 24f) : minX) + (float)rnd.NextDouble() * (maxX - minX));
                float rot = big ? (rnd.Next(2) == 0 ? 0 : 180) + rnd.Next(-15, 15) : rnd.Next(360);
                Dino(k, new Vector3(x, 0, z), rot, h, root, "eat", "idle", "walk");
            }
    }

    // ---------- jungle valley ----------
    static void Jungle(Transform root, float length, float L, float half)
    {
        WorldBuilder.Box(root, new Vector3(0, -0.15f, half), new Vector3(400f, 0.2f, L + 300), HalloweenWorld.TexMat(lib.grassTex, 100, (L + 300) / 4, Color.white, 0, 0.1f));
        Edges(root, length);
        foreach (int s in new[] { -1, 1 })
        {
            for (float z = -10; z < length + 120; z += 1.4f + (float)rnd.NextDouble() * 1.5f)
                P(Pick(Bushes), new Vector3(s * (5.2f + (float)rnd.NextDouble() * 4f), 0, z), rnd.Next(360), 1f + (float)rnd.NextDouble() * 1.4f, root);
            for (float z = -10; z < length + 120; z += 2f + (float)rnd.NextDouble() * 2f)
                P(Pick(Flowers), new Vector3(s * (5f + (float)rnd.NextDouble() * 7f), 0, z), rnd.Next(360), 0.5f + (float)rnd.NextDouble() * 0.5f, root);
            for (float z = -10; z < length + 140; z += 3f + rnd.Next(4))
                P(Pick(Palms), new Vector3(s * (7f + rnd.Next(10)), 0, z), rnd.Next(360), 7f + rnd.Next(6), root);
            for (float z = 0; z < length + 140; z += 14f + rnd.Next(10))
                P(Pick(Rocks), new Vector3(s * (13f + rnd.Next(12)), 0, z), rnd.Next(360), 2.5f + rnd.Next(3), root);
            // green mountains far away
            for (float z = -20; z < length + 300; z += 40f + rnd.Next(30))
            {
                var hill = WorldBuilder.Mat(new Color(0.3f, 0.6f, 0.32f)); hill.SetFloat("_Smoothness", 0.05f);
                var g = new GameObject("Mountain", typeof(MeshFilter), typeof(MeshRenderer));
                g.transform.SetParent(root);
                float sz = 60 + rnd.Next(60);
                g.transform.position = new Vector3(s * (70f + rnd.Next(40)), -2, z);
                g.transform.localScale = new Vector3(sz, sz * 0.6f, sz);
                g.GetComponent<MeshFilter>().sharedMesh = Cone();
                g.GetComponent<MeshRenderer>().sharedMaterial = hill;
            }
        }
        Herd(root, length, new[] { "Apatosaurus", "Stegosaurus", "Parasaurolophus" }, 16f, 34f, 18f);
        Clouds(root, length);
    }

    // ---------- volcano ----------
    static void Volcanic(Transform root, float length, float L, float half)
    {
        var rock = HalloweenWorld.TexMat(lib.basaltTex, 100, (L + 300) / 4, new Color(0.85f, 0.8f, 0.8f), 0, 0.2f);
        var lava = HalloweenWorld.TexMat(lib.lavaTex, 8, L / 8, Color.white, 2.2f, 0.4f);
        WorldBuilder.Box(root, new Vector3(0, -0.2f, half), new Vector3(400f, 0.2f, L + 300), rock);
        foreach (int s in new[] { -1, 1 })
        {
            WorldBuilder.Box(root, new Vector3(s * 9.5f, -0.12f, half), new Vector3(7f, 0.2f, L), lava);      // lava rivers along the path
            for (float z = -10; z < length + 120; z += 2.5f + (float)rnd.NextDouble() * 2f)
                P(rnd.NextDouble() < 0.5 ? "rock_smallA" : "rock_smallD", new Vector3(s * (5.3f + (float)rnd.NextDouble() * 0.8f), 0, z), rnd.Next(360), 0.35f + (float)rnd.NextDouble() * 0.35f, root);
            for (float z = 0; z < length + 140; z += 6f + rnd.Next(6))
                P(rnd.NextDouble() < 0.5 ? "tree_thin_dark" : "tree_simple_dark", new Vector3(s * (14f + rnd.Next(14)), 0, z), rnd.Next(360), 5f + rnd.Next(4), root);
            for (float z = 0; z < length + 140; z += 9f + rnd.Next(8))
                P(Pick(Rocks), new Vector3(s * (16f + rnd.Next(18)), 0, z), rnd.Next(360), 4f + rnd.Next(6), root);
        }
        var cone = HalloweenWorld.TexMat(lib.basaltTex, 6, 4, new Color(0.7f, 0.6f, 0.6f), 0, 0.1f);
        for (float z = 120; z < length + 400; z += 260f)
            Volcano(root, new Vector3((rnd.Next(2) * 2 - 1) * (135f + rnd.Next(30)), -2, z), 150f, cone, lava);
        Herd(root, length, new[] { "Stegosaurus", "Triceratops" }, 18f, 30f, 26f);
    }

    // ---------- golden T-rex valley: waterfalls, lake, palms at sunset ----------
    static void Valley(Transform root, float length, float L, float half)
    {
        WorldBuilder.Box(root, new Vector3(0, -0.15f, half), new Vector3(400f, 0.2f, L + 300), HalloweenWorld.TexMat(lib.grassTex, 100, (L + 300) / 4, new Color(1f, 0.9f, 0.7f), 0, 0.1f));
        var water = WorldBuilder.Mat(new Color(0.25f, 0.6f, 0.85f)); water.SetFloat("_Smoothness", 0.95f); water.SetFloat("_Metallic", 0.2f);
        Edges(root, length);
        foreach (int s in new[] { -1, 1 })
        {
            WorldBuilder.Box(root, new Vector3(s * 20f, -0.1f, half), new Vector3(16f, 0.2f, L), water);   // lakes on both sides
            for (float z = -10; z < length + 120; z += 1.6f + (float)rnd.NextDouble() * 1.5f)
                P(Pick(Bushes), new Vector3(s * (5.2f + (float)rnd.NextDouble() * 5f), 0, z), rnd.Next(360), 0.9f + (float)rnd.NextDouble() * 1.2f, root);
            for (float z = -10; z < length + 140; z += 4f + rnd.Next(5))
                P(Pick(Palms), new Vector3(s * (8f + rnd.Next(4)), 0, z), rnd.Next(360), 7f + rnd.Next(6), root);
            // cliff walls with waterfalls behind the lake
            for (float z = -20; z < length + 200; z += 12f)
            {
                bool fall = rnd.NextDouble() < 0.3;
                var c = P(fall ? "cliff_waterfall_rock" : "cliff_large_rock", new Vector3(s * 32f, 0, z), s < 0 ? 90 : -90, 16f + rnd.Next(8), root);
                if (c) { var b = WorldBuilder.Bounds(c); c.transform.localScale = Vector3.Scale(c.transform.localScale, new Vector3(1, 1, 1)); }
            }
        }
        Herd(root, length, new[] { "Apatosaurus", "Parasaurolophus" }, 13f, 26f, 22f);
        Clouds(root, length);
    }

    static void Clouds(Transform root, float length)
    {
        var m = WorldBuilder.Emissive(Color.white, 0.3f);
        for (float z = -20; z < length + 300; z += 18f + rnd.Next(18))
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(root);
            g.transform.position = new Vector3(rnd.Next(-100, 100), 30 + rnd.Next(15), z);
            g.transform.localScale = new Vector3(16 + rnd.Next(14), 4f, 9 + rnd.Next(8));
            g.GetComponent<Renderer>().sharedMaterial = m;
        }
    }
}
