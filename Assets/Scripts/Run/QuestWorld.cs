using System.Collections.Generic;
using UnityEngine;

// Move Quest Run worlds (Minecraft-style, own pixel textures): snowy taiga at sunset, jungle temple, the End.
public static class QuestWorld
{
    static System.Random rnd;
    static Texture2D T(string n) => BlockFigure.Tex.TryGetValue(n, out var t) ? t : null;
    static Material Tx(string n, float u = 1, float v = 1, float glow = 0) => BlockWorld.Tex(T(n), u, v, glow);
    static GameObject B(Transform p, Vector3 pos, Vector3 size, string tex, float glow = 0) => WorldBuilder.Box(p, pos, size, Tx(tex, size.x, size.y, glow));

    static void Column(Transform p, float x, float z, float w, int h, string body, string cap, string top)
    {
        if (h > 1) WorldBuilder.Box(p, new Vector3(x, (h - 1) * 0.5f, z), new Vector3(w, h - 1, w), Tx(body, w, h - 1));
        WorldBuilder.Box(p, new Vector3(x, h - 0.5f, z), new Vector3(w, 1, w), Tx(cap, w, 1));
        WorldBuilder.Box(p, new Vector3(x, h + 0.005f, z), new Vector3(w, 0.01f, w), Tx(top, w, w));
    }

    // cone-ish spruce: log + stacked leaf layers with snow caps
    static void Spruce(Transform p, Vector3 b, bool snowy)
    {
        int h = 6 + rnd.Next(4);
        WorldBuilder.Box(p, b + new Vector3(0, h * 0.5f, 0), new Vector3(1, h, 1), Tx("spruce_log", 1, h));
        int w = 5;
        for (float y = 2; y < h; y += 1.5f, w = Mathf.Max(1, w - 1))
        {
            WorldBuilder.Box(p, b + new Vector3(0, y + 0.5f, 0), new Vector3(w, 1, w), Tx("spruce_leaves", w, 1));
            if (snowy) WorldBuilder.Box(p, b + new Vector3(0, y + 1.02f, 0), new Vector3(w, 0.08f, w), Tx("snow", w, w));
        }
        WorldBuilder.Box(p, b + new Vector3(0, h + 0.5f, 0), Vector3.one, Tx(snowy ? "snow" : "spruce_leaves"));
    }

