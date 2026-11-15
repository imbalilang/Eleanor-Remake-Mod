#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Apply the cleaned common Act 3 blocks to Content/story_v06.json and create
a translation worklist. Chapter c11 (the shared 800/attendant entry) is not
touched.
"""
from __future__ import annotations

import json
import shutil
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FLOW = ROOT / "tools" / "NovelWorkFlow_Eleanor"
BLOCKS = FLOW / "tmp_common_act3_blocks.json"
CONTENT = ROOT / "Content" / "story_v06.json"
UI_ZH = ROOT / "tools" / "ui_text_zh.json"
UI_EN = ROOT / "tools" / "ui_text_en.json"
ITEMS = FLOW / "tmp_common_act3_items.json"
STAMP = datetime.now().strftime("%Y%m%d-%H%M%S")
BACKUP = FLOW / "Novel_Output" / f"backup_common_act3_content_{STAMP}"

UI = {
    "er_prompt_threat_common": ("（三百压在柜台上。）", "(Three hundred sits on the counter.)"),
    "er_choice_drive_keep": ("替她收着", "Keep it for her"),
    "er_choice_drive_keep_desc": ("把数据盘锁在柜台下面。", "Lock the drive under the counter."),
    "er_choice_drive_ask": ("先问她盘里还有什么", "Ask what else is on the drive"),
    "er_choice_drive_ask_desc": ("先听她把盘里的东西说完。", "Let her finish describing what is on it."),
    "er_choice_threat_refuse": ("不收，话带到", "Refuse the money, carry the message"),
    "er_choice_threat_refuse_desc": ("不收三百，把威胁原话带给她。", "Do not take the three hundred; tell her exactly what was said."),
    "er_choice_threat_throw": ("叫他出去", "Throw him out"),
    "er_choice_threat_throw_desc": ("不接这个话头，把他赶出去。", "Do not take the bait; order him out."),
    "er_prompt_post_care": ("（她靠着柜台不走。）", "(She stays leaning on the counter.)"),
    "er_choice_post_sit": ("让她坐", "Tell her to sit"),
    "er_choice_post_sit_desc": ("让她坐下来歇一会儿。", "Let her sit down and rest."),
    "er_choice_post_ask": ("问她买什么", "Ask what she wants to buy"),
    "er_choice_post_ask_desc": ("先问她要买什么。", "Ask what she came to buy first."),
}


def main():
    parsed = json.loads(BLOCKS.read_text(encoding="utf-8"))
    content = json.loads(CONTENT.read_text(encoding="utf-8"))
    BACKUP.mkdir(parents=True, exist_ok=True)
    shutil.copy2(CONTENT, BACKUP / "story_v06.json")
    shutil.copy2(UI_ZH, BACKUP / "ui_text_zh.json")
    shutil.copy2(UI_EN, BACKUP / "ui_text_en.json")

    chapters = {ch["id"]: ch for ch in content["chapters"]}
    items = []
    for newch in parsed["chapters"]:
        blocks = []
        for b in newch["blocks"]:
            lines = []
            for i, line in enumerate(b["lines"], 1):
                row = {"speaker": line["speaker"], "text": line["text"], "en": ""}
                if line.get("expr"):
                    row["expr"] = line["expr"]
                lines.append(row)
                items.append({
                    "key": f"er_v06_{b['id'].replace('.', '_')}_{i:02d}",
                    "kind": "line",
                    "speaker": line["speaker"],
                    "zh": line["text"],
                })
            blocks.append({
                "id": b["id"],
                "scene": newch["title"],
                "label": b["label"],
                "lines": lines,
            })
        chapters[newch["id"]] = {
            "id": newch["id"],
            "title": newch["title"],
            "blocks": blocks,
        }
        # Keep the original chapter order.
    order = [ch["id"] for ch in content["chapters"]]
    content["chapters"] = [chapters[cid] for cid in order]
    CONTENT.write_text(json.dumps(content, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    zh = json.loads(UI_ZH.read_text(encoding="utf-8"))
    en = json.loads(UI_EN.read_text(encoding="utf-8"))
    for key, (zh_text, en_text) in UI.items():
        zh[key] = zh_text
        en[key] = en_text
    UI_ZH.write_text(json.dumps(zh, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    UI_EN.write_text(json.dumps(en, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    ITEMS.write_text(json.dumps(items, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("updated chapters:", [ch["id"] for ch in parsed["chapters"]])
    print("blocks:", sum(len(ch["blocks"]) for ch in parsed["chapters"]),
          "lines:", sum(len(b["lines"]) for ch in parsed["chapters"] for b in ch["blocks"]))
    print("items:", len(items))
    print("backup:", BACKUP)


if __name__ == "__main__":
    main()
