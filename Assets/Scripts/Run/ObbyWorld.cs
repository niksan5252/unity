using System.Collections.Generic;
using UnityEngine;

// Roblox-style obby: a floating studded-brick path (with real gaps at JUMP cues) high in the sky.
public static class ObbyWorld
{
    static readonly Dictionary<(Color, Vector2, float), Material> cache = new Dictionary<(Color, Vector2, float), Material>();
    public static Texture2D Studs, Arrows;

    public static Material Brick(Color c, float u = 1, float v = 1, float glow = 0)
    {
        var key = (c, new Vector2(u, v), glow);
        if (cache.TryGetValue(key, out var m) && m) return m;
        m = WorldBuilder.Mat(c);
        if (Studs) { m.SetTexture("_BaseMap", Studs); m.SetTextureScale("_BaseMap", new Vector2(u, v)); }
        m.SetFloat("_Smoothness", 0.35f);
        if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); }
        return cache[key] = m;
    }

    public static readonly Color[] Bright =
    {
        new Color(1f, 0.25f, 0.25f), new Color(1f, 0.6f, 0.1f), new Color(1f, 0.85f, 0.15f), new Color(0.3f, 0.85f, 0.3f),
        new Color(0.2f, 0.6f, 1f), new Color(0.6f, 0.35f, 1f), new Color(1f, 0.4f, 0.75f),
    };

    public static void Build(RunLibrary lib, RunTheme theme, Transform root, float length, System.Random rnd)
    {
        Studs = lib.studs; Arrows = lib.arrows;
        var sun = Object.FindAnyObjectByType<Light>();
        if (sun) { sun.intensity = theme == RunTheme.LavaObby ? 1.1f : 1.35f; sun.color = theme == RunTheme.LavaObby ? new Color(1f, 0.8f, 0.6f) : Color.white; }

        // gaps in the path at every JUMP cue
        var gaps = new List<Vector2>();
        foreach (var c in WorldBuilder.Cues)
            if (c.move == RunMove.Jump) gaps.Add(new Vector2(c.time * WorldBuilder.Speed - 1.7f, c.time * WorldBuilder.Speed + 1.7f));

        // path: 4 m segments, colors per theme
        const float seg = 4f;
        int idx = 0;
        for (float z = -30f; z < length + 80f; z += seg, idx++)
        {
            float a = z, b = z + seg;
            foreach (var g in gaps)
                if (b > g.x && a < g.y) { if (a < g.x) b = g.x; else if (b > g.y) a = g.y; else { a = b; } }
            if (b - a < 0.2f) continue;
            Color col = theme switch
            {
                RunTheme.SkyObby => Bright[idx % Bright.Length],
                RunTheme.LavaObby => idx % 2 == 0 ? new Color(0.25f, 0.25f, 0.3f) : new Color(0.4f, 0.4f, 0.45f),
                _ => Color.HSVToRGB((idx * 0.07f) % 1f, 0.65f, 1f),
            };
            WorldBuilder.Box(root, new Vector3(0, -0.5f, (a + b) * 0.5f), new Vector3(8.6f, 1f, b - a), Brick(col, 8.6f / 2, (b - a) / 2));
            // rim trims
            foreach (int s in new[] { -1, 1 })
                WorldBuilder.Box(root, new Vector3(s * 4.45f, 0.15f, (a + b) * 0.5f), new Vector3(0.3f, 0.3f, b - a), Brick(Color.white, 0.15f, (b - a) / 2));
            if (theme == RunTheme.RainbowObby && idx % 6 == 3 && Arrows)
            {
                var pad = WorldBuilder.Emissive(new Color(0.3f, 1f, 1f), 1.4f);
                pad.SetTexture("_BaseMap", Arrows); pad.SetTexture("_EmissionMap", Arrows);
                WorldBuilder.Box(root, new Vector3(0, 0.02f, (a + b) * 0.5f), new Vector3(2.2f, 0.04f, 2.2f), pad);
            }
        }
        // red kill-bricks on the far edge of every gap
        var kill = Brick(new Color(1f, 0.1f, 0.1f), 4, 0.5f, 1.5f);
        foreach (var g in gaps)
            WorldBuilder.Box(root, new Vector3(0, -0.45f, g.x - 0.5f), new Vector3(8.6f, 1.1f, 1f), kill);

        // scenery around the path
        var cloud = WorldBuilder.Emissive(Color.white, theme == RunTheme.LavaObby ? 0.1f : 0.35f);
        if (theme == RunTheme.LavaObby)
        {
            var lava = WorldBuilder.Emissive(new Color(1f, 0.35f, 0.05f), 2.5f);
            WorldBuilder.Box(root, new Vector3(0, -18f, length * 0.5f), new Vector3(400f, 1f, length + 400f), lava);
        }
        else
        {
            for (float z = -20; z < length + 200; z += 10f + rnd.Next(14))
                WorldBuilder.Box(root, new Vector3(rnd.Next(-90, 90), -12f - rnd.Next(10), z), new Vector3(14 + rnd.Next(20), 3f, 8 + rnd.Next(12)), cloud);
        }
        for (float z = -20; z < length + 200; z += 16f + rnd.Next(16))
            WorldBuilder.Box(root, new Vector3(rnd.Next(-90, 90), 26f + rnd.Next(14), z), new Vector3(12 + rnd.Next(16), 2f, 6 + rnd.Next(10)), cloud);

        foreach (int s in new[] { -1, 1 })
        {
            // other obby structures floating around: platforms, stair runs, towers
            for (float z = 0; z < length + 150; z += 9f + rnd.Next(9))
            {
                float x = s * (9f + rnd.Next(30));
                float y = -6f + rnd.Next(14);
                var col = theme == RunTheme.LavaObby ? (rnd.NextDouble() < 0.4 ? new Color(1f, 0.15f, 0.1f) : new Color(0.3f, 0.3f, 0.35f)) : Bright[rnd.Next(Bright.Length)];
                int kind = rnd.Next(3);
                if (kind == 0)
                    WorldBuilder.Box(root, new Vector3(x, y, z), new Vector3(3 + rnd.Next(3), 0.8f, 3 + rnd.Next(3)), Brick(col, 2, 2));
                else if (kind == 1)
                    for (int k = 0; k < 4; k++)
                        WorldBuilder.Box(root, new Vector3(x, y + k * 1.2f, z + k * 2.2f), new Vector3(2f, 0.6f, 2f), Brick(Bright[(rnd.Next(Bright.Length) + k) % Bright.Length], 1, 1));
                else
                {
                    float h = 8 + rnd.Next(18);
                    WorldBuilder.Box(root, new Vector3(x * 1.4f, y - 10 + h * 0.5f, z), new Vector3(4f, h, 4f), Brick(col, 2, h / 2));
                    WorldBuilder.Box(root, new Vector3(x * 1.4f, y - 10 + h + 0.3f, z), new Vector3(5f, 0.6f, 5f), Brick(Color.white, 2.5f, 2.5f));
                }
            }
        }
    }
}
