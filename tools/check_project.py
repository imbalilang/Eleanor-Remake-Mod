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
    # Prefixes concatenated with project numbers are verified through maid_ui.
    keys={k for k in keys if not k.endswith('_')}
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
    maid_path=os.path.join(ROOT,'Content','maid_v05.json')
    if os.path.exists(maid_path):
        maid=json.load(open(maid_path,encoding='utf-8'))
        interaction={'blocks':[]}
        for extra_name in ['interaction_v1.json','relationship_choices_v1.json']:
            interaction_path=os.path.join(ROOT,'Content',extra_name)
            if os.path.exists(interaction_path):interaction['blocks'].extend(json.load(open(interaction_path,encoding='utf-8'))['blocks'])
        maid['blocks'].extend(interaction['blocks'])
        blocks={b['id']:b for b in maid['blocks']}
        translations={}
        for path in glob.glob(os.path.join(ROOT,'Content','maid_v05_en_part*.json')):
            translations.update(json.load(open(path,encoding='utf-8')))
        translations.update({b['id']:[line['en'] for line in b['lines']] for b in interaction['blocks']})
        for bid,b in blocks.items():
            if len(translations.get(bid,[]))!=len(b['lines']):errors.append('Maid translation mismatch: '+bid)
            for i,line in enumerate(b['lines'],1):
                key=f'er_maid_{bid}_{i:02d}'
                if not zh.get(key) or not en.get(key):errors.append('Missing maid line: '+key)
                elif re.search(r'[\u4e00-\u9fff]',en[key]):errors.append('Untranslated English: '+key)
        for bid in set(re.findall(r'"((?:m\d+|legacy|wait|daily|tragic|exit|ending|world|interaction|gala|choicefix)_b\d{3})"',source)):
            if bid not in blocks:errors.append('Unknown maid block: '+bid)
        ui=json.load(open(os.path.join(ROOT,'Content','maid_ui.json'),encoding='utf-8'))
        business_path=os.path.join(ROOT,'Content','business_ui.json')
        if os.path.exists(business_path):ui.update(json.load(open(business_path,encoding='utf-8')))
        dividend_path=os.path.join(ROOT,'Content','dividend_ui.json')
        if os.path.exists(dividend_path):ui.update(json.load(open(dividend_path,encoding='utf-8')))
        scav_path=os.path.join(ROOT,'Content','scav_ui.json')
        if os.path.exists(scav_path):ui.update(json.load(open(scav_path,encoding='utf-8')))
        discount_path=os.path.join(ROOT,'Content','project_discount_ui.json')
        if os.path.exists(discount_path):ui.update(json.load(open(discount_path,encoding='utf-8')))
        interaction_ui_path=os.path.join(ROOT,'Content','interaction_ui.json')
        if os.path.exists(interaction_ui_path):ui.update(json.load(open(interaction_ui_path,encoding='utf-8')))
        choice_ui_path=os.path.join(ROOT,'Content','relationship_choice_ui.json')
        if os.path.exists(choice_ui_path):ui.update(json.load(open(choice_ui_path,encoding='utf-8')))
        for key,pair in ui.items():
            if set(re.findall(r'\{\d+\}',pair[0]))!=set(re.findall(r'\{\d+\}',pair[1])):errors.append('Placeholder mismatch: '+key)
        print(f"maid blocks: {len(blocks)}, lines: {sum(len(b['lines']) for b in blocks.values())}, UI: {len(ui)}")
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
