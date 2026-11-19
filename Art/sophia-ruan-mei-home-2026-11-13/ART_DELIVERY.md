# 索菲娅常服与私服定稿

2026-11-14 的运行贴图尺寸及差分见[尺寸调整记录](../sophia-scale-2026-11-14/ART_DELIVERY.md)。本页记录原尺寸的定稿与首次接入。

2026-11-13。常服使用用户选定的阮·梅初始版，私服使用用户确认的微侧头白色针织版。私服参考旧版居家造型的横向翻领和头部倾斜，保留阮·梅版的铜红低髻、玉簪和绿眼。

[定稿同尺寸对照](previews/approved-outfits-2x.png) · [私服母图](masters/sophia_white_knit_relaxed_v2_master.png) · [私服游戏尺寸](portraits/er_portrait_mother_home_female_beauty_v2.png) · [定稿与贴图校验](approval.json)

两张 288×212 贴图已替换项目内原有的索菲娅常服和私服资源键。原有公开到店场景继续读取常服。私服有独立资源键，目前没有剧情场景调用。已核对等比导出、二值透明度、透明顶行及文件哈希。实机显示未验证。

[私服提示词](prompts/white-knit-relaxed-v2.txt) · [本轮参考图与哈希](reference-relaxed-v2.json)

## 正面圆领候选

2026-11-13。以用户选定的阮·梅初始版索菲娅为底稿，用生图功能重绘服装。保留铜红低髻、玉簪、绿眼和正视镜头的神态。白色针织参考图用于宽低圆领、翻折罗纹和修身长袖。领口与胸下的织纹和柔和阴影突出原有胸部轮廓。

[高清透明母图](masters/sophia_white_knit_master.png) · [288×212 游戏尺寸](portraits/er_portrait_mother_home_female_beauty.png) · [原生对照](previews/comparison-native.png) · [最近邻 2 倍对照](previews/comparison-2x.png)

使用原作者 B/detail 管线等比导出。以目视估读的母图眼中点 (489, 365) 和下巴 y≈591 对齐阮·梅初始版的画布坐标 (145, 115) 与 y≈137。已检查二值透明度、透明顶行与重复导出逐像素一致。原生画布可辨宽领和衣料体积，白、黑、深灰底 3 倍检查时轮廓连续。母图发冠与源图顶边接触，游戏尺寸画布上方留白。当前是未接入游戏的资源。

[实际提示词](prompts/white-knit.txt) · [参考图与哈希](reference.json) · [导出参数与校验](asset_validation.json)
