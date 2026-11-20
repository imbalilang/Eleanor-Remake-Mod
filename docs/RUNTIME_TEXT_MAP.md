# 实装逻辑与文案调用图（v1.23 源码核对）

状态：工作参考，2026-11-20 按工作区源码与 Content 只读核对。不是规则文档，不替代 [PROJECT_PRINCIPLES.md](PROJECT_PRINCIPLES.md) 或 `CLAUDE.md`。用途：在改文案或调查文本问题前，先确认“这段字在哪里、经过谁、最后怎么上屏”。

## 1. 端到端链路

```
Content/*.json ──build_content.py──> src/*Content.g.cs（只有 key）+ Localization/zh.csv, en.csv
                                        │
运行时：各路线 Story/Text 类 ── L.Get(key) ──> ModHelper.GetLocalized ──> CSV 里的正文
                                        │
                                  原生 Dialogue(title=说话人名, text=正文)
                                        │
   DialogueSpeakers（气泡名/历史）＋ StoryPresentation（第二人物槽）＋ PortraitExpression（表情）
                                        │
                                  DialogUIManager 原生显示
                                        │
                        endAction / InterceptDismiss / OnDialogueFinished ──> 状态与章节推进
```

关键点：**`*.g.cs` 里没有正文**，只有说话人与 key；运行时正文全部来自 Localization CSV。ja 由 `ja_translations.json` + `tools/apply_ja_translations.py` 生成，ko 直接维护 CSV。

## 2. 各类文案的真源位置

| 文案类型 | 真源 | 备注 |
|---|---|---|
| 剧情正文/旁白/动作 | `Content/*.json` | 9 个内容源；`<i>…</i>` 是行内动作 |
| 英文正文 | 路线 JSON 的 `en` 字段；共同线 `tools/en_v06_part*.json` | 生成 `Localization/en.csv` |
| 共同线选项标签/提示/通知/结局页/名字 | `tools/ui_text_zh.json`（对应 `ui_text_en.json`） | 不进 Content JSON |
| 路线选项标签/说明 | Content JSON 的 `choices`/`segments.options` | 构建成 `er_love_choice_*`、`er_dual_choice_*`、`er_maid_v2_*`、`er_rev_v2_*` 键 |
| 结局页标题/正文 | `tools/ui_text_zh.json` 的 `er_ending_*` | `CgKey` 是图像 key，不在 CSV |
| 物品名/描述/风味 | `Content/*_ui.json` | `er_item_*`、`er_dual_*` |
| 说话人名 | `tools/ui_text_zh.json` 的 `er_name_*` | 埃莉诺名走原版 `name_eleanor` |
| 通知/系统提示 | `tools/ui_text_zh.json` | `er_notice_*`、`er_line_*` |
| 日/韩正文 | `Localization/ja.csv` / `ko.csv` | ja 由覆盖层生成；ko 直接维护 |

## 3. 各路线怎么把块变成对白

| 路线 | 构造器 | 流程/选项来源 | 断点续读 |
|---|---|---|---|
| 共同线 | `StoryEngine.Build/BuildSelected/Append/Chain` + `StoryV06Content` | 硬编码在 `src/EleanorStory.cs`（章节方法 + 选择 + 交易结果块） | 无（重进按章节重播） |
| 恋爱线 | `LoveText.Build` + `LoveContent` | 硬编码在 `src/LoveStory.cs` 的 10 个 Create 方法 | 有 `cursor.<block>` |
| 后宫线 | `DualText.Build`/`BuildBackup` + `DualContent` | 硬编码在 `src/DualStory.cs`/`src/DualV2Story.cs`（`SimpleChoice` 等） | 有 `cursor.<block>` |
| 侍从线旧版 | `MaidText.Build` + `MaidContent` | 硬编码在 `src/MaidStory.cs`/`src/MaidReturnStory.cs` | 无 |
| 侍从线 v2 | `MaidV2Text.Build` + `MaidV2Content.Scene.Segments` | **数据驱动**：lines/choice/trade 段由 Content JSON 生成；进线与状态仍由代码判 | 无（场景级） |
| 革命军线旧版 | `StoryEngine.Build` + `RevolutionContent` | 硬编码在 `src/RevolutionStory.cs` | 无 |
| 革命军线 v2 | `RevolutionV2Text.Build` + `RevolutionV2Content.Scene.Segments` | **数据驱动**：同上 | 无（场景级） |

