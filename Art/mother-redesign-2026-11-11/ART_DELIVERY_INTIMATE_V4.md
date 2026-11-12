# 索菲娅亲密针织私服 v4

后续状态：2026-11-11用户停止等待v5，将本版与v3上层装确认为定稿并要求导入。最新记录见 [FINAL_INTEGRATION.md](FINAL_INTEGRATION.md)；下文保留生成时的候选说明。

2026-11-11。按用户附件制作的独立居家服饰候选。以当前上层装 v3 为唯一人物身份基底，保留外表约28岁的成年面容、铜红侧披长发、绿眼、温和表情、现有体型和双手姿势。附件只参考紧身针织衣款式，不复制参考人物、厨房、勺子或俯视镜头。

新衣为奶油白细罗纹长袖针织，宽折露肩领、贴身腰线和柔和针织受力。衣料实体保持不透明。上层绿色衣裙、披肩、宝石领扣和正式腰饰已去掉，小金耳钉与婚戒保留。

## 交付

- 高清透明母图：`masters/mother_beauty_master_v4_intimate.png`，1145×1374 RGBA。
- 游戏尺寸候选：`portraits/er_portrait_mother_sophia_home_beauty_v4.png`，288×212 RGBA。
- 主预览：`previews/mother_beauty_master_v4_intimate_review.jpg`。
- 上层装与私服对照：`previews/costume_upper_vs_knit_v4.jpg`。
- 脸部同坐标比较：`previews/face_upper_vs_knit_v4.jpg`。
- 原生同框：`previews/comparison_v4_intimate_native.png`。
- 三种背景3倍检查：`previews/mattes_v4_intimate_3x.png`。
- 实际提示词：`prompts/mother_intimate_v4.txt`。
- 参考职责、原图/压缩副本哈希、大小与请求记录：`generation_v4_intimate.json`。
- 导出参数、来源哈希、人工标点与结果：`framing_v4_intimate.json`、`validation_v4_intimate.json`。

## 生成与检查

使用既有 `codexzh-image/scripts/image.mjs`，模型 `gpt-image-2`，每次请求 n=1。第一次连接在收到结果前终止，本地未收到图片；原参数重试一次后完整PNG正常下载。两次完整POST体均为 **1,117,237字节**，低于20,000,000字节，见 `request_receipts.jsonl`。所有上传使用另存压缩副本；原始用户附件和上层装母图哈希未变。

使用项目已验证 B/detail 导出，与上层装共用 `s=0.125, tx=77.8125, ty=72.6875`；衣服轮廓改变不影响脸部缩放。先采样288×211，再顶部补1行，最终288×212。源图是真实透明背景，不启用洋红抠图和轮廓除色。重复导出像素一致，alpha仅0/255。

已目视检查高清母图、换装前后同坐标脸部、原生同框和黑/白/深灰底3倍预览。脸、眼睛、笑意、侧披发轮廓与上层装保持视觉一致，不声称生成结果在衣服外逐像素相同；坐标为人工估读。完整母图保留束发位置、腰部与双手，当前运行尺寸裁切优先保留脸部尺度，不容纳全部下半身细节。

复现：`python Art/mother-redesign-2026-11-11/export_preview.py --revision v4_intimate`。

本次仅交付私服美术候选，上层装母图、既有正式Textures和剧情保持原状态，未部署或发布，也未进行实机UI验收。针织私服按换衣需求沿用既有身材，原先相对Lydia胸围的最终造型判断仍独立保留。
