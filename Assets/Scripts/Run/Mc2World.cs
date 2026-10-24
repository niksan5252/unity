using System.Collections.Generic;
using UnityEngine;

// Move Quest Run #5 (Minecraft Quest 2) worlds, bright and busy, from our own 16x16 textures:
// a cherry grove at sunrise (pink trees, waterfalls, a village, bees, horses), a warm turquoise ocean
// (a causeway, coral reefs, palm islands, a monument, leaping dolphins, turtles) and a crimson / warped
// nether (giant red and teal fungi, a gold bastion, ghasts, striders on the lava). No child figures.
public static class Mc2World
{
    static System.Random rnd;
    static Texture2D T(string n) => BlockFigure.Tex.TryGetValue(n, out var t) ? t : null;
    static Material Tx(string n, float u = 1, float v = 1, float glow = 0) => BlockWorld.Tex(T(n), u, v, glow);
    static GameObject B(Transform p, Vector3 pos, Vector3 size, string tex, float glow = 0) => WorldBuilder.Box(p, pos, size, Tx(tex, size.x, size.y, glow));
    static float F(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

    public struct Sky { public Color top, mid, horizon, sun; public Vector3 sunDir; public float sunSize; }
    public static Sky Palette(RunTheme t) => t switch
    {
        RunTheme.McCherry => new Sky { top = new Color(0.3f, 0.52f, 1f), mid = new Color(0.72f, 0.7f, 1f), horizon = new Color(1f, 0.76f, 0.68f), sun = new Color(1.6f, 1.25f, 0.8f), sunDir = new Vector3(0.25f, 0.18f, 1f), sunSize = 0.12f },
        RunTheme.McOcean => new Sky { top = new Color(0.12f, 0.45f, 1f), mid = new Color(0.4f, 0.72f, 1f), horizon = new Color(0.78f, 0.93f, 1f), sun = new Color(1.6f, 1.55f, 1.2f), sunDir = new Vector3(-0.3f, 0.45f, 1f), sunSize = 0.1f },
        _ => new Sky { top = new Color(0.2f, 0.02f, 0.04f), mid = new Color(0.55f, 0.08f, 0.06f), horizon = new Color(1f, 0.38f, 0.14f), sun = new Color(1.4f, 0.5f, 0.15f), sunDir = new Vector3(0f, 0.12f, 1f), sunSize = 0.0f },
    };

    public static void Build(RunLibrary lib, RunTheme theme, Transform root, float length, System.Random r)
    {
        rnd = r;
        McWorld.LightSpots.Clear();
        float L = length + 160f, half = length * 0.5f + 60f;
        var pal = Palette(theme);
        var cam = Camera.main;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.fogColor = pal.horizon;
        RenderSettings.fogStartDistance = theme == RunTheme.McCrimson ? 45f : 70f;
        RenderSettings.fogEndDistance = theme == RunTheme.McCrimson ? 190f : 260f;
        RenderSettings.ambientLight = theme == RunTheme.McCherry ? new Color(0.86f, 0.78f, 0.84f) : theme == RunTheme.McOcean ? new Color(0.8f, 0.86f, 0.92f) : new Color(0.78f, 0.5f, 0.45f);
        if (cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = pal.horizon; }
        foreach (var dl in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (dl.type == LightType.Directional)
            {
                dl.intensity = theme == RunTheme.McCrimson ? 0.9f : 1.4f;
                dl.color = theme == RunTheme.McCherry ? new Color(1f, 0.86f, 0.72f) : theme == RunTheme.McOcean ? new Color(1f, 0.98f, 0.92f) : new Color(1f, 0.55f, 0.35f);
                dl.transform.rotation = Quaternion.Euler(theme == RunTheme.McCherry ? 28f : 52f, -30f, 0f);
            }

        switch (theme)
        {
            case RunTheme.McCherry: Cherry(root, length, L, half); break;
            case RunTheme.McOcean: Ocean(root, length, L, half); break;
            default: Crimson(root, length, L, half); break;
        }
        McWorld.LightSpots.Sort((a, b) => a.z.CompareTo(b.z));
        root.gameObject.AddComponent<Mc2Fx>().Setup(lib, theme, length);
    }

    // ---------------- shared pieces ----------------
    static void Column(Transform p, float x, float z, float w, int h, string body, string cap, string top)
    {
        if (h > 1) WorldBuilder.Box(p, new Vector3(x, (h - 1) * 0.5f, z), new Vector3(w, h - 1, w), Tx(body, w, h - 1));
        WorldBuilder.Box(p, new Vector3(x, h - 0.5f, z), new Vector3(w, 1, w), Tx(cap, w, 1));
        WorldBuilder.Box(p, new Vector3(x, h + 0.005f, z), new Vector3(w, 0.01f, w), Tx(top, w, w));
    }

    static readonly Dictionary<Color, Material> plants = new Dictionary<Color, Material>();
    static Material PlantMat(Color petal, bool grass)
    {
        var key = grass ? Color.clear : petal;
        if (plants.TryGetValue(key, out var m) && m) return m;
        var t = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[256];
        var stem = new Color(0.22f, 0.6f, 0.18f);
        if (grass)
            for (int b = 0; b < 6; b++) { int x = 2 + b * 2 + (b % 2); for (int y = 0; y < 6 + (b * 5) % 7; y++) px[y * 16 + x] = Color.Lerp(stem, new Color(0.55f, 0.9f, 0.35f), y / 12f); }
        else
        {
            for (int y = 0; y < 9; y++) px[y * 16 + 7] = stem;
            px[4 * 16 + 6] = stem; px[5 * 16 + 8] = stem;
            foreach (var (x, y) in new[] { (7, 12), (6, 11), (8, 11), (7, 10), (6, 10), (8, 10), (7, 9), (5, 11), (9, 11), (7, 13), (6, 12), (8, 12) }) px[y * 16 + x] = petal;
            px[11 * 16 + 7] = new Color(1f, 0.92f, 0.35f);
        }
        t.SetPixels(px); t.Apply();
        m = WorldBuilder.Mat(Color.white);
        m.SetTexture("_BaseMap", t);
        m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cull", 0f);
        m.renderQueue = 2450;
        return plants[key] = m;
    }

    static void Plant(Transform p, Vector3 pos, Material m, float h)
    {
        for (int i = 0; i < 2; i++)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(p);
            q.transform.position = pos + Vector3.up * h * 0.5f;
            q.transform.rotation = Quaternion.Euler(0, 45 + i * 90, 0);
            q.transform.localScale = new Vector3(h, h, 1);
            q.GetComponent<Renderer>().sharedMaterial = m;
        }
    }

    static BlockFigure Mob(BlockFigure.Kind k, Vector3 pos, float yaw, string state, float scale = 1f)
    {
        var f = BlockFigure.Create(k, WorldBuilder.Dyn);
        f.transform.position = pos;
        f.transform.rotation = Quaternion.Euler(0, yaw, 0);
        f.transform.localScale = Vector3.one * scale;
        f.state = state;
        f.speedMul = F(0.6f, 1f);
        return f;
    }

    // ---------------- cherry grove at sunrise ----------------
    static void CherryTree(Transform p, Vector3 b, float size)
    {
        int h = 5 + rnd.Next(3);
        float lean = F(-0.6f, 0.6f);
        WorldBuilder.Box(p, b + new Vector3(0, h * 0.25f, 0), new Vector3(1, h * 0.5f, 1), Tx("cherry_log", 1, h * 0.5f));
        WorldBuilder.Box(p, b + new Vector3(lean, h * 0.75f, 0), new Vector3(1, h * 0.5f, 1), Tx("cherry_log", 1, h * 0.5f));
        var top = b + new Vector3(lean, h, 0);
        float w = 7 * size;
        WorldBuilder.Box(p, top + new Vector3(0, 0.5f, 0), new Vector3(w, 2, w), Tx("cherry_leaves", w, 2));
        WorldBuilder.Box(p, top + new Vector3(0, 2f, 0), new Vector3(w - 2, 1.2f, w - 2), Tx("cherry_leaves", w - 2, 1.2f));
        WorldBuilder.Box(p, top + new Vector3(F(-1, 1), 2.9f, F(-1, 1)), new Vector3(w - 4, 1, w - 4), Tx("cherry_leaves", w - 4, 1));
        // hanging blossom strips under the canopy
        for (int i = 0; i < 6; i++)
        {
            float len = F(0.6f, 1.6f);
            var o = new Vector3(F(-w * 0.45f, w * 0.45f), -0.5f - len * 0.5f, F(-w * 0.45f, w * 0.45f));
            WorldBuilder.Box(p, top + o, new Vector3(1, len, 1), Tx("cherry_leaves", 1, len));
        }
    }

    static void PinkHouse(Transform p, Vector3 o, int s)
    {
        float w = 6, d = 6, h = 4;
        B(p, o + new Vector3(0, 0.5f, 0), new Vector3(w + 0.4f, 1, d + 0.4f), "cobblestone");
        B(p, o + new Vector3(0, 1 + (h - 1) * 0.5f, 0), new Vector3(w, h - 1, d), "cherry_planks");
        foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
            B(p, o + new Vector3(c.x * (w * 0.5f - 0.5f), h * 0.5f, c.y * (d * 0.5f - 0.5f)), new Vector3(1.02f, h, 1.02f), "cherry_log");
        for (int i = 0; i < 4; i++) B(p, o + new Vector3(0, h + 0.5f + i, 0), new Vector3(w + 1.2f - i * 2f, 1, d + 1.2f), "terracotta");
        var glass = WorldBuilder.Emissive(new Color(1f, 0.9f, 0.6f), 0.9f);
        float fx = -s * (w * 0.5f + 0.02f);
        foreach (var dz in new[] { -1.5f, 1.5f }) WorldBuilder.Box(p, o + new Vector3(fx, 2.5f, dz), new Vector3(0.05f, 1, 1), glass);
        WorldBuilder.Box(p, o + new Vector3(fx, 1.5f, 0), new Vector3(0.06f, 2, 1), Tx("planks", 1, 2));
        // flower boxes under the windows
        foreach (var dz in new[] { -1.5f, 1.5f })
        {
            B(p, o + new Vector3(fx - s * 0.3f, 1.8f, dz), new Vector3(0.6f, 0.4f, 1.2f), "dirt");
            Plant(p, o + new Vector3(fx - s * 0.3f, 2f, dz), PlantMat(new Color(1f, 0.3f, 0.45f), false), 0.6f);
        }
        McWorld.LightSpots.Add(o + new Vector3(fx, 2.5f, 0));
    }

    static void Waterfall(Transform p, Vector3 o, int s)
    {
        int h = 10 + rnd.Next(8);
        for (int step = 0; step < 4; step++)
            Column(p, o.x + s * step * 3f, o.z, 3f, h - step * 2, "stone", "grass_side", "grass_petals");
        B(p, o + new Vector3(-s * 1.55f, h * 0.5f, 0), new Vector3(0.3f, h, 2.2f), "water", 0.35f);
        B(p, o + new Vector3(-s * 3.6f, 0.05f, 0), new Vector3(4, 0.12f, 5), "water", 0.35f);
        CherryTree(p, o + new Vector3(s * 6f, h - 4, 2f), 0.8f);
    }

    static void Cherry(Transform root, float length, float L, float half)
    {
        B(root, new Vector3(0, -0.1f, half), new Vector3(400, 0.2f, L + 300), "grass_petals");
        WorldBuilder.Box(root, new Vector3(0, 0.005f, half), new Vector3(8.4f, 0.02f, L), Tx("dirt_path", 8.4f, L));
        Color[] petals = { new Color(1f, 0.45f, 0.7f), new Color(1f, 0.95f, 0.98f), new Color(0.75f, 0.45f, 1f), new Color(1f, 0.85f, 0.15f), new Color(1f, 0.25f, 0.25f), new Color(0.4f, 0.6f, 1f) };
        foreach (int s in new[] { -1, 1 })
        {
            for (float z = -20; z < length + 120; z += 1f) Column(root, s * 4.7f, z, 1f, 1, "dirt", "grass_side", "grass_petals");
            for (float z = -10; z < length + 140; z += 3.5f + rnd.Next(4)) CherryTree(root, new Vector3(s * F(8f, 22f), 0, z), F(0.8f, 1.15f));
            // meadow: flowers and grass tufts
            for (float z = 0; z < length + 120; z += 0.45f + (float)rnd.NextDouble() * 0.6f)
            {
                double pick = rnd.NextDouble();
                var m = pick < 0.45 ? PlantMat(Color.clear, true) : PlantMat(petals[rnd.Next(petals.Length)], false);
                Plant(root, new Vector3(s * F(5.6f, 22f), 0, z), m, F(0.7f, 1f));
            }
            // rolling pink hills and blossom-topped mountains far away
            for (float z = -20; z < length + 300; z += 2f)
            {
                int h = 1 + (int)(Mathf.PerlinNoise(z * 0.04f, s * 3.3f) * 6f);
                Column(root, s * (26f + (z * 7 % 5)), z, 2f, h, "dirt", "grass_side", "grass_petals");
                int mh = 8 + (int)(Mathf.PerlinNoise(z * 0.015f, s * 9.1f) * 26f);
                float mx = s * (48f + (z * 13 % 9));
                Column(root, mx, z, 3f, mh, "stone", "grass_side", "grass_petals");
                if ((int)z % 6 == 0) B(root, new Vector3(mx, mh + 1f, z), new Vector3(4f, 2f, 4f), "cherry_leaves");
            }
            for (float z = 30; z < length + 120; z += 45f + rnd.Next(25)) PinkHouse(root, new Vector3(s * (13f + rnd.Next(4)), 0, z), s);
            for (float z = 70; z < length + 140; z += 95f + rnd.Next(40)) Waterfall(root, new Vector3(s * 30f, 0, z), s);
            // bee nests on posts with bees around them
            for (float z = 20; z < length + 120; z += 38f + rnd.Next(20))
            {
                var nest = new Vector3(s * F(7f, 11f), 0, z);
                B(root, nest + new Vector3(0, 1.2f, 0), new Vector3(0.4f, 2.4f, 0.4f), "cherry_log");
                B(root, nest + new Vector3(0, 2.8f, 0), Vector3.one, "bee_nest");
                for (int i = 0; i < 3; i++)
                {
                    var bee = Mob(BlockFigure.Kind.Bee, nest + new Vector3(F(-1.5f, 1.5f), F(2f, 3.5f), F(-1.5f, 1.5f)), rnd.Next(360), "Idle", 1.6f);
                    bee.gameObject.AddComponent<Mc2Hover>().Set(F(1.2f, 2.4f), F(0.3f, 0.6f), F(0.6f, 1.2f));
                }
            }
            // animals and villagers
            for (float z = 8; z < length + 120; z += 8f + rnd.Next(8))
            {
                var kinds = new[] { BlockFigure.Kind.Horse, BlockFigure.Kind.Sheep, BlockFigure.Kind.Pig, BlockFigure.Kind.Chicken, BlockFigure.Kind.Villager, BlockFigure.Kind.Cow };
                var k = kinds[rnd.Next(kinds.Length)];
                int n = k == BlockFigure.Kind.Villager || k == BlockFigure.Kind.Horse ? 1 : 1 + rnd.Next(3);
                for (int i = 0; i < n; i++)
                {
                    var f = Mob(k, new Vector3(s * F(6.5f, 18f), 0, z + F(0f, 4f)), rnd.Next(360), rnd.NextDouble() < 0.5 ? "Graze" : "Idle");
                    if (k == BlockFigure.Kind.Villager || rnd.NextDouble() < 0.3) f.gameObject.AddComponent<Mc2Wander>().Set(F(3f, 7f), F(0.8f, 1.3f));
                }
            }
        }
    }

    // ---------------- warm ocean ----------------
    static void Palm(Transform p, Vector3 b)
    {
        int h = 6 + rnd.Next(4);
        float lean = F(-0.8f, 0.8f);
        for (int i = 0; i < h; i++) B(p, b + new Vector3(lean * i / h, i + 0.5f, 0), new Vector3(0.8f, 1, 0.8f), "jungle_log");
        var top = b + new Vector3(lean, h + 0.4f, 0);
        B(p, top, new Vector3(1.6f, 0.8f, 1.6f), "jungle_leaves");
        for (int i = 0; i < 4; i++)
        {
            var dir = Quaternion.Euler(0, i * 90 + 45, 0) * Vector3.forward;
            B(p, top + dir * 1.8f + Vector3.down * 0.3f, new Vector3(1.4f, 0.4f, 1.4f), "jungle_leaves");
            B(p, top + dir * 3.0f + Vector3.down * 0.9f, new Vector3(1f, 0.4f, 1f), "jungle_leaves");
        }
    }

    static void Reef(Transform p, Vector3 o)
    {
        string[] cor = { "coral_blue", "coral_pink", "coral_purple", "coral_red", "coral_yellow" };
        int n = 3 + rnd.Next(5);
        for (int i = 0; i < n; i++)
        {
            int h = 1 + rnd.Next(3);
            var c = cor[rnd.Next(cor.Length)];
            for (int y = 0; y < h; y++) B(p, o + new Vector3(F(-2f, 2f), -0.3f + y * 0.9f, F(-2f, 2f)), Vector3.one * F(0.8f, 1.1f), c, 0.18f);
        }
        for (int i = 0; i < 3; i++)
        {
            float kh = F(1f, 2.6f);
            B(p, o + new Vector3(F(-2.5f, 2.5f), kh * 0.5f - 0.4f, F(-2.5f, 2.5f)), new Vector3(0.35f, kh, 0.35f), "kelp");
        }
    }

    static void Monument(Transform p, Vector3 o)
    {
        for (int i = 0; i < 5; i++)
        {
            float w = 26 - i * 5;
            B(p, o + new Vector3(0, i * 3f + 1f, 0), new Vector3(w, 3f, w * 0.7f), i % 2 == 0 ? "prismarine_bricks" : "dark_prismarine");
            foreach (var dx in new[] { -w * 0.5f, w * 0.5f })
                B(p, o + new Vector3(dx, i * 3f + 2.6f, 0), new Vector3(1, 1, 1), "sea_lantern", 1.6f);
        }
        foreach (var dx in new[] { -11f, 11f }) B(p, o + new Vector3(dx, 9f, 0), new Vector3(3, 18, 3), "prismarine_bricks");
        B(p, o + new Vector3(0, 16.5f, 0), new Vector3(2, 2, 2), "sea_lantern", 2f);
    }

    static void Shipwreck(Transform p, Vector3 o, int s)
    {
        var ship = new GameObject("Shipwreck").transform; ship.SetParent(p); ship.position = o;
        for (int i = 0; i < 7; i++)
        {
            float w = 4 - Mathf.Abs(i - 3) * 0.7f;
            B(ship, o + new Vector3(0, 0.8f, (i - 3) * 1.5f), new Vector3(w, 2.2f, 1.5f), "planks");
        }
        B(ship, o + new Vector3(0, 5f, 0), new Vector3(0.5f, 8, 0.5f), "log_side");
        B(ship, o + new Vector3(0, 6.5f, 0.3f), new Vector3(4.5f, 3.5f, 0.15f), "wool");
        ship.rotation = Quaternion.Euler(0, rnd.Next(360), s * 14f);
    }

    static void Ocean(Transform root, float length, float L, float half)
    {
        B(root, new Vector3(0, -0.45f, half), new Vector3(500, 0.2f, L + 400), "sea_water", 0.25f);
        // the causeway: light sand road, prismarine kerbs with sea lanterns
        WorldBuilder.Box(root, new Vector3(0, -0.2f, half), new Vector3(8.6f, 0.4f, L), Tx("sand_light", 8.6f, L));
        foreach (int s in new[] { -1, 1 })
        {
            WorldBuilder.Box(root, new Vector3(s * 4.7f, 0.15f, half), new Vector3(1f, 0.7f, L), Tx("prismarine_bricks", 1, L));
            for (float z = -10; z < length + 120; z += 8f)
            {
                B(root, new Vector3(s * 4.7f, 0.55f, z), new Vector3(1.02f, 0.3f, 1.02f), "sea_lantern", 1.8f);
                McWorld.LightSpots.Add(new Vector3(s * 4.7f, 1f, z));
            }
            for (float z = 0; z < length + 140; z += 7f + rnd.Next(7)) Reef(root, new Vector3(s * F(7f, 20f), 0, z));
            // palm islands and beaches with turtles
            for (float z = 15; z < length + 140; z += 30f + rnd.Next(25))
            {
                var c = new Vector3(s * F(14f, 30f), 0, z);
                float r = F(4f, 7f);
                B(root, c + new Vector3(0, -0.15f, 0), new Vector3(r * 2, 0.6f, r * 2), "sand_light");
                B(root, c + new Vector3(0, 0.25f, 0), new Vector3(r * 1.3f, 0.4f, r * 1.3f), "sand_light");
                Palm(root, c + new Vector3(F(-1, 1), 0.45f, F(-1, 1)));
                if (rnd.NextDouble() < 0.6) Palm(root, c + new Vector3(F(-r * 0.6f, r * 0.6f), 0.45f, F(-r * 0.6f, r * 0.6f)));
                for (int i = 0; i < 2; i++)
                {
                    var t = Mob(BlockFigure.Kind.Turtle, c + new Vector3(F(-r * 0.7f, r * 0.7f), 0.15f, F(-r * 0.7f, r * 0.7f)), rnd.Next(360), "Walk", 1.4f);
                    t.speedMul = 0.4f;
                }
            }
            for (float z = 60; z < length + 200; z += 140f + rnd.Next(40)) Monument(root, new Vector3(s * F(42f, 55f), -1f, z));
            for (float z = 100; z < length + 140; z += 120f + rnd.Next(60)) Shipwreck(root, new Vector3(s * F(20f, 30f), 0, z), s);
            // far islands on the horizon
            for (float z = -20; z < length + 320; z += 18f + rnd.Next(20))
            {
                float x = s * F(70f, 110f), w = F(8f, 18f);
                B(root, new Vector3(x, 0.5f, z), new Vector3(w, 1.6f + F(0, 4f), w), rnd.NextDouble() < 0.5 ? "sand_light" : "grass_side");
            }
            // dolphins leaping beside the road
            for (float z = 20; z < length + 120; z += 22f + rnd.Next(18))
            {
                var dph = Mob(BlockFigure.Kind.Dolphin, new Vector3(s * F(7f, 14f), -1.5f, z), 0, "Run", 1.8f);
                dph.gameObject.AddComponent<Mc2Leap>().Set(F(0f, 3f), F(2.6f, 3.8f), F(3.5f, 5f));
            }
        }
    }

    // ---------------- crimson & warped nether ----------------
    static void Fungus(Transform p, Vector3 b, bool warped, float size)
    {
        string stem = warped ? "warped_stem" : "crimson_stem", cap = warped ? "warped_wart" : "nether_wart";
        int h = (int)((8 + rnd.Next(7)) * size);
        float w = (7 + rnd.Next(3)) * size;
        WorldBuilder.Box(p, b + new Vector3(0, h * 0.5f, 0), new Vector3(size * 1.4f, h, size * 1.4f), Tx(stem, 1, h));
        WorldBuilder.Box(p, b + new Vector3(0, h + 0.5f, 0), new Vector3(w, 2.5f, w), Tx(cap, w, 2.5f));
        WorldBuilder.Box(p, b + new Vector3(0, h + 2.2f, 0), new Vector3(w * 0.6f, 1.2f, w * 0.6f), Tx(cap, w * 0.6f, 1.2f));
        for (int i = 0; i < 3; i++)
        {
            var o = new Vector3(F(-w * 0.4f, w * 0.4f), h - 0.9f, F(-w * 0.4f, w * 0.4f));
            B(p, b + o, Vector3.one, "shroomlight", 2.2f);
            McWorld.LightSpots.Add(b + o);
        }
        // weeping / twisting vines
        var vine = WorldBuilder.Emissive(warped ? new Color(0.1f, 0.8f, 0.7f) : new Color(0.75f, 0.08f, 0.1f), 0.5f);
        for (int i = 0; i < 4; i++)
        {
            float len = F(1.5f, 4f);
            WorldBuilder.Box(p, b + new Vector3(F(-w * 0.45f, w * 0.45f), h - 0.75f - len * 0.5f, F(-w * 0.45f, w * 0.45f)), new Vector3(0.25f, len, 0.25f), vine);
        }
    }

    static void Bastion(Transform p, Vector3 o)
    {
        for (int i = 0; i < 3; i++)
        {
            float w = 14 - i * 3.5f;
            B(p, o + new Vector3(0, -6 + 4 + i * 4f, 0), new Vector3(w, 4, w), i == 1 ? "gilded_blackstone" : "blackstone");
        }
        foreach (var dx in new[] { -6f, 6f })
        {
            B(p, o + new Vector3(dx, 6f, 0), new Vector3(3, 12, 3), "blackstone");
            B(p, o + new Vector3(dx, 12.5f, 0), new Vector3(3.6f, 1, 3.6f), "gold_block", 0.4f);
        }
        B(p, o + new Vector3(0, 8.5f, 0), new Vector3(3, 3, 3), "gold_block", 0.6f);
        McWorld.LightSpots.Add(o + new Vector3(0, 9f, 0));
    }

    static void Crimson(Transform root, float length, float L, float half)
    {
        B(root, new Vector3(0, -6f, half), new Vector3(400, 0.2f, L + 300), "lava", 1.9f);
        WorldBuilder.Box(root, new Vector3(0, -0.5f, half), new Vector3(8.6f, 1f, L), Tx("blackstone", 8.6f, L));
        foreach (int s in new[] { -1, 1 })
        {
            bool warped = s > 0;
            string nyl = warped ? "warped_nylium" : "crimson_nylium";
            WorldBuilder.Box(root, new Vector3(s * 4.55f, 0.3f, half), new Vector3(0.6f, 0.6f, L), Tx("gilded_blackstone", 1, L));
            for (float z = -20; z < length + 120; z += 6f)
                B(root, new Vector3(s * 4.55f, -3.5f, z), new Vector3(1.2f, 6f, 1.2f), "blackstone");
            for (float z = 0; z < length + 120; z += 16f)
            {
                B(root, new Vector3(s * 4.55f, 1.1f, z), new Vector3(0.5f, 1f, 0.5f), "blackstone");
                B(root, new Vector3(s * 4.55f, 1.8f, z), new Vector3(0.45f, 0.45f, 0.45f), "shroomlight", 2.4f);
                McWorld.LightSpots.Add(new Vector3(s * 4.55f, 2f, z));
            }
            // forest islands on the lava
            for (float z = -10; z < length + 160; z += 12f + rnd.Next(10))
            {
                var c = new Vector3(s * F(10f, 30f), 0, z);
                float w = F(7f, 12f);
                B(root, c + new Vector3(0, -3f, 0), new Vector3(w, 6f, w), "netherrack");
                B(root, c + new Vector3(0, 0.25f, 0), new Vector3(w, 0.5f, w), nyl);
                Fungus(root, c + new Vector3(F(-1, 1), 0.5f, F(-1, 1)), warped, F(0.9f, 1.3f));
                if (rnd.NextDouble() < 0.5)
                {
                    var mob = Mob(rnd.NextDouble() < 0.5 ? BlockFigure.Kind.Piglin : BlockFigure.Kind.MagmaCube, c + new Vector3(F(-w * 0.3f, w * 0.3f), 0.5f, F(-w * 0.3f, w * 0.3f)), rnd.Next(360), "Idle");
                    if (mob.kind == BlockFigure.Kind.MagmaCube) mob.transform.localScale = Vector3.one * 0.7f;
                }
            }
            // netherrack spires with lava falls far out
            for (float z = 0; z < length + 220; z += 9f + rnd.Next(12))
            {
                float x = s * F(36f, 70f); int h = 14 + rnd.Next(30);
                B(root, new Vector3(x, -6 + h * 0.5f, z), new Vector3(5, h, 5), "netherrack");
                if (rnd.NextDouble() < 0.35) B(root, new Vector3(x - s * 2.6f, -6 + h * 0.5f, z), new Vector3(0.3f, h, 2.4f), "lava", 2.2f);
                if (rnd.NextDouble() < 0.4) B(root, new Vector3(x, -6 + h + 1f, z), new Vector3(3, 2, 3), "glowstone", 1.8f);
            }
            for (float z = 50; z < length + 140; z += 75f + rnd.Next(30)) Bastion(root, new Vector3(s * F(20f, 26f), 0, z));
            // striders walking on the lava sea
            for (float z = 10; z < length + 120; z += 18f + rnd.Next(14))
            {
                var st = Mob(BlockFigure.Kind.Strider, new Vector3(s * F(6.5f, 9f), -6f, z), rnd.Next(360), "Walk", 1.2f);
                st.gameObject.AddComponent<Mc2Wander>().Set(F(3f, 6f), F(0.6f, 1f));
            }
        }
        // ghasts drifting high up
        for (float z = 20; z < length + 200; z += 30f + rnd.Next(25))
        {
            var g = Mob(BlockFigure.Kind.Ghast, new Vector3(F(-30f, 30f), F(10f, 22f), z), 180, "Idle", F(2.2f, 3.2f));
            g.gameObject.AddComponent<Mc2Hover>().Set(F(3f, 6f), F(0.08f, 0.15f), F(0.8f, 1.5f));
        }
    }
}

// ---------------- little movers for the Dyn creatures ----------------
// circles round where it started, bobbing (bees, ghasts)
public class Mc2Hover : MonoBehaviour
{
    Vector3 anchor; float radius, speed, bob, a;
    public void Set(float r, float s, float b) { anchor = transform.position; radius = r; speed = s; bob = b; a = Random.value * 6.28f; }
    void Update()
    {
        a += Time.deltaTime * speed;
        var p = anchor + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a * 2.3f) * bob * 0.5f, Mathf.Sin(a) * radius);
        var dir = p - transform.position;
        transform.position = p;
        if (dir.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
    }
}

