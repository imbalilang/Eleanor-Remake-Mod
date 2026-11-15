#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Render the revolution v2 route as a Chinese reading edition and append it to 全局剧本.md."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Content" / "revolution_v2.json"
DOC = ROOT / "docs" / "革命军线_v2_全剧情阅读版.md"
GLOBAL = ROOT / "docs" / "全局剧本.md"
START = "<!-- REVOLUTION_V2_START -->"
END = "<!-- REVOLUTION_V2_END -->"

SPEAKERS = {"Narrator": "旁白", "Player": "玩家", "Eleanor": "埃莉诺",
            "RevQuartermaster": "革命军军需官", "RevFighter": "革命军战士",
            "RevGuerrilla": "革命军游击队"}
CLIENTS = {"Eleanor": "埃莉诺"}
DAYS = {280: "第46/35天", 281: "第48/37天", 282: "第51/40天",
        283: "第54/43天", 284: "第57/46天", 285: "第60/49天，清晨优先",
        286: "第62/51天", 287: "第64/53天", 288: "第66/55天",
        289: "第69/58天", 290: "第72/61天，清晨优先"}
ENDS = {
    280: "完成第一次代表队伍收货；推进 281。",
    281: "完成医疗物资采购；推进 282（第二幕）。",
    282: "完成武器交易；交易结果按原生成交价判定；推进 283。",
    283: "空手来访结束；推进 284。",
    284: "记录队伍损失；推进 285（第三幕）。",
    285: "记录第一次杀人并推进 286。剧情插画显示后，点继续返回当铺。",
    286: "第三幕收束；推进 287（第四幕远征）。",
    287: "军需官来访并记录远征事实；推进 288。",
    288: "战士来访并转达她的近况；记录远征事实；推进 289。",
    289: "游击队来访并记录远征事实；推进 290。",
    290: "远征归来，最后一句“我好想你”；立即显示 E9 结局页，复用现有革命军结局 CG，不新增结局编号。",
}
IMPACT = "只改变当场的回答与后续语气；不改变货物、金钱、关系分数或路线推进。"
TRADE_IMPACT = {
    280: "收购她的队伍货单（TOOL）；可按原生讲价／批发成交，收益来自后续转卖。",
    281: "向她出售医疗物资（MEDICAL），基础预算 300；实际成交价决定她的回应。",
    282: "向她出售武器（WEAPON），基础预算 250；实际成交价决定她的回应。",
    287: "收购军需官的折价材料（MATERIAL）；按原生议价成交。",
    288: "收购战士带来的武器（WEAPON）；按原生议价成交。",
    289: "收购游击队的近战武器（MELEE_WEAPON）；按原生议价成交。",
}


def clean(text: str) -> str:
    return text.replace("<i>", "（").replace("</i>", "）")


def render():
    data = json.loads(SRC.read_text(encoding="utf-8"))
    ui = json.loads((ROOT / "tools" / "ui_text_zh.json").read_text(encoding="utf-8"))
    lines = [
        "# 革命军线 v2 全剧情阅读版",
        "",
        "版本：2026-11-14；对应新档 `RevVersion=2`。",
        "旧档 `RevVersion=1` 仍走 R01–R14（运行章节 101–113），不强制迁移，也不追溯授予 v2 完成页。",
        "",
        "## 入口与规则",
        "- 入口：共同线第15章“先救人，还是先公开”选择保护不足仍立即公开；母亲遇害，转入革命军线，章节从 280 起。",
        "- 目标日：280=第46天至 290=第72天；285、290 为清晨优先来访；提前相遇特性整体减11天（斜杠后为提前日期）。",
        "- 不新增有名字的角色或交易 NPC：第四幕固定复用原作革命军军需官、革命军战士、革命军游击队。",
        "- 旧档已进入革命军线（RevVersion=1）保持原 101–113 调度与旧结局页；新档只走 280–290。",
        "- 完成页沿用 E9 与现有革命军结局 CG `er_ending_revolution_cg`；不新增路线或结局编号。",
        "",
    ]
    for scene in data["scenes"]:
        code = scene["code"]
        lines.append(f"## 革命军 v2·{code}　{scene['title_zh']}")
        lines.append("")
        client = CLIENTS.get(scene.get("client", ""), scene.get("client", ""))
        lines.append(f"出现：{DAYS.get(code, '')}；来客：{client}；立绘：{scene.get('portrait', '')}；意图：{scene.get('intent', '')}。")
        lines.append("")
        for trade in scene.get("trades", []):
            lines.append(
                f"交易：{trade.get('id')} {trade.get('intent')} / {trade.get('type')} / "
                f"预算 {trade.get('budget')} / 客户 {trade.get('client')}；"
                f"结果按原生交易取{'／'.join(trade.get('outcomes', {}).keys())}。")
            if code in TRADE_IMPACT:
                lines.append(f"实际影响：{TRADE_IMPACT[code]}")
            lines.append("")
        for seg in scene["segments"]:
            if seg["type"] == "lines":
                for line in scene["blocks"][seg["block"]]["lines"]:
                    lines.append(f"{SPEAKERS.get(line['speaker'], line['speaker'])}：{clean(line['text'])}")
                lines.append("")
            elif seg["type"] == "result":
                lines.append(f"旁白：{clean(seg['result'])}")
                lines.append("")
            elif seg["type"] == "choice":
                lines.append(f"选择：{seg['prompt']}")
                for opt in seg["options"]:
                    lines.append(f"- **{opt['label']}**：{IMPACT}")
                    if opt.get("block"):
                        for line in scene["blocks"][opt["block"]]["lines"]:
                            lines.append(f"  - {SPEAKERS.get(line['speaker'], line['speaker'])}：{clean(line['text'])}")
                        if opt.get("result"):
                            lines.append(f"  - 结果：{opt['result']}")
                lines.append("")
        if code == 285:
            lines.append(f"**剧情插画 · {ui['er_story_cg_revolution_embrace_title']}**")
            lines.append("")
            lines.append(ui["er_story_cg_revolution_embrace_text"])
            lines.append("")
        lines.append(f"推进：{ENDS.get(code, '')}")
        lines.append("")
    text = "\n".join(lines) + "\n"
    DOC.write_text(text, encoding="utf-8")

    section = START + "\n" + text + "\n" + END + "\n"
    global_text = GLOBAL.read_text(encoding="utf-8")
    if START in global_text and END in global_text:
        a = global_text.index(START)
        b = global_text.index(END) + len(END)
        global_text = global_text[:a] + section + global_text[b:]
    else:
        global_text = global_text.rstrip() + "\n\n" + section
    GLOBAL.write_text(global_text, encoding="utf-8")
    print(f"Wrote {DOC}")
    print(f"Updated {GLOBAL} (revolution v2 section)")


if __name__ == "__main__":
    render()
