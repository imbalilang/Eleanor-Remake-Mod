# 可复用美术提示词

配合[美术规范](ART_REQUIREMENTS.md)使用。先替换所有 `{{变量}}`，明确每张参考图的用途，再调用当前生图功能；不能把离线算法的公式混进绘画提示词，让生成器自行“压缩”。实际调用的完整提示词另存到任务目录。

## A. 新画像母图

```text
Use case: identity-preserve / character outfit design.
Asset: one normal high-resolution character portrait master for Eleanor Story Expand.
Image 1: authoritative identity reference for {{character}} in {{style_branch}}.
Image 2: costume reference ONLY; borrow {{garment_features}}, not its character, pose, weapons or background.
Image 3: existing project portrait framing reference.
Character: {{confirmed_identity_and_adult_age_appearance}}. Preserve {{hair_eyes_face_and_identifying_accessories}}.
Costume: {{confirmed_cut_material_colour_and_support_structure}}.
Expression and pose: {{expression}}, upright relaxed front/three-quarter pose matching the portrait reference. Preserve recognizable mature proportions and natural anatomy.
Framing: complete top of head and both shoulders down to the natural waist. No hips, thighs or full body. A single centered character, with clean margins and actual transparent background.
Medium: normal detailed game source illustration matching the identity reference's linework, face construction and shading. Preserve eyes, hair locks, fabric seams and coherent lighting. We will area-downsample it offline.
Constraints: {{must_preserve}}. No simulated pixel grid, dither, mosaic or forced limited palette. No text, UI, logos, watermark, extra character or unrequested prop.
```

原版与美化分支的 `style_branch` 和身份参考必须分别选择，不能只改一句“pixel art”作为两套画风的区别。

## B. 服装或材质局部修改

```text
Use case: identity-preserve / targeted outfit edit.
Image 1 is the EDIT TARGET. Image 2 is {{specific_supporting_reference_role}}.
Change ONLY {{specific_garment_region_or_material}} to {{desired_design}}.
Material behaviour: {{weave_or_surface_structure}}, {{highlight_and_shadow_character}}, {{folds_seams_and_fabric_tension}}.
Fit: {{desired_supported_silhouette_without_unrequested_body_enlargement}}.
Preserve exactly the same identity, face, expression, eyes, hair silhouette, jewellery, head angle, body pose, lighting, crop, character scale and position.
Keep the original transparency. Normal high-resolution illustration, not pre-pixelated. No added props, UI or text.
```

本项目黑色战术服材质示例：`fine tightly woven black technical stretch fabric; soft diffuse charcoal highlights and subtle textile translucency on thinner panels; a clean supported bodice with a higher under-bust seam; no broad rubber-like white reflections`。只有在该套服装需求中使用，不作为其他服装的通用要求。

## C. 单张表情差分

```text
Use case: identity-preserve / expression-only edit.
Image 1 is the exact neutral base for {{character_outfit_style_family}}.
Change only eyebrows, eyelids, gaze nuance, mouth and the requested cheek blush.
Expression ({{runtime_tag}}): {{specific_visible_expression}}.
LOCK the facial identity, age, iris colours, head size, face shape and angle, hair, accessories, neck, body, costume, pose, lighting, scale and canvas position.
The neutral and expression versions must look like the same drawing when toggled. Do not replace the face with another style of the same character.
One character and one expression on actual transparent background; same visible upper-body framing. Normal source illustration for later offline sampling, no expression sheet, no text, no emoji, symbols, sweat drops or extra objects.
```

| 标签 | 表情要求 |
| --- | --- |
| angry | 眉间收紧、目光坚定、嘴唇抿起；克制的不满 |
| sad | 内眉微抬、目光低落、小幅下垂嘴角；避免夸张哭脸 |
| serious | 平静专注、睁眼、嘴合拢；与愤怒区分 |
| shy | 温暖脸红、略局促的小笑；保持成年神态和原头部角度 |
| soft | 柔和眉眼、轻小闭嘴笑；亲密而从容 |
| determined | 坚定目光和略收紧的眉形；主动而有信心 |
| tired | 上眼睑稍低、放松眉形、倦意；不用外伤或夸张黑眼圈 |
| tsun | 微微撅嘴、脸红、保留傲气；不新增转头动作 |

Lydia 的 flustered / hurt / serious / smile / tease 沿用运行标签。日常美化v4已获用户定稿，日常/约会共10张新差分已完成；使用 `lydia-style-fusion-2026-11-10/approved/lydia_daily_beauty_master.png` 作为身份基底，不再参考旧的大眼卷发基础像。