    static void JungleTree(Transform p, Vector3 b, Material vine)
    {
        int h = 9 + rnd.Next(7);
        WorldBuilder.Box(p, b + new Vector3(0, h * 0.5f, 0), new Vector3(2, h, 2), Tx("jungle_log", 2, h));
        WorldBuilder.Box(p, b + new Vector3(0, h, 0), new Vector3(7, 2, 7), Tx("jungle_leaves", 7, 2));
        WorldBuilder.Box(p, b + new Vector3(0, h + 1.5f, 0), new Vector3(4, 1, 4), Tx("jungle_leaves", 4, 1));
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90 + rnd.Next(30);
            var o = Quaternion.Euler(0, a, 0) * new Vector3(3.6f, 0, 0);
            float len = 2 + rnd.Next(5);
            WorldBuilder.Box(p, b + o + new Vector3(0, h - 1 - len * 0.5f, 0), new Vector3(0.12f, len, 0.6f), vine);
        }
    }

    static void Igloo(Transform p, Vector3 o)
    {
        for (int ring = 0; ring < 4; ring++)
        {
            float r = 3f - ring * 0.8f;
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6;
                B(p, o + new Vector3(Mathf.Cos(a) * r, 0.5f + ring, Mathf.Sin(a) * r), Vector3.one, "snow");
            }
        }
        B(p, o + new Vector3(0, 4.5f, 0), Vector3.one, "snow");
    }

    static void Temple(Transform p, Vector3 o, int s)
    {
        int lv = 4;
        for (int i = 0; i < lv; i++)
        {
            float w = 12 - i * 2.5f;
            B(p, o + new Vector3(0, i * 2.5f + 1.25f, 0), new Vector3(w, 2.5f, w), i % 2 == 0 ? "mossy_cobble" : "stone_bricks");
        }
        var glow = WorldBuilder.Emissive(new Color(0.3f, 1f, 0.5f), 3f);
        WorldBuilder.Box(p, o + new Vector3(-s * 6.05f, 1.6f, 0), new Vector3(0.1f, 2.4f, 2f), WorldBuilder.Mat(new Color(0.05f, 0.05f, 0.05f)));   // doorway
        WorldBuilder.Box(p, o + new Vector3(0, lv * 2.5f + 0.6f, 0), new Vector3(1.2f, 1.2f, 1.2f), glow);                                             // emerald on top
    }

    public static void Build(RunLibrary lib, RunTheme theme, Transform root, float length, System.Random r)
    {
        rnd = r;
        McWorld.LightSpots.Clear();
        float L = length + 160f, half = length * 0.5f + 60f;
        var cam = Camera.main;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        Color sky = theme == RunTheme.McTaiga ? new Color(1f, 0.62f, 0.45f) : theme == RunTheme.McJungle ? new Color(0.55f, 0.85f, 0.75f) : new Color(0.1f, 0.03f, 0.18f);
        RenderSettings.fogColor = theme == RunTheme.McTaiga ? new Color(1f, 0.75f, 0.65f) : sky;
        RenderSettings.fogStartDistance = theme == RunTheme.McJungle ? 40f : 60f;
        RenderSettings.fogEndDistance = theme == RunTheme.McJungle ? 170f : theme == RunTheme.McEnd ? 260f : 240f;
        RenderSettings.ambientLight = theme == RunTheme.McTaiga ? new Color(0.82f, 0.72f, 0.78f) : theme == RunTheme.McJungle ? new Color(0.68f, 0.8f, 0.66f) : new Color(0.66f, 0.62f, 0.66f);
        if (cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = sky; }
        foreach (var dl in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (dl.type == LightType.Directional)
            {
                dl.intensity = theme == RunTheme.McEnd ? 0.55f : 1.3f;
                dl.color = theme == RunTheme.McTaiga ? new Color(1f, 0.75f, 0.55f) : theme == RunTheme.McJungle ? new Color(1f, 0.98f, 0.85f) : new Color(0.8f, 0.6f, 1f);
                dl.transform.rotation = Quaternion.Euler(theme == RunTheme.McTaiga ? 18f : 50f, -30f, 0f);
            }

        switch (theme)
        {
            case RunTheme.McTaiga: Taiga(root, length, L, half); break;
            case RunTheme.McJungle: Jungle(root, length, L, half); break;
            default: End(root, length, L, half); break;
        }
        root.gameObject.AddComponent<QuestFx>().Setup(theme, length);
    }

    static void Herd(Transform root, float length, BlockFigure.Kind[] kinds, float minX, float maxX, float every)
    {
        foreach (int s in new[] { -1, 1 })
            for (float z = 10; z < length + 120; z += every + rnd.Next((int)every))
            {
                var k = kinds[rnd.Next(kinds.Length)];
                int n = 1 + rnd.Next(2);
                for (int i = 0; i < n; i++)
                {
                    var f = BlockFigure.Create(k, WorldBuilder.Dyn);
                    f.transform.position = new Vector3(s * (minX + (float)rnd.NextDouble() * (maxX - minX)), 0, z + i * 2.5f);
                    f.transform.rotation = Quaternion.Euler(0, rnd.Next(360), 0);
                    f.state = rnd.NextDouble() < 0.5 ? "Graze" : "Idle";
                    f.speedMul = 0.5f + (float)rnd.NextDouble() * 0.5f;
                }
            }
    }

    // ---------------- snowy taiga at sunset ----------------
    static void Taiga(Transform root, float length, float L, float half)
    {
        WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(400, 0.2f, L + 300), Tx("snow", 400, L + 300));
        WorldBuilder.Box(root, new Vector3(0, 0.005f, half), new Vector3(8.4f, 0.02f, L), Tx("gravel", 8.4f, L));
        foreach (int s in new[] { -1, 1 })
        {
            for (float z = -20; z < length + 120; z += 1f) Column(root, s * 4.7f, z, 1f, 1, "dirt", "snow_side", "snow");
            for (float z = -10; z < length + 140; z += 3.5f + rnd.Next(4)) Spruce(root, new Vector3(s * (7f + rnd.Next(14)), 0, z), rnd.NextDouble() < 0.7);
            for (float z = 40; z < length + 120; z += 70f + rnd.Next(30))
                WorldBuilder.Box(root, new Vector3(s * (12f + rnd.Next(5)), 0.03f, z), new Vector3(10, 0.06f, 14), Tx("ice", 10, 14, 0.1f));   // frozen ponds
            for (float z = 25; z < length + 120; z += 55f + rnd.Next(30)) Igloo(root, new Vector3(s * (16f + rnd.Next(5)), 0, z));
            // snowy mountains far away
            for (float z = -20; z < length + 300; z += 2.5f)
            {
                int h = 6 + (int)(Mathf.PerlinNoise(z * 0.02f, s * 7.7f) * 22f);
                Column(root, s * (45f + (z * 13 % 9)), z, 3f, h, "stone", "stone", "snow");
                B(root, new Vector3(s * (45f + (z * 13 % 9)), h - 1.5f, z), new Vector3(3.02f, 3f, 3.02f), "snow");
            }
        }
        Herd(root, length, new[] { BlockFigure.Kind.PolarBear, BlockFigure.Kind.Wolf, BlockFigure.Kind.Wolf }, 6.5f, 14f, 16f);
    }

    // ---------------- jungle temple ----------------
    static void Jungle(Transform root, float length, float L, float half)
    {
        WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(400, 0.2f, L + 300), Tx("jungle_grass", 400, L + 300));
        WorldBuilder.Box(root, new Vector3(0, 0.005f, half), new Vector3(8.4f, 0.02f, L), Tx("mossy_cobble", 8.4f, L));
        var vine = WorldBuilder.Mat(new Color(0.18f, 0.5f, 0.15f));
        var flame = WorldBuilder.Emissive(new Color(1f, 0.75f, 0.3f), 6f);
        foreach (int s in new[] { -1, 1 })
        {
            for (float z = -20; z < length + 120; z += 1f) B(root, new Vector3(s * 4.7f, 0.5f, z), Vector3.one, rnd.NextDouble() < 0.5 ? "mossy_cobble" : "stone_bricks");
            for (float z = -10; z < length + 140; z += 4f + rnd.Next(4)) JungleTree(root, new Vector3(s * (8f + rnd.Next(12)), 0, z), vine);
            for (float z = 0; z < length + 120; z += 2f + rnd.Next(3))
            {
                if (rnd.NextDouble() < 0.4) B(root, new Vector3(s * (6f + rnd.Next(6)), 0.5f, z), Vector3.one, "melon");
                else WorldBuilder.Box(root, new Vector3(s * (5.8f + rnd.Next(6)), 1.2f, z), new Vector3(1.6f, 2.4f, 1.6f), Tx("jungle_leaves", 1.6f, 2.4f));   // bushes
            }
            // bamboo groves
            for (float z = 5; z < length + 120; z += 9f + rnd.Next(8))
                for (int k = 0; k < 4; k++)
                {
                    float hh = 4 + rnd.Next(5);
                    WorldBuilder.Box(root, new Vector3(s * (6.2f + k * 0.6f), hh * 0.5f, z + k * 0.7f), new Vector3(0.3f, hh, 0.3f), WorldBuilder.Mat(new Color(0.45f, 0.75f, 0.2f)));
                }
            for (float z = 40; z < length + 120; z += 80f + rnd.Next(30)) Temple(root, new Vector3(s * (20f + rnd.Next(6)), 0, z), s);
            // stone torch pillars along the road
            for (float z = 6; z < length + 120; z += 14f)
            {
                B(root, new Vector3(s * 5.6f, 1.25f, z), new Vector3(1, 2.5f, 1), "stone_bricks");
                WorldBuilder.Box(root, new Vector3(s * 5.6f, 2.75f, z), Vector3.one * 0.3f, flame);
            }
        }
        Herd(root, length, new[] { BlockFigure.Kind.Ocelot, BlockFigure.Kind.Chicken, BlockFigure.Kind.Pig }, 6f, 12f, 14f);
    }

    // ---------------- the End ----------------
    static void End(Transform root, float length, float L, float half)
    {
        WorldBuilder.Box(root, new Vector3(0, -0.5f, half), new Vector3(12f, 1f, L), Tx("end_stone", 12f, L));
        var crystal = WorldBuilder.Emissive(new Color(1f, 0.45f, 0.95f), 4f);
        foreach (int s in new[] { -1, 1 })
        {
            for (float z = -20; z < length + 120; z += 1f)
                if (rnd.NextDouble() < 0.6) B(root, new Vector3(s * (5.5f + rnd.Next(2)), -0.5f, z), Vector3.one, "end_stone");
            // obsidian pillars with glowing crystals
            for (float z = 10; z < length + 140; z += 18f + rnd.Next(10))
            {
                float x = s * (14f + rnd.Next(14)); int h = 12 + rnd.Next(20);
                B(root, new Vector3(x, h * 0.5f - 6, z), new Vector3(3, h, 3), "obsidian");
                var c = WorldBuilder.Box(root, new Vector3(x, h - 4.5f, z), Vector3.one * 1.4f, crystal);
                c.transform.rotation = Quaternion.Euler(45, 45, 0);
                McWorld.LightSpots.Add(new Vector3(x, h - 4.5f, z));
            }
            // purpur towers
            for (float z = 40; z < length + 140; z += 60f + rnd.Next(30))
            {
                float x = s * (24f + rnd.Next(10)); int h = 18 + rnd.Next(12);
                B(root, new Vector3(x, h * 0.5f - 6, z), new Vector3(5, h, 5), "purpur");
                B(root, new Vector3(x, h - 5.5f, z), new Vector3(7, 1, 7), "purpur");
            }
            // floating end-stone islands
            for (float z = 0; z < length + 200; z += 14f + rnd.Next(14))
            {
                float x = s * (10f + rnd.Next(40)), y = -14 + rnd.Next(30);
                float w = 4 + rnd.Next(6);
                B(root, new Vector3(x, y, z), new Vector3(w, 2, w), "end_stone");
                B(root, new Vector3(x, y - 1.5f, z), new Vector3(w * 0.6f, 1.5f, w * 0.6f), "end_stone");
            }
        }
        // the exit portal at the end of the road
        var portal = WorldBuilder.Emissive(new Color(0.3f, 0.1f, 0.6f), 3f);
        for (int i = -3; i <= 3; i++)
        {
            B(root, new Vector3(i * 1f, 0.5f, length + 40f), Vector3.one, "end_stone");
            B(root, new Vector3(i * 1f, 6.5f, length + 40f), Vector3.one, "end_stone");
        }
        WorldBuilder.Box(root, new Vector3(0, 3.5f, length + 40f), new Vector3(7, 5, 0.3f), portal);
        McWorld.LightSpots.Sort((a, b) => a.z.CompareTo(b.z));
    }
}

