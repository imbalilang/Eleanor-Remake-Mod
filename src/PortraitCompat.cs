using System;
using UnityEngine;

namespace EleanorRomance
{
    /// <summary>
    /// Bundled beautified portraits.  Eleanor's CutePortraits base portraits are
    /// included in this mod with the original author's permission; the separate
    /// CutePortraits mod is no longer detected or required.  This mod replaces
    /// native Eleanor art and its own authored clients, but deliberately leaves
    /// another mod's clients and textures untouched (same cooperative model as
    /// the portrait replacement mods).  Lydia and Mother always use this mod's
    /// beautified portraits; the old vanilla keys remain only for migration.
    /// </summary>
    public static class PortraitCompat
    {
        // Attribution and legacy-save keys.  No external mod lookup remains.
        public const string BeautyModId = "cute_portraits";
        public const ulong BeautyWorkshopId = 3829696854UL;
        public const string BeautyKeyPrefix = "cp_portrait_";
        public const string LocalPortraitPrefix = "er_portrait_";
        /// <summary>Single external-facing default: our normal beautified Eleanor.</summary>
        public const string DefaultEleanorKey = "cp_portrait_eleanor1";
        public const string MaidKey = "er_portrait_eleanor_maid_female_beauty";
        public const string VanillaMaidKey = "er_portrait_eleanor_maid_female_vanilla";
        public const string GalaKey = "er_portrait_eleanor_gala_female_beauty";
        public const string LydiaDateVanillaKey = "er_portrait_lydia_date_female_vanilla";
        public const string LydiaDateBeautyKey = "er_portrait_lydia_date_female_beauty";

        /// <summary>The bundled beauty set is always active.</summary>
        public static bool IsBeautyActive => true;

        public enum PortraitMode
        {
            /// <summary>Native/our story use ours; another mod's Eleanor art is kept.</summary>
            Auto,
            /// <summary>All Eleanor clients use ours, overriding other mods.</summary>
            Force,
            /// <summary>Use vanilla/other-mod portraits everywhere, including our story.</summary>
            Other
        }

        public static PortraitMode Mode { get; private set; } = PortraitMode.Auto;
        private static bool configRegistered;

        /// <summary>
        /// Native mod settings entry.  Players can resolve portrait-mod conflicts
        /// without editing files; the default remains the cooperative Auto mode.
        /// </summary>
        public static void RegisterConfig()
        {
            if (configRegistered) return;
            configRegistered = true;
            try
            {
                ModConfig config = ModHelper.GetConfig(ModConstants.ModId);
                ConfigEntry<PortraitMode> entry = config.DefineEnum(
                    "portrait_mode",
                    PortraitMode.Auto,
                    "立绘 / Portraits",
                    () => L.Get("er_config_portrait_mode"),
                    () => L.Get("er_config_portrait_mode_tip"));
                entry.OnChanged += value =>
                {
                    Mode = value;
                    StoryDirector.Log("Portrait mode: " + value);
                    EnforceCooperativeClientPortrait();
                };
                Mode = entry.Value;
                StoryDirector.Log("Portrait mode: " + Mode);
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Portrait config unavailable: " + ex);
            }
        }

        public static string Status =>
            "Bundled beautified portraits; CutePortraits assets included with permission; Lydia/Mother use project beauty art.";

        public static void Refresh()
        {
            StoryDirector.Log("Portrait set: " + Status);
        }

