using System;
using UnityEngine;

namespace EleanorRomance
{
    /// <summary>
    /// Soft compatibility with the portrait replacement mod
    /// "少女当铺 / Maiden's Pawnshop" (Workshop 3829696854, manifest
    /// cute_portraits).
    ///
    /// CutePortraits keeps its art in LoadedMod.Sprites under
    /// "cp_portrait_<originalKey>" and normally replaces the displayed sprite
    /// with a Harmony postfix.  We additionally expose those sprites through
    /// SpriteDict so our named clients can request them directly.  If the mod
    /// is absent or disabled, Resolve() returns the original vanilla key, and
    /// Sophia, maid, gala and vest outfits fall back to existing game portraits.
    /// Lydia retains her separate authored vanilla portraits.
    /// </summary>
    public static class PortraitCompat
    {
        public const string BeautyModId = "cute_portraits";
        public const ulong BeautyWorkshopId = 3829696854UL;
        public const string BeautyKeyPrefix = "cp_portrait_";
        public const string LocalPortraitPrefix = "er_portrait_";
        public const string MaidKey = "er_portrait_eleanor_maid_female_beauty";
        public const string VanillaMaidKey = "er_portrait_eleanor_maid_female_vanilla";
        public const string GalaKey = "er_portrait_eleanor_gala_female_beauty";
        public const string LydiaDateVanillaKey = "er_portrait_lydia_date_female_vanilla";
        public const string LydiaDateBeautyKey = "er_portrait_lydia_date_female_beauty";

        private static LoadedMod beautyMod;

        public static bool IsBeautyActive
        {
            get
            {
#if NEXUS_BEAUTY
                return true;
#else
                if (beautyMod == null || !beautyMod.IsEnabled)
                {
                    beautyMod = FindBeautyMod();
                }
                return beautyMod != null && beautyMod.IsEnabled;
#endif
            }
        }

        public static string Status
        {
            get
            {
#if NEXUS_BEAUTY
                return "Nexus Beauty Edition; using bundled beautified portraits";
#else
                return IsBeautyActive
                    ? "cute_portraits (" + BeautyWorkshopId + ") active; using beautified portraits"
                    : "cute_portraits (" + BeautyWorkshopId + ") absent/disabled; using vanilla portraits";
#endif
            }
        }

        public static void Refresh()
        {
            beautyMod = FindBeautyMod();
            StoryDirector.Log("Portrait compatibility: " + Status);
        }

        /// <summary>
        /// Returns the beautified key when CutePortraits is active and provides
        /// the requested portrait; otherwise returns the original vanilla key.
        /// </summary>
        public static string Resolve(string vanillaKey)
        {
            if (string.IsNullOrEmpty(vanillaKey)) return vanillaKey;
            if (vanillaKey.StartsWith(BeautyKeyPrefix, StringComparison.Ordinal)
                || vanillaKey.StartsWith(LocalPortraitPrefix, StringComparison.Ordinal)) return vanillaKey;

            LoadedMod mod = FindBeautyMod();
            if (mod == null) return vanillaKey;

            string beautyKey = BeautyKeyPrefix + vanillaKey;
            string dictionaryKey = HasGenderToken(beautyKey)
                ? beautyKey
                : beautyKey + "_female";
            return EnsureSprite(mod, beautyKey, dictionaryKey) ? dictionaryKey : vanillaKey;
        }

        /// <summary>
        /// Custom-outfit portrait with a hard vanilla fallback.  When
        /// CutePortraits is active we use the local beautified portrait; when it
        /// is absent or disabled we deliberately use an existing game sprite
        /// instead of the old local "_vanilla" artwork.  If the beautified
        /// portrait is missing, the generic fallback is resolved through
        /// CutePortraits (or left as the original key).
        /// </summary>
        public static string ResolveOutfit(string beautyKey, string fallbackKey)
        {
            return IsBeautyActive && EnsureLocalPortrait(beautyKey)
                ? beautyKey
                : Resolve(fallbackKey);
        }

        public static string ResolveCharacter(string beautyKey, string fallbackKey)
        {
            return ResolveOutfit(beautyKey, fallbackKey);
        }

        public static string ResolveLydia()
        {
            // Lydia is a romance lead and always keeps a distinct portrait.
            // Without CutePortraits use the authored vanilla-style variant;
            // only an unavailable asset falls back to a generic NPC.
            string chosen = IsBeautyActive
                ? ModConstants.LydiaPortraitBeauty
                : ModConstants.LydiaPortraitVanilla;
            return EnsureLocalPortrait(chosen) ? chosen : Resolve(ModConstants.LydiaSprite);
        }

        public static string ResolveMother()
        {
            return ResolveCharacter(ModConstants.MotherPortraitBeauty, ModConstants.MotherSprite);
        }

