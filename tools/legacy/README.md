# 旧版工具（不要运行）

这些脚本属于已停用的内容管线。当前文本源是 `Content/*.json`，生成入口是 `tools/build_content.py`。

- `parse_v06.py`：会从旧台本覆盖 `Content/story_v06.json`，禁止运行。
- `gen_localization.py`、`gen_script_doc.py`、`add_v04_localization.py`：旧本地化/台本生成链。
- `import_maid_v05.py`：按旧阅读稿位置生成侍从块 ID，重跑会使块 ID 位移。
- `package_candidate.ps1`：旧候选包脚本，默认版本仍为 0.67。

如需追溯旧流程，请先阅读对应的历史归档文档，再决定是否恢复；不要直接执行。
