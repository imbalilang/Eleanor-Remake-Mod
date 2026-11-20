from __future__ import annotations

import copy
import csv
import difflib
import hashlib
import html
import json
import re
import sys
from pathlib import Path


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
SOURCE = ROOT / "Content/story_v06.json"
NAMES = {"eleanor": "埃莉诺", "lydia": "莉迪亚", "mother": "索菲娅", "player": "店主",
         "narrator": "旁白", "broker": "掮客", "thug": "黑市打手", "uncle": "自称叔叔的男人",
         "officer": "警官", "security_notice": "治安部通告", "news": "新闻"}
HIDDEN = {"c01.n003": {3}, "c02.n002": {0, 6}, "c03.n002": {4, 5},
          "c04.n002": {0}, "c04.b003": {0}, "c05.n002": {0, 3, 8},
          "c17.b101": {7, 10, 18}}
UNUSED = {"c17.b004", "c17.b005", "c19.b002", "c19.b003", "c19.b004", "c19.b005",
          "c20.b002", "c20.b003"}
SELECTORS = {
    "c01.n003": [lambda index: index != 3],
    "c02.n002": [lambda index: index > 0 and index != 6],
    "c03.n002": [lambda index: index < 4],
    "c04.n002": [lambda index: index >= 1],
    "c04.b003": [lambda index: index >= 1],
    "c04.n005": [lambda index: index <= 2, lambda index: index >= 3],
    "c05.n002": [lambda index: index not in (0, 3, 9)],
    "c10.n205": [lambda index: index < 9 or index > 13, lambda index: True],
    "c14.b003": [lambda index: index != 4, lambda index: True],
    "c17.b101": [lambda index: index <= 21 and index not in (7, 10, 18),
                 lambda index: index >= 22, lambda index: index >= 22 and index != 26],
    "c18.b101": [lambda index: index <= 23, lambda index: index <= 23 and index != 19,
                 lambda index: index <= 23 and (index < 17 or index > 20), lambda index: index >= 24],
    "c22.b005": [lambda index: index >= 1],
}


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def serialized(value):
    return json.dumps(value, ensure_ascii=False, indent=2) + "\n"


