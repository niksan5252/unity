using System.Collections.Generic;
using UnityEngine;

// Minecraft-style creatures built from boxes (16 px per block proportions), animated in code:
// limb swing for walking/running, arms up for jumping, punches, zombie arms forward, quadrupeds, spider, slime.
public class BlockFigure : MonoBehaviour
{
    public enum Kind { Hero, Heroine, Zombie, Skeleton, Spider, Slime, Pig, Cow, Sheep, Chicken, Knight, KnightGold, Shade, PolarBear, Wolf, Ocelot }
    public Kind kind;
    public string state = "Run";      // Run, Walk, Idle, Jump, Punch, HitReact, Graze
    public float speedMul = 1f;
    float phase, stateT;
    Transform head, body, armL, armR, legL, legR;
    readonly List<Transform> legs = new List<Transform>();   // quadrupeds / spider
    Vector3 baseScale;
    static readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
    public static Dictionary<string, Texture2D> Tex = new Dictionary<string, Texture2D>();

    const float P = 1.8f / 32f;   // one "pixel" of a 32-px tall player = 1.8 m

    static Material M(Color c)
    {
        if (mats.TryGetValue(c, out var m) && m) return m;
        m = WorldBuilder.Mat(c);
        m.SetFloat("_Smoothness", 0.05f);
        return mats[c] = m;
    }

    static Material FaceMat(string tex)
    {
        Tex.TryGetValue(tex, out var t);
        var m = WorldBuilder.Mat(Color.white);
        if (t) m.SetTexture("_BaseMap", t);
        m.SetFloat("_Smoothness", 0.05f);
        return m;
    }

    // a box part whose pivot is at the top-center (limbs swing from the shoulder / hip)
    Transform Part(Transform parent, string name, Vector3 pivot, Vector3 sizePx, Color c, float hang = 1f)
    {
        var piv = new GameObject(name).transform;
        piv.SetParent(parent, false);
        piv.localPosition = pivot * P;
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(box.GetComponent<Collider>());
        box.transform.SetParent(piv, false);
        box.transform.localScale = sizePx * P;
        box.transform.localPosition = new Vector3(0, -sizePx.y * 0.5f * P * hang, 0);
        box.GetComponent<Renderer>().sharedMaterial = M(c);
        return piv;
    }

