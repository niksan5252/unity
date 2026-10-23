using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Halloween Quest atmosphere: sky dome, beat-pulsing lights, fireworks, ambient particles
// (confetti / fireflies / embers + ghost wisps), floating balloons / candles / bats, orbiting ghosts,
// witches and dragons, lightning, and fly-bys that swoop right over the camera in the gaps between cues.
public class SpookyFx : MonoBehaviour
{
    RunTheme theme;
    RunLibrary lib;
    Camera cam;
    RunDirector d;
    SpookyWorld.Palette pal;
    Transform sky;
    Light sun; float sunBase;
    Light bolt; AudioSource thunder; float nextBolt = 9f, boltT = -1f;
    int lastBar = -1;

    readonly List<Transform> culled = new List<Transform>();
    float cullT;
    readonly List<(Transform t, Vector3 off)> follow = new List<(Transform, Vector3)>();
    readonly List<Transform> beams = new List<Transform>();

    Transform[] floaters; Vector3[] floatVel;

    // fly-bys
    readonly List<float> flyTimes = new List<float>();
    int flyNext, flyCount;
    float flyT = -1f, flySide = 1f;
    Transform flyer; ParticleSystem flyTrail;
    Transform[] swarm; Vector3[] swarmOff; float[] swarmLag;
    readonly List<Transform> pool = new List<Transform>();   // pre-built flyers, one per kind
    int kind;
    const float FlyDur = 2.5f;
    AudioClip whoosh, roar;

    public void Setup(RunLibrary l, RunTheme t, float length)
    {
        lib = l; theme = t; cam = Camera.main; d = FindAnyObjectByType<RunDirector>();
        pal = SpookyWorld.Pal(t);
        foreach (var dl in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (dl.type == LightType.Directional) sun = dl;
        sunBase = sun ? sun.intensity : 0;
        whoosh = Resources.Load<AudioClip>("Voice/whoosh_sfx");
        roar = Resources.Load<AudioClip>("Voice/roar_sfx");

        // everything the world put in Dyn gets switched off while far from the camera
        foreach (Transform c in WorldBuilder.Dyn) culled.Add(c);

        BuildSky();
        var spark = SpookyWorld.T(lib, "star_06") ? SpookyWorld.T(lib, "star_06") : lib.spark;
        switch (t)
        {
            case RunTheme.SpookyCarnival:
                Ambient("Confetti", null, false, SpookyWorld.Party, 0.07f, 0.14f, 70, new Vector3(26, 2, 40), new Vector3(0, 9, 22), new Vector2(-1.6f, -0.8f), 6f, 0.8f, true);
                Ambient("Glitter", spark, true, new[] { SpookyWorld.Gold, Color.white }, 0.08f, 0.2f, 60, new Vector3(30, 8, 45), new Vector3(0, 4, 24), new Vector2(-0.2f, 0.3f), 3f, 0.5f, false);
                Balloons(26);
                OrbitGhosts(length);
                Searchlights(new Color(0.55f, 0.3f, 0.8f), 4);
                break;
            case RunTheme.WitchForest:
                Ambient("Fireflies", lib.spark, true, new[] { new Color(0.75f, 1f, 0.3f), new Color(1f, 0.9f, 0.4f), SpookyWorld.Teal }, 0.07f, 0.17f, 150, new Vector3(30, 7, 46), new Vector3(0, 2.6f, 22), new Vector2(-0.3f, 0.4f), 4.5f, 0.9f, false);
                Ambient("MagicDust", spark, true, new[] { SpookyWorld.Purple, SpookyWorld.Pink }, 0.05f, 0.12f, 50, new Vector3(20, 4, 30), new Vector3(0, 1.5f, 14), new Vector2(0.1f, 0.6f), 3f, 0.4f, false);
                Candles(40);
                OrbitWitches(length);
                Cauldrons();
                break;
            default:
                Ambient("Embers", lib.spark, true, new[] { SpookyWorld.Orange, new Color(1f, 0.3f, 0.1f), SpookyWorld.Gold }, 0.05f, 0.13f, 110, new Vector3(30, 2, 44), new Vector3(0, -1, 22), new Vector2(1.2f, 2.8f), 4f, 0.6f, false);
                Ambient("Souls", lib.smoke, true, new[] { new Color(0.2f, 0.9f, 1f) * 0.35f, new Color(0.6f, 0.4f, 1f) * 0.35f }, 0.6f, 1.4f, 26, new Vector3(80, 2, 90), new Vector3(0, -8, 45), new Vector2(2f, 4f), 6f, 0.3f, false);
                Bats(16);
                OrbitDragons(length);
                Searchlights(new Color(0.5f, 0.25f, 0.35f), 2);
                var bg = new GameObject("Lightning"); bg.transform.SetParent(WorldBuilder.Dyn);
                bolt = bg.AddComponent<Light>(); bolt.type = LightType.Point; bolt.range = 90f; bolt.intensity = 0; bolt.shadows = LightShadows.None;
                bolt.color = new Color(0.75f, 0.65f, 1f);
                thunder = gameObject.AddComponent<AudioSource>();
                thunder.clip = Resources.Load<AudioClip>("thunder"); thunder.volume = 0.5f;
                break;
        }
        BuildFlyers();
        Schedule();
    }

    void OnDestroy() { if (sun) sun.intensity = sunBase; }

    // ---------------- sky ----------------
    void BuildSky()
    {
        if (!lib.skyShader || !lib.skyShader.isSupported) return;   // no magenta dome: fall back to the plain horizon colour
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(g.GetComponent<Collider>());
        g.name = "SkyDome";
        g.transform.SetParent(WorldBuilder.Dyn);
        g.transform.localScale = Vector3.one * 700f;   // radius 350 < camera far plane 400
        var m = new Material(lib.skyShader);
        m.SetColor("_Top", pal.top); m.SetColor("_Mid", pal.mid); m.SetColor("_Horizon", pal.horizon);
        m.SetVector("_MoonDir", pal.moonDir); m.SetColor("_MoonColor", pal.moon); m.SetFloat("_MoonSize", pal.moonSize);
        m.SetFloat("_Clouds", pal.clouds); m.SetColor("_CloudColor", pal.cloud);
        m.SetFloat("_Aurora", pal.aurora);
        m.SetFloat("_Stars", theme == RunTheme.MoonCastle ? 0.7f : 1f);
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        sky = g.transform;
    }

    // ---------------- particles ----------------
    // alpha-blended (not additive: additive quads pick up the fog colour and show as boxes far away);
    // "glow" particles get an HDR tint instead so the bloom still lights them up
    static Material PMat(Texture2D tex, bool glow)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        if (tex) m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", glow ? new Color(2.6f, 2.6f, 2.6f, 1f) : Color.white);
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        return m;
    }

