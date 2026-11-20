# 1.11 停水事件更新记录（2026-11-16）

本更新沿用创意工坊条目 3834588749 与 1.11 版本号，仅更新内容；作者现有工坊标题与简介保持不变。

## 变更

- 恋爱线第二幕《停水》改为索菲娅视角：她为街坊买水时被逼急的下层居民抢水推倒，受伤。
- 索菲娅作为当天队列最后一位常规客人；交易与抢水段结束后播放《夜路》CG，点继续后强制闭店并进入次日。
- 停水当天强制原生 `water_treatment_breakdown` 事件持续 1 天，保证水价、供水议价和事件客户真实生效；次日《价签》仍按水管修好写。
- 《夜路》使用正式插画 `er_event_love_water_cut_cg`：店主扶索菲娅回家，她在桌边坐下，把半杯水推给店主。
- 共同线早期 c01/c02/c05 恢复原版埃莉诺的负面特质与关键打击：监禁、荒谬、报复自信、自怜、遭贼、有罪判决、牢底坐穿、求求你；移除提前道歉。
- 同步最新表情、事件 CG、恋爱/侍从/革命军 v2 内容源与路线图第二幕说明。

## 验证

- `python tools/build_content.py` 通过。
- `python tools/check_project.py` 通过：中英无缺键、无无效块引用。
- `python tools/check_route_map.py` 通过：manifest 1.11、5 个现行结局、20 个分幕、站点副本一致。
- `dotnet build EleanorRomance.csproj -c Release`：0 警告、0 错误。
- 发布包 157 个文件、13,876,042 bytes；ZIP SHA-256：`585BB1613A36C32E634A57FEA6D238A0DAEAF8F35B2A5D66EF0CF9EDDCB452F8`。
- 原生能力扫描未能在当前 PowerShell 环境完成：同一扫描脚本对已发布的 1.10 包也报 `MissingMethodException: String.Split(Char, StringSplitOptions)`，属于扫描环境问题而非本包特有；发布前已确认 DLL/内容/本地化构建与版本一致。

## 发布状态

2026-11-16（北京时间）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 7.03 秒；未设置标题、未设置描述、未请求内容描述符。远程复核：标题仍为 `Eleanor Story Expand`，文件大小 13,876,042 bytes，与本地包一致，可见性公开，未封禁。

未执行本地 `Mods/` 部署。
