using UnityEngine;

public enum RunMove { Jump, Duck, Left, Right, Dash, Smash, Mirror }
public enum RunTheme { Day, Tunnel, Neon, Village, Mine, Nether, SkyObby, LavaObby, RainbowObby, Mansion, PumpkinFarm, Graveyard, GummyForest, ChocolateFactory, CottonCandySky, JungleValley, VolcanoRun, DinoValley, McPlains, McCaves, McNether, McTaiga, McJungle, McEnd, NeonCity, ConcertStage, DemonRealm, SpookyCarnival, WitchForest, MoonCastle }

[System.Serializable]
public class RunLevel
{
    public string title = "LEVEL 1";
    public RunTheme theme;
    public AudioClip clip;
    public float bpm = 152f;
    public int bars = 40;
    public int cueBeats;    // 0 = director default (for slow tracks: fewer beats between cues)
    public int restBeats;   // 0 = director default
    public RunMove[] moves;
    public RunMove[] newMoves;
}

public struct RunCue
{
    public float time;
    public RunMove move;
    public int variant;   // MIRROR: which pose (RunMoveInfo.Poses)
}

public static class RunMoveInfo
{
    // pose-wall silhouettes in Resources/Poses
    public static readonly string[] Poses = { "star", "flamingo", "squat", "warrior", "dab", "lunge", "tpose", "karate", "toetouch" };

    public static string Label(RunMove m) => m switch
    {
        RunMove.Jump => "JUMP",
        RunMove.Duck => "DUCK",
        RunMove.Left => "LEFT",
        RunMove.Right => "RIGHT",
        RunMove.Dash => "DASH",
        RunMove.Smash => "SMASH",
        _ => "MIRROR ME",
    };

    public static string Glyph(RunMove m) => m switch
    {
        RunMove.Jump => "↑",
        RunMove.Duck => "↓",
        RunMove.Left => "←",
        RunMove.Right => "→",
        RunMove.Dash => "»",
        RunMove.Smash => "!",
        _ => "↔",
    };
}
