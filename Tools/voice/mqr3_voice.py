"""Announcer lines for Halloween Quest (MQR #3): edge-tts Ava + loudnorm, into Assets/Resources/Voice.
usage (from the project folder): python -I Tools/voice/mqr3_voice.py"""
import subprocess, os, sys
LINES = {
    "welcome_mqr3": "Welcome to Halloween Quest!",
    "level1_mqr3": "Level one! The Spooky Fair!",
    "level2_mqr3": "Level two! The Witch Forest!",
    "level3_mqr3": "Level three! The Moon Castle!",
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
