#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Audit current MOD text against the working text-style standard (v0.1).

只读 Content；不修改任何文件。输出终端报告，可用 --md 保存到 dist/。

标准：docs/TEXT_STYLE_AUDIT_STANDARD.md
- 路线级 gate：动作标签、句子完成度、情绪幅度、玩家能动性等
- 场景级 warning：只列极端值，避免把正常场景全部标红
"""
from __future__ import annotations

import argparse
import json
import re
import statistics
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

CJK = re.compile(r"[\u4e00-\u9fff]")
ACTION = re.compile(r"<i>(.*?)</i>", re.S)
PARTICLES = re.compile(r"[啊吧呢嘛呗呀哦噢咯啦哈]")
INSULT = re.compile(r"蠢|滚|闭嘴|少来|混账|废物|骗子|荒唐|荒谬|绝了|可恶|该死|见鬼")
REL = re.compile(r"我们|关系|喜欢|爱|恋人|告白|约定|答应|信任|原谅|尊重|以后|将来|永远|自己决定|别替我|说清楚")
THERAPY = re.compile(r"你不需要|你可以|你不必|你不用|我不逼你|你自己决定|你的选择|别勉强|你愿意|尊重你|你有权|你应当")
PROP = re.compile(r"价签|账本|账|价|钱|柜台|窗口|卷帘|椅子|凳|杯|灯|袋|箱|名单|名册|单子|盘|吊坠|怀表|手镯|药|果|绷带")
NARR_INTERP = re.compile(r"知道|明白|懂|意识|发现|仿佛|似乎|像是|好像|终于|心里|心底|其实|原来|也许|或许|某种")

# 路线级 gate（v0.1 试行；待作者确认锚点后校准）
GATES = {
    "action_rate":        (1.0, 10.0, "%", "含动作标签的 NPC 行占比"),
    "action_start_rate":  (0.0, 6.0, "%", "以动作标签开头的 NPC 行占比"),
    "avg_len":            (12.0, 18.0, "字", "NPC 行均汉字数"),
    "fragment_rate":      (2.5, 16.0, "%", "未完成句/以……结尾的 NPC 行占比（观察项）"),
    "exclaim_rate":       (2.0, 10.0, "%", "含！的 NPC 行占比"),
    "period_rate":        (70.0, 90.0, "%", "以。结尾的 NPC 行占比"),
    "particle_density":   (3.0, 7.0, "/k", "语气词 / 千汉字"),
    "rel_density":        (0.0, 5.0, "/k", "关系/承诺直说 / 千汉字（观察项）"),
    "therapy_density":    (0.0, 1.0, "/k", "边界/治疗话术 / 千汉字"),
}
# 场景级 warning 阈值（只抓极端）
WARN = {
    "action_rate": 25.0,
    "avg_len": 21.0,
    "rel_density": 8.0,
}

def load(path):
    return json.loads(path.read_text(encoding="utf-8"))

def is_player(sp):
    return (sp or "").strip().lower() in ("player", "玩家", "店主")

def is_narr(sp):
    return (sp or "").strip().lower() in ("narrator", "旁白")

def collect_scenes():
    out = []
    d = load(ROOT / "Content" / "story_v06.json")
    for ch in d.get("chapters", []):
        lines = [((ln.get("speaker") or "").strip(), ln.get("text", ""))
                 for b in ch.get("blocks", []) for ln in b.get("lines", [])]
        out.append(("story", ch.get("id") or "", ch.get("title_zh") or "", lines))
    for f in ("love_romance_v01.json", "love_romance_v02.json", "love_romance_v03.json"):
        for sc in load(ROOT / "Content" / f).get("scenes", []):
            lines = [((ln.get("speaker") or "").strip(), ln.get("text", ""))
                     for b in sc.get("blocks", []) for ln in b.get("lines", [])]
            out.append(("love", sc.get("id"), sc.get("title_zh") or "", lines))
    for sc in load(ROOT / "Content" / "dual_romance_v04.json").get("scenes", []):
        if sc.get("id") != "d01" and not (sc.get("id") or "").startswith("dv"):
            continue
        bs = sc.get("blocks")
        items = list(bs.values()) if isinstance(bs, dict) else (bs or [])
        lines = [((ln.get("speaker") or "").strip(), ln.get("text", ""))
                 for b in items for ln in b.get("lines", [])]
        out.append(("dual", sc.get("id"), sc.get("title_zh") or "", lines))
    for f, route in (("maid_v2.json", "maid_v2"), ("revolution_v2.json", "rev_v2")):
        for sc in load(ROOT / "Content" / f).get("scenes", []):
            bs = sc.get("blocks")
            items = list(bs.values()) if isinstance(bs, dict) else (bs or [])
            lines = [((ln.get("speaker") or "").strip(), ln.get("text", ""))
                     for b in items for ln in b.get("lines", [])]
            out.append((route, sc.get("id"), sc.get("title_zh") or "", lines))
    return out

def metrics(lines):
    npc = [(s, t) for s, t in lines if not is_player(s) and not is_narr(s)]
    pl = [(s, t) for s, t in lines if is_player(s)]
    narr = [(s, t) for s, t in lines if is_narr(s)]
    n = len(npc)
    if n == 0:
        return None
    chars = sum(len(CJK.findall(t)) for _, t in npc)
    lens = [len(CJK.findall(t)) for _, t in npc]
    act_tags = [x.strip() for _, t in npc for x in ACTION.findall(t)]
    m = {
        "npc": n, "player": len(pl), "chars": chars,
        "action_rate": 100 * sum(1 for _, t in npc if ACTION.search(t)) / n,
        "action_start_rate": 100 * sum(1 for _, t in npc if t.strip().startswith("<i>")) / n,
        "action_full_sentence": sum(1 for a in act_tags if "。" in a),
        "fragment_rate": 100 * sum(1 for _, t in npc
                                   if (not re.search(r"[。！？…]$", t)) or t.endswith("……")) / n,
        "exclaim_rate": 100 * sum(1 for _, t in npc if "！" in t) / n,
        "period_rate": 100 * sum(1 for _, t in npc if t.rstrip().endswith("。")) / n,
        "avg_len": chars / n,
        "cv": statistics.pstdev(lens) / max(1.0, chars / n),
        "particle_density": 1000 * sum(len(PARTICLES.findall(t)) for _, t in npc) / max(1, chars),
        "insult_density": 1000 * sum(len(INSULT.findall(t)) for _, t in npc) / max(1, chars),
        "rel_density": 1000 * sum(1 for _, t in npc if REL.search(t)) / max(1, chars),
        "therapy_density": 1000 * sum(1 for _, t in npc if THERAPY.search(t)) / max(1, chars),
        "prop_density": 1000 * sum(len(PROP.findall(t)) for _, t in npc) / max(1, chars),
        "narrator_n": len(narr),
        "narrator_avg": sum(len(CJK.findall(t)) for _, t in narr) / max(1, len(narr)),
        "narrator_interp": sum(1 for _, t in narr if NARR_INTERP.search(t)),
        "agency_lines": sum(1 for _, t in pl if len(CJK.findall(t)) >= 8),
    }
    return m

def gate_status(v, lo, hi):
    if lo is not None and v < lo:
        return "LOW"
    if hi is not None and v > hi:
        return "HIGH"
    return "OK"

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--md", default="")
    args = ap.parse_args()
    rows = []
    for route, sid, title, lines in collect_scenes():
        m = metrics(lines)
        if m:
            m.update(route=route, sid=sid, title=title)
            rows.append(m)

    md = ["# 文本风格审计（v0.1 试行标准）\n",
          "数据源：`Content/*.json` 当前正文；只读。标准见 `docs/TEXT_STYLE_AUDIT_STANDARD.md`。",
          "本报告只做分诊：指标异常只提示“去读那几场”，不提示“改成什么”。验收必须走盲测（见 docs/TEXT_STYLE_AUDIT_STANDARD.md §5）。\n"]

    routes = ("story", "love", "dual", "maid_v2", "rev_v2")
    md.append("## 路线级 gate\n")
    md.append("| 指标 | 取值 | 阈值 | " + " | ".join(routes) + " |")
    md.append("|---|---|---|" + "---|" * len(routes))
    for key, (lo, hi, unit, desc) in GATES.items():
        cells = []
        for route in routes:
            rs = [r for r in rows if r["route"] == route]
            v = sum(r[key] for r in rs) / max(1, len(rs))
            st = gate_status(v, lo, hi)
            cells.append(f"{v:.1f}{unit} {st}")
        rng = f"{lo if lo is not None else '-'}–{hi if hi is not None else '-'}{unit}"
        md.append(f"| {key} | {desc} | {rng} | " + " | ".join(cells) + " |")

    md.append("\n## 路线汇总\n")
    md.append("| 路线 | 场景 | 均气泡 | 均句长 | 动作% | 动作开头% | 断句% | ！% | 句号% | CV | 语气词/k | 玩家能动场景 |")
    md.append("|---|---|---|---|---|---|---|---|---|---|---|---|")
    for route in routes:
        rs = [r for r in rows if r["route"] == route]
        if not rs:
            continue
        n = len(rs)
        avg = lambda k: sum(r[k] for r in rs) / n
        md.append(f"| {route} | {n} | {avg('npc')+avg('player'):.0f} | {avg('avg_len'):.1f} | "
                  f"{avg('action_rate'):.0f} | {avg('action_start_rate'):.0f} | {avg('fragment_rate'):.0f} | "
                  f"{avg('exclaim_rate'):.0f} | {avg('period_rate'):.0f} | {avg('cv'):.2f} | {avg('particle_density'):.1f} | "
                  f"{sum(1 for r in rs if r['agency_lines'] > 0)}/{n} |")

    md.append("\n## 场景级 warning（只列极端值）\n")
    md.append("| 场景 | NPC | 动作% | 断句% | 均长 | 关系/k | warning |")
    md.append("|---|---|---|---|---|---|---|")
    for r in rows:
        w = []
        if r["action_rate"] > WARN["action_rate"]:
            w.append("动作过密")
        if r["avg_len"] > WARN["avg_len"]:
            w.append("句长过长")
        if r["rel_density"] > WARN["rel_density"]:
            w.append("关系直说过密")
        if r["agency_lines"] == 0 and r["player"] > 0:
            w.append("无玩家能动句")
        if w:
            md.append(f"| {r['route']}/{r['sid']} | {r['npc']} | {r['action_rate']:.0f} | "
                      f"{r['fragment_rate']:.0f} | {r['avg_len']:.1f} | {r['rel_density']:.1f} | {', '.join(w)} |")
    if len(md) <= 0:
        pass

    md.append("\n## 动作标签格式\n")
    bad = [r for r in rows if r["action_full_sentence"] > 0]
    if bad:
        for r in bad:
            md.append(f"- {r['route']}/{r['sid']}：{r['action_full_sentence']} 个动作标签内含完整句号")
    else:
        md.append("- 无")

    md.append("\n## 旁白解释性句子\n")
    interp = [r for r in rows if r["narrator_interp"]]
    if interp:
        for r in interp:
            md.append(f"- {r['route']}/{r['sid']}：{r['narrator_interp']} 条")
    else:
        md.append("- 无")

    report = "\n".join(md) + "\n"
    print(report)
    if args.md:
        p = ROOT / args.md
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(report, encoding="utf-8")
        print(f"Wrote {p}")

if __name__ == "__main__":
    main()
