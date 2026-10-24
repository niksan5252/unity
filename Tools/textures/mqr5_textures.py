"""16x16 pixel textures for Move Quest Run #5 (Minecraft Quest 2): cherry grove, warm ocean, crimson nether,
plus the faces of the new mobs (drawn 8x8, doubled like the others). Our own drawings, no game assets.
usage: python -I Tools/textures/mqr5_textures.py   (writes Assets/Textures/Pixel)"""
import os, random
from PIL import Image
random.seed(505)
O = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Textures", "Pixel") + os.sep
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
def log(base, line, every=4):
    im = Image.new("RGB", (N, N))
    for y in range(N):
        for x in range(N): im.putpixel((x, y), line if x % every == 0 or random.random() < 0.07 else jitter(base, 6))
    return im
def planks(base, dark):
    im = Image.new("RGB", (N, N))
    for y in range(N):
        for x in range(N):
            c = jitter(base, 6)
            if y % 4 == 3 or (x + (y // 4) * 5) % 16 == 0: c = dark
            im.putpixel((x, y), c)
    return im
def bricks(base, mortar, h=4, w=8):
    im = Image.new("RGB", (N, N))
    for y in range(N):
        for x in range(N):
            c = jitter(base, 7)
            if y % h == h - 1 or (x + (w // 2 if (y // h) % 2 else 0)) % w == w - 1: c = mortar
            im.putpixel((x, y), c)
    return im
def lamp(base, light, frame):
    im = Image.new("RGB", (N, N))
    for y in range(N):
        for x in range(N):
            edge = x in (0, 15) or y in (0, 15)
            cross = x in (7, 8) or y in (7, 8)
            im.putpixel((x, y), jitter(frame, 6) if edge else jitter(light, 10) if cross else jitter(base, 10))
    return im
def coral(base, hi):
    im = noise(base, 14, [hi], 0.25)
    for _ in range(14): im.putpixel((random.randint(0, 15), random.randint(0, 15)), jitter(tuple(min(255, c + 40) for c in hi), 6))
    return im

# ---- cherry grove ----
save(log((58, 30, 40), (40, 20, 28)), "cherry_log")
save(noise((246, 168, 205), 12, [(255, 196, 225), (226, 128, 180), (255, 225, 240)], 0.45), "cherry_leaves")
save(planks((236, 186, 182), (196, 128, 130)), "cherry_planks")
gp = Image.open(O + "grass_top.png").convert("RGB")
for _ in range(26): gp.putpixel((random.randint(0, 15), random.randint(0, 15)), jitter((255, 170, 210), 12))
save(gp, "grass_petals")
save(noise((150, 112, 66), 8, [(170, 130, 80), (124, 92, 52)], 0.3), "dirt_path")
hay = Image.new("RGB", (N, N))
for y in range(N):
    for x in range(N): hay.putpixel((x, y), (160, 50, 30) if y in (3, 12) else jitter((222, 178, 52), 14) if x % 3 else jitter((190, 150, 40), 10))
save(hay, "hay")
bn = planks((214, 168, 70), (170, 120, 40))
for (x, y) in [(5, 6), (6, 6), (9, 9), (10, 9), (5, 11), (11, 5)]: bn.putpixel((x, y), (60, 40, 20))
save(bn, "bee_nest")
save(bricks((196, 98, 70), (150, 70, 50)), "terracotta")
# ---- warm ocean ----
save(noise((99, 171, 158), 14, [(80, 140, 130), (130, 200, 185), (70, 120, 115)], 0.4), "prismarine")
save(bricks((99, 190, 170), (60, 130, 115), 8, 8), "prismarine_bricks")
save(bricks((52, 92, 76), (30, 60, 50), 8, 8), "dark_prismarine")
save(lamp((170, 215, 205), (240, 255, 250), (120, 170, 160)), "sea_lantern")
save(coral((40, 80, 230), (90, 140, 255)), "coral_blue")
save(coral((230, 90, 170), (255, 150, 210)), "coral_pink")
save(coral((160, 50, 200), (210, 110, 240)), "coral_purple")
save(coral((210, 40, 40), (255, 110, 80)), "coral_red")
save(coral((230, 210, 50), (255, 240, 120)), "coral_yellow")
save(noise((40, 190, 215), 10, [(80, 220, 240), (30, 160, 200), (200, 250, 255)], 0.22), "sea_water")
save(noise((60, 130, 45), 14, [(40, 100, 30), (90, 170, 60)], 0.4), "kelp")
save(noise((232, 220, 170), 10, [(214, 200, 150), (245, 235, 195)], 0.35), "sand_light")
# ---- crimson / warped nether ----
save(log((140, 40, 70), (100, 25, 50)), "crimson_stem")
cn = noise((110, 25, 30), 12, [(150, 40, 45), (80, 18, 22)], 0.4); save(cn, "crimson_nylium")
save(noise((140, 20, 20), 14, [(170, 40, 30), (110, 12, 12)], 0.45), "nether_wart")
save(log((60, 110, 120), (40, 80, 90)), "warped_stem")
save(noise((22, 125, 120), 14, [(40, 170, 160), (15, 95, 95)], 0.45), "warped_wart")
save(noise((40, 110, 100), 12, [(30, 150, 130), (25, 80, 75)], 0.4), "warped_nylium")
save(lamp((255, 160, 70), (255, 230, 150), (220, 110, 40)), "shroomlight")
save(noise((42, 38, 46), 8, [(30, 28, 34), (58, 52, 62)], 0.35), "blackstone")
gb = noise((42, 38, 46), 8, [(30, 28, 34)], 0.3)
for _ in range(30): gb.putpixel((random.randint(0, 15), random.randint(0, 15)), jitter((240, 190, 40), 15))
save(gb, "gilded_blackstone")
gold = Image.new("RGB", (N, N))
for y in range(N):
    for x in range(N): gold.putpixel((x, y), (255, 245, 150) if (x + y) % 9 == 0 else (200, 140, 20) if x in (0, 15) or y in (0, 15) else jitter((248, 205, 50), 8))
save(gold, "gold_block")
save(noise((120, 40, 10), 14, [(255, 120, 20), (200, 70, 10), (60, 20, 10)], 0.4), "magma")

# ---- faces: 8x8, a letter per pixel, doubled to 16x16 ----
def face(rows, pal, name):
    im = Image.new("RGB", (8, 8))
    for y, row in enumerate(rows):
        for x, ch in enumerate(row): im.putpixel((x, y), jitter(pal[ch], 5) if ch in "gGsSwW" else pal[ch])
    im.resize((16, 16), Image.NEAREST).save(O + name + ".png")

face(["gGgggGgg", "gggGgggg", "gkkggkkg", "gkkggkkg", "gggkkggg", "ggkkkkgg", "ggkggkgg", "gggGgggg"],
     {"g": (90, 190, 70), "G": (60, 150, 50), "k": (15, 20, 15)}, "face_creeper")
face(["ssssssss", "ssssssss", "bbbbbbbs", "swgsgwss", "sssnnsss", "sssnnsss", "ssssssss", "ssssssss"],
     {"s": (190, 140, 105), "b": (70, 45, 30), "w": (245, 245, 245), "g": (40, 150, 60), "n": (165, 115, 85)}, "face_villager")
face(["ssssssss", "ssssssss", "skwsswks", "ssssssss", "sspnnpss", "sspnnpss", "sTssssTs", "ssssssss"],
     {"s": (230, 150, 140), "k": (30, 20, 20), "w": (250, 250, 250), "p": (200, 110, 100), "n": (110, 60, 55), "T": (250, 240, 210)}, "face_piglin")
face(["gGgggggg", "gggggGgg", "gccggccg", "gccggccg", "gggggggg", "ggkkkkgg", "gggggggg", "gGgggggg"],
     {"g": (70, 140, 140), "G": (50, 110, 110), "c": (100, 255, 240), "k": (30, 70, 70)}, "face_drowned")
face(["ssssssss", "ssssssss", "skkssskk", "skkssskk", "ssssssss", "skkkkkks", "ssssssss", "ssssssss"],
     {"s": (45, 45, 50), "k": (10, 10, 12)}, "face_wither")
face(["yyyyyyyy", "yyyyyyyy", "ykkyykky", "ywkyywky", "yyyyyyyy", "bbbbbbbb", "yyyyyyyy", "bbbbbbbb"],
     {"y": (245, 200, 50), "k": (25, 20, 20), "w": (250, 250, 250), "b": (60, 40, 25)}, "face_bee")
face(["wwwwwwww", "wwwwwwww", "wkkwwkkw", "wrwwwwrw", "wwwwwwww", "wwkkkkww", "wwkkkkww", "wwwwwwww"],
     {"w": (240, 240, 240), "k": (60, 60, 65), "r": (200, 70, 70)}, "face_ghast")
face(["bbbbbbbb", "bbbbbbbb", "bkbbbbkb", "bbbbbbbb", "bbbbbbbb", "bbbbbbbb", "bnnbbnnb", "bbbbbbbb"],
     {"b": (150, 100, 60), "k": (20, 15, 10), "n": (90, 55, 30)}, "face_horse")
face(["gggggggg", "gggggggg", "gkgggkgg", "gggggggg", "gggggggg", "ggyyyygg", "gggggggg", "gggggggg"],
     {"g": (110, 175, 90), "k": (20, 25, 20), "y": (230, 210, 120)}, "face_turtle")
face(["mmmmmmmm", "mmmmmmmm", "moooooom", "moyooyom", "moooooom", "mmmmmmmm", "mmmmmmmm", "mmmmmmmm"],
     {"m": (70, 20, 20), "o": (230, 80, 20), "y": (255, 230, 80)}, "face_magma")
face(["rrrrrrrr", "rRrrrRrr", "rrrrrrrr", "rkkrrkkr", "rrrrrrrr", "rrrrrrrr", "rRrrrrRr", "rrrrrrrr"],
     {"r": (170, 50, 55), "R": (130, 35, 40), "k": (25, 15, 15)}, "face_strider")
face(["ffffffff", "ffffffff", "fkffffkf", "fwfffwff", "ffffffff", "ffffffff", "ffffffff", "ffffffff"],
     {"f": (90, 150, 210), "k": (15, 15, 25), "w": (250, 250, 250)}, "face_dolphin")
print("ok")
