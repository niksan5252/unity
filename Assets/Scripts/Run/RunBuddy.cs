using UnityEngine;

// A character running ahead of the camera: same path as the player, `lead` seconds earlier,
// so it jumps / ducks / dodges just before us. Steps aside and punches on SMASH.
public class RunBuddy : MonoBehaviour
{
    public RunDirector d;
    public float lead = 0.45f;
    public float sideOnSmash = 2.6f;
    Animation anim;
    string current;
    float xSmooth;
    int level = -2;

    public static RunBuddy Create(RunDirector d, GameObject prefab)
    {
        var go = WorldBuilder.Spawn(prefab, Vector3.zero, 180, null);
        go.name = "Buddy";
        var b = go.AddComponent<RunBuddy>();
        b.d = d;
        b.anim = go.GetComponentInChildren<Animation>();
        b.Play("Run");
        if (b.anim) b.anim.Sample();
        RunObstacle.FitHeightPosed(go, 1.7f);
        return b;
    }

    void Play(string key, float fade = 0.12f)
    {
        if (!anim || current == key) return;
        foreach (AnimationState s in anim)
        {
            var n = s.name.Substring(s.name.LastIndexOf('|') + 1);
            if (n == key)
            {
                s.wrapMode = key == "Run" || key == "Duck" ? WrapMode.Loop : WrapMode.Once;
                anim.CrossFade(s.name, fade);
                current = key;
                return;
            }
        }
    }

    void LateUpdate()
    {
        if (d.CurrentLevel < 0) return;
        if (d.CurrentLevel != level) { level = d.CurrentLevel; xSmooth = (level % 2 == 0 ? 1f : -1f) * WorldBuilder.LaneGap; }
        float tb = d.SongTime + lead;
        var cues = d.CurrentCues;
        var off = d.PathOffset(tb, cues);

        // home = a side lane (alternates per level); leave it only when a cart train blocks it
        float home = (level % 2 == 0 ? 1f : -1f) * WorldBuilder.LaneGap;
        float targetX = home;
        bool smash = false, duck = false;
        foreach (var c in cues)
        {
            float u = tb - c.time;
            if ((c.move == RunMove.Left || c.move == RunMove.Right) && u > -0.7f && u < 1.95f)
            {
                float free = c.move == RunMove.Left ? -1f : 1f;
                if (Mathf.Sign(home) != free) targetX = free * WorldBuilder.LaneGap;
            }
            if (c.move == RunMove.Smash && u > -0.9f && u < 0.5f) smash = true;
            if (c.move == RunMove.Duck && u > -0.45f && u < 0.35f) duck = true;
        }
        xSmooth = Mathf.MoveTowards(xSmooth, targetX, Time.deltaTime * 11f);

        float y = Mathf.Max(0f, off.y);
        transform.position = new Vector3(xSmooth, y, tb * d.speed);

        if (smash) Play("Punch");
        else if (off.y > 0.25f) Play("Jump");
        else if (duck) Play("Duck", 0.06f);
        else Play("Run");
        if (current != "Run" && current != "Duck" && anim && !anim.isPlaying) { current = null; Play("Run"); }
    }
}