        /// <summary>
        /// Resolves a saved/native sprite key against the bundled set.  Our own
        /// clients use private bundled keys instead of the native Eleanor keys,
        /// so a mod that replaces native art cannot reach our authored scenes.
        /// Local er_portrait_* keys are returned as-is.
        /// </summary>
        public static string Resolve(string vanillaKey)
        {
            if (string.IsNullOrEmpty(vanillaKey)) return vanillaKey;
            if (Mode == PortraitMode.Other)
            {
                if (vanillaKey.StartsWith(LocalPortraitPrefix, StringComparison.Ordinal))
                    return IsLocalEleanorOutfitKey(vanillaKey) ? EleanorNativeFallback(vanillaKey) : vanillaKey;
                if (vanillaKey.StartsWith(BeautyKeyPrefix, StringComparison.Ordinal)
                    || IsNativeEleanorKey(vanillaKey))
                    return EleanorNativeFallback(vanillaKey);
                return vanillaKey;
            }
            if (vanillaKey.StartsWith(LocalPortraitPrefix, StringComparison.Ordinal)) return vanillaKey;
            if (vanillaKey.StartsWith(BeautyKeyPrefix, StringComparison.Ordinal))
            {
                return HasBundledSprite(vanillaKey) ? vanillaKey : RevertKey(vanillaKey);
            }
            switch (vanillaKey)
            {
                case "eleanor1":
                case "eleanor2":
                case "eleanor3":
                case "eleanor_scav":
                case "eleanor_rev":
                    return BeautyKeyPrefix + vanillaKey;
                default:
                    return vanillaKey;
            }
        }

        private static bool IsLocalEleanorOutfitKey(string key)
        {
            return key == MaidKey || key == VanillaMaidKey || key == GalaKey
                || key == ModConstants.EleanorVestPortrait
                || key == ModConstants.EleanorIntimatePortrait;
        }

        /// <summary>Native/base key used when the player chooses another portrait set.</summary>
        private static string EleanorNativeFallback(string spriteName)
        {
            string key = spriteName ?? "";
            if (key.StartsWith(BeautyKeyPrefix, StringComparison.Ordinal))
                key = key.Substring(BeautyKeyPrefix.Length);
            if (key.EndsWith("_female", StringComparison.Ordinal))
                key = key.Substring(0, key.Length - "_female".Length);
            switch (key)
            {
                case "eleanor2":
                case "eleanor3":
                case "eleanor_scav":
                case "eleanor_rev":
                    return key;
                case "eleanor1":
                    return "eleanor1";
                default:
                    break;
            }
            if (key == MaidKey || key == VanillaMaidKey) return "eleanor_scav";
            if (key == GalaKey) return "eleanor1";
            if (key == ModConstants.EleanorVestPortrait) return "eleanor_rev";
            if (key == ModConstants.EleanorIntimatePortrait) return "eleanor1";
            if (IsLocalEleanorOutfitKey(spriteName) || IsLocalEleanorOutfitKey(key))
                return "eleanor1";
            return IsNativeEleanorKey(key) ? key : "eleanor1";
        }

        /// <summary>Custom-outfit portrait with a bundled-safe fallback.</summary>
        public static string ResolveOutfit(string beautyKey, string fallbackKey)
        {
            return EnsureLocalPortrait(beautyKey) ? beautyKey : Resolve(fallbackKey);
        }

        public static string ResolveCharacter(string beautyKey, string fallbackKey)
        {
            return ResolveOutfit(beautyKey, fallbackKey);
        }

        public static string ResolveLydia()
        {
            // Lydia is a romance lead and only uses her beautified portrait.
            // The generic NPC key is only a missing-file safety net.
            return EnsureLocalPortrait(ModConstants.LydiaPortraitBeauty)
                ? ModConstants.LydiaPortraitBeauty
                : Resolve(ModConstants.LydiaSprite);
        }

        public static string ResolveMother()
        {
            return EnsureLocalPortrait(ModConstants.MotherPortraitBeauty)
                ? ModConstants.MotherPortraitBeauty
                : Resolve(ModConstants.MotherSprite);
        }

        /// <summary>
        /// Approved private knit outfit.  Missing home art falls back to the
        /// approved daily outfit, then to the generic game portrait.
        /// </summary>
        public static string ResolveMotherHome()
        {
            return EnsureLocalPortrait(ModConstants.MotherHomePortraitBeauty)
                ? ModConstants.MotherHomePortraitBeauty
                : ResolveMother();
        }

