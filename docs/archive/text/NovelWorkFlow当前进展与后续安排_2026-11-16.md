# NovelWorkFlow 当前进展与后续安排

**核查日期：** 2026-11-16  
**工作分支：** `codex/1.12-stabilization`  
**范围：** 五场试点报告之后的 W0–W2 工作包、R2 隔离试验准备、生产正文保护状态

## 1. 当前结论

W0–W2 已完成。试点适配器已经支持独立输出目录、显式世界书、输入漂移检查和候选父稿绑定；交易事实和革命军结构的离线研究也已完成。

当前没有执行付费主笔调用，没有生成 R2 的 Opus/Gemini 候选，没有运行 Super QA 候选修订，也没有把任何候选写入 `Content/`。

下一步不应直接重跑模型。`love_a2_01` 需要先处理正式模板冲突；`v2_r1c1` 的 R1/R2 研究都没有形成可以安全生成正文的结构。

## 2. 已完成事项

### W0：证据校准

产物：

- `tools/NovelWorkFlow_Eleanor_Test/Test_Output/PILOT_2026-11-16-R2/W0_W2_index.json`
- `.../baseline.json`
- `.../baseline_hashes.json`
- `.../previous_evidence_hashes.json`
- `.../w0_w2_manifest.json`

已确认：

- R2 基线记录 59 个当前工作区生产文件；这是“相对于 R2 基线未漂移”，不是表示 Git 工作区干净。
- 历史试点目录保持只读，R2 准备过程没有改写历史候选。
- R2 记录的生产默认主笔仍是 `anthropic/claude-opus-5.5`；Gemini 仅为平行候选。
- R2 付费调用数为 0，`production_written=false`。

### W1：工具和契约

已完成并通过 Python 编译：

- `tools/novel_workflow_pilot.py`
  - 必须使用新的 `--output`；历史目录拒绝写入；
  - 支持 `--world-book` 和 `--scenes`；
  - 已生成输入发生漂移时拒绝覆盖；
  - 请求前检查输入、基线和 16 MB 工作上限。
- `tools/novel_workflow_pilot_qa.py`
  - 不再固定审查 A；
  - 必须显式提供候选父稿、Brief 和资格文件；
  - 记录 `candidate_parent_sha256`。
- `tools/novel_workflow_contract.py`
  - 把交易、公共尾段、选择、路径和运行事实检查与文风 linter 分开。
- `tools/test_novel_workflow_r2.py`
  - 覆盖输出隔离、幂等准备、输入漂移拒绝、缺失预算拒绝和生成闸门。

离线回归结果：

```text
PASS: R2 adapter isolation, contract gate, idempotence and drift checks
```

### W2：场景输入与结构研究

#### `love_a2_01`

已将交易事实固定为：

```text
T1 / BUY / HOUSEHOLD_GOOD / 预算150 / 客户索菲娅
```

并记录运行顺序：

```text
la2_s1_pre → C1回应 → 原生交易 → 四结果之一 → la2_s1_common → LoveA2Closing.FinishFromVisit
```

共同段只允许执行一次。静态角色卡恢复为正式全局版本，阶段行为放在隔离 ActCard/state 中。

当前闸门：`blocked-template-approval`。

原因：正式模板仍同时包含旧的章节体量要求和 `预算=200` 的交易示例。模板未获批准前，不发送模型请求。

#### `v2_r1c1`

已完成两份结构研究：

- `revolution-R1/structure/R1.md`：认尸保留在本场，但同场重排不足以消除信息集中和 16 句连续；建议暂不生成正文。
- `revolution-R2/structure/R2.md`：将认尸前移到 c14 不可安全承接，会影响事实时序和其他路线；建议保留 A。

两个方案都保留 `C1`、`T1`、四个交易结果、`CompleteScene(280)` 和下一节点 281。R2 不新增章节键，也没有伪造 line map。

当前闸门：

- R1：`blocked-timeline-review`
- R2：`blocked-structure`

## 3. 已完成的检查

- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过。
- `python tools/NovelWorkFlow_Eleanor/check_worldbooks.py`：通过，8 个 active world book 合规。
- 三份 R2 场景 contract 检查：通过。
- 试点适配器和回归测试 Python 编译：通过。
- `template_proposal.diff`：`git apply --check` 通过；正式模板实际未修改。

## 4. 尚未完成与当前阻塞

### 未完成

- 没有 R2 主笔 B 候选。
- 没有以 B 为父稿的 Super QA C 候选。
- 没有 A/B/C 阅读结论或采用决定。
- 没有正文 patch、英文同步、构建或候选实机验证。
- `tools/test_runtime_flows.ps1` 的旧 Cecil 夹具问题仍未作为本轮内容工作的一部分修复。

