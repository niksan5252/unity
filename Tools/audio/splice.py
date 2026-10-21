"""Build 40-bar level tracks from bar spans of trimmed Suno tracks (short crossfade at each joint).
usage: python -I splice.py <bpm> <out.wav>=<src.wav>:a-b,c-d [...]   (paths relative to Assets/Audio/Tracks)"""
import soundfile as sf, numpy as np, sys
D = "C:/Users/PC/SubwayRun/Assets/Audio/Tracks/"
bar = 4 * 60 / float(sys.argv[1])
for spec in sys.argv[2:]:
    out, rest = spec.split("=")
    src, spans = rest.split(":")
    spans = [tuple(int(x) for x in s.split("-")) for s in spans.split(",")]
    y, sr = sf.read(D + src); xf = int(0.03 * sr)
    parts = [y[int(a * bar * sr):int(b * bar * sr)] for a, b in spans]
    res = parts[0]
    for p in parts[1:]:
        fade = np.linspace(0, 1, xf)[:, None] if res.ndim > 1 else np.linspace(0, 1, xf)
        res = np.concatenate([res[:-xf], res[-xf:] * (1 - fade) + p[:xf] * fade, p[xf:]])
    sf.write(D + out, res, sr)
    print(out, round(len(res) / sr, 1), "s", sum(b - a for a, b in spans), "bars")
