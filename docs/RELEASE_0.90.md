# 0.90：革命军线 R01—R14

2026-11-11。状态：**已构建、已打包、已发布到原创意工坊条目 3834588749，并已同步至 GitHub `main`**；实机验收待完成。

## 内容

- 实装革命军线 13 次事件：R01、R02、R04—R14，运行章节 101—113；R03 仍并入 R02/R04。
- 补给累计：G1=400、G2=1000、G3=1800，按交付物品原生价值计数，不按成交价或玩家收款；正常价、让利与真实 0 价交付同值。每次有效交付显示“革命军补给：当前累计／下一阶段目标”。
- 门槛等待期间保留补给/战利品短访；R05 初始档、R07 第一档、R10 第二档、R13 最高档货源。R06 与 R14 使用清晨优先队列；R12 完成后间隔 3 天触发 R13。
- R14 最后一句“我想见你”之后立即显示独立 E9 结局页，复用 `er_ending_revolution_cg`；GameOver 不重复。E1—E14 编号与完美集合 E1/E4/E3/E9 不变，E10 保留旧档/朋友用途。
- 旧八次丙线存档按最近阶段续接；已完成的旧短版只标记 legacy，不追溯授予新 R14、最高档或结局页。
- 工坊说明新增“旧存档兼容”：从未订阅过本 MOD 的存档，首次开店会触发“旧档案”事件；无论原埃莉诺进度如何，可选择重置她的故事并从本 MOD 第 1 章重开。
- 中文正文 73 个 block，英文同步；旧 `rc.b001—b015` 已从 Content 移除。

## 2026-11-11 文风修订审核

- 审核 `全局剧本_修订稿_2026-11-11.md`，并入 c01–c23 的句子级收紧、c03/c10 的行合并与 m28_b002 的反差句；中英同步。
- 弃用 c06 的“等你分吃的”分支假设与“社区精神”对照句、c11 的“当我没说”、c16 的“想了一路”、c19 的问答式长句、m19_b006/dr17_open 的重复或对称插入，以及 c09/c10 的“我家里能帮”家族代理改写。
- 修订后正文：story 159 block/540 行，maid 180/628，合计 503 block/1,718 行；中 2,178 / 英 2,236 键。

## 2026-11-11 再修订审核（本地，未重新发布）

- 逐章审核 `全局剧本_再修订.md`，仅并入：`c01.b001`、`c01.b002`、`c01.b003` 首句、`c02.b001`、`c03.b001`、`c07.b001`、`c08.b001` 第 3 句、`c12.b001`、`c12.b002`。
- 其余改动因新增幽灵接话、条件分支无依据、删掉关键细节、改写 v0.4 玩家线或整体变长而未采用；候选稿归档为 `docs/archive/script_docs/全局剧本_再修订_未采用_2026-11-11.md`，审核表见 `docs/再修订审核_2026-11-11.md`。
- 本地正文：story 159 block/519 行，合计 503/1,693；Localization 2,153 zh / 2,211 en；`check_project.py`、`check_route_map.py`、Release 构建均通过。
- 高频意象清理：全稿“账”25→8、“名字”30→15、“毛病”16→6；“不是A，是B”式对仗 11→5（余下为剧情指令、疑问或口语），全稿“不是”98→75。
- 留白删除（第二轮）：删除 21 行低影响口癖/总结句；行首“……”101→0、任意“……”172→65，实际“后来”26→2，财务义“笔”21→0（全稿只剩 3 处真实的笔）；“记住+记得+记着”45→29，“只是”26→13，“不过”20→3，“真的”14→6，“那天”13→5；“以前—现在”同 block 27→20。
- 剧情与表情复核：修复 `c12.b003` 的接受句、`c20.b006` 的新人指代、`m23_b009` 的清单前因；表情标签 `c01.b002` sad→angry、`c20.b002` determined→shy、`c22.b010` sad→soft。全稿 doc/Content 逐 block 对齐 0 差异。
- 已发布的创意工坊 0.90 包和 Nexus 0.90 包仍为审核前的 540 行 / 2,178 key 版本；本次未重新打包或发布。

## 已知限制

