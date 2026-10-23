"""One-shot setup for Halloween Quest (MQR #3): tracks -> voice -> build scene in the open Unity.
usage (from C:/Users/PC/SubwayRun): python -I Tools/mqr3_setup.py
Prints only a short summary; on error shows the failing step's last lines."""
import glob, os, re, shutil, subprocess, sys, tempfile, time
SRC = os.path.expanduser("~/Desktop/треки для run 2/3 видео")
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

# 1. tracks: first 3 files (wav/mp3) by name -> trimmed mqr3_1..3.wav + real BPM
files = sorted(f for f in glob.glob(os.path.join(SRC, "*")) if f.lower().endswith((".wav", ".mp3")))[:3]
if len(files) < 3: print(f"FAIL tracks: need 3 files in {SRC}, found {len(files)}"); sys.exit(1)
tmp = tempfile.mkdtemp()
for i, f in enumerate(files, 1):
    run(["ffmpeg", "-y", "-loglevel", "error", "-i", f, "-ar", "44100", os.path.join(tmp, f"{i}.wav")], "ffmpeg")
out = run([sys.executable, "-I", "Tools/audio/energy.py", tmp, TRACKS, "mqr3"], "energy.py")
shutil.rmtree(tmp, ignore_errors=True)
bpms = [float(m) for m in re.findall(r"bpm ([\d.]+)", out)]
with open(os.path.join(TRACKS, "mqr3_bpm.txt"), "w") as fh: fh.write("\n".join(f"{b:.1f}" for b in bpms))
print("tracks:", ", ".join(f"{os.path.basename(f)} -> {b:.0f} BPM" for f, b in zip(files, bpms)))

# 2. announcer lines
run([sys.executable, "-I", "Tools/voice/mqr3_voice.py"], "voice")
print("voice: ok")

# 3. Unity: recompile, build + open the scene
unity("recompile")
for _ in range(60):
    time.sleep(3)
    st = unity("recompile_status").lower()
    if not any(w in st for w in ("compiling", "running", "busy", "true")): break
unity("menu", "--path", "Move Quest Run/3. Halloween Quest - Build Scene")
log = unity("console")
errs = [l for l in log.splitlines() if "error" in l.lower() or "exception" in l.lower()]
built = [l for l in log.splitlines() if "MQR_HalloweenQuest built" in l]
print("unity:", built[-1].strip() if built else "build line not found")
if errs: print("console errors:\n" + "\n".join(errs[-6:]))
