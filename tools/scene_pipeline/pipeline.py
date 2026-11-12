#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Route-level scene pipeline for Eleanor Story Expand.

One run produces a whole route (1-2 chapters, ~2800-3500 Chinese chars,
24-40 blocks) in the game script format, not a novel chapter.

Flow: world book -> route planner -> route writer -> QA -> markdown assembler.

Writer and QA are separate providers. API keys are read from two environment
variables (defaults: WRITER_API_KEY and QA_API_KEY). Never hardcode keys in
this file or commit them.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
from pathlib import Path
from typing import Any

try:
    import httpx
except ImportError:  # pragma: no cover
    httpx = None

ROOT = Path(__file__).resolve().parents[2]
PIPELINE_DIR = Path(__file__).resolve().parent
DEFAULT_CONFIG = PIPELINE_DIR / "config.example.json"
LAST_API_META: dict[str, Any] = {}

SPEAKER_LABELS = {
    "eleanor": "埃莉诺",
    "lydia": "莉迪亚",
    "sophia": "母亲",
    "player": "玩家",
    "npc": "其他角色",
}

PRESET_STYLE_NAMES = [
    "✅基础文风",
    "🖋️默认",
    "✅白描基调",
    "🧊抗机器人",
    "🌸禁词表",
    "✅基础创作准则",
    "📘写作指南",
    "🛡️准则结束",
    "🔗反八股",
    "🔗活人感对白",
    "🧊反全知",
    "🧊抗升华",
]
PRESET_BLOCK_WORDS = [
    "色情",
    "NSFW",
    "涩涩",
    "破限",
    "变量",
    "格式",
    "字数",
    "思考",
    "回复",
    "开场",
    "破甲",
    "底部",
    "预填充",
]
PRESET_LINE_BLOCK_WORDS = [
    "性器官",
    "肉棒",
    "精液",
    "淫",
    "性交",
    "乳房",
    "乳头",
    "阴道",
    "射精",
    "口交",
    "自慰",
    "色情",
    "露骨",
]


def load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8") as f:
        return json.load(f)


def load_text(path: Path) -> str:
    with path.open("r", encoding="utf-8") as f:
        return f.read()


def robust_json_parse(text: str) -> Any:
    """Parse a model response that may contain code fences or chatter."""
    text = text.strip()
    if text.startswith("```"):
        text = re.sub(r"^```(?:json|JSON)?\s*", "", text)
        text = re.sub(r"\s*```$", "", text)
    candidates = [text]
    start = text.find("{")
    end = text.rfind("}")
    if start != -1 and end != -1 and end > start:
        candidates.append(text[start : end + 1])
    last_err: Exception | None = None
    for candidate in candidates:
        for attempt in (candidate, re.sub(r",\s*([}\]])", r"\1", candidate)):
            try:
                return json.loads(attempt)
            except Exception as exc:  # noqa: BLE001
                last_err = exc
    raise ValueError(f"No valid JSON object found: {last_err}")


def chinese_chars(text: str) -> int:
    return len(re.findall(r"[\u4e00-\u9fff]", text))


def load_preset_style(preset_path: Path) -> str:
    """Extract selected non-NSFW style prompts from a SillyTavern preset."""
    if not preset_path.exists():
        return ""
    data = load_json(preset_path)
    chunks: list[str] = []
    for prompt in data.get("prompts", []):
        name = str(prompt.get("name", ""))
        content = str(prompt.get("content", "")).strip()
        if not content:
            continue
        if any(word in name for word in PRESET_BLOCK_WORDS):
            continue
        if not any(key in name for key in PRESET_STYLE_NAMES):
            continue
        kept = []
        for line in content.splitlines():
            if "{{" in line and "}}" in line:
                continue
            if any(word in line for word in PRESET_LINE_BLOCK_WORDS):
                continue
            kept.append(line)
        cleaned = "\n".join(kept).strip()
        if cleaned:
            chunks.append(f"【预设：{name}】\n{cleaned}")
    return "\n\n---\n\n".join(chunks)[:9000]