- 未做实机验收：选项 1/2 的行动桥接行、清晨入队、门槛等待短访和 E9 页面仍需进游戏验证。
- 免费交付未新增独立界面；依赖原生交易把价格压到 0。若原生不允许 0 价，玩家仍可让利，累计按物品价值照计，但“0 价赠送”动作没有单独入口。
- 私人背心美术仍是候选资源，未登记运行时键；R06/R11/R14 按规划回退 `eleanor_rev`，正文不描述未显示的裁剪。
- R02/R05/R08 的物品使用原生奢侈品/工具/药品实例并改显示名以匹配台本；物品 ID、价值与美术仍为原生资源。
- 旧档映射只做阶段对齐，不补演已错过的 R05/R06 等新章；旧档在后续门槛需要重新累计补给。
- 门槛数值 400/1000/1800 为按原生物品价值校准的首版参数，待实机经济复核。

## 验证

- `python tools/build_content.py`：生成 `src/StoryV06Content.g.cs`、`src/RevolutionContent.g.cs`、Localization。
- `python tools/check_project.py`：通过；革命军 73 blocks / 55 UI 键，全稿中英无缺键（2,178 zh / 2,236 en）。
- `python tools/check_route_map.py`：0.90，18 节点，E1—E14，通过。
- `dotnet build EleanorRomance.csproj -c Release`：0 警告、0 错误。

## 发布证据

- DLL SHA-256：`D2150E14D59DF12DC6090F2EC6F6A27523ED8C8DDF5BF5288283EBBF01FC1D2B`。
- 运行包：49 个文件，5,234,340 字节；暂存目录 `dist/workshop-0.90/EleanorRomance`，清单与逐文件哈希在 `dist/workshop-0.90/release-files.json`。
- ZIP：`dist/EleanorRomance-0.90.zip`；SHA-256 `BA0874C870BAD93E3C677BFCFE5442E6CAE374B74B75BF3C818EEFDBED255A48`。
- 工坊发布：`k_EResultOK`，条目 `3834588749`，内容与说明同时提交，无待接受协议；完成于 2026-11-11T06:27:26Z，耗时 6.05 秒；回执 `dist/workshop-0.90/publish-result.json`。
- 远程复核：`result=1`、`file_size=5,234,340`、`time_updated=1794378445`、公开、未封禁；远程快照 `dist/workshop-0.90/remote-after.json`。
- 工坊说明：`docs/WORKSHOP_DESCRIPTION_0.90.txt`，采用简短“旧存档兼容”口径后为 7,375 UTF-8 字节，低于 8,000 限制；更新日志 `docs/WORKSHOP_CHANGELOG_0.90.txt`。
- 说明补充发布：两次 `description-only` 更新均为 `k_EResultOK`；回执 `dist/workshop-0.90/publish-description-result.json`、`publish-description-concise-result.json`；远程复核 `remote-after-concise.json`，远程说明 7,375 字节且包含中英“旧存档兼容”口径。
- 扫描说明：本机没有 PowerShell 7，游戏自带 ModScanner 在 PowerShell 5.1 下报 `MissingMethodException`，因此本轮未取得原生扫描回执；已人工核对新增代码未引入 files/network/reflection/process/native 能力，0.89 的清理扫描结果仍作为上一次基线。
- GitHub：发布提交 `9becd8e` 已推送到 `https://github.com/imbalilang/Eleanor-Remake-Mod.git` 的 `main`；同步细节保存在本地 `dist/workshop-0.90/github-sync.json`（发布证据目录不随仓库提交）。

## Nexus

- `docs/NEXUS_README.txt` 已更新到 0.90：包含革命军线、三档补给、E9 结局页，以及简短的“旧存档兼容”口径。
- 0.90 Nexus Beauty 包已重新构建：271 个文件，15,663,723 字节，DLL SHA-256 `BA10D09EEEA7A74FA15FE9E207D5614E8DC298917C04956426786F6D6F29E8F6`。
- ZIP：`dist/EleanorRomance-0.90-nexus.zip`；SHA-256 `EC36CE60867518B90ECE4CCE3B76FDDB539B6449B2CACC746B6DC36FE0431B04`。
- 尚未上传到 Nexus；Nexus 页面说明与包体为手动发布准备。
