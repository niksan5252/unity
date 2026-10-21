using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

// Records the whole video (Game View incl. HUD + audio) to Recordings/ as MP4, 60 fps.
// Play mode is entered automatically; recording stops when RunDirector.Finished.
public static class RunRecording
{
    const int Fps = 60;
    static RecorderController controller;
    static bool restoreMute;
    static (int w, int h, float mbit, string label) pending;

    [MenuItem("SubwayRun/Record Full Video (4K)")]
    static void Rec4K() => Launch((3840, 2160, 60f, "4K"));

    [MenuItem("SubwayRun/Record Full Video (1080p test)")]
    static void Rec1080() => Launch((1920, 1080, 16f, "1080p"));

    [MenuItem("SubwayRun/Record Full Video (4K)", true)]
    [MenuItem("SubwayRun/Record Full Video (1080p test)", true)]
    static bool CanLaunch() => !EditorApplication.isPlayingOrWillChangePlaymode;

    static void Launch((int, int, float, string) profile)
    {
        pending = profile;
        SessionState.SetBool("RunRecording.pending", true);
        SessionState.SetString("RunRecording.profile", $"{profile.Item1}|{profile.Item2}|{profile.Item3}|{profile.Item4}");
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    static void Hook()
    {
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("RunRecording.pending", false))
            {
                SessionState.SetBool("RunRecording.pending", false);
                var p = SessionState.GetString("RunRecording.profile", "1920|1080|16|1080p").Split('|');
                Begin(int.Parse(p[0]), int.Parse(p[1]), float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture), p[3]);
            }
            if (s == PlayModeStateChange.ExitingPlayMode) Finish();
        };
    }

    static void Begin(int w, int h, float mbit, string label)
    {
        Directory.CreateDirectory("Recordings");
        var dir = UnityEngine.Object.FindAnyObjectByType<RunDirector>();
        string output = Path.Combine("Recordings", (dir ? dir.filePrefix : "SubwayRun") + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + label);

        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "Run Movie";
        movie.Enabled = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.Custom,
            EncodingProfile = CoreEncoderSettings.H264EncodingProfile.High,
            TargetBitRate = mbit,
        };
        movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = w, OutputHeight = h };
        movie.AudioInputSettings.PreserveAudio = true;
        movie.OutputFile = output;

        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        settings.AddRecorderSettings(movie);
        settings.SetRecordModeToManual();
        settings.FrameRatePlayback = FrameRatePlayback.Constant;
        settings.FrameRate = Fps;
        settings.CapFrameRate = true;

        restoreMute = EditorUtility.audioMasterMute;
        EditorUtility.audioMasterMute = false;
        controller = new RecorderController(settings);
        controller.PrepareRecording();
        controller.StartRecording();
        EditorApplication.update += Watch;
        Debug.Log("[RunRecording] recording to " + output + ".mp4");
    }

    static void Watch()
    {
        var d = UnityEngine.Object.FindAnyObjectByType<RunDirector>();
        if (d && d.Finished) EditorApplication.isPlaying = false;
    }

    static void Finish()
    {
        EditorApplication.update -= Watch;
        if (controller == null) return;
        if (controller.IsRecording()) controller.StopRecording();
        controller = null;
        EditorUtility.audioMasterMute = restoreMute;
        Debug.Log("[RunRecording] done");
    }
}
