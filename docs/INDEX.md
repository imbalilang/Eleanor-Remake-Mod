# 文档索引与权威性说明

最后整理：2026-11-20（1.23 发布后）。本文件是 `docs/` 的总入口：说明每类文档的用途、权威性和维护义务。新增、移动或归档文档时，同步更新本文件与 [archive/README.md](archive/README.md)。

> **线上发布文案以作者在 Steam / Nexus 平台手写的版本为准。** 本仓库里的 `WORKSHOP_*`、`NEXUS_*` 是 AI 记录、候选或打包输入，不是权威发布稿；不要拿本地记录去推断线上现状。

## 1. 真相源链（先看这里）

| 内容 | 唯一真源 | 派生/证据 | 不要手改 |
|---|---|---|---|
| 剧情正文与选项 | `Content/*.json` | `src/*Content.g.cs`、`Localization/zh.csv` | 两个生成物 |
| 英文 | `Content/*.json` 内 `en` 字段、`tools/en_v06_part*.json`、`tools/ui_text_en.json` | `Localization/en.csv` | 生成物 |
| 日语 | `tools/ja_translations.json` + `tools/ja_batch_*.py` 覆盖层 | `Localization/ja.csv`（由 `tools/apply_ja_translations.py` 生成） | 生成物 |
| 韩语 | `Localization/ko.csv`（直接维护）+ `Localization/ko_work/` 过程稿 | 无生成脚本 | — |
| 路线/结局/节点 | [route_map.html](route_map.html) 的 `DATA`/`CHART` | [ROUTE_MAP_MAINTENANCE.md](ROUTE_MAP_MAINTENANCE.md) 日志 | — |
| 规则与创作原则 | [PROJECT_PRINCIPLES.md](PROJECT_PRINCIPLES.md)、仓库根 `CLAUDE.md` | 各设计文档引用 | — |
| 发布证据 | `dist/`（不入库）+ [archive/releases/](archive/releases/) | — | — |
| 美术资源 | `Textures/Items/` + [美术资源登记_1.21_2026-11-19.json](美术资源登记_1.21_2026-11-19.json) | `Art/` 母版与交付记录 | `Textures/Items/` 之外的图不进运行区 |

阅读材料：旧阅读稿（`全局剧本`、侍从/革命军 v2 阅读版）已于 2026-11-20 归档到 [archive/script_docs/](archive/script_docs/)，只作历史；当前阅读底稿是 [CURRENT_TEXT_READING.md](CURRENT_TEXT_READING.md)（由 `tools/render_current_reading.py` 从 Content 生成，块级）。

## 2. 现行权威：规则与创作基准

改动正文、剧情、设计、美术方向或发布文案前必读。

| 文档 | 用途 | 变更义务 |
|---|---|---|
| [PROJECT_PRINCIPLES.md](PROJECT_PRINCIPLES.md) | 项目定位、创作原则、检查表 | 作者决定；Agent 不得自行增删原则 |
| 仓库根 `CLAUDE.md` | 工程与内容工作规则、发布纪律 | 规则变化时同步 |
| [新章对白写作指引](新章对白写作指引.md) | 新章起草与监修。用《停下》、手镯和《窗口》等修订实例处理反复说明立场、机械断句、动作挤框与分支复述 | 写本次来访和选项时使用，按直接相接的前情与出口顺读 |
| [原作Eleanor章节_对照稿_2026-11-10.md](原作Eleanor章节_对照稿_2026-11-10.md)、[原作非台词表现与MOD应用审计_2026-11-10.md](原作非台词表现与MOD应用审计_2026-11-10.md) | **作者的主要文风/表现参照**：游戏本体官方中文与原生表现 | 只作参照；不把原作文本当 MOD 事实 |
| [埃利诺-角色设定.txt](埃利诺-角色设定.txt)、[莉迪亚-角色设定.txt](莉迪亚-角色设定.txt)、[索菲娅-角色设定.txt](索菲娅-角色设定.txt) | 三位角色的**一级人设**：共同线状态下的基准人设 | 人设变化先改卡 |
| [埃莉诺与莉迪亚_全局人设基准_2026-11-11.md](埃莉诺与莉迪亚_全局人设基准_2026-11-11.md) | 各路线/阶段表的参考材料（§2.2、§3.2 的处境、欲望、回避、禁止项），可用于理解**路线二级人设**；本身不是正式二级人设，冲突时以一级静态卡与 Content 为准 | 关系或阶段行为变化时同步；正式路线人设写在各路线的 ActCard/world book（stage/facts/voice/boundaries，本地材料，`tools/NovelWorkFlow_Eleanor/` 不入库） |
| [文风与玩家台词基准_2026-11-10.md](文风与玩家台词基准_2026-11-10.md) | **参考**：Agent/流水线根据原作与预设整理的写作分析，不是作者提供的标准 | 与游戏原文冲突时以原文与静态卡为准；不逐条修补 |
| [表现与交互机制清单与内容契约_2026-11-12.md](表现与交互机制清单与内容契约_2026-11-12.md) | **Agent 工作机制约束**：约束主笔/机制建议用，作者不额外审核，不是项目原则 | 机制建议按其可用范围；不作为对作者的写作要求 |
| [售卖亭空间修正_2026-11-15.md](售卖亭空间修正_2026-11-15.md) | 作者确认的售卖亭空间规则与逐条改稿证据 | 现行规则同时在 `CLAUDE.md` |
| [ROUTE_MAP_MAINTENANCE.md](ROUTE_MAP_MAINTENANCE.md) + [route_map.html](route_map.html) | 路线图、结局编号、发布时间线 | 任何路线/选项/日期/结局改动必须同步并跑 `tools/check_route_map.py` |
| [MODDING_SECURITY_COMPATIBILITY.md](MODDING_SECURITY_COMPATIBILITY.md) | MOD API 安全边界、正式版/测试版兼容 | 发布前按该文件清单核对 |
| [TESTING_CONSOLE.md](TESTING_CONSOLE.md) | F8/F9 调试与测试流程 | 调试按钮变化时同步（当前版本口径待更新） |

