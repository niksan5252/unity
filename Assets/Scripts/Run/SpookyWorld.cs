using System.Collections.Generic;
using UnityEngine;

// Halloween Quest (Move Quest Run #3): spooky night fair, witch forest, moon castle.
// Bright night palette (purple / orange / lime / teal) under a painted sky dome (Spooky/Sky).
// Everything that moves lives in WorldBuilder.Dyn: ferris wheels, carousels, dancing monsters,
// orbiting dragons, swaying banners. Beat pulse, fireworks and fly-bys are in SpookyFx.
public static class SpookyWorld
{
    static RunLibrary lib;
    static System.Random rnd;

    public static readonly Color Orange = new Color(1f, 0.5f, 0.08f), Purple = new Color(0.62f, 0.2f, 1f), Lime = new Color(0.55f, 1f, 0.15f),
        Teal = new Color(0.1f, 0.95f, 0.85f), Pink = new Color(1f, 0.3f, 0.75f), Gold = new Color(1f, 0.82f, 0.25f);
    public static readonly Color[] Party = { Orange, Purple, Lime, Pink, Gold, Teal };

    // ---------- palette per world ----------
    public struct Palette
    {
        public Color top, mid, horizon, ambient, sun, moon, cloud;
        public float sunIntensity, moonSize, clouds, aurora, fogStart, fogEnd;
        public Vector3 moonDir, sunEuler;
    }

    public static Palette Pal(RunTheme t)
    {
        switch (t)
        {
            case RunTheme.WitchForest:
                return new Palette
                {
                    top = new Color(0.01f, 0.03f, 0.1f), mid = new Color(0.04f, 0.2f, 0.32f), horizon = new Color(0.16f, 0.5f, 0.52f),
                    ambient = new Color(0.42f, 0.56f, 0.66f), sun = new Color(0.6f, 0.9f, 1f), sunIntensity = 0.85f, sunEuler = new Vector3(38f, 25f, 0),
                    moon = new Color(0.85f, 1f, 0.9f), moonDir = new Vector3(0.42f, 0.36f, 1f), moonSize = 0.085f,
                    cloud = new Color(0.03f, 0.1f, 0.15f), clouds = 0.25f, aurora = 1.4f, fogStart = 25f, fogEnd = 165f,
                };
            case RunTheme.MoonCastle:
                return new Palette
                {
                    top = new Color(0.06f, 0f, 0.14f), mid = new Color(0.42f, 0.05f, 0.28f), horizon = new Color(1f, 0.36f, 0.14f),
                    ambient = new Color(0.56f, 0.42f, 0.62f), sun = new Color(1f, 0.62f, 0.45f), sunIntensity = 0.9f, sunEuler = new Vector3(35f, 20f, 0),
                    moon = new Color(1f, 0.62f, 0.26f), moonDir = new Vector3(0.1f, 0.2f, 1f), moonSize = 0.17f,
                    cloud = new Color(0.16f, 0.02f, 0.12f), clouds = 0.6f, aurora = 0f, fogStart = 40f, fogEnd = 230f,
                };
            default:   // spooky carnival
                return new Palette
                {
                    top = new Color(0.04f, 0.02f, 0.16f), mid = new Color(0.32f, 0.07f, 0.46f), horizon = new Color(0.95f, 0.4f, 0.34f),
                    ambient = new Color(0.56f, 0.46f, 0.76f), sun = new Color(0.78f, 0.62f, 1f), sunIntensity = 0.95f, sunEuler = new Vector3(40f, 20f, 0),
                    moon = new Color(1f, 0.93f, 0.72f), moonDir = new Vector3(-0.38f, 0.33f, 1f), moonSize = 0.11f,
                    cloud = new Color(0.2f, 0.06f, 0.28f), clouds = 0.45f, aurora = 0f, fogStart = 35f, fogEnd = 195f,
                };
        }
    }

    // ---------- materials (cached so the static batch merges them) ----------
    public static readonly List<(Material m, Color c)> BeatMats = new List<(Material, Color)>();   // emission pulses on the beat
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    public static Material Glow(Color c, float intensity, bool beat = false)
    {
        string key = "g" + c + intensity + beat;
        if (cache.TryGetValue(key, out var m) && m) return m;
        m = WorldBuilder.Emissive(c, intensity);
        m.SetFloat("_Smoothness", 0.5f);
        if (beat) BeatMats.Add((m, c * intensity));
        return cache[key] = m;
    }

