from __future__ import annotations

import copy
import csv
import difflib
import hashlib
import html
import json
import re
from pathlib import Path


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
HISTORY = ROOT / "tools/NovelWorkFlow_Eleanor_Test/Test_Output"
SOURCE = ROOT / "Content/story_v06.json"
RELEASE = ROOT / "dist/workshop-1.13"
NAMES = {"eleanor": "埃莉诺", "player": "店主", "narrator": "旁白", "broker": "黑市掮客", "lydia": "莉迪亚"}
INTERACTION_ENTRIES = {
    "c10.n202", "c10.n203", "c10.n204", "c10.n207", "c10.n208", "c10.n210",
    "c10.n211", "c10.n212", "c10.n214", "c19.b102", "c19.b103",
}


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save(name, data):
    (HERE / name).write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def all_blocks(document):
    return {block["id"]: block for chapter in document["chapters"] for block in chapter["blocks"]}


def visible(text):
    return re.sub(r"\s|<[^>]+>", "", text)


def metrics(rows):
    npc_run = 0
    max_run = 0
    adjacent_run = 0
    max_adjacent = 0
    previous = None
    adjacent_previous = None
    for row in rows:
        if row.get("break_before"):
            npc_run = adjacent_run = 0
            previous = adjacent_previous = None
        speaker = row["speaker"]
        if speaker == "narrator":
            adjacent_run = 0
            adjacent_previous = None
            continue
        if speaker == "player":
            npc_run = adjacent_run = 0
            previous = adjacent_previous = None
            continue
        npc_run = npc_run + 1 if previous == speaker else 1
        adjacent_run = adjacent_run + 1 if adjacent_previous == speaker else 1
        max_run = max(max_run, npc_run)
        max_adjacent = max(max_adjacent, adjacent_run)
        previous = adjacent_previous = speaker
    return {
        "displayed_rows": len(rows),
        "dialogue_bubbles": sum(row["speaker"] != "narrator" for row in rows),
        "narration_bubbles": sum(row["speaker"] == "narrator" for row in rows),
        "player_bubbles": sum(row["speaker"] == "player" for row in rows),
        "visible_characters_including_punctuation": sum(len(visible(row["text"])) for row in rows),
        "longest_npc_run_ignoring_narration": max_run,
        "longest_npc_run_reset_by_narration": max_adjacent,
    }


def mapped_rows(blocks, references):
    return [dict(blocks[identifier]["lines"][row_index],
                 break_before=(row_index == 0 and identifier in INTERACTION_ENTRIES))
            for identifier, row_index in references]


def primary_script_rows(text):
    rows = []
    sections = {}
    current = "未命名"
    pending_break = False
    reverse = {name: key for key, name in NAMES.items()}
    reverse["玩家"] = "player"
    for line in text.splitlines():
        if line.strip() == "<分支附录>":
            break
        match = re.fullmatch(r"\[([^\n]+)\]", line.strip())
        if not match:
            continue
        fields = match[1].split("|")
        if fields[0] == "章":
            current = fields[1]
            sections.setdefault(current, [])
            pending_break = True
        elif fields[0] == "/选择":
            pending_break = True
        elif fields[0] == "旁白":
            row = {"speaker": "narrator", "text": "|".join(fields[1:]), "break_before": pending_break}
            pending_break = False
            rows.append(row)
            sections.setdefault(current, []).append(row)
        elif len(fields) >= 3 and fields[1] == "对白":
            row = {"speaker": reverse.get(fields[0], fields[0]), "text": "|".join(fields[2:]), "break_before": pending_break}
            pending_break = False
            rows.append(row)
            sections.setdefault(current, []).append(row)
    return rows, sections


