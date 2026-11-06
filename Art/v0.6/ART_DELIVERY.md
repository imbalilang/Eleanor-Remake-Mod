# v0.6 美术交付

项目：上层区的富家千金爱上在下层区开当铺的我：埃莉诺剧情扩展  
Workshop 标题：Eleanor Story Expand / 上层区的富家千金爱上在下层区开当铺的我：埃莉诺剧情扩展  
日期：2026-11-06

按本目录 `ART_REQUIREMENTS.md` 的方案 B 制作。四张角色资源已经分别重绘，覆盖开发目录的同图占位；未改动剧情代码、兼容逻辑、存档或已安装 MOD，也没有上传工坊。

## 交付文件

| 文件 | 规格 | 用途 |
|---|---|---|
| `portraits/er_portrait_lydia_female_vanilla.png` | 288×212 RGBA；48色 | 原版风莉迪亚 |
| `portraits/er_portrait_lydia_female_beauty.png` | 288×212 RGBA；63色 | 少女当铺风莉迪亚 |
| `portraits/er_portrait_mother_female_vanilla.png` | 288×212 RGBA；47色 | 原版风母亲 |
| `portraits/er_portrait_mother_female_beauty.png` | 288×212 RGBA；64色 | 少女当铺风母亲 |
| `items/eleanor_locket.png` | 64×64 RGBA；16色 | 封闭椭圆吊坠，连续链条，植物刻纹 |
| `items/eleanor_bracelet.png` | 64×64 RGBA；16色 | 上层区手工镯，连接的宝石镶座、修换扣环与旧划痕 |
| `workshop/preview_steam_768.png` | 768×768 PNG；859,455字节 | 小于1 MB的工坊预览；与现有开发目录 preview.png 一致 |
| `masters/workshop_cover_master.png` | 1254×1254 RGB | 封面大图，保留完整中文项目名 |

六张游戏内图片均已同步到 `EleanorRomanceDev/Textures/Items/`。文件名保留 `female`，沿用 PortraitCompat 的 `_vanilla` / `_beauty` 选择；无需改资源键。工作副本与开发目录文件哈希相同。

Eleanor 及通用 NPC 继续走现有原版／少女当铺兼容，不随资产包重新分发少女当铺的五张 Eleanor 图。莉迪亚与母亲各有自己的两版资源，既不复制通用 NPC，也不将美化版直接兼作原版。

## 设计与制作

- 母亲：红棕发、灰白发丝、低发髻、年龄纹理、橄榄色衬衫与修补过的紫褐针织外套。两版服装、年龄和身份特征一致。
- 莉迪亚：深色短发、榛色眼睛、几何耳饰、深青高领衣服与金色扣饰。原版采用较自然的小眼睛与硬朗轮廓，美化版采用少女当铺的动漫面部比例。
- 封面：Eleanor 身穿参考中的紫金衣装，处于下层区当铺柜台；人物与完整中文标题均可辨认。封面是宣传插画，不表示游戏增加了该柜台互动动画或新场景。
- 创作使用内置 **image_gen**，未调用外部 API 或 CLI 生图。全部最终提示词及初稿提示词保存在 `prompts/generation_prompts.json`。
- 每位角色的两版正式稿分别生成，不是同一张图改名。高分辨率图只作为母版，不能直接放进游戏。
- 导出使用最近邻采样、透明轮廓阈值和有限色板；不对小图做平滑缩放。`export_final_art.ps1` 是可重复的尺寸/色板导出流程，使用 Windows PowerShell 5.1 执行：`powershell.exe -NoProfile -File export_final_art.ps1`。

## 已完成的检查

- 四张角色图片均为288×212 RGBA；两张物品图片均为64×64 RGBA。
- 六张图片的 Alpha 只有0/255，无半透明抗锯齿边缘；每张不透明轮廓均为单个八邻域连通区域。
- 吊坠的链条与坠体连接；手镯的宝石与镯身连接，图案没有贴到画布边缘。
- 四张角色头顶均在 y=54，左右留白、头部和肩部在文件中完整；底边是有意的胸像截断。
- 在目标分辨率下目视检查，并与已读取的原版女上层区立绘、原版 Eleanor、少女当铺参考对照；未将高分辨率母版误当游戏成图验收。
- 两种画风的对应文件哈希不同；正式导出与开发目录的副本哈希一致。
- 工坊小图为859,455字节，低于1 MB；标题文字已目视核对。
- 详细结果：`asset_validation.json`。

## 尚未完成的实机检查

本轮未启动游戏，以下不能标为通过：在游戏真实柜台缩放下核对头像位置；分别启用/停用少女当铺确认切换；在物品格与交易桌实际显示中核对两张图标。

建议下一次程序验收分别触发莉迪亚、母亲来访并查看吊坠与手镯，记录两种环境截图；如果游戏实际裁切范围不同，再调整导出位置。当前没有宣称已通过完整实机验收。

## 参考与包装边界

人物参考：少女当铺 / Maiden’s Pawnshop，作者“码头薯条侠”，Workshop 3829696854：
https://steamcommunity.com/sharedfiles/filedetails/?id=3829696854

原版参考来自本机游戏资源的 NPC 与物品图。`references_local/` 仅供本地比对，**不放入交付 ZIP，不上传工坊**。备份与概念稿也不进入运行用资产包。

`EleanorStoryExpand_art_v06.zip` 包含六张正式 PNG（按 `Textures/Items/` 排列）、工坊 preview.png、本说明与校验记录；它是资产更新包，不是包含 DLL 的独立可运行 MOD。

## 回滚

被替换的六张占位图保存在 `backups/before_final_art_20261106_190807/`。本轮保留所有母版和原始生成文件，未删除用户的参考、剧情或原有安装包。
