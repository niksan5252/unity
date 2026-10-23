using System.Collections.Generic;
using UnityEngine;
using static SpookyWorld;

// Red Light Green Light Quest (Move Quest Run #4): sunny playground, giant toy store, candy factory.
// A giant traffic-light robot (FreezeBot) stands far ahead: it turns around on every FREEZE cue.
// Geometry helpers come from SpookyWorld (Box / Cyl / Glow / Lit / Tex / Critter ...).
public static class ToyWorld
{
    static RunLibrary lib;
    static System.Random rnd;

    public static readonly Color Red = new Color(1f, 0.25f, 0.25f), Blue = new Color(0.2f, 0.55f, 1f), Yellow = new Color(1f, 0.85f, 0.15f),
        Green = new Color(0.3f, 0.85f, 0.3f), Violet = new Color(0.65f, 0.4f, 1f), Peach = new Color(1f, 0.6f, 0.35f);
    public static readonly Color[] Toy = { Red, Blue, Yellow, Green, Violet, Peach };
    static readonly string[] Kids = { "character-a", "character-b", "character-c", "character-d", "character-e", "character-f", "character-g", "character-h", "character-i", "character-j", "character-k", "character-l" };

    public static Palette Pal(RunTheme t)
    {
        switch (t)
        {
            case RunTheme.RlToyStore:
                return new Palette
                {
                    top = new Color(1f, 0.85f, 0.7f), mid = new Color(1f, 0.88f, 0.75f), horizon = new Color(1f, 0.9f, 0.78f),
                    ambient = new Color(0.82f, 0.78f, 0.78f), sun = new Color(1f, 0.95f, 0.85f), sunIntensity = 0.9f, sunEuler = new Vector3(55f, 25f, 0),
                    moon = Color.white, moonDir = new Vector3(0, -1, 0), moonSize = 0.001f, cloud = Color.white, clouds = 0f, fogStart = 45f, fogEnd = 210f,
                };
            case RunTheme.RlCandyFactory:
                return new Palette
                {
                    top = new Color(0.55f, 0.35f, 0.9f), mid = new Color(1f, 0.55f, 0.8f), horizon = new Color(1f, 0.85f, 0.78f),
                    ambient = new Color(0.86f, 0.74f, 0.84f), sun = new Color(1f, 0.92f, 0.86f), sunIntensity = 1.2f, sunEuler = new Vector3(45f, 25f, 0),
                    moon = new Color(1f, 0.95f, 0.8f), moonDir = new Vector3(0.45f, 0.5f, 1f), moonSize = 0.05f,
                    cloud = new Color(1f, 0.9f, 0.96f), clouds = 0.75f, fogStart = 55f, fogEnd = 240f,
                };
            default:   // playground
                return new Palette
                {
                    top = new Color(0.16f, 0.42f, 0.95f), mid = new Color(0.42f, 0.7f, 1f), horizon = new Color(0.84f, 0.93f, 1f),
                    ambient = new Color(0.74f, 0.78f, 0.86f), sun = new Color(1f, 0.96f, 0.88f), sunIntensity = 1.35f, sunEuler = new Vector3(45f, 30f, 0),
                    moon = new Color(1f, 0.97f, 0.85f), moonDir = new Vector3(0.5f, 0.55f, 1f), moonSize = 0.05f,
                    cloud = Color.white, clouds = 0.7f, fogStart = 60f, fogEnd = 260f,
                };
        }
    }

    static GameObject P(string n, Vector3 pos, float rot, float h, Transform root) => CandyWorld.Put(lib, n, pos, rot, h, root);
    static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
    static T Pick<T>(T[] a) => a[rnd.Next(a.Length)];
    static Color C() => Toy[rnd.Next(Toy.Length)];

