# 0.74发布记录

本次合并按日累计／来访领取的合作分红、侍从关系递进修订和原版风女仆眼部微调。中英各2109键。分红节点、金额、经济口径与存档处理见`DIVIDENDS_0.74.md`；剧情和美术记录见`MAID_MOTIVATION_REWORK.md`及`Art/v0.5/eye_retouch_prompt.json`。

发布时间：2026-11-08 21:44:20（Asia/Shanghai）。原条目：[Steam Workshop 3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 构建与检查

- Release编译0警告0错误；正式包DLL通过41组离线回归，SHA256：`7D2D87556B95F9636B82B76F5AAED4F4EA91363585CDEE53AC3074E75C1BE61A`。
- 中英各2109键；主线181块／885行，侍从190块／929行，UI136键。占位符、翻译与块引用检查通过。
- 原生ModScanner扫描966个方法，无拒绝、错误、权限申请、硬标记或发现项。
- 两种女仆PNG尺寸、Alpha和色板验证通过；原版风眼部新哈希`4A1C8E92438543712F3AD07C0CCC8C037CC1B9FB77F6A7CED1F6C5296AAA80ED`，眼部之外逐像素保留。
- 运行包15文件，1,603,117字节；ZIP SHA256：`0327856373ADD8B5C7B79A056221EA19FBA27538CC2D8F6C3F9AFA72FCEDDA6F`。
- 测试所用原版程序集SHA256仍为`D9ABC029F6B390619C31175DF5735901AEC6DC071C35C50CE5B3FDF92E3EDE46`，未改动游戏安装文件。

## 工坊确认

- Steam返回`k_EResultOK`，原条目3834588749，无待接受协议；上传耗时43.6秒。
- 服务器更新时间1794145460（21:44:20+08:00），公开状态0；文件大小1,603,117字节，与正式包一致。所有者76561198109201343、游戏4348910核对一致，正文已更新0.74及分红说明。
- 客户端记录ManifestID `4132335304885044407`，随后明确记录原条目上传完成OK。

凭证保存在`dist/workshop-0.74/`：`release-files.json`、`tested.json`、`runtime-regression.log`、`native-scan.json`、`asset-validation.json`、`publish-result.json`、`publish.log`、`remote-before.json`、`remote-after.json`、`remote-verified.json`及`workshop-upload.log`。ZIP为`dist/EleanorRomance-0.74.zip`；正文和更新日志源文件为`WORKSHOP_DESCRIPTION_0.74.txt`、`WORKSHOP_CHANGELOG_0.74.txt`。

实机体验由用户验证。本轮不操作游戏界面或玩家存档，不提交或推送Git变更；离线回归不代替真实UI、磁盘读档与完整经营平衡验证。
