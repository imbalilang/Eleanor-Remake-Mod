# 1.32 发布记录（2026-11-22 / 23）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 莉迪亚付费闭店约会改为原版卢式晨间特殊来访：在原生生成早晨顾客时用 `AddNextClient` 插到队首，不再由卷帘按钮补丁或额外自动叫号触发。
- 删除 `StoreShutterButton.OpenShutter` 与 `PlayerStore.OpenShutter` 的闭店补丁；展示和叫号交给正常开门流程。
- 读档清理卡住的闭店客户，次晨重新排队；已有存档不需要重开或重装。
- 保留 1.31 韩语校对、1.30 后宫 v2 香水覆盖、1.29 闭店约会/奖励/警告礼修复、1.28 治安部渠道与侍从线本地化、1.27 Serpico 顺延及更早修复。
- 不改路线、选项结果、目标日或结局。

## 发布包与运行资源

- 本次只发布代码与本地化修复；运行时 `Textures/Items` 使用 1.31 已发布版本的 119 个文件，未包含工作区中尚未审核/安装的表情候选。
- 因此 1.32 包为 141 个文件，与 1.31 的文件清单一致，没有删除或新增运行时纹理。
- 打包脚本新增可选 `-TextureFolder`，本次使用 `dist/workshop-1.31/EleanorRomance/Textures/Items`，避免把未完成的 tsun/表情试验资产带入发布包。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，路线图与 manifest 均为 1.32。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.32/native-scan.log`。
- 发布包：141 个文件，20,380,681 bytes；ZIP `dist/EleanorRomance-1.32.zip`，SHA-256 `903139751CA466657F06784B6EC1C384925C2BB4C1E653F954BC3A4D7FB6F2F6`；DLL SHA-256 `7B12CCB52B637336B77FCF8F4A7DADE3BED20B9C544DD8002A6B10CCD7C9DAF9`。
- 本机完整运行时回归仍受 Unity `ECall` 环境限制，未执行。

## 发布状态

- 2026-11-22 17:08:58（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 5.53 秒；本次没有设置标题/简介/内容描述符。
- Steam Web API 复核：标题 `Eleanor Story Expand`；文件大小 20,380,681 bytes，与本地包一致；公开（visibility 0），未封禁。远程简介 4,485 bytes，SHA-256 `88FFA20908973A0AE7BF9CFB3BC5D17D1658E27B54168C41C462FF4550C6A27C`（由之前版本保留，本次未修改）。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-22T17:08:58Z`，内容句柄 `4614269694324592370`。
- 证据：`dist/workshop-1.32/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