        public static bool IsMaidScene(string id)
        {
            switch (id)
            {
                case "er_story_maid_m19": case "er_story_maid_m20":
                case "er_story_maid_m22": case "er_story_maid_m23": case "er_story_maid_m28":
                case "er_story_maid_wait": case "er_story_maid_daily": case "er_story_maid_housing":
                case "er_story_maid_feedback": case "er_story_maid_world": return true;
                default: return false;
            }
        }

        public static bool CanUseMaid(string id)
        {
            return IsMaidSceneEligible(id) && HasLocalSprite(MaidKey);
        }

        private static bool IsMaidSceneEligible(string id)
        {
            return MaidState.Active && IsMaidScene(id)
                && (MaidState.DebutShown || id == "er_story_maid_m19");
        }

        public static string ResolveMaid(string id, string fallback)
        {
            return CanUseMaid(id) && EnsureLocalPortrait(MaidKey) ? MaidKey : Resolve(fallback);
        }

        public static string ResolveLydiaDate()
        {
            return EnsureLocalPortrait(LydiaDateBeautyKey) ? LydiaDateBeautyKey : ResolveLydia();
        }

        public static Sprite GetLocalSprite(string key)
        {
            try
            {
                var mod = FindOurMod();
                if (mod?.Sprites != null && !string.IsNullOrEmpty(key)
                    && mod.Sprites.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Local sprite lookup failed for " + key + ": " + ex);
            }
            return null;
        }

        /// <summary>
        /// True for any client that is Eleanor herself.  Identity comes from the
        /// character (localized name, identifier), so another mod's custom
        /// identifier or portrait key is still recognised; the sprite key is
        /// only a last resort for saved/queued copies without a resolved name.
        /// </summary>
        public static bool IsEleanorClient(StoreClient client)
        {
            if (client == null) return false;
            string id = client.identifier ?? "";
            string name = client.displayName ?? "";
            try
            {
                if (!string.IsNullOrEmpty(name)
                    && (name == L.Eleanor || name == "Eleanor" || name == "埃莉诺")) return true;
            }
            catch
            {
                // Localization is not ready; fall through to identifier checks.
            }
            if (id.Length > 0)
            {
                if (id.Equals("eleanorUncle", StringComparison.OrdinalIgnoreCase)
                    || id.Equals("eleanorThreat", StringComparison.OrdinalIgnoreCase)
                    || id.Equals("eleanorBailOfficer", StringComparison.OrdinalIgnoreCase)
                    || id.Equals(VanillaEleanorBridge.BailOfficerId, StringComparison.OrdinalIgnoreCase)) return false;
                if (id.IndexOf("eleanor", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            // Saved/queued copies may not have a resolved display name yet.
            return IsEleanorSpriteKey(client.spriteName ?? "");
        }

        /// <summary>True for clients authored by this mod.</summary>
        public static bool IsOurEleanorClient(StoreClient client)
        {
            if (client == null) return false;
            if (client.sourceModId == ModConstants.ModId) return true;
            return (client.identifier ?? "").StartsWith("er_story_", StringComparison.Ordinal);
        }

        /// <summary>True for a client or artwork owned by another enabled mod.</summary>
        public static bool IsExternalModClient(StoreClient client)
        {
            return client != null && !string.IsNullOrEmpty(client.sourceModId)
                && client.sourceModId != ModConstants.ModId;
        }

        /// <summary>
        /// True when the Eleanor client being displayed or about to be displayed
        /// belongs to another mod.  In that case native-key replacement must
        /// stand down, otherwise another mod's authored outfit is flattened to
        /// this mod's default look (Agri Expansion uses native keys directly).
        /// </summary>
        private static bool IsExternalEleanorContext()
        {
            try
            {
                PlayerStore store = PlayerStore.Instance;
                if (store == null) return false;
                StoreClient current = store.currentClientInstance?.GetClientBlueprint();
                if (current != null && IsEleanorClient(current) && IsExternalModClient(current)) return true;
                var stack = store.storeClientManager?.clientStack;
                if (stack != null && stack.Count > 0)
                {
                    StoreClient next = stack[0];
                    if (next != null && IsEleanorClient(next) && IsExternalModClient(next)) return true;
                }
            }
            catch
            {
                // A failed ownership check must not disturb native portrait calls.
            }
            return false;
        }

        /// <summary>
        /// Resolves the visible portrait for an Eleanor client.  Vanilla/base
        /// clients map native Eleanor keys to this mod's set; our own story keeps
        /// its authored outfits and variants; another mod's client is left alone.
        /// </summary>
        public static Sprite ResolveEleanorClientSprite(StoreClient client)
        {
            if (client == null) return null;

            string spriteName = client.spriteName ?? "";
            if (!IsOurEleanorClient(client))
            {
                // Other uses vanilla/other-mod art; Force gives every Eleanor
                // our default; Auto leaves another mod's client alone.
                if (Mode == PortraitMode.Other) return null;
                if (Mode == PortraitMode.Auto && IsExternalModClient(client)) return null;
                return DefaultEleanorSprite();
            }
            if (Mode == PortraitMode.Other) return null;

            if (spriteName.StartsWith(LocalPortraitPrefix, StringComparison.Ordinal))
                return GetLocalSprite(spriteName);

            string outfit = EleanorOutfitKey(spriteName);
            if (outfit != null)
            {
                Sprite local = GetLocalSprite(outfit);
                if (local != null) return local;
            }

            string variant = EleanorVariantFromKey(spriteName);
            Sprite sprite;
            if (variant != null && TryFindBundledSprite(BeautyKeyPrefix + variant, out sprite))
                return sprite;
            return DefaultEleanorSprite();
        }

        private static Sprite DefaultEleanorSprite() => GetLocalSprite(DefaultEleanorKey);

        /// <summary>
        /// Cooperative display guard.  Native Eleanor art is replaced with this
        /// mod's art; our own clients are protected; another mod's client or
        /// texture is never taken over.  Expressions already belonging to this
        /// mod are preserved.
        /// </summary>
        public static void EnforceCooperativeClientPortrait(Dialogue dialogue = null)
        {
            try
            {
                StoreClient client = PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
                if (!IsEleanorClient(client)) return;
                if (Mode == PortraitMode.Other)
                {
                    EnforceOtherModePortrait(client);
                    return;
                }

                bool ours = IsOurEleanorClient(client);
                if (Mode == PortraitMode.Auto && !ours && IsExternalModClient(client)) return;

                StoreClientMono mono = StoreClientMono.Instance;
                if (mono == null || mono.image == null) return;

                Sprite current = mono.image.sprite;
                if (IsOurSprite(current)) return;
                // Auto yields to another portrait mod's art; Force wins by
                // explicit player choice.  Auto also pauses expression switching
                // so it cannot flash our art over theirs and back.
                if (Mode != PortraitMode.Force && IsForeignModSprite(current)) return;

                Sprite desired = dialogue != null && ours
                    ? PortraitExpression.Resolve(dialogue) : null;
                if (desired == null) desired = ResolveEleanorClientSprite(client);
                if (desired != null) mono.image.sprite = desired;
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Eleanor portrait cooperative guard failed: " + ex);
            }
        }

        /// <summary>
        /// Other mode: our own story falls back to the native/base portrait so
        /// the player's chosen portrait mod can own it; expressions are off.
        /// </summary>
        private static void EnforceOtherModePortrait(StoreClient client)
        {
            try
            {
                if (!IsOurEleanorClient(client)) return;
                StoreClientMono mono = StoreClientMono.Instance;
                if (mono == null || mono.image == null) return;
                if (!IsOurSprite(mono.image.sprite)) return;

                string fallback = EleanorNativeFallback(client.spriteName);
                Sprite sprite = SpriteDict.Instance != null
                    ? SpriteDict.Instance.GetSprite(fallback) : null;
                if (sprite != null) mono.image.sprite = sprite;
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Eleanor native portrait fallback failed: " + ex);
            }
        }

        /// <summary>
        /// True when the visible client image is currently owned by another mod.
        /// Expression switching is suppressed in that state instead of forcing
        /// our portrait back.
        /// </summary>
        public static bool IsForeignPortraitOnCurrentClient()
        {
            if (Mode == PortraitMode.Force) return false;
            if (Mode == PortraitMode.Other)
            {
                StoreClient client = PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
                return IsEleanorClient(client);
            }
            try
            {
                StoreClientMono mono = StoreClientMono.Instance;
                if (mono == null || mono.image == null) return false;
                Sprite current = mono.image.sprite;
                if (current == null || IsOurSprite(current)) return false;
                return IsForeignModSprite(current);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Cooperative key-level replacement.  Only the game's native Eleanor
        /// keys are eligible; another mod's key or already-applied foreign art
        /// is left untouched.
        /// </summary>
        public static bool TryNativeEleanorOverride(string key, Sprite current, out Sprite sprite)
        {
            sprite = null;
            if (Mode == PortraitMode.Other || !IsNativeEleanorKey(key)) return false;
            if (IsExternalEleanorContext()) return false;
            if (Mode != PortraitMode.Force && current != null
                && (IsOurSprite(current) || IsForeignModSprite(current))) return false;
            sprite = DefaultEleanorSprite();
            return sprite != null;
        }

        private static bool IsNativeEleanorKey(string key)
        {
            switch (key)
            {
                case "eleanor1":
                case "eleanor2":
                case "eleanor3":
                case "eleanor_scav":
                case "eleanor_rev":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Any sprite key naming Eleanor, excluding items and our local art.</summary>
        private static bool IsEleanorSpriteKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (key.StartsWith(LocalPortraitPrefix, StringComparison.Ordinal)) return false;
            if (key.StartsWith("er_expr_", StringComparison.Ordinal)) return false;
            if (key.IndexOf("eleanor", StringComparison.OrdinalIgnoreCase) < 0) return false;
            if (key.IndexOf("bracelet", StringComparison.OrdinalIgnoreCase) >= 0
                || key.IndexOf("locket", StringComparison.OrdinalIgnoreCase) >= 0
                || key.IndexOf("perfume", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return true;
        }

        private static string EleanorOutfitKey(string spriteName)
        {
            string key = (spriteName ?? "").ToLowerInvariant();
            if (key.Contains("gala")) return GalaKey;
            if (key.Contains("maid")) return MaidKey;
            if (key.Contains("intimate")) return ModConstants.EleanorIntimatePortrait;
            if (key.Contains("undersuit") || key.Contains("_vest") || key.Contains("vest_"))
                return ModConstants.EleanorVestPortrait;
            return null;
        }

        private static string EleanorVariantFromKey(string spriteName)
        {
            string key = (spriteName ?? "").ToLowerInvariant();
            if (key.Contains("scav")) return "eleanor_scav";
            if (key.Contains("eleanor_rev") || key.Contains("_rev_") || key.EndsWith("_rev"))
                return "eleanor_rev";
            if (key.Contains("eleanor3") || key.Contains("eleanor_3") || key.Contains("sad")) return "eleanor3";
            if (key.Contains("eleanor2") || key.Contains("eleanor_2") || key.Contains("jewel")) return "eleanor2";
            if (key.Contains("eleanor1") || key.Contains("eleanor_1")) return "eleanor1";
            return null;
        }

        private static bool IsOurSprite(Sprite sprite)
        {
            if (sprite == null) return false;
            try
            {
                LoadedMod mod = FindOurMod();
                if (mod?.Sprites == null) return false;
                foreach (Sprite own in mod.Sprites.Values)
                    if (ReferenceEquals(own, sprite)) return true;
            }
            catch
            {
                // A failed ownership check only costs expression continuity.
            }
            return false;
        }

        /// <summary>
        /// True when the visible sprite's texture belongs to another enabled mod.
        /// This is the cooperative half of the portrait policy: vanilla clients
        /// keep another mod's art instead of being forced back to ours.
        /// </summary>
        private static bool IsForeignModSprite(Sprite sprite)
        {
            if (sprite == null) return false;
            try
            {
                if (ModLoader.Mods == null) return false;
                foreach (LoadedMod mod in ModLoader.Mods)
                {
                    if (mod == null || !mod.IsEnabled || mod.Manifest == null) continue;
                    if (mod.Manifest.ID == ModConstants.ModId) continue;
                    if (ContainsSprite(mod.Sprites, sprite) || ContainsSprite(mod.PaperSprites, sprite))
                        return true;
                    if (mod.Textures == null) continue;
                    foreach (Texture2D texture in mod.Textures.Values)
                        if (texture != null && sprite.texture == texture) return true;
                }
            }
            catch
            {
                // A failed ownership check must not disturb the native display.
            }
            return false;
        }

        private static bool ContainsSprite(System.Collections.Generic.Dictionary<string, Sprite> sprites, Sprite sprite)
        {
            if (sprites == null) return false;
            foreach (Sprite candidate in sprites.Values)
                if (ReferenceEquals(candidate, sprite)) return true;
            return false;
        }

        public static bool IsGalaScene(string id) => id == "er_story_gala" && GalaStory.Eligible
            && MaidState.PendingVisit == GalaStory.VisitId;

        public static string GalaDescription => L.Get(ResolveGala("er_story_gala") == GalaKey
            ? "er_gala_gown_beauty" : "er_gala_gown_vanilla");

        public static string ResolveGala(string id)
        {
            if (id == "er_story_gala" && GalaStory.Eligible && EnsureLocalPortrait(GalaKey)) return GalaKey;
            return Resolve("eleanor1");
        }

        /// <summary>
        /// Bridges a bundled cp_portrait_* sprite into the native dictionary for
        /// vanilla keys such as eleanor1.  Only characters bundled with this mod
        /// are replaced; other keys fall through to the game.
        /// </summary>
        public static bool TryBundledBeauty(string key, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(key)) return false;
            if (key.StartsWith(LocalPortraitPrefix, StringComparison.Ordinal)) return false;

            string beautyKey = key.StartsWith(BeautyKeyPrefix, StringComparison.Ordinal)
                ? key
                : BeautyKeyPrefix + key;
            if (!TryFindBundledSprite(beautyKey, out sprite)) return false;
            try
            {
                if (SpriteDict.Instance != null && SpriteDict.Instance.spriteDictionary != null)
                    SpriteDict.Instance.spriteDictionary[key] = sprite;
            }
            catch
            {
                // The direct sprite result is enough if the dictionary is not ready.
            }
            return true;
        }

        private static bool TryFindBundledSprite(string beautyKey, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(beautyKey)) return false;

            LoadedMod own = FindOurMod();
            if (own == null || own.Sprites == null) return false;

            string[] candidates = beautyKey.EndsWith("_female", StringComparison.Ordinal)
                ? new[] { beautyKey, beautyKey.Substring(0, beautyKey.Length - "_female".Length) }
                : new[] { beautyKey, beautyKey + "_female" };

            foreach (string candidate in candidates)
            {
                if (!own.Sprites.TryGetValue(candidate, out sprite) || sprite == null) continue;
                return true;
            }
            sprite = null;
            return false;
        }

        /// <summary>
        /// Safety net for SpriteDict rebuilds.  Local er_portrait_* keys are
        /// bridged from this mod; cp_portrait_* keys use the bundled set.
        /// </summary>
        public static void EnsureKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (key == VanillaMaidKey)
            {
                // Legacy maid key: show the beautified maid art when possible.
                if (MaidState.Active && EnsureLocalPortrait(MaidKey))
                    AliasLocalPortrait(MaidKey, VanillaMaidKey);
                return;
            }
            if (key == MaidKey)
            {
                if (!MaidState.Active) return;
                EnsureLocalPortrait(MaidKey);
                return;
            }
            if (key == GalaKey)
            {
                // The gala look is also used by maid v2 stage scenes, so it
                // cannot be gated on the standalone gala story.  Register it
                // whenever requested; if the file is missing, show our default
                // Eleanor rather than the game's random-scav fallback.
                if (!EnsureLocalPortrait(GalaKey))
                    AliasLocalPortrait(Resolve("eleanor1"), GalaKey);
                return;
            }
            if (key == ModConstants.LydiaPortraitVanilla || key == LydiaDateVanillaKey)
            {
                // Legacy Lydia keys must show the beautified portrait, never the old vanilla art.
                string beauty = key == LydiaDateVanillaKey ? ResolveLydiaDate() : ResolveLydia();
                AliasLocalPortrait(beauty, key);
                return;
            }
            if (key == ModConstants.MotherPortraitVanilla)
            {
                // Legacy Mother key must show the beautified daily portrait.
                AliasLocalPortrait(ResolveMother(), key);
                return;
            }

            if (key.StartsWith(LocalPortraitPrefix, StringComparison.Ordinal))
            {
                EnsureLocalPortrait(key);
                return;
            }

            if (key.StartsWith(BeautyKeyPrefix, StringComparison.Ordinal))
            {
                TryBundledBeauty(key, out _);
            }
        }

        private static void AliasLocalPortrait(string sourceKey, string targetKey)
        {
            Sprite sprite = GetLocalSprite(sourceKey);
            if (sprite == null || SpriteDict.Instance == null || SpriteDict.Instance.spriteDictionary == null) return;
            SpriteDict.Instance.spriteDictionary[targetKey] = sprite;
        }

        private static bool EnsureLocalPortrait(string key)
        {
            try
            {
                if (SpriteDict.Instance == null || SpriteDict.Instance.spriteDictionary == null) return false;
                if (SpriteDict.Instance.spriteDictionary.ContainsKey(key)) return true;

                LoadedMod mod = FindOurMod();
                if (mod == null || mod.Sprites == null) return false;

                Sprite sprite;
                if (!mod.Sprites.TryGetValue(key, out sprite) || sprite == null) return false;

                SpriteDict.Instance.spriteDictionary[key] = sprite;
                StoryDirector.Log("Local portrait bridged: " + key);
                return true;
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Local portrait bridge failed for " + key + ": " + ex);
                return false;
            }
        }

        private static bool HasLocalSprite(string key)
        {
            LoadedMod own = FindOurMod();
            return own?.Sprites != null && own.Sprites.TryGetValue(key, out var sprite) && sprite != null;
        }

        private static bool HasBundledSprite(string key)
        {
            LoadedMod own = FindOurMod();
            if (own == null || own.Sprites == null || string.IsNullOrEmpty(key)) return false;
            if (own.Sprites.TryGetValue(key, out var sprite) && sprite != null) return true;
            if (key.EndsWith("_female", StringComparison.Ordinal))
            {
                string baseKey = key.Substring(0, key.Length - "_female".Length);
                return own.Sprites.TryGetValue(baseKey, out sprite) && sprite != null;
            }
            return own.Sprites.TryGetValue(key + "_female", out sprite) && sprite != null;
        }

        private static LoadedMod FindOurMod()
        {
            try
            {
                if (ModLoader.Mods == null) return null;
                foreach (LoadedMod mod in ModLoader.Mods)
                {
                    if (mod == null || !mod.IsEnabled || mod.Manifest == null) continue;
                    if (mod.Manifest.ID == ModConstants.ModId) return mod;
                }
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Local mod detection failed: " + ex);
            }
            return null;
        }

        /// <summary>
        /// Migrates clients restored from a save: vanilla Lydia/Mother keys are
        /// upgraded to the bundled beauty portraits, unbundled external
        /// cp_portrait_* keys are reverted to their vanilla base, and local
        /// beauty keys are preserved.
        /// </summary>
        public static void SanitizeLoadedClients()
        {
            try
            {
                if (PlayerStore.Instance == null || PlayerStore.Instance.storeClientManager == null) return;
                var stack = PlayerStore.Instance.storeClientManager.clientStack;
                if (stack != null) foreach (StoreClient client in stack)
                {
                    SanitizeClient(client);
                }
                SanitizeClient(PlayerStore.Instance.currentClientInstance?.GetClientBlueprint());
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Portrait save sanitisation failed: " + ex);
            }
        }

        private static void SanitizeClient(StoreClient client)
        {
            if (client == null || string.IsNullOrEmpty(client.spriteName)) return;

            if (IsEleanorClient(client) && IsOurEleanorClient(client))
            {
                // Other mode hands our story clients back to the native/base
                // keys so the player's chosen portrait mod can own them.
                if (Mode == PortraitMode.Other)
                {
                    client.spriteName = EleanorNativeFallback(client.spriteName);
                    return;
                }
                // Otherwise our own clients move off native keys so a native-art
                // replacement mod has no key to overwrite in our scenes.
                if (IsNativeEleanorKey(client.spriteName))
                {
                    client.spriteName = Resolve(client.spriteName);
                    return;
                }
            }

            if (client.spriteName == GalaKey)
            {
                // Maid v2 stage scenes reuse the gala look and carry an
                // er_story_v2_* identifier; do not degrade them to the default
                // portrait on load like a stale standalone-gala key.
                bool keepGala = IsGalaScene(client.identifier)
                    || (IsOurEleanorClient(client)
                        && !string.IsNullOrEmpty(client.identifier)
                        && client.identifier.StartsWith("er_story_v2_", StringComparison.Ordinal));
                client.spriteName = keepGala && EnsureLocalPortrait(GalaKey)
                    ? GalaKey : Resolve("eleanor1");
            }
            else if (client.spriteName == MaidKey || client.spriteName == VanillaMaidKey)
            {
                client.spriteName = IsMaidSceneEligible(client.identifier)
                    ? ResolveMaid(client.identifier, "eleanor_scav")
                    : Resolve("eleanor_scav");
            }
            else if (client.spriteName == LydiaDateVanillaKey || client.spriteName == LydiaDateBeautyKey)
            {
                client.spriteName = ResolveLydiaDate();
            }
            else if (client.spriteName == ModConstants.MotherHomePortraitBeauty)
            {
                client.spriteName = ResolveMotherHome();
            }
            else if (client.spriteName == ModConstants.MotherPortraitVanilla
                || client.spriteName == ModConstants.MotherPortraitBeauty
                || client.identifier == "er_story_mother"
                || client.identifier == "er_story_maid_m17")
            {
                client.spriteName = ResolveMother();
            }
            else if (client.spriteName == ModConstants.LydiaPortraitVanilla
                || client.spriteName == ModConstants.LydiaPortraitBeauty
                || (client.spriteName == ModConstants.LydiaSprite
                    && !string.IsNullOrEmpty(client.displayName)
                    && client.displayName == L.Get("er_name_lydia")))
            {
                client.spriteName = ResolveLydia();
            }
            else if (client.spriteName.StartsWith(BeautyKeyPrefix, StringComparison.Ordinal)
                && !HasBundledSprite(client.spriteName))
            {
                client.spriteName = RevertKey(client.spriteName);
            }
        }

        private static string RevertKey(string beautyKey)
        {
            string key = beautyKey.Substring(BeautyKeyPrefix.Length);
            if (key.EndsWith("_female", StringComparison.Ordinal))
            {
                key = key.Substring(0, key.Length - "_female".Length);
            }
            return key;
        }
    }
}