### 需要作者决定

1. **是否批准正式模板的最小冲突修复。** 提案在：
   `tools/NovelWorkFlow_Eleanor_Test/Test_Output/PILOT_2026-11-16-R2/template_proposal.md` 及 `template_proposal.diff`。
   该修改只增加“现有场景修订/结构研究”任务模式、统一 ActCard 体量口径、禁止主笔猜交易参数、允许公共尾段按运行契约映射；不修改 `writer_system_prompt.md`。
2. **是否承担革命军新增来访或前场改动的路线/旧档成本。** 在没有这个决定前，`v2_r1c1` 保留当前文本，不生成 R1/R2 正文。

## 5. 推荐后续顺序

### 第一步：等待模板决定

当前最有价值的下一步是 `love_a2_01` 的事实契约复跑。建议批准最小模板 diff 后再继续；不建议为了绕过模板闸门建立第三套临时提示词。

### 第二步：应用并验证模板 diff

只有得到明确批准后：

1. 保存正式模板当前 SHA-256；
2. 应用 `template_proposal.diff`；
3. 重新生成 R2 输入哈希；
4. 运行 `check_worldbooks.py`、适配器回归、场景 contract 检查；
5. 确认 `writer_system_prompt.md`、正式 world book 和 `Content/` 没有非目标变化。

### 第三步：只做 `love_a2_01` 有界生成

最多两次串行主笔请求：

- Opus 一次；
- Gemini 一次作为平行对照。

两个请求使用相同事实契约和同一体量目标，只检验交易参数和公共尾段是否服从。不得借此重写整场、减少事实或新增选择。

### 第四步：合同检查和定点 QA

先检查 B 的事实、路径、交易声明和公共尾段映射。失败则直接 `retain_A`，不让 Super QA 修复预算或结构错误。只有 B 合格，才生成以 B 为父稿的 Brief 和 C。

Super QA 最多一次针对明确的 P0/P1 或作者批准的 voice pass；没有明确目标时记录 `zero_changes`，不发送请求。

### 第五步：采用闸门

只有候选同时满足以下条件，才生成未应用 patch：

- 预算为 150、客户为索菲娅、类型为 `HOUSEHOLD_GOOD`；
- 三个回应、四个交易结果和共同段均保留；
- 每条可达路径的共同段只出现一次；
- 没有新增选择、物品、客户、状态、表情或空间位置；
- 角色声音和玩家回应比 A 有明确净收益，而不是仅仅更短；
- 适配器、contract、Super QA 和中英文键检查全部通过。

没有净收益时，保留 A，结束本轮调用，不生成 patch。

### 第六步：正式接入前的工程验证

若确有采用候选，才执行：

1. 生成逐行 `patch_manifest.json`；
2. 更新中文源并通过既有英文同步流程；
3. `build_content.py`；
4. `check_project.py`、`check_route_map.py`；
5. 内置 SDK Release 编译；
6. 检查停水四个交易结果到 `la2_s1_common` 的单次接续、闭店回调、次日入口；
7. 修复并运行实际 runtime harness；
8. 有实机证据后才把场景标为可发布。

## 6. 当前总体判断

本阶段已经从“规划”进入“可控试验准备完成”。目前最正确的状态是停在闸门前，而不是为了显示进度强行生成候选。

若批准模板最小修复，下一轮只验证 `love_a2_01`；革命军暂不调用。若不批准，保留现稿并转入其他不依赖模板修改的离线审计，不新增付费请求。


## 6. G0 执行结果（2026-11-17）

按 `template_proposal.diff审阅批注_2026-11-17.md` 完成了离线 G0 修订。三份隔离场景已统一使用 `existing_scene_revision`、结构化长度预算、机器统计口径和交易结果映射；停水场景的预算、客户、类型与公共尾段已锁定，革命军仍只保留结构研究闸门。

准备阶段现在会写入预算哈希、任务模式、统计口径、公共尾段和 `over_budget_report`；非法预算、可复制占位符、缺失字段和重复公共尾段会在模型请求前拒绝。三份新鲜临时输出均通过 prepare/verify，R2 回归、项目检查、路线检查、隔离 world book 检查和 Python 编译均通过。

正式 `writer_prompt_template.md`、`writer_system_prompt.md`、正式 world book 和 `Content/` 未修改，模型请求数仍为 0。当前阶段已从 W0–W2 进入 G1，等待作者批准最小模板 diff；批准前不调用模型。
