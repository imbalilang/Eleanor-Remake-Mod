#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Translate Content/revolution_v2.json into English with the existing OpenRouter writer key."""
from __future__ import annotations

import argparse
import json
import os
import re
import time
from pathlib import Path

from openai import OpenAI

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Content" / "revolution_v2.json"
ENGINE = ROOT / "tools" / "NovelWorkFlow_Eleanor" / "Novel_Engine_V7.py"

SYSTEM = (
    "You are translating a mature, non-explicit Chinese game script into natural English. "
    "Translate faithfully and concisely; do not add, remove, explain, soften, or censor. "
    "Keep each line short enough for a dialogue bubble. Preserve <i>...</i> tags exactly. "
    "Use these terms consistently: Eleanor, Sophia, Lydia, shopkeeper, Revolution, "
    "Revolutionary Quartermaster, Revolutionary Fighter, Revolutionary Guerrilla, "
    "Upper Level, Lower Level, credits, Nutrifruit, Pastel, roster, unit, squad leader, "
    "tactical vest, bandages, antiseptic, candy wrapper, fence (sell loot), list. "
    "Eleanor is proud, sharp, tired, direct, and often sarcastic; she never calls the player "
    "Master on this route. The shopkeeper/player is terse. Revolution NPCs are plain, practical, "
    "and report facts. '我好想你' must be translated as 'I missed you.' "
    "Return ONLY a JSON object mapping each input id to its English translation."
)


def api_key() -> str:
    key = os.environ.get("OPENROUTER_API_KEY", "").strip()
    if key:
        return key
    text = ENGINE.read_text(encoding="utf-8")
    m = re.search(r'api_key="(sk-or-[^"]+)"', text)
    if not m:
        raise SystemExit("OPENROUTER_API_KEY not set and no OpenRouter key found in Novel_Engine_V7.py")
    return m.group(1)


def parse_json(text: str) -> dict:
    t = text.strip()
    t = re.sub(r"^```(?:json)?\s*", "", t)
    t = re.sub(r"\s*```$", "", t)
    start = t.find("{")
    end = t.rfind("}")
    if start < 0 or end <= start:
        raise ValueError("no JSON object in response")
    return json.loads(t[start:end + 1])


def collect(data: dict):
    tasks = []
    for si, scene in enumerate(data["scenes"]):
        if not scene.get("title_en") or scene.get("title_en") == scene.get("title_zh"):
            tasks.append({"id": f"T{si}", "text": scene.get("title_zh", ""), "kind": "title", "si": si})
        for block_id, block in scene.get("blocks", {}).items():
            for li, line in enumerate(block.get("lines", [])):
                en = line.get("en")
                text = line.get("text", "")
                if not en or en == text:
                    tasks.append({"id": f"L{si}:{block_id}:{li}", "text": text, "kind": "line",
                                  "si": si, "block": block_id, "li": li})
        for ci, seg in enumerate(scene.get("segments", [])):
            if seg.get("type") != "choice":
                continue
            if not seg.get("prompt_en") or seg.get("prompt_en") == seg.get("prompt"):
                tasks.append({"id": f"C{si}:{ci}:prompt", "text": seg.get("prompt", ""),
                              "kind": "prompt", "si": si, "ci": ci})
            for oi, opt in enumerate(seg.get("options", [])):
                if not opt.get("label_en") or opt.get("label_en") == opt.get("label"):
                    tasks.append({"id": f"O{si}:{ci}:{oi}", "text": opt.get("label", ""),
                                  "kind": "label", "si": si, "ci": ci, "oi": oi})
    return tasks


def apply(data: dict, task: dict, value: str):
    si = task["si"]; scene = data["scenes"][si]
    if task["kind"] == "title":
        scene["title_en"] = value
    elif task["kind"] == "line":
        scene["blocks"][task["block"]]["lines"][task["li"]]["en"] = value
    elif task["kind"] == "prompt":
        scene["segments"][task["ci"]]["prompt_en"] = value
    elif task["kind"] == "label":
        scene["segments"][task["ci"]]["options"][task["oi"]]["label_en"] = value


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--batch", type=int, default=40)
    ap.add_argument("--model", default="anthropic/claude-opus-5.5")
    ap.add_argument("--limit", type=int, default=0)
    args = ap.parse_args()
    data = json.loads(SRC.read_text(encoding="utf-8"))
    tasks = collect(data)
    if args.limit:
        tasks = tasks[:args.limit]
    if not tasks:
        print("Nothing to translate.")
        return
    client = OpenAI(base_url="https://openrouter.ai/api/v1", api_key=api_key())
    print(f"Translating {len(tasks)} items in batches of {args.batch} with {args.model}")
    done = 0
    for start in range(0, len(tasks), args.batch):
        batch = tasks[start:start + args.batch]
        prompt = (SYSTEM + "\n\nTranslate these items. Return a JSON object keyed by id. "
                  "Every id must appear exactly once.\n\n"
                  + json.dumps([{"id": t["id"], "text": t["text"]} for t in batch], ensure_ascii=False))
        last_error = None
        for attempt in range(3):
            try:
                res = client.chat.completions.create(
                    model=args.model,
                    messages=[{"role": "system", "content": SYSTEM}, {"role": "user", "content": prompt}],
                    max_tokens=8000,
                )
                obj = parse_json(res.choices[0].message.content or "")
                missing = [t["id"] for t in batch if t["id"] not in obj]
                if missing:
                    raise ValueError("missing ids: " + ", ".join(missing[:5]))
                for t in batch:
                    value = str(obj[t["id"]]).strip()
                    if not value:
                        raise ValueError("empty translation for " + t["id"])
                    if re.search(r"[\u4e00-\u9fff]", value):
                        raise ValueError("untranslated Chinese in " + t["id"])
                    apply(data, t, value)
                SRC.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
                done += len(batch)
                print(f"  {done}/{len(tasks)} translated")
                last_error = None
                break
            except Exception as exc:
                last_error = exc
                print(f"  batch {start//args.batch + 1} attempt {attempt+1} failed: {exc}")
                time.sleep(2)
        if last_error is not None:
            raise SystemExit(f"Translation stopped at batch {start//args.batch + 1}: {last_error}")
    print("Done.")


if __name__ == "__main__":
    main()
