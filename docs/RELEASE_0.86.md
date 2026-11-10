# 0.86：表情分布、价格判定与结算修正

2026-11-11。状态：**已构建、已打包、已发布到原创意工坊条目 3834588749**；实机验收待完成。

## 内容

- 平衡主线表情变化：补齐 C09/C11/C14/C16/C18/C20/C21/C22 与后日谈等可用表情素材的章节；表情标签总数为 143。C01/C07 不再明显多于其他章节。
- 修复 1×1 铭牌/赞助饰品/谢礼饰品的点击命中区：给紧凑图的透明边补 1/255 Alpha 命中底，整格可点，视觉不变；源纹理不可读时安全回退。
- 价格判定：`DealTracker` 改用谈判界面 `GetNegociatedValue()` 和 `bargainDiscount`/`bargainMarkup` 等实际特性；修复 -25% 让利被判为高价，并同步修正玩家买入方向、C05 援助阈值与 C09 Lydia 公道镯子检查。
- C03 等 SELL 客户：每次成交后刷新 `allDoneDialogue`，避免原版通用结束对白覆盖章节收尾；交易后追加的吊坠段落 `c03.b007` 现在会播放。
- 800 调查款：`RomanceState.Savings` 同步到原版 `StoreClientData.eleanorSavings`；C07/C08 每笔拾荒交易显示“调查款 X/800（还差 Y）”上方提示。
- C08“照常付款”回礼由 `scrap_metal`（废金属）改为 `common_electronic`（电子元件（普通））。
- 保留 0.85 表情/CG 美术、0.84 游戏 1.0.0.14 兼容与全部路线、数值、E1～E14 判定。

## 验证

- Release 构建：0 警告、0 错误。
- `tools/check_project.py`：通过（中英各 1956 键，无缺键、无非法块引用）。
- `tools/check_route_map.py`：通过（0.86，18 个时间线节点，E1～E14）。
- `tools/build_expressions.py`：生成 143 个表情标签。
- 原生扫描：本轮未重新执行；本机缺少 PowerShell 7，Windows PowerShell 5.1 无法加载 netstandard2.1 程序集。0.85 的原生扫描结果不自动等同于本版全部改动。
- 实机仍需验证：表情切换、1×1 饰品点击、C03 吊坠追加、C07/C08 进度提示与 C08 回礼。

## 构建与包

- DLL SHA256：`28D428C1B859C108CC32E9ABB22F925AAD5E7B6EB1B05D52B847D8BC90BACB34`。
- 运行包 48 个文件，3,774,413 字节。
- ZIP SHA256：`C4ADCFC6CF2FD174C4AFB756A50FD9BA8C198FCACACD60B1450F63D6A3EB0F71`。
- 工坊发布：`k_EResultOK`，完成时间 2026-11-11 00:25:34（北京时间）；内容与说明一并提交，48 个文件，3,774,413 字节。
- 发布回执：`dist/workshop-0.86/publish-result.json`；构建与检查日志同目录。


## Nexus Beauty Edition 包

- 新增 `tools/package_nexus.ps1`：使用 `-p:NexusBeauty=true` 单独构建 `NEXUS_BEAUTY` DLL，并把授权的 221 张 CutePortraits `cp_portrait_*` 人像一并打包。
- 运行时 Nexus 版本默认启用美化立绘：本 MOD 自带的莉迪亚/母亲/女仆/礼服直接使用 `_beauty`，埃莉诺与通用访客通过本地 `cp_portrait_*` 映射替换；缺失时回退原版。
- 输出：`dist/EleanorRomance-0.86-nexus.zip`，270 个文件、14,202,264 字节；Nexus DLL SHA256 `21A516D89C8853E563D095DA0023426CAA7F7AE6E59AF7EB12ED803F0C342631`；ZIP SHA256 `4419AD148EE6690FEA024ACD35D831B63DBCF04A9643266509E3130DCACCA065`。
- 创意工坊 0.86 包保持默认软兼容行为，未使用 Nexus DLL；Nexus 包**未上传**，且不要与 CutePortraits 或创意工坊副本同时启用。
- Nexus 包内 `NEXUS_README.txt` 已注明 CutePortraits 作者署名与授权来源。
