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
                FitHeight(m, 3.2f);
                ob.actor = m.transform;
                ob.anim = m.GetComponentInChildren<Animation>();
                ob.PlayClip("run", "walk");
                break;
            }
        }
        return ob;
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
        if (!fired && t >= hitTime - 0.05f) Fire();
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
                    PlayClip("hit", "death");
                    actor.DOMove(actor.position + new Vector3(Random.Range(-3f, 3f), 4f, 30f), 0.8f).SetEase(Ease.OutQuad);
                    actor.DORotate(new Vector3(-80, 0, Random.Range(-60f, 60f)), 0.8f, RotateMode.LocalAxisAdd);
                    RunFx.Burst(actor.position + Vector3.up * 1.4f, new Color(1f, 0.95f, 0.4f));
                }
                director.Hud.Flash();
                break;
        }
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
