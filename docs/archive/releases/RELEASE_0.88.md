# 0.88：Lydia 独立形象修正

2026-11-11。状态：**已构建、已打包、已发布到原创意工坊条目 3834588749**；实机验收待完成。

> 0.87 曾把 Lydia 回退成通用 `femaleUpper10`，不符合“恋爱对象必须有独立形象”的要求。0.88 已恢复 Lydia 的独立画像并覆盖线上版本。

## 内容

- **Lydia 独立形象恢复：**
  - 无少女当铺：`er_portrait_lydia_female_vanilla`
  - 有少女当铺：`er_portrait_lydia_female_beauty`
  - 约会服同样使用 `er_portrait_lydia_date_female_vanilla` / `_beauty`
  - 只有自定义立绘文件缺失时才回退原游戏 `femaleUpper10` 防崩溃。
- 撤销 0.87 将 Lydia 回退为通用 NPC 的方案。
- 母亲：无少女当铺 → 原游戏 `femaleUpper3`；有少女当铺 → `er_portrait_mother_female_beauty`。
- 女仆装、礼服、战术背心：继续按 0.87 规则，无少女当铺时复用原游戏 Eleanor 画像（`eleanor_scav` / `eleanor1` / `eleanor_rev`），不显示自定义 `_vanilla` 服装图。
- 保留 0.87 的旧档立绘键清理、即时 E3/E4 结局页、侍从入口短提示，以及 0.86 的表情与结算修正。

## 验证

- Release 构建：0 警告、0 错误。
- `tools/check_project.py`：通过（中英各 1,959 键，无缺键、无非法块引用）。
- `tools/check_route_map.py`：通过（0.88，18 个时间线节点，E1～E14；路线、编号、完美集合未变）。
- 实机仍需验证：无少女当铺/其他美化 MOD 下 Lydia 独立形象、约会服、母亲 femaleUpper3、女仆/礼服/背心回退、旧档清理、E3/E4 即时结局页。
- 原生扫描本轮未执行；本机缺少 PowerShell 7。

## 构建与包

- DLL SHA256：`110526E2805643620230B71C9C0AFEE0D35083C0A4363DD1E3E97FFA09F5E571`。
- 运行包 48 个文件，3,783,300 字节。
- ZIP：`dist/EleanorRomance-0.88.zip`；ZIP SHA256：`2D7A824966F18A0DB3D624898B02B0F123F5290E1FF270FA700827FE2A75224A`。
- 暂存目录：`dist/workshop-0.88/EleanorRomance`。
- 工坊发布：`k_EResultOK`，内容与说明一并提交，48 个文件，3,783,300 字节；完成时间 2026-11-11 04:27:15（北京时间），耗时 4.54 秒。
- 远程复核（Steam GetPublishedFileDetails）：`result=1`，`file_size=3,783,300`，`time_updated=1794342435`（2026-11-11 04:27:15 北京时间），`visibility=0`（公开），`banned=0`，`hcontent_file=5723549524829495591`；远程正文与 `docs/WORKSHOP_DESCRIPTION_0.88.txt` 逐字一致（7,824 字节）。
- 发布回执：`dist/workshop-0.88/publish-result.json`；远程快照：`dist/workshop-0.88/remote-after.json`。
