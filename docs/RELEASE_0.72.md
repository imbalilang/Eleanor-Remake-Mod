# 0.72 发布记录

发布时间：2026-11-08 14:24:27（Asia/Shanghai）。原条目：[Steam Workshop 3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

按用户要求发布当前0.72，合并王尔德说明精简、首次安顿剧情引导，以及保留原文风的章节承接优化。中英同步；章节排期、路线、付款和奖励规则保留。内容记录见`WILDE_TEXT_0.72.md`、`CHAPTER_CONTEXT_POLISH_0.72.md`。

## 构建与检查

- Release编译0警告0错误；正式DLL与通过29组离线回归的构建一致，SHA256：`1D65CC7183318F402F413A86B4EFC074B51B1D62B1AF3486A325138AF83445F5`。
- 中英各2073键，主线181块／885行、侍从184块／936行、UI93键；内容、占位符及引用检查通过。
- 正式包原生ModScanner扫描849个方法，无拒绝、错误、能力申请、硬标记或发现项。
- 运行包15个文件，共1,566,942字节。ZIP SHA256：`C46C8043A5E09076823DEF6CA6BBCCEB5BA832F2834FDC7F80E81BE525C4CF67`。

## 发布验证

- Steam返回`k_EResultOK`，条目3834588749，无待接受协议，耗时5.03秒。
- 服务器更新时间1794119067、大小1,566,942字节，与正式包一致；公开状态0，所有者及游戏ID核对一致，正文包含0.72更新说明。
- 客户端记录新ManifestID `5838225324198756508`，随后明确记录该条目上传完成OK。

证据位于`dist/workshop-0.72/`：`release-files.json`、`tested.json`、`runtime-regression.log`、`native-scan.json`、`publish-result.json`、`publish.log`、`remote-before.json`、`remote-after.json`。本地ZIP：`dist/EleanorRomance-0.72.zip`。正文和更新日志源文件为`WORKSHOP_DESCRIPTION_0.72.txt`、`WORKSHOP_CHANGELOG_0.72.txt`。

实机验证由用户进行。本轮未操作游戏界面或玩家存档，未提交或推送Git变更。
