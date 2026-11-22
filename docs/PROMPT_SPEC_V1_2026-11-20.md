# 提示词规范 v1

## 调研结论

- dsh-image-gen 插件：有功能文档和 500+ 提示词示例库，但没有系统性的提示词写法教程。
- OpenRouter：图像 API 文档给请求格式、参数和示例 Prompt，没有统一提示词方法论。
- OpenAI / xAI / 腾讯混元：官网有各自的提示词建议，但当前网络环境下部分页面无法直接抓取；先采用通用最佳实践并在后续补充原文链接。

## 本项目提示词结构

1. 参考图角色：谁提供身份、谁提供姿势、谁提供风格
2. 镜头与主体：角色主导构图，背景简单
3. 人物与服装：身份、发型、眼睛、服装
4. 姿势：优先引用姿势参考图，文字只补充镜像/动作差异
5. 画风：短风格块
6. 输出：比例、分辨率、禁止项

## 长度与注意力

- 软上限 1200 字符，硬上限 1600 字符。
- 不再把 T0 数量锁写进提示词。
- 负向约束最多 4 条。
- 一次只改一个主要变量：姿势、服装、画风三者不要同时大改。
- 模型对最后出现的约束更敏感，重要要求放前面，不要把关键指令埋在中段。

## 统一模板

Image 1 = [identity/pose/style role].
Image 2 = [role].

[Shot]: [number] young adult characters, [composition], simple background.
[Subject and outfit]: [identity, hair, eyes, costume].
[Pose]: Follow Image X pose; [only the essential mirror/difference].
[Style]: [short style block].
[Output]: 16:9 2K, no text, no watermark.

## 模型差异

- Seedream：只做构图/舞台调度，提示词 <800 字符，不承担最终画风。
- OpenAI：可承载更完整的描述，但仍保持 <1200 字符；强调保留项和变更项。
- Grok：短而直接，姿势和风格尽量用参考图，不用长负向列表。
- 腾讯混元：一次性生成，提示词 <900 字符，不连续改图；身份、姿势、画风各一条即可。

## 提示词检查

- 是否有明确的参考图角色？
- 是否只有一个主要变更？
- 是否把 T0 清单误写进提示词？
- 是否超过长度上限？
- 负向约束是否超过 4 条？
- 关键要求是否出现在前 1/3？
