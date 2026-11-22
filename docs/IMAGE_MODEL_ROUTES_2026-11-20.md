# OpenRouter 图像模型通路（2026-11-20）

## 官方接口

- 模型库（70 个生图模型）：`https://openrouter.ai/api/v1/images/models`
- 单模型端点：`https://openrouter.ai/api/v1/images/models/{model_id}/endpoints`
- 生图接口：`https://openrouter.ai/api/v1/images`

## 工具侧调用方式

- provider：`openai-compat`
- channel：`openrouter`
- 必须显式传 `model`，不要依赖频道默认模型

## 四个主模型

| 用途 | 模型 ID | 参考图上限 | 备注 |
|---|---|---|---|
| Seedream 高频出图 | `bytedance-seed/seedream-5-5` | 10 | 支持 1K/1.5K/2K、透明背景、PNG/JPEG，n=1 |
| OpenAI 精确编辑 | `openai/gpt-image-2.6-flare` | 16 | 支持多参考图、文字、透明背景、GIF/PNG/JPEG/WebP |
| Grok 备选构图 | `x-ai/grok-imagine-image-2.0` | 5 | 支持 1K/2K、low/medium/auto、n=1..10 |
| 腾讯混元私密/兜底 | `tencent/hy-image-v3.5-preview` | 20 | 支持 1K/1.5K/2K/4K、seed、size |

## Grok 正确参数

```json
{
  "model": "x-ai/grok-imagine-image-2.0",
  "prompt": "...",
  "resolution": "2K",
  "aspect_ratio": "16:9",
  "quality": "medium",
  "n": 1,
  "input_references": [
    {"type": "image_url", "image_url": {"url": "data:image/png;base64,..."}}
  ]
}
```

Grok 不接受显式像素 `size`，必须用 `resolution` + `aspect_ratio`。

## Seedream 正确参数

```json
{
  "model": "bytedance-seed/seedream-5-5",
  "prompt": "...",
  "resolution": "2K",
  "aspect_ratio": "16:9",
  "output_format": "png",
  "background": "auto",
  "n": 1,
  "input_references": [
    {"type": "image_url", "image_url": {"url": "data:image/png;base64,..."}}
  ]
}
```

## OpenAI 正确参数

```json
{
  "model": "openai/gpt-image-2.6-flare",
  "prompt": "...",
  "aspect_ratio": "16:9",
  "quality": "high",
  "background": "auto",
  "n": 1,
  "input_references": [
    {"type": "image_url", "image_url": {"url": "data:image/png;base64,..."}}
  ]
}
```

## 腾讯混元正确参数

```json
{
  "model": "tencent/hy-image-v3.5-preview",
  "prompt": "...",
  "resolution": "2K",
  "aspect_ratio": "16:9",
  "n": 1,
  "input_references": [
    {"type": "image_url", "image_url": {"url": "data:image/png;base64,..."}}
  ]
}
```

## 规则

1. 每次调用必须显式指定模型 ID。
2. 16:9 资产统一用 `aspect_ratio: "16:9"` + `resolution: "2K"`（Grok/Seedream/混元）。
3. Grok 禁止显式像素尺寸。
4. 同一内容被某模型明确拒绝后，不重复重试该模型；切换到腾讯混元或其他可用模型。
5. 参考图通过 `input_references` 传入，支持 HTTP(S) 或 base64 data URL。
6. 机器可读配置：`docs/image_model_routes_2026-11-20.json`。

## 内容尺度排序

从最严格到最宽松：

1. Seedream
2. OpenAI
3. Grok
4. 腾讯混元

腾讯混元目前没有观察到明确拒绝请求的情况，但这只是当前观察，不视为永久保证。

## 腾讯混元漂移规则

腾讯混元的主要问题不是拒绝，而是经过一遍遍改图后会出现明显的像素和颜色漂移。

因此：

1. 优先从原始源图重新生成，不连续编辑旧输出。
2. 同一张腾讯混元结果最多允许 1 次修正。
3. 如果还需要第二次修正，必须从干净的原始母图重新生成，或切换模型。
4. 不把上一版腾讯混元输出当作下一版的源图。
5. 出现明显色偏、像素密度变化或调色板漂移时，判定为漂移，直接重画，不继续局部修补。


## Grok large-CG routing (2026-11-21 update)

- OpenAI-rejected large CGs use Grok Imagine Image 2.0 for content-master restyle.
- Grok must receive explicit size="WIDTHxHEIGHT"; image_size + aspect_ratio may not work through the local tool path.
- See docs/GROK_LARGE_CG_ROUTING_2026-11-21.md.


## Grok large-CG 4/5 acceptance (2026-11-21)

- Grok large CGs can be accepted at 4/5 under the same one-metric / 40%-deviation rule used for Hunyuan.
- Policy: docs/CG_STYLE_ACCEPTANCE_GROK_2026-11-21.md.
- Applied: image-f0c62f02.jpg -> harem_two_bunny_bed_grok_4of5, approved.


- Grok style restyle: one content master + exactly one style anchor; multiple style references are forbidden (content pollution).
