import json, re, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FINAL = os.path.join(ROOT, "tools", "NovelWorkFlow_Eleanor", "Novel_Output", "volume_harem_D_Chapter_1_Final.txt")
BRANCH = os.path.join(ROOT, "tools", "NovelWorkFlow_Eleanor", "Novel_Output", "volume_harem_D_Chapter_1_Branches.txt")
DUAL = os.path.join(ROOT, "Content", "dual_romance_v04.json")
UI = os.path.join(ROOT, "Content", "dual_ui.json")

SPEAKERS = {"莉迪亚": "Lydia", "埃莉诺": "Eleanor", "店主": "Player", "旁白": "Narrator", "SEC": "SEC"}

def parse_lines(raw_lines):
    out, pending = [], {}
    for raw in raw_lines:
        line = raw.strip()
        if not line: continue
        if line.startswith(("[时间|", "[章|", "[选择|", "[选项|", "[表演|", "[汇合|", "[结果|", "[选项后续|", "[/选项后续]")):
            continue
        if line == "[/选择]": continue
        m = re.match(r"^\[([^|\]]+)\|表情\|([^\]]+)\]$", line)
        if m:
            pending[m.group(1)] = m.group(2); continue
        m = re.match(r"^\[([^|\]]+)\|对白\|(.*)\]$", line)
        if m:
            name, text = m.group(1), m.group(2)
            sp = SPEAKERS.get(name)
            if sp is None: raise ValueError("unknown speaker " + name)
            row = {"speaker": sp, "text": text, "en": ""}
            if name in pending: row["expr"] = pending.pop(name)
            out.append(row); continue
        m = re.match(r"^\[旁白\|(.*)\]$", line)
        if m:
            out.append({"speaker": "Narrator", "text": m.group(1), "en": ""}); continue
        raise ValueError("unparsed line " + line)
    return out

final = open(FINAL, encoding="utf-8").read().splitlines()
def find(prefix, start=0):
    for i in range(start, len(final)):
        if final[i].startswith(prefix): return i
    raise ValueError("missing " + prefix)
def close_after(i):
    for j in range(i, len(final)):
        if final[j].strip() == "[/选择]": return j
    raise ValueError("missing close")
c1 = find("[选择|C1|"); c1c = close_after(c1)
c2 = find("[选择|C2|"); c2c = close_after(c2)
ch1 = find("[章|她把人带来了]"); ch2 = find("[章|表盖上有灰]")
ch3 = find("[章|架子最下层]")
common = find("[莉迪亚|对白|灯的事我自己去问。]")
branch_raw = open(BRANCH, encoding="utf-8").read()
branch = {}
for m in re.finditer(r"\[选项后续\|([^|\]]+)\|(\d+)\](.*?)\[\/选项后续\]", branch_raw, re.DOTALL):
    branch[(m.group(1), m.group(2))] = m.group(3).splitlines()

def block(bid, scene, lines):
    return {"id": bid, "label_zh": scene, "label_en": scene, "lines": parse_lines(lines)}

blocks = [
    block("d01_b001", "她把人带来了", final[ch1+1:ch2]),
    block("d01_b002", "表盖上有灰", final[ch2+1:c1]),
    block("d01_b003", "把表推给她看", final[c1c+1:ch3]),
    block("d01_b004", "说这表不卖", branch[("C1","2")]),
    block("d01_b005", "收回柜台里", branch[("C1","3")]),
    block("d01_b006", "架子最下层", final[ch3+1:c2]),
    block("d01_b007", "说别送东西", final[c2c+1:common]),
    block("d01_b008", "让她列完", branch[("C2","1")]),
    block("d01_b009", "把本子拿过来", branch[("C2","3")]),
    block("d01_b010", "灯的事", final[common:]),
]
blocks[0]["presentation"] = {"mode":"immediate","backup1":{"sprite":"eleanor1","name":"埃莉诺"}}

choices = [
    {"id":"d01_c1_1","label_zh":"把表推给她看","label_en":"Push the watch toward her","description_zh":"让她拿起来看。","description_en":"Let her pick it up."},
    {"id":"d01_c1_2","label_zh":"说这表不卖","label_en":"Say it's not for sale","description_zh":"不卖，但让她看。","description_en":"Not for sale, but let her look."},
    {"id":"d01_c1_3","label_zh":"收回柜台里","label_en":"Put it back under the counter","description_zh":"把表收回抽屉。","description_en":"Put the watch back in the drawer."},
    {"id":"d01_c2_1","label_zh":"让她列完","label_en":"Let her finish the list","description_zh":"让她把清单写完。","description_en":"Let her finish writing the list."},
    {"id":"d01_c2_2","label_zh":"说别送东西","label_en":"Tell her not to send anything","description_zh":"拦下这份安排。","description_en":"Stop the arrangement."},
    {"id":"d01_c2_3","label_zh":"把本子拿过来","label_en":"Take the notebook","description_zh":"把本子拿过来看。","description_en":"Take the notebook and read it."},
]
scene = {"id":"d01","title_zh":"窗口","title_en":"The Window","blocks":blocks,"choices":choices}

d = json.load(open(DUAL, encoding="utf-8"))
d["scenes"] = [s for s in d["scenes"] if s.get("id") != "d01"]
d["scenes"].insert(0, scene)
json.dump(d, open(DUAL, "w", encoding="utf-8"), ensure_ascii=False, indent=2)

ui = json.load(open(UI, encoding="utf-8"))
ui["er_d01_c1_prompt"] = ["她的手悬在表上方，没落下去。", "Her hand hovers over the watch without touching it."]
ui["er_d01_c2_prompt"] = ["笔尖悬在纸上。", "The pen hovers over the paper."]
json.dump(ui, open(UI, "w", encoding="utf-8"), ensure_ascii=False, indent=2)

print("blocks", [(b["id"], len(b["lines"])) for b in blocks])
print("choices", len(choices), "total lines", sum(len(b["lines"]) for b in blocks))
# write translation source
items=[]
for b in blocks:
    for i,ln in enumerate(b["lines"],1):
        items.append({"key":f"er_dual_{b['id']}_{i:02d}","speaker":ln["speaker"],"text":ln["text"]})
json.dump(items, open(os.path.join(ROOT,"tmp_d1_en_items.json"),"w",encoding="utf-8"), ensure_ascii=False, indent=2)
print("translation items", len(items))
