using UnityEngine;

public enum RunMove { Jump, Duck, Left, Right, Dash, Smash, Mirror }
public enum RunTheme { Day, Tunnel, Neon }

[System.Serializable]
public class RunLevel
{
    public string title = "LEVEL 1";
    public RunTheme theme;
    public AudioClip clip;
    public float bpm = 152f;
    public int bars = 40;
    public RunMove[] moves;
    public RunMove[] newMoves;
}

public struct RunCue
{
    public float time;
    public RunMove move;
}

public static class RunMoveInfo
{
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
