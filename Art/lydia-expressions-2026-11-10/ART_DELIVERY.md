# Lydia 表情差分交付

后续修复：10张Lydia表情现与Eleanor共用基础像下颌/颈部保护，眼眉保持原像素。当前资源的补充校验见[修复记录](../expression-chin-fix-2026-11-10/ART_DELIVERY.md)。

2026-11-10。以用户已定稿的日常美化v4及其约会服v1为基础，用内置 `image_gen.imagegen` 完成5张表情母图，分别装配到两套服装，导出10张正式差分。两张新底图和十张表情已更新到工作区 `Textures/Items`；本轮未发布创意工坊，未做游戏内显示实测。

## 表情设计

| 运行标签 | 表现 |
| --- | --- |
| smile | 温暖开心、露齿小笑；上眼睑抬高，眼睛比中性像更开 |
| flustered | 惊讶与羞窘、眉毛抬起、脸红、嘴微张；睁眼变化最明显 |
| serious | 目光更专注、嘴收拢，保留稳重，区别于愤怒 |
| hurt | 内眉轻抬、低落的目光与收起的笑意，克制地受伤 |
| tease | 略收眼睑、微抬一侧眉与含蓄的笑，表现从容调侃 |

中性像略收眼睑的神态保留。部分表情通过眼睑开合变化表现情绪，保持成年大小姐的身份；没有按可见虹膜面积重新缩放人物。

## 文件与复现

- `masters/lydia_face_<标签>.png`：5张1461×1076高清透明生图母版。
- `generation_jobs.json`、`prompts/<标签>.txt`：实际调用提示词、参考图和生成结果记录。
- `portraits/er_expr_lydia_daily_<标签>.png`：日常服5张。
- `portraits/er_expr_lydia_date_<标签>.png`：约会服5张。
- `portraits/er_portrait_lydia_female_beauty.png`、`er_portrait_lydia_date_female_beauty.png`：两套对应底图，也已同步至旧美术目录的画像镜像。
- [原生尺寸全览](previews/contact-sheet-native.png)、[日常面部4倍](previews/daily-faces-4x.png)、[约会面部4倍](previews/date-faces-4x.png)。每排从左至右为中性、开心、慌乱、认真、受伤、调侃。
- [完整校验与哈希](asset_validation.json)、[安装后资源审计](audit-after/asset_inventory.json)、`before-inventory.json`：新资源及旧文件哈希；旧画像本地保存在忽略提交的 `before/` 中。

项目根目录执行：

```powershell
python tools/export_lydia_expressions.py
# 已授权的工作区更新：
python tools/export_lydia_expressions.py --install
```

默认仅导出美术目录；`--install` 同时更新工作区。生成本身使用5次内置生图编辑，重复导出不重新调用生图。日常与约会使用相同的5个新面部图层，身体、衣装和alpha继承各自底图。

## 验证

全部正式PNG均为288×212、RGBA、alpha仅0/255，保留完整RGB。使用已验证原作者B/detail采样、0.35覆盖率、一次细节增强与透明区RGB扩展；源图已有正确透明度，不做洋红抠图或轮廓除色。

所有母图继承同一固定映射：`s=0.14487300094073377, tx=38.81514581373473, ty=59.11665098777047`。仅提取人工复核的面部区域，窄遮罩过渡后装配，不接受生图重画带来的身体漂移。十张差分的alpha与各自中性像逐像素相同，面部区域外可见像素不变。原生全览与面部整数放大已查看，开心与慌乱的睁眼变化可辨认。

实际文件键沿用现有 `lydia_daily`、`lydia_date` 及5种运行标签；中性和缺失表情回退逻辑不变，无需修改剧情或程序集。`tools/check_project.py` 通过，未发现缺失中英文键或无效剧情引用。安装后全项目资源审计为41张PNG，Lydia十张表情均无轮廓差异；唯一标记项是此次未处理的后宫CG占位。

原版Lydia、Eleanor、物品与后宫CG不参与此次替换；导出器对这些资源作哈希保护。旧画像导出入口已更新为优先使用新美化定稿，独立重放导出的底图哈希与已安装文件一致，避免未来导出恢复旧脸。
