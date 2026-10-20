using UnityEngine;

// Halloween worlds: haunted mansion corridor, pumpkin farm at night, graveyard in the rain.
public static class HalloweenWorld
{
    static RunLibrary lib;
    static System.Random rnd;
    public static readonly System.Collections.Generic.List<Vector3> LightSpots = new System.Collections.Generic.List<Vector3>();

    static Light Sun()
    {
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) return l;
        return null;
    }

    static GameObject K(string n) => lib.Kit(n);

    static GameObject Put(string name, Vector3 pos, float rotY, float height, Transform root)
    {
        var p = K(name);
        if (!p) return null;
        var go = WorldBuilder.Spawn(p, pos, rotY, root);
        BlockWorld.Fit(go, height);
        go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y + pos.y, go.transform.position.z);
        return go;
    }

    public static Material TexMat(Texture2D t, float u, float v, Color tint, float glow = 0, float smooth = 0.15f)
    {
        var m = WorldBuilder.Mat(tint);
        if (t) { m.SetTexture("_BaseMap", t); m.SetTextureScale("_BaseMap", new Vector2(u, v)); }
        m.SetFloat("_Smoothness", smooth);
        if (glow > 0) { m.EnableKeyword("_EMISSION"); if (t) { m.SetTexture("_EmissionMap", t); m.SetTextureScale("_EmissionMap", new Vector2(u, v)); } m.SetColor("_EmissionColor", tint * glow); }
        return m;
    }

    public static Material Transparent(Texture2D t, Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetTexture("_BaseMap", t);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        return m;
    }

    static void Quad(Transform root, Vector3 pos, Quaternion rot, Vector2 size, Material m)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(root);
        q.transform.SetPositionAndRotation(pos, rot);
        q.transform.localScale = new Vector3(size.x, size.y, 1);
        q.GetComponent<Renderer>().sharedMaterial = m;
    }

    public static void Build(RunLibrary l, RunTheme theme, Transform root, float length, System.Random r)
    {
        lib = l; rnd = r;
        LightSpots.Clear();
        float L = length + 160f, half = length * 0.5f + 60f;
        var cam = Camera.main;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        var sun = Sun();

        switch (theme)
        {
            case RunTheme.Mansion: Mansion(root, length, L, half); break;
            case RunTheme.PumpkinFarm: Farm(root, length, L, half); break;
            default: Graveyard(root, length, L, half); break;
        }

        Color sky = theme == RunTheme.Mansion ? new Color(0.03f, 0.015f, 0.04f) : theme == RunTheme.PumpkinFarm ? new Color(0.09f, 0.08f, 0.3f) : new Color(0.06f, 0.08f, 0.17f);
        RenderSettings.fogColor = sky;
        RenderSettings.fogDensity = theme == RunTheme.Mansion ? 0.022f : theme == RunTheme.PumpkinFarm ? 0.013f : 0.025f;
        RenderSettings.ambientLight = theme == RunTheme.Mansion ? new Color(0.58f, 0.42f, 0.52f) : theme == RunTheme.PumpkinFarm ? new Color(0.4f, 0.42f, 0.7f) : new Color(0.38f, 0.43f, 0.62f);
        if (cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = sky; }
        if (sun)
        {
            sun.color = theme == RunTheme.Mansion ? new Color(0.5f, 0.45f, 0.8f) : new Color(0.55f, 0.65f, 1f);
            sun.intensity = theme == RunTheme.Mansion ? 0.15f : 0.45f;
            sun.transform.rotation = Quaternion.Euler(35f, 160f, 0f);
        }
        LightSpots.Sort((a, b) => a.z.CompareTo(b.z));
        var fx = root.gameObject.AddComponent<HalloweenFx>();
        fx.Setup(lib, theme, length);
    }

    // ---------------- haunted mansion ----------------
    static void Mansion(Transform root, float length, float L, float half)
    {
        var floor = TexMat(lib.darkWood, 10, L / 4, new Color(0.7f, 0.55f, 0.45f), 0, 0.45f);
        var carpet = WorldBuilder.Mat(new Color(0.55f, 0.03f, 0.04f));
        carpet.SetFloat("_Smoothness", 0.1f);
        var wall = TexMat(lib.wallpaper, L / 3, 2, Color.white);
        var wains = TexMat(lib.darkWood, L / 3, 1, new Color(0.6f, 0.45f, 0.35f));
        var ceil = WorldBuilder.Mat(new Color(0.08f, 0.05f, 0.06f));
        var gold = WorldBuilder.Mat(new Color(0.6f, 0.45f, 0.15f)); gold.SetFloat("_Metallic", 0.8f); gold.SetFloat("_Smoothness", 0.6f);
        var flame = WorldBuilder.Emissive(new Color(1f, 0.6f, 0.2f), 4f);
        var window = WorldBuilder.Emissive(new Color(0.35f, 0.45f, 1f), 1.4f);
        var web = Transparent(lib.cobweb, new Color(1, 1, 1, 0.55f));

        WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(11f, 0.2f, L), floor);
        WorldBuilder.Box(root, new Vector3(0, 0.005f, half), new Vector3(6.4f, 0.02f, L), carpet);
        foreach (int s in new[] { -1, 1 })
        {
            WorldBuilder.Box(root, new Vector3(s * 5.4f, 3.5f, half), new Vector3(0.2f, 7f, L), wall);
            WorldBuilder.Box(root, new Vector3(s * 5.25f, 0.6f, half), new Vector3(0.12f, 1.2f, L), wains);
            WorldBuilder.Box(root, new Vector3(s * 5.2f, 1.22f, half), new Vector3(0.1f, 0.08f, L), gold);
        }
        WorldBuilder.Box(root, new Vector3(0, 7.05f, half), new Vector3(11f, 0.2f, L), ceil);

        for (float z = 0; z < length + 100; z += 6f)
        {
            int k = (int)(z / 6f);
            foreach (int s in new[] { -1, 1 })
            {
                // candle sconce
                if (k % 2 == 0)
                {
                    WorldBuilder.Box(root, new Vector3(s * 5.15f, 2.6f, z), new Vector3(0.12f, 0.35f, 0.12f), gold);
                    WorldBuilder.Box(root, new Vector3(s * 5.05f, 2.85f, z), new Vector3(0.07f, 0.16f, 0.07f), flame);
                    LightSpots.Add(new Vector3(s * 4.6f, 2.9f, z));
                }
                // creepy portrait / window / door
                int kind = (k + (s > 0 ? 1 : 0) + rnd.Next(2)) % 4;
                float pz = z + 3f;
                if (kind == 0 && lib.portraits != null && lib.portraits.Length > 0)
                {
                    var tex = lib.portraits[rnd.Next(lib.portraits.Length)];
                    var pm = TexMat(tex, 1, 1, Color.white, 0.35f);
                    Quad(root, new Vector3(s * 5.28f, 3.4f, pz), Quaternion.Euler(0, s < 0 ? -90 : 90, 0), new Vector2(1.6f, 2f), pm);
                }
                else if (kind == 1)
                {
                    WorldBuilder.Box(root, new Vector3(s * 5.29f, 3.6f, pz), new Vector3(0.05f, 3f, 1.6f), window);
                    WorldBuilder.Box(root, new Vector3(s * 5.27f, 3.6f, pz), new Vector3(0.06f, 3.1f, 0.08f), ceil);
                    WorldBuilder.Box(root, new Vector3(s * 5.27f, 3.6f, pz), new Vector3(0.06f, 0.08f, 1.7f), ceil);
                }
                else if (kind == 2)
                {
                    WorldBuilder.Box(root, new Vector3(s * 5.28f, 1.6f, pz), new Vector3(0.1f, 3.2f, 1.6f), WorldBuilder.Mat(new Color(0.12f, 0.06f, 0.04f)));
                    WorldBuilder.Box(root, new Vector3(s * 5.2f, 1.6f, pz + 0.55f), new Vector3(0.08f, 0.08f, 0.08f), gold);
                }
                // props along the walls
                if (rnd.NextDouble() < 0.5)
                {
                    string[] props = { "coffin", "urn-round", "candle-multiple", "pumpkin-carved", "altar-wood", "pillar-small", "urn-square" };
                    var name = props[rnd.Next(props.Length)];
                    var go = Put(name, new Vector3(s * 4.3f, 0, z + 1.5f), s < 0 ? 90 : -90, name == "coffin" ? 2.1f : name.StartsWith("pillar") ? 1.4f : name.StartsWith("candle") ? 0.8f : 1f, root);
                    if (go && name == "coffin") go.transform.rotation = Quaternion.Euler(-75f, s < 0 ? 90 : -90, 0) * K(name).transform.rotation;
                }
                // cobwebs in the upper corners
                if (rnd.NextDouble() < 0.35)
                    Quad(root, new Vector3(s * 4.6f, 6.3f, z), Quaternion.Euler(0, 0, s < 0 ? 0 : 90) * Quaternion.Euler(0, 0, 180), new Vector2(1.6f, 1.6f), web);
            }
            // chandelier every 18 m
            if (k % 3 == 1)
            {
                var c = new Vector3(0, 5.8f, z);
                WorldBuilder.Box(root, c + Vector3.up * 0.6f, new Vector3(0.05f, 1.2f, 0.05f), gold);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4;
                    var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.9f;
                    WorldBuilder.Box(root, p, new Vector3(0.1f, 0.25f, 0.1f), gold);
                    WorldBuilder.Box(root, p + Vector3.up * 0.2f, new Vector3(0.06f, 0.14f, 0.06f), flame);
                }
                WorldBuilder.Box(root, c, new Vector3(1.9f, 0.06f, 1.9f), gold);
                LightSpots.Add(c + Vector3.down * 0.4f);
            }
        }
    }

    // ---------------- pumpkin farm at night ----------------
    static void Farm(Transform root, float length, float L, float half)
    {
        var dirt = WorldBuilder.Mat(new Color(0.24f, 0.19f, 0.15f)); dirt.SetFloat("_Smoothness", 0.05f);
        var field = WorldBuilder.Mat(new Color(0.1f, 0.13f, 0.08f));
        WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(9f, 0.2f, L), dirt);
        WorldBuilder.Box(root, new Vector3(0, -0.15f, half), new Vector3(300f, 0.2f, L + 200), field);
        var glowPumpkin = WorldBuilder.Emissive(new Color(1f, 0.45f, 0.05f), 2.2f);

        foreach (int s in new[] { -1, 1 })
        {
            // fence along the road
            for (float z = -20; z < length + 100; z += 2f)
                Put(rnd.NextDouble() < 0.12 ? "fence-damaged" : "fence", new Vector3(s * 5f, 0, z), 90, 1.3f, root);
            // glowing pumpkins along the fence
            for (float z = 0; z < length + 100; z += 5f + rnd.Next(5))
            {
                var p = Put(rnd.NextDouble() < 0.5 ? "pumpkin-carved" : "pumpkin-tall-carved", new Vector3(s * 4.3f, 0, z), rnd.Next(360), 0.7f + (float)rnd.NextDouble() * 0.3f, root);
                if (p) { WorldBuilder.Box(root, p.transform.position + Vector3.up * 0.3f, Vector3.one * 0.25f, glowPumpkin); LightSpots.Add(p.transform.position + Vector3.up * 0.6f); }
            }
            // pumpkin field rows, hay bales, scarecrows, crooked pines
            for (float z = 0; z < length + 140; z += 3f + rnd.Next(4))
            {
                float x = s * (7f + (float)rnd.NextDouble() * 16f);
                double pick = rnd.NextDouble();
                if (pick < 0.55) Put(rnd.NextDouble() < 0.7 ? "pumpkin" : "pumpkin-tall", new Vector3(x, 0, z), rnd.Next(360), 0.6f + (float)rnd.NextDouble() * 0.5f, root);
                else if (pick < 0.75) Put(rnd.NextDouble() < 0.5 ? "hay-bale" : "hay-bale-bundled", new Vector3(x, 0, z), rnd.Next(360), 1.1f, root);
                else if (pick < 0.82) Scarecrow(root, new Vector3(x, 0, z), s);
                else Put(rnd.NextDouble() < 0.5 ? "pine-crooked" : "pine-fall-crooked", new Vector3(s * (12f + rnd.Next(20)), 0, z), rnd.Next(360), 5f + rnd.Next(5), root);
            }
            for (float z = 0; z < length + 100; z += 26f + rnd.Next(10))
            { Put("lightpost-single", new Vector3(s * 4.6f, 0, z), s < 0 ? 90 : -90, 3.2f, root); LightSpots.Add(new Vector3(s * 4.2f, 3f, z)); }
            // dark tree line on the horizon
            for (float z = -20; z < length + 200; z += 4f)
                Put("pine", new Vector3(s * (34f + rnd.Next(10)), 0, z), rnd.Next(360), 9f + rnd.Next(8), root);
        }
        // windmill silhouette far away
        for (float z = 160; z < length + 200; z += 280)
        {
            var dark = WorldBuilder.Mat(new Color(0.05f, 0.05f, 0.08f));
            float x = (rnd.Next(2) * 2 - 1) * 45f;
            WorldBuilder.Box(root, new Vector3(x, 7, z), new Vector3(4, 14, 4), dark);
            for (int i = 0; i < 4; i++)
            {
                var blade = WorldBuilder.Box(root, new Vector3(x, 14, z - 2.2f), new Vector3(1.2f, 10f, 0.2f), dark);
                blade.transform.rotation = Quaternion.Euler(0, 0, i * 90 + 20);
                blade.transform.position += blade.transform.up * 5f;
            }
        }
    }

    static void Scarecrow(Transform root, Vector3 p, int s)
    {
        var wood = WorldBuilder.Mat(new Color(0.3f, 0.2f, 0.12f));
        WorldBuilder.Box(root, p + Vector3.up * 1.3f, new Vector3(0.15f, 2.6f, 0.15f), wood);
        WorldBuilder.Box(root, p + Vector3.up * 2.0f, new Vector3(1.8f, 0.12f, 0.12f), wood);
        var body = WorldBuilder.Mat(new Color(0.45f, 0.3f, 0.2f));
        WorldBuilder.Box(root, p + Vector3.up * 1.75f, new Vector3(0.7f, 0.9f, 0.35f), body);
        var head = Put("pumpkin-carved", p + Vector3.up * 2.25f, s < 0 ? -90 : 90, 0.6f, root);
        WorldBuilder.Box(root, p + new Vector3(0, 2.85f, 0), new Vector3(0.9f, 0.08f, 0.9f), WorldBuilder.Mat(new Color(0.12f, 0.1f, 0.08f)));
        WorldBuilder.Box(root, p + new Vector3(0, 3.1f, 0), new Vector3(0.45f, 0.45f, 0.45f), WorldBuilder.Mat(new Color(0.12f, 0.1f, 0.08f)));
    }

    // ---------------- graveyard in the rain ----------------
    static void Graveyard(Transform root, float length, float L, float half)
    {
        var path = WorldBuilder.Mat(new Color(0.16f, 0.17f, 0.2f)); path.SetFloat("_Smoothness", 0.85f);   // wet stones
        var grass = WorldBuilder.Mat(new Color(0.07f, 0.1f, 0.09f)); grass.SetFloat("_Smoothness", 0.6f);
        WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(9f, 0.2f, L), path);
        WorldBuilder.Box(root, new Vector3(0, -0.15f, half), new Vector3(300f, 0.2f, L + 200), grass);
        string[] stones = { "gravestone-cross", "gravestone-round", "gravestone-bevel", "gravestone-decorative", "gravestone-roof", "gravestone-wide", "gravestone-broken", "gravestone-cross-large", "cross", "cross-wood", "grave", "grave-border" };
        var glowPumpkin = WorldBuilder.Emissive(new Color(1f, 0.45f, 0.05f), 2.2f);
        var candle = WorldBuilder.Emissive(new Color(1f, 0.7f, 0.3f), 8f);

        foreach (int s in new[] { -1, 1 })
        {
            for (float z = -20; z < length + 100; z += 2f)
                Put(rnd.NextDouble() < 0.1 ? "iron-fence-damaged" : "iron-fence", new Vector3(s * 4.8f, 0, z), 90, 1.5f, root);
            // dense rows of graves
            for (float z = -10; z < length + 120; z += 2.2f)
                for (int row = 0; row < 4; row++)
                {
                    if (rnd.NextDouble() < 0.25) continue;
                    var n = stones[rnd.Next(stones.Length)];
                    var g = Put(n, new Vector3(s * (6.5f + row * 2.6f + (float)rnd.NextDouble() * 0.6f), 0, z + (float)rnd.NextDouble()), (s < 0 ? 90 : -90) + rnd.Next(-12, 12), n.StartsWith("grave") && !n.StartsWith("gravestone") ? 0.5f : 1.1f + (float)rnd.NextDouble() * 0.6f, root);
                    if (g && rnd.NextDouble() < 0.05) WorldBuilder.Box(root, g.transform.position + new Vector3(0, 0.15f, -0.5f), new Vector3(0.06f, 0.2f, 0.06f), candle);
                }
            for (float z = 8; z < length + 120; z += 22f + rnd.Next(18))
                Put(rnd.NextDouble() < 0.5 ? "crypt" : rnd.NextDouble() < 0.5 ? "crypt-a" : "crypt-b", new Vector3(s * (17f + rnd.Next(6)), 0, z), s < 0 ? 90 : -90, 4.5f + rnd.Next(2), root);
            for (float z = 0; z < length + 140; z += 7f + rnd.Next(7))
                Put(rnd.NextDouble() < 0.5 ? "pine-crooked" : "pine-fall-crooked", new Vector3(s * (9f + rnd.Next(18)), 0, z), rnd.Next(360), 5f + rnd.Next(5), root);
            for (float z = 4; z < length + 100; z += 16f + rnd.Next(10))
            {
                var p = Put("pumpkin-carved", new Vector3(s * 4.1f, 0, z), rnd.Next(360), 0.6f, root);
                if (p) { WorldBuilder.Box(root, p.transform.position + Vector3.up * 0.25f, Vector3.one * 0.22f, glowPumpkin); LightSpots.Add(p.transform.position + Vector3.up * 0.6f); }
            }
            for (float z = 10; z < length + 100; z += 24f)
            { Put("lightpost-double", new Vector3(s * 4.3f, 0, z), s < 0 ? 90 : -90, 3.4f, root); LightSpots.Add(new Vector3(s * 3.8f, 3.2f, z)); }
        }
        // big crypt / chapel silhouettes far behind
        for (float z = 60; z < length + 200; z += 90f)
            Put("crypt-large", new Vector3((rnd.Next(2) * 2 - 1) * (30f + rnd.Next(10)), 0, z), rnd.Next(4) * 90, 9f, root);
    }
}

