#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Render the maid v2 route as a Chinese reading edition and append it to 全局剧本.md."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Content" / "maid_v2.json"
DOC = ROOT / "docs" / "侍从线_v2_全剧情阅读版.md"
GLOBAL = ROOT / "docs" / "全局剧本.md"
START = "<!-- MAID_V2_START -->"
END = "<!-- MAID_V2_END -->"

SPEAKERS = {"Narrator": "旁白", "Player": "玩家", "Eleanor": "埃莉诺",
            "Sophia": "索菲娅", "Thug": "黑市打手"}
DAYS = {8: "第33/22天", 12: "第39/28天", 13: "第41/30天", 14: "第43/32天",
        15: "第44/33天", 16: "第45/34天", 17: "第46/35天", 18: "第48/37天",
        19: "第50/39天", 20: "第52/41天", 21: "第54/43天", 22: "第55/44天",
        25: "第61/50天", 26: "第63/52天", 27: "第65/54天", 28: "第67/56天"}
ENDS = {
    "v2_a0c1": "完成后推进共同09。补缺口必须在原生 SELL 实际成交后，且她仍差800；未成交、缺口为0或不补都回共同线。",
    "v2_a0c2": "点头答应才建立 v2 侍从约定；婉拒回共同线。",
    "v2_a1c1": "她把数据盘贴身保管；推进14。",
    "v2_a1c2": "黑市打手留下300跑腿钱；推进15。",
    "v2_a1c3": "申请保护与交盘换人都能安全救母并继续侍从结局E3；营养果研究只保留经营与背景差异。立即公开转革命军。",
    "v2_a1c4": "母亲转入看护、安全；推进17。",
    "v2_a1c5": "索菲娅来店买日用品（T1 BUY / HOUSEHOLD_GOOD / 预算220）；推进18。",
    "v2_a2c1": "她第一次私下叫“主人”，仍可差点收回；推进19。",
    "v2_a2c2": "告白确认侍从恋人；之后才提唱歌；P2 成为第三幕门槛。",
    "v2_a3c1": "P2 已买后她提出试唱；推进21。",
    "v2_a3c2": "第一次登台唱砸，她自己换曲练习；推进22。",
    "v2_a3c3": "第二次首秀成功，拿到付费返场；基础线完成，进入第四幕等待。",
    "v2_a4c1": "返场满座/加唱、驻场只写她的名字；她拒绝母亲案宣传；推进26。",
    "v2_a4c2": "旧约结清、借款照还、另立新约；推进27。",
    "v2_a4c3": "上层晚会请柬，嘉宾栏写“我的店主”；推进28。",
    "v2_a4c4": "私下尾声；侍从结局E3完成，复用现有女仆装结局CG。",
}
IMPACTS = {
    ("v2_a0c1", "C1", "o1"): "原生 SELL 后补实际缺口：扣玩家现金、记借款；进入共事候选。",
    ("v2_a0c1", "C1", "o2"): "不垫款：回共同线，不排共事来访。",
    ("v2_a0c2", "C2", "o1"): "进入侍从线 v2；后续可到E3。",
    ("v2_a0c2", "C2", "o2"): "婉拒共事：回共同线；已垫款仍用于调查。",
    ("v2_a1c3", "C1", "o1"): "研究归还；母亲获释/流放下层；侍从线 → E3。",
    ("v2_a1c3", "C1", "o2"): "申请保护、母亲安全；侍从线 → E3，研究保留为经营与背景差异。",
    ("v2_a1c3", "C1", "o3"): "立即公开；母亲死亡；转入革命军线。",
    ("v2_a2c2", "C2", "o1"): "确认侍从恋人；关系结果不变。",
    ("v2_a2c2", "C2", "o2"): "确认侍从恋人；关系结果不变。",
    ("v2_a2c2", "C2", "o3"): "确认侍从恋人；关系结果不变。",
    ("v2_a4c2", "C2", "o1"): "接受续约；旧约结清、新约继续。",
    ("v2_a4c2", "C2", "o2"): "接受续约；旧约结清、新约继续。",
    ("v2_a4c2", "C2", "o3"): "接受续约；旧约结清、新约继续。",
    ("v2_a4c3", "C3", "o1"): "接受晚会邀请；嘉宾栏写“我的店主”。",
    ("v2_a4c3", "C3", "o2"): "接受晚会邀请；嘉宾栏写“我的店主”。",
    ("v2_a4c3", "C3", "o3"): "接受晚会邀请；嘉宾栏写“我的店主”。",
}


