from __future__ import annotations

import copy
import csv
import html
import json
import sys
from pathlib import Path


HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
import build_review as previous


def save(name, value):
    (HERE / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def references_for(blocks):
    paths = {}

    def append(name, identifiers, held=True):
        paths[name] = [
            (identifier, row_index)
            for identifier in identifiers
            for row_index in range(len(blocks[identifier]["lines"]))
            if held or identifier != "c10.n205" or not 10 <= row_index <= 14
        ]

    for route, head in (("归还研究", "c19.b101_return"), ("保护", "c19.b101")):
        for outcome, tail in (("接受", "c19.b102"), ("朋友", "c19.b103")):
            append(f"C19 · {route} · {outcome}", [head, tail])
    for label, branch in (("直说", "c10.n202"), ("只说来过", "c10.n203"), ("反问认识", "c10.n204")):
        for held in (True, False):
            append(f"C10 · 埃莉诺 · {label} · {'持有镯子' if held else '已无镯子'}",
                   ["c10.n201", branch, "c10.n205"], held)
    for label, identifiers in {
        "掮客 · 不成交": ["c10.n206", "c10.n207"],
        "掮客 · 成交": ["c10.n206", "c10.n208"],
        "莉迪亚 · 原价回收": ["c10.n209", "c10.n210", "c10.n213"],
        "莉迪亚 · 直接赠送": ["c10.n209", "c10.n211", "c10.n213"],
        "莉迪亚 · 自报五百后成交": ["c10.n209", "c10.n212", "c10.n214", "c10.n213"],
        "莉迪亚 · 自报五百后未成交": ["c10.n209", "c10.n212"],
    }.items():
        append(f"C10 · {label}", identifiers)
    return paths


def path_rows(blocks, references, removed):
    rows = []
    entered = set()
    for identifier, old_index in references:
        if (identifier, old_index) in removed:
            continue
        row = dict(blocks[identifier]["lines"][old_index])
        row["break_before"] = identifier not in entered and identifier in previous.INTERACTION_ENTRIES
        entered.add(identifier)
        rows.append(row)
    return rows


def main():
    manifest = previous.load(HERE / "deletions.json")
    v1_path = HERE.parent / "story_v06.agent.zh-review.json"
    assert previous.digest(previous.SOURCE) == manifest["source_sha256"]
    assert previous.digest(v1_path) == manifest["v1_sha256"]
    source = previous.load(previous.SOURCE)
    v1 = previous.load(v1_path)
    original = previous.all_blocks(source)
    before = previous.all_blocks(v1)
    candidate = copy.deepcopy(v1)
    after = previous.all_blocks(candidate)
    targets = [identifier for identifier in before if identifier.startswith("c10.n") or identifier in {
        "c19.b101", "c19.b101_return", "c19.b102", "c19.b103",
    }]
    removed = {}
    for deletion in manifest["deletions"]:
        for identifier in deletion["blocks"]:
            reference = (identifier, deletion["index"])
            assert reference not in removed and identifier in targets
            row = before[identifier]["lines"][deletion["index"]]
            assert row["text"] == deletion["text"], reference
            assert row["speaker"] != "player", reference
            removed[reference] = deletion
    mapping = []
    for identifier in targets:
        surviving = []
        for old_index, row in enumerate(before[identifier]["lines"]):
            deleted = (identifier, old_index) in removed
            new_index = None if deleted else len(surviving)
            mapping.append({
                "block": identifier, "old_index": old_index, "new_index": new_index,
                "old_key": f"er_v06_{identifier.replace('.', '_')}_{old_index + 1:02d}",
                "new_key": None if deleted else f"er_v06_{identifier.replace('.', '_')}_{new_index + 1:02d}",
                "deleted": deleted, "text": row["text"], "removed_expr": row.get("expr") if deleted else None,
            })
            if not deleted:
                surviving.append(copy.deepcopy(row))
        assert surviving, identifier
        after[identifier]["lines"] = surviving
    reconstructed = copy.deepcopy(candidate)
    reconstructed_blocks = previous.all_blocks(reconstructed)
    for identifier in targets:
        for old_index, row in enumerate(before[identifier]["lines"]):
            if (identifier, old_index) in removed:
                reconstructed_blocks[identifier]["lines"].insert(old_index, copy.deepcopy(row))
    assert reconstructed == v1, "Unexpected non-deletion change"
    for identifier in ("c19.b101", "c19.b101_return"):
        for old_index in (3, 5, 7, 8, 9, 10, 11, 12, 13):
            assert (identifier, old_index) not in removed
        assert before[identifier]["lines"][12] == original[identifier]["lines"][12]
        assert before[identifier]["lines"][13] == original[identifier]["lines"][13]
    assert before["c19.b101"]["lines"][3] == original["c19.b101"]["lines"][3]
    assert before["c19.b101_return"]["lines"][3] == original["c19.b101_return"]["lines"][3]
    assert "看守" not in before["c19.b101_return"]["lines"][3]["text"]
    assert after["c19.b102"] == before["c19.b102"]
    assert after["c19.b103"] == before["c19.b103"]
    for old_index in range(14):
        assert (("c19.b101", old_index) in removed) == (("c19.b101_return", old_index) in removed)
        if old_index != 3:
            assert before["c19.b101"]["lines"][old_index]["text"] == before["c19.b101_return"]["lines"][old_index]["text"]
    assert ("c10.n213", 20) not in removed
    assert ("c10.n213", 11) not in removed

    release_csv = previous.RELEASE / "EleanorRomance/Localization/zh.csv"
    release_files = previous.load(previous.RELEASE / "release-files.json")
    release_entry = next(entry for entry in release_files if entry["path"].replace("\\", "/") == "Localization/zh.csv")
    assert previous.digest(release_csv) == release_entry["sha256"].lower()
    receipt = previous.load(previous.RELEASE / "publish-result.json")
    assert receipt["version"] == "1.13" and receipt["result"] == "k_EResultOK"
    with release_csv.open(encoding="utf-8-sig", newline="") as stream:
        published = dict(csv.reader(stream))
    for row in mapping:
        assert published[row["old_key"]] == original[row["block"]]["lines"][row["old_index"]]["text"]

    paths = references_for(before)
    counts = {}
    for name, references in paths.items():
        counts[name] = {
            "published": previous.metrics(path_rows(original, references, {})),
            "agent_v1": previous.metrics(path_rows(before, references, {})),
            "agent_v2": previous.metrics(path_rows(before, references, removed)),
        }
        assert counts[name]["published"]["player_bubbles"] == counts[name]["agent_v2"]["player_bubbles"]
    conditional = [row for row in mapping if row["block"] == "c10.n205" and 10 <= row["old_index"] <= 14 and not row["deleted"]]
    assert [row["new_index"] for row in conditional] == [9, 10, 11, 12]
    for name, references in paths.items():
        if "已无镯子" in name:
            assert not any(identifier == "c10.n205" and 10 <= old_index <= 14 for identifier, old_index in references)
        if "自报五百" in name:
            first_offer = next(row for row in path_rows(before, references, removed)
                               if row["text"] == before["c10.n212"]["lines"][1]["text"])
            assert first_offer["break_before"]

    save("story_v06.agent-deletion.zh-review.json", candidate)
    save("line_map.json", {"index_base": 0, "rows": mapping,
         "n205_no_bracelet_skip_new_indices": [row["new_index"] for row in conditional]})
    save("metrics.json", {"definition": "text 行为显示气泡；字符含标点，不含空白及标签。NPC 连续数在玩家对白、选择、原生交易处重置，另列旁白是否重置两种口径。",
         "coverage": "16 条内容路径；不计运行时 wait/nosale/missing、交易 UI、读档续接、C19 b001 和解前缀及 b006 不告白。不是实机结果。",
         "paths": counts})
    expression_losses = [row for row in mapping if row["removed_expr"]]
    save("verification.json", {
        "source_sha256": previous.digest(previous.SOURCE), "v1_sha256": previous.digest(v1_path),
        "candidate_sha256": previous.digest(HERE / "story_v06.agent-deletion.zh-review.json"),
        "release_csv_sha256": previous.digest(release_csv), "release_matched_rows": len(mapping),
        "release_version": receipt["version"], "release_upload_completed_utc": receipt["completedUtc"],
        "deleted_rows": len(removed), "deleted_by_scene": {
            scene: sum(identifier.startswith(scene + ".") for identifier, old_index in removed)
            for scene in ("c10", "c19")},
        "remaining_target_rows": len(mapping) - len(removed),
        "surviving_rows_all_fields_unchanged_from_v1": True,
        "reinserting_deleted_rows_reconstructs_v1_exactly": True,
        "player_rows_preserved_all_16_paths": True,
        "route_specific_mother_and_locked_confession_preserved": True,
        "expression_losses": expression_losses,
        "n205_conditional_requires_remap": {"old_skip": [10, 11, 12, 13, 14], "new_skip": [9, 10, 11, 12]},
        "production_applied": False, "english_revised": False, "new_external_model_calls": 0,
        "semantic_review": "同一编辑自审；自动核对只验证删行、来源、索引及统计，不能证明文学质量或实机兼容。",
    })

    readable = ["# 允许删整句后的修订稿", "", "这是上一版的删减稿：没有补写或替换任何一句。以下四段各是一条完整阅读路径，互斥分支另见三版对照。中文审阅候选，未接入游戏。", ""]
    selected = ["C19 · 归还研究 · 接受", "C10 · 埃莉诺 · 直说 · 持有镯子", "C10 · 掮客 · 不成交", "C10 · 莉迪亚 · 原价回收"]
    for name in selected:
        readable += [f"## {name}", ""]
        for row in path_rows(before, paths[name], removed):
            if row.get("break_before"):
                readable += ["*〔玩家选择／交易结果后继续〕*", ""]
            speaker = previous.NAMES.get(row["speaker"], row["speaker"])
            readable += [f"{speaker}：{row['text']}", ""]
    readable += ["## 其他分支", "", "C19 保护入口仍保留看守点名；归还入口没有看守。朋友结尾、接受后的全部对白和亲密动作保持上一版。C10 的各项选择和交易结果分别列在三版对照中，没有把互斥台词接在一起。", "",
                 "[查看结论、删减代价与接入注意事项](REPORT.md) · [三版全文对照](comparison.html)", ""]
    (HERE / "READABLE.md").write_text("\n".join(readable), encoding="utf-8")

    pages = ['<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>整句删减：三版全文对照</title>',
        '<style>body{margin:0;background:#f4f2eb;color:#252925;font:17px/1.8 system-ui,sans-serif}main{max-width:1500px;margin:auto;padding:28px}h1{font-size:30px}select,button{font:inherit;padding:8px;max-width:100%}a{color:#285e50}.note{background:#e5ede5;padding:16px}table{border-collapse:collapse;width:100%;table-layout:fixed;background:white}td,th{border:1px solid #d7d7cf;vertical-align:top;padding:12px}th{background:#ecebe4}small{color:#686e68}.removed{background:#f8ece6}.removed strong{color:#864b35}.boundary{text-align:center;background:#ecebe4;font-size:14px}.path{display:none}.path.active{display:block}body.hide-deleted .removed{display:none}@media(max-width:750px){main{padding:12px}td,th{padding:7px;font-size:14px}}</style><main>',
        '<h1>允许删整句之后，节奏有没有改善？</h1><p class="note">同一批内容的发布原文、上一版 Agent 改稿、本次删减稿。删减稿只删除整条对白或旁白，未补写任何一句。数字描述阅读长度，不等于文学评分。各选择路径单独呈现。</p>',
        '<p><a href="READABLE.md">只读成稿</a> · <a href="REPORT.md">结论与代价</a> · <a href="../comparison.html">此前管线全文对照</a></p>',
        '<label>阅读路径 <select id="path">']
    for path_index, name in enumerate(paths):
        pages.append(f'<option value="path-{path_index}">{html.escape(name)}</option>')
    pages += ['</select></label><p>三列始终保留各版本的全部文字；本次列标明删除处。连续阅读删减后的成稿，请使用上方“只读成稿”。</p>']
    for path_index, (name, references) in enumerate(paths.items()):
        pages.append(f'<section id="path-{path_index}" class="path{" active" if path_index == 0 else ""}"><h2>{html.escape(name)}</h2>')
        pages.append('<table><thead><tr><th>1.13 发布原文</th><th>Agent 上一版：只改措辞</th><th>Agent 本次：允许删整句</th></tr></thead><tbody>')
        summaries = []
        for version in ("published", "agent_v1", "agent_v2"):
            count = counts[name][version]
            summaries.append(f'{count["displayed_rows"]} 个显示气泡 · {count["visible_characters_including_punctuation"]} 字符 · 最长 NPC 连续 {count["longest_npc_run_ignoring_narration"]} 条对白')
        pages.append('<tr>' + ''.join(f'<td>{summary}</td>' for summary in summaries) + '</tr>')
        entered = set()
        for identifier, old_index in references:
            if identifier not in entered and identifier in previous.INTERACTION_ENTRIES:
                pages.append('<tr class="boundary"><td colspan="3">玩家选择／原生交易结果后继续</td></tr>')
            entered.add(identifier)
            row = before[identifier]["lines"][old_index]
            deletion = removed.get((identifier, old_index))
            speaker = html.escape(previous.NAMES.get(row["speaker"], row["speaker"]))
            pages.append(f'<tr class="{"removed" if deletion else "kept"}"><td><small>{identifier}:{old_index:02d} · {speaker}</small><br>{html.escape(original[identifier]["lines"][old_index]["text"])}</td><td>{html.escape(row["text"])}</td>')
            if deletion:
                pages.append(f'<td><strong>删除</strong><br>{html.escape(deletion["reason"])}<br><small>代价：{html.escape(deletion["cost"])}</small></td></tr>')
            else:
                pages.append(f'<td>{html.escape(row["text"])}</td></tr>')
        pages.append('</tbody></table></section>')
    pages.append('<script>document.getElementById("path").addEventListener("change",function(){document.querySelectorAll(".path").forEach(section=>section.classList.toggle("active",section.id===this.value))});</script></main></html>')
    (HERE / "comparison.html").write_text("\n".join(pages), encoding="utf-8")
    assert previous.digest(previous.SOURCE) == manifest["source_sha256"]
    assert previous.digest(v1_path) == manifest["v1_sha256"]
    print(json.dumps({"deleted_rows": len(removed), "retained_rows": len(mapping) - len(removed),
                      "expression_losses": len(expression_losses), "validated_paths": len(paths),
                      "selected_metrics": {name: counts[name] for name in selected},
                      "production_source_and_v1_unchanged": True}, ensure_ascii=True, indent=2))


if __name__ == "__main__":
    main()
