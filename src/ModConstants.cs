namespace EleanorRomance
{
    public static class ModConstants
    {
        public const string ModId = "eleanor_romance";
        public const string LegacyScenarioId = "eleanor_romance.old_echo";
        public const string EarlyStartPerkId = "eleanor_romance.early_start";
        public const string LocketItemId = "eleanor_romance.locket";
        public const string BraceletItemId = "eleanor_romance.upper_bracelet";
        public const string PlaqueItemId = "eleanor_romance.recommendation_plaque";
        public const string PatronItemId = "eleanor_romance.upper_patron_ornament";
        public const string GratitudeItemId = "eleanor_romance.upper_gratitude_ornament";
        public const string MetaFile = "meta.json";
        public const string LogTag = "[EleanorRomance]";

        // Lydia is a romance lead and keeps a dedicated dual-version portrait.
        // femaleUpper10 is only an emergency fallback if the local Lydia art
        // is missing. Mother is not romanceable and falls back to original
        // femaleUpper3 when CutePortraits is not active.
        public const string LydiaSprite = "femaleUpper10";
        public const string MotherSprite = "femaleUpper3";

        // Lydia's _vanilla art remains active when CutePortraits is absent;
        // the Mother _vanilla file is legacy/cleanup-only.
        public const string LydiaPortraitVanilla = "er_portrait_lydia_female_vanilla";
        public const string LydiaPortraitBeauty = "er_portrait_lydia_female_beauty";
        public const string MotherPortraitVanilla = "er_portrait_mother_female_vanilla";
        public const string MotherPortraitBeauty = "er_portrait_mother_female_beauty";
    }
}
