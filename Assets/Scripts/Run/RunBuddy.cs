using UnityEngine;

// A character running ahead of the camera: same path as the player, `lead` seconds earlier,
// so it jumps / ducks / dodges just before us. Steps aside and punches on SMASH.
public class RunBuddy : MonoBehaviour
{
    public RunDirector d;
    public float lead = 0.45f;
    public float sideOnSmash = 2.6f;
    public float yaw = 180f;   // model facing: Cube World rigs need 180, Kenney rigs 0
    Animation anim;
    string current;
    float xSmooth;
    int level = -2;

    // gag: 1-2 times per video the buddy misses an obstacle, gets knocked out of frame, then catches up
    enum State { Run, Fly, Back }
    State state;
    float stateT, curLead;
    Vector3 flyPos, flyVel, spin;
    float failTime = -1f;
    RunMove failMove;

    public static RunBuddy Create(RunDirector d, GameObject prefab, float yaw = 180f)
    {
        var go = WorldBuilder.Spawn(prefab, Vector3.zero, yaw, null);
        go.name = "Buddy";
        var b = go.AddComponent<RunBuddy>();
        b.d = d;
        b.yaw = yaw;
        b.baseRot = prefab.transform.rotation;
        b.curLead = b.lead;
        b.anim = go.GetComponentInChildren<Animation>();
        b.Play("Run");
        if (b.anim) b.anim.Sample();
        RunObstacle.FitHeightPosed(go, 1.7f);
        b.baseScale = go.transform.localScale;
        return b;
    }

    Quaternion baseRot;
    Vector3 baseScale;
    float squash = 1f;

    void PickFail()
    {
        // level 1: the 3rd JUMP (trips over the planks); level 3: the 2nd DUCK (head hits the beam)
        failTime = -1f;
        RunMove want = level == 0 ? RunMove.Jump : level == 2 ? RunMove.Duck : (RunMove)(-1);
        int nth = level == 0 ? 3 : 2, k = 0;
        foreach (var c in d.CurrentCues)
            if (c.move == want && ++k == nth) { failTime = c.time; failMove = want; break; }
    }

    void StartFly()
    {
        state = State.Fly; stateT = 0; failTime = -1f;
        flyPos = transform.position;
        float side = Mathf.Sign(xSmooth == 0 ? 1 : xSmooth);
        // tripping launches it sideways off the map; a beam to the head sends it back over the camera
        flyVel = failMove == RunMove.Jump ? new Vector3(side * 9f, 9f, d.speed + 3f) : new Vector3(side * 2.5f, 8f, d.speed - 14f);
        spin = new Vector3(Random.Range(500f, 700f), Random.Range(-300f, 300f), Random.Range(200f, 400f)) * (failMove == RunMove.Jump ? 1 : -1);
        current = null; Play("HitReact", 0.05f);
        RunFx.Burst(flyPos + Vector3.up * 1.2f, new Color(1f, 0.9f, 0.5f));
        d.Say("oops");
    }

    void Fly()
    {
        stateT += Time.deltaTime;
        flyVel += Vector3.down * 18f * Time.deltaTime;
        flyPos += flyVel * Time.deltaTime;
        transform.position = flyPos;
        transform.Rotate(spin * Time.deltaTime, Space.World);
        if (stateT > 1.4f)
        {
            state = State.Back; stateT = 0; curLead = -1.1f;
            transform.rotation = Quaternion.Euler(0, yaw, 0) * baseRot;
            current = null; Play("Run");
        }
    }

    // Cube World and Kenney rigs name their clips differently
    static readonly System.Collections.Generic.Dictionary<string, string[]> Alias = new System.Collections.Generic.Dictionary<string, string[]>
    {
        { "Run", new[] { "Run", "sprint", "walk" } },
        { "Jump", new[] { "Jump", "jump" } },
        { "Duck", new[] { "Duck", "crouch" } },
        { "Punch", new[] { "Punch", "attack-melee-right" } },
        { "HitReact", new[] { "HitReact", "fall", "die" } },
    };

    void Play(string key, float fade = 0.12f)
    {
        if (!anim || current == key) return;
        var names = Alias.TryGetValue(key, out var a) ? a : new[] { key };
        foreach (var want in names)
            foreach (AnimationState s in anim)
            {
                var n = s.name.Substring(s.name.LastIndexOf('|') + 1);
                if (n == want)
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
        if (d.CurrentLevel != level) { level = d.CurrentLevel; xSmooth = (level % 2 == 0 ? 1f : -1f) * WorldBuilder.LaneGap; PickFail(); state = State.Run; curLead = lead; transform.rotation = Quaternion.Euler(0, yaw, 0) * baseRot; }
        if (state == State.Fly) { Fly(); return; }
        if (state == State.Back)
        {
            // runs in from behind the camera and overtakes us back to its spot
            stateT += Time.deltaTime;
            curLead = Mathf.Lerp(-1.1f, lead, Mathf.SmoothStep(0, 1, stateT / 1.8f));
            if (stateT >= 1.8f) { state = State.Run; curLead = lead; }
        }
        float tb = d.SongTime + curLead;
        if (state == State.Run && failTime > 0 && tb >= failTime - 0.08f) { StartFly(); return; }
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
            if (c.move == RunMove.Duck && u > -0.45f && u < 0.35f && !(c.time == failTime && state == State.Run)) duck = true;
        }
        xSmooth = Mathf.MoveTowards(xSmooth, targetX, Time.deltaTime * 11f);

        float y = Mathf.Max(0f, off.y);
        if (failTime > 0 && Mathf.Abs(tb - failTime) < 0.5f && failMove == RunMove.Jump && state == State.Run) y = 0;   // "forgets" to jump
        transform.position = new Vector3(xSmooth, y, tb * d.speed);
        // squash down while ducking so it always clears the beam, whatever the rig's crouch clip looks like
        squash = Mathf.MoveTowards(squash, duck ? 0.72f : 1f, Time.deltaTime * 6f);
        transform.localScale = new Vector3(baseScale.x, baseScale.y * squash, baseScale.z);

        if (smash) Play("Punch");
        else if (y > 0.25f) Play("Jump");
        else Play("Run");   // legs keep running while ducking; the squash does the ducking
        if (current != "Run" && current != "Duck" && anim && !anim.isPlaying) { current = null; Play("Run"); }
    }
}
