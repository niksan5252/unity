"""Announcer lines for Red Light Green Light Quest (MQR #4): edge-tts Ava + loudnorm, into Assets/Resources/Voice.
usage (from the project folder): python -I Tools/voice/mqr4_voice.py"""
import subprocess, os, sys
LINES = {
    "welcome_mqr4": "Welcome to Red Light, Green Light Quest!",
    "level1_mqr4": "Level one! The Playground!",
    "level2_mqr4": "Level two! The Toy Store!",
    "level3_mqr4": "Level three! The Candy Factory!",
    "Freeze": "Red light! Freeze!",
    "greenlight": "Green light! Go!",
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
