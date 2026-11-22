"""Report spoken Chinese length for editorial review. Run after build_content.py."""

import argparse
import csv
import json
import math
import re
from pathlib import Path

from render_current_reading import DEAD_BLOCKS


ROOT = Path(__file__).resolve().parents[1]
ORIGINAL = ROOT / "docs/原作Eleanor章节_对照稿_2026-11-10.md"
ROUTES = {
    "maid_v2": "er_maid_v2_",
    "revolution_v2": "er_rev_v2_",
    "love_romance_v01": "er_love_",
    "love_romance_v02": "er_love_",
    "love_romance_v03": "er_love_",
    "dual_romance_v04": "er_dual_",
}
ORIGINAL_NAMES = {"埃莉诺": "Eleanor", "埃莉诺的叔叔": "Uncle", "治安部警官": "Officer", "黑市打手": "Thug"}
# One-based lines unconditionally filtered by EleanorStory.Create* on these paths.
UNPLAYED = {
    "er_v06_c01_n003": {4}, "er_v06_c02_n002": {1, 7}, "er_v06_c04_n002": {1},
    "er_v06_c04_b003": {1}, "er_v06_c05_n002": {1, 4, 9}, "er_v06_c17_b101": {8, 11, 19},
}


def original_frames(with_actions=False):
    for n, line in enumerate(ORIGINAL.read_text(encoding="utf-8").splitlines(), 1):
        name, _, text = line.partition("：")
        if name in ORIGINAL_NAMES:
            spoken = re.sub(r"\*[^*]*\*", "", text)
            if count(spoken):
                yield ORIGINAL_NAMES[name], str(n), text if with_actions else spoken


def script_blocks():
    # New-save story blocks, including conditional and trade outcomes. These are
    # script samples, not frequency-weighted player sessions.
    story = json.loads((ROOT / "Content/story_v06.json").read_text(encoding="utf-8"))
    skipped = set(DEAD_BLOCKS["story_v06.json"]) | {
        "c07.b101", "c07.b106", "c18.b001", "c18.b003", "c18.b003a", "c18.b003b",
        "c18.b003c", "c18.b004", "c22.b005", "ep.b001", "ep.b002", "ep.b003", "ep.b004",
    }
    for chapter in story["chapters"]:
        if chapter["id"] in {"c15", "c16", "rd"}:
            continue
        for block in chapter["blocks"]:
            if block["id"] not in skipped:
                yield chapter["id"], "er_v06_" + block["id"].replace(".", "_"), block
    maid = json.loads((ROOT / "Content/maid_v05.json").read_text(encoding="utf-8"))
    for block in maid["blocks"]:
        if block["id"] in {"m11_b001", "m11_b002", "m11_b003", "m11_b004", "m23_b006", "world_b004"}:
            yield "common-extra", "er_maid_" + block["id"], block
    for source, prefix in ROUTES.items():
        data = json.loads((ROOT / "Content" / (source + ".json")).read_text(encoding="utf-8"))
        for scene in data["scenes"]:
            sid = scene["id"]
            if prefix == "er_dual_" and sid not in {"d01", "dr_daily_2", "dr_daily_3"} and not re.fullmatch(r"dv\d{2}", sid):
                continue
            blocks = scene["blocks"]
            items = blocks.items() if isinstance(blocks, dict) else ((b["id"], b) for b in blocks)
            for bid, block in items:
                yield sid, prefix + bid, block


