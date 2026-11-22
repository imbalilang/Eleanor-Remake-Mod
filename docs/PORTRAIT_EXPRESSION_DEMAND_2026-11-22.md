# 立绘表情需求与降配建议 2026-11-22

只统计运行时会真正解析到的表情：当前可见 client 的 spriteName 决定基础立绘，且表情行 speaker 必须等于该 client 的 displayName。因此 backup、SEC、旁白、以及当前 client 不是该角色时的表情行不计。

统计来源：maid_v2 与 revolution_v2 用场景 portrait/client；love 用 LoveStory client；story_v06 用章节 client；dual_romance_v04 用场景主 client；maid_v05 默认 scav，m19/m20/m23/m28 为 maid。

| 基础立绘 | 可解析表情数 | 主要表情 | 建议 |
|---|---:|---|---|
| eleanor1 | 108 | shy27 soft23 serious20 angry15 smile12 | L1 |
| eleanor_scav | 93 | serious20 soft19 shy16 angry10 determined10 | L1 |
| eleanor_gala | 46 | serious9 smile9 shy8 soft6 angry5 | L1 |
| eleanor_maid | 26 | soft9 shy8 serious4 | L2 |
| eleanor_rev | 62 | serious13 tired7 angry3 determined3 | L2 |
| eleanor_revolution_undersuit | 45 | tired9 serious5 smile4 soft4 | L2 |
| lydia_daily | 117 | serious36 smile27 hurt24 shy8 | L1 |
| lydia_date | 15 | smile5 serious4 shy4 hurt2 | L2 |
| sophia | 24 | serious9 soft5 tired5 smile4 | L2 |
| sophia_home | 13 | serious5 smile4 soft3 tired1 | L2 |
| eleanor_love_intimate | 0 | - | L3 |

判定：0-2 次为 L3；核心六项 serious/smile/hurt/shy/soft/tired 多项 >=4，且其他 <=3 为 L2；核心六项外仍有 >=4 次表情为 L1。gala 的 angry=5 保留 L1；rev 与 undersuit 的非核心表情为 3 次，降 L2。

建议层级：L1（4）eleanor1、eleanor_scav、eleanor_gala、lydia_daily；L2（6）eleanor_maid、eleanor_rev、eleanor_revolution_undersuit、lydia_date、sophia、sophia_home；L3（5）eleanor_love_intimate + 原 L3 四件；L4 五件；L5 一件。

工作量：4x10 + 6x6 = 76 个差分，比原 94 少 18 个（约 19%）。

补充：L1 中 eleanor1/scav/gala 补 hurt、tsun；lydia_daily 补 soft、sad、tsun。L2 中 maid 补 hurt；undersuit、sophia、sophia_home 补 hurt、shy；lydia_date 补 soft、tired；rev 已齐。

状态：仅统计与建议，未改 registry，未裁剪或安装运行时资产。