def build_world_micro(world_book: dict[str, Any]) -> str:
    """Build the compact world/character/style context used by all calls."""
    parts: list[str] = []
    setting = world_book.get("setting", {})
    parts.append("【世界观】")
    parts.append(str(setting.get("world", "")))
    background = setting.get("background", [])
    if isinstance(background, str):
        background = [background]
    if background:
        parts.append("【背景】")
        parts.extend(str(x) for x in background)
    if setting.get("player_role"):
        parts.append("【玩家身份】" + str(setting["player_role"]))
    if setting.get("economy"):
        parts.append("【经济】" + str(setting["economy"]))
    if setting.get("tone"):
        parts.append("【基调】" + str(setting["tone"]))
    parts.append(str(setting.get("player_shop", "")))
    facts = setting.get("key_facts", [])
    if facts:
        parts.append("【关键事实】")
        parts.extend(f"- {x}" for x in facts)

    characters = world_book.get("characters", {})
    parts.append("【角色】")
    for key in ("eleanor", "lydia", "sophia", "player"):
        char = characters.get(key)
        if not char:
            continue
        parts.append(f"### {char.get('name', key)}")
        if char.get("age"):
            parts.append(f"年龄：{char['age']}")
        for section in ("core", "voice", "boundaries"):
            values = char.get(section, [])
            if values:
                parts.append(f"{section}:")
                parts.extend(f"- {x}" for x in values)

    reader = world_book.get("reader_experience", {})
    if reader:
        parts.append("【爽文体验】")
        if reader.get("genre"):
            parts.append("题材：" + str(reader["genre"]))
        if reader.get("promise"):
            parts.append("承诺：" + str(reader["promise"]))
        for section in ("rules", "anti_patterns"):
            values = reader.get(section, [])
            if values:
                parts.append(f"{section}:")
                parts.extend(f"- {x}" for x in values)
        if reader.get("chapter_check"):
            parts.append("章检查：" + str(reader["chapter_check"]))

    style = world_book.get("style", {})
    parts.append("【文风】")
    for section in ("scene_contract", "dialogue", "player", "ai_tells"):
        values = style.get(section, [])
        if values:
            parts.append(f"{section}:")
            parts.extend(f"- {x}" for x in values)

    rules = world_book.get("output_rules", {})
    if rules:
        parts.append("【输出规则】")
        for k, v in rules.items():
            if isinstance(v, str):
                parts.append(f"- {k}: {v}")
    return "\n".join(parts)


def build_route_context(world_book: dict[str, Any], route_key: str) -> str:
    route = world_book.get("routes", {}).get(route_key, {})
    if not route:
        return ""
    parts = [
        f"【路线】{route.get('name', route_key)}",
        f"前提：{route.get('premise', '')}",
        f"弧线：{route.get('arc', '')}",
        f"结局：{route.get('ending', '')}",
    ]
    engine = route.get("event_engine", [])
    if engine:
        parts.append("事件引擎：" + "、".join(engine))
    return "\n".join(parts)


def fill_template(template: str, values: dict[str, str]) -> str:
    result = template
    for key, value in values.items():
        result = result.replace("{" + key + "}", value)
    return result


def resolve_api_key(cfg: dict[str, Any]) -> tuple[str, str]:
    """Resolve a key from api_key, or from api_key_env (env name or literal).

    Returns (key, source). Never log the key itself.
    """
    literal = str(cfg.get("api_key", "")).strip()
    if literal:
        return literal, "api_key"
    value = str(cfg.get("api_key_env", "")).strip()
    if not value:
        return "", "missing"
    if re.match(r"^[A-Za-z_][A-Za-z0-9_]*$", value):
        return os.environ.get(value, ""), value
    # The user pasted a key into api_key_env. Accept it, but warn outside.
    return value, "literal"


def post_chat(
    base_url: str,
    api_key: str,
    model: str,
    messages: list[dict[str, str]],
    temperature: float,
    max_tokens: int,
    timeout: float,
) -> str:
    if httpx is None:
        raise RuntimeError("httpx is required. Install it with: pip install httpx")
    url = base_url.rstrip("/") + "/chat/completions"
    payload = {
        "model": model,
        "messages": messages,
        "temperature": temperature,
        "max_tokens": max_tokens,
    }
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
    }
    with httpx.Client(timeout=timeout) as client:
        response = client.post(url, headers=headers, json=payload)
        response.raise_for_status()
        data = response.json()
    choice = data["choices"][0]
    global LAST_API_META
    LAST_API_META = {
        "finish_reason": choice.get("finish_reason"),
        "usage": data.get("usage"),
    }
    return choice["message"]["content"]


