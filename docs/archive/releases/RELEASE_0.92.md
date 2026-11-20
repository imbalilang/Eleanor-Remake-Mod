# 0.92：双恋入口台词与声望提示修正

2026-11-11。状态：**已构建、已打包、已发布到原创意工坊条目 3834588749**；实机验收待完成。

## 内容

- 双恋入口选项从系统说明改为角色口吻：`dr01_accept` 中文“那你教我几句。”、英文“Then teach me a few lines.”。
- 路线、奖励与声望条件移入选项说明：进入埃莉诺与莉迪亚的三人关系扩展；保留原恋爱资格；上层区声望 50／100／150 依次推进并获得三件橱窗饰品；不扣声望，未达标可继续经营与日常。
- 双恋等待期日常不再插入旁白式的“上层区声望……”对话节点；改为场景开始时 `StoryDirector.Notify(progress)` 顶部提示（`src/DualDaily.cs`）。
- 加入索菲娅 v3 上层日常装与 v4 奶油白针织私服定稿；启用少女当铺时使用新图，未启用仍回退原游戏 `femaleUpper3`。
- 选项结果、数值、路线与 E1—E14 编号不变；保留 0.91 的选项发言归属修复与全部既有内容。

## 验证

- Release 构建：0 警告、0 错误。
- `tools/check_project.py`：通过（zh 2153 键、en 2211 键，无缺键、无非法块引用；双恋 75 块／13 选项）。
- `tools/check_route_map.py`：通过（route map 0.92，manifest 0.92；18 个时间线节点，E1～E14）。
- 实机仍需验证：双恋入口选项气泡与说明、等待期顶部声望提示、索菲娅 v3/v4 画像在有／无少女当铺时的回退。
- 原生扫描本轮未执行；本机缺少 PowerShell 7。

## 构建与包

- DLL SHA256：`45F02A48CAEC35E628F0E57693505C6E64335D7E195A21814F76DA8AC3B15633`。
- 运行包 50 个文件，5,279,184 字节。
- ZIP：`dist/EleanorRomance-0.92.zip`；ZIP SHA256：`41A49448471727119409765B56473D6CC7DE545291374D246531FACB9DCDDE72`。
- 工坊发布：首次尝试即 `k_EResultOK`，内容与说明一并提交，50 个文件，5,279,184 字节；完成时间 2026-11-11 21:54:40（北京时间），耗时 4.54 秒。
- 远程复核（Steam GetPublishedFileDetails）：`result=1`，`file_size=5,279,184`，`time_updated=1794405280`（2026-11-11 21:54:40 北京时间），`visibility=0`（公开），`banned=0`，`hcontent_file=2746402240846112642`；远程正文与 `docs/WORKSHOP_DESCRIPTION_0.92.txt` 逐字一致（7,211 字节）。
- 发布回执：`dist/workshop-0.92/publish-result.json`；远程快照：`dist/workshop-0.92/remote-after.json`；暂存目录：`dist/workshop-0.92/EleanorRomance`。
