from PIL import Image
import os

base = r"D:\GameProjects\SevenPages\Assets\Resources\Art\Cards"
names = ["strike", "ink_splash", "page_ripper", "sevenfold", "mend",
         "stillness", "whisper", "ink_ward", "backflow", "karma", "card_back"]
labels = ["MO CI", "NONG MO JIAN", "SI YE SHOU", "QI YE ZHONG ZHANG", "XU MING",
          "JING XIN", "LUN HUI DI YU", "MO BI", "NI MO HEN", "YE ZHANG", "CARD BACK"]

tw, th, cols = 320, 427, 4
rows = (len(names) + cols - 1) // cols
sheet = Image.new("RGB", (cols * tw, rows * th), (14, 14, 17))

for i, key in enumerate(names):
    p = os.path.join(base, key + ".png")
    if not os.path.exists(p):
        continue
    im = Image.open(p).convert("RGB")
    im.thumbnail((tw - 8, th - 8), Image.LANCZOS)
    x = (i % cols) * tw + (tw - im.width) // 2
    y = (i // cols) * th + (th - im.height) // 2
    sheet.paste(im, (x, y))

out = r"C:\Users\Administrator\Desktop\七页轮回-卡牌\00_全部卡面.png"
sheet.save(out)
print("saved", out, sheet.size)
