using System.Collections.Generic;
using UnityEngine;

// Minecraft-style worlds from our own 16x16 pixel textures: plains village, ore caves, nether fortress.
public static class McWorld
{
    static System.Random rnd;
    public static readonly List<Vector3> LightSpots = new List<Vector3>();

    static Texture2D T(string n) => BlockFigure.Tex.TryGetValue(n, out var t) ? t : null;
    static Material Tx(string n, float u = 1, float v = 1, float glow = 0) => BlockWorld.Tex(T(n), u, v, glow);

    public static GameObject Block(Transform p, Vector3 pos, string tex, float glow = 0) =>
        WorldBuilder.Box(p, pos, Vector3.one, Tx(tex, 1, 1, glow));

    // grass block column: dirt body, grass-side cap, grass top
    static void GrassColumn(Transform p, float x, float z, float w, int h)
    {
        if (h > 1) WorldBuilder.Box(p, new Vector3(x, (h - 1) * 0.5f, z), new Vector3(w, h - 1, w), Tx("dirt", w, h - 1));
        WorldBuilder.Box(p, new Vector3(x, h - 0.5f, z), new Vector3(w, 1, w), Tx("grass_side", w, 1));
        WorldBuilder.Box(p, new Vector3(x, h + 0.005f, z), new Vector3(w, 0.01f, w), Tx("grass_top", w, w));
    }

    static void Tree(Transform p, Vector3 b)
    {
        int h = 4 + rnd.Next(3);
        WorldBuilder.Box(p, b + new Vector3(0, h * 0.5f, 0), new Vector3(1, h, 1), Tx("log_side", 1, h));
        var leaves = Tx("leaves", 5, 2);
        WorldBuilder.Box(p, b + new Vector3(0, h - 0.5f, 0), new Vector3(5, 2, 5), leaves);
        WorldBuilder.Box(p, b + new Vector3(0, h + 1f, 0), new Vector3(3, 1, 3), Tx("leaves", 3, 1));
        WorldBuilder.Box(p, b + new Vector3(0, h + 2f, 0), new Vector3(1, 1, 1), Tx("leaves"));
    }

    static void House(Transform p, Vector3 o, int s)
    {
        float w = 6, d = 5, h = 4;
        WorldBuilder.Box(p, o + new Vector3(0, 0.5f, 0), new Vector3(w, 1, d), Tx("cobblestone", w, 1));
        WorldBuilder.Box(p, o + new Vector3(0, 1 + (h - 1) * 0.5f, 0), new Vector3(w, h - 1, d), Tx("planks", w, h - 1));
        foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
            WorldBuilder.Box(p, o + new Vector3(c.x * (w * 0.5f - 0.5f), h * 0.5f, c.y * (d * 0.5f - 0.5f)), new Vector3(1.02f, h, 1.02f), Tx("log_side", 1, h));
        var glass = WorldBuilder.Emissive(new Color(0.7f, 0.9f, 1f), 0.4f);
        WorldBuilder.Box(p, o + new Vector3(-s * (w * 0.5f + 0.01f), 2.5f, 0), new Vector3(0.05f, 1, 1), glass);
        WorldBuilder.Box(p, o + new Vector3(-s * (w * 0.5f + 0.01f), 1.5f, 1.5f), new Vector3(0.05f, 2, 1), Tx("log_side", 1, 2));   // door
        for (int i = 0; i < 3; i++)
            WorldBuilder.Box(p, o + new Vector3(0, h + 0.5f + i, 0), new Vector3(w + 1 - i * 2.4f, 1, d + 1), Tx("log_top", w + 1 - i * 2.4f, 1));
        // torch by the door
        var flame = WorldBuilder.Emissive(new Color(1f, 0.75f, 0.3f), 6f);
        WorldBuilder.Box(p, o + new Vector3(-s * (w * 0.5f + 0.1f), 2.3f, -0.8f), new Vector3(0.12f, 0.5f, 0.12f), Tx("planks"));
        WorldBuilder.Box(p, o + new Vector3(-s * (w * 0.5f + 0.1f), 2.6f, -0.8f), Vector3.one * 0.16f, flame);
    }

    // cross-shaped billboard plants (flowers, tall grass) like the game's sprites
    static Material flowerRed, flowerYellow, tallGrass;
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

