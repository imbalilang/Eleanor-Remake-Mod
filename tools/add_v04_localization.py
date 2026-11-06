#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Append v0.4 visitor/message localization keys to zh.csv and en.csv."""

import csv
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

zh = {
    "er_name_victor": "退婚经纪人维克多",
    "er_name_lydia": "上层区旧友莉迪亚",
    "er_name_harvey": "帕斯特主管哈维",
    "er_name_mother": "埃莉诺的母亲",
    "er_vis_victor_1": "埃莉诺小姐。塞拉斯少爷让我来取回订婚戒指。还有，他说——",
    "er_vis_victor_2": "请你以后不要再自称‘塞拉斯少爷的未婚妻’。上层区的人，脸皮薄。",
    "er_vis_victor_3": "这就是你现在的‘体面’？埃莉诺小姐，你母亲的事，本来可以更体面地结束。是你自己不肯低头。",
    "er_vis_victor_4": "（埃莉诺没有回答。）",
    "er_vis_victor_prompt": "（维克多等着你开口。）",
    "er_vis_victor_opt_out": "不出面，让她自己解决。",
    "er_vis_victor_opt_ul": "用上层区关系压住维克多（需要上层区声望 20）。",
    "er_vis_victor_opt_bm": "用黑市门路提醒维克多（需要黑市声望 20）。",
    "er_vis_victor_opt_kick": "直接把他赶出去。",
    "er_vis_victor_react_out": "维克多离开了。埃莉诺没有看你。",
    "er_vis_victor_react_ul": "维克多认出了你背后的人，笑容僵在脸上，退了出去。",
    "er_vis_victor_react_bm": "维克多没再提塞拉斯，走得很快。",
    "er_vis_victor_react_kick": "维克多被赶了出去。埃莉诺没有道谢。",
    "er_vis_victor_leave": "（维克多离开后，当铺安静下来。）",
    "er_vis_lydia_1": "埃莉诺，你还要闹到什么时候？塞拉斯那边已经松口了。只要你放弃调查，他可以当什么都没发生。",
    "er_vis_lydia_2": "然后呢？继续做你们圈子里那个‘被退婚的可怜虫’？",
    "er_vis_lydia_3": "至少你还是上层区的人。",
    "er_vis_lydia_4": "我现在也是下层区的人。至少这里的人不会在我落难的时候换号码。",
    "er_vis_lydia_5": "你给她灌了什么迷魂汤？",
    "er_vis_lydia_prompt": "（莉迪亚看向你。）",
    "er_vis_lydia_opt_out": "不插话，让她自己处理。",
    "er_vis_lydia_opt_ul": "用上层区关系让莉迪亚闭嘴（需要上层区声望 20）。",
    "er_vis_lydia_opt_bm": "用黑市门路‘送客’（需要黑市声望 20）。",
    "er_vis_lydia_react_out": "埃莉诺把莉迪亚请了出去。",
    "er_vis_lydia_react_ul": "莉迪亚认出了你背后的人，悻悻离开。",
    "er_vis_lydia_react_bm": "莉迪亚走得更快，埃莉诺皱眉看了你一眼。",
    "er_vis_lydia_leave": "（莉迪亚离开后，埃莉诺没有说话。）",
    "er_vis_harvey_1": "埃莉诺小姐的母亲，曾经是帕斯特最好的农场主管。我们很尊重她。",
    "er_vis_harvey_2": "但尊重不等于纵容。她手里的东西，如果流出去，很多人会失去工作。包括你这样的下层区店主。",
    "er_vis_harvey_3": "考虑一下。我们给的价钱，比她的调查员高得多。",
    "er_vis_harvey_prompt": "（哈维等着你的回答。）",
    "er_vis_harvey_opt_refuse": "不接话，拒绝。",
    "er_vis_harvey_opt_ul": "用上层区关系反问（需要上层区声望 20）。",
    "er_vis_harvey_opt_bm": "用黑市门路威胁（需要黑市声望 20）。",
    "er_vis_harvey_react_refuse": "哈维看了你一会儿，离开了。",
    "er_vis_harvey_react_ul": "哈维记住了你。",
    "er_vis_harvey_react_bm": "哈维离开时没有再说一句话。",
    "er_vis_harvey_leave": "（哈维离开后，店里的空气才松下来。）",
    "er_vis_victor2_1": "埃莉诺小姐……这是误会。塞拉斯少爷一直很关心你。",
    "er_vis_victor2_2": "那你告诉他，我现在关心的东西，他赔不起。",
    "er_vis_victor2_3": "我们走。",
    "er_vis_victor2_leave": "（维克多站在原地，没有再追上来。）",
    "er_vis_mother_1": "你怎么穿这么少。",
    "er_vis_mother_2": "妈。",
    "er_vis_mother_3": "别欺负我女儿。",
    "er_vis_mother_4": "这就是你待的地方？",
    "er_vis_mother_5": "嗯。",
    "er_vis_mother_6": "比实验室暖和。",
    "er_vis_mother_leave": "（母亲没有久留。她走的时候，埃莉诺一直看着门口。）",
    "er_msg_lydia_ch2": "【通讯记录】莉迪亚：埃莉诺？你还在下层区？我还以为你只是去散心。听说你连公寓都……",
    "er_msg_eleanor_ch4": "【通讯记录】埃莉诺：我舅舅是不是找过你？别信他。他以前帮我母亲处理过账目，后来突然不见了。",
    "er_sys_security": "【系统通知】埃莉诺。持有伪造赃物清单，疑似接货。保释金 1500，或等待调查。临时放行：1 小时。",
    "er_msg_lydia_ch6": "【通讯记录】莉迪亚：你还在跟那个当铺老板混在一起？埃莉诺，你不会真打算留在下层区吧？",
    "er_msg_loan_ch7": "【通讯记录】放贷人：听说她在找调查员？八百信用点，不多。但你要想清楚，这钱借出去，可不一定收得回来。",
    "er_msg_victor_ch7": "【通讯记录】维克多：上层区有人想知道，埃莉诺小姐的调查进行到哪一步了。好心提醒：有些事，查得太深，对谁都不好。",
    "er_news_engagement": "【新闻】上层区社交圈：某水培农场前主管之女被退婚。知情人士称，塞拉斯家族已收回订婚戒指。",
    "er_news_multi": "【新闻/案卷摘要】上层区的律师把旧案卷送进法官办公室；下城区的街坊堵住帕斯特的仓库；黑市的中间人把假账复印件塞进新闻投递口；安保的巡逻车在帕斯特总部停了一整夜。",
    "er_news_pastel_fall": "【新闻】帕斯特仓库被查封。合规部主管哈维被带走调查。配给价格将重新审核。",
}