    static Gradient Fade(bool glow)
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    // a box of particles that rides along with the camera
    ParticleSystem Ambient(string name, Texture2D tex, bool additive, Color[] colors, float sMin, float sMax, int rate, Vector3 box, Vector3 offset, Vector2 rise, float life, float noise, bool tumble)
    {
        var g = new GameObject(name); g.transform.SetParent(WorldBuilder.Dyn);
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.startLifetime = life; main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(sMin, sMax);
        main.startColor = new ParticleSystem.MinMaxGradient(colors[0], colors[colors.Length > 1 ? 1 : 0]);
        if (colors.Length > 2)
        {
            var gr = new Gradient();
            var keys = new GradientColorKey[Mathf.Min(8, colors.Length)];
            for (int i = 0; i < keys.Length; i++) keys[i] = new GradientColorKey(colors[i], i / (float)Mathf.Max(1, keys.Length - 1));
            gr.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            gr.mode = GradientMode.Fixed;
            main.startColor = new ParticleSystem.MinMaxGradient(gr) { mode = ParticleSystemGradientMode.RandomColor };
        }
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 2000;
        if (tumble) { main.startRotation3D = false; main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); }
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f); vel.y = new ParticleSystem.MinMaxCurve(rise.x, rise.y); vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        if (noise > 0) { var n = ps.noise; n.enabled = true; n.strength = noise; n.frequency = 0.6f; n.scrollSpeed = 0.4f; }
        if (tumble) { var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f); }
        var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Fade(additive));
        var pr = g.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = PMat(tex, additive);
        pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
        follow.Add((g.transform, offset));
        return ps;
    }

    void Cauldrons()
    {
        var bubbleMat = PMat(lib.spark, true);
        var smokeMat = PMat(lib.smoke, true);
        foreach (var (pos, c) in SpookyWorld.Cauldrons)
        {
            var g = new GameObject("CauldronFx"); g.transform.SetParent(WorldBuilder.Dyn); g.transform.position = pos;
            var ps = g.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f); main.startColor = new ParticleSystem.MinMaxGradient(c, Color.Lerp(c, Color.white, 0.5f));
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 200;
            var em = ps.emission; em.rateOverTime = 22;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.6f; sh.rotation = new Vector3(-90, 0, 0);   // cone axis +Z -> up
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Fade(true));
            var pr = g.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = bubbleMat;
            ps.Play();

            // coloured smoke column
            var sg = new GameObject("CauldronSmoke"); sg.transform.SetParent(g.transform, false);
            var sp = sg.AddComponent<ParticleSystem>();
            sp.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var sm = sp.main; sm.loop = true; sm.startLifetime = 3f; sm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
            sm.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.4f); sm.startColor = c * 0.45f;
            sm.simulationSpace = ParticleSystemSimulationSpace.World; sm.maxParticles = 60;
            sm.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var se = sp.emission; se.rateOverTime = 7;
            var ss = sp.shape; ss.shapeType = ParticleSystemShapeType.Cone; ss.angle = 8f; ss.radius = 0.4f; ss.rotation = new Vector3(-90, 0, 0);
            var sc = sp.colorOverLifetime; sc.enabled = true; sc.color = new ParticleSystem.MinMaxGradient(Fade(true));
            var sz = sp.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.6f));
            sg.GetComponent<ParticleSystemRenderer>().sharedMaterial = smokeMat;
            sp.Play();
            culled.Add(g.transform);
        }
    }

    // ---------------- floating things ----------------
    void Balloons(int n)
    {
        floaters = new Transform[n]; floatVel = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            var b = new GameObject("Balloon").transform; b.SetParent(WorldBuilder.Dyn);
            var c = SpookyWorld.Party[i % SpookyWorld.Party.Length];
            SpookyWorld.Ball(b, Vector3.zero, new Vector3(0.9f, 1.1f, 0.9f), SpookyWorld.Lit(c, 0.9f), false);
            SpookyWorld.Box(b, new Vector3(0, -1.2f, 0), new Vector3(0.02f, 1.4f, 0.02f), SpookyWorld.Lit(Color.white), false);
            floaters[i] = b;
            Respawn(i, 0, true);
        }
    }

    void Candles(int n)
    {
        floaters = new Transform[n]; floatVel = new Vector3[n];
        var wax = SpookyWorld.Lit(new Color(1f, 0.95f, 0.85f), 0.4f);
        var flame = SpookyWorld.Glow(new Color(1f, 0.7f, 0.3f), 7f, true);
        for (int i = 0; i < n; i++)
        {
            var b = new GameObject("FloatingCandle").transform; b.SetParent(WorldBuilder.Dyn);
            SpookyWorld.Cyl(b, Vector3.zero, 0.14f, 0.4f, wax, null, false);
            SpookyWorld.Ball(b, new Vector3(0, 0.3f, 0), new Vector3(0.1f, 0.17f, 0.1f), flame, false);
            floaters[i] = b;
            Respawn(i, 0, true);
        }
    }

    Material batMat;
    void Bats(int n)
    {
        if (!lib.bat) return;
        floaters = new Transform[n]; floatVel = new Vector3[n];
        batMat = HalloweenWorld.Transparent(lib.bat, Color.white);
        for (int i = 0; i < n; i++)
        {
            floaters[i] = SpookyWorld.Quad(WorldBuilder.Dyn, Vector3.zero, Quaternion.identity, new Vector2(1.1f, 0.55f), batMat).transform;
            Respawn(i, 0, true);
        }
    }

    void Respawn(int i, float camZ, bool first)
    {
        float side = Random.value < 0.5f ? -1 : 1;
        var f = floaters[i];
        switch (theme)
        {
            case RunTheme.SpookyCarnival:
                f.position = new Vector3(side * Random.Range(5.8f, 26f), first ? Random.Range(0f, 22f) : Random.Range(-1f, 3f), camZ + Random.Range(first ? 0f : 30f, 130f));
                floatVel[i] = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(1.3f, 2.4f), 0);
                break;
            case RunTheme.WitchForest:
                f.position = new Vector3(side * Random.Range(4.9f, 18f), Random.Range(2.2f, 7f), camZ + Random.Range(first ? 0f : 40f, 140f));
                floatVel[i] = new Vector3(0, Random.Range(0.15f, 0.45f), 0);
                break;
            default:
                f.position = new Vector3(side * Random.Range(4f, 18f), Random.Range(4f, 14f), camZ + Random.Range(25f, 100f));
                floatVel[i] = new Vector3(-side * Random.Range(2f, 6f), Random.Range(-0.5f, 1f), Random.Range(-6f, 4f));
                break;
        }
    }

    void UpdateFloaters(Vector3 cp, float t)
    {
        if (floaters == null) return;
        for (int i = 0; i < floaters.Length; i++)
        {
            var f = floaters[i];
            if (!f) continue;
            f.position += floatVel[i] * Time.deltaTime;
            if (theme == RunTheme.MoonCastle)
            {
                // bats: billboard + wing flap
                f.rotation = Quaternion.LookRotation(f.position - cp) * Quaternion.Euler(0, 0, Mathf.Sin(t * 20f + i) * 15f);
                f.localScale = new Vector3(1.1f, 0.55f * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 20f + i))), 1);
                if (f.position.z < cp.z - 5 || Mathf.Abs(f.position.x) > 34) Respawn(i, cp.z, false);
            }
            else
            {
                f.position += new Vector3(Mathf.Sin(t * 0.9f + i) * 0.3f, Mathf.Sin(t * 1.7f + i * 2f) * (theme == RunTheme.WitchForest ? 0.25f : 0f), 0) * Time.deltaTime;
                f.rotation = Quaternion.Euler(Mathf.Sin(t + i) * 8f, 0, Mathf.Cos(t * 0.8f + i) * 8f);
                if (f.position.z < cp.z - 8 || f.position.y > 30) Respawn(i, cp.z, false);
            }
        }
    }

    // ---------------- orbiting ghosts / witches / dragons ----------------
    void OrbitGhosts(float length)
    {
        for (float z = 40; z < length + 120; z += 55f)
        {
            float s = Random.value < 0.5f ? -1 : 1;
            var g = SpookyWorld.Critter(lib, Random.value < 0.7f ? "Ghost" : "Ghost_Skull", new Vector3(0, 0, z), 0, Random.Range(1.6f, 2.4f), WorldBuilder.Dyn, "flying_idle", "fast_flying");
            if (!g) return;
            SpookyWorld.Spookify(g, new Color(0.7f, 0.9f, 1f), 0.6f);
            var o = g.AddComponent<Orbit>();
            o.center = new Vector3(s * Random.Range(10f, 16f), Random.Range(7f, 12f), z); o.radius = Random.Range(4f, 7f);
            o.speed = Random.Range(0.6f, 0.9f) * (Random.value < 0.5f ? -1 : 1); o.phase = Random.value * 6f; o.wobble = 1.5f;
            culled.Add(g.transform);
        }
    }

    void OrbitWitches(float length)
    {
        for (float z = 50; z < length + 150; z += 85f)
        {
            float s = Random.value < 0.5f ? -1 : 1;
            var w = Witch(WorldBuilder.Dyn, 1.3f);
            var o = w.gameObject.AddComponent<Orbit>();
            o.center = new Vector3(s * Random.Range(16f, 26f), Random.Range(11f, 18f), z); o.radius = Random.Range(10f, 16f);
            o.speed = Random.Range(0.45f, 0.7f) * (Random.value < 0.5f ? -1 : 1); o.phase = Random.value * 6f; o.wobble = 2.5f;
            Trail(w, new Color(0.7f, 1f, 0.3f), SpookyWorld.Purple, 10f);
            culled.Add(w);
        }
    }

    void OrbitDragons(float length)
    {
        for (float z = 70; z < length + 200; z += 120f)
        {
            float s = Random.value < 0.5f ? -1 : 1;
            var g = SpookyWorld.Critter(lib, "Dragon", new Vector3(0, 0, z), 0, Random.Range(6f, 9f), WorldBuilder.Dyn, "fast_flying", "flying_idle");
            if (!g) return;
            SpookyWorld.Spookify(g, SpookyWorld.Purple, 0.25f);
            var o = g.AddComponent<Orbit>();
            o.center = new Vector3(s * Random.Range(32f, 48f), Random.Range(22f, 32f), z); o.radius = Random.Range(22f, 32f);
            o.speed = Random.Range(0.22f, 0.32f) * (Random.value < 0.5f ? -1 : 1); o.phase = Random.value * 6f; o.wobble = 4f;
            culled.Add(g.transform);
        }
    }

    // a cartoon witch on a broom built from primitives, facing +Z
    public static Transform Witch(Transform parent, float scale)
    {
        var w = new GameObject("Witch").transform;
        w.SetParent(parent);
        var wood = SpookyWorld.Lit(new Color(0.45f, 0.28f, 0.15f));
        var dress = SpookyWorld.Lit(new Color(0.42f, 0.12f, 0.7f), 0.3f);
        var skin = SpookyWorld.Lit(new Color(0.6f, 0.9f, 0.45f), 0.3f);
        var hatM = SpookyWorld.Lit(new Color(0.12f, 0.05f, 0.2f), 0.3f);
        SpookyWorld.Cyl(w, Vector3.zero, 0.09f, 2.4f, wood, Quaternion.Euler(90, 0, 0), false);
        var br = SpookyWorld.ConeObj(w, new Vector3(0, 0, -1.05f), 0.55f, 0.75f, SpookyWorld.Lit(new Color(0.95f, 0.75f, 0.3f)), 10, 0, false);
        br.transform.rotation = Quaternion.Euler(90, 0, 0);
        SpookyWorld.ConeObj(w, new Vector3(0, 0.02f, -0.15f), 0.75f, 0.95f, dress, 12, 0, false);
        SpookyWorld.Ball(w, new Vector3(0, 1.08f, -0.1f), Vector3.one * 0.42f, skin, false);
        SpookyWorld.Box(w, new Vector3(0, 1.0f, -0.32f), new Vector3(0.5f, 0.45f, 0.1f), SpookyWorld.Lit(SpookyWorld.Orange), false);   // hair
        SpookyWorld.Cyl(w, new Vector3(0, 1.26f, -0.1f), 0.85f, 0.03f, hatM, null, false);
        var tip = SpookyWorld.ConeObj(w, new Vector3(0, 1.27f, -0.1f), 0.42f, 0.8f, hatM, 12, 0, false);
        tip.transform.rotation = Quaternion.Euler(-25, 0, 0);
        SpookyWorld.Cyl(w, new Vector3(0, 1.33f, -0.1f), 0.44f, 0.08f, SpookyWorld.Glow(SpookyWorld.Lime, 1.5f), null, false);
        foreach (int s in new[] { -1, 1 })
        {
            var arm = SpookyWorld.Box(w, new Vector3(s * 0.2f, 0.62f, 0.25f), new Vector3(0.12f, 0.12f, 0.6f), dress, false);
            arm.transform.rotation = Quaternion.Euler(30, 0, 0);
        }
        var cape = SpookyWorld.Box(w, new Vector3(0, 0.6f, -0.5f), new Vector3(0.7f, 0.9f, 0.05f), SpookyWorld.Lit(new Color(0.85f, 0.2f, 0.25f)), false);
        cape.transform.rotation = Quaternion.Euler(-35, 0, 0);
        w.localScale = Vector3.one * scale;
        return w;
    }

    Material sparkMat;
    Material SparkMat() => sparkMat ? sparkMat : (sparkMat = PMat(lib.spark, true));

    ParticleSystem Trail(Transform t, Color a, Color b, float perMetre)
    {
        var g = new GameObject("Trail"); g.transform.SetParent(t, false); g.transform.localPosition = new Vector3(0, 0, -1.2f);
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.3f); main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f); main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 500; main.gravityModifier = 0.15f;
        var em = ps.emission; em.rateOverTime = 0; em.rateOverDistance = perMetre;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.25f;
        var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Fade(true));
        g.GetComponent<ParticleSystemRenderer>().sharedMaterial = SparkMat();
        ps.Play();
        return ps;
    }

    // ---------------- searchlights ----------------
    void Searchlights(Color c, int n)
    {
        var mat = PMat(null, false);
        mat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, 0.13f));
        for (int i = 0; i < n * 2; i++)
        {
            var pivot = new GameObject("Searchlight").transform; pivot.SetParent(WorldBuilder.Dyn);
            var beam = SpookyWorld.ConeObj(pivot, Vector3.zero, 7f, 120f, mat, 16, 0, false);
            beam.transform.localPosition = new Vector3(0, 120f, 0);
            beam.transform.localRotation = Quaternion.Euler(180, 0, 0);   // apex at the lamp, wide end in the sky
            beams.Add(pivot);
        }
    }

    // ---------------- fireworks ----------------
    void Firework(Vector3 at, Color c)
    {
        var rocket = new GameObject("Rocket"); rocket.transform.SetParent(WorldBuilder.Dyn);
        rocket.transform.position = at + new Vector3(Random.Range(-3f, 3f), -26f, 0);
        var trail = Trail(rocket.transform, Color.Lerp(c, Color.white, 0.6f), SpookyWorld.Gold, 3f);
        trail.transform.localPosition = Vector3.zero;
        rocket.transform.DOMove(at, 0.9f).SetEase(Ease.OutQuad).SetLink(rocket).OnComplete(() =>
        {
            Burst(at, c);
            trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(rocket, 1.5f);
        });
    }

    void Burst(Vector3 at, Color c)
    {
        var g = new GameObject("FireworkBurst"); g.transform.SetParent(WorldBuilder.Dyn); g.transform.position = at;
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.duration = 0.3f; main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.3f, 2f); main.startSpeed = new ParticleSystem.MinMaxCurve(9f, 14f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.95f);
        main.startColor = new ParticleSystem.MinMaxGradient(c, Color.Lerp(c, Color.white, 0.5f));
        main.gravityModifier = 0.35f; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)170) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
        var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 3.5f; lim.dampen = 0.09f;
        var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Fade(true));
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.3f));
        g.GetComponent<ParticleSystemRenderer>().sharedMaterial = SparkMat();
        ps.Play();
        // the burst lights up the scenery for a moment
        var lg = new GameObject("FireworkLight"); lg.transform.SetParent(g.transform, false);
        var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 75f; l.color = c; l.intensity = 5f; l.shadows = LightShadows.None;
        DOTween.To(() => l.intensity, v => l.intensity = v, 0f, 1.1f).SetLink(lg);
    }

    // ---------------- fly-bys ----------------
    // camera-relative path: far ahead and to one side -> past the camera just over its head -> behind
    Vector3 Bez(float u, float side, bool big)
    {
        var p0 = new Vector3(side * 16f, big ? 13f : 10f, 48f);
        var p1 = new Vector3(side * 3.5f, big ? 8f : 5.2f, 12f);
        var p2 = new Vector3(-side * 4f, big ? 7f : 3.6f, -8f);
        float a = 1 - u;
        return a * a * p0 + 2 * a * u * p1 + u * u * p2;
    }

    void Schedule()
    {
        var cues = d ? d.CurrentCues : null;
        if (cues == null || cues.Count == 0) return;
        // RunLookBack picks its turn the same way: keep fly-bys away from it
        float look = -1;
        if (d.CurrentLevel == 0 || d.CurrentLevel == 2)
            for (float t = 18f; t < 55f; t += 0.25f)
            {
                bool clear = true;
                foreach (var c in cues) if (c.time > t - 1.4f && c.time < t + 3.2f) { clear = false; break; }
                if (clear) { look = t; break; }
            }
        float last = -99f, end = cues[cues.Count - 1].time + 3f;
        for (float t = cues[0].time - 0.5f; t < end; t += 0.25f)
        {
            if (t - last < 6.5f) continue;
            bool clear = true;
            foreach (var c in cues) if (c.time > t - 0.9f && c.time < t + 2.7f) { clear = false; break; }
            if (look > 0 && t > look - 3f && t < look + 4.5f) clear = false;
            if (clear) { flyTimes.Add(t); last = t; }
        }
    }

    // kinds per world: 0 ghost, 1 ghost trio, 2 skull ghosts, 3 witch, 4 firefly swarm, 5 glubs, 6 bat swarm, 7 dragon
    int[] Kinds() => theme == RunTheme.SpookyCarnival ? new[] { 0, 1, 2 } : theme == RunTheme.WitchForest ? new[] { 3, 4, 3, 5 } : new[] { 6, 7, 6, 2 };

    void BuildFlyers()
    {
        foreach (int k in Kinds())
        {
            while (pool.Count <= k) pool.Add(null);
            if (pool[k]) continue;
            var holder = new GameObject("Flyer" + k).transform; holder.SetParent(WorldBuilder.Dyn);
            switch (k)
            {
                case 0: AddModel(holder, "Ghost", 2.6f, Vector3.zero); break;
                case 1: for (int i = 0; i < 3; i++) AddModel(holder, "Ghost", 1.6f, new Vector3((i - 1) * 2.2f, i == 1 ? 0.8f : 0, i == 1 ? 1.2f : 0)); break;
                case 2: for (int i = 0; i < 2; i++) AddModel(holder, "Ghost_Skull", 1.7f, new Vector3((i - 0.5f) * 2.6f, i * 0.6f, 0)); break;
                case 3: { var w = Witch(holder, 1.25f); w.localPosition = Vector3.zero; Trail(w, new Color(0.75f, 1f, 0.3f), SpookyWorld.Purple, 14f); break; }
                case 5: for (int i = 0; i < 3; i++) AddModel(holder, "Glub", 1.4f, new Vector3((i - 1) * 2f, Mathf.Abs(i - 1) * 0.6f, -Mathf.Abs(i - 1) * 1f)); break;
                case 7: AddModel(holder, "Dragon", 7f, Vector3.zero); break;
            }
            holder.gameObject.SetActive(false);
            pool[k] = holder;
        }
    }

    void AddModel(Transform holder, string name, float h, Vector3 local)
    {
        var g = SpookyWorld.Critter(lib, name, Vector3.zero, 0, h, holder, "fast_flying", "flying_idle");
        if (!g) return;
        SpookyWorld.Spookify(g, name == "Dragon" ? SpookyWorld.Purple : new Color(0.7f, 0.95f, 1f), name == "Dragon" ? 0.3f : 0.7f);
        g.transform.localPosition = local + g.transform.localPosition.y * Vector3.up;
    }

    void StartFly()
    {
        var kinds = Kinds();
        kind = kinds[flyCount % kinds.Length];
        flySide = flyCount % 2 == 0 ? 1f : -1f;
        flyCount++;
        flyT = 0;
        flyer = null; swarm = null;
        if (kind == 4 || kind == 6)
        {
            // swarm of glowing fireflies / bats
            int n = kind == 6 ? 42 : 60;
            swarm = new Transform[n]; swarmOff = new Vector3[n]; swarmLag = new float[n];
            var m = kind == 6 ? (batMat ? batMat : HalloweenWorld.Transparent(lib.bat, Color.white)) : SpookyWorld.Glow(new Color(0.8f, 1f, 0.35f), 6f);
            for (int i = 0; i < n; i++)
            {
                var q = kind == 6 ? SpookyWorld.Quad(WorldBuilder.Dyn, Vector3.zero, Quaternion.identity, new Vector2(1.1f, 0.55f), m)
                                  : SpookyWorld.Ball(WorldBuilder.Dyn, Vector3.zero, Vector3.one * 0.14f, m, false);
                swarm[i] = q.transform;
                swarmOff[i] = Random.insideUnitSphere * (kind == 6 ? 3f : 2.2f);
                swarmLag[i] = Random.Range(0f, 0.12f);
            }
        }
        else if (kind < pool.Count && pool[kind])
        {
            flyer = pool[kind];
            flyer.gameObject.SetActive(true);
        }
        if (kind == 7) { d.PlaySfx(roar, 0.7f); d.Shake(0.06f); }
        else d.PlaySfx(whoosh, 0.9f);
    }

    void UpdateFly(Vector3 cp, float t)
    {
        flyT += Time.deltaTime;
        float u = flyT / FlyDur;
        bool big = kind == 7;
        if (u >= 1f)
        {
            flyT = -1f;
            if (flyer) flyer.gameObject.SetActive(false);
            if (swarm != null) foreach (var s in swarm) if (s) Destroy(s.gameObject);
            swarm = null; flyer = null;
            return;
        }
        Vector3 Rel(float k) { var b = Bez(Mathf.Clamp01(k), flySide, big); return new Vector3(b.x, b.y, cp.z + b.z); }
        if (flyer)
        {
            var p = Rel(u);
            var dir = Bez(Mathf.Clamp01(u + 0.02f), flySide, big) - Bez(u, flySide, big);
            flyer.position = p;
            if (dir.sqrMagnitude > 1e-5f) flyer.rotation = Quaternion.LookRotation(dir.normalized);
            if (kind == 7 && u > 0.55f && u < 0.6f) d.Shake(0.08f);
        }
        if (swarm != null)
            for (int i = 0; i < swarm.Length; i++)
            {
                var s = swarm[i];
                s.position = Rel(u - swarmLag[i]) + swarmOff[i] * (1f + 0.3f * Mathf.Sin(t * 3f + i));
                if (kind == 6)
                {
                    s.rotation = Quaternion.LookRotation(s.position - cp) * Quaternion.Euler(0, 0, Mathf.Sin(t * 22f + i) * 18f);
                    s.localScale = new Vector3(1.1f, 0.55f * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 22f + i))), 1);
                }
            }
    }

    // ---------------- per frame ----------------
    void LateUpdate()
    {
        if (!cam || !d || d.CurrentLevel < 0) return;
        var cp = cam.transform.position;
        float t = d.SongTime;
        float bpm = d.levels[d.CurrentLevel].bpm;
        float beat = t * bpm / 60f;
        float pulse = Mathf.Pow(1f - Mathf.Repeat(beat, 1f), 3f);

        if (sky) sky.position = cp;
        foreach (var (m, c) in SpookyWorld.BeatMats)
            if (m) m.SetColor("_EmissionColor", c * (0.55f + 0.85f * pulse));
        foreach (var (f, off) in follow) if (f) f.position = new Vector3(0, 0, cp.z) + off;

        // far things off, near things on
        cullT -= Time.deltaTime;
        if (cullT <= 0)
        {
            cullT = 0.2f;
            foreach (var c in culled)
            {
                if (!c) continue;
                float dz = c.position.z - cp.z;
                bool on = dz > -45f && dz < 240f;
                if (c.gameObject.activeSelf != on) c.gameObject.SetActive(on);
            }
        }

        UpdateFloaters(cp, Time.time);

        for (int i = 0; i < beams.Count; i++)
        {
            float s = i % 2 == 0 ? -1 : 1;
            float ahead = 70f + (i / 2) * 45f;
            beams[i].position = new Vector3(s * (24f + (i / 2) * 9f), -2f, cp.z + ahead);
            beams[i].rotation = Quaternion.Euler(Mathf.Sin(Time.time * 0.5f + i) * 22f - 8f, 0, s * (12f + Mathf.Sin(Time.time * 0.7f + i * 1.3f) * 20f));
        }

        // fireworks on the bar (fair: every bar, castle: every second bar)
        int bar = Mathf.FloorToInt(beat / 4f);
        if (bar != lastBar)
        {
            lastBar = bar;
            if (theme == RunTheme.SpookyCarnival || (theme == RunTheme.MoonCastle && bar % 2 == 0))
            {
                Color[] cols = theme == RunTheme.MoonCastle ? new[] { SpookyWorld.Lime, SpookyWorld.Purple, SpookyWorld.Orange } : SpookyWorld.Party;
                int n = theme == RunTheme.SpookyCarnival && bar % 4 == 0 ? 2 : 1;
                for (int k = 0; k < n; k++)
                    Firework(new Vector3((Random.value < 0.5f ? -1 : 1) * Random.Range(12f, 36f), Random.Range(20f, 34f), cp.z + Random.Range(55f, 95f)), cols[Random.Range(0, cols.Length)]);
            }
        }

        // lightning over the castle
        if (bolt)
        {
            if (t > nextBolt) { boltT = 0; nextBolt = t + Random.Range(7f, 12f); if (thunder.clip) thunder.PlayDelayed(Random.Range(0.2f, 0.5f)); }
            if (boltT >= 0)
            {
                boltT += Time.deltaTime;
                float f = boltT < 0.07f ? 1f : boltT < 0.14f ? 0.25f : boltT < 0.22f ? 0.9f : Mathf.Max(0, 1f - (boltT - 0.22f) * 4f);
                bolt.transform.position = cp + new Vector3(0, 30f, 40f);
                bolt.intensity = 7f * f;
                if (sun) sun.intensity = sunBase + f * 1.4f;
                if (boltT > 0.6f) { boltT = -1; bolt.intensity = 0; if (sun) sun.intensity = sunBase; }
            }
        }

        // fly-bys in the gaps between cue bursts
        if (flyT < 0 && flyNext < flyTimes.Count && t >= flyTimes[flyNext]) { flyNext++; StartFly(); }
        if (flyT >= 0) UpdateFly(cp, t);
    }
}
