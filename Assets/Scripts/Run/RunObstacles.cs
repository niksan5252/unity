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
    BlockFigure figure;
    public System.Collections.Generic.List<Color> pieceColors;
    Rigidbody[] pieces;

    public static RunObstacle Create(RunLibrary lib, RunCue cue, float speed, Transform parent, RunDirector d, System.Random rnd)
    {
        float z = d ? d.Dist(cue.time) : cue.time * speed;
        var root = new GameObject("Obstacle_" + cue.move).transform;
        root.SetParent(parent);
        root.position = new Vector3(0, 0, z);
        var ob = root.gameObject.AddComponent<RunObstacle>();
        ob.move = cue.move; ob.hitTime = cue.time; ob.director = d;

        if (cue.move == RunMove.Mirror) { ob.BuildPoseWall(cue, z, root); return ob; }
        if (lib.blockMode && ob.BuildBlocks(lib, cue, z, root, rnd)) return ob;
        if (lib.obbyMode && ob.BuildObby(cue, z, root, rnd)) return ob;
        if (lib.halloweenMode && ob.BuildHalloween(lib, cue, z, root, rnd)) return ob;
        if (lib.candyMode && ob.BuildCandy(lib, cue, z, root, rnd)) return ob;
        if (lib.dinoMode && ob.BuildDino(lib, cue, z, root, rnd)) return ob;
        if (lib.mcMode && ob.BuildMc(lib, cue, z, root, rnd)) return ob;
        if (lib.kpopMode && ob.BuildKpop(lib, cue, z, root, rnd)) return ob;
        if (lib.spookyMode && ob.BuildSpooky(lib, cue, z, root, rnd)) return ob;
        if (lib.toyMode && ob.BuildToy(lib, cue, z, root, rnd)) return ob;

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
                if (lib.mcMode)
                {
                    var th2 = WorldBuilder.Theme;
                    var kinds = th2 == RunTheme.McEnd ? new[] { BlockFigure.Kind.Shade, BlockFigure.Kind.Shade, BlockFigure.Kind.Slime }
                              : th2 == RunTheme.McCherry ? new[] { BlockFigure.Kind.Zombie, BlockFigure.Kind.Creeper, BlockFigure.Kind.Skeleton }
                              : th2 == RunTheme.McOcean ? new[] { BlockFigure.Kind.Drowned, BlockFigure.Kind.Drowned, BlockFigure.Kind.Creeper }
                              : th2 == RunTheme.McCrimson ? new[] { BlockFigure.Kind.Piglin, BlockFigure.Kind.WitherSkeleton, BlockFigure.Kind.MagmaCube }
                              : th2 == RunTheme.McTaiga ? new[] { BlockFigure.Kind.Zombie, BlockFigure.Kind.Skeleton }
                              : new[] { BlockFigure.Kind.Zombie, BlockFigure.Kind.Skeleton, BlockFigure.Kind.Spider, BlockFigure.Kind.Slime };
                    var fig = BlockFigure.Create(kinds[rnd.Next(kinds.Length)], root);
                    fig.transform.position = new Vector3(0, 0, z + 1.5f);
                    fig.transform.rotation = Quaternion.Euler(0, 180, 0);
                    fig.SetState("Walk");
                    if (fig.kind == BlockFigure.Kind.Slime || fig.kind == BlockFigure.Kind.MagmaCube) fig.transform.localScale = Vector3.one * 1.3f;
                    if (fig.kind == BlockFigure.Kind.WitherSkeleton) fig.transform.localScale = Vector3.one * 1.15f;
                    ob.figure = fig;
                    ob.actor = fig.transform;
                    break;
                }
                var p = lib.monsters[rnd.Next(lib.monsters.Length)];
                var m = WorldBuilder.Spawn(p, new Vector3(0, 0, z + 1.5f), 180, root);
                ob.actor = m.transform;
                if (lib.dinoMode) DinoWorld.Brighten(m);
                if (lib.kpopMode) KpopWorld.Demonize(m);
                if (lib.spookyMode) SpookyWorld.Spookify(m, SpookyWorld.Lime, 0.3f);
                ob.anim = m.GetComponentInChildren<Animation>();
                ob.PlayClip("sprint", "run", "walk", "fast_flying", "flying");
                if (ob.anim) ob.anim.Sample();   // measure the posed mesh, not the raw import bounds
                FitHeightPosed(m, 3.2f);
                break;
            }
        }
        return ob;
    }

    // lay a model's long axis across the road and size it to `width`
    static void AcrossRoad(GameObject go, float width, float height, float baseY, float z)
    {
        var b = WorldBuilder.Bounds(go);
        if (b.size.z > b.size.x) { go.transform.rotation = Quaternion.Euler(0, 90, 0) * go.transform.rotation; b = WorldBuilder.Bounds(go); }
        go.transform.localScale = Vector3.Scale(go.transform.localScale, new Vector3(width / Mathf.Max(0.01f, b.size.x), height / Mathf.Max(0.01f, b.size.y), height / Mathf.Max(0.01f, b.size.y)));
        b = WorldBuilder.Bounds(go);
        go.transform.position += new Vector3(-b.center.x, baseY - b.min.y, z - b.center.z);
    }

    // MIRROR ME: a bright panel with a black pose silhouette rushes at us; strike the pose, it bursts
    Color wallColor;
    Transform wall;
    void BuildPoseWall(RunCue cue, float z, Transform root)
    {
        var th = WorldBuilder.Theme;
        wallColor = th == RunTheme.McTaiga ? new Color(0.35f, 0.85f, 1f) : th == RunTheme.McEnd ? new Color(1f, 0.35f, 0.85f) : th == RunTheme.McJungle ? new Color(1f, 0.9f, 0.1f)
                  : th == RunTheme.McCherry ? new Color(1f, 0.45f, 0.75f) : th == RunTheme.McOcean ? new Color(0.2f, 0.95f, 1f) : th == RunTheme.McCrimson ? new Color(1f, 0.75f, 0.1f)
                  : th == RunTheme.SpookyCarnival ? new Color(1f, 0.55f, 0.1f) : th == RunTheme.WitchForest ? new Color(0.55f, 1f, 0.2f) : th == RunTheme.MoonCastle ? new Color(0.75f, 0.35f, 1f)
                  : new Color(1f, 0.9f, 0.1f);
        wall = new GameObject("PoseWall").transform;
        wall.SetParent(root);
        wall.position = new Vector3(0, 0, z);
        var panel = WorldBuilder.Emissive(wallColor, 0.55f);
        var g = WorldBuilder.Box(wall, new Vector3(0, 1.75f, z), new Vector3(4.4f, 3.3f, 0.2f), panel);
        var frame = WorldBuilder.Emissive(Color.white, 1.2f);
        WorldBuilder.Box(wall, new Vector3(0, 3.45f, z - 0.02f), new Vector3(4.6f, 0.12f, 0.24f), frame);
        WorldBuilder.Box(wall, new Vector3(0, 0.05f, z - 0.02f), new Vector3(4.6f, 0.12f, 0.24f), frame);
        WorldBuilder.Box(wall, new Vector3(-2.25f, 1.75f, z - 0.02f), new Vector3(0.12f, 3.5f, 0.24f), frame);
        WorldBuilder.Box(wall, new Vector3(2.25f, 1.75f, z - 0.02f), new Vector3(0.12f, 3.5f, 0.24f), frame);
        var tex = Resources.Load<Texture2D>("Poses/" + RunMoveInfo.Poses[cue.variant % RunMoveInfo.Poses.Length]);
        if (tex)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(wall);
            q.transform.position = new Vector3(0, 1.75f, z - 0.12f);
            q.transform.localScale = new Vector3(3.2f, 3.2f, 1);
            q.GetComponent<Renderer>().sharedMaterial = HalloweenWorld.Transparent(tex, Color.white);
        }
    }

    // Minecraft-style versions
    bool BuildMc(RunLibrary lib, RunCue cue, float z, Transform root, System.Random rnd)
    {
        var th = WorldBuilder.Theme;
        switch (cue.move)
        {
            case RunMove.Jump:
            {
                string a = th == RunTheme.McCherry ? "cherry_planks" : th == RunTheme.McOcean ? "prismarine_bricks" : th == RunTheme.McCrimson ? "blackstone" : th == RunTheme.McNether ? "nether_bricks" : th == RunTheme.McCaves ? "cobblestone" : th == RunTheme.McTaiga ? "snow" : th == RunTheme.McJungle ? "mossy_cobble" : th == RunTheme.McEnd ? "end_stone" : "planks";
                string b = th == RunTheme.McCherry ? "hay" : th == RunTheme.McOcean ? "coral_pink" : th == RunTheme.McCrimson ? "gold_block" : th == RunTheme.McNether ? "netherrack" : th == RunTheme.McCaves ? "stone" : th == RunTheme.McTaiga ? "spruce_log" : th == RunTheme.McJungle ? "melon" : th == RunTheme.McEnd ? "purpur" : "log_side";
                for (int i = 0; i < 8; i++) McWorld.Block(root, new Vector3(-3.5f + i, 0.5f, z), i % 3 == 1 ? b : a);
                return true;
            }
            case RunMove.Duck:
            {
                string pil = th == RunTheme.McOcean ? "prismarine_bricks" : th == RunTheme.McCrimson ? "blackstone" : th == RunTheme.McNether ? "nether_bricks" : th == RunTheme.McEnd ? "obsidian" : th == RunTheme.McJungle ? "stone_bricks" : "cobblestone";
                string beam = th == RunTheme.McCherry ? "cherry_log" : th == RunTheme.McOcean ? "dark_prismarine" : th == RunTheme.McCrimson ? "crimson_stem" : th == RunTheme.McNether ? "nether_bricks" : th == RunTheme.McTaiga ? "spruce_log" : th == RunTheme.McJungle ? "jungle_log" : th == RunTheme.McEnd ? "purpur" : "log_side";
                foreach (int sd in new[] { -1, 1 })
                    for (int y = 0; y < 2; y++) McWorld.Block(root, new Vector3(sd * 4.5f, 0.5f + y, z), pil);
                for (int i = 0; i < 10; i++) McWorld.Block(root, new Vector3(-4.5f + i, 1.95f, z), beam);
                return true;
            }
            case RunMove.Left:
            case RunMove.Right:
            {
                int free = cue.move == RunMove.Left ? -1 : 1;
                if (th == RunTheme.McCherry || th == RunTheme.McOcean || th == RunTheme.McCrimson)
                {
                    // cherry grove: rolling hay bales, ocean: boats sliding at us, nether: bouncing magma cubes
                    foreach (int lane in new[] { 0, -free })
                    {
                        var c = new Vector3(lane * WorldBuilder.LaneGap, 0, z + 2f);
                        if (th == RunTheme.McCherry)
                        {
                            var o = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(o.GetComponent<Collider>());
                            o.transform.SetParent(root); o.transform.position = c + Vector3.up * 1.1f; o.transform.localScale = new Vector3(2.2f, 1.1f, 2.2f);
                            o.transform.rotation = Quaternion.Euler(0, 0, 90);
                            o.GetComponent<Renderer>().sharedMaterial = BlockWorld.Tex(BlockFigure.Tex.TryGetValue("hay", out var ht) ? ht : null, 2, 2);
                            o.AddComponent<Roll>();
                        }
                        else if (th == RunTheme.McOcean)
                        {
                            var hull = BlockWorld.Tex(BlockFigure.Tex.TryGetValue("planks", out var pt) ? pt : null, 2, 1);
                            WorldBuilder.Box(root, c + new Vector3(0, 0.45f, 0), new Vector3(1.8f, 0.5f, 3.4f), hull);
                            foreach (var sd in new[] { -0.85f, 0.85f }) WorldBuilder.Box(root, c + new Vector3(sd, 0.95f, 0), new Vector3(0.15f, 0.6f, 3.4f), hull);
                            foreach (var sd in new[] { -1.65f, 1.65f }) WorldBuilder.Box(root, c + new Vector3(0, 0.95f, sd), new Vector3(1.8f, 0.6f, 0.15f), hull);
                            WorldBuilder.Box(root, c + new Vector3(0, 1.9f, 0), new Vector3(0.15f, 2.2f, 0.15f), hull);
                            WorldBuilder.Box(root, c + new Vector3(0, 2.2f, 0.1f), new Vector3(1.6f, 1.4f, 0.06f), WorldBuilder.Emissive(Color.white, 0.25f));
                        }
                        else
                        {
                            var mc = BlockFigure.Create(BlockFigure.Kind.MagmaCube, root);
                            mc.transform.position = c; mc.transform.rotation = Quaternion.Euler(0, 180, 0);
                            mc.transform.localScale = Vector3.one * 1.35f; mc.speedMul = 2.2f;
                        }
                    }
                    actor = root;
                    return true;
                }
                if (th == RunTheme.McTaiga || th == RunTheme.McJungle || th == RunTheme.McEnd)
                {
                    // taiga: giant rolling snowballs, jungle: rolling barrels, End: sliding purpur blocks
                    foreach (int lane in new[] { 0, -free })
                    {
                        var c = new Vector3(lane * WorldBuilder.LaneGap, 0, z + 2f);
                        GameObject o;
                        if (th == RunTheme.McTaiga)
                        {
                            o = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(o.GetComponent<Collider>());
                            o.transform.SetParent(root); o.transform.position = c + Vector3.up * 1.1f; o.transform.localScale = Vector3.one * 2.2f;
                            o.GetComponent<Renderer>().sharedMaterial = BlockWorld.Tex(BlockFigure.Tex.TryGetValue("snow", out var st) ? st : null, 2, 2);
                            o.AddComponent<Roll>();
                        }
                        else if (th == RunTheme.McJungle)
                        {
                            o = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(o.GetComponent<Collider>());
                            o.transform.SetParent(root); o.transform.position = c + Vector3.up * 1f; o.transform.localScale = new Vector3(2f, 1.1f, 2f);
                            o.transform.rotation = Quaternion.Euler(0, 0, 90);
                            o.GetComponent<Renderer>().sharedMaterial = BlockWorld.Tex(BlockFigure.Tex.TryGetValue("planks", out var pt) ? pt : null, 2, 2);
                            o.AddComponent<Roll>();
                        }
                        else
                        {
                            o = McWorld.Block(root, c + Vector3.up * 1.1f, "purpur");
                            o.transform.localScale = Vector3.one * 2.2f;
                        }
                    }
                    actor = root;
                    return true;
                }
                // minecarts rolling at us in the center and one side lane
                var iron = WorldBuilder.Mat(new Color(0.55f, 0.56f, 0.6f)); iron.SetFloat("_Metallic", 0.4f);
                var dark = WorldBuilder.Mat(new Color(0.2f, 0.2f, 0.22f));
                foreach (int lane in new[] { 0, -free })
                    for (int k = 0; k < 3; k++)
                    {
                        var c = new Vector3(lane * WorldBuilder.LaneGap, 0, z + 2f + k * 2.4f);
                        WorldBuilder.Box(root, c + new Vector3(0, 0.75f, 0), new Vector3(1.8f, 0.1f, 2f), iron);
                        foreach (var sd in new[] { -0.85f, 0.85f })
                            WorldBuilder.Box(root, c + new Vector3(sd, 1.05f, 0), new Vector3(0.1f, 0.7f, 2f), iron);
                        foreach (var sd in new[] { -0.95f, 0.95f })
                            WorldBuilder.Box(root, c + new Vector3(0, 1.05f, sd), new Vector3(1.8f, 0.7f, 0.1f), iron);
                        foreach (var (wx, wz) in new[] { (-0.7f, -0.6f), (0.7f, -0.6f), (-0.7f, 0.6f), (0.7f, 0.6f) })
                            WorldBuilder.Box(root, c + new Vector3(wx, 0.35f, wz), new Vector3(0.15f, 0.5f, 0.5f), dark);
                        if (k == 1) McWorld.Block(root, c + new Vector3(0, 1.2f, 0), "glowstone".Length > 0 && th == RunTheme.McPlains ? "planks" : "coal_ore");
                    }
                actor = root;
                return true;
            }
            case RunMove.Dash:
            {
                string[] mix = th == RunTheme.McCherry ? new[] { "cherry_planks", "cherry_leaves", "cherry_log", "hay", "grass_side" }
                             : th == RunTheme.McOcean ? new[] { "prismarine", "prismarine_bricks", "coral_blue", "coral_yellow", "sea_lantern" }
                             : th == RunTheme.McCrimson ? new[] { "blackstone", "gilded_blackstone", "nether_wart", "warped_wart", "gold_block" }
                             : th == RunTheme.McTaiga ? new[] { "snow", "ice", "spruce_log", "snow_side" }
                             : th == RunTheme.McJungle ? new[] { "mossy_cobble", "stone_bricks", "melon", "jungle_leaves" }
                             : th == RunTheme.McEnd ? new[] { "end_stone", "purpur", "obsidian" }
                             : th == RunTheme.McNether ? new[] { "netherrack", "nether_bricks", "soul_sand", "glowstone" }
                             : th == RunTheme.McCaves ? new[] { "stone", "cobblestone", "coal_ore", "iron_ore", "diamond_ore", "gold_ore" }
                             : new[] { "dirt", "grass_side", "stone", "cobblestone", "planks" };
                Color[] col = th == RunTheme.McCherry ? new[] { new Color(0.93f, 0.73f, 0.71f), new Color(1f, 0.66f, 0.8f), new Color(0.23f, 0.12f, 0.16f), new Color(0.87f, 0.7f, 0.2f), new Color(0.4f, 0.6f, 0.25f) }
                            : th == RunTheme.McOcean ? new[] { new Color(0.39f, 0.67f, 0.62f), new Color(0.39f, 0.75f, 0.67f), new Color(0.16f, 0.31f, 0.9f), new Color(0.9f, 0.82f, 0.2f), new Color(0.9f, 1f, 0.98f) }
                            : th == RunTheme.McCrimson ? new[] { new Color(0.16f, 0.15f, 0.18f), new Color(0.6f, 0.45f, 0.15f), new Color(0.55f, 0.08f, 0.08f), new Color(0.09f, 0.49f, 0.47f), new Color(1f, 0.8f, 0.2f) }
                            : th == RunTheme.McTaiga ? new[] { Color.white, new Color(0.6f, 0.8f, 1f), new Color(0.35f, 0.25f, 0.15f), new Color(0.85f, 0.9f, 0.95f) }
                            : th == RunTheme.McJungle ? new[] { new Color(0.4f, 0.5f, 0.35f), new Color(0.55f, 0.55f, 0.52f), new Color(0.5f, 0.75f, 0.15f), new Color(0.2f, 0.55f, 0.12f) }
                            : th == RunTheme.McEnd ? new[] { new Color(0.87f, 0.87f, 0.62f), new Color(0.66f, 0.47f, 0.66f), new Color(0.12f, 0.08f, 0.18f) }
                            : th == RunTheme.McNether ? new[] { new Color(0.45f, 0.15f, 0.15f), new Color(0.2f, 0.1f, 0.1f), new Color(0.33f, 0.25f, 0.2f), new Color(1f, 0.85f, 0.45f) }
                            : th == RunTheme.McCaves ? new[] { new Color(0.5f, 0.5f, 0.5f), new Color(0.45f, 0.45f, 0.45f), new Color(0.2f, 0.2f, 0.2f), new Color(0.85f, 0.7f, 0.6f), new Color(0.4f, 0.95f, 0.9f), new Color(1f, 0.85f, 0.2f) }
                            : new[] { new Color(0.52f, 0.37f, 0.26f), new Color(0.4f, 0.6f, 0.25f), new Color(0.5f, 0.5f, 0.5f), new Color(0.45f, 0.45f, 0.45f), new Color(0.64f, 0.5f, 0.3f) };
                var list = new System.Collections.Generic.List<Rigidbody>();
                pieceColors = new System.Collections.Generic.List<Color>();
                for (int x = 0; x < 8; x++)
                    for (int y = 0; y < 4; y++)
                    {
                        int k = rnd.Next(mix.Length);
                        var c = McWorld.Block(root, new Vector3(-3.5f + x, 0.5f + y, z), mix[k], mix[k] == "glowstone" || mix[k] == "sea_lantern" ? 1.2f : mix[k] == "gold_block" ? 0.4f : 0);
                        var rb = c.AddComponent<Rigidbody>(); rb.isKinematic = true;
                        list.Add(rb); pieceColors.Add(col[k]);
                    }
                pieces = list.ToArray();
                return true;
            }
        }
        return false;
    }

    // Demon Hunters (K-pop) versions
    bool BuildKpop(RunLibrary lib, RunCue cue, float z, Transform root, System.Random rnd)
    {
        var th = WorldBuilder.Theme;
        var black = WorldBuilder.Mat(new Color(0.06f, 0.05f, 0.08f)); black.SetFloat("_Smoothness", 0.7f);
        Color pink = new Color(1f, 0.25f, 0.8f), cyan = new Color(0.25f, 0.9f, 1f), violet = new Color(0.75f, 0.3f, 1f);
        switch (cue.move)
        {
            case RunMove.Jump:
            {
                // low LED barrier (in the realm: a row of crystals)
                for (int i = 0; i < 4; i++)
                {
                    float x = -3.3f + i * 2.2f;
                    if (th == RunTheme.DemonRealm)
                    {
                        var c = WorldBuilder.Box(root, new Vector3(x, 0.45f, z), new Vector3(1.9f, 0.9f, 0.5f), KpopWorld.Glow(violet, 2.5f, true));
                        c.transform.rotation = Quaternion.Euler(0, 0, rnd.Next(-6, 6));
                    }
                    else
                    {
                        WorldBuilder.Box(root, new Vector3(x, 0.4f, z), new Vector3(2f, 0.8f, 0.6f), black);
                        WorldBuilder.Box(root, new Vector3(x, 0.4f, z - 0.31f), new Vector3(1.9f, 0.7f, 0.02f), KpopWorld.Glow(i % 2 == 0 ? pink : cyan, 3f, true));
                    }
                }
                return true;
            }
            case RunMove.Duck:
            {
                foreach (int sd in new[] { -1, 1 })
                    WorldBuilder.Box(root, new Vector3(sd * 4.5f, 1.2f, z), new Vector3(0.3f, 2.4f, 0.3f), black);
                WorldBuilder.Box(root, new Vector3(0, 1.75f, z), new Vector3(9.2f, 0.5f, 0.35f), black);
                WorldBuilder.Box(root, new Vector3(0, 1.55f, z - 0.18f), new Vector3(9f, 0.1f, 0.02f), KpopWorld.Glow(th == RunTheme.DemonRealm ? violet : pink, 5f, true));
                WorldBuilder.Box(root, new Vector3(0, 1.95f, z - 0.18f), new Vector3(9f, 0.1f, 0.02f), KpopWorld.Glow(cyan, 5f, true));
                return true;
            }
            case RunMove.Left:
            case RunMove.Right:
            {
                // giant rolling disco balls (dark glowing orbs in the realm)
                int free = cue.move == RunMove.Left ? -1 : 1;
                foreach (int lane in new[] { 0, -free })
                {
                    var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(b.GetComponent<Collider>());
                    b.transform.SetParent(root);
                    b.transform.position = new Vector3(lane * WorldBuilder.LaneGap, 1.15f, z + 2f);
                    b.transform.localScale = Vector3.one * 2.3f;
                    Material m;
                    if (th == RunTheme.DemonRealm) { m = WorldBuilder.Emissive(new Color(0.25f, 0.05f, 0.35f), 0.6f); m.SetColor("_EmissionColor", violet * 1.4f); }
                    else
                    {
                        m = WorldBuilder.Mat(new Color(0.85f, 0.85f, 0.95f));
                        m.SetFloat("_Metallic", 1f); m.SetFloat("_Smoothness", 0.95f);
                        if (lib.ledTex) { m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", lib.ledTex); m.SetColor("_EmissionColor", Color.white * 0.9f); }
                    }
                    b.GetComponent<Renderer>().sharedMaterial = m;
                    b.AddComponent<Roll>();
                }
                actor = root;
                return true;
            }
            case RunMove.Dash:
            {
                // a wall of LED panels that shatters into colored pixels
                Color[] cols = th == RunTheme.DemonRealm ? new[] { violet, new Color(0.4f, 0.1f, 0.5f), cyan } : new[] { pink, cyan, violet, new Color(1f, 0.85f, 0.25f) };
                var list = new System.Collections.Generic.List<Rigidbody>();
                pieceColors = new System.Collections.Generic.List<Color>();
                for (int x = 0; x < 8; x++)
                    for (int y = 0; y < 4; y++)
                    {
                        var col = cols[(x + y * 2) % cols.Length];
                        var c = WorldBuilder.Box(root, new Vector3(-3.5f + x, 0.5f + y, z), Vector3.one * 0.95f, KpopWorld.Glow(col, 1.6f, true));
                        var rb = c.AddComponent<Rigidbody>(); rb.isKinematic = true;
                        list.Add(rb); pieceColors.Add(col);
                    }
                pieces = list.ToArray();
                return true;
            }
        }
        return false;
    }

    // dino versions
    bool BuildDino(RunLibrary lib, RunCue cue, float z, Transform root, System.Random rnd)
    {
        switch (cue.move)
        {
            case RunMove.Jump:
            {
                var log = HPut(lib, rnd.NextDouble() < 0.5 ? "log_large" : "log", new Vector3(0, 0, z), 0, 1f, root);
                if (log) AcrossRoad(log, 8.6f, 0.95f, 0f, z);
                foreach (int sd in new[] { -1, 1 })
                    HPut(lib, "rock_smallA", new Vector3(sd * 4.6f, 0, z + 0.4f), rnd.Next(360), 0.8f, root);
                return true;
            }
            case RunMove.Duck:
            {
                // branch with hanging vines between two rock pillars
                foreach (int sd in new[] { -1, 1 })
                    HPut(lib, "rock_tallB", new Vector3(sd * 4.7f, 0, z), rnd.Next(360), 2.4f, root);
                var branch = HPut(lib, "log", new Vector3(0, 0, z), 0, 1f, root);
                if (branch) AcrossRoad(branch, 9.4f, 0.45f, 1.45f, z);
                var vine = WorldBuilder.Mat(new Color(0.2f, 0.62f, 0.2f));
                var leaf = WorldBuilder.Mat(new Color(0.35f, 0.8f, 0.28f));
                for (int i = 0; i < 9; i++)
                {
                    float x = -3.8f + i * 0.95f, len = 0.25f + (float)rnd.NextDouble() * 0.2f;
                    WorldBuilder.Box(root, new Vector3(x, 1.45f - len * 0.5f, z), new Vector3(0.06f, len, 0.06f), vine);
                    WorldBuilder.Box(root, new Vector3(x, 1.45f - len, z), new Vector3(0.22f, 0.1f, 0.22f), leaf);
                }
                return true;
            }
            case RunMove.Left:
            case RunMove.Right:
            {
                // triceratops stampede in the center and one side lane
                int free = cue.move == RunMove.Left ? -1 : 1;
                foreach (int lane in new[] { 0, -free })
                    DinoWorld.Dino("Triceratops", new Vector3(lane * WorldBuilder.LaneGap, 0, z + 2.5f), 180, 2.3f, root, "run", "walk");
                actor = root;
                return true;
            }
            case RunMove.Dash:
            {
                var stone = HalloweenWorld.TexMat(lib.stoneTex, 0.5f, 0.5f, Color.white, 0, 0.1f);
                var list = new System.Collections.Generic.List<Rigidbody>();
                for (int x = 0; x < 8; x++)
                    for (int y = 0; y < 4; y++)
                    {
                        var c = WorldBuilder.Box(root, new Vector3(-3.5f + x, 0.5f + y, z), Vector3.one * 0.98f, stone);
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

    // Halloween Quest versions: fair / witch forest / moon castle
    bool BuildSpooky(RunLibrary lib, RunCue cue, float z, Transform root, System.Random rnd)
    {
        var th = WorldBuilder.Theme;
        Color fx = th == RunTheme.WitchForest ? SpookyWorld.Lime : th == RunTheme.MoonCastle ? SpookyWorld.Purple : SpookyWorld.Orange;
        burstColor = fx;
        switch (cue.move)
        {
            case RunMove.Jump:
            {
                if (th == RunTheme.WitchForest)
                {
                    // fallen mossy log with glowing toadstools
                    var bark = SpookyWorld.Lit(new Color(0.36f, 0.23f, 0.14f), 0.15f);
                    SpookyWorld.Cyl(root, new Vector3(0, 0.42f, z), 0.84f, 9f, bark, Quaternion.Euler(0, 0, 90));
                    foreach (int sd in new[] { -1, 1 })
                        SpookyWorld.Cyl(root, new Vector3(sd * 4.51f, 0.42f, z), 0.76f, 0.04f, SpookyWorld.Lit(new Color(0.75f, 0.58f, 0.36f)), Quaternion.Euler(0, 0, 90), false);
                    var moss = SpookyWorld.Lit(new Color(0.25f, 0.6f, 0.25f), 0.1f);
                    for (int i = 0; i < 6; i++)
                        SpookyWorld.Box(root, new Vector3(-3.6f + i * 1.45f + (float)rnd.NextDouble() * 0.4f, 0.8f, z), new Vector3(0.7f, 0.1f, 0.5f), moss, false);
                    for (int i = 0; i < 5; i++)
                    {
                        float x = -3.8f + i * 1.9f;
                        var c = SpookyWorld.Party[(i + 2) % SpookyWorld.Party.Length];
                        SpookyWorld.Cyl(root, new Vector3(x, 0.9f, z), 0.07f, 0.16f, SpookyWorld.Lit(new Color(0.95f, 0.92f, 0.85f)), null, false);
                        SpookyWorld.Ball(root, new Vector3(x, 0.99f, z), new Vector3(0.26f, 0.14f, 0.26f), SpookyWorld.Glow(c, 2.5f, true), false);
                    }
                }
                else if (th == RunTheme.MoonCastle)
                {
                    // low stone wall lined with candles
                    SpookyWorld.Box(root, new Vector3(0, 0.36f, z), new Vector3(8.9f, 0.72f, 0.7f), SpookyWorld.Tex("castlewall", 4, 0.5f, Color.white, 0, 0.15f));
                    var wax = SpookyWorld.Lit(new Color(1f, 0.95f, 0.85f), 0.4f);
                    for (int i = 0; i < 7; i++)
                    {
                        float x = -3.9f + i * 1.3f;
                        SpookyWorld.Cyl(root, new Vector3(x, 0.83f, z), 0.12f, 0.22f, wax, null, false);
                        SpookyWorld.Ball(root, new Vector3(x, 0.99f, z), new Vector3(0.09f, 0.14f, 0.09f), SpookyWorld.Glow(new Color(1f, 0.7f, 0.3f), 7f, true), false);
                    }
                }
                else
                {
                    // striped fun-fair barrier with marquee bulbs and jack-o'-lanterns at the ends
                    SpookyWorld.Box(root, new Vector3(0, 0.38f, z), new Vector3(8.8f, 0.76f, 0.5f), SpookyWorld.Tex("tent", 4, 1, Color.white, 0, 0.35f));
                    for (int i = 0; i < 12; i++)
                        SpookyWorld.Ball(root, new Vector3(-4.1f + i * 0.745f, 0.84f, z - 0.1f), Vector3.one * 0.16f, SpookyWorld.Glow(i % 2 == 0 ? SpookyWorld.Gold : SpookyWorld.Orange, 6f, true), false);
                    foreach (int sd in new[] { -1, 1 })
                    {
                        var pk = HPut(lib, "pumpkin-carved", new Vector3(sd * 4.25f, 0.76f, z), 180, 0.55f, root);
                        if (pk) SpookyWorld.Box(root, pk.transform.position + Vector3.up * 0.25f, Vector3.one * 0.22f, SpookyWorld.Glow(SpookyWorld.Orange, 3f, true), false);
                    }
                }
                return true;
            }
            case RunMove.Duck:
            {
                if (th == RunTheme.WitchForest)
                {
                    // twisted branch between two old trunks, toadstools and lanterns on top
                    var bark = SpookyWorld.Lit(new Color(0.3f, 0.2f, 0.13f), 0.15f);
                    foreach (int sd in new[] { -1, 1 })
                        SpookyWorld.Cyl(root, new Vector3(sd * 4.9f, 2f, z), 0.9f, 4f, bark);
                    SpookyWorld.Cyl(root, new Vector3(0, 1.72f, z), 0.5f, 10.2f, bark, Quaternion.Euler(0, 0, 90));
                    SpookyWorld.Box(root, new Vector3(0, 1.49f, z - 0.2f), new Vector3(9.4f, 0.05f, 0.05f), SpookyWorld.Glow(SpookyWorld.Lime, 4f, true), false);
                    for (int i = 0; i < 4; i++)
                    {
                        float x = -3.3f + i * 2.2f;
                        SpookyWorld.Cyl(root, new Vector3(x, 2.05f, z), 0.08f, 0.2f, SpookyWorld.Lit(new Color(0.95f, 0.92f, 0.85f)), null, false);
                        SpookyWorld.Ball(root, new Vector3(x, 2.17f, z), new Vector3(0.34f, 0.18f, 0.34f), SpookyWorld.Glow(SpookyWorld.Party[(i + 1) % SpookyWorld.Party.Length], 2.5f, true), false);
                    }
                }
                else if (th == RunTheme.MoonCastle)
                {
                    // half-lowered portcullis: spikes end at 1.45 m
                    var wall = SpookyWorld.Tex("castlewall", 1, 3, Color.white, 0, 0.15f);
                    var iron = SpookyWorld.Lit(new Color(0.2f, 0.18f, 0.24f), 0.5f, 0.8f);
                    foreach (int sd in new[] { -1, 1 })
                        SpookyWorld.Box(root, new Vector3(sd * 4.85f, 2.8f, z), new Vector3(0.75f, 5.6f, 0.9f), wall);
                    SpookyWorld.Box(root, new Vector3(0, 5.2f, z), new Vector3(10.4f, 0.9f, 0.9f), SpookyWorld.Tex("castlewall", 4, 0.4f, Color.white, 0, 0.15f));
                    for (int i = 0; i < 17; i++)
                    {
                        float x = -4.4f + i * 0.55f;
                        SpookyWorld.Box(root, new Vector3(x, 3.2f, z), new Vector3(0.09f, 3.2f, 0.09f), iron, false);
                        var sp = SpookyWorld.ConeObj(root, new Vector3(x, 1.62f, z), 0.16f, 0.17f, iron, 6, 0, false);
                        sp.transform.rotation = Quaternion.Euler(180, 0, 0);
                    }
                    foreach (float y in new[] { 1.66f, 2.6f, 3.6f, 4.5f })
                        SpookyWorld.Box(root, new Vector3(0, y, z), new Vector3(9f, 0.09f, 0.11f), iron, false);
                    SpookyWorld.Box(root, new Vector3(0, 1.62f, z - 0.08f), new Vector3(9f, 0.05f, 0.04f), SpookyWorld.Glow(SpookyWorld.Purple, 5f, true), false);
                }
                else
                {
                    // "BOO! DUCK! BOO!" marquee sign on striped posts
                    var stripe = SpookyWorld.Tex("tent", 1, 3, Color.white, 0, 0.35f);
                    foreach (int sd in new[] { -1, 1 })
                        SpookyWorld.Box(root, new Vector3(sd * 4.75f, 1.2f, z), new Vector3(0.35f, 2.4f, 0.35f), stripe);
                    SpookyWorld.Box(root, new Vector3(0, 1.84f, z), new Vector3(9.4f, 0.78f, 0.22f), SpookyWorld.Lit(new Color(0.18f, 0.05f, 0.28f)));
                    var sign = SpookyWorld.Tex("boosign", 1, 1, Color.white, 1.1f, 0.3f);
                    foreach (int sd in new[] { -1, 1 })
                        SpookyWorld.Quad(root, new Vector3(sd * 2.3f, 1.84f, z - 0.12f), Quaternion.identity, new Vector2(4.5f, 0.74f), sign);
                    SpookyWorld.Box(root, new Vector3(0, 1.46f, z - 0.12f), new Vector3(9.3f, 0.04f, 0.03f), SpookyWorld.Glow(SpookyWorld.Gold, 5f, true), false);
                }
                return true;
            }
            case RunMove.Left:
            case RunMove.Right:
            {
                int free = cue.move == RunMove.Left ? -1 : 1;
                foreach (int lane in new[] { 0, -free })
                {
                    var c = new Vector3(lane * WorldBuilder.LaneGap, 0, z + 2.5f);
                    GameObject frog = th == RunTheme.WitchForest ? SpookyWorld.Critter(lib, "Frog", c, 180, 2.3f, root, "jump", "idle") : null;
                    if (frog)
                    {
                        // giant frogs hopping at us
                        SpookyWorld.Spookify(frog, SpookyWorld.Lime, 0.25f);
                        var hop = frog.AddComponent<Hop>(); hop.height = 0.8f; hop.period = 0.55f; hop.phase = lane * 0.37f;
                    }
                    else
                    {
                        // giant rolling skulls with glowing eyes
                        var sk = new GameObject("Skull").transform; sk.SetParent(root); sk.position = c + Vector3.up * 1.15f;
                        var p = sk.position;
                        var bone = SpookyWorld.Lit(new Color(0.95f, 0.92f, 0.82f), 0.4f);
                        var hole = SpookyWorld.Lit(new Color(0.08f, 0.04f, 0.1f), 0.2f);
                        SpookyWorld.Ball(sk, p, new Vector3(2.3f, 2.2f, 2.3f), bone);
                        foreach (int sd in new[] { -1, 1 })
                        {
                            SpookyWorld.Ball(sk, p + new Vector3(sd * 0.45f, 0.15f, -0.95f), new Vector3(0.62f, 0.6f, 0.4f), hole, false);
                            SpookyWorld.Ball(sk, p + new Vector3(sd * 0.45f, 0.15f, -1.1f), Vector3.one * 0.22f, SpookyWorld.Glow(th == RunTheme.MoonCastle ? SpookyWorld.Purple : SpookyWorld.Lime, 6f, true), false);
                        }
                        var nose = SpookyWorld.Box(sk, p + new Vector3(0, -0.28f, -1.08f), new Vector3(0.22f, 0.22f, 0.1f), hole, false);
                        nose.transform.rotation = Quaternion.Euler(0, 0, 45);
                        for (int i = 0; i < 5; i++)
                            SpookyWorld.Box(sk, p + new Vector3(-0.4f + i * 0.2f, -0.62f, -0.98f), new Vector3(0.15f, 0.22f, 0.12f), bone, false);
                        sk.gameObject.AddComponent<Roll>();
                    }
                }
                actor = root;
                return true;
            }
            case RunMove.Dash:
            {
                // rune stones / castle blocks / candy crates that burst apart
                var list = new System.Collections.Generic.List<Rigidbody>();
                bool candy = th == RunTheme.SpookyCarnival;
                if (candy) pieceColors = new System.Collections.Generic.List<Color>();
                for (int x = 0; x < 8; x++)
                    for (int y = 0; y < 4; y++)
                    {
                        Material m = th == RunTheme.WitchForest ? SpookyWorld.Tex("runes", 1, 1, Color.white, 2.2f, 0.2f)
                                   : th == RunTheme.MoonCastle ? SpookyWorld.Tex("castlewall", 0.25f, 0.5f, Color.white, 0, 0.15f)
                                   : SpookyWorld.Glow(SpookyWorld.Party[(x + y * 3) % SpookyWorld.Party.Length], 0.8f, true);
                        var c = WorldBuilder.Box(root, new Vector3(-3.5f + x, 0.5f + y, z), Vector3.one * 0.97f, m);
                        c.AddComponent<BoxCollider>();
                        var rb = c.AddComponent<Rigidbody>(); rb.isKinematic = true;
                        list.Add(rb);
                        if (candy) pieceColors.Add(SpookyWorld.Party[(x + y * 3) % SpookyWorld.Party.Length]);
                    }
                pieces = list.ToArray();
                return true;
            }
        }
        return false;
    }

    Color burstColor = new Color(1f, 0.8f, 0.3f);

    // Red Light Green Light versions: playground / toy store / candy factory, plus the FREEZE stop line
    bool BuildToy(RunLibrary lib, RunCue cue, float z, Transform root, System.Random rnd)
    {
        var th = WorldBuilder.Theme;
        burstColor = SpookyWorld.Party[rnd.Next(SpookyWorld.Party.Length)];
        Material Block() => SpookyWorld.Tex("block" + "ABCDEF"[rnd.Next(6)], 1, 1, Color.white, 0, 0.3f);
        switch (cue.move)
        {
            case RunMove.Freeze:
            {
                // checkered stop line just ahead of where the run halts, traffic lights on both sides
                SpookyWorld.Quad(root, new Vector3(0, 0.03f, z + 3.2f), Quaternion.Euler(90, 0, 0), new Vector2(9f, 1.1f), SpookyWorld.Tex("stopline", 2, 1, Color.white, 0.3f, 0.3f));
                var dark = SpookyWorld.Lit(new Color(0.1f, 0.1f, 0.12f), 0.6f);
                var red = WorldBuilder.Emissive(new Color(1f, 0.15f, 0.12f), 0.3f);
                var green = WorldBuilder.Emissive(new Color(0.2f, 1f, 0.3f), 4f);
                foreach (int sd in new[] { -1, 1 })
                {
                    var x = sd * 4.75f;
                    SpookyWorld.Box(root, new Vector3(x, 1.7f, z + 3.2f), new Vector3(0.2f, 3.4f, 0.2f), dark);
                    SpookyWorld.Box(root, new Vector3(x, 3.75f, z + 3.2f), new Vector3(0.7f, 1.5f, 0.55f), dark);
                    SpookyWorld.Ball(root, new Vector3(x, 4.1f, z + 2.9f), new Vector3(0.5f, 0.5f, 0.2f), red, false);
                    SpookyWorld.Ball(root, new Vector3(x, 3.4f, z + 2.9f), new Vector3(0.5f, 0.5f, 0.2f), green, false);
                }
                var sl = gameObject.AddComponent<StopLight>(); sl.Init(director, cue.time, red, green);
                return true;
            }
            case RunMove.Jump:
                if (th == RunTheme.RlCandyFactory)
                    for (int i = 0; i < 4; i++)
                    {
                        var g = HPut(lib, "donut-sprinkles", new Vector3(-3.3f + i * 2.2f, 0, z), rnd.Next(360), 1f, root);
                        if (g) g.transform.localScale = Vector3.Scale(g.transform.localScale, new Vector3(1.6f, 1f, 1.6f));
                    }
                else
                    for (int i = 0; i < 8; i++) SpookyWorld.Box(root, new Vector3(-3.5f + i, 0.48f, z), Vector3.one * 0.96f, Block());
                return true;
            case RunMove.Duck:
                if (th == RunTheme.RlCandyFactory)
                {
                    var cane = HalloweenWorld.TexMat(lib.stripes, 6, 0.5f, Color.white, 0, 0.7f);
                    WorldBuilder.Box(root, new Vector3(0, 1.65f, z), new Vector3(9.2f, 0.42f, 0.42f), cane);
                    foreach (int sd in new[] { -1, 1 })
                        WorldBuilder.Box(root, new Vector3(sd * 4.5f, 0.95f, z), new Vector3(0.35f, 1.9f, 0.35f), cane);
                }
                else if (th == RunTheme.RlToyStore)
                {
                    foreach (int sd in new[] { -1, 1 })
                        for (int y = 0; y < 2; y++) SpookyWorld.Box(root, new Vector3(sd * 4.85f, 0.48f + y * 0.96f, z), Vector3.one * 0.96f, Block());
                    for (int i = 0; i < 10; i++) SpookyWorld.Box(root, new Vector3(-4.5f + i, 1.94f, z), new Vector3(0.98f, 0.9f, 0.9f), Block());
                }
                else
                {
                    // monkey bars
                    var post = SpookyWorld.Lit(ToyWorld.Blue, 0.5f);
                    foreach (int sd in new[] { -1, 1 })
                        SpookyWorld.Box(root, new Vector3(sd * 4.7f, 1.2f, z), new Vector3(0.28f, 2.4f, 0.28f), post);
                    SpookyWorld.Cyl(root, new Vector3(0, 1.7f, z), 0.45f, 9.6f, SpookyWorld.Lit(ToyWorld.Yellow, 0.6f), Quaternion.Euler(0, 0, 90));
                    for (int i = 0; i < 7; i++)
                        SpookyWorld.Cyl(root, new Vector3(-3.6f + i * 1.2f, 1.7f, z), 0.5f, 0.12f, SpookyWorld.Lit(ToyWorld.Red, 0.6f), Quaternion.Euler(0, 0, 90), false);
                }
                return true;
            case RunMove.Left:
            case RunMove.Right:
            {
                int free = cue.move == RunMove.Left ? -1 : 1;
                foreach (int lane in new[] { 0, -free })
                {
                    if (th == RunTheme.RlToyStore && lib.trains != null && lib.trains.Length > 0)
                    {
                        // toy trains rushing at us
                        var t = WorldBuilder.Spawn(lib.trains[rnd.Next(lib.trains.Length)], new Vector3(lane * WorldBuilder.LaneGap, 0, z + 4f), 180, root);
                        FitWidth(t, 2.2f, true);
                        Paint(t, TrainColors[rnd.Next(TrainColors.Length)]);
                    }
                    else
                    {
                        // giant rolling gumballs (candy) / beach balls
                        var m = th == RunTheme.RlCandyFactory ? SpookyWorld.Lit(SpookyWorld.Party[rnd.Next(SpookyWorld.Party.Length)], 0.95f)
                                                              : SpookyWorld.Tex("beachball", 1, 1, Color.white, 0, 0.7f);
                        var b = SpookyWorld.Ball(root, new Vector3(lane * WorldBuilder.LaneGap, 1.15f, z + 2f), Vector3.one * 2.3f, m);
                        b.AddComponent<Roll>();
                    }
                }
                actor = root;
                return true;
            }
            case RunMove.Dash:
            {
                var list = new System.Collections.Generic.List<Rigidbody>();
                var choc = th == RunTheme.RlCandyFactory ? HalloweenWorld.TexMat(lib.chocolateTex, 0.5f, 0.5f, Color.white, 0, 0.6f) : null;
                for (int x = 0; x < 8; x++)
                    for (int y = 0; y < 4; y++)
                    {
                        var c = WorldBuilder.Box(root, new Vector3(-3.5f + x, 0.5f + y, z), Vector3.one * 0.97f, choc ? choc : Block());
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
            var p = actor.position; p.z = director.Dist(hitTime) + 1.5f + ahead; actor.position = p;
        }
        if (move is RunMove.Left or RunMove.Right && actor && !fired)
        {
            // oncoming train
            float ahead = Mathf.Max(0f, hitTime - t) * 10f;
            var p = actor.position; p.z = director.Dist(hitTime) + ahead; actor.position = p;
        }
        // with a buddy running ahead, the crate wall bursts when the buddy hits it
        float fireAt = move == RunMove.Dash && director.Buddy ? hitTime - director.Buddy.lead : move == RunMove.Mirror ? hitTime - 0.2f : hitTime - 0.05f;
        if (!fired && t >= fireAt) Fire();
    }

    void Fire()
    {
        fired = true;
        if (director.lib && (director.lib.spookyMode || director.lib.toyMode))
        {
            // Halloween Quest game feel: shake + FOV punch + a burst in the world colour
            if (move == RunMove.Dash) { director.Shake(0.13f); director.Kick(8f); RunFx.Burst(transform.position + new Vector3(0, 2f, 0.5f), burstColor, false, 90); }
            if (move == RunMove.Smash) { director.Shake(0.11f); director.Kick(4f); }
            if (move == RunMove.Mirror) director.Kick(5f);
        }
        switch (move)
        {
            case RunMove.Mirror:
                if (wall)
                {
                    for (int i = 0; i < 6; i++)
                        RunFx.Burst(wall.position + new Vector3(-1.8f + i * 0.72f, 1.75f, 0), wallColor, true, 16, 0.22f);
                    wall.gameObject.SetActive(false);
                }
                director.Hud.Flash();
                director.Say("perfect");
                break;
            case RunMove.Dash:
                if (pieceColors != null)
                {
                    for (int i = 0; i < pieces.Length; i++)
                    {
                        if (!pieces[i]) continue;
                        RunFx.Burst(pieces[i].position, pieceColors[i], true, 14, 0.18f);
                        pieces[i].gameObject.SetActive(false);
                    }
                    director.PlaySfx(director.lib ? director.lib.sfxBreak : null, 1f);
                    break;
                }
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
                    if (figure) figure.SetState("HitReact");
                    PlayClip("hit", "fall", "die", "death");
                    // knocked away in an arc, lands, disappears in a puff - never left hanging in the sky
                    var a = actor;
                    var end = a.position + new Vector3(Random.Range(-1f, 1f) < 0 ? -4.5f : 4.5f, 0f, 14f);
                    a.DOJump(end, 3.2f, 1, 0.85f).SetLink(a.gameObject).OnComplete(() =>
                    {
                        if (!a) return;
                        RunFx.Burst(a.position + Vector3.up * 0.8f, new Color(0.9f, 0.9f, 0.9f), true);
                        a.gameObject.SetActive(false);
                    });
                    a.DORotate(new Vector3(-80, 0, Random.Range(-60f, 60f)), 0.85f, RotateMode.LocalAxisAdd).SetLink(a.gameObject);
                    director.PlaySfx(director.lib ? director.lib.sfxHit : null, 0.8f);
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

    public static Bounds PosedBoundsOf(GameObject go) => PosedBounds(go);

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

    public static void Burst(Vector3 pos, Color c, bool pixels = false, int count = 60, float size = 0f)
    {
        var go = new GameObject("Burst");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.4f; main.loop = false;
        main.startLifetime = 0.6f; main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 14f);
        main.startSize = size > 0 ? new ParticleSystem.MinMaxCurve(size * 0.6f, size) : new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        if (pixels) { main.gravityModifier = 1.6f; main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f); main.startLifetime = 0.9f; }
        main.startColor = c;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.5f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        if (spark && !pixels) m.SetTexture("_BaseMap", spark);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 2);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_DstBlend", (int)(pixels ? UnityEngine.Rendering.BlendMode.Zero : UnityEngine.Rendering.BlendMode.One));
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

// FREEZE traffic lights: red from the icon until GREEN LIGHT, green otherwise
public class StopLight : MonoBehaviour
{
    RunDirector d; float at; Material red, green;
    public void Init(RunDirector dir, float time, Material r, Material g) { d = dir; at = time; red = r; green = g; }
    void LateUpdate()
    {
        if (!d) return;
        float u = d.SongTime - at;
        bool stop = u > -1.3f && u < d.FreezeHold;
        red.SetColor("_EmissionColor", new Color(1f, 0.15f, 0.12f) * (stop ? 6f : 0.3f));
        green.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.3f) * (stop ? 0.3f : 4f));
    }
}
