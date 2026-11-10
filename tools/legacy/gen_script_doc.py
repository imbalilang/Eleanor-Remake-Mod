#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Generate a novel/screenplay-style script document from Localization/zh.csv."""

import csv
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CSV_PATH = os.path.join(ROOT, "Localization", "zh.csv")
BOOK_PATH = os.path.join(ROOT, "Books", "mothers_notes.json")
OUT_PATH = os.path.join(ROOT, "docs", "剧情与选项设计_台本版.txt")

vals = {}
with open(CSV_PATH, encoding="utf-8") as f:
    for row in csv.reader(f):
        if len(row) >= 2:
            vals[row[0]] = row[1]

used = set()


def L(key, *args):
    used.add(key)
    text = vals.get(key, "【缺失：" + key + "】")
    if args:
        try:
            text = text.format(*args)
        except Exception:
            pass
    return text


def N(key):
    return L("er_name_narrator")


def say(speaker, key, *args):
    text = L(key, *args)
    return speaker + "：\n" + text + "\n"


def stage(key, *args):
    return "【旁白】\n" + L(key, *args) + "\n"


def auto(speaker, key, *args):
    text = L(key, *args)
    if text.startswith("（"):
        return "【旁白】\n" + text + "\n"
    return say(speaker, key, *args)


def section(title):
    return "\n" + "=" * 72 + "\n" + title + "\n" + "=" * 72 + "\n"


def branch(title):
    return "\n—— " + title + " ——\n"


def choice_block(prompt_key, options):
    lines = [stage(prompt_key), "【选择】"]
    for i, opt in enumerate(options, 1):
        label = L(opt["label"], *opt.get("args", ()))
        lines.append("  " + str(i) + ". " + label)
        if opt.get("desc"):
            lines.append("     [提示] " + L(opt["desc"]))
        if opt.get("cond"):
            lines.append("     [条件] " + opt["cond"])
        if opt.get("effect"):
            lines.append("     [效果] " + opt["effect"])
    return "\n".join(lines) + "\n"


out = []
out.append("《上层区千金与下层区当铺》")
out.append("Eleanor 个人剧情线重制 · 台本版")
out.append("版本 v0.1 · 含全部台词、选项与分支")
out.append("")
out.append("说明：")
out.append("1. 本文按章节顺序展开，每一句游戏内台词都单独列出。")
out.append("2. “玩家”台词就是游戏中的选项文本；选择后会作为玩家气泡显示。")
out.append("3. “【旁白】”包括场景描写、动作提示与系统叙述。")
out.append("4. 选项后的“[条件] / [效果]”是解锁与判定逻辑。")
out.append("5. 标注为“分支”的段落只在玩家做出对应选择时出现。")
out.append("6. 剧透警告：本文完整包含真结局、母亲命运与所有关键转折。")
out.append("")
out.append("目录")
out.append("  第 1 章  上层区的客人")
out.append("  第 2 章  不合时宜的体面")
out.append("  第 3 章  被退婚的千金")
out.append("  第 4 章  舅舅的包裹")
out.append("  第 5A 章 逮捕救援（舅舅陷阱触发）")
out.append("  第 5B 章 母亲的判决（未触发陷阱）")
out.append("  第 6 章  拾荒的千金")
out.append("  第 7 章  八百信用点的缺口")
out.append("  第 8 章  U 盘")
out.append("  第 9 章  黑市的问候")
out.append("  第 10 章 最后一张牌")
out.append("  第 11 章 长尾声（真结局 / 回归 / 泄露 / 锁死）")
out.append("  常驻内容（每周来访 / 上层区渠道）")
out.append("  旧档兼容事件")
out.append("  附录 A  新开局")
out.append("  附录 B  物品与纸书")
out.append("  附录 C  结局变体与系统提示")
out.append("  附录 D  供评估的剧情/数值问题")
out.append("  附录 E  解锁逻辑速查")

# ======================================================================
# 第 1 章
# ======================================================================
out.append(section("第 1 章  上层区的客人\n第 23 天 · 下层区当铺"))
out.append("【场景】当铺门铃响起。一个穿着上层区服饰的女人走进来，身后没有随从。\n")
out.append(auto("埃莉诺", "er_ch1_1"))
out.append(auto("埃莉诺", "er_ch1_2"))
out.append(auto("埃莉诺", "er_ch1_3"))
out.append(choice_block("er_ch1_choice_prompt", [
    {"label": "er_ch1_choice_1", "desc": "er_ch1_choice_1_desc", "cond": "无", "effect": "好感 +1"},
    {"label": "er_ch1_choice_2", "desc": "er_ch1_choice_2_desc", "cond": "无", "effect": "好感 +0"},
    {"label": "er_choice_bm20_paid", "args": (20, 100), "desc": "er_ch1_choice_3_desc",
     "cond": "黑市声望 ≥ 20；支付 100 信用点", "effect": "好感 +2"},
]))
branches = [
    ("选择 1：先请她坐下，倒杯水", "er_ch1_choice_1", (), [
        ("埃莉诺", "er_ch1_after_polite"),
        ("埃莉诺", "er_ch1_4"),
        ("埃莉诺", "er_ch1_5"),
    ]),
    ("选择 2：冷笑：上层区的人也会饿？", "er_ch1_choice_2", (), [
        ("埃莉诺", "er_ch1_after_cold"),
        ("埃莉诺", "er_ch1_4"),
        ("埃莉诺", "er_ch1_5"),
    ]),
    ("选择 3：从黑市渠道拿一份稀有食材", "er_choice_bm20_paid", (20, 100), [
        ("埃莉诺", "er_ch1_after_bm"),
        ("埃莉诺", "er_ch1_4"),
        ("埃莉诺", "er_ch1_5"),
    ]),
]
for title, label_key, args, lines in branches:
    out.append(branch(title))
    out.append(say("玩家", label_key, *args))
    for speaker, key in lines:
        out.append(auto(speaker, key))
out.append(stage("er_ch1_end"))
out.append("【系统】第 1 章结束，推进到第 2 章（第 25 天）。\n")
out.append("【交易追加】若玩家实际卖给她食物并成交：\n")
out.append(say("埃莉诺", "er_ch1_deal"))
out.append("【系统】好感 +1。\n")

