# 0.91：修复选项气泡把 NPC 分支当成玩家选项

2026-11-11。状态：**已构建、已打包、已发布到原创意工坊条目 3834588749**；实机验收待完成。

## 内容

- 原生 `DialogUIManager.DisplayChoice()` 显示 `choices[i].Item2.plainText`；直接 `AddChoice` 若把 NPC 分支作为 next，就会把 NPC 的第一句显示成玩家选项。
- 新增 `StoryEngine.PlayerReply(text, next)`，所有直接 `AddChoice` 菜单先插入玩家回应节点。
- 修复：
  - C08 回礼第三个选项：先显示玩家“你留着换钱吧。”，再进入 Eleanor 的“下次我挑一件……”回应。
  - 侍从入口“还是算了”返回选项。
  - 保护失败 fallback 选项。
  - 吊坠婉拒选项。
  - 纪念物“稍后领取”选项。
  - 保释官“之后再救”选项。
- 选项文本、分支结果、数值、路线和 E1—E14 编号不变；只修正玩家/NPC 发言归属。
- 保留 0.90 的革命军线、0.89 像素结局 CG、0.88 Lydia 独立画像与全部既有内容。

## 验证

- Release 构建：0 警告、0 错误。
- `tools/check_project.py`：通过（无缺键、无非法块引用）。
- `tools/check_route_map.py`：通过（0.91，18 个时间线节点，E1～E14；路线、编号、完美集合未变）。
- 实机仍需验证：上述六类直接选项气泡文本、点击后的玩家回应与 NPC 分支、旧档续接。
- 原生扫描本轮未执行；本机缺少 PowerShell 7。

## 构建与包

- DLL SHA256：`C5D8BA423AD823EDC79DC1FC0E53CCE33B8BBB8506F191BCF48628DBC9DF731D`。
- 运行包 49 个文件，5,232,378 字节。
- ZIP：`dist/EleanorRomance-0.91.zip`；ZIP SHA256：`B75A6C427971A1318CCBA3BBBE96B8B663DA09118DF16B46AC2BF6671788059E`。
- 工坊发布：首次尝试 `k_EResultFail`，核对远程仍为 0.90 后重试；第二次 `k_EResultOK`，内容与说明一并提交，49 个文件，5,232,378 字节；完成时间 2026-11-11 19:28:07（北京时间），耗时 5.04 秒。
- 远程复核（Steam GetPublishedFileDetails）：`result=1`，`file_size=5,232,378`，`time_updated=1794396487`（2026-11-11 19:28:07 北京时间），`visibility=0`（公开），`banned=0`，`hcontent_file=1293614828236397252`；远程正文与 `docs/WORKSHOP_DESCRIPTION_0.91.txt` 逐字一致（7,125 字节）。
- 发布回执：`dist/workshop-0.91/publish-result.json`；远程快照：`dist/workshop-0.91/remote-after.json`；暂存目录：`dist/workshop-0.91/EleanorRomance`。
