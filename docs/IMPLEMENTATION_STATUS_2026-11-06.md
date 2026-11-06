# Eleanor Remake · v0.6 Implementation Status

Date: 2026-11-06. Scope: `EleanorRomanceDev`, installed game 0.46D.

This note describes what this pass actually changed. It is not a claim that the
mod has passed a full playthrough.

## 1. Content source

- `docs/全流程攻略_纯阅读版_v0.6_文风优化.txt` is the authoritative text.
- `tools/parse_v06.py` parses it into `Content/story_v06.json`.
  - 26 sections: chapters 1-23, route C, route D, epilogue, ending variants.
  - 181 dialogue blocks, 885 unique dialogue lines.
  - A representative successful route selects 85 blocks / about 525 lines,
    above the 420-line target. This is a static estimate, not a played count.
  - Guide-only `玩家处理` / `结果` notes are not turned into dialogue.
- `tools/build_content.py` compiles the JSON into
  `src/StoryV06Content.g.cs` and generates `Localization/zh.csv` / `en.csv`.
- `tools/check_project.py` verifies every `L.Get` key exists in Chinese and
  every `StoryV06Content.B_*` constant maps to a parsed block.

## 2. Story and state

- 24-step common spine: food → household → belongings → fake uncle → warning /
  rescue → hardship → two scavenger sales → gift → Lydia → answer → 800 credits
  → drive → threat → direction → preparation → reunion → mother → after →
  confession → cooperation → Lydia again → future → conclusion.
- Route C is a separate 8-step arc (101-108); route D is a one-shot arc (201).
- `RomanceState` schema 2 stores mother fate, research/drive custody, romance,
  work, trap facts and deal facts separately.
- Scheduling is story-relative: `NextVisitDay` is based on the last completed
  chapter and a target-day table, not a parallel absolute calendar. Queueing a
  client never advances the story; only the wrap-up commit does.
- Old saves with vanilla Eleanor progress are still protected. The reset removes
  this mod's `er_story_*` clients and the vanilla `eleanor*` records only.

## 3. Behaviour coverage

| Node | Native behaviour |
|---|---|
| Food / household | `onItemBought` reads real sale price; fair / discount / overcharge branches |
| Belongings | `onItemSold` reads real purchase price; fair / overpay / lowball / no-sale branches |
| Fake uncle | stolen `luxury_supply_crate`, native report / capture callbacks, warning clears the trap |
| Trap | `onItemAddedToCounters` + `ArrestCurrentClientForStolenGoods`; rescue next day |
| Hardship | native food sale at zero/low price; no food vs refusal vs accepted |
| Scavenger fund | `onItemSold` accumulates to 800; deadline fallback by her own work / locket sale |
| Drive | custody is a state, not an inventory item; handover options check custody |
| Threat | report/upper channel requires actual evidence + reputation; selling the drive is a route-D choice |
| Protection | Security / Upper / case-record channels; conditions and costs; automatic route-A fallback |
| Reunion | mother safe only after the confirmation dialogue; route B frees seeds once |
| Romance | explicit accept / slow / friends; friends and slow do not undo the rescue |
| Cooperation | Eleanor's visit queues a normal upper-district buyer for a real trade |
| Route C | weapon purchase is a real weapon sale; food purchase is a real food sale |
| Route D | betrayal/exit choices close romance and skip the normal ending branches |

## 4. Build and deployment

```bash
python tools/parse_v06.py
python tools/build_content.py
python tools/check_project.py
bash build.sh
```

`build.sh` now builds to `bin/Release`, keeps a `dist/rollback_*` copy, removes
stale deployed textures, copies the DLL/manifest/CSV/textures/README, and fails
if the DLL or CSV is empty.

## 5. Verified in this pass

- `dotnet build -c Release`: 0 warnings, 0 errors.
- `check_project.py`: no missing Chinese or English keys, no invalid block references.
- `zh.csv`: 1,039 keys. `en.csv`: 1,039 keys; Chinese and English keys match one-for-one.
- The mod DLL and resources deploy to `Mods/EleanorRomance`.

## 6. Not verified / known gaps

1. **No full playthrough.** The game was launched once through Steam, but the
   local mod was disabled in the loader UI; no run was played. All runtime
   ordering, dismissal and save/load behaviour below remains untested.
2. **English is a first-pass translation.** All 885 dialogue keys and UI keys
   are translated, but they have not had a native-speaker review or an in-game
   text-length pass.
3. **Trap report flow** depends on the native report UI accepting a stolen
   luxury crate from the fake uncle. The code copies the vanilla callbacks, but
   the actual report button and arrest sequence need in-game verification.
4. **Rescue chapter** assumes the player can return to Security the next day
   and pay; it is a contextual dialogue, not a new bail UI. Verify it does not
   conflict with an active vanilla event.
5. **KnewTrapUnwarned** is inferred from accepting/inspecting the crate. There
   is no separate "I read the stolen tag" event; the explicit inspect choice is
   a flavour distinction, not a fully separate evidence state.
6. **Mother/Lydia portraits** use fixed vanilla sprite keys as placeholders.
   Replace with the finished art when available.
7. **Bracelet icon** is a generated placeholder, not a style-approved asset.
8. **Balance values** (30 reputation, 120-1200 rescue costs, 300-600 protection
   costs) are provisional and need a normal run to check accessibility.
9. **Route C/D and epilogue** need save/load and interruption tests.
10. **Scenario "Old Echo"** is registered but experimental; it starts chapter 1
    on day 5 (no affection/reputation skip) and has not been played.

## 7. Recommended next tests

1. New save, days 23-30: food/household/belongings sales, uncle accept/report,
   warning, trap arrest, rescue, hardship.
2. Days 31-42: two scavenger sales, savings accumulation, deadline fallback,
   gift, Lydia bracelet, answer branch.
3. Days 41-48: drive custody, threat channels, route A and route B preparation,
   reunion, mother visit.
4. Days 50-59: confession choices, cooperation buyer, Lydia second visit,
   locket gift, conclusion, game-over ending text.
5. Separate runs for route C, route D (all four kinds), friends, slow romance,
   and the old-save reset prompt.
