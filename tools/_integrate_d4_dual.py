import json, os, re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FINAL = os.path.join(ROOT, "tools", "NovelWorkFlow_Eleanor", "Novel_Output", "volume_harem_D_Chapter_4_Final.txt")
BRANCH = os.path.join(ROOT, "tools", "NovelWorkFlow_Eleanor", "Novel_Output", "volume_harem_D_Chapter_4_Branches.txt")
DUAL = os.path.join(ROOT, "Content", "dual_romance_v04.json")
UI = os.path.join(ROOT, "Content", "dual_ui.json")

SPEAKERS = {"莉迪亚": "Lydia", "店主": "Player", "旁白": "Narrator", "SEC": "SEC"}


def parse_lines(raw_lines):
    out, pending = [], {}
    for raw in raw_lines:
        line = raw.strip()
        if not line:
            continue
        if line.startswith(("[时间|", "[章|", "[表演|", "[选择|", "[选项|", "[汇合|", "[结果|",
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
d08_common = find("[店主|对白|埃莉诺怎么想，得她自己说。]")
day69 = find("[时间|第69天]")
c2 = find("[选择|C2|")
c2c = close_after(c2)
d09_common = find("[莉迪亚|对白|那我说。我喜欢他，前几天已经跟他讲了。]")

branch_raw = open(BRANCH, encoding="utf-8").read()
branches = {}
for m in re.finditer(r"\[选项后续\|([^|\]]+)\|(\d+)\](.*?)\[/选项后续\]", branch_raw, re.DOTALL):
    branches[(m.group(1), m.group(2))] = m.group(3).splitlines()


def block(bid, label_zh, label_en, lines, presentation=None):
    b = {"id": bid, "label_zh": label_zh, "label_en": label_en, "lines": parse_lines(lines)}
    if presentation:
        b["presentation"] = presentation
    return b


d08_blocks = [
    block("d08_b001", "贺词", "Congratulations", final[:c1]),
    block("d08_b002", "说我也喜欢你", "Say You Like Her Too", final[c1c + 1:d08_common]),
    block("d08_b003", "问她真想退吗", "Ask If She Really Wants to Leave", branches[("C1", "2")]),
    block("d08_b004", "拿吃的给她", "Give Her Something to Eat", branches[("C1", "3")]),
    block("d08_b005", "三个人当面说", "Talk in Person, All Three", final[d08_common:day69]),
]
d08_choices = [
    {"id": "d08_c1_1", "label_zh": "说我也喜欢你", "label_en": "Say you like her too",
     "description_zh": "先把喜欢说出口，但不替埃莉诺决定。", "description_en": "Say it, without deciding for Eleanor."},
    {"id": "d08_c1_2", "label_zh": "问她真想退吗", "label_en": "Ask if she really wants to leave",
     "description_zh": "让她把退和不退都说清楚。", "description_en": "Make her say both sides plainly."},
    {"id": "d08_c1_3", "label_zh": "拿吃的给她", "label_en": "Give her something to eat",
     "description_zh": "先让她吃口东西再谈。", "description_en": "Let her eat before they talk."},
]
d08 = {"id": "d08", "title_zh": "贺词", "title_en": "Congratulations",
       "blocks": d08_blocks, "choices": d08_choices}

d09_blocks = [
    block("d09_b001", "当面", "Face to Face", final[day69:c2],
          {"mode": "immediate", "backup1": {"sprite": "eleanor1", "name": "埃莉诺"}}),
    block("d09_b002", "让她们先说", "Let Them Speak First", final[c2c + 1:d09_common]),
    block("d09_b003", "先说自己想法", "Say What You Think First", branches[("C2", "2")]),
    block("d09_b004", "把凳子挪近", "Move the Stool Closer", branches[("C2", "3")]),
    block("d09_b005", "三个人都点头", "All Three Nod", final[d09_common:]),
]
d09_choices = [
    {"id": "d09_c2_1", "label_zh": "让她们先说", "label_en": "Let them speak first",
     "description_zh": "把位置留给她们两个人。", "description_en": "Leave the floor to the two of them."},
    {"id": "d09_c2_2", "label_zh": "先说自己想法", "label_en": "Say what you think first",
     "description_zh": "先说自己想留下两个人。", "description_en": "Say you want to keep them both."},
    {"id": "d09_c2_3", "label_zh": "把凳子挪近", "label_en": "Move the stool closer",
     "description_zh": "让她们坐近点把话说开。", "description_en": "Put them close enough to talk."},
]
d09 = {"id": "d09", "title_zh": "当面", "title_en": "Face to Face",
       "blocks": d09_blocks, "choices": d09_choices}

d = json.load(open(DUAL, encoding="utf-8"))
d["scenes"] = [s for s in d["scenes"] if s.get("id") not in ("d08", "d09")]
insert_at = next(i for i, s in enumerate(d["scenes"]) if s["id"] == "d07") + 1
for scene in (d08, d09):
    d["scenes"].insert(insert_at, scene)
    insert_at += 1
json.dump(d, open(DUAL, "w", encoding="utf-8"), ensure_ascii=False, indent=2)

ui = json.load(open(UI, encoding="utf-8"))
ui["er_d08_c1_prompt"] = ["她说要退出，手已经搭上凳子边，准备起身。", "She says she is leaving, one hand already on the stool."]
ui["er_d09_c2_prompt"] = ["埃莉诺看着莉迪亚，莉迪亚看着地，两个人都在等下一句。", "Eleanor looks at Lydia. Lydia looks at the floor. Both are waiting."]
json.dump(ui, open(UI, "w", encoding="utf-8"), ensure_ascii=False, indent=2)

items = []
for scene in (d08, d09):
    for b in scene["blocks"]:
        for i, ln in enumerate(b["lines"], 1):
            items.append({"key": f"er_dual_{b['id']}_{i:02d}", "speaker": ln["speaker"], "text": ln["text"]})
json.dump(items, open(os.path.join(ROOT, "tmp_d4_en_items.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=2)

for scene in (d08, d09):
    print(scene["id"], [(b["id"], len(b["lines"])) for b in scene["blocks"]],
          "choices", [c["id"] for c in scene["choices"]])
print("total lines", sum(len(b["lines"]) for s in (d08, d09) for b in s["blocks"]))
print("translation items", len(items))
