# 1.28 发布记录（2026-11-21）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 治安部保护渠道改为只看治安部声望 30：保护菜单仍需 300 信用点，但不再要求此前举报假叔叔或已有威胁记录；侍从线威胁举报选项也可直接用 30 声望受理，并在完成后写入威胁记录。
- 上层区渠道（声望 30＋数据盘＋400 信用点）与旧案渠道（曾举报假叔叔＋200 信用点）不变；条件不足仍会转入普通复查路线。
- 修复侍从线数据盘场景及 16 个受影响块的英/日/韩文对齐，补齐日/韩 26 个缺失键；旁白动作归回独立行，后续对白逐行复位。
- 斜体动作与后续对白之间加入换行；示例“撕下一页账纸递过来”不再和下一句台词相连。
- 修正共同线 `c05_n002` 的旧版英/日/韩译文，补齐日文王尔德筹备提示；英文不再残留中文行。
- 保留 1.27 Serpico 来访日顺延、1.26 文案收尾、1.25 全局修订、1.24 原生操作压价判定及更早修复；不改路线、目标日或结局。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，路线图与 manifest 均为 1.28。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.28/native-scan.log`。
- 发布包：141 个文件，20,396,127 bytes；ZIP `dist/EleanorRomance-1.28.zip`，SHA-256 `8C6FC6B30931CED7CB04B30E50982C5741408D5D33EE27B4D7F7238A41A8C692`；DLL SHA-256 `2BFC99AAB409580BCDF8713DAB6A6734872425285140722D285C92BBDDA2FC3E`。
- 本机完整运行时回归仍受 Unity `ECall` 环境限制，未执行。

## 发布状态

- 2026-11-21 15:16:20（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 10.03 秒；本次没有设置标题/简介/内容描述符。
- Steam Web API 复核：标题 `Eleanor Story Expand`；文件大小 20,396,127 bytes，与本地包一致；公开（visibility 0），未封禁。远程简介 4,485 bytes，SHA-256 `88FFA20908973A0AE7BF9CFB3BC5D17D1658E27B54168C41C462FF4550C6A27C`（由之前版本保留，本次未修改）。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-21T15:16:20Z`，内容句柄 `7801171381122368187`。
- 证据：`dist/workshop-1.28/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`；更新说明 `docs/WORKSHOP_CHANGELOG_1.28.txt`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