def save(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(serialized(value), encoding="utf-8")


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def blocks(document):
    return {block["id"]: block for chapter in document["chapters"] for block in chapter["blocks"]}


def key_for(identifier, index):
    return f"er_v06_{identifier.replace('.', '_')}_{index + 1:02d}"


def normalized(text):
    return re.sub(r"\W+", "", re.sub(r"<[^>]*>", "", text).replace("{0}", "800"))


def main():
    snapshot = HERE / "baseline/story_v06.json"
    if not snapshot.exists():
        snapshot.parent.mkdir(parents=True, exist_ok=True)
        snapshot.write_bytes(SOURCE.read_bytes())
        for path in sorted((ROOT / "tools").glob("en_v06_part*.json")):
            (snapshot.parent / path.name).write_bytes(path.read_bytes())
        (snapshot.parent / "EleanorStory.cs.txt").write_bytes((ROOT / "src/EleanorStory.cs").read_bytes())
    source = load(snapshot)
    candidate = copy.deepcopy(source)
    before = blocks(source)
    after = blocks(candidate)
    manifest = load(HERE / "edits.json")
    decisions = load(HERE / "chapter_review.json")
    replacements = {(entry["block"], entry["index"]): entry for entry in manifest["edits"]}
    assert len(replacements) == len(manifest["edits"])
    changed_blocks = {identifier for identifier, index in replacements}
    english_files = {path.name: load(path) for path in sorted(snapshot.parent.glob("en_v06_part*.json"))}
    revised_english = copy.deepcopy(english_files)
    translations = {}
    owners = {}
    for name, entries in english_files.items():
        for key, text in entries.items():
            assert key not in owners, key
            owners[key] = name
            translations[key] = text
    mapping = []
    expression_moves = []
    for identifier in sorted(changed_blocks):
        rows = copy.deepcopy(before[identifier]["lines"])
        for old_index, row in enumerate(rows):
            edit = replacements.get((identifier, old_index))
            if edit:
                assert row["text"] == edit["before"], (identifier, old_index)
                assert row["speaker"] != "player", (identifier, old_index)
                if edit.get("delete"):
                    if row.get("expr"):
                        destination = edit["move_expr_to"]
                        assert destination > old_index and "expr" not in rows[destination]
                        assert not replacements.get((identifier, destination), {}).get("delete")
                        rows[destination]["expr"] = row["expr"]
                        expression_moves.append({"block": identifier, "from_old_index": old_index,
                                                 "to_old_index": destination, "expr": row["expr"]})
                else:
                    assert edit.get("en")
                    assert not re.search(r"[\u4e00-\u9fff]", edit["en"])
                    row["text"] = edit["after"]
        survivors = []
        english_rows = []
        original_owners = set()
        for old_index, row in enumerate(rows):
            old_key = key_for(identifier, old_index)
            owner = owners[old_key]
            original_owners.add(owner)
            del revised_english[owner][old_key]
            edit = replacements.get((identifier, old_index), {})
            new_index = None if edit.get("delete") else len(survivors)
            mapping.append({"block": identifier, "old_index": old_index, "new_index": new_index,
                            "old_key": old_key, "new_key": None if new_index is None else key_for(identifier, new_index)})
            if new_index is not None:
                survivors.append(row)
                english_rows.append(edit.get("en", translations[old_key]))
        assert survivors and len(original_owners) == 1, (identifier, original_owners)
        owner = next(iter(original_owners))
        for new_index, text in enumerate(english_rows):
            revised_english[owner][key_for(identifier, new_index)] = text
        after[identifier]["lines"] = survivors
    for edit in manifest["metadata_edits"]:
        assert after[edit["block"]]["scene"] == edit["before"]
        after[edit["block"]]["scene"] = edit["after"]
    for identifier in before:
        if identifier not in changed_blocks:
            assert before[identifier]["lines"] == after[identifier]["lines"], identifier
        assert [row for row in before[identifier]["lines"] if row["speaker"] == "player"] == [
            row for row in after[identifier]["lines"] if row["speaker"] == "player"], identifier
        for key, value in before[identifier].items():
            if key not in ("lines", "scene"):
                assert after[identifier][key] == value
    for chapter_before, chapter_after in zip(source["chapters"], candidate["chapters"]):
        assert {key: value for key, value in chapter_before.items() if key != "blocks"} == {
            key: value for key, value in chapter_after.items() if key != "blocks"}
    selector_checks = 0
    for identifier, predicates in SELECTORS.items():
        row_map = [entry for entry in mapping if entry["block"] == identifier]
        if not row_map:
            row_map = [{"old_index": index, "new_index": index} for index in range(len(before[identifier]["lines"]))]
        for predicate in predicates:
            for entry in row_map:
                if entry["new_index"] is not None:
                    assert predicate(entry["old_index"]) == predicate(entry["new_index"]), (identifier, entry)
            selector_checks += 1
    assert before["c18.b101"]["lines"][13] == after["c18.b101"]["lines"][13]
    assert before["c10.n205"]["lines"][9]["text"] == "镯子呢。"
    assert before["c10.n205"]["lines"][13]["text"].startswith("她会回来拿的")
    original_text = (ROOT / "docs/原作Eleanor章节_对照稿_2026-11-10.md").read_text(encoding="utf-8-sig")
    official = {normalized(line.split("：", 1)[1]) for line in original_text.splitlines() if line.startswith("埃莉诺：")}
    for identifier, limit in (("c06.b101", 11), ("c07.b101", 6)):
        for row in before[identifier]["lines"][:limit]:
            assert normalized(row["text"]) in official, (identifier, row["text"])
    runtime = (snapshot.parent / "EleanorStory.cs.txt").read_text(encoding="utf-8-sig")
    assert "index => index <= 2" in runtime and "index => index >= 3" in runtime
    assert "index => index != 4 || RomanceState.Rescued" in runtime
    assert "index => held || index < 9 || index > 13" in runtime

    save(HERE / "story_v06.optimized.json", candidate)
    save(HERE / "line_map.json", {"index_base": 0, "rows": mapping, "expression_moves": expression_moves})
    replacements_by_path = {"Content/story_v06.json": serialized(candidate)}
    runtime_before = (snapshot.parent / "EleanorStory.cs.txt").read_text(encoding="utf-8-sig")
    runtime_after = runtime_before.replace("index => index != 0 && index != 3 && index != 9);",
                                           "index => index != 0 && index != 3 && index != 8);")
    assert runtime_before != runtime_after
    replacements_by_path["src/EleanorStory.cs"] = runtime_after
    for name, entries in revised_english.items():
        if entries != english_files[name]:
            ordered = {key: entries[key] for key in english_files[name] if key in entries}
            ordered.update({key: text for key, text in entries.items() if key not in ordered})
            replacements_by_path[f"tools/{name}"] = serialized(ordered)
    patch_parts = ["*** Begin Patch\n"]
    for relative, content in replacements_by_path.items():
        path = ROOT / relative
        baseline_path = snapshot if relative.startswith("Content/") else snapshot.parent / (path.name + ".txt" if relative.startswith("src/") else path.name)
        diff = list(difflib.unified_diff(baseline_path.read_text(encoding="utf-8-sig").splitlines(True), content.splitlines(True)))
        if diff:
            patch_parts.append(f"*** Update File: {path.as_posix()}\n")
            patch_parts.extend("@@\n" if line.startswith("@@") else line for line in diff[2:])
    patch_parts.append("*** End Patch\n")
    (HERE / "apply.patch").write_text("".join(patch_parts), encoding="utf-8")

    stats = []
    index_lines = ["# 共同线逐章分析与完成稿", "", "基线为已包含 C10／C19 关键句恢复稿的工作区。本轮按实际播放来源分析 C01—C23，分支分别阅读。原作借用以官方中文对照稿核对，不按题材相似就判为照搬。", "",
                   "正文行数包含互斥分支和保留的兼容块，不代表一次来访长度；不以删减比例评价文笔。详细结论见 [交付说明](REPORT.md)。", "",
                   "| 章节 | 判断 | 原行数 → 新行数 | 主要问题 |", "|---|---|---:|---|"]
    pages = ['<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>共同线逐章改稿对照</title>',
             '<style>body{font:17px/1.8 system-ui,sans-serif;background:#f5f3ed;color:#242a25;margin:0}main{max-width:1320px;padding:24px;margin:auto}table{border-collapse:collapse;width:100%;table-layout:fixed;background:white}th,td{border:1px solid #d5d8d0;padding:12px;vertical-align:top;overflow-wrap:anywhere}th{background:#e7ede5}.reason{font-size:14px;color:#5c625b}a{color:#285f4d}select{font:inherit;max-width:100%;padding:8px}.chapter{display:none}.chapter.active{display:block}.deleted{background:#faeee8}summary{cursor:pointer}h1{font-size:28px}small{color:#647064}@media(max-width:650px){main{padding:12px}td,th{padding:7px;font-size:14px}}</style><main>',
             '<h1>共同线：逐章分析与修改对照</h1><p>本页列出全部实际改动。只读全文见各章链接。C10 与 C19 已认可的告白内容保留；这里没有重新评分。</p><label>章节 <select id="chapter">']
    for chapter_index, chapter in enumerate(source["chapters"][:23]):
        pages.append(f'<option value="chapter-{chapter_index}">{chapter["id"].upper()} · {html.escape(chapter["title"])}</option>')
    pages.append('</select></label>')
    for chapter_index, (chapter_before, chapter_after) in enumerate(zip(source["chapters"][:23], candidate["chapters"][:23])):
        identifier = chapter_before["id"]
        decision = decisions[identifier]
        edits = [entry for entry in manifest["edits"] if entry["block"].startswith(identifier + ".")]
        old_rows = sum(len(block["lines"]) for block in chapter_before["blocks"])
        new_rows = sum(len(block["lines"]) for block in chapter_after["blocks"])
        old_chars = sum(len(row["text"]) for block in chapter_before["blocks"] for row in block["lines"])
        new_chars = sum(len(row["text"]) for block in chapter_after["blocks"] for row in block["lines"])
        stats.append({"chapter": identifier, "rows_before": old_rows, "rows_after": new_rows,
                      "characters_before": old_chars, "characters_after": new_chars,
                      "deleted": sum(bool(entry.get("delete")) for entry in edits),
                      "rewritten": sum(not entry.get("delete") for entry in edits), "decision": decision["decision"]})
        relative = f"chapters/{identifier}.md"
        index_lines.append(f'| [{identifier.upper()} {chapter_before["title"]}]({relative}) | {decision["decision"]} | {old_rows} → {new_rows} | {decision["problem"]} |')
        reading = [f'# {identifier.upper()} · {chapter_before["title"]}', "", f'**处理：{decision["decision"]}**', "",
                   f'来源判断：{decision["origin"]}', "", f'编辑判断：{decision["problem"]}', "",
                   f'保留重点：{decision["preserve"]}', "", "## 完成稿", "",
                   "按内容块区分选择、交易反应和条件分支，不能把互斥块连读。已省去代码明确过滤的行；交易、选项、价格和触发条件沿用当前实现。", ""]
        for block in chapter_after["blocks"]:
            if block["id"] in UNUSED:
                continue
            reading += [f'### {block["id"]} · {block.get("scene") or "对话"}', ""]
            if block["id"] == "c17.b101":
                reading += ["*保护安排下的来访；末段只在买到物品后播放，带走营养果还取决于实际购买。*", ""]
            if block["id"] == "c18.b101":
                reading += ["*莉迪亚陪同条件成立时播放；母亲买了哪些东西、是否按价签付钱依赖购买记录，末段收款只在成交后播放。*", ""]
            if block["id"] == "c14.b003":
                reading += ["*‘不过最后确实放我回来了’只在此前被捕并获救时播放。*", ""]
            for new_index, row in enumerate(block["lines"]):
                if new_index in HIDDEN.get(block["id"], set()):
                    continue
                reading += [f'{NAMES.get(row["speaker"], row["speaker"])}：{row["text"]}', ""]
        reading += ["## 本章修改记录", ""]
        if not edits:
            reading += ["正文保留。跳过原因见本页开头。", ""]
        for entry in edits:
            reading += [f'**{entry["block"]}，旧索引 {entry["index"]}**', "", f'原：{entry["before"]}', "",
                        "新：" + ("〔删除整行〕" if entry.get("delete") else entry["after"]), "", entry["reason"], ""]
        reading += ["[返回章节目录](../INDEX.md)", ""]
        output = HERE / relative
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text("\n".join(reading), encoding="utf-8")
        pages.append(f'<section id="chapter-{chapter_index}" class="chapter{" active" if chapter_index == 0 else ""}"><h2>{identifier.upper()} · {html.escape(chapter_before["title"])}</h2>')
        pages.append(f'<p><b>{decision["decision"]}</b> · {old_rows} → {new_rows} 行（含分支） · <a href="{relative}">阅读本章全文</a></p><p>{html.escape(decision["origin"])}</p><p>{html.escape(decision["problem"])}</p><p>保留：{html.escape(decision["preserve"])}</p>')
        if edits:
            pages.append('<table><tr><th>修改前</th><th>修改后与理由</th></tr>')
            for entry in edits:
                replacement = "〔删除整行〕" if entry.get("delete") else entry["after"]
                pages.append(f'<tr class="{"deleted" if entry.get("delete") else "edited"}"><td><small>{entry["block"]}:{entry["index"]}</small><br>{html.escape(entry["before"])}</td><td>{html.escape(replacement)}<p class="reason">{html.escape(entry["reason"])}</p></td></tr>')
            pages.append('</table>')
        else:
            pages.append('<p>本轮保留正文。</p>')
        pages.append('</section>')
    pages.append('<script>document.getElementById("chapter").addEventListener("change",function(){document.querySelectorAll(".chapter").forEach(section=>section.classList.toggle("active",section.id===this.value))});</script></main></html>')
    (HERE / "comparison.html").write_text("\n".join(pages), encoding="utf-8")
    (HERE / "INDEX.md").write_text("\n".join(index_lines) + "\n", encoding="utf-8")
    save(HERE / "metrics.json", {"definition": "整章内容行及字符含互斥、兼容块；不是一次来访长度，字符含标点和行内标签。", "chapters": stats})
    current = load(SOURCE)
    applying = current == source
    applied = current == candidate
    assert applying or applied, "Source changed since baseline; merge before applying"
    for relative, text in replacements_by_path.items():
        current_text = (ROOT / relative).read_text(encoding="utf-8-sig")
        baseline_path = snapshot if relative.startswith("Content/") else snapshot.parent / (Path(relative).name + ".txt" if relative.startswith("src/") else Path(relative).name)
        assert current_text == text or current_text == baseline_path.read_text(encoding="utf-8-sig"), relative
    verification = {"baseline_sha256": digest(snapshot), "candidate_sha256": digest(HERE / "story_v06.optimized.json"),
                    "edited_chapters": sum(bool(entry["deleted"] or entry["rewritten"]) for entry in stats),
                    "deleted_rows": sum(entry["deleted"] for entry in stats), "rewritten_rows": sum(entry["rewritten"] for entry in stats),
                    "metadata_label_updates": len(manifest["metadata_edits"]), "selector_checks": selector_checks,
                    "all_player_rows_preserved": True, "c06_original_rows_verified": 11, "c07_original_rows_verified": 6,
                    "c10_approved_all_rows_preserved": before["c10.n205"] == after["c10.n205"],
                    "c19_approved_confession_preserved": all(before[identifier] == after[identifier] for identifier in
                        ("c19.b101", "c19.b101_return", "c19.b102", "c19.b103")),
                    "expression_moves": expression_moves, "source_applied": applied,
                    "runtime_change": "C05 食物援助过滤第 8 行的固定营养果动作，恢复第 9 行保密请求；其余代码与基线一致。",
                    "external_model_calls": 0, "deployed_or_published_by_this_task": False}
    if "--verify" in sys.argv:
        assert applied
        assert (ROOT / "src/EleanorStory.cs").read_text(encoding="utf-8-sig") == runtime_after
        hardship = [row["text"] for index, row in enumerate(after["c05.n002"]["lines"]) if index not in (0, 3, 8)]
        assert hardship.index("尤其是后半段。") == hardship.index("今天的事，不许说出去。") + 1
        with (ROOT / "Localization/zh.csv").open(encoding="utf-8-sig", newline="") as stream:
            chinese = dict(csv.reader(stream))
        with (ROOT / "Localization/en.csv").open(encoding="utf-8-sig", newline="") as stream:
            english = dict(csv.reader(stream))
        merged_english = {key: text for entries in revised_english.values() for key, text in entries.items()}
        for identifier in after:
            for index, row in enumerate(after[identifier]["lines"]):
                key = key_for(identifier, index)
                assert chinese[key] == row["text"], key
                assert english[key] == merged_english[key], key
        for identifier in changed_blocks:
            for index in range(len(after[identifier]["lines"]), len(before[identifier]["lines"])):
                assert key_for(identifier, index) not in chinese and key_for(identifier, index) not in english
        verification["generated_chinese_and_english_verified"] = True
    save(HERE / "verification.json", verification)
    print(json.dumps(verification, ensure_ascii=True, indent=2))


if __name__ == "__main__":
    main()
