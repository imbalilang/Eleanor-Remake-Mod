# 0.89：像素风结局 CG

2026-11-11。用户已确认两张新CG并授权更新原创意工坊条目和GitHub。状态：**已构建、已打包、已发布到原创意工坊条目3834588749**；GitHub同步记录见下，实机验收待完成。

## 内容

- 正式替换侍从线 `er_ending_maid_cg`、后宫线 `er_ending_harem_cg`，960×540、不透明PNG；从已确认母图等比面积采样，无后期减色、锐化或抖动。
- E3/E4即时结局页改为上方完整等比CG、下方结局文字；正文适度自适应字号，避免面板遮挡人物、手部与端茶动作。旧档GameOver背景图也改为保比例。
- 革命军CG v3以 `er_ending_revolution_cg` 随包保存，新14章和R14触发仍待实施，没有挂到旧丙线或失败结局。
- 保留0.88的Lydia独立形象与立绘兼容。18个时间线节点、E1～E14编号、路线完成条件、数值与完美集合不变。

## 验证

- Release构建：0警告、0错误。
- `tools/check_project.py`：中英各1,959键，无缺键或非法块引用。
- `tools/check_route_map.py`：0.89，18节点，E1～E14，通过。
- 两张运行PNG与用户确认的960图逐字节哈希一致；替换前文件备份至 `dist/rollback-cg-0.89/`。
- 尚未进行游戏中实际结局触发/显示验收，不把构建或离线预览当作实机通过；本轮不改剧情判定，未运行依赖PowerShell 7的完整剧情回归。

## 发布证据

- DLL SHA-256：`65D448E41A4A456406F7091F5BECF229662846D63AF063E5699775F09863C95C`。
- 运行包：49个文件，5,167,914字节；清单中每个文件与工作区发布源哈希一致，ZIP内容校验通过。
- ZIP：`dist/EleanorRomance-0.89.zip`；SHA-256 `07FD2D503293C238D1F0D6A8D1A98C5E6ACE4D89B8CCA4BA3A8D4ACA2CE8D2DC`。
- 暂存目录：`dist/workshop-0.89/EleanorRomance`；文件清单、校验和回执保存在同级目录。
- 工坊发布：`k_EResultOK`，原条目 `3834588749`，内容及说明同时提交，无待接受协议；完成于2026-11-11 09:49:18（北京时间），耗时6.54秒。
- 远程复核：`result=1`、`file_size=5,167,914`、`time_updated=1794361758`、公开、未封禁；正文与 `WORKSHOP_DESCRIPTION_0.89.txt` 逐字一致（7,490 UTF-8字节）。
- 发布回执：`dist/workshop-0.89/publish-result.json`；远程快照：`remote-after.json`；离线包校验：`package-validation.json`。
- GitHub：本版源代码、正式CG、母图/提示词和发布记录随本次发布提交同步至现有仓库 `imbalilang/Eleanor-Remake-Mod` 的 `main`；远程提交号核对结果保存在 `dist/workshop-0.89/github-sync.json`。未创建额外GitHub Release。
