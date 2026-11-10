# Eleanor Story Expand · v0.6 美术需求

更新日期：2026-11-06  
适用范围：`EleanorRomanceDev` / Workshop 物品 3834588749

> 2026-11-11 兼容策略更新：Lydia 是恋爱对象，未启用少女当铺时继续使用本文件的 `er_portrait_lydia_female_vanilla` 独立立绘（缺图才回退 `femaleUpper10`）；母亲未启用少女当铺时回退原游戏 `femaleUpper3`，自定义母亲 `_vanilla` 不再作为运行显示图。女仆/礼服/战术背心未启用时回退 Eleanor 原游戏画像。下表旧的双版本说明保留作美术记录，运行行为以本注和 `src/PortraitCompat.cs` 为准。

## 1. 当前状态

- Eleanor 与所有通用 NPC：复用游戏原版立绘键；少女当铺（Workshop 3829696854）启用时自动换成其 `cp_portrait_*` 美化立绘。
- **莉迪亚与母亲：方案 B**。无论少女当铺是否启用，都使用本 MOD 自己的专有形象，不再使用少女当铺的通用上层区女性立绘。
- **2026-11-06 已交付**：四张莉迪亚/母亲立绘与两张物品图标已由正式美术替换，`_vanilla` 与 `_beauty` 是两套不同成图。交付与校验见 `ART_DELIVERY.md`、`asset_validation.json`。下文保留为规格与后续替换标准。

## 2. 双版本兼容规则

代码位置：`src/PortraitCompat.cs` / `src/EleanorStory.cs`。

| 环境 | 使用的资源 |
|---|---|
| 未启用少女当铺 | Lydia 使用独立 `_vanilla`；Mother 使用原游戏 `femaleUpper3` |
| 启用少女当铺 | `_beauty` 版本 |
| 对应文件缺失 | Lydia 回退 `femaleUpper10`；Mother 回退 `femaleUpper3` |

以下是 2026-11-06 双版本美术记录；2026-11-11 起 `_vanilla` 图不再用于运行显示，仅保留作历史与旧档清理参考：

### 莉迪亚

| 文件 | 用途 | 风格要求 |
|---|---|---|
| `Textures/Items/er_portrait_lydia_female_vanilla.png` | 无少女当铺（Lydia 独立形象） | 贴近游戏原版 288×212 像素立绘；线稿、描边、色板、光向与原版一致 |
| `Textures/Items/er_portrait_lydia_female_beauty.png` | 有少女当铺 | 贴近少女当铺的动漫像素立绘；与 `cp_portrait_femaleUpper*` 并排时不割裂 |

### 母亲

| 文件 | 用途 | 风格要求 |
|---|---|---|
| `Textures/Items/er_portrait_mother_female_vanilla.png` | 历史/旧档清理 | 同上，原版像素风 |
| `Textures/Items/er_portrait_mother_female_beauty.png` | 有少女当铺 | 同上，少女当铺动漫像素风 |

## 3. 立绘规格

- 尺寸：**288 × 212** RGBA PNG。
- 背景：透明；不要绘制背景、相框、文字、水印。
- 构图：头部到上胸/肩部，人物居中；参考少女当铺 `cp_portrait_femaleUpper3.png` 的取景。
- 像素风：游戏内按 Point 过滤显示；不要使用柔和渐变、抗锯齿描边或高分辨率厚涂。
- 角色辨识：
  - 莉迪亚：上层区旧友，势利、嘴快但不坏；深色短发、耳饰、深绿/青色高领服装方向可参考 `lydia_master.png`。
  - 母亲：前水培农场负责人，温和但有脾气；红棕发、灰白发丝、针织外套/衬衫方向可参考 `eleanor_mother_master.png`。
- 表情：首版每个版本至少 1 张。若要差分，先与程序确认命名与接入；当前代码只使用一个基础键，额外差分不会自动播放。
- 性别推断：文件名必须保留 `female` 标记；代码依赖它让游戏把角色识别为女性。

## 4. 物品图标

| 文件 | 尺寸 | 状态 |
|---|---|---|
| `Textures/Items/eleanor_locket.png` | 64×64 RGBA | 已交付正式 16 色像素图 |
| `Textures/Items/eleanor_bracelet.png` | 64×64 RGBA | 已交付正式 16 色像素图；宝石与镯身连接 |

图标要求：透明背景、深色描边、有限色板、与游戏 `Items/*` 物品图标的像素密度一致；不要带文字。

## 5. 封面与创意工坊预览

- 源文件：`Art/v0.6/masters/workshop_cover_master.png`（1254×1254 RGB）。
- 已生成 `preview.png`：768×768 PNG，859,455 字节，低于 Steam 1 MB 预览限制。
- 封面已包含中文项目名；Workshop 标题使用中英双语：
  `Eleanor Story Expand / 上层区的富家千金爱上在下层区开当铺的我：埃莉诺剧情扩展`

## 6. 目录与交付

- 概念稿/母版：`Art/v0.6/masters/`
- 游戏内导出：`Textures/Items/`
- 角色导出工作目录：`Art/v0.6/portraits/`（当前为空）
- 不要交付或上传 `Art/v0.6/references_local/` 中的游戏提取参考图。

交付前验收：

1. 四张角色立绘均为 288×212 RGBA，透明背景。
2. `_vanilla` 与 `_beauty` 是两套不同的正式美术，不是同一张图的副本。
3. 分别与游戏原版立绘、少女当铺对应立绘并排检查描边、色板、光向、像素密度。
4. 在游戏内以实际缩放检查头部是否被裁切、角色是否偏移。
5. 物品图标在游戏内 1×1 物品格中检查轮廓与可读性。
