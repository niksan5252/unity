"""Trim the quiet intro of each Suno track and print bass energy per bar.
usage: python -I energy.py <src dir with 1.wav 2.wav ...> <dst Assets/Audio/Tracks> <prefix>"""
import librosa, numpy as np, soundfile as sf, sys, glob, os
src, dst, pref = sys.argv[1], sys.argv[2], sys.argv[3]
for i, f in enumerate(sorted(glob.glob(src + "/*.wav")), 1):
    y, sr = sf.read(f); mono = y.mean(1) if y.ndim > 1 else y
    m = librosa.resample(mono.astype(np.float32), orig_sr=sr, target_sr=22050)
    tempo, beats = librosa.beat.beat_track(y=m, sr=22050, units="time"); bpm = float(np.atleast_1d(tempo)[0])
    S = np.abs(librosa.stft(m, n_fft=2048, hop_length=256)); fr = librosa.fft_frequencies(sr=22050)
    lb = S[fr < 150].sum(0); ref = np.median(lb[len(lb) // 4:3 * len(lb) // 4])
    k = np.argmax(lb > 0.6 * ref) * 256 / 22050
    b = beats[beats <= k + 0.05]; cut = max(0.0, (b[-1] if len(b) else k) - 0.02)
    y = y[int(cut * sr):]; sf.write(f"{dst}/{pref}_{i}.wav", y, sr)
    m = m[int(cut * 22050):]; S = np.abs(librosa.stft(m, n_fft=2048, hop_length=512)); lb = S[fr < 150].sum(0)
    bar = 4 * 60 / bpm; fr2 = 512 / 22050
    vals = [lb[int(j * bar / fr2):int((j + 1) * bar / fr2)].mean() for j in range(int(len(m) / 22050 / bar))]
    med = np.median(vals)
    print(i, os.path.basename(f), f"bpm {bpm:.1f} cut {cut:.2f} len {len(y)/sr:.1f}s bars {len(vals)}")
    print("  ", " ".join(f"{j}:{v/med:.1f}" for j, v in enumerate(vals)))
