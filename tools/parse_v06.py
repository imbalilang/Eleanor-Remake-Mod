#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Parse the v0.6 reading script into structured story blocks.

The source file is a human-readable guide.  Only speaker-labelled dialogue and
explicit scene narration become story lines; "玩家处理"/"结果" notes and branch
explanations are kept as metadata, never as dialogue.

Outputs (run from the dev folder):
    Content/story_v06.json          structured source of truth
    docs/v06_block_index.md         review index with block ids
    src/StoryV06Content.g.cs        generated C# one-line summary (optional)
"""

from __future__ import annotations

import json
import os
import re
import sys

SPEAKERS = {
    "埃莉诺": "eleanor",
    "玩家": "player",
    "母亲": "mother",
    "莉迪亚": "lydia",
    "假叔叔": "uncle",
    "黑市打手": "thug",
    "治安部警官": "officer",
    "新闻": "news",
    "治安部通知": "security_notice",
}

# Chapter-like headings.  The main line is followed by a "第N天" descriptor.
CHAPTER_RE = re.compile(r"^第(\d+)章\s+(.+)$")
ROUTE_RE = re.compile(r"^(路线丙|路线丁)(?:\s{2,}|$)|^(第60天以后)(?:\s|$)|^(原版游戏结束时|关键行为与分支速查)")
SECTION_RE = re.compile(r"^【(.+?)】$")
DAY_RE = re.compile(r"^(参考)?第(\d+)(?:～(\d+))?天")

# Plain branch labels are guide metadata, but we also need their boundary so
# dialogue can be attached to the right branch.  A label is a short line ending
# in a full-width colon that is not a speaker and not a section header.
LABEL_RE = re.compile(r"^[^\n：]{1,48}：$")


class Block:
    def __init__(self, block_id: str, chapter: str, scene: str, label: str):
        self.id = block_id
        self.chapter = chapter
        self.scene = scene
        self.label = label
        self.lines: list[dict] = []

    def add(self, speaker: str, text: str) -> None:
        text = text.strip()
        if text:
            self.lines.append({"speaker": speaker, "text": text})

    def to_dict(self) -> dict:
        return {
            "id": self.id,
            "chapter": self.chapter,
            "scene": self.scene,
            "label": self.label,
            "lines": self.lines,
        }


def clean(text: str) -> str:
    return text.replace("\u3000", " ").strip()


def parse(path: str) -> dict:
    with open(path, encoding="utf-8-sig") as f:
        lines = f.read().splitlines()

    chapters: list[dict] = []
    current_chapter = None
    current_scene = ""
    blocks: list[Block] = []
    block_seq = 0
    current_block: Block | None = None
    pending_label: str | None = None
    skip_until_chapter = False
    chapter_day = ""
    in_chapter = False

    def flush_label():
        nonlocal current_block, pending_label
        if pending_label is not None and current_chapter is not None:
            current_block = new_block(current_scene, pending_label)
            pending_label = None

    def new_block(scene: str, label: str | None) -> Block:
        nonlocal block_seq
        block_seq += 1
        b = Block(f"{current_chapter['id']}.b{block_seq:03d}", current_chapter["id"], scene, label or "")
        blocks.append(b)
        return b

    def ensure_block() -> Block:
        nonlocal current_block
        if current_block is None:
            current_block = new_block(current_scene, "")
        return current_block

    def finish_chapter():
        nonlocal current_chapter, blocks, block_seq, current_block, current_scene, pending_label, in_chapter, chapter_day
        if current_chapter is not None:
            current_chapter["blocks"] = [b.to_dict() for b in blocks if b.lines]
            chapters.append(current_chapter)
        current_chapter = None
        blocks = []
        block_seq = 0
        current_block = None
        current_scene = ""
        pending_label = None
        in_chapter = False
        chapter_day = ""

    i = 0
    while i < len(lines):
        raw = lines[i]
        line = clean(raw)
        i += 1

        if not line:
            continue

        m = CHAPTER_RE.match(line)
        if m:
            finish_chapter()
            num = int(m.group(1))
            current_chapter = {
                "id": f"c{num:02d}",
                "number": num,
                "title": m.group(2).strip(),
                "day": "",
                "kind": "chapter",
                "blocks": [],
            }
            in_chapter = True
            continue

        rm = ROUTE_RE.match(line)
        if rm:
            route = next((g for g in rm.groups() if g), "")
            if route == "关键行为与分支速查":
                finish_chapter()
                break
            finish_chapter()
            route_ids = {
                "路线丙": "rc",
                "路线丁": "rd",
                "第60天以后": "ep",
                "原版游戏结束时": "end",
            }
            current_chapter = {
                "id": route_ids[route],
                "number": 0,
                "title": line,
                "day": "",
                "kind": "end" if route == "原版游戏结束时" else "route",
                "blocks": [],
            }
            in_chapter = True
            continue

        if not in_chapter:
            continue

        # Chapter day descriptor directly below the title.
        if current_chapter is not None and not current_chapter["day"]:
            dm = DAY_RE.match(line)
            if dm:
                current_chapter["day"] = line
                # Some titles are followed by a separator; ignore it.
                continue

        sec = SECTION_RE.match(line)
        if sec:
            current_scene = sec.group(1).strip()
            current_block = None
            pending_label = None
            continue

        # Known speaker: next non-empty line is the dialogue paragraph.
        speaker = None
        text = ""
        if line.endswith("："):
            name = line[:-1].strip()
            if name in SPEAKERS:
                speaker = SPEAKERS[name]
                # Advance to the next non-empty line.
                while i < len(lines) and not clean(lines[i]):
                    i += 1
                if i < len(lines):
                    text = clean(lines[i])
                    i += 1
        elif line in SPEAKERS:
            # Rare: speaker name without colon.
            speaker = SPEAKERS[line]
            while i < len(lines) and not clean(lines[i]):
                i += 1
            if i < len(lines):
                text = clean(lines[i])
                i += 1

        if speaker is not None and text:
            ensure_block().add(speaker, text)
            continue

        # Branch labels such as "按正常价格成交：" start a new dialogue block.
        if LABEL_RE.match(line) and line[:-1].strip() not in SPEAKERS:
            label = line[:-1].strip()
            # Ignore guide sections; they carry no dialogue of their own.
            if any(k in label for k in ("玩家处理", "结果", "完整路线", "演出方式", "阅读说明")):
                continue
            current_block = new_block(current_scene, label)
            continue

        # Ending variants are pure narration paragraphs below a condition header.
        if current_chapter is not None and current_chapter.get("kind") == "end":
            if line.strip("= ") == "":
                continue
            if line == "遵守实际结局，不覆盖原版叙事":
                continue
            ensure_block().add("narrator", line)
            continue

    finish_chapter()
    return {"chapters": chapters}


def write_index(data: dict, path: str) -> None:
    out = ["# v0.6 场景块索引", ""]
    for ch in data["chapters"]:
        out.append(f"## {ch['id']} {ch['title']}  {ch['day']}")
        for b in ch["blocks"]:
            head = b["lines"][0]["text"] if b["lines"] else ""
            label = f" / {b['label']}" if b["label"] else ""
            scene = f"【{b['scene']}】" if b["scene"] else ""
            out.append(f"- `{b['id']}` {scene}{label} — {head[:48]}")
        out.append("")
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(out))


def main() -> int:
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    src = os.path.join(root, "docs", "全流程攻略_纯阅读版_v0.6_文风优化.txt")
    if not os.path.exists(src):
        print("Source not found:", src, file=sys.stderr)
        return 1

    data = parse(src)
    content_dir = os.path.join(root, "Content")
    os.makedirs(content_dir, exist_ok=True)
    json_path = os.path.join(content_dir, "story_v06.json")
    with open(json_path, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

    write_index(data, os.path.join(root, "docs", "v06_block_index.md"))

    line_count = sum(len(b["lines"]) for ch in data["chapters"] for b in ch["blocks"])
    print(f"Wrote {json_path}")
    print(f"Chapters: {len(data['chapters'])}, blocks: {sum(len(ch['blocks']) for ch in data['chapters'])}, lines: {line_count}")
    for ch in data["chapters"]:
        counts = sum(len(b["lines"]) for b in ch["blocks"])
        print(f"  {ch['id']} {ch['title']}: {len(ch['blocks'])} blocks / {counts} lines")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
