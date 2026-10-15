using System.Collections.Generic;
using UnityEngine;

// Minecraft-style block worlds: textured 1 m blocks, terraces, mineshaft, fiery cave.
public static class BlockWorld
{
    static readonly Dictionary<(Texture2D, Vector2, float, Color), Material> cache = new Dictionary<(Texture2D, Vector2, float, Color), Material>();
    public static Color GrassTint = new Color(0.8f, 1.12f, 0.62f);

    public static Material Tex(Texture2D t, float u = 1, float v = 1, float glow = 0) => Tex(t, u, v, glow, Color.white);

    public static Material Tex(Texture2D t, float u, float v, float glow, Color tint)
    {
        var key = (t, new Vector2(u, v), glow, tint);
        if (cache.TryGetValue(key, out var m) && m) return m;
        m = WorldBuilder.Mat(tint);
        if (t)
        {
            m.SetTexture("_BaseMap", t);
            m.SetTextureScale("_BaseMap", new Vector2(u, v));
        }
        m.SetFloat("_Smoothness", 0.05f);
        if (glow > 0)
        {
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", t);
            m.SetTextureScale("_EmissionMap", new Vector2(u, v));
            m.SetColor("_EmissionColor", Color.white * glow);
        }
        return cache[key] = m;
    }

    public static GameObject Block(Transform p, Vector3 pos, Texture2D t, float size = 1f, float glow = 0) =>
        WorldBuilder.Box(p, pos, Vector3.one * size, Tex(t, 1, 1, glow));

    // a column of blocks: dirt body, grass-edged top block, grass top face
    static void Column(Transform p, float x, float z, float w, int h, RunLibrary lib, Texture2D body, Texture2D cap, Texture2D top)
    {
        if (h > 1) WorldBuilder.Box(p, new Vector3(x, (h - 1) * 0.5f, z), new Vector3(w, h - 1, w), Tex(body, w, h - 1));
        WorldBuilder.Box(p, new Vector3(x, h - 0.5f, z), new Vector3(w, 1f, w), Tex(cap, w, 1));
        WorldBuilder.Box(p, new Vector3(x, h + 0.005f, z), new Vector3(w, 0.01f, w), Tex(top, w, w, 0, top == lib.grassTop ? GrassTint : Color.white));
    }

