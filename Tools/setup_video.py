"""One-shot setup of a new video in the open Unity: tracks -> real BPM -> voice -> build scene.
usage (from C:/Users/PC/SubwayRun):
  python -I Tools/setup_video.py <tracks folder> <prefix> <voice script> "<build menu path>"
example (Red Light Green Light Quest):
  python -I Tools/setup_video.py "C:/Users/PC/Desktop/треки для run/10 видео" mqr4 Tools/voice/mqr4_voice.py "Move Quest Run/4. Red Light Quest - Build Scene"
Takes the first 3 wav/mp3 files by name (level 1, 2, 3). Prints only a short summary."""
import glob, os, re, shutil, subprocess, sys, tempfile, time
SRC, PREFIX, VOICE, MENU = sys.argv[1:5]
PROJ = "C:/Users/PC/SubwayRun"
TRACKS = "Assets/Audio/Tracks"
os.chdir(PROJ)

def run(cmd, step):
    r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    out = (r.stdout + r.stderr).strip()
    if r.returncode != 0:
        print(f"FAIL {step}:\n" + "\n".join(out.splitlines()[-8:])); sys.exit(1)
    return out

def unity(*args):
    return run(["unity", "command", "--project-path", PROJ, *args], "unity " + args[0])

# 1. tracks -> trimmed <prefix>_1..3.wav + measured BPM (the build menu reads <prefix>_bpm.txt)
files = sorted(f for f in glob.glob(os.path.join(SRC, "*")) if f.lower().endswith((".wav", ".mp3")))[:3]
if len(files) < 3: print(f"FAIL tracks: need 3 files in {SRC}, found {len(files)}"); sys.exit(1)
tmp = tempfile.mkdtemp()
for i, f in enumerate(files, 1):
    run(["ffmpeg", "-y", "-loglevel", "error", "-i", f, "-ar", "44100", os.path.join(tmp, f"{i}.wav")], "ffmpeg")
out = run([sys.executable, "-I", "Tools/audio/energy.py", tmp, TRACKS, PREFIX], "energy.py")
shutil.rmtree(tmp, ignore_errors=True)
bpms = [float(m) for m in re.findall(r"bpm ([\d.]+)", out)]
with open(os.path.join(TRACKS, PREFIX + "_bpm.txt"), "w") as fh: fh.write("\n".join(f"{b:.1f}" for b in bpms))
print("tracks:", ", ".join(f"{os.path.basename(f)} -> {b:.0f} BPM" for f, b in zip(files, bpms)))

# 2. announcer lines
run([sys.executable, "-I", VOICE], "voice")
print("voice: ok")

# 3. Unity: recompile, then build + open the scene
unity("recompile")
for _ in range(60):
    time.sleep(3)
    st = unity("recompile_status").lower()
    if not any(w in st for w in ("compiling", "running", "busy", "true")): break
unity("menu", "--path", MENU)
log = unity("console")
errs = [l for l in log.splitlines() if "error" in l.lower() or "exception" in l.lower()]
built = [l for l in log.splitlines() if " built" in l]
print("unity:", built[-1].strip() if built else "build line not found")
if errs: print("console errors:\n" + "\n".join(errs[-6:]))