两条 v2 路线是“JSON 定正文顺序、代码定条件和状态”；共同线、恋爱、后宫、旧侍从/革命军是“代码定顺序”。

## 4. 运行时事实（改文案前必须知道）

1. **块前缀 ≠ 运行章号**：第 5–24 章用 `c04`–`c23`（前缀 = 章号 − 1）；`c10` 供第 10/11 章；`c19` 用于第 20 章告白；第 12 章进入侍从入口（`m11`/`v2_a0c2` 路径）。
2. **一条 Content 行 = 一个气泡**；恋爱/后宫/侍从 v2/革命 v2 构造器按 `\n` 拆段，共同线不拆。
3. **选项气泡显示“下一节点文本”**：所以共同线用 `PlayerReply`、后宫用 `DualText.Option` 处理标签回显；选项说明作为原生 shortDescription 传入。
4. **第二人物借原生 SEC 气泡**：`StoryPresentation` 摆第二个 GenericClient 槽并逐行换名；共同线第 19 章莉迪亚陪同是唯一硬编码的 backup，`story_v06.json` 的 `presentation` 字段为空。
5. **表情按 key 查表**：`Content` 行的 `expr` → `ExpressionContent.ByKey`；显示时要求该行说话人等于当前主客 displayName；NPC/backup 没有表情。`neutral` 回底图，缺图有 shy/flustered 回退链。
6. **死亡文案（确认无任何代码路径）**：共同线 9 块（`c17.b004/b005`、`c19.b002–b005`、`c20.b002/b003`、`ep.b006`，33 行）；后宫 4 块（`common09_add_*`、`common10_add_family`，11 行）；侍从旧版 4 块（`exit_b002`、`m12_b005`、`m20_b010/b011`，15 行）。合计 17 块 / 59 行。
7. **永久被索引过滤掉的行（共同线）**：`c01.n003` 第4行、`c02.n002` 第1/7行、`c03.n002` 第5/6行、`c05.n002` 第1/4/9行、`c17.b101` 第8/11/19行，共 11 行；另有一批按 `held/Rescued/MotherBoughtGoods/MotherBoughtFruit/tracker.Any` 条件显示。
8. **两种旧档分叉**：侍从 `FeatureVersion`（v1/v2）、革命 `RevVersion`（v1/v2）、后宫 `d.route + d02–d09` 是否已开始（dv / d / dr 三套）。同一“章节号”可能播完全不同的正文。
9. **数据驱动路线的生成字段并非都被使用**：侍从 v2 的 `result` 段、`Scene.TitleKey`、`Trade.Type/Budget/Client`，后宫 `Line.Expr`、`Choice.Mainline`，革命 v2 的 `Scene.TitleKey` 等目前没有运行时读取；改这些字段不会改变游戏内容。

## 5. 内容统计与可达性

| 路线 | 块 | 行 | 可达块 | 可达行 | 确认死亡行 |
|---|---:|---:|---:|---:|---:|
| 共同线 | 172 | 877 | 163 | 844 | 33 |
| 恋爱线 | 76 | 555 | 76 | 555 | 0 |
| 后宫线 | 181 | 1557 | 177 | 1546 | 11 |
| 侍从旧版 | 199 | 672 | 195 | 657 | 15 |
| 侍从 v2 | 54 | 679 | 54 | 679 | 0 |
| 革命军旧版 | 76 | 226 | 76 | 226 | 0 |
| 革命军 v2 | 57 | 549 | 57 | 549 | 0 |
| 合计 | 815 | 5115 | 798 | 5056 | 59 |

“可达”= 被代码/场景结构引用；**不等于同一周目全部显示**，条件块与互斥分支仍会大幅减少单次阅读量。

## 6. 共同线章节→块（源码核对）