def main():
    source = load(SOURCE)
    source_hash = digest(SOURCE)
    before = all_blocks(source)
    edits = load(HERE / "agent_edits.json")["edits"]
    candidate = copy.deepcopy(source)
    after = all_blocks(candidate)
    targets = [identifier for identifier in before if identifier.startswith("c10.n") or identifier in {
        "c19.b101", "c19.b101_return", "c19.b102", "c19.b103"
    }]
    seen = set()
    for edit in edits:
        identifier = (edit["block"], edit["index"])
        assert identifier not in seen, identifier
        seen.add(identifier)
        row = after[edit["block"]]["lines"][edit["index"]]
        assert row["text"] == edit["before"], identifier
        assert edit["block"] in targets, identifier
        row["text"] = edit["after"]
    restored = copy.deepcopy(candidate)
    restored_blocks = all_blocks(restored)
    for edit in edits:
        restored_blocks[edit["block"]]["lines"][edit["index"]]["text"] = edit["before"]
    assert restored == source, "Unexpected change outside authorized Chinese text"
    for identifier in ("c19.b101", "c19.b101_return"):
        for row_index in (3, 4, 5, 11, 12, 13):
            assert after[identifier]["lines"][row_index] == before[identifier]["lines"][row_index]
    assert "看守" not in after["c19.b101_return"]["lines"][3]["text"]
    assert after["c19.b101"]["lines"][3]["text"] == before["c19.b101"]["lines"][3]["text"]
    assert after["c19.b103"] == before["c19.b103"]
    for row_index in range(14):
        if row_index != 3:
            assert after["c19.b101"]["lines"][row_index]["text"] == after["c19.b101_return"]["lines"][row_index]["text"]

    release_csv = RELEASE / "EleanorRomance/Localization/zh.csv"
    with release_csv.open(encoding="utf-8-sig", newline="") as stream:
        published = dict(csv.reader(stream))
    release_files = load(RELEASE / "release-files.json")
    released_entry = next(entry for entry in release_files if entry["path"].replace("\\", "/") == "Localization/zh.csv")
    assert digest(release_csv) == released_entry["sha256"].lower()
    receipt = load(RELEASE / "publish-result.json")
    assert receipt["version"] == "1.13" and receipt["result"] == "k_EResultOK"
    matched = []
    for identifier in targets:
        for row_index, row in enumerate(before[identifier]["lines"]):
            key = f"er_v06_{identifier.replace('.', '_')}_{row_index + 1:02d}"
            assert published[key] == row["text"], key
            matched.append(key)

    source_paths = {"c10": {}, "c19": {}}
    for variant, branch in (("直说", "c10.n202"), ("只说来过", "c10.n203"), ("反问认识", "c10.n204")):
        for held in (True, False):
            suffix_indices = [row_index for row_index in range(26) if held or row_index < 10 or row_index > 14]
            source_paths["c10"][f"埃莉诺/{variant}/{'持有镯子' if held else '已无镯子'}"] = [
                ("c10.n201", row_index) for row_index in range(len(before["c10.n201"]["lines"]))
            ] + [(branch, row_index) for row_index in range(len(before[branch]["lines"]))] + [
                ("c10.n205", row_index) for row_index in suffix_indices
            ]
    for label, identifiers in {
        "掮客/不成交": ["c10.n206", "c10.n207"],
        "掮客/成交": ["c10.n206", "c10.n208"],
        "莉迪亚/原价回收": ["c10.n209", "c10.n210", "c10.n213"],
        "莉迪亚/直接赠送": ["c10.n209", "c10.n211", "c10.n213"],
        "莉迪亚/自报五百后成交": ["c10.n209", "c10.n212", "c10.n214", "c10.n213"],
        "莉迪亚/自报五百后未成交": ["c10.n209", "c10.n212"],
    }.items():
        source_paths["c10"][label] = [(identifier, row_index) for identifier in identifiers for row_index in range(len(before[identifier]["lines"]))]
    for route, head in (("归还研究", "c19.b101_return"), ("保护", "c19.b101")):
        for outcome, tail in (("接受", "c19.b102"), ("朋友", "c19.b103")):
            identifiers = [head, tail]
            source_paths["c19"][f"{route}/{outcome}"] = [(identifier, row_index) for identifier in identifiers for row_index in range(len(before[identifier]["lines"]))]
    counts = {}
    for scene, paths in source_paths.items():
        counts[scene] = {}
        for path_name, references in paths.items():
            counts[scene][path_name] = {
                "original": metrics(mapped_rows(before, references)),
                "agent": metrics(mapped_rows(after, references)),
            }

    pipeline_specs = [
        ("c10_production_opus", "c10", "PRODUCTION_AI_REDUCTION_2026-11-17/c10/B_opus/candidate_script.txt"),
        ("c19_production_opus", "c19", "PRODUCTION_AI_REDUCTION_2026-11-17/c19/B_opus/candidate_script.txt"),
        ("c19_pilot_opus", "c19", "PILOT_2026-11-16/c19/B_opus/candidate_script.txt"),
        ("c19_pilot_gemini", "c19", "PILOT_2026-11-16/c19/B_gemini/candidate_script.txt"),
    ]
    pipeline_data = {}
    provenance = []
    for label, scene, relative in pipeline_specs:
        origin = HISTORY / relative
        text = origin.read_text(encoding="utf-8-sig")
        rows, sections = primary_script_rows(text)
        snapshot = f"{label}.txt"
        (HERE / snapshot).write_bytes(origin.read_bytes())
        pipeline_data[label] = {
            "scene": scene, "snapshot": snapshot,
            "primary_path_metrics": metrics(rows) if scene == "c19" else None,
            "section_metrics": {name: metrics(section_rows) for name, section_rows in sections.items()},
            "text": text,
        }
        provenance.append({"source": origin.relative_to(ROOT).as_posix(), "sha256": digest(origin)})
    qa_origin = HISTORY / "POINT_EDIT_2026-11-17/c19/C_superqa/candidate_raw.json"
    qa = load(qa_origin)
    qa_rows = {(row["block"], row["index"]): row for row in qa["lines"]}
    save("c19_point_qa.json", qa)
    provenance.append({"source": qa_origin.relative_to(ROOT).as_posix(), "sha256": digest(qa_origin)})
    qa_metrics = {name: metrics([dict(qa_rows[(identifier, row_index)],
                                    break_before=(row_index == 0 and identifier in INTERACTION_ENTRIES))
                               for identifier, row_index in references])
                  for name, references in source_paths["c19"].items()}

    save("story_v06.agent.zh-review.json", candidate)
    candidate_path = HERE / "story_v06.agent.zh-review.json"
    (HERE / "agent_vs_source.diff").write_text("".join(difflib.unified_diff(
        SOURCE.read_text(encoding="utf-8").splitlines(keepends=True),
        candidate_path.read_text(encoding="utf-8").splitlines(keepends=True),
        fromfile="a/Content/story_v06.json", tofile="b/Content/story_v06.json"
    )), encoding="utf-8")
    save("metrics.json", {
        "definition": "一个源 text 行计一个显示气泡；表情标签、选择元数据不计；字符统计含标点，不含空白与 HTML 标签；NPC 连续数在玩家发言、选择或原生交易边界归零，提供忽略旁白/遇旁白归零两种口径。",
        "coverage": "c10 六条埃莉诺路径、两条掮客路径、四条莉迪亚路径；不计原生交易 UI、运行时 wait/nosale/missing 提示及重载续接。c19 四条主要告白路径；不计条件性 b001 和解前缀与不告白 b006，二者未改。",
        "path_counts": counts,
        "pipeline": {label: {key: value for key, value in entry.items() if key != "text"} for label, entry in pipeline_data.items()},
        "point_qa": qa_metrics,
    })
    save("verification.json", {
        "structural_checks_passed": True,
        "published_version": receipt["version"],
        "publication_utc": receipt["completedUtc"],
        "publication_receipt": "dist/workshop-1.13/publish-result.json",
        "publication_receipt_sha256": digest(RELEASE / "publish-result.json"),
        "release_csv_sha256": digest(release_csv),
        "source_sha256": source_hash,
        "candidate_sha256": digest(candidate_path),
        "release_text_rows_matched": len(matched),
        "changed_rows": len(edits),
        "changed_by_scene": {scene: sum(edit["block"].startswith(scene + ".") for edit in edits) for scene in ("c10", "c19")},
        "all_non_text_fields_unchanged": True,
        "locked_confession_and_route_specific_mother_lines_unchanged": True,
        "published_text_basis": "上传成功回执对应的本地 1.13 发布包；本次没有重新下载 Steam 内容或重新请求线上详情。",
        "semantic_review": "由同一编辑逐项自审，见 REPORT.md；结构校验不能证明语义正确或文笔更好。",
        "production_applied": False,
        "english_revised": False,
        "new_external_model_calls": 0,
        "historical_sources": provenance,
    })
    for scene in ("c19", "c10"):
        lines = [f"# {scene}：Agent 直接修订稿", "", "中文审阅候选。每个分支分开列出，不应把互斥内容连续朗读。行号从 0 起。英文沿用基线，尚未同步；此副本不能直接发布。", ""]
        for identifier in targets:
            if not identifier.startswith(scene + "."):
                continue
            block = after[identifier]
            lines += [f"## {identifier} · {block.get('scene', '')}", ""]
            for row_index, row in enumerate(block["lines"]):
                marker = "（改）" if (identifier, row_index) in seen else ""
                lines += [f"{row_index:02d} {NAMES.get(row['speaker'], row['speaker'])}{marker}：{row['text']}", ""]
        (HERE / f"{scene}_agent_readable.md").write_text("\n".join(lines), encoding="utf-8")

    pages = ['<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>原文、管线稿与 Agent 修订对照</title>',
        '<style>body{font:17px/1.7 system-ui,sans-serif;color:#242933;background:#f6f4ee;margin:0;padding:28px}main{max-width:1480px;margin:auto}h1{font-size:30px}h2{margin-top:40px}table{border-collapse:collapse;width:100%;table-layout:fixed;background:#fff}th,td{padding:12px;border:1px solid #dcdad3;vertical-align:top;overflow-wrap:anywhere}th{background:#eeece5}td small{color:#777}.changed{background:#eef5ed}pre{white-space:pre-wrap;font:16px/1.7 system-ui}summary{cursor:pointer;font-weight:600}details{background:white;padding:16px;margin:12px 0}.note{padding:16px;border-left:4px solid #507665;background:#eaf0e8}a{color:#265d52}@media(max-width:700px){body{padding:10px}th,td{padding:7px;font-size:14px}}</style><main>',
        '<h1>直接改稿后，AI 感有没有降低？</h1><p>2026-11-17 · 中文对照 · 作者已授权 Agent 直接修订 · 未发布</p>',
        '<p class="note">这里提供完整文本供核对。绿色仅表示改动，不表示质量得分。原文来自已上传成功的 1.13 发布包，与当前源中这两场的 162 行逐一一致。管线稿保留原样；本次稿保留行数、说话人和分支，因此长独白数量没有下降。没有独立读者盲评。</p>',
        '<p><a href="REPORT.md">阅读结论与事实核查</a> · <a href="c19_agent_readable.md">只读告白修订稿</a> · <a href="c10_agent_readable.md">只读三次来访修订稿</a></p>',
        '<label><input id="changes" type="checkbox"> 只显示 Agent 改过的行（取消后查看完整上下文）</label>']
    for scene in ("c19", "c10"):
        pages.append(f'<h2>{scene}：发布原文与本次修订</h2>')
        if scene == "c19":
            pages.append('<p>中列是最近一次 Super QA 定点稿。保护入口 b101 与归还入口 b101_return 的母亲处境不同；两个入口只读其一，接受/朋友也只读其一。</p>')
        else:
            pages.append('<p>n201–n205 为埃莉诺来访；n206–n208 为掮客；n209–n214 为莉迪亚。整场 Opus 重新组织了结构，不能伪造逐行对应，全文列在本节末尾。</p>')
        for identifier in targets:
            if not identifier.startswith(scene + "."):
                continue
            pages.append(f'<h3>{html.escape(identifier)} · {html.escape(before[identifier].get("scene", ""))}</h3><table><tr><th>发布原文</th>')
            if scene == "c19":
                pages.append('<th>最近定点 Super QA</th>')
            pages.append('<th>Agent 直接修订</th></tr>')
            for row_index, row in enumerate(before[identifier]["lines"]):
                changed = (identifier, row_index) in seen
                pages.append(f'<tr class="{"edited" if changed else "same"}"><td><small>{row_index:02d} {html.escape(NAMES.get(row["speaker"], row["speaker"]))}</small><br>{html.escape(row["text"])}</td>')
                if scene == "c19":
                    qa_text = qa_rows[(identifier, row_index)]["text"]
                    pages.append(f'<td>{html.escape(qa_text)}</td>')
                pages.append(f'<td class="{"changed" if changed else ""}">{html.escape(after[identifier]["lines"][row_index]["text"])}</td></tr>')
            pages.append('</table>')
        for label, entry in pipeline_data.items():
            if entry["scene"] == scene:
                pages.append(f'<details><summary>此前管线完整稿：{html.escape(label)}</summary><p>保持历史原样，包含分支附录。标题不等于新增运行来访；具体机制差异见报告。</p><pre>{html.escape(entry["text"])}</pre></details>')
    pages.append('<script>document.getElementById("changes").addEventListener("change",function(){document.querySelectorAll("tr.same").forEach(row=>row.hidden=this.checked)});</script></main></html>')
    (HERE / "comparison.html").write_text("\n".join(pages), encoding="utf-8")
    assert digest(SOURCE) == source_hash, "Production source changed during review"
    print(json.dumps({"matched_published_rows": len(matched), "edited_rows": len(edits),
                      "changed_by_scene": {scene: sum(edit["block"].startswith(scene + ".") for edit in edits) for scene in ("c10", "c19")},
                      "candidate": str(candidate_path), "structural_checks_passed": True,
                      "production_source_unchanged": True}, ensure_ascii=True))


if __name__ == "__main__":
    main()
