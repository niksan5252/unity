"""Announcer lines for Minecraft Quest 2 (MQR #5): edge-tts Ava + loudnorm, into Assets/Resources/Voice.
usage (from the project folder): python -I Tools/voice/mqr5_voice.py"""
import subprocess, os, sys
LINES = {
    "welcome_mqr5": "Welcome to Minecraft Quest two!",
    "level1_mqr5": "Level one! The Cherry Grove!",
    "level2_mqr5": "Level two! The Warm Ocean!",
    "level3_mqr5": "Level three! The Nether! Boss battle!",
}
out = os.path.join("Assets", "Resources", "Voice")
for name, text in LINES.items():
    mp3 = name + ".mp3"
    subprocess.run([sys.executable, "-m", "edge_tts", "--voice", "en-US-AvaNeural", "--rate=+12%", "--pitch=+4Hz",
                    "--text", text, "--write-media", mp3], check=True)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", mp3, "-af", "loudnorm=I=-11", "-ar", "44100",
                    os.path.join(out, name + ".wav")], check=True)
    os.remove(mp3)
    print("ok", name)
