using System.Collections.Generic;
using UnityEngine;

// Demon Hunters Run: neon city at night, K-pop concert arena, demon realm.
// Everything pulses on the beat (KpopFx): signs, LED screens, floor strips, crowd lightsticks.
public static class KpopWorld
{
    static RunLibrary lib;
    static System.Random rnd;
    public static readonly List<(Material m, Color c)> BeatMats = new List<(Material, Color)>();   // emission pulses on the beat
    static readonly Color[] Neon =
    {
        new Color(1f, 0.25f, 0.8f), new Color(0.25f, 0.9f, 1f), new Color(0.7f, 0.35f, 1f),
        new Color(1f, 0.85f, 0.25f), new Color(0.35f, 1f, 0.65f),
    };

    public static Material Glow(Color c, float i, bool beat = false)
    {
        var m = WorldBuilder.Emissive(c, i);
        if (beat) BeatMats.Add((m, c * i));
        return m;
    }

    static Material SignMat(Texture2D t)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetTexture("_BaseMap", t);
        m.SetColor("_BaseColor", Color.white * 1.6f);
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 1f);          // additive
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        return m;
    }

    static GameObject Quad(Transform p, Vector3 pos, Quaternion rot, Vector2 size, Material m)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(p);
        q.transform.SetPositionAndRotation(pos, rot);
        q.transform.localScale = new Vector3(size.x, size.y, 1);
        var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return q;
    }

    // a demon from the Quaternius packs made spooky: dark body, purple/magenta glow
    public static void Demonize(GameObject go, float glow = 0.9f)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var ms = r.materials;
            foreach (var m in ms)
            {
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.Lerp(m.GetColor("_BaseColor"), new Color(0.35f, 0.12f, 0.5f), 0.55f));
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(0.8f, 0.15f, 1f) * glow);
            }
            r.materials = ms;
        }
    }

    public static void Build(RunLibrary l, RunTheme theme, Transform root, float length, System.Random r)
    {
        lib = l; rnd = r;
        BeatMats.Clear();
        KpopFx.SpotAnchors.Clear();
        KpopFx.LedMat = null;
        float L = length + 160f, half = length * 0.5f + 60f;
        var cam = Camera.main;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        Color sky = theme == RunTheme.NeonCity ? new Color(0.08f, 0.04f, 0.2f) : theme == RunTheme.ConcertStage ? new Color(0.05f, 0.02f, 0.1f) : new Color(0.16f, 0.04f, 0.22f);
        RenderSettings.fogColor = sky;
        RenderSettings.fogStartDistance = theme == RunTheme.DemonRealm ? 25f : 50f;
        RenderSettings.fogEndDistance = theme == RunTheme.DemonRealm ? 150f : 210f;
        RenderSettings.ambientLight = theme == RunTheme.NeonCity ? new Color(0.45f, 0.4f, 0.7f) : theme == RunTheme.ConcertStage ? new Color(0.42f, 0.35f, 0.6f) : new Color(0.5f, 0.32f, 0.62f);
        if (cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = sky; }
        foreach (var dl in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (dl.type == LightType.Directional)
            {
                dl.intensity = 0.55f; dl.color = new Color(0.65f, 0.55f, 1f);
                dl.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            }

        switch (theme)
        {
            case RunTheme.NeonCity: City(root, length, L, half); break;
            case RunTheme.ConcertStage: Stage(root, length, L, half); break;
            default: Realm(root, length, L, half); break;
        }
        var fx = root.gameObject.AddComponent<KpopFx>();
        fx.Setup(lib, theme, length);
    }

    // ---------------- neon city ----------------
    static void City(Transform root, float length, float L, float half)
    {
        var road = HalloweenWorld.TexMat(lib.asphaltTex, 4, L / 3, Color.white, 0, 0.9f);   // wet, reflective
        WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(9f, 0.2f, L), road);
        var walk = WorldBuilder.Mat(new Color(0.12f, 0.11f, 0.18f)); walk.SetFloat("_Smoothness", 0.7f);
        foreach (int s in new[] { -1, 1 })
        {
            WorldBuilder.Box(root, new Vector3(s * 6.6f, 0.15f, half), new Vector3(4.2f, 0.3f, L), walk);
            WorldBuilder.Box(root, new Vector3(s * 4.5f, 0.32f, half), new Vector3(0.12f, 0.06f, L), Glow(Neon[s > 0 ? 0 : 1], 5f, true));
            WorldBuilder.BuildingRow(lib.skyscrapers, root, length, rnd, s, 8.8f, 6f, go => WorldBuilder.Tint(go, Neon[rnd.Next(Neon.Length)] * 0.6f));
            // neon signs on the facades
            for (float z = 4; z < length + 120; z += 7f + rnd.Next(8))
            {
                var t = lib.kpopSigns[rnd.Next(lib.kpopSigns.Length)];
                bool round = t.width == t.height;
                float w = round ? 3.2f : 5.5f + rnd.Next(3);
                Quad(root, new Vector3(s * 8.7f, 4f + rnd.Next(10), z), Quaternion.Euler(0, s < 0 ? -90 : 90, 0), new Vector2(w, round ? w : w * 0.5f), SignMat(t));
            }
            // street lamps with neon tubes
            for (float z = 0; z < length + 100; z += 14f)
            {
                WorldBuilder.Box(root, new Vector3(s * 5.2f, 2.6f, z), new Vector3(0.12f, 5.2f, 0.12f), WorldBuilder.Mat(new Color(0.1f, 0.1f, 0.14f)));
                WorldBuilder.Box(root, new Vector3(s * 5.2f, 2.6f, z), new Vector3(0.2f, 3.2f, 0.2f), Glow(Neon[(int)(z / 14) % Neon.Length], 4f, true));
            }
        }
        // string lights across the street
        for (float z = 10; z < length + 100; z += 12f)
        {
            var bulb = Glow(Neon[rnd.Next(Neon.Length)], 6f, true);
            WorldBuilder.Box(root, new Vector3(0, 6.2f, z), new Vector3(13f, 0.03f, 0.03f), WorldBuilder.Mat(new Color(0.05f, 0.05f, 0.05f)));
            for (int i = 0; i < 9; i++)
            {
                float x = -6f + i * 1.5f;
                WorldBuilder.Box(root, new Vector3(x, 6.05f - Mathf.Sin(i / 8f * Mathf.PI) * 0.6f, z), Vector3.one * 0.18f, bulb);
            }
        }
        Stars(root, length);
    }

    // ---------------- concert arena ----------------
    static void Stage(Transform root, float length, float L, float half)
    {
        var floor = HalloweenWorld.TexMat(lib.stageTex, 4, L / 2.2f, Color.white, 0, 0.85f);
        WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(9f, 0.2f, L), floor);
        var black = WorldBuilder.Mat(new Color(0.05f, 0.04f, 0.07f)); black.SetFloat("_Smoothness", 0.6f);
        WorldBuilder.Box(root, new Vector3(0, -0.4f, half), new Vector3(120f, 0.2f, L + 200), black);
        // glowing floor strips that flash on the beat
        for (float z = 0; z < length + 100; z += 4f)
            WorldBuilder.Box(root, new Vector3(0, 0.01f, z), new Vector3(8.6f, 0.02f, 0.12f), Glow(Neon[(int)(z / 4) % Neon.Length], 4f, true));
        var led = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        led.SetTexture("_BaseMap", lib.ledTex); led.SetColor("_BaseColor", Color.white * 1.3f);
        KpopFx.LedMat = led;
        var truss = WorldBuilder.Mat(new Color(0.55f, 0.56f, 0.6f)); truss.SetFloat("_Metallic", 0.7f);
        foreach (int s in new[] { -1, 1 })
        {
            // stage edge and speaker stacks
            WorldBuilder.Box(root, new Vector3(s * 4.6f, 0.3f, half), new Vector3(0.3f, 0.6f, L), black);
            WorldBuilder.Box(root, new Vector3(s * 4.62f, 0.62f, half), new Vector3(0.32f, 0.04f, L), Glow(Neon[2], 5f, true));
            for (float z = 6; z < length + 100; z += 16f)
            {
                WorldBuilder.Box(root, new Vector3(s * 6f, 1.2f, z), new Vector3(1.6f, 2.4f, 1.4f), black);
                WorldBuilder.Box(root, new Vector3(s * 5.19f, 1.6f, z), new Vector3(0.02f, 0.8f, 0.8f), Glow(Neon[0], 3f, true));
                WorldBuilder.Box(root, new Vector3(s * 5.19f, 0.6f, z), new Vector3(0.02f, 0.5f, 0.5f), Glow(Neon[1], 3f, true));
            }
            // seating tiers for the crowd
            for (int t = 0; t < 5; t++)
                WorldBuilder.Box(root, new Vector3(s * (9f + t * 2.2f), t * 1f, half), new Vector3(2.2f, 0.5f, L), black);
            // giant LED screens
            for (float z = 20; z < length + 120; z += 42f)
                Quad(root, new Vector3(s * 21f, 9f, z), Quaternion.Euler(0, s < 0 ? -90 : 90, 0), new Vector2(16f, 9f), led);
        }
        // trusses over the stage
        for (float z = 12; z < length + 100; z += 20f)
        {
            foreach (int s in new[] { -1, 1 })
                WorldBuilder.Box(root, new Vector3(s * 7f, 4f, z), new Vector3(0.3f, 8f, 0.3f), truss);
            WorldBuilder.Box(root, new Vector3(0, 8f, z), new Vector3(14.3f, 0.35f, 0.35f), truss);
            KpopFx.SpotAnchors.Add(new Vector3(-3f, 7.8f, z)); KpopFx.SpotAnchors.Add(new Vector3(3f, 7.8f, z));
        }
    }

    // ---------------- demon realm ----------------
    static void Realm(Transform root, float length, float L, float half)
    {
        var ground = HalloweenWorld.TexMat(lib.realmTex, 4, L / 3, Color.white, 0, 0.3f);
        ground.EnableKeyword("_EMISSION"); ground.SetTexture("_EmissionMap", lib.realmTex); ground.SetTextureScale("_EmissionMap", new Vector2(4, L / 3)); ground.SetColor("_EmissionColor", Color.white * 0.6f);
        WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(9f, 0.2f, L), ground);
        var far = HalloweenWorld.TexMat(lib.realmTex, 60, (L + 200) / 4, new Color(0.7f, 0.6f, 0.8f), 0, 0.2f);
        WorldBuilder.Box(root, new Vector3(0, -0.3f, half), new Vector3(300f, 0.2f, L + 200), far);
        var rock = WorldBuilder.Mat(new Color(0.16f, 0.08f, 0.22f)); rock.SetFloat("_Smoothness", 0.2f);
        var crystalA = Glow(new Color(0.85f, 0.2f, 1f), 3.5f, true);
        var crystalB = Glow(new Color(0.25f, 0.85f, 1f), 3.5f, true);
        foreach (int s in new[] { -1, 1 })
        {
            // crystal clusters along the path
            for (float z = 0; z < length + 100; z += 3f + rnd.Next(4))
                for (int k = 0; k < 3; k++)
                {
                    var c = WorldBuilder.Box(root, new Vector3(s * (5.2f + (float)rnd.NextDouble() * 2.5f), 0.6f, z + (float)rnd.NextDouble()), new Vector3(0.35f, 1.4f + (float)rnd.NextDouble() * 1.6f, 0.35f), rnd.NextDouble() < 0.6 ? crystalA : crystalB);
                    c.transform.rotation = Quaternion.Euler(rnd.Next(-25, 25), rnd.Next(360), rnd.Next(-25, 25));
                }
            // jagged rock spires
            for (float z = 0; z < length + 160; z += 8f + rnd.Next(10))
            {
                float h = 8 + rnd.Next(22);
                var sp = WorldBuilder.Box(root, new Vector3(s * (14f + rnd.Next(25)), h * 0.5f - 1, z), new Vector3(3 + rnd.Next(4), h, 3 + rnd.Next(4)), rock);
                sp.transform.rotation = Quaternion.Euler(0, rnd.Next(360), rnd.Next(-8, 8));
            }
            // floating islands with glowing undersides
            for (float z = 10; z < length + 200; z += 22f + rnd.Next(18))
            {
                var p = new Vector3(s * (16f + rnd.Next(30)), 10f + rnd.Next(14), z);
                WorldBuilder.Box(root, p, new Vector3(6 + rnd.Next(6), 2f, 5 + rnd.Next(5)), rock);
                WorldBuilder.Box(root, p + Vector3.down * 1.05f, new Vector3(4f, 0.1f, 3f), crystalA);
            }
            // giant demon silhouettes on the horizon
            for (float z = 60; z < length + 250; z += 120f + rnd.Next(60))
            {
                var dp = lib.chaser ? lib.chaser : null;
                if (!dp) break;
                var g = WorldBuilder.Spawn(dp, new Vector3(s * (60f + rnd.Next(20)), 0, z), s < 0 ? 60 : -60, root);
                BlockWorld.Fit(g, 35f);
                Demonize(g, 0.5f);
            }
        }
        // glowing portal rings over the road
        for (float z = 30; z < length + 100; z += 50f)
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2 / 16;
                var seg = WorldBuilder.Box(root, new Vector3(Mathf.Cos(a) * 6.5f, 2.5f + Mathf.Sin(a) * 6.5f, z), new Vector3(0.45f, 2.6f, 0.45f), i % 2 == 0 ? crystalA : crystalB);
                seg.transform.rotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
            }
        Stars(root, length);
    }

    static void Stars(Transform root, float length)
    {
        var star = Glow(Color.white, 2.5f);
        for (int i = 0; i < 260; i++)
            WorldBuilder.Box(root, new Vector3(rnd.Next(-160, 160), 40 + rnd.Next(50), rnd.Next(-20, (int)length + 300)), Vector3.one * (0.3f + (float)rnd.NextDouble() * 0.4f), star);
    }
}

