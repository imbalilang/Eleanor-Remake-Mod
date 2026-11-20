# 0.76发布记录

发布时间：2026-11-09 01:49:36（Asia/Shanghai）。原条目：[Steam Workshop 3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。Steam返回成功，服务器已确认新内容与正文。

## 内容

- 第20章三种告白回应全部延续已选侍从恋爱，不再提供普通恋人/朋友转线；第28章明确旧约履行与自愿留下，保留三种回应和后续资格。
- 第12章和第19章旧伤改为修复信任的不同方式，继续调查与约定；原件保管和共享记录仍有实际事实差异，信任由后续结果兑现。
- 第14章提出能力顾虑先讨论可行办法；明确退出位于子菜单。数据盘处置与研究路线补充专用后果说明，出卖、擅自公开和高风险公开均可在最终确认前返回。
- 普通线暂缓后的朋友选项明确方向。已转普通恋人/朋友的存档保持原结果，不强制重新入线，已付安顿继续交付。
- 全部中英同步，新增5块22句及24项界面文本；原有关键对白与说明同步修订。完整实施记录见 `RELATIONSHIP_CHOICE_FIX.md`，旧版问题保留在 `RELATIONSHIP_CHOICE_AUDIT_0.75.md`。

## 正式包检查

- 内容生成及检查通过：中英各2294键，缺失0；主线181块/885行，侍从及互动213块/1059行，UI191项。
- Release编译0警告0错误。正式包DLL通过60组离线回归，SHA256：`1CFDBF06F7DDCF75E2029E509783EBF8A66FB932DE89B7F5B124BA4749879F86`。
- 新增6组覆盖三种告白、历史转线存档、交盘修复及中断重建、旧档两种和解、能力顾虑与正常退出、四种危险决定的返回/提交、三种续约的关系/分红/晚会资格；原54组按新的剧情行为同步调整并继续通过。
- 原生ModScanner扫描1051个方法，无拒绝、错误、权限申请、硬性标记或发现项。两种女仆与晚礼服PNG尺寸、Alpha、色板和包内哈希检查通过。
- 运行包16文件，1,690,924字节；ZIP SHA256：`51D0CE5D07136CEF0F06D9E4D2B79376541DA163DF79C16AD593C2D1695D7909`。
- 原版程序集SHA256仍为 `D9ABC029F6B390619C31175DF5735901AEC6DC071C35C50CE5B3FDF92E3EDE46`；未修改游戏安装文件或玩家存档。

## 创意工坊确认

- Steam返回 `k_EResultOK`，原条目3834588749，无待接受协议，上传4.03秒。
- 服务器更新时间1794160176，公开状态0、封禁0；大小1,690,924字节与正式包一致。所有者76561198109201343、游戏4348910核对一致。
- 服务器正文与 `WORKSHOP_DESCRIPTION_0.76.txt` 规范化换行后逐字一致。更新说明为 `WORKSHOP_CHANGELOG_0.76.txt`。
- ManifestID `330763494317360261`；Steam客户端日志明确记录原条目上传完成OK。

证据目录 `dist/workshop-0.76/`：`release-files.json`、`package.json`、`tested.json`、`runtime-regression.log`、`native-scan.json`、`asset-validation.json`、`publish-result.json`、`publish.log`、`remote-before.json`、`remote-after.json`、`remote-verified.json`、`workshop-upload.log`。正式归档为 `dist/EleanorRomance-0.76.zip`，0.75及更早归档保留。

实机UI、磁盘读档和节奏体验继续由用户验证。本轮未操作游戏界面、未提交或推送Git；离线回归不代替完整实机验收。
