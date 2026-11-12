【本次任务】

路线：{route}

路线大纲：
{route_outline}

路线计划（JSON）：
{route_plan}

当前状态：
{state}

世界书微上下文：
{world_micro}

补充要求：
{extra_instruction}

【输出要求】

只输出一个合法 JSON 对象。不要输出 Markdown 代码块、解释或前言。

- 一次生成整条路线：1—2 章，全路线 24—40 个 block，约 2800—3500 中文字符；
- 每章 12—20 个 block，约 1400—1800 字；
- 每个 block 2—5 行，每行 10—30 字；不要用 1 行 block 凑数；
- 每个 block 使用路线计划里的 block ID；
- 玩家短选择 2—4 个，标签 2—8 字；每 2—3 个 block 至少给玩家一次输入/选择；
- NPC 连续独白不超过 3 行；每 3—5 行给玩家一次输入；
- 写不到目标字数就扩写对话/动作/事件，不要提前结束，不要用旁白/总结补字数；
- 每章至少 2—3 个具体事件；短动作/场景节拍 2—5 条/场；
- 结尾落在具体动作、交易结果、一句硬话或沉默；
- 不要写小说段落、作者分析、状态面板或数值说明。

JSON schema（示例 ID 只作格式参考；实际使用路线计划里的 chapter/block ID）：

{
  "route": "normal",
  "title": "回家",
  "summary": "一句话总结整条路线",
  "chapters": [
    {
      "chapter": "nl01",
      "title": "母亲搬来了",
      "summary": "一句话总结本章",
      "pleasure_beats": ["本章具体爽点1", "本章具体爽点2"],
      "blocks": [
        {
          "id": "nl01_b001",
          "scene": "场景名",
          "label": "",
          "lines": [
            {"speaker": "eleanor", "text": "台词"}
          ],
          "actions": ["短动作/场景备注"],
          "choices": [
            {"label": "短标签", "next": "nl01_b002", "note": "选后回应"}
          ],
          "state": {}
        }
      ]
    }
  ]
}