    public static Material Lit(Color c, float smooth = 0.25f, float metal = 0f)
    {
        string key = "l" + c + smooth + metal;
        if (cache.TryGetValue(key, out var m) && m) return m;
        m = WorldBuilder.Mat(c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        return cache[key] = m;
    }

    public static Texture2D T(RunLibrary l, string n)
    {
        if (l && l.spookyTex != null) foreach (var t in l.spookyTex) if (t && t.name == n) return t;
        if (l && l.toyTex != null) foreach (var t in l.toyTex) if (t && t.name == n) return t;
        return null;
    }

    public static Material Tex(string n, float u, float v, Color tint, float glow = 0, float smooth = 0.2f)
    {
        string key = "t" + n + u + "_" + v + tint + glow + smooth;
        if (cache.TryGetValue(key, out var m) && m) return m;
        m = HalloweenWorld.TexMat(T(lib, n), u, v, tint, glow, smooth);
        return cache[key] = m;
    }

    // ---------- geometry helpers ----------
    public static GameObject Prim(PrimitiveType type, Transform p, Vector3 pos, Vector3 scale, Material m, Quaternion? rot = null, bool shadows = true)
    {
        var g = GameObject.CreatePrimitive(type);
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(p);
        g.transform.SetPositionAndRotation(pos, rot ?? Quaternion.identity);
        g.transform.localScale = scale;
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = m;
        if (!shadows) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g;
    }

    public static GameObject Box(Transform p, Vector3 pos, Vector3 size, Material m, bool shadows = true) => Prim(PrimitiveType.Cube, p, pos, size, m, null, shadows);
    // textured sign: a Quad reads correctly when its back (+Z) points away from the viewer
    public static GameObject Quad(Transform p, Vector3 pos, Quaternion rot, Vector2 size, Material m) => Prim(PrimitiveType.Quad, p, pos, new Vector3(size.x, size.y, 1), m, rot, false);
    public static GameObject Ball(Transform p, Vector3 pos, Vector3 size, Material m, bool shadows = true) => Prim(PrimitiveType.Sphere, p, pos, size, m, null, shadows);
    // cylinder primitive: height h along Y, centred at pos
    public static GameObject Cyl(Transform p, Vector3 pos, float diameter, float h, Material m, Quaternion? rot = null, bool shadows = true) =>
        Prim(PrimitiveType.Cylinder, p, pos, new Vector3(diameter, h * 0.5f, diameter), m, rot, shadows);

    static readonly Dictionary<int, Mesh> cones = new Dictionary<int, Mesh>();
    // cone/pyramid with `n` sides, base radius 0.5 at y=0, apex at y=1, UVs wrapped around (u) and up (v)
    public static Mesh Cone(int n)
    {
        if (cones.TryGetValue(n, out var mesh) && mesh) return mesh;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i < n; i++)
        {
            float a0 = i * Mathf.PI * 2 / n, a1 = (i + 1) * Mathf.PI * 2 / n;
            int b = v.Count;
            v.Add(new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f)); uv.Add(new Vector2(i / (float)n, 0));
            v.Add(new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f)); uv.Add(new Vector2((i + 1) / (float)n, 0));
            v.Add(new Vector3(0, 1, 0)); uv.Add(new Vector2((i + 0.5f) / n, 1));
            t.Add(b); t.Add(b + 2); t.Add(b + 1);
        }
        // bottom cap
        int c = v.Count;
        v.Add(Vector3.zero); uv.Add(new Vector2(0.5f, 0.5f));
        for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2 / n; v.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f)); uv.Add(new Vector2(0.5f, 0.5f)); }
        for (int i = 0; i < n; i++) { t.Add(c); t.Add(c + 1 + i); t.Add(c + 2 + i); }
        mesh = new Mesh { name = "Cone" + n };
        mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return cones[n] = mesh;
    }

    public static GameObject ConeObj(Transform p, Vector3 basePos, float diameter, float h, Material m, int sides = 20, float rotY = 0, bool shadows = true)
    {
        var g = new GameObject("Cone", typeof(MeshFilter), typeof(MeshRenderer));
        g.transform.SetParent(p);
        g.transform.SetPositionAndRotation(basePos, Quaternion.Euler(0, rotY, 0));
        g.transform.localScale = new Vector3(diameter, h, diameter);
        g.GetComponent<MeshFilter>().sharedMesh = Cone(sides);
        var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m;
        if (!shadows) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g;
    }

    // other worlds (ToyWorld) reuse these helpers: point them at the current library first
    public static void Use(RunLibrary l, System.Random r) { lib = l; rnd = r; }

    static GameObject P(string n, Vector3 pos, float rot, float h, Transform root) => CandyWorld.Put(lib, n, pos, rot, h, root);
    static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
    static T Pick<T>(T[] a) => a[rnd.Next(a.Length)];

    // an animated Quaternius creature looping one of `clips` (Dance, Jump, Fast_Flying ...), sized by its posed mesh
    public static GameObject Critter(RunLibrary l, string name, Vector3 pos, float rotY, float height, Transform parent, params string[] clips)
    {
        var p = l.Kit(name);
        if (!p) return null;
        var go = WorldBuilder.Spawn(p, pos, rotY, parent);
        var a = go.GetComponentInChildren<Animation>();
        if (a)
        {
            if (!LoopExact(a, clips)) DinoWorld.Loop(a, clips);
            a.Sample();
            a.cullingType = AnimationCullingType.BasedOnRenderers;
        }
        RunObstacle.FitHeightPosed(go, height);
        go.transform.position += Vector3.up * pos.y;
        return go;
    }

    // "sit" must not pick "wheelchair-sit": prefer clips whose own name (after "Armature|") is exactly the key
    static bool LoopExact(Animation a, string[] keys)
    {
        foreach (var k in keys)
            foreach (AnimationState s in a)
                if (s.name.Substring(s.name.LastIndexOf('|') + 1).ToLower() == k)
                {
                    s.wrapMode = WrapMode.Loop;
                    s.time = Random.value * s.length;
                    a.Play(s.name);
                    return true;
                }
        return false;
    }

    // monsters get a soft glow in the world colour so they pop out of the night
    public static void Spookify(GameObject go, Color glow, float k = 0.35f)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var ms = r.materials;
            foreach (var m in ms)
            {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap")) m.SetTexture("_EmissionMap", m.GetTexture("_BaseMap"));
                m.SetColor("_EmissionColor", Color.Lerp(Color.white, glow, 0.5f) * k);
            }
            r.materials = ms;
        }
    }

    // purple witch hat with an orange band on the buddy's head
    public static void WitchHat(GameObject who)
    {
        var b = RunObstacle.PosedBoundsOf(who);
        Transform head = null;
        foreach (var t in who.GetComponentsInChildren<Transform>()) if (t.name == "Head") { head = t; break; }
        var hat = new GameObject("WitchHat").transform;
        hat.position = new Vector3(b.center.x, b.max.y - 0.12f, b.center.z);
        var felt = Lit(new Color(0.35f, 0.1f, 0.6f), 0.3f);
        Cyl(hat, hat.position + Vector3.up * 0.015f, 0.82f, 0.03f, felt, null, false);
        Cyl(hat, hat.position + Vector3.up * 0.07f, 0.5f, 0.1f, Glow(Orange, 1.2f), null, false);
        var tip = ConeObj(hat, hat.position + Vector3.up * 0.03f, 0.48f, 0.62f, felt, 16, 0, false);
        tip.transform.rotation = Quaternion.Euler(0, 0, -14f);
        Ball(hat, hat.position + new Vector3(0.16f, 0.62f, 0), Vector3.one * 0.09f, Glow(Lime, 2f), false);
        hat.SetParent(head ? head : who.transform, true);
    }

    // =====================================================================
    public static void Build(RunLibrary l, RunTheme theme, Transform root, float length, System.Random r)
    {
        lib = l; rnd = r;
        float L = length + 160f, half = length * 0.5f + 60f;
        var p = Pal(theme);
        var cam = Camera.main;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = p.horizon;
        RenderSettings.fogStartDistance = p.fogStart; RenderSettings.fogEndDistance = p.fogEnd;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = p.ambient;
        if (cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = p.horizon; }
        foreach (var dl in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (dl.type == LightType.Directional)
            {
                dl.color = p.sun; dl.intensity = p.sunIntensity;
                dl.transform.rotation = Quaternion.Euler(p.sunEuler);
                dl.shadowStrength = 0.6f;
            }

        switch (theme)
        {
            case RunTheme.WitchForest: Forest(root, length, L, half); break;
            case RunTheme.MoonCastle: Castle(root, length, L, half); break;
            default: Carnival(root, length, L, half); break;
        }
        var fx = root.gameObject.AddComponent<SpookyFx>();
        fx.Setup(lib, theme, length);
    }

    // =====================================================================
    // SPOOKY FAIR: boardwalk through a haunted fun fair - booths, big tops, carousels, ferris wheels,
    // light arches over the road, monsters dancing in the crowd
    static void Carnival(Transform root, float length, float L, float half)
    {
        var dyn = WorldBuilder.Dyn;
        Box(root, new Vector3(0, -0.1f, half), new Vector3(9f, 0.2f, L), Tex("boardwalk", 2, L / 4, Color.white, 0, 0.35f));
        Box(root, new Vector3(0, -0.15f, half), new Vector3(420f, 0.2f, L + 300), Tex("nightgrass", 100, (L + 300) / 4, new Color(0.8f, 0.6f, 1f), 0, 0.1f));
        foreach (int s in new[] { -1, 1 })
        {
            Box(root, new Vector3(s * 4.62f, 0.06f, half), new Vector3(0.16f, 0.12f, L), Glow(s < 0 ? Orange : Purple, 4f, true), false);
            Box(root, new Vector3(s * 6.5f, -0.08f, half), new Vector3(3.6f, 0.2f, L), Lit(new Color(0.16f, 0.1f, 0.22f), 0.5f));   // midway walkway

            // booths with prizes, each with its own glowing sign and marquee bulbs
            for (float z = -10; z < length + 120; z += 8.5f + rnd.Next(4))
                if (rnd.NextDouble() < 0.8) Booth(root, new Vector3(s * 8.4f, 0, z), s);
            // pumpkins and hay along the walkway
            for (float z = -5; z < length + 100; z += 5f + rnd.Next(6))
            {
                var pk = P(rnd.NextDouble() < 0.6 ? "pumpkin-carved" : "pumpkin-tall-carved", new Vector3(s * R(5.0f, 5.6f), 0, z), rnd.Next(360), R(0.55f, 0.85f), root);
                if (pk) Box(root, pk.transform.position + Vector3.up * 0.28f, Vector3.one * 0.24f, Glow(Orange, 3f, true), false);
                if (rnd.NextDouble() < 0.25) P("hay-bale", new Vector3(s * R(5.0f, 5.3f), 0, z + 2.6f), s < 0 ? 0 : 180, 0.8f, root);
            }
            // dancing monster crowd
            string[] crowd = { "GreenBlob", "PinkBlob", "Mushnub", "Alien", "GreenSpikyBlob", "Cat", "Mushnub_Evolved" };
            for (float z = 6; z < length + 100; z += 7f + rnd.Next(8))
                Critter(lib, Pick(crowd), new Vector3(s * R(5.7f, 6.9f), 0, z), -s * R(105f, 140f), R(1.0f, 1.45f), dyn, rnd.NextDouble() < 0.75 ? "dance" : "jump", "idle");
            // big tops (kept clear of the carousels and ferris wheels)
            float wheel0 = 50 + (s > 0 ? 90 : 0);
            for (float z = 10 + rnd.Next(20); z < length + 160; z += 34f + rnd.Next(20))
            {
                float w = Mathf.Repeat(z - wheel0, 180f);
                if (w < 14f || w > 166f) continue;
                BigTop(root, new Vector3(s * R(22f, 29f), 0, z), R(8f, 13f));
            }
            // carousels
            for (float z = 30 + (s > 0 ? 40 : 0); z < length + 120; z += 85f + rnd.Next(20))
                Carousel(dyn, root, new Vector3(s * 14.5f, 0, z));
            // ferris wheels on the horizon
            for (float z = wheel0; z < length + 220; z += 180f)
                Ferris(dyn, root, new Vector3(s * R(40f, 44f), 0, z), R(12f, 15f));
            // far tree line
            for (float z = -20; z < length + 220; z += 5f + rnd.Next(4))
                P(rnd.NextDouble() < 0.5 ? "pine-crooked" : "pine", new Vector3(s * R(46f, 60f), 0, z), rnd.Next(360), R(8f, 14f), root);
        }
        // light arches over the road; every third one is the SPOOKY FAIR gate
        int k = 0;
        for (float z = 12; z < length + 100; z += 24f, k++)
        {
            var pole = Lit(new Color(0.12f, 0.06f, 0.18f), 0.4f, 0.3f);
            foreach (int s in new[] { -1, 1 })
            {
                Box(root, new Vector3(s * 5.3f, 3.2f, z), new Vector3(0.28f, 6.4f, 0.28f), pole);
                Ball(root, new Vector3(s * 5.3f, 6.5f, z), Vector3.one * 0.5f, Glow(Party[(k + (s > 0 ? 1 : 0)) % Party.Length], 4f, true), false);
            }
            if (k % 3 == 1)
            {
                Box(root, new Vector3(0, 7.35f, z), new Vector3(7.6f, 1.9f, 0.3f), Lit(new Color(0.12f, 0.03f, 0.2f)));
                Quad(root, new Vector3(0, 7.35f, z - 0.17f), Quaternion.identity, new Vector2(7.2f, 1.8f), Tex("fairsign", 1, 1, Color.white, 1.3f, 0.3f));
                foreach (int s in new[] { -1, 1 }) Box(root, new Vector3(s * 4.4f, 6.9f, z), new Vector3(1.8f, 0.2f, 0.2f), pole);
            }
            else
            {
                for (int i = 0; i <= 14; i++)
                {
                    float a = i / 14f * Mathf.PI;
                    Ball(root, new Vector3(Mathf.Cos(a) * 5.3f, 6.5f + Mathf.Sin(a) * 2.2f, z), Vector3.one * 0.3f, Glow(Party[(i + k) % Party.Length], 5f, true), false);
                }
            }
            // bunting halfway to the next arch
            float bz = z + 12f;
            var str = Lit(new Color(0.1f, 0.1f, 0.1f));
            Box(root, new Vector3(0, 5.6f, bz), new Vector3(10.6f, 0.03f, 0.03f), str, false);
            for (int i = 0; i < 15; i++)
            {
                float x = -5f + i * (10f / 14f);
                float sag = Mathf.Sin(i / 14f * Mathf.PI) * 0.7f;
                var f = Box(root, new Vector3(x, 5.35f - sag, bz), new Vector3(0.42f, 0.5f, 0.02f), Lit(Party[(i + k) % Party.Length], 0.2f), false);
                f.transform.rotation = Quaternion.Euler(0, 0, 45);
                f.transform.localScale = new Vector3(0.36f, 0.36f, 0.02f);
            }
        }
    }

    static readonly string[] Prizes = { "lollypop", "candy-bar", "cupcake", "donut-sprinkles", "ice-cream", "cake-birthday", "popsicle", "muffin", "cookie-chocolate" };

    static void Booth(Transform root, Vector3 p, int s)
    {
        var stripe = Tex(rnd.Next(3) == 0 ? "tent2" : "tent", 2, 1, Color.white, 0, 0.3f);
        var dark = Lit(new Color(0.16f, 0.07f, 0.24f));
        Box(root, p + new Vector3(0, 0.55f, 0), new Vector3(2.6f, 1.1f, 2.4f), stripe);
        Box(root, p + new Vector3(0, 1.14f, 0), new Vector3(2.75f, 0.08f, 2.55f), Lit(new Color(0.95f, 0.85f, 0.6f), 0.4f));
        Box(root, p + new Vector3(s * 1.15f, 1.9f, 0), new Vector3(0.2f, 1.6f, 2.4f), dark);
        foreach (float dz in new[] { -1.15f, 1.15f })
            Box(root, p + new Vector3(-s * 1.2f, 1.95f, dz), new Vector3(0.12f, 1.7f, 0.12f), dark);
        ConeObj(root, p + new Vector3(0, 2.75f, 0), 4.1f, 1.5f, stripe, 4, 45);
        var c = Party[rnd.Next(Party.Length)];
        Box(root, p + new Vector3(-s * 1.32f, 2.95f, 0), new Vector3(0.08f, 0.5f, 2.2f), Glow(c, 2.4f, true), false);
        for (int i = 0; i < 6; i++)
            Ball(root, p + new Vector3(-s * 1.42f, 2.62f, -1.05f + i * 0.42f), Vector3.one * 0.14f, Glow(Gold, 6f, true), false);
        for (int i = 0; i < 3; i++)
            P(Pick(Prizes), p + new Vector3(-s * 0.55f, 1.18f, -0.75f + i * 0.75f), rnd.Next(360), R(0.35f, 0.55f), root);
        // balloon bunch tied to the corner
        var tie = p + new Vector3(-s * 1.2f, 3.1f, 1.15f);
        for (int i = 0; i < 4; i++)
        {
            var bp = tie + new Vector3(R(-0.5f, 0.5f), R(0.6f, 1.4f), R(-0.4f, 0.4f));
            Ball(root, bp, new Vector3(0.5f, 0.62f, 0.5f), Lit(Party[rnd.Next(Party.Length)], 0.85f), false);
            var dir = tie - bp;
            var str = Box(root, (tie + bp) * 0.5f, new Vector3(0.015f, dir.magnitude, 0.015f), Lit(Color.white), false);
            str.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        }
    }

    static void BigTop(Transform root, Vector3 p, float d)
    {
        var stripe = Tex(rnd.Next(2) == 0 ? "tent" : "tent2", 8, 1, Color.white, 0, 0.3f);
        float h = d * 0.42f;
        Cyl(root, p + Vector3.up * h * 0.5f, d, h, stripe);
        ConeObj(root, p + Vector3.up * h, d * 1.12f, d * 0.7f, stripe, 24);
        Box(root, p + Vector3.up * (h + d * 0.7f + 0.8f), new Vector3(0.12f, 1.8f, 0.12f), Lit(new Color(0.2f, 0.2f, 0.25f)));
        Box(root, p + new Vector3(0.5f, h + d * 0.7f + 1.4f, 0), new Vector3(0.9f, 0.5f, 0.05f), Glow(Party[rnd.Next(Party.Length)], 2.5f, true), false);
        // ring of bulbs on the eave
        for (int i = 0; i < 18; i++)
        {
            float a = i * Mathf.PI * 2 / 18;
            Ball(root, p + new Vector3(Mathf.Cos(a) * d * 0.56f, h + 0.1f, Mathf.Sin(a) * d * 0.56f), Vector3.one * 0.3f, Glow(i % 2 == 0 ? Gold : Orange, 5f, true), false);
        }
        // glowing entrance facing the road
        Box(root, p + new Vector3(-Mathf.Sign(p.x) * d * 0.5f, h * 0.38f, 0), new Vector3(0.1f, h * 0.75f, d * 0.22f), Glow(Gold, 1.8f), false);
    }

    static void Carousel(Transform dyn, Transform root, Vector3 p)
    {
        var stripe = Tex("tent", 6, 1, Color.white, 0, 0.3f);
        Cyl(root, p + Vector3.up * 0.2f, 7.2f, 0.4f, Lit(new Color(0.3f, 0.1f, 0.45f), 0.5f));
        var top = new GameObject("Carousel").transform;
        top.SetParent(dyn); top.position = p;
        Cyl(top, p + Vector3.up * 2.4f, 0.5f, 4.4f, Lit(Gold, 0.7f, 0.6f));
        ConeObj(top, p + Vector3.up * 4.5f, 8f, 2.4f, stripe, 16, 0);
        Cyl(top, p + Vector3.up * 4.5f, 8f, 0.35f, Glow(Purple, 1.5f, true));
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2 / 16;
            Ball(top, p + new Vector3(Mathf.Cos(a) * 4.05f, 4.45f, Mathf.Sin(a) * 4.05f), Vector3.one * 0.26f, Glow(i % 2 == 0 ? Gold : Pink, 6f, true), false);
        }
        // eight bobbing "horses" (pumpkin-headed ponies) on gold poles
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2 / 8;
            var hp = p + new Vector3(Mathf.Cos(a) * 2.8f, 0, Mathf.Sin(a) * 2.8f);
            Cyl(top, hp + Vector3.up * 2.4f, 0.08f, 4f, Lit(Gold, 0.8f, 0.7f), null, false);
            var horse = new GameObject("Pony").transform;
            horse.SetParent(top); horse.position = hp + Vector3.up * 1.3f;
            horse.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)));
            var col = Party[i % Party.Length];
            var body = Box(horse, horse.position, new Vector3(0.45f, 0.5f, 1.1f), Lit(col, 0.6f), false);
            body.transform.rotation = horse.rotation;
            var head = P("pumpkin-carved", horse.position + horse.forward * 0.65f + Vector3.up * 0.35f, 0, 0.5f, horse);
            if (head) head.transform.rotation = horse.rotation * head.transform.rotation;
            foreach (var (fx, fz) in new[] { (-0.15f, -0.4f), (0.15f, -0.4f), (-0.15f, 0.4f), (0.15f, 0.4f) })
            {
                var leg = Box(horse, horse.position + horse.right * fx + horse.forward * fz + Vector3.down * 0.45f, new Vector3(0.1f, 0.5f, 0.1f), Lit(col * 0.7f), false);
                leg.transform.rotation = horse.rotation;
            }
            var bob = horse.gameObject.AddComponent<Bob>(); bob.height = 0.35f; bob.speed = 2.6f; bob.phase = i * 0.8f;
        }
        var spin = top.gameObject.AddComponent<Spin>(); spin.axis = Vector3.up; spin.speed = 28f;
    }

    static void Ferris(Transform dyn, Transform root, Vector3 p, float radius)
    {
        var steel = Lit(new Color(0.85f, 0.85f, 0.95f), 0.6f, 0.5f);
        var hubPos = p + Vector3.up * (radius + 2.5f);
        // A-frame legs in front of and behind the wheel
        foreach (int fz in new[] { -1, 1 })
            foreach (int sx in new[] { -1, 1 })
            {
                var foot = p + new Vector3(sx * radius * 0.45f, 0, fz * 1.6f);
                var mid = (foot + hubPos + new Vector3(0, 0, fz * 1.6f)) * 0.5f;
                var dir = hubPos + new Vector3(0, 0, fz * 1.6f) - foot;
                var leg = Box(root, mid, new Vector3(0.45f, dir.magnitude, 0.45f), steel);
                leg.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            }
        var wheel = new GameObject("FerrisWheel").transform;
        wheel.SetParent(dyn); wheel.position = hubPos;
        Cyl(wheel, hubPos, 1.4f, 3.4f, Lit(Gold, 0.6f, 0.6f), Quaternion.Euler(90, 0, 0));
        int n = 20;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2 / n, a1 = (i + 1) * Mathf.PI * 2 / n;
            foreach (float dz in new[] { -1.1f, 1.1f })
            {
                var p0 = hubPos + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, dz);
                var p1 = hubPos + new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, dz);
                var seg = Box(wheel, (p0 + p1) * 0.5f, new Vector3(0.3f, (p1 - p0).magnitude + 0.05f, 0.3f), Glow(Purple, 1.3f, true), false);
                seg.transform.rotation = Quaternion.FromToRotation(Vector3.up, p1 - p0);
                if (dz < 0) Ball(wheel, p0 + Vector3.back * 0.2f, Vector3.one * 0.42f, Glow(i % 2 == 0 ? Gold : Orange, 6f, true), false);
            }
            if (i % 2 == 0)
            {
                var spoke = Box(wheel, hubPos + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * radius * 0.5f, new Vector3(0.14f, radius, 0.14f), steel, false);
                spoke.transform.rotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 90f);
            }
        }
        var cabins = new List<Transform>();
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * 2 / 10;
            var cp = hubPos + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0);
            var cab = new GameObject("Cabin").transform;
            cab.SetParent(wheel); cab.position = cp;
            var col = Party[i % Party.Length];
            Box(cab, cp + Vector3.down * 1.2f, new Vector3(1.7f, 1.3f, 1.5f), Lit(col, 0.5f), false);
            Box(cab, cp + Vector3.down * 1.1f + Vector3.back * 0.76f, new Vector3(1.2f, 0.5f, 0.02f), Glow(Gold, 2f), false);
            ConeObj(cab, cp + Vector3.down * 0.5f, 2.1f, 0.6f, Lit(col * 0.75f), 4, 45, false);
            cabins.Add(cab);
        }
        var spin = wheel.gameObject.AddComponent<Spin>(); spin.axis = Vector3.forward; spin.speed = 9f; spin.upright = cabins.ToArray();
    }

    // =====================================================================
    // WITCH FOREST: moss path through a bioluminescent forest - giant glowing mushrooms, bubbling
    // cauldrons, rune circles, dancing wizards and hopping frogs, aurora in the sky
    public static readonly List<(Vector3 pos, Color c)> Cauldrons = new List<(Vector3, Color)>();

    static void Forest(Transform root, float length, float L, float half)
    {
        var dyn = WorldBuilder.Dyn;
        Cauldrons.Clear();
        Box(root, new Vector3(0, -0.1f, half), new Vector3(9f, 0.2f, L), Tex("mosspath", 3, L / 3, Color.white, 0, 0.3f));
        Box(root, new Vector3(0, -0.15f, half), new Vector3(420f, 0.2f, L + 300), Tex("nightgrass", 100, (L + 300) / 4, new Color(0.7f, 1f, 1f), 0, 0.15f));
        Color[] shroom = { Teal, Pink, Lime, Purple, new Color(0.25f, 0.6f, 1f) };
        string[] trees = { "tree_oak_dark", "tree_default_dark", "tree_detailed_dark", "tree_thin_dark", "tree_simple_dark", "tree_tall_dark", "tree_plateau_dark", "tree_cone_dark" };
        foreach (int s in new[] { -1, 1 })
        {
            // little glowing mushrooms lining the path
            for (float z = -10; z < length + 100; z += 1.1f + (float)rnd.NextDouble() * 1.4f)
                Mushroom(root, new Vector3(s * R(4.75f, 5.7f), 0, z), R(0.35f, 0.8f), Pick(shroom), false);
            // ferns and bushes
            for (float z = -10; z < length + 120; z += 2f + (float)rnd.NextDouble() * 2f)
                P(rnd.NextDouble() < 0.5 ? "plant_bushLarge" : rnd.NextDouble() < 0.5 ? "grass_leafsLarge" : "plant_flatTall", new Vector3(s * R(6.8f, 10f), 0, z), rnd.Next(360), R(0.9f, 1.8f), root);
            // giant toadstools
            for (float z = 0; z < length + 140; z += 6f + rnd.Next(7))
                Mushroom(root, new Vector3(s * R(9f, 24f), 0, z), R(4.5f, 12f), Pick(shroom), true);
            // trees
            for (float z = -10; z < length + 160; z += 3.5f + rnd.Next(4))
            {
                if (rnd.NextDouble() < 0.3) P(rnd.NextDouble() < 0.5 ? "pine-crooked" : "pine-fall-crooked", new Vector3(s * R(8f, 30f), 0, z), rnd.Next(360), R(6f, 11f), root);
                else P(Pick(trees), new Vector3(s * R(10f, 34f), 0, z), rnd.Next(360), R(8f, 15f), root);
            }
            // bubbling cauldrons by the path
            for (float z = 20 + (s > 0 ? 18 : 0); z < length + 80; z += 36f + rnd.Next(10))
                Cauldron(root, new Vector3(s * 7.9f, 0, z), Pick(new[] { Lime, Purple, Teal, Pink }));
            // glowing potion bottles
            for (float z = 8 + rnd.Next(10); z < length + 80; z += 22f + rnd.Next(14))
                Potion(root, new Vector3(s * R(9.2f, 10.5f), 0, z), R(1.4f, 2.6f), Pick(shroom));
            // rune stone circles
            for (float z = 60 + (s > 0 ? 50 : 0); z < length + 100; z += 110f)
                RuneCircle(root, new Vector3(s * 17f, 0, z));
            // dancing wizards and blobs; giant frogs hopping in place
            string[] party = { "Wizard", "Mushnub", "Mushnub_Evolved", "GreenSpikyBlob", "Wizard", "GreenBlob" };
            for (float z = 10; z < length + 100; z += 9f + rnd.Next(9))
                Critter(lib, Pick(party), new Vector3(s * R(5.9f, 6.6f), 0, z), -s * R(105f, 140f), R(1.0f, 1.5f), dyn, "dance", "jump");
            for (float z = 25 + rnd.Next(20); z < length + 100; z += 30f + rnd.Next(20))
                Critter(lib, "Frog", new Vector3(s * R(8f, 12f), 0, z), -s * R(100f, 150f), R(1.6f, 2.4f), dyn, "jump", "idle");
            // dark hills far away
            for (float z = -20; z < length + 300; z += 45f + rnd.Next(30))
            {
                float sz = 70 + rnd.Next(60);
                ConeObj(root, new Vector3(s * (80f + rnd.Next(40)), -2, z), sz, sz * 0.55f, Lit(new Color(0.05f, 0.16f, 0.2f), 0.05f), 24, rnd.Next(360));
            }
        }
    }

    // stem + glowing cap with white spots; `big` ones pulse on the beat
    static void Mushroom(Transform root, Vector3 p, float h, Color c, bool big)
    {
        var stem = Lit(new Color(0.92f, 0.9f, 0.8f), 0.3f);
        float w = h * (big ? 0.14f : 0.22f);
        Cyl(root, p + Vector3.up * h * 0.5f, w, h, stem, null, big);
        float rx = h * (big ? 0.42f : 0.5f), ry = rx * 0.55f;
        Ball(root, p + Vector3.up * h, new Vector3(rx * 2, ry * 2, rx * 2), Glow(c, big ? 1.4f : 2.2f, big), big);
        Ball(root, p + Vector3.up * (h - ry * 0.2f), new Vector3(rx * 1.8f, ry * 0.7f, rx * 1.8f), Glow(c * 0.5f, 1.2f), false);   // gills
        int spots = big ? 7 : 3;
        var white = Glow(new Color(1f, 1f, 0.92f), 1.6f);
        for (int i = 0; i < spots; i++)
        {
            float a = R(0, Mathf.PI * 2), e = R(0.35f, 1.15f);
            var sp = p + Vector3.up * h + new Vector3(Mathf.Cos(a) * Mathf.Cos(e) * rx, Mathf.Sin(e) * ry, Mathf.Sin(a) * Mathf.Cos(e) * rx);
            Ball(root, sp, Vector3.one * rx * 0.24f, white, false);
        }
    }

    static void Cauldron(Transform root, Vector3 p, Color c)
    {
        var iron = Lit(new Color(0.12f, 0.1f, 0.14f), 0.7f, 0.6f);
        Ball(root, p + Vector3.up * 0.85f, new Vector3(1.9f, 1.4f, 1.9f), iron);
        Cyl(root, p + Vector3.up * 1.42f, 1.75f, 0.12f, iron);
        Cyl(root, p + Vector3.up * 1.47f, 1.5f, 0.04f, Glow(c, 4f, true), null, false);
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 2 / 3;
            Box(root, p + new Vector3(Mathf.Cos(a) * 0.65f, 0.2f, Mathf.Sin(a) * 0.65f), new Vector3(0.12f, 0.45f, 0.12f), iron);
        }
        P("campfire_logs", p, rnd.Next(360), 0.35f, root);
        Ball(root, p + Vector3.up * 0.15f, new Vector3(0.9f, 0.3f, 0.9f), Glow(Orange, 4f, true), false);
        Cauldrons.Add((p + Vector3.up * 1.5f, c));
    }

    static void Potion(Transform root, Vector3 p, float h, Color c)
    {
        Ball(root, p + Vector3.up * h * 0.38f, Vector3.one * h * 0.76f, Glow(c, 1.6f, true));
        Cyl(root, p + Vector3.up * h * 0.82f, h * 0.22f, h * 0.3f, Glow(c * 0.8f, 1.2f));
        Cyl(root, p + Vector3.up * h * 1.0f, h * 0.26f, h * 0.1f, Lit(new Color(0.55f, 0.35f, 0.2f)));
        Ball(root, p + new Vector3(-h * 0.12f, h * 0.5f, -h * 0.2f), Vector3.one * h * 0.12f, Glow(Color.white, 2f), false);   // highlight
    }

    static void RuneCircle(Transform root, Vector3 c)
    {
        var rune = Tex("runes", 1, 1, Color.white, 2.2f, 0.2f);
        var stone = Lit(new Color(0.28f, 0.3f, 0.34f), 0.15f);
        for (int i = 0; i < 7; i++)
        {
            float a = i * Mathf.PI * 2 / 7;
            var sp = c + new Vector3(Mathf.Cos(a) * 6f, 0, Mathf.Sin(a) * 6f);
            float h = R(3f, 4.5f);
            var st = Box(root, sp + Vector3.up * h * 0.5f, new Vector3(1.4f, h, 0.7f), stone);
            st.transform.rotation = Quaternion.LookRotation(c - sp);
            var face = Box(root, sp + Vector3.up * h * 0.55f + (c - sp).normalized * 0.37f, new Vector3(1.1f, 1.1f, 0.02f), rune, false);
            face.transform.rotation = Quaternion.LookRotation(sp - c);
        }
        Cyl(root, c + Vector3.up * 0.02f, 9f, 0.04f, Glow(Lime * 0.6f, 1.5f, true), null, false);
    }

    // =====================================================================
    // MOON CASTLE: run along the castle rampart toward a giant harvest moon - towers with lit windows,
    // gate arches, banners, skeletons and monsters partying, dragons circling, lightning
    static void Castle(Transform root, float length, float L, float half)
    {
        var dyn = WorldBuilder.Dyn;
        var wallTex = Tex("castlewall", 1, 1, Color.white, 0, 0.15f);
        Box(root, new Vector3(0, -0.6f, half), new Vector3(9.4f, 1.2f, L), Tex("cobble", 3, L / 3, Color.white, 0, 0.35f));
        Box(root, new Vector3(0, -40f, half), new Vector3(600f, 0.2f, L + 400), Lit(new Color(0.16f, 0.06f, 0.22f), 0.1f));   // valley floor far below
        var flame = Glow(Orange, 6f, true);
        foreach (int s in new[] { -1, 1 })
        {
            // parapet with merlons and torches
            Box(root, new Vector3(s * 4.95f, 0.42f, half), new Vector3(0.55f, 0.85f, L), Tex("castlewall", 0.6f, L / 2, Color.white, 0, 0.15f));
            Box(root, new Vector3(s * 4.95f, -6f, half), new Vector3(0.6f, 12f, L), Tex("castlewall", 0.6f, L / 2, new Color(0.7f, 0.65f, 0.8f), 0, 0.15f));   // bridge wall below
            for (float z = -20; z < length + 100; z += 1.6f)
                Box(root, new Vector3(s * 4.95f, 1.15f, z), new Vector3(0.55f, 0.6f, 0.8f), wallTex);
            for (float z = 4; z < length + 100; z += 8f)
            {
                Box(root, new Vector3(s * 4.95f, 1.75f, z + 0.8f), new Vector3(0.12f, 0.6f, 0.12f), Lit(new Color(0.25f, 0.15f, 0.08f)), false);
                Box(root, new Vector3(s * 4.95f, 2.15f, z + 0.8f), new Vector3(0.2f, 0.28f, 0.2f), flame, false);
            }
            // balconies outside the parapet: skeletons and monsters partying
            for (float z = 10 + (s > 0 ? 9 : 0); z < length + 100; z += 18f + rnd.Next(6))
            {
                if (Mathf.Abs(Mathf.Repeat(z - 30f + 32f, 64f) - 32f) < 3.5f) continue;   // clear of the gate pillars
                Box(root, new Vector3(s * 7.2f, -0.2f, z), new Vector3(2.6f, 0.4f, 3.4f), wallTex);
                bool skel = rnd.NextDouble() < 0.55;
                for (int k = 0; k < 2; k++)
                    Critter(lib, skel ? "Skeleton" : Pick(new[] { "GreenBlob", "PinkBlob", "Mushnub", "Alien" }), new Vector3(s * 7.2f, 0, z - 0.8f + k * 1.6f), -s * R(100f, 140f), skel ? 1.6f : 1.2f, dyn, skel ? "jump" : "dance", "idle");
            }
            // banners swaying on tall poles
            for (float z = 14; z < length + 100; z += 20f)
            {
                if (Mathf.Abs(Mathf.Repeat(z - 30f + 32f, 64f) - 32f) < 3f) continue;
                Box(root, new Vector3(s * 5.55f, 3.6f, z), new Vector3(0.14f, 7.2f, 0.14f), Lit(new Color(0.15f, 0.12f, 0.12f), 0.4f, 0.6f));
                Box(root, new Vector3(s * 5.25f, 7.1f, z), new Vector3(0.75f, 0.1f, 0.1f), Lit(new Color(0.15f, 0.12f, 0.12f), 0.4f, 0.6f));
                var pivot = new GameObject("Banner").transform;
                pivot.SetParent(dyn); pivot.position = new Vector3(s * 5.0f, 7.05f, z);
                Quad(pivot, pivot.position + Vector3.down * 1.5f, Quaternion.Euler(0, s < 0 ? -90 : 90, 0), new Vector2(1.4f, 3f), Tex("banner", 1, 1, Color.white, 0.25f, 0.2f));
                var sw = pivot.gameObject.AddComponent<Sway>(); sw.angle = 8f; sw.speed = R(1.4f, 2f); sw.axis = Vector3.right;
            }
            // towers rising from the valley
            for (float z = 0 + rnd.Next(10); z < length + 200; z += 16f + rnd.Next(14))
                Tower(dyn, root, new Vector3(s * R(15f, 42f), 0, z), R(3f, 6f), R(12f, 40f));
            // great keeps far away
            for (float z = 40 + rnd.Next(40); z < length + 300; z += 90f + rnd.Next(40))
                Tower(dyn, root, new Vector3(s * R(70f, 95f), 0, z), R(9f, 14f), R(45f, 70f));
        }
        // gate arches over the road
        for (float z = 30; z < length + 100; z += 64f)
        {
            foreach (int s in new[] { -1, 1 })
            {
                Box(root, new Vector3(s * 5.6f, 4.5f, z), new Vector3(2f, 9f, 2.2f), wallTex);
                Box(root, new Vector3(s * 4.55f, 6.4f, z - 1.15f), new Vector3(0.25f, 0.35f, 0.25f), flame, false);
                Quad(root, new Vector3(s * 3.3f, 7.0f, z - 1.12f), Quaternion.identity, new Vector2(1.4f, 2.8f), Tex("banner", 1, 1, Color.white, 0.25f, 0.2f));
            }
            Box(root, new Vector3(0, 9.4f, z), new Vector3(13.2f, 1.8f, 2.2f), wallTex);
            for (int i = 0; i < 7; i++)
                Box(root, new Vector3(-6f + i * 2f, 10.7f, z), new Vector3(1f, 0.9f, 2.2f), wallTex);
            Box(root, new Vector3(0, 8.4f, z - 1.12f), new Vector3(9f, 0.12f, 0.05f), Glow(Purple, 4f, true), false);
        }
    }

    static void Tower(Transform dyn, Transform root, Vector3 p, float r, float h)
    {
        float bottom = -40f, tall = h - bottom;
        Cyl(root, new Vector3(p.x, bottom + tall * 0.5f, p.z), r * 2, tall, Tex("castlewall", Mathf.Round(r * 1.2f), Mathf.Round(tall / 4f), new Color(0.9f, 0.85f, 1f), 0, 0.15f));
        Cyl(root, new Vector3(p.x, h + 0.4f, p.z), r * 2.3f, 0.8f, Tex("castlewall", Mathf.Round(r), 0.3f, Color.white, 0, 0.15f));
        var roofCol = rnd.Next(3) == 0 ? new Color(0.15f, 0.45f, 0.3f) : new Color(0.38f, 0.12f, 0.58f);
        ConeObj(root, new Vector3(p.x, h + 0.8f, p.z), r * 2.4f, r * 2.3f, Lit(roofCol, 0.35f), 20);
        // lit windows on the side facing the road
        var win = Glow(rnd.Next(4) == 0 ? Lime : Orange, 3f, rnd.Next(2) == 0);
        float face = p.x > 0 ? Mathf.PI : 0;   // toward x = 0
        for (float y = 2; y < h - 2; y += R(4f, 7f))
        {
            float a = face + R(-0.7f, 0.7f);
            var wp = new Vector3(p.x + Mathf.Cos(a) * r * 0.98f, y, p.z + Mathf.Sin(a) * r * 0.98f);
            var w = Box(root, wp, new Vector3(0.9f, 1.5f, 0.15f), win, false);
            w.transform.rotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
        }
        // pennant on the spire
        float tip = h + 0.8f + r * 2.3f;
        Box(root, new Vector3(p.x, tip + 1f, p.z), new Vector3(0.1f, 2f, 0.1f), Lit(new Color(0.2f, 0.2f, 0.2f)), false);
        var pivot = new GameObject("Pennant").transform;
        pivot.SetParent(dyn); pivot.position = new Vector3(p.x, tip + 1.7f, p.z);
        Box(pivot, pivot.position + new Vector3(0, 0, 0.9f), new Vector3(0.04f, 0.8f, 1.8f), Lit(Party[rnd.Next(Party.Length)], 0.3f), false);
        var sw = pivot.gameObject.AddComponent<Sway>(); sw.angle = 18f; sw.speed = R(2f, 3f); sw.axis = Vector3.up;
    }
}