# ======================================================================
# 第 2 章
# ======================================================================
out.append(section("第 2 章  不合时宜的体面\n第 25 天 · 当铺"))
out.append("【场景】Eleanor 穿着同一身衣服回来，手里攥着一张折得整整齐齐的购物清单。\n")
out.append(auto("埃莉诺", "er_ch2_1"))
out.append(auto("埃莉诺", "er_ch2_2"))
out.append(auto("埃莉诺", "er_ch2_3"))
out.append(choice_block("er_ch2_choice_prompt", [
    {"label": "er_ch2_choice_1", "args": (50,), "desc": "er_ch2_choice_1_desc",
     "cond": "支付 50 信用点", "effect": "好感 +2"},
    {"label": "er_ch2_choice_2", "desc": "er_ch2_choice_2_desc", "cond": "无", "effect": "好感 +1"},
    {"label": "er_ch2_choice_3", "desc": "er_ch2_choice_3_desc", "cond": "无", "effect": "好感 -1"},
    {"label": "er_choice_ll20", "args": (20,), "desc": "er_ch2_choice_4_desc",
     "cond": "下城区声望 ≥ 20", "effect": "好感 +2"},
]))
branches = [
    ("选择 1：给她打折，再塞一包吃的", "er_ch2_choice_1", (50,), [
        ("埃莉诺", "er_ch2_after_1"), ("埃莉诺", "er_ch2_4"), ("埃莉诺", "er_ch2_5")]),
    ("选择 2：原价卖，但告诉她下层区的规矩", "er_ch2_choice_2", (), [
        ("埃莉诺", "er_ch2_after_2"), ("埃莉诺", "er_ch2_4"), ("埃莉诺", "er_ch2_5")]),
    ("选择 3：问她珠宝卖不卖", "er_ch2_choice_3", (), [
        ("埃莉诺", "er_ch2_after_3"), ("埃莉诺", "er_ch2_4"), ("埃莉诺", "er_ch2_5")]),
    ("选择 4：用下城区人脉帮她找便宜住处", "er_choice_ll20", (20,), [
        ("埃莉诺", "er_ch2_after_4"), ("埃莉诺", "er_ch2_4"), ("埃莉诺", "er_ch2_5")]),
]
for title, label_key, args, lines in branches:
    out.append(branch(title))
    out.append(say("玩家", label_key, *args))
    for speaker, key in lines:
        out.append(auto(speaker, key))
out.append(stage("er_ch2_end"))
out.append("【系统】第 2 章结束，推进到第 3 章（第 27 天）。")
out.append("【交易追加】若实际成交：好感 +1。\n")

# ======================================================================
# 第 3 章
# ======================================================================
out.append(section("第 3 章  被退婚的千金\n第 27 天 · 当铺"))
out.append("【场景】Eleanor 把母亲的吊坠放在柜台上。吊坠的链子断了一截。\n")
out.append(auto("埃莉诺", "er_ch3_1"))
out.append(auto("埃莉诺", "er_ch3_2"))
out.append(auto("埃莉诺", "er_ch3_3"))
out.append(choice_block("er_ch3_choice_prompt", [
    {"label": "er_ch3_choice_1", "args": (300,), "desc": "er_ch3_choice_1_desc",
     "cond": "支付 300 信用点", "effect": "好感 +2；获得吊坠"},
    {"label": "er_ch3_choice_2", "args": (100,), "desc": "er_ch3_choice_2_desc",
     "cond": "支付 100 信用点", "effect": "好感 +1；获得吊坠"},
    {"label": "er_ch3_choice_3", "desc": "er_ch3_choice_3_desc", "cond": "无", "effect": "好感 -1"},
    {"label": "er_choice_ul20_paid", "args": (20, 150), "desc": "er_ch3_choice_4_desc",
     "cond": "上层区声望 ≥ 20；支付 150 信用点", "effect": "好感 +3"},
]))
branches = [
    ("选择 1：按高价收下，但把当票留给她", "er_ch3_choice_1", (300,), [
        ("埃莉诺", "er_ch3_after_1"), ("埃莉诺", "er_ch3_4"), ("埃莉诺", "er_ch3_5")]),
    ("选择 2：按市价收，公事公办", "er_ch3_choice_2", (100,), [
        ("埃莉诺", "er_ch3_after_2"), ("埃莉诺", "er_ch3_4"), ("埃莉诺", "er_ch3_5")]),
    ("选择 3：不收。告诉她当铺不是慈善堂", "er_ch3_choice_3", (), [
        ("埃莉诺", "er_ch3_after_3"), ("埃莉诺", "er_ch3_4"), ("埃莉诺", "er_ch3_5")]),
    ("选择 4：用上层区人脉压下退婚丑闻", "er_choice_ul20_paid", (20, 150), [
        ("埃莉诺", "er_ch3_after_4"), ("埃莉诺", "er_ch3_4"), ("埃莉诺", "er_ch3_5")]),
]
for title, label_key, args, lines in branches:
    out.append(branch(title))
    out.append(say("玩家", label_key, *args))
    for speaker, key in lines:
        out.append(auto(speaker, key))
    if label_key in ("er_ch3_choice_1", "er_ch3_choice_2"):
        out.append("【获得物品】" + L("er_item_locket_name"))
        out.append("　　" + L("er_item_locket_desc"))
        out.append("　　" + L("er_item_locket_flavor") + "\n")
out.append(stage("er_ch3_end"))
out.append("【系统】第 3 章结束，推进到第 4 章（第 28 天）。\n")

