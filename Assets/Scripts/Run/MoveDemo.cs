using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Animated neon silhouettes for the level cards (the BEAT TUNNEL trainer figure, Jennifer):
// one figure per move, looping its clip far below the world, each filmed by its own camera
// into a RenderTexture that the card shows through the SilhouetteGlowV5 contour shader.
public class MoveDemo : MonoBehaviour
{
    const int Layer = 31;
    static MoveDemo inst;
    readonly Dictionary<RunMove, RenderTexture> rts = new Dictionary<RunMove, RenderTexture>();
    readonly List<(PlayableGraph g, AnimationClipPlayable p, float len)> graphs = new List<(PlayableGraph, AnimationClipPlayable, float)>();
    readonly List<Camera> cams = new List<Camera>();
    Material bodyMat;
    public static Shader GlowShader;
    bool active;

    public static void Init(RunLibrary lib)
    {
        if (inst || !lib || !lib.demoModel || lib.demoClips == null) return;
        var go = new GameObject("MoveDemo");
        inst = go.AddComponent<MoveDemo>();
        GlowShader = lib.silGlow;
        inst.Build(lib);
    }

    public static Texture Get(RunMove m) => inst && inst.rts.TryGetValue(m, out var rt) ? rt : null;
    public static void SetActive(bool on) { if (inst) inst.active = on; if (inst) foreach (var c in inst.cams) c.enabled = on; }

    void Build(RunLibrary lib)
    {
        bodyMat = new Material(lib.silBody ? lib.silBody : Shader.Find("Universal Render Pipeline/Unlit"));
        if (bodyMat.HasProperty("_FillColor")) bodyMat.SetColor("_FillColor", new Color(0.92f, 0.93f, 0.97f));
        if (bodyMat.HasProperty("_RimColor")) bodyMat.SetColor("_RimColor", new Color(1f, 0.75f, 0.2f));
        RunMove[] moves = { RunMove.Jump, RunMove.Duck, RunMove.Left, RunMove.Right, RunMove.Dash, RunMove.Smash, RunMove.Mirror, RunMove.Freeze };
        for (int i = 0; i < moves.Length && i < lib.demoClips.Length; i++)
        {
            var clip = lib.demoClips[i];
            if (!clip) continue;
            var origin = new Vector3(i * 12f, -600f, 0f);
            var fig = Instantiate(lib.demoModel, origin, Quaternion.identity, transform);
            foreach (var t in fig.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            foreach (var r in fig.GetComponentsInChildren<Renderer>())
            {
                var ms = r.sharedMaterials;
                for (int k = 0; k < ms.Length; k++) ms[k] = bodyMat;
                r.sharedMaterials = ms;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
            }
            var animator = fig.GetComponent<Animator>() ?? fig.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var g = PlayableGraph.Create("MoveDemo" + i);
            g.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var p = AnimationClipPlayable.Create(g, clip);
            p.SetApplyFootIK(true);
            var output = AnimationPlayableOutput.Create(g, "out", animator);
            output.SetSourcePlayable(p);
            graphs.Add((g, p, clip.length));

            var rt = new RenderTexture(320, 400, 16, RenderTextureFormat.ARGB32) { useMipMap = true, autoGenerateMips = true, antiAliasing = 1 };
            rt.Create();
            rts[moves[i]] = rt;
            var cg = new GameObject("DemoCam" + i);
            cg.transform.SetParent(transform);
            var cam = cg.AddComponent<Camera>();
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 30f;
            cam.targetTexture = rt;
            // in front of the figure, looking back at it (the instructor faces the viewer)
            cam.transform.position = origin + new Vector3(0, 1.2f, 4.9f);
            cam.transform.rotation = Quaternion.Euler(4f, 180f, 0f);
            cam.enabled = false;
            cams.Add(cam);
        }
    }

    void Update()
    {
        if (!active) return;
        float t = Time.time;
        foreach (var (g, p, len) in graphs)
        {
            p.SetTime(len > 0 ? t % len : 0);
            g.Evaluate();
        }
    }

    void OnDestroy()
    {
        foreach (var (g, _, _) in graphs) if (g.IsValid()) g.Destroy();
        foreach (var rt in rts.Values) if (rt) rt.Release();
        if (inst == this) inst = null;
    }
}
