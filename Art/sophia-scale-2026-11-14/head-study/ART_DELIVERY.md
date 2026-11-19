# 索菲娅头部重绘与游戏接入

2026-11-14。常服和私服已按原资源键写入项目及本机 1.03 模组目录。每套九张差分以各自定稿母图为底稿逐张生成。导出时每张图用同一套头部坐标对齐双眼和嘴部，再用 B/detail 采样合成到基础立绘上。18 张差分的透明度和表情区域外的像素与各自底图一致。已核对游戏尺寸和放大对照，通过本机安装目录的游戏原生扫描。实机显示待核对。

[常服表情对照](regenerated/previews/sophia-native.png) · [私服表情对照](regenerated/previews/sophia_home-native.png) · [表情导出校验](regenerated/asset_validation.json) · [本机同步及备份记录](regenerated/integration.json)

逐张生成的母图和提示词存于 [regenerated/masters](regenerated/masters) 和 [regenerated/prompts](regenerated/prompts)。上一轮用旧母图重合成的差分及同步记录保留在本目录供追溯。

## 深青常服

用内置生图以索菲娅深青常服定稿母图为底稿，参考埃莉诺常服的头脸比例和大块发束画法。试稿提高发顶、加宽脸颊和眼距，并把铜红发束的高光归并为较大的色块。低髻、玉簪和服装设计沿用定稿。衣服的褶皱、肩线和饰件位置也有少量重绘。

新稿使用上一版索菲娅常服的相同等比采样倍率，以目视估读的双眼中点对齐到 (145, 115)。发顶约从 y=73 提到 y=68，下巴约从 y=140 移到 y=144，发顶至下巴约增加 8–9 像素。坐标为目视估读。已检查 288×212 RGBA、透明顶行、二值透明度和重复导出。

[高清母图](masters/sophia_head_study.png) · [游戏尺寸](portraits/er_portrait_mother_head_study.png) · [同倍率对照](previews/comparison-2x.png) · [原生画布](previews/canvas-native.png)

[实际提示词](prompts/head-study.txt) · [参考与哈希](references.json) · [导出校验](asset_validation.json)

## 白色针织私服

以定稿私服为编辑底稿、常服头部试稿为脸部与画法参照。保留微侧头、露肩针织裙和低髻玉簪，重绘头部尺度和发束高光。服装织纹与褶皱亦有轻微重绘。

按常服新稿的相同采样倍率和双眼中点导出。发顶约在 y=70，下巴约在 y=143，头部高度约 73 像素。已核对 288×212 RGBA、透明顶行、二值透明度与重复导出像素一致。

[高清私服母图](masters/sophia_home_head_study.png) · [私服游戏尺寸](portraits/er_portrait_mother_home_head_study.png) · [与常服、现版私服对照](previews/home-comparison-2x.png) · [原生画布](previews/home-canvas-native.png)

[实际提示词](prompts/home-head-study.txt) · [参考与哈希](home-references.json) · [导出校验](home_asset_validation.json)