# ======================================================================
# 第 4 章
# ======================================================================
uncle = L("er_name_uncle")
out.append(section("第 4 章  舅舅的包裹\n第 28 天 · 当铺"))
out.append("【场景】一个自称 Eleanor 舅舅的男人走进当铺，门外停着一辆没有标识的车。\n")
out.append(auto(uncle, "er_ch4_1"))
out.append(auto(uncle, "er_ch4_2"))
out.append(auto(uncle, "er_ch4_3"))
out.append(choice_block("er_ch4_choice_prompt", [
    {"label": "er_ch4_choice_1", "desc": "er_ch4_choice_1_desc", "cond": "无",
     "effect": "好感 +1；安保声望 +5；发现定位器与伪造清单"},
    {"label": "er_ch4_choice_2", "desc": "er_ch4_choice_2_desc", "cond": "无",
     "effect": "好感 +0；触发第 5 章逮捕救援线"},
    {"label": "er_ch4_choice_3", "desc": "er_ch4_choice_3_desc", "cond": "无",
     "effect": "好感 +1；安保声望 +3；警告 Eleanor"},
    {"label": "er_ch4_choice_4", "args": (20,), "desc": "er_ch4_choice_4_desc",
     "cond": "黑市声望 ≥ 20", "effect": "好感 -2；获得 300 信用点"},
]))
branches = [
    ("选择 1：收下，但当着他的面检查包裹", "er_ch4_choice_1", (), [
        (N(""), "er_ch4_after_1"), (N(""), "er_ch4_after_1b")]),
    ("选择 2：收下，原封不动交给 Eleanor", "er_ch4_choice_2", (), [
        (N(""), "er_ch4_after_2")]),
    ("选择 3：拒绝，并警告 Eleanor", "er_ch4_choice_3", (), [
        (N(""), "er_ch4_after_3"), (N(""), "er_ch4_after_3b")]),
    ("选择 4：把包裹卖给黑市", "er_ch4_choice_4", (20,), [
        (N(""), "er_ch4_after_4")]),
]
for title, label_key, args, lines in branches:
    out.append(branch(title))
    out.append(say("玩家", label_key, *args))
    for speaker, key in lines:
        out.append(auto(speaker, key))
out.append(stage("er_ch4_end"))
out.append("【系统】第 4 章结束，推进到第 5 章（第 29 天）。\n")

# ======================================================================
# 第 5A 章：陷阱线
# ======================================================================
out.append(section("第 5A 章  逮捕救援（舅舅陷阱触发）\n第 29 天 · 安保大楼"))
out.append("【说明】若第 4 章选择“收下，原封不动交给 Eleanor”，进入本章。\n")
out.append("【场景】包裹里的定位器与伪造清单被当成 Eleanor“接货”的证据。她只被允许出来一小时。\n")
out.append(auto("埃莉诺", "er_ch5_trap_1"))
out.append(auto("埃莉诺", "er_ch5_trap_2"))
out.append(auto("埃莉诺", "er_ch5_trap_3"))
out.append(choice_block("er_ch5_trap_choice_prompt", [
    {"label": "er_choice_sec40_paid", "args": (40, 500), "desc": "er_ch5_trap_choice_1_desc",
     "cond": "安保声望 ≥ 40；支付 500 信用点", "effect": "好感 +2；安保声望 +5；成功救援"},
    {"label": "er_choice_bm40_paid", "args": (40, 400), "desc": "er_ch5_trap_choice_2_desc",
     "cond": "黑市声望 ≥ 40；支付 400 信用点", "effect": "好感 +1；成功救援"},
    {"label": "er_choice_money", "args": (1500,), "desc": "er_ch5_trap_choice_3_desc",
     "cond": "支付 1500 信用点", "effect": "好感 +0；成功救援"},
    {"label": "er_ch5_trap_choice_4", "desc": "er_ch5_trap_choice_4_desc", "cond": "无",
     "effect": "浪漫线立即锁死；主线结束"},
]))
branches = [
    ("选择 1：用安保关系保她出来", "er_choice_sec40_paid", (40, 500), [
        ("埃莉诺", "er_ch5_trap_after_1"), ("埃莉诺", "er_ch5_1")]),
    ("选择 2：找黑市律师", "er_choice_bm40_paid", (40, 400), [
        ("埃莉诺", "er_ch5_trap_after_2"), ("埃莉诺", "er_ch5_1")]),
    ("选择 3：交巨额保释金", "er_choice_money", (1500,), [
        ("埃莉诺", "er_ch5_trap_after_3"), ("埃莉诺", "er_ch5_1")]),
    ("选择 4：不管她", "er_ch5_trap_choice_4", (), [
        (N(""), "er_ch5_trap_after_4")]),
]
for title, label_key, args, lines in branches:
    out.append(branch(title))
    out.append(say("玩家", label_key, *args))
    for speaker, key in lines:
        out.append(auto(speaker, key))
    if label_key == "er_ch5_trap_choice_4":
        out.append("【系统】浪漫线锁死；不会进入第 6~11 章，也没有每周来访与新开局。\n")
out.append(stage("er_ch5_trap_end"))
out.append("【系统】成功救援后，原版 eleanorArrested 标记被清除；推进到第 6 章（第 31 天）。\n")

# ======================================================================
# 第 5B 章：判决线
# ======================================================================
out.append(section("第 5B 章  母亲的判决（未触发陷阱）\n第 29 天 · 当铺"))
out.append("【说明】若第 4 章选择检查包裹、警告或卖黑市，进入本章。\n")
out.append("【场景】母亲被判二十年。Eleanor 的住处被翻，钱、证件与研究笔记全没了。\n")
out.append(auto("埃莉诺", "er_ch5_1"))
out.append(auto("埃莉诺", "er_ch5_2"))
out.append(auto("埃莉诺", "er_ch5_3"))
out.append(choice_block("er_ch5_choice_prompt", [
    {"label": "er_choice_money", "args": (200,), "desc": "er_ch5_choice_1_desc",
     "cond": "支付 200 信用点", "effect": "好感 +2"},
    {"label": "er_choice_ul20_paid", "args": (20, 300), "desc": "er_ch5_choice_2_desc",
     "cond": "上层区声望 ≥ 20；支付 300 信用点", "effect": "好感 +3；已找律师"},
    {"label": "er_choice_bm20", "args": (20,), "desc": "er_ch5_choice_3_desc",
     "cond": "黑市声望 ≥ 20", "effect": "好感 +2；黑市线索"},
    {"label": "er_ch5_choice_4", "desc": "er_ch5_choice_4_desc", "cond": "无", "effect": "好感 -2"},
]))
branches = [
    ("选择 1：给她食物和现金", "er_choice_money", (200,), [
        ("埃莉诺", "er_ch5_after_1"), ("埃莉诺", "er_ch5_endline")]),
    ("选择 2：帮她找律师", "er_choice_ul20_paid", (20, 300), [
        ("埃莉诺", "er_ch5_after_2"), ("埃莉诺", "er_ch5_endline")]),
    ("选择 3：用黑市门路查案", "er_choice_bm20", (20,), [
        ("埃莉诺", "er_ch5_after_3"), ("埃莉诺", "er_ch5_endline")]),
    ("选择 4：什么都没做", "er_ch5_choice_4", (), [
        (N(""), "er_ch5_after_4")]),
]
for title, label_key, args, lines in branches:
    out.append(branch(title))
    out.append(say("玩家", label_key, *args))
    for speaker, key in lines:
        out.append(auto(speaker, key))