## 3. 剧情、文本与作者手稿

| 文档 | 状态 | 说明 |
|---|---|---|
| `Content/story_v06.json` 等 9 个内容源 | 现行 | 正文真源；见 §1 |
| `全局剧本`、侍从/革命军 v2 阅读版 | **已归档**（2026-11-20） | 见 `archive/script_docs/`；旧稿不再维护，正文以 `Content/*.json` 为准 |
| [台词与意象重复_作者审核_2026-11-15.md](台词与意象重复_作者审核_2026-11-15.md) | 作者审核记录 | T01–T06 尚有“保留/只留一处/作者重写”待逐组决定 |
| [后宫收束_手动改写工作稿_2026-11-15.md](后宫收束_手动改写工作稿_2026-11-15.md) | 作者手稿，原样保留 | dv12/dv13 来源；禁止覆写 |
| [角色卡_前期共同线_c01-c05_2026-11-12.md](角色卡_前期共同线_c01-c05_2026-11-12.md) | NovelWorkFlow 输入 | 历史管线的 world book 输入之一 |
| [RUNTIME_TEXT_MAP.md](RUNTIME_TEXT_MAP.md) | **工作参考**：v1.23 源码核对的正文调用链、块↔章节映射、死块与阅读文档漂移 | 改文案/查文本问题前先看；源码或内容结构变化时更新 |
| [CURRENT_TEXT_READING.md](CURRENT_TEXT_READING.md) | **当前阅读底稿**：由 `tools/render_current_reading.py` 从 Content 生成（块级、按运行章节/场景分组，含未上屏块附录） | 修改 Content 后重新生成，不手改 |
| [TEXT_STYLE_AUDIT_STANDARD.md](TEXT_STYLE_AUDIT_STANDARD.md) | 既有正文的专项审计参考，记录 AI 感成因假设及盲测方法 | 专门评估某项写法时参照；日常新稿使用上面的《新章对白写作指引》 |

参考与研究（不是 MOD 事实）：[原作经营机制盘点与Eleanor路线组合研究_2026-11-09.md](原作经营机制盘点与Eleanor路线组合研究_2026-11-09.md)、[NATIVE_STORY_MECHANISMS_ASSESSMENT.md](NATIVE_STORY_MECHANISMS_ASSESSMENT.md)。原作对照稿与非台词审计已见 §2。

## 4. 美术与 CG

