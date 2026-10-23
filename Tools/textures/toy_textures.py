"""Own textures for Red Light Green Light Quest (MQR #4): playground, toy store, candy factory.
usage: python -I toy_textures.py <Assets/Textures/Toy>"""
from PIL import Image, ImageDraw, ImageFilter, ImageFont
import random, sys, os, math
random.seed(404)
O = sys.argv[1] if len(sys.argv) > 1 else "Assets/Textures/Toy"
os.makedirs(O, exist_ok=True)
FONT = next((f for f in ["/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", "C:/Windows/Fonts/arialbd.ttf"] if os.path.exists(f)), None)
def font(sz): return ImageFont.truetype(FONT, sz) if FONT else ImageFont.load_default()
def jit(c, v): return tuple(max(0, min(255, c[i] + random.randint(-v, v))) for i in range(3))
def save(im, n): im.save(os.path.join(O, n + ".png"))

# running track across the 9 m road: rubber + white lane lines between the 3 lanes (x = -2.6, 0, 2.6)
W, H = 512, 256
im = Image.new("RGB", (W, H))
for y in range(H):
    for x in range(W): im.putpixel((x, y), jit((214, 72, 52), 12))
d = ImageDraw.Draw(im)
for xm in (-3.9, -1.3, 1.3, 3.9):
    x = int((xm + 4.5) / 9 * W)
    d.rectangle([x - 4, 0, x + 4, H], fill=(250, 250, 245))
save(im, "track")

# mowed grass with light/dark stripes
S = 256
im = Image.new("RGB", (S, S))
for y in range(S):
    base = (92, 200, 70) if (y // 32) % 2 == 0 else (78, 182, 60)
    for x in range(S): im.putpixel((x, y), jit(base, 10))
save(im, "grass")

# toy store floor: glossy teal / cream checker
im = Image.new("RGB", (S, S)); d = ImageDraw.Draw(im)
for i in range(4):
    for j in range(4):
        d.rectangle([i * 64, j * 64, i * 64 + 63, j * 64 + 63], fill=(70, 200, 210) if (i + j) % 2 == 0 else (255, 244, 220))
save(im, "checker")

# candy factory floor: pink tiles, white grout
im = Image.new("RGB", (S, S), (255, 255, 255)); d = ImageDraw.Draw(im)
for i in range(4):
    for j in range(4):
        d.rounded_rectangle([i * 64 + 3, j * 64 + 3, i * 64 + 60, j * 64 + 60], 8, fill=jit((255, 150, 200), 8))
save(im, "pinktile")

# letter blocks
cols = [(255, 80, 80), (60, 160, 255), (255, 210, 40), (90, 210, 90), (190, 100, 255), (255, 150, 40)]
for k, ch in enumerate("ABCDEF"):
    im = Image.new("RGB", (128, 128), cols[k]); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 127, 127], outline=(255, 255, 255), width=10)
    f = font(84); w = d.textlength(ch, font=f)
    d.text(((128 - w) / 2 + 3, 12 + 3), ch, font=f, fill=(0, 0, 0, 90))
    d.text(((128 - w) / 2, 12), ch, font=f, fill=(255, 255, 255))
    save(im, "block" + ch)

# beach ball: wedges around (u wraps around the sphere)
im = Image.new("RGB", (256, 128)); d = ImageDraw.Draw(im)
bc = [(255, 60, 60), (255, 255, 255), (40, 140, 255), (255, 255, 255), (255, 220, 30), (255, 255, 255)]
for i, c in enumerate(bc): d.rectangle([i * 256 // 6, 0, (i + 1) * 256 // 6, 128], fill=c)
d.rectangle([0, 0, 256, 10], fill=(255, 255, 255)); d.rectangle([0, 118, 256, 128], fill=(255, 255, 255))
save(im, "beachball")

# stop line: red / white checker strip with STOP
im = Image.new("RGB", (512, 64), (255, 255, 255)); d = ImageDraw.Draw(im)
for i in range(16):
    for j in range(2):
        if (i + j) % 2 == 0: d.rectangle([i * 32, j * 32, i * 32 + 31, j * 32 + 31], fill=(230, 30, 40))
save(im, "stopline")

# conveyor belt with yellow chevrons
im = Image.new("RGB", (128, 256), (60, 60, 70)); d = ImageDraw.Draw(im)
for y in range(0, 256, 64):
    d.polygon([(20, y + 40), (64, y + 10), (108, y + 40), (108, y + 58), (64, y + 28), (20, y + 58)], fill=(255, 200, 40))
save(im, "conveyor")

# robot face screen: dark glass with a happy mouth
im = Image.new("RGB", (256, 256), (20, 24, 40)); d = ImageDraw.Draw(im)
d.rounded_rectangle([6, 6, 249, 249], 40, outline=(120, 220, 255), width=10)
d.arc([70, 120, 186, 210], 15, 165, fill=(120, 220, 255), width=14)
save(im, "botface")

# "RED LIGHT  GREEN LIGHT" arch sign
W, H = 1024, 192
im = Image.new("RGB", (W, H), (25, 25, 45)); d = ImageDraw.Draw(im)
d.rounded_rectangle([6, 6, W - 7, H - 7], 30, outline=(255, 255, 255), width=10)
f = font(56)
for word, col, cx in (("RED LIGHT", (255, 60, 60), W * 0.26), ("GREEN LIGHT", (80, 240, 90), W * 0.73)):
    w = d.textlength(word, font=f)
    d.text((cx - w / 2 + 5, 62 + 5), word, font=f, fill=(0, 0, 0))
    d.text((cx - w / 2, 62), word, font=f, fill=col)
d.ellipse([W / 2 - 26, H / 2 - 26, W / 2 + 26, H / 2 + 26], fill=(255, 210, 40))
save(im, "rlglsign")
print("ok", len(os.listdir(O)))