out.append(stage("er_ch5_end"))
out.append("【系统】若第 4 章检查或警告过包裹，本章额外好感 +1。第 5 章结束，推进到第 6 章（第 31 天）。\n")

# ======================================================================
# 第 6 章
# ======================================================================
out.append(section("第 6 章  拾荒的千金\n第 31 天 · 当铺"))
out.append("【场景】Eleanor 带着废弃区捡来的货物来到当铺，手套磨破，指节磨红。\n")
out.append(auto("埃莉诺", "er_ch6_1"))
out.append(auto("埃莉诺", "er_ch6_2"))
out.append(auto("埃莉诺", "er_ch6_3"))
out.append(choice_block("er_ch6_choice_prompt", [
    {"label": "er_ch6_choice_1", "desc": "er_ch6_choice_1_desc", "cond": "无", "effect": "好感 +1"},
    {"label": "er_ch6_choice_2", "args": (150,), "desc": "er_ch6_choice_2_desc",
     "cond": "支付 150 信用点", "effect": "好感 +2"},
    {"label": "er_ch6_choice_3", "desc": "er_ch6_choice_3_desc",
     "cond": "黑市声望 ≥ 20 或 下城区声望 ≥ 20", "effect": "好感 +2；生意课"},
    {"label": "er_ch6_choice_4", "desc": "er_ch6_choice_4_desc", "cond": "无",
     "effect": "好感 -2；本次收购价被压低"},
]))
branches = [
    ("选择 1：按公道价收", "er_ch6_choice_1", (), [
        ("埃莉诺", "er_ch6_after_1"), ("埃莉诺", "er_ch6_endline")]),
    ("选择 2：多付她 150 信用点", "er_ch6_choice_2", (150,), [
        ("埃莉诺", "er_ch6_after_2"), ("埃莉诺", "er_ch6_endline")]),
    ("选择 3：教她辨认赃物和市场价", "er_ch6_choice_3", (), [
        ("埃莉诺", "er_ch6_after_3"), ("埃莉诺", "er_ch6_endline")]),
    ("选择 4：压价", "er_ch6_choice_4", (), [
        (N(""), "er_ch6_after_4")]),
]
for title, label_key, args, lines in branches:
    out.append(branch(title))
    out.append(say("玩家", label_key, *args))
    for speaker, key in lines:
        out.append(auto(speaker, key))
out.append(stage("er_ch6_end"))
out.append("【系统】章节在对话结束时推进到第 7 章（第 33 天）；随后出现的收购交易是可选的额外收益。\n")
out.append("【交易收束】若玩家完成收购：\n")
out.append(stage("er_ch6_done"))
out.append("")

# ======================================================================
# 第 7 章
# ======================================================================
out.append(section("第 7 章  八百信用点的缺口\n第 33 天 · 当铺"))
out.append("【场景】Eleanor 找到愿意进入母亲旧实验室的调查员，需要 800 信用点。\n")
out.append(auto("埃莉诺", "er_ch7_1"))
out.append(auto("埃莉诺", "er_ch7_2"))
out.append(auto("埃莉诺", "er_ch7_3"))
out.append(choice_block("er_ch7_choice_prompt", [
    {"label": "er_ch7_choice_1", "args": (800,), "desc": "er_ch7_choice_1_desc",
     "cond": "支付 800 信用点", "effect": "好感 +3"},
    {"label": "er_choice_bm40_paid", "args": (40, 300), "desc": "er_ch7_choice_2_desc",
     "cond": "黑市声望 ≥ 40；支付 300 信用点", "effect": "好感 +2"},
    {"label": "er_choice_ul40", "args": (40,), "desc": "er_ch7_choice_3_desc",
     "cond": "上层区声望 ≥ 40", "effect": "好感 +2"},
    {"label": "er_ch7_choice_4", "desc": "er_ch7_choice_4_desc", "cond": "无",
     "effect": "好感 +0；第 8 章额外延后 4 天"},
    {"label": "er_ch7_choice_5", "desc": "er_ch7_choice_5_desc", "cond": "无",
     "effect": "好感 -5；永久关闭真结局"},
]))
branches = [
    ("选择 1：借她 800，不收利息", "er_ch7_choice_1", (800,), [
        ("埃莉诺", "er_ch7_after_1"), ("埃莉诺", "er_ch7_endline")]),
    ("选择 2：用黑市借贷帮她凑", "er_choice_bm40_paid", (40, 300), [
        ("埃莉诺", "er_ch7_after_2"), ("埃莉诺", "er_ch7_endline")]),
    ("选择 3：向上层区旧友借", "er_choice_ul40", (40,), [
        ("埃莉诺", "er_ch7_after_3"), ("埃莉诺", "er_ch7_endline")]),
    ("选择 4：让她自己继续攒", "er_ch7_choice_4", (), [
        ("埃莉诺", "er_ch7_after_4")]),
    ("选择 5：提出用感情换钱", "er_ch7_choice_5", (), [
        (N(""), "er_ch7_after_5")]),
]
for title, label_key, args, lines in branches:
    out.append(branch(title))
    out.append(say("玩家", label_key, *args))
    for speaker, key in lines:
        out.append(auto(speaker, key))
out.append(stage("er_ch7_end"))
out.append("【系统】第 7 章结束。正常推进到第 8 章（第 37 天）；选择“让她自己继续攒”则约第 41 天。\n")

