using System;
using UnityEngine;

namespace EleanorRomance
{
    /// <summary>
    /// Bundled beautified portraits.  Eleanor's CutePortraits base portraits are
    /// included in this mod with the original author's permission; the separate
    /// CutePortraits mod is no longer detected or required.  Lydia and Mother
    /// always use this mod's beautified portraits; the old vanilla keys remain
    /// only for save migration.
    /// </summary>
    public static class PortraitCompat
    {
        // Attribution and legacy-save keys.  No external mod lookup remains.
        public const string BeautyModId = "cute_portraits";
        public const ulong BeautyWorkshopId = 3829696854UL;
        public const string BeautyKeyPrefix = "cp_portrait_";
        public const string LocalPortraitPrefix = "er_portrait_";
        public const string MaidKey = "er_portrait_eleanor_maid_female_beauty";
        public const string VanillaMaidKey = "er_portrait_eleanor_maid_female_vanilla";
        public const string GalaKey = "er_portrait_eleanor_gala_female_beauty";
        public const string LydiaDateVanillaKey = "er_portrait_lydia_date_female_vanilla";
        public const string LydiaDateBeautyKey = "er_portrait_lydia_date_female_beauty";

        /// <summary>The bundled beauty set is always active.</summary>
        public static bool IsBeautyActive => true;

        public static string Status =>
            "Bundled beautified portraits; CutePortraits assets included with permission; Lydia/Mother use project beauty art.";

        public static void Refresh()
        {
            StoryDirector.Log("Portrait set: " + Status);
        }

        /// <summary>
        /// Resolves a saved/native sprite key against the bundled set.  Vanilla
        /// keys are left alone and replaced at render time by TryBundledBeauty;
        /// local er_portrait_* keys are returned as-is.
        /// </summary>
        public static string Resolve(string vanillaKey)
        {
            if (string.IsNullOrEmpty(vanillaKey)) return vanillaKey;
            if (vanillaKey.StartsWith(LocalPortraitPrefix, StringComparison.Ordinal)) return vanillaKey;
            if (vanillaKey.StartsWith(BeautyKeyPrefix, StringComparison.Ordinal))
            {
                return HasBundledSprite(vanillaKey) ? vanillaKey : RevertKey(vanillaKey);
            }
            return vanillaKey;
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

            LoadedMod own = FindOurMod();
            if (own == null || own.Sprites == null) return false;

            string beautyKey = key.StartsWith(BeautyKeyPrefix, StringComparison.Ordinal)
                ? key
                : BeautyKeyPrefix + key;
            string[] candidates = beautyKey.EndsWith("_female", StringComparison.Ordinal)
                ? new[] { beautyKey, beautyKey.Substring(0, beautyKey.Length - "_female".Length) }
                : new[] { beautyKey, beautyKey + "_female" };

            foreach (string candidate in candidates)
            {
                if (!own.Sprites.TryGetValue(candidate, out sprite) || sprite == null) continue;
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
                if (!IsGalaScene("er_story_gala")) return;
                EnsureLocalPortrait(GalaKey);
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

            if (client.spriteName == GalaKey)
            {
                client.spriteName = IsGalaScene(client.identifier) ? ResolveGala(client.identifier) : Resolve("eleanor1");
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
