# Project instructions

## Content-rating labels (user requirement)

- Never add content-rating information or labels on your own — including Mature, all-ages, R18, adult, age-gating, age ratings, 成熟向, 全年龄, 年龄分级 or 年龄提示 — in README, manifest, Workshop/Nexus copy, site, docs, release notes, change logs, UI or configuration.
- Remove such labels when touching existing files. Do not infer or declare the project's rating or target audience; describe only gameplay/story facts the user explicitly asked for.

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

The current endings are **E1 romance, E2 friends, E3 attendant, E4 harem, E9 revolution**. The author states that opening the nutrifruit research is flavor/business content: it does not create a different relationship ending or a perfect/non-perfect rank. E5/E6/E7/E8 are historical duplicates merged into E1/E2/E3/E4 respectively; keep those old numbers reserved as aliases, not separate endings. E10–E14 remain archived for legacy saves. Append actual new endings as E15, E16, …; never renumber or reuse a reserved number. Death/arrest/leaving the station are ending suffixes, not new numbers. Preserve existing business flags and conditional background lines; do not collapse save Route values merely to merge ending labels.

If the route map is not updated, the story change is incomplete. After editing, run `python tools/check_route_map.py`; it verifies the manifest version, five current endings and historical aliases, act and scene coverage, guide links, site synchronization, and that duplicate research endings and retired nodes have not come back.

## Route terminology (user requirement)

- The only official routes are **恋爱线、后宫线、革命军线、侍从线**, plus the common-line sections.
- **Never use 甲线/乙线/丙线/丁线, A/B/C/D route labels, or the old 甲乙丙丁 world-route classification** in new docs, UI, prompts, comments, tests, or route-map copy. Refer to the five current endings above. Describe research handling as business/background flavor, not an ending classification; immediate publication that kills the mother still changes to the revolution route.
- Historical release notes and `docs/archive/` may retain old wording. Active docs, Content, Localization, route map and site copy must be migrated when touched.

## Text and content source of truth

## Shop space (user requirement, 2026-11-15)

- The player operates a sales kiosk. Regular trades and dialogue events occur through its sales window; the player stays inside and all visitors stay outside.
- The rolling shutter covers the sales window. It is not a hinged customer entrance. Do not give it door handles, hinges, door boards, a latch, or a cord tied to keep a door shut.
- A side door exists but cannot stage dialogue or a complete visit. Do not route a visitor through it to justify an indoor interaction.
- Visitors point to stock; the player brings it to the ledge. Sorting, appraising, ledger work and hand-to-hand exchanges can happen across the window. Keep chairs and stools for visitors outside.
- Close contact must fit across the window. Do not put visitors behind the player, inside the kiosk, or beside the player's knees. Off-screen daily life, home scenes and explicit off-site events have their own locations.

## Content generation

- Shipped localization is generated from the Content JSON via `tools/build_content.py`; the JSON is the real source.
- **Do not run `tools/legacy/parse_v06.py`** — it overwrites `Content/story_v06.json` from the stale text script. Kept only as an archived legacy parser.
- Run `python tools/build_content.py` and `python tools/check_project.py` after content changes.
- The dual route source is `Content/dual_romance_v04.json`; the scene logic is in `src/DualStory.cs`.
- The solo romance (恋爱线) source is `Content/love_romance_v01.json`, `Content/love_romance_v02.json` and `Content/love_romance_v03.json`; scene logic is in `src/LoveStory.cs` / `src/LoveDirector.cs`, with generated text in `src/LoveContent.g.cs`.

## Writer pipeline usage rules (user requirement)

These rules apply to every call of the novel/writer pipeline under `tools/NovelWorkFlow_Eleanor/`:

1. **Only the three source files may be loaded**: `writer_prompt_template.md`, `writer_system_prompt.md`, and a selected `world_book*.json`. Do not use ad-hoc custom system/user prompt files, inline prompt replacements, or extra prompt documents for pipeline calls.
2. **The Agent may modify `world_book.json` only by default.** It may back up or copy the world book (for example `world_book_D.json`) for revisions or branch work, and choose which copy the pipeline uses. Story outlines, ActCards, units, and branch material must be injected through the selected world book, not through separate prompt files.
3. **Modifying `writer_prompt_template.md` requires the user's explicit approval button.** Do not edit it, or revert/edit an approved change, without that approval.
4. **Never modify `writer_system_prompt.md`.** It is read-only.
5. **All active `world_book*.json` must use `project_name` = `上层区的富家千金爱上在下层区开当铺的我`.** Backups (`*.bak*`) may keep historical values; run `python tools/NovelWorkFlow_Eleanor/check_worldbooks.py` after adding or copying a world book.
6. **Route world books use the global static character cards as the base.** `characters.*.writing_notes` stays the static card text; route-specific stage, facts, voice and boundaries belong in `act_cards`, `initial_state` and `dynamic_state`, not in a rewritten character card.

Operational notes:
- The engine selects the world book with `ELEANOR_WORLD_BOOK` (default `world_book.json`), for example `ELEANOR_WORLD_BOOK=world_book_D.json python Novel_Engine_V7.py`.
- Existing custom-prompt experiment scripts in `tools/NovelWorkFlow_Eleanor/` are not valid pipeline entry points under these rules. Do not use them for production generation.

## Publishing

- Do not push to GitHub, Steam Workshop, or Nexus unless the user explicitly asks.
- Build with `bash build.sh` only when a deploy to the local game is intended; use `dotnet build` for compile-only checks.
- Keep release evidence in `dist/` and release notes in `docs/RELEASE_*.md`.
- Keep all DLL/executable rollback copies outside the entire installed mod directory (for example in workspace `dist/`). Renaming a DLL to `.bak` inside the mod still triggers the game's `payload-file` rejection. After any local deployment, run `tools/scan_package.ps1` against the actual installed mod directory, not only a staging package, before reporting success.
