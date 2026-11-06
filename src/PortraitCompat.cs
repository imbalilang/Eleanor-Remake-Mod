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
    /// is absent or disabled, Resolve() returns the original vanilla key.
    /// </summary>
    public static class PortraitCompat
    {
        public const string BeautyModId = "cute_portraits";
        public const ulong BeautyWorkshopId = 3829696854UL;
        public const string BeautyKeyPrefix = "cp_portrait_";
        public const string LocalPortraitPrefix = "er_portrait_";

        private static LoadedMod beautyMod;

        public static bool IsBeautyActive
        {
            get
            {
                if (beautyMod == null || !beautyMod.IsEnabled)
                {
                    beautyMod = FindBeautyMod();
                }
                return beautyMod != null && beautyMod.IsEnabled;
            }
        }

        public static string Status
        {
            get
            {
                return IsBeautyActive
                    ? "cute_portraits (" + BeautyWorkshopId + ") active; using beautified portraits"
                    : "cute_portraits (" + BeautyWorkshopId + ") absent/disabled; using vanilla portraits";
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
        /// Dual-version custom portraits for Lydia and Mother.  The beauty key
        /// is used when CutePortraits is active; the vanilla key is used
        /// otherwise.  If the requested local sprite has not been delivered yet,
        /// fall back to the generic key (which may itself be beautified).
        /// </summary>
        public static string ResolveCharacter(string vanillaKey, string beautyKey, string fallbackKey)
        {
            string chosen = IsBeautyActive ? beautyKey : vanillaKey;
            if (EnsureLocalPortrait(chosen)) return chosen;
            return Resolve(fallbackKey);
        }

        /// <summary>
        /// Safety net for SpriteDict rebuilds and direct cp_portrait_* lookups.
        /// Called by a GetSprite prefix; does nothing outside the beauty prefix.
        /// </summary>
        public static void EnsureKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

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
            if (IsBeautyActive) return;
            try
            {
                if (PlayerStore.Instance == null || PlayerStore.Instance.storeClientManager == null) return;
                var stack = PlayerStore.Instance.storeClientManager.clientStack;
                if (stack == null) return;
                foreach (StoreClient client in stack)
                {
                    if (client == null || string.IsNullOrEmpty(client.spriteName)) continue;
                    if (client.spriteName.StartsWith(BeautyKeyPrefix, StringComparison.Ordinal))
                    {
                        client.spriteName = RevertKey(client.spriteName);
                    }
                }
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Portrait save sanitisation failed: " + ex);
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
