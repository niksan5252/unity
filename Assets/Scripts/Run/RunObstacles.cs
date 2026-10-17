using DG.Tweening;
using UnityEngine;

// Creates the obstacle for a cue and animates it against song time.
public class RunObstacle : MonoBehaviour
{
    public RunMove move;
    public float hitTime;
    public RunDirector director;
    Transform actor;
    Animation anim;
    bool fired;
    Rigidbody[] pieces;

    public static RunObstacle Create(RunLibrary lib, RunCue cue, float speed, Transform parent, RunDirector d, System.Random rnd)
    {
        float z = cue.time * speed;
        var root = new GameObject("Obstacle_" + cue.move).transform;
        root.SetParent(parent);
        root.position = new Vector3(0, 0, z);
        var ob = root.gameObject.AddComponent<RunObstacle>();
        ob.move = cue.move; ob.hitTime = cue.time; ob.director = d;

        if (lib.blockMode && ob.BuildBlocks(lib, cue, z, root, rnd)) return ob;
        if (lib.obbyMode && ob.BuildObby(cue, z, root, rnd)) return ob;
        if (lib.halloweenMode && ob.BuildHalloween(lib, cue, z, root, rnd)) return ob;
        if (lib.candyMode && ob.BuildCandy(lib, cue, z, root, rnd)) return ob;

        switch (cue.move)
        {
            case RunMove.Jump:
            {
                // striped hurdle across all lanes, top at ~1 m
                var red = WorldBuilder.Emissive(new Color(1f, 0.15f, 0.1f), 1.2f);
                var white = WorldBuilder.Emissive(new Color(1f, 1f, 1f), 0.6f);
                var leg = WorldBuilder.Mat(new Color(0.25f, 0.25f, 0.3f));
                for (int i = 0; i < 11; i++)
                    WorldBuilder.Box(root, new Vector3(-4.4f + i * 0.88f, 0.75f, z), new Vector3(0.88f, 0.45f, 0.15f), i % 2 == 0 ? red : white);
                for (int i = -2; i <= 2; i++)
                    WorldBuilder.Box(root, new Vector3(i * 2.1f, 0.3f, z), new Vector3(0.15f, 0.6f, 0.6f), leg);
                break;
            }
            case RunMove.Duck:
            {
                // striped beam on two posts, bottom edge at 1.25 m
                var red = WorldBuilder.Emissive(new Color(1f, 0.15f, 0.1f), 1.2f);
                var white = WorldBuilder.Emissive(new Color(1f, 1f, 1f), 0.6f);
                var post = WorldBuilder.Mat(new Color(0.25f, 0.25f, 0.3f));
                for (int i = 0; i < 11; i++)
                    WorldBuilder.Box(root, new Vector3(-4.4f + i * 0.88f, 1.45f, z), new Vector3(0.88f, 0.4f, 0.4f), i % 2 == 0 ? red : white);
                WorldBuilder.Box(root, new Vector3(-4.6f, 1.2f, z), new Vector3(0.3f, 2.4f, 0.3f), post);
                WorldBuilder.Box(root, new Vector3(4.6f, 1.2f, z), new Vector3(0.3f, 2.4f, 0.3f), post);
                break;
            }
            case RunMove.Left:
            case RunMove.Right:
            {
                // train fills center + one side lane; player steps to the free side
                int free = cue.move == RunMove.Left ? -1 : 1;
                foreach (int lane in new[] { 0, -free })
                {
                    var t = WorldBuilder.Spawn(lib.trains[rnd.Next(lib.trains.Length)], new Vector3(lane * WorldBuilder.LaneGap, 0, z + 4f), 180, root);
                    FitWidth(t, 2.3f, true);
                    Paint(t, TrainColors[rnd.Next(TrainColors.Length)]);
                    ob.actor = root;
                }
                break;
            }
            case RunMove.Dash:
            {
                var crate = WorldBuilder.Mat(new Color(0.85f, 0.55f, 0.2f));
                var edge = WorldBuilder.Mat(new Color(0.45f, 0.25f, 0.1f));
                var list = new System.Collections.Generic.List<Rigidbody>();
                for (int x = 0; x < 6; x++)
                    for (int y = 0; y < 4; y++)
                    {
                        var c = WorldBuilder.Box(root, new Vector3(-3.4f + x * 1.36f, 0.65f + y * 1.32f, z), new Vector3(1.3f, 1.3f, 1.3f), (x + y) % 3 == 0 ? edge : crate);
                        c.AddComponent<BoxCollider>();
                        var rb = c.AddComponent<Rigidbody>(); rb.isKinematic = true;
                        list.Add(rb);
                    }
                ob.pieces = list.ToArray();
                break;
            }
            case RunMove.Smash:
            {
                var p = lib.monsters[rnd.Next(lib.monsters.Length)];
                var m = WorldBuilder.Spawn(p, new Vector3(0, 0, z + 1.5f), 180, root);
                ob.actor = m.transform;
                ob.anim = m.GetComponentInChildren<Animation>();
                ob.PlayClip("sprint", "run", "walk");
                if (ob.anim) ob.anim.Sample();   // measure the posed mesh, not the raw import bounds
                FitHeightPosed(m, 3.2f);
                break;
            }
        }
        return ob;
    }