# ======================================================================
# 第 8 章
# ======================================================================
out.append(section("第 8 章  U 盘\n第 37 天 · 当铺"))
out.append("【场景】Eleanor 从母亲旧实验室带回 U 盘，手指一直按在内袋上。\n")
out.append(auto("埃莉诺", "er_ch8_1"))
out.append(auto("埃莉诺", "er_ch8_2"))
out.append(auto("埃莉诺", "er_ch8_3"))
out.append(auto("埃莉诺", "er_ch8_4"))
out.append(choice_block("er_ch8_choice_prompt", [
    {"label": "er_ch8_choice_1", "desc": "er_ch8_choice_1_desc", "cond": "无",
     "effect": "好感 +2；共同调查"},
    {"label": "er_choice_ul40", "args": (40,), "desc": "er_ch8_choice_2_desc",
     "cond": "上层区声望 ≥ 40", "effect": "好感 +1；改革派"},
    {"label": "er_ch8_choice_3", "desc": "er_ch8_choice_3_desc", "cond": "无",
     "effect": "好感 +1；公开"},
    {"label": "er_ch8_choice_4", "desc": "er_ch8_choice_4_desc", "cond": "无",
     "effect": "好感 -3；拒绝；不获得研究笔记"},
]))
branches = [
    ("选择 1：替她保管，并一起查下去", "er_ch8_choice_1", (), [
        ("埃莉诺", "er_ch8_after_1"), ("埃莉诺", "er_ch8_endline")]),
    ("选择 2：劝她交给上层区改革派", "er_choice_ul40", (40,), [
        ("埃莉诺", "er_ch8_after_2"), ("埃莉诺", "er_ch8_endline")]),
    ("选择 3：建议她把资料公开", "er_ch8_choice_3", (), [
        ("埃莉诺", "er_ch8_after_3"), ("埃莉诺", "er_ch8_endline")]),
    ("选择 4：拒绝，让她自己处理", "er_ch8_choice_4", (), [
        ("埃莉诺", "er_ch8_after_4")]),
]
for title, label_key, args, lines in branches:
    out.append(branch(title))
    out.append(say("玩家", label_key, *args))
    for speaker, key in lines:
        out.append(auto(speaker, key))
out.append(stage("er_ch8_end"))
out.append("【系统】玩家获得 U 盘。若未选择“拒绝”，同时获得 Paper Book《母亲的研究笔记》。")
out.append("【物品】母亲的研究笔记（三页）")
out.append("　　第一页：营养果压制记录 / 第二页：伪造罪名过程 / 第三页：母亲留下的便条。")
out.append("【系统】第 8 章结束，推进到第 9 章（第 38 天）。\n")

# ======================================================================
# 第 9 章
# ======================================================================
thug = L("er_name_thug")
out.append(section("第 9 章  黑市的问候\n第 38 天 · 当铺"))
out.append("【场景】一个自称帕斯特“清洁工”的男人走进当铺，把没有封口的信封推过柜台。\n")
out.append(auto(thug, "er_ch9_1"))
out.append(auto(thug, "er_ch9_2"))
out.append(auto(thug, "er_ch9_3"))
out.append(choice_block("er_ch9_choice_prompt", [
    {"label": "er_choice_bm40_paid", "args": (40, 200), "desc": "er_ch9_choice_1_desc",
     "cond": "黑市声望 ≥ 40；支付 200 信用点", "effect": "好感 +2；威胁解除"},
    {"label": "er_choice_sec40", "args": (40,), "desc": "er_ch9_choice_2_desc",
     "cond": "安保声望 ≥ 40", "effect": "好感 +2；威胁解除"},
    {"label": "er_choice_bm20_paid", "args": (20, 150), "desc": "er_ch9_choice_3_desc",
     "cond": "黑市声望 ≥ 20；支付 150 信用点", "effect": "好感 +0；获得伪造芯片；真 U 盘仍在"},
    {"label": "er_ch9_choice_4", "desc": "er_ch9_choice_4_desc", "cond": "无",
     "effect": "好感 +1；硬扛"},
    {"label": "er_ch9_choice_5", "desc": "er_ch9_choice_5_desc", "cond": "无",
     "effect": "好感 -5；失去 U 盘；真结局/回归/泄露/卖盘全部关闭"},
]))
branches = [
    ("选择 1：用黑市地位让他滚", "er_choice_bm40_paid", (40, 200), [
        (N(""), "er_ch9_after_1"), (N(""), "er_ch9_endline")]),
    ("选择 2：用安保关系反查他", "er_choice_sec40", (40,), [
        (N(""), "er_ch9_after_2"), (N(""), "er_ch9_endline")]),
    ("选择 3：收下钱，交出一个假 U 盘", "er_choice_bm20_paid", (20, 150), [
        (N(""), "er_ch9_after_3"), (N(""), "er_ch9_endline")]),
    ("选择 4：拒绝，让他回去告诉帕斯特“等着”", "er_ch9_choice_4", (), [
        (N(""), "er_ch9_after_4"), (N(""), "er_ch9_endline")]),
    ("选择 5：交出真 U 盘", "er_ch9_choice_5", (), [
        (N(""), "er_ch9_after_5")]),
]
for title, label_key, args, lines in branches:
    out.append(branch(title))
    out.append(say("玩家", label_key, *args))
    for speaker, key in lines:
        out.append(auto(speaker, key))
    if label_key == "er_choice_bm20_paid":
        out.append("【获得物品】" + L("er_item_decoy_name"))
        out.append("　　" + L("er_item_decoy_desc"))
        out.append("　　" + L("er_item_decoy_flavor") + "\n")
    if label_key == "er_ch9_choice_5":
        out.append("【系统】U 盘丢失；第 10 章只剩“劝她放弃”可选。\n")
out.append(stage("er_ch9_end"))
out.append("【系统】第 9 章结束，推进到第 10 章（第 40 天）。\n")