| 运行章 | 创建方法 | 块 | 合计行 |
|---:|---|---|---:|
| 1 | CreateFoodChapter | c01.n001–n005, b003–b006 | 32 |
| 2 | CreateHouseholdChapter | c02.n001–n002, b003–b006 | 29 |
| 3 | CreateBelongingsChapter | c03.n001–n005, b003–b006 | 34 |
| 4 | CreateUncleChapter | c04.n001–n004 | 20 |
| 5 | 原生保释/获释（CreateClient 返回 null） | c05.b007/b009/b010 由桥接追加 | — |
| 6 | CreateHardshipChapter | c05.n001–n005, b004, b006 | 34 |
| 7 | CreateScavengerChapter | c06.b101, b003–b006 | 15 |
| 8 | CreateScavengerSecondChapter | c07.b101, b003–b007, b106 | 15 |
| 9 | CreateGiftChapter | c08.b101–b104 | 21 |
| 10 | CreateLydiaChapter | c09.n101–n106 | 60 |
| 11 | CreateLydiaResponseChapter（按 DPreludeStep 分三路） | c09/c10 的 prelude 块：c10.n201–n205、n206–n214 等 | 见源码 |
| 12 | MaidStory.CreateEntry | c11.b001 / m11_b*（侍从入口） | — |
| 13 | CreateDriveChapter | c12.b001–b003 | 37 |
| 14 | CreateThreatChapter | c13.b001–b003 | 33 |
| 15 | CreateDecisionChapter | c14.b001–b011 | 72 |
| 16 | CreatePreparationChapter | c15.b002–b006 | 22 |
| 17 | CreateReunionChapter | c16.b001–b005 | 21 |
| 18 | CreateMotherChapter | c17.b001–b003, b101 | 37 |
| 19 | CreateAfterReunionChapter | c18.b001–b004, b003a–c, b101 | 44 |
| 20 | CreateConfessionChapter | c19.b001, b006, b101, b101_return, b102, b103 | 51 |
| 21 | CreateCooperationChapter | c20.b001, b004–b006 | 8 |
| 22 | CreateLydiaSecondChapter | c21.b001–b006 | 18 |
| 23 | CreateFutureChapter | c22.b001–b010 | 22 |
| 24 | CreateConclusionChapter | c23.b001–b004 | 14 |
| 201 | CreateRouteD | rd.b001–b005 | 16 |
| rd/ep | CreateEpilogueClient 等 | ep.b001–b006 | — |

“合计行”是所引块的全部行数（含互斥交易反应/选择分支），不是单次阅读行数。

## 7. 阅读文档与当前正文的漂移（重要）

- [全局剧本（已归档）](archive/script_docs/全局剧本_旧汇编_2026-11-16.md)：`####` 块级汇编 540 节，覆盖旧版共同/侍从/双恋/革命军；缺 9 个现行共同线块（`c14.b009–b011`、`c17.b101`、`c18.b101`、`c19.b101/b101_return/b102/b103`）；76 个块文本与当前 Content 不同；整条恋爱线（76 块）、后宫 D/dv 改写（90 块）在块级汇编中缺失；文末“侍从线/革命军线 v2 全剧情阅读版”是旧稿（革命军结局仍是“让她进来、拎椅子进门、门在身后合上”，现行正文是窗边椅子）。
- [侍从线 v2 阅读版（已归档）](archive/script_docs/侍从线_v2_全剧情阅读版_旧稿_2026-11-15.md)：16 个场景、595 行阅读台词，其中 346 行与当前 Content 对不上（58%）；且仍写“P2 唯一必做，P1/P3/P4 可选”，与 1.22 起 P1→P2 前置不符。
- [革命军线 v2 阅读版（已归档）](archive/script_docs/革命军线_v2_全剧情阅读版_旧稿_2026-11-14.md)：11 个场景、409 行，361 行对不上（88%）；结尾仍是旧门/进屋写法。
- 结论：**阅读文档不能当作当前正文的审阅本**；用于文风分析或发布宣传前必须重新生成或按 Content 重建。

## 8. 改动影响面

| 想改什么 | 必须动什么 | 必须重建/检查 |
|---|---|---|
| 一句正文/旁白 | `Content/*.json` 对应行 + 对应英文 | build_content → check_project |
| 共同线选项标签/提示 | `tools/ui_text_zh.json` + `ui_text_en.json` | build_content → check_project |
| 路线选项标签/说明 | Content JSON 的 choices/segments.options | build_content → check_project |
| 选项数量/顺序/分支条件 | C#（共同/恋爱/后宫/旧侍从/旧革命军）或 Content+代码（两条 v2） | 编译 + 路径/离线回归 |
| 交易反应文案 | Content 对应 reaction 块（代码按 F/D/O/L 选） | build_content → check_project |
| 结局页文案 | `tools/ui_text_zh.json` 的 `er_ending_*` | build_content → check_project |
| 表情 | Content 行 `expr` → `ExpressionContent` | build_content（含表情生成） |
| 改名/新增说话人 | `tools/ui_text_zh.json` + 代码映射/SpeakerName | 编译 + 逐句核对 |

