#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Static checks that do not require launching the game."""

from __future__ import annotations

import csv
import glob
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def read_csv(name: str) -> dict:
    path = os.path.join(ROOT, "Localization", name + ".csv")
    out = {}
    with open(path, encoding="utf-8-sig") as f:
        for row in csv.reader(f):
            if len(row) >= 2:
                out[row[0]] = row[1]
    return out


def main() -> int:
    errors = []
    zh = read_csv("zh")
    en = read_csv("en")

    source = ""
    for path in glob.glob(os.path.join(ROOT, "src", "*.cs")):
        with open(path, encoding="utf-8") as f:
            source += f.read()

    keys = set(re.findall(r'L\.Get\("([^"]+)"', source))
    keys.update(re.findall(r'Prompt\("([^"]+)"', source))
    missing_zh = sorted(k for k in keys if k not in zh)
    if missing_zh:
        errors.append("Missing zh keys: " + ", ".join(missing_zh))

    with open(os.path.join(ROOT, "Content", "story_v06.json"), encoding="utf-8") as f:
        content = json.load(f)
    content_keys = []
    content_blocks = set()
    for ch in content["chapters"]:
        for b in ch["blocks"]:
            content_blocks.add(b["id"])
            for i in range(1, len(b["lines"]) + 1):
                content_keys.append("er_v06_" + b["id"].replace(".", "_") + f"_{i:02d}")

    missing_content = sorted(k for k in content_keys if k not in zh)
    if missing_content:
        errors.append("Missing zh content keys: " + ", ".join(missing_content[:20]))

    used_blocks = set(re.findall(r'StoryV06Content\.B_[A-Z0-9_]+', source))
    # Constants are declared in the generated file; check that each used constant
    # maps to a block in the parsed source.
    gen = open(os.path.join(ROOT, "src", "StoryV06Content.g.cs"), encoding="utf-8").read()
    const_map = dict(re.findall(r'public const string (B_[A-Z0-9_]+) = "([^"]+)";', gen))
    for const in re.findall(r'StoryV06Content\.(B_[A-Z0-9_]+)', source):
        block = const_map.get(const)
        if block is None:
            errors.append("Unknown block constant used: " + const)
        elif block not in content_blocks:
            errors.append("Block constant has no source block: " + const + " -> " + block)

    missing_en_content = sorted(k for k in content_keys if k not in en)
    missing_en_code = sorted(k for k in keys if k not in en)
    if missing_en_content:
        errors.append("Missing en content keys: " + ", ".join(missing_en_content[:20]))
    if missing_en_code:
        errors.append("Missing en code keys: " + ", ".join(missing_en_code[:20]))
    print(f"zh keys: {len(zh)}")
    print(f"en keys: {len(en)} (missing {len(missing_en_content)} content keys, {len(missing_en_code)} code keys)")
    print(f"content blocks: {len(content_blocks)}")
    print(f"content lines: {len(content_keys)}")
    print(f"code L.Get keys: {len(keys)}")

    if errors:
        for e in errors:
            print("ERROR:", e)
        return 1
    print("OK: no missing zh/en keys or invalid block references.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
