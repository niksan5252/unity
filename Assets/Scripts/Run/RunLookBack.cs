using UnityEngine;

// "Don't look back!": in a quiet gap the camera spins around and something is chasing us.
public class RunLookBack : MonoBehaviour
{
    RunDirector d;
    RunLibrary lib;
    int level = -2;
    float at = -1;        // song time of the turn
    Transform chaser;
    Animation anim;
    bool voiced, voiced2;
    GameObject chaserPrefab;
    static readonly string[] Chasers = { "character-vampire", "character-zombie", "character-keeper" };

    public static RunLookBack Create(RunDirector d, RunLibrary lib)
    {
        var lb = d.gameObject.AddComponent<RunLookBack>();
        lb.d = d; lb.lib = lib;
        return lb;
    }

    void PickTime()
    {
        at = -1;
        // levels 1 and 3 only: two scares per video
        if (level != 0 && level != 2) return;
        var cues = d.CurrentCues;
        for (float t = 18f; t < 55f; t += 0.25f)
        {
            bool clear = true;
            foreach (var c in cues) if (c.time > t - 1.4f && c.time < t + 3.2f) { clear = false; break; }
            if (clear) { at = t; break; }
        }
        voiced = voiced2 = false;
        if (chaser) Destroy(chaser.gameObject);
        if (lib.mcMode && at > 0)
        {
            // block chasers: a zombie mob on level 1, a giant spider on level 3
            var holder = new GameObject("McChaser").transform;
            var kinds = level == 0 ? new[] { BlockFigure.Kind.Zombie, BlockFigure.Kind.Zombie, BlockFigure.Kind.Skeleton } : new[] { BlockFigure.Kind.Spider };
            for (int i = 0; i < kinds.Length; i++)
            {
                var f = BlockFigure.Create(kinds[i], holder);
                f.SetState("Run"); f.speedMul = 1.3f;
                f.transform.localPosition = new Vector3((i - (kinds.Length - 1) * 0.5f) * 1.6f, 0, -i % 2 * 1.2f);
                if (kinds[i] == BlockFigure.Kind.Spider) f.transform.localScale = Vector3.one * 2.4f;
            }
            holder.position = new Vector3(0, -50, 0);
            chaser = holder;
            chaserPrefab = null;
            return;
        }
        var p = lib.chaser ? lib.chaser : lib.Kit(Chasers[(level / 2) % Chasers.Length]);
        chaserPrefab = p;
        if (p && at > 0)
        {
            var go = WorldBuilder.Spawn(p, new Vector3(0, -50, 0), 0, null);
            if (lib.chaser) DinoWorld.Brighten(go);
            anim = go.GetComponentInChildren<Animation>();
            if (anim)
                foreach (AnimationState s in anim)
                    if (s.name.EndsWith("sprint") || s.name.EndsWith("_Run")) { s.wrapMode = WrapMode.Loop; anim.Play(s.name); break; }
            if (anim) anim.Sample();
            if (lib.chaser) RunObstacle.FitHeightPosed(go, 4.6f); else BlockWorld.Fit(go, 2.3f);
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.updateWhenOffscreen = true;
            // pale glowing eyes-ish tint so it pops out of the dark
            if (!lib.chaser)
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var ms = r.materials;
                foreach (var m in ms) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(0.35f, 0.4f, 0.6f) * 0.5f); }
                r.materials = ms;
            }
            chaser = go.transform;
        }
    }

    void OnDestroy() { if (chaser) Destroy(chaser.gameObject); }

    void LateUpdate()
    {
        if (d.CurrentLevel < 0) return;
        if (d.CurrentLevel != level) { level = d.CurrentLevel; PickTime(); }
        if (at < 0) { d.Yaw = 0; return; }

        float u = d.SongTime - at;          // 0..0.35 turn, ..1.75 hold, ..2.1 turn back
        float yaw = 0;
        if (u > 0 && u < 0.35f) yaw = Mathf.SmoothStep(0, 180, u / 0.35f);
        else if (u >= 0.35f && u < 1.75f) yaw = 180;
        else if (u >= 1.75f && u < 2.1f) yaw = Mathf.SmoothStep(180, 0, (u - 1.75f) / 0.35f);
        d.Yaw = yaw;

        if (u > -0.05f && u < 0.05f && !voiced) { voiced = true; d.Say(lib.chaser ? "roar_sfx" : "whoosh_sfx"); if (lib.mcMode) d.PlaySfx(lib.sfxHit, 0.5f); }
        if (u > 1.75f && !voiced2) { voiced2 = true; d.Say("behind"); }

        if (chaser)
        {
            float camZ = d.SongTime * d.speed;
            // closes in while we look at it, then stays just behind us
            float gap = u < 0 ? 30f : Mathf.Lerp(9f, 4.5f, Mathf.Clamp01(u / 1.75f));
            if (lib.chaser) gap += 3f;   // the T-rex is big: keep it a little further back
            chaser.position = new Vector3(Mathf.Sin(d.SongTime * 2f) * 0.6f, 0, camZ - gap);
            chaser.rotation = chaserPrefab ? chaserPrefab.transform.rotation : Quaternion.identity;
            if (u > 3f) { Destroy(chaser.gameObject); chaser = null; }
        }
    }
}
