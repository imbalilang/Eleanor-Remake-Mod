# OpenRouter 图片本地转接层

`dsh-image-gen` 的 `openai-compat` 图生图走 OpenAI 的 `POST /images/edits`（multipart），而 OpenRouter 的图像接口只有 `POST /api/v1/images`（JSON，参考图放在 `input_references`）。本转接层补上这段协议差：

```
插件 --multipart /v1/images/edits--> 本机 relay --JSON + input_references--> OpenRouter /api/v1/images
插件 --JSON /v1/images/generations--> 本机 relay --原样转发---------------> OpenRouter /api/v1/images
```

## 启动

```powershell
node tools/openrouter_relay/server.mjs
```

监听 `127.0.0.1:18787`，只绑定回环地址。环境变量：

| 变量 | 默认 | 说明 |
|---|---|---|
| `PORT` | `18787` | 监听端口 |
| `OPENROUTER_API_KEY` | 空 | 请求没带 `Authorization` 时使用的兜底密钥 |
| `RELAY_MODEL` | `bytedance-seed/seedream-5-5` | 请求没写 model 时的兜底 |
| `RELAY_TIMEOUT_MS` | `300000` | 上游超时 |
| `OPENROUTER_IMAGES_URL` | `https://openrouter.ai/api/v1/images` | 上游端点 |

健康检查：`GET http://127.0.0.1:18787/health`。

## 接到插件

插件面板 → dsh-image-gen → 图像生成 → OpenRouter 通道，把 **Base URL** 改成：

```
http://127.0.0.1:18787/v1
```

保存后插件重启并生效。通道的 Key 不变，relay 会把插件带来的 `Authorization` 原样转给 OpenRouter；不需要在 relay 里再存一份。

> 运行中的 DSH 只在启动时读取 `cordis.patch.yml`。本仓库已经把该文件的 `baseURL` 改成 relay 地址，但**正在运行的进程不会热加载文件改动**：把 `dsh-image-gen` bundle 关闭再启用一次即可重新 compose（本次已这样做过并验证通过），或在插件面板里改一次 Base URL，或重启 DSH。

## 已验证（2026-11-19）

| 路径 | 结果 |
|---|---|
| `GET /health` | 200 |
| `POST /v1/images/edits`（multipart，单图 + 1024×1024） | 200，2048×2048，参考图生效 |
| `POST /v1/images/edits`（插件实际会带的 `response_format=url`） | 200，relay 强制返回 `b64_json`，避免 24 小时签名链接 |
| `POST /v1/images/generations`（JSON + `size:1024x1536`） | 200，1366×2048 |
| 插件 `edit_image`（配置生效后） | **200**，2048×2048，relay 日志 `edits ... refs=1 status=200` |
| 插件 `generate_image`（配置生效后） | **200**，2048×2048，relay 日志 `generations ... refs=0 status=200` |
| 插件 `edit_image`（配置生效前） | 404，确认走的是 `/images/edits` |

让运行中的插件读到新 Base URL 的做法：把 `dsh-image-gen` 这个 bundle 关闭再启用一次（`plugin_manager set_bundle`），插件会重新 compose，读到已改好的 `cordis.patch.yml`。只改文件而不重载插件不会生效。

输出对齐：本转接层把 OpenAI 的像素 `size` 转成 OpenRouter 的 `aspect_ratio`（可约分且受支持时），档位字符串保留为 `resolution`；`quality` 不是 OpenRouter 枚举值时丢弃。

## 常驻与自启

Base URL 同时影响文生图与图生图，所以 **relay 必须常驻**；relay 不在时插件两条路径都会连接失败。

- 已放好登录自启：`%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\DSH OpenRouter Relay.vbs`，隐藏窗口运行 `node .../server.mjs`。
- 手动启动：`powershell -File tools/openrouter_relay/start.ps1`，或 `node tools/openrouter_relay/server.mjs`。
- 取消自启：删除上面那个 `.vbs`。
- DSH 重启但未重新登录时，自启不会再次运行；用 `start.ps1` 手动拉起一次即可。

## 注意

- 只监听 127.0.0.1；不记录 prompt 内容，只记录 model、参考图数量、状态码和耗时。
- 回滚：`cordis.patch.yml` 的备份是 `cordis.patch.yml.bak-relay`；把 Base URL 改回 `https://openrouter.ai/api/v1`，再重载一次 `dsh-image-gen` bundle。
- 本转接层不改变 OpenRouter 的内容策略；模型拒绝仍按上游返回。
