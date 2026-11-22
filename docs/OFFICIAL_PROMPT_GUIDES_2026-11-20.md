# 各模型官方提示词文档调研（2026-11-20）

## 目的

这份文档从各模型官方文档中提取可执行的提示词结构与约束，用来统一本项目的 CG 提示词规划。原始抓取文本保存在 docs/official_prompt_research/。

## 已确认来源

- OpenAI Image prompting: https://platform.openai.com/docs/guides/image-prompting （200）
- OpenAI Image generation: https://platform.openai.com/docs/guides/image-generation （200）
- xAI Image Prompting Guide: https://docs.x.ai/developers/model-capabilities/images/prompting-guide （200）
- xAI Image Generation: https://docs.x.ai/developers/model-capabilities/images/generation （200）
- 腾讯混元 提交混元生图任务: https://cloud.tencent.com/document/product/1729/105969 （200）
- 腾讯混元 多轮对话生图: https://cloud.tencent.com/document/product/1729/110428 （200）
- 腾讯混元 文生图轻量版: https://cloud.tencent.com/document/product/1729/108738 （200）
- Seedream 5.5: https://seed.bytedance.com/en/seedream5_5 （200）
- OpenRouter Image Generation: https://openrouter.ai/docs/features/multimodal/image-generation （200）
- dsh-image-gen GitHub: https://github.com/shanliuling/dsh-image-gen （200）

## OpenAI：结构化 + 参考图角色

OpenAI 的 Image prompting 指南（GPT-Image-2.6 部分）给出的核心做法：

1. 先定义最终要什么图，再写主体、构图、风格和约束。
2. 复杂请求按 scene / subject / details / constraints 分段，而不是把所有要求揉成一段。
3. 编辑时明确只改 X，并列出必须保持的内容：身份、几何、布局、光线、标签。
4. 每张参考图分配一个角色，例如 Image 1 是人物身份，Image 2 是服装，Image 3 是风格，并说明如何组合。
5. 描述可见细节：材质、光线、颜色、媒介；人物要写身体取景、相对大小、视线和与物体的交互。
6. 明确文字内容，并要求无额外文字。
7. 小步迭代：每次只改一个点，重复关键保留项。
8. 官方示例使用正向可见性描述，例如 full body visible, feet included，而不是只写不要裁掉脚。

对本项目的影响：OpenAI 适合分块 + 参考图角色 + 保留项的写法；不要把所有 T0 检查都塞进提示词。

## xAI：主体第一句，风格后置

xAI 的 Image Prompting Guide 对 grok-imagine-image-2.0 的结构建议非常明确：

1. 第一句先写主体：主体是什么、处于什么状态或形态、由什么承载、在什么环境。
2. 然后写细节、数量、镜头角度、艺术方向、光线、颜色、构图。
3. 不要用情绪、胶片型号或一长串 avoid 列表开头；这样容易得到有风格但主体是替身的画面。
4. 原文：A long avoid list is easy to ignore, and it spends the prompt on objects that then show up anyway.
5. 约束只在数量或具体对象必要时保留，并和想要的东西配对写。
6. 生成用于主体不对、缺失或数量不对；编辑用于主体和布局已经正确、只改 1–3 个细节。
7. 编辑时先写改动，再用一句话写保持主体、角度、裁剪、道具位置。
8. 判断提示词是否有效，先看主体是否匹配，再看 palette、grain、styling。

对本项目的影响：Grok 的提示词应写成主体优先的短散文，风格描述放在后半段；避免长负向列表。

## 腾讯混元：Prompt 详细，NegativePrompt 独立

腾讯混元的 API 文档要点：

- Prompt：文本描述。轻量版文档明确建议详细描述画面主体、细节、场景等，文本描述越丰富，生成效果越精美。
- NegativePrompt：反向提示词，只能在一定程度上引导，不能完全杜绝。
- PromptExtension：默认开启，会自动扩写原始 prompt；官方建议开启，以提升效果和细节。
- 多轮对话生图支持每轮输入一条 Prompt，逐步调整图像内容。
- 风格通过风格编号控制；不传则使用默认或不指定风格。

对本项目的影响：腾讯适合较丰富的描述，但 PromptExtension 会自动扩写，可能引入额外细节；需要把不可改变的身份和构图写得短而明确。负向要求应放进独立 NegativePrompt，不要混在正文长列表里。

## Seedream 5.5：长描述段落，但不是像素画引擎

Seedream 5.5 官方页面没有单独的提示词公式，但示例 Prompt 都是较长的描述性段落，覆盖：

- 主体、动作、环境
- 相机或镜头、构图
- 光线、色彩、材质
- 边框、透明背景等输出要求
- 末尾直接写排除项，例如不要出现完整建筑、人物或车辆

官方强调的是真实质感、精细材质、自然光影、透明素材和多轮编辑，不是像素画风。

对本项目的影响：Seedream 适合构图、场景和摄影感描述；不能作为最终像素 CG 画风的生成器。

## OpenRouter 与 dsh-image-gen

- OpenRouter 文档主要给 Image API、模型发现、参数、参考图和示例 Prompt；没有独立提示词方法论。
- dsh-image-gen README/插件文档主要给功能、Provider、500+ Prompt 灵感库；没有系统性提示词教程。

## 统一结论

1. 先写主体和场景，再写风格；不要用 pixel art 开头。
2. 参考图必须分配角色：身份、姿势、风格、构图，各自只负责一件事。
3. 用正向可见性描述代替 T0 长清单，例如两个角色全身可见、四肢分开、脚在画面内。
4. 负向约束最多 3–4 条；能正向写的不要用负向写。
5. 姿势优先引用姿势图；文字只写镜像关系和必要差异。
6. 一次只改一个主要变量；T0 检查放在生成后，不塞进提示词。
7. 腾讯混元把 NegativePrompt 单独放；注意 PromptExtension 会自动扩写。
8. Seedream 只做构图或调度，不承担最终像素画风。
9. OpenAI 适合结构化分段和参考图角色分配；xAI 适合主体优先的短散文；Grok 不要堆 avoid 列表。

## 待办

- 将本文件结论合并进 docs/PROMPT_SPEC_V2_2026-11-20.md。
- 用新 spec 重写双人兔女郎和单人 CG 的模板。
- 用 3–5 次重复生成验证模板的可复现性；不要再用单张图反复补丁。