    void Face(Transform headPivot, float headSizePx, string tex, float yOff)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(headPivot, false);
        q.transform.localScale = Vector3.one * headSizePx * P;
        // the figure faces +Z; the quad's visible side looks along -Z, so turn it around
        q.transform.localPosition = new Vector3(0, yOff * P, (headSizePx * 0.5f + 0.05f) * P);
        q.transform.localRotation = Quaternion.Euler(0, 180, 0);
        q.GetComponent<Renderer>().sharedMaterial = FaceMat(tex);
    }

    public static BlockFigure Create(Kind kind, Transform parent = null)
    {
        var go = new GameObject("Block" + kind);
        if (parent) go.transform.SetParent(parent, false);
        var f = go.AddComponent<BlockFigure>();
        f.kind = kind;
        f.phase = Random.value * 10f;
        f.Build();
        f.baseScale = go.transform.localScale;
        return f;
    }

    void Build()
    {
        var root = transform;
        switch (kind)
        {
            case Kind.Hero:
            case Kind.Heroine:
            case Kind.Zombie:
            case Kind.Skeleton:
            case Kind.Knight:
            case Kind.KnightGold:
            case Kind.Shade:
            {
                if (kind == Kind.Shade) { BuildShade(root); break; }
                bool knight = kind == Kind.Knight || kind == Kind.KnightGold;
                bool skel = kind == Kind.Skeleton, girl = kind == Kind.Heroine;
                Color skin = kind == Kind.Zombie ? new Color(0.32f, 0.55f, 0.28f) : skel ? new Color(0.78f, 0.78f, 0.78f) : girl ? new Color(0.91f, 0.7f, 0.55f) : new Color(0.77f, 0.59f, 0.43f);
                Color shirt = kind == Kind.Hero ? new Color(0.0f, 0.68f, 0.68f) : girl ? new Color(0.45f, 0.72f, 0.3f) : kind == Kind.Zombie ? new Color(0.0f, 0.62f, 0.62f) : skin;
                Color pants = kind == Kind.Hero ? new Color(0.29f, 0.25f, 0.6f) : girl ? new Color(0.45f, 0.32f, 0.2f) : kind == Kind.Zombie ? new Color(0.27f, 0.22f, 0.55f) : skin;
                Color shoe = skel ? skin : new Color(0.35f, 0.35f, 0.38f);
                if (knight)
                {
                    // shiny armour over the hero: diamond (cyan) or gold
                    Color arm = kind == Kind.Knight ? new Color(0.36f, 0.9f, 0.88f) : new Color(1f, 0.82f, 0.2f);
                    shirt = arm; pants = arm * 0.85f; pants.a = 1; shoe = arm * 0.7f; shoe.a = 1;
                    skin = kind == Kind.KnightGold ? new Color(0.91f, 0.7f, 0.55f) : new Color(0.77f, 0.59f, 0.43f);
                }
                float limb = skel ? 2f : 4f;
                body = Part(root, "Body", new Vector3(0, 24, 0), new Vector3(8, 12, 4), shirt);
                head = Part(root, "Head", new Vector3(0, 32, 0), new Vector3(8, 8, 8), skin);
                Face(head, 8, kind == Kind.Hero || kind == Kind.Knight ? "face_hero" : girl || kind == Kind.KnightGold ? "face_heroine" : kind == Kind.Zombie ? "face_zombie" : "face_skeleton", -4);
                if (knight)
                {
                    // helmet: top, back and sides, open face
                    void Hm(Vector3 s, Vector3 p) { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(head, false); g.transform.localScale = s * P; g.transform.localPosition = p * P; g.GetComponent<Renderer>().sharedMaterial = M(shirt); }
                    Hm(new Vector3(9f, 2.2f, 9f), new Vector3(0, -0.6f, 0));
                    Hm(new Vector3(9f, 7f, 1.2f), new Vector3(0, -4f, -4.2f));
                    Hm(new Vector3(1.2f, 7f, 9f), new Vector3(-4.2f, -4f, 0));
                    Hm(new Vector3(1.2f, 7f, 9f), new Vector3(4.2f, -4f, 0));
                    var shine = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(shine.GetComponent<Collider>());
                    shine.transform.SetParent(body, false); shine.transform.localScale = new Vector3(2f, 2f, 0.4f) * P; shine.transform.localPosition = new Vector3(-2f, -3f, 2.1f) * P;
                    shine.GetComponent<Renderer>().sharedMaterial = M(Color.Lerp(shirt, Color.white, 0.6f));
                }
                if ((kind == Kind.Hero || girl) && !knight)
                {
                    // hair on top and back of the head
                    var hair = girl ? new Color(0.9f, 0.47f, 0.16f) : new Color(0.27f, 0.17f, 0.09f);
                    var top = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(top.GetComponent<Collider>());
                    top.transform.SetParent(head, false); top.transform.localScale = new Vector3(8.4f, 2f, 8.4f) * P; top.transform.localPosition = new Vector3(0, -1f * P, 0);
                    top.GetComponent<Renderer>().sharedMaterial = M(hair);
                    var back = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(back.GetComponent<Collider>());
                    back.transform.SetParent(head, false); back.transform.localScale = new Vector3(8.4f, girl ? 9f : 6f, 1f) * P; back.transform.localPosition = new Vector3(0, (girl ? -5f : -4f) * P, -4f * P);
                    back.GetComponent<Renderer>().sharedMaterial = M(hair);
                }
                armL = Part(root, "ArmL", new Vector3(-(4 + limb * 0.5f), 24, 0), new Vector3(limb, 12, limb), knight ? shirt : skin);
                armR = Part(root, "ArmR", new Vector3(4 + limb * 0.5f, 24, 0), new Vector3(limb, 12, limb), knight ? shirt : skin);
                if ((kind == Kind.Hero || girl) && !knight)
                {
                    // short sleeves in the shirt colour
                    foreach (var a in new[] { armL, armR })
                    {
                        var sl = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(sl.GetComponent<Collider>());
                        sl.transform.SetParent(a, false); sl.transform.localScale = new Vector3(4.2f, 4f, 4.2f) * P; sl.transform.localPosition = new Vector3(0, -2f * P, 0);
                        sl.GetComponent<Renderer>().sharedMaterial = M(shirt);
                    }
                }
                legL = Part(root, "LegL", new Vector3(-limb * 0.5f - (skel ? 1 : 0), 12, 0), new Vector3(limb, 12, limb), pants);
                legR = Part(root, "LegR", new Vector3(limb * 0.5f + (skel ? 1 : 0), 12, 0), new Vector3(limb, 12, limb), pants);
                foreach (var lg in new[] { legL, legR })
                {
                    var sh = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(sh.GetComponent<Collider>());
                    sh.transform.SetParent(lg, false); sh.transform.localScale = new Vector3(limb + 0.2f, 2f, limb + 0.2f) * P; sh.transform.localPosition = new Vector3(0, -11f * P, 0);
                    sh.GetComponent<Renderer>().sharedMaterial = M(shoe);
                }
                break;
            }
            case Kind.Pig:
            case Kind.Cow:
            case Kind.Sheep:
            case Kind.PolarBear:
            case Kind.Wolf:
            case Kind.Ocelot:
            {
                Color c = kind == Kind.Pig ? new Color(0.94f, 0.63f, 0.63f) : kind == Kind.Cow ? new Color(0.24f, 0.18f, 0.15f)
                        : kind == Kind.PolarBear ? new Color(0.95f, 0.95f, 0.93f) : kind == Kind.Wolf ? new Color(0.72f, 0.72f, 0.74f)
                        : kind == Kind.Ocelot ? new Color(0.95f, 0.78f, 0.35f) : new Color(0.93f, 0.93f, 0.93f);
                Color legC = kind == Kind.Sheep ? new Color(0.86f, 0.78f, 0.7f) : c;
                float bl = kind == Kind.Pig ? 16 : kind == Kind.PolarBear ? 22 : kind == Kind.Wolf || kind == Kind.Ocelot ? 14 : 18;
                float bh = kind == Kind.Pig ? 8 : kind == Kind.PolarBear ? 12 : kind == Kind.Wolf || kind == Kind.Ocelot ? 7 : 10;
                float lh = kind == Kind.Pig ? 6 : kind == Kind.Wolf || kind == Kind.Ocelot ? 8 : 12;
                body = Part(root, "Body", new Vector3(0, lh + bh, 0), new Vector3(10, bh, bl), c);
                if (kind == Kind.Cow)
                    for (int i = 0; i < 4; i++)
                    {
                        var spot = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(spot.GetComponent<Collider>());
                        spot.transform.SetParent(body, false); spot.transform.localScale = new Vector3(10.2f, 4f, 5f) * P;
                        spot.transform.localPosition = new Vector3(0, (-2f - (i % 2) * 4f) * P, (-6 + i * 4f) * P);
                        spot.GetComponent<Renderer>().sharedMaterial = M(new Color(0.92f, 0.92f, 0.9f));
                    }
                head = Part(root, "Head", new Vector3(0, lh + bh + 2, bl * 0.5f + 3), new Vector3(8, 8, 6), kind == Kind.Sheep ? legC : c);
                Face(head, 8, kind == Kind.Pig ? "face_pig" : kind == Kind.Cow ? "face_cow" : kind == Kind.Sheep || kind == Kind.PolarBear ? "face_sheep" : kind == Kind.Ocelot ? "face_chicken" : "face_skeleton", -4);
                if (kind == Kind.Wolf || kind == Kind.Ocelot || kind == Kind.PolarBear)
                {
                    // ears + tail
                    foreach (var ex in new[] { -2.5f, 2.5f })
                    {
                        var e = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(e.GetComponent<Collider>());
                        e.transform.SetParent(head, false); e.transform.localScale = new Vector3(2, 2, 1) * P; e.transform.localPosition = new Vector3(ex, 1f, -1f) * P;
                        e.GetComponent<Renderer>().sharedMaterial = M(c);
                    }
                    var tail = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(tail.GetComponent<Collider>());
                    tail.transform.SetParent(body, false); tail.transform.localScale = new Vector3(2, 2, kind == Kind.PolarBear ? 2 : 8) * P; tail.transform.localPosition = new Vector3(0, -1f, -bl * 0.5f - (kind == Kind.PolarBear ? 1 : 4)) * P;
                    tail.transform.localRotation = Quaternion.Euler(-25, 0, 0);
                    tail.GetComponent<Renderer>().sharedMaterial = M(c);
                }
                head.GetChild(head.childCount - 1).localPosition = new Vector3(0, -4 * P, 3.05f * P);
                foreach (var (x, z) in new[] { (-3f, bl * 0.5f - 3), (3f, bl * 0.5f - 3), (-3f, -bl * 0.5f + 3), (3f, -bl * 0.5f + 3) })
                    legs.Add(Part(root, "Leg", new Vector3(x, lh, z), new Vector3(4, lh, 4), legC));
                break;
            }
            case Kind.Chicken:
            {
                body = Part(root, "Body", new Vector3(0, 9, 0), new Vector3(6, 6, 8), new Color(0.96f, 0.96f, 0.96f));
                head = Part(root, "Head", new Vector3(0, 15, 4), new Vector3(4, 6, 3), new Color(0.96f, 0.96f, 0.96f));
                Face(head, 4, "face_chicken", -3);
                head.GetChild(head.childCount - 1).localPosition = new Vector3(0, -3 * P, 1.55f * P);
                armL = Part(root, "WingL", new Vector3(-3.5f, 9, 0), new Vector3(1, 4, 6), new Color(0.9f, 0.9f, 0.9f));
                armR = Part(root, "WingR", new Vector3(3.5f, 9, 0), new Vector3(1, 4, 6), new Color(0.9f, 0.9f, 0.9f));
                legs.Add(Part(root, "Leg", new Vector3(-1.5f, 3, 0), new Vector3(1, 3, 1), new Color(0.95f, 0.65f, 0.1f)));
                legs.Add(Part(root, "Leg", new Vector3(1.5f, 3, 0), new Vector3(1, 3, 1), new Color(0.95f, 0.65f, 0.1f)));
                break;
            }
            case Kind.Spider:
            {
                var dark = new Color(0.2f, 0.17f, 0.17f);
                body = Part(root, "Body", new Vector3(0, 9, -6), new Vector3(10, 8, 12), dark);
                head = Part(root, "Head", new Vector3(0, 9, 3), new Vector3(8, 8, 8), dark);
                Face(head, 8, "face_spider", -4);
                for (int i = 0; i < 8; i++)
                {
                    float side = i < 4 ? -1 : 1, z = -3 + (i % 4) * 2.5f;
                    var l = Part(root, "Leg", new Vector3(side * 4, 7, z), new Vector3(16, 2, 2), dark, 0f);
                    l.GetChild(0).localPosition = new Vector3(side * 8 * P, 0, 0);
                    legs.Add(l);
                }
                break;
            }
            case Kind.Slime:
            {
                var g = new Color(0.45f, 0.8f, 0.35f);
                body = Part(root, "Body", new Vector3(0, 16, 0), new Vector3(16, 16, 16), g);
                var m = M(new Color(0.45f, 0.8f, 0.35f, 1f));
                head = body;
                Face(body, 16, "face_slime", -8);
                break;
            }
        }
    }

    // tall thin dark End creature with glowing purple eyes
    void BuildShade(Transform root)
    {
        var dark = new Color(0.07f, 0.06f, 0.09f);
        body = Part(root, "Body", new Vector3(0, 42, 0), new Vector3(8, 12, 4), dark);
        head = Part(root, "Head", new Vector3(0, 50, 0), new Vector3(8, 8, 8), dark);
        var eyeM = WorldBuilder.Emissive(new Color(0.85f, 0.35f, 1f), 3f);
        foreach (var ex in new[] { -2f, 2f })
        {
            var e = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(e.GetComponent<Collider>());
            e.transform.SetParent(head, false); e.transform.localScale = new Vector3(2.4f, 1f, 0.4f) * P; e.transform.localPosition = new Vector3(ex, -4.5f, 4.1f) * P;
            e.GetComponent<Renderer>().sharedMaterial = eyeM;
        }
        armL = Part(root, "ArmL", new Vector3(-5, 42, 0), new Vector3(2, 28, 2), dark);
        armR = Part(root, "ArmR", new Vector3(5, 42, 0), new Vector3(2, 28, 2), dark);
        legL = Part(root, "LegL", new Vector3(-2, 30, 0), new Vector3(2, 30, 2), dark);
        legR = Part(root, "LegR", new Vector3(2, 30, 0), new Vector3(2, 30, 2), dark);
    }

    public void SetState(string s)
    {
        if (s == state) return;
        state = s; stateT = 0;
    }

    void Update()
    {
        stateT += Time.deltaTime;
        bool run = state == "Run", walk = state == "Walk" || state == "Graze";
        float freq = (run ? 9.5f : walk ? 5f : 2f) * speedMul;
        phase += Time.deltaTime * freq;
        float sw = Mathf.Sin(phase) * (run ? 55f : walk ? 30f : 3f);

        switch (kind)
        {
            case Kind.Hero:
            case Kind.Heroine:
            case Kind.Zombie:
            case Kind.Skeleton:
            case Kind.Knight:
            case Kind.KnightGold:
            case Kind.Shade:
            {
                bool undead = kind == Kind.Zombie || kind == Kind.Skeleton;
                legL.localRotation = Quaternion.Euler(sw, 0, 0);
                legR.localRotation = Quaternion.Euler(-sw, 0, 0);
                if (state == "Jump") { legL.localRotation = Quaternion.Euler(-35, 0, 0); legR.localRotation = Quaternion.Euler(25, 0, 0); }
                Quaternion aL = Quaternion.Euler(-sw, 0, 0), aR = Quaternion.Euler(sw, 0, 0);
                if (undead) { aL = Quaternion.Euler(-90 + sw * 0.15f, 0, 0); aR = Quaternion.Euler(-90 - sw * 0.15f, 0, 0); }
                if (state == "Jump") { aL = Quaternion.Euler(-170, 0, -20); aR = Quaternion.Euler(-170, 0, 20); }
                if (state == "Punch")
                {
                    float k = Mathf.Repeat(stateT * 4f, 1f);
                    aR = Quaternion.Euler(-90 - Mathf.Sin(k * Mathf.PI) * 30f, 0, 0);
                    aL = Quaternion.Euler(-40, 0, 0);
                }
                armL.localRotation = aL; armR.localRotation = aR;
                head.localRotation = Quaternion.Euler(state == "HitReact" ? -30 : Mathf.Sin(phase * 0.5f) * 3f, 0, 0);
                body.localRotation = Quaternion.Euler(run ? 6f : 0f, 0, 0);
                break;
            }
            case Kind.Pig:
            case Kind.Cow:
            case Kind.Sheep:
            case Kind.PolarBear:
            case Kind.Wolf:
            case Kind.Ocelot:
                for (int i = 0; i < legs.Count; i++) legs[i].localRotation = Quaternion.Euler((i == 0 || i == 3 ? sw : -sw) * 0.8f, 0, 0);
                head.localRotation = Quaternion.Euler(state == "Graze" ? 35f + Mathf.Sin(phase * 2f) * 8f : Mathf.Sin(phase * 0.5f) * 5f, 0, 0);
                break;
            case Kind.Chicken:
                for (int i = 0; i < legs.Count; i++) legs[i].localRotation = Quaternion.Euler(i == 0 ? sw : -sw, 0, 0);
                armL.localRotation = Quaternion.Euler(0, 0, -Mathf.Abs(Mathf.Sin(phase * 2.5f)) * 40f);
                armR.localRotation = Quaternion.Euler(0, 0, Mathf.Abs(Mathf.Sin(phase * 2.5f)) * 40f);
                head.localRotation = Quaternion.Euler(Mathf.Max(0, Mathf.Sin(phase * 0.8f)) * 40f, 0, 0);
                break;
            case Kind.Spider:
                for (int i = 0; i < legs.Count; i++)
                {
                    float side = i < 4 ? -1 : 1;
                    legs[i].localRotation = Quaternion.Euler(0, Mathf.Sin(phase * 1.4f + i) * 20f, side * (-25f + Mathf.Sin(phase * 1.4f + i * 1.7f) * 12f));
                }
                break;
            case Kind.Slime:
            {
                float b = Mathf.Abs(Mathf.Sin(phase * 0.6f));
                body.localScale = new Vector3(1f + (1 - b) * 0.15f, 0.8f + b * 0.3f, 1f + (1 - b) * 0.15f);
                body.localPosition = new Vector3(0, (16 + b * 8f) * P, 0);
                break;
            }
        }
    }
}