def fallback_route_plan(route_outline: dict[str, Any]) -> dict[str, Any]:
    chapters = []
    for chapter in route_outline.get("chapters", []):
        chapter_id = str(chapter.get("chapter", ""))
        blocks = []
        for i, scene in enumerate(chapter.get("blocks", []), start=1):
            blocks.append(
                {
                    "id": f"{chapter_id}_b{i:03d}",
                    "scene": str(scene),
                    "label": "",
                    "purpose": str(scene),
                }
            )
        chapters.append(
            {
                "chapter": chapter_id,
                "title": chapter.get("title", ""),
                "event": chapter.get("outline", ""),
                "want": "按路线大纲中人物的具体目标写。",
                "obstacle": "按路线大纲中的冲突写。",
                "beats": chapter.get("must_have", []),
                "choices": [
                    {"label": "我看看", "consequence": "玩家主动处理。"},
                    {"label": "你自己定", "consequence": "玩家把决定交还给她。"},
                    {"label": "先坐会儿", "consequence": "玩家先让场景慢下来。"},
                ],
                "key_lines": [],
                "end_change": "按路线大纲的收束写。",
                "blocks": blocks,
            }
        )
    return {
        "route": "normal",
        "title": route_outline.get("title", "回家"),
        "summary": "整条路线计划。",
        "chapters": chapters,
    }


def route_text_blob(route_bundle: dict[str, Any]) -> str:
    parts: list[str] = []
    for chapter in route_bundle.get("chapters", []):
        for block in chapter.get("blocks", []):
            for line in block.get("lines", []):
                parts.append(str(line.get("text", "")))
            for action in block.get("actions", []) or []:
                parts.append(str(action))
    return "\n".join(parts)


