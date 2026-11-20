# 0.77发布记录

发布时间：2026-11-09 03:58:10（Asia/Shanghai）。原条目：[Steam Workshop 3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。Steam与公开服务器均已确认更新。

## 内容

- 修复假叔叔举报、箱子陷害拘捕中的治安员归属，使用原版 `SEC`；玩家保留 `PLAYER`。
- 统一处理本MOD明确指定的旁白、来宾、采购行管事、治安部通知与新闻，现有姓名栏和记录保持一致。不新增场景，不修改真正的顾客身份、交易或其他MOD对白。
- 处理弱所有权、旧来源确认、嵌套调用、启停与异常清理，姓名栏故障不会中断对话；不修改已有历史字符串、剧情事实、付款或schema。
- 中英各2294键不变。实现与核对范围见 `DIALOGUE_SPEAKERS_0.77.md`，更新说明与正文见 `WORKSHOP_CHANGELOG_0.77.txt`、`WORKSHOP_DESCRIPTION_0.77.txt`。

## 正式包验证

- Release编译0警告0错误。正式包DLL通过66组离线回归，SHA256：`47628A0DB0A851642E94E99070A5A9C68A4C7B168DAD3C305A02EF0F7EAFA26F`。
- 原版分发与记录器重查149种访客/分支构造组合，原先69条不同文本键的身份归属均正确，未解决0。该检查不是所有世界状态的穷举或实机验证。
- 内容检查：中英各2294键，缺失0；主线181块/885行，侍从及互动213块/1059行，UI191项。
- 原生ModScanner扫描1068个方法，无拒绝、错误、权限申请、硬性标记或发现项。游戏自带TMP/UI引用不随包复制。
- 三种正式头像PNG的尺寸、Alpha、色板及包内哈希检查通过，资源本次未修改。
- 运行包16文件，1,694,750字节；ZIP SHA256：`EE2077EE719417AE99178ABBCC2E01CF4A66D5C882DFAC2591EF56067BC8957F`。
- 测试原版程序集SHA256仍为 `D9ABC029F6B390619C31175DF5735901AEC6DC071C35C50CE5B3FDF92E3EDE46`；未修改游戏安装文件或玩家存档。

## 工坊确认与源码同步

- Steam返回 `k_EResultOK`，原条目3834588749，无待接受协议，上传4.02秒。
- 服务器更新时间1794167890，公开状态0、封禁0；文件大小与正式包一致，正文与本地0.77正文规范化换行后逐字一致。
- 所有者76561198109201343、游戏4348910核对一致，ManifestID `5080503495279489552`，客户端日志明确记录上传完成OK。
- 按用户要求同步当前工作区到 `imbalilang/Eleanor-Remake-Mod` 的 `main`，包含此前0.67～0.76尚未同步的代码、文案、美术、工具和记录，以及本轮0.77修复。未来路线文档和概念图仍按各自文件标明的设计稿状态保存，不因此成为本次运行内容。
- `bin/obj/dist`构建与发布凭证、本地游戏参考图、备份均不提交；正式包以本次上传清单和SHA为准。

本地证据目录 `dist/workshop-0.77/`：`release-files.json`、`package.json`、`tested.json`、`runtime-regression.log`、`speaker-audit.json`、`native-scan.json`、`asset-validation.json`、`publish-result.json`、`publish.log`、`remote-before.json`、`remote-after.json`、`remote-verified.json`、`workshop-upload.log`。归档 `dist/EleanorRomance-0.77.zip`，旧归档保留。

实际UI、语言环境和磁盘存读档由用户验证。离线回归不代替完整实机验收。本轮未操作游戏界面。
