using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Runs the whole video: intro, levels (world + music + cues), cards between levels, outro.
public class RunDirector : MonoBehaviour
{
    public RunLevel[] levels;
    public RunLibrary lib;
    public float speed = 16f;
    public float iconLead = 1.3f;
    public int cueBeats = 5;        // beats between cues inside a burst (~2 s at 152 BPM)
    public int burstLength = 7;     // cues per burst
    public int restBeats = 12;      // pause between bursts
    public float camHeight = 1.7f;
    public int seed = 3;
    public int soloLevel = -1;
    public string videoTitle = "SUBWAY RUN";
    public string welcomeClip = "welcome";
    public string level2Clip = "level2";
    public string level1Clip = "level1";
    public string level3Clip = "level3";      // >=0 plays only that level (for testing)

    public RunHud Hud { get; private set; }
    public RunBuddy Buddy { get; private set; }
    public float Yaw;   // camera look-back angle (RunLookBack)
    public float SongTime { get; private set; }
    public int CurrentLevel { get; private set; } = -1;
    public bool Finished { get; private set; }
    public List<RunCue> CurrentCues { get; private set; } = new List<RunCue>();

    Camera cam;
    AudioSource src, voice;
    Transform world;
    Vector3 camOffset;

    void Start()
    {
        cam = Camera.main;
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        RunFx.spark = lib ? lib.spark : null;
        WorldBuilder.AtlasFallback = null;
        if (lib && lib.atlas)
        {
            WorldBuilder.AtlasFallback = WorldBuilder.Mat(Color.white);
            WorldBuilder.AtlasFallback.SetTexture("_BaseMap", lib.atlas);
            WorldBuilder.AtlasFallback.SetFloat("_Smoothness", 0.1f);
        }
        MoveDemo.Init(lib);
        Hud = RunHud.Create();
        if (lib && lib.buddy) Buddy = RunBuddy.Create(this, lib.buddy, lib.buddyYaw);
        if (lib && lib.halloweenMode)
        {
            // first-person shovel arm disabled (too close to a competitor look)
            RunLookBack.Create(this, lib);
        }
        StartCoroutine(Play());
    }

