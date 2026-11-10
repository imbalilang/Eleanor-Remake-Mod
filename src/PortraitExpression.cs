using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EleanorRomance
{
    /// <summary>
    /// Tags dialogue nodes with an expression key at build time and resolves the
    /// actual sprite when the line is displayed (when the current client is known).
    /// Untagged lines keep the previous expression; neutral resets to the base.
    /// </summary>
    public static class PortraitExpression
    {
        private sealed class Tag
        {
            public readonly string Key;
            public readonly string Speaker;
            public Tag(string key, string speaker) { Key = key; Speaker = speaker; }
        }

        private static readonly ConditionalWeakTable<Dialogue, Tag> Tags =
            new ConditionalWeakTable<Dialogue, Tag>();
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>Attach metadata only; no scene/current-client lookup at build time.</summary>
        public static void Apply(Dialogue node, string key, string speaker)
        {
            if (node == null || string.IsNullOrEmpty(key)) return;
            if (!ExpressionContent.ByKey.TryGetValue(key, out var expr) || string.IsNullOrEmpty(expr)) return;
            Tags.Remove(node);
            Tags.Add(node, new Tag(key, speaker));
        }

        /// <summary>Resolve and attach the sprite immediately before the line is shown.</summary>
        public static void Resolve(Dialogue node)
        {
            if (node == null || !PortraitCompat.IsBeautyActive) return;
            if (!Tags.TryGetValue(node, out var tag)) return;

            var client = PlayerStore.Instance != null ? PlayerStore.Instance.currentClientInstance : null;
            if (client == null) return;
            var blueprint = client.GetClientBlueprint();
            if (blueprint == null) return;

            // Only the visible client has a portrait. Do not put Eleanor's face on Lydia.
            if (!string.Equals(tag.Speaker, blueprint.displayName, StringComparison.Ordinal)) return;

            string baseKey = BaseKey(blueprint.spriteName);
            if (string.IsNullOrEmpty(baseKey)) return;
            string expr = ExpressionContent.ByKey[tag.Key];

            if (expr == "neutral")
            {
                node.SetPortrait(BaseSprite(blueprint));
                return;
            }

            string asset = "er_expr_" + baseKey + "_" + expr;
            if (!Cache.TryGetValue(asset, out var sprite))
            {
                sprite = PortraitCompat.GetLocalSprite(asset);
                Cache[asset] = sprite;
            }
            node.SetPortrait(sprite != null ? sprite : BaseSprite(blueprint));
        }

        public static void Reset()
        {
            Cache.Clear();
        }

        private static Sprite BaseSprite(StoreClient blueprint)
        {
            try
            {
                if (SpriteDict.Instance != null && !string.IsNullOrEmpty(blueprint.spriteName))
                    return SpriteDict.Instance.ShowClientSprite(blueprint.spriteName);
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Base portrait lookup failed: " + ex);
            }
            return null;
        }

        private static string BaseKey(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            if (spriteName.StartsWith("cp_portrait_", StringComparison.Ordinal))
            {
                spriteName = spriteName.Substring("cp_portrait_".Length);
                if (spriteName.EndsWith("_female", StringComparison.Ordinal))
                    spriteName = spriteName.Substring(0, spriteName.Length - "_female".Length);
            }
            if (spriteName.IndexOf("eleanor_maid", StringComparison.OrdinalIgnoreCase) >= 0) return "eleanor_maid";
            if (spriteName.IndexOf("eleanor_gala", StringComparison.OrdinalIgnoreCase) >= 0) return "eleanor_gala";
            if (spriteName.IndexOf("lydia_date", StringComparison.OrdinalIgnoreCase) >= 0) return "lydia_date";
            if (spriteName.IndexOf("er_portrait_lydia", StringComparison.OrdinalIgnoreCase) >= 0) return "lydia_daily";
            switch (spriteName)
            {
                case "eleanor1": case "eleanor2": case "eleanor3":
                case "eleanor_scav": case "eleanor_rev":
                    return spriteName;
                default:
                    return null;
            }
        }
    }
}