        /// <summary>
        /// Approved private knit outfit. Public shop visits use ResolveMother;
        /// home scenes must request this explicitly. Missing home art falls
        /// back to the approved daily outfit before the generic game portrait.
        /// </summary>
        public static string ResolveMotherHome()
        {
            return IsBeautyActive && EnsureLocalPortrait(ModConstants.MotherHomePortraitBeauty)
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
            return IsBeautyActive && IsMaidSceneEligible(id) && HasLocalMaidSprite(MaidKey);
        }
        private static bool IsMaidSceneEligible(string id)
        {
            return MaidState.Active && IsMaidScene(id)
                && (MaidState.DebutShown || id == "er_story_maid_m19");
        }
        private static bool HasLocalMaidSprite(string key)
        {
            LoadedMod own = FindOurMod();
            return own?.Sprites != null && own.Sprites.TryGetValue(key, out var sprite) && sprite != null;
        }
        public static string ResolveMaid(string id,string fallback)
        {
            // The custom maid outfit exists only in the beautified art set.
            // Without CutePortraits, reuse the scene's ordinary Eleanor portrait.
            return CanUseMaid(id) && EnsureLocalPortrait(MaidKey) ? MaidKey : Resolve(fallback);
        }
        public static string ResolveLydiaDate()
        {
            string chosen = IsBeautyActive ? LydiaDateBeautyKey : LydiaDateVanillaKey;
            return EnsureLocalPortrait(chosen) ? chosen : ResolveLydia();
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

        public static bool IsGalaScene(string id)=>id=="er_story_gala" && GalaStory.Eligible
            && MaidState.PendingVisit==GalaStory.VisitId;
        public static string GalaDescription=>L.Get(ResolveGala("er_story_gala")==GalaKey
            ?"er_gala_gown_beauty":"er_gala_gown_vanilla");
        public static string ResolveGala(string id)
        {
            // Only a beautified portrait is authored. Vanilla reuses Eleanor's
            // original upper-level portrait; creation precedes queue ownership.
            if(id=="er_story_gala" && GalaStory.Eligible && IsBeautyActive && HasLocalMaidSprite(GalaKey)
                && EnsureLocalPortrait(GalaKey))return GalaKey;
            return Resolve("eleanor1");
        }

#if NEXUS_BEAUTY
        /// <summary>
        /// Nexus builds bundle the CutePortraits portraits under this mod's
        /// Textures/Items.  Resolve any vanilla portrait key (and cp_portrait
        /// alias) to that local beauty sprite before the native dictionary is
        /// consulted.  This makes the Nexus package beauty-first without the
        /// separate CutePortraits mod.
        /// </summary>
        public static bool TryNexusBeauty(string key, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(key)) return false;
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
#endif

        /// <summary>
        /// Safety net for SpriteDict rebuilds and direct cp_portrait_* lookups.
        /// Called by a GetSprite prefix; does nothing outside the beauty prefix.
        /// </summary>
        public static void EnsureKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (key == VanillaMaidKey) return; // Legacy key; no longer displayed.
            if (key == MaidKey && (!IsBeautyActive || !MaidState.Active)) return;
            if (key == GalaKey && (!IsBeautyActive || !IsGalaScene("er_story_gala"))) return;

            if (key.StartsWith(LocalPortraitPrefix, StringComparison.Ordinal))
            {
                EnsureLocalPortrait(key);
                return;
            }

            if (!key.StartsWith(BeautyKeyPrefix, StringComparison.Ordinal)) return;

            LoadedMod mod = FindBeautyMod();
            if (mod == null || mod.Sprites == null) return;

            string sourceKey = key;
            if (!mod.Sprites.ContainsKey(sourceKey) && key.EndsWith("_female", StringComparison.Ordinal))
            {
                sourceKey = key.Substring(0, key.Length - "_female".Length);
            }
            EnsureSprite(mod, sourceKey, key);
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
        /// Reverts beauty keys on clients restored from a save when the beauty
        /// mod is no longer enabled, so a stale key cannot fall back to a random
        /// sprite.
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
            if(client.spriteName==GalaKey)
            {
                client.spriteName=IsGalaScene(client.identifier)?ResolveGala(client.identifier):Resolve("eleanor1");
            }
            else if (client.spriteName == MaidKey || client.spriteName == VanillaMaidKey)
            {
                client.spriteName = IsMaidSceneEligible(client.identifier)
                    ? ResolveMaid(client.identifier, "eleanor_scav")
                    : Resolve("eleanor_scav");
            }
            else if (client.spriteName == ModConstants.LydiaPortraitVanilla
                || client.spriteName == ModConstants.LydiaPortraitBeauty)
            {
                client.spriteName = ResolveLydia();
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
            else if (client.spriteName == LydiaDateVanillaKey || client.spriteName == LydiaDateBeautyKey)
            {
                client.spriteName = ResolveLydiaDate();
            }
            else if (!IsBeautyActive && client.spriteName.StartsWith(BeautyKeyPrefix, StringComparison.Ordinal))
                client.spriteName = RevertKey(client.spriteName);
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

        private static LoadedMod FindBeautyMod()
        {
            try
            {
                if (ModLoader.Mods == null) return null;
                foreach (LoadedMod mod in ModLoader.Mods)
                {
                    if (mod == null || !mod.IsEnabled || mod.Manifest == null) continue;
                    if (mod.Manifest.ID == BeautyModId || mod.WorkshopId == BeautyWorkshopId)
                    {
                        return mod;
                    }
                }
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Portrait compatibility detection failed: " + ex);
            }
            return null;
        }

        private static bool EnsureSprite(LoadedMod mod, string sourceKey, string dictionaryKey)
        {
            try
            {
                if (SpriteDict.Instance == null || SpriteDict.Instance.spriteDictionary == null) return false;
                if (mod.Sprites == null) return false;
                if (SpriteDict.Instance.spriteDictionary.ContainsKey(dictionaryKey)) return true;

                Sprite sprite;
                if (!mod.Sprites.TryGetValue(sourceKey, out sprite) || sprite == null) return false;

                SpriteDict.Instance.spriteDictionary[dictionaryKey] = sprite;
                StoryDirector.Log("Portrait sprite bridged: " + dictionaryKey);
                return true;
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Portrait sprite bridge failed for " + dictionaryKey + ": " + ex);
                return false;
            }
        }

        private static bool HasGenderToken(string key)
        {
            return key.IndexOf("male", StringComparison.OrdinalIgnoreCase) >= 0
                || key.IndexOf("female", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