    public static void Build(RunLibrary l, RunTheme theme, Transform root, float length, System.Random r)
    {
        lib = l; rnd = r;
        SpookyWorld.Use(l, r);
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
                dl.shadowStrength = 0.55f;
            }
        switch (theme)
        {
            case RunTheme.RlToyStore: Store(root, length, L, half); break;
            case RunTheme.RlCandyFactory: Factory(root, length, L, half); break;
            default: Playground(root, length, L, half); break;
        }
        var fx = root.gameObject.AddComponent<ToyFx>();
        fx.Setup(lib, theme, length);
    }

    // a row of cheering kids (Kenney blocky characters) facing the road
    static void Crowd(Transform dyn, float length, int s, float x0, float x1, float every, params string[] clips)
    {
        for (float z = 4; z < length + 100; z += every + (float)rnd.NextDouble() * every)
            Critter(lib, Pick(Kids), new Vector3(s * R(x0, x1), 0, z), -s * R(110f, 150f), R(1.1f, 1.4f), dyn, clips);
    }

    static bool NearBoard(float z) => Mathf.Abs(Mathf.Repeat(z - 30f + 24f, 48f) - 24f) < 5.5f;

    // RED LIGHT / GREEN LIGHT billboards beside the road, turned toward the camera
    // (nothing spans the road: the FreezeBot glides along it 60 m ahead)
    static void Billboards(Transform root, float length, float every, Material pole, float x)
    {
        int k = 0;
        for (float z = 30; z < length + 100; z += every, k++)
        {
            int s = k % 2 == 0 ? -1 : 1;
            var rot = Quaternion.Euler(0, s * 35f, 0);
            var c = new Vector3(s * x, 4.2f, z);
            foreach (int e in new[] { -1, 1 })
                Box(root, new Vector3(c.x, 1.6f, c.z) + rot * new Vector3(e * 2.6f, 0, 0.1f), new Vector3(0.25f, 3.2f, 0.25f), pole);
            var back = Box(root, c + rot * new Vector3(0, 0, 0.12f), new Vector3(6.4f, 1.55f, 0.2f), Lit(new Color(0.1f, 0.1f, 0.18f)));
            back.transform.rotation = rot;
            Quad(root, c, rot, new Vector2(6.2f, 1.16f), Tex("rlglsign", 1, 1, Color.white, 1.0f, 0.3f));
        }
    }

    // =====================================================================
    // PLAYGROUND: running track through a sunny park - slides, swings, seesaws, merry-go-rounds,
    // trampolines, kids cheering behind the fence, houses and kites
    static void Playground(Transform root, float length, float L, float half)
    {
        var dyn = WorldBuilder.Dyn;
        Box(root, new Vector3(0, -0.1f, half), new Vector3(9f, 0.2f, L), Tex("track", 1, L / 8, Color.white, 0, 0.25f));
        Box(root, new Vector3(0, -0.15f, half), new Vector3(420f, 0.2f, L + 300), Tex("grass", 100, (L + 300) / 6, Color.white, 0, 0.1f));
        var white = Lit(Color.white, 0.5f);
        string[] trees = { "tree_default", "tree_oak", "tree_detailed", "tree_fat", "tree_small", "tree_cone", "tree_blocks" };
        string[] flowers = { "flower_purpleA", "flower_redA", "flower_yellowA", "flower_purpleB", "flower_redB", "flower_yellowB", "plant_bush", "plant_bushDetailed" };
        foreach (int s in new[] { -1, 1 })
        {
            Box(root, new Vector3(s * 4.6f, 0.05f, half), new Vector3(0.2f, 0.12f, L), white);
            // picket fence in toy colours
            Box(root, new Vector3(s * 5.3f, 0.45f, half), new Vector3(0.08f, 0.1f, L), white, false);
            Box(root, new Vector3(s * 5.3f, 0.8f, half), new Vector3(0.08f, 0.1f, L), white, false);
            for (float z = -20; z < length + 100; z += 1.2f)
                Box(root, new Vector3(s * 5.3f, 0.55f, z), new Vector3(0.1f, 1.1f, 0.35f), Lit(Toy[((int)(z / 1.2f) % Toy.Length + Toy.Length) % Toy.Length], 0.4f), false);
            Crowd(dyn, length, s, 5.9f, 7.2f, 3.5f, "emote-yes", "idle");
            for (float z = -10; z < length + 120; z += 1.6f + (float)rnd.NextDouble() * 2f)
                P(Pick(flowers), new Vector3(s * R(7.5f, 10f), 0, z), rnd.Next(360), R(0.5f, 1.1f), root);
            // playground equipment
            for (float z = 6 + rnd.Next(8); z < length + 120; z += 9f + rnd.Next(7))
            {
                if (NearBoard(z)) continue;
                var p = new Vector3(s * R(11.5f, 17f), 0, z);
                switch (rnd.Next(6))
                {
                    case 0: Slide(root, p, s); break;
                    case 1: Swings(root, dyn, p); break;
                    case 2: Seesaw(root, dyn, p); break;
                    case 3: MerryGoRound(root, dyn, p); break;
                    case 4: Trampoline(root, dyn, p); break;
                    default: ClimbDome(root, p); break;
                }
            }
            for (float z = -10; z < length + 160; z += 6f + rnd.Next(6))
                P(Pick(trees), new Vector3(s * R(20f, 34f), 0, z), rnd.Next(360), R(6f, 10f), root);
            if (lib.suburban != null && lib.suburban.Length > 0)
                WorldBuilder.BuildingRow(lib.suburban, root, length, rnd, s, 38f, 4.5f);
        }
        Billboards(root, length, 48f, Lit(Blue, 0.5f), 9f);
    }

    static void Slide(Transform root, Vector3 p, int s)
    {
        var c = C(); var c2 = C();
        var frame = Lit(c2, 0.5f);
        foreach (float dx in new[] { -0.8f, 0.8f })
            foreach (float dz in new[] { -0.8f, 0.8f })
                Box(root, p + new Vector3(dx, 1.4f, dz), new Vector3(0.18f, 2.8f, 0.18f), frame);
        Box(root, p + new Vector3(0, 2.6f, 0), new Vector3(1.8f, 0.15f, 1.8f), Lit(Yellow, 0.4f));
        ConeObj(root, p + new Vector3(0, 2.8f, 0), 2.6f, 1.4f, Lit(c, 0.4f), 4, 45);
        for (int i = 0; i < 6; i++) Box(root, p + new Vector3(0, 0.4f + i * 0.4f, 1.0f), new Vector3(1.4f, 0.08f, 0.08f), frame, false);
        var chute = Box(root, p + new Vector3(0, 1.35f, -2.4f), new Vector3(1.2f, 0.12f, 3.6f), Lit(c, 0.8f));
        chute.transform.rotation = Quaternion.Euler(-38f, 0, 0);
    }

    static void Swings(Transform root, Transform dyn, Vector3 p)
    {
        var frame = Lit(C(), 0.5f);
        foreach (float dz in new[] { -2.2f, 2.2f })
            foreach (int sx in new[] { -1, 1 })
            {
                var leg = Box(root, p + new Vector3(sx * 0.6f, 1.6f, dz), new Vector3(0.15f, 3.4f, 0.15f), frame);
                leg.transform.rotation = Quaternion.Euler(0, 0, sx * 12f);
            }
        Box(root, p + new Vector3(0, 3.2f, 0), new Vector3(0.18f, 0.18f, 4.6f), frame);
        for (int i = 0; i < 2; i++)
        {
            var pivot = new GameObject("Swing").transform; pivot.SetParent(dyn); pivot.position = p + new Vector3(0, 3.1f, -1f + i * 2f);
            var rope = Lit(new Color(0.3f, 0.25f, 0.2f));
            foreach (float dz in new[] { -0.3f, 0.3f }) Box(pivot, pivot.position + new Vector3(0, -1.2f, dz), new Vector3(0.04f, 2.4f, 0.04f), rope, false);
            Box(pivot, pivot.position + new Vector3(0, -2.4f, 0), new Vector3(0.45f, 0.08f, 0.75f), Lit(C(), 0.5f), false);
            var kid = Critter(lib, Pick(Kids), pivot.position + new Vector3(0, -2.4f, 0), Mathf.Sign(p.x) > 0 ? -90 : 90, 1.0f, pivot, "sit", "idle");
            if (kid) kid.transform.position = pivot.position + new Vector3(0, -2.75f, 0);
            var sw = pivot.gameObject.AddComponent<Sway>(); sw.axis = Vector3.forward; sw.angle = 38f; sw.speed = 2.6f;
        }
    }

    static void Seesaw(Transform root, Transform dyn, Vector3 p)
    {
        Box(root, p + new Vector3(0, 0.35f, 0), new Vector3(0.6f, 0.7f, 0.6f), Lit(C(), 0.5f));
        var pivot = new GameObject("Seesaw").transform; pivot.SetParent(dyn); pivot.position = p + new Vector3(0, 0.75f, 0);
        Box(pivot, pivot.position, new Vector3(0.4f, 0.12f, 4.6f), Lit(C(), 0.5f), false);
        foreach (int e in new[] { -1, 1 })
        {
            Box(pivot, pivot.position + new Vector3(0, 0.35f, e * 1.9f), new Vector3(0.6f, 0.08f, 0.08f), Lit(Yellow, 0.6f), false);
            var kid = Critter(lib, Pick(Kids), pivot.position + new Vector3(0, 0.05f, e * 2.05f), e > 0 ? 180 : 0, 1.0f, pivot, "sit", "idle");
            if (kid) kid.transform.position = pivot.position + new Vector3(0, -0.2f, e * 2.05f);
        }
        var sw = pivot.gameObject.AddComponent<Sway>(); sw.axis = Vector3.right; sw.angle = 16f; sw.speed = 2.2f;
    }

    static void MerryGoRound(Transform root, Transform dyn, Vector3 p)
    {
        var top = new GameObject("MerryGoRound").transform; top.SetParent(dyn); top.position = p;
        Cyl(top, p + Vector3.up * 0.2f, 4f, 0.2f, Lit(C(), 0.5f));
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3;
            var hp = p + new Vector3(Mathf.Cos(a) * 1.6f, 0.7f, Mathf.Sin(a) * 1.6f);
            Box(top, hp, new Vector3(0.08f, 0.9f, 0.08f), Lit(Toy[i % Toy.Length], 0.6f), false);
        }
        Cyl(top, p + Vector3.up * 0.8f, 0.2f, 1.2f, Lit(Yellow, 0.7f));
        for (int i = 0; i < 2; i++)
        {
            float a = i * Mathf.PI;
            Critter(lib, Pick(Kids), p + new Vector3(Mathf.Cos(a) * 1.2f, 0.3f, Mathf.Sin(a) * 1.2f), rnd.Next(360), 1.0f, top, "emote-yes", "idle");
        }
        var spin = top.gameObject.AddComponent<Spin>(); spin.axis = Vector3.up; spin.speed = 70f;
    }

    static void Trampoline(Transform root, Transform dyn, Vector3 p)
    {
        Cyl(root, p + Vector3.up * 0.55f, 3.6f, 0.12f, Lit(Blue, 0.5f));
        Cyl(root, p + Vector3.up * 0.58f, 3.1f, 0.06f, Lit(new Color(0.1f, 0.1f, 0.12f), 0.3f), null, false);
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI / 2 + 0.4f;
            Box(root, p + new Vector3(Mathf.Cos(a) * 1.6f, 0.28f, Mathf.Sin(a) * 1.6f), new Vector3(0.1f, 0.56f, 0.1f), Lit(Color.gray), false);
        }
        var kid = Critter(lib, Pick(Kids), p + Vector3.up * 0.62f, rnd.Next(360), 1.15f, dyn, "emote-yes", "idle");
        if (kid) { var b = kid.AddComponent<Bob>(); b.height = 0.9f; b.speed = 5.5f; b.phase = (float)rnd.NextDouble() * 6f; kid.transform.position += Vector3.up * 0.9f; }
    }

    static void ClimbDome(Transform root, Vector3 p)
    {
        for (int ring = 0; ring < 4; ring++)
        {
            float e = ring * 0.38f, rr = 2.2f * Mathf.Cos(e), y = 2.2f * Mathf.Sin(e);
            int n = 10;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n, a1 = (i + 1) * Mathf.PI * 2 / n;
                var p0 = p + new Vector3(Mathf.Cos(a) * rr, y + 0.05f, Mathf.Sin(a) * rr);
                var p1 = p + new Vector3(Mathf.Cos(a1) * rr, y + 0.05f, Mathf.Sin(a1) * rr);
                var bar = Box(root, (p0 + p1) * 0.5f, new Vector3(0.09f, (p1 - p0).magnitude, 0.09f), Lit(Toy[(ring + i) % Toy.Length], 0.6f), false);
                bar.transform.rotation = Quaternion.FromToRotation(Vector3.up, p1 - p0);
                if (ring < 3)
                {
                    float e2 = (ring + 1) * 0.38f;
                    var p2 = p + new Vector3(Mathf.Cos(a) * 2.2f * Mathf.Cos(e2), 2.2f * Mathf.Sin(e2) + 0.05f, Mathf.Sin(a) * 2.2f * Mathf.Cos(e2));
                    var up = Box(root, (p0 + p2) * 0.5f, new Vector3(0.09f, (p2 - p0).magnitude, 0.09f), Lit(Toy[(ring + i + 2) % Toy.Length], 0.6f), false);
                    up.transform.rotation = Quaternion.FromToRotation(Vector3.up, p2 - p0);
                }
            }
        }
    }

    // =====================================================================
    // TOY STORE: a giant toy shop - towering shelves of toys, letter-block towers, teddy bears,
    // spinning tops, rocking horses, marching toy soldiers, toy trains racing along the aisle
    static void Store(Transform root, float length, float L, float half)
    {
        var dyn = WorldBuilder.Dyn;
        Box(root, new Vector3(0, -0.1f, half), new Vector3(9f, 0.2f, L), Tex("checker", 2.25f, L / 4, Color.white, 0, 0.75f));
        Box(root, new Vector3(0, -0.15f, half), new Vector3(60f, 0.2f, L + 200), Tex("checker", 15, (L + 200) / 4, new Color(0.85f, 0.85f, 0.9f), 0, 0.6f));
        var ceil = Box(root, new Vector3(0, 24f, half), new Vector3(60f, 0.4f, L + 200), Lit(new Color(1f, 0.92f, 0.8f), 0.2f));
        ceil.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // the sun still lights the shop
        foreach (int s in new[] { -1, 1 })
        {
            var wall = Box(root, new Vector3(s * 28f, 12f, half), new Vector3(0.4f, 24f, L + 200), Lit(s < 0 ? new Color(0.65f, 0.85f, 1f) : new Color(1f, 0.8f, 0.85f), 0.2f));
            wall.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Box(root, new Vector3(s * 4.6f, 0.05f, half), new Vector3(0.2f, 0.12f, L), Glow(Yellow, 1.5f, true), false);
            // rails for the toy trains (ToyFx runs them at x = +-6)
            foreach (float dx in new[] { -0.55f, 0.55f })
                Box(root, new Vector3(s * 6f + dx, 0.06f, half), new Vector3(0.1f, 0.12f, L), Lit(new Color(0.6f, 0.6f, 0.65f), 0.7f, 0.6f), false);
            for (float z = -20; z < length + 100; z += 1.5f)
                Box(root, new Vector3(s * 6f, 0.02f, z), new Vector3(1.6f, 0.06f, 0.3f), Lit(new Color(0.55f, 0.35f, 0.2f)), false);
            // shelves full of toys
            for (float z = -10; z < length + 120; z += 7f)
                Shelf(root, new Vector3(s * 11.5f, 0, z), s);
            // spinning tops, rocking horses and big teddies on the floor in front of the shelves
            for (float z = 5 + rnd.Next(6); z < length + 100; z += 8f + rnd.Next(8))
            {
                var p = new Vector3(s * R(8.4f, 8.8f), 0, z);
                switch (rnd.Next(4))
                {
                    case 0: Top(dyn, p); break;
                    case 1: RockingHorse(dyn, p, s); break;
                    case 2: Teddy(root, p + new Vector3(s * 0.3f, 0, 0), R(1.8f, 2.4f), s); break;
                    default: BlockTower(root, p, rnd.Next(2, 5)); break;
                }
            }
            // marching toy soldiers
            for (float z = 20 + rnd.Next(30); z < length + 100; z += 55f + rnd.Next(25))
                for (int k = 0; k < 5; k++)
                    Soldier(dyn, new Vector3(s * 7.55f, 0, z + k * 1.3f), k);
            // giant teddies in the background
            for (float z = 30 + (s > 0 ? 30 : 0); z < length + 160; z += 60f)
                Teddy(root, new Vector3(s * R(19f, 24f), 0, z), R(6f, 9f), s);
        }
        // hanging lamps over the shelves and bunting along them (kept off the FreezeBot's path over the road)
        foreach (int s in new[] { -1, 1 })
            for (float z = 0; z < length + 120; z += 12f)
            {
                Box(root, new Vector3(s * 9f, 21f, z), new Vector3(0.06f, 6f, 0.06f), Lit(Color.gray), false);
                Ball(root, new Vector3(s * 9f, 17.8f, z), new Vector3(1.4f, 0.8f, 1.4f), Glow(new Color(1f, 0.95f, 0.8f), 3f), false);
                for (int i = 0; i < 6; i++)
                {
                    var f = Box(root, new Vector3(s * 9f, 15.2f - Mathf.Sin(i / 5f * Mathf.PI) * 0.9f, z + 1f + i * 2f), new Vector3(0.02f, 0.6f, 0.6f), Lit(Toy[i % Toy.Length], 0.3f), false);
                    f.transform.rotation = Quaternion.Euler(45, 0, 0);
                }
            }
        // RED LIGHT / GREEN LIGHT signs high on the shelf ends
        for (float z = 30; z < length + 100; z += 48f)
            foreach (int s in new[] { -1, 1 })
                Quad(root, new Vector3(s * 9.7f, 13.6f, z), Quaternion.Euler(0, s < 0 ? -90 : 90, 0), new Vector2(6.2f, 1.16f), Tex("rlglsign", 1, 1, Color.white, 1.0f, 0.3f));
    }

    static void Shelf(Transform root, Vector3 p, int s)
    {
        var wood = Lit(new Color(0.95f, 0.85f, 0.7f), 0.3f);
        Box(root, p + new Vector3(s * 1.6f, 6f, 0), new Vector3(0.2f, 12f, 7f), Lit(new Color(1f, 0.95f, 0.85f), 0.2f));   // back board
        Box(root, p + new Vector3(0, 6f, 3.45f), new Vector3(3.4f, 12f, 0.15f), wood);
        for (int k = 0; k < 4; k++)
        {
            float y = 0.15f + k * 3f;
            Box(root, p + new Vector3(0, y, 0), new Vector3(3.4f, 0.2f, 7f), wood);
            // toys on this shelf
            for (float dz = -3f; dz < 3.2f; dz += R(1.2f, 1.8f))
            {
                var tp = p + new Vector3(R(-0.6f, 0.4f), y + 0.1f, dz);
                switch (rnd.Next(5))
                {
                    case 0: { var sz = new Vector3(R(0.9f, 1.4f), R(1.2f, 2.4f), R(0.9f, 1.3f)); Box(root, tp + Vector3.up * sz.y * 0.5f, sz, Lit(C(), 0.4f)); Box(root, tp + new Vector3(-s * sz.x * 0.51f, sz.y * 0.55f, 0), new Vector3(0.02f, sz.y * 0.5f, sz.z * 0.7f), Glow(Color.white, 0.6f), false); break; }
                    case 1: Ball(root, tp + Vector3.up * 0.55f, Vector3.one * 1.1f, Tex("beachball", 1, 1, Color.white, 0, 0.7f)); break;
                    case 2: Teddy(root, tp, R(1.2f, 1.8f), s); break;
                    case 3: BlockTower(root, tp, 2); break;
                    default: Box(root, tp + Vector3.up * 0.45f, new Vector3(1f, 0.9f, 1f), Tex("block" + "ABCDEF"[rnd.Next(6)], 1, 1, Color.white, 0, 0.3f)); break;
                }
            }
        }
    }

    static void BlockTower(Transform root, Vector3 p, int n)
    {
        for (int i = 0; i < n; i++)
        {
            var b = Box(root, p + Vector3.up * (0.45f + i * 0.9f), Vector3.one * 0.9f, Tex("block" + "ABCDEF"[rnd.Next(6)], 1, 1, Color.white, 0, 0.3f));
            b.transform.rotation = Quaternion.Euler(0, rnd.Next(-20, 20), 0);
        }
    }

    // a sitting teddy bear built from spheres, facing the road
    static void Teddy(Transform root, Vector3 p, float h, int s)
    {
        var fur = Lit(Pick(new[] { new Color(0.75f, 0.5f, 0.3f), new Color(1f, 0.7f, 0.8f), new Color(0.6f, 0.75f, 1f), new Color(0.95f, 0.85f, 0.6f) }), 0.15f);
        var muzzle = Lit(new Color(1f, 0.92f, 0.82f), 0.2f);
        var eye = Lit(new Color(0.05f, 0.05f, 0.08f), 0.8f);
        var face = new Vector3(-s, 0, -0.6f).normalized;   // toward the road and the camera
        Ball(root, p + Vector3.up * h * 0.3f, new Vector3(h * 0.62f, h * 0.6f, h * 0.62f), fur);
        var head = p + Vector3.up * h * 0.75f;
        Ball(root, head, Vector3.one * h * 0.45f, fur);
        foreach (int e in new[] { -1, 1 })
        {
            var side = Vector3.Cross(Vector3.up, face) * e;
            Ball(root, head + side * h * 0.18f + Vector3.up * h * 0.18f, Vector3.one * h * 0.16f, fur);
            Ball(root, head + face * h * 0.2f + side * h * 0.08f + Vector3.up * h * 0.05f, Vector3.one * h * 0.06f, eye, false);
            Ball(root, p + Vector3.up * h * 0.1f + face * h * 0.22f + side * h * 0.17f, new Vector3(h * 0.18f, h * 0.16f, h * 0.18f), fur);   // feet
            Ball(root, p + Vector3.up * h * 0.38f + side * h * 0.3f + face * h * 0.06f, new Vector3(h * 0.16f, h * 0.26f, h * 0.16f), fur);  // arms
        }
        Ball(root, head + face * h * 0.19f - Vector3.up * h * 0.05f, new Vector3(h * 0.18f, h * 0.13f, h * 0.14f), muzzle, false);
        Ball(root, head + face * h * 0.27f - Vector3.up * h * 0.02f, Vector3.one * h * 0.05f, eye, false);
        Ball(root, p + Vector3.up * h * 0.55f + face * h * 0.18f, new Vector3(h * 0.22f, h * 0.08f, h * 0.08f), Lit(Red, 0.4f), false);   // bow
    }

    static void Top(Transform dyn, Vector3 p)
    {
        var t = new GameObject("SpinningTop").transform; t.SetParent(dyn); t.position = p;
        var a = C(); var b = C();
        var lo = ConeObj(t, p + Vector3.up * 0.75f, 1.5f, 0.75f, Lit(a, 0.6f), 12, 0);
        lo.transform.rotation = Quaternion.Euler(180, 0, 0);
        ConeObj(t, p + Vector3.up * 0.75f, 1.5f, 0.45f, Lit(b, 0.6f), 12, 30);
        Cyl(t, p + Vector3.up * 1.35f, 0.12f, 0.5f, Lit(Yellow, 0.6f), null, false);
        var spin = t.gameObject.AddComponent<Spin>(); spin.axis = Vector3.up; spin.speed = 400f;
    }

    static void RockingHorse(Transform dyn, Vector3 p, int s)
    {
        var pivot = new GameObject("RockingHorse").transform; pivot.SetParent(dyn); pivot.position = p;
        var col = C(); var body = Lit(col, 0.5f);
        var fwd = new Vector3(0, 0, 1);
        Box(pivot, p + Vector3.up * 0.08f, new Vector3(0.6f, 0.15f, 2.2f), Lit(new Color(0.6f, 0.35f, 0.2f)), false);
        Box(pivot, p + Vector3.up * 1.0f, new Vector3(0.55f, 0.6f, 1.3f), body);
        Box(pivot, p + Vector3.up * 1.55f + fwd * 0.65f, new Vector3(0.4f, 0.75f, 0.4f), body);
        Box(pivot, p + Vector3.up * 1.8f + fwd * 0.9f, new Vector3(0.35f, 0.35f, 0.6f), body);
        Box(pivot, p + Vector3.up * 1.75f + fwd * 0.6f, new Vector3(0.12f, 0.6f, 0.45f), Lit(Yellow, 0.4f), false);   // mane
        foreach (float dz in new[] { -0.45f, 0.45f })
            foreach (float dx in new[] { -0.2f, 0.2f })
                Box(pivot, p + new Vector3(dx, 0.45f, dz), new Vector3(0.12f, 0.6f, 0.12f), body, false);
        var sw = pivot.gameObject.AddComponent<Sway>(); sw.axis = Vector3.right; sw.angle = 12f; sw.speed = 3f;
    }

    static void Soldier(Transform dyn, Vector3 p, int k)
    {
        var t = new GameObject("ToySoldier").transform; t.SetParent(dyn); t.position = p;
        Box(t, p + new Vector3(0, 0.4f, 0), new Vector3(0.35f, 0.8f, 0.3f), Lit(new Color(0.15f, 0.2f, 0.5f)), false);
        Box(t, p + new Vector3(0, 1.15f, 0), new Vector3(0.5f, 0.7f, 0.4f), Lit(Red, 0.5f), false);
        Ball(t, p + new Vector3(0, 1.7f, 0), Vector3.one * 0.38f, Lit(new Color(1f, 0.85f, 0.7f)), false);
        Cyl(t, p + new Vector3(0, 2.1f, 0), 0.36f, 0.55f, Lit(new Color(0.08f, 0.08f, 0.1f), 0.6f), null, false);
        Box(t, p + new Vector3(0, 1.15f, -0.21f), new Vector3(0.06f, 0.5f, 0.02f), Lit(Yellow, 0.7f, 0.6f), false);
        var b = t.gameObject.AddComponent<Bob>(); b.height = 0.12f; b.speed = 9f; b.phase = k * 1.6f;
        var m = t.gameObject.AddComponent<March>(); m.speed = 1.6f;
    }

    // =====================================================================
    // CANDY FACTORY: pink-tiled floor under a candy-floss sky - conveyor belts carrying sweets,
    // giant lollipops and candy canes, chocolate river, gumball machines, robot arms, pipes overhead
    public static readonly List<Material> ScrollMats = new List<Material>();

    static void Factory(Transform root, float length, float L, float half)
    {
        var dyn = WorldBuilder.Dyn;
        ScrollMats.Clear();
        Box(root, new Vector3(0, -0.1f, half), new Vector3(9f, 0.2f, L), Tex("pinktile", 2.25f, L / 4, Color.white, 0, 0.6f));
        Box(root, new Vector3(0, -0.15f, half), new Vector3(420f, 0.2f, L + 300), HalloweenWorld.TexMat(lib.frostMint, 80, (L + 300) / 5, Color.white, 0, 0.3f));
        var choc = HalloweenWorld.TexMat(lib.chocolateTex, 4, L / 6, Color.white, 0, 0.85f);
        ScrollMats.Add(choc);
        var cane = HalloweenWorld.TexMat(lib.stripes, 2, 3, Color.white, 0, 0.7f);
        foreach (int s in new[] { -1, 1 })
        {
            Box(root, new Vector3(s * 4.6f, 0.05f, half), new Vector3(0.2f, 0.12f, L), Glow(Pink, 1.8f, true), false);
            // conveyor belt along the road
            var belt = HalloweenWorld.TexMat(T(lib, "conveyor"), 1, L / 4, Color.white, 0, 0.4f);
            ScrollMats.Add(belt);
            Box(root, new Vector3(s * 7.6f, 0.5f, half), new Vector3(1.8f, 1f, L), Lit(new Color(0.75f, 0.75f, 0.82f), 0.6f, 0.5f));
            Box(root, new Vector3(s * 7.6f, 1.02f, half), new Vector3(1.5f, 0.04f, L), belt, false);
            // gummy crowd dancing between the road and the belt
            string[] gummies = { "PinkBlob", "GreenBlob", "Mushnub", "Cat", "Alien" };
            for (float z = 6; z < length + 100; z += 6f + rnd.Next(7))
                Critter(lib, Pick(gummies), new Vector3(s * R(5.6f, 6.2f), 0, z), -s * R(110f, 145f), R(0.9f, 1.25f), dyn, "dance", "jump");
            // chocolate river
            Box(root, new Vector3(s * 13f, -0.05f, half), new Vector3(6f, 0.2f, L), choc);
            // giant lollipops, candy canes and sweets
            for (float z = 0; z < length + 140; z += 7f + rnd.Next(6))
            {
                if (NearBoard(z)) continue;
                var p = new Vector3(s * R(17f, 30f), 0, z);
                switch (rnd.Next(4))
                {
                    case 0: Lolly(root, p, R(6f, 11f)); break;
                    case 1: Cyl(root, p + Vector3.up * 4.5f, 1.1f, 9f, cane); Ball(root, p + Vector3.up * 9f, Vector3.one * 1.2f, cane); break;
                    case 2: P(Pick(new[] { "cupcake", "donut-sprinkles", "cake-birthday", "ice-cream" }), p, rnd.Next(360), R(4f, 7f), root); break;
                    default: Gumball(root, p, R(3.5f, 5f)); break;
                }
            }
            // robot arms beside the belts
            for (float z = 25 + (s > 0 ? 20 : 0); z < length + 100; z += 45f)
                RobotArm(root, dyn, new Vector3(s * 9.6f, 0, z), s);
        }
        // candy pipes high over the road (above the FreezeBot)
        for (float z = 20; z < length + 100; z += 36f)
        {
            var c = Party[rnd.Next(Party.Length)];
            Cyl(root, new Vector3(0, 25f, z), 1.4f, 30f, Glow(c, 0.9f, true), Quaternion.Euler(0, 0, 90));
            foreach (int s in new[] { -1, 1 }) Cyl(root, new Vector3(s * 15f, 12.5f, z), 1.4f, 25f, Lit(new Color(0.85f, 0.85f, 0.9f), 0.7f, 0.6f));
        }
        Billboards(root, length, 48f, Lit(Pink, 0.5f), 18f);
    }

    static void Lolly(Transform root, Vector3 p, float h)
    {
        Cyl(root, p + Vector3.up * h * 0.4f, h * 0.06f, h * 0.8f, Lit(Color.white, 0.6f));
        var c = Party[rnd.Next(Party.Length)];
        var disc = Cyl(root, p + Vector3.up * h * 0.8f, h * 0.45f, h * 0.08f, Glow(c, 0.7f, true), Quaternion.Euler(90, 0, 0));
        disc.transform.rotation = Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, rnd.Next(360), 0);
        Cyl(root, p + Vector3.up * h * 0.8f + Vector3.back * h * 0.045f, h * 0.3f, h * 0.02f, Glow(Color.white, 0.8f), Quaternion.Euler(90, 0, 0), false);
        Cyl(root, p + Vector3.up * h * 0.8f + Vector3.back * h * 0.05f, h * 0.15f, h * 0.02f, Glow(c, 1.2f), Quaternion.Euler(90, 0, 0), false);
    }

    static void Gumball(Transform root, Vector3 p, float h)
    {
        Box(root, p + Vector3.up * h * 0.2f, new Vector3(h * 0.45f, h * 0.4f, h * 0.45f), Lit(Red, 0.7f));
        var glob = p + Vector3.up * h * 0.62f;
        Ball(root, glob, Vector3.one * h * 0.5f, Lit(new Color(0.85f, 0.95f, 1f), 0.95f));
        for (int i = 0; i < 14; i++)
        {
            var o = Random.insideUnitSphere * h * 0.2f; o.y = Mathf.Abs(o.y) * -0.6f;
            Ball(root, glob + o + new Vector3(0, 0, -h * 0.2f), Vector3.one * h * 0.1f, Glow(Party[i % Party.Length], 0.8f), false);
        }
        Cyl(root, p + Vector3.up * h * 0.92f, h * 0.2f, h * 0.1f, Lit(Red, 0.7f));
    }

    static void RobotArm(Transform root, Transform dyn, Vector3 p, int s)
    {
        var steel = Lit(new Color(1f, 0.75f, 0.2f), 0.6f, 0.4f);
        Cyl(root, p + Vector3.up * 0.4f, 1.4f, 0.8f, Lit(new Color(0.3f, 0.3f, 0.35f), 0.6f, 0.5f));
        var turn = new GameObject("RobotArm").transform; turn.SetParent(dyn); turn.position = p;
        Cyl(turn, p + Vector3.up * 2f, 0.6f, 2.4f, steel);
        var arm = Box(turn, p + new Vector3(-s * 1.3f, 3.3f, 0), new Vector3(2.8f, 0.4f, 0.4f), steel);
        Box(turn, p + new Vector3(-s * 2.7f, 2.6f, 0), new Vector3(0.3f, 1.4f, 0.3f), steel);
        P(Pick(new[] { "lollypop", "candy-bar", "donut-sprinkles" }), p + new Vector3(-s * 2.7f, 1.3f, 0), 0, 0.7f, turn);
        var sw = turn.gameObject.AddComponent<Sway>(); sw.axis = Vector3.up; sw.angle = 50f; sw.speed = 1.6f;
    }
}

// marches slowly along +Z (toy soldiers)
public class March : MonoBehaviour
{
    public float speed = 1.5f;
    void Update() { transform.position += Vector3.forward * speed * Time.deltaTime; }
}
