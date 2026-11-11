# 0.90：革命军线 R01—R14

2026-11-11。状态：**已构建、已打包、已发布到原创意工坊条目 3834588749**；GitHub 同步待提交完成，实机验收待完成。

## 内容

- 实装革命军线 13 次事件：R01、R02、R04—R14，运行章节 101—113；R03 仍并入 R02/R04。
- 补给累计：G1=400、G2=1000、G3=1800，按交付物品原生价值计数，不按成交价或玩家收款；正常价、让利与真实 0 价交付同值。每次有效交付显示“革命军补给：当前累计／下一阶段目标”。
- 门槛等待期间保留补给/战利品短访；R05 初始档、R07 第一档、R10 第二档、R13 最高档货源。R06 与 R14 使用清晨优先队列；R12 完成后间隔 3 天触发 R13。
- R14 最后一句“我想见你”之后立即显示独立 E9 结局页，复用 `er_ending_revolution_cg`；GameOver 不重复。E1—E14 编号与完美集合 E1/E4/E3/E9 不变，E10 保留旧档/朋友用途。
- 旧八次丙线存档按最近阶段续接；已完成的旧短版只标记 legacy，不追溯授予新 R14、最高档或结局页。
- 中文正文 73 个 block，英文同步；旧 `rc.b001—b015` 已从 Content 移除。

## 2026-11-11 文风修订审核

- 审核 `全局剧本_修订稿_2026-11-11.md`，并入 c01–c23 的句子级收紧、c03/c10 的行合并与 m28_b002 的反差句；中英同步。
- 弃用 c06 的“等你分吃的”分支假设与“社区精神”对照句、c11 的“当我没说”、c16 的“想了一路”、c19 的问答式长句、m19_b006/dr17_open 的重复或对称插入，以及 c09/c10 的“我家里能帮”家族代理改写。
- 修订后正文：story 159 block/540 行，maid 180/628，合计 503 block/1,718 行；中 2,178 / 英 2,236 键。

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
- 工坊说明：`docs/WORKSHOP_DESCRIPTION_0.90.txt`，6,899 UTF-8 字节，低于 8,000 限制；更新日志 `docs/WORKSHOP_CHANGELOG_0.90.txt`。
- 扫描说明：本机没有 PowerShell 7，游戏自带 ModScanner 在 PowerShell 5.1 下报 `MissingMethodException`，因此本轮未取得原生扫描回执；已人工核对新增代码未引入 files/network/reflection/process/native 能力，0.89 的清理扫描结果仍作为上一次基线。