# ======================================================================
# 第 10 章
# ======================================================================
out.append(section("第 10 章  最后一张牌\n第 40 天 · 当铺"))
out.append("【场景】帕斯特给出最后期限。Eleanor 站在柜台前，没有低头。\n")
out.append("【版本 A：正常持有 U 盘】")
out.append(auto("埃莉诺", "er_ch10_1"))
out.append(auto("埃莉诺", "er_ch10_2"))
out.append(auto("埃莉诺", "er_ch10_3"))
out.append(stage("er_ch10_requirements"))
out.append(auto("埃莉诺", "er_ch10_4"))
out.append("")
out.append("【版本 B：第 9 章交出真 U 盘】")
out.append(auto("埃莉诺", "er_ch10_1_lost"))
out.append(auto("埃莉诺", "er_ch10_2_lost"))
out.append(auto("埃莉诺", "er_ch10_3"))
out.append(stage("er_ch10_requirements"))
out.append(auto("埃莉诺", "er_ch10_4"))
out.append("")
out.append(choice_block("er_ch10_choice_prompt", [
    {"label": "er_ch10_choice_true_rep", "args": (60, 800, 12),
     "desc": "er_ch10_choice_true_rep_desc",
     "cond": "好感 ≥ 12；未失去 U 盘；未利用她；未锁死浪漫线；任一势力声望 ≥ 60；支付 800 信用点",
     "effect": "母亲获救；真结局；解锁新开局；释放 Nutrifruit；生成新闻"},
    {"label": "er_ch10_choice_true_money", "args": (2500, 12),
     "desc": "er_ch10_choice_true_money_desc",
     "cond": "好感 ≥ 12；未失去 U 盘；未利用她；未锁死浪漫线；支付 2500 信用点",
     "effect": "母亲获救；真结局；解锁新开局；释放 Nutrifruit；生成新闻"},
    {"label": "er_ch10_choice_return", "desc": "er_ch10_choice_return_desc",
     "cond": "上层区声望 ≥ 20；未失去 U 盘", "effect": "母亲流放；浪漫线继续；不解锁新开局"},
    {"label": "er_ch10_choice_leak", "desc": "er_ch10_choice_leak_desc",
     "cond": "未失去 U 盘", "effect": "母亲死亡；Eleanor 加入革命；不解锁新开局"},
    {"label": "er_ch10_choice_giveup", "desc": "er_ch10_choice_giveup_desc",
     "cond": "无", "effect": "母亲活着但疏远；浪漫线锁死"},
    {"label": "er_ch10_choice_bm", "args": (20,), "desc": "er_ch10_choice_bm_desc",
     "cond": "黑市声望 ≥ 20；未失去 U 盘", "effect": "获得 1000 信用点；母亲死亡；浪漫线锁死"},
]))
branches = [
    ("真结局 · 人脉路线：联合多方保下母亲", "er_ch10_choice_true_rep", (60, 800, 12), [
        (N(""), "er_ch10_true_1"), (N(""), "er_ch10_true_2"), (N(""), "er_ch10_true_3")]),
    ("真结局 · 金钱路线：砸钱买通整条链", "er_ch10_choice_true_money", (2500, 12), [
        (N(""), "er_ch10_true_1"), (N(""), "er_ch10_true_2"), (N(""), "er_ch10_true_3")]),
    ("回归路线：把 U 盘交给上层区法官", "er_ch10_choice_return", (), [
        (N(""), "er_ch10_return_1"), (N(""), "er_ch10_return_2")]),
    ("泄露路线：把 U 盘交给下层区报纸", "er_ch10_choice_leak", (), [
        (N(""), "er_ch10_leak_1"), (N(""), "er_ch10_leak_2"), (N(""), "er_ch10_leak_3")]),
    ("放弃路线：劝她放弃，保住自己", "er_ch10_choice_giveup", (), [
        (N(""), "er_ch10_giveup_1"), (N(""), "er_ch10_giveup_2")]),
    ("黑市卖盘：把 U 盘卖给黑市", "er_ch10_choice_bm", (20,), [
        (N(""), "er_ch10_bm_1"), (N(""), "er_ch10_bm_2")]),
]
for title, label_key, args, lines in branches:
    out.append(branch(title))
    out.append(say("玩家", label_key, *args))
    for speaker, key in lines:
        out.append(auto(speaker, key))
out.append("【共同收束：以上所有路线都会到达这里】\n")
out.append(stage("er_ch10_end"))
out.append("【系统】第 10 章结束。第 11 章在数日后触发；被锁死的路线进入短尾声或直接结束。\n")

# ======================================================================
# 第 11 章
# ======================================================================
out.append(section("第 11 章  长尾声\n第 43 天 · 当铺"))
out.append("【说明】本章按第 10 章路线分为四个版本。\n")

out.append("【11A · 真结局：母亲获救】")
out.append(stage("er_ch11_true_1"))
out.append(stage("er_ch11_true_2"))
out.append(stage("er_ch11_true_3"))
out.append(say("埃莉诺", "er_ch11_true_4"))
out.append(say("埃莉诺", "er_ch11_true_5"))
out.append(say("埃莉诺", "er_ch11_true_6"))
out.append(choice_block("er_ch11_choice_prompt", [
    {"label": "er_ch11_choice_stay", "desc": "er_ch11_choice_stay_desc", "cond": "无",
     "effect": "+10 下城区声望；每周来访；无上层区渠道"},
    {"label": "er_ch11_choice_return", "desc": "er_ch11_choice_return_desc", "cond": "无",
     "effect": "+10 上层区声望；每周来访；每 14 天上层区渠道"},
]))
out.append(branch("选择 1：让她留在当铺，做合伙人"))
out.append(say("玩家", "er_ch11_choice_stay"))
out.append(say("埃莉诺", "er_ch11_stay_1"))
out.append(say("埃莉诺", "er_ch11_stay_2"))
out.append(stage("er_ch11_stay_3"))
out.append(branch("选择 2：支持她回上层区，重建母亲的研究"))
out.append(say("玩家", "er_ch11_choice_return"))
out.append(say("埃莉诺", "er_ch11_return_1"))
out.append(stage("er_ch11_return_2"))
out.append(stage("er_ch11_return_3"))
out.append("【共同收束：留在当铺与回上层区两个选择共用这段尾声】\n")
out.append(stage("er_ch11_end"))
out.append("")

out.append("【11B · 回归路线：母亲被流放】")
out.append(stage("er_ch11_returnroute_1"))
out.append(stage("er_ch11_returnroute_2"))
out.append(say("埃莉诺", "er_ch11_returnroute_3"))
out.append(stage("er_ch11_returnroute_4"))
out.append(stage("er_ch11_end"))
out.append("")

out.append("【11C · 泄露路线：母亲死亡，Eleanor 加入革命】")
out.append(stage("er_ch11_leak_1"))
out.append(say("埃莉诺", "er_ch11_leak_2"))
out.append(say("埃莉诺", "er_ch11_leak_3"))
out.append(stage("er_ch11_leak_4"))
out.append(stage("er_ch11_end"))
out.append("")

out.append("【11D · 锁死路线：放弃 / 卖盘 / 放弃救援】")
out.append(stage("er_ch11_bad_1"))
out.append(stage("er_ch11_bad_2"))
out.append(stage("er_ch11_bad_3"))
out.append(stage("er_ch11_end"))
out.append("")

