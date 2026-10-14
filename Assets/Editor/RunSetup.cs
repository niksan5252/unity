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

    static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    static GameObject[] LoadMany(string folder, string prefix) =>
        AssetDatabase.FindAssets("t:Model", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => System.IO.Path.GetFileName(p).StartsWith(prefix))
            .Select(Load).Where(p => p).ToArray();
}
