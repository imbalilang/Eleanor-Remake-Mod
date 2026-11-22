# QQ Reference Images as Direct Masters — 2026-11-22

## 结论

4 张 `Art/原图参考/QQ图片*.png` 已经是像素风原始母图：RGBA、alpha 仅为 0/255、无白底/洋红底问题。可以直接送 B/detail，不需要重新生成。

但它们只覆盖 4 个状态，不等于 11 张基础立绘齐全。

## 对应关系

| QQ 文件 | 内容 | 直接导出结果 |
|---|---|---|
| `QQ图片20261119194822.png` | Eleanor 默认紫披肩 | 与 `cp_portrait_eleanor1.png` 完全一致：IoU 1.0 / MAE 0 |
| `QQ图片20261119200052.png` | Eleanor 默认紫披肩·低落表情 | 与 `cp_portrait_eleanor3.png` 完全一致：IoU 1.0 / MAE 0 |
| `QQ图片20261119200041.png` | Eleanor 拾荒装 + 十字镐 | 可导出，轮廓接近但不是 workshop `cp_scav` 的逐像素复制 |
| `QQ图片20261119200057.png` | Eleanor 绿色衬衫 | 可导出，轮廓接近但不是 workshop `cp_rev` 的逐像素复制 |

## 使用方式

```powershell
python tools/build_bust_portrait_asset.py --source "Art/原图参考/QQ图片20261119194822.png" --output <out.png> --profile cp --no-magenta-key
```

这 4 张不是洋红底，必须加 `--no-magenta-key`。

## 缺口

仍需新母图的状态：`cp_portrait_eleanor2`（另一表情）、maid、gala、love intimate、revolution undersuit、Lydia daily/date、Sophia daily/home。

## 建议

- `cp1`、`cp3`：保持现状，不需要处理。
- `cp_scav`、`cp_rev`：如果接受作者 QQ 版本，可把这两张直接当母图；如果要求与 workshop 旧图逐像素兼容，则保留 workshop 原文件。
- 其余 7 个状态继续用洋红底 + 头部锚点生成。
