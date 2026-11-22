# 1.29 发布记录（2026-11-22）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 修复莉迪亚付费闭店约会：开店前约会已排入但未显示时，改走原生到店队列，同时挂中央 `PlayerStore.OpenShutter`，并在读档/再次开店时清理过期的待处理或卡住的闭店客户。
- 恢复自我补充香水为所有成功结局的直接原生柜台奖励：单人恋爱、侍从 v2、革命军 v1/v2 补上发放；普通/朋友/后宫/旧侍从线不再经过“收下/稍后”菜单；旧存档在加载或开店时自动补发。
- 假叔叔事件警告谢礼改用原版 `EleanorWarned` 自由奖励 barter 界面和原生归属转移，移除自定义拦截、背包投递与满仓重试菜单。
- 同步本轮文案修订：共同第 20 章告白选项、革命军《空着手》《第七行》的回答、侍从线《唱快了》第一项和共同第 23 章朋友工作收尾；选项结果不变。
- 同步日/韩缺失与错位行，包括革命军 r1c1 的市井反应、侍从线慢歌回答与后宫回执领奖文案。
- 保留 1.28 治安部渠道与本地化修复、1.27 Serpico 顺延、1.26 文案收尾及更早修复；不改路线、目标日或结局。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，路线图与 manifest 均为 1.29。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.29/native-scan.log`。
- 发布包：141 个文件，20,388,101 bytes；ZIP `dist/EleanorRomance-1.29.zip`，SHA-256 `9D42B0641107857A63448A0BCF8108F0E87480B229C42036B93BC1D0E95CC92A`；DLL SHA-256 `BB0717F47D3428BCFFDD016AE0361336A42BCCDDC58F15D1000F6AAD89BD88C2`。
- 本机完整运行时回归仍受 Unity `ECall` 环境限制，未执行；本版改动了原版警告礼 barter UI 和多个结局奖励发放，建议实机走一次第 5 章警告送礼、恋爱结局、侍从 v2 终章和革命军结局。

## 发布状态

- 2026-11-22 06:00:49（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 7.54 秒；本次没有设置标题/简介/内容描述符。
- Steam Web API 复核：标题 `Eleanor Story Expand`；文件大小 20,388,101 bytes，与本地包一致；公开（visibility 0），未封禁。远程简介 4,485 bytes，SHA-256 `88FFA20908973A0AE7BF9CFB3BC5D17D1658E27B54168C41C462FF4550C6A27C`（由之前版本保留，本次未修改）。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-22T06:00:49Z`，内容句柄 `3062416773879701758`。
- 证据：`dist/workshop-1.29/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`；更新说明 `docs/WORKSHOP_CHANGELOG_1.29.txt`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