// walks to and fro along its facing (villagers, horses, striders)
public class Mc2Wander : MonoBehaviour
{
    Vector3 anchor, dir; float dist, speed, u;
    BlockFigure fig;
    public void Set(float d, float s)
    {
        anchor = transform.position; dist = d; speed = s;
        dir = transform.forward; dir.y = 0; dir.Normalize();
        fig = GetComponent<BlockFigure>();
        if (fig) fig.SetState("Walk");
    }
    void Update()
    {
        u += Time.deltaTime * speed / Mathf.Max(0.5f, dist);
        float k = Mathf.PingPong(u, 1f);
        transform.position = anchor + dir * (k - 0.5f) * dist;
        bool back = Mathf.Repeat(u, 2f) > 1f;
        transform.rotation = Quaternion.LookRotation(back ? -dir : dir);
    }
}

// leaps out of the water in an arc every few seconds (dolphins)
public class Mc2Leap : MonoBehaviour
{
    Vector3 start; float delay, height, every, t;
    public void Set(float d, float h, float e) { start = transform.position; delay = d; height = h; every = e; t = -d; }
    void Update()
    {
        t += Time.deltaTime;
        float u = Mathf.Repeat(t, every) / 1.6f;            // a jump takes 1.6 s, then it is under water
        if (t < 0 || u > 1f) { transform.position = start + Vector3.down * 3f; return; }
        float y = Mathf.Sin(u * Mathf.PI) * height;
        transform.position = start + new Vector3(0, y + 0.8f, (u - 0.5f) * 6f);
        transform.rotation = Quaternion.Euler(Mathf.Lerp(-45f, 45f, u), 0, 0);
    }
}

