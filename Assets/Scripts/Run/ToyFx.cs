using System.Collections.Generic;
using UnityEngine;
using static SpookyWorld;

// Red Light Green Light Quest atmosphere: sky dome, the giant FreezeBot that turns around on every FREEZE cue
// (red eyes, scanning lasers, red light over the road; green lamp on its back while we run), confetti on
// GREEN LIGHT, beat pulse, balloons and kites (playground), toy trains (toy store), sweets riding the
// conveyor belts (candy factory), ambient bubbles / sparkles / sprinkles.
public class ToyFx : MonoBehaviour
{
    RunTheme theme;
    RunLibrary lib;
    Camera cam;
    RunDirector d;
    Transform sky;
    readonly List<Transform> culled = new List<Transform>();
    float cullT;
    readonly List<(Transform t, Vector3 off)> follow = new List<(Transform, Vector3)>();

    // FreezeBot
    Transform bot, beamL, beamR;
    Vector3 eyeL, eyeR;   // local positions of the eyes
    Material eyeMat, antennaMat, chestRed, chestGreen, backMat;
    Light redLight;
    readonly HashSet<float> cheered = new HashSet<float>();

    // movers: balloons / kites / trains / conveyor sweets
    readonly List<Transform> movers = new List<Transform>();
    readonly List<Vector3> moverVel = new List<Vector3>();
    readonly List<Vector3> moverOff = new List<Vector3>();   // kites ride along with the camera

    public void Setup(RunLibrary l, RunTheme t, float length)
    {
        lib = l; theme = t; cam = Camera.main; d = FindAnyObjectByType<RunDirector>();
        foreach (Transform c in WorldBuilder.Dyn) culled.Add(c);
        BuildSky(ToyWorld.Pal(t));
        BuildBot();
        var spark = T(lib, "star_06") ? T(lib, "star_06") : lib.spark;
        switch (t)
        {
            case RunTheme.RlPlayground:
                Ambient("Bubbles", lib.spark, new[] { new Color(0.75f, 0.95f, 1f), new Color(1f, 0.85f, 1f) }, 0.15f, 0.4f, 30, new Vector3(24, 3, 36), new Vector3(0, 2.5f, 18), new Vector2(0.2f, 0.8f), 5f);
                for (int i = 0; i < 22; i++) Balloon(i);
                for (int i = 0; i < 6; i++) Kite(i);
                break;
            case RunTheme.RlToyStore:
                Ambient("Sparkles", spark, new[] { ToyWorld.Yellow, Color.white, ToyWorld.Blue }, 0.06f, 0.16f, 50, new Vector3(22, 8, 40), new Vector3(0, 5, 20), new Vector2(-0.2f, 0.3f), 3f);
                foreach (int s in new[] { -1, 1 }) Train(s);
                break;
            default:
                Ambient("Sprinkles", null, Party, 0.06f, 0.12f, 60, new Vector3(24, 2, 38), new Vector3(0, 9, 20), new Vector2(-1.4f, -0.7f), 6f);
                foreach (int s in new[] { -1, 1 }) for (int i = 0; i < 18; i++) Sweet(s, i);
                for (int i = 0; i < 14; i++) Balloon(i);
                break;
        }
    }

    void OnDestroy() { if (redLight) Destroy(redLight.gameObject); }

    // ---------------- sky ----------------
    void BuildSky(Palette pal)
    {
        if (!lib.skyShader || !lib.skyShader.isSupported) return;
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(g.GetComponent<Collider>());
        g.name = "SkyDome";
        g.transform.SetParent(WorldBuilder.Dyn);
        g.transform.localScale = Vector3.one * 700f;
        var m = new Material(lib.skyShader);
        m.SetColor("_Top", pal.top); m.SetColor("_Mid", pal.mid); m.SetColor("_Horizon", pal.horizon);
        m.SetVector("_MoonDir", pal.moonDir); m.SetColor("_MoonColor", pal.moon); m.SetFloat("_MoonSize", pal.moonSize);
        m.SetFloat("_Clouds", pal.clouds); m.SetColor("_CloudColor", pal.cloud);
        m.SetFloat("_Aurora", 0f); m.SetFloat("_Stars", 0f);
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        sky = g.transform;
    }