// ---------- small motion helpers (all live in WorldBuilder.Dyn) ----------
public class Spin : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float speed = 20f;          // deg/s
    public Transform[] upright;        // ferris wheel cabins stay level
    void Update()
    {
        transform.Rotate(axis, speed * Time.deltaTime, Space.World);
        if (upright != null) foreach (var u in upright) if (u) u.rotation = Quaternion.identity;
    }
}

public class Bob : MonoBehaviour
{
    public float height = 0.3f, speed = 2f, phase;
    float baseY = float.NaN;
    void Update()
    {
        if (float.IsNaN(baseY)) baseY = transform.localPosition.y;
        var p = transform.localPosition;
        p.y = baseY + Mathf.Sin(Time.time * speed + phase) * height;
        transform.localPosition = p;
    }
}

public class Sway : MonoBehaviour
{
    public float angle = 10f, speed = 1.5f;
    public Vector3 axis = Vector3.forward;
    Quaternion baseRot; bool init;
    void Update()
    {
        if (!init) { baseRot = transform.rotation; init = true; }
        transform.rotation = Quaternion.AngleAxis(Mathf.Sin(Time.time * speed + transform.position.z) * angle, axis) * baseRot;
    }
}

// circles around a point, facing along the path (dragons, ghosts)
public class Orbit : MonoBehaviour
{
    public Vector3 center;
    public float radius = 20f, speed = 0.4f, phase, wobble = 2f;
    Quaternion baseRot;   // the model's own root rotation (Blender exports carry a -90 X fix)
    void Awake() { baseRot = transform.rotation; }
    void Update()
    {
        float a = Time.time * speed + phase;
        var p = center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a * 2.3f) * wobble, Mathf.Sin(a) * radius);
        var tangent = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)) * Mathf.Sign(speed);
        transform.SetPositionAndRotation(p, Quaternion.LookRotation(tangent) * baseRot);
    }
}

// obstacle frogs: hop up and down while the obstacle root carries them toward the camera
public class Hop : MonoBehaviour
{
    public float height = 0.9f, period = 0.6f, phase;
    float baseY = float.NaN;
    void Update()
    {
        if (float.IsNaN(baseY)) baseY = transform.localPosition.y;
        float k = Mathf.Repeat(Time.time / period + phase, 1f);
        var p = transform.localPosition;
        p.y = baseY + Mathf.Sin(k * Mathf.PI) * height;
        transform.localPosition = p;
    }
}