## 9. 口径与限制（适用于全文）

- “可达”是按代码与生成场景结构静态判断，未模拟运行时分支；单周目实际行数会更少。
- 双人/备份立绘、表情切换、结局页均依赖游戏内运行时；本图只覆盖文本与状态路径。
- 未覆盖旧档迁移的全部历史形状（v0.6x–1.x 的存档字段）；旧档正文以对应 `FeatureVersion/RevVersion/d.route` 分支为准。
- 阅读文档漂移的百分比按“行文本规范化后是否存在于该场景当前块”估算，含动作标签转换误差。

## 10. 单周目节奏估算

方法：按代码路径取“主选项 + 一次公允交易反应”，把选择提示与玩家回应各计 1 个气泡；侍从 v2/革命军 v2 由 `segments` 的 `mainline` 与交易 `outcomes` 计算，恋爱/后宫 v2 由 C# 图手工编码，共同线为按代码路径与块行数的估算（±2）。不含 CG 页、选择悬停、读档续播的重复气泡。

原作的普通来访基线约为 **6–11 个气泡**（见 [文风与玩家台词基准](文风与玩家台词基准_2026-11-10.md) 与历史体检文档）。当前正文的单次来访：

| 路线 | 单路径气泡范围 | 中位/典型 |
|---|---|---|
| 恋爱线（10 场） | 25–43 | 约 38–40，多数场景 3–6 倍原作 |
| 后宫 v2（13 场） | 22–49 | 约 35–40 |
| 侍从 v2（16 场） | 24–50 | 约 35–40 |
| 革命军 v2（11 场） | 29–55 | 约 42–45 |
| 共同线（代表章） | 8–51 | 多数普通章 12–27；关系/选择章 22–51 |

共同线代表值：ch1≈24、ch2≈21、ch3≈27、ch4≈15（被捕路径≈29）、ch6≈21、ch7/ch8≈12、ch9≈13、ch10≈40、ch11≈16（掮客）/29（赎回）、ch13≈39、ch14≈25–32、ch15≈51、ch16≈23、ch17≈22、ch18≈21、ch19≈22、ch20≈29、ch21≈8、ch22≈15、ch23≈17、ch24≈17。ch5 由原生保释/获释来访承载，ch12 为侍从入口。

侍从 v2 最长：`v2_a0c1`(47)、`v2_a0c2`(45)、`v2_a4c1`(45)、`v2_a2c1`(42)；革命军 v2 最长：`v2_r1c1`(51)、`v2_r1c2`(48)、`v2_r2c3`(47)、`v2_r3c2`(45)、`v2_r4c3`(45)。恋爱线最长：A1.1(43)、A1.2/A2.1/A2.2(40)；后宫最长：dv07(47)、dv05(46)、dv01(45)。

## 11. 阅读稿生成器现状

旧阅读稿已于 2026-11-20 归档，对应生成器也已退役到 `tools/legacy/`；当前阅读底稿改用 `tools/render_current_reading.py`。

| 脚本 | 状态 | 说明 |
|---|---|---|
| `tools/render_current_reading.py` | **现行** | 从全部 `Content/*.json` 生成 [CURRENT_TEXT_READING.md](CURRENT_TEXT_READING.md)：共同线按运行章节、恋爱/后宫按场景、侍从/革命军 v2 按 segments、结局页、旧档索引、未上屏附录。块级底稿，不是单周目回放。 |
| `tools/legacy/render_maid_v2_reading.py` | 已退役 | 旧侍从 v2 阅读版生成器；不渲染交易结果块，硬编码 P2/DAYS/ENDS 已落后。不要运行。 |
| `tools/legacy/render_revolution_v2_reading.py` | 已退役 | 旧革命军 v2 阅读版生成器；不渲染交易结果块，元数据为手写。不要运行。 |
| `tools/legacy/render_all_story_reading.py` | 已退役 | 旧“本MOD全剧情阅读版_2026-11-09”四件套；不读恋爱线。不要运行。 |
| `tools/legacy/render_dual_reading.py` | 已退役 | 旧后宫 v0.5 阅读稿。不要运行。 |
| （无） | — | 旧 `全局剧本` 的 `####` 块级汇编没有生成器，已归档为历史材料。 |

结论：当前可用的阅读底稿是 [CURRENT_TEXT_READING.md](CURRENT_TEXT_READING.md)；旧阅读稿只当历史文本，不再用于文风评审或发布文案。单周目主线路版尚未生成。

