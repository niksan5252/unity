using UnityEngine;

// Builds one level's corridor along +Z: 3 rail lanes, ground, side scenery per theme.
public static class WorldBuilder
{
    public const float LaneGap = 2.6f;
    public static System.Collections.Generic.List<RunCue> Cues = new System.Collections.Generic.List<RunCue>();
    public static float Speed = 16f;
    static readonly float[] Lanes = { -LaneGap, 0f, LaneGap };

    public static Transform Build(RunLibrary lib, RunTheme theme, float length, int seed)
    {
        var root = new GameObject("World_" + theme).transform;
        var rnd = new System.Random(seed);
        ApplyAtmosphere(theme);

        if (theme >= RunTheme.SkyObby) { ObbyWorld.Build(lib, theme, root, length, rnd); StaticBatchingUtility.Combine(root.gameObject); return root; }
        bool blocks = theme >= RunTheme.Village;
        if (!blocks)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            Object.Destroy(ground.GetComponent<Collider>());
            ground.transform.SetParent(root);
            ground.transform.localScale = new Vector3(60f, 0.2f, length + 200f);
            ground.transform.position = new Vector3(0, -0.12f, length * 0.5f);
            ground.GetComponent<Renderer>().sharedMaterial = Mat(theme switch
            {
                RunTheme.Day => new Color(0.42f, 0.4f, 0.4f),
                RunTheme.Tunnel => new Color(0.18f, 0.17f, 0.2f),
                _ => new Color(0.08f, 0.05f, 0.14f),
            });
        }

        // Rails (long axis of the model goes along Z)
        if (lib.track)
        {
            var sz = Measure(lib.track, out _);
            bool rot = sz.x > sz.z;
            float tl = Mathf.Max(0.5f, rot ? sz.x : sz.z), tw = rot ? sz.z : sz.x;
            float k = blocks && tw > 0.01f ? 1.8f / tw : 1f;
            tl *= k;
            foreach (float x in Lanes)
                for (float z = -20f; z < length + 60f; z += tl)
                    Spawn(lib.track, new Vector3(x, 0.02f, z), rot ? 90 : 0, root).transform.localScale *= k;
        }

        if (blocks) { BlockWorld.Build(lib, theme, root, length, rnd); StaticBatchingUtility.Combine(root.gameObject); return root; }

