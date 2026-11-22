# 美术资产工作队列

日期：2026-11-19
项目版本：1.21
接管文档：`Art/ART_ASSET_HANDOVER_2026-11-19.md`

## 已完成

- [x] 运行美术盘点：132 张 PNG，16,734,084 bytes。
- [x] 1.21 运行美术登记：`docs/美术资源登记_1.21_2026-11-19.json`。
- [x] 审计器改为使用内置运行中性像，不再依赖外部 CutePortraits 路径。
- [x] 11 个表情族全部纳入中性参考映射；审计标记从 54 降为 0。
- [x] 美术接管说明：`Art/ART_ASSET_HANDOVER_2026-11-19.md`。
- [!] 皮衣画像白底自动透明化候选被否决：自动修复删掉了 750 个 alpha 像素，其中 408 个在脸部，造成脸部镂空。文件已移入 `rejected/`；工具默认关闭自动 hair-gap repair。
- [!] 纯洋红 chroma 模型 RGB 候选因头部漂移被否决；仅保留其 alpha。
- [x] 原始 RGB + chroma alpha 候选已生成，头部对齐全：`Art/pixel-art-direction-2026-11-19/leather-jacket-portrait-pipeline/transparent_master_attempt_04_original_rgb_chroma_alpha/candidates/er_portrait_eleanor_revolution_date_female_beauty_candidate.png`。
- [x] 作者确认：标准流程就是纯洋红底 + 本地色键，不要求生图模型输出真 alpha；`PIXEL_ART_METHOD.md` 已同步。

## 进行中

### P0 像素头像头发白点手修

- 状态：待手工母图修补，不使用全图白键。
- 输入：`Art/pixel-art-direction-2026-11-19/head_calibration/outputs/portrait_master_head_calibrated_v1.png`。
- 要求：只补头发内部白点/浅色缝隙；白夹克不动；alpha 不删除。
- 完成后：重新透明化，再跑 `Ht=204、Cx=144.5、Yb=211` 的 B/detail。

### P1 革命军线摩托 CG brief

- 状态：brief 已建，等待画风/构图确认。
- 文件：`docs/CG_BRIEF_revolution_bike_ride_2026-11-19.md`。
- 先确认：A 规制高清还是 P 规制像素；玩家视角是后座 POV 还是侧向跟拍。

## 11 份游戏内形象四产物台账

- 台账：`Art/character-assets-2026-11-20/README.md`
- 机器可读：`Art/character-assets-2026-11-20/character_asset_registry.json`
- 当前：A 全身详细设定稿 0/11；B 像素原图 8/11（且有软 alpha，需另存二值 alpha 档案）；C 288×212 11/11；D 表情 11/11（104 张）。
- 下一步优先补 A，再规范化 B。

## 待排期

- [x] 改为 bust-first：直接生成 1536×1152 半身洋红底母图，不再补全身。
- [x] 管线更新为 bust-first + 洋红底 + 本地色键；新增 `tools/build_bust_portrait_asset.py`，默认 `cp` profile（Ht=150）。
- [ ] 审核 cp150 候选：`Art/pixel-art-direction-2026-11-19/leather-jacket-portrait-pipeline/bust_master_attempt_05_chroma/candidates/er_portrait_eleanor_revolution_date_female_beauty_cp150.png`。
- [ ] 母图微调：cp150 外框已对齐，但眼睛比内置 cp1 高约 3px、肩宽窄约 8px；调整后再接入。
- [x] 归档规则：每次导出同时保存去洋红后的全分辨率透明母图到 `Art/pixel-art-direction-2026-11-19/transparent_portrait_archive/`，并写同名 JSON。

- [ ] 选择并写下一个 CG 格位的完整 brief。
- [ ] 统一像素新画法与正式 A/P 规制的关系。
- [ ] 完成 288×212 原生 1 倍、3–4 倍最近邻和实机检查。
- [ ] 发布前更新 manifest 版本、README/发布记录并跑包扫描。

## 纪律

- 候选图不自动写 `Textures/Items/`。
- 不覆盖母图和历史版本。
- 不自动推 GitHub、Steam Workshop 或 Nexus。

## 12 套全局工作量修正

- 现有 11 套全部是 legacy 参考，后续用新像素管线逐套重做。
- 埃莉诺皮衣是第 12 套，不是 11 套的附加变体。
- 总工作量 12 套 × 4 产物 = 48 槽位；新管线批准 0/48。
- 台账：`Art/character-assets-2026-11-20/README.md` 和 `character_asset_registry.json`。

## 四层级工作量补充

