# 0.75发布记录

发布时间：2026-11-09 01:23:01（Asia/Shanghai）。原条目：[Steam Workshop 3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。Steam和公开服务器均已确认更新。

## 内容

- 互动补强：第19章共同估价与两类最终反馈，第25章根据实际教学经历回应；救母团聚尝试开门等待，原版事件优先，普通同章访客兜底；第一单结果两天后在已有安全来访中分享一次客户消息。
- 上层区晚会：第28章完成三天后可邀约，最多两次主动邀请。普通对白和旁白叙述赴宴、公开恋人身份与跳舞，不新增场景或跨夜逻辑，不扣款或原版外出次数。支持延期、预约错过后的确认、读段续接与下一游戏日原生新闻。
- 礼服：原版复用上层区埃莉诺；《少女当铺》使用新酒红晚礼服，停用、缺图与无效场景可回退。
- 拾荒协助：从静默永久加成改为垃圾填埋场可见随机事件，持续当天，拾荒次数+2且不会受伤；原版随机阶段70%，固定日事件优先。
- 王尔德：四项各一个现金方案，后三项按指定声望自动半价；未达标可原价办理，不消耗声望。旧人脉方案的结清历史和剧情结果保留。
- 完整中英各2248键。规则及实机清单见 `INTERACTION_AND_BALL_IMPLEMENTATION.md`、`SCAV_ASSISTANCE.md`、`WILDE_CASH_DISCOUNTS.md`；美术见 `Art/gala/ART_DELIVERY.md`。

## 构建与检查

- Release编译0警告0错误；正式发布包DLL通过54组离线回归，SHA256：`6AB6B380953536853A8BF9629AC29E311E161EAB621460602CB95E86D94C4416`。
- 中英各2248键，缺失0；主线181块/885行，侍从及新增互动208块/1037行，UI167键。占位符与块引用检查通过。
- 原生ModScanner扫描1052个方法，无拒绝、错误、权限申请、硬性标记或发现项。
- 两种女仆与新晚礼服的正式包PNG尺寸、透明度与色板通过；晚礼服SHA256：`E5E7F9F37E993D2095864C9F42D0C8B60D836DF48E8A14EE5E4DF56762B7B120`。
- 运行包16文件，1,677,809字节；ZIP SHA256：`85D1D02D149A2517F51E0D5221ABD8118E50B0C71A3C61DC8043D725F489C265`。
- 测试原版程序集SHA256：`D9ABC029F6B390619C31175DF5735901AEC6DC071C35C50CE5B3FDF92E3EDE46`；未修改游戏安装文件或玩家存档。

## 创意工坊确认

- Steam返回 `k_EResultOK`，更新原条目3834588749，无待接受协议，上传4.54秒。
- 服务器更新时间1794158581，公开状态0、封禁0；大小1,677,809字节，与正式包一致。所有者76561198109201343、游戏4348910核对一致，工坊正文与本地0.75正文规范化换行后逐字一致。
- 新ManifestID `1242371644977315003`；Steam客户端日志明确记录上传原条目完成OK。
- 更新说明：`WORKSHOP_CHANGELOG_0.75.txt`；工坊正文：`WORKSHOP_DESCRIPTION_0.75.txt`。

发布凭证位于 `dist/workshop-0.75/`：`release-files.json`、`package.json`、`tested.json`、`runtime-regression.log`、`native-scan.json`、`asset-validation.json`、`publish-result.json`、`publish.log`、`remote-before.json`、`remote-after.json`、`remote-verified.json` 和 `workshop-upload.log`。正式归档 `dist/EleanorRomance-0.75.zip`，没有覆盖旧0.74归档。

实机体验由用户验证。离线回归不代替真实柜台UI、晨间视觉顺序、磁盘读档或完整经营节奏验收。本轮不操作游戏界面，不提交或推送Git变更。
