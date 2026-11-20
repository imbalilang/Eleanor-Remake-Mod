# 美术资产接管与项目现状说明

日期：2026-11-19
接管范围：Eleanor Story Expand 当前工作区的角色画像、表情、CG、物品图标、场景参考与美术管线。
项目版本：1.21

## 一、接管结论

当前运行美术资产完整可审计，内容工程检查通过：

- `Textures/Items/`：132 张 PNG，合计 16,734,084 bytes。
- 画像：17 张（5 张 `cp_portrait_*` 基础 Eleanor，12 张 `er_portrait_*` 本地角色/服装像）。
- 表情：104 张，覆盖 11 个表情族。
- CG：8 张，包含 4 张高清事件 CG 和 4 张结局 CG。
- 物品/奖章：3 张 64×64 图标。
- 审计结果：`all_runtime_png_unchanged: true`，`flagged: 0`。
- `python tools/check_project.py` 返回 OK，文本块、双语键和资源引用均通过。

接管原则：**Art/ 是源文件与候选区，Textures/Items/ 是运行区。没有作者明确指令，不把候选图写入运行资源。**

## 二、运行资产的权威边界

| 位置 | 职责 |
|---|---|
| `Textures/Items/` | 实际随包发布的 PNG。 |
| `src/PortraitCompat.cs` | 画像键解析、回退、外部 MOD 兼容、立绘切换。 |
| `src/PortraitExpression.cs` | 对白表情键解析。 |
| `manifest.xml` | MOD 版本、发布元数据；当前 1.21。 |
| `docs/美术资源登记_1.21_2026-11-19.json` | 当前 1.21 运行美术登记，132 张 PNG，含逐文件 SHA-256。 |
| `docs/美术资源登记_2026-11-16.json` | 历史运行登记，版本 1.12，保留归档，不再作为当前事实表。 |
| `Art/` | 母图、候选、参考、对比、处理记录，不自动进入运行区。 |
| `Art/art-audit-handover-2026-11-19/asset_inventory.json` | 当前 132 张运行 PNG 的逐文件审计结果。 |

## 三、运行画像与表情

### 画像族

- Eleanor 基础：`cp_portrait_eleanor1.png`、`cp_portrait_eleanor2.png`、`cp_portrait_eleanor3.png`、`cp_portrait_eleanor_scav.png`、`cp_portrait_eleanor_rev.png`。
- Eleanor 本地服装：女仆、礼服、亲密装、革命军黑色战术服。
- Lydia：日常装、约会装；`_vanilla` 只作为旧档迁移/防崩溃回退。
- Sophia：日常装、居家针织装；`_vanilla` 只作为旧档回退。
- 主角色像文件为 288×212 RGBA，alpha 全部为 0/255。

### 表情族（104 张）

- `eleanor1`：9
- `eleanor_scav`：9
- `eleanor_maid`：10
- `eleanor_gala`：9
- `eleanor_love_intimate`：9
- `eleanor_rev`：9
- `eleanor_revolution_undersuit`：9
- `lydia_daily`：11
- `lydia_date`：11
- `sophia`：9
- `sophia_home`：9

命名规则：`er_expr_<画像族>_<表情标签>.png`。常用标签包括 angry、determined、hurt、sad、serious、shy、smile、soft、tired；Lydia 另有 flustered、tease；女仆另有 tsun。

### 解析规则

- 本地 `er_portrait_*` 直接作为本 MOD 的角色像。
- `cp_portrait_*` 是内置 CutePortraits 基础像，按作者授权随包使用。
- 缺图时按 `PortraitCompat` 回退到原版/本 MOD 安全键；`_vanilla` 不是默认显示版本。
- 其他 MOD 的埃莉诺立绘按 Portrait mode 处理，不能强行覆盖其他角色或破坏其他 MOD。

## 四、CG 与图标

### 已运行 CG

高清事件 CG（1664×936）：

- `er_event_harem_lydia_date_cg.png`
- `er_event_love_water_cut_cg.png`
- `er_event_maid_gala_cg.png`
- `er_event_revolution_embrace_cg.png`

结局 CG：

- `er_ending_love_wedding.png`：1664×936，高清婚礼 CG。
- `er_ending_maid_cg.png`：960×540，像素风侍从结局。
- `er_ending_harem_cg.png`：960×540，像素风后宫结局。
- `er_ending_revolution_cg.png`：960×540，像素风革命结局。

运行层注意：高清事件 CG 和婚礼 CG 需要双线性过滤；三张像素结局 CG 继续使用最近邻。

### 物品图标

- `eleanor_bracelet.png`：64×64。
- `eleanor_locket.png`：64×64。
- `er_lydia_recommendation_plaque.png`：64×64。

## 五、源美术树

| 目录 | 内容 |
|---|---|
| `Art/v0.5/`、`Art/v0.6/` | 早期母图、导出脚本和校验记录。 |
| `Art/gala/` | Eleanor 晚会礼服母图。 |
| `Art/dual-romance-v0.3/` | Lydia 日常/约会画像与导出。 |
| `Art/lydia-style-fusion-2026-11-10/` | Lydia 定稿融合、约会母图和导出。 |
| `Art/mother-redesign-2026-11-11/` | Sophia 正式服/居家服定稿与导入记录。 |
| `Art/revolution-route-2026-11-10/` | 革命军黑色战术服母图与候选。 |
| `Art/love-intimate-outfit-2026-11-13/` | Eleanor 亲密私服与画像。 |
| `Art/love-ending-2026-11-13/` | 婚礼结局 CG 源文件。 |
| `Art/route-endings-pixel-2026-11-11/` | 侍从/后宫/革命像素结局 CG。 |
| `Art/expressions-and-endings-2026-11-10/` | 早期表情与侍从结局交付。 |
| `Art/pixel-art-direction-2026-11-19/` | 当前像素画法基准、头部校准和透明化工作，不是运行资产。 |
| `Art/场景参考/` | 下层区场景参考与场景风格分析。 |
| `Art/model-tests/` | 模型测试和风格实验，体量大，不进入运行包。 |