    IEnumerator Play()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            if (soloLevel >= 0 && i != soloLevel) continue;
            yield return PlayLevel(i, soloLevel < 0 && i == 0);
        }
        Hud.SetProgress(0, false);
        Say("thanks");
        yield return Hud.Card("THANKS FOR WATCHING", "SEE YOU SOON!", null, null, 4f);
        Finished = true;
    }

    IEnumerator PlayLevel(int index, bool withIntro)
    {
        var lv = levels[index];
        CurrentLevel = index;
        float beat = 60f / lv.bpm;
        float dur = Mathf.Min(lv.bars * 4 * beat, lv.clip ? lv.clip.length : 999f);

        float cardEnd = withIntro ? 16 * beat * 1.5f : 12 * beat;
        CurrentCues = BuildCues(lv, beat, cardEnd + 2f, dur - 2f, seed + index);
        WorldBuilder.Cues = CurrentCues;   // obby paths need the jump gaps
        WorldBuilder.Speed = speed;
        if (world) Destroy(world.gameObject);
        world = WorldBuilder.Build(lib, lv.theme, dur * speed + 80f, seed + index * 17);

        // cards play over the first seconds of the track; cues start after them
        src.clip = lv.clip; src.volume = 0.72f; src.time = 0; src.Play();
        double t0 = AudioSettings.dspTime;
        SongTime = 0;
        Hud.SetProgress(0, false);
        StartCoroutine(TrackTime(t0, dur, index));

        if (withIntro) StartCoroutine(IntroCards(lv, beat));
        else StartCoroutine(LevelCards(lv, index, beat));

        var rnd = new System.Random(seed + index * 31);
        foreach (var c in CurrentCues) RunObstacle.Create(lib, c, speed, world, this, rnd);
        StartCoroutine(CueHud(CurrentCues));
        RunCoins.Build(this, CurrentCues, cardEnd + 0.5f, dur - 1f, world);

        while (SongTime < dur) yield return null;
        src.DOFade(0f, 0.4f);
        yield return new WaitForSeconds(0.4f);
        src.Stop();
        CurrentLevel = -1;
    }

    IEnumerator SayLater(string clip, float delay) { yield return Wait(delay); Say(clip); }

    IEnumerator TrackTime(double t0, float dur, int level)
    {
        while (CurrentLevel == level && SongTime < dur + 0.5f)
        {
            SongTime = (float)(AudioSettings.dspTime - t0);
            yield return null;
        }
    }

    IEnumerator IntroCards(RunLevel lv, float beat)
    {
        float b2 = beat * 4;   // one bar per card
        StartCoroutine(Hud.Card("WELCOME", videoTitle, null, null, 0)); Say(welcomeClip); yield return Wait(b2 * 1.5f);
        StartCoroutine(Hud.Card(levels.Length + " LEVELS", "WATCH THE OBSTACLES!", null, null, 0)); Say("levels"); StartCoroutine(SayLater("obstacles", 1.1f)); yield return Wait(b2 * 1.5f);
        StartCoroutine(Hud.Card("ARE YOU READY?", "", null, null, 0)); Say("ready"); yield return Wait(b2 * 1f);
        StartCoroutine(Hud.Card(lv.title, "", lv.moves, lv.newMoves, 0)); Say(level1Clip); yield return Wait(b2 * 2f);
        Hud.HideCard();
        Hud.SetProgress(0, true);
    }

    IEnumerator LevelCards(RunLevel lv, int index, float beat)
    {
        string[] hype = { "KEEP MOVING!", "FINAL RUN!" };
        string[] hypeVoice = { "keepmoving", "finalrun" };
        int h = Mathf.Clamp(index - 1, 0, hype.Length - 1);
        StartCoroutine(Hud.Card(hype[h], "", null, null, 0)); Say(hypeVoice[h]); yield return Wait(beat * 4);
        StartCoroutine(Hud.Card(lv.title, "", lv.moves, lv.newMoves, 0)); Say(index == 0 ? level1Clip : index == 1 ? level2Clip : level3Clip); yield return Wait(beat * 8);
        Hud.HideCard();
        Hud.SetProgress(0, true);
    }

    WaitForSeconds Wait(float s) => new WaitForSeconds(s);

    public int Coins { get; private set; }
    AudioClip coinClip;
    int coinSfx;

    public void CollectCoin()
    {
        Coins++;
        Hud.SetCoins(Coins);
        if (coinClip == null) coinClip = Resources.Load<AudioClip>("coin");
        if (coinClip && (coinSfx++ % 2 == 0)) voice.PlayOneShot(coinClip, 0.22f);
    }

    // where the camera will be (x side step, y hop/dip) at song time t - mirrors CameraMove
    public Vector2 PathOffset(float t, List<RunCue> cues)
    {
        float x = 0, y = 0;
        foreach (var c in cues)
        {
            float u = t - c.time;
            if (u < -0.4f || u > 2.1f) continue;
            switch (c.move)
            {
                case RunMove.Jump:
                    if (u > -0.35f && u < 0.3f) y = 1.1f * Mathf.Sin(Mathf.PI * (u + 0.35f) / 0.65f);
                    break;
                case RunMove.Duck:
                    if (u > -0.2f && u < 0.3f) y = -0.9f;
                    break;
                case RunMove.Left:
                case RunMove.Right:
                    float side = c.move == RunMove.Left ? -WorldBuilder.LaneGap : WorldBuilder.LaneGap;
                    float k = u < -0.05f ? Mathf.InverseLerp(-0.35f, -0.05f, u) : u < 1.55f ? 1f : 1f - Mathf.InverseLerp(1.55f, 1.95f, u);
                    x = side * k;
                    break;
            }
        }
        return new Vector2(x, y);
    }

    public void Say(string clip)
    {
        var c = Resources.Load<AudioClip>("Voice/" + clip);
        if (c) voice.PlayOneShot(c, 1f);
    }

    IEnumerator CueHud(List<RunCue> cues)
    {
        int lvl = CurrentLevel;
        foreach (var c in cues)
        {
            while (SongTime < c.time - iconLead) { if (CurrentLevel != lvl) yield break; yield return null; }
            Hud.ShowIcon(c.move);
            Say(c.move.ToString());
            StartCoroutine(CameraMove(c));
            while (SongTime < c.time + 0.25f) yield return null;
            Hud.HideIcon();
        }
    }

    // camera reacts like a player: hop, dip, side step
    IEnumerator CameraMove(RunCue c)
    {
        while (SongTime < c.time - 0.35f) yield return null;
        switch (c.move)
        {
            case RunMove.Jump:
                DOTween.To(() => camOffset.y, v => camOffset.y = v, 1.1f, 0.3f).SetEase(Ease.OutQuad)
                    .OnComplete(() => DOTween.To(() => camOffset.y, v => camOffset.y = v, 0f, 0.35f).SetEase(Ease.InQuad));
                break;
            case RunMove.Duck:
                DOTween.To(() => camOffset.y, v => camOffset.y = v, -0.9f, 0.25f).SetEase(Ease.OutQuad)
                    .OnComplete(() => DOTween.To(() => camOffset.y, v => camOffset.y = v, 0f, 0.35f).SetDelay(0.25f));
                break;
            case RunMove.Left:
            case RunMove.Right:
                float side = c.move == RunMove.Left ? -WorldBuilder.LaneGap : WorldBuilder.LaneGap;
                DOTween.To(() => camOffset.x, v => camOffset.x = v, side, 0.3f).SetEase(Ease.OutQuad)
                    .OnComplete(() => DOTween.To(() => camOffset.x, v => camOffset.x = v, 0f, 0.4f).SetDelay(1.6f));
                break;
        }
    }

    void LateUpdate()
    {
        if (!cam || CurrentLevel < 0) return;
        float bob = Mathf.Sin(SongTime * Mathf.PI * 2f * (levels[CurrentLevel].bpm / 60f)) * 0.05f;
        cam.transform.position = new Vector3(camOffset.x, camHeight + camOffset.y + bob, SongTime * speed);
        cam.transform.rotation = Quaternion.Euler(4f, Yaw, -camOffset.x * 1.5f);
        var lv = levels[CurrentLevel];
        float beat = 60f / lv.bpm;
        float dur = lv.bars * 4 * beat;
        Hud.SetProgress(SongTime / dur, SongTime > 1f);
    }

    // ---------- cue plan ----------
    List<RunCue> BuildCues(RunLevel lv, float beat, float start, float end, int s)
    {
        var rnd = new System.Random(s);
        var list = new List<RunCue>();
        int b = Mathf.CeilToInt(start / beat);
        RunMove last = (RunMove)(-1);
        int burst = 0;
        var introQueue = new Queue<RunMove>();
        if (lv.newMoves != null)
            for (int k = 0; k < 2; k++) foreach (var m in lv.newMoves) introQueue.Enqueue(m);

        while (b * beat < end)
        {
            for (int i = 0; i < burstLength && b * beat < end; i++)
            {
                RunMove m;
                if (introQueue.Count > 0 && i % 2 == 0) m = introQueue.Dequeue();
                else
                {
                    int guard = 0;
                    do m = lv.moves[rnd.Next(lv.moves.Length)];
                    while ((m == last || (Heavy(m) && Heavy(last))) && ++guard < 20);
                }
                list.Add(new RunCue { time = b * beat, move = m });
                // side steps and heavy moves need extra room
                b += cueBeats + (m is RunMove.Left or RunMove.Right or RunMove.Dash ? 1 : 0);
                last = m;
            }
            b += restBeats;
            burst++;
        }
        return list;
    }

    static bool Heavy(RunMove m) => m is RunMove.Left or RunMove.Right or RunMove.Dash or RunMove.Smash;
}
