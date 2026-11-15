#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Apply translated common Act 3 English to Content and a fresh en part."""
from __future__ import annotations

import glob
import json
import shutil
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FLOW = ROOT / "tools" / "NovelWorkFlow_Eleanor"
EN = FLOW / "tmp_common_act3_en.json"
CONTENT = ROOT / "Content" / "story_v06.json"
PART8 = ROOT / "tools" / "en_v06_part8.json"
STAMP = datetime.now().strftime("%Y%m%d-%H%M%S")
BACKUP = FLOW / "Novel_Output" / f"backup_common_act3_en_{STAMP}"


def main():
    translated = json.loads(EN.read_text(encoding="utf-8"))
    content = json.loads(CONTENT.read_text(encoding="utf-8"))
    BACKUP.mkdir(parents=True, exist_ok=True)
    shutil.copy2(CONTENT, BACKUP / "story_v06.json")
    for path in glob.glob(str(ROOT / "tools" / "en_v06_part*.json")):
        if Path(path).name == "en_v06_part8.json":
            continue
        shutil.copy2(path, BACKUP / Path(path).name)

    for ch in content["chapters"]:
        if ch["id"] not in ("c12", "c13", "c14"):
            continue
        for block in ch["blocks"]:
            for i, line in enumerate(block["lines"], 1):
                key = f"er_v06_{block['id'].replace('.', '_')}_{i:02d}"
                if key not in translated:
                    raise KeyError("missing translation " + key)
                line["en"] = translated[key]
    CONTENT.write_text(json.dumps(content, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    # Remove old c12-c14 keys from the historical parts, then add part8.
    for path in sorted(glob.glob(str(ROOT / "tools" / "en_v06_part*.json"))):
        if Path(path).name == "en_v06_part8.json":
            continue
        data = json.loads(Path(path).read_text(encoding="utf-8"))
        filtered = {k: v for k, v in data.items()
                    if not k.startswith(("er_v06_c12_", "er_v06_c13_", "er_v06_c14_"))}
        if len(filtered) != len(data):
            Path(path).write_text(json.dumps(filtered, ensure_ascii=False, indent=2) + "\n",
                                  encoding="utf-8")
            print("cleaned", path, len(data) - len(filtered), "keys")
    PART8.write_text(json.dumps(translated, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("wrote", PART8, len(translated), "keys")
    print("updated", CONTENT)
    print("backup", BACKUP)


if __name__ == "__main__":
    main()
