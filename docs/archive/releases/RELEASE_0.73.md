# 0.73发布记录

发布时间：2026-11-08 18:47:05（Asia/Shanghai）。原条目：[Steam Workshop 3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

本次合并经营互动补充、三种筹备期可重复日常、母亲吊坠2×2占位。实施与验证边界见`BUSINESS_INTERACTION_0.73.md`。估价、商单、样品与补货沿用原版经营流程，告白与最终约定保留情感空间。

## 构建与检查

- Release编译0警告0错误；正式包DLL经过34组离线回归（原29组及新增5组），SHA256：`08CF4110B54A1D3A62B8054D349FCBBE4C36FAD951CF57C5BD9E04C93564A1FB`。
- 中英各2107键，主线181块／885行、侍从184块／936行、UI127键；内容、占位符及引用检查通过。新增经营文案34键。
- 原版ModScanner扫描934个方法，无拒绝、错误、能力申请、硬标记或发现项。未增加系统反射等额外授权要求。
- 运行包15个文件，共1,589,973字节。ZIP SHA256：`8386E3CD48FF0AF25A6D1BD0CBAD30D9FF4F08008281687F08ED2F903F8F11DB`。
- 测试所用原版Assembly-CSharp SHA256：`D9ABC029F6B390619C31175DF5735901AEC6DC071C35C50CE5B3FDF92E3EDE46`。夹具只在内存改写程序集，没有改动原版安装文件。

## 发布验证

- Steam返回`k_EResultOK`，条目3834588749，无待接受协议，耗时4.13秒。
- 服务器更新时间1794134825（18:47:05+08:00），大小1,589,973字节，与正式包一致；公开状态0，所有者76561198109201343、游戏4348910核对一致，正文包含0.73说明。
- Steam客户端记录新ManifestID `6795903057145110376`，随后记录原条目上传完成OK。

证据位于`dist/workshop-0.73/`：`release-files.json`、`tested.json`、`runtime-regression.log`、`native-scan.json`、`publish-result.json`、`publish.log`、`remote-before.json`、`remote-after.json`、`remote-verified.json`及`workshop-upload.log`。本地ZIP为`dist/EleanorRomance-0.73.zip`；发布正文和更新日志分别为`WORKSHOP_DESCRIPTION_0.73.txt`、`WORKSHOP_CHANGELOG_0.73.txt`。

实机验证由用户进行。本轮不操作游戏界面或玩家存档，不提交或推送Git变更。离线回归不代替完整经营平衡、实际UI和存档交互的实机体验验证。
