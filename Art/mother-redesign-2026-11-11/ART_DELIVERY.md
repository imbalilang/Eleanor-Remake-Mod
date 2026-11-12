# 索菲娅上层服饰候选 v3

后续状态：2026-11-11用户已将本版与v4私服确认为定稿并要求导入。最新记录见 [FINAL_INTEGRATION.md](FINAL_INTEGRATION.md)；下文为生成时的候选交付记录。

2026-11-11。按用户最新要求，将过于破旧的 v2 衬衫/针织外套换成《少女当铺》上层女性风格。当前为美化分支候选，未替换正式资源、未部署或发布。

## 新衣装与参考

实际查看《少女当铺》13 张 `cp_portrait_femaleUpper*.png`，从 Upper10 采用绿色金边结构，从 Upper11 采用象牙色领片与金色菱格袖饰，从 Upper12 借鉴整洁正式的剪裁。最终为翡翠绿修身衣裙、象牙色立领/袖子、金边肩部披帛、小绿宝石领扣和腰饰。补缀、旧针织与褪色工作装已移除。

人物保留铜红发、绿眼、外表约28岁的成年面容、温和微笑、身前低束侧披长发及原姿势。实际年龄仍为38岁。当前按换衣指令保留 v2 身材，不将“比Lydia明显更丰满”包装为已经精确量化验收的结果。

## 文件

- 高清透明母图：`masters/mother_beauty_master_v3_upper.png`，1145×1374 RGBA。
- 游戏尺寸候选：`portraits/er_portrait_mother_sophia_beauty_upper_v3.png`，288×212 RGBA。
- 完整新衣预览：`previews/mother_beauty_master_v3_upper_review.jpg`。
- 新旧服装对照：`previews/costume_before_after_v3.jpg`。
- 原生/2倍同框：`previews/comparison_v3_upper_native.png`、`comparison_v3_upper_2x.png`。
- 白、黑、深灰底3倍检查：`previews/mattes_v3_upper_3x.png`。
- 实际提示词：`prompts/mother_beauty_v3_upper.txt`。
- 来源、参考压缩副本及SHA-256：`generation_v3.json`。
- 采样、整体映射、人工标点、输出SHA-256与检查：`framing_v3.json`、`validation_v3.json`。

## 生成与导出

通过 `codexzh-image/scripts/image.mjs` 的 `gpt-image-2` 图像编辑完成一张换装母图，使用 v2 人物压缩副本和三位上层女性服装参考拼图。本次只发送一次生成请求，完整 POST 请求体为 **1,103,346 字节**，低于20,000,000字节；含base64膨胀的保守上界为1,534,449字节。记录见 `request_receipts.jsonl`。参考原图与母图均保留。

采用已验证 B/detail 精确面积采样、35%覆盖率、一次detail及透明RGB扩展。v3沿用v2源图到游戏画布的同一等比变换 `s=0.125, tx=77.8125, ty=72.6875`，不因新衣轮廓变化而重新决定脸部尺度。在288×211采样后补一行至288×212。源图是真实alpha且含合法衣色，不启用洋红抠图或轮廓除色。

已查看高清母图、脸部坐标图、原生同框、2倍同框和3倍黑白/深灰底。人工估读的虹膜中心/下巴在新旧母图中位置相近，保留原映射；这不代表非衣物区域逐像素相同。眼部中线约y=115、下巴约y=137.4。输出alpha仅0/255，重复导出一致，正式Textures所有文件与任务初始哈希相同。

复现：`python Art/mother-redesign-2026-11-11/export_preview.py --revision v3`。

## 保留事项

候选尚待造型确认，未进行游戏内UI显示验收。腰部、手和发梢束带保留在母图中，但按当前脸部尺度导出的运行裁切不会完整显示这些部位。原版风格、居家服与表情组尚未制作。

v2旧衣装及此前失败/超时请求保留历史记录。v2最终完整原图曾因下载失败，通过小范围GET分段取回；v3本次由原生成客户端正常下载成功。
