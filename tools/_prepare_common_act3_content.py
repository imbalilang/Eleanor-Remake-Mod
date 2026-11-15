#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Parse the new common Act 3 draft into Content-ready blocks (chapters c12-c14).

Chapter 1 / c11 is the shared 800 + attendant entry and is intentionally not
touched here. The new draft's chapters 2-5 map to c12-c14:
- chapter 2 -> c12 (data drive)
- chapter 3 -> c13 (threat)
- chapter 4 -> c14 decision
- chapter 5 -> c14 aftermath (same visit, condensed to the visit cap)
"""
from __future__ import annotations

import json
import re
from pathlib import Path

BASE = Path(__file__).resolve().parents[1]
FLOW = BASE / "tools" / "NovelWorkFlow_Eleanor"
FINAL = FLOW / "Novel_Output" / "volume_common_regular_Chapter_3_Final.txt"
BRANCH = FLOW / "Novel_Output" / "volume_common_regular_Chapter_3_Branches.txt"
OUT = FLOW / "tmp_common_act3_blocks.json"

ROLE = {"埃莉诺": "eleanor", "店主": "player", "黑市打手": "thug", "旁白": "narrator"}


def parse_lines(raw_lines):
    out, pending = [], {}
    for raw in raw_lines:
        line = raw.strip()
        if not line:
            continue
        if line.startswith(("[章|", "[选择|", "[选项|", "[/选择]", "[汇合|", "[结果|",
                            "[选项后续|", "[/选项后续]", "[交易声明|", "[交易后续|", "[/交易后续]")):
            continue
        m = re.match(r"^\[([^|\]]+)\|表情\|([^\]]+)\]$", line)
        if m:
            pending[m.group(1)] = m.group(2).strip()
            continue
        m = re.match(r"^\[([^|\]]+)\|对白\|(.*)\]$", line)
        if m:
            name, text = m.group(1), m.group(2).strip()
            role = ROLE.get(name)
            if role is None:
                raise ValueError("unknown speaker " + name)
            row = {"speaker": role, "text": text}
            if name in pending:
                if role == "eleanor":
                    row["expr"] = pending[name]
                pending.pop(name, None)
            out.append(row)
            continue
        m = re.match(r"^\[旁白\|(.*)\]$", line)
        if m:
            out.append({"speaker": "narrator", "text": m.group(1).strip()})
            continue
        m = re.match(r"^\[通告\|([^|\]]+)\|(.*)\]$", line)
        if m:
            out.append({"speaker": "security_notice", "text": m.group(2).strip()})
            continue
        m = re.match(r"^\[新闻\|([^|\]]+)\|(.*)\]$", line)
        if m:
            out.append({"speaker": "news", "text": m.group(1).strip() + "：" + m.group(2).strip()})
            continue
        raise ValueError("unparsed line " + line)
    return out


def split_chapters(text):
    chapters = {}
    current = None
    for raw in text.splitlines():
        line = raw.strip()
        if line.startswith("[章|"):
            current = line[3:-1].strip()
            chapters[current] = []
            continue
        if current is not None:
            chapters[current].append(raw)
    return chapters


def parse_chapter(lines):
    pre, post, options = [], [], []
    choice = None
    mainline = None
    mode = "pre"
    for raw in lines:
        line = raw.strip()
        if line.startswith("[选择|"):
            m = re.match(r"^\[选择\|([^|\]]+)\|([^|\]]*)\|主线=(\d+)\]$", line)
            if not m:
                raise ValueError("bad choice " + line)
            choice = m.group(1)
            mainline = int(m.group(3))
            mode = "options"
            continue
        if line.startswith("[选项|"):
            options.append(line[4:-1].strip())
            continue
        if line == "[/选择]":
            mode = "post"
            continue
        (pre if mode == "pre" else post).append(line)
    return pre, options, post, choice, mainline


def parse_branches():
    text = BRANCH.read_text(encoding="utf-8")
    branches = {}
    for m in re.finditer(r"\[选项后续\|([^|\]]+)\|(\d+)\](.*?)\[\/选项后续\]", text, re.DOTALL):
        cid, n, body = m.group(1), int(m.group(2)), m.group(3)
        merge = re.search(r"\[汇合\|([^\]]*)\]", body)
        result = re.search(r"\[结果\|([^|\]]+)\|([^\]]*)\]", body)
        clean = re.sub(r"\[(?:汇合|结果)\|[^\]]*\]", "", body)
        branches[(cid, n)] = {
            "lines": parse_lines(clean.splitlines()),
            "merge": merge.group(1).strip() if merge else None,
            "result": {"id": result.group(1).strip(), "text": result.group(2).strip()} if result else None,
        }
    trades = {}
    for m in re.finditer(r"\[交易后续\|([^|\]]+)\|([^\]]+)\](.*?)\[\/交易后续\]", text, re.DOTALL):
        trades[(m.group(1), m.group(2).strip())] = parse_lines(m.group(3).splitlines())
    return branches, trades


def fix_no_state(lines):
    """Remove state-dependent lines from branches that must not change facts."""
    return lines


def main():
    chapters = split_chapters(FINAL.read_text(encoding="utf-8"))
    branches, trades = parse_branches()
    result = {"chapters": []}

    # c12: chapter 2
    pre, options, post, choice, mainline = parse_chapter(chapters["生日"])
    # Remove the new personal message on the drive.
    pre = [l for l in pre if "里面有她留给我的一句话" not in l]
    pre = [l.replace("里面有她留给我的一句话，我不想让别人读。",
                     "里面全是她的工作记录。我不想让别人先翻。") for l in pre]
    ask = [
        "[店主|对白|盘里还有别的吗。]",
        "[埃莉诺|表情|serious]",
        "[埃莉诺|对白|有。她记的班次，工人的名字，几笔没走完的账。]",
        "[埃莉诺|对白|我看不懂的比看懂的多。]",
    ]
    result["chapters"].append({
        "id": "c12",
        "title": "生日",
        "blocks": [
            {"id": "c12.b001", "label": "生日", "lines": parse_lines(pre)},
            {"id": "c12.b002", "label": "先问她盘里还有什么", "lines": parse_lines(ask)},
            {"id": "c12.b003", "label": "替她收着", "lines": parse_lines(post)},
        ],
        "choice": {
            "prompt": "er_prompt_drive_custody",
            "options": [
                ("替她收着", "er_choice_drive_keep", "er_choice_drive_keep_desc", "c12.b003", None),
                ("先问她盘里还有什么", "er_choice_drive_ask", "er_choice_drive_ask_desc", "c12.b002", "c12.b003"),
            ],
            "mainline": 1,
        },
    })

    # c13: chapter 3; drop the new "take 300" branch to preserve current facts.
    pre, options, post, choice, mainline = parse_chapter(chapters["跑腿钱"])
    throw = branches[("C3", 3)]["lines"]
    result["chapters"].append({
        "id": "c13",
        "title": "跑腿钱",
        "blocks": [
            {"id": "c13.b001", "label": "跑腿钱", "lines": parse_lines(pre)},
            {"id": "c13.b002", "label": "叫他出去", "lines": throw},
            {"id": "c13.b003", "label": "不收，话带到", "lines": parse_lines(post)},
        ],
        "choice": {
            "prompt": "er_prompt_threat_common",
            "options": [
                ("不收，话带到", "er_choice_threat_refuse", "er_choice_threat_refuse_desc", "c13.b003", None),
                ("叫他出去", "er_choice_threat_throw", "er_choice_threat_throw_desc", "c13.b002", "c13.b003"),
            ],
            "mainline": 1,
        },
    })

    # c14: chapters 4 + 5
    pre4, options4, post4, _, main4 = parse_chapter(chapters["三条路"])
    pre5, options5, post5, _, main5 = parse_chapter(chapters["看护单"])
    # Condense the two new visits into the existing single c14 decision visit.
    def drop(lines, text):
        return [line for line in lines if line.strip() != text]
    for text in (
        "[店主|对白|上回箱子的事，他们是按程序办的。]",
        "[埃莉诺|对白|按程序。呵，我被那套程序问了三个钟头。]",
        "[埃莉诺|对白|谁再想让它烂在抽屉里，都晚了。]",
        "[店主|对白|他会不痛快。]",
        "[埃莉诺|对白|让他不痛快去。你别跟他硬顶，你这店还得开。]",
    ):
        post4 = drop(post4, text)
    for text in (
        "[埃莉诺|对白|那个跑腿的来过没有？]",
        "[店主|对白|来过。我说你不交。]",
        "[埃莉诺|对白|他说什么了。]",
        "[店主|对白|骂了一句，走了。]",
    ):
        pre5 = drop(pre5, text)
    for text in (
        "[埃莉诺|对白|还会问我密码是怎么猜到的。总不能说我猜了一整晚，才想到是我自己。]",
        "[埃莉诺|对白|八百花出去了，这两天也没去垃圾场，身上只剩点零钱。]",
        "[埃莉诺|对白|按价签来，我付得起。]",
    ):
        post5 = drop(post5, text)
    ret = branches[("C4", 1)]["lines"]
    pub = branches[("C4", 3)]["lines"]
    # Preserve the established death wording.
    for l in pub:
        l["text"] = l["text"].replace("于看押期间病故", "于羁押中死亡，原因待查")
        l["text"] = l["text"].replace("于看护期间病故", "于羁押中死亡，原因待查")
    ask5 = branches[("C5", 2)]["lines"]
    reaction = {}
    for label in ("公道", "让利", "高价", "未成交"):
        reaction[label] = trades[("T2", label)]
    result["chapters"].append({
        "id": "c14",
        "title": "三条路",
        "blocks": [
            {"id": "c14.b001", "label": "三条路", "lines": parse_lines(pre4)},
            {"id": "c14.b002", "label": "交盘换人", "lines": ret},
            {"id": "c14.b003", "label": "先保人再公开", "lines": parse_lines(post4)},
            {"id": "c14.b004", "label": "立即公开", "lines": pub},
            {"id": "c14.b005", "label": "看护单", "lines": parse_lines(pre5)},
            {"id": "c14.b006", "label": "问她买什么", "lines": ask5},
            {"id": "c14.b007", "label": "让她坐", "lines": parse_lines(post5)},
            {"id": "c14.b008", "label": "公道", "lines": reaction["公道"]},
            {"id": "c14.b009", "label": "让利", "lines": reaction["让利"]},
            {"id": "c14.b010", "label": "高价", "lines": reaction["高价"]},
            {"id": "c14.b011", "label": "未成交", "lines": reaction["未成交"]},
        ],
        "choice": {
            "prompt": "er_prompt_drive_direction",
            "options": [
                ("交盘换人", "er_choice_drive_return", "er_choice_drive_return_desc", "c14.b002", None, "return"),
                ("先保人再公开", "er_choice_drive_protect", "er_choice_drive_protect_desc", "c14.b003", None, "protect"),
                ("现在就公开", "er_choice_drive_public", "er_choice_drive_public_desc", "c14.b004", None, "public"),
            ],
            "mainline": 2,
            "after_choice": {
                "prompt": "er_prompt_post_care",
                "from": "c14.b003",
                "options": [
                    ("让她坐", "er_choice_post_sit", "er_choice_post_sit_desc", "c14.b007", None),
                    ("问她买什么", "er_choice_post_ask", "er_choice_post_ask_desc", "c14.b006", "c14.b007"),
                ],
                "mainline": 1,
                "trade": "T2",
            },
        },
    })

    OUT.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    for ch in result["chapters"]:
        print(ch["id"], ch["title"], [(b["id"], len(b["lines"])) for b in ch["blocks"]])
    print("wrote", OUT)


if __name__ == "__main__":
    main()