    static Texture2D PlantTex(Color petal, Color stem, bool grass)
    {
        var t = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[256];
        for (int i = 0; i < 256; i++) px[i] = Color.clear;
        if (grass)
            for (int b = 0; b < 6; b++) { int x = 2 + b * 2 + (b % 2); for (int y = 0; y < 6 + (b * 5) % 7; y++) px[y * 16 + x] = Color.Lerp(stem, new Color(0.5f, 0.85f, 0.35f), y / 12f); }
        else
        {
            for (int y = 0; y < 9; y++) px[y * 16 + 7] = stem;
            px[4 * 16 + 6] = stem; px[5 * 16 + 8] = stem;
            foreach (var (x, y) in new[] { (7, 12), (6, 11), (8, 11), (7, 10), (6, 10), (8, 10), (7, 9), (5, 11), (9, 11), (7, 13) }) px[y * 16 + x] = petal;
            px[11 * 16 + 7] = new Color(1f, 0.9f, 0.3f);
        }
        t.SetPixels(px); t.Apply();
        return t;
    }

    static Material Cutout(Texture2D t)
    {
        var m = WorldBuilder.Mat(Color.white);
        m.SetTexture("_BaseMap", t);
        m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cull", 0f);
        m.renderQueue = 2450;
        return m;
    }

