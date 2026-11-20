# 0.82：弃用能力警告修复与晚会入场演出

2026-11-10。**已发布到原创意工坊条目3834588749**。Steam返回`k_EResultOK`，完成时间为北京时间2026-11-10 03:25:18，公共更新时间、文件大小和0.82说明已回查通过。

## 原因

0.81的晚会关门流程在`GalaClosing.SchedulePresentation`和`ScheduleSleep`两处调用了`StoreUIManager.Instance.StartCoroutine`。游戏把在非模组自有对象上启动的协程归为`threads`弃用能力，模组菜单因此显示“下一次游戏更新将拒绝”的警告。

本机工坊下载、正式发布包与构建DLL哈希一致，确认不是下载了其他文件。0.81发布检查只读取普通能力、发现和硬性标记，漏读了`Pending`与弃用能力字段，所以错误地将“这些字段为空”当作完整扫描通过。旧包的完整扫描可重现两项`threads`待生效发现及`UsesDeprecatedCapability=true`。

## 修复

- 两处等待改用`ModHelper.StartCoroutine(ModConstants.ModId, ...)`，由加载器管理取消；继续使用原有等待例程和0.1／4.1秒时序。
- 晚会关门邀请使用`StoreUIManager.QueueClientArriveAnimation`，沿普通顾客路径从左侧走入，到达柜台后由原生回调开始对白；移除瞬间到位与立即强制对白。
- 发布扫描现在检查所有待生效项、弃用能力及布尔标记，任一存在均失败。没有通过声明权限或掩盖警告绕过检查。
- 测试在替换Unity展示边界前，先核对实际发布DLL中的托管协程归属、正常走入调用及无提前对白；验证安装游戏的原生路径确实由到达回调衔接。
- 路线图与阅读版同步0.82演出说明。剧情节点、关系、数值和最新画像保持0.81内容。

## 验证

- 编译0警告、0错误；完整离线回归90组PASS。
- 新扫描器对未修改的0.81包返回失败，并列出两处具体调用位置，形成可复现反例。
- 修复包需要满足普通能力、发现、硬性标记、待生效项和弃用能力均为空，且弃用布尔标记为false。
- 原生动画路径已核对；实际左侧行走和柜台表现仍需游戏加载更新后查看，没有把离线检查表述为已完成实机演出验收。

证据位于`dist/workshop-0.82/`；旧包完整扫描为`old-0.81-full-scan.json`，构建与回归为`build.log`、`runtime-tests.log`。发布与公共状态核对完成后补充回执。

## 正式发布回执

- 19个文件，2,037,930字节；90组离线回归通过。
- 原生扫描：普通能力、发现、硬性标记、Pending和DeprecatedCapabilities均为空；UsesDeprecatedCapability=false。
- DLL SHA256：`8C9E4634C09E2CD885119C216A325DAEDF7D695012678CCE8FE8A14D3ECA0EB7`。
- ZIP SHA256：`252EB77339C2C0ACC57DA3DBE54A95B406532A8BF388238985837110851DE7F2`。
- Steam上传与公共状态核对：`dist/workshop-0.82/publish-result.json`、`release-verification.json`。
- 玩家需完全退出游戏，待Steam下载完成后重新启动，加载0.82并重新扫描。
