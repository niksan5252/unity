using System.Collections.Generic;
using UnityEngine;

// Minecraft-style creatures built from boxes (16 px per block proportions), animated in code:
// limb swing for walking/running, arms up for jumping, punches, zombie arms forward, quadrupeds, spider, slime.
public class BlockFigure : MonoBehaviour
{
    public enum Kind { Hero, Heroine, Zombie, Skeleton, Spider, Slime, Pig, Cow, Sheep, Chicken, Knight, KnightGold, Shade, PolarBear, Wolf, Ocelot,
        Creeper, Villager, Piglin, Drowned, WitherSkeleton, Bee, Turtle, Dolphin, Fish, Squid, Ghast, Strider, Horse, MagmaCube }
    public Kind kind;
    public string state = "Run";      // Run, Walk, Idle, Jump, Punch, HitReact, Graze
    public float speedMul = 1f;
    float phase, stateT;
    Transform head, body, armL, armR, legL, legR;
    readonly List<Transform> legs = new List<Transform>();   // quadrupeds / spider
    readonly List<Transform> extra = new List<Transform>();  // wings, tentacles, tails, fins (MQR #5 mobs)
    public Color tint = new Color(1f, 0.55f, 0.15f);          // a tropical fish's colour
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
            case Kind.Piglin:
            case Kind.Drowned:
            case Kind.WitherSkeleton:
            case Kind.Villager:
            {
                if (kind == Kind.Shade) { BuildShade(root); break; }
                if (kind == Kind.Villager) { BuildVillager(root); break; }
                if (kind == Kind.Piglin) { BuildPiglin(root); break; }
                bool knight = kind == Kind.Knight || kind == Kind.KnightGold;
                bool skel = kind == Kind.Skeleton || kind == Kind.WitherSkeleton, girl = kind == Kind.Heroine;
                Color skin = kind == Kind.Zombie ? new Color(0.32f, 0.55f, 0.28f) : kind == Kind.Drowned ? new Color(0.3f, 0.55f, 0.55f) : kind == Kind.WitherSkeleton ? new Color(0.17f, 0.17f, 0.19f)
                           : skel ? new Color(0.78f, 0.78f, 0.78f) : girl ? new Color(0.91f, 0.7f, 0.55f) : new Color(0.77f, 0.59f, 0.43f);
                Color shirt = kind == Kind.Hero ? new Color(0.0f, 0.68f, 0.68f) : girl ? new Color(0.45f, 0.72f, 0.3f) : kind == Kind.Zombie ? new Color(0.0f, 0.62f, 0.62f)
                            : kind == Kind.Drowned ? new Color(0.35f, 0.62f, 0.5f) : skin;
                Color pants = kind == Kind.Hero ? new Color(0.29f, 0.25f, 0.6f) : girl ? new Color(0.45f, 0.32f, 0.2f) : kind == Kind.Zombie ? new Color(0.27f, 0.22f, 0.55f)
                            : kind == Kind.Drowned ? new Color(0.22f, 0.32f, 0.5f) : skin;
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
                Face(head, 8, kind == Kind.Hero || kind == Kind.Knight ? "face_hero" : girl || kind == Kind.KnightGold ? "face_heroine" : kind == Kind.Zombie ? "face_zombie"
                    : kind == Kind.Drowned ? "face_drowned" : kind == Kind.WitherSkeleton ? "face_wither" : "face_skeleton", -4);
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
            case Kind.Horse:
            {
                if (kind == Kind.Horse) { BuildHorse(root); break; }
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
            case Kind.Creeper: BuildCreeper(root); break;
            case Kind.Bee: BuildBee(root); break;
            case Kind.Turtle: BuildTurtle(root); break;
            case Kind.Dolphin: BuildDolphin(root); break;
            case Kind.Fish: BuildFish(root); break;
            case Kind.Squid: BuildSquid(root); break;
            case Kind.Ghast: BuildGhast(root); break;
            case Kind.Strider: BuildStrider(root); break;
            case Kind.MagmaCube:
            case Kind.Slime:
            {
                var g = kind == Kind.MagmaCube ? new Color(0.42f, 0.12f, 0.08f) : new Color(0.45f, 0.8f, 0.35f);
                body = Part(root, "Body", new Vector3(0, 16, 0), new Vector3(16, 16, 16), g);
                var m = M(new Color(0.45f, 0.8f, 0.35f, 1f));
                head = body;
                Face(body, 16, kind == Kind.MagmaCube ? "face_magma" : "face_slime", -8);
                if (kind == Kind.MagmaCube)
                    for (int i = 0; i < 3; i++) Box(body, new Vector3(0, -3 - i * 4.5f, 0), new Vector3(16.3f, 1.2f, 16.3f), WorldBuilder.Emissive(new Color(1f, 0.45f, 0.05f), 2.5f));
                break;
            }
        }
    }

