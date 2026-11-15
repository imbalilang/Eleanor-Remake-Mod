import json, os, re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FINAL = os.path.join(ROOT, "tools", "NovelWorkFlow_Eleanor", "Novel_Output", "volume_harem_D_Chapter_2_Final.txt")
BRANCH = os.path.join(ROOT, "tools", "NovelWorkFlow_Eleanor", "Novel_Output", "volume_harem_D_Chapter_2_Branches.txt")
DUAL = os.path.join(ROOT, "Content", "dual_romance_v04.json")
UI = os.path.join(ROOT, "Content", "dual_ui.json")

SPEAKERS = {"莉迪亚": "Lydia", "店主": "Player", "黑市打手": "Thug", "旁白": "Narrator", "埃莉诺": "SEC"}


def parse_lines(raw_lines):
    out, pending = [], {}
    for raw in raw_lines:
        line = raw.strip()
        if not line:
            continue
        if line.startswith(("[时间|", "[章|", "[选择|", "[选项|", "[汇合|", "[结果|",
                            "[选项后续|", "[/选项后续]")):
            continue
        if line == "[/选择]":
            continue
        m = re.match(r"^\[([^|\]]+)\|表情\|([^\]]+)\]$", line)
        if m:
            pending[m.group(1)] = m.group(2)
            continue
        m = re.match(r"^\[([^|\]]+)\|对白\|(.*)\]$", line)
        if m:
            name, text = m.group(1), m.group(2)
            sp = SPEAKERS.get(name)
            if sp is None:
                raise ValueError("unknown speaker " + name)
            row = {"speaker": sp, "text": text, "en": ""}
            if name in pending:
                row["expr"] = pending.pop(name)
            out.append(row)
            continue
        m = re.match(r"^\[旁白\|(.*)\]$", line)
        if m:
            out.append({"speaker": "Narrator", "text": m.group(1), "en": ""})
            continue
        m = re.match(r"^\[通告\|([^|\]]+)\|(.*)\]$", line)
        if m:
            out.append({"speaker": "Narrator", "text": m.group(1) + "：" + m.group(2), "en": ""})
            continue
        raise ValueError("unparsed line " + line)
    return out


final = open(FINAL, encoding="utf-8").read().splitlines()


def find(prefix, start=0):
    for i in range(start, len(final)):
        if final[i].strip().startswith(prefix):
            return i
    raise ValueError("missing " + prefix)


def close_after(i):
    for j in range(i, len(final)):
        if final[j].strip() == "[/选择]":
            return j
    raise ValueError("missing close")


c1 = find("[选择|C1|")
c1c = close_after(c1)
grab = find("[旁白|他的手落下去，扣住那只戴镯子的腕子。]")
slam = find("[旁白|门被摔上，玻璃抖了两下才停。]")
time55 = find("[时间|第55天]")
ch2 = find("[章|六个人]")
c2 = find("[选择|C2|")
c2c = close_after(c2)
ch4 = find("[章|门还歪着]")

branch_raw = open(BRANCH, encoding="utf-8").read()
branches = {}
for m in re.finditer(r"\[选项后续\|([^|\]]+)\|(\d+)\](.*?)\[/选项后续\]", branch_raw, re.DOTALL):
    branches[(m.group(1), m.group(2))] = m.group(3).splitlines()


def block(bid, label_zh, label_en, lines, presentation=None):
    b = {"id": bid, "label_zh": label_zh, "label_en": label_en, "lines": parse_lines(lines)}
    if presentation:
        b["presentation"] = presentation
    return b