| 文档 | 状态/用途 |
|---|---|
| [Art/README.md](../Art/README.md)、[Art/ART_REQUIREMENTS.md](../Art/ART_REQUIREMENTS.md)、[Art/PROMPT_TEMPLATES.md](../Art/PROMPT_TEMPLATES.md)、[Art/SCENE_CG_STYLE.md](../Art/SCENE_CG_STYLE.md) | 现行美术规范、提示词模板、CG 交付检查 |
| [CG_CURRENT_PLAN.md](CG_CURRENT_PLAN.md) | 作者版排期：4 路线 × 8 格 = 32 格 |
| [CG_LOCATION_AND_PLACEMENT_PLAN.md](CG_LOCATION_AND_PLACEMENT_PLAN.md) | 地点、站位、服装、投放类别与 brief 字段（强制） |
| [CG_CANDIDATES_BY_ROUTE.md](CG_CANDIDATES_BY_ROUTE.md) | 36 格候选扩张池，不是当前排期 |
| [LONG_EVENT_CG_HOOKS.md](LONG_EVENT_CG_HOOKS.md) | 长对白事件的动作/交易断点与中场 CG 挂点 |
| [ART_ASSET_WORKQUEUE_2026-11-19.md](ART_ASSET_WORKQUEUE_2026-11-19.md) | 美术工作队列（追加式，最新值以末尾为准） |
| [CG_SELECTED_ASSETS_ROUTE_REVIEW.md](CG_SELECTED_ASSETS_ROUTE_REVIEW.md) | 试作立绘/CG 筛选记录 |
| [CG_CHARACTER_COVERAGE_2026-11-20.md](CG_CHARACTER_COVERAGE_2026-11-20.md)、[CG_BRIEF_revolution_bike_ride_2026-11-19.md](CG_BRIEF_revolution_bike_ride_2026-11-19.md) | 形象覆盖审计、摩托 CG brief（画风/POV 待定） |
| [CHARACTER_BODY_ANCHORS_2026-11-20.md](CHARACTER_BODY_ANCHORS_2026-11-20.md) + [character_body_anchors_2026-11-20.json](character_body_anchors_2026-11-20.json) | 三围/头身比/裆高锚定（取最新值） |
| [CROTCH_LOCK_2026-11-20.md](CROTCH_LOCK_2026-11-20.md)、[FULLBODY_OUTFIT_SWAP_PIPELINE.md](FULLBODY_OUTFIT_SWAP_PIPELINE.md)、[IMAGE_SHARPNESS_GATE.md](IMAGE_SHARPNESS_GATE.md) | 裆高锁定、全身换装、清晰度门槛 |
| [美术资源登记_1.21_2026-11-19.json](美术资源登记_1.21_2026-11-19.json) | 运行资源登记（现行；旧版在 archive/art） |

**结局与 CG 事实说明（为协作写清，不作为方向性原则）**：现行五个结局 E1 恋爱、E2 朋友、E3 侍从、E4 后宫、E9 革命军。四条路线完成结局 **E1/E3/E4/E9** 各配独立结局页与 1 张结局 CG；E2 朋友结局沿用原生 GameOver，不配 MOD 页面/CG——这是设计，不是缺失。`完美集合 E1/E4/E3/E9` 是 0.90 时代历史词，2026-11-15 结局合并后已废止；`tools/check_route_map.py` 会拒绝 `perfect` 字段与“完美结局”字样。

## 5. 本地化与发布

| 文档/路径 | 用途 |
|---|---|
| [JA_LOCALIZATION_STYLE.md](JA_LOCALIZATION_STYLE.md)、[KO_LOCALIZATION_STYLE.md](KO_LOCALIZATION_STYLE.md) | 日语/韩语风格规则 |
| [NEXUS_README.txt](NEXUS_README.txt) | Nexus 打包输入（本地内容可能落后；作者手动同步线上文案，仓库不回填） |
| `docs/archive/workshop/` | AI 记录的旧工坊简介/更新日志（非权威） |
| `docs/archive/releases/` | 0.67–1.23 发布记录（变更与验证证据） |
| `dist/` | 打包与平台回执证据，不入库 |

## 6. 写作生产管线（历史实验）

`tools/NovelWorkFlow_Eleanor/` 是曾用于生成候选正文的实验管线：三文件链（`writer_prompt_template.md`、只读的 `writer_system_prompt.md`、`world_book*.json`）+ 主笔/QA 脚本。它没有接入游戏构建，产物属于历史候选稿，不是正式正文来源；2026-11-17 起作者允许 Agent 直接改稿，正式做法改为在 `Content/*.json` 上直接修订并保留逐行证据。相关评估与实验记录见 [archive/text/](archive/text/)。

调用该管线时仍受 `CLAUDE.md` 的「Writer pipeline usage rules」约束（只加载三文件、Agent 默认只改 world book、模板修改需作者批准、system prompt 只读）。

## 7. 文档中记录的悬空事项（来自历史文档，未经作者确认）

以下事项在多份历史文档里被写成“待作者决定/待确认”。它们只是文档记录，不代表作者现在需要逐项回答；等对项目理解更完整后，再按作者指定的顺序讨论。

