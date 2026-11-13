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

        // Lydia and Sophia always use their beautified portraits.  The generic
        // game keys are emergency fallbacks only when a local file is missing.
        public const string LydiaSprite = "femaleUpper10";
        public const string MotherSprite = "femaleUpper3";

        // Legacy _vanilla keys remain for save migration only; they are
        // upgraded to the beauty portraits when a save is loaded.
        public const string LydiaPortraitVanilla = "er_portrait_lydia_female_vanilla";
        public const string LydiaPortraitBeauty = "er_portrait_lydia_female_beauty";
        public const string MotherPortraitVanilla = "er_portrait_mother_female_vanilla";
        public const string MotherPortraitBeauty = "er_portrait_mother_female_beauty";
        public const string MotherHomePortraitBeauty = "er_portrait_mother_home_female_beauty";
    }
}
