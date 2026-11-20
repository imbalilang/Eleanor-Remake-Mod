# 1.01：后宫线饰品对所有上层顾客生效

2026-11-12。状态：**已构建、已打包、已通过 Steam 预检；尚未上传创意工坊**。

## 问题

玩家反馈第三件饰品（名媛致意银饰）对囤水者不生效。

根因：`DualShowcaseEffects.Eligible()` 只接受 `clientIntent == BUY` 且 ID 为 `upperLowerVisitor`、`upperLowerVisitorLuxury`、`ulDiscerningShopper` 的三个普通上层顾客。囤水者（`waterStockpiler`）属于 `FACTION_UPPER_LEVEL`，但使用 `SELLNBUY` 且不在白名单，因此座饰预算和银饰小费都没有进入效果系统。

## 修改

- `Eligible()` 改为：
  - `IsClientBuying()`（覆盖 `BUY` 与 `SELLNBUY`）且使用客户预算；
  - 按 `FACTION_UPPER_LEVEL` 判定，覆盖所有上层买家；
  - 继续排除其他 MOD 客户、本 MOD 剧情客户（双恋推荐客除外）、原版埃莉诺剧情 ID，以及不做购买的顾客。
- 囤水者、私人厨师、医院采购、上层女仆、私人安保等正规上层顾客现在都会获得座饰预算与银饰小费效果。
- 物品描述由“上层普通买家”改为“上层顾客”；三件饰品仍要求真实橱窗陈列，银饰每访小费上限 150。
- 仅改判定范围，不改奖励数值、路线、选项或结局；1.00 的合作场景立绘修复保留。

## 验证

- Release 构建：0 警告、0 错误。
- `tools/check_project.py`：通过（zh 2170 键、en 2228 键；无缺键、无非法块引用）。
- `tools/check_route_map.py`：通过（route map 1.01，manifest 1.01；18 个时间线节点，E1～E14）。
- `tools/test_dual_showcase.ps1` 已同步：上层顾客按阵营判定；新增 `waterStockpiler` 的 `SELLNBUY` 可生效断言，以及下层顾客被排除的断言（本轮未执行完整运行时回归）。
- Steam 发布预检（`publish_workshop.ps1 -CheckOnly`）：通过。
- 实机仍需验证：囤水者等 `SELLNBUY` 上层顾客到店后的预算与成交小费。

## 构建与包

- DLL SHA256：`B6E28B03DCC0C65C8AFC9183EBBD551E1068890B0403C7D6693167C91851F089`。
- 运行包 50 个文件，5,294,617 字节。
- ZIP：`dist/EleanorRomance-1.01.zip`；ZIP SHA256：`E186B6AF72DAFC23641DB8FFB0404B7AD61E13B64A8ED3B1CA6BF62AC3CA2A35`。
- 暂存目录：`dist/workshop-1.01/EleanorRomance`；待发布。