    // candy versions
    bool BuildCandy(RunLibrary lib, RunCue cue, float z, Transform root, System.Random rnd)
    {
        var th = WorldBuilder.Theme;
        switch (cue.move)
        {
            case RunMove.Jump:
            {
                string n = th == RunTheme.GummyForest ? "donut-sprinkles" : th == RunTheme.ChocolateFactory ? "candy-bar" : "cupcake";
                for (int i = 0; i < 4; i++)
                {
                    var g = HPut(lib, n, new Vector3(-3.3f + i * 2.2f, 0, z), th == RunTheme.ChocolateFactory ? 90 : rnd.Next(360), 1f, root);
                    if (g && n == "donut-sprinkles") g.transform.localScale = Vector3.Scale(g.transform.localScale, new Vector3(1.6f, 1f, 1.6f));
                }
                return true;
            }
            case RunMove.Duck:
            {
                // candy cane bar: red/white stripes on two white posts
                var cane = HalloweenWorld.TexMat(lib.stripes, 6, 0.5f, Color.white, 0, 0.7f);
                WorldBuilder.Box(root, new Vector3(0, 1.65f, z), new Vector3(9f, 0.42f, 0.42f), cane);
                var post = WorldBuilder.Mat(Color.white); post.SetFloat("_Smoothness", 0.8f);
                foreach (int sd in new[] { -1, 1 })
                    WorldBuilder.Box(root, new Vector3(sd * 4.4f, 0.95f, z), new Vector3(0.35f, 1.9f, 0.35f), cane);
                return true;
            }
            case RunMove.Left:
            case RunMove.Right:
            {
                // giant donuts rolling at us in the center and one side lane
                int free = cue.move == RunMove.Left ? -1 : 1;
                foreach (int lane in new[] { 0, -free })
                {
                    var dn = HPut(lib, rnd.NextDouble() < 0.5 ? "donut-sprinkles" : "donut-chocolate", new Vector3(lane * WorldBuilder.LaneGap, 0, z + 2f), 0, 0.6f, root);
                    if (dn)
                    {
                        // stand the donut up on its edge and size it to ~2.3 m
                        dn.transform.rotation = Quaternion.Euler(0, 0, 0) * Quaternion.Euler(90, 90, 0) * lib.Kit(dn.name.Replace("(Clone)", "")).transform.rotation;
                        var b = WorldBuilder.Bounds(dn);
                        dn.transform.localScale *= 2.3f / Mathf.Max(0.01f, b.size.y);
                        b = WorldBuilder.Bounds(dn);
                        dn.transform.position += new Vector3(lane * WorldBuilder.LaneGap - b.center.x, -b.min.y, 0);
                        dn.AddComponent<Roll>();
                    }
                }
                actor = root;
                return true;
            }
            case RunMove.Dash:
            {
                // a giant chocolate bar wall that breaks into squares
                var choc = HalloweenWorld.TexMat(lib.chocolateTex, 0.5f, 0.5f, Color.white, 0, 0.55f);
                var list = new System.Collections.Generic.List<Rigidbody>();
                for (int x = 0; x < 8; x++)
                    for (int y = 0; y < 4; y++)
                    {
                        var c = WorldBuilder.Box(root, new Vector3(-3.5f + x, 0.5f + y, z), new Vector3(0.98f, 0.98f, 0.5f), choc);
                        c.AddComponent<BoxCollider>();
                        var rb = c.AddComponent<Rigidbody>(); rb.isKinematic = true;
                        list.Add(rb);
                    }
                pieces = list.ToArray();
                return true;
            }
        }
        return false;
    }