def route_frames(localization, with_actions=False):
    with localization.open(encoding="utf-8-sig", newline="") as stream:
        strings = dict(csv.reader(stream))
    for sid, prefix, block in script_blocks():
        for i, line in enumerate(block["lines"], 1):
            if i in UNPLAYED.get(prefix, ()) or (prefix == "er_v06_c03_n002" and i > 4):
                continue
            speaker = line["speaker"]
            if speaker.lower() in {"narrator", "news", "security_notice"}:
                continue
            if speaker == "SEC":
                speaker = {"dv07": "Lydia", "dv13": "Lydia", "love_a3_04": "Sophia"}.get(sid, "Eleanor")
            elif speaker == "mother":
                speaker = "Sophia"
            else:
                speaker = speaker.capitalize()
            key = f"{prefix}_{i:02d}"
            for frame in strings[key].replace(r"\n", "\n").splitlines():
                spoken = re.sub(r"<i>.*?</i>", "", frame)
                if count(spoken) and not re.fullmatch(r"（.*）", spoken.strip()):
                    yield speaker, key, re.sub(r"</?i>", "", frame) if with_actions else spoken


def count(text):
    return sum(ch.isalnum() for ch in re.sub(r"\{\d+\}", "0", text))


def measure(frames, eleanor=False):
    seen = set()
    sentences, boxes = [], []
    for speaker, key, text in frames:
        text = text.strip()
        if eleanor and speaker != "Eleanor":
            continue
        if (speaker, text) in seen or not count(text):
            continue
        seen.add((speaker, text))
        boxes.append((count(text), key, text))
        for part in re.split(r"[。！？!?]+", text):
            size = count(part)
            if size:
                sentences.append((size, key, part))
    return sentences, boxes


def join_continuations(frames):
    """Join only an adjacent line of the same speaker after a trailing comma."""
    pending = None
    last_key = None
    for speaker, key, text in frames:
        if pending:
            who, start, previous = pending
            if key.isdigit():  # The reference transcript leaves a blank line between cues.
                adjacent = int(key) == int(last_key) + 2
            else:
                block, index = key.rsplit("_", 1)
                last_block, last_index = last_key.rsplit("_", 1)
                adjacent = block == last_block and (key == last_key or int(index) == int(last_index) + 1)
            if adjacent and who == speaker and previous.rstrip().endswith(("，", ",")):
                pending = who, start, previous + text
            else:
                yield pending
                pending = speaker, key, text
        else:
            pending = speaker, key, text
        last_key = key
    if pending:
        yield pending


def percentile(values, pct):
    return sorted(values)[math.ceil(len(values) * pct / 100) - 1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--zh", type=Path, default=ROOT / "Localization/zh.csv",
                        help="Chinese localization with the current scene and line keys")
    parser.add_argument("--over", action="store_true", help="list MOD lines above the corresponding original P95")
    args = parser.parse_args()
    original = list(original_frames())
    mod = list(route_frames(args.zh))
    visible_orig = list(original_frames(with_actions=True))
    visible_mod = list(route_frames(args.zh, with_actions=True))
    print("角色\t来源\t口径\t样本数\tP50\tP90\tP95\t最大\t超过原作P95")
    for eleanor in (True, False):
        role = "埃莉诺" if eleanor else "全角色"
        baseline = measure(original, eleanor)
        current = measure(mod, eleanor)
        whole_orig, _ = measure(join_continuations(original), eleanor)
        whole_mod, _ = measure(join_continuations(mod), eleanor)
        _, original_boxes = measure(visible_orig, eleanor)
        _, mod_boxes = measure(visible_mod, eleanor)
        for kind, orig, edited in (("完整语句", whole_orig, whole_mod),
                                   ("框内单句", baseline[0], current[0]),
                                   ("单框口播", baseline[1], current[1]),
                                   ("对白框含动作", original_boxes, mod_boxes)):
            threshold = percentile([x[0] for x in orig], 95)
            for label, rows in (("原作", orig), ("MOD", edited)):
                sizes = [x[0] for x in rows]
                print(role, label, kind, len(sizes), *(percentile(sizes, q) for q in (50, 90, 95, 100)),
                      sum(x > threshold for x in sizes), sep="\t")
            if args.over:
                for n, key, text in sorted(edited, reverse=True):
                    if n > threshold:
                        print(f"  {n:3} {key} {text}")


if __name__ == "__main__":
    main()
