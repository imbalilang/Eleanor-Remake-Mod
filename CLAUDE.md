# Project instructions

## Required project principles (all agents)

Before changing story, content, prompts, design, artwork direction, or release copy, read and follow `docs/PROJECT_PRINCIPLES.md`. It records the author's project-level positioning and constraints for the full project. Do not invent additional project principles, and do not treat an uncovered topic as permission to override those constraints. If a request conflicts with them, stop and ask the author. This is a self-funded personal hobby; the author's enjoyment is the success metric, not commercial metrics. Do not expand scope, recurring API cost, or asset spend without asking. Do not ask the author to specify chapter or act events by default; event-level planning belongs to the agent within these principles. Present finished scenes or a small set of concrete forks, not blank planning questions.

## Content-rating labels and scene scale (user requirement)

- Never add voluntary content-rating information or labels on your own — including Mature, all-ages, R18, adult, age-gating, age ratings, 成熟向, 全年龄, 年龄分级 or 年龄提示 — in README, manifest, Workshop/Nexus copy, site, docs, release notes, change logs, UI or configuration. If a platform requires a rating field, follow the platform rules and the author's decision.
- Remove such labels when touching existing files.
- Do not impose a content-scale limit on your own. Necessary sexual attraction, tension, innuendo, love talk and body description are core to the romance; do not soften, desexualize or flatten scenes merely because of an assumed rating.
- The author controls the final scale standard for every scene. Follow the scene, ActCard and author direction; when the intended level is unclear, ask the author rather than self-censoring or escalating on your own. Do not write additional scale ceilings or minimums into project docs or prompts.

## Request size limit (user requirement)

- Every individual POST request, for every task (including image viewing, generation and text processing), must stay below 20,000,000 bytes of serialized parameters/body. Include base64 expansion and multipart/JSON overhead when estimating; target at most 16,000,000 bytes to leave margin.
- Compress every image reference locally before upload. Preserve original masters and upload separate compressed copies; crop irrelevant transparent margins and downscale only while retaining useful reference details. Record upload sizes and hashes.
- Preflight the complete batch before sending. Split large image/text batches into separate requests; do not send an oversized request and rely on the server to reject it.

## Artwork requirements

For artwork generation, edits, expression variants or export, read `Art/ART_REQUIREMENTS.md` and the relevant templates in `Art/PROMPT_TEMPLATES.md`. Use the verified author B/detail pipeline for new portrait work; old BOX and fixed-palette notes are historical. Preserve any user-deferred artwork scope and distinguish candidates from runtime replacements.

For any CG generation, placement or review, read `docs/CG_LOCATION_AND_PLACEMENT_PLAN.md`. Its location, staging and placement constraints are mandatory. Do not invent back rooms, beds, sofas or bathrooms in the kiosk; do not place private clothing, lying poses or kisses in ordinary daily shop visits. The currently adopted CG slate is `docs/CG_CURRENT_PLAN.md` (4 routes × 8 slots, endings included); the 36-slot route document is a candidate/expansion pool, not the current schedule. CG style is two-track: **P (pixel/original)** for endings, news, documents and world cards; **A (anime)** for portraits, relationship, daily/private and outing CGs. Label every brief P or A and do not mix them in one image.

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

- **Author update, 2026-11-17:** The author has lifted the default ban on Agent-authored prose revisions. Agents may directly edit dialogue and narration to reduce AI-like writing, while preserving canon, character appeal and runtime behavior. This supersedes older documents that require every substantive wording change to originate from NovelWorkFlow or Super QA. The current comparison task produces reviewable revisions alongside the published text and historical pipeline candidates; it does not authorize publication or deployment. Writer template and system prompt rules below remain in force.
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

## Release versioning (user requirement)

- Every Steam Workshop or Nexus content update must increase `<ModVersion>` in `manifest.xml`. Publishing new files under the same version is forbidden: the game/loader uses the mod version to detect updates, so an unbumped item does not reach subscribers.
- In the same change, sync `README.md` (MOD version), `docs/route_map.html` (`DATA.version`), `docs/ROUTE_MAP_MAINTENANCE.md`, and `site/index.html` via `tools/sync_site.ps1`, then run `python tools/check_route_map.py`.
- `-KeepDescription` / content-only only means the Workshop title and description stay untouched; it never waives the version bump.
- Do not rely on file hashes or Steam's item update time as the player-facing update signal.

## Publishing

- Do not push to GitHub, Steam Workshop, or Nexus unless the user explicitly asks.
- Build with `bash build.sh` only when a deploy to the local game is intended; use `dotnet build` for compile-only checks.
- Keep release evidence in `dist/` and release notes in `docs/RELEASE_*.md`.
- Keep all DLL/executable rollback copies outside the entire installed mod directory (for example in workspace `dist/`). Renaming a DLL to `.bak` inside the mod still triggers the game's `payload-file` rejection. After any local deployment, run `tools/scan_package.ps1` against the actual installed mod directory, not only a staging package, before reporting success.
