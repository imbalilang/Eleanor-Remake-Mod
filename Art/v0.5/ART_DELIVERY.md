# 侍从路线 v0.5 美术交付

正式资源均为288×212 RGBA，透明背景，Alpha仅0/255：

| 资源 | 环境 | 色板 |
|---|---|---|
| `Textures/Items/er_portrait_eleanor_maid_female_beauty.png` | 启用少女当铺 | 64色 |
| `Textures/Items/er_portrait_eleanor_maid_female_vanilla.png` | 未启用少女当铺 | 48色 |

两张为独立生成的正式稿，文件哈希不同。服装均使用深紫工作裙、白围裙、整齐袖口与头饰，保留埃莉诺的铜红发、绿眼和金色颈饰。原版风沿用原作的小眼睛、明确鼻梁、硬朗成年脸型和沉稳色彩。

使用内置 image_gen 分别生成母版，保存在 `masters/eleanor_maid_master.png` 与 `masters/eleanor_maid_vanilla_master.png`。美化版参考少女当铺；2026-11-08补制的原版风使用本机原作埃莉诺常服／拾荒图约束人物与画风，现有美化女仆图仅作为服装参考。游戏提取图保存在忽略目录 `references_local`，不随包再分发。正式PNG由 `export_maid.ps1` 按项目既有最近邻和有限色板流程导出。

当前工作区的关系递进修订已将完整女仆装首秀后移至第19章，第18章使用常服；后续私人场景按白名单启用，并根据少女当铺状态选择对应画风。危机、正式业务、母亲、莉迪亚、采购商、退出及丙线不使用。停用少女当铺后，当前与已排美化女仆访客切为原版女仆；再次启用只影响新建的适用来访。缺所选资源时回退常服，剧情与奖励保留。本变更按用户追加要求实施，取代冻结设计稿中“无少女当铺时仅用衣装旁白”的美术条件。

母版与288×212正式图均已目视检查；尺寸、透明度、色板、边界及哈希见 `asset_validation.json`。柜台内位置、缩放和同屏表现由用户实机验收，尚未标记通过。

重现导出：`powershell.exe -NoProfile -ExecutionPolicy Bypass -File Art/v0.5/export_maid.ps1 -Variant Vanilla`；`Beauty`只导出美化版，`All`导出两版。

最终提示词见 `generation_prompt.json`、`generation_prompt_vanilla.json`。未调用 CLI 或外部图像 API。

发布追记：下述眼部修订现已随0.74发布，正式包与工坊凭证见`docs/RELEASE_0.74.md`。末段“未升版本或上传”记录的是单独完成修图时的状态。

2026-11-08按玩家左眼反馈补做轻微眼部精修，仍使用内置image_gen，提示词与原图、目标图和哈希记录见`eye_retouch_prompt.json`。AI编辑结果保存在`masters/eleanor_maid_vanilla_eye_edit.png`，原生成母版保留；导出脚本把眼部像素簇按原人物位置配准、最近邻采样，并限制在原48色色板。正式图只有眼部两个12×7区域内的110个像素变化，区域外及Alpha与修改前逐像素一致；美化版哈希不变。原立绘备份在忽略目录`backups/eye-retouch-2026-11-08/`。未升版本或上传工坊。