# ======================================================================
# 常驻内容
# ======================================================================
out.append(section("常驻内容（第 11 章之后）"))
out.append("【每周来访 · 真结局】")
out.append(auto("埃莉诺", "er_weekly_1"))
out.append(auto("埃莉诺", "er_weekly_2"))
out.append(stage("er_weekly_3"))
out.append(choice_block("er_weekly_prompt", [
    {"label": "er_weekly_choice_1", "cond": "无", "effect": "好感/关系文本"},
    {"label": "er_weekly_choice_2", "cond": "无", "effect": "好感/关系文本"},
]))
out.append(branch("选择 1：收下，给她倒杯水"))
out.append(say("玩家", "er_weekly_choice_1"))
out.append(say("埃莉诺", "er_weekly_after_1"))
out.append(branch("选择 2：问她下次什么时候来"))
out.append(say("玩家", "er_weekly_choice_2"))
out.append(say("埃莉诺", "er_weekly_after_2"))
out.append(stage("er_weekly_end"))
out.append("【系统】真结局每周来访：100 信用点；+1 声望（回上层区路线加上层区，留店路线加下城区）。\n")

out.append("【每周来访 · 回归/泄露路线】")
out.append(auto("埃莉诺", "er_weekly_quiet_1"))
out.append(auto("埃莉诺", "er_weekly_quiet_2"))
out.append(stage("er_weekly_quiet_3"))
out.append(choice_block("er_weekly_prompt", [
    {"label": "er_weekly_choice_1", "cond": "无", "effect": "好感/关系文本"},
    {"label": "er_weekly_choice_2", "cond": "无", "effect": "好感/关系文本"},
]))
out.append(branch("选择 1：收下，给她倒杯水"))
out.append(say("玩家", "er_weekly_choice_1"))
out.append(say("埃莉诺", "er_weekly_quiet_after_1"))
out.append(branch("选择 2：问她下次什么时候来"))
out.append(say("玩家", "er_weekly_choice_2"))
out.append(say("埃莉诺", "er_weekly_quiet_after_2"))
out.append(stage("er_weekly_quiet_end"))
out.append("【系统】非真结局每周来访：50 信用点；+1 下城区声望。\n")

broker = L("er_name_upper_contact")
out.append("【上层区渠道 · 仅真结局“回上层区”路线】")
out.append("【场景】一位上层区中间人走进当铺，带着采购清单。\n")
out.append(auto(broker, "er_upper_1"))
out.append(auto(broker, "er_upper_2"))
out.append(auto(broker, "er_upper_3"))
out.append("【系统】上层区采购客户：预算 800，收购奢侈品/艺术品/高端货；首次成交上层区声望 +1。")
out.append("【提示】" + L("er_upper_deal") + "\n")

# ======================================================================
# 旧档兼容
# ======================================================================
archivist = L("er_name_archivist")
out.append(section("旧档兼容事件（检测到原版 Eleanor 进度）"))
out.append("【场景】下层区档案员把一份旧档案放在柜台上。\n")
out.append(auto(archivist, "er_old_prompt_1"))
out.append(auto(archivist, "er_old_prompt_2"))
out.append(stage("er_old_prompt_choice_prompt"))
out.append("【选择】")
out.append("  1. " + L("er_old_prompt_reset"))
out.append("     [提示] " + L("er_old_prompt_reset_desc"))
out.append("     [效果] 清除原版与 MOD 的 Eleanor 剧情进度；已获得物品保留；从下一次开店开始重制线")
out.append("  2. " + L("er_old_prompt_keep"))
out.append("     [提示] " + L("er_old_prompt_keep_desc"))
out.append("     [效果] 本存档继续运行原版 Eleanor 剧情")
out.append(branch("选择 1：重置埃莉诺剧情线"))
out.append(say("玩家", "er_old_prompt_reset"))
out.append(stage("er_old_prompt_reset_line"))
out.append("【系统】" + L("er_notice_reset_done"))
out.append(branch("选择 2：保留原版埃莉诺剧情"))
out.append(say("玩家", "er_old_prompt_keep"))
out.append(stage("er_old_prompt_keep_line"))
out.append("【系统】" + L("er_notice_keep_vanilla"))
out.append("")

# ======================================================================
# 附录
# ======================================================================
out.append(section("附录 A  开局特性“旧日回想”"))
out.append("【名称】" + L("er_early_start_name"))
out.append("【说明】" + L("er_early_start_desc") + "\n")

out.append(section("附录 B  物品与纸书"))
out.append("【物品】" + L("er_item_locket_name"))
out.append("  描述：" + L("er_item_locket_desc"))
out.append("  风味：" + L("er_item_locket_flavor"))
out.append("【物品】" + L("er_item_decoy_name"))
out.append("  描述：" + L("er_item_decoy_desc"))
out.append("  风味：" + L("er_item_decoy_flavor"))
out.append("")
if os.path.exists(BOOK_PATH):
    with open(BOOK_PATH, encoding="utf-8") as f:
        book = json.load(f)
    out.append("【Paper Book】" + book.get("title", "Mother's Research Notes"))
    for page in book.get("pages", []):
        out.append("　— " + page.get("name", ""))
        for el in page.get("elements", []):
            if el.get("type") == "text":
                zh = el.get("translations", {}).get("zh", el.get("text", ""))
                out.append("　　" + zh)
        out.append("")
    item = book.get("item", {})
    out.append("【纸书物品】" + item.get("name", ""))
    out.append("　　" + item.get("description", ""))
out.append("")

out.append(section("附录 C  结局变体与系统提示"))
for key in ["er_ending_variant_true", "er_ending_variant_normal", "er_ending_variant_locked"]:
    out.append("【结局变体】" + L(key))
out.append("")
for key in ["er_notice_old_save", "er_notice_version", "er_notice_early_start",
            "er_notice_true_unlock", "er_notice_trust", "er_notice_deal_first",
            "er_notice_queue_now", "er_notice_reset_arm", "er_notice_reset_done",
            "er_notice_keep_vanilla", "er_notice_story_inactive"]:
    out.append("【系统提示】" + L(key))
out.append("")

