# OpenRouter / Grok Image Parameters — 2026-11-21

## Verified official behavior

OpenRouter's Image API supports:

- `resolution`: normalized tier, e.g. `1K`, `2K`
- `aspect_ratio`: e.g. `16:9`
- `size`: convenience width x height shorthand

Docs: `https://openrouter.ai/docs/guides/overview/multimodal/image-generation.md`

Grok endpoint capability record:

`https://openrouter.ai/api/v1/images/models/x-ai/grok-imagine-image-2.0/endpoints`

It confirms:

- `resolution`: `1K`, `2K`
- `aspect_ratio`: includes `16:9`
- `quality`: `low`, `medium`, `auto` (`high` is rejected)
- `input_references`: 0–5

## Important tool-path correction

In the local `edit_image` path with `provider=openai-compat`, `channel=openrouter`:

- `image_size` + `aspect_ratio` alone were not forwarded correctly; the request arrived as `1:1`
- `size="1792x1024"` produced `1792x1024`
- `size="2048x1152"` produced exact `2048x1152`

Therefore, for Grok through this tool path, use `size` with explicit width x height.

## Test results

- `image-cd443f9f.jpg`: 1024x1024, `PASS_STYLE`, but wrong ratio
- `image-7c7407ec.jpg`: 1792x1024, `PASS_STYLE`
- `image-7df98a14.jpg`: 2048x1152, `PASS_STYLE` 5/5
  - hf_ratio 0.0622
  - flat_ratio 0.239
  - palette_5bit 1555
  - edge_density 0.1489
  - smooth_fraction 0.616

## Conclusion

The interface can control both ratio and size. The earlier 1:1 outputs were a parameter-mapping mistake in the local tool path, not an OpenRouter/Grok limitation.
