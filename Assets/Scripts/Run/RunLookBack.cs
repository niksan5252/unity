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
    bool big;   // a dedicated chaser model (lib.chaser / lib.chasers), not a Kenney graveyard character
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
            var kinds = WorldBuilder.Theme == RunTheme.McEnd ? new[] { BlockFigure.Kind.Shade, BlockFigure.Kind.Shade, BlockFigure.Kind.Shade }
                      : level == 0 ? new[] { BlockFigure.Kind.Zombie, BlockFigure.Kind.Zombie, BlockFigure.Kind.Skeleton } : new[] { BlockFigure.Kind.Spider };
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
        var perLevel = lib.chasers != null && level < lib.chasers.Length ? lib.chasers[level] : null;
        var p = perLevel ? perLevel : lib.chaser ? lib.chaser : lib.Kit(Chasers[(level / 2) % Chasers.Length]);
        big = perLevel || lib.chaser;
        chaserPrefab = p;
        if (p && at > 0)
        {
            var go = WorldBuilder.Spawn(p, new Vector3(0, -50, 0), 0, null);
            if (big && !lib.kpopMode && !lib.spookyMode) DinoWorld.Brighten(go);
            if (lib.spookyMode) SpookyWorld.Spookify(go, level == 0 ? new Color(0.7f, 0.95f, 1f) : SpookyWorld.Purple, level == 0 ? 0.8f : 0.3f);
            if (lib.kpopMode) KpopWorld.Demonize(go, 1.4f);
            anim = go.GetComponentInChildren<Animation>();
            if (anim)
                foreach (AnimationState s in anim)
                    if (s.name.EndsWith("sprint") || s.name.EndsWith("_Run") || s.name.EndsWith("|Run") || s.name.EndsWith("|Fast_Flying") || s.name.EndsWith("|Flying_Idle")) { s.wrapMode = WrapMode.Loop; anim.Play(s.name); break; }
            if (anim) anim.Sample();
            if (big) RunObstacle.FitHeightPosed(go, lib.chaserHeight); else BlockWorld.Fit(go, 2.3f);
            if (lib.chaserPet)
            {
                var pet = WorldBuilder.Spawn(lib.chaserPet, Vector3.zero, 0, go.transform);
                var pa = pet.GetComponentInChildren<Animation>();
                if (pa) foreach (AnimationState s in pa) if (s.name.EndsWith("|Run") || s.name.EndsWith("Gallop")) { s.wrapMode = WrapMode.Loop; pa.Play(s.name); break; }
                if (pa) pa.Sample();
                pet.transform.SetParent(null, true);
                RunObstacle.FitHeightPosed(pet, 0.9f);
                pet.transform.SetParent(go.transform, true);
                pet.transform.position = go.transform.position + new Vector3(1.3f, 0, 0.6f);
            }
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.updateWhenOffscreen = true;
            // pale glowing eyes-ish tint so it pops out of the dark
            if (!big && !lib.kpopMode)
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

        if (u > -0.05f && u < 0.05f && !voiced) { voiced = true; d.Say(big && !(lib.spookyMode && level == 0) ? "roar_sfx" : "whoosh_sfx"); if (lib.mcMode) d.PlaySfx(lib.sfxHit, 0.5f); }
        if (u > 1.75f && !voiced2) { voiced2 = true; d.Say("behind"); }

        if (chaser)
        {
            float camZ = d.Dist(d.SongTime);
            // closes in while we look at it, then stays just behind us
            float gap = u < 0 ? 30f : Mathf.Lerp(9f, 4.5f, Mathf.Clamp01(u / 1.75f));
            if (big && lib.chaserHeight > 3f) gap += 3f;   // the T-rex is big: keep it a little further back
            float lift = lib.chaserLift > 0 ? lib.chaserLift + Mathf.Sin(d.SongTime * 3f) * 0.3f : 0f;   // flying chasers hover
            chaser.position = new Vector3(Mathf.Sin(d.SongTime * 2f) * 0.6f, lift, camZ - gap);
            chaser.rotation = chaserPrefab ? Quaternion.Euler(0, lib.chaserYaw, 0) * chaserPrefab.transform.rotation : Quaternion.identity;
            if (u > 3f) { Destroy(chaser.gameObject); chaser = null; }
        }
    }
}
