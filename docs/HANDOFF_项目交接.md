# Eleanor 重制 MOD · 项目交接说明

更新日期：2026-11-06  
项目目录：`D:\Program Files (x86)\Steam\steamapps\common\Probably Stolen`  
游戏：Probably Stolen / 深空当铺：可能是偷的  
版本：0.46D，Steam AppID `4348910`  
MOD 入口：`Mods\EleanorRomance\`  
开发工程：`EleanorRomanceDev\`

---

## 1. 一句话目标

把原版 Eleanor 的个人线从第 23 天起全面重制为一段“上层区千金 × 下层区当铺老板”的全年龄恋爱爽文：保留她的成长与母亲冤案主线，用实际声望、金钱、物品和行为推进关系，影响关键节点、结局与后续内容。

---

## 2. 当前状态总览

### 2.1 已实现（代码可构建）

- Unity ModLoader 工程、`manifest.xml`、`IMod` 入口、Harmony 自动补丁。
- `EleanorStory` 第 1~11 章旧版剧情和选项（v0.1 结构）。
- 原版 Eleanor 三个调度方法的拦截：
  - `StoreClientManager.CheckStoryLineEleanor`
  - `StoreClientManager.HandleEleanorEarlyClient`
  - `StoreClientManager.HandleEleanorUncleClient`
- 游戏结束界面恋爱尾声补丁。
- 旧档检测与“重置/保留原版”档案员事件。
- 版本闸门（0.46 / 0.46D）。
- 每周来访、上层区渠道、结局变体、新开局“旧日回响”。
- 物品：母亲吊坠、伪造芯片、母亲研究笔记（Paper Book）。
- **第一阶段 v0.4 同步**：
  - 新增访客客户：维克多（第 26 天）、莉迪亚（第 32 天）、哈维（第 36 天）、维克多第二次（第 41 天）、母亲（第 44 天，仅真结局）。
  - 新增通讯/新闻节点：第 2、3、4、5A、6、7、10 章。
  - 中英本地化各 367 条。
  - 构建通过：0 警告、0 错误。

### 2.2 部分实现

- 访客调度已接入 `StoryDirector.TryQueueNextVisitor`，但**尚未实机验证**同日排队、主线/访客先后顺序、旧档和真结局门槛。
- 通讯/新闻目前是带 `【通讯记录】/【新闻】` 前缀的玩家/旁白文字节点，不是自定义 UI。
- 母亲来访已接为访客；第 11 章莉迪亚/上层区客人的场次还未接入代码。
- 新开局、结局变体、每周来访已实现，但内容量偏少。

### 2.3 仅设计、未实现

- 行为系统：交易定价（公平/宰客/让利/赠予）、检视陷阱箱、放置/举报/转卖、物品行为。
- 后间 `afterhours` 场景（类似 Lou MOD 的 `lou_home`）。
- 自定义通讯/新闻/案卷 UI。
- v0.4 全流程攻略里的完整章节文本替换（当前代码仍是旧版章节选项）。
- 与 Lou MOD 同量级的内容体量。

---

## 3. 已锁定的设计决策

1. **全面重制**：从第 23 天起替换原版 Eleanor 9 次主线，保留原日程骨架，另加后日谈。
2. **关系模式**：身份反差 + 双向救赎；Eleanor 从俯视到主动依赖，同时成长为独立盟友。
3. **玩家身份**：低调大佬，但**不凭空造牌**；强力解法必须由实际派系声望、金钱或关键物品解锁。
4. **声望门控**：20 / 40 / 60 三档，随章节递增；金钱与关键物品可作为替代。
5. **好感 + 关键选择**：日常行为累积好感，关键节点用显式选择锁路线。
6. **原主线保留并重写**：母亲冤案、Nutrifruit 压制、U 盘归还/泄露、黑市威胁。
7. **真结局**：好感 ≥ 12；未失去 U 盘；未利用她；浪漫线未锁死；任一势力声望 ≥ 60 + 800 信用点，或 2500 信用点。
8. **长尾声，不结束跑局**：真结局后 Eleanor 成为常驻访客，每周来访；回上层区路线每 14 天有上层区采购客户。
9. **结局变体**：原版游戏结束界面追加恋爱尾声。
10. **新开局“旧日回响”**：真结局解锁；好感 +20、上层区 +20、下城区 -10、吊坠、第 5 天开始。
11. **旧档保护**：检测原版 Eleanor 进度；第一次开店由“下层区档案员”询问重置/保留；重置保留物品。
12. **内容尺度**：全年龄。
13. **语言**：简中 + 英文，其他语言回退英文。
14. **美术**：复用原版 Eleanor 立绘键；不新增角色立绘；只有吊坠/伪造芯片图标、Paper Book、封面。
15. **兼容策略**：最小侵入；只补丁 Eleanor 调度和结束界面；复用原版立绘与现有系统。
16. **发布**：`steamcmd` + `workshop_build_item`；首次 hidden 测试，再公开。

---

## 4. 剧情/流程结构

### 4.1 普通模式日程

| 章节 | 天数 | 内容 |
|---|---|---|
| 1 | 23 | 初遇 / 食物 / 第一印象 |
| 2 | 25 | 日用品 / 体面 / 下城区住处 |
| 3 | 27 | 退婚 / 吊坠 / 维克多 |
| 4 | 28 | 舅舅的包裹 / 识破陷阱 |
| 5A | 29 | 逮捕救援（陷阱触发） |
| 5B | 29 | 母亲判决（未触发陷阱） |
| 6 | 31 | 拾荒 / 收购 / 第一份回礼 |
| 7 | 33 | 800 信用点 / 莉迪亚 / 深夜 |
| 8 | 37 | U 盘 / 哈维 |
| 9 | 38 | 黑市威胁 |
| 10 | 40 | 最后一张牌 / 母亲与维克多后续 |
| 11 | 43 | 长尾声 |

### 4.2 新开局“旧日回响”日程

第 5 / 7 / 9 / 10 / 11 / 13 / 15 / 19 / 20 / 22 / 25 天。

### 4.3 访客日程（第一阶段代码已接）

| 访客 | 天数 | 前置条件 |
|---|---|---|
| 维克多 | 26 | 第 3 章前 |
| 莉迪亚 | 32 | 第 7 章前 |
| 哈维 | 36 | 第 8 章前 |
| 维克多第二次 | 41 | 第 11 章前，未锁死 |
| 母亲 | 44 | 第 12 章后，真结局 |

---

## 5. 核心判定与状态

### 5.1 真结局

- 好感 ≥ 12
- 未失去 U 盘（第 9 章未交出真盘）
- 未选择“用感情换钱”（第 7 章）
- 浪漫线未锁死
- 任一势力（黑市/上层区/安保/下城区）声望 ≥ 60 + 800 信用点，或直接 2500 信用点

### 5.2 其他路线

- 回归：上层区声望 ≥ 20；未失去 U 盘；母亲流放。
- 泄露：未失去 U 盘；母亲死亡；Eleanor 加入革命。
- 放弃：永远可选；母亲活着但疏远；浪漫线锁死。
- 黑市卖盘：黑市声望 ≥ 20；未失去 U 盘；+1000 信用点；母亲死亡；浪漫线锁死。
- 逮捕救援：安保 40 + 500 / 黑市 40 + 400 / 1500 / 放弃。

### 5.3 重要 MOD 状态

`RomanceState` 中以 `er.` 前缀持久化：

- `chapter` / `pending_chapter` / `next_visit_day` / `last_queued_day`
- `affection`
- `visitor_index` / `last_visitor_day`
- `weekly_day` / `upper_channel_day`
- `uncle_choice` / `threat_choice` / `drive_route` / `mother_fate`
- `partner_path` / `true_ending` / `epilogue_done` / `romance_locked`
- `drive_lost` / `exploited` / `has_drive`
- 各章风味旗标：`uncle_inspected`、`uncle_warned`、`uncle_sold`、`lawyer_found`、`bm_clue`、`business_lesson`、`rescued_from_arrest`、`locket_given`、`notes_given`

### 5.4 旧档

- `HasOriginalEleanorProgress()` 检查原版 `StoreClientData` 与 `clientTracker / permanentClientTracker / uniqueClientTracker` 的 `eleanor*` 记录。
- 旧档默认不接管；档案员提示重置/保留。
- 重置清除原版标记、MOD 数据、原版 Eleanor 队列；已获得物品保留。

---

## 6. 代码结构与关键 API

### 6.1 源码

| 文件 | 职责 |
|---|---|
| `src/ModEntry.cs` | IMod 入口、Hook 订阅、调试入口、场景注册 |
| `src/StoryDirector.cs` | 章节/访客调度、日程、结局推进、旧档提示 |
| `src/RomanceState.cs` | MOD 存档状态、旧档检测/重置 |
| `src/EleanorStory.cs` | 章节客户、访客客户、对话图、选项 |
| `src/Items.cs` | 吊坠、伪造芯片、研究笔记发放 |
| `src/Patches.cs` | 原版 Eleanor 拦截、游戏结束尾声补丁 |
| `src/ScenarioRegistration.cs` | 新开局注册 |
| `src/MetaProgress.cs` | 真结局全局解锁文件 |
| `src/DebugTools.cs` | F9 调试：重置、跳章、解锁 |
| `src/VersionGate.cs` | 0.46 版本闸门 |
| `src/Localization.cs` | `L.Get` / 角色名 |
| `src/ModConstants.cs` | MOD ID、场景 ID、物品 ID |

### 6.2 关键游戏 API

- 访客/章节客户：`StoreClient`、`StoreClientManager.AddNextClient`、`AddClient`
- 对话：`Dialogue.SetText`、`NextDialogue`、`AddChoice`、`SetEndAction`
- 全局 Hook：`ModHook.OnGenerateCustomerVeryEarly`、`OnGameLoadedInit`、`OnNewGameStarted`
- 本地化：`ModHelper.GetLocalized`、`LocHelper.GetLocalizedClientName`
- 存档：`ModHelper.SetModDataInt/Bool/String`、`GetModData*`
- 声望：`StoreReputation.GetBMReputation / GetULReputation / GetLLReputation / GetSecReputation / GetRevReputation`
- 结局：`GameOverUIManager.DisplayUI`、`RunSummaryUI`
- Paper Book：`Books/mothers_notes.json`
- 新开局：`ModHelper.AddStartingScenario`
- 结束界面尾声：`Patches.cs` 的 `DisplayUI` Prefix
- 可能的后续载体：`ModHelper.AddAfterhourLocation`、`CustomUIManager`（尚未使用）

### 6.3 构建

```bash
cd "D:\Program Files (x86)\Steam\steamapps\common\Probably Stolen\EleanorRomanceDev"
bash build.sh
```

产物在 `Mods\EleanorRomance\`。

当前构建状态：通过，0 警告，0 错误。  
本地化校验：代码中直接 `L.Get` 键 355 个，缺失 0；中英 CSV 各 367 条。

---

## 7. 文件地图

### 7.1 当前权威文档

1. **`EleanorRomance_全流程攻略_纯阅读版.txt`**
   - 当前剧情/场次/台词的 v0.4 可演出版本。
   - 只保留柜台访客、玩家/旁白文字、通讯记录、新闻/纸书、后间对话。
   - 不含不可演的门口/街口/探头/挽手/夜景等镜头。
   - **这是当前剧情文本的主要依据。**

2. **`EleanorRomanceDev/docs/剧情与选项设计_行为化台本.txt`**
   - 行为化设计稿（v0.2）。
   - 说明哪些旧对话菜单要退役、如何用交易/举报/物品/联系人行为替代。
   - 用于第二阶段实现。

3. **`EleanorRomance_剧情与选项设计_旧版对白选项.txt`**
   - 旧版逐句对白台本，包含 305 条旧文本和完整旧选项。
   - 用于复用旧台词/对照，不是当前场次依据。

4. **`EleanorRomanceDev/docs/剧情与选项设计_摘要版.txt`**
   - 快速决策摘要，适合 5 分钟了解项目。

### 7.2 代码与资源

- `EleanorRomanceDev/src/`：C# 源码
- `EleanorRomanceDev/Localization/zh.csv`、`en.csv`：367 条中英文本
- `EleanorRomanceDev/Books/mothers_notes.json`：三页研究笔记
- `EleanorRomanceDev/Textures/Items/eleanor_locket.png`
- `EleanorRomanceDev/Textures/Items/eleanor_decoy_drive.png`
- `EleanorRomanceDev/preview.png`
- `EleanorRomanceDev/manifest.xml`
- `Mods/EleanorRomance/`：实际安装包

### 7.3 工具

- `tools/gen_localization.py`：从内嵌旧文本生成 CSV，并保留后续追加键
- `tools/add_v04_localization.py`：追加 v0.4 访客/通讯/新闻键
- `tools/gen_script_doc.py`：生成旧版逐句台本
- `screenshot.ps1` / `capture_game.ps1` / `click.ps1` / `foreground.ps1`：测试辅助脚本

---

## 8. 当前代码与设计稿的差异（交接重点）

1. **代码是混合状态**：
   - 第一阶段已加入 v0.4 的访客和通讯/新闻节点。
   - 但第 1~11 章的主体对话和选项仍是 v0.1 旧结构。
   - 不要误以为代码已经完全等于 v0.4 剧情稿。

2. **旧对话菜单仍在**：
   - 第 1/2/3/6/7/9 章仍是“解法菜单”式选项。
   - 第二阶段要把它们替换为交易/检视/放置/举报/物品行为。

3. **通讯/新闻是临时载体**：
   - 现在是玩家/旁白文字节点，前缀 `【通讯记录】/【新闻】`。
   - 还不是自定义 UI、报纸或纸书。

4. **后间/afterhours 未实现**：
   - 第 7、11 章的后间场景目前只是对话文字。
   - 若要做 Lou 式体验，需要新增一个 afterhours 地点 + 背景图 + 返回面板。

5. **访客调度未实机验证**：
   - 访客日：26 / 32 / 36 / 41 / 44。
   - 访客优先于章节；访客离开后同日可继续排章节。
   - 需要测试：错过访客、跨天补触发、旧档、真结局母亲门槛。

6. **内容量不足**：
   - 当前 v0.4 文档约 64 句角色台词；单周目约 80~100 句。
   - Lou MOD 3832595288：1,339 条有效文本，18 章，单周目估计 350~500+ 句。
   - 若目标是爽文体量，需要至少 3~5 倍实际台词，不能靠攻略里的行为说明凑数。

---

## 9. 下一阶段任务

### P0：实机验证第一阶段

目标：确认新增访客和通讯节点不会卡流程。

测试点：
- 新档推进到第 26 天，确认维克多先出现，随后主线第 3 章仍可正常触发。
- 第 32 天莉迪亚、36 天哈维、41 天维克多第二次。
- 真结局后第 44 天母亲出现；非真结局不出现。
- 访客被跳过/跨天后能补触发。
- 旧档提示与重置仍正常。

### P1：章节文本与行为对齐

- 用 v0.4 剧情稿替换第 1~11 章旧对话/旧选项。
- 行为系统优先做：
  - 交易定价：公平 / 宰客 / 让利 / 赠予 / 拒绝。
  - 陷阱箱：检视、放置、举报、转卖、警告。
  - 第 5 章救援/帮助：用现有安保/黑市/上层区关系，而不是纯菜单。
  - 第 7 章资金：信用点/借款凭证/联系人。
  - 第 9 章威胁：举报/报警/黑市/武器/交出 U 盘。
- 保留显式对话选择只用于：警告 Eleanor、情感节点、最终路线确认。

### P2：承载层

- 一个 afterhours 后间（参考 Lou 的 `lou_home`）。
- 自定义通讯/新闻/案卷 UI，或纸书/报纸载体。
- 第 11 章莉迪亚/上层区客人短场景。
- 母亲/维克多第二次等访客的正式立绘/文本打磨。

### P3：内容量

- 目标：单周目 300~400 句，总文本 800~1,200 条。
- 通过新增短场景/访客/通讯/新闻/恋爱节点扩量，不把单章拉长。
- 每章仍保持短场景，单章不超过约 20 句。

### P4：测试与发布

- 新档全流程、旧档、重置、各声望门槛、U 盘分支、真结局、常驻内容、新开局、结局变体。
- `steamcmd` + `workshop_build_item` 发 hidden 工坊测试，再公开。
- 首次上传需在 Steam 页面接受创意工坊法律协议。

---

## 10. 风险与注意事项

- **不要直接重写/覆盖用户当前修改的文档**；先读再改，必要时备份。
- **不要把不可演镜头重新写进代码**：门口、街口、探头、挽手、夜景等没有载体。
- **不要凭空给玩家大势力选项**：必须按实际声望/金钱/物品门控。
- **不要把攻略里的 95 条行为说明当成台词**：它们只是说明，实际需要写成角色台词/场景。
- **访客调度可能和主章节同日冲突**：已用 `LastVisitorDay` 与 `LastQueuedDay` 分离，但仍需实机验证。
- **母亲访客只在真结局出现**：`TryQueueNextVisitor` 会跳过非真结局，后续不要强行排队。
- **旧档重置会清除原版标记但不收回物品**：文档和提示已说明。
- **本地化生成器会保留已有键**：新增键请同时加中英，避免运行时显示 key。
- **当前没有 git 仓库**：大改前复制文件或使用独立分支/备份。
- **Lou MOD 是对比基准，不是要照抄**：借鉴 afterhours/UI/调度/存档 schema；差异化仍是行为化 + 全剧情重制 + 声望门控。

---

## 11. 推荐阅读顺序

1. `EleanorRomance_全流程攻略_纯阅读版.txt`（当前剧情/场次）
2. `EleanorRomanceDev/docs/HANDOFF_项目交接.md`（本文）
3. `EleanorRomanceDev/docs/剧情与选项设计_行为化台本.txt`（设计原则与行为替代）
4. `EleanorRomanceDev/src/EleanorStory.cs`（当前实现）
5. `EleanorRomanceDev/src/StoryDirector.cs`（调度）
6. `EleanorRomanceDev/src/RomanceState.cs`（状态/旧档）
7. `EleanorRomance_剧情与选项设计_旧版对白选项.txt`（旧台词复用）

---

## 12. 交接检查清单

- [ ] 已阅读 v0.4 剧情稿，理解“只写可显示文本”的规则。
- [ ] 已确认当前代码是“Phase 1 访客/通讯 + 旧章节选项”的混合状态。
- [ ] 已确认访客日程与真结局母亲门槛。
- [ ] 已确认行为系统属于第二阶段，不把菜单当最终方案。
- [ ] 已确认构建命令与输出目录。
- [ ] 已确认旧档检测/重置与版本闸门逻辑。
- [ ] 已确认内容量差距（对比 Lou MOD）。
- [ ] 大改前已备份相关文件。