1. **NovelWorkFlow 正式模板最小 diff（G1）**：历史计划里的批准闸门，至今无批准记录；后续做法已绕开。
2. **革命军线 `v2_r1c1` 结构改动**：文档记录过“是否接受新增来访/前场改动及其路线和旧档成本”。
3. **CG 待定**：摩托 CG 的 A/P 画风与玩家 POV；未设计/未标注格位；32 格是否扩到 36 格。
4. **美术管线**：新像素管线重做旧参考的范围与成本；后宫睡衣 L4 候选与 L5 修女服集成。
5. **台词母题 T01–T06**：价签、分账、自己决定、门口没人→主人、别笑、店主导师腔。
6. **玩家台词口径**：历史基准“默认 0 句、路径 ≤15%”与 11-16 起“不能只剩嗯/好/行”的方向差异。
7. **当前阅读底稿的维护**：`CURRENT_TEXT_READING.md` 是否随每次 Content 改动重建（当前由脚本生成，旧阅读稿已归档）。
8. **旧档 legacy 文本**：侍从旧版（`maid_v05`）、革命军旧版（`revolution_v1`）、后宫旧线（`dr`/`d02–d09`）是否继续维护文风。
9. **唱歌 canon**：`maid_v2` 有 55 处“唱”，三份静态角色卡没有“她学过唱歌”的事实。

发布文案不在本清单内：线上工坊/Nexus 文案由作者手动维护，仓库不要求回填，也不以本地记录推断线上状态。

## 8. 已知陈旧内容（不再逐条修补）

按作者要求，过期内容不再逐条修补；整篇过时时直接归档，不在此处做“顺手修”。以下只记录事实：

- 旧阅读稿（全局剧本、侍从/革命军 v2 阅读版）已于 2026-11-20 归档到 `archive/script_docs/`：它们是 2026-11-14/16 的旧文本，与当前 Content 有大量差异（侍从 58%、革命军 88% 行不一致）。当前阅读底稿见 [CURRENT_TEXT_READING.md](CURRENT_TEXT_READING.md)。
- [MODDING_SECURITY_COMPATIBILITY.md](MODDING_SECURITY_COMPATIBILITY.md)：§5.4 仍提已退役的 `site/index.html` 与 `sync_site.ps1`（`CLAUDE.md` 已禁止重建）。
- [TESTING_CONSOLE.md](TESTING_CONSOLE.md)：仍以 0.46D/0.67 描述步骤与版本。
- [表现与交互机制清单与内容契约_2026-11-12.md](表现与交互机制清单与内容契约_2026-11-12.md)：仍写“结局页仅 E3/E4”，实际 E1/E9 页已加入；文件仍是 Agent 草案。
- [NEXUS_README.txt](NEXUS_README.txt)：内容落后于当前版本；线上由作者手动同步，仓库不回填。
- `Localization/ko_work/TRANSLATION_RULES.md`：仍用 엘레노어（1.21 已统一为 엘리너），且该目录是过程稿。
- 历史文档与 `docs/archive/` 中保留的旧尺度口径、A/B/C/D 路线称呼、“甲乙丙丁”等，只能作历史参考；现行术语与规则见 §2。

## 9. 维护规则

1. 正文改动：`Content/*.json` → `python tools/build_content.py` → `python tools/check_project.py`；不手改 `.g.cs` 或 CSV。
2. 路线/选项/日期/结局改动：同一次改动更新 [route_map.html](route_map.html) 与 [ROUTE_MAP_MAINTENANCE.md](ROUTE_MAP_MAINTENANCE.md)，跑 `python tools/check_route_map.py`；公开条件变化时同步 Steam 指南 3842734960。
3. 发布：`manifest.xml` 版本 + README 版本节 + `DATA.version` + 维护文档条目，跑检查；发布记录进 [archive/releases/](archive/releases/)，工坊更新说明另写。
4. 文档整理：现行文档放 `docs/` 根目录；被取代的设计、审计、发布材料移入 `docs/archive/<类别>/`，并同步本文件与 [archive/README.md](archive/README.md)。移入归档的文件默认不再更新。
5. 实机验收：本项目从未由 Agent 主导实机验收（历次尝试均未成功）。Agent 只负责离线检查、Release 编译、原生扫描与打包证据；实机验证是否做、做到什么程度由作者决定与执行。
6. Git 提交：本项目所有提交都由作者明确指令后执行；Agent 不自动提交、不自动推送（GitHub / Steam 工坊 / Nexus 同理）。
7. 不确定权威性时：先看本文件 §1/§2；仍不确定就问作者，不要把历史文档当现行规则。
