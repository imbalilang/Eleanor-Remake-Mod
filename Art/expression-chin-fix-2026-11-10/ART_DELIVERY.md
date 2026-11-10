# 表情下巴覆盖修复

## 已确认原因

玩家截图对应第7章 `c07.b001` 的第3句话。第1句话设为 `soft`，其后两句继承表情，因此实际使用 `er_expr_eleanor_scav_soft.png`。

生成母图按眼睛对齐后，下颌比原拾荒立绘约低2～3个原生像素。旧导出器把替换区域画成眼部和嘴颊椭圆，嘴部椭圆下缘及羽化覆盖到了原下颌和颈部，将颈部的一部分换成生成图的肤色，形成截图中的突出块。PNG外侧透明轮廓没有改变，因为此处原本就是不透明的脖子；先前仅检查alpha和面部区域外像素不能发现这类内部结构变化。

## 修复

- 保留已生成的表情和采样对齐，不重新生成或更换画风。
- 新增 `tools/expression_face_bounds.py`，按5套实际中性像人工确认脸部下边界。面部层在该边界内装配，窄羽化仅向内，原下颌线与颈部始终来自基础像。
- 更新Eleanor、Lydia两个正式导出器，重新导出并更新工作区27张表情：Eleanor基础5、拾荒6、女仆6；Lydia日常5、约会5。
- CG、物品、基础立绘、剧情和程序集没有因本次修复而改变。

## 验证与文件

[原图、修复前、修复后对照](reported-expression-before-after-6x.png)；[拾荒装修复后全表情](scav-after-4x.png)。对照采用原生像素的整数放大，不进行平滑。修复前文件保存在本地忽略提交的 `before/`，其哈希在 `before_inventory.json`。

`tools/check_expression_jaws.py` 不使用生产遮罩来证明自身，而使用独立选取的原图下颌/颈部像素区核对实际PNG。截图表情在这些区域原有58个不同像素，修复后为0；其余表情也存在不同程度的区域覆盖，并不代表每张都出现同样明显的突出块。27张修复后均通过，表情仍有独立五官差异，原有眼睛和眉毛像素保持不变。原生全览、下颌坐标格及修复后放大对照已人工查看。

完整结果：[regression_results.json](regression_results.json)；安装后资源审计：[audit-after/asset_inventory.json](audit-after/asset_inventory.json)。非表情运行PNG的哈希与修复前相同，27张表情alpha均与各自基础像一致。项目中英文和剧情引用检查通过。尚未重新进行游戏内截图验收，未发布创意工坊或同步GitHub。

本机0.85安装目录中的17张Eleanor表情也已同步修复，替换前逐一确认与问题输入哈希一致并备份，其他安装文件保持原样。安装版Lydia仍为旧造型，不混入项目中的未发布新脸或差分。详见[本地同步记录](local_install_receipt.json)。运行中精灵有缓存，需要重启游戏后复测；此同步没有更新工坊远端内容或版本号。

复现：

```powershell
python tools/export_expression_assets.py --install
python tools/export_lydia_expressions.py --install
python tools/check_expression_jaws.py
python tools/check_project.py
```

两个导出器不传 `--install` 时只写美术候选；后宫CG候选仍由单独脚本管理，不受此次修复影响。