    // ---------------- the FreezeBot ----------------
    static Material Dyn(Color c, float glow)
    {
        var m = WorldBuilder.Emissive(c, glow);
        m.SetFloat("_Smoothness", 0.7f);
        return m;
    }

    void BuildBot()
    {
        bot = new GameObject("FreezeBot").transform;
        bot.SetParent(WorldBuilder.Dyn);
        var o = Vector3.zero;
        var white = Lit(new Color(0.95f, 0.96f, 1f), 0.8f, 0.2f);
        var blue = Lit(new Color(0.25f, 0.5f, 1f), 0.6f, 0.3f);
        var dark = Lit(new Color(0.08f, 0.08f, 0.12f), 0.8f);
        // hovers on jets (no legs): its lowest point stays above every obstacle it glides over
        var flame = Glow(new Color(1f, 0.6f, 0.15f), 5f, true);
        foreach (int s in new[] { -1, 1 })
        {
            Cyl(bot, o + new Vector3(s * 2.6f, 6.6f, 0), 1.6f, 0.8f, dark);
            var jet = ConeObj(bot, o + new Vector3(s * 2.6f, 6.2f, 0), 1.3f, 1.4f, flame, 12, 0, false);
            jet.transform.rotation = Quaternion.Euler(180, 0, 0);
            Ball(bot, o + new Vector3(s * 4.6f, 13.4f, 0), Vector3.one * 1.8f, blue);
            var arm = Box(bot, o + new Vector3(s * 5.2f, 11f, 0), new Vector3(1.2f, 5f, 1.2f), white);
            arm.transform.rotation = Quaternion.Euler(0, 0, s * 8f);
            Ball(bot, o + new Vector3(s * 5.6f, 8.3f, 0), Vector3.one * 1.6f, blue);
            Ball(bot, o + new Vector3(s * 3.5f, 17.4f, 0), new Vector3(1f, 2.2f, 2.2f), blue);   // ears
        }
        Box(bot, o + new Vector3(0, 7.6f, 0), new Vector3(8f, 1.6f, 3.4f), blue);
        Box(bot, o + new Vector3(0, 11.2f, 0), new Vector3(8.4f, 6f, 4f), white);
        // traffic light on the chest (front = -Z, toward the camera when it looks at us)
        Box(bot, o + new Vector3(0, 11.2f, -2.05f), new Vector3(3f, 5.4f, 0.3f), dark);
        chestRed = Dyn(new Color(1f, 0.15f, 0.12f), 0.3f);
        chestGreen = Dyn(new Color(0.2f, 1f, 0.3f), 0.3f);
        Ball(bot, o + new Vector3(0, 12.9f, -2.25f), new Vector3(1.6f, 1.6f, 0.6f), chestRed, false);
        Ball(bot, o + new Vector3(0, 11.2f, -2.25f), new Vector3(1.6f, 1.6f, 0.6f), Dyn(new Color(1f, 0.8f, 0.1f), 0.4f), false);
        Ball(bot, o + new Vector3(0, 9.5f, -2.25f), new Vector3(1.6f, 1.6f, 0.6f), chestGreen, false);
        Cyl(bot, o + new Vector3(0, 14.8f, 0), 1.4f, 1.2f, dark);
        Box(bot, o + new Vector3(0, 17.4f, 0), new Vector3(6.4f, 4.8f, 4.4f), white);
        Quad(bot, o + new Vector3(0, 17.3f, -2.22f), Quaternion.identity, new Vector2(5.4f, 4f), Tex("botface", 1, 1, Color.white, 0.6f, 0.8f));
        eyeMat = Dyn(Color.white, 1.2f);
        eyeL = o + new Vector3(-1.25f, 17.9f, -2.32f); eyeR = o + new Vector3(1.25f, 17.9f, -2.32f);
        Ball(bot, eyeL, new Vector3(1f, 1.25f, 0.4f), eyeMat, false);
        Ball(bot, eyeR, new Vector3(1f, 1.25f, 0.4f), eyeMat, false);
        Cyl(bot, o + new Vector3(0, 20.8f, 0), 0.18f, 2f, dark, null, false);
        antennaMat = Dyn(new Color(0.2f, 1f, 0.3f), 3f);
        Ball(bot, o + new Vector3(0, 22f, 0), Vector3.one * 0.8f, antennaMat, false);
        // big GO lamp on its back: what we see while it looks away
        backMat = Dyn(new Color(0.2f, 1f, 0.3f), 3f);
        Cyl(bot, o + new Vector3(0, 11.4f, 2.1f), 4f, 0.3f, dark, Quaternion.Euler(90, 0, 0));
        Cyl(bot, o + new Vector3(0, 11.4f, 2.3f), 3.4f, 0.2f, backMat, Quaternion.Euler(90, 0, 0), false);
        Cyl(bot, o + new Vector3(0, 17.4f, 2.25f), 2f, 0.2f, backMat, Quaternion.Euler(90, 0, 0), false);
        // scanning lasers from the eyes
        var laser = Dyn(new Color(1f, 0.1f, 0.1f), 8f);
        beamL = Box(WorldBuilder.Dyn, Vector3.zero, Vector3.one, laser, false).transform;
        beamR = Box(WorldBuilder.Dyn, Vector3.zero, Vector3.one, laser, false).transform;
        beamL.gameObject.SetActive(false); beamR.gameObject.SetActive(false);
        var lg = new GameObject("RedLight");
        lg.transform.SetParent(WorldBuilder.Dyn);
        redLight = lg.AddComponent<Light>(); redLight.type = LightType.Point; redLight.range = 80f; redLight.intensity = 0;
        redLight.color = new Color(1f, 0.15f, 0.12f); redLight.shadows = LightShadows.None;
        bot.rotation = Quaternion.Euler(0, 180, 0);
    }

