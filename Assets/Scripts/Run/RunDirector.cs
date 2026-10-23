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
    public string filePrefix = "SubwayRun";
    public bool questStyle;
    public float endScreenSeconds;   // >0: hold an end-screen card this long for YouTube end-screen elements
    public AudioClip endScreenMusic;   // Move Quest Run look (purple / lime / gold HUD)
    public string welcomeClip = "welcome";
    public string level2Clip = "level2";
    public string level1Clip = "level1";
    public string level3Clip = "level3";      // >=0 plays only that level (for testing)

    public RunHud Hud { get; private set; }
    public RunBuddy Buddy { get; private set; }
    public float Yaw;   // camera look-back angle (RunLookBack)
    public float RideDip;   // camera drop while sitting in the minecart (McRide)
    public McHud Mc { get; private set; }
    public bool buddyIsHeroine;   // Minecraft mode: level 2 runs with the girl hero
    public float SongTime { get; private set; }
    public int CurrentLevel { get; private set; } = -1;
    public bool Finished { get; private set; }
    public List<RunCue> CurrentCues { get; private set; } = new List<RunCue>();

    Camera cam;
    AudioSource src, voice;
    float shake, fovKick, baseFov = -1f;

    // game-feel hits from obstacles / fly-bys: camera shake (metres) and a short FOV punch (degrees)
    public void Shake(float amount) => shake = Mathf.Max(shake, amount);
    public void Kick(float degrees) => fovKick = Mathf.Max(fovKick, degrees);
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
        if (lib && lib.pixTex != null)
            foreach (var t in lib.pixTex) if (t) BlockFigure.Tex[t.name] = t;
        RunHud.Quest = questStyle;
        Hud = RunHud.Create();
        if (lib && lib.mcMode)
        {
            Mc = McHud.Create(this);
            Mc.Show(false);
            Hud.hideCoins = true;
            Buddy = RunBuddy.CreateFigure(this, questStyle ? BlockFigure.Kind.Knight : BlockFigure.Kind.Hero);
            McRide.Create(this);
        }
        if (lib && lib.buddy && !lib.mcMode) Buddy = RunBuddy.Create(this, lib.buddy, lib.buddyYaw);
        if (lib && lib.spookyMode && Buddy) SpookyWorld.WitchHat(Buddy.gameObject);
        if (lib && (lib.halloweenMode || lib.dinoMode || lib.mcMode || lib.kpopMode || lib.spookyMode || lib.toyMode || lib.chaser))
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
        if (endScreenSeconds > 0)
        {
            if (endScreenMusic) { src.clip = endScreenMusic; src.time = 0; src.volume = 0f; src.Play(); src.DOFade(0.45f, 0.6f); }
            SayQ("thanks_end");
            yield return Hud.EndScreen(endScreenSeconds);
            if (endScreenMusic) { src.DOFade(0f, 0.5f); yield return new WaitForSeconds(0.5f); src.Stop(); }
        }
        else
        {
            Say("thanks");
            yield return Hud.Card("THANKS FOR WATCHING", "SEE YOU SOON!", null, null, 4f);
        }
        Finished = true;
    }

    IEnumerator PlayLevel(int index, bool withIntro)
    {
        var lv = levels[index];
        CurrentLevel = index;
        if (lib && lib.mcMode && Buddy) Buddy.SwapFigure(index == 1 ? (questStyle ? BlockFigure.Kind.KnightGold : BlockFigure.Kind.Heroine) : (questStyle ? BlockFigure.Kind.Knight : BlockFigure.Kind.Hero));
        float beat = 60f / lv.bpm;
        float dur = Mathf.Min(lv.bars * 4 * beat, lv.clip ? lv.clip.length : 999f);

        bool montage = withIntro && lib && lib.spoilers != null && lib.spoilers.Length > 0;
        float cardEnd = withIntro ? (montage ? 32 : 24) * beat : 12 * beat;
        CurrentCues = BuildCues(lv, beat, cardEnd + 2f, dur - 2f, seed + index);
        FreezeHold = beat * 4f;
        ending = false;
        BuildDist(dur);
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
        if (Mc) Mc.Toast(index == levels.Length - 1 ? "Minecraft World Complete!" : "Level Complete!", "diamond");
        ending = true;
        Time.timeScale = 1f;
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
        if (lib && lib.spoilers != null && lib.spoilers.Length > 0) { SayQ("spoiler"); yield return Hud.Montage(lib.spoilers, b2 * 2f); }
        StartCoroutine(Hud.Card("WELCOME", videoTitle, null, null, 0)); SayQ(welcomeClip); yield return Wait(b2 * 1.5f);
        StartCoroutine(Hud.Card(levels.Length + " LEVELS", "WATCH THE OBSTACLES!", null, null, 0)); SayQ("levels"); SayQ("obstacles"); yield return Wait(b2 * 1.5f);
        StartCoroutine(Hud.Card("ARE YOU READY?", "", null, null, 0)); SayQ("ready"); yield return Wait(b2 * 1f);
        StartCoroutine(Hud.Card(lv.title, "", lv.moves, lv.newMoves, 0)); SayQ(level1Clip); yield return Wait(b2 * 2f);
        Hud.HideCard();
        Hud.SetProgress(0, true);
    }

    IEnumerator LevelCards(RunLevel lv, int index, float beat)
    {
        string[] hype = { "KEEP MOVING!", "FINAL RUN!" };
        string[] hypeVoice = { "keepmoving", "finalrun" };
        int h = Mathf.Clamp(index - 1, 0, hype.Length - 1);
        StartCoroutine(Hud.Card(hype[h], "", null, null, 0)); SayQ(hypeVoice[h]); yield return Wait(beat * 4);
        StartCoroutine(Hud.Card(lv.title, "", lv.moves, lv.newMoves, 0)); SayQ(index == 0 ? level1Clip : index == 1 ? level2Clip : level3Clip); yield return Wait(beat * 8);
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
        if (Mc) { Mc.AddXp(); if (lib.sfxOrb) { voice.pitch = 1f; voice.PlayOneShot(lib.sfxOrb, 0.18f); } return; }
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

    public void PlaySfx(AudioClip c, float vol)
    {
        if (c) voice.PlayOneShot(c, vol);
    }

    public void Say(string clip)
    {
        var c = Resources.Load<AudioClip>("Voice/" + clip);
        if (c) { voice.PlayOneShot(c, 1f); voiceBusyUntil = Mathf.Max(voiceBusyUntil, Time.time + Trimmed(c)); }
    }

    // announcer lines in a queue: each waits until the previous one has finished
    float voiceBusyUntil;
    public void SayQ(string clip) => StartCoroutine(SayQueued(clip));
    IEnumerator SayQueued(string clip)
    {
        while (Time.time < voiceBusyUntil) yield return null;
        Say(clip);
    }

    // clip length without its trailing silence (TTS files end with ~0.3-0.6 s of quiet)
    static readonly Dictionary<AudioClip, float> spoken = new Dictionary<AudioClip, float>();
    static float Trimmed(AudioClip c)
    {
        if (spoken.TryGetValue(c, out var len)) return len;
        len = c.length;
        var data = new float[c.samples * c.channels];
        if (c.GetData(data, 0))
        {
            int last = data.Length - 1;
            while (last > 0 && Mathf.Abs(data[last]) < 0.02f) last--;
            len = (last / (float)c.channels) / c.frequency + 0.12f;
        }
        return spoken[c] = len;
    }

    IEnumerator CueHud(List<RunCue> cues)
    {
        int lvl = CurrentLevel;
        foreach (var c in cues)
        {
            while (SongTime < c.time - iconLead) { if (CurrentLevel != lvl) yield break; yield return null; }
            Hud.ShowIcon(c.move, c.move == RunMove.Mirror ? Resources.Load<Texture2D>("Poses/w_" + RunMoveInfo.Poses[c.variant % RunMoveInfo.Poses.Length]) : null);
            if (Mc) Mc.SelectFor(c.move);
            Say(c.move.ToString());
            StartCoroutine(CameraMove(c));
            if (c.move == RunMove.Freeze) StartCoroutine(FreezeShow(c));
            // FREEZE keeps its icon up for the whole red light
            float hideAt = c.move == RunMove.Freeze ? c.time + FreezeHold : c.time + 0.25f;
            while (SongTime < hideAt) yield return null;
            Hud.HideIcon();
            if (Mc) Mc.Praise(c.move);
        }
    }

    // RED LIGHT: banner + red screen edges for the whole hold, then GREEN LIGHT and off we go
    IEnumerator FreezeShow(RunCue c)
    {
        while (SongTime < c.time - 0.05f) yield return null;
        Hud.Banner("RED LIGHT!", new Color(1f, 0.22f, 0.18f));
        Hud.Tint(new Color(1f, 0.1f, 0.1f), 0.55f, FreezeHold);
        while (SongTime < c.time + FreezeHold) yield return null;
        Say("greenlight");
        Hud.Banner("GREEN LIGHT!", new Color(0.35f, 1f, 0.3f));
        Hud.Tint(new Color(0.2f, 1f, 0.3f), 0.45f, 0.35f);
        Kick(6f);
    }

    // ---------- FREEZE: the run really stops ----------
    // Distance along the road is no longer SongTime * speed: around each FREEZE cue the speed ramps to 0,
    // holds for FreezeHold, ramps back. Everything placed on the road uses Dist(t); the world itself
    // slows to a near stop (Time.timeScale) and the music drops out while frozen.
    public float FreezeHold { get; private set; } = 1.6f;
    const float DistStep = 1f / 120f, FreezeIn = 0.3f, FreezeOut = 0.45f;
    float[] distTable;
    bool hasFreeze, ending;

    // 1 = running, 0 = frozen
    public float FreezeFactor(float t)
    {
        if (!hasFreeze) return 1f;
        float f = 1f;
        foreach (var c in CurrentCues)
        {
            if (c.move != RunMove.Freeze) continue;
            float u = t - c.time;
            if (u < -FreezeIn || u > FreezeHold + FreezeOut) continue;
            float g = u < 0 ? -u / FreezeIn : u < FreezeHold ? 0f : (u - FreezeHold) / FreezeOut;
            f = Mathf.Min(f, g);
        }
        return f;
    }

    void BuildDist(float dur)
    {
        hasFreeze = CurrentCues.Exists(c => c.move == RunMove.Freeze);
        if (!hasFreeze) { distTable = null; return; }
        int n = Mathf.CeilToInt((dur + 10f) / DistStep) + 2;
        distTable = new float[n];
        for (int i = 1; i < n; i++) distTable[i] = distTable[i - 1] + speed * DistStep * FreezeFactor((i - 0.5f) * DistStep);
    }

    public float Dist(float t)
    {
        if (distTable == null || t <= 0) return t * speed;
        float k = t / DistStep;
        int i = (int)k, last = distTable.Length - 1;
        if (i >= last) return distTable[last] + (t - last * DistStep) * speed;
        return Mathf.Lerp(distTable[i], distTable[i + 1], k - i);
    }

    public float InvDist(float z)
    {
        if (distTable == null || z <= 0) return z / speed;
        int last = distTable.Length - 1;
        if (z >= distTable[last]) return last * DistStep + (z - distTable[last]) / speed;
        int lo = 0, hi = last;
        while (hi - lo > 1) { int mid = (lo + hi) / 2; if (distTable[mid] < z) lo = mid; else hi = mid; }
        float span = distTable[hi] - distTable[lo];
        return (lo + (span > 1e-6f ? (z - distTable[lo]) / span : 0f)) * DistStep;
    }

    void OnDestroy() { Time.timeScale = 1f; }

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
        float ff = FreezeFactor(SongTime);
        if (hasFreeze && !ending)
        {
            Time.timeScale = Mathf.Lerp(0.04f, 1f, ff);          // the whole world holds its breath
            src.volume = 0.72f * Mathf.Lerp(0.05f, 1f, ff);      // ...and the music drops out
        }
        float bob = Mathf.Sin(SongTime * Mathf.PI * 2f * (levels[CurrentLevel].bpm / 60f)) * 0.05f * ff;
        cam.transform.position = new Vector3(camOffset.x, camHeight + camOffset.y + bob + RideDip, Dist(SongTime));
        cam.transform.rotation = Quaternion.Euler(4f, Yaw, -camOffset.x * 1.5f);
        if (shake > 0.002f)
        {
            float st = Time.time * 23f;
            cam.transform.position += new Vector3(Mathf.PerlinNoise(st, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, st) - 0.5f, 0) * (2f * shake);
            shake *= Mathf.Exp(-Time.deltaTime * 7f);
        }
        if (baseFov < 0) baseFov = cam.fieldOfView;
        if (fovKick > 0.01f || cam.fieldOfView != baseFov)
        {
            cam.fieldOfView = baseFov + fovKick;
            fovKick *= Mathf.Exp(-Time.deltaTime * 5f);
        }
        var lv = levels[CurrentLevel];
        float beat = 60f / lv.bpm;
        float dur = lv.bars * 4 * beat;
        Hud.SetProgress(SongTime / dur, SongTime > 1f);
        if (Mc) Mc.Show(SongTime > 1f && !Hud.CardVisible);
    }

    // ---------- cue plan ----------
    List<RunCue> BuildCues(RunLevel lv, float beat, float start, float end, int s)
    {
        var rnd = new System.Random(s);
        var list = new List<RunCue>();
        int b = Mathf.CeilToInt(start / beat);
        RunMove last = (RunMove)(-1);
        int burst = 0;
        int poseIdx = rnd.Next(RunMoveInfo.Poses.Length);
        var introQueue = new Queue<RunMove>();
        if (lv.newMoves != null)
            for (int k = 0; k < 2; k++) foreach (var m in lv.newMoves) if (m != RunMove.Freeze) introQueue.Enqueue(m);
        // FREEZE is not random: the 3rd and 6th cue of every burst (RED LIGHT twice per burst)
        bool freezeLevel = System.Array.IndexOf(lv.moves, RunMove.Freeze) >= 0;

        while (b * beat < end)
        {
            for (int i = 0; i < burstLength && b * beat < end; i++)
            {
                RunMove m;
                if (freezeLevel && (i == 2 || i == 5)) m = RunMove.Freeze;
                else if (introQueue.Count > 0 && i % 2 == 0) m = introQueue.Dequeue();
                else
                {
                    int guard = 0;
                    do m = lv.moves[rnd.Next(lv.moves.Length)];
                    while ((m == last || m == RunMove.Freeze || (Heavy(m) && Heavy(last)) || (m == RunMove.Mirror && rnd.NextDouble() < 0.6)) && ++guard < 20);
                }
                list.Add(new RunCue { time = b * beat, move = m, variant = m == RunMove.Mirror ? poseIdx++ : 0 });
                // side steps and heavy moves need extra room
                b += (lv.cueBeats > 0 ? lv.cueBeats : cueBeats) + (m is RunMove.Left or RunMove.Right or RunMove.Dash ? 1 : m == RunMove.Mirror ? 3 : m == RunMove.Freeze ? 4 : 0);   // hold the pose / the freeze
                last = m;
            }
            b += lv.restBeats > 0 ? lv.restBeats : restBeats;
            burst++;
        }
        return list;
    }

    static bool Heavy(RunMove m) => m is RunMove.Left or RunMove.Right or RunMove.Dash or RunMove.Smash or RunMove.Mirror or RunMove.Freeze;
}