// Sky dome, a square sun, block clouds, falling petals / sea sparkles / embers, a few lights near the camera.
public class Mc2Fx : MonoBehaviour
{
    RunTheme theme;
    Camera cam;
    Transform sky, clouds, follow;
    Light[] pool;

    public void Setup(RunLibrary lib, RunTheme t, float length)
    {
        theme = t; cam = Camera.main;
        var pal = Mc2World.Palette(t);
        sky = new GameObject("Mc2Sky").transform; sky.SetParent(WorldBuilder.Dyn);
        if (lib && lib.skyShader && lib.skyShader.isSupported)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(sky, false); g.transform.localScale = Vector3.one * 700f;
            var m = new Material(lib.skyShader);
            m.SetColor("_Top", pal.top); m.SetColor("_Mid", pal.mid); m.SetColor("_Horizon", pal.horizon);
            m.SetVector("_MoonDir", pal.sunDir); m.SetColor("_MoonColor", pal.sun); m.SetFloat("_MoonSize", pal.sunSize);
            m.SetFloat("_Stars", 0f); m.SetFloat("_Clouds", t == RunTheme.McCrimson ? 0.35f : 0f);
            m.SetColor("_CloudColor", new Color(0.35f, 0.05f, 0.05f)); m.SetFloat("_Aurora", 0f);
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }
        if (t != RunTheme.McCrimson)
        {
            // the square sun of the block world, glowing, and flat block clouds
            var sun = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(sun.GetComponent<Collider>());
            sun.transform.SetParent(sky, false);
            sun.transform.localPosition = pal.sunDir.normalized * 330f; sun.transform.localScale = Vector3.one * 38f;
            sun.transform.rotation = Quaternion.LookRotation(pal.sunDir.normalized);
            var sm = new Material(Shader.Find("Universal Render Pipeline/Unlit")); sm.SetColor("_BaseColor", pal.sun * 1.3f);
            sun.GetComponent<Renderer>().sharedMaterial = sm;
            clouds = new GameObject("Clouds").transform; clouds.SetParent(WorldBuilder.Dyn);
            var cm = WorldBuilder.Emissive(t == RunTheme.McCherry ? new Color(1f, 0.92f, 0.95f) : Color.white, 0.45f);
            var rr = new System.Random(5);
            for (float z = -40; z < length + 420; z += 10f + rr.Next(14))
                WorldBuilder.Box(clouds, new Vector3(rr.Next(-140, 140), 40f + rr.Next(8), z), new Vector3(8 + rr.Next(5) * 4, 1.8f, 6 + rr.Next(4) * 4), cm);
        }
        follow = t == RunTheme.McCherry ? Particles("Petals", new Color(1f, 0.6f, 0.8f), new Color(1f, 0.85f, 0.92f), 260, -0.7f, 0.16f, new Vector3(36, 10, 46), 1.2f)
               : t == RunTheme.McOcean ? Particles("Sparkles", new Color(1f, 1f, 1f), new Color(0.7f, 0.95f, 1f), 140, 0.2f, 0.1f, new Vector3(40, 1, 50), 2.2f)
               : Particles("Embers", new Color(1f, 0.45f, 0.1f), new Color(1f, 0.85f, 0.3f), 220, 1.1f, 0.14f, new Vector3(36, 8, 46), 2.4f);
        if (t == RunTheme.McCrimson)
        {
            pool = new Light[5];
            for (int i = 0; i < pool.Length; i++)
            {
                var g = new GameObject("ShroomLight" + i); g.transform.SetParent(WorldBuilder.Dyn);
                pool[i] = g.AddComponent<Light>(); pool[i].type = LightType.Point; pool[i].range = 14; pool[i].color = new Color(1f, 0.6f, 0.3f); pool[i].intensity = 0; pool[i].shadows = LightShadows.None;
            }
        }
    }

    Transform Particles(string name, Color a, Color b, float rate, float speedY, float size, Vector3 box, float hdr)
    {
        var g = new GameObject(name); g.transform.SetParent(WorldBuilder.Dyn);
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.startLifetime = 6f; main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size); main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 2500;
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f); vel.y = new ParticleSystem.MinMaxCurve(speedY); vel.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        m.SetColor("_BaseColor", new Color(hdr, hdr, hdr, 1f));
        g.GetComponent<ParticleSystemRenderer>().sharedMaterial = m;
        ps.Play();
        return g.transform;
    }

    void LateUpdate()
    {
        if (!cam) return;
        var cp = cam.transform.position;
        if (sky) sky.position = cp;
        if (clouds) clouds.position += Vector3.forward * Time.deltaTime * 1.2f;
        if (follow) follow.position = cp + new Vector3(0, theme == RunTheme.McCherry ? 6f : theme == RunTheme.McOcean ? -0.2f : 1f, 18f);
        if (pool != null)
        {
            int k = 0;
            foreach (var sp in McWorld.LightSpots)
            {
                if (k >= pool.Length) break;
                float dz = sp.z - cp.z;
                if (dz < -4f || dz > 45f) continue;
                pool[k].transform.position = sp;
                pool[k].intensity = 3f * (0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 4f + k, k));
                k++;
            }
            for (; k < pool.Length; k++) pool[k].intensity = 0;
        }
    }
}