        switch (theme)
        {
            case RunTheme.Day: BuildDay(lib, root, length, rnd); break;
            case RunTheme.Tunnel: BuildTunnel(lib, root, length, rnd); break;
            default: BuildNeon(lib, root, length, rnd); break;
        }
        StaticBatchingUtility.Combine(root.gameObject);
        return root;
    }

    // continuous street wall of buildings, rotated so the long side faces the tracks
    static void BuildingRow(GameObject[] pool, Transform root, float length, System.Random rnd, int side, float x0, float scale, System.Action<GameObject> post = null)
    {
        float z = -30f;
        while (z < length + 140f)
        {
            var p = Pick(pool, rnd);
            if (!p) break;
            var go = Spawn(p, Vector3.zero, side > 0 ? -90 : 90, root);
            go.transform.localScale *= scale * (0.9f + (float)rnd.NextDouble() * 0.3f);
            var b = Bounds(go);
            float along = b.size.z, depth = b.size.x;
            go.transform.position = new Vector3(side * (x0 + depth * 0.5f) - (b.center.x - go.transform.position.x),
                -b.min.y, z + along * 0.5f - (b.center.z - go.transform.position.z));
            post?.Invoke(go);
            z += along + 0.2f;
        }
    }

    static void Edges(Transform root, float length, Material walk, Material wall, Material stripe, float wallH)
    {
        float half = length * 0.5f + 60f;
        foreach (int side in new[] { -1, 1 })
        {
            Box(root, new Vector3(side * 6.2f, 0.35f, half), new Vector3(3.2f, 0.7f, length + 160f), walk);
            Box(root, new Vector3(side * 4.55f, wallH * 0.5f, half), new Vector3(0.25f, wallH, length + 160f), wall);
            Box(root, new Vector3(side * 4.42f, wallH - 0.15f, half), new Vector3(0.02f, 0.1f, length + 160f), stripe);
        }
    }

    // spray tags on the inner faces of the corridor walls
    static Material[] graffiti;
    static void Graffiti(Transform root, float length, System.Random rnd, float x, float yMin, float yMax, float glow)
    {
        if (graffiti == null || graffiti.Length == 0 || graffiti[0] == null)
        {
            var list = new System.Collections.Generic.List<Material>();
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            foreach (var tex in Resources.LoadAll<Texture2D>("Graffiti"))
            {
                var m = new Material(unlit);
                m.SetTexture("_BaseMap", tex);
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                list.Add(m);
            }
            graffiti = list.ToArray();
        }
        if (graffiti.Length == 0) return;
        foreach (int side in new[] { -1, 1 })
            for (float z = 6f + (float)rnd.NextDouble() * 6f; z < length + 100f; z += 9f + (float)rnd.NextDouble() * 10f)
            {
                float h = yMax - yMin;
                float w = h * 2f * (0.85f + (float)rnd.NextDouble() * 0.3f);
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(root);
                q.transform.position = new Vector3(side * x, yMin + h * 0.5f, z);
                q.transform.rotation = Quaternion.Euler(0, side < 0 ? -90 : 90, (float)(rnd.NextDouble() * 8 - 4));
                q.transform.localScale = new Vector3(w, w * 0.5f, 1);
                var mat = graffiti[rnd.Next(graffiti.Length)];
                if (glow < 0.5f) { mat = new Material(mat); mat.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.9f, 1f)); }
                q.GetComponent<Renderer>().sharedMaterial = mat;
            }
    }

    static void BuildDay(RunLibrary lib, Transform root, float length, System.Random rnd)
    {
        Edges(root, length, Mat(new Color(0.78f, 0.76f, 0.72f)), Mat(new Color(0.62f, 0.6f, 0.58f)), Mat(new Color(1f, 0.8f, 0.1f)), 2.6f);
        Graffiti(root, length, rnd, 4.41f, 0.25f, 2.4f, 0.15f);
        foreach (int side in new[] { -1, 1 })
        {
            BuildingRow(lib.commercial.Length > 0 ? lib.commercial : lib.suburban, root, length, rnd, side, 8f, 5.5f);
            for (float pz = 0; pz < length + 100f; pz += 16f)
            {
                if (lib.pole) Spawn(lib.pole, new Vector3(side * 5.6f, 0.7f, pz), side > 0 ? 180 : 0, root).transform.localScale *= 2.2f;
                var t = Pick(lib.trees, rnd);
                if (t) Spawn(t, new Vector3(side * 6.9f, 0.7f, pz + 8f), (float)rnd.NextDouble() * 360f, root).transform.localScale *= 3.2f;
            }
        }
        // skyline far behind
        foreach (int side in new[] { -1, 1 })
            BuildingRow(lib.skyscrapers, root, length, rnd, side, 30f, 7f);
    }

    static void BuildTunnel(RunLibrary lib, Transform root, float length, System.Random rnd)
    {
        var wall = Mat(new Color(0.22f, 0.22f, 0.26f));
        var stripe = Emissive(new Color(1f, 0.55f, 0.1f), 3f);
        var lamp = Emissive(new Color(0.9f, 0.95f, 1f), 4f);
        float half = length * 0.5f + 60f;
        Box(root, new Vector3(-6.5f, 3f, half), new Vector3(1f, 6f, length + 160f), wall);
        Box(root, new Vector3(6.5f, 3f, half), new Vector3(1f, 6f, length + 160f), wall);
        Box(root, new Vector3(0, 6.2f, half), new Vector3(14f, 0.6f, length + 160f), wall);
        Box(root, new Vector3(-5.95f, 1.2f, half), new Vector3(0.1f, 0.25f, length + 160f), stripe);
        Box(root, new Vector3(5.95f, 1.2f, half), new Vector3(0.1f, 0.25f, length + 160f), stripe);
        Graffiti(root, length, rnd, 5.98f, 0.8f, 3.6f, 0.6f);
        for (float z = 0; z < length + 120f; z += 10f)
        {
            Box(root, new Vector3(0, 5.85f, z), new Vector3(4f, 0.12f, 0.8f), lamp);
            if (lib.pillar && ((int)(z / 10f)) % 2 == 0)
            {
                Spawn(lib.pillar, new Vector3(-5.3f, 0, z), 0, root);
                Spawn(lib.pillar, new Vector3(5.3f, 0, z), 0, root);
            }
        }
    }

    static void BuildNeon(RunLibrary lib, Transform root, float length, System.Random rnd)
    {
        Color[] neon = { new Color(1f, 0.2f, 0.8f), new Color(0.2f, 0.9f, 1f), new Color(0.6f, 0.3f, 1f), new Color(1f, 0.85f, 0.2f) };
        Edges(root, length, Mat(new Color(0.12f, 0.1f, 0.18f)), Mat(new Color(0.1f, 0.08f, 0.15f)), Emissive(new Color(1f, 0.2f, 0.8f), 4f), 2.2f);
        Graffiti(root, length, rnd, 4.41f, 0.2f, 2.0f, 1.2f);
        foreach (int side in new[] { -1, 1 })
        {
            BuildingRow(lib.skyscrapers, root, length, rnd, side, 8.5f, 6f, go => Tint(go, neon[rnd.Next(neon.Length)]));
            var strip = Emissive(neon[side > 0 ? 0 : 1], 5f);
            Box(root, new Vector3(side * 4.6f, 0.05f, length * 0.5f + 60f), new Vector3(0.15f, 0.08f, length + 160f), strip);
            for (float pz = 0; pz < length + 100f; pz += 14f)
                Box(root, new Vector3(side * 5.2f, 2.2f, pz), new Vector3(0.12f, 4.4f, 0.12f), Emissive(neon[rnd.Next(neon.Length)], 4f));
        }
    }

    static void ApplyAtmosphere(RunTheme theme)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        bool closed = theme == RunTheme.Tunnel || theme == RunTheme.Mine;
        RenderSettings.fogStartDistance = closed ? 25f : 60f;
        RenderSettings.fogEndDistance = closed ? 120f : 220f;
        Color sky = theme switch
        {
            RunTheme.Day => new Color(0.55f, 0.78f, 1f),
            RunTheme.Village => new Color(0.5f, 0.75f, 1f),
            RunTheme.Tunnel => new Color(0.05f, 0.05f, 0.07f),
            RunTheme.Mine => new Color(0.06f, 0.05f, 0.05f),
            RunTheme.Nether => new Color(0.35f, 0.06f, 0.04f),
            RunTheme.SkyObby => new Color(0.45f, 0.75f, 1f),
            RunTheme.LavaObby => new Color(1f, 0.45f, 0.2f),
            RunTheme.RainbowObby => new Color(0.75f, 0.6f, 1f),
            _ => new Color(0.12f, 0.03f, 0.2f),
        };
        RenderSettings.fogColor = sky;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = theme switch
        {
            RunTheme.Day => new Color(0.75f, 0.75f, 0.8f),
            RunTheme.Village => new Color(0.6f, 0.64f, 0.7f),
            RunTheme.Tunnel => new Color(0.45f, 0.42f, 0.5f),
            RunTheme.Mine => new Color(0.5f, 0.42f, 0.36f),
            RunTheme.Nether => new Color(0.7f, 0.4f, 0.32f),
            RunTheme.SkyObby or RunTheme.RainbowObby => new Color(0.7f, 0.72f, 0.8f),
            RunTheme.LavaObby => new Color(0.75f, 0.5f, 0.4f),
            _ => new Color(0.45f, 0.3f, 0.6f),
        };
        var cam = Camera.main;
        if (cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = sky; }
        var sun = Object.FindAnyObjectByType<Light>();
        if (sun && sun.type == LightType.Directional)
        {
            sun.intensity = theme is RunTheme.Day or RunTheme.Village ? 1.4f : theme is RunTheme.Tunnel or RunTheme.Mine ? 0.25f : theme == RunTheme.Nether ? 0.6f : 0.35f;
            sun.color = theme == RunTheme.Neon ? new Color(0.7f, 0.5f, 1f) : theme == RunTheme.Nether ? new Color(1f, 0.5f, 0.3f) : new Color(1f, 0.95f, 0.85f);
        }
    }

    // ---------- helpers ----------
    // models whose imported materials came without a texture get this one (Cube World atlas)
    public static Material AtlasFallback;

    public static GameObject Spawn(GameObject prefab, Vector3 pos, float rotY, Transform parent)
    {
        // keep the model's own root rotation (Blender exports carry a -90 X fix)
        var go = Object.Instantiate(prefab, pos, Quaternion.Euler(0, rotY, 0) * prefab.transform.rotation, parent);
        foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        if (AtlasFallback)
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var ms = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < ms.Length; i++)
                    if (ms[i] && !ms[i].mainTexture) { ms[i] = AtlasFallback; changed = true; }
                if (changed) r.sharedMaterials = ms;
            }
        return go;
    }

    public static Vector3 Measure(GameObject prefab, out bool rotated)
    {
        var tmp = Object.Instantiate(prefab);
        var b = Bounds(tmp);
        Object.DestroyImmediate(tmp);
        rotated = false;
        return b.size;
    }

    public static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static GameObject Pick(GameObject[] pool, System.Random rnd) =>
        pool == null || pool.Length == 0 ? null : pool[rnd.Next(pool.Length)];

    public static GameObject Box(Transform parent, Vector3 pos, Vector3 size, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    static Shader lit;
    public static Material Mat(Color c)
    {
        lit ??= Shader.Find("Universal Render Pipeline/Lit");
        var m = new Material(lit);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.2f);
        return m;
    }

    public static Material Emissive(Color c, float intensity)
    {
        var m = Mat(c);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * intensity);
        return m;
    }

    static void Tint(GameObject go, Color neon)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = r.materials;
            foreach (var m in mats)
            {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap")) m.SetTexture("_EmissionMap", m.GetTexture("_BaseMap"));
                m.SetColor("_EmissionColor", neon * 0.9f);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.Lerp(Color.white, neon, 0.3f) * 0.6f);
            }
            r.materials = mats;
        }
    }
}
