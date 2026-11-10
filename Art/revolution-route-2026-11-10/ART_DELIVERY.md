# 革命军线：贴身战术服画像 v3

当前交付版本为 **v3：黑色战术织物版**。使用内置生图功能在 v2 腰上画像上修改服装材质、颜色与胸前剪裁。v1 黑色全身方案和 v2 白色亮面腰上方案保留作历史草稿，不作为当前候选。

## 当前资源

- 高清透明母图：`masters/eleanor_revolution_undersuit_beauty_v3.png`，1460×1077，仅绘制头顶至自然腰部。
- 游戏尺寸候选：`portraits/er_portrait_eleanor_revolution_undersuit_female_beauty_v3.png`，288×212，RGBA。
- 整数二倍预览：`previews/revolution_undersuit_v3_2x.png`。
- 与《少女当铺》革命军画像的二倍对照：`previews/revolution_undersuit_v3_comparison_2x.png`。
- 实际生图提示词：`prompt_v3.txt`。
- 构图参数：`framing_v3.json`；导出校验记录：`validation_v3.json`。

## 造型与构图

在 v2 的高领、露肩、弧形侧腰开口设计上，将白色胶质亮面改为带细密织纹、轻微透感和柔和反光的黑色战术弹力布。胸前使用更高的承托弧线和收拢剪裁，减少下坠轮廓及松弛横褶，保留原有体量。半透明质感绘制在人物表面，人物实体像素仍保持不透明，避免错误透出场景背景。

保留 Eleanor 的脸部、神态、橙色长发、绿眼、饰品和革命军矩形吊坠。母图在自然腰部截断；游戏尺寸候选沿用 v2 的头顶、双眼、下巴位置与脸部尺寸，因此下方会进一步裁掉部分躯干，避免为了容纳整件衣服把脸缩小。

母图采用正常插画生成。游戏画像复用项目现有的预乘透明度 BOX 面积缩小流程，保留完整 RGB，不做限色、抖动、额外锐化或非等比拉伸；导出透明度为 0/255。已查看母图及二倍对照预览，确认完整头顶、肩部、透明背景和衣服轮廓；尺寸、透明像素和重复导出一致性检查通过。

## 复现

在项目根目录执行 `python Art/revolution-route-2026-11-10/export_portrait.py`，默认导出 v3。可追加 `--revision v2` 复现上一版，或追加 `--reference` 和本地 `cp_portrait_eleanor_rev.png` 的路径生成对照预览。外部参考仅用于对照，不复制到运行资源中。

当前为革命军线设计阶段的美术候选，尚未登记剧情解锁或替换运行时画像。

运行时兼容规则：仅在少女当铺启用时使用本战术背心美化图；未启用时不显示本图，场景直接回退到 Eleanor 原有的 `eleanor_rev` 画像。接入时通过 `PortraitCompat.ResolveOutfit(vestBeautyKey, "eleanor_rev")`，不要新增 `_vanilla` 战术背心资源。
