# 后宫线结局 CG 候选

2026-11-10。内置 `image_gen.imagegen` 生成，当前为 **v3 睁眼、不同期待表情版**，尚未替换工作区运行占位或发布。高清母图与游戏尺寸版均已保存。

## 当前画面

Eleanor 与 Lydia 穿各自的上层区服装，在关店后的当铺并肩弯腰靠近玩家，从较低的位置抬眼看向相机。镜头略高于两人的脸；玩家保持第一人称、不绘制固定外貌。背景承接 `Content/dual_romance_v04.json` 的 dr20 聚餐及 `er_ending_harem_01`～`03` 结局中的日常来访，没有另加婚礼、宫殿或新剧情事件。

Eleanor 使用紫金衣裙、深色披肩、金颈饰与橙色卷发；Lydia 使用用户已定稿的长直微卷棕发、玫瑰珍珠发饰、奶油白与玫瑰色衣裙。身份参考及哈希保存在生成记录中。

按用户对首版的反馈，两人都更明显地睁眼。Eleanor 是带羞意、嘴唇轻启的期待，Lydia 则更明朗主动，抬眉并露齿张嘴笑；避免两人重复同一闭嘴浅笑。构图、衣装和场景沿用首版方向，属于生图编辑后的视觉保持，不声称场景像素逐一不变。

## 文件

- [当前高清母图](masters/ending_harem_master_v3.png)：1672×941 RGB。
- [游戏尺寸候选](endings/er_ending_harem_cg_v3.png)：960×540 RGBA，全不透明。
- [面部2倍预览](previews/v3/faces-2x.png)、[整图2倍预览](previews/v3/ending-harem-native-2x.png)。
- 实际提示词：`prompts/ending_harem_v1.txt`～`ending_harem_v3.txt`；调用与参考记录：`generation_job_v1.json`～`generation_job_v3.json`。
- 导出与哈希：[asset_validation.json](asset_validation.json)；旧版本母图保留在 `masters/`，不覆盖。

版本过程：v1为原构图，两人均为收眼浅笑；v2按用户反馈睁眼并改为轻启嘴唇，但两人的嘴形仍接近；v3保留Eleanor的羞怯期待，单独将Lydia改为明朗的主动喜悦。

## 导出与检查

```powershell
python tools/export_harem_cg.py --revision v3
```

使用已验证的精确面积采样与一次B/detail，整幅等比缩小至960×540，仅居中裁去生成尺寸比例误差产生的不足1源像素窄条。没有非等比拉伸、固定减色、抖动或画像补顶行。母图、960×540成图与面部整数放大均已查看；缩小后仍能辨认两人的睁眼与不同嘴形。

导出器只写本美术目录，并对所有运行PNG作哈希保护；`Textures/Items/er_ending_harem_cg.png` 仍为原占位。实际运行键与 `EndingPages` 中的 `er_ending_harem_cg` 对应，当前候选文件名带版本后缀以便审阅。实机结局画面的文字覆盖与不同屏幕比例尚未验收；`EndingPageUI` 现有铺满策略保持不变。
