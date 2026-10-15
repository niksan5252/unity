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
        RunObstacle.FitHeightPosed(go, 1.85f);
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
        if (d.CurrentLevel != level) { level = d.CurrentLevel; xSmooth = 0; }
        float tb = d.SongTime + lead;
        var cues = d.CurrentCues;
        var off = d.PathOffset(tb, cues);

        float targetX = off.x;
        bool smash = false;
        foreach (var c in cues)
            if (c.move == RunMove.Smash && tb > c.time - 0.9f && tb < c.time + 0.5f) { smash = true; targetX = sideOnSmash; }
        xSmooth = Mathf.MoveTowards(xSmooth, targetX, Time.deltaTime * 9f);

        float y = Mathf.Max(0f, off.y);
        transform.position = new Vector3(xSmooth, y, tb * d.speed);

        if (smash) Play("Punch");
        else if (off.y > 0.25f) Play("Jump");
        else if (off.y < -0.3f) Play("Duck");
        else Play("Run");
        if (current != "Run" && current != "Duck" && anim && !anim.isPlaying) { current = null; Play("Run"); }
    }
}