def validate_route(route_bundle: dict[str, Any]) -> list[str]:
    warnings: list[str] = []
    chapters = route_bundle.get("chapters")
    if not isinstance(chapters, list) or not chapters:
        return ["route.chapters missing or empty"]
    if len(chapters) > 2:
        warnings.append(f"route has {len(chapters)} chapters; target is 1-2")
    all_ids: set[str] = set()
    total_blocks = 0
    for chapter in chapters:
        chapter_id = str(chapter.get("chapter", ""))
        if not chapter_id:
            warnings.append("chapter id missing")
        blocks = chapter.get("blocks", [])
        if not isinstance(blocks, list) or not blocks:
            warnings.append(f"{chapter_id}: blocks missing")
            continue
        pleasure_beats = chapter.get("pleasure_beats", [])
        if not isinstance(pleasure_beats, list) or not pleasure_beats:
            warnings.append(f"{chapter_id}: pleasure_beats missing (爽点)")
        chapter_chars = chinese_chars(
            "\n".join(
                str(line.get("text", ""))
                for block in blocks
                for line in block.get("lines", [])
            )
        )
        if chapter_chars < 1000:
            warnings.append(
                f"{chapter_id}: only {chapter_chars} Chinese chars (target ~1400-1800)"
            )
        one_line_blocks = sum(
            1 for block in blocks if len(block.get("lines", [])) < 2
        )
        if one_line_blocks > max(2, len(blocks) // 3):
            warnings.append(
                f"{chapter_id}: {one_line_blocks}/{len(blocks)} blocks have <2 lines"
            )
        total_blocks += len(blocks)
        chapter_ids: set[str] = set()
        for block in blocks:
            block_id = str(block.get("id", ""))
            if not re.match(rf"^{re.escape(chapter_id)}_b\d{{3}}$", block_id):
                warnings.append(f"bad block id: {block_id}")
            if block_id in all_ids:
                warnings.append(f"duplicate block id: {block_id}")
            all_ids.add(block_id)
            chapter_ids.add(block_id)
            if not block.get("scene"):
                warnings.append(f"{block_id}: scene missing")
            lines = block.get("lines", [])
            if not isinstance(lines, list) or not lines:
                warnings.append(f"{block_id}: lines missing")
            for line in lines:
                if not isinstance(line, dict):
                    warnings.append(f"{block_id}: line is not an object")
                    continue
                if line.get("speaker") not in SPEAKER_LABELS:
                    warnings.append(f"{block_id}: bad speaker {line.get('speaker')}")
                if not str(line.get("text", "")).strip():
                    warnings.append(f"{block_id}: empty text")
        for block in blocks:
            for choice in block.get("choices", []) or []:
                next_id = choice.get("next", "")
                if next_id and next_id not in all_ids and next_id not in chapter_ids:
                    warnings.append(f"{block.get('id')}: choice next id not found: {next_id}")
    total_chars = chinese_chars(route_text_blob(route_bundle))
    if total_chars < 2400:
        warnings.append(f"route text is short: {total_chars} Chinese chars (target 2800-3500)")
    if total_chars > 4200:
        warnings.append(f"route text is long: {total_chars} Chinese chars (target 2800-3500)")
    if total_blocks < 20:
        warnings.append(f"route has {total_blocks} blocks (target 24-40)")
    if total_blocks > 44:
        warnings.append(f"route has {total_blocks} blocks (target 24-40)")
    return warnings


def assemble_route_markdown(route_bundle: dict[str, Any], state_delta: dict[str, Any] | None = None) -> str:
    lines: list[str] = []
    for chapter in route_bundle.get("chapters", []):
        lines.append(f"## {chapter.get('chapter', '')}　{chapter.get('title', '')}")
        lines.append("")
        for block in chapter.get("blocks", []):
            label = str(block.get("label", "")).strip()
            scene = str(block.get("scene", "")).strip()
            suffix = f"（scene=`{scene}`" + (f"，label=`{label}`" if label else "") + "）"
            lines.append(f"#### {block.get('id', '')}{suffix}")
            lines.append("```text")
            for line in block.get("lines", []):
                speaker = SPEAKER_LABELS.get(line.get("speaker"), "其他角色")
                text = str(line.get("text", "")).strip()
                if text:
                    lines.append(f"{speaker}：{text}")
            for action in block.get("actions", []) or []:
                action = str(action).strip()
                if action:
                    lines.append(f"（{action}）")
            lines.append("```")
            choices = block.get("choices", []) or []
            if choices:
                lines.append("")
                lines.append("【选项｜任选一项】")
                for i, choice in enumerate(choices, start=1):
                    label_text = str(choice.get("label", "")).strip()
                    next_id = str(choice.get("next", "")).strip()
                    lines.append(f"{i}. {label_text}" + (f" → {next_id}" if next_id else ""))
            lines.append("")
    if state_delta:
        lines.append(f"【状态】{json.dumps(state_delta, ensure_ascii=False)}")
        lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def load_progress(out_dir: Path) -> dict[str, Any]:
    path = out_dir / "progress.json"
    if path.exists():
        return load_json(path)
    return {"state": {}, "summaries": {}}


def save_progress(out_dir: Path, progress: dict[str, Any]) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "progress.json").write_text(
        json.dumps(progress, ensure_ascii=False, indent=2), encoding="utf-8"
    )