    // small blocky house: plank walls, trunk corners, glass windows, stepped roof
    static void House(Transform p, Vector3 o, RunLibrary lib, System.Random rnd)
    {
        float w = 5, d = 5, h = 3;
        WorldBuilder.Box(p, o + new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), Tex(lib.planks, w, h));
        foreach (var c in new[] { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(-1, 0, 1), new Vector3(1, 0, 1) })
            WorldBuilder.Box(p, o + new Vector3(c.x * (w * 0.5f), h * 0.5f, c.z * (d * 0.5f)), new Vector3(1.02f, h, 1.02f), Tex(lib.trunk, 1, h));
        var glass = WorldBuilder.Emissive(new Color(0.6f, 0.85f, 1f), 0.6f);
        foreach (int sx in new[] { -1, 1 })
            WorldBuilder.Box(p, o + new Vector3(sx * (w * 0.5f + 0.01f), 1.6f, 0), new Vector3(0.05f, 1f, 1f), glass);
        for (int i = 0; i < 3; i++)
            WorldBuilder.Box(p, o + new Vector3(0, h + 0.5f + i, 0), new Vector3(w + 1 - i * 2, 1, d + 1 - i * 1.6f), Tex(lib.netherrack, w + 1 - i * 2, 1));
    }

    static Material cloud;
    static void Clouds(Transform p, float length, System.Random rnd)
    {
        if (!cloud) { cloud = WorldBuilder.Emissive(Color.white, 0.35f); }
        for (float z = 0; z < length + 300; z += 18f + rnd.Next(20))
        {
            float x = rnd.Next(-70, 70), y = 28 + rnd.Next(10);
            WorldBuilder.Box(p, new Vector3(x, y, z), new Vector3(10 + rnd.Next(14), 2f, 6 + rnd.Next(8)), cloud);
        }
    }

    public static void Build(RunLibrary lib, RunTheme theme, Transform root, float length, System.Random rnd)
    {
        float L = length + 160f, half = length * 0.5f + 60f;
        switch (theme)
        {
            case RunTheme.Village:
            {
                WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(120, 0.2f, L), Tex(lib.grassTop, 120, L, 0, GrassTint));
                Clouds(root, length, rnd);
                WorldBuilder.Box(root, new Vector3(0, 0.01f, half), new Vector3(8.4f, 0.02f, L), Tex(lib.gravel, 8.4f, L));
                foreach (int s in new[] { -1, 1 })
                {
                    for (float z = -20; z < length + 120; z += 2f)
                    {
                        Column(root, s * 5.5f, z, 2f, 1, lib, lib.dirt, lib.dirtGrass, lib.grassTop);
                        Column(root, s * 7.5f, z, 2f, rnd.NextDouble() < 0.15 ? 2 : 1, lib, lib.dirt, lib.dirtGrass, lib.grassTop);
                    }
                    for (float z = -20; z < length + 120; z += 8f + rnd.Next(6))
                        Column(root, s * (25f + rnd.Next(8)), z, 6f, 4 + rnd.Next(7), lib, lib.dirt, lib.dirtGrass, lib.grassTop);
                    for (float z = 0; z < length + 100; z += 7f + rnd.Next(6))
                    {
                        var t = Pick(lib.blockTrees, rnd);
                        if (t) Fit(WorldBuilder.Spawn(t, new Vector3(s * (10f + rnd.Next(3)), 0, z), rnd.Next(4) * 90, root), 5f + rnd.Next(3));
                    }
                    // herds of animals in the meadow beyond the terraces
                    for (float z = 6; z < length + 100; z += 12f + rnd.Next(10))
                    {
                        var a = Pick(lib.animals, rnd);
                        int n = 2 + rnd.Next(3);
                        for (int k = 0; k < n && a; k++)
                            Fit(WorldBuilder.Spawn(a, new Vector3(s * (9.2f + (float)rnd.NextDouble() * 4f), 0, z + (float)rnd.NextDouble() * 6f), rnd.Next(360), root), a.name.Contains("Horse") ? 1.7f : a.name.Contains("Chick") ? 0.6f : 1.05f);
                    }
                    // flowers, bushes, mushrooms on the meadow
                    for (float z = 0; z < length + 100; z += 2.5f + rnd.Next(3))
                    {
                        var f = Pick(lib.flowers, rnd);
                        if (f) Fit(WorldBuilder.Spawn(f, new Vector3(s * (8.8f + (float)rnd.NextDouble() * 9f), 0, z), rnd.Next(360), root), 0.5f + (float)rnd.NextDouble() * 0.6f);
                    }
                    // houses behind the trees
                    for (float z = 20; z < length + 100; z += 34f + rnd.Next(20))
                        House(root, new Vector3(s * (17f + rnd.Next(4)), 0, z), lib, rnd);
                }
                break;
            }
            case RunTheme.Mine:
            {
                WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(14, 0.2f, L), Tex(lib.gravel, 14, L));
                foreach (int s in new[] { -1, 1 })
                    WorldBuilder.Box(root, new Vector3(s * 6f, 3f, half), new Vector3(1f, 6f, L), Tex(lib.stone, L, 6));
                WorldBuilder.Box(root, new Vector3(0, 6.5f, half), new Vector3(13f, 1f, L), Tex(lib.stone, 13, L));
                var torch = WorldBuilder.Emissive(new Color(1f, 0.7f, 0.25f), 6f);
                for (float z = 0; z < length + 100; z += 1f)
                    foreach (int s in new[] { -1, 1 })
                        if (rnd.NextDouble() < 0.35)
                            Block(root, new Vector3(s * 5.45f, 0.5f + rnd.Next(5), z), lib.ores[rnd.Next(lib.ores.Length)]);
                for (float z = 0; z < length + 100; z += 10f)
                {
                    // mineshaft frame: two trunk posts + plank beam
                    foreach (int s in new[] { -1, 1 })
                    {
                        WorldBuilder.Box(root, new Vector3(s * 4.9f, 2.5f, z), new Vector3(0.6f, 5f, 0.6f), Tex(lib.trunk, 1, 5));
                        WorldBuilder.Box(root, new Vector3(s * 4.55f, 3.3f, z + 5f), new Vector3(0.15f, 0.3f, 0.15f), Tex(lib.planks));
                        WorldBuilder.Box(root, new Vector3(s * 4.55f, 3.55f, z + 5f), new Vector3(0.22f, 0.22f, 0.22f), torch);
                    }
                    WorldBuilder.Box(root, new Vector3(0, 5.3f, z), new Vector3(10.4f, 0.6f, 0.6f), Tex(lib.planks, 10, 1));
                    var pr = Pick(lib.mineProps, rnd);
                    if (pr && rnd.NextDouble() < 0.5) Fit(WorldBuilder.Spawn(pr, new Vector3((rnd.Next(2) * 2 - 1) * 4.6f, 0, z + 3f), rnd.Next(360), root), 1.2f);
                }
                break;
            }
            default: // Nether
            {
                WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(9, 0.2f, L), Tex(lib.netherrack, 9, L));
                foreach (int s in new[] { -1, 1 })
                {
                    WorldBuilder.Box(root, new Vector3(s * 12f, -0.05f, half), new Vector3(15f, 0.2f, L), Tex(lib.lava, 15, L, 1.6f));
                    for (float z = -20; z < length + 120; z += 2f)
                        Column(root, s * 5.5f, z, 2f, 1 + rnd.Next(2), lib, lib.netherrack, lib.netherrack, lib.netherrack);
                    for (float z = -20; z < length + 120; z += 6f + rnd.Next(8))
                    {
                        float x = s * (20f + rnd.Next(8)); int h = 6 + rnd.Next(14);
                        Column(root, x, z, 5f, h, lib, lib.netherrack, lib.netherrack, lib.netherrack);
                        if (rnd.NextDouble() < 0.3) // lava fall
                            WorldBuilder.Box(root, new Vector3(x - s * 2.6f, h * 0.5f, z), new Vector3(0.3f, h, 2f), Tex(lib.lava, 2, h, 2.2f));
                    }
                    for (float z = 5; z < length + 100; z += 9f + rnd.Next(8))
                    {
                        var pr = Pick(lib.netherProps, rnd);
                        if (pr) Fit(WorldBuilder.Spawn(pr, new Vector3(s * 7.6f, 0, z), rnd.Next(360), root), 2.5f + rnd.Next(3));
                    }
                }
                break;
            }
        }
    }

    static GameObject Pick(GameObject[] a, System.Random r) => a == null || a.Length == 0 ? null : a[r.Next(a.Length)];

    public static void Fit(GameObject go, float h, bool keepY = false)
    {
        var b = WorldBuilder.Bounds(go);
        if (b.size.y > 0.01f) go.transform.localScale *= h / b.size.y;
        b = WorldBuilder.Bounds(go);
        float baseY = keepY ? go.transform.position.y : 0f;
        go.transform.position += Vector3.up * (baseY - b.min.y);
    }
}