def clean(text: str) -> str:
    return text.replace("<i>", "（").replace("</i>", "）")


def render():
    data = json.loads(SRC.read_text(encoding="utf-8"))
    lines = [
        "# 侍从线 v2 全剧情阅读版",
        "",
        "版本：2026-11-15；对应新档 `FeatureVersion=2`。",
        "旧档 `FeatureVersion=1` 仍走原侍从线，不强制迁移。",
        "",
        "## 入口与规则",
        "- C07 第二次拾荒售货：原生 SELL 实际成交后，若她仍差 800，玩家只补实际缺口（扣玩家现金，算借款）。未成交、缺口为 0 或不补，都回共同线。",
        "- 第12章共事入口：点头答应才进入 v2；婉拒回共同线。",
        "- P2“通行与正装”是首个且唯一必做的王尔德项目，买下后开启第三幕。P1/P3/P4 与经营/分红保留为可选背景，不卡主线。",
        "- 两个安全救母方案都走侍从结局E3，使用同一结局CG；营养果是否开源不另拆结局或区分好坏。",
        "",
    ]
    for scene in data["scenes"]:
        code = scene["code"]
        lines.append(f"## 侍从 v2·{code:02d}　{scene['title_zh']}")
        lines.append("")
        lines.append(f"出现：{DAYS.get(code,'')}；来客：{scene.get('client','')}；立绘：{scene.get('portrait','')}；意图：{scene.get('intent','')}。")
        lines.append("")
        for trade in scene.get("trades", []):
            lines.append(f"交易：T1 {trade.get('intent')} / {trade.get('type')} / 预算 {trade.get('budget')} / 客户 {trade.get('client')}；结果按原生交易取{'／'.join(trade.get('outcomes',{}).keys())}。")
            lines.append("")
        for seg in scene["segments"]:
            if seg["type"] == "lines":
                variants = {"v2_a1c5_s0": "v2_a1c5_exchange",
                            "v2_a4c2_s0": "v2_a4c2_exchange"}
                blocks = [("", seg["block"])]
                if seg["block"] in variants:
                    blocks = [("研究保留时", seg["block"]),
                              ("研究归还时（互斥替换）", variants[seg["block"]])]
                for condition, block_id in blocks:
                    if condition:
                        lines.extend([f"**{condition}**", ""])
                    for line in scene["blocks"][block_id]["lines"]:
                        lines.append(f"{SPEAKERS.get(line['speaker'],line['speaker'])}：{clean(line['text'])}")
                    lines.append("")
            elif seg["type"] == "choice":
                lines.append(f"选择：{seg['prompt']}")
                for opt in seg["options"]:
                    suffix = opt["id"].split("_")[-1]
                    note = IMPACTS.get((scene["id"], seg["id"], suffix),
                                       "不改变成功、驻场、邀请或关系结果；只改变语气/说法。")
                    lines.append(f"- **{opt['label']}**：{note}")
                    if not opt.get("mainline") and opt.get("block"):
                        for line in scene["blocks"][opt["block"]]["lines"]:
                            lines.append(f"  - {SPEAKERS.get(line['speaker'],line['speaker'])}：{clean(line['text'])}")
                        if opt.get("result"):
                            lines.append(f"  - 结果：{opt['result']}")
                lines.append("")
        lines.append(f"推进：{ENDS.get(scene['id'],'')}")
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
    print(f"Updated {GLOBAL} (maid v2 section)")


if __name__ == "__main__":
    render()
