# 0.68 启动修复发布记录

发布时间：2026-11-08 00:47:42（Asia/Shanghai）。原条目：[Steam Workshop 3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

用户报告0.67在模组界面出现NullReferenceException。实际Player.log调用链为：ModEntry.OnEnable → MaidProjects.Register → ModHelper.AddNetworkUpgrade → NetworkUpgrade.RegisterModUpgrade → PlayerStore.Instance → PlayerStore构造函数。原生Instance是会创建对象的getter；主菜单阶段其NewGameData依赖未就绪，不能用于就绪检查。

项目注册从OnEnable移到OnGameLoadedInit。核对安装游戏IL后，确认该事件由已有PlayerStore的LoadGame在存档反序列化前触发，因此定义仍能参与原生读档补齐。付款事务恢复和项目显示状态继续在OnGameLoadedEarly之后处理；新局沿用OnNewGameStarted回调。停用时解绑，重复启用不重复订阅。

验证结果：

- Release编译0警告0错误；中英各2019键，内容检查无缺失。
- 26组离线集成检查通过。新增夹具先复现未建立PlayerStore时直接注册的异常，再验证无存档对象时启停不访问创建getter、重复启用不重复订阅、停用解绑。另验证就绪对象的实际初始化钩子、七定义注册、重复加载、已付款状态和新游戏回调；既有剧情、交易和费用回归均通过。
- 原生ModScanner扫描838个方法，无错误、拒绝、能力申请、硬标记或发现项。
- 正式DLL SHA256：`170F01A65DBE2A0442FF6654F40D7258BF48C89D1A0B7929AC8A8F98B7626EDE`。
- 运行包15个文件，共1,549,862字节。Steam返回`k_EResultOK`，条目ID一致，无待接受协议。
- 服务器更新时间1794070062、大小1,549,862字节，公开状态保留，介绍包含0.68修复说明。客户端记录新ManifestID `3223899652108228047`，随后明确记录上传完成OK。

离线测试使用安装游戏程序集，在内存替换对象供应、渲染及I/O边界；没有操作玩家存档或完成Unity界面实机验收。用户自行实机验证。未提交或推送Git变更。

本地ZIP：`dist/EleanorRomance-0.68.zip`。发布清单、测试日志、原生扫描、Steam回调和线上核对证据：`dist/workshop-0.68/`。工坊正文及更新记录源文件：`WORKSHOP_DESCRIPTION_0.68.txt`、`WORKSHOP_CHANGELOG_0.68.txt`。
