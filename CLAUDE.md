# Project instructions

## Artwork requirements

For artwork generation, edits, expression variants or export, read `Art/ART_REQUIREMENTS.md` and the relevant templates in `Art/PROMPT_TEMPLATES.md`. Use the verified author B/detail pipeline for new portrait work; old BOX and fixed-palette notes are historical. Preserve any user-deferred artwork scope and distinguish candidates from runtime replacements.

## Route map maintenance (required)

Any change to story chapters, scene order, target days, story-impacting choices, routes, relationships, or endings must update these two files in the same change:

- `docs/route_map.html` — the visual route/ending preview.
- `docs/ROUTE_MAP_MAINTENANCE.md` — the numbering and maintenance rules.

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
