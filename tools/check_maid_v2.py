#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Validate Content/maid_v2.json scene graph before runtime tests."""
from __future__ import annotations

import json
import sys
from pathlib import Path

EXPECTED_CODES = [8, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 25, 26, 27, 28]


def main(root: Path) -> int:
    path = root / "Content" / "maid_v2.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    errors = []
    scenes = data.get("scenes", [])
    if len(scenes) != 16:
        errors.append(f"expected 16 scenes, got {len(scenes)}")
    codes = [s.get("code") for s in scenes]
    if codes != EXPECTED_CODES:
        errors.append(f"scene code order mismatch: {codes}")

    block_ids = set()
    for scene in scenes:
        sid = scene.get("id", "?")
        blocks = scene.get("blocks", {})
        for bid in blocks:
            if bid in block_ids:
                errors.append(f"duplicate block id {bid}")
            block_ids.add(bid)
        for seg in scene.get("segments", []):
            if seg.get("type") == "lines":
                if seg.get("block") not in blocks:
                    errors.append(f"{sid}: missing segment block {seg.get('block')}")
            elif seg.get("type") == "choice":
                options = seg.get("options", [])
                if not (2 <= len(options) <= 3):
                    errors.append(f"{sid}/{seg.get('id')}: {len(options)} options")
                mainline = seg.get("mainline")
                if not isinstance(mainline, int) or not (1 <= mainline <= len(options)):
                    errors.append(f"{sid}/{seg.get('id')}: bad mainline {mainline}")
                for opt in options:
                    if opt.get("mainline"):
                        continue
                    if not opt.get("block") and not opt.get("result"):
                        errors.append(f"{sid}/{seg.get('id')}/{opt.get('id')}: no branch/result")
                    if opt.get("block") and opt["block"] not in blocks:
                        errors.append(f"{sid}/{seg.get('id')}/{opt.get('id')}: missing block {opt['block']}")
            elif seg.get("type") not in ("result",):
                errors.append(f"{sid}: unknown segment type {seg.get('type')}")

        for trade in scene.get("trades", []):
            if not trade.get("outcomes"):
                errors.append(f"{sid}: trade without outcomes")
            for outcome, bid in trade.get("outcomes", {}).items():
                if bid not in blocks:
                    errors.append(f"{sid}: missing trade outcome block {bid} ({outcome})")

    if errors:
        print("FAIL")
        for e in errors:
            print(" -", e)
        return 1
    print(f"OK: {len(scenes)} scenes, {len(block_ids)} blocks, "
          f"{sum(1 for s in scenes for x in s['segments'] if x['type']=='choice')} choices")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(Path(__file__).resolve().parents[1]))