    // ---- MQR #5 (Minecraft Quest 2) mobs: our own blocky look-alikes, adult proportions only ----

    // a box centred at posPx inside `parent` (no pivot); a Material or a colour
    Transform Box(Transform parent, Vector3 posPx, Vector3 sizePx, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false); g.transform.localScale = sizePx * P; g.transform.localPosition = posPx * P;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g.transform;
    }
    Transform Box(Transform parent, Vector3 posPx, Vector3 sizePx, Color c) => Box(parent, posPx, sizePx, M(c));

    void BuildCreeper(Transform root)
    {
        var g = new Color(0.38f, 0.75f, 0.3f);
        body = Part(root, "Body", new Vector3(0, 18, 0), new Vector3(8, 12, 4), g);
        foreach (var (y, z) in new[] { (-3f, 2.05f), (-8f, 2.05f), (-5f, -2.05f), (-10f, -2.05f) })
            Box(body, new Vector3(1.5f - (int)(y * 7) % 3, y, z), new Vector3(3, 2, 0.2f), new Color(0.27f, 0.6f, 0.22f));   // the mottled skin
        head = Part(root, "Head", new Vector3(0, 26, 0), new Vector3(8, 8, 8), g);
        Face(head, 8, "face_creeper", -4);
        foreach (var (x, z) in new[] { (-2f, 4f), (2f, 4f), (-2f, -4f), (2f, -4f) })
            legs.Add(Part(root, "Leg", new Vector3(x, 6, z), new Vector3(4, 6, 4), new Color(0.33f, 0.68f, 0.27f)));
    }

    // an adult villager: long brown robe, arms folded in front, big nose
    void BuildVillager(Transform root)
    {
        var robe = new Color(0.47f, 0.32f, 0.2f); var skin = new Color(0.74f, 0.55f, 0.41f);
        body = Part(root, "Body", new Vector3(0, 24, 0), new Vector3(8, 12, 6), robe);
        Box(body, new Vector3(0, -10, 0), new Vector3(8.3f, 6, 6.3f), robe);                        // robe over the legs
        head = Part(root, "Head", new Vector3(0, 34, 0), new Vector3(8, 10, 8), skin);
        Face(head, 8, "face_villager", -4);
        Box(head, new Vector3(0, -7, 5f), new Vector3(2, 4, 2), new Color(0.66f, 0.47f, 0.35f));    // the nose
        armL = Part(root, "ArmsFolded", new Vector3(0, 21, 3.5f), new Vector3(10, 4, 4), robe, 0.5f);
        legL = Part(root, "LegL", new Vector3(-2, 12, 0), new Vector3(4, 12, 4), new Color(0.38f, 0.26f, 0.16f));
        legR = Part(root, "LegR", new Vector3(2, 12, 0), new Vector3(4, 12, 4), new Color(0.38f, 0.26f, 0.16f));
        armR = armL;
    }

    // piglin: wide pink snout head with ears, leather tunic, a gold sword
    void BuildPiglin(Transform root)
    {
        var skin = new Color(0.9f, 0.6f, 0.56f); var tunic = new Color(0.52f, 0.36f, 0.2f);
        body = Part(root, "Body", new Vector3(0, 24, 0), new Vector3(8, 12, 4), tunic);
        Box(body, new Vector3(0, -11, 0), new Vector3(8.3f, 2, 4.3f), new Color(0.95f, 0.78f, 0.2f));   // gold belt
        head = Part(root, "Head", new Vector3(0, 32, 0), new Vector3(10, 8, 8), skin);
        Face(head, 8, "face_piglin", -4);
        Box(head, new Vector3(0, -5.5f, 4.6f), new Vector3(4, 3, 1.5f), new Color(0.82f, 0.5f, 0.47f));   // snout
        foreach (var ex in new[] { -5.5f, 5.5f }) Box(head, new Vector3(ex, -3, 0), new Vector3(1, 5, 3), skin).localRotation = Quaternion.Euler(0, 0, ex * 4f);
        armL = Part(root, "ArmL", new Vector3(-6, 24, 0), new Vector3(4, 12, 4), skin);
        armR = Part(root, "ArmR", new Vector3(6, 24, 0), new Vector3(4, 12, 4), skin);
        var sword = Box(armR, new Vector3(0, -12, 4), new Vector3(1, 1, 12), WorldBuilder.Emissive(new Color(1f, 0.82f, 0.2f), 0.6f));
        sword.localRotation = Quaternion.Euler(-20, 0, 0);
        legL = Part(root, "LegL", new Vector3(-2, 12, 0), new Vector3(4, 12, 4), new Color(0.42f, 0.28f, 0.16f));
        legR = Part(root, "LegR", new Vector3(2, 12, 0), new Vector3(4, 12, 4), new Color(0.42f, 0.28f, 0.16f));
    }

    void BuildHorse(Transform root)
    {
        var c = new Color(0.6f, 0.4f, 0.24f); var mane = new Color(0.25f, 0.16f, 0.1f);
        body = Part(root, "Body", new Vector3(0, 24, 0), new Vector3(10, 10, 22), c);
        head = Part(root, "Head", new Vector3(0, 32, 12), new Vector3(6, 6, 12), c, 0.2f);
        head.GetChild(0).localPosition = new Vector3(0, -1 * P, 3 * P);
        Box(head, new Vector3(0, 1.5f, -2), new Vector3(2, 8, 8), mane);
        Face(head, 6, "face_horse", -1);
        head.GetChild(head.childCount - 1).localPosition = new Vector3(0, -1 * P, 9.05f * P);
        head.localRotation = Quaternion.Euler(30, 0, 0);
        var tail = Box(body, new Vector3(0, -4, -12), new Vector3(3, 10, 3), mane); tail.localRotation = Quaternion.Euler(-25, 0, 0);
        extra.Add(tail);
        foreach (var (x, z) in new[] { (-3f, 8f), (3f, 8f), (-3f, -8f), (3f, -8f) })
            legs.Add(Part(root, "Leg", new Vector3(x, 14, z), new Vector3(4, 14, 4), c));
    }

    void BuildBee(Transform root)
    {
        var y = new Color(0.97f, 0.78f, 0.2f); var b = new Color(0.25f, 0.17f, 0.1f);
        body = Part(root, "Body", new Vector3(0, 7, 0), new Vector3(7, 7, 10), y);
        foreach (var z in new[] { -1.5f, 2f }) Box(body, new Vector3(0, -3.5f, z), new Vector3(7.2f, 7.2f, 1.5f), b);
        head = body;
        Face(body, 7, "face_bee", -3.5f);
        body.GetChild(body.childCount - 1).localPosition = new Vector3(0, -3.5f * P, 5.05f * P);
        var wing = WorldBuilder.Emissive(new Color(0.85f, 0.95f, 1f), 0.3f);
        foreach (var x in new[] { -2f, 2f })
        {
            var w = new GameObject("Wing").transform; w.SetParent(body, false); w.localPosition = new Vector3(x, 0, 0) * P;
            Box(w, new Vector3(x * 1.5f, 0.2f, 0), new Vector3(5, 0.3f, 4), wing);
            extra.Add(w);
        }
    }

    void BuildTurtle(Transform root)
    {
        var shell = new Color(0.3f, 0.5f, 0.24f); var skin = new Color(0.45f, 0.68f, 0.38f);
        body = Part(root, "Shell", new Vector3(0, 7, 0), new Vector3(14, 5, 16), shell);
        Box(body, new Vector3(0, -0.5f, 0), new Vector3(10, 6, 12), new Color(0.38f, 0.58f, 0.3f));
        head = Part(root, "Head", new Vector3(0, 6, 10), new Vector3(6, 5, 5), skin);
        Face(head, 5, "face_turtle", -2.5f);
        foreach (var (x, z) in new[] { (-6f, 6f), (6f, 6f), (-5f, -6f), (5f, -6f) })
            legs.Add(Part(root, "Flipper", new Vector3(x, 3, z), new Vector3(4, 3, 4), skin));
    }

    void BuildDolphin(Transform root)
    {
        var c = new Color(0.45f, 0.62f, 0.8f); var belly = new Color(0.85f, 0.9f, 0.95f);
        body = Part(root, "Body", new Vector3(0, 7, 0), new Vector3(8, 7, 16), c);
        Box(body, new Vector3(0, -6.2f, 1), new Vector3(7, 1, 12), belly);
        Box(body, new Vector3(0, 0, 0), new Vector3(1, 5, 4), c).localRotation = Quaternion.Euler(-30, 0, 0);       // dorsal fin
        head = Part(root, "Head", new Vector3(0, 6, 10), new Vector3(7, 6, 5), c);
        Box(head, new Vector3(0, -4.5f, 4), new Vector3(3, 2, 4), belly);                                           // snout
        Face(head, 6, "face_dolphin", -3);
        head.GetChild(head.childCount - 1).localPosition = new Vector3(0, -3f * P, 2.55f * P);
        var tail = new GameObject("Tail").transform; tail.SetParent(root, false); tail.localPosition = new Vector3(0, 3.5f, -8) * P;
        Box(tail, new Vector3(0, 0, -3), new Vector3(4, 3, 6), c); Box(tail, new Vector3(0, 0, -6.5f), new Vector3(10, 1, 3), c);
        extra.Add(tail);
    }

    void BuildFish(Transform root)
    {
        var c = tint; var w = new Color(0.98f, 0.98f, 0.98f);
        body = Part(root, "Body", new Vector3(0, 4, 0), new Vector3(2, 5, 7), c);
        Box(body, new Vector3(0, -2.5f, 0.5f), new Vector3(2.2f, 5.2f, 1.5f), w);
        Box(body, new Vector3(0, -1.5f, 3.4f), new Vector3(2.4f, 1, 1), new Color(0.05f, 0.05f, 0.05f));
        head = body;
        var tail = new GameObject("Tail").transform; tail.SetParent(root, false); tail.localPosition = new Vector3(0, 1.5f, -3.5f) * P;
        Box(tail, new Vector3(0, 0, -1.5f), new Vector3(1, 4, 3), c);
        extra.Add(tail);
    }

    void BuildSquid(Transform root)
    {
        var c = new Color(0.2f, 0.32f, 0.55f);
        body = Part(root, "Body", new Vector3(0, 26, 0), new Vector3(12, 16, 12), c);
        head = body;
        foreach (var x in new[] { -3f, 3f }) Box(body, new Vector3(x, -12, 6.1f), new Vector3(2, 2, 0.3f), new Color(0.95f, 0.95f, 0.95f));
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            var t = Part(root, "Tentacle", new Vector3(Mathf.Cos(a) * 4.5f, 10, Mathf.Sin(a) * 4.5f), new Vector3(2, 14, 2), c);
            extra.Add(t);
        }
    }

    void BuildGhast(Transform root)
    {
        var w = new Color(0.94f, 0.94f, 0.95f);
        body = Part(root, "Body", new Vector3(0, 40, 0), new Vector3(16, 16, 16), w);
        head = body;
        Face(body, 16, "face_ghast", -8);
        for (int i = 0; i < 9; i++)
        {
            var t = Part(root, "Tentacle", new Vector3(-5 + (i % 3) * 5, 24, -5 + (i / 3) * 5), new Vector3(2, 10 + (i * 7) % 6, 2), w);
            extra.Add(t);
        }
    }

    void BuildStrider(Transform root)
    {
        var c = new Color(0.68f, 0.2f, 0.22f);
        body = Part(root, "Body", new Vector3(0, 32, 0), new Vector3(16, 16, 16), c);
        head = body;
        Face(body, 16, "face_strider", -8);
        for (int i = 0; i < 6; i++)
            extra.Add(Box(body, new Vector3(-6 + i * 2.4f, 2, 0), new Vector3(1, 6 + i % 3 * 2, 1), new Color(0.55f, 0.15f, 0.15f)));   // the hair strands
        legL = Part(root, "LegL", new Vector3(-4, 16, 0), new Vector3(3, 16, 3), new Color(0.45f, 0.42f, 0.5f));
        legR = Part(root, "LegR", new Vector3(4, 16, 0), new Vector3(3, 16, 3), new Color(0.45f, 0.42f, 0.5f));
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
            case Kind.Piglin:
            case Kind.Drowned:
            case Kind.WitherSkeleton:
            case Kind.Villager:
            {
                if (kind == Kind.Villager)
                {
                    legL.localRotation = Quaternion.Euler(sw * 0.7f, 0, 0); legR.localRotation = Quaternion.Euler(-sw * 0.7f, 0, 0);
                    head.localRotation = Quaternion.Euler(0, Mathf.Sin(phase * 0.3f) * 20f, 0);
                    break;
                }
                bool undead = kind == Kind.Zombie || kind == Kind.Skeleton || kind == Kind.Drowned;
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
            case Kind.Horse:
            case Kind.Turtle:
            case Kind.Creeper:
                for (int i = 0; i < legs.Count; i++) legs[i].localRotation = Quaternion.Euler((i == 0 || i == 3 ? sw : -sw) * 0.8f, 0, 0);
                if (kind == Kind.Horse) { head.localRotation = Quaternion.Euler((state == "Graze" ? 70f : 30f) + Mathf.Sin(phase * 0.5f) * 5f, 0, 0); if (extra.Count > 0) extra[0].localRotation = Quaternion.Euler(-25, Mathf.Sin(phase * 0.7f) * 15f, 0); break; }
                if (kind == Kind.Creeper) { head.localRotation = Quaternion.Euler(0, Mathf.Sin(phase * 0.3f) * 10f, 0); break; }
                head.localRotation = Quaternion.Euler(state == "Graze" ? 35f + Mathf.Sin(phase * 2f) * 8f : Mathf.Sin(phase * 0.5f) * 5f, 0, 0);
                break;
            case Kind.Bee:
                foreach (var w in extra) w.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 60f) * 35f * Mathf.Sign(w.localPosition.x));
                body.localPosition = new Vector3(0, (7f + Mathf.Sin(phase * 0.8f) * 1.5f) * P, 0);
                break;
            case Kind.Dolphin:
            case Kind.Fish:
                foreach (var t in extra) t.localRotation = Quaternion.Euler(kind == Kind.Dolphin ? Mathf.Sin(phase * 1.2f) * 20f : 0, kind == Kind.Fish ? Mathf.Sin(phase * 2f) * 30f : 0, 0);
                break;
            case Kind.Squid:
            case Kind.Ghast:
                for (int i = 0; i < extra.Count; i++) extra[i].localRotation = Quaternion.Euler(Mathf.Sin(phase * 0.6f + i) * 18f, 0, Mathf.Cos(phase * 0.5f + i * 1.3f) * 14f);
                body.localPosition = new Vector3(0, ((kind == Kind.Ghast ? 40f : 26f) + Mathf.Sin(phase * 0.3f) * 2f) * P, 0);
                break;
            case Kind.Strider:
                legL.localRotation = Quaternion.Euler(sw * 0.6f, 0, 0); legR.localRotation = Quaternion.Euler(-sw * 0.6f, 0, 0);
                body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(phase) * 4f);
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
            case Kind.MagmaCube:
            {
                float b = Mathf.Abs(Mathf.Sin(phase * 0.6f));
                body.localScale = new Vector3(1f + (1 - b) * 0.15f, 0.8f + b * 0.3f, 1f + (1 - b) * 0.15f);
                body.localPosition = new Vector3(0, (16 + b * 8f) * P, 0);
                break;
            }
        }
    }
}