out.append(section("附录 D  供评估的剧情/数值问题"))
out.append("1. 好感 12 是否太容易？")
out.append("   无势力声望也可达约 19，真结局真正的门槛是声望 60/金钱。")
out.append("   可考虑：提高到 15~16；或保持 12，但让好感影响尾声质量。")
out.append("")
out.append("2. 真结局双门槛是否合理？")
out.append("   任一势力 60 + 800 信用点，或 2500 信用点。")
out.append("   需评估第 40 天前达到 60 声望的现实难度；若太紧，可降到 50。")
out.append("")
out.append("3. 第 8 章态度选择目前主要影响文本与《母亲的研究笔记》。")
out.append("   可考虑：共同调查降低真结局声望门槛；改革派让回归路线母亲不被流放；")
out.append("   公开让泄露路线出现母亲存活机会。")
out.append("")
out.append("4. 第 9 章解决威胁的方式目前主要影响好感与文本，只有交出真 U 盘会锁死后续。")
out.append("   可考虑让黑市/安保/假 U 盘分别改变最终章成本或新增限时事件。")
out.append("")
out.append("5. 恋爱浓度是否足够？")
out.append("   目前以信任、让利、共同承担风险为主，第 11 章有暧昧台词但没有明确告白。")
out.append("   可考虑在第 10 章真结局或第 11 章加入一段明确告白/拥抱。")
out.append("")
out.append("6. “低调大佬”的爽点是否足够？")
out.append("   人脉通过声望选项体现，但缺少一个 Eleanor 亲眼看见玩家动用大势力的集中高潮。")
out.append("   可考虑在第 5A 救援或第 9 章威胁加一段专属演出。")
out.append("")
out.append("7. 反派是否太“幕后”？")
out.append("   帕斯特始终在幕后，台前只有舅舅和清洁工。")
out.append("   可考虑新增一个可打脸的帕斯特中层客户。")
out.append("")
out.append("8. 母亲是否应该正面登场？")
out.append("   真结局目前只有旁白“她站在监狱门口”。")
out.append("   可考虑在真结局尾声加一个母亲来访的小场景。")
out.append("")
out.append("9. 第 11 章之后的内容是否太晚？")
out.append("   普通模式第 43 天才结束，常驻内容体验时间可能偏短。")
out.append("   可考虑把第 11 章提前到第 38~40 天，或让常驻内容在真结局后立即出现一次。")
out.append("")
out.append("10. 舅舅陷阱的惩罚是否过重？")
out.append("    不检查/不警告会进入逮捕线；没有 SEC40/BM40 时只能交 1500 保释金或放弃她。")
out.append("    可考虑增加一个无门槛但代价更高的补救选项，或把“不管她”改成延迟补救线。")
out.append("")
out.append("11. 第 6 章的交易是否足够有玩法？")
out.append("    章节在对话结束时推进，收购是可选收益。")
out.append("    可考虑把“是否实际买下她的货”纳入好感或后续文本。")
out.append("")
out.append("12. 目前只做风味、尚未参与硬判定的旗标：")
out.append("    已检查包裹、已警告、已出售包裹、已找律师、黑市线索、生意课、")
out.append("    共同调查、改革派、公开、拒绝、假货、硬扛。")
out.append("    如果希望“选择有后果”，优先把第 8 章态度与第 9 章解决方式接入最终章门槛。")
out.append("")

out.append(section("附录 E  解锁逻辑速查"))
out.append("【真结局】")
out.append("  好感 ≥ 12")
out.append("  且 未失去 U 盘（第 9 章未交出真盘）")
out.append("  且 未选择“用感情换钱”（第 7 章 E）")
out.append("  且 浪漫线未锁死")
out.append("  且（任一势力声望 ≥ 60 且支付 800 信用点，或 支付 2500 信用点）")
out.append("")
out.append("【回归路线】上层区声望 ≥ 20，且未失去 U 盘；母亲流放，浪漫线继续。")
out.append("【泄露路线】未失去 U 盘；母亲死亡，Eleanor 加入革命。")
out.append("【放弃路线】永远可选；母亲活着但疏远，浪漫线锁死。")
out.append("【黑市卖盘】黑市声望 ≥ 20，且未失去 U 盘；获得 1000 信用点，母亲死亡，浪漫线锁死。")
out.append("")
out.append("【第 5A 逮捕救援】")
out.append("  安保声望 ≥ 40 + 500 信用点")
out.append("  或 黑市声望 ≥ 40 + 400 信用点")
out.append("  或 1500 信用点")
out.append("  否则只能“不管她”，主线立即结束。")
out.append("")
out.append("【声望门槛速查】")
out.append("  黑市 20：第1章食材、第4章卖包裹、第5B线索、第6章生意课、第9章假U盘、第10章卖盘")
out.append("  黑市 40：第5A律师、第7章借贷、第9章解决威胁")
out.append("  黑市/上层区/安保/下城区 60：第10章真结局人脉路线（任一即可）")
out.append("  上层区 20：第3章压丑闻、第5B律师、第10章回归路线")
out.append("  上层区 40：第7章借款、第8章改革派")
out.append("  下城区 20：第2章住处、第6章生意课")
out.append("  安保 40：第5A救援、第9章反查威胁者")
out.append("")
out.append("【重要状态】")
out.append("  DriveLost：第9章交出真U盘；关闭真结局/回归/泄露/卖盘。")
out.append("  Exploited：第7章用感情换钱；永久关闭真结局。")
out.append("  RomanceLocked：第5A放弃救援，或第10章放弃/卖盘；无每周来访。")
out.append("  TrueEnding：第10章真结局。")
out.append("")
out.append("【日程】")
out.append("  普通模式：23 / 25 / 27 / 28 / 29 / 31 / 33 / 37 / 38 / 40 / 43 天")
out.append("  旧日回想：第 12 天开始，埃莉诺剧情目标日程提前 11 天。")
out.append("  漏章：下一次开店补触发；每个营业日至多排入 1 个 Eleanor 剧情客户。")
out.append("")

# 把所有未在正文使用的文本也列出，保证没有遗漏
unused = [k for k in sorted(vals) if k not in used]
if unused:
    out.append(section("附录 F  其他未在正文引用的文本"))
    for key in unused:
        out.append(key + "：" + vals[key])
    out.append("")

os.makedirs(os.path.dirname(OUT_PATH), exist_ok=True)
with open(OUT_PATH, "w", encoding="utf-8-sig") as f:
    f.write("\n".join(out))

print("Wrote", OUT_PATH)
print("Used keys:", len(used))
print("Unused keys:", len(unused))
if unused:
    print("Unused:", unused)