d02_blocks = [
    block("d02_b001", "空手来的莉迪亚", "Lydia Comes Empty-Handed", final[:c1]),
    block("d02_b002", "站到两人中间", "Step Between Them", final[c1c + 1:grab]),
    block("d02_b003", "问他要卖什么", "Ask What He Is Selling", branches[("C1", "2")]),
    block("d02_b004", "拉开门请他出去", "Open the Door", branches[("C1", "3")]),
    block("d02_b005", "手扣住了镯子", "The Hand Closes on the Bracelet", final[grab:slam + 1]),
    block("d02_b006", "她走出去", "She Walks Out", final[slam + 1:time55]),
]
d02_choices = [
    {"id": "d02_c1_1", "label_zh": "站到两人中间", "label_en": "Step between them",
     "description_zh": "挡在莉迪亚和打手之间。", "description_en": "Put yourself between Lydia and the thug."},
    {"id": "d02_c1_2", "label_zh": "问他要卖什么", "label_en": "Ask what he is selling",
     "description_zh": "把话拉回买卖。", "description_en": "Turn it back into a transaction."},
    {"id": "d02_c1_3", "label_zh": "拉开门请他出去", "label_en": "Open the door and ask him out",
     "description_zh": "把巷子的光放进来。", "description_en": "Let the alley light in."},
]
d02 = {"id": "d02", "title_zh": "空手来的人", "title_en": "The One Who Came Empty-Handed",
       "blocks": d02_blocks, "choices": d02_choices}

d03_blocks = [
    block("d03_b001", "六个人", "Six Men", final[ch2 + 1:c2],
          {"mode": "immediate", "backup1": {"sprite": "eleanor1", "name": "埃莉诺"}}),
    block("d03_b002", "让她去报案", "Let Her Report It", branches[("C2", "1")]),
    block("d03_b003", "先把货捡起来", "Pick Up the Stock First", final[c2c + 1:ch4]),
    block("d03_b004", "叫她收起镯子", "Tell Her to Hide the Bracelet", branches[("C2", "3")]),
    block("d03_b005", "门还歪着", "The Door Still Leans", final[ch4 + 1:]),
]
d03_choices = [
    {"id": "d03_c2_1", "label_zh": "让她去报案", "label_en": "Let her report it",
     "description_zh": "让她去治安部。", "description_en": "Let her go to Security."},
    {"id": "d03_c2_2", "label_zh": "先把货捡起来", "label_en": "Pick up the stock first",
     "description_zh": "先把能捡的捡回来。", "description_en": "Salvage what can be salvaged."},
    {"id": "d03_c2_3", "label_zh": "叫她收起镯子", "label_en": "Tell her to hide the bracelet",
     "description_zh": "别再让人看见。", "description_en": "Stop letting it show."},
]
d03 = {"id": "d03", "title_zh": "六个人", "title_en": "Six Men",
       "blocks": d03_blocks, "choices": d03_choices}

d = json.load(open(DUAL, encoding="utf-8"))
d["scenes"] = [s for s in d["scenes"] if s.get("id") not in ("d02", "d03")]
d["scenes"].insert(1, d02)
d["scenes"].insert(2, d03)
json.dump(d, open(DUAL, "w", encoding="utf-8"), ensure_ascii=False, indent=2)

ui = json.load(open(UI, encoding="utf-8"))
ui["er_d02_c1_prompt"] = ["他的手搭在柜沿，人还站着没动。", "His hand rests on the counter's edge. He has not moved."]
ui["er_d03_c2_prompt"] = ["纸上两行字摊在柜面，两个人都在看他。", "Two lines of handwriting lie on the counter. Both women are looking at him."]
ui["er_d_act2a_done"] = ["D线第二幕（上）已完成；几天后继续。", "D Act 2, part one, is complete. The rest comes in a few days."]
ui["er_d_act2_done"] = ["D线第二幕已完成；第三幕「双螺旋」接入中。", "D Act 2 is complete. Act 3, Double Helix, is being connected."]
json.dump(ui, open(UI, "w", encoding="utf-8"), ensure_ascii=False, indent=2)

items = []
for scene in (d02, d03):
    for b in scene["blocks"]:
        for i, ln in enumerate(b["lines"], 1):
            items.append({"key": f"er_dual_{b['id']}_{i:02d}", "speaker": ln["speaker"], "text": ln["text"]})
json.dump(items, open(os.path.join(ROOT, "tmp_d2_en_items.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=2)

for scene in (d02, d03):
    print(scene["id"], [(b["id"], len(b["lines"])) for b in scene["blocks"]],
          "choices", [c["id"] for c in scene["choices"]])
print("total lines", sum(len(b["lines"]) for s in (d02, d03) for b in s["blocks"]))
print("translation items", len(items))
