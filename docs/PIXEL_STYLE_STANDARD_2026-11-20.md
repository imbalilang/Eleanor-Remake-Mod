# 像素 CG 量化标准 v1

日期：2026-11-20

## 唯一基准

从之前通过的 5 张 CG 中选定：

love_white_sleepwear_wakeup B（白花睡服 B）

文件：
Art/character-assets-2026-11-20/cg/love_white_sleepwear_wakeup/approved/love_white_sleepwear_wakeup_cg_960_approved.png

其余四张保留为 legacy_accepted_not_standard，不再作为第二、第三套风格基准。

## 五项量化指标

统一转成 RGB，缩放到宽 960 后测量。

指标 / 基准 / 允许范围

hf_ratio / 0.0515 / 0.0386-0.0644
flat_ratio / 0.273 / 0.205-0.341
palette_5bit / 1607 / 1205-2009
edge_density / 0.1688 / 0.135-0.203
smooth_fraction / 0.622 / 0.529-0.715

## 通过规则

1. 五项指标全部在允许范围内
2. 没有全图规则棋盘格或 screen-door 抖动
3. 没有平滑动漫画风或大面积柔化渐变
4. 细密像素簇、局部不规则抖动、清晰深色描边仍需人工确认

量化指标只负责挡住明显跑偏，不能替代人工画风确认。

## 工具

python tools/check_pixel_style.py 图片路径

输出 PASS_STYLE 或 FAIL_STYLE。

机器可读配置：docs/PIXEL_STYLE_PROFILE_2026-11-20.json

## 当前五张的判定

白花睡服 B：PASS_STYLE，唯一新基准
侍从兔女郎：FAIL_STYLE，legacy 保留
Lydia 白兔床姿：FAIL_STYLE，legacy 保留
Lydia Grok 最终版：FAIL_STYLE，legacy 保留
双人兔女郎 v7：FAIL_STYLE，已撤销，仅留历史

## 结论

以后只认一个标准：白花睡服 B 的像素纹理 profile。

其余四张可以继续使用，但不再作为风格参照。
