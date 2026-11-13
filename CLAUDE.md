# Project instructions

## Content rating (user requirement)

- This project is **Mature**, not all-ages. Do not describe it as “全年龄” or “all ages” in README, Workshop, Nexus, site copy, or active docs.
- Minimum compliance: **no direct R18 / explicit sexual scenes**. Mature themes, sexual tension, innuendo, and non-explicit suggestive or body description are allowed.
- Keep platform age-gating and mature-content tags accurate when publishing. Historical release notes and archives may keep their original wording.

## Request size limit (user requirement)

- Every individual POST request, for every task (including image viewing, generation and text processing), must stay below 20,000,000 bytes of serialized parameters/body. Include base64 expansion and multipart/JSON overhead when estimating; target at most 16,000,000 bytes to leave margin.
- Compress every image reference locally before upload. Preserve original masters and upload separate compressed copies; crop irrelevant transparent margins and downscale only while retaining useful reference details. Record upload sizes and hashes.
- Preflight the complete batch before sending. Split large image/text batches into separate requests; do not send an oversized request and rely on the server to reject it.

## Artwork requirements

For artwork generation, edits, expression variants or export, read `Art/ART_REQUIREMENTS.md` and the relevant templates in `Art/PROMPT_TEMPLATES.md`. Use the verified author B/detail pipeline for new portrait work; old BOX and fixed-palette notes are historical. Preserve any user-deferred artwork scope and distinguish candidates from runtime replacements.

## Route map maintenance (required)

Any change to story chapters, scene order, target days, story-impacting choices, routes, relationships, or endings must update these files in the same change:

- `docs/route_map.html` — the visual route/ending preview.
- `docs/ROUTE_MAP_MAINTENANCE.md` — the numbering and maintenance rules.
- `site/index.html` — the public GitHub Pages copy; run `tools/sync_site.ps1` after editing the route map.

Keep the existing ending numbers **E1–E14 stable**. Append new endings as E15, E16, …; never renumber or reuse an old number. Death/arrest/leaving the station are ending suffixes, not new numbers.

If the route map is not updated, the story change is incomplete. After editing, run `python tools/check_route_map.py`; it verifies the manifest version, E1–E14 coverage, required timeline nodes, and that the removed repeat-exit choices have not come back.

## Text and content source of truth

- Shipped localization is generated from the Content JSON via `tools/build_content.py`; the JSON is the real source.
- **Do not run `tools/legacy/parse_v06.py`** — it overwrites `Content/story_v06.json` from the stale text script. Kept only as an archived legacy parser.
- Run `python tools/build_content.py` and `python tools/check_project.py` after content changes.
- The dual route source is `Content/dual_romance_v04.json`; the scene logic is in `src/DualStory.cs`.

## Publishing

- Do not push to GitHub, Steam Workshop, or Nexus unless the user explicitly asks.
- Build with `bash build.sh` only when a deploy to the local game is intended; use `dotnet build` for compile-only checks.
- Keep release evidence in `dist/` and release notes in `docs/RELEASE_*.md`.
- Keep all DLL/executable rollback copies outside the entire installed mod directory (for example in workspace `dist/`). Renaming a DLL to `.bak` inside the mod still triggers the game's `payload-file` rejection. After any local deployment, run `tools/scan_package.ps1` against the actual installed mod directory, not only a staging package, before reporting success.
