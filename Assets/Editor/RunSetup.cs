using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RunSetup
{
    const string K = "Assets/ThirdParty/Kenney/";
    const string Q = "Assets/ThirdParty/Quaternius/Monsters/";

    [MenuItem("SubwayRun/1. Import Monsters As Legacy")]
    public static void MonstersLegacy()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Q }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is ModelImporter mi && mi.animationType != ModelImporterAnimationType.Legacy)
            {
                mi.animationType = ModelImporterAnimationType.Legacy;
                mi.importAnimation = true;
                mi.SaveAndReimport();
            }
        }
        Debug.Log("Monsters set to Legacy animation");
    }

    [MenuItem("SubwayRun/2. Build Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 72f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 400f;
        camGo.AddComponent<AudioListener>();
        var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        camGo.transform.position = new Vector3(0, 1.7f, 0);

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

        // post: bloom + tonemapping
        var profilePath = "Assets/Settings/RunVolume.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (!profile)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true); bloom.intensity.Override(1.2f); bloom.threshold.Override(0.95f);
            var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var ca = profile.Add<ColorAdjustments>(true); ca.saturation.Override(18f); ca.contrast.Override(10f);
            var vig = profile.Add<Vignette>(true); vig.intensity.Override(0.25f);
            System.IO.Directory.CreateDirectory("Assets/Settings");
            AssetDatabase.CreateAsset(profile, profilePath);
        }
        var vol = new GameObject("PostVolume").AddComponent<Volume>();
        vol.isGlobal = true; vol.sharedProfile = profile;

        var dir = new GameObject("RunDirector");
        var lib = dir.AddComponent<RunLibrary>();
        lib.track = Load(K + "train-kit/track.fbx");
        lib.commercial = LoadMany(K + "city-commercial", "building-") .Where(p => !p.name.Contains("skyscraper") && !p.name.StartsWith("low")).ToArray();
        lib.skyscrapers = LoadMany(K + "city-commercial", "building-skyscraper");
        lib.suburban = LoadMany(K + "city-suburban", "building-type");
        lib.trees = LoadMany(K + "city-suburban", "tree-");
        lib.pole = Load(K + "city-roads/light-curved.fbx");
        lib.pillar = Load(K + "city-roads/bridge-pillar.fbx");
        lib.barrier = Load(K + "city-roads/construction-barrier.fbx");
        lib.cone = Load(K + "city-roads/construction-cone.fbx");
        lib.gantry = Load(K + "city-roads/sign-highway-wide.fbx");
        lib.trains = new[] { "train-electric-subway-a", "train-electric-subway-b", "train-electric-subway-c", "train-electric-city-a" }
            .Select(n => Load(K + "train-kit/" + n + ".fbx")).Where(p => p).ToArray();
        lib.monsters = LoadMany(Q + "Big", "").Concat(LoadMany(Q + "Blob", "")).ToArray();
        lib.spark = AssetDatabase.LoadAssetAtPath<Texture2D>(K + "particles/flare_01.png");
        lib.smoke = AssetDatabase.LoadAssetAtPath<Texture2D>(K + "particles/smoke_01.png");

        // animated trainer silhouettes for the level cards (from the BEAT TUNNEL project)
        const string MX = "Assets/ThirdParty/Mixamo/";
        lib.demoModel = Load(MX + "Characters/Jennifer.fbx");
        AnimationClip Clip(string f) => AssetDatabase.LoadAllAssetsAtPath(MX + "Clips/" + f + ".fbx").OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
        lib.demoClips = new[] { "X Bot@Jump In Place A", "X Bot@Air Squat", "X Bot@Side Step Short Left", "X Bot@Side Step Short Right", "Running Up Stairs", "X Bot@Jab Cross", "X Bot@Jumping Jacks" }.Select(Clip).ToArray();
        lib.silBody = Shader.Find("Tunnel/SilhouetteBodyV5");
        lib.silGlow = Shader.Find("Tunnel/SilhouetteGlowV5");

        var d = dir.AddComponent<RunDirector>();
        d.lib = lib;
        var clips = Enumerable.Range(1, 3).Select(i => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Tracks/run1_{i}.wav")).ToArray();
        d.levels = new[]
        {
            new RunLevel { title = "LEVEL 1", theme = RunTheme.Day, clip = clips[0],
                moves = new[] { RunMove.Jump, RunMove.Duck }, newMoves = new[] { RunMove.Jump, RunMove.Duck } },
            new RunLevel { title = "LEVEL 2", theme = RunTheme.Tunnel, clip = clips[1],
                moves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Left, RunMove.Right, RunMove.Dash }, newMoves = new[] { RunMove.Left, RunMove.Right, RunMove.Dash } },
            new RunLevel { title = "LEVEL 3", theme = RunTheme.Neon, clip = clips[2],
                moves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Left, RunMove.Right, RunMove.Dash, RunMove.Smash }, newMoves = new[] { RunMove.Smash } },
        };

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/SubwayRun.unity");
        Debug.Log($"SubwayRun scene built: trains {lib.trains.Length}, monsters {lib.monsters.Length}, commercial {lib.commercial.Length}");
    }

    const string CW = "Assets/ThirdParty/Quaternius/CubeWorld/";
    const string VX = "Assets/ThirdParty/Kenney/voxel/Tiles/";

    [MenuItem("SubwayRun/Block World/1. Import Settings (pixel + legacy anims)")]
    public static void BlockImport()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { VX, CW }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is TextureImporter ti && ti.filterMode != FilterMode.Point)
            {
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = true;
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
        }
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { CW + "Enemies", CW + "Animals" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is ModelImporter mi && mi.animationType != ModelImporterAnimationType.Legacy)
            {
                mi.animationType = ModelImporterAnimationType.Legacy;
                mi.importAnimation = true;
                mi.SaveAndReimport();
            }
        }
        Debug.Log("Block world import settings applied");
    }

    [MenuItem("SubwayRun/Block World/2. Build Scene")]
    public static void BuildBlockScene()
    {
        BuildScene();   // same camera/post/director setup, then swap content
        var d = Object.FindAnyObjectByType<RunDirector>();
        var lib = d.lib;
        Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>(VX + n + ".png");
        lib.blockMode = true;
        lib.atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(CW + "Atlas.png");
        lib.track = Load(CW + "Environment/Rail_Straight.fbx");
        lib.grassTop = T("grass_top"); lib.dirtGrass = T("dirt_grass"); lib.dirt = T("dirt"); lib.gravel = T("gravel_stone");
        lib.stone = T("stone"); lib.planks = T("wood"); lib.trunk = T("trunk_side"); lib.leaves = T("leaves");
        lib.lava = T("lava"); lib.netherrack = T("redstone"); lib.sand = T("sand");
        lib.ores = new[] { "stone_coal", "stone_iron", "stone_gold", "stone_diamond", "greystone_ruby", "stone_silver" }.Select(T).Where(t => t).ToArray();
        lib.blockTrees = new[] { "Tree_1", "Tree_2", "Tree_3" }.Select(n => Load(CW + "Environment/" + n + ".fbx")).Where(p => p).ToArray();
        lib.animals = new[] { "Pig", "Sheep", "Chicken", "Horse", "Dog", "Cat" }.Select(n => Load(CW + "Animals/" + n + ".fbx")).Where(p => p).ToArray();
        lib.mineProps = new[] { "Crystal_Big", "Crystal_Small", "Chest_Closed", "Rock1", "Rock2", "Mushroom" }.Select(n => Load(CW + "Environment/" + n + ".fbx")).Where(p => p).ToArray();
        lib.netherProps = new[] { "DeadTree_1", "DeadTree_2", "DeadTree_3", "Crystal_Big" }.Select(n => Load(CW + "Environment/" + n + ".fbx")).Where(p => p).ToArray();
        lib.trains = new[] { Load(CW + "Environment/Cart.fbx") }.Where(p => p).ToArray();
        lib.buddy = Load(CW + "Characters/Character_Male_1.fbx");
        lib.flowers = new[] { "Flowers_1", "Flowers_2", "Grass_Big", "Grass_Small", "Bush", "Mushroom", "Plant_2", "Plant_3" }.Select(n => Load(CW + "Environment/" + n + ".fbx")).Where(p => p).ToArray();

        // punchier colors for the block world: no ACES (it washes the pixel colors out), more saturation
        var bp = "Assets/Settings/BlockVolume.asset";
        var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(bp);
        if (!prof)
        {
            prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = prof.Add<Bloom>(true); bloom.intensity.Override(0.7f); bloom.threshold.Override(1.0f);
            var tone = prof.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.Neutral);
            var ca = prof.Add<ColorAdjustments>(true); ca.saturation.Override(22f); ca.contrast.Override(14f); ca.postExposure.Override(0.1f);
            var vig = prof.Add<Vignette>(true); vig.intensity.Override(0.2f);
            AssetDatabase.CreateAsset(prof, bp);
        }
        Object.FindAnyObjectByType<Volume>().sharedProfile = prof;
        lib.monsters = new[] { "Zombie", "Skeleton", "Skeleton_Armor", "Goblin", "Demon", "Giant" }.Select(n => Load(CW + "Enemies/" + n + ".fbx")).Where(p => p).ToArray();

        d.videoTitle = "BLOCK WORLD RUN";
        d.welcomeClip = "welcome_block";
        d.level2Clip = "level2_block";
        var clips = Enumerable.Range(1, 3).Select(i => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Tracks/run2_{i}.wav")).ToArray();
        d.levels[0].theme = RunTheme.Village; d.levels[0].clip = clips[0];
        d.levels[1].theme = RunTheme.Mine;    d.levels[1].clip = clips[1];
        d.levels[2].theme = RunTheme.Nether;  d.levels[2].clip = clips[2];
        EditorSceneManager.SaveScene(d.gameObject.scene, "Assets/Scenes/BlockWorldRun.unity");
        Debug.Log($"BlockWorldRun scene built: carts {lib.trains.Length}, mobs {lib.monsters.Length}, ores {lib.ores.Length}, trees {lib.blockTrees.Length}");
    }

    [MenuItem("SubwayRun/Obby/Build Scene")]
    public static void BuildObbyScene()
    {
        BuildBlockScene();   // reuse block setup (mobs, buddy, atlas), then switch to obby content
        var d = Object.FindAnyObjectByType<RunDirector>();
        var lib = d.lib;
        lib.blockMode = false;
        lib.obbyMode = true;
        lib.studs = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Obby/studs.png");
        lib.arrows = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Obby/arrows.png");
        lib.track = null;   // no rails on an obby
        lib.monsters = new[] { "Zombie", "Goblin", "Demon", "Skeleton" }.Select(n => Load(CW + "Enemies/" + n + ".fbx")).Where(p => p).ToArray();
        lib.buddy = Load(CW + "Characters/Character_Male_2.fbx");
        d.videoTitle = "OBBY RUN";
        d.welcomeClip = "welcome_obby";
        d.level1Clip = "level1_obby";
        d.level2Clip = "level2";
        var clips = Enumerable.Range(1, 3).Select(i => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Tracks/run3_{i}.wav")).ToArray();
        d.levels[0].theme = RunTheme.SkyObby;     d.levels[0].clip = clips[0];
        d.levels[1].theme = RunTheme.LavaObby;    d.levels[1].clip = clips[1];
        d.levels[2].theme = RunTheme.RainbowObby; d.levels[2].clip = clips[2];
        EditorSceneManager.SaveScene(d.gameObject.scene, "Assets/Scenes/ObbyRun.unity");
        Debug.Log("ObbyRun scene built");
    }

    const string GY = "Assets/ThirdParty/Kenney/graveyard/";
    const string HT = "Assets/Textures/Halloween/";

    [MenuItem("SubwayRun/Halloween/Build Scene")]
    public static void BuildHalloweenScene()
    {
        // legacy animations for the Kenney characters
        foreach (var n in new[] { "character-ghost", "character-keeper", "character-skeleton", "character-vampire", "character-zombie" })
            if (AssetImporter.GetAtPath(GY + n + ".fbx") is ModelImporter mi && mi.animationType != ModelImporterAnimationType.Legacy)
            {
                mi.animationType = ModelImporterAnimationType.Legacy; mi.importAnimation = true; mi.SaveAndReimport();
            }
        BuildBlockScene();
        var d = Object.FindAnyObjectByType<RunDirector>();
        var lib = d.lib;
        lib.blockMode = false; lib.obbyMode = false;
        lib.halloweenMode = true;
        lib.track = null;
        lib.kit = AssetDatabase.FindAssets("t:Model", new[] { GY }).Select(AssetDatabase.GUIDToAssetPath).Select(Load).Where(p => p).ToArray();
        Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>(HT + n + ".png");
        lib.wallpaper = T("wallpaper"); lib.darkWood = T("wood"); lib.cobweb = T("cobweb"); lib.moon = T("moon"); lib.rain = T("rain"); lib.bat = T("bat");
        lib.portraits = Enumerable.Range(0, 4).Select(i => T("portrait" + i)).Where(t => t).ToArray();
        lib.weapon = Load(GY + "shovel.fbx");
        lib.buddy = Load(GY + "character-keeper.fbx");
        lib.buddyYaw = 0f;
        lib.monsters = new[] { "character-zombie", "character-skeleton", "character-vampire" }.Select(n => Load(GY + n + ".fbx")).Where(p => p).ToArray();

        var hp = "Assets/Settings/HalloweenVolume.asset";
        var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(hp);
        if (!prof)
        {
            prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = prof.Add<Bloom>(true); bloom.intensity.Override(1.6f); bloom.threshold.Override(0.8f); bloom.scatter.Override(0.75f);
            var tone = prof.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var ca = prof.Add<ColorAdjustments>(true); ca.saturation.Override(12f); ca.contrast.Override(22f); ca.postExposure.Override(0.35f);
            var vig = prof.Add<Vignette>(true); vig.intensity.Override(0.4f); vig.smoothness.Override(0.5f);
            var grain = prof.Add<FilmGrain>(true); grain.intensity.Override(0.25f);
            var chrom = prof.Add<ChromaticAberration>(true); chrom.intensity.Override(0.15f);
            AssetDatabase.CreateAsset(prof, hp);
        }
        Object.FindAnyObjectByType<Volume>().sharedProfile = prof;

        d.videoTitle = "HALLOWEEN RUN";
        d.welcomeClip = "welcome_halloween";
        d.level1Clip = "level1_h"; d.level2Clip = "level2_h"; d.level3Clip = "level3_h";
        var clips = Enumerable.Range(1, 3).Select(i => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Tracks/run4_{i}.wav")).ToArray();
        d.levels[0].title = "LEVEL 1"; d.levels[0].theme = RunTheme.Mansion;     d.levels[0].clip = clips[0];
        d.levels[1].title = "LEVEL 2"; d.levels[1].theme = RunTheme.PumpkinFarm; d.levels[1].clip = clips[1];
        d.levels[2].title = "LEVEL 3"; d.levels[2].theme = RunTheme.Graveyard;   d.levels[2].clip = clips[2];
        EditorSceneManager.SaveScene(d.gameObject.scene, "Assets/Scenes/HalloweenRun.unity");
        Debug.Log($"HalloweenRun scene built: kit {lib.kit.Length}, mobs {lib.monsters.Length}");
    }

    [MenuItem("SubwayRun/Candy/Build Scene")]
    public static void BuildCandyScene()
    {
        BuildBlockScene();
        var d = Object.FindAnyObjectByType<RunDirector>();
        var lib = d.lib;
        lib.blockMode = false; lib.obbyMode = false; lib.halloweenMode = false;
        lib.candyMode = true;
        lib.track = null;
        lib.kit = AssetDatabase.FindAssets("t:Model", new[] { "Assets/ThirdParty/Kenney/food" }).Select(AssetDatabase.GUIDToAssetPath).Select(Load).Where(p => p).ToArray();
        Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Candy/" + n + ".png");
        lib.stripes = T("stripes"); lib.frostMint = T("frosting_mint"); lib.frostPink = T("frosting_pink"); lib.frostBlue = T("frosting_blue");
        lib.chocolateTex = T("chocolate"); lib.rainbowTex = T("rainbow");
        lib.monsters = new[] { "PinkBlob", "GreenBlob", "GreenSpikyBlob", "Mushnub" }.Select(n => Load(Q + "Blob/" + n + ".fbx")).Where(p => p).ToArray();
        lib.buddy = Load(CW + "Characters/Character_Female_1.fbx");
        lib.buddyYaw = 180f;
        d.videoTitle = "CANDY LAND ESCAPE";
        d.welcomeClip = "welcome_candy";
        d.level1Clip = "level1_c"; d.level2Clip = "level2_c"; d.level3Clip = "level3_c";
        var clips = Enumerable.Range(1, 3).Select(i => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Tracks/run5_{i}.wav")).ToArray();
        d.levels[0].theme = RunTheme.GummyForest;      d.levels[0].clip = clips[0];
        d.levels[1].theme = RunTheme.ChocolateFactory; d.levels[1].clip = clips[1];
        d.levels[2].theme = RunTheme.CottonCandySky;   d.levels[2].clip = clips[2]; d.levels[2].bpm = 129.2f;   // Suno made this one slower
        EditorSceneManager.SaveScene(d.gameObject.scene, "Assets/Scenes/CandyRun.unity");
        Debug.Log($"CandyRun scene built: kit {lib.kit.Length}, mobs {lib.monsters.Length}");
    }

    [MenuItem("SubwayRun/Dino/Build Scene")]
    public static void BuildDinoScene()
    {
        BuildBlockScene();
        var d = Object.FindAnyObjectByType<RunDirector>();
        var lib = d.lib;
        lib.blockMode = false; lib.obbyMode = false; lib.halloweenMode = false; lib.candyMode = false;
        lib.dinoMode = true;
        lib.track = null;
        const string DN = "Assets/ThirdParty/Quaternius/Dinos/";
        var nature = AssetDatabase.FindAssets("t:Model", new[] { "Assets/ThirdParty/Kenney/nature" }).Select(AssetDatabase.GUIDToAssetPath).Select(Load);
        var dinos = new[] { "Trex", "Velociraptor", "Triceratops", "Stegosaurus", "Apatosaurus", "Parasaurolophus" }.Select(n => Load(DN + n + ".glb"));
        lib.kit = nature.Concat(dinos).Where(p => p).ToArray();
        Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Dino/" + n + ".png");
        lib.grassTex = T("grass"); lib.pathTex = T("path"); lib.basaltTex = T("basalt"); lib.lavaTex = T("lava"); lib.stoneTex = T("stone");
        lib.monsters = new[] { Load(DN + "Velociraptor.glb") }.Where(p => p).ToArray();
        lib.chaser = Load(DN + "Trex.glb");
        lib.buddy = Load(CW + "Characters/Character_Male_1.fbx");
        lib.buddyYaw = 180f;
        d.videoTitle = "DINO ESCAPE";
        d.welcomeClip = "welcome_dino";
        d.level1Clip = "level1_d"; d.level2Clip = "level2_d"; d.level3Clip = "level3_d";
        var clips = new[] { "run6_L1", "run6_L2", "run6_L3" }.Select(n => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Tracks/" + n + ".wav")).ToArray();
        d.levels[0].theme = RunTheme.JungleValley; d.levels[0].clip = clips[0];
        d.levels[1].theme = RunTheme.VolcanoRun;   d.levels[1].clip = clips[1];
        d.levels[2].theme = RunTheme.DinoValley;   d.levels[2].clip = clips[2];
        var dp = "Assets/Settings/DinoVolume.asset";
        var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(dp);
        if (!prof)
        {
            prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = prof.Add<Bloom>(true); bloom.intensity.Override(0.9f); bloom.threshold.Override(0.95f);
            var tone = prof.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.Neutral);
            var ca = prof.Add<ColorAdjustments>(true); ca.saturation.Override(28f); ca.contrast.Override(14f); ca.postExposure.Override(0.15f);
            var vig = prof.Add<Vignette>(true); vig.intensity.Override(0.18f);
            AssetDatabase.CreateAsset(prof, dp);
        }
        Object.FindAnyObjectByType<Volume>().sharedProfile = prof;
        EditorSceneManager.SaveScene(d.gameObject.scene, "Assets/Scenes/DinoRun.unity");
        Debug.Log("DinoRun scene built: kit " + lib.kit.Length);
    }

    [MenuItem("SubwayRun/Block World 2/1. Pixel Texture Import")]
    public static void PixelImport()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures/Pixel" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is TextureImporter ti && (ti.filterMode != FilterMode.Point || ti.textureCompression != TextureImporterCompression.Uncompressed))
            {
                ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.wrapMode = TextureWrapMode.Repeat; ti.alphaIsTransparency = true; ti.SaveAndReimport();
            }
        }
        Debug.Log("Pixel textures set to point filtering");
    }

    [MenuItem("SubwayRun/Block World 2/2. Build Scene")]
    public static void BuildMcScene()
    {
        PixelImport();
        BuildBlockScene();
        var d = Object.FindAnyObjectByType<RunDirector>();
        var lib = d.lib;
        lib.blockMode = false; lib.obbyMode = false; lib.halloweenMode = false; lib.candyMode = false; lib.dinoMode = false;
        lib.mcMode = true;
        lib.buddy = null;
        lib.track = Load(CW + "Environment/Rail_Straight.fbx");   // rails in the caves
        lib.pixTex = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures/Pixel" }).Select(AssetDatabase.GUIDToAssetPath)
            .Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p)).Where(t => t).ToArray();
        AudioClip S(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sfx/" + n + ".wav");
        lib.sfxBreak = S("break"); lib.sfxHit = S("hit"); lib.sfxOrb = S("orb"); lib.sfxLevel = S("levelup"); lib.sfxToast = S("toast");
        lib.spoilers = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures/Spoilers" }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => System.IO.Path.GetFileName(p).StartsWith("run7_")).OrderBy(p => p)
            .Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p)).Where(t => t).ToArray();
        d.videoTitle = "MINECRAFT WORLD RUN";
        d.welcomeClip = "welcome_mc";
        d.level1Clip = "level1_mc"; d.level2Clip = "level2_mc"; d.level3Clip = "level3_mc";
        var clips = new[] { "run7_L1", "run7_L2", "run7_L3" }.Select(n => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Tracks/" + n + ".wav")).ToArray();
        d.levels[0].theme = RunTheme.McPlains; d.levels[0].clip = clips[0];
        d.levels[1].theme = RunTheme.McCaves;  d.levels[1].clip = clips[1];
        d.levels[2].theme = RunTheme.McNether; d.levels[2].clip = clips[2];
        EditorSceneManager.SaveScene(d.gameObject.scene, "Assets/Scenes/BlockWorldRun2.unity");
        Debug.Log("BlockWorldRun2 scene built: pix " + lib.pixTex.Length + ", spoilers " + lib.spoilers.Length);
    }

    [MenuItem("Move Quest Run/1. Minecraft Quest - Build Scene")]
    public static void BuildMqrMinecraft()
    {
        UnityEditor.AssetDatabase.Refresh();
        BuildMcScene();
        var d = Object.FindAnyObjectByType<RunDirector>();
        d.questStyle = true;
        d.filePrefix = "MQR";
        d.videoTitle = "MINECRAFT QUEST";
        d.welcomeClip = "welcome_mqr";
        d.level1Clip = "level1_mqr"; d.level2Clip = "level2_mqr"; d.level3Clip = "level3_mqr";
        var clips = new[] { "mqr1_L1", "mqr1_L2", "mqr1_L3" }.Select(n => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Tracks/" + n + ".wav")).ToArray();
        d.levels[0].theme = RunTheme.McTaiga; d.levels[0].clip = clips[0];
        d.levels[0].moves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Mirror };
        d.levels[0].newMoves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Mirror };
        d.levels[1].theme = RunTheme.McJungle; d.levels[1].clip = clips[1];
        d.levels[1].moves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Left, RunMove.Right, RunMove.Dash, RunMove.Mirror };
        d.levels[1].newMoves = new[] { RunMove.Left, RunMove.Right, RunMove.Dash };
        d.levels[2].theme = RunTheme.McEnd; d.levels[2].clip = clips[2];
        d.levels[2].moves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Left, RunMove.Right, RunMove.Dash, RunMove.Smash, RunMove.Mirror };
        d.levels[2].newMoves = new[] { RunMove.Smash };
        d.lib.spoilers = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures/Spoilers" }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => System.IO.Path.GetFileName(p).StartsWith("mqr1_")).OrderBy(p => p)
            .Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p)).Where(t => t).ToArray();
        EditorSceneManager.SaveScene(d.gameObject.scene, "Assets/Scenes/MQR_MinecraftQuest.unity");
        Debug.Log("MQR_MinecraftQuest scene built: spoilers " + d.lib.spoilers.Length);
    }

    [MenuItem("SubwayRun/Demon Hunters/Build Scene")]
    public static void BuildKpopScene()
    {
        const string FL = Q + "Flying/";
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { FL }))
            if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) is ModelImporter mi && mi.animationType != ModelImporterAnimationType.Legacy)
            { mi.animationType = ModelImporterAnimationType.Legacy; mi.importAnimation = true; mi.SaveAndReimport(); }
        BuildBlockScene();
        var d = Object.FindAnyObjectByType<RunDirector>();
        var lib = d.lib;
        lib.blockMode = false; lib.obbyMode = false; lib.halloweenMode = false; lib.candyMode = false; lib.dinoMode = false; lib.mcMode = false;
        lib.kpopMode = true;
        lib.track = null;
        Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Kpop/" + n + ".png");
        lib.kpopSigns = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures/Kpop" }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => System.IO.Path.GetFileName(p).StartsWith("sign")).Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p)).Where(t => t).ToArray();
        lib.ledTex = T("led"); lib.realmTex = T("realm"); lib.asphaltTex = T("asphalt"); lib.stageTex = T("stage");
        lib.monsters = new[] { FL + "Demon.fbx", FL + "Ghost.fbx", FL + "Ghost_Skull.fbx", FL + "Hywirl.fbx", Q + "Big/Demon.fbx", Q + "Big/BlueDemon.fbx" }.Select(Load).Where(p => p).ToArray();
        lib.chaser = Load(Q + "Big/Demon.fbx");
        lib.buddy = Load(CW + "Characters/Character_Female_2.fbx");
        lib.buddyYaw = 180f;
        lib.spoilers = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures/Spoilers" }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => System.IO.Path.GetFileName(p).StartsWith("run8_")).OrderBy(p => p)
            .Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p)).Where(t => t).ToArray();
        d.videoTitle = "DEMON HUNTERS RUN";
        d.filePrefix = "SubwayRun";
        d.welcomeClip = "welcome_kpop";
        d.level1Clip = "level1_k"; d.level2Clip = "level2_k"; d.level3Clip = "level3_k";
        var clips = new[] { "run8_L1", "run8_L2", "run8_L3" }.Select(n => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Tracks/" + n + ".wav")).ToArray();
        d.levels[0].theme = RunTheme.NeonCity;     d.levels[0].clip = clips[0];
        d.levels[1].theme = RunTheme.ConcertStage; d.levels[1].clip = clips[1];
        d.levels[2].theme = RunTheme.DemonRealm;   d.levels[2].clip = clips[2];
        var kp = "Assets/Settings/KpopVolume.asset";
        var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(kp);
        if (!prof)
        {
            prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = prof.Add<Bloom>(true); bloom.intensity.Override(1.8f); bloom.threshold.Override(0.85f); bloom.scatter.Override(0.7f);
            var tone = prof.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var ca = prof.Add<ColorAdjustments>(true); ca.saturation.Override(25f); ca.contrast.Override(18f); ca.postExposure.Override(0.3f);
            var vig = prof.Add<Vignette>(true); vig.intensity.Override(0.3f);
            var chrom = prof.Add<ChromaticAberration>(true); chrom.intensity.Override(0.12f);
            AssetDatabase.CreateAsset(prof, kp);
        }
        Object.FindAnyObjectByType<Volume>().sharedProfile = prof;
        EditorSceneManager.SaveScene(d.gameObject.scene, "Assets/Scenes/DemonHuntersRun.unity");
        Debug.Log("DemonHuntersRun scene built: signs " + lib.kpopSigns.Length + ", demons " + lib.monsters.Length + ", spoilers " + lib.spoilers.Length);
    }

    [MenuItem("Move Quest Run/2. Subway Surfers - Build Scene")]
    public static void BuildMqrSubway()
    {
        UnityEditor.AssetDatabase.Refresh();
        BuildScene();   // city / tunnel / neon subway worlds, trains, graffiti, coins
        var d = Object.FindAnyObjectByType<RunDirector>();
        var lib = d.lib;
        d.questStyle = true;
        d.filePrefix = "MQR";
        d.videoTitle = "SUBWAY SURFERS RUN";
        d.welcomeClip = "welcome_mqr2";
        d.level1Clip = "level1_mqr2"; d.level2Clip = "level2_mqr2"; d.level3Clip = "level3_mqr2";
        d.endScreenSeconds = 20f;
        const string CW = "Assets/ThirdParty/Quaternius/CubeWorld/";
        lib.buddy = Load(CW + "Characters/Character_Female_1.fbx"); lib.buddyYaw = 180f;
        lib.atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(CW + "Atlas.png");
        lib.chaser = Load(CW + "Characters/Character_Male_2.fbx");   // the grumpy guard...
        lib.chaserHeight = 1.9f; lib.chaserYaw = 180f;
        lib.chaserPet = Load(CW + "Animals/Dog.fbx");                 // ...and his dog
        lib.spoilers = new Texture2D[0];
        var clips = new[] { "mqr2_L1", "mqr2_L2", "mqr2_L3" }.Select(n => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Tracks/" + n + ".wav")).ToArray();
        d.endScreenMusic = clips[0];
        d.levels[0].clip = clips[0];
        d.levels[0].moves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Mirror };
        d.levels[0].newMoves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Mirror };
        d.levels[1].clip = clips[1]; d.levels[1].bpm = 99.4f; d.levels[1].bars = 25; d.levels[1].cueBeats = 3; d.levels[1].restBeats = 8;
        d.levels[1].moves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Left, RunMove.Right, RunMove.Dash, RunMove.Mirror };
        d.levels[1].newMoves = new[] { RunMove.Left, RunMove.Right, RunMove.Dash };
        d.levels[2].clip = clips[2];
        d.levels[2].moves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Left, RunMove.Right, RunMove.Dash, RunMove.Smash, RunMove.Mirror };
        d.levels[2].newMoves = new[] { RunMove.Smash };
        EditorSceneManager.SaveScene(d.gameObject.scene, "Assets/Scenes/MQR_SubwaySurfers.unity");
        Debug.Log("MQR_SubwaySurfers scene built");
    }

    [MenuItem("SubwayRun/Demon Hunters/Build Scene 2")]
    public static void BuildKpopScene2()
    {
        BuildKpopScene();
        var d = Object.FindAnyObjectByType<RunDirector>();
        d.videoTitle = "DEMON HUNTERS RUN 2";
        d.seed = 11;
        var clips = new[] { "run9_L1", "run9_L2", "run9_L3" }.Select(n => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Tracks/" + n + ".wav")).ToArray();
        for (int i = 0; i < 3; i++) d.levels[i].clip = clips[i];
        d.lib.spoilers = new Texture2D[0];
        EditorSceneManager.SaveScene(d.gameObject.scene, "Assets/Scenes/DemonHuntersRun2.unity");
        Debug.Log("DemonHuntersRun2 built");
    }

    [MenuItem("Move Quest Run/3. Halloween Quest - Build Scene")]
    public static void BuildMqrHalloween()
    {
        AssetDatabase.Refresh();
        MonstersLegacy();
        foreach (var path in new[] { CW + "Enemies/Skeleton.fbx" })
            if (AssetImporter.GetAtPath(path) is ModelImporter mi && mi.animationType != ModelImporterAnimationType.Legacy)
            { mi.animationType = ModelImporterAnimationType.Legacy; mi.importAnimation = true; mi.SaveAndReimport(); }
        PlayerSettings.runInBackground = true;
        BuildBlockScene();
        var d = Object.FindAnyObjectByType<RunDirector>();
        var lib = d.lib;
        lib.blockMode = false; lib.obbyMode = false; lib.halloweenMode = false; lib.candyMode = false; lib.dinoMode = false; lib.mcMode = false; lib.kpopMode = false;
        lib.spookyMode = true;
        lib.track = null;

        // scenery: Kenney graveyard + nature kits, a few Food Kit prizes, animated Quaternius monsters
        var models = AssetDatabase.FindAssets("t:Model", new[] { K + "graveyard", K + "nature" }).Select(AssetDatabase.GUIDToAssetPath).Select(Load);
        var food = new[] { "lollypop", "candy-bar", "cupcake", "donut-sprinkles", "ice-cream", "cake-birthday", "popsicle", "muffin", "cookie-chocolate" }.Select(n => Load(K + "food/" + n + ".fbx"));
        var critters = new[] { "Blob/GreenBlob", "Blob/PinkBlob", "Blob/Mushnub", "Blob/Mushnub_Evolved", "Blob/Wizard", "Blob/Cat", "Blob/GreenSpikyBlob", "Blob/Alien",
                               "Big/Frog", "Flying/Ghost", "Flying/Ghost_Skull", "Flying/Dragon", "Flying/Glub" }.Select(n => Load(Q + n + ".fbx"));
        lib.kit = models.Concat(food).Concat(critters).Concat(new[] { Load(CW + "Enemies/Skeleton.fbx") }).Where(p => p).ToArray();
        var fxTex = new[] { "star_06", "magic_04" }.Select(n => AssetDatabase.LoadAssetAtPath<Texture2D>(K + "particles/" + n + ".png"));
        lib.spookyTex = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures/Spooky" }).Select(AssetDatabase.GUIDToAssetPath)
            .Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p)).Concat(fxTex).Where(t => t).ToArray();
        lib.skyShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/SpookySky.shader");
        lib.bat = AssetDatabase.LoadAssetAtPath<Texture2D>(HT + "bat.png");
        lib.moon = AssetDatabase.LoadAssetAtPath<Texture2D>(HT + "moon.png");
        lib.monsters = new[] { Load(CW + "Enemies/Skeleton.fbx"), Load(Q + "Big/Orc_Skull.fbx"), Load(Q + "Flying/Ghost_Skull.fbx") }.Where(p => p).ToArray();
        lib.chaser = null; lib.chaserPet = null;
        lib.chasers = new[] { Load(Q + "Flying/Ghost.fbx"), null, Load(Q + "Flying/Dragon.fbx") };
        lib.chaserHeight = 4.2f; lib.chaserYaw = 0f; lib.chaserLift = 1.2f;
        lib.buddy = Load(CW + "Characters/Character_Female_1.fbx");
        lib.buddyYaw = 180f;
        lib.spoilers = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures/Spoilers" }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => System.IO.Path.GetFileName(p).StartsWith("mqr3_")).OrderBy(p => p)
            .Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p)).Where(t => t).ToArray();

        d.questStyle = true;
        d.filePrefix = "MQR";
        d.videoTitle = "HALLOWEEN QUEST";
        d.endScreenSeconds = 20f;
        // announcer: own lines if Tools/voice/mqr3_voice.py has been run, otherwise generic ones
        string V(string want, string fallback) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Voice/" + want + ".wav") ? want : fallback;
        d.welcomeClip = V("welcome_mqr3", "welcome_halloween");
        d.level1Clip = V("level1_mqr3", "level1"); d.level2Clip = V("level2_mqr3", "level2"); d.level3Clip = V("level3_mqr3", "level3");
        // tracks: spliced 40-bar levels if present, else the trimmed Suno tracks
        AudioClip Track(int i) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Tracks/mqr3_L{i}.wav") ?? AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Tracks/mqr3_{i}.wav");
        var clips = new[] { Track(1), Track(2), Track(3) };
        d.endScreenMusic = clips[0];
        d.levels[0].theme = RunTheme.SpookyCarnival; d.levels[0].clip = clips[0];
        d.levels[0].moves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Mirror };
        d.levels[0].newMoves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Mirror };
        d.levels[1].theme = RunTheme.WitchForest; d.levels[1].clip = clips[1];
        d.levels[1].moves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Left, RunMove.Right, RunMove.Dash, RunMove.Mirror };
        d.levels[1].newMoves = new[] { RunMove.Left, RunMove.Right, RunMove.Dash };
        d.levels[2].theme = RunTheme.MoonCastle; d.levels[2].clip = clips[2];
        d.levels[2].moves = new[] { RunMove.Jump, RunMove.Duck, RunMove.Left, RunMove.Right, RunMove.Dash, RunMove.Smash, RunMove.Mirror };
        d.levels[2].newMoves = new[] { RunMove.Smash };
        // real tempo measured by Tools/mqr3_setup.py (Suno sometimes delivers ~129 instead of 152)
        var bpmFile = "Assets/Audio/Tracks/mqr3_bpm.txt";
        if (System.IO.File.Exists(bpmFile))
        {
            var lines = System.IO.File.ReadAllLines(bpmFile);
            for (int i = 0; i < 3 && i < lines.Length; i++)
                if (float.TryParse(lines[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var bpm)
                    && bpm > 110f && bpm < 175f && Mathf.Abs(bpm - 152f) > 4f)
                    d.levels[i].bpm = bpm;
        }

        // juicy night look: strong bloom on the bulbs, neutral tonemapping keeps the candy colours
        var sp = "Assets/Settings/SpookyVolume.asset";
        var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(sp);
        if (!prof)
        {
            prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = prof.Add<Bloom>(true); bloom.intensity.Override(1.5f); bloom.threshold.Override(0.9f); bloom.scatter.Override(0.72f);
            var tone = prof.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.Neutral);
            var ca = prof.Add<ColorAdjustments>(true); ca.saturation.Override(30f); ca.contrast.Override(16f); ca.postExposure.Override(0.3f);
            var vig = prof.Add<Vignette>(true); vig.intensity.Override(0.26f); vig.smoothness.Override(0.45f);
            var chrom = prof.Add<ChromaticAberration>(true); chrom.intensity.Override(0.08f);
            AssetDatabase.CreateAsset(prof, sp);
        }
        Object.FindAnyObjectByType<Volume>().sharedProfile = prof;
        EditorSceneManager.SaveScene(d.gameObject.scene, "Assets/Scenes/MQR_HalloweenQuest.unity");
        Debug.Log($"MQR_HalloweenQuest built: kit {lib.kit.Length}, tex {lib.spookyTex.Length}, sky {(lib.skyShader ? "ok" : "MISSING")}, " +
                  $"tracks {clips.Count(c => c)}/3 ({string.Join(", ", clips.Select(c => c ? c.name : "-"))}), voice {d.welcomeClip}/{d.level1Clip}, bpm {string.Join("/", d.levels.Select(l => l.bpm.ToString("0")))}");
    }

    static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    static GameObject[] LoadMany(string folder, string prefix) =>
        AssetDatabase.FindAssets("t:Model", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => System.IO.Path.GetFileName(p).StartsWith(prefix))
            .Select(Load).Where(p => p).ToArray();
}
