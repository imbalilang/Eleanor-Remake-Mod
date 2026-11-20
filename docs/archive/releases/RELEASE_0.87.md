# 0.87：终极立绘兼容与即时结局页

2026-11-11。状态：**已构建、已打包、已发布到原创意工坊条目 3834588749**；实机验收待完成。

> 勘误：0.87 将 Lydia 回退为通用 `femaleUpper10` 的方案已被否决。Lydia 是恋爱对象，必须有独立形象；0.88 已恢复 `er_portrait_lydia_female_vanilla` / `_beauty`。本文件保留 0.87 的发布记录。

## 内容

- **终极立绘兼容：** 未启用少女当铺时，所有新画像回退原游戏已有 ID：
  - Lydia → `femaleUpper10`
  - 母亲 → `femaleUpper3`
  - Eleanor 女仆装 → 当前场景常服 `eleanor_scav` / `eleanor1`
  - Eleanor 礼服 → `eleanor1`
  - 战术背心 → `eleanor_rev`（后续接入时通过 `PortraitCompat.ResolveOutfit`）
- 不再显示自定义 `_vanilla` 女仆/莉迪亚/母亲服装图；旧存档里的相关立绘键会在载入时清理为原游戏 ID。
- 其他美化 MOD 只替换普通原游戏立绘，不再出现“普通立绘美化、女仆装却是本 MOD 原版风”的混搭；本 MOD 只识别少女当铺（3829696854 / `cute_portraits`）。
- 少女当铺启用时仍使用本 MOD 的美化女仆、礼服、Lydia/母亲画像；美化文件缺失时回退原游戏 ID。
- **即时结局页：** E3 侍从线与 E4 后宫线在 `m28` / `dr20` 完成时立即显示独立结局页，不再等真正 GameOver；已显示过结局页的存档在 GameOver 时不重复同一 CG。页面使用原有结局文案，并增加“本 MOD 的这段剧情到此结束”提示与“返回当铺”按钮。
- **侍从入口 UI：** 移除超过 130 字的长条款提示，改用“你的回答”短提示和短选项说明；完整条件保留在 README/工坊说明。
- 保留 0.86 的表情分布、价格判定、1×1 饰品点击、C03 追加段、调查款进度与 C08 回礼修正。

## 版本对照

- 0.86：表情分布、价格判定、1×1 饰品点击、C03 追加段、800 调查款提示、C08 回礼。
- 0.87：本次立绘兼容、即时 E3/E4 结局页、侍从入口短提示。

## 验证

- Release 构建：0 警告、0 错误。
- `tools/check_project.py`：通过（中英各 1,959 键，无缺键、无非法块引用）。
- `tools/check_route_map.py`：通过（0.87，18 个时间线节点，E1～E14；路线、编号、完美集合未变）。
- 原生扫描：本轮未重新执行；本机缺少 PowerShell 7，Windows PowerShell 5.1 无法加载 netstandard2.1 程序集。0.86 的原生扫描结果不自动等同于本版全部改动。
- 实机仍需验证：无少女当铺/其他美化 MOD 下的 Lydia、母亲、女仆装、礼服、战术背心回退；旧档立绘清理；E3/E4 即时结局页与 GameOver 去重。

## 构建与包

- DLL SHA256：`07E28DCF9D85B293DC832ED1C7D029F6D0C54E38D1B99ECD35D5161F752F341C`。
- 运行包 48 个文件，3,781,480 字节。
- ZIP：`dist/EleanorRomance-0.87.zip`；ZIP SHA256：`5BE824BA277E02C5754116568D9352EA7F440A9300B4ECEC17808E048915095E`。
- 工坊发布：`k_EResultOK`，内容与说明一并提交，48 个文件，3,781,480 字节；完成时间 2026-11-11 03:43:33（北京时间），耗时 7.04 秒。
- 远程复核（Steam GetPublishedFileDetails）：`result=1`，`file_size=3,781,480`，`time_updated=1794339812`（2026-11-11 03:43:32 北京时间），`visibility=0`（公开），`banned=0`，`hcontent_file=3269287661832489111`；远程正文与 `docs/WORKSHOP_DESCRIPTION_0.87.txt` 规范化换行后逐字一致（7,716 字节）。
- 发布回执：`dist/workshop-0.87/publish-result.json`；远程快照：`dist/workshop-0.87/remote-after.json`；暂存目录：`dist/workshop-0.87/EleanorRomance`。
- 本次只发布创意工坊版本；Nexus 包未重新生成或上传。