Lydia 的中性表情保留略收眼睑的稳重；smile、flustered 适度睁眼，serious 稍更专注，hurt、tease 仍较克制。可加入：`Lift the upper eyelids to reveal more of the existing amber irises. Keep the eye corners, horizontal eye span and iris size unchanged; this is a change in eyelid aperture, not a new larger-eye character design. Preserve her poised adult identity.` 各表情需要单独写明睁眼程度，不能全部变成惊讶脸。固定源图到游戏画布的映射，不能按睁眼后可见的虹膜面积反向调整脸部尺度。

编辑完成后使用同族底图作面部图层装配，不能直接接受生成器重新画出的身体漂移。完整英文提示词已逐张保存在 `expressions-and-endings-2026-11-10/prompts/`（Eleanor）及 `lydia-expressions-2026-11-10/prompts/`（Lydia），是可直接核对的实际示例。

## D. 结局 CG

```text
Use case: illustration-story / identity-preserving ending CG.
Image 1: authoritative {{character_and_costume}} identity reference.
Image 2: setting reference ONLY, for {{existing_game_location}}. Do not copy its UI or other character rendering.
Story facts: {{facts_from_current_ending_text}}.
Depict one quiet concrete moment: {{action_that_pays_off_the_route}}.
Composition: {{actual_target_aspect_ratio}}, fully painted scene edge to edge. Place the character at {{position}} and reserve {{quiet_text_area}} for the existing ending overlay without rendering lettering.
Camera: {{viewpoint}}. Player appearance is {{confirmed_design_or_first_person_view}}.
Character: faithful face, hairstyle, eyes, mature proportions and exact confirmed costume. {{emotion_and_natural_hand_action}}.
Environment and lighting: {{game_setting_details_and_lighting}}.
Style: normal polished game-source illustration consistent with the project character references, coherent linework and cel shading. No fake pixel grid or dither.
Output: complete opaque full-colour scene, no UI, subtitles, logo or watermark. No unrequested wedding, palace, crowd or invented plot event.
```

CG 按整个目标画幅导出，不套画像的人物高度、透明轮廓和补顶行；实机文字区域必须复核。侍从结局实际示例见 `expressions-and-endings-2026-11-10/prompts/ending_maid.txt`。后宫 CG 的双人构图与不同期待表情编辑示例见 `harem-ending-2026-11-10/prompts/ending_harem_v1.txt`～`ending_harem_v3.txt`；使用高于脸部的镜头、人物弯腰后抬眼的明确关系，避免误画成相机从下往上仰拍。双人CG须分别指定眼睑、眉形、嘴形和情绪，避免机械复用同一浅笑。

## E. 物品图标

```text
Asset: a single readable inventory item icon for {{item}}.
Reference 1: identity and materials. Reference 2: an existing item at the intended physical scale.
Actual game footprint: {{columns}} by {{rows}} inventory cells. Source icon canvas {{width}} by {{height}}; runtime crop/scale {{actual_sprite_rule}}.
Design: {{compact_clear_silhouette_material_and_distinguishing_detail}}.
Perspective and lighting: match {{approved_neighbouring_game_items}}.
Transparent background, one item only, clean separated silhouette. No lettering or UI. Keep its apparent physical size plausible beside the scale reference.
```

不让生成器自行决定物品占位；网格形状、图标裁切与显示比例必须先查项目实际规则。

## F. 离线导出工作指令模板

```text
不重新生成或重画人物。读取已确认母图 {{master_path}}，保留原图与原始 alpha。
调用 tools/author_portrait_pipeline.py 的已验证 B/detail 实现：
采样画布={{sampling_width}}×{{sampling_height}}；Ht={{scaled_figure_height}}；Cx={{centre_x}}；Yb={{bottom_exclusive}}。
洋红抠图={{true_or_false_and_reason}}；轮廓除色={{true_or_false_and_reason}}。
按精确面积采样、0.35 覆盖率、一次 detail、透明 RGB 扩展执行；本画像仅在采样画布为288×211时补顶部1行，成品为288×212。
保存派生图和参数/哈希；预览用原生1倍、最近邻3/4倍及白/黑/#252C35背景。
不得拉伸宽高、重复补行、固定减色、增加马赛克或反复锐化。
若为表情差分，继承 {{neutral_base}} 的画布、alpha和身体，仅装配本次生图创作的面部差分，验证族内切换不抖动。
面部层须限制在该基础像人工确认的脸部内部，保留原下颌线和颈部；羽化不能越界。仅比较alpha不足以发现不透明皮肤区域内的下巴覆盖，须检查实际RGB结构边界。
输出先写到 {{art_review_output}}；本轮授权替换的资源再按实际资源名写入 Textures/Items。
```
