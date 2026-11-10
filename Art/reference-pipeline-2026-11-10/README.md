# 正常母版 → 面积缩小

> 后续校准：用户随后提供了原作者的完整 B/detail 算法，现已逐像素复现其参考成图。本目录记录的是此前 BOX 近似流程，新的验证、差异与候选见 [原作者算法验证](../author-pipeline-validation-2026-11-10/README.md)。

这两张是依据用户提供的原作者前后样图校准后，从原有正常绘画母版导出的288×212候选，也是本轮工作区正式资源的对应副本。

- `er_portrait_eleanor_gala_female_beauty.png`：美化礼服。
- `er_portrait_eleanor_maid_female_beauty.png`：美化女仆装。

采用一次预乘Alpha空间BOX面积缩小、全RGB、无调色板归并和抖动，最终Alpha为0／255。脸部大小、位置、原有造型与姿态保持；不是上一轮简化重绘母版的缩小结果。

同名JSON记录来源、导出参数和哈希。[完整管线及对照](<B:/Eleanor Story Expand/docs/画像导出管线_按作者样图校准_2026-11-10.md>)。未部署或发布。
