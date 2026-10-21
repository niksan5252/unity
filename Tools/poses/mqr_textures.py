"""Extra 16x16 pixel textures for Move Quest Run worlds (snowy taiga, jungle temple, End) + Mirror card silhouette."""
from PIL import Image, ImageFilter
import random
random.seed(808)
O = "C:/Users/PC/SubwayRun/Assets/Textures/Pixel/"
N = 16

def jitter(c, v): return tuple(max(0, min(255, c[i] + random.randint(-v, v))) for i in range(3))
def noise(base, v, extra=None, p=0.0):
    im = Image.new("RGB", (N, N))
    for y in range(N):
        for x in range(N):
            c = jitter(base, v)
            if extra and random.random() < p: c = jitter(random.choice(extra), 8)
            im.putpixel((x, y), c)
    return im
def save(im, n): im.save(O + n + ".png")

snow = noise((240, 248, 255), 6, [(220, 232, 245)], 0.3); save(snow, "snow")
dirt = Image.open(O + "dirt.png").convert("RGB")
side = dirt.copy()
for x in range(N):
    h = 3 + random.randint(0, 2)
    for y in range(h): side.putpixel((x, y), jitter((240, 248, 255), 6))
save(side, "snow_side")
save(noise((150, 195, 250), 10, [(175, 215, 255), (130, 175, 240)], 0.35), "ice")
sl = Image.new("RGB", (N, N))
for y in range(N):
    for x in range(N): sl.putpixel((x, y), (58, 40, 26) if x % 4 == 0 or random.random() < 0.08 else jitter((82, 58, 36), 6))
save(sl, "spruce_log")
save(noise((48, 92, 52), 12, [(30, 66, 36), (66, 112, 70)], 0.4), "spruce_leaves")
jl = Image.new("RGB", (N, N))
for y in range(N):
    for x in range(N): jl.putpixel((x, y), (70, 58, 30) if (x + y // 3) % 5 == 0 else jitter((96, 78, 40), 8))
save(jl, "jungle_log")
save(noise((48, 150, 30), 18, [(30, 115, 20), (90, 190, 45)], 0.45), "jungle_leaves")
save(noise((70, 175, 40), 16, [(50, 140, 30), (110, 200, 60)], 0.4), "jungle_grass")
mc = Image.open(O + "cobblestone.png").convert("RGB")
for _ in range(60): mc.putpixel((random.randint(0, 15), random.randint(0, 15)), jitter((80, 120, 50), 12))
save(mc, "mossy_cobble")
cs = Image.new("RGB", (N, N))
for y in range(N):
    for x in range(N):
        c = jitter((150, 150, 140), 8)
        if y % 8 == 7 or (x + (8 if (y // 8) % 2 else 0)) % 16 == 15: c = (95, 95, 90)
        cs.putpixel((x, y), c)
save(cs, "stone_bricks")
save(noise((222, 222, 160), 10, [(200, 200, 140), (235, 235, 180)], 0.4), "end_stone")
pp = Image.new("RGB", (N, N))
for y in range(N):
    for x in range(N):
        c = jitter((170, 120, 170), 6)
        if x % 8 == 0 or y % 8 == 0: c = (140, 95, 140)
        pp.putpixel((x, y), c)
save(pp, "purpur")
save(noise((130, 190, 40), 10, [(110, 165, 30), (160, 210, 60)], 0.3), "melon")

# white silhouette with gold glow for the MIRROR button on the level card
P = "C:/Users/PC/SubwayRun/Assets/Resources/"
star = Image.open(P + "Poses/star.png").convert("RGBA").resize((400, 400), Image.LANCZOS)
a = star.split()[3]
glow = a.filter(ImageFilter.MaxFilter(13)).filter(ImageFilter.GaussianBlur(6))
out = Image.new("RGBA", (400, 400), (0, 0, 0, 0))
out.paste((255, 200, 60, 255), (0, 0), glow.point(lambda v: int(v * 0.9)))
out.paste((255, 255, 255, 255), (0, 0), a)
out.save(P + "Silhouettes/Mirror.png")
print("ok")
