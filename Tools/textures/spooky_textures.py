"""Own textures for Halloween Quest (MQR #3): carnival, witch forest, moon castle.
usage: python -I spooky_textures.py <Assets/Textures/Spooky>"""
from PIL import Image, ImageDraw, ImageFilter, ImageFont
import random, sys, os, math
random.seed(1031)
O = sys.argv[1] if len(sys.argv) > 1 else "Assets/Textures/Spooky"
os.makedirs(O, exist_ok=True)
FONT = None
for f in ["/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", "C:/Windows/Fonts/arialbd.ttf"]:
    if os.path.exists(f): FONT = f; break

def jit(c, v): return tuple(max(0, min(255, c[i] + random.randint(-v, v))) for i in range(3))
def save(im, n): im.save(os.path.join(O, n + ".png"))
def font(sz): return ImageFont.truetype(FONT, sz) if FONT else ImageFont.load_default()

# tent canvas: orange / purple stripes with soft fold shading
S = 256
im = Image.new("RGB", (S, S)); d = ImageDraw.Draw(im)
for x in range(S):
    k = (x // 32) % 2
    base = (255, 128, 20) if k == 0 else (130, 40, 220)
    shade = 0.82 + 0.18 * math.sin((x % 32) / 32 * math.pi)
    d.line([(x, 0), (x, S)], fill=tuple(int(c * shade) for c in base))
save(im, "tent")

# lime / black stripes (second tent colour)
im = Image.new("RGB", (S, S)); d = ImageDraw.Draw(im)
for x in range(S):
    k = (x // 32) % 2
    base = (150, 235, 40) if k == 0 else (40, 20, 60)
    shade = 0.82 + 0.18 * math.sin((x % 32) / 32 * math.pi)
    d.line([(x, 0), (x, S)], fill=tuple(int(c * shade) for c in base))
save(im, "tent2")

# boardwalk planks across the road (purple-stained wood)
im = Image.new("RGB", (S, S)); d = ImageDraw.Draw(im)
for y in range(0, S, 32):
    col = jit((120, 70, 110), 14)
    d.rectangle([0, y, S, y + 29], fill=col)
    for _ in range(40):
        yy = y + random.randint(2, 27); x0 = random.randint(0, S)
        d.line([(x0, yy), (x0 + random.randint(20, 90), yy)], fill=jit((95, 52, 88), 8))
    d.rectangle([0, y + 29, S, y + 31], fill=(40, 18, 40))
    for x in (20, S - 20): d.ellipse([x - 2, y + 13, x + 2, y + 17], fill=(210, 190, 150))
save(im, "boardwalk")

# mossy forest path: round stones in glowing teal moss
im = Image.new("RGB", (S, S), (24, 60, 52)); d = ImageDraw.Draw(im)
for _ in range(2500):
    x, y = random.randint(0, S), random.randint(0, S); d.point((x, y), fill=jit((40, 110, 85), 18))
for _ in range(36):
    x, y, r = random.randint(0, S), random.randint(0, S), random.randint(14, 30)
    for ox in (-S, 0, S):
        for oy in (-S, 0, S):
            d.ellipse([x - r + ox, y - r * 0.8 + oy, x + r + ox, y + r * 0.8 + oy], fill=jit((105, 100, 125), 12), outline=(60, 160, 140))
im = im.filter(ImageFilter.SMOOTH)
save(im, "mosspath")

# night grass / forest floor
im = Image.new("RGB", (S, S))
for y in range(S):
    for x in range(S): im.putpixel((x, y), jit((34, 70, 58), 14))
im = im.filter(ImageFilter.SMOOTH_MORE)
save(im, "nightgrass")

# castle cobblestones
im = Image.new("RGB", (S, S), (50, 46, 66)); d = ImageDraw.Draw(im)
for row in range(8):
    off = 16 if row % 2 else 0
    for c in range(-1, 8):
        x, y = c * 32 + off, row * 32
        d.rounded_rectangle([x + 2, y + 2, x + 30, y + 30], 6, fill=jit((112, 106, 135), 14))
save(im, "cobble")

# castle wall bricks
im = Image.new("RGB", (S, S), (40, 36, 52)); d = ImageDraw.Draw(im)
for row in range(8):
    off = 32 if row % 2 else 0
    for c in range(-1, 5):
        x, y = c * 64 + off, row * 32
        d.rectangle([x + 2, y + 2, x + 62, y + 30], fill=jit((92, 86, 118), 12))
save(im, "castlewall")

# glowing rune stones (DASH wall in the witch forest); dark stone, bright green runes -> also the emission map
im = Image.new("RGB", (S, S), (34, 40, 48)); d = ImageDraw.Draw(im)
for _ in range(1600):
    d.point((random.randint(0, S), random.randint(0, S)), fill=jit((48, 56, 64), 10))
d.rectangle([0, 0, S - 1, S - 1], outline=(20, 24, 28), width=8)
cx, cy = S // 2, S // 2
pts = [(cx, cy - 70), (cx + 55, cy + 50), (cx - 55, cy + 50)]
d.line(pts + [pts[0]], fill=(120, 255, 90), width=10)
d.ellipse([cx - 22, cy - 6, cx + 22, cy + 38], outline=(120, 255, 90), width=8)
d.line([(cx, cy - 100), (cx, cy - 80)], fill=(120, 255, 90), width=8)
im = Image.blend(im, im.filter(ImageFilter.GaussianBlur(4)), 0.35)
save(im, "runes")

# "BOO!" sign for the carnival DUCK beam: marquee bulbs around a purple board
W, H = 1024, 192
im = Image.new("RGB", (W, H), (60, 14, 90)); d = ImageDraw.Draw(im)
d.rectangle([10, 10, W - 11, H - 11], outline=(255, 150, 30), width=8)
for x in range(30, W - 20, 48):
    for y in (26, H - 26): d.ellipse([x - 9, y - 9, x + 9, y + 9], fill=(255, 235, 140))
for i, word in enumerate(["BOO!", "DUCK!", "BOO!"]):
    f = font(92); w = d.textlength(word, font=f)
    x = (i + 0.5) * W / 3 - w / 2
    d.text((x + 5, 46 + 5), word, font=f, fill=(20, 0, 30))
    d.text((x, 46), word, font=f, fill=(255, 140, 20) if i != 1 else (170, 255, 60))
save(im, "boosign")

# "SPOOKY FAIR" entrance arch sign
W, H = 1024, 256
im = Image.new("RGB", (W, H), (30, 8, 50)); d = ImageDraw.Draw(im)
d.rounded_rectangle([8, 8, W - 9, H - 9], 40, outline=(255, 210, 60), width=12)
for x in range(40, W - 30, 52):
    for y in (30, H - 30): d.ellipse([x - 10, y - 10, x + 10, y + 10], fill=(255, 240, 160))
f = font(130); word = "SPOOKY FAIR"; w = d.textlength(word, font=f)
d.text(((W - w) / 2 + 6, 56 + 6), word, font=f, fill=(0, 0, 0))
d.text(((W - w) / 2, 56), word, font=f, fill=(255, 130, 20))
save(im, "fairsign")

# castle banner: purple with an orange jack-o'-lantern face
W, H = 256, 512
im = Image.new("RGB", (W, H), (90, 30, 150)); d = ImageDraw.Draw(im)
d.rectangle([0, 0, W - 1, H - 1], outline=(255, 200, 60), width=10)
d.ellipse([38, 150, 218, 310], fill=(255, 130, 20))
d.rectangle([118, 128, 138, 156], fill=(60, 140, 40))
d.polygon([(80, 205), (110, 205), (95, 180)], fill=(60, 10, 40))
d.polygon([(146, 205), (176, 205), (161, 180)], fill=(60, 10, 40))
d.polygon([(70, 240), (186, 240), (170, 275), (150, 260), (128, 280), (106, 260), (86, 275)], fill=(60, 10, 40))
save(im, "banner")

# ghost-train carriage side: purple panel, yellow trim, painted eyes
W, H = 512, 256
im = Image.new("RGB", (W, H), (110, 30, 170)); d = ImageDraw.Draw(im)
d.rectangle([0, 0, W - 1, H - 1], outline=(255, 210, 40), width=14)
d.rectangle([0, H // 2 - 6, W, H // 2 + 6], fill=(255, 130, 20))
for x in (150, 360):
    d.ellipse([x - 46, 60, x + 46, 160], fill=(255, 255, 255))
    d.ellipse([x - 18, 96, x + 18, 140], fill=(20, 0, 30))
save(im, "ghosttrain")
print("ok", sorted(os.listdir(O)))