en = {
    "er_name_victor": "Victor, the broken-engagement broker",
    "er_name_lydia": "Lydia, Eleanor's upper-district friend",
    "er_name_harvey": "Harvey, Pastel supervisor",
    "er_name_mother": "Eleanor's Mother",
    "er_vis_victor_1": "Miss Eleanor. Master Silas sent me to collect the engagement ring. And he also said—",
    "er_vis_victor_2": "He said you should stop calling yourself 'Master Silas's fiancee'. Upper-district people are thin-skinned.",
    "er_vis_victor_3": "Is this what passes for 'dignity' now? Miss Eleanor, your mother's case could have ended more quietly. You were the one who refused to bow.",
    "er_vis_victor_4": "(Eleanor does not answer.)",
    "er_vis_victor_prompt": "(Victor waits for you to speak.)",
    "er_vis_victor_opt_out": "Stay out of it and let her handle him.",
    "er_vis_victor_opt_ul": "Use upper-district connections to pressure Victor (Upper District rep 20).",
    "er_vis_victor_opt_bm": "Use black-market leverage to warn Victor (Black Market rep 20).",
    "er_vis_victor_opt_kick": "Throw him out.",
    "er_vis_victor_react_out": "Victor leaves. Eleanor does not look at you.",
    "er_vis_victor_react_ul": "Victor recognises the people behind you. His smile freezes, and he backs out.",
    "er_vis_victor_react_bm": "Victor stops mentioning Silas and leaves quickly.",
    "er_vis_victor_react_kick": "Victor is thrown out. Eleanor does not thank you.",
    "er_vis_victor_leave": "(After Victor leaves, the pawnshop goes quiet.)",
    "er_vis_lydia_1": "Eleanor, how long are you going to keep this up? Silas has softened. If you drop the investigation, he can pretend none of this happened.",
    "er_vis_lydia_2": "And then what? Go back to being the 'jilted heiress' of your circle?",
    "er_vis_lydia_3": "At least you would still be an upper-district person.",
    "er_vis_lydia_4": "I am a lower-level person now too. At least people here don't change their numbers when I fall.",
    "er_vis_lydia_5": "What did you pour into her head?",
    "er_vis_lydia_prompt": "(Lydia looks at you.)",
    "er_vis_lydia_opt_out": "Say nothing and let her handle it.",
    "er_vis_lydia_opt_ul": "Use upper-district connections to silence Lydia (Upper District rep 20).",
    "er_vis_lydia_opt_bm": "Use black-market leverage to 'see her out' (Black Market rep 20).",
    "er_vis_lydia_react_out": "Eleanor shows Lydia out.",
    "er_vis_lydia_react_ul": "Lydia recognises the people behind you and leaves in a hurry.",
    "er_vis_lydia_react_bm": "Lydia leaves faster; Eleanor frowns at you.",
    "er_vis_lydia_leave": "(After Lydia leaves, Eleanor says nothing.)",
    "er_vis_harvey_1": "Eleanor's mother was once Pastel's best hydroponics supervisor. We respect her.",
    "er_vis_harvey_2": "But respect is not indulgence. If what she has gets out, many people will lose their jobs. Including shopkeepers like you.",
    "er_vis_harvey_3": "Think about it. We pay much better than her investigator.",
    "er_vis_harvey_prompt": "(Harvey waits for your answer.)",
    "er_vis_harvey_opt_refuse": "Say nothing and refuse.",
    "er_vis_harvey_opt_ul": "Use upper-district connections to push back (Upper District rep 20).",
    "er_vis_harvey_opt_bm": "Use black-market leverage to threaten him (Black Market rep 20).",
    "er_vis_harvey_react_refuse": "Harvey looks at you for a moment, then leaves.",
    "er_vis_harvey_react_ul": "Harvey remembers you.",
    "er_vis_harvey_react_bm": "Harvey leaves without another word.",
    "er_vis_harvey_leave": "(After Harvey leaves, the shop can breathe again.)",
    "er_vis_victor2_1": "Miss Eleanor... this is a misunderstanding. Master Silas has always cared about you.",
    "er_vis_victor2_2": "Then tell him the thing I care about now is something he cannot afford.",
    "er_vis_victor2_3": "Let's go.",
    "er_vis_victor2_leave": "(Victor stands there and does not follow.)",
    "er_vis_mother_1": "Why are you dressed so lightly?",
    "er_vis_mother_2": "Mom.",
    "er_vis_mother_3": "Don't bully my daughter.",
    "er_vis_mother_4": "Is this where you stay?",
    "er_vis_mother_5": "Yes.",
    "er_vis_mother_6": "Warmer than the lab.",
    "er_vis_mother_leave": "(Mother does not stay long. After she leaves, Eleanor keeps looking at the door.)",
    "er_msg_lydia_ch2": "[Message] Lydia: Eleanor? You're still in the lower levels? I thought you were just taking a break. I heard you lost the apartment too...",
    "er_msg_eleanor_ch4": "[Message] Eleanor: Did my uncle come to see you? Don't trust him. He used to handle Mother's accounts, then he vanished.",
    "er_sys_security": "[System] Eleanor. Held on suspicion of receiving forged stolen-goods evidence. Bail 1,500, or wait for investigation. Temporary release: 1 hour.",
    "er_msg_lydia_ch6": "[Message] Lydia: You're still mixed up with that pawnshop owner? Eleanor, you're not really staying in the lower levels, are you?",
    "er_msg_loan_ch7": "[Message] Loan Shark: I hear she's looking for an investigator? Eight hundred credits, not much. But think carefully. Money lent here isn't always paid back in credits.",
    "er_msg_victor_ch7": "[Message] Victor: Someone in the upper district wants to know how far Miss Eleanor's investigation has gone. A friendly warning: some things are dangerous to dig into.",
    "er_news_engagement": "[News] Upper-district social circle: the daughter of a former hydroponics supervisor has been jilted. Sources say the Silas family has taken back the engagement ring.",
    "er_news_multi": "[News/File Summary] An upper-district lawyer delivers an old case file to a judge's office; lower-level neighbours block Pastel's ration warehouse; black-market brokers stuff copies of Pastel's fake ledger into newspaper drop boxes; a security patrol car sits outside Pastel HQ all night.",
    "er_news_pastel_fall": "[News] Pastel warehouses sealed. Harvey of the compliance department detained for investigation. Ration prices to be reviewed.",
}

for code, table in (("zh", zh), ("en", en)):
    path = os.path.join(ROOT, "Localization", code + ".csv")
    rows = []
    existing = set()
    with open(path, encoding="utf-8-sig") as f:
        for row in csv.reader(f):
            if len(row) >= 2:
                rows.append(row)
                existing.add(row[0])
    added = 0
    with open(path, "w", encoding="utf-8-sig", newline="") as f:
        writer = csv.writer(f, quoting=csv.QUOTE_ALL, lineterminator="\n")
        for row in rows:
            writer.writerow(row)
        for key, value in table.items():
            if key not in existing:
                writer.writerow([key, value])
                added += 1
    print(code, "added", added)
