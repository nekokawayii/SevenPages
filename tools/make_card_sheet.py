from PIL import Image
import os

base = r"D:\GameProjects\SevenPages\Assets\Resources\Art\Cards"
names = [
    "strike", "ink_splash", "page_ripper", "sevenfold", "mend",
    "stillness", "whisper", "ink_ward", "backflow", "karma",
    "sever", "turn_page", "devour", "afterglow", "thousand_pages",
    "ink_cage", "reincarnation_seal", "dry_ink", "karmic_debt", "karma_bind",
    "card_back",
]

tw, th, cols = 300, 400, 5
rows = (len(names) + cols - 1) // cols
sheet = Image.new("RGB", (cols * tw, rows * th), (14, 14, 17))

placed = 0
for i, key in enumerate(names):
    p = os.path.join(base, key + ".png")
    if not os.path.exists(p):
        continue
    im = Image.open(p).convert("RGB")
    im.thumbnail((tw - 8, th - 8), Image.LANCZOS)
    x = (i % cols) * tw + (tw - im.width) // 2
    y = (i // cols) * th + (th - im.height) // 2
    sheet.paste(im, (x, y))
    placed += 1

out = r"C:\Users\Administrator\Desktop\七页轮回-卡牌\00_全部卡面.png"
sheet.save(out)
print("saved", out, sheet.size, "cards:", placed)