    public static void Build(RunLibrary lib, RunTheme theme, Transform root, float length, System.Random r)
    {
        rnd = r;
        LightSpots.Clear();
        float L = length + 160f, half = length * 0.5f + 60f;
        if (!flowerRed)
        {
            flowerRed = Cutout(PlantTex(new Color(0.9f, 0.1f, 0.1f), new Color(0.2f, 0.55f, 0.15f), false));
            flowerYellow = Cutout(PlantTex(new Color(1f, 0.85f, 0.1f), new Color(0.2f, 0.55f, 0.15f), false));
            tallGrass = Cutout(PlantTex(Color.clear, new Color(0.25f, 0.6f, 0.15f), true));
        }

        var cam = Camera.main;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        Color sky = theme == RunTheme.McPlains ? new Color(0.47f, 0.65f, 1f) : theme == RunTheme.McCaves ? new Color(0.05f, 0.05f, 0.07f) : new Color(0.38f, 0.07f, 0.05f);
        RenderSettings.fogColor = sky;
        RenderSettings.fogStartDistance = theme == RunTheme.McCaves ? 20f : 60f;
        RenderSettings.fogEndDistance = theme == RunTheme.McCaves ? 95f : theme == RunTheme.McNether ? 150f : 230f;
        RenderSettings.ambientLight = theme == RunTheme.McPlains ? new Color(0.75f, 0.77f, 0.82f) : theme == RunTheme.McCaves ? new Color(0.52f, 0.48f, 0.46f) : new Color(0.72f, 0.45f, 0.38f);
        if (cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = sky; }
        foreach (var dl in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (dl.type == LightType.Directional)
            {
                dl.intensity = theme == RunTheme.McPlains ? 1.35f : theme == RunTheme.McCaves ? 0.25f : 0.7f;
                dl.color = theme == RunTheme.McNether ? new Color(1f, 0.6f, 0.45f) : new Color(1f, 0.97f, 0.9f);
                dl.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

        switch (theme)
        {
            case RunTheme.McPlains: Plains(root, length, L, half); break;
            case RunTheme.McCaves: Caves(lib, root, length, L, half); break;
            default: Nether(root, length, L, half); break;
        }
        var fx = root.gameObject.AddComponent<McFx>();
        fx.Setup(theme, length);
    }

    // ---------------- plains & village ----------------
    static void Plains(Transform root, float length, float L, float half)
    {
        WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(400, 0.2f, L + 300), Tx("grass_top", 400, L + 300));
        WorldBuilder.Box(root, new Vector3(0, 0.005f, half), new Vector3(8.4f, 0.02f, L), Tx("gravel", 8.4f, L));
        foreach (int s in new[] { -1, 1 })
        {
            // low grass terrace along the road, rolling hills further out
            for (float z = -20; z < length + 120; z += 1f)
                GrassColumn(root, s * 4.7f, z, 1f, 1);
            for (float z = -20; z < length + 160; z += 2f)
            {
                int h = 1 + (int)(Mathf.PerlinNoise(z * 0.05f, s * 3.3f) * 4f);
                GrassColumn(root, s * (22f + (z * 7 % 5)), z, 2f, h);
            }
            for (float z = 0; z < length + 140; z += 5f + rnd.Next(6))
                Tree(root, new Vector3(s * (9f + rnd.Next(12)), 0, z));
            for (float z = 0; z < length + 120; z += 0.8f + (float)rnd.NextDouble() * 1.2f)
            {
                double pick = rnd.NextDouble();
                var m = pick < 0.6 ? tallGrass : pick < 0.8 ? flowerRed : flowerYellow;
                Plant(root, new Vector3(s * (5.6f + (float)rnd.NextDouble() * 14f), 0, z), m, 0.9f);
            }
            for (float z = 25; z < length + 120; z += 40f + rnd.Next(25))
                House(root, new Vector3(s * (14f + rnd.Next(4)), 0, z), s);
            // ponds
            for (float z = 60; z < length + 120; z += 90f + rnd.Next(40))
                WorldBuilder.Box(root, new Vector3(s * (11f + rnd.Next(6)), 0.02f, z), new Vector3(6, 0.05f, 9), Tx("water", 6, 9, 0.15f));
            // animals
            for (float z = 8; z < length + 120; z += 9f + rnd.Next(9))
            {
                var kinds = new[] { BlockFigure.Kind.Pig, BlockFigure.Kind.Cow, BlockFigure.Kind.Sheep, BlockFigure.Kind.Chicken };
                int n = 1 + rnd.Next(3);
                var k = kinds[rnd.Next(kinds.Length)];
                for (int i = 0; i < n; i++)
                {
                    var f = BlockFigure.Create(k, WorldBuilder.Dyn);
                    f.transform.position = new Vector3(s * (6.5f + (float)rnd.NextDouble() * 9f), 0, z + (float)rnd.NextDouble() * 4f);
                    f.transform.rotation = Quaternion.Euler(0, rnd.Next(360), 0);
                    f.state = rnd.NextDouble() < 0.6 ? "Graze" : "Idle";
                    f.speedMul = 0.5f + (float)rnd.NextDouble() * 0.5f;
                }
            }
        }
    }

    // ---------------- caves: stone, ores, lava, torches, rails ----------------
    static void Caves(RunLibrary lib, Transform root, float length, float L, float half)
    {
        WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(14, 0.2f, L), Tx("stone", 14, L));
        WorldBuilder.Box(root, new Vector3(0, 0.005f, half), new Vector3(8.4f, 0.02f, L), Tx("gravel", 8.4f, L));
        foreach (int s in new[] { -1, 1 })
            WorldBuilder.Box(root, new Vector3(s * 7.5f, 4f, half), new Vector3(1f, 8f, L), Tx("stone", L, 8));
        WorldBuilder.Box(root, new Vector3(0, 8.5f, half), new Vector3(16, 1f, L), Tx("stone", 16, L));
        string[] ores = { "coal_ore", "iron_ore", "gold_ore", "diamond_ore", "redstone_ore", "emerald_ore", "cobblestone", "gravel" };
        var flame = WorldBuilder.Emissive(new Color(1f, 0.75f, 0.3f), 7f);
        foreach (int s in new[] { -1, 1 })
        {
            for (float z = -10; z < length + 100; z += 1f)
                for (int y = 0; y < 8; y++)
                    if (rnd.NextDouble() < 0.18)
                    {
                        var ore = ores[rnd.Next(ores.Length)];
                        Block(root, new Vector3(s * 6.6f, 0.5f + y, z), ore, ore == "diamond_ore" || ore == "emerald_ore" || ore == "redstone_ore" ? 0.35f : 0);
                    }
            // ledges and lava pools on the sides
            for (float z = 0; z < length + 100; z += 2f)
                Block(root, new Vector3(s * 5.4f, 0.5f, z), rnd.NextDouble() < 0.5 ? "cobblestone" : "stone");
            for (float z = 20; z < length + 100; z += 28f + rnd.Next(20))
            {
                WorldBuilder.Box(root, new Vector3(s * 6.4f, 4f, z), new Vector3(0.2f, 8f, 2f), Tx("lava", 2, 8, 2.2f));   // lava fall
                WorldBuilder.Box(root, new Vector3(s * 5.6f, 0.08f, z), new Vector3(2.4f, 0.1f, 4f), Tx("lava", 2, 4, 2.2f));
                LightSpots.Add(new Vector3(s * 5.2f, 1.5f, z));
            }
            // torches on the walls
            for (float z = 4; z < length + 100; z += 9f)
            {
                WorldBuilder.Box(root, new Vector3(s * 6.0f, 2.6f, z), new Vector3(0.12f, 0.6f, 0.12f), Tx("planks"));
                WorldBuilder.Box(root, new Vector3(s * 6.0f, 3.0f, z), Vector3.one * 0.18f, flame);
                LightSpots.Add(new Vector3(s * 5.4f, 3.0f, z));
            }
        }
        // rails on all three lanes (minecart vibe)
        if (lib.track)
        {
            var sz = WorldBuilder.Measure(lib.track, out _);
            bool rot = sz.x > sz.z;
            float tl = Mathf.Max(0.5f, rot ? sz.x : sz.z), tw = rot ? sz.z : sz.x;
            float k = tw > 0.01f ? 1.8f / tw : 1f; tl *= k;
            foreach (float x in new[] { -WorldBuilder.LaneGap, 0f, WorldBuilder.LaneGap })
                for (float z = -20f; z < length + 60f; z += tl)
                    WorldBuilder.Spawn(lib.track, new Vector3(x, 0.03f, z), rot ? 90 : 0, root).transform.localScale *= k;
        }
        LightSpots.Sort((a, b) => a.z.CompareTo(b.z));
    }

