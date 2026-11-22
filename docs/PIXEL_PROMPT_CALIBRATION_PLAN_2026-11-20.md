# 四模型像素画风提示词校准计划

日期：2026-11-20

## 目标

为 Seedream、OpenAI、Grok、腾讯混元各找到一条相对稳定的提示词配方，使生成结果通过：

- docs/PIXEL_STYLE_PROFILE_2026-11-20.json
- tools/check_pixel_style.py

参考图角色固定为：

1. 内容/姿势参考
2. 风格锚点：白花睡服 B
3. 角色身份参考

不再使用其他 legacy CG 作为风格基准。

## 统一风格块

Pixel-art CG rendering. Use dense 1-3 px pixel clusters to build every form. Use irregular, localized clustered dithering only in tonal transitions; never a repeating checkerboard or screen-door pattern. Use hard-edged color steps, crisp dark 1 px outlines, and rich pixel-level detail in hair, skin, fabric and background. Do not use anti-aliasing, soft airbrush, blur, smooth gradients, 3D rendering or flat anime shading.

负向块：

No repeating checkerboard, no screen-door dithering, no smooth anime, no 3D render, no anti-aliasing, no soft airbrush, no blur, no large unbroken flat fills, no photographic gradients.

## 三个变体

V1 保守：只描述像素画风，尽量保持参考图。

V2 平衡：加入低分辨率原生画布、2-4 px 像素簇、局部抖动、禁止后处理平滑。

V3 严格：640x360 原生画布、1-3 px 像素簇、硬色阶、局部不规则抖动、明确禁止棋盘格和平滑。

## 模型适配

Seedream：强调 2K 放大后不得平滑、不得降噪、保持硬像素边。

OpenAI：强调严格照做、不得重新解释成精致动漫或 3D、不得添加小于一个像素块的细节。

Grok：强调原生低分辨率画布 + nearest-neighbor 放大，不允许重新画成平滑动漫。

腾讯混元：强调一次性生成、不连续改图、保持参考色板、避免默认平滑质感。

## 测试矩阵

每个模型跑 V1、V2、V3 三张，共 12 张。

固定：

- 2K
- 16:9
- 相同内容参考
- 相同风格锚点
- 相同身份参考

## 验收

单张通过：

- check_pixel_style.py 输出 PASS_STYLE
- 人工确认没有规则棋盘格

单个模型稳定通过：

- 至少 2/3 张 PASS_STYLE
- 三张的中位 fail_distance 不超过 0.10

## 执行顺序

第一轮先跑 Seedream 和 Grok 各 3 张，共 6 张。

原因：

- Seedream 通常更服从画面描述
- Grok 已经被观察到容易把画面画平滑
- 这两个模型先拉开差距，能最快看出提示词哪一段有效

第二轮再把胜出的风格块套到 OpenAI 和腾讯混元。

## 产出

- prompts/pixel_style_calibration_2026-11-20.json
- 每个模型最终保留一条 winning prompt
- 每个模型的 3 张测试图、指标和 fail_distance
- 一份模型提示词对照报告
