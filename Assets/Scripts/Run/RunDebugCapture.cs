using System.Collections.Generic;
using UnityEngine;

// Test helper: saves a screenshot at exact song times. Format "level:time,level:time" (level 0-based).
public class RunDebugCapture : MonoBehaviour
{
    public string shots = "";
    public string folder = "Temp/shots";
    public bool autoPerMove = true;
    public bool hideHud;
    public int superSize = 1;   // first cue of every move: shots at -0.7 s and -0.1 s
    int autoLevel = -1, n = 100;
    readonly List<(int lvl, float t, int idx)> queue = new List<(int, float, int)>();
    RunDirector d;

    void Start()
    {
        d = GetComponent<RunDirector>();
        System.IO.Directory.CreateDirectory(folder);
        int i = 0;
        foreach (var s in shots.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
        {
            var p = s.Split(':');
            queue.Add((int.Parse(p[0]), float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture), i++));
        }
    }

    void LateUpdate()
    {
        if (!d) return;
        if (autoPerMove && d.CurrentLevel >= 0 && d.CurrentLevel != autoLevel)
        {
            autoLevel = d.CurrentLevel;
            var seen = new HashSet<RunMove>();
            foreach (var c in d.CurrentCues)
                if (seen.Add(c.move))
                {
                    queue.Add((autoLevel, c.time - 0.7f, n++));
                    queue.Add((autoLevel, c.time - 0.1f, n++));
                }
        }
        for (int k = queue.Count - 1; k >= 0; k--)
        {
            var q = queue[k];
            if (d.CurrentLevel == q.lvl && d.SongTime >= q.t)
            {
                if (hideHud && d.Hud) d.Hud.gameObject.SetActive(false);
                ScreenCapture.CaptureScreenshot($"{folder}/s{q.idx:00}_L{q.lvl + 1}_{d.SongTime:F1}.png", superSize);
                queue.RemoveAt(k);
            }
        }
    }
}