// Beat pulse, sweeping spotlights, crowd lightsticks, confetti / demon sparks, purple lightning.
public class KpopFx : MonoBehaviour
{
    public static readonly List<Vector3> SpotAnchors = new List<Vector3>();
    public static Material LedMat;
    RunTheme theme;
    Camera cam;
    RunDirector d;
    readonly List<Transform> spots = new List<Transform>();
    Transform crowdL, crowdR, sparks;
    Light flash;
    float nextBolt = 5f, boltT = -1;
    Light sun; float sunBase;

    public void Setup(RunLibrary lib, RunTheme t, float length)
    {
        theme = t; cam = Camera.main; d = FindAnyObjectByType<RunDirector>();
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) sun = l;
        sunBase = sun ? sun.intensity : 0;
        if (t == RunTheme.ConcertStage)
        {
            // spotlight cones (additive) hanging from the trusses
            var mesh = ConeMesh();
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 1f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
            int i = 0;
            foreach (var a in SpotAnchors)
            {
                var g = new GameObject("Spot", typeof(MeshFilter), typeof(MeshRenderer));
                g.transform.SetParent(WorldBuilder.Dyn); g.transform.position = a;
                g.GetComponent<MeshFilter>().sharedMesh = mesh;
                var mi = new Material(m); var c = i % 3 == 0 ? new Color(0.5f, 0.15f, 0.45f) : i % 3 == 1 ? new Color(0.12f, 0.35f, 0.5f) : new Color(0.4f, 0.25f, 0.55f);
                mi.SetColor("_BaseColor", c * 0.35f);
                var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = mi; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                spots.Add(g.transform); i++;
            }
            crowdL = Crowd(-1); crowdR = Crowd(1);
        }
        if (t == RunTheme.NeonCity || t == RunTheme.ConcertStage) sparks = Particles(new Color(1f, 0.5f, 0.9f), new Color(0.5f, 0.9f, 1f), 0.12f, 160, -1.2f);
        if (t == RunTheme.DemonRealm) sparks = Particles(new Color(0.9f, 0.2f, 1f), new Color(0.3f, 0.8f, 1f), 0.15f, 220, 1.4f);
        if (t != RunTheme.NeonCity)
        {
            var fg = new GameObject("StageLight"); fg.transform.SetParent(WorldBuilder.Dyn);
            flash = fg.AddComponent<Light>(); flash.type = LightType.Point; flash.range = 26; flash.intensity = 0; flash.shadows = LightShadows.None;
            flash.color = t == RunTheme.DemonRealm ? new Color(0.8f, 0.3f, 1f) : new Color(1f, 0.5f, 0.9f);
        }
    }

    static Mesh cone;
    static Mesh ConeMesh()
    {
        if (cone) return cone;
        const int n = 20; float len = 9f, r0 = 0.15f, r1 = 2.2f;
        var v = new Vector3[n * 2]; var t = new int[n * 6];
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2 / n;
            v[i] = new Vector3(Mathf.Cos(a) * r0, 0, Mathf.Sin(a) * r0);
            v[n + i] = new Vector3(Mathf.Cos(a) * r1, -len, Mathf.Sin(a) * r1);
            int j = (i + 1) % n;
            t[i * 6] = i; t[i * 6 + 1] = j; t[i * 6 + 2] = n + i; t[i * 6 + 3] = j; t[i * 6 + 4] = n + j; t[i * 6 + 5] = n + i;
        }
        cone = new Mesh { vertices = v, triangles = t }; cone.RecalculateNormals();
        return cone;
    }

    // waving lightsticks on the seating tiers: a particle box that rides along with the camera
    Transform Crowd(int side)
    {
        var g = new GameObject("Crowd" + side); g.transform.SetParent(WorldBuilder.Dyn);
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.startLifetime = 5f; main.startSpeed = 0f; main.maxParticles = 3000;
        main.startSize3D = true; main.startSizeX = 0.12f; main.startSizeY = 0.55f; main.startSizeZ = 0.12f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.3f, 0.85f), new Color(0.3f, 0.9f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 520;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(10f, 4f, 70f);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 2f;
        var pr = g.GetComponent<ParticleSystemRenderer>();
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 1f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); m.renderQueue = 3000;
        pr.sharedMaterial = m;
        ps.Play();
        return g.transform;
    }

    Transform Particles(Color a, Color b, float size, int rate, float rise)
    {
        var g = new GameObject("Sparks"); g.transform.SetParent(WorldBuilder.Dyn);
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.startLifetime = 4f; main.startSpeed = 0.2f; main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = new ParticleSystem.MinMaxGradient(a, b); main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 1200;
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(26, 8, 40);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f); vel.y = new ParticleSystem.MinMaxCurve(rise * 0.5f, rise); vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        var pr = g.GetComponent<ParticleSystemRenderer>();
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 1f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); m.renderQueue = 3000;
        pr.sharedMaterial = m;
        ps.Play();
        return g.transform;
    }

    void OnDestroy() { if (sun) sun.intensity = sunBase; }

    void LateUpdate()
    {
        if (!cam || !d || d.CurrentLevel < 0) return;
        var cp = cam.transform.position;
        float bpm = d.levels[d.CurrentLevel].bpm, t = d.SongTime;
        float beat = t * bpm / 60f;
        float pulse = Mathf.Pow(1f - Mathf.Repeat(beat, 1f), 3f);   // sharp hit on each beat, then decays

        foreach (var (m, c) in KpopWorld.BeatMats)
            if (m) m.SetColor("_EmissionColor", c * (0.55f + 0.9f * pulse));
        if (LedMat) { LedMat.SetTextureOffset("_BaseMap", new Vector2(t * 0.15f, t * 0.05f)); LedMat.SetColor("_BaseColor", Color.white * (0.9f + 0.8f * pulse)); }

        for (int i = 0; i < spots.Count; i++)
        {
            float a = Mathf.Sin(t * 1.3f + i * 1.7f) * 35f, b = Mathf.Cos(t * 0.9f + i) * 25f;
            spots[i].rotation = Quaternion.Euler(b, 0, a);
        }
        if (crowdL) crowdL.position = new Vector3(-12.5f, 3.2f, cp.z + 30f);
        if (crowdR) crowdR.position = new Vector3(12.5f, 3.2f, cp.z + 30f);
        if (sparks) sparks.position = cp + new Vector3(0, 2f, 20f);

        if (flash)
        {
            flash.transform.position = cp + new Vector3(0, 5f, 10f);
            if (theme == RunTheme.ConcertStage) flash.intensity = 2.5f * pulse;
            else
            {
                // purple lightning in the demon realm
                if (t > nextBolt) { boltT = 0; nextBolt = t + Random.Range(6f, 11f); }
                if (boltT >= 0)
                {
                    boltT += Time.deltaTime;
                    float f = boltT < 0.07f ? 1f : boltT < 0.14f ? 0.2f : boltT < 0.22f ? 0.9f : Mathf.Max(0, 1f - (boltT - 0.22f) * 4f);
                    flash.intensity = 6f * f;
                    if (sun) sun.intensity = sunBase + f * 1.5f;
                    if (boltT > 0.6f) { boltT = -1; flash.intensity = 0; if (sun) sun.intensity = sunBase; }
                }
            }
        }
    }
}