当前已采纳的像素画法母图：

- `Art/pixel-art-direction-2026-11-19/masters/eleanor_pixel_cyberpunk_portrait_master.png`
- `Art/pixel-art-direction-2026-11-19/masters/eleanor_pixel_motorcycle_cg_master.png`

这两张是画法基准和候选母图，不代表已经替换运行画像或 CG。

## 六、管线与验证工具

- `tools/author_portrait_pipeline.py`：B/detail 精确面积采样、透明区 RGB 扩散、detail、顶部补行。
- `tools/export_expression_assets.py`：Eleanor 表情导出/装配。
- `tools/export_lydia_expressions.py`：Lydia 表情导出/装配。
- `tools/build_content.py`：Content JSON 到本地化/生成文本。
- `tools/check_project.py`：内容、双语键、场景与块引用检查。
- `tools/audit_art_assets.py`：运行 PNG 清单、alpha、包围盒、尺寸和缺失中性参考检查。
- `tools/scan_package.ps1`：本地部署包扫描。
- `tools/package_release.ps1`、`tools/package_nexus.ps1`：发布包生成。

渲染/导出后必须保留母图、实际提示、参考图分工、参数和 SHA-256。不得用聊天截图或有损图替代母图。

## 七、当前 CG 计划状态

依据 `docs/CG_CURRENT_PLAN.md`：当前为 4 条路线 × 8 格，共 32 格；结局不计入新增事件 CG 预算。

- 需要重做：1 格（恋爱线索菲娅递水）。
- 未设计：15 格（后宫 4、侍从 5、革命军 6）。
- 未标注/待确认：16 格。
- 现有事件 CG 和结局 CG 覆盖了部分格位，但当前计划表中仍有大量空的、未设计的或等待确认的格位。

正式文件仍把事件 CG 定为 A 规制（高清人物插画），P 规制用于结局/世界/档案层。最近作者指定的像素风摩托车 CG、下层区主路 CG 是实验性候选，尚未被写成正式运行替换。不要把两者混用。

## 八、审计发现与风险

1. **运行登记已更新**：已生成 `docs/美术资源登记_1.21_2026-11-19.json`，以当前 132 张运行 PNG 的实际尺寸、alpha、字节数和 SHA-256 为准；`docs/美术资源登记_2026-11-16.json` 仅作 1.12 历史归档。
2. **表情中性参考已补全**：`tools/audit_art_assets.py` 已把 11 个表情族映射到内置运行中性像，不再依赖外部 CutePortraits 路径。审计从 54 个 `local_neutral_reference_unavailable` 降为 0 个标记；gala、love_intimate、rev、revolution_undersuit、sophia、sophia_home 均已覆盖。
3. **像素新画法尚未完成运行化**：头部校准 v1 已有方向，但透明母图仍有头发白点；白色边缘、头部比例和表情位置尚未冻结。不能提前把校准图写入运行画像。
4. **纯白背景 keying 的风险**：全局去白会打穿白夹克；需要头发专用手工修补，不应继续自动全图抠白。
5. **场景与 CG 资产还没有运行接入**：下层区主路、摩托车 CG 等都是实验候选，地点/插入点需要按 `CG_CURRENT_PLAN` 和 `CG_LOCATION_AND_PLACEMENT_PLAN` 确认。
6. **发布版本纪律**：任何进入 Workshop/Nexus 的美术更新必须 bump `manifest.xml` 的 ModVersion，并同步 README/发布记录；外部 site 同步必须由作者单独授权。

## 九、接管后的工作规则

- 不覆盖运行资源，先做候选和对比。
- 不删除母图、旧版和失败候选；失败版本放在对应试验目录。
- 每次生成/导出保留 prompt、参考分工、参数、输入输出哈希。
- 先看 288×212 原生 1 倍、最近邻 3–4 倍，再在白/黑/#252C35 底检查 alpha 和边缘。
- CG 先确认地点、触发点、服装理由、姿态、光源和夜景流程；没有落点不直接生成。
- 不自动把像素实验写成正式 A/P 规制；需要作者确认后才改变项目级画风。
- 发布前运行 `python tools/check_project.py`、美术审计和包扫描；不自动推 GitHub、Steam Workshop 或 Nexus。

## 十、建议的下一步

1. 已完成：1.21 运行美术登记 `docs/美术资源登记_1.21_2026-11-19.json`。
2. 继续完成像素头像的头部校准和头发白点手修，但保持为候选，不写运行资源。
3. 从 `docs/CG_CURRENT_PLAN.md` 的未设计格位中选一小格，先做完整 CG brief，再进入美术生成。
4. 已完成：审计已补齐 11 个表情族的中性参考映射，当前 0 个审计标记。
5. 若要正式转向像素 CG，先更新项目级画风规则和 CG 计划，不要把实验图直接接入。

## 十一、执行入口

- 工作队列：`docs/ART_ASSET_WORKQUEUE_2026-11-19.md`
- 当前 1.21 登记：`docs/美术资源登记_1.21_2026-11-19.json`
- 当前审计：`Art/art-audit-handover-2026-11-19/asset_inventory.json`
- 第一个待确认 CG brief：`docs/CG_BRIEF_revolution_bike_ride_2026-11-19.md`
