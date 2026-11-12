# Route Pipeline（整条路线 → 游戏台本）

这是一条把 `NovelWorkFlow` 的“世界书 + 规划 + 写作 + QA + 状态结算”改造成**游戏台本格式**的流水线。

它不产出小说段落。一次运行生成**一条完整路线**（1—2 章，约 2800—3500 中文字符，24—40 个 block），直接产出可以贴进 `docs/全局剧本.md` 的：

- `## 章节　标题`
- `#### block_id（scene=...）`
- 角色台词
- 短动作备注
- 玩家短选项

## 结构

```
世界观/角色/文风（world_book.json）
        ↓
路线规划器（planner.md）→ route_plan.json
        ↓
台词作者（writer_system.md + writer_template.md）→ route_bundle.json
        ↓
QA/连续性检查（qa.md）→ PASS / FIXED / REJECT
        ↓
组装器 → out/normal.md（整条路线的游戏台本）
```

## 为什么不是小说格式

`NovelWorkFlow` 输出的是 2500—4000 字小说章节。我们的游戏需要：

- 每个 block 有稳定 ID；
- 每行是 `角色：台词`；
- 选项是短标签 + 后续 block；
- 玩家不能长篇自由对白；
- 每章有事件、选择、动作、后果；
- 不写旁白/作者分析/状态面板。

所以 writer prompt 被重写为“游戏台词作者”，并强制输出整条路线的 JSON route bundle。

## 文件

| 文件 | 作用 |
|---|---|
| `world_book.json` | 原作背景、世界/角色/路线/文风/输出规则/路线大纲 |
| `config.example.json` | API 配置示例；复制为 `config.json` 后填自己的 |
| `prompts/planner.md` | 路线大纲 → 1—2 章、24—40 block 的路线计划 |
| `prompts/writer_system.md` | 作者系统提示：游戏格式、角色声音、文风、输出 schema |
| `prompts/writer_template.md` | 本次写作的上下文注入模板 |
| `prompts/qa.md` | JSON/格式/路线契约/声音/连续性检查 |
| `pipeline.py` | 调用 API、组装、校验 |
| `out/` | 生成的 prompt、route bundle、QA 结果和 markdown 台本 |

## 配置

```bash
copy tools\scene_pipeline\config.example.json tools\scene_pipeline\config.json
```

`config.json` 里填**两套独立供应商**：

- `writer.base_url` / `writer.model` / `writer.api_key_env`：主笔/规划器供应商
- `qa.base_url` / `qa.model` / `qa.api_key_env`：QA 供应商
- `paths.preset`：酒馆预设路径，默认 `../夏瑾 天琴座 Beta 2.42.json`
- `generation.temperature` / `max_tokens`

API key 分别从环境变量读取：

```bash
set WRITER_API_KEY=your-writer-key
set QA_API_KEY=your-qa-key
```

主笔和 QA 可以是不同厂商、不同 base_url、不同模型。不要把 key 写进 `config.json`、`pipeline.py` 或 git。

## 爽文体验（硬要求）

`world_book.json` 的 `reader_experience` 现在是硬验收项：

- 每章至少 1—2 个具体爽点：成交、打脸、公开承认、护短、母亲认可、亲密升级、婚礼。
- 爽点必须由玩家的选择/行动带来，不能靠旁白宣布。
- 玩家可以吃瘪一次，但下一拍要赢回来；不连续两场受气。
- 女主高身份、高姿态，但选择必须主动；不是通关奖品。
- 每章至少 2 个埃莉诺私人甜点；冲突短、反击快。
- 不写 NTR、女主被抢走、长期苦情、反派降智爽点。

规划器会输出 `pleasure_beats`，QA 会检查它是否对应正文里的具体动作/价格/物品/公开反应。

> 注意：`NovelWorkFlow/Novel_Engine_V7.py` 里有硬编码的 API key。不要复制或使用它；建议让作者轮换那组 key。

## 运行

先看 prompt，不花钱：

```bash
python tools/scene_pipeline/pipeline.py --route normal --dry-run
```

跑整条常规恋爱线：

```bash
python tools/scene_pipeline/pipeline.py --route normal
```

输出：

- `tools/scene_pipeline/out/normal_planner_prompt.txt`
- `tools/scene_pipeline/out/normal_writer_system.txt`
- `tools/scene_pipeline/out/normal_writer_prompt.txt`
- `tools/scene_pipeline/out/normal_qa_prompt.txt`
- `tools/scene_pipeline/out/normal_route.json`
- `tools/scene_pipeline/out/normal_qa.json`
- `tools/scene_pipeline/out/normal.md`

`normal.md` 就是可直接粘贴的整条路线台本格式（1—2 章）。

## 当前限制

- 只做单候选：先看质量，不做多候选/自动选优。
- 只挂了常规恋爱线 `normal`；其他路线后续在 `world_book.json` 增加 `route_outline`。
- API 输出仍可能有 AI 腔；QA 是第一道过滤，不是最终验收。
- 生成的是候选，不直接写 `Content`、`Localization` 或 `src`。
- 母亲/路线/旧档等事实以 `world_book.json` 为准；它需要持续维护。
- 酒馆预设只抽取非 NSFW 的文风/对白/反八股提示；不加载 NSFW/破限/数值面板提示。

## 下一步

1. 填 `config.json`，跑 `--route normal --dry-run` 看 prompt。
2. 跑 `--route normal`，读 `out/normal.md`。
3. 如果格式/声音方向对，再用同一套 prompt 加其他路线。
4. 如果不对，先改 `world_book.json` 和 writer/qa prompt，不要急着扩全稿。