// Falling snow (taiga), leaves/fireflies (jungle), stars + floating particles + crystal light (End).
public class QuestFx : MonoBehaviour
{
    RunTheme theme;
    Camera cam;
    Transform follow, sky;
    Light[] pool;

    ParticleSystem Particles(string name, Color a, Color b, float rate, float speedY, float size, Vector3 box, bool glow)
    {
        var g = new GameObject(name); g.transform.SetParent(WorldBuilder.Dyn);
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.startLifetime = 5f; main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size); main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 2500;
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f); vel.y = new ParticleSystem.MinMaxCurve(speedY); vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        var m = new Material(Shader.Find(glow ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Particles/Unlit"));
        m.SetColor("_BaseColor", Color.white);
        g.GetComponent<ParticleSystemRenderer>().sharedMaterial = m;
        ps.Play();
        return ps;
    }

    public void Setup(RunTheme t, float length)
    {
        theme = t; cam = Camera.main;
        if (t == RunTheme.McTaiga)
            follow = Particles("Snow", Color.white, new Color(0.9f, 0.95f, 1f), 500, -1.8f, 0.14f, new Vector3(40, 1, 50), false).transform;
        else if (t == RunTheme.McJungle)
            follow = Particles("Fireflies", new Color(1f, 1f, 0.5f), new Color(0.7f, 1f, 0.4f), 60, 0.3f, 0.1f, new Vector3(30, 6, 40), true).transform;
        else
        {
            follow = Particles("EndDust", new Color(0.8f, 0.4f, 1f), new Color(1f, 0.6f, 1f), 120, 0.4f, 0.12f, new Vector3(36, 10, 46), true).transform;
            sky = new GameObject("Stars").transform; sky.SetParent(WorldBuilder.Dyn);
            var tex = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var px = new Color32[1024 * 512]; var r = new System.Random(3);
            for (int i = 0; i < 1600; i++) { byte v = (byte)(140 + r.Next(115)); px[r.Next(512) * 1024 + r.Next(1024)] = new Color32(v, (byte)(v * 0.85f), v, v); }
            tex.SetPixels32(px); tex.Apply();
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(sky, false); q.transform.localPosition = new Vector3(0, 70, 380); q.transform.localScale = new Vector3(1100, 520, 1);
            q.GetComponent<Renderer>().sharedMaterial = HalloweenWorld.Transparent(tex, Color.white);
            pool = new Light[4];
            for (int i = 0; i < pool.Length; i++)
            {
                var g = new GameObject("CrystalLight" + i); g.transform.SetParent(WorldBuilder.Dyn);
                pool[i] = g.AddComponent<Light>(); pool[i].type = LightType.Point; pool[i].range = 18; pool[i].color = new Color(1f, 0.5f, 1f); pool[i].intensity = 0; pool[i].shadows = LightShadows.None;
            }
        }
    }

    void LateUpdate()
    {
        if (!cam) return;
        var cp = cam.transform.position;
        if (follow) follow.position = cp + new Vector3(0, theme == RunTheme.McTaiga ? 12f : 2f, 18f);
        if (sky) sky.position = new Vector3(0, 0, cp.z);
        if (pool != null)
        {
            int k = 0;
            foreach (var sp in McWorld.LightSpots)
            {
                if (k >= pool.Length) break;
                float dz = sp.z - cp.z;
                if (dz < -6f || dz > 60f) continue;
                pool[k].transform.position = sp; pool[k].intensity = 4f; k++;
            }
            for (; k < pool.Length; k++) pool[k].intensity = 0;
        }
    }
}