    GameObject HPut(RunLibrary lib, string name, Vector3 pos, float rotY, float h, Transform root)
    {
        var p = lib.Kit(name);
        if (!p) return null;
        var go = WorldBuilder.Spawn(p, pos, rotY, root);
        BlockWorld.Fit(go, h);
        go.transform.position += Vector3.up * pos.y;
        return go;
    }

    System.Collections.Generic.List<Rigidbody> breakable = new System.Collections.Generic.List<Rigidbody>();
    void MakeBreakable(GameObject go)
    {
        if (!go) return;
        var b = WorldBuilder.Bounds(go);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = go.transform.InverseTransformPoint(b.center);
        bc.size = go.transform.InverseTransformVector(b.size);
        bc.size = new Vector3(Mathf.Abs(bc.size.x), Mathf.Abs(bc.size.y), Mathf.Abs(bc.size.z));
        var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true;
        breakable.Add(rb);
    }

    // halloween versions, themed per world
    bool BuildHalloween(RunLibrary lib, RunCue cue, float z, Transform root, System.Random rnd)
    {
        var th = WorldBuilder.Theme;
        switch (cue.move)
        {
            case RunMove.Jump:
                if (th == RunTheme.Mansion)
                    for (int i = -1; i <= 1; i++) HPut(lib, "coffin", new Vector3(i * WorldBuilder.LaneGap, 0, z), 90, 0.75f, root);
                else if (th == RunTheme.PumpkinFarm)
                    for (int i = 0; i < 4; i++) HPut(lib, "hay-bale", new Vector3(-3.3f + i * 2.2f, 0, z), 90, 0.95f, root);
                else
                    for (int i = 0; i < 4; i++) HPut(lib, "stone-wall", new Vector3(-3.3f + i * 2.2f, 0, z), 0, 1f, root);
                return true;
            case RunMove.Duck:
            {
                var wood = HalloweenWorld.TexMat(lib.darkWood, 4, 0.5f, new Color(0.8f, 0.6f, 0.45f));
                WorldBuilder.Box(root, new Vector3(0, 1.65f, z), new Vector3(9f, 0.42f, 0.42f), wood);
                var iron = WorldBuilder.Mat(new Color(0.12f, 0.12f, 0.14f)); iron.SetFloat("_Metallic", 0.8f);
                foreach (float cx in new[] { -3.6f, 3.6f })
                    WorldBuilder.Box(root, new Vector3(cx, 3.6f, z), new Vector3(0.08f, 3.6f, 0.08f), iron);   // chains up to the ceiling
                foreach (int sd in new[] { -1, 1 })
                    WorldBuilder.Box(root, new Vector3(sd * 4.4f, 0.9f, z), new Vector3(0.3f, 1.8f, 0.3f), WorldBuilder.Mat(new Color(0.15f, 0.1f, 0.07f)));
                var flame = WorldBuilder.Emissive(new Color(1f, 0.6f, 0.2f), 3f);
                foreach (float x in new[] { -2.6f, 0f, 2.6f })
                {
                    var l = HPut(lib, "lantern-candle", new Vector3(x, 1.86f, z), 0, 0.45f, root);
                    if (l) WorldBuilder.Box(root, new Vector3(x, 2.05f, z), Vector3.one * 0.1f, flame);
                }
                return true;
            }
            case RunMove.Left:
            case RunMove.Right:
            {
                // giant rolling jack-o'-lanterns in the center and one side lane
                int free = cue.move == RunMove.Left ? -1 : 1;
                foreach (int lane in new[] { 0, -free })
                {
                    var pk = HPut(lib, "pumpkin-carved", new Vector3(lane * WorldBuilder.LaneGap, 0, z + 2f), 180, 2.2f, root);
                    if (pk)
                    {
                        foreach (var r in pk.GetComponentsInChildren<Renderer>())
                        {
                            var ms = r.materials;
                            foreach (var m in ms) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.05f) * 0.35f); }
                            r.materials = ms;
                        }
                        pk.AddComponent<Roll>();
                    }
                }
                actor = root;
                return true;
            }
            case RunMove.Dash:
            {
                string piece = th == RunTheme.Mansion ? "brick-wall" : th == RunTheme.PumpkinFarm ? "hay-bale" : "stone-wall";
                for (int x = 0; x < 4; x++)
                    for (int y = 0; y < 3; y++)
                        MakeBreakable(HPut(lib, piece, new Vector3(-3.3f + x * 2.2f, y * 1.05f, z), 0, 1.05f, root));
                pieces = breakable.ToArray();
                return true;
            }
        }
        return false;
    }

    // obby versions: the JUMP gap is cut into the path by ObbyWorld
    bool BuildObby(RunCue cue, float z, Transform root, System.Random rnd)
    {
        var kill = ObbyWorld.Brick(new Color(1f, 0.1f, 0.1f), 4, 0.25f, 1.6f);
        var white = ObbyWorld.Brick(Color.white, 0.5f, 1.2f);
        switch (cue.move)
        {
            case RunMove.Jump:
                return true;
            case RunMove.Duck:
                foreach (int sd in new[] { -1, 1 })
                    WorldBuilder.Box(root, new Vector3(sd * 4.3f, 1.2f, z), new Vector3(0.5f, 2.4f, 0.5f), white);
                WorldBuilder.Box(root, new Vector3(0, 1.6f, z), new Vector3(8.6f, 0.5f, 0.5f), kill);
                return true;
            case RunMove.Dash:
            {
                var list = new System.Collections.Generic.List<Rigidbody>();
                for (int x = 0; x < 8; x++)
                    for (int y = 0; y < 4; y++)
                    {
                        var c = WorldBuilder.Box(root, new Vector3(-3.5f + x, 0.5f + y, z), Vector3.one * 0.98f,
                            ObbyWorld.Brick(ObbyWorld.Bright[(x + y * 3) % ObbyWorld.Bright.Length], 0.5f, 0.5f));
                        c.AddComponent<BoxCollider>();
                        var rb = c.AddComponent<Rigidbody>(); rb.isKinematic = true;
                        list.Add(rb);
                    }
                pieces = list.ToArray();
                return true;
            }
            case RunMove.Left:
            case RunMove.Right:
            {
                // big sliding bricks fill the center and one side lane
                int free = cue.move == RunMove.Left ? -1 : 1;
                foreach (int lane in new[] { 0, -free })
                {
                    var col = ObbyWorld.Bright[rnd.Next(ObbyWorld.Bright.Length)];
                    WorldBuilder.Box(root, new Vector3(lane * WorldBuilder.LaneGap, 1.2f, z + 4f), new Vector3(2.4f, 2.4f, 7f), ObbyWorld.Brick(col, 1.2f, 3.5f, 0.15f));
                }
                actor = root;
                return true;
            }
        }
        return false;
    }

    // Minecraft-style versions of the obstacles; false = fall back to the default ones
    bool BuildBlocks(RunLibrary lib, RunCue cue, float z, Transform root, System.Random rnd)
    {
        switch (cue.move)
        {
            case RunMove.Jump:
                for (int i = 0; i < 8; i++)
                    BlockWorld.Block(root, new Vector3(-3.5f + i, 0.5f, z), i % 3 == 1 ? lib.trunk : lib.planks);
                return true;
            case RunMove.Duck:
                foreach (int sd in new[] { -1, 1 })
                    WorldBuilder.Box(root, new Vector3(sd * 4.5f, 1.15f, z), new Vector3(1f, 2.3f, 1f), BlockWorld.Tex(lib.stone, 1, 2.3f));
                for (int i = 0; i < 8; i++)
                    BlockWorld.Block(root, new Vector3(-3.5f + i, 1.8f, z), lib.trunk);
                return true;
            case RunMove.Dash:
            {
                var list = new System.Collections.Generic.List<Rigidbody>();
                for (int x = 0; x < 8; x++)
                    for (int y = 0; y < 4; y++)
                    {
                        var tex = rnd.NextDouble() < 0.3 ? lib.ores[rnd.Next(lib.ores.Length)] : (y == 3 ? lib.dirtGrass : rnd.NextDouble() < 0.5 ? lib.dirt : lib.stone);
                        var c = BlockWorld.Block(root, new Vector3(-3.5f + x, 0.5f + y, z), tex);
                        c.AddComponent<BoxCollider>();
                        var rb = c.AddComponent<Rigidbody>(); rb.isKinematic = true;
                        list.Add(rb);
                    }
                pieces = list.ToArray();
                return true;
            }
            case RunMove.Left:
            case RunMove.Right:
            {
                // a short train of minecarts in the center + blocked side lane
                int free = cue.move == RunMove.Left ? -1 : 1;
                foreach (int lane in new[] { 0, -free })
                    for (int k = 0; k < 3; k++)
                    {
                        var cart = WorldBuilder.Spawn(lib.trains[rnd.Next(lib.trains.Length)], new Vector3(lane * WorldBuilder.LaneGap, 0, z + 2f + k * 2.6f), 0, root);
                        FitWidth(cart, 2f, true);
                    }
                actor = root;
                return true;
            }
        }
        return false;
    }

    void PlayClip(params string[] keys)
    {
        if (!anim) return;
        foreach (var k in keys) if (TryClip(k)) return;
    }

    bool TryClip(string key)
    {
        foreach (AnimationState s in anim)
            if (s.name.ToLower().Contains(key))
            {
                s.wrapMode = WrapMode.Loop;
                anim.Play(s.name);
                return true;
            }
        return false;
    }

    void Update()
    {
        float t = director.SongTime;
        if (move == RunMove.Smash && actor && !fired)
        {
            // monster charges toward the camera, arriving at hit time
            float ahead = Mathf.Max(0f, hitTime - t) * 6f;
            var p = actor.position; p.z = hitTime * director.speed + 1.5f + ahead; actor.position = p;
        }
        if (move is RunMove.Left or RunMove.Right && actor && !fired)
        {
            // oncoming train
            float ahead = Mathf.Max(0f, hitTime - t) * 10f;
            var p = actor.position; p.z = hitTime * director.speed + ahead; actor.position = p;
        }
        // with a buddy running ahead, the crate wall bursts when the buddy hits it
        float fireAt = move == RunMove.Dash && director.Buddy ? hitTime - director.Buddy.lead : hitTime - 0.05f;
        if (!fired && t >= fireAt) Fire();
    }

    void Fire()
    {
        fired = true;
        switch (move)
        {
            case RunMove.Dash:
                foreach (var rb in pieces)
                {
                    rb.isKinematic = false;
                    var dir = (rb.position - transform.position).normalized + Vector3.forward * 1.5f + Vector3.up;
                    rb.AddForce(dir * Random.Range(9f, 15f) + director.speed * Vector3.forward, ForceMode.VelocityChange);
                    rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.VelocityChange);
                }
                RunFx.Burst(transform.position + new Vector3(0, 2, 0), new Color(1f, 0.8f, 0.3f));
                break;
            case RunMove.Smash:
                if (actor)
                {
                    PlayClip("hit", "fall", "die", "death");
                    actor.DOMove(actor.position + new Vector3(Random.Range(-3f, 3f), 4f, 30f), 0.8f).SetEase(Ease.OutQuad);
                    actor.DORotate(new Vector3(-80, 0, Random.Range(-60f, 60f)), 0.8f, RotateMode.LocalAxisAdd);
                    RunFx.Burst(actor.position + Vector3.up * 1.4f, new Color(1f, 0.95f, 0.4f));
                }
                director.Hud.Flash();
                break;
        }
    }

    static readonly Color[] TrainColors =
    {
        new Color(1f, 0.55f, 0.1f), new Color(0.15f, 0.75f, 1f), new Color(1f, 0.85f, 0.15f),
        new Color(0.95f, 0.25f, 0.3f), new Color(0.45f, 0.9f, 0.35f), new Color(0.75f, 0.4f, 1f),
    };

    // bright livery over the Kenney colormap
    static void Paint(GameObject go, Color c)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = r.materials;
            foreach (var m in mats)
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.Lerp(Color.white, c, 0.75f) * 1.15f);
            r.materials = mats;
        }
    }

    static Bounds PosedBounds(GameObject go)
    {
        var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (smrs.Length == 0) return WorldBuilder.Bounds(go);
        bool any = false; var b = new Bounds();
        foreach (var r in smrs)
        {
            var mesh = new Mesh();
            r.BakeMesh(mesh, true);
            var lb = mesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var c = lb.center + Vector3.Scale(lb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var w = r.transform.TransformPoint(c);
                if (!any) { b = new Bounds(w, Vector3.zero); any = true; } else b.Encapsulate(w);
            }
            Object.Destroy(mesh);
        }
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) b.Encapsulate(r.bounds);
        return b;
    }

    public static void FitHeightPosed(GameObject go, float h)
    {
        var b = PosedBounds(go);
        if (b.size.y > 0.001f) go.transform.localScale *= h / b.size.y;
        b = PosedBounds(go);
        go.transform.position += Vector3.up * (0f - b.min.y);
    }

    static void FitHeight(GameObject go, float h)
    {
        var b = WorldBuilder.Bounds(go);
        if (b.size.y > 0.01f) go.transform.localScale *= h / b.size.y;
        b = WorldBuilder.Bounds(go);
        go.transform.position += Vector3.up * (0f - b.min.y);
    }

    static void FitWidth(GameObject go, float w, bool keepGround = false)
    {
        var b = WorldBuilder.Bounds(go);
        float cur = Mathf.Max(0.01f, b.size.x);
        go.transform.localScale *= w / cur;
        if (keepGround) { b = WorldBuilder.Bounds(go); go.transform.position += Vector3.up * (0f - b.min.y); }
    }
}

public static class RunFx
{
    public static Texture2D spark;

    public static void Burst(Vector3 pos, Color c)
    {
        var go = new GameObject("Burst");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.4f; main.loop = false;
        main.startLifetime = 0.6f; main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 14f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        main.startColor = c;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.5f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        if (spark) m.SetTexture("_BaseMap", spark);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 2);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_ZWrite", 0);
        m.renderQueue = 3000;
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        r.sharedMaterial = m;
        ps.Play();
    }
}

// spins a rolling obstacle around its local X while it is moving
public class Roll : MonoBehaviour
{
    Vector3 last;
    void Start() { last = transform.position; }
    void Update()
    {
        float dz = transform.position.z - last.z;
        last = transform.position;
        var c = WorldBuilder.Bounds(gameObject).center;
        transform.RotateAround(c, Vector3.right, dz * 52f);
    }
}