    // ---------------- nether fortress ----------------
    static void Nether(Transform root, float length, float L, float half)
    {
        WorldBuilder.Box(root, new Vector3(0, -6f, half), new Vector3(400, 0.2f, L + 300), Tx("lava", 200, (L + 300) / 2, 1.8f));   // lava sea far below
        WorldBuilder.Box(root, new Vector3(0, -0.5f, half), new Vector3(8.6f, 1f, L), Tx("nether_bricks", 8.6f, L));                  // fortress bridge
        foreach (int s in new[] { -1, 1 })
        {
            WorldBuilder.Box(root, new Vector3(s * 4.55f, 0.5f, half), new Vector3(0.5f, 1f, L), Tx("nether_bricks", L, 1));
            for (float z = -20; z < length + 120; z += 6f)
                WorldBuilder.Box(root, new Vector3(s * 4.55f, -3.5f, z), new Vector3(1.2f, 6f, 1.2f), Tx("nether_bricks", 1, 6));   // bridge pillars
            for (float z = 0; z < length + 160; z += 6f + rnd.Next(10))
            {
                float x = s * (14f + rnd.Next(26)); int h = 6 + rnd.Next(20);
                WorldBuilder.Box(root, new Vector3(x, -6 + h * 0.5f, z), new Vector3(4, h, 4), Tx("netherrack", 4, h));
                if (rnd.NextDouble() < 0.4) WorldBuilder.Box(root, new Vector3(x, -6 + h + 0.5f, z), new Vector3(2, 1, 2), Tx("glowstone", 2, 1, 1.6f));
                if (rnd.NextDouble() < 0.25) WorldBuilder.Box(root, new Vector3(x - s * 2.1f, -6 + h * 0.5f, z), new Vector3(0.2f, h, 2f), Tx("lava", 2, h, 2.2f));
            }
            // fortress towers along the bridge
            for (float z = 30; z < length + 120; z += 45f + rnd.Next(20))
            {
                float x = s * (9f + rnd.Next(4)); int h = 10 + rnd.Next(8);
                WorldBuilder.Box(root, new Vector3(x, -6 + h * 0.5f, z), new Vector3(5, h, 5), Tx("nether_bricks", 5, h));
                WorldBuilder.Box(root, new Vector3(x, -6 + h + 0.5f, z), new Vector3(6, 1, 6), Tx("nether_bricks", 6, 1));
            }
        }
        // glowstone clusters in the sky
        for (float z = 0; z < length + 200; z += 15f + rnd.Next(15))
            WorldBuilder.Box(root, new Vector3(rnd.Next(-40, 40), 22 + rnd.Next(10), z), new Vector3(3, 2, 3), Tx("glowstone", 3, 2, 2f));
    }
}