- L1 满表情（10 差分）：7 套
- L2 精简表情（6 差分）：4 套
- L3 仅画像：1 套（皮衣）
- L4 仅全身设定稿：已知 2 套（兔女郎、白色睡衣），另有私密/CG 候选池待归组
- 已知 14 套；按当前估算 49 个产物包、132 张图；L4 候选池未计入
- L5 已有设计、集成待定：1 套（`eleanor_nun` 修女服）；设计已有，B/C/D 暂缓，新增 1 个集成决策任务。
- 加入 L5 后：已知 15 套；图片工作量仍为 49 包 / 132 图，另加 1 个 L5 集成决策任务。

## 2026-11-20 新增归档

- L3：`eleanor_black_mesh_halter` 黑色短裙
- L3：`eleanor_black_sports_hoodie` 黑色运动衫（待调优）
- L3：`sophia_white_halter_knit_variant` Sophia 白色针织变体
- L4：`lydia_rose_swimsuit` Lydia 玫瑰泳装（两张 CG master）
- 更新总量：19 套 / 59 包 / 142 图 + 1 个 L5 决策任务
## CG 形象缺口审计：32 格中 1 重做 / 15 未设计 / 16 待确认；4 个缺口：white_casual、sophia_formal_teal、lydia_nightwear、revolution_massage。
审计文档：`docs/CG_CHARACTER_COVERAGE_2026-11-20.md`；总量更新为 23 套 / 63 包 / 146 图 + 1 个 L5 决策任务。
## CG 缺口修正：真实缺口仅 `lydia_sleepwear_harem`、`eleanor_sleepwear_harem`；白色私服→love_intimate L1，索菲娅递水→sophia_home L2，按摩Play→revolution_undersuit L1；总量 21 套 / 61 包 / 144 图 + 1 个 L5 决策任务。
## 后宫线睡衣 L4 候选已生成：Eleanor 黑色版 + Lydia 白色版，位于 `Art/character-assets-2026-11-20/design_sheets/`；待审。
## 后宫线睡衣 L4 v2 身体校准完成：Eleanor 34D、Lydia 34E；位于 `design_sheets/`，待审。
## Lydia 睡衣 v3（34F）已生成；Eleanor 睡衣 v2（34D）保留；待审。
## Lydia 睡衣 v4（34F，下胸肤色修正）已完成；对比图 `chest_compare_v3_v4_skin_fix.jpg`；待审。
## 后宫睡衣少女化试版：Eleanor 158/32D、Lydia 160/32F；待审。
## 少女化睡衣试版 v6：Lydia 32F 胸部已明显大于 Eleanor 32D；待审。
## 成本规则：OpenAI 已明确拒绝的同一图像/内容不再重试；Lydia 32F 回调版 v7 已完成，待审。
## Lydia 32F v8：+5% 体积、挺拔版完成；OpenAI 未重试，直接混元；待审。
## Lydia 32F v9：全新干净生成，替代模糊的 v6-v8 链；待审。
## 少女化静态数据定稿：Eleanor 158/32D、Lydia 160/32F；两套后宫睡衣 approved。B01 常服需按新锚定重做。
## L1 全身定稿：Eleanor 常服、Lydia 日常已通过；后续以这两张为换装基准。
## 胯高审计：常服基准约 56-57%，战术服约 56%，目标改为 50%；B01 暂停，先修基准。
## Eleanor 常服基准已输出 50% 胯高修正候选：`eleanor_L1_casual_fullbody_corrected_v2.png`；待审后重做 B01。
## Eleanor 胸围更新 32E/94；后宫线睡衣 32E 候选已生成，待审。
## Eleanor 身体敏感服饰候选：恋爱线白色花卉睡服、侍从线兔女郎蹲姿，32E；待审。
## Eleanor 睡服/兔女郎 v2：白睡服裸腿裸足、兔女郎连体衣+黑丝，统一站姿；待审。
## 全局清晰度门槛已加入：lap_var>=300 且 sobel_mean>=3.4；失败必须从头重画。两张睡服/兔女郎 v2 均已判定 FAIL_BLUR。
## 清晰度门槛落地：旧白睡服/兔女郎 FAIL_BLUR，已从源参考重画 fresh v3 并通过（286.8 / 277.9）。
## 侍从线兔女郎→L1 常服：OpenAI 一次成功，清晰度 PASS；后宫近裸兔女郎仍仅混元。
## 战术服 OpenAI 换装：清晰度 PASS，但胯高约 57%，比例 FAIL；暂停 OpenAI 全身换装，先锁骨盆/裆高。
## 双人兔女郎床上 CG 候选已生成（混元）：Eleanor 黑左、Lydia 白右，960×540 像素风，清晰度 PASS；待审。
## 双人兔女郎床上 CG v2：Lydia 32F 已明显大于 Eleanor 32E，像素风按 image-8ebd4ec5 重做；960 导出 PASS，待审。
## 双人兔女郎床上 CG v3：像素网格更彻底，Lydia 抬臂托胸后明显大于 Eleanor；960 导出 PASS，待审。
## 双人兔女郎床上 CG v4 对称构图：两人姿势镜像、Lydia 腿环换到另一腿；960 导出 PASS，待审。
## 双人兔女郎床上 CG v5：Lydia 姿势镜像、腿环换边；Eleanor 头发仍偏宽，建议单独做发型轮廓轮。
## 双人兔女郎床上 CG v7 最终候选：身材/对称/像素风三项通过；使用 6/20 次尝试；待审。
## 已通过：Eleanor + Lydia 双人兔女郎床上 CG（v7，960×540，approved）。
## 恋爱线白色睡衣起床 CG 候选已生成：脸部特写、白花蕾丝睡服、像素风对齐 image-8ebd4ec5；待审。
## 已通过：Eleanor 恋爱线白色睡衣起床 CG（960×540，approved）。
## 白色睡衣起床 CG v1 撤销（脸过成熟），v2 少女脸候选已生成；待审。
## 白色睡衣起床 CG v3：胸部按 32E 修正，保留少女脸/像素风；待审。
## 白色睡衣起床 CG v4：服装/胸围按白花 master，脸按原图少女风格，绿眼修正；待审。
## 白色睡衣起床 CG v5：眼色改自然灰绿、移除耳坠，头部按 QQ 参考；待审。
## 白色睡衣起床 CG 三版独立候选 A/B/C 已生成；待用户选一版。
## 已通过：Eleanor 恋爱线白色睡衣起床 CG，采用 B 版（960×540，approved）。
## 侍从线兔女郎高角度近景 CG 候选：无头、无道具、黑丝+连体衣、像素风；待审。
## 已通过：侍从线兔女郎高角度近景 CG（采用 image-0488bb01，960×540 approved）。
## L1/L2 基础画像重生成：11 套（7 L1 + 4 L2），不生成差分；候选未安装。
## Sophia L2 修正：改用学者版 `sophia_ruan-mei_master.png` / `sophia_white_knit_relaxed_v2_master.png`，非人妻版。
## Lydia 头部替换测试：保留白兔耳/眼镜/格纹服/姿态/画风，仅换头；待审。
## Lydia 后宫线白兔睡服床上姿势还原候选：前倾趴枕、低机位、平面 2D；待审。
## Eleanor 极限表情特写测试：参考心形瞳/吐舌/脸红/条纹背景，换 Eleanor 头；待审。
## Eleanor 后宫线黑兔睡服极限表情特写：长发修正，心形瞳/吐舌/脸红；待审。
## Eleanor 恋爱线白花睡服腿部姿势还原：双腿斜伸构图、白丝+白高跟；待审。
## Eleanor 白花睡服腿部姿势 v2：手扶膝/大腿位置修正；待审。
## 战术背心+黑丝参考姿势 v3：Eleanor 长发/绿眼修正；待审。
## 已通过：Lydia 后宫线白兔睡服床上姿势（image-a433992e，无胸结/无光环/简化腕套）。
## 双人兔女郎床上 CG v8 修正候选：三耳/腿部姿势/三角泳衣修正；旧 v7 已撤销，待审。
## 已记录 OpenRouter 图像通路：Seedream / OpenAI / Grok / 腾讯混元 四模型，含参数与规则。
## 模型尺度记录：Seedream < OpenAI < Grok < 腾讯混元；混元未见拒绝，但连续改图会像素/颜色漂移，最多 1 次修正，之后从源图重画。
## CG 像素颗粒审计：4 张已通过 CG 均低于 image-8ebd4ec5 基准；Grok 最平滑，白花睡服 B 最接近。
## 已建立 CG 提示词档案：4 张已通过 CG 补档，后续审批必须附完整提示词与参数。
## Grok 像素颗粒实验：640x360 原生画布 + 抖动最佳，flat_ratio 0.425→0.125，仍低于风格基准。
## 已建立唯一像素风格基准：白花睡服 B；新增五项量化指标与 check_pixel_style.py 门禁。
## 舞台独唱 CG 四模型首轮：4 张全部 FAIL_STYLE（宽舞台构图导致 flat/palette 偏出近景 profile）；OpenAI 像素边最好，Seedream 构图最好。
