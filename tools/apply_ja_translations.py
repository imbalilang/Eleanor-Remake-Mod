#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Merge tools/ja_translations.json into Localization/ja.csv.

The overlay maps localization keys to Japanese text. Only translated keys are
written; ModLoader falls back to English for missing keys.
"""
from __future__ import annotations

import csv
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OVERLAY = ROOT / "tools" / "ja_translations.json"
ZH = ROOT / "Localization" / "zh.csv"
OUT = ROOT / "Localization" / "ja.csv"


def read_zh() -> dict[str, str]:
    rows: dict[str, str] = {}
    with open(ZH, encoding="utf-8", newline="") as f:
        for row in csv.reader(f):
            if len(row) >= 2:
                rows[row[0]] = row[1]
    return rows


def write_ja(zh: dict[str, str], overlay: dict[str, str]) -> None:
    unknown = sorted(set(overlay) - set(zh))
    if unknown:
        raise SystemExit("Japanese overlay has unknown keys: " + ", ".join(unknown[:10]))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    with open(OUT, "w", encoding="utf-8", newline="") as f:
        for key in sorted(overlay):
            value = str(overlay[key]).replace("\r", "").replace("\n", "\\n").replace('"', '""')
            f.write(f'{key},"{value}"\n')


def main() -> int:
    zh = read_zh()
    overlay = json.loads(OVERLAY.read_text(encoding="utf-8"))
    write_ja(zh, overlay)
    print(f"Wrote {OUT} ({len(overlay)}/{len(zh)} keys translated)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