// Square sun, slow flat clouds, embers in the nether, a couple of lights that follow the camera in the caves.
public class McFx : MonoBehaviour
{
    RunTheme theme;
    Camera cam;
    Transform sky, clouds, embers;
    Light[] pool;
    Light lantern;

    public void Setup(RunTheme t, float length)
    {
        theme = t; cam = Camera.main;
        if (t == RunTheme.McPlains)
        {
            sky = new GameObject("Sky").transform; sky.SetParent(WorldBuilder.Dyn);
            var sun = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(sun.GetComponent<Collider>());
            sun.transform.SetParent(sky, false);
            sun.transform.localPosition = new Vector3(40, 110, 330); sun.transform.localScale = Vector3.one * 34;
            var sm = new Material(Shader.Find("Universal Render Pipeline/Unlit")); sm.SetColor("_BaseColor", new Color(1.6f, 1.5f, 0.9f));
            sun.GetComponent<Renderer>().sharedMaterial = sm;
            clouds = new GameObject("Clouds").transform; clouds.SetParent(WorldBuilder.Dyn);
            var cm = WorldBuilder.Emissive(Color.white, 0.35f);
            var r = new System.Random(4);
            for (float z = -40; z < length + 400; z += 12f + r.Next(14))
                WorldBuilder.Box(clouds, new Vector3(r.Next(-120, 120), 42f, z), new Vector3(8 + r.Next(4) * 4, 1.6f, 6 + r.Next(3) * 4), cm);
        }
        if (t != RunTheme.McPlains)
        {
            pool = new Light[5];
            for (int i = 0; i < pool.Length; i++)
            {
                var g = new GameObject("Torchlight" + i); g.transform.SetParent(WorldBuilder.Dyn);
                pool[i] = g.AddComponent<Light>(); pool[i].type = LightType.Point; pool[i].range = 9; pool[i].color = new Color(1f, 0.65f, 0.3f); pool[i].intensity = 0; pool[i].shadows = LightShadows.None;
            }
            var lg = new GameObject("Lantern"); lantern = lg.AddComponent<Light>(); lantern.type = LightType.Point;
            lantern.range = 20; lantern.intensity = t == RunTheme.McCaves ? 2.6f : 1.4f; lantern.color = new Color(1f, 0.8f, 0.6f); lantern.shadows = LightShadows.None;
        }
        if (t == RunTheme.McNether)
        {
            var eg = new GameObject("Embers"); embers = eg.transform; embers.SetParent(WorldBuilder.Dyn);
            var ps = eg.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.startLifetime = 4f; main.startSpeed = 0.5f; main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.1f), new Color(1f, 0.85f, 0.3f)); main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 600;
            var em = ps.emission; em.rateOverTime = 120;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(30, 6, 40);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World; vel.x = new ParticleSystem.MinMaxCurve(0f); vel.y = new ParticleSystem.MinMaxCurve(1.2f); vel.z = new ParticleSystem.MinMaxCurve(0f);
            var pr = eg.GetComponent<ParticleSystemRenderer>();
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); m.SetColor("_BaseColor", new Color(1f, 0.6f, 0.2f));
            pr.sharedMaterial = m;
            ps.Play();
        }
    }

    void OnDestroy() { if (lantern) Destroy(lantern.gameObject); }

    void LateUpdate()
    {
        if (!cam) return;
        var cp = cam.transform.position;
        if (sky) sky.position = new Vector3(0, 0, cp.z);
        if (clouds) clouds.position += Vector3.forward * Time.deltaTime * 1.2f;
        if (embers) embers.position = cp + new Vector3(0, 1, 18);
        if (lantern) lantern.transform.position = cp + cam.transform.forward * 3f + Vector3.up * 0.5f;
        if (pool != null)
        {
            int k = 0;
            foreach (var sp in McWorld.LightSpots)
            {
                if (k >= pool.Length) break;
                float dz = sp.z - cp.z;
                if (dz < -4f || dz > 40f) continue;
                pool[k].transform.position = sp;
                pool[k].intensity = 2.6f * (0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 8f + k, k));
                k++;
            }
            for (; k < pool.Length; k++) pool[k].intensity = 0;
        }
    }
}