def run_route(
    config: dict[str, Any],
    world_book: dict[str, Any],
    route_key: str,
    preset_style: str,
    dry_run: bool,
    out_dir: Path,
) -> None:
    prompts_dir = Path(config["paths"]["prompts"])
    if not prompts_dir.is_absolute():
        prompts_dir = ROOT / prompts_dir
    writer_system_template = load_text(prompts_dir / "writer_system.md")
    writer_template = load_text(prompts_dir / "writer_template.md")
    planner_template = load_text(prompts_dir / "planner.md")
    qa_template = load_text(prompts_dir / "qa.md")
    out_dir.mkdir(parents=True, exist_ok=True)

    route = world_book.get("routes", {}).get(route_key)
    if not route:
        raise RuntimeError(f"Unknown route: {route_key}")
    route_outline = route.get("route_outline")
    if not route_outline:
        raise RuntimeError(f"Route has no route_outline: {route_key}")
    route_ctx = build_route_context(world_book, route_key)
    world_micro = build_world_micro(world_book)
    progress = load_progress(out_dir)
    state = json.dumps(progress.get("state", {}), ensure_ascii=False)
    previous_summary = progress.get("summary", "【这是本路线第一次生成】")

    route_outline_text = json.dumps(route_outline, ensure_ascii=False, indent=2)
    planner_prompt = fill_template(
        planner_template,
        {
            "world_micro": world_micro,
            "route": route_ctx,
            "route_outline": route_outline_text,
            "style_notes": "一次规划 1—2 章、24—40 个 block；每章 12—20 个 block；一场一个事件，玩家 2—4 个短选择。",
        },
    )

    if dry_run:
        out_dir.mkdir(parents=True, exist_ok=True)
        route_plan = fallback_route_plan(route_outline)
        writer_system = fill_template(
            writer_system_template,
            {
                "reference_style": "卢线：事件+密集短选择+动作旁白+短句；深空之恋：每个角色有自己的外部生活和欲望。",
                "preset_style": preset_style or "【未加载预设】",
                "world_micro": world_micro,
                "route": route_ctx,
                "state": state,
            },
        )
        writer_prompt = fill_template(
            writer_template,
            {
                "route": route_ctx,
                "route_outline": route_outline_text,
                "route_plan": json.dumps(route_plan, ensure_ascii=False, indent=2),
                "state": state,
                "world_micro": world_micro,
                "extra_instruction": "按路线计划写整条路线；不要把计划原文写进台词。",
            },
        )
        placeholder_route = {
            "route": route_key,
            "title": route_outline.get("title", ""),
            "summary": "",
            "chapters": [],
        }
        qa_prompt = fill_template(
            qa_template,
            {
                "route": route_ctx,
                "route_outline": route_outline_text,
                "route_plan": json.dumps(route_plan, ensure_ascii=False, indent=2),
                "world_micro": world_micro,
                "bundle": json.dumps(placeholder_route, ensure_ascii=False, indent=2),
            },
        )
        for name, text in (
            ("planner_prompt", planner_prompt),
            ("writer_system", writer_system),
            ("writer_prompt", writer_prompt),
            ("qa_prompt", qa_prompt),
        ):
            (out_dir / f"{route_key}_{name}.txt").write_text(text, encoding="utf-8")
        print(f"[dry-run] wrote prompts for route {route_key} to {out_dir}")
        return

    writer_cfg = config.get("writer") or {}
    qa_cfg = config.get("qa") or {}
    writer_key, writer_key_source = resolve_api_key(writer_cfg)
    qa_key, qa_key_source = resolve_api_key(qa_cfg)
    if not writer_key:
        raise RuntimeError("Writer API key missing: set WRITER_API_KEY or writer.api_key")
    if not qa_key:
        raise RuntimeError("QA API key missing: set QA_API_KEY or qa.api_key")
    if writer_key_source == "literal" or qa_key_source == "literal":
        print("[warn] an API key is stored as a literal in config; move it to an env var before committing.")
    generation = config.get("generation", {})
    temperature = float(generation.get("temperature", 0.9))
    max_tokens = int(generation.get("max_tokens", 12000))
    writer_timeout = float(writer_cfg.get("timeout_seconds", 240))
    qa_timeout = float(qa_cfg.get("timeout_seconds", 240))

    # 1) Route planner
    raw_plan = post_chat(
        writer_cfg["base_url"], writer_key, writer_cfg["model"],
        [{"role": "user", "content": planner_prompt}],
        temperature, max_tokens, writer_timeout,
    )
    planner_raw_path = out_dir / f"{route_key}_planner_raw.txt"
    planner_raw_path.write_text(raw_plan, encoding="utf-8")
    (out_dir / f"{route_key}_planner_meta.json").write_text(
        json.dumps(LAST_API_META, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    try:
        route_plan = robust_json_parse(raw_plan)
    except Exception as exc:
        print(f"[warn] planner JSON parse failed: {exc}; using fallback plan")
        route_plan = fallback_route_plan(route_outline)

    # 2) Route writer
    writer_system = fill_template(
        writer_system_template,
        {
            "reference_style": "卢线：事件+密集短选择+动作旁白+短句；深空之恋：每个角色有自己的外部生活和欲望。",
            "preset_style": preset_style or "【未加载预设】",
            "world_micro": world_micro,
            "route": route_ctx,
            "state": state,
        },
    )
    writer_prompt = fill_template(
        writer_template,
        {
            "route": route_ctx,
            "route_outline": route_outline_text,
            "route_plan": json.dumps(route_plan, ensure_ascii=False, indent=2),
            "state": state,
            "world_micro": world_micro,
            "extra_instruction": "按路线计划写整条路线；不要把计划原文写进台词。",
        },
    )
    raw_route = post_chat(
        writer_cfg["base_url"], writer_key, writer_cfg["model"],
        [
            {"role": "system", "content": writer_system},
            {"role": "user", "content": writer_prompt},
        ],
        temperature, max_tokens, writer_timeout,
    )
    writer_raw_path = out_dir / f"{route_key}_writer_raw.txt"
    writer_raw_path.write_text(raw_route, encoding="utf-8")
    (out_dir / f"{route_key}_writer_meta.json").write_text(
        json.dumps(LAST_API_META, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    try:
        route_bundle = robust_json_parse(raw_route)
    except Exception as exc:
        raise RuntimeError(
            f"writer JSON parse failed: {exc}; raw saved to {writer_raw_path}"
        ) from exc

    # 3) QA
    qa_prompt = fill_template(
        qa_template,
        {
            "route": route_ctx,
            "route_outline": route_outline_text,
            "route_plan": json.dumps(route_plan, ensure_ascii=False, indent=2),
            "world_micro": world_micro,
            "bundle": json.dumps(route_bundle, ensure_ascii=False, indent=2),
        },
    )
    raw_qa = post_chat(
        qa_cfg["base_url"], qa_key, qa_cfg["model"],
        [{"role": "user", "content": qa_prompt}],
        float(generation.get("qa_temperature", 0.1)), max_tokens, qa_timeout,
    )
    qa_raw_path = out_dir / f"{route_key}_qa_raw.txt"
    qa_raw_path.write_text(raw_qa, encoding="utf-8")
    (out_dir / f"{route_key}_qa_meta.json").write_text(
        json.dumps(LAST_API_META, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    try:
        qa_result = robust_json_parse(raw_qa)
    except Exception as exc:
        raise RuntimeError(
            f"QA JSON parse failed: {exc}; raw saved to {qa_raw_path}"
        ) from exc

    decision = qa_result.get("decision", "PASS")
    if decision == "REJECT":
        raise RuntimeError(f"QA rejected route: {qa_result.get('feedback', '')}")
    if decision == "FIXED" and qa_result.get("fixed_route"):
        route_bundle = qa_result["fixed_route"]
    state_delta = qa_result.get("state_delta", {})
    summary = qa_result.get("summary", route_bundle.get("summary", ""))

    warnings = validate_route(route_bundle)
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / f"{route_key}_route.json").write_text(
        json.dumps(route_bundle, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    (out_dir / f"{route_key}_qa.json").write_text(
        json.dumps(qa_result, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    markdown = assemble_route_markdown(route_bundle, state_delta)
    (out_dir / f"{route_key}.md").write_text(markdown, encoding="utf-8")

    progress["state"] = {**progress.get("state", {}), **state_delta}
    progress["summary"] = summary
    progress.setdefault("summaries", {})[route_key] = summary
    save_progress(out_dir, progress)

    print(f"[done] route {route_key}: {out_dir / f'{route_key}.md'}")
    print(f"[stats] blocks={sum(len(c.get('blocks', [])) for c in route_bundle.get('chapters', []))} chars={chinese_chars(route_text_blob(route_bundle))}")
    if warnings:
        print("[warnings]")
        for w in warnings:
            print("  -", w)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", default=str(DEFAULT_CONFIG))
    parser.add_argument("--route", default="normal")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--preset", help="path to SillyTavern preset JSON")
    args = parser.parse_args()

    config = load_json(Path(args.config))
    world_path = Path(config["paths"]["world_book"])
    if not world_path.is_absolute():
        world_path = ROOT / world_path
    world_book = load_json(world_path)

    preset_path = Path(args.preset) if args.preset else Path(config["paths"]["preset"])
    if not preset_path.is_absolute():
        preset_path = ROOT / preset_path
    preset_style = load_preset_style(preset_path)

    out_dir = Path(config["paths"]["output_dir"])
    if not out_dir.is_absolute():
        out_dir = ROOT / out_dir

    try:
        run_route(config, world_book, args.route, preset_style, args.dry_run, out_dir)
    except Exception as exc:
        print(f"[error] {args.route}: {exc}", file=sys.stderr)


if __name__ == "__main__":
    main()