    void UpdateBot(Vector3 cp, float t)
    {
        float look = 0f; bool red = false;
        float hold = d.FreezeHold;
        foreach (var c in d.CurrentCues)
        {
            if (c.move != RunMove.Freeze) continue;
            float u = t - c.time;
            if (u > -0.8f && u < hold + 0.5f)
            {
                float k = u < -0.3f ? Mathf.InverseLerp(-0.8f, -0.3f, u) : u < hold ? 1f : 1f - Mathf.InverseLerp(hold, hold + 0.5f, u);
                look = Mathf.Max(look, Mathf.SmoothStep(0, 1, k));
                if (u > -0.3f && u < hold) red = true;
            }
            // GREEN LIGHT: confetti pops around the camera
            if (u >= hold && u < hold + 0.3f && cheered.Add(c.time))
                for (int i = 0; i < 6; i++)
                    RunFx.Burst(cp + new Vector3((i % 2 == 0 ? -1 : 1) * Random.Range(2f, 4.5f), Random.Range(1.5f, 4f), Random.Range(6f, 12f)), Party[i % Party.Length], true, 40, 0.2f);
        }
        float bob = (1 - look) * Mathf.Sin(t * 3f) * 0.35f;
        bot.position = new Vector3(0, bob, cp.z + 62f);
        bot.rotation = Quaternion.Euler(0, Mathf.Lerp(180f, 0f, look), 0);

        var eye = red ? new Color(1f, 0.1f, 0.08f) * 7f : Color.white * 1.2f;
        eyeMat.SetColor("_EmissionColor", eye);
        eyeMat.SetColor("_BaseColor", red ? new Color(1f, 0.2f, 0.2f) : Color.white);
        antennaMat.SetColor("_EmissionColor", red ? new Color(1f, 0.1f, 0.1f) * 5f : new Color(0.2f, 1f, 0.3f) * 3f);
        chestRed.SetColor("_EmissionColor", new Color(1f, 0.15f, 0.12f) * (red ? 6f : 0.3f));
        chestGreen.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.3f) * (red ? 0.3f : 4f));
        backMat.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.3f) * (red ? 0.3f : 3.5f));

        // red light floods the road; lasers sweep the ground in front of us
        redLight.transform.position = new Vector3(0, 12f, cp.z + 40f);
        redLight.intensity = Mathf.MoveTowards(redLight.intensity, red ? 6f : 0f, Time.unscaledDeltaTime * 20f);
        bool lasers = red && look > 0.95f;
        beamL.gameObject.SetActive(lasers); beamR.gameObject.SetActive(lasers);
        if (lasers)
        {
            float sweep = Mathf.Sin(Time.unscaledTime * 2.4f);
            Beam(beamL, bot.TransformPoint(eyeL), new Vector3(sweep * 4f - 1f, 0, cp.z + 5f));
            Beam(beamR, bot.TransformPoint(eyeR), new Vector3(-sweep * 4f + 1f, 0, cp.z + 7f));
        }
    }

    static void Beam(Transform b, Vector3 from, Vector3 to)
    {
        var dir = to - from;
        b.position = (from + to) * 0.5f;
        b.rotation = Quaternion.LookRotation(dir);
        b.localScale = new Vector3(0.09f, 0.09f, dir.magnitude);
    }

    // ---------------- ambient particles ----------------
    static Material PMat(Texture2D tex, bool glow)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        if (tex) m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", glow ? new Color(2f, 2f, 2f, 1f) : Color.white);
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        return m;
    }

    void Ambient(string name, Texture2D tex, Color[] colors, float sMin, float sMax, int rate, Vector3 box, Vector3 offset, Vector2 rise, float life)
    {
        var g = new GameObject(name); g.transform.SetParent(WorldBuilder.Dyn);
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.startLifetime = life; main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(sMin, sMax);
        var gr = new Gradient();
        var keys = new GradientColorKey[Mathf.Min(8, colors.Length)];
        for (int i = 0; i < keys.Length; i++) keys[i] = new GradientColorKey(colors[i], i / (float)Mathf.Max(1, keys.Length - 1));
        gr.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        gr.mode = GradientMode.Fixed;
        main.startColor = new ParticleSystem.MinMaxGradient(gr) { mode = ParticleSystemGradientMode.RandomColor };
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 1500;
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f); vel.y = new ParticleSystem.MinMaxCurve(rise.x, rise.y); vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        var n = ps.noise; n.enabled = true; n.strength = 0.5f; n.frequency = 0.6f;
        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
        var col = ps.colorOverLifetime; col.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(fade);
        var pr = g.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = PMat(tex, tex != null);
        pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
        follow.Add((g.transform, offset));
    }

    // ---------------- movers ----------------
    void Add(Transform t, Vector3 vel, Vector3 off) { movers.Add(t); moverVel.Add(vel); moverOff.Add(off); }

    void Balloon(int i)
    {
        var b = new GameObject("Balloon").transform; b.SetParent(WorldBuilder.Dyn);
        var c = Party[i % Party.Length];
        Ball(b, Vector3.zero, new Vector3(0.9f, 1.1f, 0.9f), Lit(c, 0.9f), false);
        Box(b, new Vector3(0, -1.2f, 0), new Vector3(0.02f, 1.4f, 0.02f), Lit(Color.white), false);
        b.position = new Vector3((Random.value < 0.5f ? -1 : 1) * Random.Range(8f, 26f), Random.Range(0f, 22f), Random.Range(0f, 130f));
        Add(b, new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(1.3f, 2.4f), 0), Vector3.zero);
    }

    void Kite(int i)
    {
        var k = new GameObject("Kite").transform; k.SetParent(WorldBuilder.Dyn);
        var c = Party[i % Party.Length];
        var body = Box(k, Vector3.zero, new Vector3(1.8f, 1.8f, 0.05f), Lit(c, 0.4f), false);
        body.transform.rotation = Quaternion.Euler(0, 0, 45);
        Box(k, Vector3.zero, new Vector3(2.5f, 0.06f, 0.08f), Lit(Color.white), false);
        for (int j = 0; j < 5; j++)
        {
            var bow = Box(k, new Vector3(0, -1.6f - j * 0.7f, 0), new Vector3(0.35f, 0.2f, 0.03f), Lit(Party[(i + j + 1) % Party.Length], 0.4f), false);
            bow.transform.rotation = Quaternion.Euler(0, 0, 20 * (j % 2 == 0 ? 1 : -1));
        }
        var off = new Vector3((i % 2 == 0 ? -1 : 1) * Random.Range(10f, 28f), Random.Range(16f, 26f), Random.Range(40f, 110f));
        Add(k, Vector3.zero, off);
    }

    void Train(int s)
    {
        if (lib.trains == null || lib.trains.Length == 0) return;
        var holder = new GameObject("ToyTrain").transform; holder.SetParent(WorldBuilder.Dyn);
        float z = 0;
        for (int i = 0; i < 4; i++)
        {
            var car = WorldBuilder.Spawn(lib.trains[i == 0 ? 0 : 1 + (i % Mathf.Max(1, lib.trains.Length - 1))], Vector3.zero, 0, holder);
            BlockWorld.Fit(car, 1.8f);
            var b = WorldBuilder.Bounds(car);
            car.transform.position += new Vector3(-b.center.x, 0, z - b.max.z);
            z -= b.size.z + 0.25f;
            var col = Party[(i + (s > 0 ? 2 : 0)) % Party.Length];
            foreach (var r in car.GetComponentsInChildren<Renderer>())
            {
                var ms = r.materials;
                foreach (var m in ms) if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.Lerp(Color.white, col, 0.7f) * 1.1f);
                r.materials = ms;
            }
        }
        holder.position = new Vector3(s * 6f, 0.12f, s > 0 ? 60f : 20f);
        Add(holder, new Vector3(0, 0, Random.Range(6f, 9f)), Vector3.zero);
    }

    void Sweet(int s, int i)
    {
        string[] names = { "lollypop", "candy-bar", "donut-sprinkles", "cupcake", "cookie-chocolate", "popsicle", "muffin" };
        var g = CandyWorld.Put(lib, names[(i + (s > 0 ? 3 : 0)) % names.Length], new Vector3(s * 7.6f, 1.05f, i * 7f + Random.Range(0f, 3f)), Random.Range(0, 360), 0.7f, WorldBuilder.Dyn);
        if (g) Add(g.transform, new Vector3(0, 0, 3f), Vector3.zero);
    }

    void UpdateMovers(Vector3 cp)
    {
        float dt = Time.deltaTime, tt = Time.time;
        for (int i = 0; i < movers.Count; i++)
        {
            var m = movers[i];
            if (!m) continue;
            if (moverOff[i] != Vector3.zero)
            {
                // kite: rides along with us, dancing in the wind
                var o = moverOff[i];
                m.position = new Vector3(o.x + Mathf.Sin(tt * 0.7f + i) * 2f, o.y + Mathf.Sin(tt * 1.3f + i * 2f) * 1.2f, cp.z + o.z);
                m.rotation = Quaternion.Euler(Mathf.Sin(tt * 1.1f + i) * 10f, 0, Mathf.Sin(tt * 1.7f + i) * 20f);
                continue;
            }
            m.position += moverVel[i] * dt;
            bool train = m.name == "ToyTrain";
            if (!train && moverVel[i].y > 0)
            {
                m.position += new Vector3(Mathf.Sin(tt * 0.9f + i) * 0.3f, 0, 0) * dt;
                if (m.position.z < cp.z - 8 || m.position.y > 30)
                    m.position = new Vector3((Random.value < 0.5f ? -1 : 1) * Random.Range(8f, 26f), Random.Range(-1f, 3f), cp.z + Random.Range(30f, 130f));
            }
            else if (m.position.z < cp.z - (train ? 40f : 8f))
                m.position = new Vector3(m.position.x, m.position.y, cp.z + Random.Range(train ? 90f : 60f, train ? 150f : 130f));
        }
    }

    // ---------------- per frame ----------------
    void LateUpdate()
    {
        if (!cam || !d || d.CurrentLevel < 0) return;
        var cp = cam.transform.position;
        float t = d.SongTime;
        float beat = t * d.levels[d.CurrentLevel].bpm / 60f;
        float pulse = Mathf.Pow(1f - Mathf.Repeat(beat, 1f), 3f) * d.FreezeFactor(t);   // lights stop pulsing while frozen

        if (sky) sky.position = cp;
        foreach (var (m, c) in BeatMats) if (m) m.SetColor("_EmissionColor", c * (0.55f + 0.85f * pulse));
        foreach (var (f, off) in follow) if (f) f.position = new Vector3(0, 0, cp.z) + off;
        foreach (var m in ToyWorld.ScrollMats)
            if (m) m.SetTextureOffset("_BaseMap", new Vector2(0, -Time.time * 0.4f));

        cullT -= Time.unscaledDeltaTime;
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
        UpdateMovers(cp);
        UpdateBot(cp, t);
    }
}
