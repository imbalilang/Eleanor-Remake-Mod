# 侍从线与后宫线：同风格像素结局CG

2026-11-11。各生成1张，共2张。**用户已确认通过并要求替换为正式资源**；0.89工作区已按原资源键安装侍从/后宫两张新图，并发布到原创意工坊条目3834588749，远程包与说明复核通过。发布详情见 [RELEASE_0.89.md](../../docs/RELEASE_0.89.md)。

## 画风与场景

以原游戏结束图 `wall_36.png` 为像素媒介/配色依据，以用户已选定的革命军CG v3为同套人物线条、明暗、色块与绘制密度参考。蓝紫工业环境、珊瑚色灯光、橙发暖色焦点与局部设计纹理保持一致。原图参考仅借画法，未复制其人物或涂鸦；未查看旧侍从/后宫CG图像来决定本轮画风。

侍从线：Eleanor在下班后回到当铺，穿自愿保留的深紫女仆装与白围裙，端茶面向玩家。使用 `Art/v0.5/masters/eleanor_maid_master.png` 确认脸、服装、头饰和首饰；延续温暖而从容的关系，背景为熟悉的店铺。

后宫线：Eleanor左、Lydia右，并肩俯身靠近玩家、抬眼看向稍高的镜头。沿用旧方案已确认的**上层区日常服**：Eleanor紫金衣裙、深色披肩与金颈饰；Lydia奶油白玫瑰衣裙、网纱领口与玫瑰珍珠发饰。Eleanor带羞意、轻启嘴唇，Lydia明朗露齿笑。没有改用晚会/约会礼服。身份分别取自 `er_expr_eleanor1_soft.png` 与用户定稿 `approved/lydia_daily_beauty_master.png`，保留棕色长直微弯发型与已确认体型。

两张均为第一人称玩家视角，不绘制玩家外貌，不新增婚礼、宫殿或其他剧情事件。背景依据原店铺墙板、柜台和终端；原生参考保存在既有忽略目录，不复制到发布包。

## 文件

| 路线 | 母图 | 960×540定稿 | 640×360对照版 |
|---|---|---|---|
| 侍从 | [1672×941母图](masters/ending_maid_pixel_master_v1.png) | [游戏尺寸](endings/er_ending_maid_cg_pixel_v1.png) | [原作同尺寸](endings/er_ending_maid_cg_pixel_v1_native640.png) |
| 后宫 | [1672×941母图](masters/ending_harem_pixel_master_v1.png) | [游戏尺寸](endings/er_ending_harem_cg_pixel_v1.png) | [原作同尺寸](endings/er_ending_harem_cg_pixel_v1_native640.png) |

实际完整提示词分别为 [侍从](prompts/ending_maid_pixel_v1.txt)、[后宫](prompts/ending_harem_pixel_v1.txt)。调用为 CodexZH 技能提供的 `image.mjs edit`，模型 `gpt-image-2`，每条线1次请求、1张结果；请求1536×1024，实际两张均返回1672×941 RGB PNG。各自的 `generation_*_pixel_v1.json` 保存参考职责、尺寸、实际提示词及输入/输出SHA-256。

## 导出与验收

使用 [export_candidates.py](export_candidates.py) 复用已选革命军CG的精确矩形面积采样。从母图半开区域 `[0,0.25,1672,940.75)` 等比直接导出960和640两种尺寸，仅修正不足1源像素的画幅误差。8位RGB求均值、`floor(v+0.5)`取整；不加锐化、减色、抖动、模糊或画像补顶行。母图永久保留。参数、文件哈希和运行图保护结果见 [asset_validation.json](asset_validation.json)。

已查看两张母图、960图、640原生及其最近邻整数2倍预览：脸与眼神可读，双人表情有区别；女仆端盘、落柜台手及后宫两只支撑手未见明显多肢、融合或穿模；头饰、发饰、耳坠及主要衣物接缝得到保留。与革命军CG采用相近线条和局部纹理，但不声称与原作220色远景背景的色数、像素密度完全相同，也不声称人体经过精确测量。

侍从候选960图SHA-256：`a23dee95a7576a26532af4b0c12342b0d18c62642dd85c49149a6cb856c8cbd8`。

后宫候选960图SHA-256：`d088018f1ab7a15911031d7a8601daf11058df18f5416ebcbd5fe2e23f57b641`。

生成候选时侍从/后宫运行PNG均未改变；用户确认后已备份原图，并将两张960图逐字节安装至正式资源键。旧/新文件SHA-256与备份位置见 [installation_0.89.json](installation_0.89.json)。

0.89同时调整即时结局页为上方完整等比图片、下方结局文字，两个区域分离；正文按可用空间适度缩放，旧档GameOver图片改为保比例。实机UI仍需验收，不能以离线图或编译检查代替真实运行结果。

用户此前选定的革命军v3已另保存为正式键 `Textures/Items/er_ending_revolution_cg.png`，仅资源就绪；新R14剧情与触发仍待实施。此次没有修改结局判定或剧情。
