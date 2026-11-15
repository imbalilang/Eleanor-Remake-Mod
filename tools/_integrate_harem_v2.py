#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Merge the parsed harem-line v2 scenes into Content/dual_romance_v04.json."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FLOW = ROOT / "tools" / "NovelWorkFlow_Eleanor"
SCENES = FLOW / "tmp_harem_v2_scenes.json"
EN = FLOW / "tmp_harem_v2_en.json"
DUAL = ROOT / "Content" / "dual_romance_v04.json"

V2_IDS = [f"dv{i:02d}" for i in range(1, 14)]
OUTCOME_EN = {"公道": "Fair price", "让利": "Discount", "高价": "Overcharged", "未成交": "No sale"}


def main():
    parsed = json.loads(SCENES.read_text(encoding="utf-8"))["scenes"]
    en = json.loads(EN.read_text(encoding="utf-8"))
    dual = json.loads(DUAL.read_text(encoding="utf-8"))

    scenes = []
    for scene in parsed:
        sid = scene["id"]
        title_en = en["title." + sid]
        choices = []
        for choice in scene["choices"]:
            cid = choice["id"]
            choices.append({
                "id": cid,
                "label_zh": choice["label_zh"],
                "label_en": en["choice." + cid + ".label"],
                "description_zh": choice["description_zh"],
                "description_en": en["choice." + cid + ".desc"],
            })

        blocks = []
        for block in scene["blocks"]:
            block_id = block["id"]
            suffix = block["label_zh"]
            if "：" in suffix:
                suffix = suffix.split("：", 1)[1]
            if suffix == scene["title_zh"]:
                label_en = title_en
            elif suffix == "共通":
                label_en = title_en + " — Common"
            elif suffix in OUTCOME_EN:
                label_en = title_en + " — " + OUTCOME_EN[suffix]
            else:
                match = next((c for c in choices if c["label_zh"] == suffix), None)
                if match is None:
                    raise ValueError(f"{block_id}: no English label for branch {suffix}")
                label_en = title_en + " — " + match["label_en"]
            lines = []
            for line in block["lines"]:
                key = line["_key"]
                if key not in en:
                    raise ValueError("missing translation " + key)
                row = {"speaker": line["speaker"], "text": line["text"], "en": en[key]}
                if line.get("expr"):
                    row["expr"] = line["expr"]
                lines.append(row)
            out_block = {
                "id": block_id,
                "label_zh": block["label_zh"],
                "label_en": label_en,
                "lines": lines,
            }
            if block.get("presentation"):
                out_block["presentation"] = block["presentation"]
            blocks.append(out_block)

        scenes.append({
            "id": sid,
            "title_zh": scene["title_zh"],
            "title_en": title_en,
            "blocks": blocks,
            "choices": choices,
        })

    # Idempotent: replace any earlier v2 attempt, keep every old scene intact.
    dual["scenes"] = [s for s in dual["scenes"] if s.get("id") not in V2_IDS]
    dual["scenes"].extend(scenes)
    DUAL.write_text(json.dumps(dual, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("merged", len(scenes), "scenes; total",
          len(dual["scenes"]), "->", DUAL)


if __name__ == "__main__":
    main()
