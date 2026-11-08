# 0.71 发布记录

发布时间：2026-11-08 04:18:56（Asia/Shanghai）。原条目：[Steam Workshop 3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

按用户要求将0.71本地修复更新为正式版并发布。修复两种王尔德界面的项目提前显示，四个项目的七种付款方案按剧情节点出现，已付记录保留所选方案；分组标题改为“埃莉诺 · 筹备”，中英同步。设计与根因见`PROJECT_VISIBILITY_0.71.md`。

验证：

- Release编译0警告0错误。正式DLL与已通过28组离线回归的本地修复一致，SHA256：`1544406907AE8B3786C523042DB5CF086982DEF6158B31F911EE993C5DDED127`。
- 中英各2067键，内容与占位符检查通过。正式包原生ModScanner扫描849个方法，零拒绝、错误、能力申请、硬标记或发现项。
- 运行包15个文件，共1,564,703字节。ZIP SHA256：`7D7ED48986F6ED8133EE69E45FF5FE4B7C39201CBD6A344E830A779A148EDBC0`。
- Steam返回`k_EResultOK`，条目3834588749，无待接受协议，上传耗时7.52秒。
- 服务器`time_updated=1794082736`，大小1,564,703字节，与正式包一致；公开状态0，条目、所有者和游戏ID均核对一致，线上正文包含0.71修复说明。
- Steam客户端记录ManifestID `7066285286110773605`，随后明确记录该条目上传完成OK。

证据位于`dist/workshop-0.71/`：`release-files.json`、`runtime-regression.log`、`native-scan.json`、`publish-result.json`、`publish.log`、`remote-before.json`、`remote-after.json`。本地ZIP为`dist/EleanorRomance-0.71.zip`。工坊正文与更新日志源文件为`WORKSHOP_DESCRIPTION_0.71.txt`、`WORKSHOP_CHANGELOG_0.71.txt`。

实机验证由用户进行；未将离线回归写成Unity界面实测结论。未修改玩家存档，未提交或推送Git变更。