// Per-level atmosphere: sky (stars + moon), lantern light at the camera, rain, lightning, bats, ghosts.
public class HalloweenFx : MonoBehaviour
{
    RunTheme theme;
    Camera cam;
    Transform sky, rain;
    Light lantern, sun;
    float nextBolt, boltT = -1, sunBase;
    Material flashMat;
    Transform[] bats; Vector3[] batVel;
    Transform ghost; float ghostT = -1, nextGhost;
    RunLibrary lib;
    AudioSource thunder;
    Light[] pool;

    public void Setup(RunLibrary l, RunTheme t, float length)
    {
        lib = l; theme = t; cam = Camera.main;
        foreach (var dl in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (dl.type == LightType.Directional) sun = dl;
        pool = new Light[6];
        for (int i = 0; i < pool.Length; i++)
        {
            var pg = new GameObject("Pool" + i);
            pg.transform.SetParent(WorldBuilder.Dyn);
            pool[i] = pg.AddComponent<Light>();
            pool[i].type = LightType.Point;
            pool[i].color = new Color(1f, 0.55f, 0.22f);
            pool[i].range = theme == RunTheme.Mansion ? 9f : 10f;
            pool[i].intensity = 0;
            pool[i].shadows = LightShadows.None;
        }
        sunBase = sun ? sun.intensity : 0;
        nextBolt = 6f; nextGhost = 5f;

        // lantern carried by the player: warm flickering point light
        var lg = new GameObject("Lantern");
        lantern = lg.AddComponent<Light>();
        lantern.type = LightType.Point;
        lantern.color = new Color(1f, 0.62f, 0.3f);
        lantern.range = theme == RunTheme.Mansion ? 20f : 26f;
        lantern.intensity = theme == RunTheme.Mansion ? 2.4f : 3f;
        lantern.shadows = LightShadows.None;

        // sky dome pieces follow the camera
        if (theme != RunTheme.Mansion)
        {
            sky = new GameObject("Sky").transform;
            sky.SetParent(WorldBuilder.Dyn);
            var stars = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var px = new Color32[1024 * 512];
            var r = new System.Random(5);
            for (int i = 0; i < 1400; i++)
            {
                int x = r.Next(1024), y = r.Next(512);
                byte b = (byte)(120 + r.Next(135));
                px[y * 1024 + x] = new Color32(b, b, (byte)Mathf.Min(255, b + 20), b);
                if (r.NextDouble() < 0.1 && x < 1023) px[y * 1024 + x + 1] = new Color32(b, b, b, (byte)(b / 2));
            }
            stars.SetPixels32(px); stars.Apply();
            var sm = HalloweenWorld.Transparent(stars, new Color(1, 1, 1, theme == RunTheme.PumpkinFarm ? 1f : 0.35f));
            Quad(sky, new Vector3(0, 70, 380), new Vector2(1100, 520), sm, Quaternion.identity);
            if (lib.moon)
                Quad(sky, new Vector3(-60, 95, 360), new Vector2(110, 110), HalloweenWorld.Transparent(lib.moon, new Color(1, 1, 1, theme == RunTheme.PumpkinFarm ? 1f : 0.6f)), Quaternion.identity);
        }

        // rain around the camera
        if (theme == RunTheme.Graveyard)
        {
            var rg = new GameObject("Rain");
            rain = rg.transform; rain.SetParent(WorldBuilder.Dyn);
            var ps = rg.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.startLifetime = 0.7f; main.startSpeed = 0f;
            main.startSize3D = true; main.startSizeX = 0.04f; main.startSizeY = 1.1f; main.startSizeZ = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 4000;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0f); vel.y = new ParticleSystem.MinMaxCurve(-26f); vel.z = new ParticleSystem.MinMaxCurve(0f);
            var em = ps.emission; em.rateOverTime = 3500;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(30, 1, 40);
            var pr = rg.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch; pr.velocityScale = 0.04f; pr.lengthScale = 1f;
            pr.sharedMaterial = HalloweenWorld.Transparent(lib.rain, new Color(0.8f, 0.85f, 1f, 0.55f));
            ps.Play();
        }

        // screen flash for lightning (mansion windows / graveyard sky)
        thunder = gameObject.AddComponent<AudioSource>();
        thunder.clip = Resources.Load<AudioClip>("thunder");
        thunder.volume = 0.55f;

        // bats
        if (lib.bat)
        {
            int n = theme == RunTheme.Mansion ? 6 : 14;
            bats = new Transform[n]; batVel = new Vector3[n];
            var bm = HalloweenWorld.Transparent(lib.bat, Color.white);
            for (int i = 0; i < n; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(WorldBuilder.Dyn);
                q.transform.localScale = new Vector3(0.9f, 0.45f, 1);
                q.GetComponent<Renderer>().sharedMaterial = bm;
                bats[i] = q.transform;
                ResetBat(i, 0);
            }
        }

        // ghost that drifts across the path from time to time
        var gp = lib.Kit("character-ghost");
        if (gp)
        {
            var g = WorldBuilder.Spawn(gp, new Vector3(0, -100, 0), 90, WorldBuilder.Dyn);
            BlockWorld.Fit(g, 2.2f);
            foreach (var rr in g.GetComponentsInChildren<Renderer>())
            {
                var mats = rr.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = HalloweenWorld.Transparent(null, new Color(0.75f, 0.9f, 1f, 0.45f));
                    mats[i] = m;
                }
                rr.materials = mats;
            }
            ghost = g.transform;
        }
    }

    void Quad(Transform p, Vector3 local, Vector2 size, Material m, Quaternion rot)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(p);
        q.transform.localPosition = local;
        q.transform.localRotation = rot;
        q.transform.localScale = new Vector3(size.x, size.y, 1);
        var r = q.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void ResetBat(int i, float camZ)
    {
        float side = Random.value < 0.5f ? -1 : 1;
        bats[i].position = new Vector3(side * Random.Range(3f, 14f), Random.Range(3f, theme == RunTheme.Mansion ? 6.5f : 12f), camZ + Random.Range(25f, 90f));
        batVel[i] = new Vector3(-side * Random.Range(2f, 6f), Random.Range(-0.5f, 1f), Random.Range(-6f, 4f));
    }

    void OnDestroy() { if (lantern) Destroy(lantern.gameObject); if (sun) sun.intensity = sunBase; }

    void LateUpdate()
    {
        if (!cam) return;
        var cp = cam.transform.position;
        float t = Time.time;
        // lantern: lower right, flickering
        lantern.transform.position = cp + cam.transform.up * 0.6f + cam.transform.forward * 2.5f;
        lantern.intensity = (theme == RunTheme.Mansion ? 3.4f : 4f) * (0.88f + 0.12f * Mathf.PerlinNoise(t * 7f, 0.3f));

        // move the pool lights onto the nearest light spots ahead of the camera
        int k = 0;
        foreach (var sp in HalloweenWorld.LightSpots)
        {
            if (k >= pool.Length) break;
            float dz = sp.z - cp.z;
            if (dz < -4f || dz > 45f) continue;
            pool[k].transform.position = sp;
            pool[k].intensity = 3.2f * (0.85f + 0.15f * Mathf.PerlinNoise(t * 9f + k, k));
            k++;
        }
        for (; k < pool.Length; k++) pool[k].intensity = 0;

        if (sky) sky.position = new Vector3(0, 0, cp.z);
        if (rain) rain.position = cp + new Vector3(0, 12f, 14f);

        // lightning
        if (theme != RunTheme.PumpkinFarm && t > nextBolt)
        {
            boltT = 0; nextBolt = t + Random.Range(7f, 13f);
            if (thunder.clip) thunder.PlayDelayed(Random.Range(0.2f, 0.6f));
        }
        if (boltT >= 0 && sun)
        {
            boltT += Time.deltaTime;
            float f = boltT < 0.08f ? 1f : boltT < 0.16f ? 0.2f : boltT < 0.24f ? 0.9f : Mathf.Max(0, 1f - (boltT - 0.24f) * 4f);
            sun.intensity = sunBase + f * (theme == RunTheme.Mansion ? 0.5f : 2.5f);
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, new Color(0.6f, 0.65f, 0.9f), 0);  // keep ambient
            if (boltT > 0.6f) { boltT = -1; sun.intensity = sunBase; }
        }

        // bats flap across
        if (bats != null)
            for (int i = 0; i < bats.Length; i++)
            {
                bats[i].position += batVel[i] * Time.deltaTime;
                bats[i].rotation = Quaternion.LookRotation(bats[i].position - cp) * Quaternion.Euler(0, 0, Mathf.Sin(t * 22f + i) * 18f);
                bats[i].localScale = new Vector3(0.9f, 0.45f * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(t * 22f + i))), 1);
                if (bats[i].position.z < cp.z - 5 || Mathf.Abs(bats[i].position.x) > 30) ResetBat(i, cp.z);
            }

        // ghost glides across the path ahead
        if (ghost)
        {
            if (ghostT < 0 && t > nextGhost) { ghostT = 0; nextGhost = t + Random.Range(9f, 15f); }
            if (ghostT >= 0)
            {
                ghostT += Time.deltaTime;
                float gk = ghostT / 3f, dir = (int)(nextGhost * 10) % 2 == 0 ? 1 : -1;
                ghost.position = new Vector3(Mathf.Lerp(-7, 7, gk) * dir, 0.6f + Mathf.Sin(ghostT * 3f) * 0.3f, cp.z + 30f);
                ghost.rotation = Quaternion.Euler(0, dir > 0 ? 90 : -90, 0);
                if (gk >= 1) { ghostT = -1; ghost.position = new Vector3(0, -100, 0); }
            }
        }
    }
}
