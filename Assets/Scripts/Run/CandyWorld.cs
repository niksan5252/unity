using UnityEngine;

// Candy Land: gummy forest, chocolate factory, cotton-candy sky. Giant Kenney Food Kit sweets.
public static class CandyWorld
{
    static RunLibrary lib;
    static System.Random rnd;

    public static GameObject Put(RunLibrary l, string name, Vector3 pos, float rotY, float height, Transform root)
    {
        var p = l.Kit(name);
        if (!p) return null;
        var go = WorldBuilder.Spawn(p, pos, rotY, root);
        BlockWorld.Fit(go, height);
        go.transform.position += Vector3.up * pos.y;
        return go;
    }

    static GameObject P(string n, Vector3 pos, float rot, float h, Transform root) => Put(lib, n, pos, rot, h, root);

    static Material Glossy(Color c, float smooth = 0.75f)
    {
        var m = WorldBuilder.Mat(c);
        m.SetFloat("_Smoothness", smooth);
        return m;
    }

    static GameObject Ball(Transform root, Vector3 pos, Vector3 size, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(root);
        g.transform.position = pos;
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static GameObject Cyl(Transform root, Vector3 pos, Vector3 size, Material m, Quaternion rot)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(root);
        g.transform.SetPositionAndRotation(pos, rot);
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static readonly Color[] Pastel =
    {
        new Color(1f, 0.45f, 0.65f), new Color(0.55f, 0.85f, 1f), new Color(0.75f, 0.55f, 1f),
        new Color(0.55f, 0.95f, 0.65f), new Color(1f, 0.85f, 0.4f), new Color(1f, 0.6f, 0.4f),
    };

    public static void Build(RunLibrary l, RunTheme theme, Transform root, float length, System.Random r)
    {
        lib = l; rnd = r;
        float L = length + 160f, half = length * 0.5f + 60f;
        var cam = Camera.main;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 70f; RenderSettings.fogEndDistance = 230f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        Color sky = theme == RunTheme.GummyForest ? new Color(0.6f, 0.85f, 1f) : theme == RunTheme.ChocolateFactory ? new Color(1f, 0.82f, 0.68f) : new Color(1f, 0.78f, 0.92f);
        RenderSettings.fogColor = sky;
        RenderSettings.ambientLight = theme == RunTheme.ChocolateFactory ? new Color(0.8f, 0.7f, 0.62f) : new Color(0.78f, 0.75f, 0.85f);
        if (cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = sky; }
        foreach (var dl in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (dl.type == LightType.Directional) { dl.intensity = 1.25f; dl.color = new Color(1f, 0.95f, 0.9f); dl.transform.rotation = Quaternion.Euler(45f, -30f, 0f); }

        // candy-cane striped road with white frosting rims
        var road = HalloweenWorld.TexMat(lib.stripes, 3, L / 3, Color.white, 0, 0.6f);
        var rim = Glossy(Color.white, 0.8f);
        if (theme == RunTheme.ChocolateFactory) road = HalloweenWorld.TexMat(lib.chocolateTex, 4, L / 2, Color.white, 0, 0.55f);
        if (theme == RunTheme.CottonCandySky) road = HalloweenWorld.TexMat(lib.frostBlue, 4, L / 2, Color.white, 0, 0.5f);
        WorldBuilder.Box(root, new Vector3(0, -0.1f, half), new Vector3(8.6f, 0.2f, L), road);
        foreach (int s in new[] { -1, 1 })
            WorldBuilder.Box(root, new Vector3(s * 4.45f, 0.12f, half), new Vector3(0.35f, 0.45f, L), rim);

        switch (theme)
        {
            case RunTheme.GummyForest: Forest(root, length, L, half); break;
            case RunTheme.ChocolateFactory: Factory(root, length, L, half); break;
            default: Sky(root, length, L, half); break;
        }
    }

    // ---------- gummy forest: lollipop trees, gumdrop hills, giant cupcakes and donuts ----------
    static void Forest(Transform root, float length, float L, float half)
    {
        WorldBuilder.Box(root, new Vector3(0, -0.15f, half), new Vector3(300f, 0.2f, L + 200), HalloweenWorld.TexMat(lib.frostMint, 60, (L + 200) / 5, Color.white, 0, 0.4f));
        string[] giants = { "cupcake", "donut-sprinkles", "donut-chocolate", "cake-birthday", "ice-cream", "sundae", "muffin", "popsicle", "cherries", "strawberry" };
        foreach (int s in new[] { -1, 1 })
        {
            for (float z = -10; z < length + 120; z += 6f + rnd.Next(5))
            {
                var lp = P("lollypop", new Vector3(s * (6.5f + rnd.Next(10)), 0, z), rnd.Next(360), 5f + rnd.Next(5), root);
            }
            for (float z = 0; z < length + 120; z += 9f + rnd.Next(9))
            {
                var n = giants[rnd.Next(giants.Length)];
                P(n, new Vector3(s * (9f + rnd.Next(14)), 0, z), rnd.Next(360), 3f + rnd.Next(4), root);
            }
            // gumdrop hills
            for (float z = 0; z < length + 160; z += 12f + rnd.Next(10))
            {
                float w = 8 + rnd.Next(10);
                var gm = Glossy(Pastel[rnd.Next(Pastel.Length)], 0.85f);
                Ball(root, new Vector3(s * (28f + rnd.Next(16)), 0, z), new Vector3(w, w * 0.8f, w), gm);
            }
        }
        Clouds(root, length, new Color(1f, 1f, 1f));
    }

    // ---------- chocolate factory: chocolate rivers, candy-bar towers, pastel pipes ----------
    static void Factory(Transform root, float length, float L, float half)
    {
        var river = Glossy(new Color(0.32f, 0.17f, 0.08f), 0.95f);
        var cream = HalloweenWorld.TexMat(lib.frostPink, 60, (L + 200) / 5, Color.white, 0, 0.4f);
        WorldBuilder.Box(root, new Vector3(0, -0.25f, half), new Vector3(300f, 0.2f, L + 200), cream);
        foreach (int s in new[] { -1, 1 })
        {
            WorldBuilder.Box(root, new Vector3(s * 8f, -0.12f, half), new Vector3(7f, 0.2f, L), river);
            // chocolate fall into the river
            for (float z = 30; z < length + 100; z += 70f)
            {
                WorldBuilder.Box(root, new Vector3(s * 11.2f, 4f, z), new Vector3(0.4f, 8f, 4f), river);
                WorldBuilder.Box(root, new Vector3(s * 12.5f, 4f, z), new Vector3(2.4f, 8f, 6f), HalloweenWorld.TexMat(lib.chocolateTex, 2, 6, Color.white));
            }
            // candy-bar and cookie towers like factory buildings
            for (float z = -10; z < length + 120; z += 8f + rnd.Next(6))
            {
                float x = s * (14f + rnd.Next(10));
                float h = 6 + rnd.Next(14);
                WorldBuilder.Box(root, new Vector3(x, h * 0.5f, z), new Vector3(4f, h, 4f), HalloweenWorld.TexMat(lib.chocolateTex, 2, h / 2, Color.white, 0, 0.5f));
                P(rnd.NextDouble() < 0.5 ? "cookie-chocolate" : "donut-sprinkles", new Vector3(x, h, z), rnd.Next(360), 2.2f, root);
            }
            // pastel pipes along the river
            var pipe = Glossy(Pastel[s > 0 ? 0 : 1], 0.8f);
            for (float z = 0; z < length + 120; z += 24f)
            {
                Cyl(root, new Vector3(s * 5.6f, 3.5f, z), new Vector3(0.6f, 3.5f, 0.6f), pipe, Quaternion.identity);
                Cyl(root, new Vector3(s * 8.5f, 6.8f, z), new Vector3(0.6f, 3f, 0.6f), pipe, Quaternion.Euler(0, 0, 90));
            }
            // floating marshmallows / candies on the river
            for (float z = 4; z < length + 100; z += 7f + rnd.Next(7))
                P(rnd.NextDouble() < 0.5 ? "candy-bar" : "chocolate", new Vector3(s * (6f + rnd.Next(4)), 0, z), rnd.Next(360), 0.7f, root);
        }
    }

    // ---------- cotton-candy sky: floating frosting road, cloud puffs, ice-cream towers, rainbows ----------
    static void Sky(Transform root, float length, float L, float half)
    {
        Clouds(root, length, new Color(1f, 0.8f, 0.92f));
        Clouds(root, length, new Color(0.75f, 0.88f, 1f));
        string[] towers = { "ice-cream", "sundae", "cake-birthday", "cupcake", "frappe", "popsicle-chocolate", "whipped-cream" };
        foreach (int s in new[] { -1, 1 })
        {
            for (float z = 0; z < length + 140; z += 8f + rnd.Next(8))
            {
                float x = s * (9f + rnd.Next(24)), y = -6f + rnd.Next(12);
                var puff = Glossy(rnd.NextDouble() < 0.5 ? new Color(1f, 0.75f, 0.9f) : new Color(0.75f, 0.88f, 1f), 0.2f);
                Ball(root, new Vector3(x, y, z), new Vector3(5f, 3f, 5f), puff);
                P(towers[rnd.Next(towers.Length)], new Vector3(x, y + 1.2f, z), rnd.Next(360), 4f + rnd.Next(5), root);
            }
        }
        // rainbow arches over the road
        if (lib.rainbowTex)
        {
            var rm = HalloweenWorld.Transparent(lib.rainbowTex, Color.white);
            for (float z = 40; z < length + 100; z += 60f)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(root);
                q.transform.position = new Vector3(0, 7f, z);
                q.transform.localScale = new Vector3(26f, 13f, 1);
                q.GetComponent<Renderer>().sharedMaterial = rm;
            }
        }
        // castle of cakes far ahead at the end of the level
        for (int i = 0; i < 3; i++)
            P("cake-birthday", new Vector3((i - 1) * 9f, -2f + (i == 1 ? 6 : 0), length + 60f), 0, i == 1 ? 18f : 12f, root);
    }

    static void Clouds(Transform root, float length, Color c)
    {
        var m = Glossy(c, 0.1f);
        m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 0.25f);
        for (float z = -20; z < length + 250; z += 14f + rnd.Next(14))
        {
            float x = rnd.Next(-80, 80), y = 22 + rnd.Next(14);
            for (int k = 0; k < 3; k++)
                Ball(root, new Vector3(x + k * 4f, y + (k == 1 ? 1.5f : 0), z), new Vector3(7f, 5f, 6f), m);
        }
    }
}
