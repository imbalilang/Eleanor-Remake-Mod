import json, os, re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FINAL = os.path.join(ROOT, "tools", "NovelWorkFlow_Eleanor", "Novel_Output", "volume_harem_D_Chapter_3_Final.txt")
BRANCH = os.path.join(ROOT, "tools", "NovelWorkFlow_Eleanor", "Novel_Output", "volume_harem_D_Chapter_3_Branches.txt")
DUAL = os.path.join(ROOT, "Content", "dual_romance_v04.json")
UI = os.path.join(ROOT, "Content", "dual_ui.json")

SPEAKERS = {"莉迪亚": "Lydia", "店主": "Player", "旁白": "Narrator", "埃莉诺": "Eleanor"}


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
kiss = find("[旁白|她往前一步，踮脚在他脸侧碰了一下，很快退开。]")
day61 = find("[时间|第61天]")
c2 = find("[选择|C2|")
c2c = close_after(c2)
deal = find("[莉迪亚|对白|一天不开门，你这店少进多少，我补多少。]")
day62 = find("[时间|第62天]")
day63 = find("[时间|第63天]")

branch_raw = open(BRANCH, encoding="utf-8").read()
branches = {}
for m in re.finditer(r"\[选项后续\|([^|\]]+)\|(\d+)\](.*?)\[/选项后续\]", branch_raw, re.DOTALL):
    branches[(m.group(1), m.group(2))] = m.group(3).splitlines()


def block(bid, label_zh, label_en, lines):
    return {"id": bid, "label_zh": label_zh, "label_en": label_en, "lines": parse_lines(lines)}


d04_blocks = [
    block("d04_b001", "门轴", "The Door Hinge", final[:c1]),
    block("d04_b002", "放下活站起来", "Put It Down and Stand", final[c1c + 1:kiss]),
    block("d04_b003", "叫她名字", "Say Her Name", branches[("C1", "2")]),
    block("d04_b004", "先递杯水", "Offer Her Water First", branches[("C1", "3")]),
    block("d04_b005", "她跑开了", "She Runs", final[kiss:day61]),
]
d04_choices = [
    {"id": "d04_c1_1", "label_zh": "放下活站起来", "label_en": "Put the work down and stand",
     "description_zh": "站起来，让她把话说完。", "description_en": "Stand up and let her finish."},
    {"id": "d04_c1_2", "label_zh": "叫她名字", "label_en": "Say her name",
     "description_zh": "叫住她。", "description_en": "Stop her with her name."},
    {"id": "d04_c1_3", "label_zh": "先递杯水", "label_en": "Offer her water first",
     "description_zh": "先让她缓一下。", "description_en": "Give her a moment."},
]
d04 = {"id": "d04", "title_zh": "门轴", "title_en": "The Door Hinge",
       "blocks": d04_blocks, "choices": d04_choices}

d05_blocks = [
    block("d05_b001", "一个数", "One Number", final[day61:c2]),
    block("d05_b002", "说不卖人", "Not for Sale", final[c2c + 1:deal]),
    block("d05_b003", "问她真想要什么", "Ask What She Wants", branches[("C2", "2")]),
    block("d05_b004", "指价签让她挑", "Point at the Tags", branches[("C2", "3")]),
    block("d05_b005", "两千", "Two Thousand", final[deal:day62]),
]
d05_choices = [
    {"id": "d05_c2_1", "label_zh": "说不卖人", "label_en": "Say you do not sell people",
     "description_zh": "把“买人”挡回去。", "description_en": "Refuse to be bought."},
    {"id": "d05_c2_2", "label_zh": "问她真想要什么", "label_en": "Ask what she really wants",
     "description_zh": "让她直说。", "description_en": "Make her say it plainly."},
    {"id": "d05_c2_3", "label_zh": "指价签让她挑", "label_en": "Point at the price tags",
     "description_zh": "用货架拒绝她。", "description_en": "Refuse her with the shelf."},
]
d05 = {"id": "d05", "title_zh": "一个数", "title_en": "One Number",
       "blocks": d05_blocks, "choices": d05_choices}

d06_blocks = [block("d06_b001", "门开了", "The Door Opens", final[day62:day63])]
d06 = {"id": "d06", "title_zh": "门开了", "title_en": "The Door Opens",
       "blocks": d06_blocks, "choices": []}

d07_blocks = [block("d07_b001", "她先来过", "She Came First", final[day63:])]
d07 = {"id": "d07", "title_zh": "她先来过", "title_en": "She Came First",
       "blocks": d07_blocks, "choices": []}

d = json.load(open(DUAL, encoding="utf-8"))
d["scenes"] = [s for s in d["scenes"] if s.get("id") not in ("d04", "d05", "d06", "d07")]
insert_at = next(i for i, s in enumerate(d["scenes"]) if s["id"] == "d03") + 1
for scene in (d04, d05, d06, d07):
    d["scenes"].insert(insert_at, scene)
    insert_at += 1
json.dump(d, open(DUAL, "w", encoding="utf-8"), ensure_ascii=False, indent=2)

ui = json.load(open(UI, encoding="utf-8"))
ui["er_d04_c1_prompt"] = ["她捏着布袋口，话说完了，人还没走。", "She is holding the bag closed. She has said it, and she has not left."]
ui["er_d05_c2_prompt"] = ["她说完那句话，自己先把手缩了回去。", "She has pulled her hand back before he can answer."]
ui["er_d_act3_done"] = ["D线第三幕已完成；第四幕「结局」接入中。", "D Act 3 is complete. Act 4, the ending, is being connected."]
json.dump(ui, open(UI, "w", encoding="utf-8"), ensure_ascii=False, indent=2)

items = []
for scene in (d04, d05, d06, d07):
    for b in scene["blocks"]:
        for i, ln in enumerate(b["lines"], 1):
            items.append({"key": f"er_dual_{b['id']}_{i:02d}", "speaker": ln["speaker"], "text": ln["text"]})
json.dump(items, open(os.path.join(ROOT, "tmp_d3_en_items.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=2)

for scene in (d04, d05, d06, d07):
    print(scene["id"], [(b["id"], len(b["lines"])) for b in scene["blocks"]],
          "choices", [c["id"] for c in scene["choices"]])
print("total lines", sum(len(b["lines"]) for s in (d04, d05, d06, d07) for b in s["blocks"]))
print("translation items", len(items))
