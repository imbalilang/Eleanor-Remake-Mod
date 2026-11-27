using System;
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
            public readonly string Expression;
            public readonly string Speaker;
            public Tag(string expression, string speaker) { Expression = expression; Speaker = speaker; }
        }

        private static readonly ConditionalWeakTable<Dialogue, Tag> Tags =
            new ConditionalWeakTable<Dialogue, Tag>();

        /// <summary>Attach metadata only; no scene/current-client lookup at build time.</summary>
        public static void Apply(Dialogue node, string key, string speaker)
        {
            if (node == null || string.IsNullOrEmpty(key)) return;
            if (!ExpressionContent.ByKey.TryGetValue(key, out var expr) || string.IsNullOrEmpty(expr)) return;
            Tags.Remove(node);
            Tags.Add(node, new Tag(expr, speaker));
        }

        /// <summary>Resolve the sprite against the current client immediately before display.</summary>
        internal static Sprite Resolve(Dialogue node)
        {
            if (node == null || !PortraitCompat.IsBeautyActive) return null;
            if (!Tags.TryGetValue(node, out var tag)) return null;

            var client = PlayerStore.Instance?.currentClientInstance;
            if (client == null) return null;
            var blueprint = client.GetClientBlueprint();
            if (blueprint == null) return null;

            // Only the visible client has a portrait. Do not put Eleanor's face on Lydia.
            if (!string.Equals(tag.Speaker, blueprint.displayName, StringComparison.Ordinal)) return null;

            string baseKey = BaseKey(blueprint.spriteName);
            if (baseKey == null) return null;

            Sprite sprite = tag.Expression == "neutral" ? null : FindExpression(baseKey, tag.Expression);
            return sprite != null ? sprite : BaseSprite(blueprint);
        }

        private static Sprite FindExpression(string baseKey, string expression)
        {
            switch (baseKey)
            {
                case "eleanor_love_intimate":
                    return null;
                case "eleanor_revolution_undersuit":
                case "lydia_date":
                case "sophia":
                case "sophia_home":
                    // L2 uses the nearest of the six authored expressions while
                    // keeping the original dialogue tags for other outfits.
                    switch (expression)
                    {
                        case "angry":
                        case "determined":
                            expression = "serious";
                            break;
                        case "sad":
                            expression = "hurt";
                            break;
                        case "tsun":
                        case "flustered":
                            expression = "shy";
                            break;
                        case "tease":
                            expression = "smile";
                            break;
                    }
                    break;
            }

            string prefix = "er_expr_" + baseKey + "_";
            Sprite sprite = PortraitCompat.GetLocalSprite(prefix + expression);
            if (sprite != null) return sprite;

            switch (expression)
            {
                case "shy": return PortraitCompat.GetLocalSprite(prefix + "flustered");
                case "flustered":
                case "tsun": return PortraitCompat.GetLocalSprite(prefix + "shy");
                case "tease": return PortraitCompat.GetLocalSprite(prefix + "smile");
                default: return null;
            }
        }

        private static Sprite BaseSprite(StoreClient blueprint)
        {
            try
            {
                // Our own story keeps its authored outfit/variant.  The
                // SpriteDict fallback intentionally returns the external default
                // for any Eleanor key, so ask the client-aware resolver first.
                if (PortraitCompat.IsEleanorClient(blueprint))
                {
                    Sprite own = PortraitCompat.ResolveEleanorClientSprite(blueprint);
                    if (own != null) return own;
                }
                if (SpriteDict.Instance != null && !string.IsNullOrEmpty(blueprint.spriteName))
                    return SpriteDict.Instance.GetSprite(blueprint.spriteName);
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
            if (spriteName.StartsWith(PortraitCompat.BeautyKeyPrefix, StringComparison.Ordinal))
            {
                spriteName = spriteName.Substring(PortraitCompat.BeautyKeyPrefix.Length);
                if (spriteName.EndsWith("_female", StringComparison.Ordinal))
                    spriteName = spriteName.Substring(0, spriteName.Length - "_female".Length);
            }
            switch (spriteName)
            {
                case "eleanor1": case "eleanor2": case "eleanor3":
                case "eleanor_scav": case "eleanor_rev":
                    return spriteName;
                case ModConstants.EleanorPlainCasualPortrait: return "eleanor1_no_earrings";
                case PortraitCompat.MaidKey: return "eleanor_maid";
                case PortraitCompat.GalaKey: return "eleanor_gala";
                case ModConstants.EleanorVestPortrait: return "eleanor_revolution_undersuit";
                case ModConstants.EleanorIntimatePortrait: return "eleanor_love_intimate";
                case ModConstants.LydiaPortraitBeauty: return "lydia_daily";
                case PortraitCompat.LydiaDateBeautyKey: return "lydia_date";
                case ModConstants.MotherPortraitBeauty: return "sophia";
                case ModConstants.MotherHomePortraitBeauty: return "sophia_home";
                default:
                    return null;
            }
        }
    }
}
